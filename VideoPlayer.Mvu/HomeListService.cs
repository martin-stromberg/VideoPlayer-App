using System.Text.Json;
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

    /// <summary>Erstellt den Dienst.</summary>
    /// <param name="cache">Lokale Cache-Ablage mit den synchronisierten Listen.</param>
    public HomeListService(CacheDatabase cache)
    {
        this.cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    /// <summary>
    /// Liest alle drei Startseiten-Listen aus dem Cache. Fehlende oder
    /// beschädigte Einträge ergeben leere Teillisten — die Startseite bleibt
    /// damit in jedem Fall anzeigbar.
    /// </summary>
    /// <param name="cancellationToken">Bricht das Lesen ab.</param>
    /// <returns>Die drei Startseiten-Listen aus dem Cache.</returns>
    public async Task<HomeLists> LoadHomeListsAsync(CancellationToken cancellationToken = default)
    {
        var continueWatching = await ReadListAsync<ContinueWatchingEntry>(ServerSyncService.ContinueWatchingCacheKey);
        var favorites = await ReadFavoritesAsync();
        var recent = await ReadListAsync<RecentEntry>(ServerSyncService.RecentCacheKey);

        return new HomeLists
        {
            ContinueWatching = MapAll(continueWatching, MapContinueWatching),
            Favorites = MapAll(favorites, f => MapEntry(f.Entry)),
            Recent = MapAll(recent, r => MapEntry(r.Entry))
        };
    }

    private async Task<IReadOnlyList<T>> ReadListAsync<T>(string key)
    {
        try
        {
            var entry = await cache.Connection.Table<CachedListEntry>()
                .Where(e => e.Key == key)
                .FirstOrDefaultAsync();
            if (entry?.Json is not { Length: > 0 } json)
            {
                return [];
            }

            return JsonSerializer.Deserialize<List<T>>(json, JsonOptions) ?? [];
        }
        catch (Exception ex)
        {
            // Beschädigter oder nicht lesbarer Cache → leere Liste statt Fehler.
            System.Diagnostics.Debug.WriteLine($"[HomeListService] Lesen der Liste '{key}' fehlgeschlagen: {ex.Message}");
            return [];
        }
    }

    private async Task<IReadOnlyList<FavoriteEntry>> ReadFavoritesAsync()
    {
        try
        {
            var rows = await cache.Connection.Table<CachedFavoritesEntry>().ToListAsync();
            var favorites = new List<FavoriteEntry>(rows.Count);
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
                    System.Diagnostics.Debug.WriteLine($"[HomeListService] Favorit '{row.Key}' nicht lesbar: {ex.Message}");
                }
            }

            return favorites;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[HomeListService] Lesen der Favoriten fehlgeschlagen: {ex.Message}");
            return [];
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
            EpisodeId = episodeId
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

    /// <summary>
    /// Serienkontext-Hints aus dem eingebetteten Objektgraph — Grundlage
    /// der Detail-Navigation (Episoden/Staffeln öffnen die Serienansicht
    /// mit Vorwahl, wie im Altbestand).
    /// </summary>
    /// <param name="media">Das Medium, aus dem der Serienkontext gelesen wird.</param>
    /// <param name="ShowId">Tupel-Element des Ergebnisses: Id der Serie, falls auflösbar.</param>
    /// <param name="SeasonId">Tupel-Element des Ergebnisses: Id der Staffel, falls auflösbar.</param>
    private static (long? ShowId, long? SeasonId, long? EpisodeId) MediaHints(MediaEntry media)
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
