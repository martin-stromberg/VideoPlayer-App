#pragma warning disable xUnit1051 // Kurzlebige Unit-Tests: TestContext-CancellationToken nicht erforderlich
using System.Net;
using VideoPlayer.ApiClient;
using VideoPlayer.ApiClient.Models;
using VideoWebPlayer.Maui.Tests.Stubs;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>Tests der Playlist-Endpunkte des <c>VideoPlayerApiClient</c> (Issue 43, Schritt 4).</summary>
public class VideoPlayerApiClientTests_Playlists
{
    /// <summary>Prüft GetPlaylistsAsync: ruft <c>api/playlists</c> ab und mappt die DTO-Felder.</summary>
    [Fact]
    public async Task GetPlaylistsAsync_DeserializesPlaylists()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, """
            [{"Id":7,"Name":"Serien-Abend","Description":"Unsere Liste","SortMode":"Manual",
              "CreatedAt":"2026-01-01T10:00:00Z","UpdatedAt":"2026-02-01T10:00:00Z",
              "Genres":[{"Id":3,"Name":"Drama"}],"AllGenreIds":[3,5],
              "GenresManuallyOverridden":true,"CoverPictureId":44,
              "CoverPictureIsUserUploaded":true,"IsPublic":false,"IsOwner":true}]
            """);
        var client = ApiClientTestFactory.CreateClient(handler);

        var playlists = await client.GetPlaylistsAsync();

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("/api/playlists", request.PathAndQuery);

        var playlist = Assert.Single(playlists);
        Assert.Equal(7, playlist.Id);
        Assert.Equal("Serien-Abend", playlist.Name);
        Assert.Equal("Unsere Liste", playlist.Description);
        Assert.Equal(PlaylistSortModeValues.Manual, playlist.SortMode);
        var genre = Assert.Single(playlist.Genres);
        Assert.Equal(3, genre.Id);
        Assert.Equal("Drama", genre.Name);
        Assert.Equal([3L, 5L], playlist.AllGenreIds);
        Assert.True(playlist.GenresManuallyOverridden);
        Assert.Equal(44, playlist.CoverPictureId);
        Assert.True(playlist.CoverPictureIsUserUploaded);
        Assert.False(playlist.IsPublic);
        Assert.True(playlist.IsOwner);
    }

    /// <summary>Prüft GetPublicPlaylistsAsync: ruft <c>api/playlists/public</c> ab.</summary>
    [Fact]
    public async Task GetPublicPlaylistsAsync_UsesPublicEndpoint()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, "[]");
        var client = ApiClientTestFactory.CreateClient(handler);

        var playlists = await client.GetPublicPlaylistsAsync();

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("/api/playlists/public", request.PathAndQuery);
        Assert.Empty(playlists);
    }

    /// <summary>Prüft GetPlaylistAsync: ruft die Einzel-Playlist ab; 404 → <c>null</c>.</summary>
    [Fact]
    public async Task GetPlaylistAsync_NotFound_ReturnsNull()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.NotFound, """{"error":"not found"}""");
        var client = ApiClientTestFactory.CreateClient(handler);

        var playlist = await client.GetPlaylistAsync(9);

        var request = Assert.Single(handler.Requests);
        Assert.Equal("/api/playlists/9", request.PathAndQuery);
        Assert.Null(playlist);
    }

    /// <summary>Prüft GetPlaylistAsync: liefert die Playlist bei Erfolg.</summary>
    [Fact]
    public async Task GetPlaylistAsync_Found_ReturnsPlaylist()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, """
            {"Id":9,"Name":"Krimis","SortMode":"ByReleaseDate","IsPublic":true,"IsOwner":false}
            """);
        var client = ApiClientTestFactory.CreateClient(handler);

        var playlist = await client.GetPlaylistAsync(9);

        Assert.NotNull(playlist);
        Assert.Equal("Krimis", playlist.Name);
        Assert.True(playlist.IsPublic);
        Assert.False(playlist.IsOwner);
    }

    /// <summary>Prüft GetPlaylistEntriesPagedAsync: URL mit 1-basiertem Paging und DTO-Mapping.</summary>
    [Fact]
    public async Task GetPlaylistEntriesPagedAsync_UsesPagedUrlAndDeserializes()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, """
            {"Entries":[{"Id":1,"PlaylistId":7,"MediaType":"TVShowEpisode","MediaId":55,
              "MediaTitle":"Folge 4","ParentMediaType":"TVShow","ParentMediaId":12,
              "ParentMediaTitle":"Serie","AddedAt":"2026-02-01T10:00:00Z",
              "ResolvedPictureId":88,"IsAccessible":true,"SortOrder":2,
              "EpisodeNumber":4}],
             "TotalCount":21,"HasNextPage":true,"PageNumber":2,"PageSize":20}
            """);
        var client = ApiClientTestFactory.CreateClient(handler);

        var page = await client.GetPlaylistEntriesPagedAsync(7, pageNumber: 2, pageSize: 20);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("/api/playlists/7/entries/paged?pageNumber=2&pageSize=20", request.PathAndQuery);

        Assert.Equal(21, page.TotalCount);
        Assert.True(page.HasNextPage);
        Assert.Equal(2, page.PageNumber);
        var entry = Assert.Single(page.Entries);
        Assert.Equal(55, entry.MediaId);
        Assert.Equal("TVShowEpisode", entry.MediaType);
        Assert.Equal(12, entry.ParentMediaId);
        Assert.Equal(88, entry.ResolvedPictureId);
        Assert.Equal(4, entry.EpisodeNumber);
    }

    /// <summary>Prüft GetPlaylistCoverAsync: ruft das Cover-Bild ab; 404 → <c>null</c>.</summary>
    [Fact]
    public async Task GetPlaylistCoverAsync_NotFound_ReturnsNull()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.NotFound, """{"error":"no cover"}""");
        var client = ApiClientTestFactory.CreateClient(handler);

        var bytes = await client.GetPlaylistCoverAsync(7);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("/api/playlists/7/cover", request.PathAndQuery);
        Assert.Null(bytes);
    }

    /// <summary>Prüft GetPlaylistCoverAsync: liefert die Bilddaten bei Erfolg.</summary>
    [Fact]
    public async Task GetPlaylistCoverAsync_Found_ReturnsBytes()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent([137, 80, 78, 71])
        });
        var client = ApiClientTestFactory.CreateClient(handler);

        var bytes = await client.GetPlaylistCoverAsync(7);

        Assert.NotNull(bytes);
        Assert.Equal([137, 80, 78, 71], bytes);
    }

    /// <summary>Prüft PlaylistSortModeValues.GetDisplayLabel: liefert die deutsche Anzeigebezeichnung je Sortiermodus.</summary>
    /// <param name="sortMode">Der rohe Sortiermodus-Wert.</param>
    /// <param name="expected">Die erwartete Anzeigebezeichnung.</param>
    [Theory]
    [InlineData(PlaylistSortModeValues.Manual, "Manuell")]
    [InlineData(PlaylistSortModeValues.ByReleaseDate, "Nach Erscheinungsdatum")]
    [InlineData("AndererModus", "Nach Erscheinungsdatum")]
    [InlineData(null, "Nach Erscheinungsdatum")]
    public void GetDisplayLabel_MapsSortMode(string? sortMode, string expected)
    {
        Assert.Equal(expected, PlaylistSortModeValues.GetDisplayLabel(sortMode));
    }

    /// <summary>Prüft die Argument-Guards der Playlist-Methoden.</summary>
    [Fact]
    public async Task PlaylistMethods_InvalidArguments_Throw()
    {
        var handler = new StubHttpMessageHandler();
        var client = ApiClientTestFactory.CreateClient(handler);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => client.GetPlaylistAsync(0));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => client.GetPlaylistCoverAsync(-1));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => client.GetPlaylistEntriesPagedAsync(7, 0, 20));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => client.GetPlaylistEntriesPagedAsync(7, 1, 0));
        Assert.Empty(handler.Requests);
    }
}
