using System.Net;
using System.Text.Json;
using VideoPlayer.ApiClient.Models;
using VideoPlayer.Mvu;
using VideoPlayer.Storage;
using VideoPlayer.Storage.Entities;
using VideoWebPlayer.Maui.Tests.Stubs;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>
/// QuellauflÃ¶sung des Players (Schritt 10): Stream-URL mit Token,
/// Fortsetzungsposition aus Server-Liste bzw. Cache und lokale Dateien
/// aus dem Download-Manifest.
/// </summary>
[Collection("SqliteDatabase")]
public class PlaybackSourceServiceTests : IDisposable
{
    private static readonly Uri Server = new("http://testserver/videoplayer/");

    private readonly string root;
    private readonly AppStorageLayout layout;
    private readonly StubHttpMessageHandler handler = new();
    private readonly AppSecretsService secrets;
    private readonly AuthSessionService authSession;
    private readonly CacheDatabase cache;

    /// <summary>Initialisiert eine neue Instanz der Testklasse <c>PlaybackSourceServiceTests</c>.</summary>
    public PlaybackSourceServiceTests()
    {
        root = Path.Combine(Path.GetTempPath(), "videoplayer-source-tests", Guid.NewGuid().ToString("N"));
        layout = new AppStorageLayout(
            backupRoot: Path.Combine(root, "backup"),
            noBackupRoot: Path.Combine(root, "nobackup"),
            cacheRoot: Path.Combine(root, "cache"),
            profile: AppProfile.Create(null));
        layout.EnsureDirectoriesExist();
        cache = new CacheDatabase(layout);
        cache.InitializeAsync().Wait();

        secrets = new AppSecretsService(
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

    private PlaybackSourceService CreateService(Func<string, bool>? fileExists = null) =>
        new(authSession, secrets, cache, fileExists ?? (_ => false));

    private async Task LoginAsync()
    {
        handler.EnqueueJson(HttpStatusCode.OK, new { token = "jwt", expires = "2030-01-01T00:00:00Z" });
        await authSession.LoginAsync(Server, "martin", "pw", TestContext.Current.CancellationToken);
        handler.Requests.Clear();
    }

    private async Task SeedCachedContinueWatchingAsync(params ContinueWatchingEntry[] entries)
    {
        await cache.Connection.InsertOrReplaceAsync(new CachedListEntry
        {
            Key = ServerSyncService.ContinueWatchingCacheKey,
            Json = JsonSerializer.Serialize(entries),
            UpdatedAtUtc = DateTime.UtcNow
        });
    }

    private async Task SeedDownloadAsync(int videoId, string videoType, string state, string filePath)
    {
        await cache.Connection.InsertAsync(new DownloadManifestEntry
        {
            VideoId = videoId,
            VideoType = videoType,
            State = state,
            FilePath = filePath,
            UpdatedAtUtc = DateTime.UtcNow
        });
    }

    /// <summary>PrÃ¼ft ResolveAsync: bei einem Film online â†’ Stream-URL mit Token-Parameter.</summary>
    [Fact]
    public async Task ResolveAsync_Movie_ReturnsStreamUrlWithToken()
    {
        await LoginAsync();
        handler.EnqueueJson(HttpStatusCode.OK, Array.Empty<ContinueWatchingEntry>());

        var source = await CreateService().ResolveAsync("movie", 42, TestContext.Current.CancellationToken);

        Assert.NotNull(source);
        Assert.Equal(PlaybackSourceKind.ServerStream, source.Kind);
        Assert.Equal("http://testserver/videoplayer/api/items/movie/42/stream?access_token=jwt", source.Location);
        Assert.Null(source.ResumePosition);
    }

    /// <summary>PrÃ¼ft ResolveAsync: bei einer Episode â†’ Fortsetzungsposition der Server-Liste.</summary>
    [Fact]
    public async Task ResolveAsync_Episode_UsesServerResumePosition()
    {
        await LoginAsync();
        handler.EnqueueJson(HttpStatusCode.OK, new[]
        {
            new ContinueWatchingEntry
            {
                MediaType = "episode",
                PositionSeconds = 120,
                Entry = new TvShowEpisode { Id = 31, Name = "Pilot" }
            },
            new ContinueWatchingEntry
            {
                MediaType = "movie",
                PositionSeconds = 999,
                Entry = new Movie { Id = 42, Name = "Film" }
            }
        });

        var source = await CreateService().ResolveAsync("tvshowepisode", 31, TestContext.Current.CancellationToken);

        Assert.NotNull(source);
        Assert.Equal(PlaybackSourceKind.ServerStream, source.Kind);
        Assert.Contains("/api/items/tvshowepisode/31/stream", source.Location, StringComparison.Ordinal);
        Assert.Equal(TimeSpan.FromSeconds(120), source.ResumePosition);
    }

    /// <summary>PrÃ¼ft ResolveAsync: Server-Abruf scheitert â†’ Fortsetzungsposition aus dem Cache.</summary>
    [Fact]
    public async Task ResolveAsync_ServerFails_UsesCachedResumePosition()
    {
        await LoginAsync();
        await SeedCachedContinueWatchingAsync(new ContinueWatchingEntry
        {
            MediaType = "episode",
            PositionSeconds = 77,
            Entry = new TvShowEpisode { Id = 31, Name = "Pilot" }
        });
        // Transportfehler für alle Versuche (Initialversuch + 3 Wiederholungen).
        for (var i = 0; i < 4; i++)
        {
            handler.EnqueueException(new HttpRequestException("offline"));
        }

        var source = await CreateService().ResolveAsync("tvshowepisode", 31, TestContext.Current.CancellationToken);

        Assert.NotNull(source);
        Assert.Equal(TimeSpan.FromSeconds(77), source.ResumePosition);
    }

    /// <summary>PrÃ¼ft ResolveAsync: fertiger Download mit vorhandener Datei â†’ lokale Datei (offline-fÃ¤hig).</summary>
    [Fact]
    public async Task ResolveAsync_CompletedDownload_ReturnsLocalFile()
    {
        // Ohne Anmeldung (kein Client): Fortsetzungsposition kommt aus dem
        // Cache â€” die Quelle ist die lokale Datei, kein Stream nÃ¶tig.
        await SeedCachedContinueWatchingAsync(new ContinueWatchingEntry
        {
            MediaType = "movie",
            PositionSeconds = 300,
            Entry = new Movie { Id = 42, Name = "Film" }
        });
        await SeedDownloadAsync(42, "movie", "completed", @"C:\downloads\film.mp4");

        var source = await CreateService(fileExists: path => path == @"C:\downloads\film.mp4")
            .ResolveAsync("movie", 42, TestContext.Current.CancellationToken);

        Assert.NotNull(source);
        Assert.Equal(PlaybackSourceKind.LocalFile, source.Kind);
        Assert.Equal(@"C:\downloads\film.mp4", source.Location);
        Assert.Equal(TimeSpan.FromSeconds(300), source.ResumePosition);
    }

    /// <summary>PrÃ¼ft ResolveAsync: Download ohne Datei â†’ fÃ¤llt auf den Stream zurÃ¼ck.</summary>
    [Fact]
    public async Task ResolveAsync_MissingFile_FallsBackToStream()
    {
        await LoginAsync();
        handler.EnqueueJson(HttpStatusCode.OK, Array.Empty<ContinueWatchingEntry>());
        await SeedDownloadAsync(42, "movie", "completed", @"C:\downloads\weg.mp4");

        var source = await CreateService(fileExists: _ => false).ResolveAsync("movie", 42, TestContext.Current.CancellationToken);

        Assert.NotNull(source);
        Assert.Equal(PlaybackSourceKind.ServerStream, source.Kind);
    }

    /// <summary>PrÃ¼ft ResolveAsync: nicht fertiger Download â†’ fÃ¤llt auf den Stream zurÃ¼ck.</summary>
    [Fact]
    public async Task ResolveAsync_IncompleteDownload_FallsBackToStream()
    {
        await LoginAsync();
        handler.EnqueueJson(HttpStatusCode.OK, Array.Empty<ContinueWatchingEntry>());
        await SeedDownloadAsync(42, "movie", "downloading", @"C:\downloads\teil.mp4");

        var source = await CreateService(fileExists: _ => true).ResolveAsync("movie", 42, TestContext.Current.CancellationToken);

        Assert.NotNull(source);
        Assert.Equal(PlaybackSourceKind.ServerStream, source.Kind);
    }

    /// <summary>PrÃ¼ft ResolveAsync: weder Datei noch Server â†’ keine Quelle.</summary>
    [Fact]
    public async Task ResolveAsync_NoSource_ReturnsNull()
    {
        var source = await CreateService().ResolveAsync("movie", 42, TestContext.Current.CancellationToken);

        Assert.Null(source);
    }

    /// <summary>PrÃ¼ft ResolveAsync: nicht abspielbarer Typ oder ungÃ¼ltige Id â†’ keine Quelle.</summary>
    [Fact]
    public async Task ResolveAsync_NonPlayable_ReturnsNull()
    {
        var service = CreateService();

        Assert.Null(await service.ResolveAsync("tvshow", 11, TestContext.Current.CancellationToken));
        Assert.Null(await service.ResolveAsync("moviecollection", 7, TestContext.Current.CancellationToken));
        Assert.Null(await service.ResolveAsync("movie", 0, TestContext.Current.CancellationToken));
    }
}

