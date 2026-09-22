using VideoPlayer.Storage;
using VideoPlayer.Storage.Entities;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>
/// Reale SQLite-Tests (sqlite-net-pcl + bundle_green laufen unter net10.0):
/// Tabellenanlage, Roundtrips und Ablageorte der beiden Datenbanken.
/// </summary>
[Collection("SqliteDatabase")]
public class LocalDatabaseTests : IDisposable
{
    private readonly string root;

    public LocalDatabaseTests()
    {
        root = Path.Combine(Path.GetTempPath(), "videoplayer-db-tests", Guid.NewGuid().ToString("N"));
    }

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
