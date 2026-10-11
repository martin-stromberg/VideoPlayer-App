using VideoPlayer.E2E.Tests.Support;

namespace VideoPlayer.E2E.Tests.Tests;

/// <summary>
/// E2E-Szenarien der Playlist-Wiedergabe (Issue #43, Schritt 5):
/// Wiedergabestart aus der Playlist-Detailansicht (ab gewähltem
/// Eintrag bzw. erstem abspielbarem Eintrag), manuelle Vor-/
/// Rückwärts-Navigation, Auto-Fortsetzung und Fehler-Rückfall über
/// die serverseitigen Play-Endpunkte sowie der playlist-gebundene
/// Weiterschauen-Eintrag, dessen Folgevideo der Playlist-Reihenfolge
/// folgt — nicht der Serienreihenfolge.
/// </summary>
public sealed class PlaylistPlaybackTests : E2ETestBase
{
    private const long PlaylistId = 701;
    private const long ShowId = 8101;
    private const long EpisodeId = 8201;
    private const string EpisodeTitle = "E2E Playlist-Episode";

    /// <summary>Initialisiert die Testklasse.</summary>
    /// <param name="fixture">Geteilte Build-Fixture.</param>
    public PlaylistPlaybackTests(AppFixture fixture) : base(fixture) { }

    private static string TestVideo => Path.Combine(AppContext.BaseDirectory, "Assets", "testvideo.mp4");

    private static MockVideoServer NewServer() =>
        new("AAAAAA", "e2e-device-playlist-play", "e2e@example.test", "e2e-pass")
        {
            PlaybackVideoPath = TestVideo
        };

    private static MockPlaylistEntry MovieEntry(long id, long mediaId, string title) =>
        new() { Id = id, MediaType = "Movie", MediaId = mediaId, Title = title };

    /// <summary>
    /// Gemeinsamer Einstieg: anmelden, Playlists-Übersicht öffnen und
    /// die Detailansicht der Playlist erreichen (Einträge geladen).
    /// </summary>
    /// <param name="app">Die laufende App-Instanz.</param>
    /// <param name="server">Der Mock-Server.</param>
    /// <param name="playlistId">Die erwartete Playlist-Id.</param>
    /// <param name="entryCount">Die erwartete Eintragszahl.</param>
    /// <returns>Eine abgeschlossene Aufgabe nach Erreichen der Detailansicht.</returns>
    private static async Task OpenPlaylistDetailsAsync(
        AppInstance app, MockVideoServer server, long playlistId, int entryCount)
    {
        await ConnectToPairingRequiredAsync(app, server);
        await PairAsync(app, server);
        await LoginAsync(app, server);

        app.Ui.OpenFlyoutItem("Playlists");
        await app.DevApi.WaitForModelAsync(
            m => m.PlaylistsPageItemCount == 1,
            description: "Playlists-Übersicht zeigt die Playlist",
            cancellationToken: Ct);

        // Sektionsheader steht vor dem Eintrag → Index 1.
        await app.DevApi.InvokeAsync("PlaylistsCollection", 1, Ct);

        await app.DevApi.WaitForModelAsync(
            m => m.PlaylistDetailsId == playlistId && m.PlaylistDetailsEntryCount == entryCount,
            description: "Playlist-Detailansicht zeigt alle Einträge",
            cancellationToken: Ct);
    }

    /// <summary>
    /// Pflichtszenario (AK 1): „Abspielen" auf der Detailseite startet
    /// die Wiedergabe beim ersten abspielbaren Eintrag — der führende,
    /// nicht abspielbare Serieneintrag wird serverseitig übersprungen.
    /// </summary>
    [Fact]
    public async Task PlayButton_StartsPlaybackAtFirstPlayableEntry()
    {
        Assert.True(File.Exists(TestVideo), $"Testvideo fehlt im Output-Verzeichnis: {TestVideo}");
        using var server = NewServer();
        server.Playlists.Add(new MockPlaylist
        {
            Id = PlaylistId,
            Name = "E2E Abspielen",
            IsOwner = true,
            Entries =
            {
                new MockPlaylistEntry
                {
                    Id = 1, MediaType = "TVShow", MediaId = 900, Title = "Serien-Referenz"
                },
                MovieEntry(5, 501, "Erster Film"),
                MovieEntry(6, 502, "Zweiter Film")
            }
        });
        using var app = await StartAppAsync();
        await OpenPlaylistDetailsAsync(app, server, PlaylistId, 3);

        await app.DevApi.InvokeAsync("PlaylistDetailsPlayButton", cancellationToken: Ct);

        // Stabiler Endzustand: das kurze Testvideo läuft über die
        // Auto-Fortsetzung bis zum letzten Eintrag durch — der erste
        // abspielbare Eintrag (501) ist Zwischenzustand und wird über
        // die angefragten Pfade nachgewiesen.
        var model = await app.DevApi.WaitForModelAsync(
            m => m.PlayerIsActive && m.PlayerMediaId == 502 && m.PlayerPlaylistId == PlaylistId,
            TimeSpan.FromSeconds(60),
            "Playlist-Wiedergabe bis zum letzten Eintrag", Ct);
        Assert.False(app.Process.HasExited, "Die App wurde beim Wiedergabestart beendet.");
        Assert.Equal(6, model.PlayerPlaylistEntryId);
        Assert.Equal(3, model.PlayerPlaylistPosition);
        Assert.True(server.WasPathRequested("/api/playlists/701/play"),
            "Der Wiedergabestart-Endpunkt wurde nicht angefragt.");
        Assert.True(server.WasPathRequested("/api/items/movie/501/stream"),
            "Der Stream des ersten abspielbaren Eintrags wurde nicht angefragt — der Serieneintrag wurde nicht übersprungen.");
        Assert.False(server.WasPathRequested("/api/items/tvshow/900/stream"),
            "Der nicht abspielbare Serieneintrag wurde abgespielt.");
    }

    /// <summary>
    /// Pflichtszenario (AK 1): ein Tap auf einen Eintrag startet die
    /// Wiedergabe ab genau diesem Eintrag (entryId des
    /// Wiedergabestarts), nicht am Listenanfang.
    /// </summary>
    [Fact]
    public async Task SelectEntry_StartsPlaybackAtThatEntry()
    {
        Assert.True(File.Exists(TestVideo), $"Testvideo fehlt im Output-Verzeichnis: {TestVideo}");
        using var server = NewServer();
        server.Playlists.Add(new MockPlaylist
        {
            Id = PlaylistId,
            Name = "E2E Eintragswahl",
            IsOwner = true,
            Entries =
            {
                MovieEntry(5, 501, "Erster Film"),
                MovieEntry(6, 502, "Zweiter Film")
            }
        });
        using var app = await StartAppAsync();
        await OpenPlaylistDetailsAsync(app, server, PlaylistId, 2);

        // Der zweite Eintrag wird gewählt (Index 1 der Eintragsliste).
        await app.DevApi.InvokeAsync("PlaylistEntriesCollection", 1, Ct);

        var model = await app.DevApi.WaitForModelAsync(
            m => m.PlayerIsActive && m.PlayerMediaId == 502,
            TimeSpan.FromSeconds(60),
            "Wiedergabe ab dem gewählten Eintrag", Ct);
        Assert.False(app.Process.HasExited, "Die App wurde beim Wiedergabestart beendet.");
        Assert.Equal(PlaylistId, model.PlayerPlaylistId);
        Assert.Equal(6, model.PlayerPlaylistEntryId);
        Assert.Equal(2, model.PlayerPlaylistPosition);
    }

    /// <summary>
    /// Pflichtszenario (AK 2): die Player-Schaltflächen „Weiter"/„Zurück"
    /// navigieren über <c>play/next</c>/<c>play/previous</c> innerhalb der
    /// Playlist-Reihenfolge. Vom stabilen Endzustand (letzter Eintrag)
    /// führt „Zurück" zum vorherigen Eintrag, „Weiter" von dort wieder
    /// zum letzten — der Fortschritts-Takt (5 s) lässt den Wechsel
    /// beobachtbar, bevor die Auto-Fortsetzung erneut zugreift.
    /// </summary>
    [Fact]
    public async Task NextAndPreviousButtons_NavigatePlaylist()
    {
        Assert.True(File.Exists(TestVideo), $"Testvideo fehlt im Output-Verzeichnis: {TestVideo}");
        using var server = NewServer();
        server.Playlists.Add(new MockPlaylist
        {
            Id = PlaylistId,
            Name = "E2E Navigation",
            IsOwner = true,
            Entries =
            {
                MovieEntry(5, 501, "Erster Film"),
                MovieEntry(6, 502, "Zweiter Film")
            }
        });
        using var app = await StartAppAsync();
        await OpenPlaylistDetailsAsync(app, server, PlaylistId, 2);
        await app.DevApi.InvokeAsync("PlaylistDetailsPlayButton", cancellationToken: Ct);

        // Stabiler Ausgangsstand: letzter Eintrag der Playlist.
        await app.DevApi.WaitForModelAsync(
            m => m.PlayerIsActive && m.PlayerMediaId == 502 && m.PlayerPlaylistEntryId == 6,
            TimeSpan.FromSeconds(60),
            "Wiedergabe des letzten Eintrags", Ct);

        await app.DevApi.InvokeAsync("PlayerPreviousButton", cancellationToken: Ct);

        var model = await app.DevApi.WaitForModelAsync(
            m => m.PlayerIsActive && m.PlayerMediaId == 501 && m.PlayerPlaylistEntryId == 5,
            TimeSpan.FromSeconds(60),
            "Rückwärts-Navigation zum ersten Eintrag", Ct);
        Assert.Equal(1, model.PlayerPlaylistPosition);
        Assert.True(server.WasPathRequested("/api/playlists/701/play/previous"),
            "Der previous-Endpunkt wurde nicht angefragt.");

        await app.DevApi.InvokeAsync("PlayerNextButton", cancellationToken: Ct);

        model = await app.DevApi.WaitForModelAsync(
            m => m.PlayerIsActive && m.PlayerMediaId == 502 && m.PlayerPlaylistEntryId == 6,
            TimeSpan.FromSeconds(60),
            "Vorwärts-Navigation zurück zum letzten Eintrag", Ct);
        Assert.Equal(2, model.PlayerPlaylistPosition);
        Assert.True(server.WasPathRequested("/api/playlists/701/play/next"),
            "Der next-Endpunkt wurde nicht angefragt.");
    }

    /// <summary>
    /// Pflichtszenario (AK 2): die Auto-Fortsetzung am Titelende holt
    /// das Folgevideo über <c>play/advance</c> aus der Playlist-Reihenfolge.
    /// </summary>
    [Fact]
    public async Task PlaybackEnd_AdvancesToNextPlaylistEntry()
    {
        Assert.True(File.Exists(TestVideo), $"Testvideo fehlt im Output-Verzeichnis: {TestVideo}");
        using var server = NewServer();
        server.Playlists.Add(new MockPlaylist
        {
            Id = PlaylistId,
            Name = "E2E Fortsetzung",
            IsOwner = true,
            Entries =
            {
                MovieEntry(5, 501, "Erster Film"),
                MovieEntry(6, 502, "Folge-Film")
            }
        });
        using var app = await StartAppAsync();
        await OpenPlaylistDetailsAsync(app, server, PlaylistId, 2);
        await app.DevApi.InvokeAsync("PlaylistDetailsPlayButton", cancellationToken: Ct);

        // Das kurze Testvideo liegt sofort im Fortsetzungsfenster → der
        // Player schaltet über den advance-Endpunkt zum Folge-Eintrag
        // weiter; der stabile Endzustand ist der letzte Eintrag.
        var model = await app.DevApi.WaitForModelAsync(
            m => m.PlayerIsActive && m.PlayerMediaId == 502 && m.PlayerPlaylistEntryId == 6,
            TimeSpan.FromSeconds(60),
            "Auto-Fortsetzung zum Folge-Eintrag", Ct);
        Assert.False(app.Process.HasExited, "Die App wurde bei der Auto-Fortsetzung beendet.");
        Assert.Null(model.PlayerError);
        Assert.True(server.WasPathRequested("/api/playlists/701/play/advance"),
            "Der advance-Endpunkt wurde nicht angefragt.");
        Assert.True(server.WasPathRequested("/api/items/movie/501/stream"),
            "Der Stream des Starteintrags wurde nicht angefragt.");

        // Der Stream-Abruf des MediaElement folgt dem Zustandswechsel
        // asynchron — kurzes Warten, bis der Request eingetroffen ist.
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(15);
        while (!server.WasPathRequested("/api/items/movie/502/stream"))
        {
            Assert.True(DateTime.UtcNow < deadline, "Der Stream des Folge-Eintrags wurde nicht angefragt.");
            await Task.Delay(250, Ct);
        }
    }

    /// <summary>
    /// Pflichtszenario (AK 2): ein nicht abrufbarer Stream des aktuellen
    /// Eintrags löst den Fehler-Rückfall aus — die Wiedergabe setzt
    /// über <c>play/advance</c> beim Folge-Eintrag fort.
    /// </summary>
    [Fact]
    public async Task StreamFailure_AdvancesToNextPlaylistEntry()
    {
        Assert.True(File.Exists(TestVideo), $"Testvideo fehlt im Output-Verzeichnis: {TestVideo}");
        using var server = NewServer();
        server.Playlists.Add(new MockPlaylist
        {
            Id = PlaylistId,
            Name = "E2E Fehler-Rückfall",
            IsOwner = true,
            Entries =
            {
                new MockPlaylistEntry
                {
                    Id = 5,
                    MediaType = "Movie",
                    MediaId = 501,
                    Title = "Defekter Film",
                    // Der Eintrag bleibt serverseitig abspielbar, nur
                    // der Stream-Abruf schlägt fehl (404).
                    StreamUnavailable = true
                },
                MovieEntry(6, 502, "Folge-Film")
            }
        });
        using var app = await StartAppAsync();
        await OpenPlaylistDetailsAsync(app, server, PlaylistId, 2);
        await app.DevApi.InvokeAsync("PlaylistDetailsPlayButton", cancellationToken: Ct);

        // Der Stream des ersten Eintrags schlägt fehl (404 am nativen
        // Player) → Rückfall auf den Folge-Eintrag über den
        // advance-Endpunkt; der stabile Endzustand ist der zweite Eintrag.
        var model = await app.DevApi.WaitForModelAsync(
            m => m.PlayerMediaId == 502 && m.PlayerPlaylistEntryId == 6,
            TimeSpan.FromSeconds(60),
            "Fehler-Rückfall zum Folge-Eintrag", Ct);
        Assert.False(app.Process.HasExited, "Die App wurde beim Fehler-Rückfall beendet.");
        Assert.True(server.WasPathRequested("/api/playlists/701/play/advance"),
            "Der advance-Endpunkt wurde beim Fehler-Rückfall nicht angefragt.");
        Assert.True(server.WasPathRequested("/api/items/movie/501/stream"),
            "Der fehlschlagende Stream des Starteintrags wurde nicht angefragt.");
    }

    /// <summary>
    /// Pflichtszenario (AK 3): ein Weiterschauen-Eintrag mit
    /// Playlist-Bezug startet die Wiedergabe über den Playlist-Kontext;
    /// das Folgevideo kommt aus der Playlist-Reihenfolge
    /// (<c>play/advance</c>) — der Serienkontext
    /// (<c>api/items/tvshow/{id}</c>) wird nicht geladen.
    /// </summary>
    [Fact]
    public async Task ContinueWatchingPlaylistEntry_AdvancesByPlaylistOrder_NotSeries()
    {
        Assert.True(File.Exists(TestVideo), $"Testvideo fehlt im Output-Verzeichnis: {TestVideo}");
        using var server = NewServer();
        server.PlaybackShowId = ShowId;
        server.PlaybackShowTitle = "E2E Serie";
        server.PlaybackEpisodeId = EpisodeId;
        server.PlaybackEpisodeTitle = EpisodeTitle;
        server.PlaybackEpisodeResumePositionSeconds = 10;
        server.ContinueWatchingPlaylistId = PlaylistId;
        server.ContinueWatchingPlaylistName = "E2E CW-Liste";
        server.ContinueWatchingPlaylistEntryId = 5;
        server.Playlists.Add(new MockPlaylist
        {
            Id = PlaylistId,
            Name = "E2E CW-Liste",
            IsOwner = true,
            Entries =
            {
                new MockPlaylistEntry
                {
                    Id = 5,
                    MediaType = "TVShowEpisode",
                    MediaId = EpisodeId,
                    Title = EpisodeTitle,
                    ResumePositionSeconds = 10
                },
                // Der Folge-Eintrag der Playlist ist ein Film — die
                // Serienreihenfolge würde eine andere Episode liefern.
                MovieEntry(6, 502, "Playlist-Folge-Film")
            }
        });
        using var app = await StartAppAsync();
        await LoginOnHomeAsync(app, server, EpisodeTitle);

        var index = await ContinueWatchingIndexAsync(app, EpisodeTitle);
        await app.DevApi.InvokeAsync("ContinueWatchingCarousel", index, Ct);

        // Der Playlist-Kontext des Players beweist den Playlist-Startpfad
        // (PlayerPlaylistId bleibt über den gesamten Lauf erhalten).
        await app.DevApi.WaitForModelAsync(
            m => m.PlayerIsActive && m.PlayerPlaylistId == PlaylistId,
            TimeSpan.FromSeconds(60),
            "Wiedergabe im Playlist-Kontext gestartet", Ct);
        Assert.False(app.Process.HasExited, "Die App wurde beim Wiedergabestart beendet.");

        // Auto-Fortsetzung: das Folgevideo kommt aus der Playlist
        // (advance-Endpunkt, Folge = Film 502) — nicht aus der Serie.
        var model = await app.DevApi.WaitForModelAsync(
            m => m.PlayerIsActive && m.PlayerMediaId == 502 && m.PlayerPlaylistEntryId == 6,
            TimeSpan.FromSeconds(60),
            "Folgevideo aus der Playlist-Reihenfolge", Ct);
        Assert.False(app.Process.HasExited, "Die App wurde beim Playlist-Wechsel beendet.");
        Assert.True(server.WasPathRequested("/api/playlists/701/play/advance"),
            "Der Folge-Eintrag kam nicht über den advance-Endpunkt.");
        lock (server.RequestedPaths)
        {
            Assert.DoesNotContain(server.RequestedPaths,
                p => p.StartsWith("GET /api/items/tvshow/", StringComparison.OrdinalIgnoreCase));
        }
    }
}
