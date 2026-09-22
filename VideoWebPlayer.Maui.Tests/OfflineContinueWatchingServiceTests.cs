#pragma warning disable xUnit1051 // Kurzlebige Unit-Tests: TestContext-CancellationToken nicht erforderlich
using System.Text.Json;
using VideoPlayer.ApiClient.Models;
using VideoPlayer.Mvu;
using VideoPlayer.Storage;
using VideoPlayer.Storage.Entities;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>
/// <see cref="OfflineContinueWatchingService"/> (Schritt 12): bildet die
/// Offline-„Weiterschauen"-Liste ausschließlich aus abgeschlossenen
/// lokalen Downloads — fehlende Folge-Episoden erscheinen als
/// Platzhalter; Fortschritt aus der lokalen Outbox
/// (<c>pending_progress</c>), Favoriten/Neuheiten aus den Caches —
/// mit echter SQLite-Ablage.
/// </summary>
[Collection("SqliteDatabase")]
public class OfflineContinueWatchingServiceTests : IDisposable
{
    private readonly string root;
    private readonly CacheDatabase cache;
    private readonly BackupDatabase backup;
    private readonly HashSet<string> existingFiles = new(StringComparer.OrdinalIgnoreCase);
    private readonly OfflineContinueWatchingService service;

    /// <summary>Initialisiert eine neue Instanz der Testklasse <c>OfflineContinueWatchingServiceTests</c>.</summary>
    public OfflineContinueWatchingServiceTests()
    {
        root = Path.Combine(Path.GetTempPath(), "videoplayer-offlinecw-tests", Guid.NewGuid().ToString("N"));
        var layout = new AppStorageLayout(
            backupRoot: Path.Combine(root, "backup"),
            noBackupRoot: Path.Combine(root, "nobackup"),
            cacheRoot: Path.Combine(root, "cache"),
            profile: AppProfile.Create(null));
        layout.EnsureDirectoriesExist();
        cache = new CacheDatabase(layout);
        cache.InitializeAsync().Wait();
        backup = new BackupDatabase(layout);
        backup.InitializeAsync().Wait();
        service = new OfflineContinueWatchingService(
            cache, backup, new HomeListService(cache), existingFiles.Contains);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        SQLite.SQLiteAsyncConnection.ResetPool();
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private async Task AddManifestEntryAsync(
        int videoId,
        string videoType,
        string state,
        string? filePath,
        DateTime? updatedAt = null,
        long? showId = null,
        string? showTitle = null,
        int? seasonNumber = null,
        int? episodeNumber = null,
        string title = "")
    {
        await cache.Connection.InsertAsync(new DownloadManifestEntry
        {
            VideoId = videoId,
            VideoType = videoType,
            Title = title,
            FilePath = filePath ?? string.Empty,
            State = state,
            ShowId = showId,
            ShowTitle = showTitle,
            SeasonNumber = seasonNumber,
            EpisodeNumber = episodeNumber,
            UpdatedAtUtc = updatedAt ?? DateTime.UtcNow
        });
    }

    private async Task WriteShowAsync(CachedShowEntry show)
    {
        await cache.Connection.InsertOrReplaceAsync(new CachedListEntry
        {
            Key = ShowDetailsCache.KeyFor(show.ShowId),
            Json = JsonSerializer.Serialize(show),
            UpdatedAtUtc = DateTime.UtcNow
        });
    }

    /// <summary>Prüft Weiterschauen: ein abgeschlossener Film-Download mit vorhandener Datei erscheint in der Liste.</summary>
    [Fact]
    public async Task ContinueWatching_CompletedMovieWithFile_Appears()
    {
        existingFiles.Add("/downloads/movie-7.mp4");
        await AddManifestEntryAsync(7, "movie", "completed", "/downloads/movie-7.mp4", title: "Film Sieben");

        var lists = await service.LoadHomeListsAsync();

        var item = Assert.Single(lists.ContinueWatching);
        Assert.Equal(7, item.Id);
        Assert.Equal("movie", item.MediaType);
        Assert.Equal("Film Sieben", item.Title);
        Assert.True(item.IsSelectable);
        Assert.False(item.IsPlaceholder);
    }

    /// <summary>Prüft Weiterschauen: nicht abgeschlossene Downloads und fehlende Dateien werden herausgefiltert.</summary>
    [Fact]
    public async Task ContinueWatching_IncompleteOrMissingFiles_AreFiltered()
    {
        existingFiles.Add("/downloads/movie-7.mp4");
        await AddManifestEntryAsync(7, "movie", "completed", "/downloads/movie-7.mp4", title: "Ok");
        await AddManifestEntryAsync(8, "movie", "downloading", "/downloads/movie-8.mp4", title: "Läuft");
        await AddManifestEntryAsync(9, "movie", "queued", "/downloads/movie-9.mp4", title: "Wartet");
        await AddManifestEntryAsync(10, "movie", "failed", "/downloads/movie-10.mp4", title: "Fehler");
        await AddManifestEntryAsync(11, "movie", "completed", "/downloads/weg.mp4", title: "Datei weg");

        var lists = await service.LoadHomeListsAsync();

        var item = Assert.Single(lists.ContinueWatching);
        Assert.Equal(7, item.Id);
    }

    /// <summary>Prüft Weiterschauen: die fehlende Folge-Episode einer Serie erscheint als nicht auswählbarer Platzhalter.</summary>
    [Fact]
    public async Task ContinueWatching_EpisodeWithMissingNext_ShowsPlaceholder()
    {
        existingFiles.Add("/downloads/ep-31.mp4");
        await AddManifestEntryAsync(
            31, "tvshowepisode", "completed", "/downloads/ep-31.mp4",
            showId: 11, showTitle: "Serie", seasonNumber: 1, episodeNumber: 1,
            title: "Serie – Pilot");
        await WriteShowAsync(new CachedShowEntry
        {
            ShowId = 11,
            Title = "Serie",
            Seasons =
            [
                new CachedShowSeason
                {
                    Id = 21,
                    Number = 1,
                    Episodes =
                    [
                        new CachedShowEpisode { Id = 31, Number = 1, Title = "Pilot" },
                        new CachedShowEpisode { Id = 32, Number = 2, Title = "Folge 2" }
                    ]
                }
            ]
        });

        var lists = await service.LoadHomeListsAsync();

        Assert.Equal(2, lists.ContinueWatching.Count);
        var playable = lists.ContinueWatching[0];
        Assert.Equal(31, playable.Id);
        Assert.True(playable.IsSelectable);
        var placeholder = lists.ContinueWatching[1];
        Assert.Equal(32, placeholder.Id);
        Assert.True(placeholder.IsPlaceholder);
        Assert.False(placeholder.IsSelectable);
        Assert.Equal(11, placeholder.ShowId);
        Assert.Equal(21, placeholder.SeasonId);
        Assert.Equal("tvshowepisode", placeholder.MediaType);
        Assert.Contains("Serie", placeholder.Title);
    }

    /// <summary>Prüft Weiterschauen: ist die Folge-Episode ebenfalls heruntergeladen, entsteht kein Platzhalter.</summary>
    [Fact]
    public async Task ContinueWatching_NextEpisodeDownloaded_NoPlaceholder()
    {
        existingFiles.Add("/downloads/ep-31.mp4");
        existingFiles.Add("/downloads/ep-32.mp4");
        await AddManifestEntryAsync(
            31, "tvshowepisode", "completed", "/downloads/ep-31.mp4",
            showId: 11, showTitle: "Serie", seasonNumber: 1, episodeNumber: 1);
        await AddManifestEntryAsync(
            32, "tvshowepisode", "completed", "/downloads/ep-32.mp4",
            showId: 11, showTitle: "Serie", seasonNumber: 1, episodeNumber: 2);
        await WriteShowAsync(new CachedShowEntry
        {
            ShowId = 11,
            Title = "Serie",
            Seasons =
            [
                new CachedShowSeason
                {
                    Id = 21,
                    Number = 1,
                    Episodes =
                    [
                        new CachedShowEpisode { Id = 31, Number = 1, Title = "A" },
                        new CachedShowEpisode { Id = 32, Number = 2, Title = "B" }
                    ]
                }
            ]
        });

        var lists = await service.LoadHomeListsAsync();

        Assert.Equal(2, lists.ContinueWatching.Count);
        Assert.All(lists.ContinueWatching, i => Assert.False(i.IsPlaceholder));
    }

    /// <summary>Prüft Weiterschauen: der lokale Fortschritt kommt aus der Outbox (Position/Dauer).</summary>
    [Fact]
    public async Task ContinueWatching_Progress_FromPendingOutbox()
    {
        existingFiles.Add("/downloads/movie-7.mp4");
        await AddManifestEntryAsync(7, "movie", "completed", "/downloads/movie-7.mp4");
        await backup.Connection.InsertAsync(new PendingProgressSyncEntry
        {
            VideoId = 7,
            VideoType = "movie",
            PositionTicks = TimeSpan.FromMinutes(30).Ticks,
            DurationTicks = TimeSpan.FromMinutes(60).Ticks,
            UpdatedAtUtc = DateTime.UtcNow
        });

        var lists = await service.LoadHomeListsAsync();

        var item = Assert.Single(lists.ContinueWatching);
        Assert.Equal(0.5, item.Progress!.Value, 3);
    }

    /// <summary>Prüft Weiterschauen: ohne Outbox-Eintrag fällt der Fortschritt auf den Server-Cache-Stand zurück.</summary>
    [Fact]
    public async Task ContinueWatching_Progress_FallsBackToCachedServerState()
    {
        existingFiles.Add("/downloads/movie-7.mp4");
        await AddManifestEntryAsync(7, "movie", "completed", "/downloads/movie-7.mp4");
        await cache.Connection.InsertOrReplaceAsync(new CachedListEntry
        {
            Key = ServerSyncService.ContinueWatchingCacheKey,
            Json = JsonSerializer.Serialize(new[]
            {
                new ContinueWatchingEntry
                {
                    MediaType = "movie",
                    PositionSeconds = 600,
                    DurationSeconds = 3600,
                    Entry = new Movie { Id = 7, Name = "Film" }
                }
            }),
            UpdatedAtUtc = DateTime.UtcNow
        });

        var lists = await service.LoadHomeListsAsync();

        var item = Assert.Single(lists.ContinueWatching);
        Assert.Equal(600d / 3600d, item.Progress!.Value, 3);
    }

    /// <summary>Prüft Weiterschauen: Favoriten und Neuheiten kommen unverändert aus den bestehenden Caches.</summary>
    [Fact]
    public async Task HomeLists_FavoritesAndRecent_ComeFromCache()
    {
        await cache.Connection.InsertAsync(new CachedFavoritesEntry
        {
            Key = "movie:42",
            VideoId = 42,
            VideoType = "movie",
            Json = JsonSerializer.Serialize(new FavoriteEntry
            {
                Id = 5,
                Entry = new Movie { Id = 42, Name = "Lieblingsfilm", PosterPictureId = 9 }
            }),
            UpdatedAtUtc = DateTime.UtcNow
        });

        var lists = await service.LoadHomeListsAsync();

        Assert.Empty(lists.ContinueWatching);
        var favorite = Assert.Single(lists.Favorites);
        Assert.Equal(42, favorite.Id);
        Assert.Equal("Lieblingsfilm", favorite.Title);
    }
}
