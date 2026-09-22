using SQLite;

namespace VideoPlayer.Storage.Entities;

/// <summary>
/// Lokale Kopie der serverseitig geführten Favoriten (nicht backup-pflichtig —
/// der Server ist die führende Quelle).
/// </summary>
[Table("cached_favorites")]
public sealed class CachedFavoritesEntry
{
    [PrimaryKey]
    public string Key { get; set; } = string.Empty;

    public int VideoId { get; set; }

    public string VideoType { get; set; } = string.Empty;

    public string Json { get; set; } = string.Empty;

    public DateTime UpdatedAtUtc { get; set; }
}
