using VideoPlayer.Storage;
using VideoPlayer.Storage.Entities;

namespace VideoPlayer.Maui.Tests.Stubs;

/// <summary>
/// Gemeinsame Manifest-Hilfen der Download-Tests: das Lesen eines
/// Manifest-Eintrags und das Anlegen eines abgeschlossenen Film-Eintrags
/// samt lokaler Datei — zuvor nahezu identisch in den
/// Download-Testklassen dupliziert.
/// </summary>
internal static class ManifestTestHelpers
{
    /// <summary>Liest den Manifest-Eintrag eines Mediums.</summary>
    /// <param name="cache">Die Cache-Ablage mit dem Download-Manifest.</param>
    /// <param name="type">Der Medientyp des Eintrags.</param>
    /// <param name="mediaId">Die Medien-Id des Eintrags.</param>
    /// <returns>Den gefundenen Eintrag; <c>null</c>, wenn keiner existiert.</returns>
    public static async Task<DownloadManifestEntry?> ReadEntryAsync(
        CacheDatabase cache, string type, long mediaId)
    {
        var rows = await cache.Connection.Table<DownloadManifestEntry>()
            .Where(e => e.VideoId == (int)mediaId)
            .ToListAsync();
        return rows.FirstOrDefault(e => e.VideoType == type);
    }

    /// <summary>
    /// Legt einen abgeschlossenen Manifest-Eintrag eines Films samt
    /// lokaler Datei (64 Bytes) im Video-Verzeichnis an.
    /// </summary>
    /// <param name="cache">Die Cache-Ablage mit dem Download-Manifest.</param>
    /// <param name="layout">Das Ablagelayout mit dem Video-Verzeichnis.</param>
    /// <param name="mediaId">Die Medien-Id des Eintrags.</param>
    /// <param name="expiresAtUtc">Optionales Ablaufdatum; <c>null</c> = ohne Ablaufdatum.</param>
    /// <param name="origin">Die persistierte Herkunft; <c>null</c> = Altbestand ohne Herkunftsangabe.</param>
    /// <returns>Den eingefügten Manifest-Eintrag.</returns>
    public static async Task<DownloadManifestEntry> InsertCompletedAsync(
        CacheDatabase cache,
        AppStorageLayout layout,
        long mediaId,
        DateTime? expiresAtUtc = null,
        string? origin = null)
    {
        var path = Path.Combine(layout.VideosDirectory, $"movie_{mediaId}.mp4");
        await File.WriteAllBytesAsync(path, new byte[64]);
        var entry = new DownloadManifestEntry
        {
            VideoId = (int)mediaId,
            VideoType = "movie",
            Title = "Ein Film",
            FilePath = path,
            State = "completed",
            SizeBytes = 64,
            Origin = origin,
            ExpiresAtUtc = expiresAtUtc,
            UpdatedAtUtc = DateTime.UtcNow
        };
        await cache.Connection.InsertAsync(entry);
        return entry;
    }
}
