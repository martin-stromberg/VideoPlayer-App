using VideoPlayer.E2E.Tests.Support;

namespace VideoPlayer.E2E.Tests.Tests;

/// <summary>
/// E2E-Szenario „Anmeldung": nach erfolgreichem Pairing steht die
/// Anmeldemaske; falsche Zugangsdaten zeigen den Inline-Fehler,
/// richtige führen auf die Startseite.
/// </summary>
public sealed class LoginTests : E2ETestBase
{
    /// <summary>Initialisiert die Testklasse.</summary>
    /// <param name="fixture">Geteilte Build-Fixture.</param>
    public LoginTests(AppFixture fixture) : base(fixture) { }

    /// <summary>
    /// Abgelehnte Zugangsdaten: der Server antwortet 401 (Credential-
    /// Fehler, kein Gate-Problem), die App zeigt den Anmeldefehler.
    /// </summary>
    [Fact]
    public async Task WrongPassword_ShowsLoginError()
    {
        using var server = new MockVideoServer("E2ETEST", "e2e-device-a", "e2e@example.test", "e2e-pass");
        using var app = await StartAppAsync();
        await ConnectToPairingRequiredAsync(app, server);
        await PairAsync(app, server);

        app.Ui.EnterText("LoginUsernameEntry", server.Username);
        app.Ui.EnterText("LoginPasswordEntry", "falsches-passwort");
        app.Ui.ClickButton("LoginButton");

        var model = await app.DevApi.WaitForModelAsync(
            m => m.LoginError is not null && !m.IsLoginRunning,
            TimeSpan.FromSeconds(30),
            "Anmeldung abgelehnt → Fehlermeldung", Ct);
        Assert.Contains("Anmeldung fehlgeschlagen", model.LoginError);
        Assert.Equal("Login", model.CurrentPage);
    }

    /// <summary>
    /// Gültige Zugangsdaten: die Anmeldung liefert das Token und die
    /// App wechselt verbunden auf die Startseite.
    /// </summary>
    [Fact]
    public async Task ValidLogin_ShowsHomePage()
    {
        using var server = new MockVideoServer("E2ETEST", "e2e-device-a", "e2e@example.test", "e2e-pass");
        using var app = await StartAppAsync();
        await ConnectToPairingRequiredAsync(app, server);
        await PairAsync(app, server);
        await LoginAsync(app, server);

        var model = await app.DevApi.WaitForModelAsync(
            m => !m.IsSyncRunning,
            TimeSpan.FromSeconds(40),
            "Erste Synchronisation abgeschlossen", Ct);
        Assert.Equal("Home", model.CurrentPage);
        Assert.Equal("Connected", model.ConnectionState);
        Assert.Contains(model.KnownServers, s => s.Address == server.BaseAddress);
        Assert.Equal(1, server.LoginCount);
    }
}
