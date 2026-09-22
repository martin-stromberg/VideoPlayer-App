using SQLite;

namespace VideoPlayer.Storage.Entities;

/// <summary>
/// Unsynchronisierter Wiedergabefortschritt (backup-pflichtig: noch nicht zum
/// Server übermittelte Fortschritte müssen eine Wiederherstellung überstehen).
/// </summary>
[Table("pending_progress")]
public sealed class PendingProgressSyncEntry
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int VideoId { get; set; }

    public string VideoType { get; set; } = string.Empty;

    public long PositionTicks { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
