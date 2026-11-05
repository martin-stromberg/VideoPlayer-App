using Foundation;
using Microsoft.Extensions.Logging;
using UIKit;
using VideoPlayer.Mvu;
using VideoPlayer.Storage;

namespace VideoPlayer.TvOS.Services
{
    /// <summary>
    /// Lädt Vorschaubilder (Poster) vom Server-Endpunkt
    /// <c>api/pictures/{id}</c> — tvOS-Pendant zum
    /// <c>PosterImageService</c> der MAUI-App mit derselben
    /// Cache-Konvention: Arbeitsspeicher, dann Datei
    /// <c>{pictureId}.poster</c> im nicht backup-pflichtigen
    /// <see cref="AppStorageLayout.PreviewsDirectory"/>, dann Abruf über
    /// den angemeldeten API-Client samt Ablage. Fehlende oder nicht
    /// ladbare Bilder liefern <c>null</c> — die Karte zeigt dann den
    /// Titel als Fallback (fehlende Vorschaubilder sind kein
    /// Fehlerzustand).
    /// </summary>
    public sealed class TvosPosterImageService
    {
        private readonly AuthSessionService authSession;
        private readonly AppStorageLayout layout;
        private readonly ILogger<TvosPosterImageService> logger;
        private readonly System.Collections.Concurrent.ConcurrentDictionary<long, UIImage> memoryCache = new();

        /// <summary>Initialisiert eine neue Instanz der <see cref="TvosPosterImageService"/>-Klasse.</summary>
        /// <param name="authSession">Sitzungsdienst; liefert den angemeldeten API-Client für Bildabrufe.</param>
        /// <param name="layout">Ablage-Layout mit dem nicht backup-pflichtigen Vorschaubild-Verzeichnis.</param>
        /// <param name="logger">Logger für das Programmprotokoll (nur Warn-/Fehlerpfade).</param>
        public TvosPosterImageService(
            AuthSessionService authSession,
            AppStorageLayout layout,
            ILogger<TvosPosterImageService> logger)
        {
            this.authSession = authSession ?? throw new ArgumentNullException(nameof(authSession));
            this.layout = layout ?? throw new ArgumentNullException(nameof(layout));
            this.logger = logger;
        }

        /// <summary>
        /// Liefert das Poster zu einer Bild-Id — aus dem Arbeitsspeicher,
        /// der lokalen Ablage oder nach Abruf vom Server. <c>null</c>,
        /// wenn kein Bild verfügbar ist (offline, fehlende Verbindung,
        /// Serverfehler) — die Karte zeigt dann den Titel.
        /// </summary>
        /// <param name="pictureId">Server-Bild-Id des Posters (<c>api/pictures/{id}</c>).</param>
        /// <param name="cancellationToken">Bricht den Bildabruf ab.</param>
        /// <returns>Das Bild oder <c>null</c>, wenn kein Bild verfügbar ist.</returns>
        public async Task<UIImage?> GetImageAsync(long pictureId, CancellationToken cancellationToken = default)
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
                    var data = NSData.FromFile(path);
                    var fileImage = data is null ? null : UIImage.LoadFromData(data);
                    if (fileImage is not null)
                    {
                        memoryCache[pictureId] = fileImage;
                    }

                    return fileImage;
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
                var image = UIImage.LoadFromData(NSData.FromArray(bytes));
                if (image is not null)
                {
                    memoryCache[pictureId] = image;
                }

                return image;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Fehlende Vorschaubilder sind kein Fehlerzustand der Seite
                // — die Karte zeigt den Titel als Fallback.
                logger.LogWarning(ex, "Bild {PictureId} nicht ladbar.", pictureId);
                return null;
            }
        }
    }
}
