using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using VideoPlayer.ApiClient.Models;
using VideoPlayer.Storage;
using VideoPlayer.Storage.Entities;

namespace VideoPlayer.Mvu;

/// <summary>
/// Liest die Startseiten-Listen aus der lokalen Cache-Ablage
/// (<see cref="CacheDatabase"/>): „Weiterschauen" und „Neu im Programm" aus
/// den von <see cref="ServerSyncService"/> geschriebenen
/// <c>cached_lists</c>-Schlüsseln sowie die Favoriten aus
/// <c>cached_favorites</c>. Grundlage des Cache-First-Verhaltens der
/// Startseite — der gelesene Stand wird ins <see cref="AppModel"/>
/// übernommen, ohne dass eine Serverantwort abgewartet werden muss.
/// </summary>
public sealed class HomeListService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly CacheDatabase cache;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<HomeListService> logger;

    /// <summary>Erstellt den Dienst.</summary>
    /// <param name="cache">Lokale Cache-Ablage mit den synchronisierten Listen.</param>
    /// <param name="logger">Optionaler Logger für das Programmprotokoll.</param>
    /// <param name="timeProvider">Uhr für die Auswertung serverseitiger Ablaufgrenzen.</param>
    public HomeListService(CacheDatabase cache, ILogger<HomeListService>? logger = null, TimeProvider? timeProvider = null)
    {
        this.cache = cache ?? throw new ArgumentNullException(nameof(cache));
        this.timeProvider = timeProvider ?? TimeProvider.System;
        this.logger = logger ?? NullLogger<HomeListService>.Instance;
    }

    /// <summary>
    /// Liest alle drei Startseiten-Listen aus dem Cache. Fehlende oder
    /// beschädigte Einträge ergeben leere Teillisten — die Startseite bleibt
    /// damit in jedem Fall anzeigbar. Beschädigte Cache-Zeilen werden
    /// verworfen (gelöscht) und über <see cref="HomeLists.DiscardedCaches"/>
    /// gemeldet, damit sie bei bestehender Verbindung neu synchronisiert
    /// werden.
    /// </summary>
    /// <param name="cancellationToken">Bricht das Lesen ab.</param>
    /// <returns>Die drei Startseiten-Listen aus dem Cache.</returns>
    public async Task<HomeLists> LoadHomeListsAsync(CancellationToken cancellationToken = default)
    {
        var continueWatching = await ReadListAsync<ContinueWatchingEntry>(ServerSyncService.ContinueWatchingCacheKey);
        var favorites = await ReadFavoritesAsync();
        var recent = await ReadListAsync<RecentEntry>(ServerSyncService.RecentCacheKey);

        var discarded = new List<string>(3);
        if (continueWatching.Discarded)
        {
            discarded.Add(ServerSyncService.ContinueWatchingCacheKey);
        }
        if (favorites.Discarded)
        {
            discarded.Add(FavoritesCacheName);
        }
        if (recent.Discarded)
        {
            discarded.Add(ServerSyncService.RecentCacheKey);
        }

        return new HomeLists
        {
            ContinueWatching = MapAll(continueWatching.Items, MapContinueWatching),
            Favorites = MapAll(favorites.Items, f => MapEntry(f.Entry)),
            Recent = MapAll(recent.Items, r => MapEntry(r.Entry)),
            SeasonalGenres = await LoadSeasonalGenresAsync(cancellationToken),
            DiscardedCaches = discarded
        };
    }

    /// <summary>
    /// Liest die Favoriten-Kopie <c>cached_favorites</c> als anzeigefertige
    /// Einträge der Startseiten-Liste — Grundlage auch der
    /// Offline-Startseite (<see cref="OfflineContinueWatchingService"/>).
    /// </summary>
    /// <param name="cancellationToken">Bricht das Lesen ab.</param>
    /// <returns>Die Favoriten-Einträge aus der lokalen Kopie.</returns>
    public async Task<IReadOnlyList<HomeMediaItem>> LoadFavoritesAsync(CancellationToken cancellationToken = default)
    {
        var favorites = await ReadFavoritesAsync();
        return MapAll(favorites.Items, f => MapEntry(f.Entry));
    }

    /// <summary>
    /// Liest die „Neu im Programm"-Kopie (<c>cached_lists</c>-Schlüssel
    /// <c>recent</c>) als anzeigefertige Einträge — Grundlage auch der
    /// Offline-Startseite (<see cref="OfflineContinueWatchingService"/>).
    /// </summary>
    /// <param name="cancellationToken">Bricht das Lesen ab.</param>
    /// <returns>Die Neuheiten-Einträge aus der lokalen Kopie.</returns>
    public async Task<IReadOnlyList<HomeMediaItem>> LoadRecentAsync(CancellationToken cancellationToken = default)
    {
        var recent = await ReadListAsync<RecentEntry>(ServerSyncService.RecentCacheKey);
        return MapAll(recent.Items, r => MapEntry(r.Entry));
    }

    /// <summary>
    /// Liest die Favoriten-Kopie <c>cached_favorites</c> und mappt sie auf
    /// anzeigefertige <see cref="FavoriteListItem"/>s der Favoriten-Seite —
    /// derselbe Lesepfad wie die Startseiten-Favoriten, jedoch mit der
    /// Favoriten-Eintrags-Id (Grundlage der Entfernen-Aktion über
    /// <c>api/favorites/remove</c>) und dem Serienkontext der
    /// Detail-Navigation.
    /// </summary>
    /// <param name="cancellationToken">Bricht das Lesen ab.</param>
    /// <returns>Die Favoriten-Einträge aus der lokalen Kopie.</returns>
    public async Task<IReadOnlyList<FavoriteListItem>> LoadFavoriteItemsAsync(CancellationToken cancellationToken = default)
    {
        var favorites = await ReadFavoritesAsync();
        var items = new List<FavoriteListItem>(favorites.Items.Count);
        foreach (var favorite in favorites.Items)
        {
            // Einträge ohne identifizierbares Medium werden übersprungen —
            // die Auswahl muss das Medium eindeutig bestimmen können.
            var item = MapFavorite(favorite);
            if (item is not null)
            {
                items.Add(item);
            }
        }

        return items;
    }

    /// <summary>Liest gültige saisonale Sektionen ohne eigene Saisonberechnung.</summary>
    /// <param name="cancellationToken">Abbruchtoken.</param>
    /// <returns>Nicht leere Sektionen vor ihrer exklusiven Server-Ablaufgrenze.</returns>
    public async Task<IReadOnlyList<HomeSeasonalSection>> LoadSeasonalGenresAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var cached = await ReadListAsync<SeasonalGenre>(ServerSyncService.SeasonalGenresCacheKey);
        var now = timeProvider.GetUtcNow();
        var sections = new List<HomeSeasonalSection>();
        foreach (var genre in cached.Items)
        {
            if (genre is null || genre.GenreId <= 0 || string.IsNullOrWhiteSpace(genre.Name) || genre.ExpiresAt <= now)
                continue;
            var items = MapAll(genre.Entries ?? [], media => media is Movie or MovieCollection or TvShow ? MapEntry(media) : null);
            if (items.Count > 0)
                sections.Add(new HomeSeasonalSection { GenreId = genre.GenreId, Name = genre.Name, ExpiresAt = genre.ExpiresAt, Items = items });
        }
        return sections;
    }

    /// <summary>Cache-Bereichsname der Favoriten-Kopie (für <see cref="HomeLists.DiscardedCaches"/>).</summary>
    internal const string FavoritesCacheName = "cached_favorites";

    private async Task<(IReadOnlyList<T> Items, bool Discarded)> ReadListAsync<T>(string key)
    {
        try
        {
            var entry = await cache.Connection.Table<CachedListEntry>()
                .Where(e => e.Key == key)
                .FirstOrDefaultAsync();
            if (entry?.Json is not { Length: > 0 } json)
            {
                return ([], false);
            }

            return (JsonSerializer.Deserialize<List<T>>(json, JsonOptions) ?? [], false);
        }
        catch (Exception ex)
        {
            // Beschädigter oder nicht lesbarer Cache → Eintrag verwerfen
            // und leere Liste zurückgeben; die Neu-Synchronisation baut
            // den Cache bei bestehender Verbindung wieder auf.
            logger.LogWarning(ex, "Lesen der Liste '{CacheKey}' fehlgeschlagen.", key);
            var discarded = await DiscardListEntryAsync(key);
            return ([], discarded);
        }
    }

    private async Task<(IReadOnlyList<FavoriteEntry> Items, bool Discarded)> ReadFavoritesAsync()
    {
        try
        {
            var rows = await cache.Connection.Table<CachedFavoritesEntry>().ToListAsync();
            var favorites = new List<FavoriteEntry>(rows.Count);
            var discarded = false;
            foreach (var row in rows.OrderByDescending(r => r.UpdatedAtUtc))
            {
                try
                {
                    var favorite = JsonSerializer.Deserialize<FavoriteEntry>(row.Json, JsonOptions);
                    if (favorite is not null)
                    {
                        favorites.Add(favorite);
                    }
                }
                catch (JsonException ex)
                {
                    logger.LogWarning(ex, "Favorit '{CacheKey}' nicht lesbar — wird verworfen.", row.Key);
                    discarded |= await DiscardRowAsync(row);
                }
            }

            return (favorites, discarded);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Lesen der Favoriten fehlgeschlagen.");
            return ([], false);
        }
    }

    /// <summary>
    /// Verwirft einen beschädigten <c>cached_lists</c>-Eintrag.
    /// </summary>
    /// <param name="key">Schlüssel des beschädigten Eintrags.</param>
    /// <returns><c>true</c>, wenn ein Eintrag gelöscht wurde; sonst <c>false</c>.</returns>
    private async Task<bool> DiscardListEntryAsync(string key)
    {
        try
        {
            var deleted = await cache.Connection.Table<CachedListEntry>()
                .DeleteAsync(e => e.Key == key);
            return deleted > 0;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Verwerfen der Liste '{CacheKey}' fehlgeschlagen.", key);
            return false;
        }
    }

    /// <summary>
    /// Verwirft eine beschädigte <c>cached_favorites</c>-Zeile.
    /// </summary>
    /// <param name="row">Die beschädigte Zeile.</param>
    /// <returns><c>true</c>, wenn die Zeile gelöscht wurde; sonst <c>false</c>.</returns>
    private async Task<bool> DiscardRowAsync(CachedFavoritesEntry row)
    {
        try
        {
            return await cache.Connection.DeleteAsync(row) > 0;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Verwerfen des Favoriten '{CacheKey}' fehlgeschlagen.", row.Key);
            return false;
        }
    }

    private static IReadOnlyList<HomeMediaItem> MapAll<T>(
        IReadOnlyList<T> source,
        Func<T, HomeMediaItem?> map)
    {
        var items = new List<HomeMediaItem>(source.Count);
        foreach (var entry in source)
        {
            // Einträge ohne identifizierbares Medium werden übersprungen —
            // die Auswahl muss das Medium eindeutig bestimmen können.
            var item = map(entry);
            if (item is not null)
            {
                items.Add(item);
            }
        }

        return items;
    }

    private static HomeMediaItem? MapContinueWatching(ContinueWatchingEntry entry)
    {
        var media = entry.Entry;
        if (media is null || media.Id <= 0)
        {
            return null;
        }

        var (showId, seasonId, episodeId) = MediaHints(media);
        return new HomeMediaItem
        {
            Id = media.Id,
            MediaType = MediaTypeOf(entry.MediaType, media),
            Title = string.IsNullOrWhiteSpace(entry.Title) ? media.Name : entry.Title,
            PosterPictureId = entry.PosterPictureId ?? media.PosterPictureId,
            Progress = entry.DurationSeconds is > 0
                ? Math.Clamp((double)entry.PositionSeconds / entry.DurationSeconds.Value, 0d, 1d)
                : null,
            ShowId = showId,
            SeasonId = seasonId,
            EpisodeId = episodeId,
            PlaylistId = entry.PlaylistId,
            PlaylistName = entry.PlaylistName,
            PlaylistEntryId = entry.PlaylistEntryId
        };
    }

    private static HomeMediaItem? MapEntry(MediaEntry? media)
    {
        if (media is null || media.Id <= 0)
        {
            return null;
        }

        var (showId, seasonId, episodeId) = MediaHints(media);
        return new HomeMediaItem
        {
            Id = media.Id,
            MediaType = MediaTypeOf(null, media),
            Title = media.Name,
            PosterPictureId = media.PosterPictureId,
            ShowId = showId,
            SeasonId = seasonId,
            EpisodeId = episodeId
        };
    }

    private static FavoriteListItem? MapFavorite(FavoriteEntry favorite)
    {
        var media = favorite.Entry;
        if (media is null || media.Id <= 0)
        {
            return null;
        }

        var (showId, seasonId, episodeId) = MediaHints(media);
        return new FavoriteListItem
        {
            FavoriteId = favorite.Id,
            Id = media.Id,
            MediaType = MediaTypeOf(null, media),
            Title = media.Name,
            PosterPictureId = media.PosterPictureId,
            ShowId = showId,
            SeasonId = seasonId,
            EpisodeId = episodeId
        };
    }

    /// <summary>
    /// Serienkontext-Hints aus dem eingebetteten Objektgraph — Grundlage
    /// der Detail-Navigation (Episoden/Staffeln öffnen die Serienansicht
    /// mit Vorwahl, wie im Altbestand).
    /// </summary>
    /// <param name="media">Das Medium, aus dem der Serienkontext gelesen wird.</param>
    /// <returns>Tupel aus Serien-, Staffel- und Episoden-Id (in dieser Reihenfolge); Elemente <c>null</c>, wenn nicht auflösbar.</returns>
    private static (long?, long?, long?) MediaHints(MediaEntry media)
    {
        return media switch
        {
            TvShowEpisode episode => (episode.Season?.Show?.Id, episode.Season?.Id, episode.Id),
            TvShowSeason season => (season.Show?.Id, season.Id, null),
            _ => (null, null, null)
        };
    }

    private static string MediaTypeOf(string? serverType, MediaEntry media)
    {
        // Der serverseitige Medientyp (Weiterschauen-Liste) hat Vorrang;
        // sonst der CLR-Typname kleingeschrieben — Konvention wie die
        // Schlüssel der Favoriten-Kopie in ServerSyncService.MediaTypeOf.
        return !string.IsNullOrWhiteSpace(serverType)
            ? serverType.ToLowerInvariant()
            : media.GetType().Name.ToLowerInvariant();
    }
}
