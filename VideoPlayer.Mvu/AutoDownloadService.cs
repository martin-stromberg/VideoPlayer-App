using VideoPlayer.ApiClient;
using VideoPlayer.ApiClient.Models;
using VideoPlayer.Storage;

namespace VideoPlayer.Mvu;

/// <summary>
/// Automatische Downloads (Service Layer): lädt bei aktivierter
/// Einstellung (<see cref="AppSettings.AutoDownloadEnabled"/>) die
/// nächsten Folge-Videos der Weiterschauen-Einträge vor — je Eintrag
/// bis zu <see cref="AppSettings.PreloadEpisodeCount"/>. Einträge mit
/// Playlist-Bezug (<see cref="HomeMediaItem.PlaylistId"/> +
/// <see cref="HomeMediaItem.PlaylistEntryId"/>) erhalten ihre Folge-
/// Videos aus der Playlist-Reihenfolge
/// (<c>api/playlists/{id}/entries/paged</c>, Server-Reihenfolge);
/// Einträge ohne Playlist-Bezug aus dem Serienkontext
/// (<c>api/items/tvshow/{showId}</c>, staffelübergreifend nach Staffel-/
/// Episodennummer — wie die Fortsetzungsliste der Detailseite).
/// Film-Einträge der Weiterschauen-Liste laden den Film selbst (bei
/// Playlist-Bezug zusätzlich die Folge-Einträge der Playlist). Bei
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

    private DateTime lastRunUtc = DateTime.MinValue;
    private int runInProgress;

    /// <summary>Erstellt den Dienst.</summary>
    /// <param name="authSession">Sitzungsdienst mit dem angemeldeten API-Client.</param>
    /// <param name="homeLists">Lesedienst der gecachten Startseiten-Listen (Weiterschauen-Stand).</param>
    /// <param name="downloads">Download-Verwaltung (Einreihen/Dedup).</param>
    /// <param name="networkMonitor">Netzwerkstatus-Erkennung (WLAN-Bedingung).</param>
    /// <param name="cache">Cache-Ablage für die Serienstruktur (Offline-Serienkontext).</param>
    public AutoDownloadService(
        AuthSessionService authSession,
        HomeListService homeLists,
        DownloadService downloads,
        INetworkStatusMonitor networkMonitor,
        CacheDatabase cache)
        : this(authSession, homeLists, downloads, networkMonitor, cache, null)
    {
    }

    /// <summary>Erstellt den Dienst mit injizierbarer Zeitquelle (für Tests).</summary>
    /// <param name="authSession">Sitzungsdienst mit dem angemeldeten API-Client.</param>
    /// <param name="homeLists">Lesedienst der gecachten Startseiten-Listen.</param>
    /// <param name="downloads">Download-Verwaltung (Einreihen/Dedup).</param>
    /// <param name="networkMonitor">Netzwerkstatus-Erkennung (WLAN-Bedingung).</param>
    /// <param name="cache">Cache-Ablage für die Serienstruktur.</param>
    /// <param name="utcNow">Zeitquelle für den Throttle; <c>null</c> = <see cref="DateTime.UtcNow"/>.</param>
    internal AutoDownloadService(
        AuthSessionService authSession,
        HomeListService homeLists,
        DownloadService downloads,
        INetworkStatusMonitor networkMonitor,
        CacheDatabase cache,
        Func<DateTime>? utcNow)
    {
        this.authSession = authSession ?? throw new ArgumentNullException(nameof(authSession));
        this.homeLists = homeLists ?? throw new ArgumentNullException(nameof(homeLists));
        this.downloads = downloads ?? throw new ArgumentNullException(nameof(downloads));
        this.networkMonitor = networkMonitor ?? throw new ArgumentNullException(nameof(networkMonitor));
        this.cache = cache ?? throw new ArgumentNullException(nameof(cache));
        this.utcNow = utcNow ?? (() => DateTime.UtcNow);
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
                return;
            }

            lastRunUtc = now;
            var lists = await homeLists.LoadHomeListsAsync(cancellationToken);
            foreach (var item in lists.ContinueWatching)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var type = MediaTypeNames.Normalize(item.MediaType);
                var hasPlaylistBinding = item.PlaylistId is > 0 && item.PlaylistEntryId is > 0;
                try
                {
                    if (type == "movie")
                    {
                        await downloads.EnqueueAsync(
                            "movie", item.Id, item.Title, settings,
                            playlistContext: hasPlaylistBinding
                                ? new DownloadPlaylistContext(item.PlaylistId, item.PlaylistEntryId, null)
                                : null,
                            cancellationToken: cancellationToken);
                        if (hasPlaylistBinding)
                        {
                            await EnqueueNextPlaylistEntriesAsync(item, settings, cancellationToken);
                        }
                    }
                    else if (type == "tvshowepisode")
                    {
                        var playlistHandled = hasPlaylistBinding
                            && await EnqueueNextPlaylistEntriesAsync(item, settings, cancellationToken);
                        if (!playlistHandled && item.ShowId is > 0)
                        {
                            await EnqueueNextEpisodesAsync(item, settings, cancellationToken);
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
                    System.Diagnostics.Debug.WriteLine($"[AutoDownloadService] Vorlauf für '{item.Title}' fehlgeschlagen: {ex.Message}");
                }
            }
        }
        finally
        {
            Interlocked.Exchange(ref runInProgress, 0);
        }
    }

    /// <summary>
    /// Reiht die nächsten Episoden eines Weiterschauen-Episoden-Eintrags
    /// ein: Serienkontext laden, alle Episoden staffelübergreifend ordnen
    /// und bis zu <c>PreloadEpisodeCount</c> Folge-Episoden einreihen.
    /// </summary>
    /// <param name="item">Der Weiterschauen-Eintrag (Episode).</param>
    /// <param name="settings">Aktuelle App-Einstellungen.</param>
    /// <param name="cancellationToken">Bricht den Lauf ab.</param>
    /// <returns>Eine abgeschlossene Aufgabe nach dem Einreihen.</returns>
    private async Task EnqueueNextEpisodesAsync(
        HomeMediaItem item,
        AppSettings settings,
        CancellationToken cancellationToken)
    {
        var client = authSession.Client;
        if (client is null || item.ShowId is not > 0)
        {
            return;
        }

        var show = await client.GetTvShowAsync(item.ShowId.Value, cancellationToken);

        // Die Serienstruktur im Cache ablegen — Grundlage der
        // Offline-Platzhalter fehlender Folge-Episoden.
        await ShowDetailsCache.WriteAsync(cache, show, cancellationToken);

        var episodes = show.Seasons
            .OrderBy(s => s.Number)
            .SelectMany(s => s.Episodes.OrderBy(e => e.Number).Select(e => (Season: s, Episode: e)))
            .ToList();
        var index = episodes.FindIndex(e => e.Episode.Id == item.Id);
        if (index < 0)
        {
            return;
        }

        var showName = show.Name;
        foreach (var (season, episode) in episodes.Skip(index + 1).Take(settings.PreloadEpisodeCount))
        {
            var title = string.IsNullOrWhiteSpace(episode.Name)
                ? $"{showName} – Episode {episode.Number}"
                : $"{showName} – {episode.Name}";
            await downloads.EnqueueAsync(
                "tvshowepisode", episode.Id, title, settings,
                new DownloadSeriesContext(show.Id, showName, season.Number, episode.Number),
                cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// Reiht die nächsten Einträge eines playlist-gebundenen
    /// Weiterschauen-Eintrags in Playlist-Reihenfolge ein: die Einträge
    /// der Playlist werden seitenweise in Server-Reihenfolge geladen,
    /// bis der aktuelle Eintrag (<see cref="HomeMediaItem.PlaylistEntryId"/>)
    /// gefunden ist; die folgenden ladbaren und zugänglichen Einträge
    /// (<c>movie</c>/<c>tvshowepisode</c>) werden bis zu
    /// <c>PreloadEpisodeCount</c> mit ihrem Playlist-Bezug eingeplant.
    /// Der Serien-Abruf entfällt — Serien-Hints kommen aus den
    /// <c>ParentMedia*</c>-Feldern der Einträge.
    /// </summary>
    /// <param name="item">Der Weiterschauen-Eintrag mit Playlist-Bezug.</param>
    /// <param name="settings">Aktuelle App-Einstellungen.</param>
    /// <param name="cancellationToken">Bricht den Lauf ab.</param>
    /// <returns>
    /// <c>true</c>, wenn der aktuelle Eintrag in der Playlist gefunden
    /// wurde (Playlist-Pfad greift — auch ohne verbleibende Folge-
    /// Einträge); <c>false</c> = Rückfall auf den Serien-Pfad.
    /// </returns>
    private async Task<bool> EnqueueNextPlaylistEntriesAsync(
        HomeMediaItem item,
        AppSettings settings,
        CancellationToken cancellationToken)
    {
        var client = authSession.Client;
        if (client is null || item.PlaylistId is not > 0 || item.PlaylistEntryId is not > 0)
        {
            return false;
        }

        var currentEntryId = item.PlaylistEntryId.Value;
        var found = false;
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
                System.Diagnostics.Debug.WriteLine(
                    $"[AutoDownloadService] Playlist-Einträge nicht ladbar (Playlist {item.PlaylistId}): {ex.Message}");
                return false;
            }

            foreach (var entry in result.Entries)
            {
                if (!found)
                {
                    found = entry.Id == currentEntryId;
                    continue;
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

                await downloads.EnqueueAsync(
                    type, entry.MediaId, PlaylistEntryTitle(entry), settings,
                    SeriesContextOf(entry),
                    new DownloadPlaylistContext(item.PlaylistId, entry.Id, entry.SortOrder),
                    cancellationToken);
                remaining--;
            }

            if (!result.HasNextPage)
            {
                break;
            }
        }

        return found;
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
}
