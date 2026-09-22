using SQLite;

namespace VideoPlayer.Storage.Entities;

/// <summary>
/// Gecachte Listeninhalte (nicht backup-pflichtig): Startseiten-Metadaten und
/// Episodenlisten — jederzeit vom Server neu ladbar.
/// </summary>
[Table("cached_lists")]
public sealed class CachedListEntry
{
    [PrimaryKey]
    public string Key { get; set; } = string.Empty;

    public string Json { get; set; } = string.Empty;

    public DateTime UpdatedAtUtc { get; set; }
}
