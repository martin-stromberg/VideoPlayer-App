using VideoPlayer.Mvu;
using VideoPlayer.Storage;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>
/// Tests für <c>AppUpdate.Update</c> – Bereich QR-/Ticket-Onboarding:
/// Kopplungs-Eingabe (Ticket-Adresse und Kurzcode), Serverprüfung vor dem
/// Austausch und das Ergebnis des Bootstrap-Handshakes.
/// </summary>
public class AppUpdateTests_Bootstrap
{
    private static AppModel SetupModel() => new()
    {
        CurrentPage = AppPage.ServerSetup,
        ConnectionState = ConnectionState.NeedsServerSetup,
        ActiveServerAddress = "http://nas/videoplayer/",
        ServerHealth = ServerHealthStatus.Reachable
    };

    /// <summary>Prüft Update: Ticket-Adresse → Serverprüfung-Effekt mit Basisadresse und Ticket.</summary>
    [Fact]
    public void Update_BootstrapTicketSubmitted_Url_StartsServerCheck()
    {
        var transition = AppUpdate.Update(
            SetupModel() with { BootstrapError = "alt" },
            new BootstrapTicketSubmittedMessage("http://srv.local:8096/videoplayer/pairing?t=ticket-1"));

        Assert.True(transition.Model.IsBootstrapRunning);
        Assert.Null(transition.Model.BootstrapError);
        var effect = Assert.IsType<CheckBootstrapServerEffect>(Assert.Single(transition.Effects));
        Assert.Equal("http://srv.local:8096/videoplayer/", effect.Address);
        Assert.Equal("ticket-1", effect.Ticket);
    }

    /// <summary>Prüft Update: Kurzcode bei verbundenem Server → direkter Austausch-Effekt.</summary>
    [Fact]
    public void Update_BootstrapTicketSubmitted_ShortCode_StartsExchangeOnActiveServer()
    {
        var transition = AppUpdate.Update(
            SetupModel(),
            new BootstrapTicketSubmittedMessage("A1B2C3D4"));

        Assert.True(transition.Model.IsBootstrapRunning);
        var effect = Assert.IsType<ExchangeBootstrapTicketEffect>(Assert.Single(transition.Effects));
        Assert.Equal("http://nas/videoplayer/", effect.ServerAddress);
        Assert.Equal("A1B2C3D4", effect.Ticket);
    }

    /// <summary>Prüft Update: Kurzcode ohne verbundenen Server → verständlicher Hinweis ohne Effekt.</summary>
    [Fact]
    public void Update_BootstrapTicketSubmitted_ShortCodeWithoutServer_ShowsNeedsServerError()
    {
        var model = new AppModel
        {
            CurrentPage = AppPage.ServerSetup,
            ConnectionState = ConnectionState.NeedsServerSetup,
            ActiveServerAddress = null
        };

        var transition = AppUpdate.Update(model, new BootstrapTicketSubmittedMessage("A1B2C3D4"));

        Assert.Equal(AppUpdate.BootstrapNeedsServerError, transition.Model.BootstrapError);
        Assert.False(transition.Model.IsBootstrapRunning);
        Assert.Empty(transition.Effects);
    }

    /// <summary>Prüft Update: nicht lesbare Eingabe → Eingabefehler ohne Effekt.</summary>
    /// <param name="input">Die ungültige Roheingabe.</param>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ftp://nas/pairing?t=x")]
    [InlineData("https://nas/pairing")] // fehlendes t
    public void Update_BootstrapTicketSubmitted_InvalidInput_SetsInputError(string input)
    {
        var transition = AppUpdate.Update(SetupModel(), new BootstrapTicketSubmittedMessage(input));

        Assert.Equal(AppUpdate.InvalidBootstrapInputError, transition.Model.BootstrapError);
        Assert.False(transition.Model.IsBootstrapRunning);
        Assert.Empty(transition.Effects);
    }

    /// <summary>Prüft Update: Submit bei laufendem Bootstrap → wird ignoriert (Doppel-Submit-Guard).</summary>
    [Fact]
    public void Update_BootstrapTicketSubmitted_WhileRunning_IsIgnored()
    {
        var model = SetupModel() with { IsBootstrapRunning = true };

        var transition = AppUpdate.Update(model, new BootstrapTicketSubmittedMessage("A1B2C3D4"));

        Assert.Same(model, transition.Model);
        Assert.Empty(transition.Effects);
    }

    /// <summary>Prüft Update: erreichbarer Ticket-Server → übernehmen + Austausch (Persist vor Exchange).</summary>
    [Fact]
    public void Update_BootstrapServerChecked_Success_PersistsThenExchanges()
    {
        var model = SetupModel() with
        {
            IsBootstrapRunning = true,
            KnownServers =
            [
                new KnownServer { Address = "http://alt/", DisplayName = "Alt" }
            ]
        };

        var transition = AppUpdate.Update(
            model,
            new BootstrapServerCheckedMessage("http://srv/videoplayer/", true, "ticket-1"));

        Assert.True(transition.Model.IsBootstrapRunning);
        Assert.Equal("http://srv/videoplayer/", transition.Model.ActiveServerAddress);
        Assert.Equal(ServerHealthStatus.Reachable, transition.Model.ServerHealth);
        // Der neue Server steht vorne in der bekannten Liste.
        Assert.Equal("http://srv/videoplayer/", transition.Model.KnownServers[0].Address);
        Assert.Equal(2, transition.Model.KnownServers.Count);
        // Reihenfolge: erst persistieren (Gate-Provider laden), dann einlösen.
        Assert.Equal(2, transition.Effects.Count);
        var persist = Assert.IsType<PersistActiveServerEffect>(transition.Effects[0]);
        Assert.Equal("http://srv/videoplayer/", persist.Address);
        var exchange = Assert.IsType<ExchangeBootstrapTicketEffect>(transition.Effects[1]);
        Assert.Equal("http://srv/videoplayer/", exchange.ServerAddress);
        Assert.Equal("ticket-1", exchange.Ticket);
    }

    /// <summary>Prüft Update: nicht erreichbarer Ticket-Server → Inline-Fehler, Server nicht übernommen.</summary>
    [Fact]
    public void Update_BootstrapServerChecked_Unreachable_ShowsError()
    {
        var model = SetupModel() with { IsBootstrapRunning = true };

        var transition = AppUpdate.Update(
            model,
            new BootstrapServerCheckedMessage("http://srv/", false, "ticket-1"));

        Assert.False(transition.Model.IsBootstrapRunning);
        Assert.Equal(AppUpdate.BootstrapServerUnreachableError, transition.Model.BootstrapError);
        Assert.Equal("http://nas/videoplayer/", transition.Model.ActiveServerAddress);
        Assert.Empty(transition.Effects);
    }

    /// <summary>Prüft Update: verspätetes Prüfergebnis (nicht mehr laufend) → ignoriert.</summary>
    [Fact]
    public void Update_BootstrapServerChecked_StaleResult_IsIgnored()
    {
        var model = SetupModel() with { IsBootstrapRunning = false };

        var transition = AppUpdate.Update(
            model,
            new BootstrapServerCheckedMessage("http://srv/", true, "ticket-1"));

        Assert.Same(model, transition.Model);
        Assert.Empty(transition.Effects);
    }

    /// <summary>Prüft Update: erfolgreicher Bootstrap → Pairing-Zwang aufgehoben, AutoLogin → Home.</summary>
    [Fact]
    public void Update_BootstrapCompleted_Succeeded_ContinuesAutoLogin()
    {
        var model = SetupModel() with { IsBootstrapRunning = true, IsPairingRequired = true };

        var transition = AppUpdate.Update(model, new BootstrapCompletedMessage(BootstrapStatus.Succeeded));

        Assert.False(transition.Model.IsBootstrapRunning);
        Assert.False(transition.Model.IsPairingRequired);
        Assert.Null(transition.Model.BootstrapError);
        Assert.Equal(ConnectionState.Connecting, transition.Model.ConnectionState);
        var effect = Assert.IsType<AttemptAutoLoginEffect>(Assert.Single(transition.Effects));
        Assert.Equal("http://nas/videoplayer/", effect.ServerAddress);
    }

    /// <summary>Prüft Update: Bootstrap-Fehler → je Status der erwartete Inline-Fehler.</summary>
    /// <param name="status">Der gemeldete Bootstrap-Status.</param>
    /// <param name="expectedError">Der erwartete Fehlertext.</param>
    [Theory]
    [InlineData(BootstrapStatus.InvalidTicket, AppUpdate.InvalidBootstrapTicketError)]
    [InlineData(BootstrapStatus.InvalidRequest, AppUpdate.InvalidBootstrapTicketError)]
    [InlineData(BootstrapStatus.RateLimited, AppUpdate.BootstrapRateLimitedError)]
    [InlineData(BootstrapStatus.NotSupported, AppUpdate.BootstrapNotSupportedError)]
    [InlineData(BootstrapStatus.ServerUnreachable, AppUpdate.BootstrapServerUnreachableError)]
    [InlineData(BootstrapStatus.Failed, AppUpdate.BootstrapFailedError)]
    public void Update_BootstrapCompleted_MapsErrors(BootstrapStatus status, string expectedError)
    {
        var model = SetupModel() with { IsBootstrapRunning = true };

        var transition = AppUpdate.Update(model, new BootstrapCompletedMessage(status));

        Assert.False(transition.Model.IsBootstrapRunning);
        Assert.Equal(expectedError, transition.Model.BootstrapError);
        Assert.Empty(transition.Effects);
    }

    /// <summary>Prüft Update: verspätetes Bootstrap-Ergebnis (nicht mehr laufend) → ignoriert.</summary>
    [Fact]
    public void Update_BootstrapCompleted_StaleResult_IsIgnored()
    {
        var model = SetupModel() with { IsBootstrapRunning = false };

        var transition = AppUpdate.Update(model, new BootstrapCompletedMessage(BootstrapStatus.Succeeded));

        Assert.Same(model, transition.Model);
        Assert.Empty(transition.Effects);
    }

    /// <summary>Prüft Update: neuer Servercheck räumt den Bootstrap-Kontext (laufend + Fehler).</summary>
    [Fact]
    public void Update_ServerAddressSubmitted_ResetsBootstrapState()
    {
        var model = SetupModel() with { IsBootstrapRunning = true, BootstrapError = "alt" };

        var transition = AppUpdate.Update(model, new ServerAddressSubmittedMessage("http://neu/"));

        Assert.False(transition.Model.IsBootstrapRunning);
        Assert.Null(transition.Model.BootstrapError);
    }
}
