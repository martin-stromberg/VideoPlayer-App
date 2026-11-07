using VideoPlayer.Mvu;
using VideoPlayer.Storage;

namespace VideoPlayer.Maui.Tests;

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

    /// <summary>Prüft OfflineDataCheckCompleted: mit Daten → wechselt in den Offlinezustand.</summary>
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

    /// <summary>Prüft OfflineDataCheckCompleted: ohne Daten → wechselt zum Server-Setup.</summary>
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

    /// <summary>Prüft OfflineDataCheckCompleted: ohne Daten, wenn bereits auf dem Setup → keine Navigation.</summary>
    [Fact]
    public void OfflineDataCheckCompleted_WithoutData_AlreadyOnSetup_NoNavigation()
    {
        var model = new AppModel { CurrentPage = AppPage.ServerSetup, ActiveServerAddress = Server };

        var transition = AppUpdate.Update(model, new OfflineDataCheckCompletedMessage(false));

        Assert.Equal(ConnectionState.NeedsServerSetup, transition.Model.ConnectionState);
        Assert.Empty(transition.Effects);
    }

    // ---- AutoLoginCompletedMessage ----

    /// <summary>Prüft AutoLoginCompleted: bei Erfolg → verbindet und navigiert zur Home-Seite.</summary>
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
        // Verbundener Zustand: Quellen-Laden fürs Menü +
        // Ereignisverbindung + Voll-Sync + Download-Wiederaufnahme +
        // Hero-Hintergrund (Einstellung standardmäßig aktiv) + Navigation.
        Assert.True(transition.Model.IsSyncRunning);
        Assert.True(transition.Model.SourcesOverview.IsLoading);
        Assert.Equal(6, transition.Effects.Count);
        Assert.IsType<LoadSourcesEffect>(transition.Effects[0]);
        Assert.IsType<ConnectServerEventsEffect>(transition.Effects[1]);
        Assert.IsType<RunFullSyncEffect>(transition.Effects[2]);
        Assert.IsType<ResumeDownloadsEffect>(transition.Effects[3]);
        Assert.IsType<LoadHeroBackgroundEffect>(transition.Effects[4]);
        var navigate = Assert.IsType<NavigateToPageEffect>(transition.Effects[5]);
        Assert.Equal(AppPage.Home, navigate.Page);
    }

    /// <summary>Prüft AutoLoginCompleted: bei fehlenden Zugangsdaten → navigiert mit Re-Anmelde-Hinweis zur Login-Seite.</summary>
    [Fact]
    public void AutoLoginCompleted_MissingCredentials_NavigatesToLoginWithReloginNotice()
    {
        // Z. B. nach Backup-Wiederherstellung: Server bekannt, Geheimnisse fehlen.
        // Der Nutzer erhält eine verständliche Begründung statt einer
        // stillen Login-Seite.
        var transition = AppUpdate.Update(
            StartupModel(),
            new AutoLoginCompletedMessage(AuthAttemptStatus.MissingCredentials, "martin"));

        Assert.False(transition.Model.IsStartupRunning);
        Assert.Equal(ConnectionState.NeedsLogin, transition.Model.ConnectionState);
        Assert.Equal("martin", transition.Model.LoginUsername);
        Assert.Equal(AppUpdate.ReloginRequiredNotice, transition.Model.LoginError);
        Assert.Equal(AppPage.Login, transition.Model.CurrentPage);
        var navigate = Assert.IsType<NavigateToPageEffect>(Assert.Single(transition.Effects));
        Assert.Equal(AppPage.Login, navigate.Page);
    }

    /// <summary>Prüft AutoLoginCompleted: bei Fehlschlag → navigiert mit Fehlermeldung zur Login-Seite.</summary>
    /// <param name="status">Der Status.</param>
    /// <param name="expectedError">Der erwartete Fehlertext.</param>
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

    /// <summary>
    /// Prüft AutoLoginCompleted: steht die Servereinrichtung im Fokus
    /// (der Nutzer hat gerade verbunden), wird nicht auf die Anmeldeseite
    /// gewechselt — die Einrichtungsseite bietet als zweite Stufe die
    /// Wahl zwischen Kurzcode und Zugangsdaten.
    /// </summary>
    [Fact]
    public void AutoLoginCompleted_OnServerSetup_StaysOnSetupPage()
    {
        var model = new AppModel
        {
            CurrentPage = AppPage.ServerSetup,
            IsServerCheckRunning = true,
            ActiveServerAddress = Server
        };

        var transition = AppUpdate.Update(
            model,
            new AutoLoginCompletedMessage(AuthAttemptStatus.MissingCredentials, "martin"));

        Assert.Equal(ConnectionState.NeedsLogin, transition.Model.ConnectionState);
        Assert.Equal(AppPage.ServerSetup, transition.Model.CurrentPage);
        Assert.Empty(transition.Effects);
    }

    /// <summary>Prüft AutoLoginCompleted: vom Server-Setup kommend → stoppt den Prüfindikator.</summary>
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

    /// <summary>
    /// Prüft AutoLoginCompleted: bei Gate-Ablehnung (widerrufenes Geräte-Token)
    /// → Re-Pairing-Eingabe statt Anmeldemaske — die Anmeldung käme nie am
    /// Client-Gate vorbei (Sackgasse).
    /// </summary>
    [Fact]
    public void AutoLoginCompleted_GateRejected_NavigatesToRePairing()
    {
        var transition = AppUpdate.Update(
            StartupModel(),
            new AutoLoginCompletedMessage(AuthAttemptStatus.GateRejected, "martin"));

        Assert.False(transition.Model.IsStartupRunning);
        Assert.True(transition.Model.IsPairingRequired);
        Assert.Equal(AppUpdate.DeviceRevokedError, transition.Model.PairingError);
        Assert.Equal(ServerHealthStatus.Reachable, transition.Model.ServerHealth);
        Assert.Equal(ConnectionState.NeedsServerSetup, transition.Model.ConnectionState);
        Assert.Equal(AppPage.ServerSetup, transition.Model.CurrentPage);
        Assert.Equal(2, transition.Effects.Count);
        Assert.IsType<DisconnectServerEventsEffect>(transition.Effects[0]);
        var navigate = Assert.IsType<NavigateToPageEffect>(transition.Effects[1]);
        Assert.Equal(AppPage.ServerSetup, navigate.Page);
    }

    /// <summary>
    /// Prüft AutoLoginCompleted: ein verspätetes Gate-Ablehnungs-Ergebnis darf
    /// den bereits gesetzten Re-Pairing-Zustand nicht auf die Anmeldemaske
    /// übersteuern (DeviceTokenRevokedMessage wurde zuerst verarbeitet).
    /// </summary>
    [Fact]
    public void AutoLoginCompleted_GateRejected_InRePairingState_IsIgnored()
    {
        var model = new AppModel
        {
            CurrentPage = AppPage.ServerSetup,
            ConnectionState = ConnectionState.NeedsServerSetup,
            ActiveServerAddress = Server,
            ServerHealth = ServerHealthStatus.Reachable,
            IsPairingRequired = true,
            PairingError = AppUpdate.DeviceRevokedError
        };

        var transition = AppUpdate.Update(
            model,
            new AutoLoginCompletedMessage(AuthAttemptStatus.GateRejected, null));

        Assert.Same(model, transition.Model);
        Assert.Empty(transition.Effects);
    }

    // ---- LoginSubmittedMessage / LoginCompletedMessage ----

    /// <summary>Prüft LoginSubmitted: bei leeren Feldern → zeigt einen Fehler ohne Effekt.</summary>
    /// <param name="username">Der Benutzername.</param>
    /// <param name="password">Das Passwort.</param>
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

    /// <summary>Prüft LoginSubmitted: bei gültigen Eingaben → startet den Login-Effekt.</summary>
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

    /// <summary>Prüft LoginSubmitted: bei <c>WithoutActiveServer</c> → wechselt zur Setup-Seite.</summary>
    [Fact]
    public void LoginSubmitted_WithoutActiveServer_GoesToSetup()
    {
        var model = new AppModel { CurrentPage = AppPage.Login };

        var transition = AppUpdate.Update(model, new LoginSubmittedMessage("martin", "secret"));

        Assert.Equal(AppPage.ServerSetup, transition.Model.CurrentPage);
        Assert.Equal(ConnectionState.NeedsServerSetup, transition.Model.ConnectionState);
    }

    /// <summary>Prüft LoginCompleted: bei Erfolg → verbindet und navigiert zur Home-Seite.</summary>
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
        Assert.Equal(6, transition.Effects.Count);
        Assert.IsType<LoadSourcesEffect>(transition.Effects[0]);
        Assert.IsType<ConnectServerEventsEffect>(transition.Effects[1]);
        Assert.IsType<RunFullSyncEffect>(transition.Effects[2]);
        Assert.IsType<ResumeDownloadsEffect>(transition.Effects[3]);
        Assert.IsType<LoadHeroBackgroundEffect>(transition.Effects[4]);
        var navigate = Assert.IsType<NavigateToPageEffect>(transition.Effects[5]);
        Assert.Equal(AppPage.Home, navigate.Page);
    }

    /// <summary>Prüft LoginCompleted: bei Fehlschlag → zeigt den Fehler und bleibt auf der Login-Seite.</summary>
    /// <param name="status">Der Status.</param>
    /// <param name="expectedError">Der erwartete Fehlertext.</param>
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

    /// <summary>
    /// Prüft LoginCompleted: bei Gate-Ablehnung → Re-Pairing-Eingabe statt
    /// Anmeldefehler (jede Anmeldung scheitert am Client-Gate).
    /// </summary>
    [Fact]
    public void LoginCompleted_GateRejected_NavigatesToRePairing()
    {
        var model = new AppModel
        {
            CurrentPage = AppPage.Login,
            IsLoginRunning = true,
            ActiveServerAddress = Server
        };

        var transition = AppUpdate.Update(
            model,
            new LoginCompletedMessage(AuthAttemptStatus.GateRejected));

        Assert.False(transition.Model.IsLoginRunning);
        Assert.True(transition.Model.IsPairingRequired);
        Assert.Equal(AppUpdate.DeviceRevokedError, transition.Model.PairingError);
        Assert.Equal(ConnectionState.NeedsServerSetup, transition.Model.ConnectionState);
        Assert.Equal(AppPage.ServerSetup, transition.Model.CurrentPage);
        Assert.IsType<NavigateToPageEffect>(transition.Effects[^1]);
    }

    // ---- SessionExpiredMessage ----

    /// <summary>Prüft SessionExpired: bei <c>FromConnected</c> → navigiert zur Login-Seite.</summary>
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

    /// <summary>Prüft SessionExpired: setzt Quellen- und Browser-Zustand der abgelaufenen Sitzung zurück.</summary>
    [Fact]
    public void SessionExpired_ResetsSourcesOverviewAndBrowser()
    {
        var model = new AppModel
        {
            CurrentPage = AppPage.Home,
            ConnectionState = ConnectionState.Connected,
            ActiveServerAddress = Server,
            SourcesOverview = new SourcesOverviewState
            {
                Attempted = true,
                Sources = [new SourceItem { Id = 7, Name = "Filme" }]
            },
            SourceBrowser = new SourceBrowserState { SourceId = 7, SourceName = "Filme" }
        };

        var transition = AppUpdate.Update(model, new SessionExpiredMessage());

        // Die Quellen der abgelaufenen Sitzung dürfen für den nächsten
        // Benutzer nicht sichtbar bleiben.
        Assert.Same(SourcesOverviewState.Initial, transition.Model.SourcesOverview);
        Assert.Same(SourceBrowserState.Empty, transition.Model.SourceBrowser);
    }

    /// <summary>Prüft SessionExpired: bei <c>WhenAlreadyNeedsLogin</c> → keine Effekte.</summary>
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

    /// <summary>Prüft NavigateTo: auf einer Menüseite → blockiert, solange ein Workflow aussteht.</summary>
    /// <param name="state">Der Zustand.</param>
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

    /// <summary>Prüft NavigateTo: zum Server-Setup → ist von der Login-Seite aus erlaubt.</summary>
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

    /// <summary>Prüft Navigated: auf einer Menüseite bei ausstehendem Workflow → navigiert zurück.</summary>
    /// <param name="state">Der Zustand.</param>
    /// <param name="expectedTarget">Das erwartete Navigationsziel.</param>
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

        var transition = AppUpdate.Update(model, new NavigatedMessage(AppPage.Favorites));

        Assert.Equal(expectedTarget, transition.Model.CurrentPage);
        var navigate = Assert.IsType<NavigateToPageEffect>(Assert.Single(transition.Effects));
        Assert.Equal(expectedTarget, navigate.Page);
    }

    /// <summary>Prüft Navigated: auf einer Menüseite bei <c>WhenConnected</c> → aktualisiert das Model.</summary>
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

    /// <summary>Prüft ServerCheckCompleted: bei Erfolg → persistiert den Server und löst AutoLogin aus.</summary>
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
            new ServerCheckCompletedMessage("http://neu/videoplayer/", "Neuer Server", true, false));

        Assert.True(transition.Model.IsServerCheckRunning);
        Assert.Equal(ConnectionState.Connecting, transition.Model.ConnectionState);
        Assert.Equal("http://neu/videoplayer/", transition.Model.ActiveServerAddress);
        Assert.Equal(AppPage.ServerSetup, transition.Model.CurrentPage);
        Assert.Equal(2, transition.Effects.Count);
        Assert.IsType<PersistActiveServerEffect>(transition.Effects[0]);
        var autoLogin = Assert.IsType<AttemptAutoLoginEffect>(transition.Effects[1]);
        Assert.Equal("http://neu/videoplayer/", autoLogin.ServerAddress);
    }

    /// <summary>Prüft ServerCheckCompleted: im Erfolgspfad → setzt Quellen- und Browser-Zustand zurück.</summary>
    [Fact]
    public void ServerCheckCompleted_Success_ResetsSourcesOverviewAndBrowser()
    {
        // Serverwechsel: die Quellen des bisherigen Servers verlieren
        // ihre Gültigkeit, bis EnterConnected den neuen Stand lädt.
        var model = new AppModel
        {
            CurrentPage = AppPage.ServerSetup,
            IsServerCheckRunning = true,
            SourcesOverview = new SourcesOverviewState
            {
                Attempted = true,
                Sources = [new SourceItem { Id = 7, Name = "Filme" }]
            },
            SourceBrowser = new SourceBrowserState { SourceId = 7, SourceName = "Filme" }
        };

        var transition = AppUpdate.Update(
            model,
            new ServerCheckCompletedMessage("http://neu/videoplayer/", "Neuer Server", true, false));

        Assert.Same(SourcesOverviewState.Initial, transition.Model.SourcesOverview);
        Assert.Same(SourceBrowserState.Empty, transition.Model.SourceBrowser);
    }
}
