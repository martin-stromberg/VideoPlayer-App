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
    private readonly System.Collections.Concurrent.ConcurrentDictionary<long, ImageSource> memoryCache = new();

    public PosterImageService(AuthSessionService authSession, AppStorageLayout layout)
    {
        this.authSession = authSession ?? throw new ArgumentNullException(nameof(authSession));
        this.layout = layout ?? throw new ArgumentNullException(nameof(layout));
    }

    /// <summary>
    /// Liefert die Bildquelle zu einer Poster-Bild-Id — aus dem
    /// Arbeitsspeicher, der lokalen Ablage oder nach Abruf vom Server.
    /// <c>null</c>, wenn kein Bild verfügbar ist (offline, fehlende
    /// Verbindung, Serverfehler) — die Karte zeigt dann den Titel.
    /// </summary>
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
            System.Diagnostics.Debug.WriteLine($"[PosterImageService] Bild {pictureId} nicht ladbar: {ex.Message}");
            return null;
        }
    }
}
