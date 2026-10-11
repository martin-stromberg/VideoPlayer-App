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
    /// <param name="profile">Expliziter Profilname für einen Neustart mit
    /// bestehender Ablage; <c>null</c> = frisches Testprofil.</param>
    /// <returns>Die laufende App-Instanz.</returns>
    protected async Task<AppInstance> StartAppAsync(
        [CallerMemberName] string testName = "", string? profile = null)
    {
        var exePath = await fixture.EnsureBuiltAsync();
        return await AppInstance.StartAsync(
            exePath, testName, cancellationToken: Ct, profile: profile);
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
        // Nach dem Pairing bleibt der Fluss auf der Einrichtungsseite —
        // die Zugangsdaten-Kachel erscheint dort als zweite Stufe.
        await app.DevApi.WaitForModelAsync(
            m => m.ConnectionState == "NeedsLogin",
            TimeSpan.FromSeconds(30),
            "Pairing erfolgreich → Anmelde-Wahl",
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

    /// <summary>
    /// Führt den Einrichtungsfluss bis zur befüllten Startseite aus
    /// (Weiterschauen-Eintrag sichtbar) — gemeinsamer Einstieg der
    /// Weiterschauen-Szenarien.
    /// </summary>
    /// <param name="app">Die laufende App-Instanz.</param>
    /// <param name="server">Der Mock-Server.</param>
    /// <param name="expectedTitle">Titel des erwarteten Weiterschauen-Eintrags (Diagnose).</param>
    /// <returns>Eine abgeschlossene Aufgabe nach Erreichen der Startseite.</returns>
    protected static async Task LoginOnHomeAsync(AppInstance app, MockVideoServer server, string expectedTitle)
    {
        await ConnectToPairingRequiredAsync(app, server);
        await PairAsync(app, server);
        await LoginAsync(app, server);

        await app.DevApi.WaitForModelAsync(
            m => m.ContinueWatchingCount > 0,
            TimeSpan.FromSeconds(30),
            $"Karussell „Weiterschauen\" befüllt (erwartet „{expectedTitle}\")", Ct);
    }

    /// <summary>Ermittelt den Index eines Weiterschauen-Eintrags anhand seines Titels im Karussell-Tree.</summary>
    /// <param name="app">Die laufende App-Instanz.</param>
    /// <param name="title">Der gesuchte Eintragstitel.</param>
    /// <returns>Der Index des Eintrags im Karussell.</returns>
    protected static async Task<int> ContinueWatchingIndexAsync(AppInstance app, string title)
    {
        var tree = await app.DevApi.GetTreeAsync(Ct);
        var carousel = tree.FindByAutomationId("ContinueWatchingCarousel");
        Assert.NotNull(carousel);
        Assert.NotNull(carousel.Items);
        var index = carousel.Items.FindIndex(i => i.Text == title);
        Assert.True(index >= 0,
            $"'{title}' fehlt im Karussell: " +
            string.Join(", ", carousel.Items.Select(i => i.Text)));
        return index;
    }
}
