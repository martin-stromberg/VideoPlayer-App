using FlaUI.Core.AutomationElements;
using VideoPlayer.E2E.Tests.Support;

namespace VideoPlayer.E2E.Tests.Tests;

/// <summary>
/// E2E-Szenarien des Weiterschauen-Kontextmenüs (Issue #43, Schritt 2):
/// ein 2-s-Long-Press auf einen Weiterschauen-Eintrag öffnet das Menü
/// mit „Ausblenden" und „Überspringen"; die Auswahl ruft den
/// zugehörigen Server-Endpunkt auf und die Liste wird aktualisiert —
/// „Abbrechen" lässt alles unverändert. Der Long-Press wird über
/// <c>POST /invoke</c> mit <c>gesture: "longpress"</c> ausgelöst (UIA-
/// Lücke der BindableLayout-Einträge, dieselbe Ereigniskette wie das
/// Halten); die Dialog-Schaltflächen werden per Klartext-Name
/// angesprochen.
/// </summary>
public sealed class ContinueWatchingContextMenuTests : E2ETestBase
{
    private const long MovieId = 8501;
    private const string MovieTitle = "E2E Kontextfilm";

    /// <summary>Initialisiert die Testklasse.</summary>
    /// <param name="fixture">Geteilte Build-Fixture.</param>
    public ContinueWatchingContextMenuTests(AppFixture fixture) : base(fixture) { }

    /// <summary>
    /// Pflichtszenario (AK 1 + 2 + 3): Der Long-Press öffnet das
    /// Kontextmenü mit beiden Aktionen; „Ausblenden" ruft den
    /// Hide-Endpunkt auf und der Eintrag verschwindet aus der Liste.
    /// </summary>
    [Fact]
    public async Task LongPressMovie_ChooseHide_CallsHideEndpoint_EntryDisappears()
    {
        using var server = CreateServerWithMovie("e2e-device-cm-hide");
        using var app = await StartAppAsync();
        await LoginOnHomeAsync(app, server, MovieTitle);

        var index = await ContinueWatchingIndexAsync(app, MovieTitle);
        await app.DevApi.LongPressAsync("ContinueWatchingCarousel", index, Ct);

        // Das Menü bietet beide Aktionen in deutscher Klartext-Beschriftung.
        WaitForName(app.Ui, "Ausblenden");
        Assert.NotNull(app.Ui.FindByName("Überspringen"));

        app.Ui.ClickByName("Ausblenden");

        await app.DevApi.WaitForModelAsync(
            m => m.ContinueWatchingCount == 0,
            TimeSpan.FromSeconds(30),
            "Eintrag nach „Ausblenden\" aus der Liste verschwunden", Ct);

        Assert.Equal(1, server.HideContinueWatchingCount);
        Assert.Equal(0, server.SkipContinueWatchingCount);
        Assert.True(server.WasPathRequested("/api/continue-watching/hide"),
            "Der Hide-Endpunkt wurde nicht angefordert.");
        Assert.Contains("movie:8501", server.RemovedContinueWatchingKeys);
        Assert.False(app.Process.HasExited, "Die App wurde beim Ausblenden beendet.");
    }

    /// <summary>
    /// Pflichtszenario (AK 2 + 3): „Überspringen" ruft den
    /// Skip-Endpunkt auf und der Eintrag verschwindet aus der Liste.
    /// </summary>
    [Fact]
    public async Task LongPressMovie_ChooseSkip_CallsSkipEndpoint_EntryDisappears()
    {
        using var server = CreateServerWithMovie("e2e-device-cm-skip");
        using var app = await StartAppAsync();
        await LoginOnHomeAsync(app, server, MovieTitle);

        var index = await ContinueWatchingIndexAsync(app, MovieTitle);
        await app.DevApi.LongPressAsync("ContinueWatchingCarousel", index, Ct);

        app.Ui.ClickByName("Überspringen");

        await app.DevApi.WaitForModelAsync(
            m => m.ContinueWatchingCount == 0,
            TimeSpan.FromSeconds(30),
            "Eintrag nach „Überspringen\" aus der Liste verschwunden", Ct);

        Assert.Equal(1, server.SkipContinueWatchingCount);
        Assert.Equal(0, server.HideContinueWatchingCount);
        Assert.True(server.WasPathRequested("/api/continue-watching/skip"),
            "Der Skip-Endpunkt wurde nicht angefordert.");
        Assert.Contains("movie:8501", server.RemovedContinueWatchingKeys);
        Assert.False(app.Process.HasExited, "Die App wurde beim Überspringen beendet.");
    }

    /// <summary>
    /// Pflichtszenario (AK 1 + 2): „Abbrechen" schließt das Menü ohne
    /// Mutation — kein hide/skip-Request, der Eintrag bleibt in der
    /// Liste.
    /// </summary>
    [Fact]
    public async Task LongPressMovie_Cancel_KeepsEntry_NoMutationRequests()
    {
        using var server = CreateServerWithMovie("e2e-device-cm-cancel");
        using var app = await StartAppAsync();
        await LoginOnHomeAsync(app, server, MovieTitle);

        var index = await ContinueWatchingIndexAsync(app, MovieTitle);
        await app.DevApi.LongPressAsync("ContinueWatchingCarousel", index, Ct);

        WaitForName(app.Ui, "Ausblenden");
        app.Ui.ClickByName("Abbrechen");

        // Stabilitätsfenster: mögliche Nachlauf-Requests abwarten.
        await Task.Delay(2000, Ct);

        Assert.Equal(0, server.HideContinueWatchingCount);
        Assert.Equal(0, server.SkipContinueWatchingCount);
        Assert.False(server.WasPathRequested("/api/continue-watching/hide"));
        Assert.False(server.WasPathRequested("/api/continue-watching/skip"));
        Assert.Equal(0, await ContinueWatchingIndexAsync(app, MovieTitle));
        var model = await app.DevApi.GetModelAsync(Ct);
        Assert.True(model.ContinueWatchingCount > 0);
        Assert.False(app.Process.HasExited, "Die App wurde beim Abbrechen beendet.");
    }

    /// <summary>Erzeugt einen Mock-Server mit einem Weiterschauen-Filmeintrag.</summary>
    /// <param name="deviceName">Der Geräte-Pairing-Name der App.</param>
    /// <returns>Der konfigurierte Mock-Server.</returns>
    private static MockVideoServer CreateServerWithMovie(string deviceName)
    {
        return new MockVideoServer("AAAAAA", deviceName, "e2e@example.test", "e2e-pass")
        {
            PlaybackMovieId = MovieId,
            PlaybackMovieTitle = MovieTitle,
            PlaybackResumePositionSeconds = 12
        };
    }

    /// <summary>Wartet bis ein Element mit dem Anzeigenamen sichtbar ist (Menü-Dialog).</summary>
    /// <param name="ui">Der UI-Treiber der laufenden App.</param>
    /// <param name="name">Der gesuchte Anzeigename.</param>
    /// <returns>Das gefundene Element.</returns>
    private static AutomationElement WaitForName(UiDriver ui, string name)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (true)
        {
            var element = ui.FindByName(name);
            if (element is not null)
            {
                return element;
            }

            if (DateTime.UtcNow >= deadline)
            {
                throw new TimeoutException($"Kein Element mit dem Namen '{name}' gefunden.");
            }

            Thread.Sleep(200);
        }
    }
}
