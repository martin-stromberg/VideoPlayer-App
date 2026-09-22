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
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int VideoId { get; set; }

    public string VideoType { get; set; } = string.Empty;

    public string FilePath { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public string State { get; set; } = string.Empty;

    public DateTime UpdatedAtUtc { get; set; }
}
