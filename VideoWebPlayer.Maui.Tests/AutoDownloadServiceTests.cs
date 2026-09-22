#pragma warning disable xUnit1051 // Kurzlebige Unit-Tests: TestContext-CancellationToken nicht erforderlich
using System.Net;
using System.Text.Json;
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
        service = new AutoDownloadService(authSession, lists, downloadService, networkMonitor);
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
