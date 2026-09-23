using SQLite;
using VideoPlayer.Storage.Entities;

namespace VideoPlayer.Storage;

/// <summary>
/// Nicht backup-pflichtige lokale Datenbank (<c>videoplayer-cache.db3</c> im
/// <see cref="AppStorageLayout.NoBackupDirectory"/>). Enthält die lokale Kopie
/// der serverseitigen Favoriten, Startseiten-Metadaten, Episodenlisten und das
/// Download-Manifest — alle jederzeit vom Server neu aufbaubar.
/// </summary>
public sealed class CacheDatabase
{
    private readonly string databasePath;
    private Lazy<SQLiteAsyncConnection> connection;

    /// <summary>Erstellt die Datenbank; die Verbindung wird erst bei Bedarf geöffnet.</summary>
    /// <param name="layout">Ablagelayout mit dem Datenbankpfad.</param>
    public CacheDatabase(AppStorageLayout layout)
    {
        ArgumentNullException.ThrowIfNull(layout);
        databasePath = layout.CacheDatabasePath;
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
        await Connection.CreateTableAsync<CachedListEntry>();
        await Connection.CreateTableAsync<CachedFavoritesEntry>();
        await Connection.CreateTableAsync<DownloadManifestEntry>();
        await EnsureColumnAsync("download_manifest", "ShowId", "bigint NULL");
        await EnsureColumnAsync("download_manifest", "ShowTitle", "varchar NULL");
        await EnsureColumnAsync("download_manifest", "SeasonNumber", "integer NULL");
        await EnsureColumnAsync("download_manifest", "EpisodeNumber", "integer NULL");
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
            System.Diagnostics.Debug.WriteLine(
                $"[CacheDatabase] Initialisierung fehlgeschlagen — Datei wird neu angelegt: {ex.Message}");
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

    /// <summary>
    /// Prüft, ob lokale Offline-Daten vorliegen (gecachte Listen/Favoriten
    /// oder Download-Manifesteinträge) — Grundlage für den Offline-Zustand
    /// des Verbindungs-Workflows.
    /// </summary>
    /// <returns><c>true</c>, wenn mindestens ein Cache- oder Download-Eintrag vorliegt.</returns>
    public async Task<bool> HasOfflineDataAsync()
    {
        var cachedLists = await Connection.Table<CachedListEntry>().CountAsync();
        if (cachedLists > 0)
        {
            return true;
        }

        var favorites = await Connection.Table<CachedFavoritesEntry>().CountAsync();
        if (favorites > 0)
        {
            return true;
        }

        var downloads = await Connection.Table<DownloadManifestEntry>().CountAsync();
        return downloads > 0;
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
        /// <summary>Name der Spalte.</summary>
        public string? Name { get; set; }
    }
}
