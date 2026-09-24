using System.Runtime.CompilerServices;
using VideoPlayer.E2E.Tests.Support;

namespace VideoPlayer.E2E.Tests.Tests;

/// <summary>
/// Basisklasse der E2E-Tests: hält die <see cref="AppFixture"/> (einmaliger
/// Debug-Build) und stellt <see cref="StartAppAsync"/> für den
/// Prozessstart mit isoliertem Profil bereit. Alle Tests laufen in der
/// sequentiellen <c>E2E</c>-Collection.
/// </summary>
[Collection("E2E")]
public abstract class E2ETestBase
{
    private readonly AppFixture fixture;

    /// <summary>Initialisiert die Basisklasse.</summary>
    /// <param name="fixture">Geteilte Build-Fixture der Collection.</param>
    protected E2ETestBase(AppFixture fixture)
    {
        this.fixture = fixture;
    }

    /// <summary>Abbruchtoken des laufenden Tests (xUnit-Kontext).</summary>
    protected static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>
    /// Baut die App (einmalig) und startet sie mit einem eigenen Profil.
    /// </summary>
    /// <param name="testName">Testname (Teil des Profilnamens; Standard: Aufrufername).</param>
    /// <returns>Die laufende App-Instanz.</returns>
    protected async Task<AppInstance> StartAppAsync([CallerMemberName] string testName = "")
    {
        var exePath = await fixture.EnsureBuiltAsync();
        return await AppInstance.StartAsync(exePath, testName, cancellationToken: Ct);
    }

    /// <summary>
    /// Führt den Setup-Fluss bis zum Pairing-Zustand aus:
    /// ServerSetup-Seite → Adresse eingeben → Verbinden →
    /// <c>isPairingRequired</c>.
    /// </summary>
    /// <param name="app">Die laufende App-Instanz.</param>
    /// <param name="server">Der Mock-Server.</param>
    protected static async Task ConnectToPairingRequiredAsync(AppInstance app, MockVideoServer server)
    {
        await app.DevApi.WaitForModelAsync(
            m => m.CurrentPage == "ServerSetup",
            description: "Erststart landet auf der Server-Einrichtung",
            cancellationToken: Ct);
        app.Ui.EnterText("AddressEntry", server.BaseAddress);
        app.Ui.ClickButton("ConnectButton");
        var model = await app.DevApi.WaitForModelAsync(
            m => m.IsPairingRequired,
            TimeSpan.FromSeconds(30),
            "Server erreichbar und Pairing erforderlich",
            Ct);
        Assert.Equal(server.BaseAddress, model.ActiveServerAddress);
    }

    /// <summary>
    /// Löst das Pairing mit dem gültigen Code des Servers aus und wartet
    /// auf die Anmeldemaske.
    /// </summary>
    /// <param name="app">Die laufende App-Instanz.</param>
    /// <param name="server">Der Mock-Server.</param>
    protected static async Task PairAsync(AppInstance app, MockVideoServer server)
    {
        app.Ui.EnterText("PairingCodeEntry", server.PairingCode);
        app.Ui.ClickButton("PairButton");
        await app.DevApi.WaitForModelAsync(
            m => m.CurrentPage == "Login",
            TimeSpan.FromSeconds(30),
            "Pairing erfolgreich → Anmeldemaske",
            Ct);
    }

    /// <summary>
    /// Meldet mit den gültigen Zugangsdaten des Servers an und wartet
    /// auf die Startseite.
    /// </summary>
    /// <param name="app">Die laufende App-Instanz.</param>
    /// <param name="server">Der Mock-Server.</param>
    protected static async Task LoginAsync(AppInstance app, MockVideoServer server)
    {
        app.Ui.EnterText("LoginUsernameEntry", server.Username);
        app.Ui.EnterText("LoginPasswordEntry", server.Password);
        app.Ui.ClickButton("LoginButton");
        DevApiModel model;
        try
        {
            model = await app.DevApi.WaitForModelAsync(
                m => m.CurrentPage == "Home" && m.ConnectionState == "Connected",
                TimeSpan.FromSeconds(40),
                "Anmeldung erfolgreich → Startseite",
                Ct);
        }
        catch (TimeoutException ex)
        {
            throw new TimeoutException(
                ex.Message + $" | Login-Requests des Mock-Servers: [{string.Join(" | ", server.LoginRequestBodies)}] (Gate: {server.LastLoginGateKey ?? "-"})");
        }
        Assert.Equal(server.BaseAddress, model.ActiveServerAddress);
    }
}
