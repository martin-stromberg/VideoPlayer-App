using VideoPlayer.ApiClient;
using VideoPlayer.Storage;

namespace VideoPlayer.Mvu;

/// <summary>
/// Automatische Downloads (Service Layer): lädt bei aktivierter
/// Einstellung (<see cref="AppSettings.AutoDownloadEnabled"/>) die
/// nächsten Episoden der Weiterschauen-Einträge vor — je Eintrag bis zu
/// <see cref="AppSettings.PreloadEpisodeCount"/> Folge-Episoden aus dem
/// Serienkontext (<c>api/items/tvshow/{showId}</c>, staffelübergreifend
/// nach Staffel-/Episodennummer — wie die Fortsetzungsliste der
/// Detailseite). Film-Einträge der Weiterschauen-Liste laden den Film
/// selbst. Bei aktivem „Downloads nur im WLAN"
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

    private readonly AuthSessionService authSession;
    private readonly HomeListService homeLists;
    private readonly DownloadService downloads;
    private readonly INetworkStatusMonitor networkMonitor;
    private readonly Func<DateTime> utcNow;

    private DateTime lastRunUtc = DateTime.MinValue;
    private int runInProgress;

    /// <summary>Erstellt den Dienst.</summary>
    /// <param name="authSession">Sitzungsdienst mit dem angemeldeten API-Client.</param>
    /// <param name="homeLists">Lesedienst der gecachten Startseiten-Listen (Weiterschauen-Stand).</param>
    /// <param name="downloads">Download-Verwaltung (Einreihen/Dedup).</param>
    /// <param name="networkMonitor">Netzwerkstatus-Erkennung (WLAN-Bedingung).</param>
    public AutoDownloadService(
        AuthSessionService authSession,
        HomeListService homeLists,
        DownloadService downloads,
        INetworkStatusMonitor networkMonitor)
        : this(authSession, homeLists, downloads, networkMonitor, null)
    {
    }

    /// <summary>Erstellt den Dienst mit injizierbarer Zeitquelle (für Tests).</summary>
    /// <param name="authSession">Sitzungsdienst mit dem angemeldeten API-Client.</param>
    /// <param name="homeLists">Lesedienst der gecachten Startseiten-Listen.</param>
    /// <param name="downloads">Download-Verwaltung (Einreihen/Dedup).</param>
    /// <param name="networkMonitor">Netzwerkstatus-Erkennung (WLAN-Bedingung).</param>
    /// <param name="utcNow">Zeitquelle für den Throttle; <c>null</c> = <see cref="DateTime.UtcNow"/>.</param>
    internal AutoDownloadService(
        AuthSessionService authSession,
        HomeListService homeLists,
        DownloadService downloads,
        INetworkStatusMonitor networkMonitor,
        Func<DateTime>? utcNow)
    {
        this.authSession = authSession ?? throw new ArgumentNullException(nameof(authSession));
        this.homeLists = homeLists ?? throw new ArgumentNullException(nameof(homeLists));
        this.downloads = downloads ?? throw new ArgumentNullException(nameof(downloads));
        this.networkMonitor = networkMonitor ?? throw new ArgumentNullException(nameof(networkMonitor));
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
                try
                {
                    if (type == "movie")
                    {
                        await downloads.EnqueueAsync("movie", item.Id, item.Title, settings, cancellationToken);
                    }
                    else if (type == "tvshowepisode" && item.ShowId is > 0)
                    {
                        await EnqueueNextEpisodesAsync(item, settings, cancellationToken);
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
        var episodes = show.Seasons
            .OrderBy(s => s.Number)
            .SelectMany(s => s.Episodes.OrderBy(e => e.Number))
            .ToList();
        var index = episodes.FindIndex(e => e.Id == item.Id);
        if (index < 0)
        {
            return;
        }

        var showName = show.Name;
        foreach (var episode in episodes.Skip(index + 1).Take(settings.PreloadEpisodeCount))
        {
            var title = string.IsNullOrWhiteSpace(episode.Name)
                ? $"{showName} – Episode {episode.Number}"
                : $"{showName} – {episode.Name}";
            await downloads.EnqueueAsync("tvshowepisode", episode.Id, title, settings, cancellationToken);
        }
    }
}
