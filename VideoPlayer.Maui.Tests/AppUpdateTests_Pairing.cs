using VideoPlayer.Mvu;

namespace VideoPlayer.Maui.Tests;

/// <summary>
/// Tests für <c>AppUpdate.Update</c> – Bereich Geräte-Pairing:
/// Code-Eingabe, Austausch-Ergebnis und Re-Pairing nach Widerruf.
/// </summary>
public class AppUpdateTests_Pairing
{
    private static AppModel PairingModel() => new()
    {
        CurrentPage = AppPage.ServerSetup,
        ConnectionState = ConnectionState.NeedsServerSetup,
        ActiveServerAddress = "http://nas/videoplayer/",
        ServerHealth = ServerHealthStatus.Reachable,
        IsPairingRequired = true
    };

    /// <summary>Prüft Update: ungültiger Pairing-Code → Format-Hinweis ohne Effekt.</summary>
    /// <param name="code">Die ungültige Roheingabe.</param>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ABC")] // unter der Mindestlänge (4)
    [InlineData("ABCD234I")] // I gehört nicht zum Alphabet
    [InlineData("ABCD2340")] // 0 gehört nicht zum Alphabet
    public void Update_PairingCodeSubmitted_Invalid_SetsFormatErrorWithoutEffects(string code)
    {
        var transition = AppUpdate.Update(PairingModel(), new PairingCodeSubmittedMessage(code));

        // Die lokale Formatprüfung nennt die eigentliche Ursache
        // (Eingabe unvollständig/falsch formatiert) — nicht „abgelaufen".
        Assert.Equal(AppUpdate.PairingCodeFormatError, transition.Model.PairingError);
        Assert.False(transition.Model.IsPairingRunning);
        Assert.Empty(transition.Effects);
    }

    /// <summary>Prüft Update: Pairing-Submit bei laufendem Austausch → wird ignoriert (Doppel-Submit-Guard).</summary>
    [Fact]
    public void Update_PairingCodeSubmitted_WhileRunning_IsIgnored()
    {
        var model = PairingModel() with { IsPairingRunning = true };

        var transition = AppUpdate.Update(model, new PairingCodeSubmittedMessage("ABCD2345"));

        Assert.Same(model, transition.Model);
        Assert.Empty(transition.Effects);
    }

    /// <summary>Prüft Update: gültiger Code → startet den Austausch-Effekt mit normalisiertem Code.</summary>
    [Fact]
    public void Update_PairingCodeSubmitted_Valid_StartsExchange()
    {
        var transition = AppUpdate.Update(
            PairingModel() with { PairingError = "alt" },
            new PairingCodeSubmittedMessage(" abcd2345 "));

        Assert.True(transition.Model.IsPairingRunning);
        Assert.Null(transition.Model.PairingError);
        var effect = Assert.IsType<ExchangePairingCodeEffect>(Assert.Single(transition.Effects));
        Assert.Equal("http://nas/videoplayer/", effect.ServerAddress);
        Assert.Equal("ABCD2345", effect.Code);
    }

    /// <summary>
    /// Prüft Update: ein Code mit serverseitig konfigurierter Länge (≠ 8,
    /// aber gültigem Alphabet und ≥ Mindestlänge) startet den Austausch —
    /// die lokale Prüfung darf die Server-Konfiguration nicht überstimmen.
    /// </summary>
    /// <param name="code">Der gültige Code in abweichender Länge.</param>
    [Theory]
    [InlineData("ABCD")]   // Server-Minimum
    [InlineData("ABC234")] // 6 Stellen
    [InlineData("ABCD23456789")] // 12 Stellen
    public void Update_PairingCodeSubmitted_ServerConfiguredLength_StartsExchange(string code)
    {
        var transition = AppUpdate.Update(PairingModel(), new PairingCodeSubmittedMessage(code));

        Assert.True(transition.Model.IsPairingRunning);
        Assert.Null(transition.Model.PairingError);
        var effect = Assert.IsType<ExchangePairingCodeEffect>(Assert.Single(transition.Effects));
        Assert.Equal(code, effect.Code);
    }

    /// <summary>Prüft Update: gültiger Code ohne aktiven Server → defensiv zurück zur Einrichtung.</summary>
    [Fact]
    public void Update_PairingCodeSubmitted_WithoutActiveServer_GoesToSetup()
    {
        var model = new AppModel { ActiveServerAddress = null, CurrentPage = AppPage.Login };

        var transition = AppUpdate.Update(model, new PairingCodeSubmittedMessage("ABCD2345"));

        Assert.Equal(AppPage.ServerSetup, transition.Model.CurrentPage);
        Assert.Equal(ConnectionState.NeedsServerSetup, transition.Model.ConnectionState);
        var navigate = Assert.IsType<NavigateToPageEffect>(Assert.Single(transition.Effects));
        Assert.Equal(AppPage.ServerSetup, navigate.Page);
    }

    /// <summary>Prüft Update: erfolgreiches Pairing → Flags zurückgesetzt, AutoLogin läuft an.</summary>
    [Fact]
    public void Update_PairingCompleted_Succeeded_ContinuesAutoLogin()
    {
        var model = PairingModel() with { IsPairingRunning = true };

        var transition = AppUpdate.Update(model, new PairingCompletedMessage(PairingStatus.Succeeded));

        Assert.False(transition.Model.IsPairingRunning);
        Assert.False(transition.Model.IsPairingRequired);
        Assert.Null(transition.Model.PairingError);
        Assert.Equal(ConnectionState.Connecting, transition.Model.ConnectionState);
        var effect = Assert.IsType<AttemptAutoLoginEffect>(Assert.Single(transition.Effects));
        Assert.Equal("http://nas/videoplayer/", effect.ServerAddress);
    }

    /// <summary>Prüft Update: Pairing-Fehler → je Status der erwartete Inline-Fehler.</summary>
    /// <param name="status">Der gemeldete Pairing-Status.</param>
    /// <param name="expectedError">Der erwartete Fehlertext.</param>
    [Theory]
    [InlineData(PairingStatus.InvalidCode, AppUpdate.InvalidPairingCodeError)]
    [InlineData(PairingStatus.RateLimited, AppUpdate.PairingRateLimitedError)]
    [InlineData(PairingStatus.InvalidRequest, AppUpdate.PairingFailedError)]
    [InlineData(PairingStatus.NotSupported, AppUpdate.PairingNotSupportedError)]
    [InlineData(PairingStatus.ServerUnreachable, AppUpdate.LoginUnreachableError)]
    [InlineData(PairingStatus.Failed, AppUpdate.PairingFailedError)]
    public void Update_PairingCompleted_MapsErrors(PairingStatus status, string expectedError)
    {
        var model = PairingModel() with { IsPairingRunning = true };

        var transition = AppUpdate.Update(model, new PairingCompletedMessage(status));

        Assert.False(transition.Model.IsPairingRunning);
        Assert.True(transition.Model.IsPairingRequired);
        Assert.Equal(expectedError, transition.Model.PairingError);
        Assert.Empty(transition.Effects);
    }

    /// <summary>
    /// Prüft Update: ein Server ohne Pairing-Endpunkt zeigt den Hinweis —
    /// bewusst ohne Fallback auf einen statischen Token (kein AutoLogin).
    /// </summary>
    [Fact]
    public void Update_PairingCompleted_NotSupported_ShowsError_WithoutFallback()
    {
        var model = PairingModel() with { IsPairingRunning = true };

        var transition = AppUpdate.Update(model, new PairingCompletedMessage(PairingStatus.NotSupported));

        Assert.Equal(AppUpdate.PairingNotSupportedError, transition.Model.PairingError);
        Assert.True(transition.Model.IsPairingRequired);
        Assert.Equal(AppPage.ServerSetup, transition.Model.CurrentPage);
        Assert.DoesNotContain(transition.Effects, e => e is AttemptAutoLoginEffect);
    }

    /// <summary>Prüft Update: verspätetes Pairing-Ergebnis (nicht mehr laufend) → wird ignoriert.</summary>
    [Fact]
    public void Update_PairingCompleted_StaleResult_WhenNotRunning_IsIgnored()
    {
        var model = PairingModel() with { IsPairingRunning = false };

        var transition = AppUpdate.Update(model, new PairingCompletedMessage(PairingStatus.Succeeded));

        Assert.Same(model, transition.Model);
        Assert.Empty(transition.Effects);
    }

    /// <summary>Prüft Update: widerrufenes Geräte-Token → Re-Pairing-Eingabe mit Hinweis.</summary>
    [Fact]
    public void Update_DeviceTokenRevoked_NavigatesToSetupWithError()
    {
        var model = new AppModel
        {
            CurrentPage = AppPage.Home,
            ConnectionState = ConnectionState.Connected,
            ActiveServerAddress = "http://nas/videoplayer/",
            SourcesOverview = new SourcesOverviewState { Attempted = true },
            HomeHero = new HomeHeroState()
        };

        var transition = AppUpdate.Update(model, new DeviceTokenRevokedMessage());

        Assert.False(transition.Model.IsStartupRunning);
        Assert.False(transition.Model.IsServerCheckRunning);
        Assert.True(transition.Model.IsPairingRequired);
        Assert.False(transition.Model.IsPairingRunning);
        Assert.Equal(AppUpdate.DeviceRevokedError, transition.Model.PairingError);
        Assert.Equal(ServerHealthStatus.Reachable, transition.Model.ServerHealth);
        Assert.Equal(ConnectionState.NeedsServerSetup, transition.Model.ConnectionState);
        Assert.Equal(AppPage.ServerSetup, transition.Model.CurrentPage);
        // Sitzungsgebundener Quellen-/Browsing-Zustand wird zurückgesetzt.
        Assert.Same(SourcesOverviewState.Initial, transition.Model.SourcesOverview);
        Assert.Same(SourceBrowserState.Empty, transition.Model.SourceBrowser);
        Assert.Same(HomeHeroState.Empty, transition.Model.HomeHero);
        Assert.Equal(2, transition.Effects.Count);
        Assert.IsType<DisconnectServerEventsEffect>(transition.Effects[0]);
        var navigate = Assert.IsType<NavigateToPageEffect>(transition.Effects[1]);
        Assert.Equal(AppPage.ServerSetup, navigate.Page);
    }

    /// <summary>Prüft Update: widerrufenes Geräte-Token auf der Einrichtungsseite → kein Navigationseffekt.</summary>
    [Fact]
    public void Update_DeviceTokenRevoked_OnSetupPage_SkipsNavigation()
    {
        var model = new AppModel
        {
            CurrentPage = AppPage.ServerSetup,
            ConnectionState = ConnectionState.Connected,
            ActiveServerAddress = "http://nas/videoplayer/"
        };

        var transition = AppUpdate.Update(model, new DeviceTokenRevokedMessage());

        Assert.True(transition.Model.IsPairingRequired);
        Assert.Equal(AppUpdate.DeviceRevokedError, transition.Model.PairingError);
        var effect = Assert.IsType<DisconnectServerEventsEffect>(Assert.Single(transition.Effects));
        Assert.NotNull(effect);
    }

    /// <summary>Prüft Update: doppelte Widerrufs-Meldung im Pairing-Zustand → ignoriert.</summary>
    [Fact]
    public void Update_DeviceTokenRevoked_AlreadyInPairingState_IsIgnored()
    {
        var model = PairingModel() with { PairingError = AppUpdate.DeviceRevokedError };

        var transition = AppUpdate.Update(model, new DeviceTokenRevokedMessage());

        Assert.Same(model, transition.Model);
        Assert.Empty(transition.Effects);
    }
}
