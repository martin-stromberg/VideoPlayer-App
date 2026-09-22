using VideoPlayer.Mvu;

namespace VideoWebPlayer.Maui.Tests;

public class MvuHostTests
{
    private sealed class RecordingEffectHandler : IAppEffectHandler
    {
        public List<AppEffect> Effects { get; } = [];

        public Task HandleAsync(AppEffect effect, CancellationToken cancellationToken = default)
        {
            Effects.Add(effect);
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task DispatchAsync_UpdatesModelAndExecutesEffects()
    {
        var handler = new RecordingEffectHandler();
        var host = new MvuHost(handler);
        var cancellationToken = TestContext.Current.CancellationToken;

        await host.DispatchAsync(new StartupRequestedMessage(), cancellationToken);
        await host.DispatchAsync(new ServerHealthCheckCompletedMessage(IsReachable: true), cancellationToken);

        Assert.Equal(AppPage.Home, host.Model.CurrentPage);
        Assert.Equal(ServerHealthStatus.Reachable, host.Model.ServerHealth);
        Assert.False(host.Model.IsStartupRunning);
        Assert.Equal(2, handler.Effects.Count);
        Assert.IsType<CheckServerHealthEffect>(handler.Effects[0]);
        Assert.IsType<NavigateToPageEffect>(handler.Effects[1]);
    }

    [Fact]
    public async Task DispatchAsync_RaisesModelChangedAndPropertyChanged()
    {
        var host = new MvuHost(new RecordingEffectHandler());
        var modelChanged = 0;
        var propertyChanged = 0;
        host.ModelChanged += (_, _) => modelChanged++;
        host.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MvuHost.Model))
            {
                propertyChanged++;
            }
        };

        await host.DispatchAsync(new NavigateToMessage(AppPage.Settings), TestContext.Current.CancellationToken);

        Assert.Equal(AppPage.Settings, host.Model.CurrentPage);
        Assert.Equal(1, modelChanged);
        Assert.Equal(1, propertyChanged);
    }

    [Fact]
    public async Task DispatchAsync_NoOpMessage_StillExecutesNoEffects()
    {
        var handler = new RecordingEffectHandler();
        var host = new MvuHost(handler);

        await host.DispatchAsync(new NavigatedMessage(AppPage.Loading), TestContext.Current.CancellationToken);

        Assert.Equal(AppPage.Loading, host.Model.CurrentPage);
        Assert.Empty(handler.Effects);
    }
}
