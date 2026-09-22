using VideoPlayer.ApiClient.Discovery;
using VideoPlayer.Mvu;
using VideoPlayer.Storage;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>
/// MVU-Übergänge der Serververwaltung: Discovery, manuelle Eingabe,
/// Kandidaten-Prüfung, Übernahme (Einrichtung und „Server wechseln").
/// </summary>
public class AppUpdateTests_ServerManagement
{
    [Fact]
    public void Update_LoadServersRequested_EmitsLoadEffect()
    {
        var transition = AppUpdate.Update(new AppModel(), new LoadServersRequestedMessage());

        var effect = Assert.Single(transition.Effects);
        Assert.IsType<LoadServersEffect>(effect);
    }

    [Fact]
    public void Update_DiscoveryRequested_StartsDiscovery()
    {
        var transition = AppUpdate.Update(new AppModel(), new ServerDiscoveryRequestedMessage());

        Assert.True(transition.Model.IsDiscoveryRunning);
        var effect = Assert.Single(transition.Effects);
        Assert.IsType<DiscoverServersEffect>(effect);
    }

    [Fact]
    public void Update_DiscoveryRequested_WhileRunning_IsIgnored()
    {
        var model = new AppModel { IsDiscoveryRunning = true };

        var transition = AppUpdate.Update(model, new ServerDiscoveryRequestedMessage());

        Assert.Same(model, transition.Model);
        Assert.Empty(transition.Effects);
    }

    [Fact]
    public void Update_DiscoveryCompleted_TakesOverCandidates()
    {
        var found = new List<DiscoveredServer>
        {
            new() { Address = "http://nas:8080/", Source = DiscoverySource.Mdns, Name = "nas" }
        };
        var model = new AppModel { IsDiscoveryRunning = true };

        var transition = AppUpdate.Update(model, new ServerDiscoveryCompletedMessage(found));

        Assert.False(transition.Model.IsDiscoveryRunning);
        Assert.True(transition.Model.DiscoveryAttempted);
        var server = Assert.Single(transition.Model.DiscoveredServers);
        Assert.Equal("http://nas:8080/", server.Address);
        Assert.Empty(transition.Effects);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("keine adresse")]
    [InlineData("ftp://server/")]
    public void Update_AddressSubmitted_Invalid_SetsErrorWithoutEffects(string input)
    {
        var transition = AppUpdate.Update(new AppModel(), new ServerAddressSubmittedMessage(input));

        Assert.Equal(AppUpdate.InvalidAddressError, transition.Model.ServerSetupError);
        Assert.False(transition.Model.IsServerCheckRunning);
        Assert.Empty(transition.Effects);
    }

    [Fact]
    public void Update_AddressSubmitted_Valid_StartsCheckWithNormalizedAddress()
    {
        var transition = AppUpdate.Update(
            new AppModel(),
            new ServerAddressSubmittedMessage("Nas:8080/videoplayer"));

        Assert.True(transition.Model.IsServerCheckRunning);
        Assert.Null(transition.Model.ServerSetupError);
        var effect = Assert.Single(transition.Effects);
        var check = Assert.IsType<CheckCandidateServerEffect>(effect);
        Assert.Equal("http://nas:8080/videoplayer/", check.Address);
    }

    [Fact]
    public void Update_DiscoveredServerSelected_StartsCheck()
    {
        var transition = AppUpdate.Update(
            new AppModel(),
            new DiscoveredServerSelectedMessage("http://nas:8080/", "nas"));

        var check = Assert.IsType<CheckCandidateServerEffect>(Assert.Single(transition.Effects));
        Assert.Equal("http://nas:8080/", check.Address);
        Assert.Equal("nas", check.DisplayName);
        Assert.True(transition.Model.IsServerCheckRunning);
    }

    [Fact]
    public void Update_ServerSwitchRequested_StartsCheck()
    {
        var transition = AppUpdate.Update(
            new AppModel { CurrentPage = AppPage.ServerSetup },
            new ServerSwitchRequestedMessage("http://alt/videoplayer/", "Altserver"));

        var check = Assert.IsType<CheckCandidateServerEffect>(Assert.Single(transition.Effects));
        Assert.Equal("http://alt/videoplayer/", check.Address);
        Assert.Equal("Altserver", check.DisplayName);
    }

    [Fact]
    public void Update_ServerCheckCompleted_Failure_ShowsErrorAndStays()
    {
        var model = new AppModel
        {
            CurrentPage = AppPage.ServerSetup,
            IsServerCheckRunning = true
        };

        var transition = AppUpdate.Update(
            model,
            new ServerCheckCompletedMessage("http://nas/", null, false));

        Assert.False(transition.Model.IsServerCheckRunning);
        Assert.Equal(AppUpdate.UnreachableServerError, transition.Model.ServerSetupError);
        Assert.Null(transition.Model.ActiveServerAddress);
        Assert.Equal(AppPage.ServerSetup, transition.Model.CurrentPage);
        Assert.Empty(transition.Effects);
    }

    [Fact]
    public void Update_ServerCheckCompleted_Success_PersistsAndNavigatesHome()
    {
        var settings = new AppSettings { DownloadQuotaGb = 9 };
        var model = new AppModel
        {
            CurrentPage = AppPage.ServerSetup,
            IsServerCheckRunning = true,
            Settings = settings
        };

        var transition = AppUpdate.Update(
            model,
            new ServerCheckCompletedMessage("http://nas:8080/", "NAS", true));

        Assert.False(transition.Model.IsServerCheckRunning);
        Assert.Null(transition.Model.ServerSetupError);
        Assert.Equal("http://nas:8080/", transition.Model.ActiveServerAddress);
        Assert.Equal(ServerHealthStatus.Reachable, transition.Model.ServerHealth);
        Assert.Equal(AppPage.Home, transition.Model.CurrentPage);

        var server = Assert.Single(transition.Model.KnownServers);
        Assert.Equal("http://nas:8080/", server.Address);
        Assert.Equal("NAS", server.DisplayName);

        // Übrige Einstellungen bleiben beim Serverwechsel unverändert.
        Assert.Equal(settings, transition.Model.Settings);

        Assert.Equal(2, transition.Effects.Count);
        var persist = Assert.IsType<PersistActiveServerEffect>(transition.Effects[0]);
        Assert.Equal("http://nas:8080/", persist.Address);
        Assert.Equal("NAS", persist.DisplayName);
        var navigate = Assert.IsType<NavigateToPageEffect>(transition.Effects[1]);
        Assert.Equal(AppPage.Home, navigate.Page);
    }

    [Fact]
    public void Update_ServerCheckCompleted_Success_ExistingServer_MovesToTop()
    {
        var model = new AppModel
        {
            CurrentPage = AppPage.ServerSetup,
            IsServerCheckRunning = true,
            KnownServers =
            [
                new KnownServer { Address = "http://neu/", LastUsedAtUtc = DateTime.UtcNow },
                new KnownServer { Address = "http://alt/", DisplayName = "Alt", LastUsedAtUtc = DateTime.UtcNow.AddDays(-1) }
            ]
        };

        var transition = AppUpdate.Update(
            model,
            new ServerCheckCompletedMessage("http://alt/", "Alt", true));

        // Kein Duplikat; der gewechselte Server steht vorn.
        Assert.Equal(2, transition.Model.KnownServers.Count);
        Assert.Equal("http://alt/", transition.Model.KnownServers[0].Address);
        Assert.Equal("http://neu/", transition.Model.KnownServers[1].Address);
    }
}
