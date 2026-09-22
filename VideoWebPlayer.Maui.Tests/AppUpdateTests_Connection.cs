using VideoPlayer.Mvu;
using VideoPlayer.Storage;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>
/// Übergänge des Verbindungs-Workflows (Schritt 5): automatische/manuelle
/// Anmeldung, Offline-Verzweigung und Sitzungsablauf. Der Zustand
/// <see cref="ConnectionState"/> ist dabei durchgehend im Model bekannt und
/// steuert die Navigation.
/// </summary>
public class AppUpdateTests_Connection
{
    private const string Server = "http://nas/videoplayer/";

    private static AppModel StartupModel() => new()
    {
        IsStartupRunning = true,
        ServerHealth = ServerHealthStatus.Reachable,
        ActiveServerAddress = Server
    };

    // ---- OfflineDataCheckCompletedMessage ----

    [Fact]
    public void OfflineDataCheckCompleted_WithData_EntersOfflineState()
    {
        var transition = AppUpdate.Update(
            StartupModel(),
            new OfflineDataCheckCompletedMessage(true));

        Assert.False(transition.Model.IsStartupRunning);
        Assert.True(transition.Model.HasOfflineData);
        Assert.Equal(ConnectionState.Offline, transition.Model.ConnectionState);
        Assert.True(transition.Model.IsOffline);
        Assert.Equal(AppPage.Home, transition.Model.CurrentPage);
        var navigate = Assert.IsType<NavigateToPageEffect>(Assert.Single(transition.Effects));
        Assert.Equal(AppPage.Home, navigate.Page);
    }

    [Fact]
    public void OfflineDataCheckCompleted_WithoutData_GoesToServerSetup()
    {
        var transition = AppUpdate.Update(
            StartupModel(),
            new OfflineDataCheckCompletedMessage(false));

        Assert.False(transition.Model.IsStartupRunning);
        Assert.False(transition.Model.HasOfflineData);
        Assert.False(transition.Model.IsOffline);
        Assert.Equal(ConnectionState.NeedsServerSetup, transition.Model.ConnectionState);
        Assert.Equal(AppPage.ServerSetup, transition.Model.CurrentPage);
        var navigate = Assert.IsType<NavigateToPageEffect>(Assert.Single(transition.Effects));
        Assert.Equal(AppPage.ServerSetup, navigate.Page);
    }

    [Fact]
    public void OfflineDataCheckCompleted_WithoutData_AlreadyOnSetup_NoNavigation()
    {
        var model = new AppModel { CurrentPage = AppPage.ServerSetup, ActiveServerAddress = Server };

        var transition = AppUpdate.Update(model, new OfflineDataCheckCompletedMessage(false));

        Assert.Equal(ConnectionState.NeedsServerSetup, transition.Model.ConnectionState);
        Assert.Empty(transition.Effects);
    }

    // ---- AutoLoginCompletedMessage ----

    [Fact]
    public void AutoLoginCompleted_Succeeded_ConnectsAndNavigatesHome()
    {
        var transition = AppUpdate.Update(
            StartupModel(),
            new AutoLoginCompletedMessage(AuthAttemptStatus.Succeeded, "martin"));

        Assert.False(transition.Model.IsStartupRunning);
        Assert.Equal(ConnectionState.Connected, transition.Model.ConnectionState);
        Assert.Equal("martin", transition.Model.LoginUsername);
        Assert.Null(transition.Model.LoginError);
        Assert.Equal(AppPage.Home, transition.Model.CurrentPage);
        // Verbundener Zustand: Ereignisverbindung + Voll-Sync + Navigation.
        Assert.True(transition.Model.IsSyncRunning);
        Assert.Equal(3, transition.Effects.Count);
        Assert.IsType<ConnectServerEventsEffect>(transition.Effects[0]);
        Assert.IsType<RunFullSyncEffect>(transition.Effects[1]);
        var navigate = Assert.IsType<NavigateToPageEffect>(transition.Effects[2]);
        Assert.Equal(AppPage.Home, navigate.Page);
    }

    [Fact]
    public void AutoLoginCompleted_MissingCredentials_NavigatesToLoginWithoutError()
    {
        // Z. B. nach Backup-Wiederherstellung: Server bekannt, Geheimnisse fehlen.
        var transition = AppUpdate.Update(
            StartupModel(),
            new AutoLoginCompletedMessage(AuthAttemptStatus.MissingCredentials, "martin"));

        Assert.False(transition.Model.IsStartupRunning);
        Assert.Equal(ConnectionState.NeedsLogin, transition.Model.ConnectionState);
        Assert.Equal("martin", transition.Model.LoginUsername);
        Assert.Null(transition.Model.LoginError);
        Assert.Equal(AppPage.Login, transition.Model.CurrentPage);
        var navigate = Assert.IsType<NavigateToPageEffect>(Assert.Single(transition.Effects));
        Assert.Equal(AppPage.Login, navigate.Page);
    }

    [Theory]
    [InlineData(AuthAttemptStatus.InvalidCredentials, AppUpdate.InvalidCredentialsError)]
    [InlineData(AuthAttemptStatus.ServerUnreachable, AppUpdate.LoginUnreachableError)]
    [InlineData(AuthAttemptStatus.Failed, AppUpdate.LoginFailedError)]
    public void AutoLoginCompleted_Failed_NavigatesToLoginWithError(AuthAttemptStatus status, string expectedError)
    {
        var transition = AppUpdate.Update(
            StartupModel(),
            new AutoLoginCompletedMessage(status, null));

        Assert.Equal(ConnectionState.NeedsLogin, transition.Model.ConnectionState);
        Assert.Equal(expectedError, transition.Model.LoginError);
        Assert.Equal(AppPage.Login, transition.Model.CurrentPage);
        Assert.Single(transition.Effects);
    }

    [Fact]
    public void AutoLoginCompleted_FromServerSetup_StopsCheckIndicator()
    {
        // Serverwechsel auf der Einrichtungsseite: erfolgreiche Übernahme löst
        // die automatische Anmeldung aus; deren Ergebnis schließt die Anzeige ab.
        var model = new AppModel
        {
            CurrentPage = AppPage.ServerSetup,
            IsServerCheckRunning = true,
            ActiveServerAddress = Server
        };

        var transition = AppUpdate.Update(
            model,
            new AutoLoginCompletedMessage(AuthAttemptStatus.Succeeded, null));

        Assert.False(transition.Model.IsServerCheckRunning);
        Assert.Equal(ConnectionState.Connected, transition.Model.ConnectionState);
        Assert.Equal(AppPage.Home, transition.Model.CurrentPage);
    }

    // ---- LoginSubmittedMessage / LoginCompletedMessage ----

    [Theory]
    [InlineData("", "secret")]
    [InlineData("martin", "")]
    [InlineData(" ", " ")]
    public void LoginSubmitted_EmptyFields_ShowsErrorWithoutEffect(string username, string password)
    {
        var model = new AppModel { CurrentPage = AppPage.Login, ActiveServerAddress = Server };

        var transition = AppUpdate.Update(model, new LoginSubmittedMessage(username, password));

        Assert.Equal(AppUpdate.CredentialsRequiredError, transition.Model.LoginError);
        Assert.False(transition.Model.IsLoginRunning);
        Assert.Empty(transition.Effects);
        Assert.Equal(AppPage.Login, transition.Model.CurrentPage);
    }

    [Fact]
    public void LoginSubmitted_Valid_StartsLoginEffect()
    {
        var model = new AppModel
        {
            CurrentPage = AppPage.Login,
            ActiveServerAddress = Server,
            LoginError = "alter Fehler"
        };

        var transition = AppUpdate.Update(model, new LoginSubmittedMessage("  martin  ", "secret"));

        Assert.True(transition.Model.IsLoginRunning);
        Assert.Null(transition.Model.LoginError);
        var effect = Assert.Single(transition.Effects);
        var login = Assert.IsType<AttemptLoginEffect>(effect);
        Assert.Equal(Server, login.ServerAddress);
        Assert.Equal("martin", login.Username);
        Assert.Equal("secret", login.Password);
    }

    [Fact]
    public void LoginSubmitted_WithoutActiveServer_GoesToSetup()
    {
        var model = new AppModel { CurrentPage = AppPage.Login };

        var transition = AppUpdate.Update(model, new LoginSubmittedMessage("martin", "secret"));

        Assert.Equal(AppPage.ServerSetup, transition.Model.CurrentPage);
        Assert.Equal(ConnectionState.NeedsServerSetup, transition.Model.ConnectionState);
    }

    [Fact]
    public void LoginCompleted_Succeeded_ConnectsAndNavigatesHome()
    {
        var model = new AppModel
        {
            CurrentPage = AppPage.Login,
            IsLoginRunning = true,
            ActiveServerAddress = Server
        };

        var transition = AppUpdate.Update(model, new LoginCompletedMessage(AuthAttemptStatus.Succeeded));

        Assert.False(transition.Model.IsLoginRunning);
        Assert.Null(transition.Model.LoginError);
        Assert.Equal(ConnectionState.Connected, transition.Model.ConnectionState);
        Assert.Equal(AppPage.Home, transition.Model.CurrentPage);
        Assert.True(transition.Model.IsSyncRunning);
        Assert.Equal(3, transition.Effects.Count);
        Assert.IsType<ConnectServerEventsEffect>(transition.Effects[0]);
        Assert.IsType<RunFullSyncEffect>(transition.Effects[1]);
        var navigate = Assert.IsType<NavigateToPageEffect>(transition.Effects[2]);
        Assert.Equal(AppPage.Home, navigate.Page);
    }

    [Theory]
    [InlineData(AuthAttemptStatus.InvalidCredentials, AppUpdate.InvalidCredentialsError)]
    [InlineData(AuthAttemptStatus.ServerUnreachable, AppUpdate.LoginUnreachableError)]
    [InlineData(AuthAttemptStatus.Failed, AppUpdate.LoginFailedError)]
    public void LoginCompleted_Failed_ShowsErrorAndStaysOnLogin(AuthAttemptStatus status, string expectedError)
    {
        var model = new AppModel
        {
            CurrentPage = AppPage.Login,
            IsLoginRunning = true,
            ActiveServerAddress = Server
        };

        var transition = AppUpdate.Update(model, new LoginCompletedMessage(status));

        Assert.False(transition.Model.IsLoginRunning);
        Assert.Equal(expectedError, transition.Model.LoginError);
        Assert.Equal(ConnectionState.NeedsLogin, transition.Model.ConnectionState);
        Assert.Equal(AppPage.Login, transition.Model.CurrentPage);
        Assert.Empty(transition.Effects);
    }

    // ---- SessionExpiredMessage ----

    [Fact]
    public void SessionExpired_FromConnected_NavigatesToLogin()
    {
        var model = new AppModel
        {
            CurrentPage = AppPage.Home,
            ConnectionState = ConnectionState.Connected,
            ActiveServerAddress = Server
        };

        var transition = AppUpdate.Update(model, new SessionExpiredMessage());

        Assert.Equal(ConnectionState.NeedsLogin, transition.Model.ConnectionState);
        Assert.Equal(AppUpdate.SessionExpiredError, transition.Model.LoginError);
        Assert.Equal(AppPage.Login, transition.Model.CurrentPage);
        // Ereignisverbindung trennen, dann zur Anmeldemaske navigieren.
        Assert.Equal(2, transition.Effects.Count);
        Assert.IsType<DisconnectServerEventsEffect>(transition.Effects[0]);
        var navigate = Assert.IsType<NavigateToPageEffect>(transition.Effects[1]);
        Assert.Equal(AppPage.Login, navigate.Page);
    }

    [Fact]
    public void SessionExpired_WhenAlreadyNeedsLogin_NoEffects()
    {
        var model = new AppModel
        {
            CurrentPage = AppPage.Login,
            ConnectionState = ConnectionState.NeedsLogin
        };

        var transition = AppUpdate.Update(model, new SessionExpiredMessage());

        Assert.Same(model, transition.Model);
        Assert.Empty(transition.Effects);
    }

    // ---- Zustandsgesteuerte Navigation (Guards) ----

    [Theory]
    [InlineData(ConnectionState.NeedsLogin)]
    [InlineData(ConnectionState.NeedsServerSetup)]
    public void NavigateTo_MenuPage_BlockedWhileWorkflowPending(ConnectionState state)
    {
        var model = new AppModel
        {
            CurrentPage = state == ConnectionState.NeedsLogin ? AppPage.Login : AppPage.ServerSetup,
            ConnectionState = state,
            ActiveServerAddress = Server
        };

        var transition = AppUpdate.Update(model, new NavigateToMessage(AppPage.Home));

        Assert.Same(model, transition.Model);
        Assert.Empty(transition.Effects);
    }

    [Fact]
    public void NavigateTo_ServerSetup_AllowedFromLogin()
    {
        // „Anderen Server einrichten" von der Anmeldemaske bleibt erlaubt.
        var model = new AppModel
        {
            CurrentPage = AppPage.Login,
            ConnectionState = ConnectionState.NeedsLogin,
            ActiveServerAddress = Server
        };

        var transition = AppUpdate.Update(model, new NavigateToMessage(AppPage.ServerSetup));

        Assert.Equal(AppPage.ServerSetup, transition.Model.CurrentPage);
        var navigate = Assert.IsType<NavigateToPageEffect>(Assert.Single(transition.Effects));
        Assert.Equal(AppPage.ServerSetup, navigate.Page);
    }

    [Theory]
    [InlineData(ConnectionState.NeedsLogin, AppPage.Login)]
    [InlineData(ConnectionState.NeedsServerSetup, AppPage.ServerSetup)]
    public void Navigated_MenuPageWhileWorkflowPending_NavigatesBack(ConnectionState state, AppPage expectedTarget)
    {
        // Flyout-Taps navigieren nativ — das Model navigiert zurück auf die
        // ausstehende Workflow-Seite.
        var model = new AppModel
        {
            CurrentPage = expectedTarget,
            ConnectionState = state,
            ActiveServerAddress = Server
        };

        var transition = AppUpdate.Update(model, new NavigatedMessage(AppPage.Sources));

        Assert.Equal(expectedTarget, transition.Model.CurrentPage);
        var navigate = Assert.IsType<NavigateToPageEffect>(Assert.Single(transition.Effects));
        Assert.Equal(expectedTarget, navigate.Page);
    }

    [Fact]
    public void Navigated_MenuPage_WhenConnected_UpdatesModel()
    {
        var model = new AppModel
        {
            CurrentPage = AppPage.Home,
            ConnectionState = ConnectionState.Connected,
            ActiveServerAddress = Server
        };

        var transition = AppUpdate.Update(model, new NavigatedMessage(AppPage.Favorites));

        Assert.Equal(AppPage.Favorites, transition.Model.CurrentPage);
        Assert.Empty(transition.Effects);
    }

    // ---- Serverwechsel löst Anmeldung aus ----

    [Fact]
    public void ServerCheckCompleted_Success_EmitsPersistAndAutoLogin()
    {
        var model = new AppModel
        {
            CurrentPage = AppPage.ServerSetup,
            IsServerCheckRunning = true,
            KnownServers = [new KnownServer { Address = "http://alt/videoplayer/" }]
        };

        var transition = AppUpdate.Update(
            model,
            new ServerCheckCompletedMessage("http://neu/videoplayer/", "Neuer Server", true));

        Assert.True(transition.Model.IsServerCheckRunning);
        Assert.Equal(ConnectionState.Connecting, transition.Model.ConnectionState);
        Assert.Equal("http://neu/videoplayer/", transition.Model.ActiveServerAddress);
        Assert.Equal(AppPage.ServerSetup, transition.Model.CurrentPage);
        Assert.Equal(2, transition.Effects.Count);
        Assert.IsType<PersistActiveServerEffect>(transition.Effects[0]);
        var autoLogin = Assert.IsType<AttemptAutoLoginEffect>(transition.Effects[1]);
        Assert.Equal("http://neu/videoplayer/", autoLogin.ServerAddress);
    }
}
