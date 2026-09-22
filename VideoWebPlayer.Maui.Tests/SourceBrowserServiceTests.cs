# pragma warning disable xUnit1051 // Kurzlebige Unit-Tests: TestContext-CancellationToken nicht erforderlich
using System.Net;
using VideoPlayer.ApiClient.Models;
using VideoPlayer.Mvu;
using VideoPlayer.Storage;
using VideoWebPlayer.Maui.Tests.Stubs;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>
/// Quellen-Browsing-Dienst (Schritt 8): Quellenliste entspricht der
/// Server-API, Genres erhalten den „Alle"-Eintrag, Einträge werden
/// seitenweise mit Suche/Genre-Filter geladen und auf die
/// Anzeige-Records gemappt.
/// </summary>
public class SourceBrowserServiceTests
{
    private static readonly Uri Server = new("http://testserver/videoplayer/");

    private readonly StubHttpMessageHandler handler = new();
    private readonly AuthSessionService authSession;

    public SourceBrowserServiceTests()
    {
        var secrets = new AppSecretsService(
            new InMemorySettingsStore(), new InMemorySecretStore(), AppProfile.Create(null));
        authSession = new AuthSessionService(
            secrets,
            uri => new HttpClient(handler, disposeHandler: false) { BaseAddress = uri },
            new RecordingDelays().Delay);
    }

    private SourceBrowserService CreateService() => new(authSession);

    private async Task LoginAsync()
    {
        handler.EnqueueJson(HttpStatusCode.OK, new { token = "jwt", expires = "2030-01-01T00:00:00Z" });
        await authSession.LoginAsync(Server, "martin", "pw");
        handler.Requests.Clear();
    }

    [Fact]
    public async Task LoadSources_MapsServerList_WithIcons()
    {
        await LoginAsync();
        handler.EnqueueJson(HttpStatusCode.OK, new[]
        {
            new MediaSource { Id = 1, Name = "Filme", IconPictureId = 91 },
            new MediaSource { Id = 2, Name = "Serien" }
        });

        var sources = await CreateService().LoadSourcesAsync();

        Assert.Equal(2, sources.Count);
        Assert.Equal(1, sources[0].Id);
        Assert.Equal("Filme", sources[0].Name);
        Assert.Equal(91, sources[0].IconPictureId);
        Assert.Null(sources[1].IconPictureId);
        Assert.EndsWith("/api/Sources", Assert.Single(handler.Requests).PathAndQuery);
    }

    [Fact]
    public async Task LoadSources_WithoutClient_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateService().LoadSourcesAsync());
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task LoadGenres_PrependsAllEntry_AndMapsServerGenres()
    {
        await LoginAsync();
        handler.EnqueueJson(HttpStatusCode.OK, new SourceGenres
        {
            SourceId = 1,
            SourceName = "Filme",
            Genres =
            [
                new Genre { Id = 4, Name = "Komödie" },
                new Genre { Id = 8, Name = "Drama" }
            ]
        });

        var genres = await CreateService().LoadGenresAsync(1);

        Assert.Equal(3, genres.Count);
        Assert.Equal(0, genres[0].Id);
        Assert.Equal("Alle", genres[0].Name);
        Assert.True(genres[0].IsSelected);
        Assert.Equal("Komödie", genres[1].Name);
        Assert.False(genres[1].IsSelected);
        Assert.EndsWith("/api/SourceGenres/1", Assert.Single(handler.Requests).PathAndQuery);
    }

    [Fact]
    public async Task LoadItems_FirstPage_MapsSummaries_AndRequestsQuery()
    {
        await LoginAsync();
        handler.EnqueueJson(HttpStatusCode.OK, new[]
        {
            new MediaItemSummary { Id = 42, Type = "Movie", Title = "Matrix", PictureId = 77 },
            new MediaItemSummary { Id = 43, Type = "TvShow", Title = "Serie A" }
        });

        var page = await CreateService().LoadItemsAsync(5, page: 0, searchText: "mat", genreId: 4);

        Assert.Equal(0, page.Page);
        Assert.Equal(2, page.Items.Count);
        Assert.Equal(42, page.Items[0].Id);
        Assert.Equal("moviecollection", page.Items[0].MediaType);
        Assert.Equal(77, page.Items[0].PosterPictureId);
        Assert.Equal("tvshow", page.Items[1].MediaType);

        var request = Assert.Single(handler.Requests);
        Assert.Contains("mediaSourceId=5", request.PathAndQuery);
        Assert.Contains("page=0", request.PathAndQuery);
        Assert.Contains($"size={SourceBrowserState.PageSize}", request.PathAndQuery);
        Assert.Contains("search=mat", request.PathAndQuery);
        Assert.Contains("genreId=4", request.PathAndQuery);
    }

    [Fact]
    public async Task LoadItems_FullPage_ReportsHasMore()
    {
        await LoginAsync();
        handler.EnqueueJson(HttpStatusCode.OK,
            Enumerable.Range(1, SourceBrowserState.PageSize)
                .Select(i => new MediaItemSummary { Id = i, Type = "Movie", Title = $"F{i}" })
                .ToArray());

        var full = await CreateService().LoadItemsAsync(5, 0, null, null);
        Assert.True(full.HasMore);

        handler.EnqueueJson(HttpStatusCode.OK, new[]
        {
            new MediaItemSummary { Id = 99, Type = "Movie", Title = "Letzter" }
        });
        var partial = await CreateService().LoadItemsAsync(5, 1, null, null);
        Assert.False(partial.HasMore);
    }

    [Fact]
    public async Task LoadItems_NoFilter_OmitsSearchAndGenreParameters()
    {
        await LoginAsync();
        handler.EnqueueJson(HttpStatusCode.OK, Array.Empty<MediaItemSummary>());

        await CreateService().LoadItemsAsync(5, 0, null, genreId: 0);

        var request = Assert.Single(handler.Requests);
        Assert.DoesNotContain("search=", request.PathAndQuery);
        Assert.DoesNotContain("genreId=", request.PathAndQuery);
    }

    [Theory]
    [InlineData("Movie", "moviecollection")]
    [InlineData("moviecollection", "moviecollection")]
    [InlineData("TvShow", "tvshow")]
    [InlineData("Season", "tvshowseason")]
    [InlineData("Episode", "tvshowepisode")]
    [InlineData("anders", "tvshow")]
    public void MapSummaryType_MapsLegacyTypes(string type, string expected)
    {
        Assert.Equal(expected, SourceBrowserService.MapSummaryType(type));
    }
}
