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
}
