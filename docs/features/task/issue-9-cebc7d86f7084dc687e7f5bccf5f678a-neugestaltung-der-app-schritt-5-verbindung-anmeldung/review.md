# Plan-Review

Geprüft: `plan.md` gegen die Implementierung (Selbstprüfung ohne Unteragenten —
dokumentierte Abweichung, es stehen keine Unteragenten zur Verfügung).
Alle Planelemente wurden im Code gesucht und vollständig gelesen.

## Ergebnis

**Status:** Vollständig umgesetzt

## Umgesetzte Planelemente

### Projekt `VideoPlayer.Mvu` (net10.0)

- [x] `ConnectionState` — Enum mit `Unknown`, `Connecting`, `Connected`,
  `NeedsLogin`, `NeedsServerSetup`, `Offline`; steuert die Navigation und ist
  durchgehend im `AppModel` bekannt.
- [x] `AuthAttemptStatus` — `Succeeded`, `MissingCredentials`,
  `InvalidCredentials`, `ServerUnreachable`, `Failed`.
- [x] `AuthAttempt` — record `(Status, Username)` inkl. `Succeeded`-
  Hilfsproperty; `Username` trägt den nicht geheimen Benutzernamen zur
  Vorbefüllung.
- [x] `AuthSessionService` — `LoginAsync`/`TryStoredLoginAsync` über
  `VideoPlayerApiClient.LoginAsync`; persistiert Zugangsdaten und Token über
  `AppSecretsService` (sichere Ablage); hält den angemeldeten `Client` mit
  verdrahtetem `TokenRefresher` (stille Neuanmeldung über eigenen kurzlebigen
  Client); `SessionExpired`-Event bei fehlenden/endgültig abgelehnten
  Zugangsdaten; injizierbare `Func<Uri, HttpClient>`-Fabrik und optionale
  `DelayAsync`-Implementierung für Tests.
- [x] `AppPage.Login` — ohne Menü-Route, dokumentiert.
- [x] Messages: `AutoLoginCompletedMessage`, `LoginSubmittedMessage`,
  `LoginCompletedMessage`, `OfflineDataCheckCompletedMessage`,
  `SessionExpiredMessage` (alle `sealed record`, konsistent zum Bestand).
- [x] Effekte: `AttemptAutoLoginEffect`, `AttemptLoginEffect`,
  `CheckOfflineDataEffect`.
- [x] `AppModel` — neue Eigenschaften `ConnectionState` (Default
  `Connecting`), `IsLoginRunning`, `LoginError`, `LoginUsername`,
  `HasOfflineData`, berechnet `IsOffline` und `StartupStatusText`.
- [x] `AppUpdate` — neue Fehlerkonstanten (`CredentialsRequiredError`,
  `InvalidCredentialsError`, `LoginUnreachableError`, `LoginFailedError`,
  `SessionExpiredError`), Hilfsmethode `LoginErrorFor`, Behandlung aller neuen
  Messages; `StartupRequestedMessage` setzt `Connecting`;
  `ServersLoadedMessage` ohne Server setzt `NeedsServerSetup`;
  `ServerHealthCheckCompletedMessage` verzweigt in `AttemptAutoLoginEffect`
  (erreichbar) bzw. `CheckOfflineDataEffect` (nicht erreichbar) statt direkter
  Home-Navigation; `ServerCheckCompletedMessage` (Erfolg) emittiert
  `[PersistActiveServerEffect, AttemptAutoLoginEffect]` und bleibt auf der
  Einrichtungsseite bis zum Login-Abschluss.
- [x] Ergänzend zum Plan: zustandsgesteuerte Navigations-Guards in
  `UpdateNavigateTo`/`UpdateNavigated` — bei `NeedsLogin`/`NeedsServerSetup`
  sind Menüseiten nicht erreichbar (Flyout-Navigation wird auf die
  Workflow-Seite zurückgeführt).

### Projekt `VideoPlayer.Storage`

- [x] `CacheDatabase.HasOfflineDataAsync()` — `true`, sobald `cached_lists`,
  `cached_favorites` oder `download_manifest` Einträge enthalten; nur lesend,
  keine Migration.

### Projekt `VideoPlayer.Maui`

- [x] `Pages/LoginPage.xaml(.cs)` — Anmeldemaske nach dem Muster der
  `ServerSetupPage`: vorbefüllter Benutzername (`LoginUsername`), Passwort-
  Feld (`IsPassword`), „Anmelden", Laufzustand (Sperrung der Eingaben),
  Inline-Fehlertext, Anzeige des aktiven Servers, „Anderen Server einrichten"
  → `NavigateToMessage(AppPage.ServerSetup)`; AutomationIds auf allen
  Bedienelementen.
- [x] `ShellAppEffectHandler` — neue Zweige `AttemptAutoLoginEffect`,
  `AttemptLoginEffect`, `CheckOfflineDataEffect` (jeweils Rückdispatch auf dem
  Main-Thread; Fehler → defensives Ergebnis statt Hängenbleiben);
  `NavigateAsync`-Sonderfall `AppPage.Login` (Window-Root vor der Shell,
  `GoToAsync("login")` in der Shell); `AuthSessionService.SessionExpired`
  abonniert → `SessionExpiredMessage` (Host erst beim Ereignis aufgelöst —
  keine DI-Zirkularität).
- [x] `AppShell` — `Routing.RegisterRoute("login", typeof(LoginPage))`.
- [x] `LoadingPage` — Bindung auf `Model.StartupStatusText` (zeigt während
  der Auto-Anmeldung „Anmeldung beim Server läuft …").
- [x] `HomePage` — `OfflineBanner` (AutomationId) sichtbar bei
  `Model.IsOffline`; Model-Abonnement ergänzt.
- [x] `MauiProgram` — `AuthSessionService` (Singleton), `LoginPage`
  (Transient) registriert.

## Verifizierung der Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung | Nachweis |
|--------------------|-----------|----------|
| Vier Zustandsausgänge des Start-Workflows | `ConnectionState` + Verzweigungen in `AppUpdate` | `AppUpdateTests_Startup`, `AppUpdateTests_Connection` (alle Zweige), `MvuHostTests`-Sequenz |
| Retry-Strategie (3×, 1 s/2 s/4 s; kein Retry bei eindeutigen 4xx außer 401/408/429) | Unverändert in `VideoPlayerApiClient.SendWithPolicyAsync` (Schritt 1) | `VideoPlayerApiClientTests_RetryPolicy` (Bestand, weiter grün); `AuthSessionServiceTests.TryStoredLogin_TransportError_*` zählt 4 Requests |
| Abgelaufener Token → stille Neuanmeldung | `TokenRefresher`-Verdrahtung in `AuthSessionService`, `SessionExpired`-Pfad | `AuthSessionServiceTests.TokenRefresh_*` (Erfolg, fehlende/abgelehnte/transiente Zugangsdaten) |
| Backup-Wiederherstellung: letzter Server + Anmeldung nur bei fehlenden Geheimnissen | `backup_state.LastConnectedServer` (backup-pflichtig) + `SecureStorage`-Geheimnisse außerhalb des Backups → `MissingCredentials` → `LoginPage` mit Vorbefüllung | `AuthSessionServiceTests.TryStoredLogin_UsernameOnly_*`, `AppUpdateTests_Connection.AutoLoginCompleted_MissingCredentials_*` |
| Ladeanzeige beim Start | `LoadingPage` + `StartupStatusText` | Contract-Test `LoadingPage_BindsStartupStatusText`, MVU-Sequenztests |
| Anmeldemaske bei fehlenden/fehlgeschlagenen Zugangsdaten | `AppPage.Login`, `LoginPage`, Message-/Effekt-Paar | `AppUpdateTests_Connection.*Login*`, `ConnectionWorkflowContractTests` |
| Serverauswahl ohne Offline-Daten | `OfflineDataCheckCompletedMessage(false)` → `NeedsServerSetup` | `AppUpdateTests_Connection.OfflineDataCheckCompleted_*` |
| Zustand durchgehend bekannt | `AppModel.ConnectionState` bei jedem Übergang gesetzt | Assertions in allen Sequenztests + Navigations-Guards |

## Feststellungen / Abweichungen vom Plan

- **Navigations-Guards ergänzt** (siehe oben): nicht im Plan benannt,
  fachliche Konsequenz aus „Zustand steuert Navigation" — verhindert, dass
  die Flyout-Navigation während `NeedsLogin`/`NeedsServerSetup` auf
  Menüseiten wechselt.
- **`AuthSessionService` mit optionalem `DelayAsync`-Parameter**: über den im
  Plan genannten Fabrik-Parameter hinaus, damit die Wiederholungsstrategie in
  Tests ohne echte Wartezeiten läuft (Produktivpfad unverändert).
- **Kein separates `Offline`-Seitengrundgerüst**: geplant war der
  Offline-Hinweis auf der Startseite; umgesetzt als `OfflineBanner` auf
  `HomePage` (die Inhaltsseiten werden erst in späteren Schritten gefüllt).

## Ergebnis der Prüfung

Alle Planelemente umgesetzt; alle Akzeptanzkriterien mit Test- oder
Contract-Nachweis belegt. Status: **Vollständig umgesetzt**.
