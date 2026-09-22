using VideoPlayer.Mvu;

namespace VideoWebPlayer.Maui.Tests;

public class AppUpdateTests_Startup
{
    [Fact]
    public void Update_StartupRequested_SetsCheckingAndEmitsHealthCheck()
    {
        var transition = AppUpdate.Update(new AppModel(), new StartupRequestedMessage());

        Assert.True(transition.Model.IsStartupRunning);
        Assert.Equal(ServerHealthStatus.Checking, transition.Model.ServerHealth);
        Assert.Equal(AppPage.Loading, transition.Model.CurrentPage);
        Assert.Single(transition.Effects);
        Assert.IsType<CheckServerHealthEffect>(transition.Effects[0]);
    }

    [Fact]
    public void Update_StartupRequested_WhenAlreadyChecking_NoEffects()
    {
        var model = new AppModel { IsStartupRunning = true, ServerHealth = ServerHealthStatus.Checking };

        var transition = AppUpdate.Update(model, new StartupRequestedMessage());

        Assert.Same(model, transition.Model);
        Assert.Empty(transition.Effects);
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
