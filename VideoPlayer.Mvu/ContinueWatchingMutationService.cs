using System.Text.Json;
using VideoPlayer.ApiClient.Models;
using VideoPlayer.Storage;
using VideoPlayer.Storage.Entities;

namespace VideoPlayer.Mvu;

/// <summary>
/// Kontextmenü-Mutationen der Weiterschauen-Liste: „Ausblenden" und
/// „Überspringen" gehen online über die Server-Endpunkte
/// <c>api/continue-watching/hide</c> bzw. <c>…/skip</c> (führendes
/// System) und erneuern danach die Cache-Kopie
/// <c>cached_lists</c>/<c>continue_watching</c> über
/// <see cref="ServerSyncService.SyncContinueWatchingAsync"/> — derselbe
/// Pfad wie die ereignisgesteuerte Synchronisation. Ohne angemeldeten
/// Client (offline) wird der Eintrag lokal aus dem Cache-Eintrag
/// entfernt — die Startseite lädt den veränderten Stand über die
/// <c>CachedListsChangedMessage</c>-Kette nach.
/// </summary>
public sealed class ContinueWatchingMutationService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly AuthSessionService authSession;
    private readonly ServerSyncService syncService;
    private readonly CacheDatabase cache;

    /// <summary>Erstellt den Dienst.</summary>
    /// <param name="authSession">Sitzungsdienst mit dem angemeldeten API-Client.</param>
    /// <param name="syncService">Synchronisationsdienst für die Cache-Kopie.</param>
    /// <param name="cache">Lokale Cache-Ablage mit der Weiterschauen-Liste.</param>
    public ContinueWatchingMutationService(
        AuthSessionService authSession,
        ServerSyncService syncService,
        CacheDatabase cache)
    {
        this.authSession = authSession ?? throw new ArgumentNullException(nameof(authSession));
        this.syncService = syncService ?? throw new ArgumentNullException(nameof(syncService));
        this.cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    /// <summary>
    /// Blendet einen Eintrag der Weiterschauen-Liste aus
    /// (<c>POST api/continue-watching/hide</c>) und erneuert die
    /// Cache-Kopie vom Serverstand. Ohne Client wird der Eintrag lokal
    /// aus dem Cache-Eintrag entfernt.
    /// </summary>
    /// <param name="mediaType">Medientyp des Eintrags (Server-Konvention der Liste).</param>
    /// <param name="mediaId">Id des Mediums.</param>
    /// <param name="cancellationToken">Bricht den Aufruf ab.</param>
    /// <returns><c>true</c>, wenn die Liste aktualisiert wurde; <c>false</c>, wenn kein passender Cache-Eintrag entfernt werden konnte.</returns>
    public async Task<bool> HideAsync(
        string mediaType,
        long mediaId,
        CancellationToken cancellationToken = default)
    {
        return await MutateAsync(
            client => client.HideContinueWatchingEntryAsync(mediaType, mediaId, cancellationToken),
            mediaType, mediaId, cancellationToken);
    }

    /// <summary>
    /// Überspringt einen Eintrag der Weiterschauen-Liste
    /// (<c>POST api/continue-watching/skip</c>) und erneuert die
    /// Cache-Kopie vom Serverstand. Ohne Client wird der Eintrag lokal
    /// aus dem Cache-Eintrag entfernt.
    /// </summary>
    /// <param name="mediaType">Medientyp des Eintrags (Server-Konvention der Liste).</param>
    /// <param name="mediaId">Id des Mediums.</param>
    /// <param name="cancellationToken">Bricht den Aufruf ab.</param>
    /// <returns><c>true</c>, wenn die Liste aktualisiert wurde; <c>false</c>, wenn kein passender Cache-Eintrag entfernt werden konnte.</returns>
    public async Task<bool> SkipAsync(
        string mediaType,
        long mediaId,
        CancellationToken cancellationToken = default)
    {
        return await MutateAsync(
            client => client.SkipContinueWatchingEntryAsync(mediaType, mediaId, cancellationToken),
            mediaType, mediaId, cancellationToken);
    }

    /// <summary>
    /// Gemeinsamer Ablauf beider Mutationen: online Server-Aufruf +
    /// autoritative Cache-Erneuerung; ohne Client lokales Entfernen.
    /// </summary>
    /// <param name="serverCall">Der Server-Aufruf mit dem angemeldeten Client.</param>
    /// <param name="mediaType">Medientyp des Eintrags.</param>
    /// <param name="mediaId">Id des Mediums.</param>
    /// <param name="cancellationToken">Bricht den Aufruf ab.</param>
    /// <returns><c>true</c>, wenn die Liste aktualisiert wurde.</returns>
    private async Task<bool> MutateAsync(
        Func<VideoPlayer.ApiClient.VideoPlayerApiClient, Task<ContinueWatchingMutationResult>> serverCall,
        string mediaType,
        long mediaId,
        CancellationToken cancellationToken)
    {
        var client = authSession.Client;
        if (client is null)
        {
            // Offline: der Server ist nicht erreichbar — der Eintrag
            // wird lokal aus der Cache-Kopie entfernt, bis der nächste
            // Sync den Serverstand wieder herstellt.
            return await RemoveCachedEntryAsync(mediaType, mediaId);
        }

        await serverCall(client);

        // Lokale Kopie autoritativ vom Serverstand erneuern — die
        // Server-Antwort entscheidet, ob der Eintrag verschwindet.
        await syncService.SyncContinueWatchingAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Entfernt den passenden Eintrag aus dem
    /// <c>cached_lists</c>-Schlüssel <c>continue_watching</c> und
    /// schreibt die gekürzte Liste zurück (Offline-Pfad).
    /// </summary>
    /// <param name="mediaType">Medientyp des Eintrags (normalisiert verglichen).</param>
    /// <param name="mediaId">Id des Mediums.</param>
    /// <returns><c>true</c>, wenn ein Eintrag entfernt wurde; sonst <c>false</c>.</returns>
    private async Task<bool> RemoveCachedEntryAsync(string mediaType, long mediaId)
    {
        var row = await cache.Connection.Table<CachedListEntry>()
            .Where(e => e.Key == ServerSyncService.ContinueWatchingCacheKey)
            .FirstOrDefaultAsync();
        if (row?.Json is not { Length: > 0 } json)
        {
            return false;
        }

        var entries = JsonSerializer.Deserialize<List<ContinueWatchingEntry>>(json, JsonOptions) ?? [];
        var removed = entries.RemoveAll(e =>
            e.Entry?.Id == mediaId
            && MediaTypeNames.Normalize(e.MediaType) == MediaTypeNames.Normalize(mediaType)) > 0;
        if (!removed)
        {
            return false;
        }

        row.Json = JsonSerializer.Serialize(entries, JsonOptions);
        row.UpdatedAtUtc = DateTime.UtcNow;
        await cache.Connection.UpdateAsync(row);
        return true;
    }
}
