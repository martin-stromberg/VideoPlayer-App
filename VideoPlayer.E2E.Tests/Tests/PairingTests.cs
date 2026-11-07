using VideoPlayer.E2E.Tests.Support;

namespace VideoPlayer.E2E.Tests.Tests;

/// <summary>
/// E2E-Szenario „Pairing-Flow": die Pairing-Eingabe auf der
/// Einrichtungsseite gegen den Mock-Server — Formatfehler (lokal),
/// falscher Code (Server-401) und der erfolgreiche Austausch bis zur
/// Anmeldemaske.
/// </summary>
public sealed class PairingTests : E2ETestBase
{
    /// <summary>Initialisiert die Testklasse.</summary>
    /// <param name="fixture">Geteilte Build-Fixture.</param>
    public PairingTests(AppFixture fixture) : base(fixture) { }

    /// <summary>
    /// Formal ungültiger Code (Zeichen außerhalb des Pairing-Alphabets):
    /// lokale Formatprüfung ohne Server-Kontakt.
    /// </summary>
    [Fact]
    public async Task MalformedCode_ShowsFormatError()
    {
        using var server = new MockVideoServer("E2ETEST", "e2e-device-a", "e2e@example.test", "e2e-pass");
        using var app = await StartAppAsync();
        await ConnectToPairingRequiredAsync(app, server);

        // „O" und „I" gehören nicht zum Pairing-Alphabet.
        app.Ui.EnterText("PairingCodeEntry", "OIOI");
        app.Ui.ClickButton("PairButton");

        var model = await app.DevApi.WaitForModelAsync(
            m => m.PairingError is not null,
            description: "Formatfehler der Pairing-Eingabe", cancellationToken: Ct);
        Assert.Contains("nicht vollständig", model.PairingError);
        Assert.True(model.IsPairingRequired);
        Assert.Equal(0, server.PairingExchangeCount);
    }

    /// <summary>
    /// Wohlgeformter, aber falscher Code: der Server lehnt den
    /// Austausch ab (401), die App zeigt den Inline-Fehler und bleibt
    /// im Pairing-Zustand.
    /// </summary>
    [Fact]
    public async Task WrongCode_ShowsPairingError()
    {
        using var server = new MockVideoServer("E2ETEST", "e2e-device-a", "e2e@example.test", "e2e-pass");
        using var app = await StartAppAsync();
        await ConnectToPairingRequiredAsync(app, server);

        app.Ui.EnterText("PairingCodeEntry", "BADX");
        app.Ui.ClickButton("PairButton");

        var model = await app.DevApi.WaitForModelAsync(
            m => m.PairingError is not null && !m.IsPairingRunning,
            TimeSpan.FromSeconds(30),
            "Server lehnt den Code ab → Pairing-Fehler", Ct);
        Assert.Contains("ungültig", model.PairingError);
        Assert.True(model.IsPairingRequired);
        Assert.Equal(1, server.PairingExchangeCount);
    }

    /// <summary>
    /// Gültiger Code: der ECDH/AES-GCM-Austausch liefert das
    /// Geräte-Token; ohne gespeicherte Zugangsdaten zeigt die
    /// Einrichtungsseite die Anmelde-Kachel als zweite Stufe.
    /// </summary>
    [Fact]
    public async Task ValidCode_ShowsCredentialsOption()
    {
        using var server = new MockVideoServer("E2ETEST", "e2e-device-a", "e2e@example.test", "e2e-pass");
        using var app = await StartAppAsync();
        await ConnectToPairingRequiredAsync(app, server);

        await PairAsync(app, server);

        var model = await app.DevApi.GetModelAsync(Ct);
        Assert.Equal("NeedsLogin", model.ConnectionState);
        Assert.False(model.IsPairingRequired);
        Assert.Null(model.PairingError);
        Assert.Equal(1, server.PairingExchangeCount);
        Assert.True(app.Ui.Exists("LoginUsernameEntry"));
        Assert.True(app.Ui.Exists("LoginPasswordEntry"));
        Assert.True(app.Ui.Exists("LoginButton"));
    }
}
