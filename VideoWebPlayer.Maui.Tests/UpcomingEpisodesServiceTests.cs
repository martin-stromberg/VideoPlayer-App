#pragma warning disable xUnit1051 // Kurzlebige Unit-Tests: TestContext-CancellationToken nicht erforderlich
using System.Net;
using VideoPlayer.ApiClient.Models;
using VideoPlayer.Mvu;
using VideoPlayer.Storage;
using VideoWebPlayer.Maui.Tests.Stubs;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>
/// Detailseiten-unabhängige Folge-Episoden-Auflösung
/// (<see cref="UpcomingEpisodesService"/>): Online-Pfad über
/// <c>api/items/tvshow/{id}</c> mit Cache-Auffrischung, Offline-Pfad über
/// den <see cref="ShowDetailsCache"/> sowie die Leer-Fälle (keine
/// Episode, kein Serienkontext) — mit echter SQLite-Ablage.
/// </summary>
[Collection("SqliteDatabase")]
public class UpcomingEpisodesServiceTests : IDisposable
{
    private static readonly Uri Server = new("http://testserver/videoplayer/");

    private readonly string root;
    private readonly StubHttpMessageHandler handler = new();
    private readonly AuthSessionService authSession;
    private readonly CacheDatabase cache;
    private readonly UpcomingEpisodesService service;

    /// <summary>Initialisiert eine neue Instanz der Testklasse <c>UpcomingEpisodesServiceTests</c>.</summary>
    public UpcomingEpisodesServiceTests()
    {
        root = Path.Combine(Path.GetTempPath(), "videoplayer-upcoming-tests", Guid.NewGuid().ToString("N"));
        var layout = new AppStorageLayout(
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
        service = new UpcomingEpisodesService(authSession, cache);
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

    private async Task LoginAsync()
    {
        handler.EnqueueJson(HttpStatusCode.OK, new { token = "jwt", expires = "2030-01-01T00:00:00Z" });
        await authSession.LoginAsync(Server, "martin", "pw");
        handler.Requests.Clear();
    }

    private static TvShow ShowWithEpisodes(long id, string name, params (int Season, int Episode, long Id, string Ep)[] eps)
    {
        var show = new TvShow { Id = id, Name = name };
        show.Seasons = eps
            .GroupBy(e => e.Season)
            .Select(g => new TvShowSeason
            {
                Id = g.Key + 100,
                Number = g.Key,
                Name = $"Staffel {g.Key}",
                Episodes = g.Select(e => new TvShowEpisode
                {
                    Id = e.Id,
                    Number = e.Episode,
                    Name = e.Ep
                }).ToArray()
            })
            .ToArray();
        return show;
    }

    /// <summary>Prüft den Online-Pfad: Folge-Episoden staffelübergreifend ab der gestarteten Episode plus Cache-Auffrischung.</summary>
    [Fact]
    public async Task LoadAsync_Online_ReturnsUpcomingEpisodes_AndCachesShow()
    {
        await LoginAsync();
        handler.EnqueueJson(HttpStatusCode.OK,
            ShowWithEpisodes(11, "Probe",
                (1, 1, 31, "A"), (1, 2, 32, "B"), (2, 1, 33, "C"), (2, 2, 34, "D")));

        var upcoming = await service.LoadAsync(32, 11);

        Assert.Equal(new long[] { 33, 34 }, upcoming.Select(e => e.MediaId));
        Assert.Equal("C", upcoming[0].Title);
        Assert.Contains(handler.Requests, r => r.PathAndQuery.Contains("api/items/tvshow/11"));
        // Die Struktur liegt danach auch offline vor.
        var cached = await ShowDetailsCache.ReadAsync(cache, 11);
        Assert.NotNull(cached);
        Assert.Equal(4, ShowDetailsCache.OrderedEpisodes(cached!).Count);
    }

    /// <summary>Prüft den Offline-Pfad: ohne angemeldeten Client liefert der Serien-Cache die Folge-Episoden.</summary>
    [Fact]
    public async Task LoadAsync_WithoutClient_ReadsShowCache()
    {
        await ShowDetailsCache.WriteAsync(cache,
            ShowWithEpisodes(11, "Probe",
                (1, 1, 31, "A"), (1, 2, 32, "B"), (1, 3, 33, "C")));

        var upcoming = await service.LoadAsync(31, 11);

        Assert.Equal(new long[] { 32, 33 }, upcoming.Select(e => e.MediaId));
        Assert.Empty(handler.Requests);
    }

    /// <summary>Prüft den Fehler-Fallback: ein Ladefehlschlag greift auf die gecachte Serienstruktur zurück.</summary>
    [Fact]
    public async Task LoadAsync_RequestFails_FallsBackToCache()
    {
        await LoginAsync();
        await ShowDetailsCache.WriteAsync(cache,
            ShowWithEpisodes(11, "Probe", (1, 1, 31, "A"), (1, 2, 32, "B")));
        handler.Enqueue(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("server error")
        });

        var upcoming = await service.LoadAsync(31, 11);

        Assert.Equal(new long[] { 32 }, upcoming.Select(e => e.MediaId));
    }

    /// <summary>Prüft die Leer-Fälle: letzte Episode, unbekannte Episode und fehlender Serienkontext ergeben keine Folge-Episoden.</summary>
    [Fact]
    public async Task LoadAsync_NoUpcomingOrContext_ReturnsEmpty()
    {
        await LoginAsync();
        // Letzte Episode der Struktur.
        handler.EnqueueJson(HttpStatusCode.OK,
            ShowWithEpisodes(11, "Probe", (1, 1, 31, "A"), (1, 2, 32, "B")));
        Assert.Empty(await service.LoadAsync(32, 11));

        // Gestartete Episode nicht in der Struktur enthalten.
        handler.EnqueueJson(HttpStatusCode.OK,
            ShowWithEpisodes(11, "Probe", (1, 1, 31, "A"), (1, 2, 32, "B")));
        Assert.Empty(await service.LoadAsync(99, 11));
    }

    /// <summary>Prüft den Offline-Leer-Fall: ohne Client und ohne Cache-Eintrag bleibt die Liste leer.</summary>
    [Fact]
    public async Task LoadAsync_WithoutClientAndCache_ReturnsEmpty()
    {
        Assert.Empty(await service.LoadAsync(31, 11));
    }
}
