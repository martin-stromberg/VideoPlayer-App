using VideoPlayer.Storage.Entities;

namespace VideoPlayer.Storage;

/// <summary>
/// Persistente Warteschlange unsynchronisierter Wiedergabefortschritte auf
/// der backup-pflichtigen Tabelle <c>pending_progress</c>. Meldungen
/// überstehen App-Neustarts und Cloud-Backup-Wiederherstellungen; beim
/// Nachsenden wird pro Medium nur die jeweils neueste Meldung übertragen
/// (der Server führt ohnehin nur den letzten Stand).
/// </summary>
public sealed class PendingProgressSyncService
{
    private readonly BackupDatabase database;

    public PendingProgressSyncService(BackupDatabase database)
    {
        this.database = database ?? throw new ArgumentNullException(nameof(database));
    }

    /// <summary>
    /// Reiht eine nicht übermittelte Fortschrittsmeldung ein
    /// (offline erfasst oder beim Senden gescheitert). Pro Medium
    /// (<c>VideoType</c>, <c>VideoId</c>) wird nur der jeweils neueste
    /// Stand vorgehalten — ältere Stände desselben Mediums sind überholt.
    /// </summary>
    public async Task EnqueueAsync(
        string videoType,
        int videoId,
        TimeSpan position,
        TimeSpan duration)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(videoType);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(videoId);

        await database.Connection.RunInTransactionAsync(connection =>
        {
            connection.Execute(
                "DELETE FROM pending_progress WHERE VideoType = ? AND VideoId = ?",
                videoType,
                videoId);
            connection.Insert(new PendingProgressSyncEntry
            {
                VideoType = videoType,
                VideoId = videoId,
                PositionTicks = position.Ticks,
                DurationTicks = duration.Ticks,
                UpdatedAtUtc = DateTime.UtcNow
            });
        });
    }

    /// <summary>
    /// Liefert die ausstehenden Meldungen — eine pro Medium, geordnet nach
    /// Erfassungszeitpunkt (älteste zuerst).
    /// </summary>
    public async Task<IReadOnlyList<PendingProgressSyncEntry>> GetPendingAsync()
    {
        var entries = await database.Connection
            .Table<PendingProgressSyncEntry>()
            .ToListAsync();

        return entries.OrderBy(e => e.UpdatedAtUtc).ToList();
    }

    /// <summary>Anzahl der wartenden Einträge (ein Eintrag pro Medium).</summary>
    public async Task<int> CountAsync()
    {
        return await database.Connection.Table<PendingProgressSyncEntry>().CountAsync();
    }

    /// <summary>
    /// Entfernt alle wartenden Einträge eines Mediums — nach erfolgreichem
    /// Nachsenden der neuesten Meldung sind ältere Stände überholt.
    /// </summary>
    public async Task RemoveForMediaAsync(string videoType, int videoId)
    {
        await database.Connection
            .Table<PendingProgressSyncEntry>()
            .DeleteAsync(e => e.VideoType == videoType && e.VideoId == videoId);
    }
}
