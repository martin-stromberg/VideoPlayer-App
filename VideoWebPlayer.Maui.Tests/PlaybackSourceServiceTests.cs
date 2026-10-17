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
        new(authSession, secrets, cache, layout, fileExists ?? (_ => false));

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
        var localPath = Path.Combine(layout.VideosDirectory, "film.mp4");
        await SeedDownloadAsync(42, "movie", "completed", localPath);

        var source = await CreateService(fileExists: path => path == localPath)
            .ResolveAsync("movie", 42, TestContext.Current.CancellationToken);

        Assert.NotNull(source);
        Assert.Equal(PlaybackSourceKind.LocalFile, source.Kind);
        Assert.Equal(localPath, source.Location);
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

    /// <summary>PrÃ¼ft ResolveAsync: im Offline-Modus (allowServerStream=false) wird die lokale Datei ohne Server-Aufruf geliefert.</summary>
    [Fact]
    public async Task ResolveAsync_OfflineMode_ReturnsLocalFileWithoutServerCall()
    {
        // Angemeldeter Client vorhanden — der Offline-Pfad darf trotzdem
        // keinen Server-Request (Weiterschauen-Liste) auslösen.
        await LoginAsync();
        await SeedCachedContinueWatchingAsync(new ContinueWatchingEntry
        {
            MediaType = "episode",
            PositionSeconds = 45,
            Entry = new TvShowEpisode { Id = 31, Name = "Pilot" }
        });
        await SeedDownloadAsync(31, "tvshowepisode", "completed", @"C:\downloads\ep31.mp4");

        var source = await CreateService(fileExists: _ => true)
            .ResolveAsync("tvshowepisode", 31, TestContext.Current.CancellationToken,
                allowServerStream: false);

        Assert.NotNull(source);
        Assert.Equal(PlaybackSourceKind.LocalFile, source.Kind);
        Assert.Equal(TimeSpan.FromSeconds(45), source.ResumePosition);
        Assert.Empty(handler.Requests);
    }

    /// <summary>PrÃ¼ft ResolveAsync: im Offline-Modus ohne lokale Datei wird keine Stream-URL erzeugt.</summary>
    [Fact]
    public async Task ResolveAsync_OfflineModeWithoutLocalFile_ReturnsNull()
    {
        await LoginAsync();
        await SeedCachedContinueWatchingAsync(new ContinueWatchingEntry
        {
            MediaType = "movie",
            PositionSeconds = 10,
            Entry = new Movie { Id = 42, Name = "Film" }
        });

        var source = await CreateService(fileExists: _ => false)
            .ResolveAsync("movie", 42, TestContext.Current.CancellationToken,
                allowServerStream: false);

        Assert.Null(source);
        Assert.Empty(handler.Requests);
    }

    /// <summary>Prüft ResolveAsync: ein Fortsetzungs-Override (Playlist-Antwort) ersetzt die Weiterschauen-Auflösung ohne Server-Abruf — auch bei 0 Sekunden.</summary>
    [Fact]
    public async Task ResolveAsync_ResumeOverride_SkipsContinueWatchingLookup()
    {
        await LoginAsync();

        var source = await CreateService().ResolveAsync(
            "movie", 42, TestContext.Current.CancellationToken,
            resumePositionSecondsOverride: 90);

        Assert.NotNull(source);
        Assert.Equal(PlaybackSourceKind.ServerStream, source.Kind);
        Assert.Equal(TimeSpan.FromSeconds(90), source.ResumePosition);
        // Die Weiterschauen-Liste wurde nicht abgefragt.
        Assert.Empty(handler.Requests);

        // Auch 0 Sekunden ist ein verbindlicher Wert (Beginn am Anfang).
        var fromStart = await CreateService().ResolveAsync(
            "movie", 42, TestContext.Current.CancellationToken,
            resumePositionSecondsOverride: 0);

        Assert.NotNull(fromStart);
        Assert.Equal(TimeSpan.Zero, fromStart.ResumePosition);
    }

    /// <summary>Prüft ResolveAsync: der Protokolleintrag trägt Medientyp/-Id und Quellart — nie die Quell-Adresse (access_token).</summary>
    [Fact]
    public async Task ResolveAsync_LogsKind_NotLocation()
    {
        var logging = new RecordingLoggerProvider();
        await LoginAsync();
        handler.EnqueueJson(HttpStatusCode.OK, Array.Empty<ContinueWatchingEntry>());
        var service = new PlaybackSourceService(
            authSession, secrets, cache, layout, _ => false, logging.CreateLogger<PlaybackSourceService>());

        var source = await service.ResolveAsync("movie", 42, TestContext.Current.CancellationToken);

        Assert.NotNull(source);
        var entry = Assert.Single(logging.Entries, e =>
            e.Category.Contains(nameof(PlaybackSourceService), StringComparison.Ordinal));
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Contains("movie/42", entry.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(PlaybackSourceKind.ServerStream), entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("access_token", entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(source.Location, entry.Message, StringComparison.Ordinal);
    }

    /// <summary>Prüft ResolveAsync: weder Datei noch Server → keine Quelle.</summary>
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

