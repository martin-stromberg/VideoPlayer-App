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
