#pragma warning disable xUnit1051 // Kurzlebige Unit-Tests: TestContext-CancellationToken nicht erforderlich
using System.Text.Json;
using VideoPlayer.ApiClient.Models;
using VideoPlayer.Mvu;
using VideoPlayer.Storage;
using VideoPlayer.Storage.Entities;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>
/// <see cref="HomeListService"/> (Schritt 7): liest die drei
/// Startseiten-Listen aus den vom <see cref="ServerSyncService"/>
/// befüllten Cache-Tabellen und mappt sie auf anzeigefertige
/// <see cref="HomeMediaItem"/>s — Grundlage des Cache-First-Verhaltens
/// beim erneuten Öffnen der App.
/// </summary>
[Collection("SqliteDatabase")]
public class HomeListServiceTests : IDisposable
{
    private readonly string root;
    private readonly CacheDatabase cache;
    private readonly HomeListService service;

    public HomeListServiceTests()
    {
        root = Path.Combine(Path.GetTempPath(), "videoplayer-homelists-tests", Guid.NewGuid().ToString("N"));
        var layout = new AppStorageLayout(
            backupRoot: Path.Combine(root, "backup"),
            noBackupRoot: Path.Combine(root, "nobackup"),
            cacheRoot: Path.Combine(root, "cache"),
            profile: AppProfile.Create(null));
        layout.EnsureDirectoriesExist();
        cache = new CacheDatabase(layout);
        cache.InitializeAsync().Wait();
        service = new HomeListService(cache);
    }

    public void Dispose()
    {
        SQLite.SQLiteAsyncConnection.ResetPool();
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private async Task WriteCachedListAsync(string key, object payload)
    {
        // Schreibt den Cache exakt so wie ServerSyncService.WriteCachedListAsync.
        await cache.Connection.InsertOrReplaceAsync(new CachedListEntry
        {
            Key = key,
            Json = JsonSerializer.Serialize(payload),
            UpdatedAtUtc = DateTime.UtcNow
        });
    }

    private async Task WriteFavoriteAsync(FavoriteEntry favorite, string mediaType)
    {
        await cache.Connection.InsertAsync(new CachedFavoritesEntry
        {
            Key = $"{mediaType}:{favorite.Entry?.Id ?? favorite.Id}",
            VideoId = (int)(favorite.Entry?.Id ?? favorite.Id),
            VideoType = mediaType,
            Json = JsonSerializer.Serialize(favorite),
            UpdatedAtUtc = DateTime.UtcNow
        });
    }

    [Fact]
    public async Task LoadHomeLists_EmptyCache_ReturnsEmptyLists()
    {
        var lists = await service.LoadHomeListsAsync();

        Assert.Empty(lists.ContinueWatching);
        Assert.Empty(lists.Favorites);
        Assert.Empty(lists.Recent);
    }

    [Fact]
    public async Task LoadHomeLists_MapsContinueWatching_WithProgressAndPoster()
    {
        await WriteCachedListAsync(ServerSyncService.ContinueWatchingCacheKey, new[]
        {
            new ContinueWatchingEntry
            {
                MediaType = "episode",
                PositionSeconds = 120,
                DurationSeconds = 600,
                Title = "Folge 4",
                PosterPictureId = 77,
                Entry = new TvShowEpisode { Id = 7, Name = "Folge 4" }
            },
            new ContinueWatchingEntry
            {
                MediaType = "movie",
                PositionSeconds = 0,
                DurationSeconds = null, // ohne Dauer → kein Fortschrittsbalken
                Title = "Film",
                PosterPictureId = null,
                Entry = new Movie { Id = 3, Name = "Film", PosterPictureId = 55 }
            }
        });

        var lists = await service.LoadHomeListsAsync();

        Assert.Equal(2, lists.ContinueWatching.Count);

        var episode = lists.ContinueWatching[0];
        Assert.Equal(7, episode.Id);
        Assert.Equal("episode", episode.MediaType);
        Assert.Equal("Folge 4", episode.Title);
        Assert.Equal(77, episode.PosterPictureId);
        Assert.Equal(0.2, episode.Progress.GetValueOrDefault(), precision: 6);
        Assert.True(episode.ShowProgress);

        var movie = lists.ContinueWatching[1];
        Assert.Equal(3, movie.Id);
        // Ohne Listen-Poster greift das Poster des Mediums.
        Assert.Equal(55, movie.PosterPictureId);
        Assert.Null(movie.Progress);
        Assert.False(movie.ShowProgress);
    }

    [Fact]
    public async Task LoadHomeLists_MapsFavorites()
    {
        await WriteFavoriteAsync(
            new FavoriteEntry { Id = 1, Entry = new TvShow { Id = 11, Name = "Serie" } },
            "tvshow");
        await WriteFavoriteAsync(
            new FavoriteEntry { Id = 2, Entry = new Movie { Id = 12, Name = "Film", PosterPictureId = 9 } },
            "movie");

        var lists = await service.LoadHomeListsAsync();

        Assert.Equal(2, lists.Favorites.Count);
        Assert.Contains(lists.Favorites, f => f.Id == 11 && f.MediaType == "tvshow" && f.Title == "Serie");
        var movie = Assert.Single(lists.Favorites, f => f.Id == 12);
        Assert.Equal("movie", movie.MediaType);
        Assert.Equal(9, movie.PosterPictureId);
        Assert.False(movie.ShowProgress);
    }

    [Fact]
    public async Task LoadHomeLists_MapsRecent()
    {
        await WriteCachedListAsync(ServerSyncService.RecentCacheKey, new[]
        {
            new RecentEntry
            {
                Id = 1,
                Entry = new Movie { Id = 21, Name = "Neuer Film", PosterPictureId = 4 }
            },
            new RecentEntry
            {
                Id = 2,
                Entry = new TvShowSeason { Id = 22, Name = "Staffel 2" }
            }
        });

        var lists = await service.LoadHomeListsAsync();

        Assert.Equal(2, lists.Recent.Count);
        Assert.Equal(21, lists.Recent[0].Id);
        Assert.Equal("movie", lists.Recent[0].MediaType);
        Assert.Equal(4, lists.Recent[0].PosterPictureId);
        Assert.Equal(22, lists.Recent[1].Id);
        Assert.Equal("tvshowseason", lists.Recent[1].MediaType);
    }

    [Fact]
    public async Task LoadHomeLists_SkipsEntriesWithoutIdentifiableMedia()
    {
        await WriteCachedListAsync(ServerSyncService.ContinueWatchingCacheKey, new[]
        {
            new ContinueWatchingEntry { MediaType = "movie", Title = "Ohne Medium" }, // Entry null
            new ContinueWatchingEntry
            {
                MediaType = "movie",
                Title = "Gültig",
                Entry = new Movie { Id = 8, Name = "Gültig" }
            }
        });

        var lists = await service.LoadHomeListsAsync();

        // Einträge ohne identifizierbares Medium werden übersprungen —
        // die Auswahl muss das Medium eindeutig bestimmen können.
        var item = Assert.Single(lists.ContinueWatching);
        Assert.Equal(8, item.Id);
    }

    [Fact]
    public async Task LoadHomeLists_CorruptJson_ReturnsEmptyList()
    {
        await cache.Connection.InsertAsync(new CachedListEntry
        {
            Key = ServerSyncService.RecentCacheKey,
            Json = "{ kein-json",
            UpdatedAtUtc = DateTime.UtcNow
        });

        var lists = await service.LoadHomeListsAsync();

        Assert.Empty(lists.Recent);
    }

    [Fact]
    public async Task LoadHomeLists_PersistsAcrossReads()
    {
        // Cache-First-Nachweis „erneutes Öffnen": der einmal geschriebene
        // Stand ist ohne weiteren Serverkontakt wieder lesbar.
        await WriteCachedListAsync(ServerSyncService.RecentCacheKey, new[]
        {
            new RecentEntry { Id = 1, Entry = new Movie { Id = 30, Name = "Film" } }
        });

        var firstRead = await service.LoadHomeListsAsync();
        var secondRead = await new HomeListService(cache).LoadHomeListsAsync();

        Assert.Single(firstRead.Recent);
        Assert.Equal(30, secondRead.Recent[0].Id);
    }
}
