#pragma warning disable xUnit1051 // Kurzlebige Unit-Tests: TestContext-CancellationToken nicht erforderlich
using System.Net;
using System.Text;
using VideoPlayer.Mvu;
using VideoPlayer.Storage;
using VideoWebPlayer.Maui.Tests.Stubs;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>
/// Playlist-Service: Verwaltungsoperationen (Issue 43, Schritt 6) —
/// Endpunkt-Aufrufe, 409-Konflikt-Ergebnisse (Weiterschauen- und
/// Verlust-Bestätigung), Medientyp-Abbildung, Genre-/Öffentlich-
/// Operationen sowie die Admin-Claim-Lesung des Anmelde-Tokens.
/// </summary>
public class PlaylistServiceTests_Management
{
    private static readonly Uri Server = new("http://testserver/videoplayer/");

    private readonly StubHttpMessageHandler handler = new();
    private readonly AppSecretsService secrets;
    private readonly AuthSessionService authSession;

    /// <summary>Initialisiert eine neue Instanz der Testklasse <c>PlaylistServiceTests_Management</c>.</summary>
    public PlaylistServiceTests_Management()
    {
        secrets = new AppSecretsService(
            new InMemorySettingsStore(), new InMemorySecretStore(), AppProfile.Create(null));
        authSession = new AuthSessionService(
            secrets,
            uri => new HttpClient(handler, disposeHandler: false) { BaseAddress = uri },
            new RecordingDelays().Delay);
    }

    private PlaylistService CreateService() => new(authSession, secrets);

    private async Task LoginAsync()
    {
        handler.EnqueueJson(HttpStatusCode.OK, new { token = "jwt", expires = "2030-01-01T00:00:00Z" });
        await authSession.LoginAsync(Server, "martin", "pw");
        handler.Requests.Clear();
    }

    /// <summary>Prüft CreatePlaylistAsync: POST mit den Formular-Feldern und Mapping der Antwort.</summary>
    [Fact]
    public async Task CreatePlaylistAsync_PostsAndMaps()
    {
        await LoginAsync();
        handler.Enqueue(HttpStatusCode.OK, """
            {"Id":9,"Name":"Neue","Description":"Beschr.","SortMode":"Manual",
             "Genres":[],"AllGenreIds":[],"IsPublic":false,"IsOwner":true}
            """);

        var item = await CreateService().CreatePlaylistAsync("Neue", "Beschr.", "Manual");

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.EndsWith("/api/playlists", request.PathAndQuery);
        Assert.Equal(9, item.PlaylistId);
        Assert.True(item.IsOwner);
    }

    /// <summary>Prüft UpdatePlaylistAsync: PUT auf die Einzel-Playlist.</summary>
    [Fact]
    public async Task UpdatePlaylistAsync_PutsAndMaps()
    {
        await LoginAsync();
        handler.Enqueue(HttpStatusCode.OK, """
            {"Id":9,"Name":"Neu","Description":null,"SortMode":"Manual",
             "Genres":[],"AllGenreIds":[],"IsPublic":false,"IsOwner":true}
            """);

        var item = await CreateService().UpdatePlaylistAsync(9, "Neu", null);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Put, request.Method);
        Assert.EndsWith("/api/playlists/9", request.PathAndQuery);
        Assert.Equal("Neu", item.Name);
    }

    /// <summary>Prüft DeletePlaylistAsync: DELETE der Playlist.</summary>
    [Fact]
    public async Task DeletePlaylistAsync_Deletes()
    {
        await LoginAsync();
        handler.Enqueue(HttpStatusCode.NoContent);

        await CreateService().DeletePlaylistAsync(9);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Delete, request.Method);
        Assert.EndsWith("/api/playlists/9", request.PathAndQuery);
    }

    /// <summary>Prüft AddMediaToPlaylistAsync: bildet den Medientyp auf den Server-Enum-Namen ab und liefert den Hinweistext.</summary>
    [Fact]
    public async Task AddMediaToPlaylistAsync_MapsMediaType_ReturnsMessage()
    {
        await LoginAsync();
        handler.Enqueue(HttpStatusCode.OK, """
            {"AddedCount":1,"SkippedCount":0,"Message":"Hinzugefügt."}
            """);

        var message = await CreateService().AddMediaToPlaylistAsync(9, "movie", 42);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.EndsWith("/api/playlists/9/entries", request.PathAndQuery);
        Assert.Contains("\"MediaType\":\"Movie\"", request.Body);
        Assert.Equal("Hinzugefügt.", message);
    }

    /// <summary>Prüft RemoveEntryAsync: Erfolg ohne Konflikt.</summary>
    [Fact]
    public async Task RemoveEntryAsync_Success_ReturnsSuccess()
    {
        await LoginAsync();
        handler.Enqueue(HttpStatusCode.NoContent);

        var outcome = await CreateService().RemoveEntryAsync(9, "movie", 42);

        Assert.True(outcome.Succeeded);
        Assert.False(outcome.RequiresConfirmation);
        var request = Assert.Single(handler.Requests);
        Assert.EndsWith("/api/playlists/9/entries/Movie/42", request.PathAndQuery);
    }

    /// <summary>Prüft RemoveEntryAsync: 409 mit Weiterschauen-Flagge meldet den Bestätigungskonflikt.</summary>
    [Fact]
    public async Task RemoveEntryAsync_Conflict_ReturnsConfirmationRequired()
    {
        await LoginAsync();
        handler.Enqueue(HttpStatusCode.Conflict, """
            {"IsContinueWatchingConfirmationRequired":true}
            """);

        var outcome = await CreateService().RemoveEntryAsync(9, "movie", 42);

        Assert.False(outcome.Succeeded);
        Assert.True(outcome.RequiresConfirmation);
    }

    /// <summary>Prüft RemoveEntryAsync: 409 ohne Flagge läuft den normalen Fehlerpfad (Exception).</summary>
    [Fact]
    public async Task RemoveEntryAsync_ConflictWithoutFlag_Throws()
    {
        await LoginAsync();
        handler.Enqueue(HttpStatusCode.Conflict, """{"error":"conflict"}""");

        await Assert.ThrowsAsync<VideoPlayer.ApiClient.ApiException>(
            () => CreateService().RemoveEntryAsync(9, "movie", 42));
    }

    /// <summary>Prüft RemoveEntryAsync: die Bestätigungsflagge hängt als Query-Parameter an.</summary>
    [Fact]
    public async Task RemoveEntryAsync_Confirmed_AppendsFlag()
    {
        await LoginAsync();
        handler.Enqueue(HttpStatusCode.NoContent);

        await CreateService().RemoveEntryAsync(9, "movie", 42, confirmContinueWatchingRemoval: true);

        var request = Assert.Single(handler.Requests);
        Assert.EndsWith("?confirmContinueWatchingRemoval=true", request.PathAndQuery);
    }

    /// <summary>Prüft MoveEntryToBeginningAsync: POST des Eintrags.</summary>
    [Fact]
    public async Task MoveEntryToBeginningAsync_Posts()
    {
        await LoginAsync();
        handler.Enqueue(HttpStatusCode.OK, """{"Id":5,"PlaylistId":9,"MediaType":"Movie","MediaId":42,"MediaTitle":"Film"}""");

        await CreateService().MoveEntryToBeginningAsync(9, 5);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.EndsWith("/api/playlists/9/entries/5/move-to-beginning", request.PathAndQuery);
    }

    /// <summary>Prüft MoveEntryToEndAsync: ermittelt max-sort-order und setzt max + 1.</summary>
    [Fact]
    public async Task MoveEntryToEndAsync_UsesMaxPlusOne()
    {
        await LoginAsync();
        handler.Enqueue(HttpStatusCode.OK, """{"MaxSortOrder":4}""");
        handler.Enqueue(HttpStatusCode.NoContent);

        await CreateService().MoveEntryToEndAsync(9, 5);

        Assert.Equal(2, handler.Requests.Count);
        Assert.EndsWith("/api/playlists/9/entries/max-sort-order", handler.Requests[0].PathAndQuery);
        Assert.Equal(HttpMethod.Put, handler.Requests[1].Method);
        Assert.EndsWith("/api/playlists/9/entries/5/order", handler.Requests[1].PathAndQuery);
        Assert.Contains("\"NewSortOrder\":5", handler.Requests[1].Body);
    }

    /// <summary>Prüft MoveEntryBetweenAsync: POST mit der Ziel-SortOrder.</summary>
    [Fact]
    public async Task MoveEntryBetweenAsync_PostsTarget()
    {
        await LoginAsync();
        handler.Enqueue(HttpStatusCode.NoContent);

        await CreateService().MoveEntryBetweenAsync(9, 5, 2);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.EndsWith("/api/playlists/9/entries/5/move-between", request.PathAndQuery);
        Assert.Contains("\"NewSortOrder\":2", request.Body);
    }

    /// <summary>Prüft ChangeSortModeAsync: Erfolg mappt die aktualisierte Playlist.</summary>
    [Fact]
    public async Task ChangeSortModeAsync_Success_MapsPlaylist()
    {
        await LoginAsync();
        handler.Enqueue(HttpStatusCode.OK, """
            {"Id":9,"Name":"Liste","SortMode":"ByReleaseDate","Genres":[],"AllGenreIds":[],
             "IsPublic":false,"IsOwner":true}
            """);

        var result = await CreateService().ChangeSortModeAsync(9, "ByReleaseDate");

        Assert.False(result.RequiresConfirmation);
        Assert.NotNull(result.Playlist);
        Assert.Equal("ByReleaseDate", result.Playlist!.SortMode);
    }

    /// <summary>Prüft ChangeSortModeAsync: 409 mit Verlust-Flagge meldet den Bestätigungskonflikt.</summary>
    [Fact]
    public async Task ChangeSortModeAsync_Conflict_ReturnsConfirmationRequired()
    {
        await LoginAsync();
        handler.Enqueue(HttpStatusCode.Conflict, """
            {"IsLossOfDataConfirmationRequired":true}
            """);

        var result = await CreateService().ChangeSortModeAsync(9, "ByReleaseDate");

        Assert.True(result.RequiresConfirmation);
        Assert.Null(result.Playlist);
    }

    /// <summary>Prüft SetGenresAsync: PUT der Ids mit Mapping der aktualisierten Playlist.</summary>
    [Fact]
    public async Task SetGenresAsync_PutsAndMaps()
    {
        await LoginAsync();
        handler.Enqueue(HttpStatusCode.OK, """
            {"Id":9,"Name":"Liste","SortMode":"Manual",
             "Genres":[{"Id":3,"Name":"Drama"}],"AllGenreIds":[3],
             "GenresManuallyOverridden":true,"IsPublic":false,"IsOwner":true}
            """);

        var item = await CreateService().SetGenresAsync(9, [3]);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Put, request.Method);
        Assert.EndsWith("/api/playlists/9/genres", request.PathAndQuery);
        Assert.True(item.GenresManuallyOverridden);
        Assert.Equal("Drama", item.GenreText);
        Assert.Equal([3L], item.AllGenreIds);
    }

    /// <summary>Prüft ResetGenresAsync: POST des Resets.</summary>
    [Fact]
    public async Task ResetGenresAsync_Posts()
    {
        await LoginAsync();
        handler.Enqueue(HttpStatusCode.OK, """
            {"Id":9,"Name":"Liste","SortMode":"Manual","Genres":[],"AllGenreIds":[],
             "GenresManuallyOverridden":false,"IsPublic":false,"IsOwner":true}
            """);

        var item = await CreateService().ResetGenresAsync(9);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.EndsWith("/api/playlists/9/genres/reset", request.PathAndQuery);
        Assert.False(item.GenresManuallyOverridden);
    }

    /// <summary>Prüft SetPublicAsync: PUT des Flags mit Mapping.</summary>
    [Fact]
    public async Task SetPublicAsync_PutsAndMaps()
    {
        await LoginAsync();
        handler.Enqueue(HttpStatusCode.OK, """
            {"Id":9,"Name":"Liste","SortMode":"Manual","Genres":[],"AllGenreIds":[],
             "IsPublic":true,"IsOwner":true}
            """);

        var item = await CreateService().SetPublicAsync(9, true);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Put, request.Method);
        Assert.EndsWith("/api/playlists/9/public", request.PathAndQuery);
        Assert.True(item.IsPublic);
    }

    /// <summary>Prüft LoadGenreOptionsAsync: mappt die Optionen des Servers.</summary>
    [Fact]
    public async Task LoadGenreOptionsAsync_MapsOptions()
    {
        await LoginAsync();
        handler.Enqueue(HttpStatusCode.OK, """[{"Id":1,"Name":"Komödie"},{"Id":2,"Name":"Drama"}]""");

        var options = await CreateService().LoadGenreOptionsAsync();

        var request = Assert.Single(handler.Requests);
        Assert.EndsWith("/api/items/genres", request.PathAndQuery);
        Assert.Equal(2, options.Count);
        Assert.Equal("Komödie", options[0].Name);
        Assert.Equal("GenreOption_1", options[0].OptionAutomationId);
    }

    /// <summary>Prüft LoadOwnPlaylistsAsync: liefert nur die eigenen Playlists.</summary>
    [Fact]
    public async Task LoadOwnPlaylistsAsync_ReturnsOnlyOwn()
    {
        await LoginAsync();
        handler.Enqueue(HttpStatusCode.OK, """
            [{"Id":7,"Name":"Eigene","SortMode":"Manual","Genres":[],"AllGenreIds":[],
              "IsPublic":false,"IsOwner":true}]
            """);
        handler.Enqueue(HttpStatusCode.OK, """
            [{"Id":8,"Name":"Fremde","SortMode":"Manual","Genres":[],"AllGenreIds":[],
              "IsPublic":true,"IsOwner":false}]
            """);

        var own = await CreateService().LoadOwnPlaylistsAsync();

        var item = Assert.Single(own);
        Assert.Equal("Eigene", item.Name);
    }

    /// <summary>Prüft IsCurrentUserAdminAsync: Token mit IsAdmin-Claim → true, ohne → false.</summary>
    [Fact]
    public async Task IsCurrentUserAdminAsync_ReadsClaim()
    {
        var service = CreateService();

        await secrets.SetAuthTokenAsync(BuildToken("{\"IsAdmin\":\"True\"}"));
        Assert.True(await service.IsCurrentUserAdminAsync());

        await secrets.SetAuthTokenAsync(BuildToken("{\"IsAdmin\":\"False\"}"));
        Assert.False(await service.IsCurrentUserAdminAsync());

        await secrets.SetAuthTokenAsync("kein-jwt");
        Assert.False(await service.IsCurrentUserAdminAsync());
    }

    /// <summary>Baut ein JWT-artiges Token mit der gegebenen Payload.</summary>
    /// <param name="payloadJson">Die JSON-Payload.</param>
    /// <returns>Das Token im JWT-Format.</returns>
    private static string BuildToken(string payloadJson)
    {
        static string B64(string text) =>
            Convert.ToBase64String(Encoding.UTF8.GetBytes(text))
                .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return $"{B64("{\"alg\":\"none\"}")}.{B64(payloadJson)}.sig";
    }
}
