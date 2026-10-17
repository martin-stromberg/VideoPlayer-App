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

    /// <summary>Initialisiert eine neue Instanz der Testklasse <c>HomeListServiceTests</c>.</summary>
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

    /// <inheritdoc/>
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

    /// <summary>Prüft LoadHomeLists: bei leerem Cache → gibt leere Listen zurück.</summary>
    [Fact]
    public async Task LoadHomeLists_EmptyCache_ReturnsEmptyLists()
    {
        var lists = await service.LoadHomeListsAsync();

        Assert.Empty(lists.ContinueWatching);
        Assert.Empty(lists.Favorites);
        Assert.Empty(lists.Recent);
    }

    /// <summary>Prüft LoadHomeLists: bei <c>MapsContinueWatching</c> → bildet ContinueWatching mit Fortschritt und Poster ab.</summary>
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

    /// <summary>Prüft LoadHomeLists: der Playlist-Bezug des Weiterschauen-Eintrags wird auf das HomeMediaItem übertragen.</summary>
    [Fact]
    public async Task LoadHomeLists_MapsContinueWatchingPlaylistContext()
    {
        await WriteCachedListAsync(ServerSyncService.ContinueWatchingCacheKey, new[]
        {
            new ContinueWatchingEntry
            {
                MediaType = "episode",
                PositionSeconds = 120,
                DurationSeconds = 600,
                Title = "Folge 4",
                Entry = new TvShowEpisode { Id = 7, Name = "Folge 4" },
                PlaylistId = 42,
                PlaylistName = "Abendliste",
                PlaylistEntryId = 55
            },
            new ContinueWatchingEntry
            {
                MediaType = "movie",
                PositionSeconds = 0,
                Title = "Film ohne Bezug",
                Entry = new Movie { Id = 3, Name = "Film" }
            }
        });

        var lists = await service.LoadHomeListsAsync();

        var bound = lists.ContinueWatching[0];
        Assert.Equal(42, bound.PlaylistId);
        Assert.Equal("Abendliste", bound.PlaylistName);
        Assert.Equal(55, bound.PlaylistEntryId);

        // Ohne Playlist-Bezug bleiben die Felder leer (Serienpfad).
        var unbound = lists.ContinueWatching[1];
        Assert.Null(unbound.PlaylistId);
        Assert.Null(unbound.PlaylistName);
        Assert.Null(unbound.PlaylistEntryId);
    }

    /// <summary>Prüft LoadHomeLists: bildet Favoriten ab.</summary>
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

    /// <summary>Prüft LoadFavoriteItems: bildet Favoriten mit Eintrags-Id und Serienkontext ab (Schritt 9).</summary>
    [Fact]
    public async Task LoadFavoriteItems_MapsFavoriteIdAndMediaHints()
    {
        await WriteFavoriteAsync(
            new FavoriteEntry { Id = 1, Entry = new TvShow { Id = 11, Name = "Serie" } },
            "tvshow");
        await WriteFavoriteAsync(
            new FavoriteEntry
            {
                Id = 2,
                Entry = new TvShowEpisode
                {
                    Id = 30,
                    Name = "Folge",
                    Season = new TvShowSeason { Id = 20, Show = new TvShow { Id = 11 } }
                }
            },
            "tvshowepisode");
        await WriteFavoriteAsync(
            new FavoriteEntry { Id = 3, Entry = new Movie { Id = 12, Name = "Film", PosterPictureId = 9 } },
            "movie");

        var items = await service.LoadFavoriteItemsAsync();

        Assert.Equal(3, items.Count);
        var episode = Assert.Single(items, i => i.Id == 30);
        // Die Eintrags-Id ist Grundlage der Entfernen-Aktion; der
        // Serienkontext steuert die Detail-Navigation.
        Assert.Equal(2, episode.FavoriteId);
        Assert.Equal("tvshowepisode", episode.MediaType);
        Assert.Equal(11, episode.ShowId);
        Assert.Equal(20, episode.SeasonId);
        Assert.Equal(30, episode.EpisodeId);
        Assert.Equal("Episode", episode.MediaTypeText);

        var movie = Assert.Single(items, i => i.FavoriteId == 3);
        Assert.Equal(9, movie.PosterPictureId);
        Assert.True(movie.HasPoster);
        Assert.True(movie.CanRemove);
        Assert.Equal("Film", movie.MediaTypeText);

        var homeItem = movie.ToMediaItem();
        Assert.Equal(12, homeItem.Id);
        Assert.Equal("movie", homeItem.MediaType);
        Assert.Equal("Film", homeItem.Title);
    }

    /// <summary>Prüft LoadFavoriteItems: bei leerem Cache oder ohne Medium → leere bzw. übersprungene Einträge.</summary>
    [Fact]
    public async Task LoadFavoriteItems_EmptyAndWithoutMedia_ReturnsOnlyIdentifiable()
    {
        Assert.Empty(await service.LoadFavoriteItemsAsync());

        await WriteFavoriteAsync(new FavoriteEntry { Id = 4 }, "movie"); // Entry null
        await WriteFavoriteAsync(
            new FavoriteEntry { Id = 5, Entry = new Movie { Id = 13, Name = "Film" } },
            "movie");

        var item = Assert.Single(await service.LoadFavoriteItemsAsync());
        Assert.Equal(5, item.FavoriteId);
    }

    /// <summary>Prüft LoadHomeLists: bildet die Recent-Liste ab.</summary>
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

    /// <summary>Prüft LoadHomeLists: überspringt Einträge ohne identifizierbare Medien.</summary>
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

    /// <summary>Prüft LoadHomeLists: bei <c>CorruptJson</c> → gibt leere Liste zurück.</summary>
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

    /// <summary>Prüft LoadHomeLists: ein beschädigter <c>cached_lists</c>-Eintrag wird verworfen und gemeldet.</summary>
    [Fact]
    public async Task LoadHomeLists_CorruptListEntry_IsDiscardedAndReported()
    {
        await cache.Connection.InsertAsync(new CachedListEntry
        {
            Key = ServerSyncService.ContinueWatchingCacheKey,
            Json = "{ defekt",
            UpdatedAtUtc = DateTime.UtcNow
        });

        var lists = await service.LoadHomeListsAsync();

        Assert.Empty(lists.ContinueWatching);
        Assert.Contains(ServerSyncService.ContinueWatchingCacheKey, lists.DiscardedCaches);
        // Der beschädigte Eintrag wurde aus der Ablage entfernt — die
        // nächste Synchronisation schreibt ihn frisch.
        Assert.Null(await cache.Connection.Table<CachedListEntry>()
            .Where(e => e.Key == ServerSyncService.ContinueWatchingCacheKey)
            .FirstOrDefaultAsync());
    }

    /// <summary>Prüft LoadHomeLists: eine beschädigte Favoriten-Zeile wird verworfen, intakte bleiben.</summary>
    [Fact]
    public async Task LoadHomeLists_CorruptFavoriteRow_IsDiscardedAndReported()
    {
        await WriteFavoriteAsync(
            new FavoriteEntry { Id = 1, Entry = new Movie { Id = 12, Name = "Film" } },
            "movie");
        await cache.Connection.InsertAsync(new CachedFavoritesEntry
        {
            Key = "movie:99",
            VideoId = 99,
            VideoType = "movie",
            Json = "{ kaputt",
            UpdatedAtUtc = DateTime.UtcNow.AddMinutes(-1)
        });

        var lists = await service.LoadHomeListsAsync();

        var item = Assert.Single(lists.Favorites);
        Assert.Equal(12, item.Id);
        Assert.Contains(HomeListService.FavoritesCacheName, lists.DiscardedCaches);
        Assert.Null(await cache.Connection.FindAsync<CachedFavoritesEntry>("movie:99"));
    }

    /// <summary>Prüft LoadHomeLists: intakter Cache → keine verworfenen Bereiche.</summary>
    [Fact]
    public async Task LoadHomeLists_IntactCache_ReportsNoDiscards()
    {
        await WriteCachedListAsync(ServerSyncService.RecentCacheKey, new[]
        {
            new RecentEntry { Id = 1, Entry = new Movie { Id = 30, Name = "Film" } }
        });

        var lists = await service.LoadHomeListsAsync();

        Assert.Empty(lists.DiscardedCaches);
    }

    /// <summary>Prüft LoadHomeLists: die Persistenz übersteht mehrere Lesevorgänge.</summary>
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
