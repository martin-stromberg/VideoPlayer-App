#pragma warning disable xUnit1051 // Kurzlebige Unit-Tests: TestContext-CancellationToken nicht erforderlich
using System.Net;
using Microsoft.Extensions.Logging;
using VideoPlayer.Mvu;
using VideoPlayer.Storage;
using VideoPlayer.Storage.Entities;
using VideoWebPlayer.Maui.Tests.Stubs;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>
/// Download-Engine (Schritt 11): Einreihen/Dedup, parallele Transfers,
/// Fortschritt, Abbrechen, Löschen, Quota-Durchsetzung, WLAN-Bedingung
/// und automatische Wiederaufnahme aus dem persistenten Manifest —
/// mit echter SQLite-Ablage und Temp-Verzeichnissen.
/// </summary>
[Collection("SqliteDatabase")]
public class DownloadServiceTests : IDisposable
{
    private static readonly Uri Server = new("http://testserver/videoplayer/");

    private readonly string root;
    private readonly AppStorageLayout layout;
    private readonly StubHttpMessageHandler handler = new();
    private readonly AppSecretsService secrets;
    private readonly AuthSessionService authSession;
    private readonly CacheDatabase cache;
    private readonly FakeNetworkMonitor networkMonitor = new();

    /// <summary>Initialisiert eine neue Instanz der Testklasse <c>DownloadServiceTests</c>.</summary>
    public DownloadServiceTests()
    {
        root = Path.Combine(Path.GetTempPath(), "videoplayer-download-tests", Guid.NewGuid().ToString("N"));
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
        // Hintergrund-Transfers können Datei-/DB-Handles kurz halten.
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

    private DownloadService CreateService(long? quotaBytesOverride = null) => new(
        authSession, secrets, cache, layout, networkMonitor,
        () => new HttpClient(handler, disposeHandler: false) { Timeout = Timeout.InfiniteTimeSpan },
        File.Exists,
        quotaBytesOverride);

    private static AppSettings Settings(bool wifiOnly = false, bool autoDelete = false) => new()
    {
        WifiOnlyDownloads = wifiOnly,
        AutoDeleteWatchedEpisodes = autoDelete
    };

    private async Task LoginAsync()
    {
        handler.EnqueueJson(HttpStatusCode.OK, new { token = "jwt", expires = "2030-01-01T00:00:00Z" });
        await authSession.LoginAsync(Server, "martin", "pw");
        handler.Requests.Clear();
    }

    private void EnqueueStream(long size = 64)
    {
        handler.Enqueue(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(new byte[size])
        });
    }

    private async Task<DownloadManifestEntry?> ReadEntryAsync(string type, long mediaId)
    {
        var rows = await cache.Connection.Table<DownloadManifestEntry>()
            .Where(e => e.VideoId == (int)mediaId)
            .ToListAsync();
        return rows.FirstOrDefault(e => e.VideoType == type);
    }

    private static async Task<DownloadManifestEntry> WaitForStateAsync(
        Func<Task<DownloadManifestEntry?>> read, string state, int timeoutMs = 15000)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromMilliseconds(timeoutMs);
        while (true)
        {
            var entry = await read();
            if (entry is not null && entry.State == state)
            {
                return entry;
            }

            Assert.True(DateTime.UtcNow < deadline, $"Zeitüberschreitung beim Warten auf Zustand '{state}'.");
            await Task.Delay(50);
        }
    }

    /// <summary>Prüft Enqueue: Download läuft durch — Datei liegt vor, Manifest steht auf completed.</summary>
    [Fact]
    public async Task Enqueue_Completes_AndWritesFileAndManifest()
    {
        await LoginAsync();
        EnqueueStream(128);
        var service = CreateService();
        var changed = new List<DownloadListItem>();
        service.ItemChanged += (_, e) => changed.Add(e.Item);

        var result = await service.EnqueueAsync("movie", 42, "Ein Film", Settings());
        Assert.Equal(DownloadService.NoticeEnqueued, result.Notice);
        Assert.Equal(DownloadState.Queued, result.Item!.State);

        var entry = await WaitForStateAsync(() => ReadEntryAsync("movie", 42), "completed");
        Assert.Equal(128, entry.SizeBytes);
        Assert.True(File.Exists(entry.FilePath));
        Assert.Equal(Path.Combine(layout.VideosDirectory, "movie_42.mp4"), entry.FilePath);
        Assert.Equal("Ein Film", entry.Title);
        Assert.Contains(changed, i => i.State == DownloadState.Downloading);
        Assert.Equal(DownloadState.Completed, changed.Last().State);
    }

    /// <summary>Prüft die Protokolleinträge: Auftragseingang und Transfer-Abschluss tragen Medientyp/-Id und Anzeigetitel.</summary>
    [Fact]
    public async Task EnqueueAsync_TransferOutcome_Logged()
    {
        var logging = new RecordingLoggerProvider();
        await LoginAsync();
        EnqueueStream(64);
        var service = new DownloadService(
            authSession, secrets, cache, layout, networkMonitor,
            () => new HttpClient(handler, disposeHandler: false) { Timeout = Timeout.InfiniteTimeSpan },
            File.Exists, null,
            logging.CreateLogger<DownloadService>());

        var result = await service.EnqueueAsync("movie", 42, "Ein Film", Settings());
        Assert.Equal(DownloadService.NoticeEnqueued, result.Notice);
        await WaitForStateAsync(() => ReadEntryAsync("movie", 42), "completed");

        // Eintrag des Auftragseingangs: Medientyp/-Id, Titel und Notice.
        Assert.Contains(logging.Entries, e =>
            e.Level == LogLevel.Information
            && e.Category.Contains(nameof(DownloadService), StringComparison.Ordinal)
            && e.Message.Contains("movie/42", StringComparison.Ordinal)
            && e.Message.Contains("Ein Film", StringComparison.Ordinal)
            && e.Message.Contains(DownloadService.NoticeEnqueued, StringComparison.Ordinal));
        // Eintrag des Zustandswechsels auf abgeschlossen.
        Assert.Contains(logging.Entries, e =>
            e.Level == LogLevel.Information
            && e.Category.Contains(nameof(DownloadService), StringComparison.Ordinal)
            && e.Message.Contains("movie/42", StringComparison.Ordinal)
            && e.Message.Contains("Completed", StringComparison.Ordinal));
    }

    /// <summary>
    /// Prüft die Protokollierung des Einreih-Ergebnisses: ein Dedup-
    /// Hinweis ohne Zustandsänderung („wartet bereits") erzeugt keinen
    /// Information-Eintrag — Auto-Download-Läufe prüfen pro Kandidat
    /// und würden sonst pro Eintrag loggen, ohne dass etwas geschah.
    /// </summary>
    [Fact]
    public async Task EnqueueAsync_DedupNotice_LogsBelowInformation()
    {
        var logging = new RecordingLoggerProvider();
        await LoginAsync();
        // WLAN-Bedingung hält den Auftrag wartend — die zweite
        // Einplanung trifft die Dedup.
        networkMonitor.IsWifi = false;
        var service = new DownloadService(
            authSession, secrets, cache, layout, networkMonitor,
            () => new HttpClient(handler, disposeHandler: false) { Timeout = Timeout.InfiniteTimeSpan },
            File.Exists, null,
            logging.CreateLogger<DownloadService>());
        var settings = Settings(wifiOnly: true);
        await service.EnqueueAsync("movie", 42, "Ein Film", settings);

        var result = await service.EnqueueAsync("movie", 42, "Ein Film", settings);

        Assert.Equal(DownloadService.NoticeAlreadyQueued, result.Notice);
        Assert.DoesNotContain(logging.Entries, e =>
            e.Level == LogLevel.Information
            && e.Category.Contains(nameof(DownloadService), StringComparison.Ordinal)
            && e.Message.Contains(DownloadService.NoticeAlreadyQueued, StringComparison.Ordinal));
    }

    /// <summary>
    /// Prüft die Protokollierung des Quota-Pfads: der Zustandswechsel
    /// auf „failed" trägt das Ereignis bereits — kein zweiter
    /// Warn-Eintrag mit dem Hinweistext für denselben Vorgang.
    /// </summary>
    [Fact]
    public async Task EnqueueAsync_QuotaExceeded_LogsSingleWarning()
    {
        var logging = new RecordingLoggerProvider();
        await LoginAsync();
        var occupying = Path.Combine(layout.VideosDirectory, "belegt.bin");
        await File.WriteAllBytesAsync(occupying, new byte[200]);
        var service = new DownloadService(
            authSession, secrets, cache, layout, networkMonitor,
            () => new HttpClient(handler, disposeHandler: false) { Timeout = Timeout.InfiniteTimeSpan },
            File.Exists, 100,
            logging.CreateLogger<DownloadService>());

        var result = await service.EnqueueAsync("movie", 42, "Ein Film", Settings());

        Assert.True(result.IsError);
        Assert.Equal(DownloadService.NoticeQuotaExceeded, result.Notice);
        Assert.Single(logging.Entries, e =>
            e.Level == LogLevel.Warning
            && e.Category.Contains(nameof(DownloadService), StringComparison.Ordinal)
            && e.Message.Contains("movie/42", StringComparison.Ordinal));
    }

    /// <summary>Prüft Enqueue: der Serienkontext einer Episode landet im Manifest.</summary>
    [Fact]
    public async Task Enqueue_WithSeriesContext_PersistsContextInManifest()
    {
        await LoginAsync();
        EnqueueStream(32);
        var service = CreateService();

        await service.EnqueueAsync(
            "tvshowepisode", 31, "Serie – Pilot", Settings(),
            seriesContext: new DownloadSeriesContext(
                ShowId: 11,
                ShowTitle: "Serie",
                SeasonNumber: 1,
                EpisodeNumber: 1));

        var entry = await WaitForStateAsync(() => ReadEntryAsync("tvshowepisode", 31), "completed");
        Assert.Equal(11, entry.ShowId);
        Assert.Equal("Serie", entry.ShowTitle);
        Assert.Equal(1, entry.SeasonNumber);
        Assert.Equal(1, entry.EpisodeNumber);
    }

    /// <summary>Prüft Enqueue: abgeschlossener Download mit Datei wird dedupliziert.</summary>
    [Fact]
    public async Task Enqueue_CompletedExisting_ReturnsAlreadyPresent()
    {
        await LoginAsync();
        EnqueueStream();
        var service = CreateService();
        await service.EnqueueAsync("movie", 42, "Ein Film", Settings());
        await WaitForStateAsync(() => ReadEntryAsync("movie", 42), "completed");
        handler.Requests.Clear();

        var result = await service.EnqueueAsync("movie", 42, "Ein Film", Settings());

        Assert.Equal(DownloadService.NoticeAlreadyPresent, result.Notice);
        Assert.Empty(handler.Requests);
    }

    /// <summary>Prüft Enqueue: wartender Auftrag wird nicht doppelt angelegt.</summary>
    [Fact]
    public async Task Enqueue_QueuedExisting_ReturnsAlreadyQueued()
    {
        await LoginAsync();
        // WLAN-only blockiert den Start — der Auftrag bleibt wartend.
        networkMonitor.IsWifi = false;
        var service = CreateService();
        var settings = Settings(wifiOnly: true);
        await service.EnqueueAsync("movie", 42, "Ein Film", settings);

        var result = await service.EnqueueAsync("movie", 42, "Ein Film", settings);

        Assert.Equal(DownloadService.NoticeAlreadyQueued, result.Notice);
        var items = await service.LoadItemsAsync();
        Assert.Single(items);
    }

    /// <summary>Prüft Engine: zwei Downloads laufen parallel (max. <c>MaxParallelDownloads</c>).</summary>
    [Fact]
    public async Task Enqueue_TwoDownloads_RunInParallel()
    {
        await LoginAsync();
        var started = new CountdownEvent(DownloadService.MaxParallelDownloads);
        var release = new ManualResetEventSlim(false);
        handler.Enqueue(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new GatedContent(started, release, new byte[32])
        });
        handler.Enqueue(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new GatedContent(started, release, new byte[32])
        });

        var service = CreateService();
        await service.EnqueueAsync("movie", 1, "Eins", Settings());
        await service.EnqueueAsync("movie", 2, "Zwei", Settings());

        // Beide Transfers müssen gleichzeitig gestartet sein — ein
        // serieller Ablauf liefe in das Zeitlimit.
        Assert.True(started.Wait(TimeSpan.FromSeconds(15)));
        release.Set();
        await WaitForStateAsync(() => ReadEntryAsync("movie", 1), "completed");
        await WaitForStateAsync(() => ReadEntryAsync("movie", 2), "completed");
    }

    /// <summary>Prüft Quota: belegtes Kontingent → Eintrag schlägt fehl, kein Download.</summary>
    [Fact]
    public async Task Enqueue_QuotaExhausted_FailsWithQuotaError()
    {
        await LoginAsync();
        var occupying = Path.Combine(layout.VideosDirectory, "belegt.bin");
        await File.WriteAllBytesAsync(occupying, new byte[200]);
        var service = CreateService(quotaBytesOverride: 100);

        var result = await service.EnqueueAsync("movie", 42, "Ein Film", Settings());

        Assert.True(result.IsError);
        Assert.Equal(DownloadService.NoticeQuotaExceeded, result.Notice);
        Assert.Equal(DownloadState.Failed, result.Item!.State);
        Assert.Equal(DownloadService.ErrorQuotaExceeded, result.Item.Error);
        Assert.Equal(DownloadService.ErrorQuotaExceeded, (await ReadEntryAsync("movie", 42))!.Error);
        Assert.Empty(handler.Requests);
    }

    /// <summary>Prüft Quota: Überschreitung während der Übertragung → fehlgeschlagen, Teildatei entfernt.</summary>
    [Fact]
    public async Task Enqueue_QuotaExceededDuringTransfer_FailsAndRemovesPartial()
    {
        await LoginAsync();
        EnqueueStream(200);
        var service = CreateService(quotaBytesOverride: 100);

        await service.EnqueueAsync("movie", 42, "Ein Film", Settings());
        var entry = await WaitForStateAsync(() => ReadEntryAsync("movie", 42), "failed");

        Assert.Equal(DownloadService.ErrorQuotaExceeded, entry.Error);
        Assert.False(File.Exists(entry.FilePath));
    }

    /// <summary>Prüft Abbrechen: laufender Download → cancelled, Teildatei entfernt.</summary>
    [Fact]
    public async Task Cancel_RunningDownload_MarksCancelled()
    {
        await LoginAsync();
        // Nie endende Antwort: der Transfer bleibt im Send hängen, bis
        // der Abbruch den ausstehenden Request über das Token auflöst.
        handler.EnqueueAsyncResponder(async (_, ct) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            return new HttpResponseMessage(HttpStatusCode.OK); // unerreichbar
        });
        var service = CreateService();
        await service.EnqueueAsync("movie", 42, "Ein Film", Settings());
        await WaitForStateAsync(() => ReadEntryAsync("movie", 42), "downloading");

        await service.CancelAsync("movie", 42);

        var entry = await WaitForStateAsync(() => ReadEntryAsync("movie", 42), "cancelled");
        Assert.False(File.Exists(entry.FilePath));
    }

    /// <summary>Prüft Abbrechen: wartender Download (WLAN-only ohne WLAN) → cancelled.</summary>
    [Fact]
    public async Task Cancel_QueuedDownload_MarksCancelled()
    {
        await LoginAsync();
        networkMonitor.IsWifi = false;
        var service = CreateService();
        await service.EnqueueAsync("movie", 42, "Ein Film", Settings(wifiOnly: true));

        await service.CancelAsync("movie", 42);

        var entry = await ReadEntryAsync("movie", 42);
        Assert.Equal("cancelled", entry!.State);
    }

    /// <summary>Prüft WLAN-only: ohne WLAN bleibt der Auftrag wartend (kein Request).</summary>
    [Fact]
    public async Task Enqueue_WifiOnlyWithoutWifi_StaysQueued()
    {
        await LoginAsync();
        networkMonitor.IsWifi = false;
        var service = CreateService();

        await service.EnqueueAsync("movie", 42, "Ein Film", Settings(wifiOnly: true));
        await Task.Delay(300);

        var entry = await ReadEntryAsync("movie", 42);
        Assert.Equal("queued", entry!.State);
        Assert.Empty(handler.Requests);
    }

    /// <summary>Prüft WLAN-Rückkehr: wartender Download startet bei WLAN.</summary>
    [Fact]
    public async Task Resume_WifiOnly_WifiAvailable_StartsDownload()
    {
        await LoginAsync();
        networkMonitor.IsWifi = false;
        var service = CreateService();
        var settings = Settings(wifiOnly: true);
        await service.EnqueueAsync("movie", 42, "Ein Film", settings);
        await Task.Delay(200);

        networkMonitor.IsWifi = true;
        EnqueueStream();
        await service.ResumeInterruptedAsync(settings);

        await WaitForStateAsync(() => ReadEntryAsync("movie", 42), "completed");
    }

    /// <summary>Prüft Wiederaufnahme: verwaister downloading-Eintrag (App-Neustart) wird erneut eingeplant.</summary>
    [Fact]
    public async Task Resume_OrphanedDownloadingEntry_RestartsDownload()
    {
        await LoginAsync();
        // App-Neustart-Simulation: Manifest-Eintrag „downloading" ohne
        // laufenden Transfer (frischer Dienst über derselben Datenbank).
        var path = Path.Combine(layout.VideosDirectory, "movie_42.mp4");
        await cache.Connection.InsertAsync(new DownloadManifestEntry
        {
            VideoId = 42,
            VideoType = "movie",
            Title = "Ein Film",
            FilePath = path,
            State = "downloading",
            UpdatedAtUtc = DateTime.UtcNow
        });
        var service = CreateService();
        EnqueueStream(96);

        await service.ResumeInterruptedAsync(Settings());

        var entry = await WaitForStateAsync(() => ReadEntryAsync("movie", 42), "completed");
        Assert.Equal(96, entry.SizeBytes);
        Assert.True(File.Exists(entry.FilePath));
        var streamRequest = Assert.Single(handler.Requests);
        Assert.Contains("api/items/movie/42/stream", streamRequest.PathAndQuery);
        Assert.Contains("access_token=jwt", streamRequest.PathAndQuery);
    }

    /// <summary>Prüft Wiederaufnahme: abgebrochene Downloads bleiben unberührt.</summary>
    [Fact]
    public async Task Resume_CancelledEntry_StaysUntouched()
    {
        await LoginAsync();
        await cache.Connection.InsertAsync(new DownloadManifestEntry
        {
            VideoId = 42,
            VideoType = "movie",
            Title = "Ein Film",
            FilePath = Path.Combine(layout.VideosDirectory, "movie_42.mp4"),
            State = "cancelled",
            UpdatedAtUtc = DateTime.UtcNow
        });
        var service = CreateService();

        await service.ResumeInterruptedAsync(Settings());
        await Task.Delay(300);

        var entry = await ReadEntryAsync("movie", 42);
        Assert.Equal("cancelled", entry!.State);
        Assert.Empty(handler.Requests);
    }

    /// <summary>Prüft Wiederaufnahme: Quota-Fehlschläge bleiben unberührt.</summary>
    [Fact]
    public async Task Resume_QuotaFailedEntry_StaysUntouched()
    {
        await LoginAsync();
        await cache.Connection.InsertAsync(new DownloadManifestEntry
        {
            VideoId = 42,
            VideoType = "movie",
            Title = "Ein Film",
            FilePath = Path.Combine(layout.VideosDirectory, "movie_42.mp4"),
            State = "failed",
            Error = DownloadService.ErrorQuotaExceeded,
            UpdatedAtUtc = DateTime.UtcNow
        });
        var service = CreateService();

        await service.ResumeInterruptedAsync(Settings());
        await Task.Delay(300);

        var entry = await ReadEntryAsync("movie", 42);
        Assert.Equal("failed", entry!.State);
        Assert.Empty(handler.Requests);
    }

    /// <summary>Prüft Fehlerbehandlung: HTTP-Fehler → failed mit Fehlercode.</summary>
    [Fact]
    public async Task Enqueue_ServerError_FailsWithHttpError()
    {
        await LoginAsync();
        handler.Enqueue(HttpStatusCode.InternalServerError);
        var service = CreateService();

        await service.EnqueueAsync("movie", 42, "Ein Film", Settings());
        var entry = await WaitForStateAsync(() => ReadEntryAsync("movie", 42), "failed");

        Assert.Equal("http_500", entry.Error);
        Assert.False(File.Exists(entry.FilePath));
    }

    /// <summary>Prüft Fehlerbehandlung: Transportfehler → failed mit Netzwerk-Code.</summary>
    [Fact]
    public async Task Enqueue_TransportError_FailsWithNetworkError()
    {
        await LoginAsync();
        handler.EnqueueException(new HttpRequestException("verbindungsfehler"));
        var service = CreateService();

        await service.EnqueueAsync("movie", 42, "Ein Film", Settings());
        var entry = await WaitForStateAsync(() => ReadEntryAsync("movie", 42), "failed");

        Assert.Equal(DownloadService.ErrorNetwork, entry.Error);
    }

    /// <summary>Prüft Löschen: Datei und Manifest-Zeile werden entfernt, ItemRemoved gemeldet.</summary>
    [Fact]
    public async Task Delete_Completed_RemovesFileAndEntry()
    {
        await LoginAsync();
        EnqueueStream();
        var service = CreateService();
        var removed = new List<DownloadRemovedEventArgs>();
        service.ItemRemoved += (_, e) => removed.Add(e);
        await service.EnqueueAsync("movie", 42, "Ein Film", Settings());
        var entry = await WaitForStateAsync(() => ReadEntryAsync("movie", 42), "completed");

        var deleted = await service.DeleteAsync("movie", 42);

        Assert.True(deleted);
        Assert.False(File.Exists(entry.FilePath));
        Assert.Null(await ReadEntryAsync("movie", 42));
        var removal = Assert.Single(removed);
        Assert.Equal(42, removal.MediaId);
        Assert.Equal("movie", removal.MediaType);
    }

    /// <summary>Prüft Löschen: laufender Download wird abgebrochen und entfernt.</summary>
    [Fact]
    public async Task Delete_RunningDownload_RemovesEntry()
    {
        await LoginAsync();
        handler.EnqueueAsyncResponder(async (_, ct) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            return new HttpResponseMessage(HttpStatusCode.OK); // unerreichbar
        });
        var service = CreateService();
        await service.EnqueueAsync("movie", 42, "Ein Film", Settings());
        await WaitForStateAsync(() => ReadEntryAsync("movie", 42), "downloading");

        var deleted = await service.DeleteAsync("movie", 42);
        await Task.Delay(300);

        Assert.True(deleted);
        Assert.Null(await ReadEntryAsync("movie", 42));
    }

    /// <summary>Prüft DeleteIfCompleted: nur abgeschlossene Downloads werden entfernt.</summary>
    [Fact]
    public async Task DeleteIfCompleted_QueuedEntry_StaysUntouched()
    {
        await LoginAsync();
        networkMonitor.IsWifi = false;
        var service = CreateService();
        await service.EnqueueAsync("tvshowepisode", 33, "Folge", Settings(wifiOnly: true));

        var deleted = await service.DeleteIfCompletedAsync("tvshowepisode", 33);

        Assert.False(deleted);
        Assert.NotNull(await ReadEntryAsync("tvshowepisode", 33));
    }

    /// <summary>Prüft Retry: fehlgeschlagener Download wird erneut eingeplant.</summary>
    [Fact]
    public async Task Enqueue_FailedEntry_Retries()
    {
        await LoginAsync();
        handler.Enqueue(HttpStatusCode.InternalServerError);
        var service = CreateService();
        await service.EnqueueAsync("movie", 42, "Ein Film", Settings());
        await WaitForStateAsync(() => ReadEntryAsync("movie", 42), "failed");

        EnqueueStream(64);
        var result = await service.EnqueueAsync("movie", 42, "Ein Film", Settings());

        Assert.Equal(DownloadService.NoticeEnqueued, result.Notice);
        var entry = await WaitForStateAsync(() => ReadEntryAsync("movie", 42), "completed");
        Assert.Null(entry.Error);
        Assert.Equal(64, entry.SizeBytes);
    }

    /// <summary>Prüft Fortschritt: Gesamtgröße und empfangene Bytes werden gemeldet.</summary>
    [Fact]
    public async Task Enqueue_ReportsProgressBytes()
    {
        await LoginAsync();
        EnqueueStream(256);
        var service = CreateService();
        var changed = new List<DownloadListItem>();
        service.ItemChanged += (_, e) => changed.Add(e.Item);

        await service.EnqueueAsync("movie", 42, "Ein Film", Settings());
        await WaitForStateAsync(() => ReadEntryAsync("movie", 42), "completed");

        var final = changed.Last();
        Assert.Equal(DownloadState.Completed, final.State);
        Assert.Equal(256, final.DownloadedBytes);
        Assert.Equal(256, final.TotalBytes);
        Assert.Equal(1d, final.Progress);
    }

    /// <summary>Prüft Enqueue: ohne Serververbindung schlägt der Transfer mit no_connection fehl.</summary>
    [Fact]
    public async Task Enqueue_WithoutLogin_FailsNoConnection()
    {
        // Kein Login → keine Basisadresse/kein Token.
        var service = CreateService();

        await service.EnqueueAsync("movie", 42, "Ein Film", Settings());
        var entry = await WaitForStateAsync(() => ReadEntryAsync("movie", 42), "failed");

        Assert.Equal(DownloadService.ErrorNoConnection, entry.Error);
    }

    /// <summary>Prüft Enqueue: ungültige Medientypen werden abgewiesen.</summary>
    [Fact]
    public async Task Enqueue_InvalidMediaType_Rejected()
    {
        var service = CreateService();

        var result = await service.EnqueueAsync("tvshow", 5, "Serie", Settings());

        Assert.True(result.IsError);
        Assert.Null(result.Item);
        Assert.Null(await ReadEntryAsync("tvshow", 5));
    }

    /// <summary>Prüft LoadItems: Manifest-Einträge in Auftragsreihenfolge.</summary>
    [Fact]
    public async Task LoadItems_ReturnsManifestOrder()
    {
        await LoginAsync();
        networkMonitor.IsWifi = false;
        var service = CreateService();
        var settings = Settings(wifiOnly: true);
        await service.EnqueueAsync("movie", 1, "Eins", settings);
        await service.EnqueueAsync("tvshowepisode", 2, "Zwei", settings);

        var items = await service.LoadItemsAsync();

        Assert.Equal(2, items.Count);
        Assert.Equal("Eins", items[0].Title);
        Assert.Equal("Zwei", items[1].Title);
        Assert.All(items, i => Assert.Equal(DownloadState.Queued, i.State));
    }

    /// <summary>Freigabegesteuerter Inhalt: belegt, dass Transfers parallel starten.</summary>
    private sealed class GatedContent : HttpContent
    {
        private readonly CountdownEvent started;
        private readonly ManualResetEventSlim release;
        private readonly byte[] data;

        /// <summary>Initialisiert den freigabegesteuerten Inhalt.</summary>
        /// <param name="started">Signalisiert den Start der Serialisierung.</param>
        /// <param name="release">Gibt die Serialisierung zum Schreiben frei.</param>
        /// <param name="data">Die zu schreibenden Nutzdaten.</param>
        public GatedContent(CountdownEvent started, ManualResetEventSlim release, byte[] data)
        {
            this.started = started;
            this.release = release;
            this.data = data;
        }

        protected override async Task SerializeToStreamAsync(
            Stream stream, TransportContext? context, CancellationToken cancellationToken)
        {
            started.Signal();
            await Task.Run(
                () => release.Wait(TimeSpan.FromSeconds(15)),
                cancellationToken);
            await stream.WriteAsync(data, cancellationToken);
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
            SerializeToStreamAsync(stream, context, CancellationToken.None);

        protected override bool TryComputeLength(out long length)
        {
            length = data.Length;
            return true;
        }
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
