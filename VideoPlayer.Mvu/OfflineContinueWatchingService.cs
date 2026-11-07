using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using VideoPlayer.ApiClient.Models;
using VideoPlayer.Storage;
using VideoPlayer.Storage.Entities;

namespace VideoPlayer.Mvu;

/// <summary>
/// Offline-Startseiten-Listen: bildet „Weiterschauen" ohne Server
/// ausschließlich aus dem Download-Manifest — nur abgeschlossene
/// Einträge (<see cref="DownloadState.Completed"/>) mit vorhandener
/// Datei. Zu jeder abspielbaren Episode wird anhand der gecachten
/// Serienstruktur (<see cref="ShowDetailsCache"/>) die Folge-Episode
/// bestimmt; ist diese nicht heruntergeladen, erscheint sie als
/// Platzhalter (<see cref="HomeMediaItem.IsPlaceholder"/>). Der
/// Fortschritt stammt aus der lokalen Outbox
/// (<c>pending_progress</c>, Fallback: letzter Server-Stand der
/// <c>continue_watching</c>-Kopie). Favoriten und „Neu im Programm"
/// kommen weiterhin aus den bestehenden Caches des
/// <see cref="HomeListService"/>.
/// </summary>
public sealed class OfflineContinueWatchingService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly CacheDatabase cache;
    private readonly BackupDatabase backup;
    private readonly HomeListService homeLists;
    private readonly AppStorageLayout layout;
    private readonly Func<string, bool> fileExists;
    private readonly ILogger<OfflineContinueWatchingService> logger;

    /// <summary>Erstellt den Dienst.</summary>
    /// <param name="cache">Cache-Ablage mit Download-Manifest und gecachten Listen.</param>
    /// <param name="backup">Backup-Ablage mit der Fortschritts-Outbox (<c>pending_progress</c>).</param>
    /// <param name="homeLists">Lesedienst der gecachten Favoriten- und Neuheiten-Listen.</param>
    /// <param name="layout">Ablagelayout mit dem Video-Verzeichnis.</param>
    /// <param name="logger">Optionaler Logger für das Programmprotokoll.</param>
    public OfflineContinueWatchingService(
        CacheDatabase cache,
        BackupDatabase backup,
        HomeListService homeLists,
        AppStorageLayout layout,
        ILogger<OfflineContinueWatchingService>? logger = null)
        : this(cache, backup, homeLists, layout, null, logger)
    {
    }

    /// <summary>Erstellt den Dienst mit injizierbarer Dateiprüfung (für Tests).</summary>
    /// <param name="cache">Cache-Ablage mit Download-Manifest und gecachten Listen.</param>
    /// <param name="backup">Backup-Ablage mit der Fortschritts-Outbox.</param>
    /// <param name="homeLists">Lesedienst der gecachten Favoriten- und Neuheiten-Listen.</param>
    /// <param name="layout">Ablagelayout mit dem Video-Verzeichnis.</param>
    /// <param name="fileExists">Dateiprüfung; <c>null</c> = <see cref="File.Exists(string?)"/>.</param>
    /// <param name="logger">Optionaler Logger für das Programmprotokoll.</param>
    internal OfflineContinueWatchingService(
        CacheDatabase cache,
        BackupDatabase backup,
        HomeListService homeLists,
        AppStorageLayout layout,
        Func<string, bool>? fileExists,
        ILogger<OfflineContinueWatchingService>? logger = null)
    {
        this.cache = cache ?? throw new ArgumentNullException(nameof(cache));
        this.backup = backup ?? throw new ArgumentNullException(nameof(backup));
        this.homeLists = homeLists ?? throw new ArgumentNullException(nameof(homeLists));
        this.layout = layout ?? throw new ArgumentNullException(nameof(layout));
        this.fileExists = fileExists ?? File.Exists;
        this.logger = logger ?? NullLogger<OfflineContinueWatchingService>.Instance;
    }

    /// <summary>
    /// Lädt die Startseiten-Listen für den Offline-Modus: „Weiterschauen"
    /// aus den lokalen Downloads samt Platzhaltern fehlender Folge-Episoden,
    /// Favoriten und Neuheiten aus dem Cache. Fehler einzelner Quellen
    /// ergeben leere Teillisten — die Startseite bleibt anzeigbar.
    /// </summary>
    /// <param name="cancellationToken">Bricht das Laden ab.</param>
    /// <returns>Die Offline-Startseiten-Listen.</returns>
    public async Task<HomeLists> LoadHomeListsAsync(CancellationToken cancellationToken = default)
    {
        var continueWatching = await LoadContinueWatchingAsync(cancellationToken);
        var favorites = await homeLists.LoadFavoritesAsync(cancellationToken);
        var recent = await homeLists.LoadRecentAsync(cancellationToken);
        return new HomeLists
        {
            ContinueWatching = continueWatching,
            Favorites = favorites,
            Recent = recent,
            SeasonalGenres = await homeLists.LoadSeasonalGenresAsync(cancellationToken)
        };
    }

    /// <summary>
    /// Bildet die Offline-„Weiterschauen"-Liste: abspielbare Downloads
    /// (completed + vorhandene Datei) in Manifest-Reihenfolge
    /// (<c>UpdatedAtUtc</c> absteigend), jeweils gefolgt von einem
    /// Platzhalter der fehlenden Folge-Episode (falls Serienstruktur
    /// gecacht und Folge-Episode nicht heruntergeladen).
    /// </summary>
    /// <param name="cancellationToken">Bricht das Laden ab.</param>
    /// <returns>Die Weiterschauen-Einträge inklusive Platzhalter.</returns>
    internal async Task<IReadOnlyList<HomeMediaItem>> LoadContinueWatchingAsync(CancellationToken cancellationToken = default)
    {
        List<DownloadManifestEntry> rows;
        try
        {
            rows = await cache.Connection.Table<DownloadManifestEntry>().ToListAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Download-Manifest nicht lesbar.");
            return [];
        }

        var completed = rows
            .Where(r => DownloadStateNames.FromPersisted(r.State) == DownloadState.Completed
                && r.FilePath is { Length: > 0 }
                && fileExists(DownloadManifestPaths.Resolve(layout, r)))
            .OrderByDescending(r => r.UpdatedAtUtc)
            .ToList();

        var downloadedEpisodeIds = completed
            .Where(r => MediaTypeNames.Normalize(r.VideoType) == "tvshowepisode")
            .Select(r => (long)r.VideoId)
            .ToHashSet();

        var progress = await LoadPendingProgressAsync(cancellationToken);
        var cachedProgress = await LoadCachedContinueWatchingAsync(cancellationToken);

        var items = new List<HomeMediaItem>(completed.Count);
        var placeholderIds = new HashSet<long>();
        foreach (var row in completed)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var type = MediaTypeNames.Normalize(row.VideoType);
            items.Add(new HomeMediaItem
            {
                Id = row.VideoId,
                MediaType = type,
                Title = string.IsNullOrWhiteSpace(row.Title) ? "Download" : row.Title,
                Progress = ResolveProgress(type, row.VideoId, progress, cachedProgress),
                ShowId = row.ShowId,
                EpisodeId = type == "tvshowepisode" ? row.VideoId : null
            });

            if (type != "tvshowepisode" || row.ShowId is not > 0)
            {
                continue;
            }

            var placeholder = await FindMissingNextEpisodeAsync(row, downloadedEpisodeIds, cancellationToken);
            if (placeholder is { } next && placeholderIds.Add(next.Episode.Id))
            {
                var showTitle = next.ShowTitle;
                var episodeTitle = string.IsNullOrWhiteSpace(next.Episode.Title)
                    ? $"Episode {next.Episode.Number}"
                    : next.Episode.Title;
                items.Add(new HomeMediaItem
                {
                    Id = next.Episode.Id,
                    MediaType = "tvshowepisode",
                    Title = string.IsNullOrWhiteSpace(showTitle) ? episodeTitle : $"{showTitle} – {episodeTitle}",
                    ShowId = next.ShowId,
                    SeasonId = next.SeasonId,
                    EpisodeId = next.Episode.Id,
                    IsPlaceholder = true
                });
            }
        }

        return items;
    }

    /// <summary>
    /// Bestimmt die Folge-Episode eines Episoden-Downloads aus der
    /// gecachten Serienstruktur, sofern diese nicht ebenfalls
    /// heruntergeladen ist.
    /// </summary>
    /// <param name="row">Manifesteintrag der Ausgangsepisode.</param>
    /// <param name="downloadedEpisodeIds">Ids aller abgeschlossenen Episoden-Downloads.</param>
    /// <param name="cancellationToken">Bricht das Lesen ab.</param>
    /// <returns>Die fehlende Folge-Episode mit Serienkontext; <c>null</c>, wenn nicht bestimmbar oder bereits vorhanden.</returns>
    private async Task<(long ShowId, string? ShowTitle, long? SeasonId, CachedShowEpisode Episode)?> FindMissingNextEpisodeAsync(
        DownloadManifestEntry row,
        HashSet<long> downloadedEpisodeIds,
        CancellationToken cancellationToken)
    {
        var show = await ShowDetailsCache.ReadAsync(cache, row.ShowId!.Value, logger, cancellationToken);
        if (show is null)
        {
            return null;
        }

        var next = row.SeasonNumber is { } seasonNumber && row.EpisodeNumber is { } episodeNumber
            ? ShowDetailsCache.NextEpisodeAfter(show, seasonNumber, episodeNumber)
            : ShowDetailsCache.NextEpisodeAfterId(show, row.VideoId);
        if (next is not { } candidate || downloadedEpisodeIds.Contains(candidate.Episode.Id))
        {
            return null;
        }

        var seasonId = show.Seasons.FirstOrDefault(s => s.Number == candidate.SeasonNumber)?.Id;
        var showTitle = string.IsNullOrWhiteSpace(row.ShowTitle) ? show.Title : row.ShowTitle;
        return (show.ShowId, showTitle, seasonId, candidate.Episode);
    }

    /// <summary>
    /// Liest die Fortschritts-Outbox (<c>pending_progress</c>) — der
    /// jeweils neueste Eintrag je Medium ist der lokal aktuellste Stand.
    /// </summary>
    /// <param name="cancellationToken">Bricht das Lesen ab.</param>
    /// <returns>Abbildung (Medientyp, Medien-Id) auf (Position, Dauer) in Ticks.</returns>
    private async Task<Dictionary<(string Type, int Id), (long PositionTicks, long DurationTicks)>> LoadPendingProgressAsync(
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<(string, int), (long, long)>();
        try
        {
            var rows = await backup.Connection.Table<PendingProgressSyncEntry>().ToListAsync();
            foreach (var row in rows.OrderBy(r => r.UpdatedAtUtc))
            {
                var type = MediaTypeNames.Normalize(row.VideoType);
                result[(type, row.VideoId)] = (row.PositionTicks, row.DurationTicks);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Fortschritts-Outbox nicht lesbar.");
        }

        return result;
    }

    /// <summary>
    /// Liest den letzten Server-Stand der Weiterschauen-Positionen aus
    /// <c>cached_lists</c> als Fallback für Medien ohne lokale
    /// Fortschrittsmeldung.
    /// </summary>
    /// <param name="cancellationToken">Bricht das Lesen ab.</param>
    /// <returns>Abbildung (Medientyp, Medien-Id) auf (Position, Dauer) in Sekunden.</returns>
    private async Task<Dictionary<(string Type, long Id), (long PositionSeconds, long DurationSeconds)>> LoadCachedContinueWatchingAsync(
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<(string, long), (long, long)>();
        try
        {
            var row = await cache.Connection.Table<CachedListEntry>()
                .Where(e => e.Key == ServerSyncService.ContinueWatchingCacheKey)
                .FirstOrDefaultAsync();
            if (row?.Json is not { Length: > 0 } json)
            {
                return result;
            }

            var entries = JsonSerializer.Deserialize<List<ContinueWatchingEntry>>(json, JsonOptions) ?? [];
            foreach (var entry in entries)
            {
                var media = entry.Entry;
                if (media is null || media.Id <= 0 || entry.DurationSeconds is not > 0)
                {
                    continue;
                }

                var type = MediaTypeNames.Normalize(entry.MediaType);
                result[(type, media.Id)] = (entry.PositionSeconds, entry.DurationSeconds.Value);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Weiterschauen-Cache nicht lesbar.");
        }

        return result;
    }

    /// <summary>
    /// Bestimmt den Anzeige-Fortschritt (0–1) eines Eintrags: lokale
    /// Outbox zuerst, sonst der letzte Server-Stand.
    /// </summary>
    /// <param name="type">Normalisierter Medientyp.</param>
    /// <param name="mediaId">Id des Mediums.</param>
    /// <param name="pending">Lokale Fortschritts-Outbox.</param>
    /// <param name="cached">Server-Stand der Weiterschauen-Liste.</param>
    /// <returns>Fortschritt im Bereich 0–1; <c>null</c> = kein Fortschritt bekannt.</returns>
    private static double? ResolveProgress(
        string type,
        int mediaId,
        Dictionary<(string Type, int Id), (long PositionTicks, long DurationTicks)> pending,
        Dictionary<(string Type, long Id), (long PositionSeconds, long DurationSeconds)> cached)
    {
        if (pending.TryGetValue((type, mediaId), out var local) && local.DurationTicks > 0)
        {
            return Math.Clamp((double)local.PositionTicks / local.DurationTicks, 0d, 1d);
        }

        if (cached.TryGetValue((type, mediaId), out var server) && server.DurationSeconds > 0)
        {
            return Math.Clamp((double)server.PositionSeconds / server.DurationSeconds, 0d, 1d);
        }

        return null;
    }
}
