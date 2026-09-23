using System.Text.Json;
using VideoPlayer.ApiClient;
using VideoPlayer.ApiClient.Models;
using VideoPlayer.Storage;
using VideoPlayer.Storage.Entities;

namespace VideoPlayer.Mvu;

/// <summary>Ergebnis einer vollständigen Synchronisation.</summary>
/// <param name="SentProgressReports">Anzahl erfolgreich nachgesendeter Fortschrittsmeldungen.</param>
/// <param name="RemainingProgressReports">Anzahl weiterhin wartender Fortschrittsmeldungen.</param>
/// <param name="MetadataSynced"><c>true</c>, wenn Quellen und „Neu im Programm" aktualisiert wurden.</param>
/// <param name="ContinueWatchingSynced"><c>true</c>, wenn die Weiterschauen-Liste aktualisiert wurde.</param>
/// <param name="FavoritesSynced"><c>true</c>, wenn die Favoriten-Kopie erneuert wurde.</param>
/// <returns>Ergebnis-Datensatz der Synchronisation.</returns>
public sealed record SyncResult(
    int SentProgressReports,
    int RemainingProgressReports,
    bool MetadataSynced,
    bool ContinueWatchingSynced,
    bool FavoritesSynced);

/// <summary>
/// Synchronisation nach (Wieder-)Verbindung: Nachsenden der wartenden
/// Fortschrittsmeldungen aus der backup-pflichtigen Warteschlange sowie
/// vollständige bzw. gezielte Aktualisierung der Metadaten-Caches
/// (Quellen, „Neu im Programm", Weiterschauen, Favoriten) in der
/// <see cref="CacheDatabase"/>.
/// </summary>
public sealed class ServerSyncService
{
    /// <summary>Cache-Schlüssel der Medienquellen in <c>cached_lists</c>.</summary>
    public const string SourcesCacheKey = "sources";

    /// <summary>Cache-Schlüssel der zuletzt hinzugefügten Inhalte.</summary>
    public const string RecentCacheKey = "recent";

    /// <summary>Cache-Schlüssel der Weiterschauen-Liste.</summary>
    public const string ContinueWatchingCacheKey = "continue_watching";

    private readonly AuthSessionService authSession;
    private readonly PendingProgressSyncService pendingProgress;
    private readonly CacheDatabase cache;

    /// <summary>Erstellt den Dienst.</summary>
    /// <param name="authSession">Sitzungsdienst mit dem angemeldeten API-Client.</param>
    /// <param name="pendingProgress">Persistente Warteschlange der Fortschrittsmeldungen.</param>
    /// <param name="cache">Lokale Cache-Ablage für die Metadaten.</param>
    public ServerSyncService(
        AuthSessionService authSession,
        PendingProgressSyncService pendingProgress,
        CacheDatabase cache)
    {
        this.authSession = authSession ?? throw new ArgumentNullException(nameof(authSession));
        this.pendingProgress = pendingProgress ?? throw new ArgumentNullException(nameof(pendingProgress));
        this.cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    /// <summary>
    /// Vollständige Synchronisation: erst werden die wartenden
    /// Fortschritte nachgesendet (damit die anschließend gelesene
    /// Weiterschauen-Liste den aktuellen Stand enthält), dann die
    /// Metadaten-Caches erneuert. Ohne angemeldeten Client ein No-Op.
    /// </summary>
    /// <param name="cancellationToken">Bricht die Synchronisation ab.</param>
    /// <returns>Das Ergebnis der Synchronisation.</returns>
    public async Task<SyncResult> SynchronizeAsync(CancellationToken cancellationToken = default)
    {
        if (authSession.Client is null)
        {
            return new SyncResult(0, await pendingProgress.CountAsync(), false, false, false);
        }

        var sent = await FlushPendingProgressAsync(cancellationToken);
        var metadata = await SyncMetadataAsync(cancellationToken);
        var continueWatching = await SyncContinueWatchingAsync(cancellationToken);
        var favorites = await SyncFavoritesAsync(cancellationToken);

        return new SyncResult(
            sent,
            await pendingProgress.CountAsync(),
            metadata,
            continueWatching,
            favorites);
    }

    /// <summary>
    /// Sendet die wartenden Fortschrittsmeldungen nach — pro Medium nur
    /// die neueste, in chronologischer Reihenfolge. Transport- und
    /// Serverfehler brechen den Lauf ab (der Rest bleibt in der
    /// Warteschlange); permanente Clientfehler verwerfen den Eintrag.
    /// </summary>
    /// <param name="cancellationToken">Bricht das Nachsenden ab.</param>
    /// <returns>Anzahl erfolgreich gesendeter Meldungen.</returns>
    public async Task<int> FlushPendingProgressAsync(CancellationToken cancellationToken = default)
    {
        var client = authSession.Client;
        if (client is null)
        {
            return 0;
        }

        var sent = 0;
        foreach (var entry in await pendingProgress.GetPendingAsync())
        {
            try
            {
                await client.ReportPlaybackProgressAsync(
                    entry.VideoType,
                    entry.VideoId,
                    (long)TimeSpan.FromTicks(entry.PositionTicks).TotalSeconds,
                    (long)TimeSpan.FromTicks(entry.DurationTicks).TotalSeconds,
                    cancellationToken);
                sent++;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (ApiException ex) when (ProgressReportService.IsPermanentClientError(ex))
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[ServerSyncService] Fortschritt {entry.VideoType}/{entry.VideoId} verworfen (Status {(int?)ex.StatusCode}).");
            }
            catch (Exception ex) when (ex is ApiException or HttpRequestException or TaskCanceledException)
            {
                // Server wieder weg o. ä. — Rest bleibt in der Warteschlange.
                System.Diagnostics.Debug.WriteLine(
                    $"[ServerSyncService] Nachsenden abgebrochen: {ex.Message}");
                break;
            }

            await pendingProgress.RemoveForMediaAsync(entry.VideoType, entry.VideoId);
        }

        return sent;
    }

    /// <summary>Aktualisiert die gecachten Quellen und „Neu im Programm".</summary>
    /// <param name="cancellationToken">Bricht die Aktualisierung ab.</param>
    /// <returns><c>true</c> bei Erfolg; <c>false</c> ohne Client oder bei Fehler.</returns>
    public async Task<bool> SyncMetadataAsync(CancellationToken cancellationToken = default)
    {
        return await TrySyncAsync(async client =>
        {
            var sources = await client.GetSourcesAsync(cancellationToken);
            await WriteCachedListAsync(SourcesCacheKey, sources);

            var recent = await client.GetRecentEntriesAsync(cancellationToken);
            await WriteCachedListAsync(RecentCacheKey, recent);
        });
    }

    /// <summary>Aktualisiert die gecachte Weiterschauen-Liste.</summary>
    /// <param name="cancellationToken">Bricht die Aktualisierung ab.</param>
    /// <returns><c>true</c> bei Erfolg; <c>false</c> ohne Client oder bei Fehler.</returns>
    public async Task<bool> SyncContinueWatchingAsync(CancellationToken cancellationToken = default)
    {
        return await TrySyncAsync(async client =>
        {
            var entries = await client.GetContinueWatchingAsync(cancellationToken);
            await WriteCachedListAsync(ContinueWatchingCacheKey, entries);
        });
    }

    /// <summary>Ersetzt die lokale Kopie der serverseitigen Favoriten.</summary>
    /// <param name="cancellationToken">Bricht die Aktualisierung ab.</param>
    /// <returns><c>true</c> bei Erfolg; <c>false</c> ohne Client oder bei Fehler.</returns>
    public async Task<bool> SyncFavoritesAsync(CancellationToken cancellationToken = default)
    {
        return await TrySyncAsync(async client =>
        {
            var favorites = await client.GetFavoritesAsync(cancellationToken);

            await cache.Connection.DeleteAllAsync<CachedFavoritesEntry>();
            foreach (var favorite in favorites)
            {
                await cache.Connection.InsertAsync(new CachedFavoritesEntry
                {
                    Key = $"{MediaTypeOf(favorite.Entry)}:{favorite.Entry?.Id ?? favorite.Id}",
                    VideoId = (int)(favorite.Entry?.Id ?? favorite.Id),
                    VideoType = MediaTypeOf(favorite.Entry),
                    Json = JsonSerializer.Serialize(favorite),
                    UpdatedAtUtc = DateTime.UtcNow
                });
            }
        });
    }

    private async Task<bool> TrySyncAsync(Func<VideoPlayerApiClient, Task> sync)
    {
        var client = authSession.Client;
        if (client is null)
        {
            return false;
        }

        try
        {
            await sync(client);
            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ServerSyncService] Synchronisation fehlgeschlagen: {ex.Message}");
            return false;
        }
    }

    private async Task WriteCachedListAsync(string key, object payload)
    {
        await cache.Connection.InsertOrReplaceAsync(new CachedListEntry
        {
            Key = key,
            Json = JsonSerializer.Serialize(payload),
            UpdatedAtUtc = DateTime.UtcNow
        });
    }

    private static string MediaTypeOf(MediaEntry? entry)
    {
        return entry?.GetType().Name.ToLowerInvariant() ?? string.Empty;
    }
}
