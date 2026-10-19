using VideoPlayer.E2E.Tests.Support;

namespace VideoPlayer.E2E.Tests.Tests;

/// <summary>
/// E2E-Szenarien der Weiterschauen-Navigation (Issue #49, Schritt 4):
/// Ein Tap auf einen abspielbaren Weiterschauen-Eintrag
/// (<c>movie</c>/<c>episode</c>) öffnet die Titel-Detailansicht und
/// startet darüber die Wiedergabe an der Fortsetzungsposition —
/// „Player schließen" kehrt zur Detailansicht zurück statt zur
/// Startliste. Das Folgevideo bleibt über den nachgeladenen
/// Serienkontext erhalten, und nicht abspielbare Einträge
/// (<c>show</c>) öffnen weiterhin nur die Detailansicht. Der
/// Auswahl-Pfad nutzt <c>/invoke</c> mit <c>itemIndex</c> auf dem
/// BindableLayout-Karussell (dieselbe Ereigniskette wie Fingertipp).
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
    /// Weiterschauen-Filmeintrag öffnet die Titel-Detailansicht und
    /// startet darüber die Wiedergabe ab der Fortsetzungsposition —
    /// „Player schließen" kehrt zur geöffneten Detailansicht zurück
    /// statt zur Startliste.
    /// </summary>
    [Fact]
    public async Task TapContinueWatchingMovie_OpensDetailsAndStartsPlaybackAtResume()
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
            "Wiedergabe über der Detailansicht ab der Fortsetzungsposition", Ct);

        Assert.False(app.Process.HasExited, "Die App wurde beim Wiedergabestart beendet.");
        Assert.Null(model.PlayerError);
        Assert.Equal(MovieId, model.PlayerMediaId);
        // Die Wiedergabe läuft und die Detailansicht wurde darunter
        // geöffnet (Movie-Detail-Request).
        Assert.True(server.WasPathRequested("/api/items/movie/8001/stream"),
            "Der Wiedergabe-Stream wurde nicht angefordert.");
        Assert.True(server.WasPathRequested("/api/items/movie/8001"),
            "Die Titel-Detailansicht wurde nicht geladen (Movie-Detail-Request fehlt).");

        // „Player schließen" kehrt zur Detailansicht zurück — die
        // Kopf-Sektion der MediaDetailsPage wird sichtbar, während ein
        // Rücksprung zur Startliste wieder deren Karussells zeigen würde.
        app.Ui.GoBack();
        await app.DevApi.WaitForModelAsync(
            m => !m.PlayerIsActive,
            TimeSpan.FromSeconds(30),
            "Player geschlossen", Ct);
        var header = app.Ui.FindByAutomationId("HeaderSection", TimeSpan.FromSeconds(30));
        Assert.NotNull(header);
    }

    /// <summary>
    /// Pflichtszenario (AK 5): Ein Tap auf den
    /// Weiterschauen-Episodeneintrag öffnet die Titel-Detailansicht im
    /// Serienkontext und startet darüber die Wiedergabe; die
    /// Fortsetzungsliste wird weiterhin nachgeladen (Folge-Episoden im
    /// Modell) und die Auto-Fortsetzung wechselt zur Folge-Episode.
    /// </summary>
    [Fact]
    public async Task TapContinueWatchingEpisode_OpensDetailsAndStartsPlaybackAndAdvancesToNextEpisode()
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

        // Wiedergabe der Episode über der geöffneten Detailansicht.
        var model = await app.DevApi.WaitForModelAsync(
            m => m.PlayerIsActive && m.PlayerMediaId == EpisodeId,
            TimeSpan.FromSeconds(60),
            "Wiedergabe der Weiterschauen-Episode über der Detailansicht", Ct);
        Assert.False(app.Process.HasExited, "Die App wurde beim Wiedergabestart beendet.");

        // Folgevideo-Funktion: das kurze Testvideo erreicht sein
        // tatsächliches Ende (MediaEnded) → der Player wechselt über
        // die nachgeladene Fortsetzungsliste zur Folge-Episode.
        model = await app.DevApi.WaitForModelAsync(
            m => m.PlayerIsActive && m.PlayerMediaId == NextEpisodeId && m.PlayerIsPlaying,
            TimeSpan.FromSeconds(60),
            "Wechsel zur Folge-Episode", Ct);
        Assert.False(app.Process.HasExited, "Die App wurde beim Wechsel zur Folge-Episode beendet.");
        Assert.Equal(NextEpisodeId, model.PlayerMediaId);
        Assert.Null(model.PlayerError);

        // Nachweis „Detailansicht geöffnet" + „Serienkontext vorhanden":
        // beide Episoden-Streams liefen, und die Serie wurde per
        // tvshow-Abruf geladen (Detailseite im Serienkontext und/oder
        // Fortsetzungs-Nachladung — beide nutzen denselben Endpunkt).
        Assert.True(server.WasPathRequested("/api/items/tvshowepisode/8201/stream"),
            "Der Stream der Ausgangs-Episode wurde nicht angefordert.");
        Assert.True(server.WasPathRequested("/api/items/tvshowepisode/8202/stream"),
            "Der Stream der Folge-Episode wurde nicht angefordert.");
        Assert.True(server.WasPathRequested("/api/items/tvshow/8101"),
            "Der Serienkontext wurde nicht per tvshow-Abruf geladen.");

        // „Player schließen" kehrt zur Detailansicht der Episode im
        // Serienkontext zurück (Staffel-Episodenliste im Modell).
        app.Ui.GoBack();
        await app.DevApi.WaitForModelAsync(
            m => !m.PlayerIsActive
                && m.MediaDetailsSeasonEpisodeTitles is { Count: > 0 },
            TimeSpan.FromSeconds(30),
            "Player geschlossen — Detailansicht im Serienkontext", Ct);
        var header = app.Ui.FindByAutomationId("HeaderSection", TimeSpan.FromSeconds(30));
        Assert.NotNull(header);
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

}
