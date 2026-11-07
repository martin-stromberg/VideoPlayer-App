using VideoPlayer.E2E.Tests.Support;

namespace VideoPlayer.E2E.Tests.Tests;

/// <summary>
/// E2E-Szenario „Server wechseln" — Regressionstest für Issue #15:
/// Einstellungen → „Server wechseln" → bekannten Server aus der Liste
/// wählen → Startseite des neuen Servers. Die Listenauswahl läuft über
/// die Developer-API (<c>/invoke</c> mit <c>itemIndex</c> auf
/// <c>KnownServersView</c>), weil CollectionView-Einträge in der
/// Windows-UIA nicht zuverlässig adressierbar sind; die echten
/// UI-Aktionen (Flyout, Buttons, Eingaben) laufen über FlaUI.
/// </summary>
public sealed class ServerSwitchTests : E2ETestBase
{
    /// <summary>Initialisiert die Testklasse.</summary>
    /// <param name="fixture">Geteilte Build-Fixture.</param>
    public ServerSwitchTests(AppFixture fixture) : base(fixture) { }

    /// <summary>
    /// Zwei Mock-Server (A und B): auf A einloggen, über die
    /// Einstellungen auf B wechseln (Pairing + Auto-Anmeldung mit den
    /// gespeicherten Zugangsdaten), dann A in der Bekannte-Server-Liste
    /// wählen und zurück auf der Startseite von A landen.
    /// </summary>
    [Fact]
    public async Task SwitchToKnownServer_ReturnsToHomePage()
    {
        using var serverA = new MockVideoServer("AAAAAA", "e2e-device-a", "e2e@example.test", "e2e-pass");
        using var serverB = new MockVideoServer("BBBBBB", "e2e-device-b", "e2e@example.test", "e2e-pass");
        using var app = await StartAppAsync();

        // 1) Auf Server A einrichten, paaren und anmelden → Startseite.
        await ConnectToPairingRequiredAsync(app, serverA);
        await PairAsync(app, serverA);
        await LoginAsync(app, serverA);

        // 2) Einstellungen → „Server wechseln" → Einrichtungsseite.
        app.Ui.OpenFlyoutItem("Einstellungen");
        await app.DevApi.WaitForModelAsync(
            m => m.CurrentPage == "Settings",
            description: "Einstellungsseite geöffnet", cancellationToken: Ct);
        app.Ui.ClickButton("ServerSwitchButton");
        var setupModel = await app.DevApi.WaitForModelAsync(
            m => m.CurrentPage == "ServerSetup",
            description: "Server-Wechsel öffnet die Einrichtungsseite", cancellationToken: Ct);

        // Beim Wechsel bleibt die aktive Adresse vorausgefüllt.
        Assert.Equal(serverA.BaseAddress, setupModel.ActiveServerAddress);

        // 3) Adresse von Server B eingeben → Pairing → Auto-Anmeldung.
        app.Ui.EnterText("AddressEntry", serverB.BaseAddress);
        app.Ui.ClickButton("ConnectButton");
        await app.DevApi.WaitForModelAsync(
            m => m.IsPairingRequired && m.ActiveServerAddress == serverB.BaseAddress,
            TimeSpan.FromSeconds(30),
            "Server B erreichbar → Pairing erforderlich", Ct);
        app.Ui.EnterText("PairingCodeEntry", serverB.PairingCode);
        app.Ui.ClickButton("PairButton");

        // Nach dem Pairing meldet die App mit den gespeicherten
        // Zugangsdaten automatisch an → Startseite von Server B.
        var modelB = await app.DevApi.WaitForModelAsync(
            m => m.CurrentPage == "Home" && m.ConnectionState == "Connected",
            TimeSpan.FromSeconds(60),
            "Wechsel zu Server B → Startseite", Ct);
        Assert.Equal(serverB.BaseAddress, modelB.ActiveServerAddress);

        // 4) Erneut wechseln: Server A ist nun ein bekannter Server.
        // Hinweis: „serversetup" bleibt nach dem Auto-Login als
        // geschobene Route im Einstellungen-Stack der Shell — die
        // Menüauswahl landet daher je nach Stand direkt auf der
        // Einrichtungsseite statt auf den Einstellungen. Beides führt
        // zum Ziel; die Bekannte-Server-Liste liegt dort ohnehin.
        app.Ui.OpenFlyoutItem("Einstellungen");
        DevApiModel pageAfterMenu;
        try
        {
            pageAfterMenu = await app.DevApi.WaitForModelAsync(
                m => m.CurrentPage is "Settings" or "ServerSetup",
                description: "Einstellungsseite geöffnet (zweiter Wechsel)", cancellationToken: Ct);
        }
        catch (TimeoutException)
        {
            app.Ui.DumpTree(Path.Combine(Path.GetTempPath(), "uia-switch2.txt"));
            throw;
        }

        if (pageAfterMenu.CurrentPage == "Settings")
        {
            app.Ui.ClickButton("ServerSwitchButton");
            await app.DevApi.WaitForModelAsync(
                m => m.CurrentPage == "ServerSetup",
                description: "Einrichtungsseite mit bekannter Liste", cancellationToken: Ct);
        }

        // Bekannte-Server-Liste über den logischen Baum lesen —
        // CollectionView-Einträge sind in der UIA nicht verlässlich.
        var tree = await app.DevApi.GetTreeAsync(Ct);
        var knownView = tree.FindByAutomationId("KnownServersView");
        Assert.NotNull(knownView);
        Assert.NotNull(knownView.Items);
        var items = knownView.Items;
        Assert.Equal(2, items.Count);

        var indexA = items.FindIndex(i =>
            string.Equals(i.Detail, serverA.BaseAddress, StringComparison.OrdinalIgnoreCase));
        Assert.True(indexA >= 0,
            $"Server A ({serverA.BaseAddress}) fehlt in der Bekannte-Server-Liste: " +
            string.Join(", ", items.Select(i => $"{i.Text}/{i.Detail}")));

        // Auswahl per /invoke + itemIndex → echte SelectedItem-
        // Semantik (SelectionChanged) ohne Koordinatenklick.
        await app.DevApi.InvokeAsync("KnownServersView", indexA, Ct);

        // Auto-Anmeldung auf A → Startseite mit Server A aktiv.
        var modelA = await app.DevApi.WaitForModelAsync(
            m => m.CurrentPage == "Home" && m.ConnectionState == "Connected",
            TimeSpan.FromSeconds(60),
            "Wechsel zurück zu Server A → Startseite", Ct);
        Assert.Equal(serverA.BaseAddress, modelA.ActiveServerAddress);
    }
}
