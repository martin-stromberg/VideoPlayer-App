using VideoPlayer.E2E.Tests.Support;

namespace VideoPlayer.E2E.Tests.Tests;

/// <summary>
/// E2E-Szenarien des Wiedergabestarts (Regressionsschutz für den
/// iOS-Absturz beim Start eines Videos): „Neu im Programm"-Eintrag über
/// die Developer-API wählen (<c>/invoke</c> mit <c>itemIndex</c> auf dem
/// BindableLayout-Karussell) → Mediendetails → „Abspielen" → die
/// Wiedergabe läuft bzw. ein Stream-Fehlschlag landet als sichtbarer
/// Fehler im Modell — in beiden Fällen darf der Prozess nicht enden.
/// </summary>
public sealed class PlaybackTests : E2ETestBase
{
    /// <summary>Initialisiert die Testklasse.</summary>
    /// <param name="fixture">Geteilte Build-Fixture.</param>
    public PlaybackTests(AppFixture fixture) : base(fixture) { }

    /// <summary>
    /// Wiedergabestart mit Fortsetzungsposition: Der Karussell-Eintrag
    /// führt über die Detailseite zum Player; der Fortsetzungs-Seek landet
    /// auf der Weiterschauen-Position — ohne Absturz und ohne Fehler.
    /// </summary>
    [Fact]
    public async Task PlayFromRecent_StartsPlaybackAtResumePosition()
    {
        using var server = NewServer(streamFails: false);
        using var app = await StartAppAsync();

        await LoginOnHomeAsync(app, server);
        await OpenDetailsAndPlayAsync(app, server);

        var model = await app.DevApi.WaitForModelAsync(
            m => m.PlayerIsActive && m.PlayerIsPlaying
                && m.PlayerPositionSeconds >= server.PlaybackResumePositionSeconds!.Value,
            TimeSpan.FromSeconds(60),
            "Wiedergabe läuft ab der Fortsetzungsposition", Ct);

        Assert.False(app.Process.HasExited, "Die App wurde beim Wiedergabestart beendet.");
        Assert.Null(model.PlayerError);
    }

    /// <summary>
    /// Stream-Fehlschlag beim Wiedergabestart (Stream-Endpunkt liefert
    /// 404): der Fehler landet als <c>playerError</c> im Modell — nicht
    /// als Prozessabbruch.
    /// </summary>
    [Fact]
    public async Task PlayWithFailingStream_ShowsErrorInsteadOfCrash()
    {
        using var server = NewServer(streamFails: true);
        using var app = await StartAppAsync();

        await LoginOnHomeAsync(app, server);
        await OpenDetailsAndPlayAsync(app, server);

        var model = await app.DevApi.WaitForModelAsync(
            m => m.PlayerIsActive && m.PlayerError is not null,
            TimeSpan.FromSeconds(60),
            "Wiedergabefehler im Modell sichtbar", Ct);

        Assert.False(app.Process.HasExited, "Die App wurde beim fehlgeschlagenen Wiedergabestart beendet.");
        Assert.False(string.IsNullOrWhiteSpace(model.PlayerError));
    }

    /// <summary>Mock-Server mit konfiguriertem Wiedergabefluss (Opt-in).</summary>
    private static MockVideoServer NewServer(bool streamFails)
    {
        var videoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "testvideo.mp4");
        Assert.True(File.Exists(videoPath), $"Testvideo fehlt im Output-Verzeichnis: {videoPath}");
        return new MockVideoServer("AAAAAA", "e2e-device-play", "e2e@example.test", "e2e-pass")
        {
            PlaybackMovieId = 4711,
            PlaybackMovieTitle = "E2E-Testfilm",
            PlaybackVideoPath = videoPath,
            PlaybackResumePositionSeconds = 1,
            PlaybackStreamFails = streamFails
        };
    }

    /// <summary>Führt den Einrichtungsfluss bis zur befüllten Startseite aus.</summary>
    private static async Task LoginOnHomeAsync(AppInstance app, MockVideoServer server)
    {
        await ConnectToPairingRequiredAsync(app, server);
        await PairAsync(app, server);
        await LoginAsync(app, server);

        // „Neu im Programm" wird aus dem synchronisierten Cache befüllt.
        await app.DevApi.WaitForModelAsync(
            m => m.RecentEntriesCount > 0,
            TimeSpan.FromSeconds(30),
            "Karussell „Neu im Programm\" befüllt", Ct);
    }

    /// <summary>
    /// Wählt den Testfilmeintrag im „Neu im Programm"-Karussell per
    /// <c>/invoke</c>+<c>itemIndex</c> aus (BindableLayout-Einträge fehlen
    /// im UIA-Baum — gleiche Lücke wie bei CollectionView) und löst auf
    /// der Detailseite „Abspielen" aus.
    /// </summary>
    private static async Task OpenDetailsAndPlayAsync(AppInstance app, MockVideoServer server)
    {
        var tree = await app.DevApi.GetTreeAsync(Ct);
        var carousel = tree.FindByAutomationId("RecentCarousel");
        Assert.NotNull(carousel);
        Assert.NotNull(carousel.Items);
        var index = carousel.Items.FindIndex(i => i.Text == server.PlaybackMovieTitle);
        Assert.True(index >= 0,
            $"'{server.PlaybackMovieTitle}' fehlt im Karussell: " +
            string.Join(", ", carousel.Items.Select(i => i.Text)));

        await app.DevApi.InvokeAsync("RecentCarousel", index, Ct);

        // „Abspielen" erscheint erst nach dem Laden der Detaildaten.
        app.Ui.FindByAutomationId("PlayButton", TimeSpan.FromSeconds(30));
        app.Ui.ClickButton("PlayButton");
    }
}
