#pragma warning disable xUnit1051 // Kurzlebige Unit-Tests: TestContext-CancellationToken nicht erforderlich
using System.Net;
using VideoPlayer.ApiClient;
using VideoPlayer.ApiClient.Models;
using VideoWebPlayer.Maui.Tests.Stubs;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>Tests der Playlist-Verwaltungs-Endpunkte des <c>VideoPlayerApiClient</c> (Issue 43, Schritt 6).</summary>
public class VideoPlayerApiClientTests_PlaylistAdmin
{
    /// <summary>Prüft CreatePlaylistAsync: POST auf api/playlists mit Name/Beschreibung/Sortiermodus.</summary>
    [Fact]
    public async Task CreatePlaylistAsync_PostsToPlaylists()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, """
            {"Id":9,"Name":"Neue Liste","Description":"Beschreibung","SortMode":"Manual",
             "Genres":[],"AllGenreIds":[],"IsPublic":false,"IsOwner":true}
            """);
        var client = ApiClientTestFactory.CreateClient(handler);

        var playlist = await client.CreatePlaylistAsync(new CreatePlaylistRequest
        {
            Name = "Neue Liste",
            Description = "Beschreibung",
            SortMode = PlaylistSortModeValues.Manual
        });

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/api/playlists", request.PathAndQuery);
        Assert.Contains("\"Name\":\"Neue Liste\"", request.Body);
        Assert.Contains("\"Description\":\"Beschreibung\"", request.Body);
        Assert.Contains("\"SortMode\":\"Manual\"", request.Body);
        Assert.Equal(9, playlist.Id);
        Assert.True(playlist.IsOwner);
    }

    /// <summary>Prüft UpdatePlaylistAsync: PUT auf die Einzel-Playlist.</summary>
    [Fact]
    public async Task UpdatePlaylistAsync_PutsToPlaylist()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, """
            {"Id":9,"Name":"Umbenannt","Description":null,"SortMode":"Manual",
             "Genres":[],"AllGenreIds":[],"IsPublic":false,"IsOwner":true}
            """);
        var client = ApiClientTestFactory.CreateClient(handler);

        var playlist = await client.UpdatePlaylistAsync(9, new UpdatePlaylistRequest
        {
            Name = "Umbenannt",
            Description = null
        });

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Put, request.Method);
        Assert.Equal("/api/playlists/9", request.PathAndQuery);
        Assert.Contains("\"Name\":\"Umbenannt\"", request.Body);
        Assert.Equal("Umbenannt", playlist.Name);
    }

    /// <summary>Prüft DeletePlaylistAsync: DELETE auf die Einzel-Playlist.</summary>
    [Fact]
    public async Task DeletePlaylistAsync_DeletesPlaylist()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.NoContent);
        var client = ApiClientTestFactory.CreateClient(handler);

        await client.DeletePlaylistAsync(9);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Delete, request.Method);
        Assert.Equal("/api/playlists/9", request.PathAndQuery);
    }

    /// <summary>Prüft AddPlaylistEntryAsync: POST des Eintrags mit Medientyp/-Id.</summary>
    [Fact]
    public async Task AddPlaylistEntryAsync_PostsEntry()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, """
            {"AddedCount":1,"SkippedCount":0,"Message":"Hinzugefügt."}
            """);
        var client = ApiClientTestFactory.CreateClient(handler);

        var result = await client.AddPlaylistEntryAsync(9, new AddPlaylistEntryRequest
        {
            MediaType = "Movie",
            MediaId = 42
        });

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/api/playlists/9/entries", request.PathAndQuery);
        Assert.Contains("\"MediaType\":\"Movie\"", request.Body);
        Assert.Contains("\"MediaId\":42", request.Body);
        Assert.Equal(1, result.AddedCount);
        Assert.Equal("Hinzugefügt.", result.Message);
    }

    /// <summary>Prüft RemovePlaylistEntryAsync: DELETE ohne Flagge ohne Query-Parameter.</summary>
    [Fact]
    public async Task RemovePlaylistEntryAsync_DeletesEntryWithoutConfirm()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.NoContent);
        var client = ApiClientTestFactory.CreateClient(handler);

        await client.RemovePlaylistEntryAsync(9, "Movie", 42);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Delete, request.Method);
        Assert.Equal("/api/playlists/9/entries/Movie/42", request.PathAndQuery);
    }

    /// <summary>Prüft RemovePlaylistEntryAsync: die Bestätigungsflagge hängt als Query-Parameter an.</summary>
    [Fact]
    public async Task RemovePlaylistEntryAsync_WithConfirm_AppendsQueryFlag()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.NoContent);
        var client = ApiClientTestFactory.CreateClient(handler);

        await client.RemovePlaylistEntryAsync(9, "Movie", 42, confirmContinueWatchingRemoval: true);

        var request = Assert.Single(handler.Requests);
        Assert.Equal("/api/playlists/9/entries/Movie/42?confirmContinueWatchingRemoval=true",
            request.PathAndQuery);
    }

    /// <summary>Prüft RemovePlaylistEntryAsync: HTTP 409 wirft ApiException mit lesbarem ResponseBody.</summary>
    [Fact]
    public async Task RemovePlaylistEntryAsync_Conflict_ThrowsApiException()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.Conflict, """
            {"IsContinueWatchingConfirmationRequired":true}
            """);
        var client = ApiClientTestFactory.CreateClient(handler);

        var exception = await Assert.ThrowsAsync<ApiException>(
            () => client.RemovePlaylistEntryAsync(9, "Movie", 42));

        Assert.Equal(HttpStatusCode.Conflict, exception.StatusCode);
        Assert.Contains("IsContinueWatchingConfirmationRequired", exception.ResponseBody);
    }

    /// <summary>Prüft MovePlaylistEntryToBeginningAsync: POST auf move-to-beginning.</summary>
    [Fact]
    public async Task MovePlaylistEntryToBeginningAsync_PostsMove()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, """
            {"Id":5,"PlaylistId":9,"MediaType":"Movie","MediaId":42,"MediaTitle":"Film","SortOrder":0}
            """);
        var client = ApiClientTestFactory.CreateClient(handler);

        var entry = await client.MovePlaylistEntryToBeginningAsync(9, 5);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/api/playlists/9/entries/5/move-to-beginning", request.PathAndQuery);
        Assert.Equal(5, entry.Id);
    }

    /// <summary>Prüft MovePlaylistEntryBetweenAsync: POST mit Ziel-SortOrder im Body.</summary>
    [Fact]
    public async Task MovePlaylistEntryBetweenAsync_PostsTargetOrder()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.NoContent);
        var client = ApiClientTestFactory.CreateClient(handler);

        await client.MovePlaylistEntryBetweenAsync(
            9, 5, new ReorderPlaylistEntryRequest { NewSortOrder = 2 });

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/api/playlists/9/entries/5/move-between", request.PathAndQuery);
        Assert.Contains("\"NewSortOrder\":2", request.Body);
    }

    /// <summary>Prüft ReorderPlaylistEntryAsync: PUT der SortOrder.</summary>
    [Fact]
    public async Task ReorderPlaylistEntryAsync_PutsOrder()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.NoContent);
        var client = ApiClientTestFactory.CreateClient(handler);

        await client.ReorderPlaylistEntryAsync(
            9, 5, new ReorderPlaylistEntryRequest { NewSortOrder = 7 });

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Put, request.Method);
        Assert.Equal("/api/playlists/9/entries/5/order", request.PathAndQuery);
        Assert.Contains("\"NewSortOrder\":7", request.Body);
    }

    /// <summary>Prüft GetPlaylistMaxSortOrderAsync: GET des Maximums.</summary>
    [Fact]
    public async Task GetPlaylistMaxSortOrderAsync_ReadsMaximum()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, """{"MaxSortOrder":4}""");
        var client = ApiClientTestFactory.CreateClient(handler);

        var max = await client.GetPlaylistMaxSortOrderAsync(9);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("/api/playlists/9/entries/max-sort-order", request.PathAndQuery);
        Assert.Equal(4, max);
    }

    /// <summary>Prüft ChangePlaylistSortModeAsync: PATCH mit Bestätigungsflagge im Body.</summary>
    [Fact]
    public async Task ChangePlaylistSortModeAsync_PatchesSortMode()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, """
            {"Id":9,"Name":"Liste","SortMode":"ByReleaseDate","Genres":[],"AllGenreIds":[],
             "IsPublic":false,"IsOwner":true}
            """);
        var client = ApiClientTestFactory.CreateClient(handler);

        var playlist = await client.ChangePlaylistSortModeAsync(9, new ChangeSortModeRequest
        {
            NewSortMode = PlaylistSortModeValues.ByReleaseDate,
            ConfirmLossOfManualOrder = true
        });

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Patch, request.Method);
        Assert.Equal("/api/playlists/9/sort-mode", request.PathAndQuery);
        Assert.Contains("\"NewSortMode\":\"ByReleaseDate\"", request.Body);
        Assert.Contains("\"ConfirmLossOfManualOrder\":true", request.Body);
        Assert.Equal(PlaylistSortModeValues.ByReleaseDate, playlist.SortMode);
    }

    /// <summary>Prüft SetPlaylistGenresAsync: PUT der Genre-Ids.</summary>
    [Fact]
    public async Task SetPlaylistGenresAsync_PutsGenreIds()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, """
            {"Id":9,"Name":"Liste","SortMode":"Manual",
             "Genres":[{"Id":3,"Name":"Drama"}],"AllGenreIds":[3],
             "GenresManuallyOverridden":true,"IsPublic":false,"IsOwner":true}
            """);
        var client = ApiClientTestFactory.CreateClient(handler);

        var playlist = await client.SetPlaylistGenresAsync(
            9, new SetPlaylistGenresRequest { GenreIds = [3] });

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Put, request.Method);
        Assert.Equal("/api/playlists/9/genres", request.PathAndQuery);
        Assert.Contains("\"GenreIds\":[3]", request.Body);
        Assert.True(playlist.GenresManuallyOverridden);
        Assert.Equal([3L], playlist.AllGenreIds);
    }

    /// <summary>Prüft ResetPlaylistGenresAsync: POST auf genres/reset.</summary>
    [Fact]
    public async Task ResetPlaylistGenresAsync_PostsReset()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, """
            {"Id":9,"Name":"Liste","SortMode":"Manual","Genres":[],"AllGenreIds":[],
             "GenresManuallyOverridden":false,"IsPublic":false,"IsOwner":true}
            """);
        var client = ApiClientTestFactory.CreateClient(handler);

        var playlist = await client.ResetPlaylistGenresAsync(9);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/api/playlists/9/genres/reset", request.PathAndQuery);
        Assert.False(playlist.GenresManuallyOverridden);
    }

    /// <summary>Prüft SetPlaylistPublicAsync: PUT des Öffentlich-Flags.</summary>
    [Fact]
    public async Task SetPlaylistPublicAsync_PutsPublicFlag()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, """
            {"Id":9,"Name":"Liste","SortMode":"Manual","Genres":[],"AllGenreIds":[],
             "IsPublic":true,"IsOwner":true}
            """);
        var client = ApiClientTestFactory.CreateClient(handler);

        var playlist = await client.SetPlaylistPublicAsync(
            9, new SetPlaylistPublicRequest { IsPublic = true });

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Put, request.Method);
        Assert.Equal("/api/playlists/9/public", request.PathAndQuery);
        Assert.Contains("\"IsPublic\":true", request.Body);
        Assert.True(playlist.IsPublic);
    }
}
