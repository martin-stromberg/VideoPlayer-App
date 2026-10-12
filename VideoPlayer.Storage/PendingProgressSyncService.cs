using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
    private readonly ILogger<PendingProgressSyncService> logger;

    /// <summary>Erstellt den Dienst.</summary>
    /// <param name="database">Backup-pflichtige Datenbank mit der Tabelle <c>pending_progress</c>.</param>
    /// <param name="logger">Optionaler Logger für das Programmprotokoll.</param>
    public PendingProgressSyncService(
        BackupDatabase database,
        ILogger<PendingProgressSyncService>? logger = null)
    {
        this.database = database ?? throw new ArgumentNullException(nameof(database));
        this.logger = logger ?? NullLogger<PendingProgressSyncService>.Instance;
    }

    /// <summary>
    /// Reiht eine nicht übermittelte Fortschrittsmeldung ein
    /// (offline erfasst oder beim Senden gescheitert). Pro Medium
    /// (<c>VideoType</c>, <c>VideoId</c>) wird nur der jeweils neueste
    /// Stand vorgehalten — ältere Stände desselben Mediums sind überholt.
    /// </summary>
    /// <param name="videoType">Medientyp des Videos (z. B. <c>movie</c>, <c>episode</c>).</param>
    /// <param name="videoId">ID des Videos.</param>
    /// <param name="position">Wiedergabeposition.</param>
    /// <param name="duration">Gesamtdauer des Mediums.</param>
    public async Task EnqueueAsync(
        string videoType,
        int videoId,
        TimeSpan position,
        TimeSpan duration)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(videoType);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(videoId);

        var replaced = 0;
        await database.Connection.RunInTransactionAsync(connection =>
        {
            replaced = connection.Execute(
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

        // Nur der Zustandswechsel wird protokolliert: das Ersetzen des
        // bestehenden Eintrags desselben Mediums im Fortschrittstakt
        // (alle ~5 s) erzeugt keinen weiteren Eintrag.
        if (replaced == 0)
        {
            logger.LogInformation(
                "Fortschrittsmeldung für {VideoType}/{VideoId} neu eingereiht.",
                videoType,
                videoId);
        }
    }

    /// <summary>
    /// Liefert die ausstehenden Meldungen — eine pro Medium, geordnet nach
    /// Erfassungszeitpunkt (älteste zuerst).
    /// </summary>
    /// <returns>Liste der ausstehenden Meldungen (ggf. leer).</returns>
    public async Task<IReadOnlyList<PendingProgressSyncEntry>> GetPendingAsync()
    {
        var entries = await database.Connection
            .Table<PendingProgressSyncEntry>()
            .ToListAsync();

        // Kein Lese-Eintrag: die Anzahl steht bereits im aggregierten
        // Sync-/Flush-Ergebnis des Aufrufers.
        return entries.OrderBy(e => e.UpdatedAtUtc).ToList();
    }

    /// <summary>Anzahl der wartenden Einträge (ein Eintrag pro Medium).</summary>
    /// <returns>Anzahl der ausstehenden Meldungen.</returns>
    public async Task<int> CountAsync()
    {
        return await database.Connection.Table<PendingProgressSyncEntry>().CountAsync();
    }

    /// <summary>
    /// Entfernt alle wartenden Einträge eines Mediums — nach erfolgreichem
    /// Nachsenden der neuesten Meldung sind ältere Stände überholt.
    /// </summary>
    /// <param name="videoType">Medientyp des Videos.</param>
    /// <param name="videoId">ID des Videos.</param>
    public async Task RemoveForMediaAsync(string videoType, int videoId)
    {
        // Kein Eintrag pro entferntem Medium: der Flush-Lauf des
        // Aufrufers protokolliert die nachgesendete Gesamtzahl.
        await database.Connection
            .Table<PendingProgressSyncEntry>()
            .DeleteAsync(e => e.VideoType == videoType && e.VideoId == videoId);
    }
}
