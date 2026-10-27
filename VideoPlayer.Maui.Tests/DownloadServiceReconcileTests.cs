#pragma warning disable xUnit1051 // Kurzlebige Unit-Tests: TestContext-CancellationToken nicht erforderlich
using System.Net;
using VideoPlayer.Maui.Tests.Stubs;
using VideoPlayer.Mvu;
using VideoPlayer.Storage;
using VideoPlayer.Storage.Entities;

namespace VideoPlayer.Maui.Tests;

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
        string state, string filePath, long mediaId = 42,
        DateTime? expiresAtUtc = null, string? origin = null)
    {
        var entry = new DownloadManifestEntry
        {
            VideoId = (int)mediaId,
            VideoType = "movie",
            Title = "Ein Film",
            FilePath = filePath,
            State = state,
            ExpiresAtUtc = expiresAtUtc,
            Origin = origin,
            UpdatedAtUtc = DateTime.UtcNow
        };
        await cache.Connection.InsertAsync(entry);
        return entry;
    }

    // Die Manifest-Lese-Hilfe liegt gemeinsam in ManifestTestHelpers —
    // der Wrapper reicht die cache-Ablage durch.
    private Task<DownloadManifestEntry?> ReadEntryAsync(long mediaId = 42)
    {
        return ManifestTestHelpers.ReadEntryAsync(cache, "movie", mediaId);
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
        await service.EnqueueAsync(new DownloadEnqueueRequest("movie", 42, "Ein Film"), new AppSettings());

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

    /// <summary>Prüft Gesamtlöschung: alle Dateien und Manifest-Einträge werden entfernt.</summary>
    [Fact]
    public async Task DeleteAll_RemovesFilesAndManifest()
    {
        var path1 = WriteFile("movie_42.mp4", 100);
        var path2 = WriteFile("movie_43.mp4", 200);
        await InsertEntryAsync("completed", path1, 42);
        await InsertEntryAsync("queued", path2, 43);
        var service = CreateService();

        var removed = await service.DeleteAllAsync();

        Assert.Equal(2, removed);
        Assert.False(File.Exists(path1));
        Assert.False(File.Exists(path2));
        Assert.Empty(await cache.Connection.Table<DownloadManifestEntry>().ToListAsync());
    }

    /// <summary>Prüft Restdaten-Bereinigung: nur Dateien ohne Manifest-Bezug werden entfernt.</summary>
    [Fact]
    public async Task PurgeOrphans_RemovesOnlyOrphaned()
    {
        var managed = WriteFile("movie_42.mp4", 100);
        var orphan = WriteFile("movie_999.mp4", 700);
        await InsertEntryAsync("completed", managed, 42);
        var service = CreateService();

        var freed = await service.PurgeOrphansAsync();

        Assert.Equal(700, freed);
        Assert.False(File.Exists(orphan));
        Assert.True(File.Exists(managed));
        Assert.Equal("completed", (await ReadEntryAsync())!.State);
    }

    /// <summary>
    /// Prüft den Container-Umzug (iOS: Update verschiebt den Datencontainer):
    /// der gespeicherte Verzeichnisanteil des Manifest-Pfads ist veraltet,
    /// die Datei liegt unter gleichem Namen im aktuellen Ablageort —
    /// der Eintrag wird geheilt und die Datei nicht als Restdaten gezählt.
    /// </summary>
    [Fact]
    public async Task Reconcile_RelocatedFile_HealsManifestPath()
    {
        var path = WriteFile("movie_42.mp4", 1024);
        var stalePath = Path.Combine(root, "alter-container", "videos", "movie_42.mp4");
        await InsertEntryAsync("completed", stalePath);
        var service = CreateService();

        var result = await service.ReconcileAsync();

        Assert.Equal(0, result.OrphanedFileCount);
        Assert.Equal(0, result.OrphanedBytes);
        Assert.Equal(0, result.CorrectedCompletedCount);
        var entry = await ReadEntryAsync();
        Assert.Equal("completed", entry!.State);
        Assert.Equal(path, entry.FilePath, ignoreCase: true);
    }

    /// <summary>
    /// Prüft: ein fertiger Eintrag mit veraltetem Verzeichnisanteil wird beim
    /// Einreihen als vorhanden erkannt statt erneut eingeplant zu werden.
    /// </summary>
    [Fact]
    public async Task Enqueue_CompletedEntryWithRelocatedFile_NotRequeued()
    {
        await LoginAsync();
        WriteFile("movie_42.mp4", 1024);
        var stalePath = Path.Combine(root, "alter-container", "videos", "movie_42.mp4");
        await InsertEntryAsync("completed", stalePath);
        var service = CreateService();

        var result = await service.EnqueueAsync(new DownloadEnqueueRequest("movie", 42, "Ein Film"), new AppSettings());

        Assert.Equal(DownloadService.NoticeAlreadyPresent, result.Notice);
        Assert.Equal("completed", (await ReadEntryAsync())!.State);
    }

    /// <summary>
    /// Prüft: die Restdaten-Bereinigung löscht keine Datei, deren
    /// Manifest-Eintrag einen veralteten Verzeichnisanteil trägt.
    /// </summary>
    [Fact]
    public async Task PurgeOrphans_RelocatedFile_IsKept()
    {
        var path = WriteFile("movie_42.mp4", 1024);
        var stalePath = Path.Combine(root, "alter-container", "videos", "movie_42.mp4");
        await InsertEntryAsync("completed", stalePath);
        var service = CreateService();

        var freed = await service.PurgeOrphansAsync();

        Assert.Equal(0, freed);
        Assert.True(File.Exists(path));
        Assert.Equal("completed", (await ReadEntryAsync())!.State);
    }

    /// <summary>Prüft Ablauf-Prüfung: abgelaufener Eintrag ohne Kandidatenbezug verliert Datei und Zeile; ItemRemoved wird gemeldet.</summary>
    [Fact]
    public async Task Reconcile_ExpiredCompleted_RemovesFileAndEntry()
    {
        var path = WriteFile("movie_42.mp4", 512);
        await InsertEntryAsync("completed", path, expiresAtUtc: DateTime.UtcNow.AddDays(-1));
        var service = CreateService();
        var removed = new List<DownloadRemovedEventArgs>();
        service.ItemRemoved += (_, e) => removed.Add(e);

        var result = await service.ReconcileAsync(new AppSettings());

        Assert.Equal(1, result.RemovedExpiredCount);
        Assert.Equal(0, result.ExtendedExpiryCount);
        Assert.False(File.Exists(path));
        Assert.Null(await ReadEntryAsync());
        var removal = Assert.Single(removed);
        Assert.Equal(42, removal.MediaId);
        Assert.Equal("movie", removal.MediaType);
    }

    /// <summary>Prüft Ablauf-Prüfung: abgelaufener Kandidat wird verlängert und auf Herkunft „auto" gestellt; ItemChanged wird gemeldet.</summary>
    [Fact]
    public async Task Reconcile_ExpiredCandidate_ExtendsAndSetsAuto()
    {
        var path = WriteFile("movie_42.mp4", 512);
        await InsertEntryAsync("completed", path,
            expiresAtUtc: DateTime.UtcNow.AddDays(-1), origin: "manual");
        var service = CreateService();
        var changed = new List<DownloadListItem>();
        service.ItemChanged += (_, e) => changed.Add(e.Item);
        var settings = new AppSettings { DownloadRetentionDays = 7 };
        var candidateKeys = new HashSet<string> { DownloadService.ManifestKey("movie", 42) };

        var result = await service.ReconcileAsync(settings, candidateKeys);

        Assert.Equal(1, result.ExtendedExpiryCount);
        Assert.Equal(0, result.RemovedExpiredCount);
        Assert.True(File.Exists(path));
        var entry = await ReadEntryAsync();
        Assert.Equal("completed", entry!.State);
        Assert.Equal("auto", entry.Origin);
        var expected = DateTime.UtcNow + TimeSpan.FromDays(7);
        Assert.True(entry.ExpiresAtUtc > DateTime.UtcNow.AddDays(6));
        Assert.True(entry.ExpiresAtUtc <= expected + TimeSpan.FromMinutes(1));
        Assert.Contains(changed, i => i.MediaId == 42 && i.Origin == DownloadOrigin.Auto);
    }

    /// <summary>Prüft Altbestand: Eintrag ohne Ablaufdatum gilt als abgelaufen und wird entfernt.</summary>
    [Fact]
    public async Task Reconcile_LegacyEntryWithoutExpiry_TreatedAsExpired()
    {
        var path = WriteFile("movie_42.mp4", 512);
        await InsertEntryAsync("completed", path);
        var service = CreateService();

        var result = await service.ReconcileAsync(new AppSettings());

        Assert.Equal(1, result.RemovedExpiredCount);
        Assert.False(File.Exists(path));
        Assert.Null(await ReadEntryAsync());
    }

    /// <summary>Prüft Ablauf-Prüfung: nicht abgelaufene Einträge bleiben unberührt.</summary>
    [Fact]
    public async Task Reconcile_ExpiredNonCandidate_QueuedOrRunning_NotTouched()
    {
        var queuedPath = WriteFile("movie_43.mp4", 64);
        var completedPath = WriteFile("movie_42.mp4", 512);
        // Wartender Eintrag ohne Ablaufdatum (Ablauf gilt nur für „completed").
        await InsertEntryAsync("queued", queuedPath, 43);
        // Fertiger Eintrag mit Ablaufdatum in der Zukunft.
        await InsertEntryAsync("completed", completedPath, 42,
            expiresAtUtc: DateTime.UtcNow.AddDays(5));
        var service = CreateService();

        var result = await service.ReconcileAsync(new AppSettings());

        Assert.Equal(0, result.RemovedExpiredCount);
        Assert.Equal(0, result.ExtendedExpiryCount);
        Assert.Equal("queued", (await ReadEntryAsync(43))!.State);
        var completed = await ReadEntryAsync(42);
        Assert.Equal("completed", completed!.State);
        Assert.NotNull(completed.ExpiresAtUtc);
        Assert.True(File.Exists(completedPath));
    }

    /// <summary>Prüft Rückwärtskompatibilität: ohne Settings läuft kein Ablauf-Pass — abgelaufene Einträge bleiben.</summary>
    [Fact]
    public async Task Reconcile_WithoutSettings_SkipsExpiryPass()
    {
        var path = WriteFile("movie_42.mp4", 512);
        await InsertEntryAsync("completed", path, expiresAtUtc: DateTime.UtcNow.AddDays(-1));
        var service = CreateService();

        var result = await service.ReconcileAsync();

        Assert.Equal(0, result.RemovedExpiredCount);
        Assert.True(File.Exists(path));
        Assert.Equal("completed", (await ReadEntryAsync())!.State);
    }
}
