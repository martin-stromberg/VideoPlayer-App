using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
    private readonly string databasePath;
    private readonly ILogger<BackupDatabase> logger;
    private Lazy<SQLiteAsyncConnection> connection;

    /// <summary>Erstellt die Datenbank; die Verbindung wird erst bei Bedarf geöffnet.</summary>
    /// <param name="layout">Ablagelayout mit dem Datenbankpfad.</param>
    /// <param name="logger">Optionaler Logger für das Programmprotokoll.</param>
    public BackupDatabase(AppStorageLayout layout, ILogger<BackupDatabase>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(layout);
        this.logger = logger ?? NullLogger<BackupDatabase>.Instance;
        databasePath = layout.DataDatabasePath;
        connection = CreateConnection(databasePath);
    }

    /// <summary>Asynchrone Verbindung (Lazy — Dateizugriff erst bei Bedarf).</summary>
    public SQLiteAsyncConnection Connection => connection.Value;

    /// <summary>
    /// Legt alle Tabellen an (idempotent) und zieht nachträglich
    /// hinzugefügte Spalten in bereits bestehenden Datenbanken nach
    /// (<c>sqlite-net</c> erweitert vorhandene Tabellen nicht automatisch).
    /// </summary>
    /// <returns>Eine abgeschlossene Aufgabe nach der Initialisierung.</returns>
    public async Task InitializeAsync()
    {
        await Connection.CreateTableAsync<KnownServerEntry>();
        await Connection.CreateTableAsync<BackupStateEntry>();
        await Connection.CreateTableAsync<PendingProgressSyncEntry>();
        await EnsureColumnAsync("pending_progress", "DurationTicks", "bigint NOT NULL DEFAULT 0");
        await EnsureColumnAsync("pending_progress", "PlaylistId", "bigint");
    }

    /// <summary>
    /// Initialisiert die Datenbank und stellt sie bei beschädigter Datei
    /// wieder her: die alte Datei wird als <c>*.corrupt-{Zeitstempel}</c>
    /// beiseitegelegt und die Datenbank neu angelegt.
    /// </summary>
    /// <returns>
    /// <c>true</c>, wenn eine Wiederherstellung stattgefunden hat (Datei
    /// wurde beiseitegelegt und neu angelegt); <c>false</c> bei einer
    /// regulären Initialisierung.
    /// </returns>
    /// <exception cref="Exception">Die Datenbank ist auch nach dem Neu-Anlegen nicht initialisierbar.</exception>
    public async Task<bool> InitializeWithRecoveryAsync()
    {
        try
        {
            await InitializeAsync();
            return false;
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Initialisierung der Backup-Datenbank fehlgeschlagen — Datei wird neu angelegt.");
        }

        // Die Lazy-Verbindung cached ihre Ausnahme — für den Neuversuch
        // wird sie zurückgesetzt; der Connection-Pool gibt ggf. noch
        // geöffnete Dateihandles frei, bevor die beschädigte Datei
        // weggelegt wird.
        connection = CreateConnection(databasePath);
        SQLiteAsyncConnection.ResetPool();
        var corruptPath = $"{databasePath}.corrupt-{DateTime.UtcNow:yyyyMMddHHmmss}";
        if (File.Exists(databasePath))
        {
            File.Move(databasePath, corruptPath);
        }

        await InitializeAsync();
        return true;
    }

    /// <summary>Ergänzt eine fehlende Spalte in einer bestehenden Tabelle.</summary>
    /// <param name="table">Name der Tabelle.</param>
    /// <param name="column">Name der zu prüfenden Spalte.</param>
    /// <param name="columnDefinition">SQL-Definition der Spalte für das <c>ALTER TABLE</c>.</param>
    /// <returns>Eine abgeschlossene Aufgabe nach der Prüfung.</returns>
    private async Task EnsureColumnAsync(string table, string column, string columnDefinition)
    {
        var columns = await Connection.QueryAsync<TableInfoRow>($"PRAGMA table_info({table})");
        if (columns.Any(c => string.Equals(c.Name, column, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        await Connection.ExecuteAsync($"ALTER TABLE {table} ADD COLUMN {column} {columnDefinition}");
    }

    /// <summary>Erzeugt die Lazy-Verbindung für einen Datenbankpfad.</summary>
    /// <param name="path">Pfad der Datenbankdatei.</param>
    /// <returns>Der Lazy-Verbindungs-Factory.</returns>
    private static Lazy<SQLiteAsyncConnection> CreateConnection(string path)
    {
        return new Lazy<SQLiteAsyncConnection>(() =>
        {
            SqliteBootstrap.EnsureInitialized();
            return new SQLiteAsyncConnection(path);
        });
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
