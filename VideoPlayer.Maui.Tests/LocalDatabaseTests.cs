#pragma warning disable xUnit1051 // Kurzlebige Unit-Tests: TestContext-CancellationToken nicht erforderlich
using VideoPlayer.Storage;
using VideoPlayer.Storage.Entities;

namespace VideoPlayer.Maui.Tests;

/// <summary>
/// Reale SQLite-Tests (sqlite-net-pcl + bundle_green laufen unter net10.0):
/// Tabellenanlage, Roundtrips und Ablageorte der beiden Datenbanken.
/// </summary>
[Collection("SqliteDatabase")]
public class LocalDatabaseTests : IDisposable
{
    private readonly string root;

    /// <summary>Initialisiert eine neue Instanz der Testklasse <c>LocalDatabaseTests</c>.</summary>
    public LocalDatabaseTests()
    {
        root = Path.Combine(Path.GetTempPath(), "videoplayer-db-tests", Guid.NewGuid().ToString("N"));
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        SQLite.SQLiteAsyncConnection.ResetPool();
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private AppStorageLayout CreateLayout()
    {
        var layout = new AppStorageLayout(
            backupRoot: Path.Combine(root, "backup"),
            noBackupRoot: Path.Combine(root, "nobackup"),
            cacheRoot: Path.Combine(root, "cache"),
            profile: AppProfile.Create(null));
        layout.EnsureDirectoriesExist();
        return layout;
    }

    /// <summary>Prüft BackupDatabase: erstellt Tabellen.</summary>
    [Fact]
    public async Task BackupDatabase_CreatesTables()
    {
        var db = new BackupDatabase(CreateLayout());
        await db.InitializeAsync();

        // Tabellen existieren, wenn ein leeres Select ohne Fehler läuft.
        Assert.Empty(await db.Connection.Table<KnownServerEntry>().ToListAsync());
        Assert.Empty(await db.Connection.Table<BackupStateEntry>().ToListAsync());
        Assert.Empty(await db.Connection.Table<PendingProgressSyncEntry>().ToListAsync());
    }

    /// <summary>Prüft BackupDatabase: Werte überstehen den Roundtrip.</summary>
    [Fact]
    public async Task BackupDatabase_RoundTrips()
    {
        var db = new BackupDatabase(CreateLayout());
        await db.InitializeAsync();

        await db.Connection.InsertAsync(new KnownServerEntry
        {
            Address = "http://server/videoplayer",
            DisplayName = "Heimserver",
            LastUsedAtUtc = new DateTime(2026, 9, 22, 0, 0, 0, DateTimeKind.Utc)
        });
        await db.Connection.InsertAsync(new BackupStateEntry
        {
            Key = BackupStateEntry.LastConnectedServerKey,
            Value = "http://server/videoplayer"
        });
        await db.Connection.InsertAsync(new PendingProgressSyncEntry
        {
            VideoId = 42,
            VideoType = "episode",
            PositionTicks = TimeSpan.FromMinutes(12).Ticks,
            UpdatedAtUtc = DateTime.UtcNow
        });

        var server = Assert.Single(await db.Connection.Table<KnownServerEntry>().ToListAsync());
        Assert.Equal("Heimserver", server.DisplayName);

        var state = await db.Connection.FindAsync<BackupStateEntry>(BackupStateEntry.LastConnectedServerKey);
        Assert.Equal("http://server/videoplayer", state!.Value);

        var progress = Assert.Single(await db.Connection.Table<PendingProgressSyncEntry>().ToListAsync());
        Assert.Equal(42, progress.VideoId);
    }

    /// <summary>Prüft CacheDatabase: erstellt Tabellen und übersteht den Roundtrip.</summary>
    [Fact]
    public async Task CacheDatabase_CreatesTablesAndRoundTrips()
    {
        var db = new CacheDatabase(CreateLayout());
        await db.InitializeAsync();

        await db.Connection.InsertAsync(new CachedListEntry
        {
            Key = "home:recentlyAdded",
            Json = "[]",
            UpdatedAtUtc = DateTime.UtcNow
        });
        await db.Connection.InsertAsync(new CachedFavoritesEntry
        {
            Key = "episode:42",
            VideoId = 42,
            VideoType = "episode",
            Json = "{}",
            UpdatedAtUtc = DateTime.UtcNow
        });
        await db.Connection.InsertAsync(new DownloadManifestEntry
        {
            VideoId = 42,
            VideoType = "episode",
            FilePath = "videos/42.mp4",
            SizeBytes = 1024,
            State = "Completed",
            UpdatedAtUtc = DateTime.UtcNow
        });

        Assert.Single(await db.Connection.Table<CachedListEntry>().ToListAsync());
        Assert.Single(await db.Connection.Table<CachedFavoritesEntry>().ToListAsync());
        var manifest = Assert.Single(await db.Connection.Table<DownloadManifestEntry>().ToListAsync());
        Assert.Equal("videos/42.mp4", manifest.FilePath);
    }

    /// <summary>Prüft CacheDatabase: bei <c>HasOfflineData</c> → false bei leerem Cache.</summary>
    [Fact]
    public async Task CacheDatabase_HasOfflineData_FalseWhenEmpty()
    {
        var db = new CacheDatabase(CreateLayout());
        await db.InitializeAsync();

        Assert.False(await db.HasOfflineDataAsync());
    }

    /// <summary>Prüft CacheDatabase: bei <c>HasOfflineData</c> → true mit gecachter Liste.</summary>
    [Fact]
    public async Task CacheDatabase_HasOfflineData_TrueWithCachedList()
    {
        var db = new CacheDatabase(CreateLayout());
        await db.InitializeAsync();
        await db.Connection.InsertAsync(new CachedListEntry
        {
            Key = "home:recentlyAdded",
            Json = "[]",
            UpdatedAtUtc = DateTime.UtcNow
        });

        Assert.True(await db.HasOfflineDataAsync());
    }

    /// <summary>Prüft CacheDatabase: bei <c>HasOfflineData</c> → true mit Downloadmanifest.</summary>
    [Fact]
    public async Task CacheDatabase_HasOfflineData_TrueWithDownloadManifest()
    {
        var db = new CacheDatabase(CreateLayout());
        await db.InitializeAsync();
        await db.Connection.InsertAsync(new DownloadManifestEntry
        {
            VideoId = 42,
            VideoType = "episode",
            FilePath = "videos/42.mp4",
            SizeBytes = 1024,
            State = "Completed",
            UpdatedAtUtc = DateTime.UtcNow
        });

        Assert.True(await db.HasOfflineDataAsync());
    }

    /// <summary>Prüft BackupDatabase: migriert DurationTicks auf einer vorhandenen Tabelle.</summary>
    [Fact]
    public async Task BackupDatabase_MigratesDurationTicksOntoExistingTable()
    {
        // Bestehende Installation (Schritt-6-Migration): Tabelle im alten
        // Schema ohne DurationTicks anlegen und eine Zeile befüllen.
        var layout = CreateLayout();
        SqliteBootstrap.EnsureInitialized();
        var legacy = new SQLite.SQLiteAsyncConnection(layout.DataDatabasePath);
        await legacy.ExecuteAsync(
            "CREATE TABLE pending_progress (" +
            "Id integer primary key autoincrement, " +
            "VideoId integer not null, " +
            "VideoType varchar not null, " +
            "PositionTicks bigint not null, " +
            "UpdatedAtUtc datetime not null)");
        await legacy.ExecuteAsync(
            "INSERT INTO pending_progress (VideoId, VideoType, PositionTicks, UpdatedAtUtc) " +
            "VALUES (42, 'episode', 123, '2026-01-01 00:00:00')");
        SQLite.SQLiteAsyncConnection.ResetPool();

        var db = new BackupDatabase(layout);
        await db.InitializeAsync();

        var entry = Assert.Single(await db.Connection.Table<PendingProgressSyncEntry>().ToListAsync());
        Assert.Equal(42, entry.VideoId);
        Assert.Equal(0, entry.DurationTicks); // Spalte per Migration, Default 0

        // Idempotent: ein zweites Initialize läuft fehlerfrei.
        await db.InitializeAsync();
    }

    /// <summary>Prüft CacheDatabase: migriert den Serienkontext auf einer vorhandenen Manifest-Tabelle.</summary>
    [Fact]
    public async Task CacheDatabase_MigratesSeriesContextOntoExistingManifest()
    {
        // Bestehende Installation (Schritt-11-Schema): Manifest ohne die
        // Serienkontext-Spalten anlegen und eine Zeile befüllen.
        var layout = CreateLayout();
        SqliteBootstrap.EnsureInitialized();
        var legacy = new SQLite.SQLiteAsyncConnection(layout.CacheDatabasePath);
        await legacy.ExecuteAsync(
            "CREATE TABLE download_manifest (" +
            "Id integer primary key autoincrement, " +
            "VideoId integer not null, " +
            "VideoType varchar not null, " +
            "FilePath varchar not null, " +
            "SizeBytes bigint not null, " +
            "Title varchar not null, " +
            "DownloadedBytes bigint not null, " +
            "TotalBytes bigint not null, " +
            "Error varchar null, " +
            "State varchar not null, " +
            "UpdatedAtUtc datetime not null)");
        await legacy.ExecuteAsync(
            "INSERT INTO download_manifest (VideoId, VideoType, FilePath, SizeBytes, Title, " +
            "DownloadedBytes, TotalBytes, State, UpdatedAtUtc) " +
            "VALUES (31, 'tvshowepisode', 'videos/31.mp4', 100, 'Ep', 100, 100, 'completed', '2026-01-01 00:00:00')");
        SQLite.SQLiteAsyncConnection.ResetPool();

        var db = new CacheDatabase(layout);
        await db.InitializeAsync();

        var entry = Assert.Single(await db.Connection.Table<DownloadManifestEntry>().ToListAsync());
        Assert.Equal(31, entry.VideoId);
        // Neue Spalten vorhanden und beschreibbar.
        entry.ShowId = 11;
        entry.SeasonNumber = 1;
        entry.EpisodeNumber = 2;
        entry.ShowTitle = "Serie";
        await db.Connection.UpdateAsync(entry);
        var reloaded = await db.Connection.Table<DownloadManifestEntry>().FirstAsync();
        Assert.Equal(11, reloaded.ShowId);
        Assert.Equal(2, reloaded.EpisodeNumber);
    }

    /// <summary>Prüft CacheDatabase: migriert den Playlist-Bezug auf einer vorhandenen Manifest-Tabelle.</summary>
    [Fact]
    public async Task CacheDatabase_MigratesPlaylistContextOntoExistingManifest()
    {
        // Bestehende Installation mit Serienkontext-, aber ohne die
        // Playlist-Spalten (Schema vor Schritt 7) anlegen und befüllen.
        var layout = CreateLayout();
        SqliteBootstrap.EnsureInitialized();
        var legacy = new SQLite.SQLiteAsyncConnection(layout.CacheDatabasePath);
        await legacy.ExecuteAsync(
            "CREATE TABLE download_manifest (" +
            "Id integer primary key autoincrement, " +
            "VideoId integer not null, " +
            "VideoType varchar not null, " +
            "FilePath varchar not null, " +
            "SizeBytes bigint not null, " +
            "Title varchar not null, " +
            "DownloadedBytes bigint not null, " +
            "TotalBytes bigint not null, " +
            "Error varchar null, " +
            "State varchar not null, " +
            "ShowId bigint null, " +
            "ShowTitle varchar null, " +
            "SeasonNumber integer null, " +
            "EpisodeNumber integer null, " +
            "UpdatedAtUtc datetime not null)");
        await legacy.ExecuteAsync(
            "INSERT INTO download_manifest (VideoId, VideoType, FilePath, SizeBytes, Title, " +
            "DownloadedBytes, TotalBytes, State, UpdatedAtUtc) " +
            "VALUES (31, 'tvshowepisode', 'videos/31.mp4', 100, 'Ep', 100, 100, 'completed', '2026-01-01 00:00:00')");
        SQLite.SQLiteAsyncConnection.ResetPool();

        var db = new CacheDatabase(layout);
        await db.InitializeAsync();

        var entry = Assert.Single(await db.Connection.Table<DownloadManifestEntry>().ToListAsync());
        Assert.Equal(31, entry.VideoId);
        Assert.Null(entry.PlaylistId);
        // Neue Spalten vorhanden und beschreibbar.
        entry.PlaylistId = 5;
        entry.PlaylistEntryId = 103;
        entry.PlaylistSortOrder = 3;
        await db.Connection.UpdateAsync(entry);
        var reloaded = await db.Connection.Table<DownloadManifestEntry>().FirstAsync();
        Assert.Equal(5, reloaded.PlaylistId);
        Assert.Equal(103, reloaded.PlaylistEntryId);
        Assert.Equal(3, reloaded.PlaylistSortOrder);

        // Idempotent: ein zweites Initialize läuft fehlerfrei.
        await db.InitializeAsync();
    }

    /// <summary>Prüft CacheDatabase: migriert die Vorschaubild-Spalte auf einer vorhandenen Manifest-Tabelle.</summary>
    [Fact]
    public async Task CacheDatabase_MigratesPictureIdOntoExistingManifest()
    {
        // Bestehende Installation ohne die PictureId-Spalte anlegen und
        // eine Zeile befüllen.
        var layout = CreateLayout();
        SqliteBootstrap.EnsureInitialized();
        var legacy = new SQLite.SQLiteAsyncConnection(layout.CacheDatabasePath);
        await legacy.ExecuteAsync(
            "CREATE TABLE download_manifest (" +
            "Id integer primary key autoincrement, " +
            "VideoId integer not null, " +
            "VideoType varchar not null, " +
            "FilePath varchar not null, " +
            "SizeBytes bigint not null, " +
            "Title varchar not null, " +
            "DownloadedBytes bigint not null, " +
            "TotalBytes bigint not null, " +
            "Error varchar null, " +
            "State varchar not null, " +
            "UpdatedAtUtc datetime not null)");
        await legacy.ExecuteAsync(
            "INSERT INTO download_manifest (VideoId, VideoType, FilePath, SizeBytes, Title, " +
            "DownloadedBytes, TotalBytes, State, UpdatedAtUtc) " +
            "VALUES (31, 'movie', 'videos/31.mp4', 100, 'Film', 100, 100, 'completed', '2026-01-01 00:00:00')");
        SQLite.SQLiteAsyncConnection.ResetPool();

        var db = new CacheDatabase(layout);
        await db.InitializeAsync();

        var entry = Assert.Single(await db.Connection.Table<DownloadManifestEntry>().ToListAsync());
        Assert.Equal(31, entry.VideoId);
        Assert.Null(entry.PictureId);
        // Neue Spalte vorhanden und beschreibbar.
        entry.PictureId = 9007;
        await db.Connection.UpdateAsync(entry);
        var reloaded = await db.Connection.Table<DownloadManifestEntry>().FirstAsync();
        Assert.Equal(9007, reloaded.PictureId);

        // Idempotent: ein zweites Initialize läuft fehlerfrei.
        await db.InitializeAsync();
    }

    /// <summary>Prüft CacheDatabase: migriert Ablaufdatum- und Herkunfts-Spalten auf einer vorhandenen Manifest-Tabelle (Altbestand = null).</summary>
    [Fact]
    public async Task CacheDatabase_MigratesExpiryAndOriginOntoExistingManifest()
    {
        // Bestehende Installation ohne die neuen Spalten anlegen und
        // eine Zeile befüllen.
        var layout = CreateLayout();
        SqliteBootstrap.EnsureInitialized();
        var legacy = new SQLite.SQLiteAsyncConnection(layout.CacheDatabasePath);
        await legacy.ExecuteAsync(
            "CREATE TABLE download_manifest (" +
            "Id integer primary key autoincrement, " +
            "VideoId integer not null, " +
            "VideoType varchar not null, " +
            "FilePath varchar not null, " +
            "SizeBytes bigint not null, " +
            "Title varchar not null, " +
            "DownloadedBytes bigint not null, " +
            "TotalBytes bigint not null, " +
            "Error varchar null, " +
            "State varchar not null, " +
            "UpdatedAtUtc datetime not null)");
        await legacy.ExecuteAsync(
            "INSERT INTO download_manifest (VideoId, VideoType, FilePath, SizeBytes, Title, " +
            "DownloadedBytes, TotalBytes, State, UpdatedAtUtc) " +
            "VALUES (31, 'movie', 'videos/31.mp4', 100, 'Film', 100, 100, 'completed', '2026-01-01 00:00:00')");
        SQLite.SQLiteAsyncConnection.ResetPool();

        var db = new CacheDatabase(layout);
        await db.InitializeAsync();

        var entry = Assert.Single(await db.Connection.Table<DownloadManifestEntry>().ToListAsync());
        Assert.Equal(31, entry.VideoId);
        Assert.Null(entry.ExpiresAtUtc);
        Assert.Null(entry.Origin);
        // Neue Spalten vorhanden und beschreibbar.
        entry.ExpiresAtUtc = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        entry.Origin = "auto";
        await db.Connection.UpdateAsync(entry);
        var reloaded = await db.Connection.Table<DownloadManifestEntry>().FirstAsync();
        Assert.Equal(new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc), reloaded.ExpiresAtUtc);
        Assert.Equal("auto", reloaded.Origin);

        // Idempotent: ein zweites Initialize läuft fehlerfrei.
        await db.InitializeAsync();
    }

    /// <summary>Prüft BackupDatabase: eine beschädigte Datei wird beiseitegelegt und neu initialisiert.</summary>
    [Fact]
    public async Task BackupDatabase_RecoversCorruptFile()
    {
        var layout = CreateLayout();
        await File.WriteAllBytesAsync(layout.DataDatabasePath, [1, 2, 3, 4, 5]); // keine SQLite-Datei

        var db = new BackupDatabase(layout);
        var recovered = await db.InitializeWithRecoveryAsync();

        Assert.True(recovered);
        // Die beschädigte Datei liegt als *.corrupt-* beiseite; die neue
        // Datenbank ist nutzbar.
        Assert.Single(Directory.GetFiles(Path.GetDirectoryName(layout.DataDatabasePath)!, "*.corrupt-*"));
        Assert.Empty(await db.Connection.Table<KnownServerEntry>().ToListAsync());
    }

    /// <summary>Prüft CacheDatabase: eine beschädigte Datei wird beiseitegelegt und neu initialisiert.</summary>
    [Fact]
    public async Task CacheDatabase_RecoversCorruptFile()
    {
        var layout = CreateLayout();
        await File.WriteAllTextAsync(layout.CacheDatabasePath, "definitiv-keine-datenbank");

        var db = new CacheDatabase(layout);
        var recovered = await db.InitializeWithRecoveryAsync();

        Assert.True(recovered);
        Assert.Single(Directory.GetFiles(Path.GetDirectoryName(layout.CacheDatabasePath)!, "*.corrupt-*"));
        Assert.Empty(await db.Connection.Table<DownloadManifestEntry>().ToListAsync());
    }

    /// <summary>Prüft Databases: eine intakte Datei löst keine Wiederherstellung aus.</summary>
    [Fact]
    public async Task Databases_IntactFile_NoRecovery()
    {
        var layout = CreateLayout();
        var db = new CacheDatabase(layout);
        await db.InitializeAsync();
        await db.Connection.InsertAsync(new CachedListEntry
        {
            Key = "home:recentlyAdded",
            Json = "[]",
            UpdatedAtUtc = DateTime.UtcNow
        });
        SQLite.SQLiteAsyncConnection.ResetPool();

        var second = new CacheDatabase(layout);
        var recovered = await second.InitializeWithRecoveryAsync();

        Assert.False(recovered);
        Assert.Single(await second.Connection.Table<CachedListEntry>().ToListAsync());
    }

    /// <summary>Prüft Databases: landen in den klassifizierten Verzeichnissen.</summary>
    [Fact]
    public async Task Databases_LandInClassifiedDirectories()
    {
        var layout = CreateLayout();
        var backupDb = new BackupDatabase(layout);
        var cacheDb = new CacheDatabase(layout);

        await backupDb.InitializeAsync();
        await cacheDb.InitializeAsync();

        // Dateien liegen in der jeweils klassifizierten Wurzel.
        Assert.True(File.Exists(layout.DataDatabasePath));
        Assert.True(File.Exists(layout.CacheDatabasePath));
        Assert.StartsWith(layout.BackupDirectory, layout.DataDatabasePath);
        Assert.StartsWith(layout.NoBackupDirectory, layout.CacheDatabasePath);
    }
}
