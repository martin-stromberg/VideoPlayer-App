using VideoPlayer.E2E.Tests.Support;

namespace VideoPlayer.E2E.Tests.Tests;

/// <summary>
/// E2E-Szenario „Server-Setup": Erststart ohne Server landet auf der
/// Einrichtungsseite; die Adresseingabe und der Verbinden-Button laufen
/// über die echte UIA-Eingabekette, die Zustands-Assertions über die
/// Developer-API (<c>GET /model</c>).
/// </summary>
public sealed class ServerSetupTests : E2ETestBase
{
    /// <summary>Initialisiert die Testklasse.</summary>
    /// <param name="fixture">Geteilte Build-Fixture.</param>
    public ServerSetupTests(AppFixture fixture) : base(fixture) { }

    /// <summary>
    /// Erststart ohne hinterlegten Server: die App zeigt die
    /// Einrichtungsseite mit Adresseingabe und Verbinden-Button.
    /// </summary>
    [Fact]
    public async Task FreshStart_ShowsServerSetupPage()
    {
        using var app = await StartAppAsync();

        var model = await app.DevApi.WaitForModelAsync(
            m => m.CurrentPage == "ServerSetup" && m.ConnectionState == "NeedsServerSetup",
            description: "Erststart landet auf der Server-Einrichtung", cancellationToken: Ct);

        Assert.Empty(model.KnownServers);
        Assert.Null(model.ActiveServerAddress);
        Assert.True(app.Ui.Exists("AddressEntry"));
        Assert.True(app.Ui.Exists("ConnectButton"));
    }

    /// <summary>
    /// Ungültige Adresseingabe: die Formatprüfung meldet den Fehler
    /// inline, ohne einen Server-Kontakt.
    /// </summary>
    [Fact]
    public async Task InvalidAddress_ShowsValidationError()
    {
        using var app = await StartAppAsync();
        await app.DevApi.WaitForModelAsync(
            m => m.CurrentPage == "ServerSetup",
            description: "Server-Einrichtung sichtbar", cancellationToken: Ct);

        app.Ui.EnterText("AddressEntry", "ftp://server");
        app.Ui.ClickButton("ConnectButton");

        var model = await app.DevApi.WaitForModelAsync(
            m => m.ServerSetupError is not null,
            description: "Validierungsfehler der Adresse", cancellationToken: Ct);
        Assert.Contains("gültige Serveradresse", model.ServerSetupError);
        Assert.Equal("ServerSetup", model.CurrentPage);
    }

    /// <summary>
    /// Nicht erreichbarer Server: die Erreichbarkeitsprüfung scheitert,
    /// die App meldet den Fehler inline und bleibt auf der
    /// Einrichtungsseite (der API-Client wiederholt Transportfehler —
    /// großzügiges Timeout).
    /// </summary>
    [Fact]
    public async Task UnreachableServer_ShowsError()
    {
        using var app = await StartAppAsync();
        await app.DevApi.WaitForModelAsync(
            m => m.CurrentPage == "ServerSetup",
            description: "Server-Einrichtung sichtbar", cancellationToken: Ct);

        // Port 9 (discard) ist auf dem Testrechner nicht belegt —
        // die Verbindung wird sofort abgelehnt.
        app.Ui.EnterText("AddressEntry", "http://127.0.0.1:9/");
        app.Ui.ClickButton("ConnectButton");

        var model = await app.DevApi.WaitForModelAsync(
            m => m.ServerSetupError is not null && !m.IsServerCheckRunning,
            TimeSpan.FromSeconds(45),
            "Erreichbarkeitsprüfung scheitert → Fehlermeldung", Ct);
        Assert.Contains("nicht erreichbar", model.ServerSetupError);
        Assert.Equal("ServerSetup", model.CurrentPage);
        Assert.False(model.IsPairingRequired);
    }

    /// <summary>
    /// Erreichbarer Server ohne Gate-Schlüssel: der Gesundheitscheck
    /// läuft durch, die App fordert das Geräte-Pairing ein.
    /// </summary>
    [Fact]
    public async Task ReachableServer_RequiresPairing()
    {
        using var server = new MockVideoServer("E2ETEST", "e2e-device-a", "e2e@example.test", "e2e-pass");
        using var app = await StartAppAsync();

        await ConnectToPairingRequiredAsync(app, server);

        // Die Pairing-Eingabe ist im Elementbaum sichtbar (UIA sichtbar,
        // Dev-API-Baum als Gegenprobe).
        Assert.True(app.Ui.Exists("PairingCodeEntry"));
        Assert.True(app.Ui.Exists("PairButton"));
        var tree = await app.DevApi.GetTreeAsync(Ct);
        Assert.NotNull(tree.FindByAutomationId("PairingCodeEntry"));
    }
}
