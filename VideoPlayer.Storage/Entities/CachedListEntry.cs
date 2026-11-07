using SQLite;

namespace VideoPlayer.Storage.Entities;

/// <summary>
/// Gecachte Listeninhalte (nicht backup-pflichtig): Startseiten-Metadaten und
/// Episodenlisten — jederzeit vom Server neu ladbar.
/// </summary>
[Table("cached_lists")]
public sealed class CachedListEntry
{
    /// <summary>Schlüssel des Cache-Eintrags (Primärschlüssel).</summary>
    [PrimaryKey]
    public string Key { get; set; } = string.Empty;

    /// <summary>Serialisierte Listendaten (JSON).</summary>
    public string Json { get; set; } = string.Empty;

    /// <summary>Zeitpunkt der letzten Aktualisierung (UTC).</summary>
    public DateTime UpdatedAtUtc { get; set; }
}
