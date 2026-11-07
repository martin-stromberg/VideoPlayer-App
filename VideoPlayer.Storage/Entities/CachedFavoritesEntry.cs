using SQLite;

namespace VideoPlayer.Storage.Entities;

/// <summary>
/// Lokale Kopie der serverseitig geführten Favoriten (nicht backup-pflichtig —
/// der Server ist die führende Quelle).
/// </summary>
[Table("cached_favorites")]
public sealed class CachedFavoritesEntry
{
    /// <summary>Schlüssel des Cache-Eintrags (Primärschlüssel).</summary>
    [PrimaryKey]
    public string Key { get; set; } = string.Empty;

    /// <summary>ID des favorisierten Videos.</summary>
    public int VideoId { get; set; }

    /// <summary>Medientyp des Videos (z. B. <c>movie</c>, <c>episode</c>).</summary>
    public string VideoType { get; set; } = string.Empty;

    /// <summary>Serialisierte Favoritendaten (JSON).</summary>
    public string Json { get; set; } = string.Empty;

    /// <summary>Zeitpunkt der letzten Aktualisierung (UTC).</summary>
    public DateTime UpdatedAtUtc { get; set; }
}
