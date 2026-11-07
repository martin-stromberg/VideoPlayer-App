#pragma warning disable xUnit1026 // Name-Parameter dient der Lesbarkeit der Testfälle
#pragma warning disable xUnit1051 // Kurzlebige Unit-Tests: TestContext-CancellationToken nicht erforderlich
using System.Net;
using System.Text.Json;
using VideoPlayer.ApiClient;
using VideoPlayer.ApiClient.Models;
using VideoPlayer.Maui.Tests.Stubs;

namespace VideoPlayer.Maui.Tests;

/// <summary>Tests der Endpoint-URLs und HTTP-Methoden des <c>VideoPlayerApiClient</c>.</summary>
public class VideoPlayerApiClientTests_Endpoints
{
    /// <summary>Liefert die Endpoint-Testfälle als Theory-Datenquelle.</summary>
    /// <returns>Argumentensätze mit Name, Methode, Pfad und Antwort-Body.</returns>
    public static IEnumerable<object[]> EndpointCases()
    {
        yield return Case("Sources", HttpMethod.Get, "/api/Sources", "[]",
            (VideoPlayerApiClient c) => c.GetSourcesAsync());
        yield return Case("Source", HttpMethod.Get, "/api/Sources/5", "{}",
            c => c.GetSourceAsync(5));
        yield return Case("SourceGenres", HttpMethod.Get, "/api/SourceGenres/5", "{}",
            c => c.GetSourceGenresAsync(5));
        yield return Case("Recent", HttpMethod.Get, "/api/items/recent", "[]",
            c => c.GetRecentEntriesAsync());
        yield return Case("ContinueWatching", HttpMethod.Get, "/api/continue-watching", "[]",
            c => c.GetContinueWatchingAsync());
        yield return Case("Progress", HttpMethod.Post, "/api/continue-watching/progress", "{}",
            c => c.ReportPlaybackProgressAsync("movie", 7, 120, 3600));
        yield return Case("Hide", HttpMethod.Post, "/api/continue-watching/hide", """{"status":"hidden","message":"ok"}""",
            c => c.HideContinueWatchingEntryAsync("movie", 7));
        yield return Case("Skip", HttpMethod.Post, "/api/continue-watching/skip", """{"status":"skipped","message":"ok"}""",
            c => c.SkipContinueWatchingEntryAsync("movie", 7));
        yield return Case("Favorites", HttpMethod.Get, "/api/favorites", "[]",
            c => c.GetFavoritesAsync());
        yield return Case("ToggleFavorite", HttpMethod.Post, "/api/favorites/toggle", "true",
            c => c.ToggleFavoriteAsync(new Movie { Id = 3, Name = "Film" }));
        yield return Case("RemoveFavorite", HttpMethod.Post, "/api/favorites/remove", "{}",
            c => c.RemoveFavoriteAsync(11));
        yield return Case("SourceItems", HttpMethod.Get, "/api/items?mediaSourceId=3&page=1&size=10", "[]",
            c => c.GetSourceItemsAsync(3, page: 1, pageSize: 10));
        yield return Case("SourceItemsSearchGenre", HttpMethod.Get, "/api/items?mediaSourceId=3&page=0&size=30&search=a%20b&genreId=2", "[]",
            c => c.GetSourceItemsAsync(3, searchText: "a b", genreId: 2));
        yield return Case("Movie", HttpMethod.Get, "/api/items/movie/9", """{"$type":"movie","Id":9,"Name":"Film"}""",
            c => c.GetMovieAsync(9));
        yield return Case("MovieCollection", HttpMethod.Get, "/api/items/moviecollection/4", """{"$type":"movieCollection","Id":4,"Name":"Sammlung","Movies":[]}""",
            c => c.GetMovieCollectionAsync(4));
        yield return Case("TvShow", HttpMethod.Get, "/api/items/tvshow/2", """{"$type":"show","Id":2,"Name":"Serie","Seasons":[]}""",
            c => c.GetTvShowAsync(2));
        yield return Case("TvShowEpisode", HttpMethod.Get, "/api/items/tvshowepisode/8", """{"$type":"episode","Id":8,"Name":"Folge"}""",
            c => c.GetTvShowEpisodeAsync(8));
        yield return Case("GenreOptions", HttpMethod.Get, "/api/items/genres", "[]",
            c => c.GetGenreOptionsAsync());
        yield return Case("Picture", HttpMethod.Get, "/api/pictures/7", null,
            c => c.GetPictureAsync(7));
        yield return Case("EpisodeBackgroundImage", HttpMethod.Get, "/api/episodes/9/background-image", null,
            c => c.GetEpisodeBackgroundImageAsync(9));
        yield return Case("SourceIcon", HttpMethod.Get, "/api/sourceicons/8", null,
            c => c.GetSourceIconAsync(8));

        static object[] Case(string name, HttpMethod method, string path, string? responseBody, Func<VideoPlayerApiClient, Task> invoke)
        {
            return [name, method, path, responseBody!, invoke];
        }
    }

    /// <summary>Prüft Endpoint: verwendet die erwartete Methode und den erwarteten Pfad.</summary>
    /// <param name="name">Der Name.</param>
    /// <param name="expectedMethod">Die erwartete HTTP-Methode.</param>
    /// <param name="expectedPath">Der erwartete Request-Pfad.</param>
    /// <param name="responseBody">Der Antwort-Body.</param>
    /// <param name="invoke">Die Invoke-Action.</param>
    [Theory]
    [MemberData(nameof(EndpointCases))]
    public async Task Endpoint_UsesExpectedMethodAndPath(
        string name,
        HttpMethod expectedMethod,
        string expectedPath,
        string? responseBody,
        Func<VideoPlayerApiClient, Task> invoke)
    {
        var handler = new StubHttpMessageHandler();
        if (responseBody is null)
        {
            handler.Enqueue(request => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent([1, 2, 3])
            });
        }
        else
        {
            handler.Enqueue(HttpStatusCode.OK, responseBody);
        }
        var client = ApiClientTestFactory.CreateClient(handler);

        await invoke(client);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(expectedMethod, request.Method);
        Assert.Equal(expectedPath, request.PathAndQuery);
        Assert.Equal("test-api-key", request.ApiKey);
    }

    /// <summary>Prüft GetRecentEntriesAsync: deserialisiert polymorphe Einträge.</summary>
    [Fact]
    public async Task GetRecentEntriesAsync_DeserializesPolymorphicEntries()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, """
            [{"Id":1,"MediaSourceId":2,"PublishedAt":"2026-01-01T10:00:00Z","Type":0,
              "Entry":{"$type":"movie","Id":9,"Name":"Neuer Film","MediaSourceId":2,"IsFavorite":true,"Plot":"Handlung"}}]
            """);
        var client = ApiClientTestFactory.CreateClient(handler);

        var entries = await client.GetRecentEntriesAsync();

        var entry = Assert.Single(entries);
        Assert.Equal(RecentEntryKind.Movie, entry.Type);
        var movie = Assert.IsType<Movie>(entry.Entry);
        Assert.Equal(9, movie.Id);
        Assert.Equal("Neuer Film", movie.Name);
        Assert.True(movie.IsFavorite);
        Assert.Equal("Handlung", movie.Plot);
    }

    /// <summary>Prüft ReportPlaybackProgressAsync: der Playlist-Kontext landet PascalCase im Request-Body.</summary>
    [Fact]
    public async Task ReportPlaybackProgress_SendsPlaylistId()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, "{}");
        var client = ApiClientTestFactory.CreateClient(handler);

        await client.ReportPlaybackProgressAsync("movie", 7, 120, 3600, playlistId: 7);

        var request = Assert.Single(handler.Requests);
        Assert.Equal("/api/continue-watching/progress", request.PathAndQuery);
        using var body = JsonDocument.Parse(request.Body!);
        Assert.Equal("movie", body.RootElement.GetProperty("MediaType").GetString());
        Assert.Equal(7, body.RootElement.GetProperty("PlaylistId").GetInt64());
    }

    /// <summary>Prüft ReportPlaybackProgressAsync: ohne Playlist-Parameter bleibt der Kontext leer (Rückwärtsverhalten).</summary>
    [Fact]
    public async Task ReportPlaybackProgress_WithoutPlaylist_OmitsContext()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, "{}");
        var client = ApiClientTestFactory.CreateClient(handler);

        await client.ReportPlaybackProgressAsync("movie", 7, 120, 3600);

        var request = Assert.Single(handler.Requests);
        using var body = JsonDocument.Parse(request.Body!);
        Assert.True(
            !body.RootElement.TryGetProperty("PlaylistId", out var playlist)
            || playlist.ValueKind == JsonValueKind.Null,
            "kontextlose Meldung trägt keinen Playlist-Bezug");
    }

    /// <summary>Prüft GetContinueWatchingAsync: deserialisiert Einträge.</summary>
    [Fact]
    public async Task GetContinueWatchingAsync_DeserializesEntries()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, """
            [{"MediaType":"movie","PositionSeconds":15,"DurationSeconds":5400,"Title":"Beispiel","PosterPictureId":22,
              "Entry":{"$type":"movie","Id":10,"Name":"Beispiel","MediaSourceId":7}}]
            """);
        var client = ApiClientTestFactory.CreateClient(handler);

        var entries = await client.GetContinueWatchingAsync();

        var entry = Assert.Single(entries);
        Assert.Equal("movie", entry.MediaType);
        Assert.Equal(15, entry.PositionSeconds);
        Assert.Equal(5400, entry.DurationSeconds);
        Assert.Equal("Beispiel", entry.Title);
        Assert.Equal(22, entry.PosterPictureId);
        Assert.IsType<Movie>(entry.Entry);
    }

    /// <summary>Prüft GetFavoritesAsync: deserialisiert Einträge.</summary>
    [Fact]
    public async Task GetFavoritesAsync_DeserializesEntries()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, """
            [{"Id":4,"CreatedAt":"2026-02-01T00:00:00Z","Entry":{"$type":"show","Id":12,"Name":"Serie","MediaSourceId":1}}]
            """);
        var client = ApiClientTestFactory.CreateClient(handler);

        var favorites = await client.GetFavoritesAsync();

        var favorite = Assert.Single(favorites);
        Assert.Equal(4, favorite.Id);
        var show = Assert.IsType<TvShow>(favorite.Entry);
        Assert.Equal("Serie", show.Name);
    }

    /// <summary>Prüft GetTvShowAsync: deserialisiert Staffeln und Episoden.</summary>
    [Fact]
    public async Task GetTvShowAsync_DeserializesSeasonsAndEpisodes()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, """
            {"$type":"show","Id":2,"Name":"Serie","GenreNames":"Drama","Seasons":[
              {"$type":"season","Id":20,"Number":1,"Episodes":[{"$type":"episode","Id":200,"Number":1,"Name":"Pilot"}]}]}
            """);
        var client = ApiClientTestFactory.CreateClient(handler);

        var show = await client.GetTvShowAsync(2);

        Assert.Equal("Serie", show.Name);
        var season = Assert.Single(show.Seasons);
        Assert.Equal(1, season.Number);
        var episode = Assert.Single(season.Episodes);
        Assert.Equal("Pilot", episode.Name);
        Assert.Equal(1, episode.Number);
    }

    /// <summary>Prüft GetSourcesAsync: deserialisiert Quellen.</summary>
    [Fact]
    public async Task GetSourcesAsync_DeserializesSources()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, """
            [{"Id":1,"Name":"Filme","MediaSourceId":0,"CreatedAt":"2025-01-01T00:00:00Z","Changed":false,"IconPictureId":3}]
            """);
        var client = ApiClientTestFactory.CreateClient(handler);

        var sources = await client.GetSourcesAsync();

        var source = Assert.Single(sources);
        Assert.Equal(1, source.Id);
        Assert.Equal("Filme", source.Name);
        Assert.Equal(3, source.IconPictureId);
    }

    /// <summary>Prüft GetSourceItemsAsync: deserialisiert Zusammenfassungen.</summary>
    [Fact]
    public async Task GetSourceItemsAsync_DeserializesSummaries()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, """
            [{"Type":"movie","Id":5,"Title":"Titel","CreatedAt":"2025-01-01T00:00:00Z","PictureId":6,"ItemCount":0}]
            """);
        var client = ApiClientTestFactory.CreateClient(handler);

        var items = await client.GetSourceItemsAsync(1);

        var item = Assert.Single(items);
        Assert.Equal("movie", item.Type);
        Assert.Equal(5, item.Id);
        Assert.Equal("Titel", item.Title);
        Assert.Equal(6, item.PictureId);
    }

    /// <summary>Prüft GetPictureAsync: gibt Bytes zurück.</summary>
    [Fact]
    public async Task GetPictureAsync_ReturnsBytes()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent([9, 8, 7])
        });
        var client = ApiClientTestFactory.CreateClient(handler);

        var bytes = await client.GetPictureAsync(7);

        Assert.Equal([9, 8, 7], bytes);
    }

    /// <summary>Prüft GetHeroBackgroundAsync: ruft den Hero-Endpunkt auf und gibt die Bilddaten zurück.</summary>
    [Fact]
    public async Task GetHeroBackgroundAsync_RequestsHeroEndpoint_AndReturnsBytes()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent([1, 2, 3, 4])
        });
        var client = ApiClientTestFactory.CreateClient(handler);

        var bytes = await client.GetHeroBackgroundAsync();

        Assert.Equal([1, 2, 3, 4], bytes);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("/api/pictures/hero-background", request.PathAndQuery);
    }

    /// <summary>Prüft GetHeroBackgroundAsync: sendet den Bearer-Token der angemeldeten Sitzung.</summary>
    [Fact]
    public async Task GetHeroBackgroundAsync_SendsBearerToken()
    {
        var handler = new StubHttpMessageHandler();
        handler.EnqueueJson(HttpStatusCode.OK, new { token = "jwt-hero", expires = "2030-01-01T00:00:00Z" });
        handler.Enqueue(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent([5])
        });
        var client = ApiClientTestFactory.CreateClient(handler);

        await client.LoginAsync("user@example.com", "secret");
        await client.GetHeroBackgroundAsync();

        Assert.Equal("Bearer jwt-hero", handler.Requests[1].Authorization);
    }

    /// <summary>Prüft GetEpisodeBackgroundImageAsync: ruft den Episoden-Still-Endpunkt auf und gibt die Bilddaten zurück.</summary>
    [Fact]
    public async Task GetEpisodeBackgroundImageAsync_RequestsStillEndpoint_AndReturnsBytes()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent([4, 5, 6])
        });
        var client = ApiClientTestFactory.CreateClient(handler);

        var bytes = await client.GetEpisodeBackgroundImageAsync(9);

        Assert.Equal([4, 5, 6], bytes);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("/api/episodes/9/background-image", request.PathAndQuery);
    }

    /// <summary>Prüft GetEpisodeBackgroundImageAsync: sendet den Bearer-Token der angemeldeten Sitzung.</summary>
    [Fact]
    public async Task GetEpisodeBackgroundImageAsync_SendsBearerToken()
    {
        var handler = new StubHttpMessageHandler();
        handler.EnqueueJson(HttpStatusCode.OK, new { token = "jwt-still", expires = "2030-01-01T00:00:00Z" });
        handler.Enqueue(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent([6])
        });
        var client = ApiClientTestFactory.CreateClient(handler);

        await client.LoginAsync("user@example.com", "secret");
        await client.GetEpisodeBackgroundImageAsync(9);

        Assert.Equal("Bearer jwt-still", handler.Requests[1].Authorization);
    }
}
