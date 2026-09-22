using SQLite;

namespace VideoPlayer.Storage.Entities;

/// <summary>
/// Unsynchronisierter Wiedergabefortschritt (backup-pflichtig: noch nicht zum
/// Server übermittelte Fortschritte müssen eine Wiederherstellung überstehen).
/// </summary>
[Table("pending_progress")]
public sealed class PendingProgressSyncEntry
{
    /// <summary>ID des Eintrags (Primärschlüssel, automatisch).</summary>
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>ID des betroffenen Videos (indiziert).</summary>
    [Indexed]
    public int VideoId { get; set; }

    /// <summary>Medientyp des Videos (z. B. <c>movie</c>, <c>episode</c>).</summary>
    public string VideoType { get; set; } = string.Empty;

    /// <summary>Wiedergabeposition in Ticks.</summary>
    public long PositionTicks { get; set; }

    /// <summary>
    /// Gesamtdauer des Mediums (die Server-API verlangt die Dauer bei jeder
    /// Fortschrittsmeldung). In bestehenden Datenbanken per Migration
    /// nachgezogen (<see cref="BackupDatabase.InitializeAsync"/>).
    /// </summary>
    public long DurationTicks { get; set; }

    /// <summary>Zeitpunkt der letzten Aktualisierung (UTC).</summary>
    public DateTime UpdatedAtUtc { get; set; }
}
