#pragma warning disable xUnit1051 // Kurzlebige Unit-Tests: TestContext-CancellationToken nicht erforderlich
using System.Net;
using System.Text.Json;
using VideoPlayer.ApiClient.Models;
using VideoPlayer.Mvu;
using VideoPlayer.Storage;
using VideoPlayer.Storage.Entities;
using VideoWebPlayer.Maui.Tests.Stubs;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>
/// Kontextmenü-Mutationen der Weiterschauen-Liste (Ausblenden/
/// Überspringen): online geht die Mutation an den Server-Endpunkt und
/// die lokale Kopie <c>cached_lists</c>/<c>continue_watching</c> wird
/// danach vom Serverstand erneuert; ohne Client (offline) wird der
/// Eintrag lokal aus dem Cache-Eintrag entfernt.
/// </summary>
[Collection("SqliteDatabase")]
public class ContinueWatchingMutationServiceTests : IDisposable
{
    private static readonly Uri Server = new("http://testserver/videoplayer/");

    private readonly string root;
    private readonly CacheDatabase cache;
    private readonly StubHttpMessageHandler handler = new();
    private readonly AuthSessionService authSession;
    private readonly ServerSyncService syncService;

    /// <summary>Initialisiert eine neue Instanz der Testklasse <c>ContinueWatchingMutationServiceTests</c>.</summary>
    public ContinueWatchingMutationServiceTests()
    {
        root = Path.Combine(Path.GetTempPath(), "videoplayer-cwmut-tests", Guid.NewGuid().ToString("N"));
        var layout = new AppStorageLayout(
            backupRoot: Path.Combine(root, "backup"),
            noBackupRoot: Path.Combine(root, "nobackup"),
            cacheRoot: Path.Combine(root, "cache"),
            profile: AppProfile.Create(null));
        layout.EnsureDirectoriesExist();
        var backup = new BackupDatabase(layout);
        backup.InitializeAsync().Wait();
        cache = new CacheDatabase(layout);
        cache.InitializeAsync().Wait();

        var secrets = new AppSecretsService(
            new InMemorySettingsStore(), new InMemorySecretStore(), AppProfile.Create(null));
        authSession = new AuthSessionService(
            secrets,
            uri => new HttpClient(handler, disposeHandler: false) { BaseAddress = uri },
            new RecordingDelays().Delay);
        syncService = new ServerSyncService(authSession, new PendingProgressSyncService(backup), cache);
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

    private ContinueWatchingMutationService CreateService() => new(authSession, syncService, cache);

    private async Task LoginAsync()
    {
        handler.EnqueueJson(HttpStatusCode.OK, new { token = "jwt", expires = "2030-01-01T00:00:00Z" });
        await authSession.LoginAsync(Server, "martin", "pw");
        handler.Requests.Clear();
    }

    /// <summary>Prüft „Ausblenden" online: der Hide-Endpunkt wird aufgerufen und die Cache-Kopie danach vom Serverstand erneuert.</summary>
    [Fact]
    public async Task HideAsync_Online_CallsHideEndpoint_AndRefreshesCache()
    {
        await LoginAsync();
        // Ausgangsbestand in der lokalen Kopie (wie nach einem Sync).
        handler.EnqueueJson(HttpStatusCode.OK, new[]
        {
            new ContinueWatchingEntry { MediaType = "movie", Entry = new Movie { Id = 42, Name = "Film A" }, Title = "Film A" },
            new ContinueWatchingEntry { MediaType = "movie", Entry = new Movie { Id = 50, Name = "Film B" }, Title = "Film B" }
        });
        await syncService.SyncContinueWatchingAsync();
        handler.Requests.Clear();

        // 1. Hide-Request → Server bestätigt „hidden"; 2. Erneuerung der
        //    Kopie vom Serverstand (Eintrag 42 fehlt nun).
        handler.EnqueueJson(HttpStatusCode.OK, new { status = "hidden", message = "ok" });
        handler.EnqueueJson(HttpStatusCode.OK, new[]
        {
            new ContinueWatchingEntry { MediaType = "movie", Entry = new Movie { Id = 50, Name = "Film B" }, Title = "Film B" }
        });

        var result = await CreateService().HideAsync("movie", 42);

        Assert.True(result);
        var requests = handler.Requests;
        Assert.Equal(2, requests.Count);
        Assert.Equal(HttpMethod.Post, requests[0].Method);
        Assert.EndsWith("/api/continue-watching/hide", requests[0].PathAndQuery);
        using (var body = JsonDocument.Parse(requests[0].Body!))
        {
            Assert.Equal("movie", body.RootElement.GetProperty("MediaType").GetString());
            Assert.Equal(42, body.RootElement.GetProperty("MediaId").GetInt64());
        }

        Assert.Equal(HttpMethod.Get, requests[1].Method);
        Assert.EndsWith("/api/continue-watching", requests[1].PathAndQuery);

        var row = await cache.Connection.Table<CachedListEntry>()
            .Where(e => e.Key == ServerSyncService.ContinueWatchingCacheKey)
            .FirstOrDefaultAsync();
        Assert.NotNull(row);
        var entries = JsonSerializer.Deserialize<List<ContinueWatchingEntry>>(row.Json)!;
        var entry = Assert.Single(entries);
        Assert.Equal(50, entry.Entry!.Id);
    }

    /// <summary>Prüft „Überspringen" online: der Skip-Endpunkt wird aufgerufen und die Cache-Kopie danach vom Serverstand erneuert.</summary>
    [Fact]
    public async Task SkipAsync_Online_CallsSkipEndpoint_AndRefreshesCache()
    {
        await LoginAsync();
        handler.EnqueueJson(HttpStatusCode.OK, new[]
        {
            new ContinueWatchingEntry { MediaType = "episode", Entry = new TvShowEpisode { Id = 31, Name = "Pilot" }, Title = "Pilot" }
        });
        await syncService.SyncContinueWatchingAsync();
        handler.Requests.Clear();

        handler.EnqueueJson(HttpStatusCode.OK, new { status = "skipped", message = "ok" });
        handler.EnqueueJson(HttpStatusCode.OK, Array.Empty<ContinueWatchingEntry>());

        var result = await CreateService().SkipAsync("episode", 31);

        Assert.True(result);
        var requests = handler.Requests;
        Assert.Equal(2, requests.Count);
        Assert.Equal(HttpMethod.Post, requests[0].Method);
        Assert.EndsWith("/api/continue-watching/skip", requests[0].PathAndQuery);
        using (var body = JsonDocument.Parse(requests[0].Body!))
        {
            Assert.Equal("episode", body.RootElement.GetProperty("MediaType").GetString());
            Assert.Equal(31, body.RootElement.GetProperty("MediaId").GetInt64());
        }

        var row = await cache.Connection.Table<CachedListEntry>()
            .Where(e => e.Key == ServerSyncService.ContinueWatchingCacheKey)
            .FirstOrDefaultAsync();
        Assert.NotNull(row);
        Assert.Empty(JsonSerializer.Deserialize<List<ContinueWatchingEntry>>(row.Json)!);
    }

    /// <summary>Prüft den Offline-Pfad: ohne Client wird der Eintrag lokal aus dem Cache-Eintrag entfernt — ohne Server-Requests.</summary>
    [Fact]
    public async Task HideAsync_Offline_RemovesEntryFromCacheLocally()
    {
        await SeedCacheAsync(
            new ContinueWatchingEntry { MediaType = "movie", Entry = new Movie { Id = 42, Name = "Film A" }, Title = "Film A" },
            new ContinueWatchingEntry { MediaType = "episode", Entry = new TvShowEpisode { Id = 31, Name = "Pilot" }, Title = "Pilot" });

        var result = await CreateService().HideAsync("movie", 42);

        Assert.True(result);
        Assert.Empty(handler.Requests);

        var entries = await ReadCacheEntriesAsync();
        var entry = Assert.Single(entries);
        Assert.Equal("episode", entry.MediaType);
        Assert.Equal(31, entry.Entry!.Id);
    }

    /// <summary>Prüft den Offline-Typvergleich: die CLR-Konvention des Anzeigetyps trifft den Server-Typ des Cache-Eintrags.</summary>
    [Fact]
    public async Task SkipAsync_Offline_ClrMediaType_MatchesServerEntry()
    {
        await SeedCacheAsync(
            new ContinueWatchingEntry { MediaType = "episode", Entry = new TvShowEpisode { Id = 31, Name = "Pilot" }, Title = "Pilot" });

        var result = await CreateService().SkipAsync("tvshowepisode", 31);

        Assert.True(result);
        Assert.Empty(await ReadCacheEntriesAsync());
    }

    /// <summary>Prüft den Offline-Fehlfall: ohne passenden Eintrag bleibt der Cache unverändert und das Ergebnis ist <c>false</c>.</summary>
    [Fact]
    public async Task HideAsync_Offline_MissingEntry_ReturnsFalse_CacheUnchanged()
    {
        await SeedCacheAsync(
            new ContinueWatchingEntry { MediaType = "movie", Entry = new Movie { Id = 50, Name = "Film B" }, Title = "Film B" });

        var result = await CreateService().HideAsync("movie", 42);

        Assert.False(result);
        var entries = await ReadCacheEntriesAsync();
        var entry = Assert.Single(entries);
        Assert.Equal(50, entry.Entry!.Id);
    }

    /// <summary>Prüft den Offline-Fehlfall: ohne Cache-Eintrag wird <c>false</c> gemeldet und nichts geschrieben.</summary>
    [Fact]
    public async Task HideAsync_Offline_NoCacheRow_ReturnsFalse()
    {
        var result = await CreateService().HideAsync("movie", 42);

        Assert.False(result);
        Assert.Empty(handler.Requests);
        Assert.Empty(await cache.Connection.Table<CachedListEntry>().ToListAsync());
    }

    private async Task SeedCacheAsync(params ContinueWatchingEntry[] entries)
    {
        await cache.Connection.InsertOrReplaceAsync(new CachedListEntry
        {
            Key = ServerSyncService.ContinueWatchingCacheKey,
            Json = JsonSerializer.Serialize(entries),
            UpdatedAtUtc = DateTime.UtcNow
        });
    }

    private async Task<List<ContinueWatchingEntry>> ReadCacheEntriesAsync()
    {
        var row = await cache.Connection.Table<CachedListEntry>()
            .Where(e => e.Key == ServerSyncService.ContinueWatchingCacheKey)
            .FirstOrDefaultAsync();
        Assert.NotNull(row);
        return JsonSerializer.Deserialize<List<ContinueWatchingEntry>>(
            row.Json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }
}
