# Tasks: Verbindungs-Workflow und Anmeldung

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | MVU-Modell | `ConnectionState`-Enum anlegen (`Unknown`, `Connecting`, `Connected`, `NeedsLogin`, `NeedsServerSetup`, `Offline`) | Offen | – |
| 2 | MVU-Modell | `AuthAttemptStatus`-Enum anlegen (`Succeeded`, `MissingCredentials`, `InvalidCredentials`, `ServerUnreachable`, `Failed`) | Offen | – |
| 3 | MVU-Modell | `AuthAttempt`-Record anlegen (`Status`, `Username`) | Offen | – |
| 4 | MVU-Modell | `AppPage.Login` ergänzen (keine Menü-Route) | Offen | – |
| 5 | MVU-Modell | `AppModel` um `ConnectionState`, `IsLoginRunning`, `LoginError`, `LoginUsername`, `HasOfflineData`, `IsOffline`, `StartupStatusText` erweitern | Offen | – |
| 6 | MVU-Messages | `AutoLoginCompletedMessage`, `LoginSubmittedMessage`, `LoginCompletedMessage`, `OfflineDataCheckCompletedMessage`, `SessionExpiredMessage` anlegen | Offen | – |
| 7 | MVU-Effekte | `AttemptAutoLoginEffect`, `AttemptLoginEffect`, `CheckOfflineDataEffect` anlegen | Offen | – |
| 8 | MVU-Update | `AppUpdate`: vier Zustandsausgänge (Auto-Login nach Health-Check, Offline-Prüfung, `NeedsLogin`/`NeedsServerSetup`/`Offline`/`Connected`), Login-Pfade, `SessionExpired`, geänderte `ServerCheckCompleted`-Erfolgsbehandlung, Fehlertext-Konstanten | Offen | – |
| 9 | Logik | `AuthSessionService` in `VideoPlayer.Mvu` (Login, `TryStoredLoginAsync`, `TokenRefresher`-Verdrahtung mit stiller Neuanmeldung, `SessionExpired`-Event, injizierbare HttpClient-Fabrik) | Offen | – |
| 10 | Datenhaltung | `CacheDatabase.HasOfflineDataAsync()` | Offen | – |
| 11 | UI | `Pages/LoginPage.xaml(.cs)` (Benutzername vorbefüllt, Passwort, „Anmelden", Fehleranzeige, „Anderen Server wählen") | Offen | – |
| 12 | UI | `LoadingPage`-Bindung auf `StartupStatusText` umstellen | Offen | – |
| 13 | UI | `HomePage` Offline-Banner (`OfflineBanner`, `IsOffline`-Bindung) | Offen | – |
| 14 | App-Rand | `ShellAppEffectHandler`: neue Effekt-Zweige, `NavigateAsync`-Sonderfall `Login`, `SessionExpired`-Verdrahtung | Offen | – |
| 15 | App-Rand | `AppShell`: Route `login` registrieren | Offen | – |
| 16 | App-Rand | `MauiProgram`: `AuthSessionService` + `Pages.LoginPage` registrieren | Offen | – |
| 17 | Tests | `AppUpdateTests_Connection` (alle Zustandsausgänge, Login-Pfade, SessionExpired, Serverwechsel) | Offen | – |
| 18 | Tests | `AuthSessionServiceTests` (Login/Auto-Login/Statusmapping, Silent-Relogin-Verdrahtung, SessionExpired) | Offen | – |
| 19 | Tests | `LocalDatabaseTests`: `HasOfflineDataAsync` | Offen | – |
| 20 | Tests | `ConnectionWorkflowContractTests` (LoginPage-XAML, Route, DI, Effekt-Handler) | Offen | – |
| 21 | Tests | Bestandstests anpassen (`AppUpdateTests_Startup`, `AppUpdateTests_ServerManagement`, `MvuHostTests`) | Offen | – |
| 22 | Dokumentation | `docs/help/verbindung-anmeldung/` + Index, README, RELEASE_NOTES | Offen | – |
