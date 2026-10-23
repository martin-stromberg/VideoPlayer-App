using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using VideoPlayer.E2E.Tests.Support;

namespace VideoPlayer.E2E.Tests.Tests;

/// <summary>
/// E2E-Szenario der Fehler-Löschaktion (Issue #55): ein über die
/// Detailseite ausgelöster Download schlägt fehl (Mock-Server lehnt den
/// Stream ab), die Downloads-Seite zeigt daraufhin den Schalter
/// „Alle Fehler löschen"; nach dessen Betätigung und Bestätigung der
/// Rückfrage ist der Eintrag entfernt und der Schalter wieder
/// ausgeblendet — ohne fehlerhafte Einträge ist er nicht sichtbar.
/// </summary>
public sealed class DownloadsTests : E2ETestBase
{
    /// <summary>Initialisiert die Testklasse.</summary>
    /// <param name="fixture">Geteilte Build-Fixture.</param>
    public DownloadsTests(AppFixture fixture) : base(fixture) { }

    private static MockVideoServer NewServer() =>
        new("AAAAAA", "e2e-device-downloads", "e2e@example.test", "e2e-pass");

    /// <summary>
    /// Pflichtszenario: Fehlerhafter Download → Downloads-Seite →
    /// „Alle Fehler löschen" → Rückfrage bestätigen → Eintrag entfernt.
    /// Belegt zugleich die Sichtbarkeitsregel: ohne fehlerhafte Einträge
    /// ist der Schalter nicht vorhanden.
    /// </summary>
    [Fact]
    public async Task DownloadsPage_DeleteFailed_RemovesFailedEntry()
    {
        using var server = NewServer();
        server.PlaybackMovieId = 501;
        server.PlaybackMovieTitle = "E2E-Fehlerdownload";
        // Der Stream-Abruf schlägt fehl (404) — der Download landet
        // im Zustand „failed".
        server.PlaybackStreamFails = true;
        using var app = await StartAppAsync();
        await ConnectToPairingRequiredAsync(app, server);
        await PairAsync(app, server);
        await LoginAsync(app, server);

        // „Neu im Programm" wird aus dem synchronisierten Cache befüllt.
        await app.DevApi.WaitForModelAsync(
            m => m.RecentEntriesCount > 0,
            TimeSpan.FromSeconds(30),
            "Karussell „Neu im Programm\" befüllt", Ct);

        // Detailseite über das Karussell öffnen (BindableLayout-Einträge
        // fehlen im UIA-Baum — Muster: PlaybackTests).
        var tree = await app.DevApi.GetTreeAsync(Ct);
        var carousel = tree.FindByAutomationId("RecentCarousel");
        Assert.NotNull(carousel?.Items);
        var index = carousel!.Items!.FindIndex(i => i.Text == server.PlaybackMovieTitle);
        Assert.True(index >= 0,
            $"'{server.PlaybackMovieTitle}' fehlt im Karussell: "
            + string.Join(", ", carousel.Items!.Select(i => i.Text)));
        await app.DevApi.InvokeAsync("RecentCarousel", index, Ct);

        // Download auslösen — der Transfer scheitert am 404-Stream.
        app.Ui.FindByAutomationId("DownloadButton", TimeSpan.FromSeconds(30));
        app.Ui.ClickButton("DownloadButton");

        // Downloads-Seite öffnen; der Eintrag muss dort erscheinen.
        app.Ui.OpenFlyoutItem("Downloads");
        await WaitForTreeAsync(
            app,
            t => t.FindByAutomationId("DownloadsCollection")?.Items
                ?.Any(i => i.Text == server.PlaybackMovieTitle) == true,
            $"Download-Eintrag „{server.PlaybackMovieTitle}\" auf der Downloads-Seite");

        // Sichtbarkeitsregel: der Schalter erscheint erst mit dem
        // fehlgeschlagenen Eintrag — zugleich die Wartebedingung für
        // den Zustandswechsel auf „failed".
        await WaitForTreeAsync(
            app,
            t => t.FindByAutomationId("DeleteFailedButton")?.IsVisible == true,
            "Schalter „Alle Fehler löschen\" wird sichtbar");

        // Aktion auslösen und Rückfrage bestätigen.
        app.Ui.ClickButton("DeleteFailedButton");
        ConfirmAlert(app, "Alle fehlerhaften Downloads löschen?", "Löschen");

        // Der Eintrag ist entfernt, die Liste leer und der Schalter
        // wieder ausgeblendet (Liste + Anzeige wurden neu geladen).
        await WaitForTreeAsync(
            app,
            t => t.FindByAutomationId("DownloadsCollection")?.Items
                ?.Any(i => i.Text == server.PlaybackMovieTitle) != true,
            "Fehlerhafter Eintrag wurde entfernt");
        await WaitForTreeAsync(
            app,
            t => t.FindByAutomationId("DeleteFailedButton")?.IsVisible != true,
            "Schalter „Alle Fehler löschen\" ist wieder ausgeblendet");
        await WaitForTreeAsync(
            app,
            t => t.FindByAutomationId("EmptyLabel")?.IsVisible == true,
            "Leerhinweis der Downloads-Seite");
    }

    /// <summary>Wartet, bis der DevApi-Elementbaum eine Bedingung erfüllt.</summary>
    /// <param name="app">Die laufende App-Instanz.</param>
    /// <param name="predicate">Bedingung auf dem Baum.</param>
    /// <param name="description">Beschreibung für den Timeout-Fehler.</param>
    /// <param name="timeout">Maximale Wartezeit (Standard 30 s).</param>
    private static async Task WaitForTreeAsync(
        AppInstance app,
        Func<DevApiTreeNode, bool> predicate,
        string description,
        TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(30));
        while (true)
        {
            var tree = await app.DevApi.GetTreeAsync(Ct);
            if (predicate(tree))
            {
                return;
            }

            Assert.True(DateTime.UtcNow < deadline, $"Zeitüberschreitung: {description}");
            await Task.Delay(250, Ct);
        }
    }

    /// <summary>
    /// Bestätigt einen <c>DisplayAlert</c>-Dialog: findet den Titeltext
    /// und betätigt den Bestätigungsschalter innerhalb des nächst-
    /// engeren Dialog-Containers (disambiguiert gegenüber gleichnamigen
    /// Schaltern der Seite, z. B. dem Eintrags-„Löschen").
    /// </summary>
    /// <param name="app">Die laufende App-Instanz.</param>
    /// <param name="title">Der Titeltext des Dialogs.</param>
    /// <param name="confirmText">Der Name des Bestätigungsschalters.</param>
    private static void ConfirmAlert(AppInstance app, string title, string confirmText)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(15);
        while (DateTime.UtcNow < deadline)
        {
            var titleElement = app.Ui.FindByName(title);
            if (titleElement is not null)
            {
                // Engster Vorfahr, der einen Schalter mit dem
                // Bestätigungstext enthält — der Dialog-Container.
                var scope = titleElement;
                for (var i = 0; i < 10 && scope.Parent is { } parent; i++)
                {
                    scope = parent;
                    var button = scope
                        .FindAllDescendants(app.Ui.Conditions.ByName(confirmText))
                        .FirstOrDefault(e => e.ControlType == ControlType.Button);
                    if (button is not null)
                    {
                        if (button.Patterns.Invoke.IsSupported)
                        {
                            button.Patterns.Invoke.Pattern.Invoke();
                        }
                        else
                        {
                            button.Click();
                        }

                        return;
                    }
                }
            }

            Thread.Sleep(200);
        }

        throw new TimeoutException(
            $"Der Dialog „{title}\" mit dem Schalter „{confirmText}\" wurde nicht gefunden.");
    }
}
