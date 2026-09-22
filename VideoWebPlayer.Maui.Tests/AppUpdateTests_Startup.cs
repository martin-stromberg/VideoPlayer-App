using VideoPlayer.Mvu;
using VideoPlayer.Storage;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>Tests für <c>AppUpdate.Update</c> – Bereich Startup.</summary>
public class AppUpdateTests_Startup
{
    /// <summary>Prüft Update: bei <c>StartupRequested</c> → setzt Checking und lädt Storage, Einstellungen und Server.</summary>
    [Fact]
    public void Update_StartupRequested_SetsCheckingAndLoadsStorageSettingsServers()
    {
        var transition = AppUpdate.Update(new AppModel(), new StartupRequestedMessage());

        Assert.True(transition.Model.IsStartupRunning);
        Assert.Equal(ServerHealthStatus.Checking, transition.Model.ServerHealth);
        Assert.Equal(AppPage.Loading, transition.Model.CurrentPage);
        // Start löst Ablage-Initialisierung, Einstellungs-Laden, das Laden der
        // Serververwaltung, das Netzwerk-Monitoring und das Lesen der
        // gecachten Startseiten-Listen (Cache-First) aus; der Health-Check
        // folgt auf ServersLoadedMessage.
        Assert.Equal(5, transition.Effects.Count);
        Assert.IsType<InitializeStorageEffect>(transition.Effects[0]);
        Assert.IsType<LoadSettingsEffect>(transition.Effects[1]);
        Assert.IsType<LoadServersEffect>(transition.Effects[2]);
        Assert.IsType<StartNetworkMonitoringEffect>(transition.Effects[3]);
        Assert.IsType<LoadHomeListsEffect>(transition.Effects[4]);
    }

    /// <summary>Prüft Update: bei <c>StartupRequested</c> während eines laufenden Checks → keine Effekte.</summary>
    [Fact]
    public void Update_StartupRequested_WhenAlreadyChecking_NoEffects()
    {
        var model = new AppModel { IsStartupRunning = true, ServerHealth = ServerHealthStatus.Checking };

        var transition = AppUpdate.Update(model, new StartupRequestedMessage());

        Assert.Same(model, transition.Model);
        Assert.Empty(transition.Effects);
    }

    /// <summary>Prüft Update: bei <c>ServersLoaded</c> mit aktivem Server während des Startups → löst den Health-Check aus.</summary>
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

    /// <summary>Prüft Update: bei <c>ServersLoaded</c> mit aktivem Server nach dem Startup → aktualisiert nur das Model.</summary>
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

    /// <summary>Prüft Update: bei <c>ServersLoaded</c> ohne Server → navigiert zum Setup.</summary>
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

    /// <summary>Prüft Update: bei <c>ServersLoaded</c> ohne Server, wenn bereits auf dem Setup → keine Navigation.</summary>
    [Fact]
    public void Update_ServersLoaded_WithoutServer_AlreadyOnSetup_NoNavigation()
    {
        var model = new AppModel { CurrentPage = AppPage.ServerSetup };

        var transition = AppUpdate.Update(model, new ServersLoadedMessage([], null));

        Assert.Empty(transition.Effects);
        Assert.Equal(AppPage.ServerSetup, transition.Model.CurrentPage);
    }

    /// <summary>Prüft Update: bei <c>HealthCheckCompleted</c>, wenn erreichbar → löst AutoLogin aus.</summary>
    [Fact]
    public void Update_HealthCheckCompleted_Reachable_EmitsAutoLogin()
    {
        // Erreichbarer Server → automatische Anmeldung mit gespeicherten
        // Zugangsdaten; die Ladeanzeige bleibt bis zum Ergebnis aktiv.
        var model = new AppModel
        {
            IsStartupRunning = true,
            ServerHealth = ServerHealthStatus.Checking,
            ActiveServerAddress = "http://nas/videoplayer/"
        };

        var transition = AppUpdate.Update(model, new ServerHealthCheckCompletedMessage(true));

        Assert.True(transition.Model.IsStartupRunning);
        Assert.Equal(ServerHealthStatus.Reachable, transition.Model.ServerHealth);
        Assert.Equal(AppPage.Loading, transition.Model.CurrentPage);
        Assert.Equal(ConnectionState.Connecting, transition.Model.ConnectionState);
        var effect = Assert.Single(transition.Effects);
        var autoLogin = Assert.IsType<AttemptAutoLoginEffect>(effect);
        Assert.Equal("http://nas/videoplayer/", autoLogin.ServerAddress);
    }

    /// <summary>Prüft Update: bei <c>HealthCheckCompleted</c>, wenn erreichbar, ohne aktiven Server → wechselt zum Setup.</summary>
    [Fact]
    public void Update_HealthCheckCompleted_Reachable_WithoutActiveServer_GoesToSetup()
    {
        // Defensiver Pfad: Health-Check ohne hinterlegten Server → Einrichtung.
        var model = new AppModel { IsStartupRunning = true, ServerHealth = ServerHealthStatus.Checking };

        var transition = AppUpdate.Update(model, new ServerHealthCheckCompletedMessage(true));

        Assert.False(transition.Model.IsStartupRunning);
        Assert.Equal(AppPage.ServerSetup, transition.Model.CurrentPage);
        Assert.Equal(ConnectionState.NeedsServerSetup, transition.Model.ConnectionState);
        var navigate = Assert.IsType<NavigateToPageEffect>(Assert.Single(transition.Effects));
        Assert.Equal(AppPage.ServerSetup, navigate.Page);
    }

    /// <summary>Prüft Update: bei <c>HealthCheckCompleted</c>, wenn nicht erreichbar → löst die Offline-Datenprüfung aus.</summary>
    [Fact]
    public void Update_HealthCheckCompleted_Unreachable_EmitsOfflineDataCheck()
    {
        // Nicht erreichbar → der Offline-Daten-Bestand entscheidet zwischen
        // Offline-Zustand und Serverauswahl.
        var model = new AppModel
        {
            IsStartupRunning = true,
            ServerHealth = ServerHealthStatus.Checking,
            ActiveServerAddress = "http://nas/videoplayer/"
        };

        var transition = AppUpdate.Update(model, new ServerHealthCheckCompletedMessage(false));

        Assert.True(transition.Model.IsStartupRunning);
        Assert.Equal(ServerHealthStatus.Unreachable, transition.Model.ServerHealth);
        Assert.Equal(AppPage.Loading, transition.Model.CurrentPage);
        Assert.IsType<CheckOfflineDataEffect>(Assert.Single(transition.Effects));
    }
}
