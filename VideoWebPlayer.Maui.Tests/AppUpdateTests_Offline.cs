using VideoPlayer.Mvu;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>
/// Offlinebetrieb und Wiederherstellung (Schritt 12): MVU-Übergänge der
/// Offline-Einschränkungen (Platzhalter, Quellen, Profil, Downloads),
/// der Ablage-Wiederherstellung (<see cref="StorageRecoveredMessage"/>),
/// der Neu-Synchronisation verworfener Caches und des
/// Re-Anmelde-Hinweises nach einer Backup-Wiederherstellung.
/// </summary>
public class AppUpdateTests_Offline
{
    private const string Server = "http://nas/videoplayer/";

    private static AppModel OfflineModel() => new()
    {
        ConnectionState = ConnectionState.Offline,
        ActiveServerAddress = Server,
        CurrentPage = AppPage.Home,
        IsOnline = false,
        IsStartupRunning = false,
        HasOfflineData = true
    };

    private static AppModel ConnectedModel() => new()
    {
        ConnectionState = ConnectionState.Connected,
        ActiveServerAddress = Server,
        CurrentPage = AppPage.Home,
        IsOnline = true,
        IsStartupRunning = false
    };

    private static HomeMediaItem Item(long id = 5, string mediaType = "movie", string title = "Film") =>
        new() { Id = id, MediaType = mediaType, Title = title };

    /// <summary>Prüft MediaItemSelected: Platzhalter fehlender Offline-Episoden navigieren nicht.</summary>
    [Fact]
    public void MediaItemSelected_Placeholder_NoNavigation()
    {
        var transition = AppUpdate.Update(
            OfflineModel(),
            new MediaItemSelectedMessage(Item(32, "tvshowepisode", "Folge 2") with { IsPlaceholder = true }));

        Assert.Empty(transition.Effects);
    }

    /// <summary>Prüft MediaItemSelected: ein abspielbarer Offline-Eintrag navigiert wie gewohnt.</summary>
    [Fact]
    public void MediaItemSelected_OfflinePlayable_Navigates()
    {
        var transition = AppUpdate.Update(
            OfflineModel(),
            new MediaItemSelectedMessage(Item(31, "tvshowepisode", "Pilot") with { ShowId = 11 }));

        var navigation = Assert.IsType<NavigateToMediaDetailsEffect>(Assert.Single(transition.Effects));
        Assert.Equal(31, navigation.MediaId);
        Assert.Equal(11, navigation.ShowId);
    }

    /// <summary>Prüft NetworkLost: aus <c>Connected</c> → setzt den Quellenstand des Menüs zurück.</summary>
    [Fact]
    public void NetworkLost_FromConnected_ResetsSourcesOverview()
    {
        // Offline steht keine Quellenliste zur Verfügung — die Menü-
        // Einträge werden mit dem Übergang entfernt (kein veralteter
        // Stand als aktuell).
        var model = ConnectedModel() with
        {
            SourcesOverview = new SourcesOverviewState
            {
                Attempted = true,
                Sources = [new SourceItem { Id = 7, Name = "Filme" }]
            }
        };

        var transition = AppUpdate.Update(model, new NetworkAvailabilityChangedMessage(false));

        Assert.Equal(ConnectionState.Offline, transition.Model.ConnectionState);
        Assert.Same(SourcesOverviewState.Initial, transition.Model.SourcesOverview);
        Assert.Contains(transition.Effects, e => e is DisconnectServerEventsEffect);
    }

    /// <summary>Prüft ProfilePageOpened: offline wird kein Profilabruf ausgelöst.</summary>
    [Fact]
    public void ProfilePageOpened_Offline_NoLoadEffect()
    {
        var transition = AppUpdate.Update(
            OfflineModel() with { CurrentPage = AppPage.Profile },
            new ProfilePageOpenedMessage());

        Assert.DoesNotContain(transition.Effects, e => e is LoadProfileInfoEffect);
        Assert.True(transition.Model.Profile.Attempted);
    }

    /// <summary>Prüft DownloadRequested: offline wird die Anforderung mit Hinweis abgelehnt (kein Effekt).</summary>
    [Fact]
    public void DownloadRequested_Offline_ShowsNoticeWithoutEffect()
    {
        var transition = AppUpdate.Update(
            OfflineModel(),
            new DownloadRequestedMessage("movie", 42, "Film"));

        Assert.Empty(transition.Effects);
        Assert.NotNull(transition.Model.MediaDetails.DownloadNotice);
        Assert.True(transition.Model.MediaDetails.DownloadNoticeIsError);
    }

    /// <summary>Prüft DownloadRequested: mit Serienkontext landen die Hints im Enqueue-Effekt.</summary>
    [Fact]
    public void DownloadRequested_WithSeriesContext_ForwardsHints()
    {
        var transition = AppUpdate.Update(
            ConnectedModel(),
            new DownloadRequestedMessage("tvshowepisode", 31, "Serie – Pilot")
            {
                ShowId = 11,
                ShowTitle = "Serie",
                SeasonNumber = 1,
                EpisodeNumber = 1
            });

        var effect = Assert.IsType<EnqueueDownloadEffect>(Assert.Single(transition.Effects));
        Assert.Equal(11, effect.ShowId);
        Assert.Equal("Serie", effect.ShowTitle);
        Assert.Equal(1, effect.SeasonNumber);
        Assert.Equal(1, effect.EpisodeNumber);
    }

    /// <summary>Prüft HomeListsLoaded: verworfene Caches lösen online eine Voll-Synchronisation aus.</summary>
    [Fact]
    public void HomeListsLoaded_DiscardedCaches_WhileConnected_RunsFullSync()
    {
        var lists = new HomeLists
        {
            DiscardedCaches = [ServerSyncService.ContinueWatchingCacheKey]
        };

        var transition = AppUpdate.Update(ConnectedModel(), new HomeListsLoadedMessage(lists));

        Assert.True(transition.Model.IsSyncRunning);
        Assert.Contains(transition.Effects, e => e is RunFullSyncEffect);
    }

    /// <summary>Prüft HomeListsLoaded: verworfene Caches lösen offline keine Synchronisation aus.</summary>
    [Fact]
    public void HomeListsLoaded_DiscardedCaches_WhileOffline_NoSync()
    {
        var lists = new HomeLists
        {
            DiscardedCaches = [ServerSyncService.ContinueWatchingCacheKey]
        };

        var transition = AppUpdate.Update(OfflineModel(), new HomeListsLoadedMessage(lists));

        Assert.DoesNotContain(transition.Effects, e => e is RunFullSyncEffect);
    }

    /// <summary>Prüft HomeListsLoaded: bei laufender Synchronisation wird kein zweiter Lauf gestartet.</summary>
    [Fact]
    public void HomeListsLoaded_DiscardedCaches_WhileSyncRunning_NoDuplicate()
    {
        var lists = new HomeLists
        {
            DiscardedCaches = [ServerSyncService.RecentCacheKey]
        };

        var transition = AppUpdate.Update(
            ConnectedModel() with { IsSyncRunning = true },
            new HomeListsLoadedMessage(lists));

        Assert.DoesNotContain(transition.Effects, e => e is RunFullSyncEffect);
    }

    /// <summary>Prüft StorageRecovered: der Hinweis landet im Model (Banner der Startseite).</summary>
    [Fact]
    public void StorageRecovered_SetsNotice()
    {
        var transition = AppUpdate.Update(
            new AppModel(),
            new StorageRecoveredMessage("Daten neu initialisiert"));

        Assert.Equal("Daten neu initialisiert", transition.Model.StorageNotice);
        Assert.Empty(transition.Effects);
    }

    /// <summary>Prüft AutoLoginCompleted: ohne konkretes Fehlerbild wird der Re-Anmelde-Hinweis gezeigt.</summary>
    [Fact]
    public void AutoLoginCompleted_MissingCredentials_ShowsReloginNotice()
    {
        var model = new AppModel
        {
            IsStartupRunning = true,
            ServerHealth = ServerHealthStatus.Reachable,
            ActiveServerAddress = Server
        };

        var transition = AppUpdate.Update(
            model,
            new AutoLoginCompletedMessage(AuthAttemptStatus.MissingCredentials, "martin"));

        Assert.Equal(AppUpdate.ReloginRequiredNotice, transition.Model.LoginError);
        Assert.Equal(ConnectionState.NeedsLogin, transition.Model.ConnectionState);
        Assert.Equal(AppPage.Login, transition.Model.CurrentPage);
        Assert.Contains(transition.Effects, e => e is NavigateToPageEffect nav && nav.Page == AppPage.Login);
    }

    /// <summary>Prüft AutoLoginCompleted: eine konkrete Fehlerart behält ihren spezifischen Text.</summary>
    [Fact]
    public void AutoLoginCompleted_ServerUnreachable_KeepsSpecificError()
    {
        var model = new AppModel
        {
            IsStartupRunning = true,
            ServerHealth = ServerHealthStatus.Reachable,
            ActiveServerAddress = Server
        };

        var transition = AppUpdate.Update(
            model,
            new AutoLoginCompletedMessage(AuthAttemptStatus.ServerUnreachable, null));

        Assert.Equal(AppUpdate.LoginUnreachableError, transition.Model.LoginError);
    }

    /// <summary>Prüft EnterConnected (Wiederverbindung): startet Ereignisverbindung, Voll-Sync und Download-Wiederaufnahme.</summary>
    [Fact]
    public void Reconnection_FromOffline_RunsFullSyncAndResumesDownloads()
    {
        // Online-Rückkehr aus dem Offline-Zustand: Erreichbarkeitsprüfung
        // → automatische Anmeldung erfolgreich → EnterConnected.
        var offline = OfflineModel() with { IsOnline = true };

        var reconnect = AppUpdate.Update(offline, new NetworkAvailabilityChangedMessage(true));
        Assert.Contains(reconnect.Effects, e => e is CheckServerHealthEffect);

        var health = AppUpdate.Update(
            reconnect.Model,
            new ServerHealthCheckCompletedMessage(true, false));
        Assert.Contains(health.Effects, e => e is AttemptAutoLoginEffect);

        var connected = AppUpdate.Update(
            health.Model,
            new AutoLoginCompletedMessage(AuthAttemptStatus.Succeeded, "martin"));

        Assert.Equal(ConnectionState.Connected, connected.Model.ConnectionState);
        Assert.True(connected.Model.IsSyncRunning);
        Assert.Contains(connected.Effects, e => e is ConnectServerEventsEffect);
        Assert.Contains(connected.Effects, e => e is RunFullSyncEffect);
        Assert.Contains(connected.Effects, e => e is ResumeDownloadsEffect);
    }
}
