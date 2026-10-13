#pragma warning disable xUnit1051 // Kurzlebige Unit-Tests: TestContext-CancellationToken nicht erforderlich
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using VideoPlayer.ApiClient.Models;
using VideoPlayer.Mvu;
using VideoPlayer.Storage;
using VideoPlayer.Storage.Entities;
using VideoWebPlayer.Maui.Tests.Stubs;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>
/// Synchronisation nach (Wieder-)Verbindung (Schritt 6): wartende
/// Fortschritte werden vor den Metadaten nachgesendet und nur nach
/// Erfolg aus der Warteschlange entfernt; Quellen, „Neu im Programm",
/// Weiterschauen und Favoriten landen in der Cache-Ablage.
/// </summary>
[Collection("SqliteDatabase")]
public class ServerSyncServiceTests : IDisposable
{
    private static readonly Uri Server = new("http://testserver/videoplayer/");

    private readonly string root;
    private readonly AppStorageLayout layout;
    private readonly PendingProgressSyncService queue;
    private readonly CacheDatabase cache;
    private readonly StubHttpMessageHandler handler = new();
    private readonly AuthSessionService authSession;

    /// <summary>Initialisiert eine neue Instanz der Testklasse <c>ServerSyncServiceTests</c>.</summary>
    public ServerSyncServiceTests()
    {
        root = Path.Combine(Path.GetTempPath(), "videoplayer-sync-tests", Guid.NewGuid().ToString("N"));
        layout = new AppStorageLayout(
            backupRoot: Path.Combine(root, "backup"),
            noBackupRoot: Path.Combine(root, "nobackup"),
            cacheRoot: Path.Combine(root, "cache"),
            profile: AppProfile.Create(null));
        layout.EnsureDirectoriesExist();
        var backup = new BackupDatabase(layout);
        backup.InitializeAsync().Wait();
        cache = new CacheDatabase(layout);
        cache.InitializeAsync().Wait();
        queue = new PendingProgressSyncService(backup);

        var secrets = new AppSecretsService(
            new InMemorySettingsStore(), new InMemorySecretStore(), AppProfile.Create(null));
        authSession = new AuthSessionService(
            secrets,
            uri => new HttpClient(handler, disposeHandler: false) { BaseAddress = uri },
            new RecordingDelays().Delay);
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

    private async Task LoginAsync()
    {
        handler.EnqueueJson(HttpStatusCode.OK, new { token = "jwt", expires = "2030-01-01T00:00:00Z" });
        await authSession.LoginAsync(Server, "martin", "pw");
    }

    private void EnqueueMetadataResponses()
    {
        handler.EnqueueJson(HttpStatusCode.OK, new[]
        {
            new MediaSource { Id = 1, Name = "Filme" }
        });
        handler.EnqueueJson(HttpStatusCode.OK, Array.Empty<RecentEntry>());
        handler.EnqueueJson(HttpStatusCode.OK, new[]
        {
            new ContinueWatchingEntry
            {
                MediaType = "movie",
                PositionSeconds = 120,
                Title = "Film",
                Entry = new Movie { Id = 42, Name = "Film" }
            }
        });
        handler.EnqueueJson(HttpStatusCode.OK, new[]
        {
            new FavoriteEntry { Id = 9, Entry = new TvShowEpisode { Id = 7, Name = "E1" } }
        });
    }

    private ServerSyncService CreateService() => new(authSession, queue, cache);

    /// <summary>Prüft SynchronizeAsync: sendet Ausstehendes und aktualisiert dann die Caches.</summary>
    [Fact]
    public async Task SynchronizeAsync_SendsPendingThenRefreshesCaches()
    {
        await LoginAsync();
        await queue.EnqueueAsync("movie", 42, TimeSpan.FromMinutes(12), TimeSpan.FromMinutes(90));
        await queue.EnqueueAsync("episode", 7, TimeSpan.FromMinutes(3), TimeSpan.FromMinutes(45));
        handler.Enqueue(HttpStatusCode.OK); // Fortschritt 1
        handler.Enqueue(HttpStatusCode.OK); // Fortschritt 2
        EnqueueMetadataResponses();

        var result = await CreateService().SynchronizeAsync();

        Assert.Equal(2, result.SentProgressReports);
        Assert.Equal(0, result.RemainingProgressReports);
        Assert.True(result.MetadataSynced);
        Assert.True(result.ContinueWatchingSynced);
        Assert.True(result.FavoritesSynced);
        Assert.Equal(0, await queue.CountAsync());

        // Reihenfolge: beide Fortschritte zuerst, dann die Metadaten.
        var paths = handler.Requests.Select(r => r.PathAndQuery).ToList();
        Assert.EndsWith("/api/continue-watching/progress", paths[1]);
        Assert.EndsWith("/api/continue-watching/progress", paths[2]);
        Assert.EndsWith("/api/Sources", paths[3]);
        Assert.EndsWith("/api/items/recent", paths[4]);
        Assert.EndsWith("/api/continue-watching", paths[5]);
        Assert.EndsWith("/api/favorites", paths[6]);

        // Fortschritts-Payload enthält Position und Dauer in Sekunden.
        using var body = JsonDocument.Parse(handler.Requests[1].Body!);
        Assert.Equal(720, body.RootElement.GetProperty("PositionSeconds").GetInt64());
        Assert.Equal(5400, body.RootElement.GetProperty("DurationSeconds").GetInt64());

        // Caches sind befüllt.
        var lists = await cache.Connection.Table<CachedListEntry>().ToListAsync();
        Assert.Contains(lists, e => e.Key == ServerSyncService.SourcesCacheKey);
        Assert.Contains(lists, e => e.Key == ServerSyncService.RecentCacheKey);
        Assert.Contains(lists, e => e.Key == ServerSyncService.ContinueWatchingCacheKey);
        var favorites = await cache.Connection.Table<CachedFavoritesEntry>().ToListAsync();
        var favorite = Assert.Single(favorites);
        Assert.Equal("tvshowepisode:7", favorite.Key);
        Assert.Equal(7, favorite.VideoId);
    }

    /// <summary>Prüft SynchronizeAsync: der Ergebnis-Eintrag enthält die SyncResult-Zähler.</summary>
    [Fact]
    public async Task SynchronizeAsync_WritesResultCounters()
    {
        var logging = new RecordingLoggerProvider();
        await LoginAsync();
        await queue.EnqueueAsync("movie", 42, TimeSpan.FromMinutes(12), TimeSpan.FromMinutes(90));
        handler.Enqueue(HttpStatusCode.OK); // Fortschritt
        EnqueueMetadataResponses();

        var result = await new ServerSyncService(
                authSession, queue, cache, logging.CreateLogger<ServerSyncService>())
            .SynchronizeAsync();

        Assert.Equal(1, result.SentProgressReports);
        Assert.Equal(0, result.RemainingProgressReports);
        Assert.Contains(logging.Entries, e =>
            e.Level == LogLevel.Information
            && e.Category.Contains(nameof(ServerSyncService), StringComparison.Ordinal)
            && e.Message.Contains("Synchronisation abgeschlossen", StringComparison.Ordinal)
            && e.Message.Contains("1 Fortschritte", StringComparison.Ordinal)
            && e.Message.Contains("0 ausstehend", StringComparison.Ordinal));
    }

    /// <summary>
    /// Prüft FlushPendingProgress: der Nachsende-Eintrag läuft nur, wenn
    /// tatsächlich Meldungen gesendet wurden — bei leerer Warteschlange
    /// ist „0 nachgesendet" ein Eintrag ohne Ereignis (derselbe Zähler
    /// steht bereits im Aggregat-Eintrag des Voll-Syncs).
    /// </summary>
    [Fact]
    public async Task FlushPendingProgress_LogsSentCount_OnlyWhenSent()
    {
        var logging = new RecordingLoggerProvider();
        await LoginAsync();
        var service = new ServerSyncService(
            authSession, queue, cache, logging.CreateLogger<ServerSyncService>());

        Assert.Equal(0, await service.FlushPendingProgressAsync());
        Assert.DoesNotContain(logging.Entries, e =>
            e.Level == LogLevel.Information
            && e.Message.Contains("nachgesendet", StringComparison.Ordinal));

        await queue.EnqueueAsync("movie", 42, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(90));
        handler.Enqueue(HttpStatusCode.OK);
        Assert.Equal(1, await service.FlushPendingProgressAsync());
        Assert.Single(logging.Entries, e =>
            e.Level == LogLevel.Information
            && e.Message.Contains("nachgesendet", StringComparison.Ordinal));
    }

    /// <summary>Prüft SynchronizeAsync: ohne Client → ist ein No-Op.</summary>
    [Fact]
    public async Task SynchronizeAsync_WithoutClient_IsNoOp()
    {
        await queue.EnqueueAsync("movie", 42, TimeSpan.FromMinutes(12), TimeSpan.FromMinutes(90));

        var result = await CreateService().SynchronizeAsync();

        Assert.Equal(0, result.SentProgressReports);
        Assert.Equal(1, result.RemainingProgressReports);
        Assert.False(result.MetadataSynced);
        Assert.Empty(handler.Requests);
    }

    /// <summary>Prüft FlushPendingProgress: bei Transportfehler → behält die verbleibenden Einträge.</summary>
    [Fact]
    public async Task FlushPendingProgress_TransportError_KeepsRemainingEntries()
    {
        await LoginAsync();
        await queue.EnqueueAsync("movie", 1, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(90));
        await queue.EnqueueAsync("movie", 2, TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(90));
        // Erste Meldung scheitert nach der Wiederholungsstrategie (4 Versuche).
        for (var i = 0; i < 4; i++)
        {
            handler.EnqueueException(new HttpRequestException("Server weg"));
        }

        var sent = await CreateService().FlushPendingProgressAsync();

        // Abbruch: nichts gesendet, beide Einträge bleiben für den
        // nächsten Sync erhalten (kein Datenverlust).
        Assert.Equal(0, sent);
        Assert.Equal(2, await queue.CountAsync());
    }

    /// <summary>Prüft FlushPendingProgress: bei permanentem Fehler → verwirft den Eintrag und setzt fort.</summary>
    [Fact]
    public async Task FlushPendingProgress_PermanentError_DiscardsEntryAndContinues()
    {
        await LoginAsync();
        await queue.EnqueueAsync("movie", 1, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(90));
        await queue.EnqueueAsync("movie", 2, TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(90));
        handler.Enqueue(HttpStatusCode.NotFound); // Medium existiert nicht mehr
        handler.Enqueue(HttpStatusCode.OK);

        var sent = await CreateService().FlushPendingProgressAsync();

        Assert.Equal(1, sent);
        Assert.Equal(0, await queue.CountAsync());
    }

    /// <summary>Prüft SynchronizeAsync: bei partiellem Fehlschlag → meldet false, läuft aber weiter.</summary>
    [Fact]
    public async Task SynchronizeAsync_PartialFailure_ReportsFalseButKeepsGoing()
    {
        await LoginAsync();
        handler.EnqueueJson(HttpStatusCode.OK, Array.Empty<MediaSource>());
        handler.EnqueueJson(HttpStatusCode.OK, Array.Empty<RecentEntry>());
        handler.EnqueueJson(HttpStatusCode.OK, Array.Empty<ContinueWatchingEntry>());
        // Favoriten: 500 wird dreimal wiederholt → 4 Antworten.
        for (var i = 0; i < 4; i++)
        {
            handler.Enqueue(HttpStatusCode.InternalServerError);
        }

        var result = await CreateService().SynchronizeAsync();

        Assert.True(result.MetadataSynced);
        Assert.True(result.ContinueWatchingSynced);
        Assert.False(result.FavoritesSynced);
    }

    /// <summary>Prüft SyncContinueWatching: aktualisiert nur den betreffenden Cache.</summary>
    [Fact]
    public async Task SyncContinueWatching_RefreshesOnlyThatCache()
    {
        await LoginAsync();
        handler.EnqueueJson(HttpStatusCode.OK, new[]
        {
            new ContinueWatchingEntry { MediaType = "episode", PositionSeconds = 60, Title = "E" }
        });

        var synced = await CreateService().SyncContinueWatchingAsync();

        Assert.True(synced);
        var lists = await cache.Connection.Table<CachedListEntry>().ToListAsync();
        var entry = Assert.Single(lists);
        Assert.Equal(ServerSyncService.ContinueWatchingCacheKey, entry.Key);
    }
}
