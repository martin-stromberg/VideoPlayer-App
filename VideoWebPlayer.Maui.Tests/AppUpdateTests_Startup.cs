using VideoPlayer.Mvu;
using VideoPlayer.Storage;

namespace VideoWebPlayer.Maui.Tests;

public class AppUpdateTests_Startup
{
    [Fact]
    public void Update_StartupRequested_SetsCheckingAndLoadsStorageSettingsServers()
    {
        var transition = AppUpdate.Update(new AppModel(), new StartupRequestedMessage());

        Assert.True(transition.Model.IsStartupRunning);
        Assert.Equal(ServerHealthStatus.Checking, transition.Model.ServerHealth);
        Assert.Equal(AppPage.Loading, transition.Model.CurrentPage);
        // Start löst Ablage-Initialisierung, Einstellungs-Laden und das Laden der
        // Serververwaltung aus; der Health-Check folgt auf ServersLoadedMessage.
        Assert.Equal(3, transition.Effects.Count);
        Assert.IsType<InitializeStorageEffect>(transition.Effects[0]);
        Assert.IsType<LoadSettingsEffect>(transition.Effects[1]);
        Assert.IsType<LoadServersEffect>(transition.Effects[2]);
    }

    [Fact]
    public void Update_StartupRequested_WhenAlreadyChecking_NoEffects()
    {
        var model = new AppModel { IsStartupRunning = true, ServerHealth = ServerHealthStatus.Checking };

        var transition = AppUpdate.Update(model, new StartupRequestedMessage());

        Assert.Same(model, transition.Model);
        Assert.Empty(transition.Effects);
    }

    [Fact]
    public void Update_ServersLoaded_WithActiveServer_DuringStartup_EmitsHealthCheck()
    {
        var servers = new List<KnownServer>
        {
            new() { Address = "http://nas/videoplayer/", LastUsedAtUtc = DateTime.UtcNow }
        };
        var model = new AppModel { IsStartupRunning = true, ServerHealth = ServerHealthStatus.Checking };

        var transition = AppUpdate.Update(
            model,
            new ServersLoadedMessage(servers, "http://nas/videoplayer/"));

        Assert.Equal("http://nas/videoplayer/", transition.Model.ActiveServerAddress);
        Assert.Single(transition.Model.KnownServers);
        var effect = Assert.Single(transition.Effects);
        Assert.IsType<CheckServerHealthEffect>(effect);
        // Noch keine Navigation — der Health-Check entscheidet.
        Assert.Equal(AppPage.Loading, transition.Model.CurrentPage);
    }

    [Fact]
    public void Update_ServersLoaded_WithActiveServer_AfterStartup_OnlyUpdatesModel()
    {
        // Nachladen aus der geöffneten Serververwaltung („Server wechseln"):
        // kein erneuter Health-Check, keine Navigation weg von der Seite.
        var model = new AppModel { CurrentPage = AppPage.ServerSetup, IsStartupRunning = false };

        var transition = AppUpdate.Update(
            model,
            new ServersLoadedMessage([], "http://nas/videoplayer/"));

        Assert.Equal("http://nas/videoplayer/", transition.Model.ActiveServerAddress);
        Assert.Empty(transition.Effects);
        Assert.Equal(AppPage.ServerSetup, transition.Model.CurrentPage);
    }

    [Fact]
    public void Update_ServersLoaded_WithoutServer_NavigatesToSetup()
    {
        // Erststart ohne hinterlegten Server → Einrichtungsdialog.
        var model = new AppModel { IsStartupRunning = true, ServerHealth = ServerHealthStatus.Checking };

        var transition = AppUpdate.Update(model, new ServersLoadedMessage([], null));

        Assert.Null(transition.Model.ActiveServerAddress);
        Assert.False(transition.Model.IsStartupRunning);
        Assert.Equal(ServerHealthStatus.Unknown, transition.Model.ServerHealth);
        Assert.Equal(AppPage.ServerSetup, transition.Model.CurrentPage);
        var effect = Assert.Single(transition.Effects);
        var navigate = Assert.IsType<NavigateToPageEffect>(effect);
        Assert.Equal(AppPage.ServerSetup, navigate.Page);
    }

    [Fact]
    public void Update_ServersLoaded_WithoutServer_AlreadyOnSetup_NoNavigation()
    {
        var model = new AppModel { CurrentPage = AppPage.ServerSetup };

        var transition = AppUpdate.Update(model, new ServersLoadedMessage([], null));

        Assert.Empty(transition.Effects);
        Assert.Equal(AppPage.ServerSetup, transition.Model.CurrentPage);
    }

    [Theory]
    [InlineData(true, ServerHealthStatus.Reachable)]
    [InlineData(false, ServerHealthStatus.Unreachable)]
    public void Update_HealthCheckCompleted_NavigatesToHome(bool isReachable, ServerHealthStatus expectedHealth)
    {
        var model = new AppModel { IsStartupRunning = true, ServerHealth = ServerHealthStatus.Checking };

        var transition = AppUpdate.Update(model, new ServerHealthCheckCompletedMessage(isReachable));

        Assert.False(transition.Model.IsStartupRunning);
        Assert.Equal(expectedHealth, transition.Model.ServerHealth);
        Assert.Equal(AppPage.Home, transition.Model.CurrentPage);
        var effect = Assert.Single(transition.Effects);
        var navigate = Assert.IsType<NavigateToPageEffect>(effect);
        Assert.Equal(AppPage.Home, navigate.Page);
    }
}
