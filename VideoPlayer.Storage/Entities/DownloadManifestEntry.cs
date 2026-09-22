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

    /// <summary>Größe der Datei in Bytes.</summary>
    public long SizeBytes { get; set; }

    /// <summary>Status des Downloads (z. B. läuft, fertig, fehlgeschlagen).</summary>
    public string State { get; set; } = string.Empty;

    /// <summary>Zeitpunkt der letzten Aktualisierung (UTC).</summary>
    public DateTime UpdatedAtUtc { get; set; }
}
