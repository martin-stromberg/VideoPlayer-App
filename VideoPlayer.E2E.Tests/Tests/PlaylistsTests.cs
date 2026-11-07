using VideoPlayer.E2E.Tests.Support;

namespace VideoPlayer.E2E.Tests.Tests;

/// <summary>
/// E2E-Szenarien der Playlist-Anbindung (Issue #43, Schritt 4): der
/// Menüeintrag „Playlists" öffnet die Übersicht mit eigenen und
/// öffentlichen Playlists (Cover-Abruf über
/// <c>api/playlists/{id}/cover</c>), die Auswahl öffnet die
/// Detailansicht mit den Einträgen in Server-Reihenfolge, und ein
/// ausgefallener Playlist-Bereich zeigt eine verständliche
/// Fehleranzeige mit funktionierendem Neuversuch — ohne Absturz.
/// </summary>
public sealed class PlaylistsTests : E2ETestBase
{
    private const long OwnPlaylistId = 701;
    private const long PublicPlaylistId = 702;

    /// <summary>Initialisiert die Testklasse.</summary>
    /// <param name="fixture">Geteilte Build-Fixture.</param>
    public PlaylistsTests(AppFixture fixture) : base(fixture) { }

    private static MockVideoServer NewServer() =>
        new("AAAAAA", "e2e-device-playlists", "e2e@example.test", "e2e-pass");

    private static MockPlaylistEntry MovieEntry(long id, long mediaId, string title) =>
        new() { Id = id, MediaType = "Movie", MediaId = mediaId, Title = title };

    /// <summary>
    /// Pflichtszenario (AK 1 + 2): der Menüeintrag „Playlists" öffnet
    /// die Übersicht; eigene und öffentliche Playlists werden mit
    /// Sektionen aufgelistet.
    /// </summary>
    [Fact]
    public async Task PlaylistsMenuItem_ShowsOwnAndPublicPlaylists()
    {
        using var server = NewServer();
        server.Playlists.Add(new MockPlaylist
        {
            Id = OwnPlaylistId,
            Name = "E2E Eigene Liste",
            IsOwner = true,
            Entries = { MovieEntry(1, 501, "Film Eins") }
        });
        server.Playlists.Add(new MockPlaylist
        {
            Id = PublicPlaylistId,
            Name = "E2E Öffentliche Liste",
            IsOwner = false,
            IsPublic = true,
            Entries = { MovieEntry(2, 502, "Film Zwei") }
        });
        using var app = await StartAppAsync();
        await ConnectToPairingRequiredAsync(app, server);
        await PairAsync(app, server);
        await LoginAsync(app, server);

        app.Ui.OpenFlyoutItem("Playlists");

        var model = await app.DevApi.WaitForModelAsync(
            m => m.PlaylistsPageItemCount == 2,
            description: "Playlists-Übersicht zeigt zwei Einträge",
            cancellationToken: Ct);
        Assert.Null(model.PlaylistsPageError);

        var tree = await app.DevApi.GetTreeAsync(Ct);
        var collection = tree.FindByAutomationId("PlaylistsCollection");
        Assert.NotNull(collection);
        var titles = collection!.Items?.Select(i => i.Text).ToList() ?? [];
        Assert.Contains("E2E Eigene Liste", titles);
        Assert.Contains("E2E Öffentliche Liste", titles);
        // Sektionsüberschriften stehen in derselben flachen Liste.
        Assert.Contains("Meine Playlists", titles);
        Assert.Contains("Öffentliche Playlists", titles);
        Assert.True(server.WasPathRequested("/api/playlists"));
        Assert.True(server.WasPathRequested("/api/playlists/public"));
    }

    /// <summary>
    /// Pflichtszenario (AK 3): die Auswahl einer Playlist öffnet die
    /// Detailansicht und zeigt die Einträge in der Server-Reihenfolge
    /// (der Server mischt die Reihenfolge absichtlich — die App darf
    /// nicht umsortieren).
    /// </summary>
    [Fact]
    public async Task SelectingPlaylist_ShowsEntriesInServerOrder()
    {
        using var server = NewServer();
        var playlist = new MockPlaylist
        {
            Id = OwnPlaylistId,
            Name = "E2E Reihenfolge",
            IsOwner = true
        };
        // Bewusst nicht id-/titel-sortiert — die gelieferte Reihenfolge
        // muss 1:1 sichtbar bleiben.
        playlist.Entries.Add(MovieEntry(3, 503, "C-Film"));
        playlist.Entries.Add(MovieEntry(1, 501, "A-Film"));
        playlist.Entries.Add(MovieEntry(2, 502, "B-Film"));
        playlist.Entries.Add(new MockPlaylistEntry
        {
            Id = 4,
            MediaType = "TVShowEpisode",
            MediaId = 504,
            Title = "Pilotfolge",
            ParentMediaType = "TVShow",
            ParentMediaId = 900,
            ParentTitle = "E2E Serie",
            EpisodeNumber = 1
        });
        server.Playlists.Add(playlist);
        using var app = await StartAppAsync();
        await ConnectToPairingRequiredAsync(app, server);
        await PairAsync(app, server);
        await LoginAsync(app, server);
        app.Ui.OpenFlyoutItem("Playlists");
        await app.DevApi.WaitForModelAsync(
            m => m.PlaylistsPageItemCount == 1,
            description: "Übersicht zeigt die Playlist",
            cancellationToken: Ct);

        // Sektionsheader steht vor dem Eintrag → Index 1 in der flachen Liste.
        await app.DevApi.InvokeAsync("PlaylistsCollection", 1, Ct);

        var model = await app.DevApi.WaitForModelAsync(
            m => m.PlaylistDetailsId == OwnPlaylistId && m.PlaylistDetailsEntryCount == 4,
            description: "Detailansicht zeigt vier Einträge",
            cancellationToken: Ct);
        Assert.Null(model.PlaylistDetailsError);

        var tree = await app.DevApi.GetTreeAsync(Ct);
        var collection = tree.FindByAutomationId("PlaylistEntriesCollection");
        var titles = collection?.Items?.Select(i => i.Text).ToList() ?? [];
        // Server-Reihenfolge: C, A, B, Episode — keine Client-Sortierung.
        Assert.Equal(["C-Film", "A-Film", "B-Film", "Pilotfolge"], titles);
        Assert.True(server.WasPathRequested("/api/playlists/701/entries/paged"));
    }

    /// <summary>
    /// Pflichtszenario (AK 4): eine Playlist mit Cover bewirkt den
    /// Abruf von <c>api/playlists/{id}/cover</c>; ohne Cover bleibt der
    /// Titel-Fallback.
    /// </summary>
    [Fact]
    public async Task PlaylistWithCover_RequestsCoverEndpoint()
    {
        using var server = NewServer();
        // Minimales valides PNG (1×1 Pixel) als Cover-Bild.
        var png = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");
        server.Playlists.Add(new MockPlaylist
        {
            Id = OwnPlaylistId,
            Name = "E2E Cover-Liste",
            IsOwner = true,
            CoverPictureId = 55,
            CoverPng = png,
            Entries = { MovieEntry(1, 501, "Film Eins") }
        });
        server.Playlists.Add(new MockPlaylist
        {
            Id = PublicPlaylistId,
            Name = "E2E Ohne Cover",
            IsOwner = false,
            IsPublic = true,
            Entries = { MovieEntry(2, 502, "Film Zwei") }
        });
        using var app = await StartAppAsync();
        await ConnectToPairingRequiredAsync(app, server);
        await PairAsync(app, server);
        await LoginAsync(app, server);
        app.Ui.OpenFlyoutItem("Playlists");
        await app.DevApi.WaitForModelAsync(
            m => m.PlaylistsPageItemCount == 2,
            description: "Übersicht zeigt beide Playlists",
            cancellationToken: Ct);

        // Der Cover-Abruf läuft asynchron beim Rendern der Karte.
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);
        while (!server.WasPathRequested("/api/playlists/701/cover"))
        {
            Assert.True(DateTime.UtcNow < deadline, "Cover-Endpunkt wurde nicht angefragt.");
            await Task.Delay(250, Ct);
        }

        // Die Playlist ohne Cover fragt nichts ab.
        Assert.False(server.WasPathRequested("/api/playlists/702/cover"));
    }

    /// <summary>
    /// Pflichtszenario (AK 5): ein ausgefallener Playlist-Bereich zeigt
    /// eine verständliche Fehleranzeige mit Neuversuch — die App stürzt
    /// nicht ab, und der Neuversuch nach Wiederherstellung lädt die
    /// Liste erfolgreich.
    /// </summary>
    [Fact]
    public async Task PlaylistsUnavailable_ShowsError_AndRetryLoads()
    {
        using var server = NewServer();
        server.PlaylistsUnavailable = true;
        using var app = await StartAppAsync();
        await ConnectToPairingRequiredAsync(app, server);
        await PairAsync(app, server);
        await LoginAsync(app, server);

        app.Ui.OpenFlyoutItem("Playlists");

        var failed = await app.DevApi.WaitForModelAsync(
            m => m.PlaylistsPageError != null,
            description: "Fehleranzeige ohne verfügbare Playlist-Endpunkte",
            cancellationToken: Ct);
        Assert.False(string.IsNullOrWhiteSpace(failed.PlaylistsPageError));
        Assert.Equal(0, failed.PlaylistsPageItemCount);
        var tree = await app.DevApi.GetTreeAsync(Ct);
        Assert.NotNull(tree.FindByAutomationId("PlaylistsErrorLabel"));
        Assert.NotNull(tree.FindByAutomationId("PlaylistsRetryButton"));

        // Wiederherstellung: der Neuversuch lädt die Liste.
        server.PlaylistsUnavailable = false;
        server.Playlists.Add(new MockPlaylist
        {
            Id = OwnPlaylistId,
            Name = "E2E Wieder da",
            IsOwner = true,
            Entries = { MovieEntry(1, 501, "Film Eins") }
        });
        await app.DevApi.InvokeAsync("PlaylistsRetryButton", cancellationToken: Ct);

        await app.DevApi.WaitForModelAsync(
            m => m.PlaylistsPageItemCount == 1 && m.PlaylistsPageError == null,
            description: "Neuversuch lädt die Playlists erfolgreich",
            cancellationToken: Ct);
    }
}
