using SQLite;
using VideoPlayer.Storage.Entities;

namespace VideoPlayer.Storage;

/// <summary>
/// Backup-pflichtige lokale Datenbank (<c>videoplayer.db3</c> im
/// <see cref="AppStorageLayout.BackupDirectory"/>). Enthält Serverliste,
/// zuletzt verbundenen Server, Profildaten ohne Geheimnisse und
/// unsynchronisierte Fortschritte.
/// </summary>
public sealed class BackupDatabase
{
    private readonly Lazy<SQLiteAsyncConnection> connection;

    public BackupDatabase(AppStorageLayout layout)
    {
        ArgumentNullException.ThrowIfNull(layout);
        connection = new Lazy<SQLiteAsyncConnection>(() =>
        {
            SqliteBootstrap.EnsureInitialized();
            return new SQLiteAsyncConnection(layout.DataDatabasePath);
        });
    }

    /// <summary>Asynchrone Verbindung (Lazy — Dateizugriff erst bei Bedarf).</summary>
    public SQLiteAsyncConnection Connection => connection.Value;

    /// <summary>Legt alle Tabellen an (idempotent).</summary>
    public async Task InitializeAsync()
    {
        await Connection.CreateTableAsync<KnownServerEntry>();
        await Connection.CreateTableAsync<BackupStateEntry>();
        await Connection.CreateTableAsync<PendingProgressSyncEntry>();
    }
}
