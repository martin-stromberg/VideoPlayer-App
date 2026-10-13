using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using VideoPlayer.ApiClient;
using VideoPlayer.Storage;

namespace VideoPlayer.Mvu;

/// <summary>
/// Meldet Wiedergabefortschritte an den Server. Ist die App offline, nicht
/// angemeldet oder schlägt die Übermittlung mit einem überholbaren Fehler
/// fehl, wird die Meldung in die persistente Warteschlange
/// (<see cref="PendingProgressSyncService"/>, backup-pflichtig) eingereiht
/// und bei der nächsten Synchronisation nachgesendet. Dauerhafte
/// Clientfehler (4xx außer 401/408/429) werden verworfen — sie würden bei
/// jedem Sync erneut scheitern.
/// </summary>
public sealed class ProgressReportService
{
    private readonly AuthSessionService authSession;
    private readonly PendingProgressSyncService pendingQueue;
    private readonly INetworkStatusMonitor networkMonitor;
    private readonly ILogger<ProgressReportService> logger;

    // Gemerktes Fehler-Outcome je Medium: Warnungen werden nur beim
    // Zustandswechsel (erster Fehlschlag bzw. Wechsel der Art)
    // protokolliert — sonst würde der 5-s-Fortschrittstakt bei einem
    // anhaltenden Fehler pro Takt einen Eintrag erzeugen. Ein Erfolg
    // löscht den Stand, sodass der nächste Fehlschlag wieder meldet.
    private readonly Dictionary<string, string> loggedOutcomes = new(StringComparer.Ordinal);

    /// <summary>Erstellt den Dienst.</summary>
    /// <param name="authSession">Sitzungsdienst mit dem angemeldeten API-Client.</param>
    /// <param name="pendingQueue">Persistente Warteschlange für nicht sendbare Meldungen.</param>
    /// <param name="networkMonitor">Netzwerkstatus-Erkennung (Online/Offline).</param>
    /// <param name="logger">Optionaler Logger für das Programmprotokoll.</param>
    public ProgressReportService(
        AuthSessionService authSession,
        PendingProgressSyncService pendingQueue,
        INetworkStatusMonitor networkMonitor,
        ILogger<ProgressReportService>? logger = null)
    {
        this.authSession = authSession ?? throw new ArgumentNullException(nameof(authSession));
        this.pendingQueue = pendingQueue ?? throw new ArgumentNullException(nameof(pendingQueue));
        this.networkMonitor = networkMonitor ?? throw new ArgumentNullException(nameof(networkMonitor));
        this.logger = logger ?? NullLogger<ProgressReportService>.Instance;
    }

    /// <summary>
    /// Meldet den Fortschritt eines Mediums; bei Offline oder
    /// überholbarem Sendefehler wird er persistent eingereiht.
    /// </summary>
    /// <param name="videoType">Medientyp des Videos (Server-Konvention).</param>
    /// <param name="videoId">Id des Videos.</param>
    /// <param name="position">Aktuelle Wiedergabeposition.</param>
    /// <param name="duration">Gesamtdauer des Videos.</param>
    /// <param name="cancellationToken">Bricht die Meldung ab.</param>
    public async Task ReportProgressAsync(
        string videoType,
        int videoId,
        TimeSpan position,
        TimeSpan duration,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(videoType);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(videoId);

        var client = authSession.Client;
        var key = videoType + "/" + videoId;
        if (client is not null && networkMonitor.IsOnline)
        {
            try
            {
                await client.ReportPlaybackProgressAsync(
                    videoType,
                    videoId,
                    (long)position.TotalSeconds,
                    (long)duration.TotalSeconds,
                    cancellationToken);
                loggedOutcomes.Remove(key);
                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (ApiException ex) when (IsPermanentClientError(ex))
            {
                LogOutcomeOnce(
                    key,
                    "discarded",
                    () => logger.LogWarning(
                        ex,
                        "Fortschritt für {VideoType}/{VideoId} verworfen (Status {StatusCode}).",
                        videoType,
                        videoId,
                        (int?)ex.StatusCode));
                return;
            }
            catch (Exception ex) when (ex is ApiException or HttpRequestException or TaskCanceledException)
            {
                // Überholbarer Fehler → unten einreihen.
                LogOutcomeOnce(
                    key,
                    "queued",
                    () => logger.LogWarning(
                        ex,
                        "Fortschritt für {VideoType}/{VideoId} wird eingereiht.",
                        videoType,
                        videoId));
            }
        }

        // Das Einreihen selbst protokolliert die Warteschlange beim
        // Zustandswechsel (neuer Eintrag des Mediums) — hier kein
        // weiterer Eintrag pro Fortschrittstakt.
        try
        {
            await pendingQueue.EnqueueAsync(videoType, videoId, position, duration);
            loggedOutcomes.Remove(key);
        }
        catch (Exception ex)
        {
            // Persistenter Schreibfehler der Warteschlange (z. B. ein
            // SQLite-Fehler der pending_progress-Tabelle): der nächste
            // Takt versucht erneut — gededupt je Medium, damit der
            // Fortschrittstakt nicht pro Meldung einen Eintrag erzeugt.
            LogOutcomeOnce(
                key,
                "enqueue_failed",
                () => logger.LogWarning(
                    ex,
                    "Einreihen des Fortschritts für {VideoType}/{VideoId} fehlgeschlagen.",
                    videoType,
                    videoId));
        }
    }

    /// <summary>
    /// Protokolliert ein Fehler-Outcome nur, wenn es sich gegenüber dem
    /// zuletzt gemerkten Stand desselben Mediums geändert hat
    /// (Zustandswechsel statt Eintrag pro Fortschrittstakt).
    /// </summary>
    /// <param name="key">Medienschlüssel (<c>type/id</c>).</param>
    /// <param name="outcome">Das neue Outcome (<c>queued</c>/<c>discarded</c>/<c>enqueue_failed</c>).</param>
    /// <param name="log">Der auszuführende Protokolleintrag.</param>
    private void LogOutcomeOnce(string key, string outcome, Action log)
    {
        if (loggedOutcomes.TryGetValue(key, out var previous) && previous == outcome)
        {
            return;
        }

        loggedOutcomes[key] = outcome;
        log();
    }

    /// <summary>
    /// Ein 4xx-Fehler (außer den wiederholbaren/authentifizierbaren Codes
    /// 401, 408, 429) wird bei jedem Versuch erneut scheitern.
    /// </summary>
    /// <param name="exception">Die zu prüfende API-Ausnahme.</param>
    /// <returns><c>true</c>, wenn der Fehler dauerhaft ist und die Meldung verworfen wird.</returns>
    internal static bool IsPermanentClientError(ApiException exception)
    {
        var status = (int?)exception.StatusCode;
        return status is >= 400 and < 500 and not 401 and not 408 and not 429;
    }
}
