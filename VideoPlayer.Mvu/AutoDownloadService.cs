using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using VideoPlayer.ApiClient;
using VideoPlayer.ApiClient.Models;
using VideoPlayer.Storage;

namespace VideoPlayer.Mvu;

/// <summary>
/// Automatische Downloads (Service Layer): lädt bei aktivierter
/// Einstellung (<see cref="AppSettings.AutoDownloadEnabled"/>) die
/// aktuelle und die folgenden Videos der Weiterschauen-Einträge vor —
/// je Eintrag bis zu <see cref="AppSettings.PreloadEpisodeCount"/>
/// Kandidaten einschließlich des aktuellen Titels. Einträge mit
/// Playlist-Bezug (<see cref="HomeMediaItem.PlaylistId"/> +
/// <see cref="HomeMediaItem.PlaylistEntryId"/>) erhalten ihre aktuellen
/// und folgenden Videos aus der Playlist-Reihenfolge
/// (<c>api/playlists/{id}/entries/paged</c>, Server-Reihenfolge);
/// Einträge ohne Playlist-Bezug aus dem Serienkontext
/// (<c>api/items/tvshow/{showId}</c>, staffelübergreifend nach Staffel-/
/// Episodennummer — wie die Fortsetzungsliste der Detailseite).
/// Film-Einträge der Weiterschauen-Liste laden den Film selbst; bei
/// Playlist-Bezug zählt der Film ins Budget — Film + Folge-Einträge
/// zusammen ≤ <see cref="AppSettings.PreloadEpisodeCount"/>. Bei
/// aktivem „Downloads nur im WLAN"
/// (<see cref="AppSettings.WifiOnlyDownloads"/>) läuft der Vorlauf nur
/// bei WLAN-Verbindung. Die Weiterschauen-Liste wird aus der Cache-
/// Kopie gelesen (kein zusätzlicher Serveraufruf); Läufe werden auf
/// <see cref="MinRunInterval"/> gedrosselt (Trigger durch
/// <c>RunAutoDownloadsEffect</c>, z. B. nach Synchronisationen).
/// </summary>
public sealed class AutoDownloadService
{
    /// <summary>Mindestabstand zwischen zwei automatischen Läufen (Throttle).</summary>
    /// <value>60 Sekunden.</value>
    public static readonly TimeSpan MinRunInterval = TimeSpan.FromSeconds(60);

    /// <summary>Seitengröße der Playlist-Eintragsabfrage des Vorlaufs.</summary>
    private const int PlaylistEntriesPageSize = 50;

    private readonly AuthSessionService authSession;
    private readonly HomeListService homeLists;
    private readonly DownloadService downloads;
    private readonly INetworkStatusMonitor networkMonitor;
    private readonly CacheDatabase cache;
    private readonly Func<DateTime> utcNow;
    private readonly ILogger<AutoDownloadService> logger;

    private DateTime lastRunUtc = DateTime.MinValue;
    private int runInProgress;

    /// <summary>Erstellt den Dienst.</summary>
    /// <param name="authSession">Sitzungsdienst mit dem angemeldeten API-Client.</param>
    /// <param name="homeLists">Lesedienst der gecachten Startseiten-Listen (Weiterschauen-Stand).</param>
    /// <param name="downloads">Download-Verwaltung (Einreihen/Dedup).</param>
    /// <param name="networkMonitor">Netzwerkstatus-Erkennung (WLAN-Bedingung).</param>
    /// <param name="cache">Cache-Ablage für die Serienstruktur (Offline-Serienkontext).</param>
    /// <param name="logger">Optionaler Logger für das Programmprotokoll.</param>
    public AutoDownloadService(
        AuthSessionService authSession,
        HomeListService homeLists,
        DownloadService downloads,
        INetworkStatusMonitor networkMonitor,
        CacheDatabase cache,
        ILogger<AutoDownloadService>? logger = null)
        : this(authSession, homeLists, downloads, networkMonitor, cache, null, logger)
    {
    }

    /// <summary>Erstellt den Dienst mit injizierbarer Zeitquelle (für Tests).</summary>
    /// <param name="authSession">Sitzungsdienst mit dem angemeldeten API-Client.</param>
    /// <param name="homeLists">Lesedienst der gecachten Startseiten-Listen.</param>
    /// <param name="downloads">Download-Verwaltung (Einreihen/Dedup).</param>
    /// <param name="networkMonitor">Netzwerkstatus-Erkennung (WLAN-Bedingung).</param>
    /// <param name="cache">Cache-Ablage für die Serienstruktur.</param>
    /// <param name="utcNow">Zeitquelle für den Throttle; <c>null</c> = <see cref="DateTime.UtcNow"/>.</param>
    /// <param name="logger">Optionaler Logger für das Programmprotokoll.</param>
    internal AutoDownloadService(
        AuthSessionService authSession,
        HomeListService homeLists,
        DownloadService downloads,
        INetworkStatusMonitor networkMonitor,
        CacheDatabase cache,
        Func<DateTime>? utcNow,
        ILogger<AutoDownloadService>? logger = null)
    {
        this.authSession = authSession ?? throw new ArgumentNullException(nameof(authSession));
        this.homeLists = homeLists ?? throw new ArgumentNullException(nameof(homeLists));
        this.downloads = downloads ?? throw new ArgumentNullException(nameof(downloads));
        this.networkMonitor = networkMonitor ?? throw new ArgumentNullException(nameof(networkMonitor));
        this.cache = cache ?? throw new ArgumentNullException(nameof(cache));
        this.utcNow = utcNow ?? (() => DateTime.UtcNow);
        this.logger = logger ?? NullLogger<AutoDownloadService>.Instance;
    }

    /// <summary>
    /// Führt einen automatischen Vorlauf-Lauf aus: bei deaktiviertem
    /// Schalter, fehlendem Client, nicht erfüllter WLAN-Bedingung oder
    /// innerhalb des <see cref="MinRunInterval"/>-Fensters ohne Wirkung.
    /// Fehler einzelner Einträge werden protokolliert und übersprungen.
    /// </summary>
    /// <param name="settings">Aktuelle App-Einstellungen.</param>
    /// <param name="force"><c>true</c> = Throttle-Fenster ignorieren (z. B. beim Einschalten der Einstellung).</param>
    /// <param name="cancellationToken">Bricht den Lauf ab.</param>
    /// <returns>Eine abgeschlossene Aufgabe nach dem Lauf.</returns>
    public async Task RunAsync(AppSettings settings, bool force = false, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (!settings.AutoDownloadEnabled
            || (settings.WifiOnlyDownloads && !networkMonitor.IsWifi)
            || authSession.Client is null)
        {
            return;
        }

        // Kein zweiter Lauf parallel — Trigger (Sync-Abschluss,
        // Cache-Änderung, Einstellung) können dicht aufeinander folgen.
        if (Interlocked.Exchange(ref runInProgress, 1) != 0)
        {
            return;
        }

        try
        {
            var now = utcNow();
            if (!force && now - lastRunUtc < MinRunInterval)
            {
                logger.LogInformation("Auto-Download-Lauf ausgelassen (Mindestabstand nicht erreicht).");
                return;
            }

            lastRunUtc = now;
            var lists = await homeLists.LoadHomeListsAsync(cancellationToken);
            var enqueued = 0;
            foreach (var item in lists.ContinueWatching)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var type = MediaTypeNames.Normalize(item.MediaType);
                var hasPlaylistBinding = item.PlaylistId is > 0 && item.PlaylistEntryId is > 0;
                try
                {
                    if (type == "movie")
                    {
                        if (await EnqueueSelfAsync(item, "movie", settings, cancellationToken))
                        {
                            enqueued++;
                        }

                        if (hasPlaylistBinding)
                        {
                            enqueued += await EnqueuePlaylistEntriesFromCurrentAsync(item, settings, cancellationToken) ?? 0;
                        }
                    }
                    else if (type == "tvshowepisode")
                    {
                        var playlistResult = hasPlaylistBinding
                            ? await EnqueuePlaylistEntriesFromCurrentAsync(item, settings, cancellationToken)
                            : null;
                        if (playlistResult is { } playlistEnqueued)
                        {
                            enqueued += playlistEnqueued;
                        }
                        else if (item.ShowId is > 0)
                        {
                            enqueued += await EnqueueEpisodesFromCurrentAsync(item, settings, cancellationToken);
                        }
                        else
                        {
                            // Weder Playlist-Treffer noch Serienbezug:
                            // die aktuelle Episode reiht sich selbst
                            // ein — der Download braucht keinen
                            // Folge-Kontext.
                            if (await EnqueueSelfAsync(item, "tvshowepisode", settings, cancellationToken))
                            {
                                enqueued++;
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex) when (ex is ApiException or HttpRequestException or TaskCanceledException or InvalidOperationException)
                {
                    // Fehler eines Eintrags stoppen den Lauf nicht.
                    logger.LogWarning(ex, "Vorlauf für {MediaType}/{MediaId} fehlgeschlagen.", type, item.Id);
                }
            }

            logger.LogInformation("Auto-Download-Lauf abgeschlossen: {EnqueuedCount} Aufträge eingeplant.", enqueued);
        }
        finally
        {
            Interlocked.Exchange(ref runInProgress, 0);
        }
    }

    /// <summary>
    /// Reiht die aktuelle und die folgenden Episoden eines
    /// Weiterschauen-Episoden-Eintrags ein: Serienkontext laden, alle
    /// Episoden staffelübergreifend ordnen und ab der aktuellen Episode
    /// bis zu <c>PreloadEpisodeCount</c> Episoden einreihen.
    /// </summary>
    /// <param name="item">Der Weiterschauen-Eintrag (Episode).</param>
    /// <param name="settings">Aktuelle App-Einstellungen.</param>
    /// <param name="cancellationToken">Bricht den Lauf ab.</param>
    /// <returns>Anzahl der eingereihten Episoden (aktuelle Episode eingeschlossen).</returns>
    private async Task<int> EnqueueEpisodesFromCurrentAsync(
        HomeMediaItem item,
        AppSettings settings,
        CancellationToken cancellationToken)
    {
        var client = authSession.Client;
        if (client is null || item.ShowId is not > 0)
        {
            return 0;
        }

        var show = await client.GetTvShowAsync(item.ShowId.Value, cancellationToken);

        // Die Serienstruktur im Cache ablegen — Grundlage der
        // Offline-Platzhalter fehlender Folge-Episoden.
        await ShowDetailsCache.WriteAsync(cache, show, logger, cancellationToken);

        var episodes = show.Seasons
            .OrderBy(s => s.Number)
            .SelectMany(s => s.Episodes.OrderBy(e => e.Number).Select(e => (Season: s, Episode: e)))
            .ToList();
        var index = episodes.FindIndex(e => e.Episode.Id == item.Id);
        if (index < 0)
        {
            return 0;
        }

        var enqueued = 0;
        var showName = show.Name;
        foreach (var (season, episode) in episodes.Skip(index).Take(settings.PreloadEpisodeCount))
        {
            var title = string.IsNullOrWhiteSpace(episode.Name)
                ? $"{showName} – Episode {episode.Number}"
                : $"{showName} – {episode.Name}";
            var enqueueResult = await downloads.EnqueueAsync(
                "tvshowepisode", episode.Id, title, settings,
                new DownloadSeriesContext(show.Id, showName, season.Number, episode.Number),
                pictureId: episode.GeneratedBackgroundPictureId ?? episode.PosterPictureId,
                cancellationToken: cancellationToken);
            if (enqueueResult.Notice == DownloadService.NoticeEnqueued)
            {
                enqueued++;
            }
        }

        if (enqueued > 0)
        {
            logger.LogInformation(
                "{EnqueuedCount} aktuelle und folgende Episoden für Episode {MediaId} eingeplant.",
                enqueued,
                item.Id);
        }

        return enqueued;
    }

    /// <summary>
    /// Reiht den aktuellen Eintrag und die folgenden Einträge eines
    /// playlist-gebundenen Weiterschauen-Eintrags in Playlist-
    /// Reihenfolge ein: die Einträge der Playlist werden seitenweise in
    /// Server-Reihenfolge geladen, bis der aktuelle Eintrag
    /// (<see cref="HomeMediaItem.PlaylistEntryId"/>) gefunden ist; ab
    /// einschließlich dieses Eintrags werden die ladbaren und
    /// zugänglichen Einträge (<c>movie</c>/<c>tvshowepisode</c>) bis zu
    /// <c>PreloadEpisodeCount</c> mit ihrem Playlist-Bezug eingeplant.
    /// Der Serien-Abruf entfällt — Serien-Hints kommen aus den
    /// <c>ParentMedia*</c>-Feldern der Einträge.
    /// </summary>
    /// <param name="item">Der Weiterschauen-Eintrag mit Playlist-Bezug.</param>
    /// <param name="settings">Aktuelle App-Einstellungen.</param>
    /// <param name="cancellationToken">Bricht den Lauf ab.</param>
    /// <returns>
    /// Anzahl der eingereihten Einträge, wenn der aktuelle Eintrag
    /// in der Playlist gefunden wurde (Playlist-Pfad greift — auch ohne
    /// verbleibende Folge-Einträge); <c>null</c> = Rückfall auf den
    /// Serien-Pfad.
    /// </returns>
    private async Task<int?> EnqueuePlaylistEntriesFromCurrentAsync(
        HomeMediaItem item,
        AppSettings settings,
        CancellationToken cancellationToken)
    {
        var client = authSession.Client;
        if (client is null || item.PlaylistId is not > 0 || item.PlaylistEntryId is not > 0)
        {
            return null;
        }

        var currentEntryId = item.PlaylistEntryId.Value;
        var found = false;
        var enqueued = 0;
        var remaining = settings.PreloadEpisodeCount;
        for (var page = 1; remaining > 0; page++)
        {
            PlaylistEntriesPagedResult result;
            try
            {
                result = await client.GetPlaylistEntriesPagedAsync(
                    item.PlaylistId.Value, page, PlaylistEntriesPageSize, cancellationToken);
            }
            catch (Exception ex) when (ex is ApiException or HttpRequestException or InvalidOperationException or System.Text.Json.JsonException)
            {
                // Playlist nicht erreichbar/entfernt/unlesbar → Rückfall
                // auf den Serien-Pfad (bzw. nur die Selbst-Einreihung bei Filmen).
                logger.LogWarning(ex, "Playlist-Einträge nicht ladbar (Playlist {PlaylistId}).", item.PlaylistId);
                return null;
            }

            foreach (var entry in result.Entries)
            {
                if (!found)
                {
                    found = entry.Id == currentEntryId;
                    if (!found)
                    {
                        continue;
                    }

                    // Der gefundene aktuelle Eintrag ist selbst erster
                    // Kandidat — bei bestandener Ladbarkeitsprüfung
                    // (unten) verbraucht er einen Budget-Platz, auch
                    // wenn die Deduplizierung die Einreihung verwirft
                    // (bereits direkt eingereihter Film); ein nicht
                    // ladbarer Eintrag wird ohne Budget-Verbrauch
                    // übersprungen.
                }

                if (remaining <= 0)
                {
                    break;
                }

                var type = MediaTypeNames.Normalize(entry.MediaType);
                if (type is not ("movie" or "tvshowepisode") || !entry.IsAccessible)
                {
                    continue;
                }

                var enqueueResult = await downloads.EnqueueAsync(
                    type, entry.MediaId, PlaylistEntryTitle(entry), settings,
                    SeriesContextOf(entry),
                    new DownloadPlaylistContext(item.PlaylistId, entry.Id, entry.SortOrder),
                    entry.ResolvedPictureId,
                    cancellationToken);
                if (enqueueResult.Notice == DownloadService.NoticeEnqueued)
                {
                    enqueued++;
                }

                remaining--;
            }

            if (!result.HasNextPage)
            {
                break;
            }
        }

        if (!found)
        {
            return null;
        }

        if (enqueued > 0)
        {
            logger.LogInformation(
                "{EnqueuedCount} Playlist-Einträge ab dem aktuellen Eintrag für Playlist {PlaylistId} eingeplant.",
                enqueued,
                item.PlaylistId);
        }

        return enqueued;
    }

    /// <summary>
    /// Anzeigetitel eines Playlist-Eintrags für das Manifest — bei
    /// Episoden mit Serien-Elternkontext „Serie – Episode" (Konvention
    /// des Serien-Vorlaufs), sonst der Medientitel des Eintrags.
    /// </summary>
    /// <param name="entry">Der Playlist-Eintrag.</param>
    /// <returns>Der Anzeigetitel des Downloads.</returns>
    private static string PlaylistEntryTitle(PlaylistEntry entry)
    {
        return MediaTypeNames.Normalize(entry.MediaType) == "tvshowepisode"
            && MediaTypeNames.Normalize(entry.ParentMediaType) == "tvshow"
            && !string.IsNullOrWhiteSpace(entry.ParentMediaTitle)
            && !string.IsNullOrWhiteSpace(entry.MediaTitle)
                ? $"{entry.ParentMediaTitle} – {entry.MediaTitle}"
                : entry.MediaTitle;
    }

    /// <summary>
    /// Serienkontext eines Playlist-Episoden-Eintrags aus den
    /// <c>ParentMedia*</c>-Feldern — die Staffelnummer ist dem Eintrag
    /// nicht bekannt und bleibt offen (die Offline-Platzhalter-Logik
    /// löst die Folge-Episode notfalls über die Medien-Id auf).
    /// </summary>
    /// <param name="entry">Der Playlist-Eintrag.</param>
    /// <returns>Der teilbefüllte Serienkontext bzw. <c>null</c> für Nicht-Episoden bzw. ohne jede Serienangabe.</returns>
    private static DownloadSeriesContext? SeriesContextOf(PlaylistEntry entry)
    {
        if (MediaTypeNames.Normalize(entry.MediaType) != "tvshowepisode")
        {
            return null;
        }

        var parentIsShow = MediaTypeNames.Normalize(entry.ParentMediaType) == "tvshow";
        var showId = parentIsShow ? entry.ParentMediaId : null;
        var showTitle = parentIsShow ? entry.ParentMediaTitle : null;
        if (showId is null && entry.EpisodeNumber is null)
        {
            return null;
        }

        return new DownloadSeriesContext(showId, showTitle, null, entry.EpisodeNumber);
    }

    /// <summary>
    /// Reiht den Weiterschauen-Eintrag selbst direkt ein — für Filme
    /// (der Film ist sein eigener Download) und für Episoden ohne
    /// Playlist-Treffer und ohne Serienbezug (Orphan-Fallback). Bei
    /// Playlist-Bindung erhält der Download seinen Playlist-Bezug.
    /// </summary>
    /// <param name="item">Der Weiterschauen-Eintrag.</param>
    /// <param name="mediaType">Normalisierter Medientyp (<c>movie</c>/<c>tvshowepisode</c>).</param>
    /// <param name="settings">Aktuelle App-Einstellungen.</param>
    /// <param name="cancellationToken">Bricht den Lauf ab.</param>
    /// <returns><c>true</c>, wenn der Eintrag neu eingeplant wurde (<see cref="DownloadService.NoticeEnqueued"/>).</returns>
    private async Task<bool> EnqueueSelfAsync(
        HomeMediaItem item,
        string mediaType,
        AppSettings settings,
        CancellationToken cancellationToken)
    {
        var hasPlaylistBinding = item.PlaylistId is > 0 && item.PlaylistEntryId is > 0;
        var result = await downloads.EnqueueAsync(
            mediaType, item.Id, item.Title, settings,
            playlistContext: hasPlaylistBinding
                ? new DownloadPlaylistContext(item.PlaylistId, item.PlaylistEntryId, null)
                : null,
            pictureId: item.PosterPictureId,
            cancellationToken: cancellationToken);
        return result.Notice == DownloadService.NoticeEnqueued;
    }
}
