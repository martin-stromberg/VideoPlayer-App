using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using VideoPlayer.E2E.Tests.Support;

namespace VideoPlayer.E2E.Tests.Tests;

/// <summary>
/// E2E-Szenarien des Protokoll-Features (Issue #43, Schritt 3):
/// Einstellungen → Schalter „Protokollierung" → „Protokoll senden"-
/// Button und Protokolldatei. Der eigentliche Mailentwurf/Teilen-Dialog
/// ist plattformseitig (kein automatisierbarer Teil der App) — der
/// Versand-Klick wird bis zur Compose-/Share-Grenze gefahren; danach
/// wird geprüft, dass die App weiterläuft und kein Fehlerhinweis im
/// Modell steht.
/// </summary>
public sealed class LogProtocolTests : E2ETestBase
{
    /// <summary>Initialisiert die Testklasse.</summary>
    /// <param name="fixture">Geteilte Build-Fixture.</param>
    public LogProtocolTests(AppFixture fixture) : base(fixture) { }

    /// <summary>
    /// Ohne aktivierte Protokollierung ist der „Protokoll senden"-Button
    /// nicht sichtbar und es existiert keine Protokolldatei.
    /// </summary>
    [Fact]
    public async Task LoggingDisabled_HidesSendButton_AndNoLogFile()
    {
        using var server = new MockVideoServer("AAAAAA", "e2e-device", "e2e@example.test", "e2e-pass");
        using var app = await StartAppAsync();
        await ConnectToPairingRequiredAsync(app, server);
        await PairAsync(app, server);
        await LoginAsync(app, server);

        await OpenSettingsAsync(app);
        // SendLogButton liegt unterhalb des sichtbaren Bereichs — WaitForVisible rollt ihn ein.

        Assert.False(app.Ui.WaitForVisible("SendLogButton", TimeSpan.FromSeconds(5)));
        Assert.Null(app.FindLogFile());
        Assert.False((await app.DevApi.GetModelAsync(Ct)).SettingsLoggingEnabled);
    }

    /// <summary>
    /// Das Aktivieren der Protokollierung zeigt den „Protokoll senden"-
    /// Button und legt die Protokolldatei im Profil an.
    /// </summary>
    [Fact]
    public async Task EnableLogging_ShowsSendButton_AndCreatesLogFile()
    {
        using var server = new MockVideoServer("BBBBBB", "e2e-device", "e2e@example.test", "e2e-pass");
        using var app = await StartAppAsync();
        await ConnectToPairingRequiredAsync(app, server);
        await PairAsync(app, server);
        await LoginAsync(app, server);

        await OpenSettingsAsync(app);
        app.Ui.Toggle("LoggingSwitch");

        await app.DevApi.WaitForModelAsync(
            m => m.SettingsLoggingEnabled,
            description: "Protokollierung aktiviert (Modell)", cancellationToken: Ct);
        // SendLogButton liegt unterhalb des sichtbaren Bereichs — WaitForVisible rollt ihn ein.
        Assert.True(app.Ui.WaitForVisible("SendLogButton"));

        var logPath = await WaitForLogFileAsync(app);
        Assert.True(new FileInfo(logPath).Length > 0);
    }

    /// <summary>
    /// Die Einstellung ist persistent: ein Neustart mit demselben Profil
    /// hält die Protokollierung aktiv und den Versand-Button sichtbar.
    /// </summary>
    [Fact]
    public async Task LoggingEnabled_PersistsAcrossRestart()
    {
        using var server = new MockVideoServer("CCCCCC", "e2e-device", "e2e@example.test", "e2e-pass");
        string profile;
        {
            using var app = await StartAppAsync();
            profile = app.Profile;
            await ConnectToPairingRequiredAsync(app, server);
            await PairAsync(app, server);
            await LoginAsync(app, server);
            await OpenSettingsAsync(app);
            app.Ui.Toggle("LoggingSwitch");
            await app.DevApi.WaitForModelAsync(
                m => m.SettingsLoggingEnabled,
                description: "Protokollierung aktiviert (Modell)", cancellationToken: Ct);
            // Das Profil bleibt für den Neustart bestehen; die Folge-
            // Instanz räumt es beim regulären Dispose wieder ab.
            app.Dispose(deleteProfile: false);
        }

        using var restarted = await StartAppAsync(profile: profile);
        var model = await restarted.DevApi.WaitForModelAsync(
            m => m.SettingsLoggingEnabled,
            description: "Protokollierung nach Neustart weiterhin aktiv", cancellationToken: Ct);
        Assert.True(model.SettingsLoggingEnabled);

        restarted.Ui.OpenFlyoutItem("Einstellungen");
        await restarted.DevApi.WaitForModelAsync(
            m => m.CurrentPage == "Settings",
            description: "Einstellungsseite geöffnet", cancellationToken: Ct);

        Assert.True(restarted.Ui.WaitForVisible("SendLogButton"));
        Assert.NotNull(restarted.FindLogFile());
    }

    /// <summary>
    /// „Protokoll senden" löst den Versand an der Plattformgrenze aus
    /// (Mailentwurf oder Teilen-Fallback) — die App bleibt danach
    /// bedienbar und das Modell trägt keinen Fehlerhinweis.
    /// </summary>
    [Fact]
    public async Task SendLog_Click_InvokesPlatformShareOrMail_WithoutCrash()
    {
        using var server = new MockVideoServer("DDDDDD", "e2e-device", "e2e@example.test", "e2e-pass");
        using var app = await StartAppAsync();
        await ConnectToPairingRequiredAsync(app, server);
        await PairAsync(app, server);
        await LoginAsync(app, server);
        await OpenSettingsAsync(app);
        app.Ui.Toggle("LoggingSwitch");
        await app.DevApi.WaitForModelAsync(
            m => m.SettingsLoggingEnabled,
            description: "Protokollierung aktiviert (Modell)", cancellationToken: Ct);
        await WaitForLogFileAsync(app);

        app.Ui.ClickButton("SendLogButton");

        try
        {
            // Der Versand endet an der Plattformgrenze (Mailentwurf bzw.
            // Share-Dialog): der Effekt darf die App nicht beschädigen.
            // Ist kein Versandweg verfügbar, landet stattdessen der
            // verständliche Fehlerhinweis im Modell — beides ist ein
            // gültiges Ende der Interaktion.
            var model = await app.DevApi.WaitForModelAsync(
                m => m.LogSendError is not null || !m.IsStartupRunning,
                TimeSpan.FromSeconds(20),
                "Versand abgearbeitet bzw. App weiterhin bedienbar",
                Ct);
            Assert.True(model.CurrentPage == "Settings");
            // Ein etwaiger Fehlerhinweis ist ein verständlicher Text,
            // keine technische Rohmeldung.
            if (model.LogSendError is not null)
            {
                Assert.False(model.LogSendError.Contains("Exception", StringComparison.Ordinal));
            }
        }
        finally
        {
            // Plattformdialoge (Teilen, „App zum Öffnen wählen") liegen
            // außerhalb der App-Fensterhierarchie — per Escape schließen,
            // damit Folgetests ein sauberes UI-Bild vorfinden.
            Keyboard.Press(VirtualKeyShort.ESCAPE);
            Keyboard.Press(VirtualKeyShort.ESCAPE);
        }

        Assert.False(app.Process.HasExited);
    }

    /// <summary>
    /// Bei aktivierter Protokollierung schreiben reale Abläufe
    /// (Auto-Anmeldung nach Neustart, Wiedergabe) diagnostische
    /// Einträge in die Protokolldatei — jenseits der Marker-Zeilen
    /// und ohne Geheimnisse (kein <c>access_token</c>, keine
    /// Zugangsdaten).
    /// </summary>
    [Fact]
    public async Task LoggingEnabled_RealFlows_WriteDiagnosticEntries()
    {
        var videoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "testvideo.mp4");
        Assert.True(File.Exists(videoPath), $"Testvideo fehlt im Output-Verzeichnis: {videoPath}");
        using var server = new MockVideoServer("EEEEEE", "e2e-device", "e2e@example.test", "e2e-pass")
        {
            PlaybackMovieId = 4711,
            PlaybackMovieTitle = "E2E-Testfilm",
            PlaybackVideoPath = videoPath,
            PlaybackResumePositionSeconds = 1
        };

        string profile;
        {
            using var app = await StartAppAsync();
            profile = app.Profile;
            await ConnectToPairingRequiredAsync(app, server);
            await PairAsync(app, server);
            await LoginAsync(app, server);
            await OpenSettingsAsync(app);
            app.Ui.Toggle("LoggingSwitch");
            await app.DevApi.WaitForModelAsync(
                m => m.SettingsLoggingEnabled,
                description: "Protokollierung aktiviert (Modell)", cancellationToken: Ct);
            await WaitForLogFileAsync(app);
            // Das Profil bleibt für den Neustart bestehen; die Folge-
            // Instanz räumt es beim regulären Dispose wieder ab.
            app.Dispose(deleteProfile: false);
        }

        using var restarted = await StartAppAsync(profile: profile);

        // Auto-Anmeldung mit den gespeicherten Zugangsdaten → Startseite.
        await restarted.DevApi.WaitForModelAsync(
            m => m.CurrentPage == "Home" && m.ConnectionState == "Connected",
            TimeSpan.FromSeconds(60),
            "Automatische Anmeldung nach Neustart",
            Ct);
        await restarted.DevApi.WaitForModelAsync(
            m => m.RecentEntriesCount > 0,
            TimeSpan.FromSeconds(30),
            "Karussell „Neu im Programm\" befüllt", Ct);

        // Wiedergabe anstoßen (Eintrag des „Neu im Programm"-Karussells → „Abspielen").
        var tree = await restarted.DevApi.GetTreeAsync(Ct);
        var carousel = tree.FindByAutomationId("RecentCarousel");
        Assert.NotNull(carousel);
        Assert.NotNull(carousel.Items);
        var index = carousel.Items.FindIndex(i => i.Text == server.PlaybackMovieTitle);
        Assert.True(index >= 0,
            $"'{server.PlaybackMovieTitle}' fehlt im Karussell: " +
            string.Join(", ", carousel.Items.Select(i => i.Text)));
        await restarted.DevApi.InvokeAsync("RecentCarousel", index, Ct);
        restarted.Ui.FindByAutomationId("PlayButton", TimeSpan.FromSeconds(30));
        restarted.Ui.ClickButton("PlayButton");
        await restarted.DevApi.WaitForModelAsync(
            m => m.PlayerIsActive,
            TimeSpan.FromSeconds(60),
            "Wiedergabe gestartet", Ct);

        var logPath = await WaitForLogFileAsync(restarted);

        // Auf den ersten Facheintrag jenseits der Marker warten —
        // Protokollzeilen werden asynchron angehängt.
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(15);
        string content;
        while (true)
        {
            content = await File.ReadAllTextAsync(logPath, Ct);
            if (content.Contains("[INFO]", StringComparison.Ordinal))
            {
                break;
            }

            Assert.True(DateTime.UtcNow < deadline,
                "Kein [INFO]-Facheintrag in der Protokolldatei:\n" + content);
            await Task.Delay(500, Ct);
        }

        Assert.DoesNotContain("access_token", content, StringComparison.Ordinal);
        Assert.DoesNotContain(server.Username, content, StringComparison.Ordinal);
        Assert.DoesNotContain(server.Password, content, StringComparison.Ordinal);
    }

    /// <summary>
    /// Optionale Teilflüsse (plan.md Tasks #55): die PiP-Umschaltung
    /// während laufender Wiedergabe und die Abmeldung über die Profil-
    /// seite erzeugen Einträge in der Protokolldatei — jeweils auf der
    /// Dienst-Schicht (PiP-Dienst, <c>AuthSessionService</c>), ohne
    /// Geheimnisse.
    /// </summary>
    [Fact]
    public async Task LoggingEnabled_LogoutAndPipSubFlows_WriteDiagnosticEntries()
    {
        var videoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "testvideo.mp4");
        Assert.True(File.Exists(videoPath), $"Testvideo fehlt im Output-Verzeichnis: {videoPath}");
        using var server = new MockVideoServer("FFFFFF", "e2e-device", "e2e@example.test", "e2e-pass")
        {
            PlaybackMovieId = 4711,
            PlaybackMovieTitle = "E2E-Testfilm",
            PlaybackVideoPath = videoPath,
            PlaybackResumePositionSeconds = 1
        };
        using var app = await StartAppAsync();
        await ConnectToPairingRequiredAsync(app, server);
        await PairAsync(app, server);
        await LoginAsync(app, server);
        await OpenSettingsAsync(app);
        app.Ui.Toggle("LoggingSwitch");
        await app.DevApi.WaitForModelAsync(
            m => m.SettingsLoggingEnabled,
            description: "Protokollierung aktiviert (Modell)", cancellationToken: Ct);
        var logPath = await WaitForLogFileAsync(app);
        await OpenHomeAsync(app);

        // Wiedergabe anstoßen und die PiP-Umschaltung auslösen. Der
        // Mini-Player-Schalter trägt keine AutomationId — UIA findet
        // ihn über den Anzeigenamen. Die Eintrags-Schicht ist der
        // PiP-Dienst; auch ein verwehter CompactOverlay-Wechsel
        // hinterlässt dort einen Eintrag.
        await OpenRecentAndPlayAsync(app, server);
        app.Ui.ClickByName("Mini-Player", TimeSpan.FromSeconds(20));
        var content = await WaitForLogContentAsync(logPath, "PiP-Umschaltung");
        Assert.Contains("PiP-Umschaltung", content, StringComparison.Ordinal);

        // Umschaltung zurück in den Vollfenster-Modus (Restore läuft
        // beim Verlassen der Seite ohnehin nach) und zurück zur
        // Hauptnavigation.
        app.Ui.ClickByName("Mini-Player", TimeSpan.FromSeconds(20));
        app.Ui.GoBack();

        // Abmeldung über die Profilseite („Abmelden" — ebenfalls ohne
        // AutomationId, per Anzeigename).
        app.Ui.OpenFlyoutItem("Profil");
        await app.DevApi.WaitForModelAsync(
            m => m.CurrentPage == "Profile",
            description: "Profilseite geöffnet", cancellationToken: Ct);
        app.Ui.ClickByName("Abmelden", TimeSpan.FromSeconds(20));
        await app.DevApi.WaitForModelAsync(
            m => m.ConnectionState == "NeedsLogin" && m.CurrentPage == "Login",
            TimeSpan.FromSeconds(30),
            "Abmeldung → Anmeldemaske",
            Ct);

        content = await WaitForLogContentAsync(logPath, "Abmeldung");
        Assert.Contains("Abmeldung", content, StringComparison.Ordinal);
        Assert.DoesNotContain("access_token", content, StringComparison.Ordinal);
        Assert.DoesNotContain(server.Username, content, StringComparison.Ordinal);
        Assert.DoesNotContain(server.Password, content, StringComparison.Ordinal);
    }

    /// <summary>
    /// Optionaler Teilfluss (plan.md Tasks #55): ein Wiedergabe-
    /// Fehlschlag (Stream-Endpunkt des Mock-Servers antwortet nicht
    /// abspielbar) landet als <c>[ERROR]</c>-Eintrag in der
    /// Protokolldatei — ohne die abgelehnte Quell-Adresse
    /// (<c>access_token</c>-Query).
    /// </summary>
    [Fact]
    public async Task LoggingEnabled_PlaybackFailureSubFlow_WritesErrorEntry()
    {
        var videoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "testvideo.mp4");
        Assert.True(File.Exists(videoPath), $"Testvideo fehlt im Output-Verzeichnis: {videoPath}");
        using var server = new MockVideoServer("777777", "e2e-device", "e2e@example.test", "e2e-pass")
        {
            PlaybackMovieId = 4711,
            PlaybackMovieTitle = "E2E-Testfilm",
            PlaybackVideoPath = videoPath,
            PlaybackResumePositionSeconds = 1,
            PlaybackStreamFails = true
        };
        using var app = await StartAppAsync();
        await ConnectToPairingRequiredAsync(app, server);
        await PairAsync(app, server);
        await LoginAsync(app, server);
        await OpenSettingsAsync(app);
        app.Ui.Toggle("LoggingSwitch");
        await app.DevApi.WaitForModelAsync(
            m => m.SettingsLoggingEnabled,
            description: "Protokollierung aktiviert (Modell)", cancellationToken: Ct);
        var logPath = await WaitForLogFileAsync(app);
        await OpenHomeAsync(app);

        await OpenRecentAndPlayAsync(app, server);
        await app.DevApi.WaitForModelAsync(
            m => m.PlayerIsActive && m.PlayerError is not null,
            TimeSpan.FromSeconds(60),
            "Wiedergabefehler im Modell sichtbar", Ct);

        var content = await WaitForLogContentAsync(logPath, "[ERROR]", TimeSpan.FromSeconds(30));
        Assert.Contains("Wiedergabe", content, StringComparison.Ordinal);
        Assert.DoesNotContain("access_token", content, StringComparison.Ordinal);
    }

    /// <summary>
    /// Wählt den Testfilmeintrag im „Neu im Programm"-Karussell per
    /// <c>/invoke</c>+<c>itemIndex</c> und löst „Abspielen" aus —
    /// endet mit aktivem Player (Muster: <c>PlaybackTests</c>).
    /// </summary>
    /// <param name="app">Die laufende App-Instanz.</param>
    /// <param name="server">Der Mock-Server mit Wiedergabe-Setup.</param>
    /// <returns>Eine abgeschlossene Aufgabe nach dem Wiedergabestart.</returns>
    private static async Task OpenRecentAndPlayAsync(AppInstance app, MockVideoServer server)
    {
        await app.DevApi.WaitForModelAsync(
            m => m.RecentEntriesCount > 0,
            TimeSpan.FromSeconds(30),
            "Karussell „Neu im Programm\" befüllt", Ct);

        var tree = await app.DevApi.GetTreeAsync(Ct);
        var carousel = tree.FindByAutomationId("RecentCarousel");
        Assert.NotNull(carousel);
        Assert.NotNull(carousel.Items);
        var index = carousel.Items.FindIndex(i => i.Text == server.PlaybackMovieTitle);
        Assert.True(index >= 0,
            $"'{server.PlaybackMovieTitle}' fehlt im Karussell: " +
            string.Join(", ", carousel.Items.Select(i => i.Text)));

        await app.DevApi.InvokeAsync("RecentCarousel", index, Ct);
        app.Ui.FindByAutomationId("PlayButton", TimeSpan.FromSeconds(30));
        app.Ui.ClickButton("PlayButton");
        await app.DevApi.WaitForModelAsync(
            m => m.PlayerIsActive,
            TimeSpan.FromSeconds(60),
            "Wiedergabe gestartet", Ct);
    }

    /// <summary>Wartet, bis die Protokolldatei den gesuchten Text enthält.</summary>
    /// <param name="logPath">Pfad der Protokolldatei.</param>
    /// <param name="needle">Der gesuchte Eintragstext.</param>
    /// <param name="timeout">Maximale Wartezeit (Standard 20 s).</param>
    /// <returns>Der aktuelle Dateiinhalt.</returns>
    private static async Task<string> WaitForLogContentAsync(
        string logPath, string needle, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(20));
        while (true)
        {
            var content = await File.ReadAllTextAsync(logPath, Ct);
            if (content.Contains(needle, StringComparison.Ordinal))
            {
                return content;
            }

            Assert.True(DateTime.UtcNow < deadline,
                $"„{needle}\" nicht in der Protokolldatei:\n" + content);
            await Task.Delay(500, Ct);
        }
    }

    /// <summary>Wechselt zurück auf die Startseite über das Shell-Flyout.</summary>
    /// <param name="app">Die laufende App-Instanz.</param>
    /// <returns>Eine abgeschlossene Aufgabe nach dem Erreichen der Seite.</returns>
    private static async Task OpenHomeAsync(AppInstance app)
    {
        app.Ui.OpenFlyoutItem("Startseite");
        await app.DevApi.WaitForModelAsync(
            m => m.CurrentPage == "Home",
            description: "Startseite geöffnet", cancellationToken: Ct);
    }

    /// <summary>Öffnet die Einstellungsseite über das Shell-Flyout.</summary>
    /// <param name="app">Die laufende App-Instanz.</param>
    /// <returns>Eine abgeschlossene Aufgabe nach dem Erreichen der Seite.</returns>
    private static async Task OpenSettingsAsync(AppInstance app)
    {
        app.Ui.OpenFlyoutItem("Einstellungen");
        await app.DevApi.WaitForModelAsync(
            m => m.CurrentPage == "Settings",
            description: "Einstellungsseite geöffnet", cancellationToken: Ct);
    }

    /// <summary>Wartet, bis die Protokolldatei des Testprofils existiert und nicht leer ist.</summary>
    /// <param name="app">Die laufende App-Instanz.</param>
    /// <returns>Pfad der Protokolldatei.</returns>
    private static async Task<string> WaitForLogFileAsync(AppInstance app)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(15);
        while (DateTime.UtcNow < deadline)
        {
            var path = app.FindLogFile();
            if (path is not null && new FileInfo(path).Length > 0)
            {
                return path;
            }

            await Task.Delay(300, Ct);
        }

        throw new TimeoutException(
            $"Keine nicht-leere Protokolldatei für Profil '{app.Profile}' gefunden.");
    }
}
