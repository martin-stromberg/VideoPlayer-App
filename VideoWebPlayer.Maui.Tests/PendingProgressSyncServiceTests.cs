using VideoPlayer.Storage;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>
/// Persistente Fortschritts-Warteschlange (Schritt 6): Einreihen mit
/// Coalescing pro Medium, chronologische Reihenfolge, Löschung nach
/// erfolgreichem Senden und Persistenz über einen „Neustart" (neue
/// Datenbankverbindung auf derselben Datei).
/// </summary>
[Collection("SqliteDatabase")]
public class PendingProgressSyncServiceTests : IDisposable
{
    private readonly string root;
    private readonly AppStorageLayout layout;

    public PendingProgressSyncServiceTests()
    {
        root = Path.Combine(Path.GetTempPath(), "videoplayer-queue-tests", Guid.NewGuid().ToString("N"));
        layout = new AppStorageLayout(
            backupRoot: Path.Combine(root, "backup"),
            noBackupRoot: Path.Combine(root, "nobackup"),
            cacheRoot: Path.Combine(root, "cache"),
            profile: AppProfile.Create(null));
        layout.EnsureDirectoriesExist();
    }

    public void Dispose()
    {
        SQLite.SQLiteAsyncConnection.ResetPool();
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private async Task<PendingProgressSyncService> CreateInitializedAsync()
    {
        var db = new BackupDatabase(layout);
        await db.InitializeAsync();
        return new PendingProgressSyncService(db);
    }

    [Fact]
    public async Task Enqueue_StoresPositionAndDuration()
    {
        var queue = await CreateInitializedAsync();

        await queue.EnqueueAsync("movie", 42, TimeSpan.FromMinutes(12), TimeSpan.FromMinutes(90));

        var entry = Assert.Single(await queue.GetPendingAsync());
        Assert.Equal("movie", entry.VideoType);
        Assert.Equal(42, entry.VideoId);
        Assert.Equal(TimeSpan.FromMinutes(12).Ticks, entry.PositionTicks);
        Assert.Equal(TimeSpan.FromMinutes(90).Ticks, entry.DurationTicks);
    }

    [Fact]
    public async Task Enqueue_SameMedia_KeepsOnlyLatestPosition()
    {
        var queue = await CreateInitializedAsync();

        await queue.EnqueueAsync("episode", 7, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(45));
        await queue.EnqueueAsync("episode", 7, TimeSpan.FromMinutes(8), TimeSpan.FromMinutes(45));

        var entry = Assert.Single(await queue.GetPendingAsync());
        Assert.Equal(TimeSpan.FromMinutes(8).Ticks, entry.PositionTicks);
        Assert.Equal(1, await queue.CountAsync());
    }

    [Fact]
    public async Task GetPending_OrdersChronologically()
    {
        var queue = await CreateInitializedAsync();

        await queue.EnqueueAsync("movie", 1, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(90));
        await queue.EnqueueAsync("episode", 2, TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(45));
        await queue.EnqueueAsync("movie", 3, TimeSpan.FromMinutes(3), TimeSpan.FromMinutes(90));

        var pending = await queue.GetPendingAsync();

        Assert.Equal([1, 2, 3], pending.Select(e => e.VideoId).ToArray());
    }

    [Fact]
    public async Task RemoveForMedia_RemovesOnlyThatMedia()
    {
        var queue = await CreateInitializedAsync();
        await queue.EnqueueAsync("movie", 1, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(90));
        await queue.EnqueueAsync("episode", 2, TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(45));

        await queue.RemoveForMediaAsync("movie", 1);

        var entry = Assert.Single(await queue.GetPendingAsync());
        Assert.Equal(2, entry.VideoId);
        // Gleiche Id unter anderem Typ bleibt unberührt.
        await queue.EnqueueAsync("movie", 2, TimeSpan.FromMinutes(4), TimeSpan.FromMinutes(90));
        Assert.Equal(2, await queue.CountAsync());
    }

    [Fact]
    public async Task Pending_SurvivesDatabaseReopen()
    {
        // Erste „App-Instanz" reiht ein, zweite Instanz auf derselben Datei
        // liest die Meldung — Persistenz über den App-Neustart.
        var first = await CreateInitializedAsync();
        await first.EnqueueAsync("movie", 42, TimeSpan.FromMinutes(12), TimeSpan.FromMinutes(90));
        SQLite.SQLiteAsyncConnection.ResetPool();

        var second = await CreateInitializedAsync();

        var entry = Assert.Single(await second.GetPendingAsync());
        Assert.Equal(42, entry.VideoId);
        Assert.Equal(TimeSpan.FromMinutes(12).Ticks, entry.PositionTicks);
        Assert.Equal(TimeSpan.FromMinutes(90).Ticks, entry.DurationTicks);
    }
}
