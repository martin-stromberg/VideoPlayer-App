using System.Text.Json;
using VideoPlayer.E2E.Tests.Support;

namespace VideoPlayer.E2E.Tests.Tests;

/// <summary>Exakte saisonale Benutzerflüsse gegen die echte Windows-App.</summary>
public sealed class SeasonalGenresTests : E2ETestBase
{
    private const long MovieId = 8701;
    private const string MovieTitle = "Saison-Film";

    /// <summary>Initialisiert die Build-Fixture.</summary>
    /// <param name="fixture">Geteilte Fixture.</param>
    public SeasonalGenresTests(AppFixture fixture) : base(fixture) { }

    /// <summary>Die saisonale Filmkarte öffnet genau den gelieferten Film.</summary>
    [Fact]
    public async Task ActiveGenre_FilmCardOpensExactMovie()
    {
        using var server = Server();
        using var app = await StartAppAsync();
        await LoginSeasonalAsync(app, server, 1);
        var genre = (await app.DevApi.GetTreeAsync(Ct)).FindByAutomationId("SeasonalGenre7");
        Assert.NotNull(genre);
        Assert.True(genre.IsVisible);
        Assert.Contains(MovieTitle, Flatten(genre).Select(n => n.Text));
        await app.DevApi.InvokeAsync("SeasonalGenre7", 0, Ct);
        await app.DevApi.WaitForModelAsync(m => m.MediaDetailsId == MovieId && m.MediaDetailsTitle == MovieTitle, description: "Saisonfilm-Detailziel", cancellationToken: Ct);
        Assert.True(server.WasPathRequested($"/api/items/movie/{MovieId}"));
    }

    /// <summary>Zwei Genres bleiben geordnet und öffnen Film und Sammlung korrekt.</summary>
    [Fact]
    public async Task MultipleGenres_KeepOrderAndCollectionNavigation()
    {
        using var server = Server();
        server.SeasonalCollectionId = 8702;
        server.SeasonalGenresJson = Genres(DateTimeOffset.UtcNow.AddDays(1), includeCollection: true);
        using var app = await StartAppAsync();
        await LoginSeasonalAsync(app, server, 2);
        var section = (await app.DevApi.GetTreeAsync(Ct)).FindByAutomationId("SeasonalGenres");
        Assert.NotNull(section);
        Assert.Equal(new[] { "SeasonalGenre7", "SeasonalGenre8" }, section.Children.Select(c => c.AutomationId));
        await app.DevApi.InvokeAsync("SeasonalGenre7", 0, Ct);
        await app.DevApi.WaitForModelAsync(m => m.MediaDetailsId == MovieId, cancellationToken: Ct);
        app.Ui.GoBack();
        await WaitForGenreTreeAsync(app, "SeasonalGenre8");
        await app.DevApi.InvokeAsync("SeasonalGenre8", 0, Ct);
        await app.DevApi.WaitForModelAsync(m => m.MediaDetailsId == 8702 && m.MediaDetailsTitle == "Saison-Sammlung", cancellationToken: Ct);
        Assert.True(server.WasPathRequested("/api/items/moviecollection/8702"));
    }

    /// <summary>Eine erfolgreiche leere Antwort entfernt vorherige saisonale Sektionen.</summary>
    [Fact]
    public async Task SuccessfulEmptyResponse_RemovesPreviousSection()
    {
        using var server = Server();
        using var app = await StartAppAsync();
        await LoginSeasonalAsync(app, server, 1);
        server.SeasonalGenresJson = "[]";
        await RefreshAsync(app, 0);
        Assert.Null((await app.DevApi.GetTreeAsync(Ct)).FindByAutomationId("SeasonalGenre7"));
    }

    /// <summary>HTTP-Fehler und Neustart bewahren gültigen Cache; späteres [] entfernt ihn.</summary>
    [Fact]
    public async Task FailedFetchAndRestart_PreserveUnexpiredCache_ThenEmptyRemovesIt()
    {
        using var server = Server();
        var app = await StartAppAsync();
        string profile;
        try
        {
            await LoginSeasonalAsync(app, server, 1);
            server.SeasonalGenresStatus = 503;
            await RefreshAsync(app, 1);
            profile = app.Profile;
        }
        finally { app.Dispose(deleteProfile: false); }
        using var restarted = await StartAppAsync(profile: profile);
        await restarted.DevApi.WaitForModelAsync(m => m.SeasonalGenresCount == 1 && !m.IsStartupRunning && !m.IsSyncRunning, TimeSpan.FromSeconds(45), "Cache nach Neustart und Abruffehler", Ct);
        await WaitForGenreTreeAsync(restarted, "SeasonalGenre7");
        server.SeasonalGenresStatus = 200;
        server.SeasonalGenresJson = "[]";
        await RefreshAsync(restarted, 0);
    }

    /// <summary>Serverantwort ohne Genres erzeugt keine leeren Überschriften oder Karten.</summary>
    [Fact]
    public async Task NoSeasonalGenres_ShowsNoEmptyCarousel()
    {
        using var server = Server();
        server.SeasonalGenresJson = "[]";
        using var app = await StartAppAsync();
        await LoginSeasonalAsync(app, server, 0);
        var sections = (await app.DevApi.GetTreeAsync(Ct)).FindByAutomationId("SeasonalGenres");
        Assert.NotNull(sections);
        Assert.Empty(sections.Children);
    }

    /// <summary>Kontrollierte Uhr entfernt bei Fehler exakt ab Ablauf auch auf offener Seite und nach Neustart.</summary>
    [Fact]
    public async Task ExpiryWhileOpen_AndRestart_HidesExpiredCacheWithoutSuccessfulFetch()
    {
        var file = Path.Combine(Path.GetTempPath(), $"seasonal-clock-{Guid.NewGuid():N}.txt");
        var expiry = new DateTimeOffset(2030, 11, 1, 0, 0, 0, TimeSpan.Zero);
        await File.WriteAllTextAsync(file, expiry.AddSeconds(-1).ToString("O"), Ct);
        using var server = Server();
        server.SeasonalGenresJson = Genres(expiry);
        AppInstance? app = null;
        var previous = Environment.GetEnvironmentVariable("VIDEOPLAYER_TEST_CLOCK_FILE");
        Environment.SetEnvironmentVariable("VIDEOPLAYER_TEST_CLOCK_FILE", file);
        try
        {
            app = await StartAppAsync();
            await LoginSeasonalAsync(app, server, 1);
            await WaitForGenreTreeAsync(app, "SeasonalGenre7");
            server.SeasonalGenresStatus = 503;
            await File.WriteAllTextAsync(file, expiry.ToString("O"), Ct);
            await app.DevApi.WaitForModelAsync(m => m.SeasonalGenresCount == 0, description: "Sektion exakt an Servergrenze verschwunden", cancellationToken: Ct);
            Assert.Null((await app.DevApi.GetTreeAsync(Ct)).FindByAutomationId("SeasonalGenre7"));
            var profile = app.Profile;
            app.Dispose(deleteProfile: false);
            server.HealthStatus = 503;
            app = await StartAppAsync(profile: profile);
            await app.DevApi.WaitForModelAsync(m => m.IsOffline && m.CurrentPage == "Home" && !m.IsStartupRunning && !m.IsSyncRunning, TimeSpan.FromSeconds(45), "Offline-Neustart nach Ablauf", Ct);
            Assert.Equal(0, (await app.DevApi.GetModelAsync(Ct)).SeasonalGenresCount);
            Assert.Null((await app.DevApi.GetTreeAsync(Ct)).FindByAutomationId("SeasonalGenre7"));
        }
        finally
        {
            app?.Dispose();
            Environment.SetEnvironmentVariable("VIDEOPLAYER_TEST_CLOCK_FILE", previous);
            File.Delete(file);
        }
    }

    private static MockVideoServer Server() => new("AAAAAA", "e2e-seasonal", "e2e@example.test", "e2e-pass")
    {
        PlaybackMovieId = MovieId,
        PlaybackMovieTitle = MovieTitle,
        SeasonalGenresJson = Genres(DateTimeOffset.UtcNow.AddDays(1))
    };

    private static string Genres(DateTimeOffset expiry, bool includeCollection = false)
    {
        var genres = new List<object> { new { genreId = 7L, name = "Halloween", expiresAt = expiry, entries = new[] { new Dictionary<string, object> { ["$type"] = "movie", ["id"] = MovieId, ["name"] = MovieTitle, ["posterPictureId"] = 123 } } } };
        if (includeCollection)
            genres.Add(new { genreId = 8L, name = "Weihnachten", expiresAt = expiry, entries = new[] { new Dictionary<string, object> { ["$type"] = "movieCollection", ["id"] = 8702L, ["name"] = "Saison-Sammlung", ["movies"] = Array.Empty<object>() } } });
        return JsonSerializer.Serialize(genres);
    }

    private static async Task LoginSeasonalAsync(AppInstance app, MockVideoServer server, int count)
    {
        await ConnectToPairingRequiredAsync(app, server);
        await PairAsync(app, server);
        await LoginAsync(app, server);
        await app.DevApi.WaitForModelAsync(m => m.SeasonalGenresCount == count && !m.IsSyncRunning, TimeSpan.FromSeconds(30), "Saison-Synchronisation", Ct);
        Assert.True(server.WasPathRequested("/api/home/seasonal-genres"));
    }

    private static async Task RefreshAsync(AppInstance app, int count)
    {
        await app.DevApi.InvokeAsync("HomeRefresh", cancellationToken: Ct);
        await Task.Delay(500, Ct);
        await app.DevApi.WaitForModelAsync(m => !m.IsSyncRunning && m.SeasonalGenresCount == count, TimeSpan.FromSeconds(35), "Startseite nach Refresh", Ct);
    }

    private static async Task WaitForGenreTreeAsync(AppInstance app, string id)
    {
        var deadline = DateTime.UtcNow.AddSeconds(20);
        while ((await app.DevApi.GetTreeAsync(Ct)).FindByAutomationId(id) is null)
        {
            Assert.True(DateTime.UtcNow < deadline, "Genre-Karussell fehlt: " + id);
            await Task.Delay(100, Ct);
        }
    }

    private static IEnumerable<DevApiTreeNode> Flatten(DevApiTreeNode node) => new[] { node }.Concat(node.Children.SelectMany(Flatten));
}
