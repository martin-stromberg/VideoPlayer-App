using VideoPlayer.E2E.Tests.Support;

namespace VideoPlayer.E2E.Tests.Tests;

/// <summary>
/// E2E-Szenarien der Serienansicht (Regressionsschutz für den
/// iOS-Befund „keine Episodenliste, falsche Staffelreihenfolge,
/// zurückspringende Auswahl"): Der Weiterschauen-Serieneintrag öffnet
/// die Detailseite, die Episodenliste ist befüllt, der Staffel-Picker
/// listet aufsteigend und ein Staffelwechsel bleibt bestehen.
/// </summary>
public sealed class SeriesDetailsTests : E2ETestBase
{
    private const long ShowId = 8401;
    private const string ShowTitle = "E2E-Serienshow";

    /// <summary>Initialisiert die Testklasse.</summary>
    /// <param name="fixture">Geteilte Build-Fixture.</param>
    public SeriesDetailsTests(AppFixture fixture) : base(fixture) { }

    /// <summary>
    /// Pflichtszenario (AK 1/2): Die Serien-Detailseite zeigt die
    /// Episodenliste der ersten Staffel und die Staffelauswahl listet
    /// die Staffeln aufsteigend („Staffel 1" zuerst).
    /// </summary>
    [Fact]
    public async Task ShowDetails_ShowsEpisodesAndAscendingSeasonOrder()
    {
        using var server = NewServer();
        using var app = await StartAppAsync();
        await LoginOnHomeAsync(app, server, ShowTitle);
        await OpenShowDetailsAsync(app);

        // Modellseitig: drei Staffeln aufsteigend, erste Staffel gewählt,
        // deren Episoden stehen bereit.
        var model = await app.DevApi.WaitForModelAsync(
            m => m.MediaDetailsSeasonNames != null
                && m.MediaDetailsSeasonNames.Count == 3
                && m.MediaDetailsSelectedSeasonNumber == 1
                && m.MediaDetailsSeasonEpisodeTitles != null
                && m.MediaDetailsSeasonEpisodeTitles.Count == 2,
            TimeSpan.FromSeconds(30),
            "Serienansicht mit 3 Staffeln und Episodenliste der 1. Staffel", Ct);
        Assert.Equal(
            ["Staffel 1", "Staffel 2", "Staffel 3"],
            model.MediaDetailsSeasonNames);

        // Anzeigeebene: der Picker listet die Staffeln in derselben
        // aufsteigenden Reihenfolge; die Episodenliste zeigt die
        // Einträge der ersten Staffel.
        var seasonNames = app.Ui.GetComboBoxItemNames("SeasonPicker");
        Assert.Equal(["Staffel 1", "Staffel 2", "Staffel 3"], seasonNames);
        var tree = await app.DevApi.GetTreeAsync(Ct);
        var episodes = tree.FindByAutomationId("EpisodesList");
        Assert.NotNull(episodes);
        Assert.NotNull(episodes.Items);
        Assert.Equal(
            ["E2E-Folge 1-1", "E2E-Folge 1-2"],
            episodes.Items.Select(i => i.Text).ToList());
    }

    /// <summary>
    /// Pflichtszenario (AK 3/4): Die Auswahl einer anderen Staffel
    /// wechselt die Episodenliste und bleibt bestehen — sie springt
    /// nicht auf „Staffel 1" zurück.
    /// </summary>
    [Fact]
    public async Task SelectAnotherSeason_SwitchesEpisodesAndKeepsSelection()
    {
        using var server = NewServer();
        using var app = await StartAppAsync();
        await LoginOnHomeAsync(app, server, ShowTitle);
        await OpenShowDetailsAsync(app);

        await app.DevApi.WaitForModelAsync(
            m => m.MediaDetailsSelectedSeasonNumber == 1,
            TimeSpan.FromSeconds(30),
            "Erste Staffel vorgewählt", Ct);

        app.Ui.SelectComboBoxItem("SeasonPicker", "Staffel 2");

        // Wechsel schlägt durch: Modell-Auswahl und Episodenliste
        // folgen der zweiten Staffel.
        await app.DevApi.WaitForModelAsync(
            m => m.MediaDetailsSelectedSeasonNumber == 2
                && m.MediaDetailsSeasonEpisodeTitles != null
                && m.MediaDetailsSeasonEpisodeTitles.SequenceEqual(
                    ["1. E2E-Folge 2-1", "2. E2E-Folge 2-2"]),
            TimeSpan.FromSeconds(30),
            "Staffel 2 gewählt und deren Episoden gelistet", Ct);

        // Die Auswahl darf über spätere Model-Ticks (z. B. Kopfbild-
        // Nachlauf, Sync-Ereignisse) nicht auf „Staffel 1" zurückspringen.
        await Task.Delay(TimeSpan.FromSeconds(5), Ct);
        var settled = await app.DevApi.GetModelAsync(Ct);
        Assert.Equal(2, settled.MediaDetailsSelectedSeasonNumber);
        Assert.Equal(
            ["1. E2E-Folge 2-1", "2. E2E-Folge 2-2"],
            settled.MediaDetailsSeasonEpisodeTitles);
        Assert.False(app.Process.HasExited, "Die App wurde beim Staffelwechsel beendet.");
    }

    /// <summary>Mock-Server mit dreistäffeliger Detailserie im Weiterschauen-Eintrag.</summary>
    /// <returns>Der konfigurierte Mock-Server.</returns>
    private static MockVideoServer NewServer()
    {
        var server = new MockVideoServer("AAAAAA", "e2e-device-series", "e2e@example.test", "e2e-pass")
        {
            ContinueWatchingShowId = ShowId,
            ContinueWatchingShowTitle = ShowTitle
        };
        server.ShowSeasons.AddRange(
        [
            new MockShowSeason
            {
                Id = 9101,
                Number = 1,
                Name = "Staffel 1",
                Episodes =
                {
                    new MockShowEpisode { Id = 91101, Number = 1, Title = "E2E-Folge 1-1" },
                    new MockShowEpisode { Id = 91102, Number = 2, Title = "E2E-Folge 1-2" }
                }
            },
            new MockShowSeason
            {
                Id = 9102,
                Number = 2,
                Name = "Staffel 2",
                Episodes =
                {
                    new MockShowEpisode { Id = 91201, Number = 1, Title = "E2E-Folge 2-1" },
                    new MockShowEpisode { Id = 91202, Number = 2, Title = "E2E-Folge 2-2" }
                }
            },
            new MockShowSeason
            {
                Id = 9103,
                Number = 3,
                Name = "Staffel 3",
                Episodes =
                {
                    new MockShowEpisode { Id = 91301, Number = 1, Title = "E2E-Folge 3-1" }
                }
            }
        ]);
        return server;
    }

    /// <summary>
    /// Öffnet die Serien-Detailseite über den Weiterschauen-Eintrag
    /// (<c>/invoke</c>+<c>itemIndex</c> des Karussells) und wartet auf
    /// die sichtbare Staffel-Sektion.
    /// </summary>
    /// <param name="app">Die laufende App-Instanz.</param>
    /// <returns>Eine abgeschlossene Aufgabe nach Erreichen der Detailseite.</returns>
    private static async Task OpenShowDetailsAsync(AppInstance app)
    {
        var index = await ContinueWatchingIndexAsync(app, ShowTitle);
        await app.DevApi.InvokeAsync("ContinueWatchingCarousel", index, Ct);
        app.Ui.FindByAutomationId("SeasonPicker", TimeSpan.FromSeconds(30));
    }
}
