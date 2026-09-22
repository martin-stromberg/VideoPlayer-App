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

    public ProgressReportService(
        AuthSessionService authSession,
        PendingProgressSyncService pendingQueue,
        INetworkStatusMonitor networkMonitor)
    {
        this.authSession = authSession ?? throw new ArgumentNullException(nameof(authSession));
        this.pendingQueue = pendingQueue ?? throw new ArgumentNullException(nameof(pendingQueue));
        this.networkMonitor = networkMonitor ?? throw new ArgumentNullException(nameof(networkMonitor));
    }

    /// <summary>
    /// Meldet den Fortschritt eines Mediums; bei Offline oder
    /// überholbarem Sendefehler wird er persistent eingereiht.
    /// </summary>
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
                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (ApiException ex) when (IsPermanentClientError(ex))
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[ProgressReportService] Fortschritt für {videoType}/{videoId} verworfen (Status {(int?)ex.StatusCode}).");
                return;
            }
            catch (Exception ex) when (ex is ApiException or HttpRequestException or TaskCanceledException)
            {
                // Überholbarer Fehler → unten einreihen.
                System.Diagnostics.Debug.WriteLine(
                    $"[ProgressReportService] Fortschritt für {videoType}/{videoId} wird eingereiht: {ex.Message}");
            }
        }

        await pendingQueue.EnqueueAsync(videoType, videoId, position, duration);
    }

    /// <summary>
    /// Ein 4xx-Fehler (außer den wiederholbaren/authentifizierbaren Codes
    /// 401, 408, 429) wird bei jedem Versuch erneut scheitern.
    /// </summary>
    internal static bool IsPermanentClientError(ApiException exception)
    {
        var status = (int?)exception.StatusCode;
        return status is >= 400 and < 500 and not 401 and not 408 and not 429;
    }
}
