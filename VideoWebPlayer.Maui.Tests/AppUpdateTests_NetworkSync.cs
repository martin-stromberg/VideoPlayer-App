using VideoPlayer.Mvu;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>
/// MVU-Übergänge des Netzwerk-/Sync-Workflows (Schritt 6): Monitoring-Start,
/// Offline↔Online-Wechsel, Ereignis-gesteuerte Teil-Synchronisationen,
/// Wiederaufbau der Ereignisverbindung und Sitzungsablauf.
/// </summary>
public class AppUpdateTests_NetworkSync
{
    private const string Server = "http://server/videoplayer/";

    private static AppModel ConnectedModel(AppPage page = AppPage.Home) => new()
    {
        ConnectionState = ConnectionState.Connected,
        ActiveServerAddress = Server,
        CurrentPage = page,
        IsOnline = true,
        IsStartupRunning = false
    };

    [Fact]
    public void StartupRequested_IncludesMonitoringEffect()
    {
        var transition = AppUpdate.Update(new AppModel(), new StartupRequestedMessage());

        Assert.Contains(transition.Effects, e => e is StartNetworkMonitoringEffect);
    }

    [Fact]
    public void NetworkLost_FromConnected_EntersOfflineAndDisconnectsEvents()
    {
        var transition = AppUpdate.Update(
            ConnectedModel(AppPage.Favorites),
            new NetworkAvailabilityChangedMessage(false));

        Assert.Equal(ConnectionState.Offline, transition.Model.ConnectionState);
        Assert.False(transition.Model.IsOnline);
        Assert.True(transition.Model.IsOffline);
        // Der Anwender bleibt auf seiner Seite — nur das Offline-Banner kippt um.
        Assert.Equal(AppPage.Favorites, transition.Model.CurrentPage);
        Assert.Contains(transition.Effects, e => e is DisconnectServerEventsEffect);
    }

    [Fact]
    public void NetworkLost_InNeedsLogin_OnlyUpdatesIsOnline()
    {
        var model = new AppModel
        {
            ConnectionState = ConnectionState.NeedsLogin,
            CurrentPage = AppPage.Login,
            IsOnline = true
        };

        var transition = AppUpdate.Update(model, new NetworkAvailabilityChangedMessage(false));

        Assert.Equal(ConnectionState.NeedsLogin, transition.Model.ConnectionState);
        Assert.False(transition.Model.IsOnline);
        Assert.Empty(transition.Effects);
    }

    [Fact]
    public void NetworkRestored_FromOffline_RunsHealthCheck()
    {
        var model = new AppModel
        {
            ConnectionState = ConnectionState.Offline,
            ActiveServerAddress = Server,
            CurrentPage = AppPage.Home,
            IsOnline = false
        };

        var transition = AppUpdate.Update(model, new NetworkAvailabilityChangedMessage(true));

        Assert.True(transition.Model.IsOnline);
        Assert.Equal(ConnectionState.Connecting, transition.Model.ConnectionState);
        Assert.Equal(ServerHealthStatus.Checking, transition.Model.ServerHealth);
        Assert.Contains(transition.Effects, e => e is CheckServerHealthEffect);
    }

    [Fact]
    public void NetworkRestored_InNeedsServerSetup_OnlyUpdatesIsOnline()
    {
        var model = new AppModel
        {
            ConnectionState = ConnectionState.NeedsServerSetup,
            CurrentPage = AppPage.ServerSetup,
            IsOnline = false
        };

        var transition = AppUpdate.Update(model, new NetworkAvailabilityChangedMessage(true));

        Assert.True(transition.Model.IsOnline);
        Assert.Equal(ConnectionState.NeedsServerSetup, transition.Model.ConnectionState);
        Assert.Empty(transition.Effects);
    }

    [Fact]
    public void AutoLoginSucceeded_OnMenuPage_ConnectsEventsAndSyncsWithoutNavigation()
    {
        // Wiederverbindung zur Laufzeit: der Anwender bleibt auf der Menüseite.
        var model = new AppModel
        {
            ConnectionState = ConnectionState.Connecting,
            ActiveServerAddress = Server,
            CurrentPage = AppPage.Favorites
        };

        var transition = AppUpdate.Update(
            model,
            new AutoLoginCompletedMessage(AuthAttemptStatus.Succeeded, "martin"));

        Assert.Equal(ConnectionState.Connected, transition.Model.ConnectionState);
        Assert.True(transition.Model.IsSyncRunning);
        Assert.Equal(AppPage.Favorites, transition.Model.CurrentPage);
        Assert.Contains(transition.Effects, e =>
            e is ConnectServerEventsEffect c && c.ServerAddress == Server);
        Assert.Contains(transition.Effects, e => e is RunFullSyncEffect);
        Assert.DoesNotContain(transition.Effects, e => e is NavigateToPageEffect);
    }

    [Fact]
    public void LoginSucceeded_OnLoginPage_NavigatesHomeAndSyncs()
    {
        var model = new AppModel
        {
            ConnectionState = ConnectionState.NeedsLogin,
            ActiveServerAddress = Server,
            CurrentPage = AppPage.Login,
            IsLoginRunning = true
        };

        var transition = AppUpdate.Update(
            model,
            new LoginCompletedMessage(AuthAttemptStatus.Succeeded));

        Assert.Equal(ConnectionState.Connected, transition.Model.ConnectionState);
        Assert.True(transition.Model.IsSyncRunning);
        Assert.Equal(AppPage.Home, transition.Model.CurrentPage);
        Assert.Contains(transition.Effects, e =>
            e is ConnectServerEventsEffect c && c.ServerAddress == Server);
        Assert.Contains(transition.Effects, e => e is RunFullSyncEffect);
        Assert.Contains(transition.Effects, e =>
            e is NavigateToPageEffect n && n.Page == AppPage.Home);
    }

    [Fact]
    public void OfflineDataCheck_OnMenuPage_KeepsPage()
    {
        // Wiederverbindungs-Prüfung scheitert erneut: mit Offline-Daten bleibt
        // der Anwender auf seiner Menüseite (kein Sprung zur Startseite).
        var model = new AppModel
        {
            ConnectionState = ConnectionState.Connecting,
            ActiveServerAddress = Server,
            CurrentPage = AppPage.Downloads
        };

        var transition = AppUpdate.Update(model, new OfflineDataCheckCompletedMessage(true));

        Assert.Equal(ConnectionState.Offline, transition.Model.ConnectionState);
        Assert.Equal(AppPage.Downloads, transition.Model.CurrentPage);
        Assert.DoesNotContain(transition.Effects, e => e is NavigateToPageEffect);
    }

    [Theory]
    [InlineData(ServerEventKind.ContinueWatchingUpdated, typeof(SyncContinueWatchingEffect))]
    [InlineData(ServerEventKind.FavoritesChanged, typeof(SyncFavoritesEffect))]
    [InlineData(ServerEventKind.NewVideosScanned, typeof(SyncMetadataEffect))]
    public void ServerEventReceived_MapsToPartialSync(ServerEventKind kind, Type expectedEffect)
    {
        var transition = AppUpdate.Update(
            ConnectedModel(),
            new ServerEventReceivedMessage(kind, SourceId: 7));

        Assert.Contains(transition.Effects, e => e.GetType() == expectedEffect);
    }

    [Fact]
    public void ServerEventReceived_WhenNotConnected_Ignored()
    {
        var model = ConnectedModel() with { ConnectionState = ConnectionState.Offline };

        var transition = AppUpdate.Update(
            model,
            new ServerEventReceivedMessage(ServerEventKind.ContinueWatchingUpdated));

        Assert.Empty(transition.Effects);
        Assert.Equal(ConnectionState.Offline, transition.Model.ConnectionState);
    }

    [Fact]
    public void ServerEventsClosed_WhenConnectedOnline_Reconnects()
    {
        var transition = AppUpdate.Update(
            ConnectedModel(),
            new ServerEventsConnectionClosedMessage());

        Assert.Contains(transition.Effects, e =>
            e is ConnectServerEventsEffect c && c.ServerAddress == Server);
    }

    [Fact]
    public void ServerEventsClosed_WhenOffline_Ignored()
    {
        var model = ConnectedModel() with
        {
            ConnectionState = ConnectionState.Offline,
            IsOnline = false
        };

        var transition = AppUpdate.Update(model, new ServerEventsConnectionClosedMessage());

        Assert.Empty(transition.Effects);
    }

    [Fact]
    public void SynchronizationCompleted_ClearsSyncFlag()
    {
        var model = ConnectedModel() with { IsSyncRunning = true };

        var transition = AppUpdate.Update(
            model,
            new SynchronizationCompletedMessage(new SyncResult(2, 0, true, true, true)));

        Assert.False(transition.Model.IsSyncRunning);
        Assert.Empty(transition.Effects);
    }

    [Fact]
    public void SessionExpired_DisconnectsEvents()
    {
        var transition = AppUpdate.Update(ConnectedModel(), new SessionExpiredMessage());

        Assert.Equal(ConnectionState.NeedsLogin, transition.Model.ConnectionState);
        Assert.Equal(AppPage.Login, transition.Model.CurrentPage);
        Assert.Contains(transition.Effects, e => e is DisconnectServerEventsEffect);
        Assert.Contains(transition.Effects, e =>
            e is NavigateToPageEffect n && n.Page == AppPage.Login);
    }
}
