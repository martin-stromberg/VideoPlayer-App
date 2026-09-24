using VideoPlayer.E2E.Tests.Support;

namespace VideoPlayer.E2E.Tests.Tests;

/// <summary>
/// E2E-Szenario „QR-/Ticket-Onboarding" (Schritt 5b): die
/// Kopplungs-Eingabe auf der Einrichtungsseite löst das Bootstrap-Ticket
/// des Mock-Servers ein — Ticket-Adresse (mit Server-Übernahme nach
/// Erreichbarkeitsprüfung) und Kurzcode gegen den verbundenen Server —
/// bis zum direkten Einstieg auf die Startseite ohne Anmeldemaske.
/// </summary>
public sealed class BootstrapTests : E2ETestBase
{
    /// <summary>Initialisiert die Testklasse.</summary>
    /// <param name="fixture">Geteilte Build-Fixture.</param>
    public BootstrapTests(AppFixture fixture) : base(fixture) { }

    /// <summary>
    /// Ticket-Adresse (QR-Inhalt): der Server wird erst nach der
    /// Erreichbarkeitsprüfung übernommen, dann das Ticket eingelöst —
    /// die App landet ohne Anmeldemaske direkt auf der Startseite.
    /// </summary>
    [Fact]
    public async Task TicketUrl_BootstrapsDirectlyToHome()
    {
        using var server = new MockVideoServer("E2ETEST", "e2e-device-a", "e2e@example.test", "e2e-pass");
        using var app = await StartAppAsync();
        await app.DevApi.WaitForModelAsync(
            m => m.CurrentPage == "ServerSetup",
            description: "Erststart landet auf der Server-Einrichtung", cancellationToken: Ct);

        var ticketUrl = $"{server.BaseAddress}pairing?t={server.BootstrapTicket}";
        app.Ui.EnterText("BootstrapTicketEntry", ticketUrl);
        app.Ui.ClickButton("BootstrapButton");

        var model = await app.DevApi.WaitForModelAsync(
            m => m.CurrentPage == "Home" && m.ConnectionState == "Connected",
            TimeSpan.FromSeconds(40),
            "Bootstrap erfolgreich → direkter Einstieg auf die Startseite", Ct);
        Assert.Equal(server.BaseAddress, model.ActiveServerAddress);
        Assert.False(model.IsPairingRequired);
        Assert.Null(model.BootstrapError);
        Assert.Equal(1, server.BootstrapCount);
        Assert.Equal(0, server.PairingExchangeCount);
        Assert.Equal(0, server.LoginCount); // kein Credential-Login nötig
        // Der Server wurde dauerhaft übernommen.
        Assert.Contains(model.KnownServers, s => s.Address == server.BaseAddress);
    }

    /// <summary>
    /// Kurzcode (manuelle Eingabe): nach der klassischen Serververbindung
    /// löst der 8-stellige Alias das Bootstrap gegen den verbundenen
    /// Server ein — ohne Pairing-Code und ohne Anmeldemaske.
    /// </summary>
    [Fact]
    public async Task ShortCode_BootstrapsOnConnectedServer()
    {
        using var server = new MockVideoServer("E2ETEST", "e2e-device-a", "e2e@example.test", "e2e-pass");
        using var app = await StartAppAsync();
        await ConnectToPairingRequiredAsync(app, server);

        app.Ui.EnterText("BootstrapTicketEntry", server.BootstrapShortCode);
        app.Ui.ClickButton("BootstrapButton");

        var model = await app.DevApi.WaitForModelAsync(
            m => m.CurrentPage == "Home" && m.ConnectionState == "Connected",
            TimeSpan.FromSeconds(40),
            "Kurzcode-Bootstrap erfolgreich → Startseite", Ct);
        Assert.Equal(server.BaseAddress, model.ActiveServerAddress);
        Assert.Equal(1, server.BootstrapCount);
        Assert.Equal(0, server.PairingExchangeCount);
        Assert.Equal(0, server.LoginCount);
    }

    /// <summary>
    /// Ungültiges Ticket: der erreichbare Server lehnt das Ticket ab
    /// (401) — die App zeigt den Inline-Fehler und übernimmt den
    /// Server dennoch nicht dauerhaft als gekoppelt.
    /// </summary>
    [Fact]
    public async Task InvalidTicket_ShowsBootstrapError()
    {
        using var server = new MockVideoServer("E2ETEST", "e2e-device-a", "e2e@example.test", "e2e-pass");
        using var app = await StartAppAsync();
        await app.DevApi.WaitForModelAsync(
            m => m.CurrentPage == "ServerSetup",
            description: "Erststart landet auf der Server-Einrichtung", cancellationToken: Ct);

        app.Ui.EnterText("BootstrapTicketEntry", $"{server.BaseAddress}pairing?t=falsches-ticket");
        app.Ui.ClickButton("BootstrapButton");

        var model = await app.DevApi.WaitForModelAsync(
            m => m.BootstrapError is not null && !m.IsBootstrapRunning,
            TimeSpan.FromSeconds(30),
            "Server lehnt das Ticket ab → Kopplungs-Fehler", Ct);
        Assert.Contains("ungültig", model.BootstrapError);
        Assert.NotEqual("Connected", model.ConnectionState);
        Assert.Equal(1, server.BootstrapCount);
        Assert.Equal(0, server.LoginCount);
    }

    /// <summary>
    /// Unerreichbarer Ticket-Server: die Erreichbarkeitsprüfung scheitert
    /// vor dem Austausch — Inline-Fehler, keine Server-Übernahme.
    /// </summary>
    [Fact]
    public async Task UnreachableTicketServer_ShowsError_WithoutPersisting()
    {
        using var server = new MockVideoServer("E2ETEST", "e2e-device-a", "e2e@example.test", "e2e-pass");
        using var app = await StartAppAsync();
        await app.DevApi.WaitForModelAsync(
            m => m.CurrentPage == "ServerSetup",
            description: "Erststart landet auf der Server-Einrichtung", cancellationToken: Ct);

        // Port 1: nichts lauscht → Verbindungsverweigerung.
        app.Ui.EnterText("BootstrapTicketEntry", "http://127.0.0.1:1/pairing?t=ticket-x");
        app.Ui.ClickButton("BootstrapButton");

        var model = await app.DevApi.WaitForModelAsync(
            m => m.BootstrapError is not null && !m.IsBootstrapRunning,
            TimeSpan.FromSeconds(40),
            "Ticket-Server nicht erreichbar → Fehler ohne Übernahme", Ct);
        Assert.Null(model.ActiveServerAddress);
        Assert.DoesNotContain(model.KnownServers, s => s.Address == "http://127.0.0.1:1/");
        Assert.Equal(0, server.BootstrapCount);
    }

    /// <summary>
    /// Unlesbare Eingabe: die lokale Formatprüfung greift vor jedem
    /// Server-Kontakt — Fehler ohne ausgehenden Request.
    /// </summary>
    [Fact]
    public async Task MalformedInput_ShowsLocalError_WithoutServerContact()
    {
        using var server = new MockVideoServer("E2ETEST", "e2e-device-a", "e2e@example.test", "e2e-pass");
        using var app = await StartAppAsync();
        await app.DevApi.WaitForModelAsync(
            m => m.CurrentPage == "ServerSetup",
            description: "Erststart landet auf der Server-Einrichtung", cancellationToken: Ct);

        app.Ui.EnterText("BootstrapTicketEntry", "ftp://nas/pairing?t=x");
        app.Ui.ClickButton("BootstrapButton");

        var model = await app.DevApi.WaitForModelAsync(
            m => m.BootstrapError is not null,
            description: "Formatfehler der Kopplungs-Eingabe", cancellationToken: Ct);
        Assert.NotNull(model.BootstrapError);
        Assert.Equal(0, server.BootstrapCount);
    }

    /// <summary>
    /// Persistenz: nach dem Neustart des Profils ist die
    /// Bootstrap-Sitzung noch aktiv — gespeichertes Token + Refresh-Token
    /// führen ohne Anmeldemaske direkt auf die Startseite.
    /// </summary>
    [Fact]
    public async Task BootstrappedSession_SurvivesAppRestart()
    {
        using var server = new MockVideoServer("E2ETEST", "e2e-device-a", "e2e@example.test", "e2e-pass");
        string profile;
        using (var app = await StartAppAsync())
        {
            profile = app.Profile;
            await app.DevApi.WaitForModelAsync(
                m => m.CurrentPage == "ServerSetup",
                description: "Erststart landet auf der Server-Einrichtung", cancellationToken: Ct);
            app.Ui.EnterText("BootstrapTicketEntry", $"{server.BaseAddress}pairing?t={server.BootstrapTicket}");
            app.Ui.ClickButton("BootstrapButton");
            await app.DevApi.WaitForModelAsync(
                m => m.CurrentPage == "Home" && m.ConnectionState == "Connected",
                TimeSpan.FromSeconds(40),
                "Bootstrap erfolgreich → Startseite", Ct);
            // Das Profil bleibt für den Neustart bestehen; die Folge-
            // Instanz räumt es beim regulären Dispose wieder ab.
            app.Dispose(deleteProfile: false);
        }

        // Neustart desselben Profils: die gespeicherte Sitzung (Anmelde-
        // + Refresh-Token) trägt ohne Anmeldemaske direkt nach Hause.
        using var restarted = await StartAppAsync(profile: profile);
        var model = await restarted.DevApi.WaitForModelAsync(
            m => m.CurrentPage == "Home" && m.ConnectionState == "Connected",
            TimeSpan.FromSeconds(40),
            "Gespeicherte Bootstrap-Sitzung → direkter Einstieg", Ct);
        Assert.Equal(server.BaseAddress, model.ActiveServerAddress);
    }
}
