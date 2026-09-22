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

    /// <summary>Erstellt die Datenbank; die Verbindung wird erst bei Bedarf geöffnet.</summary>
    /// <param name="layout">Ablagelayout mit dem Datenbankpfad.</param>
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

    /// <summary>
    /// Legt alle Tabellen an (idempotent) und zieht nachträglich
    /// hinzugefügte Spalten in bereits bestehenden Datenbanken nach
    /// (<c>sqlite-net</c> erweitert vorhandene Tabellen nicht automatisch).
    /// </summary>
    public async Task InitializeAsync()
    {
        await Connection.CreateTableAsync<KnownServerEntry>();
        await Connection.CreateTableAsync<BackupStateEntry>();
        await Connection.CreateTableAsync<PendingProgressSyncEntry>();
        await EnsureColumnAsync("pending_progress", "DurationTicks", "bigint NOT NULL DEFAULT 0");
    }

    /// <summary>Ergänzt eine fehlende Spalte in einer bestehenden Tabelle.</summary>
    /// <param name="table">Name der Tabelle.</param>
    /// <param name="column">Name der zu prüfenden Spalte.</param>
    /// <param name="columnDefinition">SQL-Definition der Spalte für das <c>ALTER TABLE</c>.</param>
    private async Task EnsureColumnAsync(string table, string column, string columnDefinition)
    {
        var columns = await Connection.QueryAsync<TableInfoRow>($"PRAGMA table_info({table})");
        if (columns.Any(c => string.Equals(c.Name, column, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        await Connection.ExecuteAsync($"ALTER TABLE {table} ADD COLUMN {column} {columnDefinition}");
    }

    /// <summary>Zeile der <c>PRAGMA table_info</c>-Ausgabe (Spaltenname).</summary>
    private sealed class TableInfoRow
    {
        // ReSharper disable once InconsistentNaming — Spaltenname des PRAGMA-Ergebnisses
        public string name { get; set; } = string.Empty;

        /// <summary>Spaltenname der Ergebniszeile.</summary>
        public string Name => name;
    }
}
