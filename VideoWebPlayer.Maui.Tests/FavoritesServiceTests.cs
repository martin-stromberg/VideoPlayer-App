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
/// Favoriten-Umschaltung der Detailseite (Schritt 8): die Änderung geht
/// an den Server (führendes System) und die lokale Kopie
/// <c>cached_favorites</c> wird danach vom Serverstand erneuert — damit
/// stimmen Favoriten-Liste und Detail-Markierung auch nach Neustart.
/// </summary>
[Collection("SqliteDatabase")]
public class FavoritesServiceTests : IDisposable
{
    private static readonly Uri Server = new("http://testserver/videoplayer/");

    private readonly string root;
    private readonly CacheDatabase cache;
    private readonly StubHttpMessageHandler handler = new();
    private readonly AuthSessionService authSession;
    private readonly ServerSyncService syncService;

    /// <summary>Initialisiert eine neue Instanz der Testklasse <c>FavoritesServiceTests</c>.</summary>
    public FavoritesServiceTests()
    {
        root = Path.Combine(Path.GetTempPath(), "videoplayer-fav-tests", Guid.NewGuid().ToString("N"));
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

    private FavoritesService CreateService() => new(authSession, syncService);

    private async Task LoginAsync()
    {
        handler.EnqueueJson(HttpStatusCode.OK, new { token = "jwt", expires = "2030-01-01T00:00:00Z" });
        await authSession.LoginAsync(Server, "martin", "pw");
        handler.Requests.Clear();
    }

    /// <summary>Prüft Toggle: bei <c>SendsEntry</c> → sendet den Eintrag und aktualisiert die gecachten Favoriten.</summary>
    [Fact]
    public async Task Toggle_SendsEntry_AndRefreshesCachedFavorites()
    {
        await LoginAsync();
        // 1. Toggle-Request → Server bestätigt „jetzt Favorit".
        handler.EnqueueJson(HttpStatusCode.OK, true);
        // 2. Danach wird die lokale Kopie vom Serverstand erneuert.
        handler.EnqueueJson(HttpStatusCode.OK, new[]
        {
            new FavoriteEntry { Id = 9, Entry = new TvShow { Id = 11, Name = "Serie" } }
        });

        var result = await CreateService().ToggleAsync("tvshow", 11, "Serie");

        Assert.True(result);
        var requests = handler.Requests;
        Assert.Equal(2, requests.Count);
        Assert.Equal(HttpMethod.Post, requests[0].Method);
        Assert.EndsWith("/api/favorites/toggle", requests[0].PathAndQuery);
        // Der Request trägt den typkorrekten Eintrag ($type der Serialisierung).
        using var body = JsonDocument.Parse(requests[0].Body!);
        Assert.Equal(11, body.RootElement.GetProperty("Id").GetInt64());
        Assert.EndsWith("/api/favorites", requests[1].PathAndQuery);

        var cached = await cache.Connection.Table<CachedFavoritesEntry>().ToListAsync();
        var entry = Assert.Single(cached);
        Assert.Equal("tvshow:11", entry.Key);
        Assert.Equal(11, entry.VideoId);
    }

    /// <summary>Prüft Toggle: bei <c>RemovedFavorite</c> → leert die Cache-Kopie.</summary>
    [Fact]
    public async Task Toggle_RemovedFavorite_EmptiesCacheCopy()
    {
        await LoginAsync();
        handler.EnqueueJson(HttpStatusCode.OK, false);
        handler.EnqueueJson(HttpStatusCode.OK, Array.Empty<FavoriteEntry>());

        var result = await CreateService().ToggleAsync("movie", 42, "Film");

        Assert.False(result);
        Assert.Empty(await cache.Connection.Table<CachedFavoritesEntry>().ToListAsync());
    }

    /// <summary>Prüft Toggle: ohne Client → gibt null zurück und fordert nichts an.</summary>
    [Fact]
    public async Task Toggle_WithoutClient_ReturnsNull_NoRequests()
    {
        var result = await CreateService().ToggleAsync("movie", 42, "Film");

        Assert.Null(result);
        Assert.Empty(handler.Requests);
    }

    /// <summary>Prüft CreateEntry: bildet auf den korrekten Eintragstyp ab.</summary>
    /// <param name="mediaType">Der Medientyp.</param>
    /// <param name="expected">Der erwartete Wert.</param>
    [Theory]
    [InlineData("movie", typeof(Movie))]
    [InlineData("moviecollection", typeof(MovieCollection))]
    [InlineData("tvshow", typeof(TvShow))]
    [InlineData("tvshowseason", typeof(TvShowSeason))]
    [InlineData("tvshowepisode", typeof(TvShowEpisode))]
    [InlineData("anders", typeof(MediaEntry))]
    public void CreateEntry_MapsToCorrectEntryType(string mediaType, Type expected)
    {
        var entry = FavoritesService.CreateEntry(mediaType, 5, "Name");

        Assert.IsType(expected, entry);
        Assert.Equal(5, entry.Id);
        Assert.Equal("Name", entry.Name);
    }
}
