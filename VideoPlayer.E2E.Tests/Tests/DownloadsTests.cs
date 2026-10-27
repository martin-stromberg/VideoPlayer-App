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

    /// <summary>
    /// Pflichtszenario Herkunft: ein automatisch geladener Download
    /// (Weiterschauen-Kandidat) erscheint auf der Downloads-Seite mit
    /// der Kennzeichnung „Cache"; ein manueller Download trägt sie nicht.
    /// </summary>
    [Fact]
    public async Task DownloadsPage_AutoDownload_ShowsCacheBadge()
    {
        using var server = NewServer();
        server.PlaybackMovieId = 501;
        server.PlaybackMovieTitle = "E2E-Cache-Film";
        // Weiterschauen-Eintrag → Auto-Download-Kandidat.
        server.PlaybackResumePositionSeconds = 60;
        server.PlaybackVideoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "testvideo.mp4");
        using var app = await StartAppAsync();
        await ConnectToPairingRequiredAsync(app, server);
        await PairAsync(app, server);
        await LoginAsync(app, server);
        await app.DevApi.WaitForModelAsync(
            m => m.RecentEntriesCount > 0,
            TimeSpan.FromSeconds(30),
            "Karussell „Neu im Programm\" befüllt", Ct);

        // Automatische Downloads aktivieren und die WLAN-Bedingung
        // aufheben (der E2E-Rechner läuft nicht zwingend über WLAN).
        app.Ui.OpenFlyoutItem("Einstellungen");
        app.Ui.FindByAutomationId("AutoDownloadSwitch", TimeSpan.FromSeconds(30));
        app.Ui.Toggle("WifiOnlySwitch");
        app.Ui.Toggle("AutoDownloadSwitch");

        // Der Auto-Download-Lauf läuft nach dem Schalterwechsel
        // erzwungen — der Eintrag erscheint mit „Cache"-Kennzeichnung.
        app.Ui.OpenFlyoutItem("Downloads");
        await WaitForTreeAsync(
            app,
            t => ContainsText(t.FindByAutomationId("DownloadsCollection"), server.PlaybackMovieTitle),
            $"Auto-Download-Eintrag „{server.PlaybackMovieTitle}\" auf der Downloads-Seite");
        await WaitForTreeAsync(
            app,
            t => ContainsText(t.FindByAutomationId("DownloadsCollection"), "Cache"),
            "Herkunfts-Kennzeichnung „Cache\" am Auto-Download-Eintrag");
    }

    /// <summary>
    /// Pflichtszenario Bereinigung: ein Download, dessen Ablaufdatum
    /// verstrichen ist (gesehen → „Löschen nach Wiedergabe, 0 Minuten"),
    /// wird beim nächsten Abgleich-Lauf entfernt.
    /// </summary>
    [Fact]
    public async Task DownloadsPage_WatchedDownload_IsCleanedUpAfterExpiry()
    {
        using var server = NewServer();
        server.PlaybackMovieId = 501;
        server.PlaybackMovieTitle = "E2E-Verfalls-Film";
        server.PlaybackVideoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "testvideo.mp4");
        // Beide Starts teilen sich das Profil — das Manifest übersteht
        // den Neustart und der Start-Abgleich wirkt auf die Ablage. Der
        // Name ist je Lauf eindeutig: ein persistiertes Profil mit
        // gespeicherten Zugangsdaten würde das Pairing direkt zur
        // Startseite führen statt zur Anmelde-Wahl.
        var profile = $"downloads-expiry-{Guid.NewGuid():N}";
        var app = await StartAppAsync(profile: profile);
        await ConnectToPairingRequiredAsync(app, server);
        await PairAsync(app, server);
        await LoginAsync(app, server);
        await app.DevApi.WaitForModelAsync(
            m => m.RecentEntriesCount > 0,
            TimeSpan.FromSeconds(30),
            "Karussell „Neu im Programm\" befüllt", Ct);

        // Option „Gesehene Downloads löschen" ohne Verzögerung aktivieren.
        // Das Zahlenfeld speichert über Completed/Unfocused — echte
        // UIA-Eingabe (Fokus + ValuePattern), damit der Wechsel der Seite
        // den Wert persistiert.
        app.Ui.OpenFlyoutItem("Einstellungen");
        app.Ui.FindByAutomationId("AutoDeletePlayedSwitch", TimeSpan.FromSeconds(30));
        app.Ui.Toggle("AutoDeletePlayedSwitch");
        app.Ui.EnterText("DeleteAfterPlaybackDelayEntry", "0");

        // Manuellen Download auslösen und auf den Abschluss warten.
        app.Ui.OpenFlyoutItem("Startseite");
        var carousel = (await app.DevApi.GetTreeAsync(Ct)).FindByAutomationId("RecentCarousel");
        Assert.NotNull(carousel?.Items);
        var index = carousel!.Items!.FindIndex(i => i.Text == server.PlaybackMovieTitle);
        Assert.True(index >= 0);
        await app.DevApi.InvokeAsync("RecentCarousel", index, Ct);
        app.Ui.FindByAutomationId("DownloadButton", TimeSpan.FromSeconds(30));
        app.Ui.ClickButton("DownloadButton");

        app.Ui.OpenFlyoutItem("Downloads");
        await WaitForTreeAsync(
            app,
            t => ContainsText(t.FindByAutomationId("DownloadsCollection"), server.PlaybackMovieTitle),
            "Fertiger Download-Eintrag auf der Downloads-Seite");

        // Gesehen-Wiedergabe aus der Datei (Detailseite erneut öffnen) →
        // Ablauf auf „jetzt" → der Abgleich beim nächsten App-Start
        // entfernt den Eintrag.
        app.Ui.OpenFlyoutItem("Startseite");
        var detailsTree = await app.DevApi.GetTreeAsync(Ct);
        var carousel2 = detailsTree.FindByAutomationId("RecentCarousel");
        Assert.NotNull(carousel2?.Items);
        await app.DevApi.InvokeAsync(
            "RecentCarousel",
            carousel2!.Items!.FindIndex(i => i.Text == server.PlaybackMovieTitle), Ct);
        app.Ui.FindByAutomationId("PlayButton", TimeSpan.FromSeconds(30));
        app.Ui.ClickButton("PlayButton");
        await app.DevApi.WaitForModelAsync(
            m => m.PlayerIsPlaying,
            TimeSpan.FromSeconds(60),
            "Wiedergabe des Downloads läuft", Ct);
        await app.DevApi.WaitForModelAsync(
            m => !m.PlayerIsPlaying,
            TimeSpan.FromSeconds(120),
            "Wiedergabe des Downloads beendet", Ct);

        // Neustart mit demselben Profil: der Abgleich beim Start
        // entfernt den inzwischen abgelaufenen Eintrag. Dispose ohne
        // Profil-Löschung — sonst startet die Folge-Instanz ohne Ablage.
        app.Dispose(deleteProfile: false);
        using var restarted = await StartAppAsync(profile: profile);
        await restarted.DevApi.WaitForModelAsync(
            m => m.CurrentPage == "Home" && m.ConnectionState == "Connected",
            TimeSpan.FromSeconds(60),
            "Neustart mit bestehendem Profil → Startseite", Ct);
        restarted.Ui.OpenFlyoutItem("Downloads");
        await WaitForTreeAsync(
            restarted,
            t => !ContainsText(t.FindByAutomationId("DownloadsCollection"), server.PlaybackMovieTitle),
            "Abgelaufener Download-Eintrag wurde entfernt");
    }

    /// <summary>Prüft rekursiv, ob ein Baumknoten, seine Einträge oder Kinder den Text tragen.</summary>
    /// <param name="node">Der zu durchsuchende Knoten.</param>
    /// <param name="text">Der gesuchte Text.</param>
    /// <returns><c>true</c>, wenn der Text gefunden wurde.</returns>
    private static bool ContainsText(DevApiTreeNode? node, string text)
    {
        if (node is null)
        {
            return false;
        }

        if (node.Text?.Contains(text, StringComparison.Ordinal) == true)
        {
            return true;
        }

        if (node.Items?.Any(i =>
                i.Text?.Contains(text, StringComparison.Ordinal) == true
                || i.Detail?.Contains(text, StringComparison.Ordinal) == true) == true)
        {
            return true;
        }

        return node.Children.Any(c => ContainsText(c, text));
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
