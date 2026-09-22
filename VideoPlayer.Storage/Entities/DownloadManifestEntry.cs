using SQLite;

namespace VideoPlayer.Storage.Entities;

/// <summary>
/// Download-Manifest (nicht backup-pflichtig): Zuordnung heruntergeladener
/// Videos zu lokalen Dateien — nach einer Wiederherstellung ohnehin neu
/// aufzubauen.
/// </summary>
[Table("download_manifest")]
public sealed class DownloadManifestEntry
{
    /// <summary>ID des Manifesteintrags (Primärschlüssel, automatisch).</summary>
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>ID des heruntergeladenen Videos (indiziert).</summary>
    [Indexed]
    public int VideoId { get; set; }

    /// <summary>Medientyp des Videos (z. B. <c>movie</c>, <c>episode</c>).</summary>
    public string VideoType { get; set; } = string.Empty;

    /// <summary>Pfad der lokalen Videodatei.</summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>Größe der Datei in Bytes (bei abgeschlossenen Downloads).</summary>
    public long SizeBytes { get; set; }

    /// <summary>Anzeigetitel des Downloads (für die Downloads-Seite ohne Server-Roundtrip).</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Bereits übertragene Bytes des laufenden/letzten Laufs.</summary>
    public long DownloadedBytes { get; set; }

    /// <summary>Erwartete Gesamtgröße in Bytes (<c>Content-Length</c>); 0 = unbekannt.</summary>
    public long TotalBytes { get; set; }

    /// <summary>
    /// Maschinenlesbarer Fehlercode des letzten Fehlschlags
    /// (z. B. <c>quota_exceeded</c>, <c>network</c>, <c>http_404</c>);
    /// leer/<c>null</c> = kein Fehler.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Status des Downloads (<c>queued</c>, <c>downloading</c>,
    /// <c>completed</c>, <c>failed</c>, <c>cancelled</c>).
    /// </summary>
    public string State { get; set; } = string.Empty;

    /// <summary>Zeitpunkt der letzten Aktualisierung (UTC).</summary>
    public DateTime UpdatedAtUtc { get; set; }
}
