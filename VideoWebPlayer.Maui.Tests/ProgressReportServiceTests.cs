#pragma warning disable xUnit1051 // Kurzlebige Unit-Tests: TestContext-CancellationToken nicht erforderlich
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using VideoPlayer.Mvu;
using VideoPlayer.Storage;
using VideoWebPlayer.Maui.Tests.Stubs;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>
/// Fortschrittsmeldungen (Schritt 6): Online direkt an den Server; offline
/// oder bei überholbarem Sendefehler in die persistente Warteschlange;
/// permanente Clientfehler werden verworfen.
/// </summary>
[Collection("SqliteDatabase")]
public class ProgressReportServiceTests : IDisposable
{
    private static readonly Uri Server = new("http://testserver/videoplayer/");

    private readonly string root;
    private readonly AppStorageLayout layout;
    private readonly PendingProgressSyncService queue;
    private readonly FakeNetworkMonitor monitor = new();
    private readonly StubHttpMessageHandler handler = new();
    private readonly AuthSessionService authSession;

    /// <summary>Initialisiert eine neue Instanz der Testklasse <c>ProgressReportServiceTests</c>.</summary>
    public ProgressReportServiceTests()
    {
        root = Path.Combine(Path.GetTempPath(), "videoplayer-report-tests", Guid.NewGuid().ToString("N"));
        layout = new AppStorageLayout(
            backupRoot: Path.Combine(root, "backup"),
            noBackupRoot: Path.Combine(root, "nobackup"),
            cacheRoot: Path.Combine(root, "cache"),
            profile: AppProfile.Create(null));
        layout.EnsureDirectoriesExist();
        var database = new BackupDatabase(layout);
        database.InitializeAsync().Wait();
        queue = new PendingProgressSyncService(database);

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

    private ProgressReportService CreateService() => new(authSession, queue, monitor);

    /// <summary>Prüft ReportProgress: bei Online → sendet direkt ohne Queue.</summary>
    [Fact]
    public async Task ReportProgress_Online_SendsDirectlyWithoutQueueing()
    {
        await LoginAsync();
        handler.Enqueue(HttpStatusCode.OK);
        var service = CreateService();

        await service.ReportProgressAsync("movie", 42, TimeSpan.FromMinutes(12), TimeSpan.FromMinutes(90));

        var request = Assert.Single(handler.Requests, r =>
            r.PathAndQuery.EndsWith("/api/continue-watching/progress"));
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("Bearer jwt", request.Authorization);

        using var body = JsonDocument.Parse(request.Body!);
        Assert.Equal("movie", body.RootElement.GetProperty("MediaType").GetString());
        Assert.Equal(42, body.RootElement.GetProperty("MediaId").GetInt64());
        Assert.Equal(720, body.RootElement.GetProperty("PositionSeconds").GetInt64());
        Assert.Equal(5400, body.RootElement.GetProperty("DurationSeconds").GetInt64());
        Assert.Equal(0, await queue.CountAsync());
    }

    /// <summary>Prüft ReportProgress: der Playlist-Kontext geht im Direktversand in den Request-Body.</summary>
    [Fact]
    public async Task ReportProgress_Online_SendsPlaylistId()
    {
        await LoginAsync();
        handler.Enqueue(HttpStatusCode.OK);
        var service = CreateService();

        await service.ReportProgressAsync(
            "movie", 42, TimeSpan.FromMinutes(12), TimeSpan.FromMinutes(90), playlistId: 7);

        var request = Assert.Single(handler.Requests, r =>
            r.PathAndQuery.EndsWith("/api/continue-watching/progress"));
        using var body = JsonDocument.Parse(request.Body!);
        Assert.Equal(7, body.RootElement.GetProperty("PlaylistId").GetInt64());
        Assert.Equal(0, await queue.CountAsync());
    }

    /// <summary>Prüft ReportProgress: beim Einreihen wird der Playlist-Kontext persistent gespeichert.</summary>
    [Fact]
    public async Task ReportProgress_Offline_EnqueuesPlaylistId()
    {
        await LoginAsync();
        monitor.IsOnline = false;
        var service = CreateService();

        await service.ReportProgressAsync(
            "movie", 42, TimeSpan.FromMinutes(12), TimeSpan.FromMinutes(90), playlistId: 7);

        var entry = Assert.Single(await queue.GetPendingAsync());
        Assert.Equal(7, entry.PlaylistId);
        Assert.DoesNotContain(handler.Requests, r =>
            r.PathAndQuery.EndsWith("/api/continue-watching/progress"));
    }

    /// <summary>Prüft ReportProgress: bei Offline → reiht ohne Request ein.</summary>
    [Fact]
    public async Task ReportProgress_Offline_EnqueuesWithoutRequest()
    {
        await LoginAsync();
        monitor.IsOnline = false;
        var service = CreateService();

        await service.ReportProgressAsync("episode", 7, TimeSpan.FromMinutes(3), TimeSpan.FromMinutes(45));

        var entry = Assert.Single(await queue.GetPendingAsync());
        Assert.Equal("episode", entry.VideoType);
        Assert.Equal(7, entry.VideoId);
        Assert.Equal(TimeSpan.FromMinutes(3).Ticks, entry.PositionTicks);
        Assert.Equal(TimeSpan.FromMinutes(45).Ticks, entry.DurationTicks);
        Assert.DoesNotContain(handler.Requests, r =>
            r.PathAndQuery.EndsWith("/api/continue-watching/progress"));
    }

    /// <summary>Prüft ReportProgress: ohne Client → reiht ein.</summary>
    [Fact]
    public async Task ReportProgress_WithoutClient_Enqueues()
    {
        // Nicht angemeldet (kein Client) — Meldung geht nicht verloren.
        var service = CreateService();

        await service.ReportProgressAsync("movie", 5, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(90));

        Assert.Equal(1, await queue.CountAsync());
    }

    /// <summary>Prüft ReportProgress: bei Transportfehler → reiht ein.</summary>
    [Fact]
    public async Task ReportProgress_TransportError_Enqueues()
    {
        await LoginAsync();
        // Wiederholungsstrategie: Erstversuch + 3 Wiederholungen.
        for (var i = 0; i < 4; i++)
        {
            handler.EnqueueException(new HttpRequestException("Netzwerk weg"));
        }
        var service = CreateService();

        await service.ReportProgressAsync("movie", 42, TimeSpan.FromMinutes(12), TimeSpan.FromMinutes(90));

        Assert.Equal(1, await queue.CountAsync());
    }

    /// <summary>Prüft ReportProgress: bei permanentem Clientfehler → verwirft den Eintrag.</summary>
    [Fact]
    public async Task ReportProgress_PermanentClientError_Discards()
    {
        await LoginAsync();
        // 404 ist nicht wiederholbar und wird bei jedem Versuch erneut
        // scheitern — die Meldung wird verworfen statt eingereiht.
        handler.Enqueue(HttpStatusCode.NotFound);
        var service = CreateService();

        await service.ReportProgressAsync("movie", 42, TimeSpan.FromMinutes(12), TimeSpan.FromMinutes(90));

        Assert.Equal(0, await queue.CountAsync());
    }

    /// <summary>
    /// Prüft ReportProgress: das Einreihen protokolliert die
    /// Warteschlange beim Zustandswechsel (nicht der Dienst pro Takt);
    /// Fehler-Warnungen erscheinen einmal je Medium und Outcome.
    /// </summary>
    [Fact]
    public async Task ReportProgressAsync_QueueAndDiscard_Logged()
    {
        var logging = new RecordingLoggerProvider();
        var loggedDb = new BackupDatabase(layout);
        await loggedDb.InitializeAsync();
        var loggedQueue = new PendingProgressSyncService(
            loggedDb, logging.CreateLogger<PendingProgressSyncService>());
        var service = new ProgressReportService(
            authSession, loggedQueue, monitor, logging.CreateLogger<ProgressReportService>());

        // Ohne Client wird eingereiht → Information nur auf der
        // Warteschlangen-Schicht, kein Doppeleintrag des Dienstes.
        await service.ReportProgressAsync("movie", 42, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(90));
        Assert.Contains(logging.Entries, e =>
            e.Level == LogLevel.Information
            && e.Category.Contains(nameof(PendingProgressSyncService), StringComparison.Ordinal)
            && e.Message.Contains("eingereiht", StringComparison.Ordinal));
        Assert.DoesNotContain(logging.Entries, e =>
            e.Category.Contains(nameof(ProgressReportService), StringComparison.Ordinal)
            && e.Message.Contains("eingereiht", StringComparison.Ordinal));

        // Erneutes Einreihen desselben Mediums im Fortschrittstakt
        // bleibt auf beiden Schichten still.
        await service.ReportProgressAsync("movie", 42, TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(90));
        Assert.Single(logging.Entries, e =>
            e.Level == LogLevel.Information
            && e.Category.Contains(nameof(PendingProgressSyncService), StringComparison.Ordinal)
            && e.Message.Contains("eingereiht", StringComparison.Ordinal));

        // Permanenter Clientfehler (4xx ohne 401/408/429) → verworfen
        // → Warning, einmal je Medium (Zustandswechsel, kein Takt).
        await LoginAsync();
        handler.Enqueue(HttpStatusCode.NotFound);
        await service.ReportProgressAsync("movie", 43, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(90));
        handler.Enqueue(HttpStatusCode.NotFound);
        await service.ReportProgressAsync("movie", 43, TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(90));
        Assert.Single(logging.Entries, e =>
            e.Level == LogLevel.Warning
            && e.Category.Contains(nameof(ProgressReportService), StringComparison.Ordinal)
            && e.Message.Contains("verworfen", StringComparison.Ordinal));
    }

    /// <summary>
    /// Prüft ReportProgress: ein persistenter Schreibfehler der
    /// Warteschlange (z. B. SQLite) wird je Medium einmal als Warnung
    /// protokolliert — nicht pro Fortschrittstakt, und er entkommt
    /// nicht als Ausnahme (der nächste Takt versucht erneut).
    /// </summary>
    [Fact]
    public async Task ReportProgress_EnqueueFailure_LogsWarningOncePerMedia()
    {
        var logging = new RecordingLoggerProvider();
        // Die Datenbank bleibt bewusst uninitialisiert — die fehlende
        // Tabelle pending_progress lässt jedes Einreihen scheitern.
        var brokenLayout = new AppStorageLayout(
            backupRoot: Path.Combine(root, "broken-backup"),
            noBackupRoot: Path.Combine(root, "broken-nobackup"),
            cacheRoot: Path.Combine(root, "broken-cache"),
            profile: AppProfile.Create(null));
        brokenLayout.EnsureDirectoriesExist();
        var brokenQueue = new PendingProgressSyncService(new BackupDatabase(brokenLayout));
        var service = new ProgressReportService(
            authSession, brokenQueue, monitor, logging.CreateLogger<ProgressReportService>());

        // Zwei Fortschrittstakte desselben Mediums — der zweite erzeugt
        // keinen weiteren Eintrag (Zustandswechsel statt Takt).
        await service.ReportProgressAsync("movie", 42, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(90));
        await service.ReportProgressAsync("movie", 42, TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(90));

        Assert.Single(logging.Entries, e =>
            e.Level == LogLevel.Warning
            && e.Category.Contains(nameof(ProgressReportService), StringComparison.Ordinal)
            && e.Message.Contains("movie/42", StringComparison.Ordinal));
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
