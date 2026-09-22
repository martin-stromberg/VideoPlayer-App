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
    private readonly Lazy<SQLiteAsyncConnection> connection;

    /// <summary>Erstellt die Datenbank; die Verbindung wird erst bei Bedarf geöffnet.</summary>
    /// <param name="layout">Ablagelayout mit dem Datenbankpfad.</param>
    public CacheDatabase(AppStorageLayout layout)
    {
        ArgumentNullException.ThrowIfNull(layout);
        connection = new Lazy<SQLiteAsyncConnection>(() =>
        {
            SqliteBootstrap.EnsureInitialized();
            return new SQLiteAsyncConnection(layout.CacheDatabasePath);
        });
    }

    /// <summary>Asynchrone Verbindung (Lazy — Dateizugriff erst bei Bedarf).</summary>
    public SQLiteAsyncConnection Connection => connection.Value;

    /// <summary>Legt alle Tabellen an (idempotent).</summary>
    public async Task InitializeAsync()
    {
        await Connection.CreateTableAsync<CachedListEntry>();
        await Connection.CreateTableAsync<CachedFavoritesEntry>();
        await Connection.CreateTableAsync<DownloadManifestEntry>();
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
}
