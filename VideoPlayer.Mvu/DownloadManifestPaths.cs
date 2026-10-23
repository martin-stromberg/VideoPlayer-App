using VideoPlayer.Storage;
using VideoPlayer.Storage.Entities;

namespace VideoPlayer.Mvu;

/// <summary>
/// Auflösung der Manifest-Ablagepfade gegen den aktuellen Video-Ablageort.
/// Der gespeicherte Verzeichnisanteil von <see cref="DownloadManifestEntry.FilePath"/>
/// kann veralten, wenn die Plattform den Datencontainer verschiebt (iOS legt
/// App-Updates in einem neuen Container-UUID-Pfad ab); der Dateiname
/// (<c>{type}_{mediaId}.mp4</c>) bleibt dagegen beständig.
/// </summary>
internal static class DownloadManifestPaths
{
    /// <summary>
    /// Löst den Ablageort des Eintrags im aktuellen Video-Verzeichnis auf —
    /// der Dateiname des gespeicherten Pfads wird dem aktuellen
    /// <see cref="AppStorageLayout.VideosDirectory"/> zugeordnet.
    /// </summary>
    /// <param name="layout">Aktuelles Ablagelayout.</param>
    /// <param name="entry">Der Manifest-Eintrag.</param>
    /// <returns>Der Pfad der Datei im aktuellen Ablageort.</returns>
    public static string Resolve(AppStorageLayout layout, DownloadManifestEntry entry)
    {
        var fileName = Path.GetFileName(entry.FilePath);
        return !string.IsNullOrEmpty(fileName)
            ? Path.Combine(layout.VideosDirectory, fileName)
            : Path.Combine(layout.VideosDirectory, $"{entry.VideoType}_{entry.VideoId}.mp4");
    }
}
