#pragma warning disable xUnit1051 // Kurzlebige Unit-Tests: TestContext-CancellationToken nicht erforderlich
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using VideoPlayer.ApiClient.Models;
using VideoPlayer.Mvu;
using VideoPlayer.Storage;
using VideoPlayer.Storage.Entities;
using VideoWebPlayer.Maui.Tests.Stubs;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>
/// Automatische Downloads (Schritt 11): Continue-Watching-Kandidaten,
/// Episoden-Auflösung, Preload-Anzahl, WLAN-only/Enabled-Schalter,
/// Dedup und Throttling — mit echter SQLite-Ablage.
/// </summary>
[Collection("SqliteDatabase")]
public class AutoDownloadServiceTests : IDisposable
{
    private static readonly Uri Server = new("http://testserver/videoplayer/");

    private readonly string root;
    private readonly AppStorageLayout layout;
    private readonly StubHttpMessageHandler handler = new();
    private readonly AuthSessionService authSession;
    private readonly CacheDatabase cache;
    private readonly FakeNetworkMonitor networkMonitor = new();
    private readonly DownloadService downloadService;
    private readonly AutoDownloadService service;
    private readonly AppSettings settings = new()
    {
        AutoDownloadEnabled = true,
        PreloadEpisodeCount = 2,
        WifiOnlyDownloads = true
    };

    /// <summary>Initialisiert eine neue Instanz der Testklasse <c>AutoDownloadServiceTests</c>.</summary>
    public AutoDownloadServiceTests()
    {
        root = Path.Combine(Path.GetTempPath(), "videoplayer-autodownload-tests", Guid.NewGuid().ToString("N"));
        layout = new AppStorageLayout(
            backupRoot: Path.Combine(root, "backup"),
            noBackupRoot: Path.Combine(root, "nobackup"),
            cacheRoot: Path.Combine(root, "cache"),
            profile: AppProfile.Create(null));
        layout.EnsureDirectoriesExist();
        cache = new CacheDatabase(layout);
        cache.InitializeAsync().Wait();

        var secrets = new AppSecretsService(
            new InMemorySettingsStore(), new InMemorySecretStore(), AppProfile.Create(null));
        authSession = new AuthSessionService(
            secrets,
            uri => new HttpClient(handler, disposeHandler: false) { BaseAddress = uri },
            new RecordingDelays().Delay);
        downloadService = new DownloadService(
            authSession, secrets, cache, layout, networkMonitor,
            () => new HttpClient(handler, disposeHandler: false) { Timeout = Timeout.InfiniteTimeSpan },
            File.Exists, null);
        var lists = new HomeListService(cache);
        service = new AutoDownloadService(authSession, lists, downloadService, networkMonitor, cache);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        SQLite.SQLiteAsyncConnection.ResetPool();
        // Hintergrund-Transfers können Datei-/DB-Handles kurz halten.
        for (var attempt = 0; attempt < 30; attempt++)
        {
            try
            {
                if (Directory.Exists(root))
                {
                    Directory.Delete(root, recursive: true);
                }

                return;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Thread.Sleep(100);
            }
        }
    }

    private async Task LoginAsync()
    {
        handler.EnqueueJson(HttpStatusCode.OK, new { token = "jwt", expires = "2030-01-01T00:00:00Z" });
        await authSession.LoginAsync(Server, "martin", "pw");
        handler.Requests.Clear();
    }

    private async Task CacheContinueWatchingAsync(params ContinueWatchingEntry[] entries)
    {
        await cache.Connection.DeleteAllAsync<CachedListEntry>();
        await cache.Connection.InsertAsync(new CachedListEntry
        {
            Key = ServerSyncService.ContinueWatchingCacheKey,
            Json = JsonSerializer.Serialize(entries),
            UpdatedAtUtc = DateTime.UtcNow
        });
    }

    private static TvShow ShowWithEpisodes(long id, string name, params (int Season, int Episode, long Id, string Ep)[] eps)
    {
        // Kein Show-Rückverweis auf den Staffeln — der Server liefert die
        // Staffeln ohne zyklische Einbettung (JSON würde sonst in einen
        // Objektzyklus laufen).
        var show = new TvShow { Id = id, Name = name };
        show.Seasons = eps
            .GroupBy(e => e.Season)
            .Select(g => new TvShowSeason
            {
                Id = g.Key + 100,
                Number = g.Key,
                Name = $"Staffel {g.Key}",
                Episodes = g.Select(e => new TvShowEpisode
                {
                    Id = e.Id,
                    Number = e.Episode,
                    Name = e.Ep
                }).ToArray()
            })
            .ToArray();
        return show;
    }

    private async Task<List<DownloadManifestEntry>> EntriesAsync() =>
        await cache.Connection.Table<DownloadManifestEntry>().ToListAsync();

    private static async Task WaitForAsync(Func<Task<bool>> done, int timeoutMs = 15000)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromMilliseconds(timeoutMs);
        while (!await done())
        {
            Assert.True(DateTime.UtcNow < deadline, "Zeitüberschreitung beim Warten.");
            await Task.Delay(50);
        }
    }

    /// <summary>Prüft Auto-Downloads: Film-Kandidat wird ohne Server-Roundtrip eingereiht.</summary>
    [Fact]
    public async Task Run_MovieCandidate_EnqueuesDirectly()
    {
        await LoginAsync();
        await CacheContinueWatchingAsync(
            new ContinueWatchingEntry
            {
                MediaType = "movie",
                PositionSeconds = 60,
                Entry = new Movie { Id = 7, Name = "Film Sieben" }
            });
        handler.Enqueue(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent([1, 2, 3])
        });

        await service.RunAsync(settings);
        // Der Transfer läuft asynchron — auf den Endzustand warten.
        await WaitForAsync(async () =>
            (await EntriesAsync()).SingleOrDefault()?.State == "completed");

        var entry = Assert.Single(await EntriesAsync());
        Assert.Equal(7, entry.VideoId);
        Assert.Equal("movie", entry.VideoType);
        Assert.Equal("Film Sieben", entry.Title);
        // Genau ein Request: der Stream-Download, kein Detail-Request.
        var request = Assert.Single(handler.Requests);
        Assert.Contains("api/items/movie/7/stream", request.PathAndQuery);
        Assert.Contains("access_token=jwt", request.PathAndQuery);
    }

    /// <summary>Prüft Auto-Downloads: Episode löst die Serie auf und reiht die nächsten <c>PreloadEpisodeCount</c> Folge-Episoden ein.</summary>
    [Fact]
    public async Task Run_EpisodeCandidate_EnqueuesUpcomingEpisodes()
    {
        await LoginAsync();
        await CacheContinueWatchingAsync(
            new ContinueWatchingEntry
            {
                MediaType = "episode",
                PositionSeconds = 120,
                Entry = new TvShowEpisode
                {
                    Id = 32,
                    Name = "Pilot",
                    Season = new TvShowSeason { Id = 21, Show = new TvShow { Id = 11 } }
                }
            });
        handler.EnqueueJson(HttpStatusCode.OK,
            ShowWithEpisodes(11, "Probe", (1, 1, 32, "A"), (1, 2, 33, "B"), (1, 3, 34, "C")));

        await service.RunAsync(settings);

        var entries = await EntriesAsync();
        Assert.Equal(2, entries.Count); // PreloadEpisodeCount = 2 → die zwei Folge-Episoden
        Assert.Equal(33, entries[0].VideoId);
        Assert.Equal(34, entries[1].VideoId);
        Assert.All(entries, e => Assert.Equal("tvshowepisode", e.VideoType));
        Assert.All(entries, e => Assert.StartsWith("Probe ", e.Title));
        // Serien-Detail-Request plus die gestarteten Stream-Requests.
        Assert.Contains(handler.Requests, r => r.PathAndQuery.Contains("api/items/tvshow/11"));
    }

    /// <summary>Prüft Auto-Downloads: nach der letzten Episode einer Staffel geht es staffelübergreifend weiter.</summary>
    [Fact]
    public async Task Run_EpisodeCandidate_CrossesIntoNextSeason()
    {
        await LoginAsync();
        await CacheContinueWatchingAsync(
            new ContinueWatchingEntry
            {
                MediaType = "episode",
                PositionSeconds = 60,
                Entry = new TvShowEpisode
                {
                    Id = 36, // Staffel 1, letzte Episode
                    Name = "Finale",
                    Season = new TvShowSeason { Id = 21, Number = 1, Show = new TvShow { Id = 11 } }
                }
            });
        handler.EnqueueJson(HttpStatusCode.OK,
            ShowWithEpisodes(11, "Probe", (1, 5, 35, "S1E5"), (1, 6, 36, "S1E6"), (2, 1, 37, "S2E1"), (2, 2, 38, "S2E2")));

        var oneEpisode = settings with { PreloadEpisodeCount = 1 };
        await service.RunAsync(oneEpisode);

        // Kein Kandidat mehr in Staffel 1 → der flache Nachfolger S2E1.
        var entry = Assert.Single(await EntriesAsync());
        Assert.Equal(37, entry.VideoId);
        Assert.Equal(11, entry.ShowId);
        Assert.Equal("Probe", entry.ShowTitle);
        Assert.Equal(2, entry.SeasonNumber);
        Assert.Equal(1, entry.EpisodeNumber);
    }

    /// <summary>Prüft Auto-Downloads: deaktiviert → kein Auftrag, kein Request.</summary>
    [Fact]
    public async Task Run_Disabled_DoesNothing()
    {
        await LoginAsync();
        await CacheContinueWatchingAsync(
            new ContinueWatchingEntry
            {
                MediaType = "movie",
                PositionSeconds = 60,
                Entry = new Movie { Id = 7, Name = "Film Sieben" }
            });
        var disabled = settings with { AutoDownloadEnabled = false };

        await service.RunAsync(disabled);

        Assert.Empty(await EntriesAsync());
        Assert.Empty(handler.Requests);
    }

    /// <summary>Prüft Auto-Downloads: WLAN-only ohne WLAN → nichts.</summary>
    [Fact]
    public async Task Run_WifiOnlyWithoutWifi_DoesNothing()
    {
        await LoginAsync();
        await CacheContinueWatchingAsync(
            new ContinueWatchingEntry
            {
                MediaType = "movie",
                PositionSeconds = 60,
                Entry = new Movie { Id = 7, Name = "Film Sieben" }
            });
        networkMonitor.IsWifi = false;

        await service.RunAsync(settings);

        Assert.Empty(await EntriesAsync());
        Assert.Empty(handler.Requests);
    }

    /// <summary>Prüft Auto-Downloads: ohne Serververbindung → nichts.</summary>
    [Fact]
    public async Task Run_NotLoggedIn_DoesNothing()
    {
        await service.RunAsync(settings);

        Assert.Empty(await EntriesAsync());
        Assert.Empty(handler.Requests);
    }

    /// <summary>Prüft Auto-Downloads: bereits im Manifest vorhandene Aufträge werden nicht doppelt angelegt.</summary>
    [Fact]
    public async Task Run_ExistingManifestEntry_Deduplicates()
    {
        await LoginAsync();
        networkMonitor.IsWifi = false;
        // Vorhandener wartender Auftrag für Film 7 (WLAN-only blockiert den Start).
        await downloadService.EnqueueAsync("movie", 7, "Film Sieben", settings);
        await CacheContinueWatchingAsync(
            new ContinueWatchingEntry
            {
                MediaType = "movie",
                PositionSeconds = 60,
                Entry = new Movie { Id = 7, Name = "Film Sieben" }
            });
        handler.Requests.Clear();
        networkMonitor.IsWifi = true;

        await service.RunAsync(settings);

        Assert.Single(await EntriesAsync());
    }

    /// <summary>Prüft Auto-Downloads: Intervall-Throttling (nur <c>force</c> läuft erneut).</summary>
    [Fact]
    public async Task Run_Throttled_UnlessForced()
    {
        await LoginAsync();
        await CacheContinueWatchingAsync(
            new ContinueWatchingEntry
            {
                MediaType = "episode",
                PositionSeconds = 60,
                Entry = new TvShowEpisode
                {
                    Id = 32,
                    Name = "Pilot",
                    Season = new TvShowSeason { Id = 21, Show = new TvShow { Id = 11 } }
                }
            });
        handler.EnqueueJson(HttpStatusCode.OK, ShowWithEpisodes(11, "Probe", (1, 2, 33, "B")));
        handler.EnqueueJson(HttpStatusCode.OK, ShowWithEpisodes(11, "Probe", (1, 2, 33, "B")));

        await service.RunAsync(settings);
        var tvShowRequests = handler.Requests.Count(r => r.PathAndQuery.Contains("api/items/tvshow/"));
        Assert.Equal(1, tvShowRequests);

        await service.RunAsync(settings); // innerhalb des Intervalls → übersprungen
        Assert.Equal(tvShowRequests, handler.Requests.Count(r => r.PathAndQuery.Contains("api/items/tvshow/")));

        await service.RunAsync(settings, force: true);
        Assert.Equal(tvShowRequests + 1, handler.Requests.Count(r => r.PathAndQuery.Contains("api/items/tvshow/")));
    }

    /// <summary>Prüft Auto-Downloads: Episoden ohne Staffelbezug werden übersprungen.</summary>
    [Fact]
    public async Task Run_EpisodeWithoutSeason_SkipsCandidate()
    {
        await LoginAsync();
        await CacheContinueWatchingAsync(
            new ContinueWatchingEntry
            {
                MediaType = "episode",
                PositionSeconds = 60,
                Entry = new TvShowEpisode { Id = 32, Name = "Pilot" }
            });

        await service.RunAsync(settings);

        Assert.Empty(await EntriesAsync());
        Assert.Empty(handler.Requests);
    }

    /// <summary>Hilfsmethode: baut die JSON-Antwort einer <c>entries/paged</c>-Seite (Server-Reihenfolge = Array-Reihenfolge).</summary>
    /// <param name="hasNextPage"><c>true</c> = weitere Seite folgt.</param>
    /// <param name="entries">Die Einträge der Seite (siehe <see cref="PlaylistEntryJson"/>).</param>
    /// <returns>Das serialisierbare Antwortobjekt der Seite.</returns>
    private static object PlaylistPageJson(bool hasNextPage, params object[] entries) => new
    {
        entries,
        totalCount = entries.Length,
        hasNextPage,
        pageNumber = 1,
        pageSize = 50
    };

    /// <summary>Hilfsmethode: baut den JSON-Eintrag einer Playlist (Felder wie der Server).</summary>
    /// <param name="id">Id des Playlist-Eintrags.</param>
    /// <param name="mediaType">Medientyp der Referenz (Server-Konvention, z. B. <c>Movie</c>, <c>TVShowEpisode</c>).</param>
    /// <param name="mediaId">Id des referenzierten Mediums.</param>
    /// <param name="title">Anzeigetitel des Mediums.</param>
    /// <param name="parentMediaType">Medientyp des Elternelements; <c>null</c> = kein Elternkontext.</param>
    /// <param name="parentMediaId">Id des Elternelements; <c>null</c> = kein Elternkontext.</param>
    /// <param name="parentMediaTitle">Titel des Elternelements; <c>null</c> = kein Elternkontext.</param>
    /// <param name="isAccessible"><c>false</c> = kein Zugriffsrecht des Benutzers.</param>
    /// <param name="sortOrder">Sortierposition in der Playlist; <c>null</c> = nicht zugewiesen.</param>
    /// <param name="episodeNumber">Episodennummer bei Episoden-Einträgen; <c>null</c> sonst.</param>
    /// <returns>Das serialisierbare Eintragsobjekt.</returns>
    private static object PlaylistEntryJson(
        long id,
        string mediaType,
        long mediaId,
        string title,
        string? parentMediaType = null,
        long? parentMediaId = null,
        string? parentMediaTitle = null,
        bool isAccessible = true,
        long? sortOrder = null,
        int? episodeNumber = null) => new
        {
            id,
            playlistId = 5L,
            mediaType,
            mediaId,
            mediaTitle = title,
            parentMediaType,
            parentMediaId,
            parentMediaTitle,
            addedAt = "2026-01-01T00:00:00Z",
            resolvedPictureId = (long?)null,
            isAccessible,
            sortOrder,
            releaseDate = (string?)null,
            plot = (string?)null,
            episodeNumber
        };

    /// <summary>Prüft Auto-Downloads: ein Weiterschauen-Eintrag mit Playlist-Bezug reiht die Folge-Einträge in Playlist- statt Serienreihenfolge ein.</summary>
    [Fact]
    public async Task Run_PlaylistEpisode_EnqueuesPlaylistOrder()
    {
        await LoginAsync();
        await CacheContinueWatchingAsync(
            new ContinueWatchingEntry
            {
                MediaType = "episode",
                PositionSeconds = 60,
                PlaylistId = 5,
                PlaylistName = "Mix",
                PlaylistEntryId = 101,
                Entry = new TvShowEpisode
                {
                    Id = 32,
                    Name = "Pilot",
                    Season = new TvShowSeason { Id = 21, Show = new TvShow { Id = 11 } }
                }
            });
        // Playlist-Reihenfolge weicht absichtlich von der Serienreihenfolge
        // ab: Film 99 liegt zwischen den Episoden 33 und 34.
        handler.EnqueueJson(HttpStatusCode.OK, PlaylistPageJson(false,
            PlaylistEntryJson(100, "TVShowEpisode", 30, "Alt"),
            PlaylistEntryJson(101, "TVShowEpisode", 32, "Pilot"),
            PlaylistEntryJson(102, "Movie", 99, "Film Neunundneunzig"),
            PlaylistEntryJson(103, "TVShowEpisode", 33, "Folge B",
                parentMediaType: "TVShow", parentMediaId: 11, parentMediaTitle: "Probe",
                sortOrder: 3, episodeNumber: 2)));

        await service.RunAsync(settings);

        var entries = (await EntriesAsync()).OrderBy(e => e.Id).ToList();
        Assert.Equal(2, entries.Count);
        Assert.Equal(99, entries[0].VideoId);
        Assert.Equal("movie", entries[0].VideoType);
        Assert.Equal(33, entries[1].VideoId);
        Assert.Equal("tvshowepisode", entries[1].VideoType);
        // Playlist-Bezug am Manifest; die Serienstruktur wird nicht abgefragt.
        Assert.Equal(5, entries[0].PlaylistId);
        Assert.Equal(102, entries[0].PlaylistEntryId);
        Assert.Equal(5, entries[1].PlaylistId);
        Assert.Equal(103, entries[1].PlaylistEntryId);
        Assert.Equal(3, entries[1].PlaylistSortOrder);
        Assert.DoesNotContain(handler.Requests, r => r.PathAndQuery.Contains("api/items/tvshow/"));
        Assert.Contains(handler.Requests, r => r.PathAndQuery.Contains("api/playlists/5/entries/paged"));
    }

    /// <summary>Prüft Auto-Downloads: nicht ladbare und nicht zugängliche Playlist-Einträge werden übersprungen.</summary>
    [Fact]
    public async Task Run_PlaylistEpisode_SkipsNonDownloadableEntries()
    {
        await LoginAsync();
        await CacheContinueWatchingAsync(
            new ContinueWatchingEntry
            {
                MediaType = "episode",
                PositionSeconds = 60,
                PlaylistId = 5,
                PlaylistName = "Mix",
                PlaylistEntryId = 101,
                Entry = new TvShowEpisode
                {
                    Id = 32,
                    Name = "Pilot",
                    Season = new TvShowSeason { Id = 21, Show = new TvShow { Id = 11 } }
                }
            });
        handler.EnqueueJson(HttpStatusCode.OK, PlaylistPageJson(false,
            PlaylistEntryJson(101, "TVShowEpisode", 32, "Pilot"),
            PlaylistEntryJson(102, "TVShow", 60, "Ganze Serie"),
            PlaylistEntryJson(103, "TVShowEpisode", 61, "Gesperrt", isAccessible: false),
            PlaylistEntryJson(104, "TVShowEpisode", 62, "Folge Eins",
                parentMediaType: "TVShow", parentMediaId: 11, parentMediaTitle: "Probe"),
            PlaylistEntryJson(105, "TVShowEpisode", 63, "Folge Zwei",
                parentMediaType: "TVShow", parentMediaId: 11, parentMediaTitle: "Probe")));

        await service.RunAsync(settings);

        var entries = (await EntriesAsync()).OrderBy(e => e.Id).ToList();
        Assert.Equal(2, entries.Count);
        Assert.Equal(62, entries[0].VideoId);
        Assert.Equal(63, entries[1].VideoId);
    }

    /// <summary>Prüft Auto-Downloads: Serien-Hints der Playlist-Einträge werden aus den Parent-Angaben ins Manifest übernommen.</summary>
    [Fact]
    public async Task Run_PlaylistEpisode_PersistsSeriesHints()
    {
        await LoginAsync();
        await CacheContinueWatchingAsync(
            new ContinueWatchingEntry
            {
                MediaType = "episode",
                PositionSeconds = 60,
                PlaylistId = 5,
                PlaylistName = "Mix",
                PlaylistEntryId = 101,
                Entry = new TvShowEpisode
                {
                    Id = 32,
                    Name = "Pilot",
                    Season = new TvShowSeason { Id = 21, Show = new TvShow { Id = 11 } }
                }
            });
        handler.EnqueueJson(HttpStatusCode.OK, PlaylistPageJson(false,
            PlaylistEntryJson(101, "TVShowEpisode", 32, "Pilot"),
            PlaylistEntryJson(102, "TVShowEpisode", 33, "Folge B",
                parentMediaType: "TVShow", parentMediaId: 11, parentMediaTitle: "Probe",
                episodeNumber: 2)));

        var oneEpisode = settings with { PreloadEpisodeCount = 1 };
        await service.RunAsync(oneEpisode);

        var entry = Assert.Single(await EntriesAsync());
        Assert.Equal(33, entry.VideoId);
        Assert.Equal(11, entry.ShowId);
        Assert.Equal("Probe", entry.ShowTitle);
        Assert.Equal(2, entry.EpisodeNumber);
        Assert.Null(entry.SeasonNumber);
        Assert.Equal("Probe – Folge B", entry.Title);
    }

    /// <summary>Prüft Auto-Downloads: ein playlist-gebundener Film lädt sich selbst plus die Folge-Einträge der Playlist.</summary>
    [Fact]
    public async Task Run_PlaylistMovie_EnqueuesSelfAndNextEntries()
    {
        await LoginAsync();
        await CacheContinueWatchingAsync(
            new ContinueWatchingEntry
            {
                MediaType = "movie",
                PositionSeconds = 60,
                PlaylistId = 5,
                PlaylistName = "Filmabend",
                PlaylistEntryId = 101,
                Entry = new Movie { Id = 7, Name = "Film Sieben" }
            });
        handler.EnqueueJson(HttpStatusCode.OK, PlaylistPageJson(false,
            PlaylistEntryJson(101, "Movie", 7, "Film Sieben"),
            PlaylistEntryJson(102, "Movie", 8, "Film Acht"),
            PlaylistEntryJson(103, "Movie", 9, "Film Neun")));

        await service.RunAsync(settings);

        var entries = (await EntriesAsync()).OrderBy(e => e.Id).ToList();
        Assert.Equal(3, entries.Count);
        Assert.Equal(7, entries[0].VideoId);
        Assert.Equal(8, entries[1].VideoId);
        Assert.Equal(9, entries[2].VideoId);
        Assert.All(entries, e => Assert.Equal(5, e.PlaylistId));
    }

    /// <summary>Prüft Auto-Downloads: ein nicht mehr in der Playlist gefundener Eintrag fällt auf den Serien-Pfad zurück.</summary>
    [Fact]
    public async Task Run_PlaylistEntryNotFound_FallsBackToSeries()
    {
        await LoginAsync();
        await CacheContinueWatchingAsync(
            new ContinueWatchingEntry
            {
                MediaType = "episode",
                PositionSeconds = 60,
                PlaylistId = 5,
                PlaylistName = "Mix",
                PlaylistEntryId = 101,
                Entry = new TvShowEpisode
                {
                    Id = 32,
                    Name = "Pilot",
                    Season = new TvShowSeason { Id = 21, Show = new TvShow { Id = 11 } }
                }
            });
        // Der Eintrag 101 ist nicht mehr Teil der Playlist.
        handler.EnqueueJson(HttpStatusCode.OK, PlaylistPageJson(false,
            PlaylistEntryJson(100, "TVShowEpisode", 30, "Alt"),
            PlaylistEntryJson(105, "TVShowEpisode", 40, "Anderes")));
        handler.EnqueueJson(HttpStatusCode.OK,
            ShowWithEpisodes(11, "Probe", (1, 1, 32, "A"), (1, 2, 33, "B"), (1, 3, 34, "C")));

        await service.RunAsync(settings);

        var entries = (await EntriesAsync()).OrderBy(e => e.Id).ToList();
        Assert.Equal(2, entries.Count);
        Assert.Equal(33, entries[0].VideoId);
        Assert.Equal(34, entries[1].VideoId);
        Assert.All(entries, e => Assert.Null(e.PlaylistId));
        Assert.Contains(handler.Requests, r => r.PathAndQuery.Contains("api/items/tvshow/11"));
    }

    /// <summary>Prüft Auto-Downloads: Playlist-Bezug ohne gültige Eintrags-Id (<c>PlaylistEntryId == null</c>) bleibt beim Serien-Pfad.</summary>
    [Fact]
    public async Task Run_PlaylistEntryMissing_FallsBackToSeries()
    {
        await LoginAsync();
        await CacheContinueWatchingAsync(
            new ContinueWatchingEntry
            {
                MediaType = "episode",
                PositionSeconds = 60,
                PlaylistId = 5,
                PlaylistName = "Mix",
                PlaylistEntryId = null,
                Entry = new TvShowEpisode
                {
                    Id = 32,
                    Name = "Pilot",
                    Season = new TvShowSeason { Id = 21, Show = new TvShow { Id = 11 } }
                }
            });
        handler.EnqueueJson(HttpStatusCode.OK,
            ShowWithEpisodes(11, "Probe", (1, 1, 32, "A"), (1, 2, 33, "B")));

        var oneEpisode = settings with { PreloadEpisodeCount = 1 };
        await service.RunAsync(oneEpisode);

        var entry = Assert.Single(await EntriesAsync());
        Assert.Equal(33, entry.VideoId);
        Assert.Null(entry.PlaylistId);
        Assert.DoesNotContain(handler.Requests, r => r.PathAndQuery.Contains("api/playlists/"));
    }

    /// <summary>Prüft die Protokolleinträge: Lauf-Ergebnis und Mindestabstand-Auslassung landen als Information.</summary>
    [Fact]
    public async Task RunAsync_LogsOutcome()
    {
        var logging = new RecordingLoggerProvider();
        var loggedService = new AutoDownloadService(
            authSession, new HomeListService(cache), downloadService, networkMonitor, cache,
            logging.CreateLogger<AutoDownloadService>());
        await LoginAsync();
        await CacheContinueWatchingAsync(
            new ContinueWatchingEntry
            {
                MediaType = "movie",
                PositionSeconds = 60,
                Entry = new Movie { Id = 7, Name = "Film Sieben" }
            });
        handler.Enqueue(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent([1, 2, 3])
        });

        await loggedService.RunAsync(settings);
        Assert.Contains(logging.Entries, e =>
            e.Level == LogLevel.Information
            && e.Category.Contains(nameof(AutoDownloadService), StringComparison.Ordinal)
            && e.Message.Contains("abgeschlossen", StringComparison.Ordinal));

        // Sofortiger Zweitlauf ohne force → Mindestabstand-Auslassung.
        await loggedService.RunAsync(settings);
        Assert.Contains(logging.Entries, e =>
            e.Level == LogLevel.Information
            && e.Category.Contains(nameof(AutoDownloadService), StringComparison.Ordinal)
            && e.Message.Contains("Mindestabstand", StringComparison.Ordinal));
    }

    private sealed class FakeNetworkMonitor : INetworkStatusMonitor
    {
        public bool IsOnline { get; set; } = true;
        public bool IsWifi { get; set; } = true;
        public event EventHandler<bool>? AvailabilityChanged;
        public void Start() { }
        public void SetOnline(bool online)
        {
            IsOnline = online;
            AvailabilityChanged?.Invoke(this, online);
        }
    }
}
