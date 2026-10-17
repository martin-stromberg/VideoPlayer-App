using Microsoft.Extensions.Logging;
using VideoPlayer.Mvu;
using VideoPlayer.Storage;

namespace VideoPlayer.Maui.Services;

/// <summary>
/// Lädt Vorschaubilder (Poster) vom Server-Endpunkt
/// <c>api/pictures/{id}</c> und legt sie im nicht backup-pflichtigen
/// <see cref="AppStorageLayout.PreviewsDirectory"/> ab — beim erneuten
/// Öffnen kommen die Bilder direkt aus der lokalen Ablage, ohne
/// Serveranfrage. Bilder werden zusätzlich im Arbeitsspeicher gehalten.
/// </summary>
public sealed class PosterImageService
{
    private readonly AuthSessionService authSession;
    private readonly AppStorageLayout layout;
    private readonly ILogger<PosterImageService> logger;
    private readonly System.Collections.Concurrent.ConcurrentDictionary<long, ImageSource> memoryCache = new();
    private readonly System.Collections.Concurrent.ConcurrentDictionary<long, ImageSource> iconMemoryCache = new();
    private readonly System.Collections.Concurrent.ConcurrentDictionary<long, ImageSource> playlistCoverMemoryCache = new();

    /// <summary>Initialisiert eine neue Instanz der <see cref="PosterImageService"/>-Klasse.</summary>
    /// <param name="authSession">Sitzungsdienst; liefert den angemeldeten API-Client für Bildabrufe.</param>
    /// <param name="layout">Ablage-Layout mit dem nicht backup-pflichtigen Vorschaubild-Verzeichnis.</param>
    /// <param name="logger">Logger für das Programmprotokoll (nur Warn-/Fehlerpfade).</param>
    public PosterImageService(
        AuthSessionService authSession,
        AppStorageLayout layout,
        ILogger<PosterImageService> logger)
    {
        this.authSession = authSession ?? throw new ArgumentNullException(nameof(authSession));
        this.layout = layout ?? throw new ArgumentNullException(nameof(layout));
        this.logger = logger;
    }

    /// <summary>
    /// Liefert die Bildquelle zu einer Poster-Bild-Id — aus dem
    /// Arbeitsspeicher, der lokalen Ablage oder nach Abruf vom Server.
    /// <c>null</c>, wenn kein Bild verfügbar ist (offline, fehlende
    /// Verbindung, Serverfehler) — die Karte zeigt dann den Titel.
    /// </summary>
    /// <param name="pictureId">Server-Bild-Id des Posters (<c>api/pictures/{id}</c>).</param>
    /// <param name="cancellationToken">Bricht den Bildabruf ab.</param>
    /// <returns>Die Bildquelle oder <c>null</c>, wenn kein Bild verfügbar ist.</returns>
    public async Task<ImageSource?> GetImageSourceAsync(long pictureId, CancellationToken cancellationToken = default)
    {
        if (pictureId <= 0)
        {
            return null;
        }

        if (memoryCache.TryGetValue(pictureId, out var cached))
        {
            return cached;
        }

        var path = Path.Combine(layout.PreviewsDirectory, $"{pictureId}.poster");
        try
        {
            if (File.Exists(path))
            {
                var fileSource = ImageSource.FromFile(path);
                memoryCache[pictureId] = fileSource;
                return fileSource;
            }

            var client = authSession.Client;
            if (client is null)
            {
                return null;
            }

            var bytes = await client.GetPictureAsync(pictureId, cancellationToken);
            if (bytes.Length == 0)
            {
                return null;
            }

            await File.WriteAllBytesAsync(path, bytes, cancellationToken);
            var source = ImageSource.FromStream(() => new MemoryStream(bytes));
            memoryCache[pictureId] = source;
            return source;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Fehlende Vorschaubilder sind kein Fehlerzustand der Seite —
            // die Karte zeigt den Titel als Fallback.
            logger.LogWarning(ex, "Bild {PictureId} nicht ladbar.", pictureId);
            return null;
        }
    }

    /// <summary>
    /// Liefert die Bildquelle zu einem Quellen-Icon
    /// (<c>api/sourceicons/{id}</c>) — gleiche Cache-Konvention wie
    /// <see cref="GetImageSourceAsync"/> (Datei <c>{id}.sourceicon</c> im
    /// nicht backup-pflichtigen <see cref="AppStorageLayout.PreviewsDirectory"/>).
    /// <c>null</c>, wenn kein Icon verfügbar ist.
    /// </summary>
    /// <param name="pictureId">Server-Bild-Id des Quellen-Icons (<c>api/sourceicons/{id}</c>).</param>
    /// <param name="cancellationToken">Bricht den Icon-Abruf ab.</param>
    /// <returns>Die Bildquelle oder <c>null</c>, wenn kein Icon verfügbar ist.</returns>
    public async Task<ImageSource?> GetSourceIconSourceAsync(long pictureId, CancellationToken cancellationToken = default)
    {
        if (pictureId <= 0)
        {
            return null;
        }

        if (iconMemoryCache.TryGetValue(pictureId, out var cached))
        {
            return cached;
        }

        var path = Path.Combine(layout.PreviewsDirectory, $"{pictureId}.sourceicon");
        try
        {
            if (File.Exists(path))
            {
                var fileSource = ImageSource.FromFile(path);
                iconMemoryCache[pictureId] = fileSource;
                return fileSource;
            }

            var client = authSession.Client;
            if (client is null)
            {
                return null;
            }

            var bytes = await client.GetSourceIconAsync(pictureId, cancellationToken);
            if (bytes.Length == 0)
            {
                return null;
            }

            await File.WriteAllBytesAsync(path, bytes, cancellationToken);
            var source = ImageSource.FromStream(() => new MemoryStream(bytes));
            iconMemoryCache[pictureId] = source;
            return source;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Fehlende Icons sind kein Fehlerzustand — der Quellenname
            // bleibt als Fallback sichtbar.
            logger.LogWarning(ex, "Quellen-Icon {PictureId} nicht ladbar.", pictureId);
            return null;
        }
    }

    /// <summary>
    /// Liefert die Bildquelle zum Cover einer Playlist
    /// (<c>api/playlists/{id}/cover</c>) — gleiche Cache-Konvention wie
    /// <see cref="GetImageSourceAsync"/> (Datei <c>{id}.playlistcover</c>
    /// im nicht backup-pflichtigen <see cref="AppStorageLayout.PreviewsDirectory"/>).
    /// <c>null</c>, wenn die Playlist kein Cover hat (der Server
    /// antwortet 404) oder kein Bild verfügbar ist.
    /// </summary>
    /// <param name="playlistId">Id der Playlist (<c>api/playlists/{id}/cover</c>).</param>
    /// <param name="cancellationToken">Bricht den Bildabruf ab.</param>
    /// <returns>Die Bildquelle oder <c>null</c>, wenn kein Cover verfügbar ist.</returns>
    public async Task<ImageSource?> GetPlaylistCoverSourceAsync(long playlistId, CancellationToken cancellationToken = default)
    {
        if (playlistId <= 0)
        {
            return null;
        }

        if (playlistCoverMemoryCache.TryGetValue(playlistId, out var cached))
        {
            return cached;
        }

        var path = Path.Combine(layout.PreviewsDirectory, $"{playlistId}.playlistcover");
        try
        {
            if (File.Exists(path))
            {
                var fileSource = ImageSource.FromFile(path);
                playlistCoverMemoryCache[playlistId] = fileSource;
                return fileSource;
            }

            var client = authSession.Client;
            if (client is null)
            {
                return null;
            }

            var bytes = await client.GetPlaylistCoverAsync(playlistId, cancellationToken);
            if (bytes is null || bytes.Length == 0)
            {
                // Kein Cover gesetzt (Server antwortet 404) — die Karte
                // zeigt den Titel als Fallback; absichtlich nichts
                // zwischengespeichert, damit ein später gesetztes Cover
                // ohne Neustart erscheint.
                return null;
            }

            await File.WriteAllBytesAsync(path, bytes, cancellationToken);
            var source = ImageSource.FromStream(() => new MemoryStream(bytes));
            playlistCoverMemoryCache[playlistId] = source;
            return source;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Fehlende Cover sind kein Fehlerzustand — der Playlist-Name
            // bleibt als Fallback sichtbar.
            logger.LogWarning(ex, "Playlist-Cover {PlaylistId} nicht ladbar.", playlistId);
            return null;
        }
    }
}
