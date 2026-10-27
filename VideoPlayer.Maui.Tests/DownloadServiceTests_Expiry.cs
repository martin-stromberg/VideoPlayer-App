#pragma warning disable xUnit1051 // Kurzlebige Unit-Tests: TestContext-CancellationToken nicht erforderlich
using VideoPlayer.Maui.Tests.Stubs;
using VideoPlayer.Mvu;
using VideoPlayer.Storage;
using VideoPlayer.Storage.Entities;

namespace VideoPlayer.Maui.Tests;

/// <summary>
/// Download-Ablauf/Zeitquelle: die injizierte <c>utcNow</c>-Zeitquelle
/// steuert alle Zeitstempel der Ablauflogik — Zielwert des
/// Wiedergabe-Schutzes (<see cref="DownloadService.PlaybackProtectionFactor"/>
/// × Restdauer) und die <c>UpdatedAtUtc</c>-Schreibstempel.
/// </summary>
[Collection("SqliteDatabase")]
public class DownloadServiceTests_Expiry : IDisposable
{
    private readonly string root;
    private readonly AppStorageLayout layout;
    private readonly StubHttpMessageHandler handler = new();
    private readonly AuthSessionService authSession;
    private readonly CacheDatabase cache;
    private readonly FakeNetworkMonitor networkMonitor = new();

    /// <summary>Initialisiert eine neue Instanz der Testklasse <c>DownloadServiceTests_Expiry</c>.</summary>
    public DownloadServiceTests_Expiry()
    {
        root = Path.Combine(Path.GetTempPath(), "videoplayer-expiry-tests", Guid.NewGuid().ToString("N"));
        layout = new AppStorageLayout(
            backupRoot: Path.Combine(root, "backup"),
            noBackupRoot: Path.Combine(root, "nobackup"),
            cacheRoot: Path.Combine(root, "cache"),
            profile: AppProfile.Create(null));
        layout.EnsureDirectoriesExist();
        cache = new CacheDatabase(layout);
        cache.InitializeAsync().Wait();

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

    private DownloadService CreateService(Func<DateTime>? utcNow = null) => new(
        authSession,
        new AppSecretsService(
            new InMemorySettingsStore(), new InMemorySecretStore(), AppProfile.Create(null)),
        cache, layout, networkMonitor,
        () => new HttpClient(handler, disposeHandler: false) { Timeout = Timeout.InfiniteTimeSpan },
        File.Exists,
        null,
        utcNow: utcNow);

    // Manifest-Lese-/-Anlege-Hilfen liegen gemeinsam in
    // ManifestTestHelpers — die Wrapper reichen cache/layout durch.
    private Task<DownloadManifestEntry?> ReadEntryAsync(string type, long mediaId)
    {
        return ManifestTestHelpers.ReadEntryAsync(cache, type, mediaId);
    }

    private Task<DownloadManifestEntry> InsertCompletedAsync(long mediaId, DateTime? expiresAtUtc)
    {
        return ManifestTestHelpers.InsertCompletedAsync(cache, layout, mediaId, expiresAtUtc, origin: "manual");
    }

    /// <summary>Prüft den Wiedergabe-Schutz: Zielwert und Schreibstempel kommen aus der injizierten Zeitquelle.</summary>
    [Fact]
    public async Task ExtendExpiry_CompletedEntry_UsesInjectedClock()
    {
        // Arrange
        var now = new DateTime(2030, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        await InsertCompletedAsync(42, expiresAtUtc: now.AddHours(1));
        var service = CreateService(utcNow: () => now);
        var remaining = TimeSpan.FromHours(3);

        // Act
        var raised = await service.ExtendExpiryAsync("movie", 42, remaining);

        // Assert
        Assert.True(raised);
        var entry = await ReadEntryAsync("movie", 42);
        Assert.Equal(now + DownloadService.PlaybackProtectionFactor * remaining, entry!.ExpiresAtUtc);
        Assert.Equal(now, entry.UpdatedAtUtc);
    }

    /// <summary>Prüft die „angesehen"-Verkürzung: der Schreibstempel kommt aus der injizierten Zeitquelle.</summary>
    [Fact]
    public async Task ExpireAfter_CompletedEntry_UsesInjectedClockForTimestamp()
    {
        // Arrange
        var now = new DateTime(2030, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        await InsertCompletedAsync(42, expiresAtUtc: now.AddDays(7));
        var service = CreateService(utcNow: () => now);

        // Act
        var shortened = await service.ExpireAfterAsync("movie", 42, TimeSpan.FromMinutes(15));

        // Assert
        Assert.True(shortened);
        var entry = await ReadEntryAsync("movie", 42);
        Assert.Equal(now + TimeSpan.FromMinutes(15), entry!.ExpiresAtUtc);
        Assert.Equal(now, entry.UpdatedAtUtc);
    }

    /// <summary>Prüft die Dedup-Verlängerung: der Schreibstempel der Verlängerung kommt aus der injizierten Zeitquelle.</summary>
    [Fact]
    public async Task Enqueue_AutoAlreadyPresent_UsesInjectedClockForTimestamp()
    {
        // Arrange
        var now = new DateTime(2030, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        await InsertCompletedAsync(42, expiresAtUtc: now.AddDays(1));
        var service = CreateService(utcNow: () => now);
        var settings = new AppSettings { DownloadRetentionDays = 7 };

        // Act
        var result = await service.EnqueueAsync(new DownloadEnqueueRequest("movie", 42, "Ein Film", Origin: DownloadOrigin.Auto), settings);

        // Assert
        Assert.Equal(DownloadService.NoticeAlreadyPresent, result.Notice);
        var entry = await ReadEntryAsync("movie", 42);
        Assert.Equal(now + TimeSpan.FromDays(7), entry!.ExpiresAtUtc);
        Assert.Equal(now, entry.UpdatedAtUtc);
    }
}
