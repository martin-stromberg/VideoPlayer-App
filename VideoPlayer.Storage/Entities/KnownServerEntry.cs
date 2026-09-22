using SQLite;

namespace VideoPlayer.Storage.Entities;

/// <summary>Bekannter VideoPlayer-Server (backup-pflichtig: Serverliste).</summary>
[Table("known_servers")]
public sealed class KnownServerEntry
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed(Unique = true)]
    public string Address { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public DateTime LastUsedAtUtc { get; set; }
}
