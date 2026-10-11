using VideoPlayer.E2E.Tests.Support;

namespace VideoPlayer.E2E.Tests.Tests;

/// <summary>
/// E2E-Szenarien der direkten Wiedergabe aus der Weiterschauen-Liste
/// (Issue #43, Schritt 1): Ein Tap auf einen abspielbaren
/// Weiterschauen-Eintrag (<c>movie</c>/<c>episode</c>) startet die
/// Wiedergabe direkt an der Fortsetzungsposition — ohne Detailseite —,
/// das Folgevideo bleibt über den unabhängig aufgelösten Serienkontext
/// erhalten, und nicht abspielbare Einträge (<c>show</c>) öffnen
/// weiterhin die Detailansicht. Der Auswahl-Pfad nutzt <c>/invoke</c>
/// mit <c>itemIndex</c> auf dem BindableLayout-Karussell (dieselbe
/// Ereigniskette wie Fingertipp).
/// </summary>
public sealed class ContinueWatchingPlaybackTests : E2ETestBase
{
    private const long MovieId = 8001;
    private const string MovieTitle = "E2E Direktfilm";
    private const long ShowId = 8101;
    private const string ShowTitle = "E2E Direktserie";
    private const long EpisodeId = 8201;
    private const string EpisodeTitle = "E2E Direkt-Episode 1";
    private const long NextEpisodeId = 8202;
    private const string NextEpisodeTitle = "E2E Direkt-Episode 2";
    private const long ShowEntryId = 8301;
    private const string ShowEntryTitle = "E2E Serieneintrag";

    /// <summary>Initialisiert die Testklasse.</summary>
    /// <param name="fixture">Geteilte Build-Fixture.</param>
    public ContinueWatchingPlaybackTests(AppFixture fixture) : base(fixture) { }

    private static string TestVideo => Path.Combine(AppContext.BaseDirectory, "Assets", "testvideo.mp4");

    /// <summary>
    /// Pflichtszenario (AK 1 + 2): Ein Tap auf den
    /// Weiterschauen-Filmeintrag startet die Wiedergabe direkt ab der
    /// Fortsetzungsposition — die Detailseite wird nicht aufgerufen
    /// (kein Movie-Detail-Request, nur der Stream).
    /// </summary>
    [Fact]
    public async Task TapContinueWatchingMovie_StartsPlaybackAtResume_WithoutDetails()
    {
        Assert.True(File.Exists(TestVideo), $"Testvideo fehlt im Output-Verzeichnis: {TestVideo}");
        using var server = new MockVideoServer("AAAAAA", "e2e-device-cw", "e2e@example.test", "e2e-pass")
        {
            PlaybackMovieId = MovieId,
            PlaybackMovieTitle = MovieTitle,
            PlaybackVideoPath = TestVideo,
            PlaybackResumePositionSeconds = 12
        };
        using var app = await StartAppAsync();
        await LoginOnHomeAsync(app, server, MovieTitle);

        var index = await ContinueWatchingIndexAsync(app, MovieTitle);
        await app.DevApi.InvokeAsync("ContinueWatchingCarousel", index, Ct);

        var model = await app.DevApi.WaitForModelAsync(
            m => m.PlayerIsActive && m.PlayerIsPlaying
                && m.PlayerPositionSeconds >= 12,
            TimeSpan.FromSeconds(60),
            "Direktwiedergabe ab der Fortsetzungsposition", Ct);

        Assert.False(app.Process.HasExited, "Die App wurde beim Wiedergabestart beendet.");
        Assert.Null(model.PlayerError);
        Assert.Equal(MovieId, model.PlayerMediaId);
        // Nachweis „keine Detailseite": der Stream wurde abgerufen,
        // der Movie-Detail-Endpunkt nicht.
        Assert.True(server.WasPathRequested("/api/items/movie/8001/stream"),
            "Der Wiedergabe-Stream wurde nicht angefordert.");
        Assert.False(server.WasPathRequested("/api/items/movie/8001"),
            "Die Detailseite wurde im Hintergrund geladen (Movie-Detail-Request).");
    }

    /// <summary>
    /// Pflichtszenario (AK 5): Ein Tap auf den
    /// Weiterschauen-Episodeneintrag startet die Wiedergabe direkt; der
    /// Serienkontext wird per <c>api/items/tvshow/{id}</c> ohne
    /// Detailseite geladen (Folge-Episoden im Modell) und die
    /// Auto-Fortsetzung wechselt zur Folge-Episode.
    /// </summary>
    [Fact]
    public async Task TapContinueWatchingEpisode_StartsPlaybackAndAdvancesToNextEpisode()
    {
        Assert.True(File.Exists(TestVideo), $"Testvideo fehlt im Output-Verzeichnis: {TestVideo}");
        using var server = new MockVideoServer("AAAAAA", "e2e-device-cw-ep", "e2e@example.test", "e2e-pass")
        {
            PlaybackVideoPath = TestVideo,
            PlaybackShowId = ShowId,
            PlaybackShowTitle = ShowTitle,
            PlaybackEpisodeId = EpisodeId,
            PlaybackEpisodeTitle = EpisodeTitle,
            PlaybackNextEpisodeId = NextEpisodeId,
            PlaybackNextEpisodeTitle = NextEpisodeTitle,
            PlaybackEpisodeResumePositionSeconds = 10
        };
        using var app = await StartAppAsync();
        await LoginOnHomeAsync(app, server, EpisodeTitle);

        var index = await ContinueWatchingIndexAsync(app, EpisodeTitle);
        await app.DevApi.InvokeAsync("ContinueWatchingCarousel", index, Ct);

        // Direktstart der Episode — ohne Episoden-Detailaufruf.
        var model = await app.DevApi.WaitForModelAsync(
            m => m.PlayerIsActive && m.PlayerMediaId == EpisodeId,
            TimeSpan.FromSeconds(60),
            "Direktwiedergabe der Weiterschauen-Episode", Ct);
        Assert.False(app.Process.HasExited, "Die App wurde beim Wiedergabestart beendet.");

        // Folgevideo-Funktion: das kurze Testvideo liegt im
        // Fortsetzungsfenster → der Player wechselt über die
        // nachgeladene Fortsetzungsliste zur Folge-Episode.
        model = await app.DevApi.WaitForModelAsync(
            m => m.PlayerIsActive && m.PlayerMediaId == NextEpisodeId && m.PlayerIsPlaying,
            TimeSpan.FromSeconds(60),
            "Wechsel zur Folge-Episode", Ct);
        Assert.False(app.Process.HasExited, "Die App wurde beim Wechsel zur Folge-Episode beendet.");
        Assert.Equal(NextEpisodeId, model.PlayerMediaId);
        Assert.Null(model.PlayerError);

        // Nachweis „keine Detailseite" + „Serienkontext unabhängig":
        // beide Episoden-Streams liefen, der Serienkontext kam per
        // tvshow-Abruf — die Episoden-Detail-Endpunkte nie.
        Assert.True(server.WasPathRequested("/api/items/tvshowepisode/8201/stream"),
            "Der Stream der Ausgangs-Episode wurde nicht angefordert.");
        Assert.True(server.WasPathRequested("/api/items/tvshowepisode/8202/stream"),
            "Der Stream der Folge-Episode wurde nicht angefordert.");
        Assert.True(server.WasPathRequested("/api/items/tvshow/8101"),
            "Der Serienkontext wurde nicht per tvshow-Abruf geladen.");
        Assert.False(server.WasPathRequested("/api/items/tvshowepisode/8201"),
            "Die Episoden-Detailseite wurde im Hintergrund geladen.");
        Assert.False(server.WasPathRequested("/api/items/tvshowepisode/8202"),
            "Die Detailseite der Folge-Episode wurde im Hintergrund geladen.");
    }

    /// <summary>
    /// Pflichtszenario (AK 4): Ein Tap auf einen nicht direkt
    /// abspielbaren Weiterschauen-Eintrag (<c>show</c>) öffnet
    /// weiterhin die Detailansicht — kein Player-Zustand.
    /// </summary>
    [Fact]
    public async Task TapContinueWatchingShowEntry_OpensDetails_NotPlayback()
    {
        using var server = new MockVideoServer("AAAAAA", "e2e-device-cw-show", "e2e@example.test", "e2e-pass")
        {
            ContinueWatchingShowId = ShowEntryId,
            ContinueWatchingShowTitle = ShowEntryTitle
        };
        using var app = await StartAppAsync();
        await LoginOnHomeAsync(app, server, ShowEntryTitle);

        var index = await ContinueWatchingIndexAsync(app, ShowEntryTitle);
        await app.DevApi.InvokeAsync("ContinueWatchingCarousel", index, Ct);

        // Detailansicht wird sichtbar (Header-Bereich der
        // MediaDetailsPage) — die Wiedergabe startet nicht.
        var header = app.Ui.FindByAutomationId("HeaderSection", TimeSpan.FromSeconds(30));
        Assert.NotNull(header);

        var model = await app.DevApi.GetModelAsync(Ct);
        Assert.False(model.PlayerIsActive);
        Assert.True(server.WasPathRequested("/api/items/tvshow/8301"),
            "Die Detailansicht der Serie wurde nicht geladen.");
        Assert.False(app.Process.HasExited, "Die App wurde beim Öffnen der Detailansicht beendet.");
    }

    /// <summary>Führt den Einrichtungsfluss bis zur befüllten Startseite aus (Weiterschauen-Eintrag sichtbar).</summary>
    /// <param name="app">Die laufende App-Instanz.</param>
    /// <param name="server">Der Mock-Server.</param>
    /// <param name="expectedTitle">Titel des erwarteten Weiterschauen-Eintrags (Diagnose).</param>
    /// <returns>Eine abgeschlossene Aufgabe nach Erreichen der Startseite.</returns>
    private static async Task LoginOnHomeAsync(AppInstance app, MockVideoServer server, string expectedTitle)
    {
        await ConnectToPairingRequiredAsync(app, server);
        await PairAsync(app, server);
        await LoginAsync(app, server);

        await app.DevApi.WaitForModelAsync(
            m => m.ContinueWatchingCount > 0,
            TimeSpan.FromSeconds(30),
            $"Karussell „Weiterschauen\" befüllt (erwartet „{expectedTitle}\")", Ct);
    }

    /// <summary>Ermittelt den Index eines Weiterschauen-Eintrags anhand seines Titels im Karussell-Tree.</summary>
    /// <param name="app">Die laufende App-Instanz.</param>
    /// <param name="title">Der gesuchte Eintragstitel.</param>
    /// <returns>Der Index des Eintrags im Karussell.</returns>
    private static async Task<int> ContinueWatchingIndexAsync(AppInstance app, string title)
    {
        var tree = await app.DevApi.GetTreeAsync(Ct);
        var carousel = tree.FindByAutomationId("ContinueWatchingCarousel");
        Assert.NotNull(carousel);
        Assert.NotNull(carousel.Items);
        var index = carousel.Items.FindIndex(i => i.Text == title);
        Assert.True(index >= 0,
            $"'{title}' fehlt im Karussell: " +
            string.Join(", ", carousel.Items.Select(i => i.Text)));
        return index;
    }
}
