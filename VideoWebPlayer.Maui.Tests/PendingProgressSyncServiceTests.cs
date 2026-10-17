using Microsoft.Extensions.Logging;
using VideoPlayer.Storage;
using VideoWebPlayer.Maui.Tests.Stubs;

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

    /// <summary>Initialisiert eine neue Instanz der Testklasse <c>PendingProgressSyncServiceTests</c>.</summary>
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

    /// <inheritdoc/>
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

    /// <summary>Prüft Enqueue: speichert Position und Duration.</summary>
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

    /// <summary>Prüft Enqueue: bei gleichen Medien → behält nur die letzte Position.</summary>
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

    /// <summary>Prüft GetPending: ordnet die Einträge chronologisch.</summary>
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

    /// <summary>Prüft RemoveForMedia: entfernt nur die betreffenden Medien.</summary>
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

    /// <summary>
    /// Prüft die Protokolleinträge: nur der Zustandswechsel (erste
    /// Einreihung eines Mediums) schreibt einen Eintrag — Ersetzen im
    /// Fortschrittstakt und das Entfernen pro Medium bleiben still.
    /// </summary>
    [Fact]
    public async Task Enqueue_Remove_LoggedAggregated()
    {
        var logging = new RecordingLoggerProvider();
        var db = new BackupDatabase(layout);
        await db.InitializeAsync();
        var queue = new PendingProgressSyncService(db, logging.CreateLogger<PendingProgressSyncService>());

        await queue.EnqueueAsync("movie", 42, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(90));
        Assert.Contains(logging.Entries, e =>
            e.Level == LogLevel.Information
            && e.Category.Contains(nameof(PendingProgressSyncService), StringComparison.Ordinal)
            && e.Message.Contains("eingereiht", StringComparison.Ordinal));

        // Erneutes Einreihen desselben Mediums (Fortschrittstakt)
        // erzeugt keinen weiteren Eintrag.
        await queue.EnqueueAsync("movie", 42, TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(90));
        Assert.Single(logging.Entries, e =>
            e.Level == LogLevel.Information
            && e.Category.Contains(nameof(PendingProgressSyncService), StringComparison.Ordinal)
            && e.Message.Contains("eingereiht", StringComparison.Ordinal));

        // Entfernen pro Medium bleibt still — die Gesamtzahl trägt der
        // Flush-Lauf des Aufrufers (ServerSyncService).
        await queue.RemoveForMediaAsync("movie", 42);
        Assert.DoesNotContain(logging.Entries, e =>
            e.Category.Contains(nameof(PendingProgressSyncService), StringComparison.Ordinal)
            && e.Message.Contains("entfernt", StringComparison.Ordinal));
    }

    /// <summary>Prüft Pending: übersteht ein erneutes Öffnen der Datenbank.</summary>
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

    /// <summary>Prüft Enqueue: der Playlist-Kontext wird in der neuen Spalte gespeichert (Issue 59).</summary>
    [Fact]
    public async Task Enqueue_StoresPlaylistId()
    {
        var queue = await CreateInitializedAsync();

        await queue.EnqueueAsync("movie", 42, TimeSpan.FromMinutes(12), TimeSpan.FromMinutes(90), playlistId: 7);

        var entry = Assert.Single(await queue.GetPendingAsync());
        Assert.Equal(7, entry.PlaylistId);
    }

    /// <summary>
    /// Prüft Enqueue: bei einem Kontextwechsel desselben Mediums gewinnt
    /// der zuletzt eingereihte Stand — die Deduplikation pro Medium
    /// (<c>VideoType</c>/<c>VideoId</c>) bleibt unverändert.
    /// </summary>
    [Fact]
    public async Task Enqueue_SameMedia_LatestContextWins()
    {
        var queue = await CreateInitializedAsync();

        await queue.EnqueueAsync("movie", 42, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(90), playlistId: 7);
        await queue.EnqueueAsync("movie", 42, TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(90), playlistId: 9);
        await queue.EnqueueAsync("movie", 42, TimeSpan.FromMinutes(3), TimeSpan.FromMinutes(90));

        var entry = Assert.Single(await queue.GetPendingAsync());
        Assert.Equal(TimeSpan.FromMinutes(3).Ticks, entry.PositionTicks);
        Assert.Null(entry.PlaylistId);
        Assert.Equal(1, await queue.CountAsync());
    }

    /// <summary>Prüft Pending: der Playlist-Bezug übersteht ein erneutes Öffnen der Datenbank.</summary>
    [Fact]
    public async Task Pending_PlaylistId_SurvivesDatabaseReopen()
    {
        var first = await CreateInitializedAsync();
        await first.EnqueueAsync("movie", 42, TimeSpan.FromMinutes(12), TimeSpan.FromMinutes(90), playlistId: 7);
        SQLite.SQLiteAsyncConnection.ResetPool();

        var second = await CreateInitializedAsync();

        var entry = Assert.Single(await second.GetPendingAsync());
        Assert.Equal(7, entry.PlaylistId);
    }

    /// <summary>
    /// Prüft die Migration: eine ohne die PlaylistId-Spalte angelegte
    /// <c>pending_progress</c>-Tabelle (Altbestand) bekommt sie über
    /// <c>InitializeAsync</c>/<c>EnsureColumnAsync</c> nachgezogen.
    /// </summary>
    [Fact]
    public async Task PendingProgress_PlaylistColumn_MigratedOnExistingDatabase()
    {
        // Tabelle im Schema einer Altversion anlegen (ohne PlaylistId) —
        // die Initialisierung muss die Spalte nachziehen statt zu scheitern.
        var db = new BackupDatabase(layout);
        await db.Connection.ExecuteAsync(
            "CREATE TABLE IF NOT EXISTS pending_progress (" +
            "Id integer primary key autoincrement, " +
            "VideoId integer not null, " +
            "VideoType varchar not null, " +
            "PositionTicks bigint not null, " +
            "DurationTicks bigint not null, " +
            "UpdatedAtUtc datetime not null)");
        await db.InitializeAsync();
        var queue = new PendingProgressSyncService(db);

        await queue.EnqueueAsync("movie", 42, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(90), playlistId: 7);

        var entry = Assert.Single(await queue.GetPendingAsync());
        Assert.Equal(7, entry.PlaylistId);
    }
}
