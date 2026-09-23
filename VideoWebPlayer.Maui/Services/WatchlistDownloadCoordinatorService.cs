using VideoWebPlayer.Client;
using VideoWebPlayer.Client.Models;
using VideoWebPlayer.Maui.Models;
using VideoWebPlayer.Maui.Services.Events;

namespace VideoWebPlayer.Maui.Services;

/// <summary>
/// Koordiniert automatische Downloads der Watchlist ("Weiterschauen"-Liste).
/// Reagiert auf ContinueWatchingUpdated-Ereignisse und reiht fehlende Videos in die Download-Queue ein.
/// </summary>
public class WatchlistDownloadCoordinatorService
{
    private readonly VideoWebPlayerClient _client;
    private readonly ISubscribeNotificationEvent _subscriber;
    private readonly SemaphoreSlim _syncLock = new(1, 1);
    private DateTime _lastSyncUtc = DateTime.MinValue;

    private readonly Action<ContinueWatchingUpdatedEvent> _handler;

    /// <summary>
    /// Initialisiert eine neue Instanz der <see cref="WatchlistDownloadCoordinatorService"/>-Klasse
    /// und abonniert das <see cref="ContinueWatchingUpdatedEvent"/>.
    /// </summary>
    /// <param name="client">Der API-Client zum Abrufen der Weiterschauen-Liste.</param>
    /// <param name="subscriber">Der Event-Subscriber für Benachrichtigungs-Ereignisse.</param>
    public WatchlistDownloadCoordinatorService(VideoWebPlayerClient client, ISubscribeNotificationEvent subscriber)
    {
        _client = client;
        _subscriber = subscriber;

        _handler = e => _ = SyncAsync(force: false);
        _subscriber.Subscribe<ContinueWatchingUpdatedEvent>(_handler);
    }

    /// <summary>
    /// Führt eine sofortige Synchronisation der Watchlist beim App-Start aus (ohne Throttle).
    /// </summary>
    /// <returns>Ein Task, der die Synchronisation abschließt.</returns>
    public Task RunOnStartupAsync()
        => SyncAsync(force: true);

    private async Task SyncAsync(bool force)
    {
        // Keine parallelen Verarbeitungen
        if (!await _syncLock.WaitAsync(0))
            return;

        try
        {
            // Throttle (für häufige Events)
            if (!force && (DateTime.UtcNow - _lastSyncUtc) < TimeSpan.FromSeconds(10))
                return;
            _lastSyncUtc = DateTime.UtcNow;

            List<ContinueWatchingDto> list;
            try
            {
                list = await _client.GetContinueWatchingAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WatchlistDownloadCoordinator] ContinueWatching fetch failed: {ex.Message}");
                return;
            }

            foreach (var item in list)
            {
                var entryId = item.Entry?.Id;
                if (!entryId.HasValue)
                    continue;

                var videoType = DownloadManager.NormalizeVideoType(item.MediaType);
                var title = item.Title ?? item.Entry?.Name ?? $"{videoType} {entryId.Value}";

                var existing = await DownloadManager.Instance.GetDownloadAsync(entryId.Value, videoType);
                if (existing != null && existing.Status == DownloadStatus.Completed && File.Exists(existing.LocalFilePath))
                    continue;

                if (DownloadQueue.Instance.IsInQueue(entryId.Value, videoType))
                    continue;

                var request = new VideoRequest
                {
                    VideoId = entryId.Value,
                    VideoType = videoType,
                    Title = title
                };

                await DownloadManager.Instance.QueueDownloadAsync(request, DownloadRetentionType.Watchlist);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WatchlistDownloadCoordinator] Sync failed: {ex.Message}");
        }
        finally
        {
            _syncLock.Release();
        }
    }
}
