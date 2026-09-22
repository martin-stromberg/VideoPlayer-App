using VideoPlayer.Mvu;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>Tests für <c>MvuHost</c>.</summary>
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

    /// <summary>Prüft DispatchAsync: aktualisiert Model und führt Effekte aus.</summary>
    [Fact]
    public async Task DispatchAsync_UpdatesModelAndExecutesEffects()
    {
        var handler = new RecordingEffectHandler();
        var host = new MvuHost(handler);
        var cancellationToken = TestContext.Current.CancellationToken;

        await host.DispatchAsync(new StartupRequestedMessage(), cancellationToken);
        // Hinterlegter Server → Health-Check → automatische Anmeldung → Startseite.
        await host.DispatchAsync(
            new ServersLoadedMessage([], "http://nas/videoplayer/"), cancellationToken);
        await host.DispatchAsync(new ServerHealthCheckCompletedMessage(IsReachable: true), cancellationToken);
        Assert.Equal(AppPage.Loading, host.Model.CurrentPage);
        Assert.True(host.Model.IsStartupRunning);
        await host.DispatchAsync(
            new AutoLoginCompletedMessage(AuthAttemptStatus.Succeeded, "martin"), cancellationToken);

        Assert.Equal(AppPage.Home, host.Model.CurrentPage);
        Assert.Equal(ConnectionState.Connected, host.Model.ConnectionState);
        Assert.Equal(ServerHealthStatus.Reachable, host.Model.ServerHealth);
        Assert.False(host.Model.IsStartupRunning);
        // Start (Ablage, Einstellungen, Serverliste, Netzwerk-Monitoring,
        // gecachte Startseiten-Listen), Health-Check, automatische
        // Anmeldung, dann Quellen-Laden fürs Menü + Ereignisverbindung +
        // Synchronisation + Download-Wiederaufnahme + Hero-Hintergrund +
        // Navigation zur Startseite.
        Assert.Equal(13, handler.Effects.Count);
        Assert.IsType<InitializeStorageEffect>(handler.Effects[0]);
        Assert.IsType<LoadSettingsEffect>(handler.Effects[1]);
        Assert.IsType<LoadServersEffect>(handler.Effects[2]);
        Assert.IsType<StartNetworkMonitoringEffect>(handler.Effects[3]);
        Assert.IsType<LoadHomeListsEffect>(handler.Effects[4]);
        Assert.IsType<CheckServerHealthEffect>(handler.Effects[5]);
        Assert.IsType<AttemptAutoLoginEffect>(handler.Effects[6]);
        Assert.IsType<LoadSourcesEffect>(handler.Effects[7]);
        Assert.IsType<ConnectServerEventsEffect>(handler.Effects[8]);
        Assert.IsType<RunFullSyncEffect>(handler.Effects[9]);
        Assert.IsType<ResumeDownloadsEffect>(handler.Effects[10]);
        Assert.IsType<LoadHeroBackgroundEffect>(handler.Effects[11]);
        Assert.IsType<NavigateToPageEffect>(handler.Effects[12]);
    }

    /// <summary>Prüft DispatchAsync: löst die ModelChanged- und PropertyChanged-Ereignisse aus.</summary>
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

    /// <summary>Prüft DispatchAsync: bei NoOp-Nachricht → führt weiterhin keine Effekte aus.</summary>
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
