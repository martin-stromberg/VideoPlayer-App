#pragma warning disable xUnit1051 // Kurzlebige Unit-Tests: TestContext-CancellationToken nicht erforderlich
using System.Net;
using VideoPlayer.Mvu;
using VideoPlayer.Storage;
using VideoPlayer.Storage.Entities;
using VideoWebPlayer.Maui.Tests.Stubs;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>
/// Download-Abgleich (Issue #51, Schritt 1): <see cref="DownloadService.ReconcileAsync"/>
/// entfernt überholte Teildateien, erkennt Restdaten ohne Manifest-Eintrag
/// (ohne sie zu löschen) und korrigiert „fertig"-Einträge ohne Datei —
/// mit echter SQLite-Ablage und Temp-Verzeichnissen.
/// </summary>
[Collection("SqliteDatabase")]
public class DownloadServiceReconcileTests : IDisposable
{
    private static readonly Uri Server = new("http://testserver/videoplayer/");

    private readonly string root;
    private readonly AppStorageLayout layout;
    private readonly StubHttpMessageHandler handler = new();
    private readonly AppSecretsService secrets;
    private readonly AuthSessionService authSession;
    private readonly CacheDatabase cache;
    private readonly FakeNetworkMonitor networkMonitor = new();

    /// <summary>Initialisiert eine neue Instanz der Testklasse <c>DownloadServiceReconcileTests</c>.</summary>
    public DownloadServiceReconcileTests()
    {
        root = Path.Combine(Path.GetTempPath(), "videoplayer-reconcile-tests", Guid.NewGuid().ToString("N"));
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
        for (var attempt = 0; attempt < 30; attempt++)
        {
            try
            {
                if (Directory.Exists(root))
                {
                    Directory.Delete(root, recursive: true);
                }

                return;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Thread.Sleep(100);
            }
        }
    }

    private DownloadService CreateService() => new(
        authSession, secrets, cache, layout, networkMonitor,
        () => new HttpClient(handler, disposeHandler: false) { Timeout = Timeout.InfiniteTimeSpan },
        File.Exists,
        null);

    private async Task LoginAsync()
    {
        handler.EnqueueJson(HttpStatusCode.OK, new { token = "jwt", expires = "2030-01-01T00:00:00Z" });
        await authSession.LoginAsync(Server, "martin", "pw");
        handler.Requests.Clear();
    }

    private async Task<DownloadManifestEntry> InsertEntryAsync(
        string state, string filePath, long mediaId = 42)
    {
        var entry = new DownloadManifestEntry
        {
            VideoId = (int)mediaId,
            VideoType = "movie",
            Title = "Ein Film",
            FilePath = filePath,
            State = state,
            UpdatedAtUtc = DateTime.UtcNow
        };
        await cache.Connection.InsertAsync(entry);
        return entry;
    }

    private async Task<DownloadManifestEntry?> ReadEntryAsync(long mediaId = 42)
    {
        var rows = await cache.Connection.Table<DownloadManifestEntry>()
            .Where(e => e.VideoId == (int)mediaId)
            .ToListAsync();
        return rows.FirstOrDefault(e => e.VideoType == "movie");
    }

    private string WriteFile(string name, int size)
    {
        var path = Path.Combine(layout.VideosDirectory, name);
        File.WriteAllBytes(path, new byte[size]);
        return path;
    }

    /// <summary>Prüft: Datei ohne Manifest-Eintrag wird als Restdaten erkannt und bleibt liegen.</summary>
    [Fact]
    public async Task Reconcile_OrphanedFile_IsCountedAndKept()
    {
        var path = WriteFile("movie_999.mp4", 512);
        var service = CreateService();

        var result = await service.ReconcileAsync();

        Assert.Equal(512, result.OrphanedBytes);
        Assert.Equal(1, result.OrphanedFileCount);
        Assert.Equal(0, result.RemovedPartialCount);
        Assert.True(File.Exists(path));
    }

    /// <summary>Prüft: Teildatei eines fehlgeschlagenen Eintrags wird entfernt.</summary>
    [Fact]
    public async Task Reconcile_StalePartialOfFailedEntry_IsRemoved()
    {
        var path = WriteFile("movie_42.mp4", 256);
        await InsertEntryAsync("failed", path);
        var service = CreateService();

        var result = await service.ReconcileAsync();

        Assert.Equal(256, result.RemovedPartialBytes);
        Assert.Equal(1, result.RemovedPartialCount);
        Assert.False(File.Exists(path));
        Assert.NotNull(await ReadEntryAsync());
    }

    /// <summary>Prüft: Teildatei eines abgebrochenen Eintrags wird entfernt.</summary>
    [Fact]
    public async Task Reconcile_StalePartialOfCancelledEntry_IsRemoved()
    {
        var path = WriteFile("movie_42.mp4", 128);
        await InsertEntryAsync("cancelled", path);
        var service = CreateService();

        var result = await service.ReconcileAsync();

        Assert.Equal(1, result.RemovedPartialCount);
        Assert.False(File.Exists(path));
    }

    /// <summary>Prüft: Teildatei eines „downloading"-Eintrags ohne laufenden Transfer wird entfernt.</summary>
    [Fact]
    public async Task Reconcile_StalePartialOfOrphanedDownloadingEntry_IsRemoved()
    {
        var path = WriteFile("movie_42.mp4", 300);
        await InsertEntryAsync("downloading", path);
        var service = CreateService();

        var result = await service.ReconcileAsync();

        Assert.Equal(1, result.RemovedPartialCount);
        Assert.False(File.Exists(path));
    }

    /// <summary>Prüft: Datei eines fertigen Eintrags bleibt unberührt.</summary>
    [Fact]
    public async Task Reconcile_CompletedFile_IsKept()
    {
        var path = WriteFile("movie_42.mp4", 1024);
        await InsertEntryAsync("completed", path);
        var service = CreateService();

        var result = await service.ReconcileAsync();

        Assert.Equal(0, result.RemovedPartialCount);
        Assert.Equal(0, result.OrphanedFileCount);
        Assert.Equal(0, result.CorrectedCompletedCount);
        Assert.True(File.Exists(path));
        Assert.Equal("completed", (await ReadEntryAsync())!.State);
    }

    /// <summary>Prüft: „fertig"-Eintrag ohne Datei wird auf fehlgeschlagen (missing_file) korrigiert.</summary>
    [Fact]
    public async Task Reconcile_CompletedWithoutFile_MarkedMissing()
    {
        var path = Path.Combine(layout.VideosDirectory, "movie_42.mp4");
        await InsertEntryAsync("completed", path);
        var service = CreateService();

        var result = await service.ReconcileAsync();

        Assert.Equal(1, result.CorrectedCompletedCount);
        var entry = await ReadEntryAsync();
        Assert.Equal("failed", entry!.State);
        Assert.Equal(DownloadService.ErrorMissingFile, entry.Error);
    }

    /// <summary>Prüft: verwaiste Teildatei eines wartenden Eintrags wird entfernt.</summary>
    [Fact]
    public async Task Reconcile_QueuedEntryFile_IsRemoved()
    {
        var path = WriteFile("movie_42.mp4", 64);
        await InsertEntryAsync("queued", path);
        var service = CreateService();

        var result = await service.ReconcileAsync();

        Assert.Equal(1, result.RemovedPartialCount);
        Assert.False(File.Exists(path));
        Assert.Equal("queued", (await ReadEntryAsync())!.State);
    }

    /// <summary>Prüft: die Datei eines tatsächlich laufenden Transfers wird nicht angefasst.</summary>
    [Fact]
    public async Task Reconcile_RunningTransferFile_IsKept()
    {
        await LoginAsync();
        // Nie endende Antwort: der Transfer bleibt aktiv, die Datei entsteht
        // erst nach dem Response-Header — der Lauf bleibt „downloading".
        handler.EnqueueAsyncResponder(async (_, ct) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            return new HttpResponseMessage(HttpStatusCode.OK); // unerreichbar
        });
        var service = CreateService();
        await service.EnqueueAsync("movie", 42, "Ein Film", new AppSettings());

        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(15);
        DownloadManifestEntry? entry;
        do
        {
            entry = await ReadEntryAsync();
            Assert.True(DateTime.UtcNow < deadline, "Transfer wurde nicht rechtzeitig 'downloading'.");
            await Task.Delay(50);
        }
        while (entry!.State != "downloading");

        var result = await service.ReconcileAsync();

        // Der laufende Transfer darf nicht beeinträchtigt werden: keine
        // Entfernung, keine Korrektur am Eintrag.
        Assert.Equal(0, result.RemovedPartialCount);
        Assert.Equal(0, result.CorrectedCompletedCount);
        Assert.Equal("downloading", (await ReadEntryAsync())!.State);
    }

    /// <summary>Prüft: nicht vorhandenes Video-Verzeichnis liefert ein leeres Ergebnis.</summary>
    [Fact]
    public async Task Reconcile_MissingDirectory_ReturnsEmpty()
    {
        Directory.Delete(layout.VideosDirectory, recursive: true);
        var service = CreateService();

        var result = await service.ReconcileAsync();

        Assert.Equal(0, result.RemovedPartialCount);
        Assert.Equal(0, result.OrphanedFileCount);
        Assert.Equal(0, result.CorrectedCompletedCount);
    }

    private sealed class FakeNetworkMonitor : INetworkStatusMonitor
    {
        public bool IsOnline { get; set; } = true;
        public bool IsWifi { get; set; } = true;
        public event EventHandler<bool>? AvailabilityChanged;
        public void Start() { }
        public void SetOnline(bool online)
        {
            IsOnline = online;
            AvailabilityChanged?.Invoke(this, online);
        }
    }
}
