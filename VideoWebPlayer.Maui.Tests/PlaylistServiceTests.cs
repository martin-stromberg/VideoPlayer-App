#pragma warning disable xUnit1051 // Kurzlebige Unit-Tests: TestContext-CancellationToken nicht erforderlich
using System.Net;
using VideoPlayer.Mvu;
using VideoPlayer.Storage;
using VideoWebPlayer.Maui.Tests.Stubs;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>
/// Playlist-Service (Issue 43, Schritt 4): DTO→Anzeige-Mapping der
/// Übersichts- und Eintragsdaten sowie der Fehlerpfad ohne verbundenen
/// Server (reines Online-Feature).
/// </summary>
public class PlaylistServiceTests
{
    private static readonly Uri Server = new("http://testserver/videoplayer/");

    private readonly StubHttpMessageHandler handler = new();
    private readonly AuthSessionService authSession;

    /// <summary>Initialisiert eine neue Instanz der Testklasse <c>PlaylistServiceTests</c>.</summary>
    public PlaylistServiceTests()
    {
        var secrets = new AppSecretsService(
            new InMemorySettingsStore(), new InMemorySecretStore(), AppProfile.Create(null));
        authSession = new AuthSessionService(
            secrets,
            uri => new HttpClient(handler, disposeHandler: false) { BaseAddress = uri },
            new RecordingDelays().Delay);
    }

    private PlaylistService CreateService() => new(authSession);

    private async Task LoginAsync()
    {
        handler.EnqueueJson(HttpStatusCode.OK, new { token = "jwt", expires = "2030-01-01T00:00:00Z" });
        await authSession.LoginAsync(Server, "martin", "pw");
        handler.Requests.Clear();
    }

    /// <summary>Prüft LoadPlaylistsAsync: fordert beide Listen an und mappt die DTO-Felder.</summary>
    [Fact]
    public async Task LoadPlaylistsAsync_LoadsBothLists_AndMapsFields()
    {
        await LoginAsync();
        handler.Enqueue(HttpStatusCode.OK, """
            [{"Id":7,"Name":"Eigene","SortMode":"Manual","CoverPictureId":44,
              "Genres":[{"Id":1,"Name":"Drama"},{"Id":2,"Name":"Krimi"}],
              "IsPublic":false,"IsOwner":true}]
            """);
        handler.Enqueue(HttpStatusCode.OK, """
            [{"Id":8,"Name":"Öffentlich","SortMode":"ByReleaseDate",
              "Genres":[],"IsPublic":true,"IsOwner":false}]
            """);

        var overview = await CreateService().LoadPlaylistsAsync();

        Assert.Equal(2, handler.Requests.Count);
        Assert.EndsWith("/api/playlists", handler.Requests[0].PathAndQuery);
        Assert.EndsWith("/api/playlists/public", handler.Requests[1].PathAndQuery);

        var own = Assert.Single(overview.Own);
        Assert.Equal(7, own.PlaylistId);
        Assert.Equal("Eigene", own.Name);
        Assert.Equal("Manual", own.SortMode);
        Assert.Equal(44, own.CoverPictureId);
        Assert.True(own.HasCover);
        Assert.Equal("Drama, Krimi", own.GenreText);
        Assert.False(own.IsPublic);
        Assert.True(own.IsOwner);

        var pub = Assert.Single(overview.Public);
        Assert.Equal(8, pub.PlaylistId);
        Assert.True(pub.IsPublic);
        Assert.False(pub.IsOwner);
        Assert.False(pub.HasCover);
    }

    /// <summary>Prüft LoadEntriesAsync: mappt Elternkontext/Episodennummer und übergibt die Seite 1-basiert.</summary>
    [Fact]
    public async Task LoadEntriesAsync_MapsEntries_InServerOrder()
    {
        await LoginAsync();
        handler.Enqueue(HttpStatusCode.OK, """
            {"Entries":[
              {"Id":1,"MediaType":"TVShowEpisode","MediaId":55,"MediaTitle":"Folge 4",
               "ParentMediaType":"TVShow","ParentMediaId":12,"ParentMediaTitle":"Serie",
               "ResolvedPictureId":88,"EpisodeNumber":4,"IsAccessible":true},
              {"Id":2,"MediaType":"Movie","MediaId":60,"MediaTitle":"Film",
               "ResolvedPictureId":null,"IsAccessible":true}],
             "TotalCount":2,"HasNextPage":false,"PageNumber":1,"PageSize":20}
            """);

        var page = await CreateService().LoadEntriesAsync(7, 1);

        var request = Assert.Single(handler.Requests);
        Assert.EndsWith("/api/playlists/7/entries/paged?pageNumber=1&pageSize=20", request.PathAndQuery);
        Assert.Equal(1, page.Page);
        Assert.False(page.HasMore);
        Assert.Equal(2, page.TotalCount);

        Assert.Equal(2, page.Items.Count);
        var episode = page.Items[0];
        Assert.Equal(1, episode.EntryId);
        Assert.Equal("tvshowepisode", episode.MediaType);
        Assert.Equal(55, episode.MediaId);
        Assert.Equal("Folge 4", episode.Title);
        Assert.Equal("tvshow", episode.ParentMediaType);
        Assert.Equal(12, episode.ParentMediaId);
        Assert.Equal("Serie", episode.ParentMediaTitle);
        Assert.Equal(88, episode.ResolvedPictureId);
        Assert.Equal(4, episode.EpisodeNumber);
        Assert.Equal("Serie · Episode 4", episode.Subtitle);
        var movie = page.Items[1];
        Assert.Equal("movie", movie.MediaType);
        Assert.Null(movie.ResolvedPictureId);
    }

    /// <summary>Prüft LoadPlaylistAsync: liefert die Kopfdaten bzw. <c>null</c> bei 404.</summary>
    [Fact]
    public async Task LoadPlaylistAsync_NotFound_ReturnsNull()
    {
        await LoginAsync();
        handler.Enqueue(HttpStatusCode.NotFound, """{"error":"not found"}""");

        var item = await CreateService().LoadPlaylistAsync(9);

        Assert.Null(item);
        Assert.EndsWith("/api/playlists/9", Assert.Single(handler.Requests).PathAndQuery);
    }

    /// <summary>Prüft die Fehlerpfade ohne angemeldeten Client (kein Server verbunden).</summary>
    [Fact]
    public async Task WithoutClient_ThrowsInvalidOperation()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.LoadPlaylistsAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.LoadPlaylistAsync(7));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.LoadEntriesAsync(7, 1));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.StartPlaybackAsync(7));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetNextEntryAsync(7, 1));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetPreviousEntryAsync(7, 1));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.AdvanceAsync(7, 1));
        Assert.Empty(handler.Requests);
    }

    // --- Playlist-Wiedergabe (Schritt 5) ---

    /// <summary>Prüft StartPlaybackAsync: ohne entryId wird <c>POST /play</c> ohne Query aufgerufen und die Antwort gemappt.</summary>
    [Fact]
    public async Task StartPlaybackAsync_MapsDto_AndPostsWithoutEntryId()
    {
        await LoginAsync();
        handler.Enqueue(HttpStatusCode.OK, """
            {"PlaylistId":7,"PlaylistName":"Abend","TotalCount":3,
             "CurrentPosition":2,"CurrentEntryId":5,
             "CurrentEntry":{"Id":5,"MediaType":"TVShowEpisode","MediaId":55,"MediaTitle":"Folge 4","IsAccessible":true},
             "StreamUrl":"/api/items/tvshowepisode/55/stream",
             "MediaType":"TVShowEpisode","MediaId":55,"StartPositionSeconds":42}
            """);

        var start = await CreateService().StartPlaybackAsync(7);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.EndsWith("/api/playlists/7/play", request.PathAndQuery);

        Assert.Equal(7, start.PlaylistId);
        Assert.Equal("Abend", start.PlaylistName);
        Assert.Equal(3, start.TotalCount);
        Assert.Equal(2, start.CurrentPosition);
        Assert.Equal(5, start.CurrentEntryId);
        Assert.Equal("tvshowepisode", start.MediaType);
        Assert.Equal(55, start.MediaId);
        Assert.Equal("Folge 4", start.Title);
        Assert.Equal(42, start.StartPositionSeconds);
    }

    /// <summary>Prüft StartPlaybackAsync: mit entryId geht die Id als Query-Parameter mit.</summary>
    [Fact]
    public async Task StartPlaybackAsync_WithEntryId_SendsQueryParameter()
    {
        await LoginAsync();
        handler.Enqueue(HttpStatusCode.OK, """
            {"PlaylistId":7,"PlaylistName":"Abend","TotalCount":3,
             "CurrentPosition":3,"CurrentEntryId":8,
             "CurrentEntry":{"Id":8,"MediaType":"Movie","MediaId":60,"MediaTitle":"Film","IsAccessible":true},
             "StreamUrl":"/api/items/movie/60/stream",
             "MediaType":"Movie","MediaId":60,"StartPositionSeconds":0}
            """);

        var start = await CreateService().StartPlaybackAsync(7, 8);

        Assert.EndsWith("/api/playlists/7/play?entryId=8",
            Assert.Single(handler.Requests).PathAndQuery);
        Assert.Equal(60, start.MediaId);
        Assert.Equal("movie", start.MediaType);
    }

    /// <summary>Prüft die Navigations-Methoden: rufen die richtigen Endpunkte mit currentEntryId auf und mappen das Ergebnis.</summary>
    /// <param name="endpoint">Navigations-Endpunkt („next“, „previous“ oder „advance“).</param>
    [Theory]
    [InlineData("next")]
    [InlineData("previous")]
    [InlineData("advance")]
    public async Task Navigation_CallsEndpoint_AndMapsResult(string endpoint)
    {
        await LoginAsync();
        handler.Enqueue(HttpStatusCode.OK, """
            {"Entry":{"Id":6,"MediaType":"TVShowEpisode","MediaId":56,"MediaTitle":"Folge 5","IsAccessible":true},
             "Position":3,"StartPositionSeconds":15}
            """);

        var service = CreateService();
        var result = endpoint switch
        {
            "next" => await service.GetNextEntryAsync(7, 5),
            "previous" => await service.GetPreviousEntryAsync(7, 5),
            _ => await service.AdvanceAsync(7, 5)
        };

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.EndsWith($"/api/playlists/7/play/{endpoint}?currentEntryId=5", request.PathAndQuery);

        Assert.NotNull(result);
        Assert.Equal(6, result.EntryId);
        Assert.Equal(3, result.Position);
        Assert.Equal("tvshowepisode", result.MediaType);
        Assert.Equal(56, result.MediaId);
        Assert.Equal("Folge 5", result.Title);
        Assert.Equal(15, result.StartPositionSeconds);
    }

    /// <summary>Prüft die Navigations-Methoden: HTTP 204 („kein Folgeeintrag") ergibt <c>null</c>.</summary>
    [Fact]
    public async Task Navigation_NoContent_ReturnsNull()
    {
        await LoginAsync();
        handler.Enqueue(HttpStatusCode.NoContent, "");

        var result = await CreateService().GetNextEntryAsync(7, 5);

        Assert.Null(result);
    }
}
