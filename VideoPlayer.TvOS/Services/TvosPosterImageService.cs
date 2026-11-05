using Foundation;
using Microsoft.Extensions.Logging;
using UIKit;
using VideoPlayer.ApiClient;
using VideoPlayer.Mvu;
using VideoPlayer.Storage;

namespace VideoPlayer.TvOS.Services
{
    /// <summary>
    /// Lädt Vorschaubilder (Poster) und Quellen-Icons vom Server —
    /// tvOS-Pendant zum <c>PosterImageService</c> der MAUI-App mit
    /// derselben Cache-Konvention: Arbeitsspeicher, dann Datei im nicht
    /// backup-pflichtigen <see cref="AppStorageLayout.PreviewsDirectory"/>,
    /// dann Abruf über den angemeldeten API-Client samt Ablage. Fehlende
    /// oder nicht ladbare Bilder liefern <c>null</c> — die Karte zeigt
    /// dann den Titel als Fallback (fehlende Vorschaubilder sind kein
    /// Fehlerzustand). Poster (<c>api/pictures/{id}</c>) und Quellen-Icons
    /// (<c>api/sourceicons/{id}</c>) nutzen getrennte Schlüsselräume, da
    /// ihre Bild-Ids nicht disjunkt sind.
    /// </summary>
    public sealed class TvosPosterImageService
    {
        private readonly AuthSessionService authSession;
        private readonly AppStorageLayout layout;
        private readonly ILogger<TvosPosterImageService> logger;
        private readonly System.Collections.Concurrent.ConcurrentDictionary<long, UIImage> memoryCache = new();

        // Eigenständiger Schlüsselraum der Quellen-Icons — die Bild-Ids
        // der Icon- und Poster-Endpunkte sind nicht disjunkt.
        private readonly System.Collections.Concurrent.ConcurrentDictionary<long, UIImage> iconCache = new();

        // Eigenständiger Schlüsselraum der Playlist-Cover — der
        // Cover-Endpunkt wird über die Playlist-Id adressiert, nicht
        // über eine Bild-Id.
        private readonly System.Collections.Concurrent.ConcurrentDictionary<long, UIImage> coverCache = new();

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
        public Task<UIImage?> GetImageAsync(long pictureId, CancellationToken cancellationToken = default) =>
            LoadImageAsync(
                pictureId,
                $"{pictureId}.poster",
                memoryCache,
                (client, ct) => client.GetPictureAsync(pictureId, ct),
                cancellationToken);

        /// <summary>
        /// Liefert das Icon einer Medienquelle zu einer Bild-Id —
        /// gleiche Cache-Konvention wie <see cref="GetImageAsync"/>, aber
        /// über den Server-Endpunkt <c>api/sourceicons/{id}</c> und mit
        /// eigenem Datei-Suffix <c>.sourceicon</c> sowie eigenem
        /// Speicher-Cache. <c>null</c>, wenn kein Icon verfügbar ist.
        /// </summary>
        /// <param name="pictureId">Server-Bild-Id des Quellen-Icons (<c>api/sourceicons/{id}</c>).</param>
        /// <param name="cancellationToken">Bricht den Bildabruf ab.</param>
        /// <returns>Das Icon oder <c>null</c>, wenn keines verfügbar ist.</returns>
        public Task<UIImage?> GetSourceIconAsync(long pictureId, CancellationToken cancellationToken = default) =>
            LoadImageAsync(
                pictureId,
                $"{pictureId}.sourceicon",
                iconCache,
                (client, ct) => client.GetSourceIconAsync(pictureId, ct),
                cancellationToken);

        /// <summary>
        /// Liefert das Cover einer Playlist zu ihrer Playlist-Id —
        /// gleiche Cache-Konvention wie <see cref="GetImageAsync"/>, aber
        /// über den Server-Endpunkt <c>api/playlists/{id}/cover</c>
        /// (schlüsselt auf die Playlist-Id, nicht auf eine Bild-Id) und
        /// mit eigenem Datei-Suffix <c>.playlistcover</c> sowie eigenem
        /// Speicher-Cache. <c>null</c>, wenn kein Cover verfügbar ist
        /// (die Playlist hat kein Cover — HTTP 404 — oder der Abruf
        /// scheiterte).
        /// </summary>
        /// <param name="playlistId">Id der Playlist, deren Cover angezeigt wird.</param>
        /// <param name="cancellationToken">Bricht den Bildabruf ab.</param>
        /// <returns>Das Cover oder <c>null</c>, wenn keines verfügbar ist.</returns>
        public Task<UIImage?> GetPlaylistCoverAsync(long playlistId, CancellationToken cancellationToken = default) =>
            LoadImageAsync(
                playlistId,
                $"{playlistId}.playlistcover",
                coverCache,
                async (client, ct) =>
                    await client.GetPlaylistCoverAsync(playlistId, ct) ?? [],
                cancellationToken);

        // Gemeinsame Cache-Kaskade beider Bildarten: Arbeitsspeicher →
        // Datei im Vorschaubild-Verzeichnis → Serverabruf samt Ablage.
        private async Task<UIImage?> LoadImageAsync(
            long pictureId,
            string fileName,
            System.Collections.Concurrent.ConcurrentDictionary<long, UIImage> cache,
            Func<VideoPlayerApiClient, CancellationToken, Task<byte[]>> fetch,
            CancellationToken cancellationToken)
        {
            if (pictureId <= 0)
            {
                return null;
            }

            if (cache.TryGetValue(pictureId, out var cached))
            {
                return cached;
            }

            var path = Path.Combine(layout.PreviewsDirectory, fileName);
            try
            {
                if (File.Exists(path))
                {
                    var data = NSData.FromFile(path);
                    var fileImage = data is null ? null : UIImage.LoadFromData(data);
                    if (fileImage is not null)
                    {
                        cache[pictureId] = fileImage;
                    }

                    return fileImage;
                }

                var client = authSession.Client;
                if (client is null)
                {
                    return null;
                }

                var bytes = await fetch(client, cancellationToken);
                if (bytes.Length == 0)
                {
                    return null;
                }

                await File.WriteAllBytesAsync(path, bytes, cancellationToken);
                var image = UIImage.LoadFromData(NSData.FromArray(bytes));
                if (image is not null)
                {
                    cache[pictureId] = image;
                }

                return image;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Fehlende Bilder sind kein Fehlerzustand der Seite —
                // die Karte zeigt den Titel als Fallback.
                logger.LogWarning(ex, "Bild {PictureId} nicht ladbar.", pictureId);
                return null;
            }
        }
    }
}
