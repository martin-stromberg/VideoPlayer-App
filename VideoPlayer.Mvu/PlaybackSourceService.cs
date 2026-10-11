using System.Text.Json;
using VideoPlayer.ApiClient;
using VideoPlayer.ApiClient.Models;
using VideoPlayer.Storage;
using VideoPlayer.Storage.Entities;

namespace VideoPlayer.Mvu;

/// <summary>
/// Löst die Wiedergabequelle eines Mediums auf (Service Layer): eine
/// fertig heruntergeladene lokale Datei aus dem Download-Manifest
/// (<c>download_manifest</c>) hat Vorrang — Grundlage der
/// Offline-Wiedergabe, die Schritt 11 um die Downloads selbst erweitert;
/// sonst wird der Server-Stream <c>api/items/{typ}/{id}/stream</c> mit
/// dem Anmelde-Token als <c>access_token</c>-Parameter gebildet (Muster
/// des Altbestands <c>DownloadManager.BuildStreamUrl</c>). Die
/// Fortsetzungsposition stammt aus der Weiterschauen-Liste — online vom
/// Server, sonst aus der Cache-Kopie <c>cached_lists</c>.
/// </summary>
public sealed class PlaybackSourceService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly AuthSessionService authSession;
    private readonly AppSecretsService secrets;
    private readonly CacheDatabase cache;
    private readonly Func<string, bool> fileExists;

    /// <summary>Erstellt den Dienst mit der produktiven Dateiprüfung.</summary>
    /// <param name="authSession">Sitzungsdienst mit dem angemeldeten API-Client und der Server-Basisadresse.</param>
    /// <param name="secrets">Ablage des Anmelde-Tokens (Fallback, wenn kein Client angemeldet ist).</param>
    /// <param name="cache">Cache-Ablage mit Download-Manifest und gecachten Listen.</param>
    public PlaybackSourceService(
        AuthSessionService authSession,
        AppSecretsService secrets,
        CacheDatabase cache)
        : this(authSession, secrets, cache, null)
    {
    }

    /// <summary>Erstellt den Dienst mit einer injizierbaren Datei-Existenzprüfung (für Tests).</summary>
    /// <param name="authSession">Sitzungsdienst mit dem angemeldeten API-Client und der Server-Basisadresse.</param>
    /// <param name="secrets">Ablage des Anmelde-Tokens.</param>
    /// <param name="cache">Cache-Ablage mit Download-Manifest und gecachten Listen.</param>
    /// <param name="fileExists">Existenzprüfung für lokale Dateien; <c>null</c> = <see cref="File.Exists(string?)"/>.</param>
    internal PlaybackSourceService(
        AuthSessionService authSession,
        AppSecretsService secrets,
        CacheDatabase cache,
        Func<string, bool>? fileExists)
    {
        this.authSession = authSession ?? throw new ArgumentNullException(nameof(authSession));
        this.secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
        this.cache = cache ?? throw new ArgumentNullException(nameof(cache));
        this.fileExists = fileExists ?? File.Exists;
    }

    /// <summary>
    /// Löst die Wiedergabequelle eines Mediums auf. Abspielbar sind nur
    /// <c>movie</c> und <c>tvshowepisode</c>; andere Typen, fehlende
    /// Serververbindung und nicht auffindbare Quellen liefern
    /// <c>null</c> (der Aufrufer zeigt den Fehler bzw. fällt auf die
    /// nächste Episode zurück).
    /// </summary>
    /// <param name="mediaType">Medientyp (wird normalisiert).</param>
    /// <param name="mediaId">Id des Mediums.</param>
    /// <param name="cancellationToken">Bricht die Auflösung ab.</param>
    /// <param name="allowServerStream">
    /// <c>false</c> = Offline-Modus: nur lokale Dateien werden aufgelöst —
    /// ohne lokale Datei wird <c>null</c> geliefert statt eine
    /// unerreichbare Stream-URL zu erzeugen; die Fortsetzungsposition
    /// kommt direkt aus der Cache-Kopie.
    /// </param>
    /// <param name="resumePositionSecondsOverride">
    /// Vom Aufrufer bereits bekannte Fortsetzungsposition in Sekunden
    /// (z. B. aus der Playlist-Start-/Navigationsantwort des Servers) —
    /// überschreibt die Weiterschauen-Auflösung verbindlich, auch bei
    /// <c>0</c> (Wiedergabe beginnt am Anfang); <c>null</c> = bisherige
    /// Weiterschauen-Auflösung.
    /// </param>
    /// <returns>Die aufgelöste Quelle; <c>null</c>, wenn keine Quelle verfügbar ist.</returns>
    public async Task<PlaybackSource?> ResolveAsync(
        string mediaType,
        long mediaId,
        CancellationToken cancellationToken = default,
        bool allowServerStream = true,
        long? resumePositionSecondsOverride = null)
    {
        var type = MediaTypeNames.Normalize(mediaType);
        if (mediaId <= 0 || type is not ("movie" or "tvshowepisode"))
        {
            return null;
        }

        var resumePosition = resumePositionSecondsOverride is { } overrideSeconds
            ? TimeSpan.FromSeconds(overrideSeconds)
            : allowServerStream
                ? await ResolveResumePositionAsync(type, mediaId, cancellationToken)
                : await ReadCachedResumePositionAsync(type, mediaId);

        var localFile = await ResolveLocalFileAsync(type, mediaId);
        if (localFile is not null)
        {
            return new PlaybackSource
            {
                Kind = PlaybackSourceKind.LocalFile,
                Location = localFile,
                ResumePosition = resumePosition
            };
        }

        if (!allowServerStream)
        {
            // Offline: keine Stream-URL für einen unerreichbaren Server.
            return null;
        }

        var baseAddress = authSession.ClientBaseAddress?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseAddress))
        {
            return null;
        }

        var token = authSession.Client?.CurrentToken ?? await secrets.GetAuthTokenAsync();
        var streamUrl = $"{baseAddress}/api/items/{type}/{mediaId}/stream";
        if (!string.IsNullOrWhiteSpace(token))
        {
            streamUrl += $"?access_token={Uri.EscapeDataString(token)}";
        }

        return new PlaybackSource
        {
            Kind = PlaybackSourceKind.ServerStream,
            Location = streamUrl,
            ResumePosition = resumePosition
        };
    }

    /// <summary>
    /// Fortsetzungsposition aus der Weiterschauen-Liste: online vom
    /// Server, bei Fehlschlag oder ohne Client aus der Cache-Kopie
    /// <c>cached_lists[continue_watching]</c>.
    /// </summary>
    /// <param name="type">Normalisierter Medientyp (CLR-Konvention).</param>
    /// <param name="mediaId">Id des Mediums.</param>
    /// <param name="cancellationToken">Bricht den Server-Aufruf ab.</param>
    /// <returns>Die Fortsetzungsposition; <c>null</c>, wenn kein Stand bekannt ist.</returns>
    private async Task<TimeSpan?> ResolveResumePositionAsync(
        string type,
        long mediaId,
        CancellationToken cancellationToken)
    {
        var client = authSession.Client;
        if (client is not null)
        {
            try
            {
                var entries = await client.GetContinueWatchingAsync(cancellationToken);
                var position = FindResumePosition(entries, type, mediaId);
                if (position is not null)
                {
                    return position;
                }
            }
            catch (Exception ex) when (ex is ApiException or HttpRequestException or TaskCanceledException)
            {
                // Server nicht erreichbar/antwortet nicht → Cache-Stand.
                System.Diagnostics.Debug.WriteLine($"[PlaybackSourceService] Weiterschauen-Liste nicht ladbar: {ex.Message}");
            }
        }

        return await ReadCachedResumePositionAsync(type, mediaId);
    }

    private async Task<TimeSpan?> ReadCachedResumePositionAsync(string type, long mediaId)
    {
        try
        {
            var entry = await cache.Connection.Table<CachedListEntry>()
                .Where(e => e.Key == ServerSyncService.ContinueWatchingCacheKey)
                .FirstOrDefaultAsync();
            if (entry?.Json is not { Length: > 0 } json)
            {
                return null;
            }

            var entries = JsonSerializer.Deserialize<List<ContinueWatchingEntry>>(json, JsonOptions);
            return entries is null ? null : FindResumePosition(entries, type, mediaId);
        }
        catch (Exception ex)
        {
            // Beschädigter Cache → kein Fortsetzungsstand statt Fehler.
            System.Diagnostics.Debug.WriteLine($"[PlaybackSourceService] Weiterschauen-Cache nicht lesbar: {ex.Message}");
            return null;
        }
    }

    private static TimeSpan? FindResumePosition(
        IEnumerable<ContinueWatchingEntry> entries,
        string type,
        long mediaId)
    {
        var entry = entries.FirstOrDefault(e =>
            MediaTypeNames.Normalize(e.MediaType) == type && e.Entry?.Id == mediaId);
        return entry is { PositionSeconds: > 0 }
            ? TimeSpan.FromSeconds(entry.PositionSeconds)
            : null;
    }

    /// <summary>
    /// Lokale Datei aus dem Download-Manifest: ein abgeschlossener
    /// Download desselben Mediums mit noch vorhandener Datei.
    /// </summary>
    /// <param name="type">Normalisierter Medientyp (CLR-Konvention).</param>
    /// <param name="mediaId">Id des Mediums.</param>
    /// <returns>Der Dateipfad; <c>null</c>, wenn kein fertiger Download vorliegt.</returns>
    private async Task<string?> ResolveLocalFileAsync(string type, long mediaId)
    {
        try
        {
            // VideoId ist in der Tabelle int — Vergleichstyp angleichen,
            // damit sqlite-net den Ausdruck übersetzen kann.
            var id = (int)mediaId;
            var candidates = await cache.Connection.Table<DownloadManifestEntry>()
                .Where(e => e.VideoId == id)
                .ToListAsync();
            var entry = candidates.FirstOrDefault(e =>
                MediaTypeNames.Normalize(e.VideoType) == type
                && string.Equals(e.State, "completed", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(e.FilePath)
                && fileExists(e.FilePath));
            return entry?.FilePath;
        }
        catch (Exception ex)
        {
            // Manifest nicht lesbar → Stream-Versuch statt Fehler.
            System.Diagnostics.Debug.WriteLine($"[PlaybackSourceService] Download-Manifest nicht lesbar: {ex.Message}");
            return null;
        }
    }
}
