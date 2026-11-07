# Umsetzungsplan: Verbindungs-Workflow und Anmeldung

## Übersicht

Die App erhält einen durchgängigen Verbindungs- und Anmeldeworkflow auf der
vorhandenen MVU-Basis: Ein neuer Verbindungszustand (`ConnectionState`:
`Connecting`, `Connected`, `NeedsLogin`, `NeedsServerSetup`, `Offline`) im
`AppModel` steuert die Navigation. Der Start-Workflow wird erweitert: nach der
Erreichbarkeitsprüfung folgt die automatische Anmeldung mit den sicher
gespeicherten Zugangsdaten (`AppSecretsService`); bei Nichterreichbarkeit
entscheidet eine Prüfung der lokalen Ablagen (`CacheDatabase`) zwischen
Offline-Zustand und Serverauswahl. Neu ist eine Anmeldemaske (`LoginPage`,
`AppPage.Login`) sowie ein plattformneutraler `AuthSessionService`, der Login,
Zugangsdaten-/Token-Persistenz und die stille Token-Neuanmeldung
(`TokenRefresher`-Verdrahtung des `VideoPlayerApiClient`) kapselt.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|------------------|------------|
| Platzierung Anmeldeorchestrierung | `AuthSessionService` im Projekt `VideoPlayer.Mvu` (net10.0, referenziert `VideoPlayer.Storage` und `VideoPlayer.ApiClient`) | Der Dienst braucht beide Abhängigkeiten; `VideoPlayer.Mvu` ist das einzige plattformneutrale Projekt mit beiden Referenzen und ist vom Testprojekt referenzierbar → vollständig laufzeittestbar (PoEAA: Service Layer für den Anmeldeablauf, Gateway zum Server). |
| Verbindungszustand | Neuer Enum `ConnectionState` (`Unknown`, `Connecting`, `Connected`, `NeedsLogin`, `NeedsServerSetup`, `Offline`) als Eigenschaft des `AppModel` | Die Anforderung verlangt, dass die Zustände „der App durchgehend bekannt" sind und die Navigation steuern — im MVU-Modell sind sie das überall sichtbar und rein testbar (fachliches Pendant zum `ConnectionState` des Altbestands, ergänzt um `Unknown`/`Connecting` für die Startphase). |
| Auto-Login als Effekt | Neuer `AttemptAutoLoginEffect` (nach erfolgreichem Health-Check und nach erfolgreichem Serverwechsel); Ergebnis via `AutoLoginCompletedMessage` | Seiteneffekte bleiben als Daten modelliert; die Zustandsverzweigung liegt vollständig in `AppUpdate`. |
| Ergebnisklassifikation | `AuthAttemptStatus` (`Succeeded`, `MissingCredentials`, `InvalidCredentials`, `ServerUnreachable`, `Failed`) + record `AuthAttempt(Status, Username)` | Ein Ergebnistyp für Auto- und manuellen Login; `Username` trägt den gespeicherten (nicht geheimen) Benutzernamen zur Vorbefüllung der Anmeldemaske. |
| Stille Token-Erneuerung | `AuthSessionService` verdrahtet `VideoPlayerApiClientOptions.TokenRefresher` mit einer Neuanmeldung über einen eigenen, kurzlebigen Client (gespeicherte Zugangsdaten); bei endgültiger Ablehnung (401/403) oder fehlenden Zugangsdaten wird `SessionExpired` ausgelöst → `SessionExpiredMessage` → `NeedsLogin` | Der Refresh-Mechanismus ist in Schritt 1 bereits gebaut und getestet; nur die produktive Verdrahtung fehlt. Eigenständiger Refresh-Client vermeidet Rekursion. `SessionExpired` ist nötig, damit die App den Zustand `NeedsLogin` auch zur Laufzeit erreicht. |
| „Offline-Daten vorhanden" | `CacheDatabase.HasOfflineDataAsync()` — `true`, sobald `download_manifest`, `cached_lists` oder `cached_favorites` Einträge enthalten | Vorgegebene Bewertung anhand der vorhandenen Ablagen; die Tabellen werden in Schritt 7/11 befüllt, die Zustandsunterscheidung ist jetzt korrekt verankert. |
| Anmeldemaske | Neue `Pages/LoginPage` (`AppPage.Login`, kein FlyoutItem) nach dem Muster der `ServerSetupPage`: MVU-Model-getrieben, Inline-Fehlertext, AutomationIds; Benutzername vorbefüllt (`LoginUsername`), „Anderen Server wählen" führt zur Serverauswahl | UI-Konvention des Projekts; kein interner Bezeichner als Eingabe — Benutzername und Passwort sind die natürlichen, dem Anwender bekannten Angaben; Serverwechsel über die bestehende Auswahlseite (Liste/Erkennung, keine Kennungseingabe). |
| Navigation zur Anmeldemaske | Wie `ServerSetup`: Window-Root vor der Shell (Start-Workflow), geschobene Shell-Route `login` zur Laufzeit (Sitzungsablauf) | Bestehendes Muster in `ShellAppEffectHandler.NavigateAsync` wiederverwendet. |
| Serverwechsel führt zur Anmeldung | `UpdateServerCheckCompleted` (Erfolg) navigiert nicht mehr direkt nach `Home`, sondern stößt `AttemptAutoLoginEffect` an; `IsServerCheckRunning` bleibt bis zum Login-Abschluss gesetzt (Anzeige bleibt) | Invariante: `Home` wird nur aus `Connected` oder `Offline` erreicht; ein Serverwechsel erfordert die Anmeldung am neuen Server. |

## Programmabläufe

### App-Start, Server erreichbar, Zugangsdaten vorhanden (Akzeptanzkriterium 1, „verbunden")

1. `App.RunStartupAsync` dispatcht `StartupRequestedMessage` → Model
   `{ IsStartupRunning = true, ServerHealth = Checking,
   ConnectionState = Connecting }`, Effekte `[InitializeStorageEffect,
   LoadSettingsEffect, LoadServersEffect]` (unverändert).
2. `ServersLoadedMessage` mit `ActiveAddress` → `CheckServerHealthEffect`
   (unverändert).
3. `ServerHealthCheckCompletedMessage(IsReachable: true)` → Model
   `{ ServerHealth = Reachable }` (bleibt `Connecting`, `IsStartupRunning`
   bleibt `true`), Effekt `[AttemptAutoLoginEffect(ActiveServerAddress)]`.
4. `ShellAppEffectHandler` → `AuthSessionService.TryStoredLoginAsync(uri)`:
   `AppSecretsService.GetUsername()` + `HasCredentialsAsync()`; vorhanden →
   `VideoPlayerApiClient.LoginAsync` → bei Erfolg `SetCredentialsAsync` +
   `SetAuthTokenAsync`, der angemeldete Client wird als `Client` gehalten →
   Rückdispatch `AutoLoginCompletedMessage(Succeeded, username)`.
5. `AppUpdate` → Model `{ IsStartupRunning = false, IsServerCheckRunning =
   false, ConnectionState = Connected, CurrentPage = Home }`, Effekt
   `[NavigateToPageEffect(Home)]` → `NavigateAsync` tauscht `LoadingPage`
   gegen `AppShell` und navigiert nach `home`.

### App-Start, Zugangsdaten fehlen oder Anmeldung schlägt fehl („Anmeldung erforderlich")

1.–3. wie oben.
4. `TryStoredLoginAsync`: `HasCredentialsAsync` → `false` →
   `AuthAttempt(MissingCredentials, username)`; Login schlägt fehl →
   `AuthAttempt(InvalidCredentials | ServerUnreachable | Failed, username)`
   → Rückdispatch `AutoLoginCompletedMessage`.
5. `AppUpdate` → Model `{ IsStartupRunning = false, IsServerCheckRunning =
   false, ConnectionState = NeedsLogin, LoginUsername = username,
   LoginError = <Text je Status, null bei MissingCredentials>,
   CurrentPage = Login }`, Effekt `[NavigateToPageEffect(Login)]` →
   `NavigateAsync` setzt `LoginPage` als Window-Root (Shell existiert noch
   nicht).

### App-Start ohne hinterlegten Server („Servereinrichtung erforderlich")

1.–2. wie bisher; `ServersLoadedMessage` mit `ActiveAddress == null` → Model
   zusätzlich `{ ConnectionState = NeedsServerSetup }`, `CurrentPage =
   ServerSetup` + `NavigateToPageEffect(ServerSetup)` (Verhalten aus Schritt
   4, Zustand ergänzt).

### App-Start, Server nicht erreichbar („offline" / Serverauswahl)

1.–2. wie oben.
3. `ServerHealthCheckCompletedMessage(IsReachable: false)` → Model
   `{ ServerHealth = Unreachable }` (bleibt `Connecting`), Effekt
   `[CheckOfflineDataEffect]`.
4. `ShellAppEffectHandler` → `CacheDatabase.HasOfflineDataAsync()` →
   Rückdispatch `OfflineDataCheckCompletedMessage(hasData)`.
5. `AppUpdate`:
   - `hasData == true` → Model `{ IsStartupRunning = false, HasOfflineData =
     true, ConnectionState = Offline, CurrentPage = Home }` +
     `NavigateToPageEffect(Home)`; die `HomePage` zeigt den Offline-Hinweis
     (`Model.IsOffline`).
   - `hasData == false` → Model `{ IsStartupRunning = false, HasOfflineData =
     false, ConnectionState = NeedsServerSetup, CurrentPage = ServerSetup }`
     + `NavigateToPageEffect(ServerSetup)` (Serverauswahl).

### Manuelle Anmeldung auf der `LoginPage`

1. Nutzer tippt „Anmelden" → `LoginSubmittedMessage(username, password)`.
   Leere Felder → Model `{ LoginError = CredentialsRequiredError }`, kein
   Effekt. Gefüllt → Model `{ IsLoginRunning = true, LoginError = null }`,
   Effekt `[AttemptLoginEffect(ActiveServerAddress, username, password)]`.
2. `ShellAppEffectHandler` → `AuthSessionService.LoginAsync(uri, u, p)` →
   Rückdispatch `LoginCompletedMessage(status)`.
3. `AppUpdate`: `Succeeded` → `{ IsLoginRunning = false, ConnectionState =
   Connected, CurrentPage = Home }` + `NavigateToPageEffect(Home)`; sonst →
   `{ IsLoginRunning = false, LoginError = <Statustext> }`, bleibt auf
   `Login`.

### „Anderen Server wählen" von der Anmeldemaske

1. `LoginPage` dispatcht `NavigateToMessage(AppPage.ServerSetup)` →
   `NavigateToPageEffect` → `NavigateAsync`: vor der Shell als Window-Root
   `ServerSetupPage`, in der Shell via `GoToAsync("serversetup")`.
2. Erfolgreiche Kandidatenprüfung (`ServerCheckCompletedMessage`,
   `Success = true`) → persistiert den Server (`PersistActiveServerEffect`)
   und stößt `AttemptAutoLoginEffect` an (statt direkter Home-Navigation —
   siehe Designentscheidung); `IsServerCheckRunning` bleibt bis zum
   Login-Abschluss gesetzt.

### Stille Token-Erneuerung zur Laufzeit (Akzeptanzkriterien 3 und 2)

1. Ein beliebiger API-Aufruf über `AuthSessionService.Client` erhält 401 →
   `SendWithPolicyAsync` ruft `TokenRefresher` (einmalig, außerhalb des
   Retry-Budgets).
2. Der Refresher (`AuthSessionService.SilentReloginAsync`) meldet mit den
   gespeicherten Zugangsdaten über einen eigenen kurzlebigen Client erneut an,
   speichert das neue Token (`SetAuthTokenAsync`) und liefert es zurück → die
   ursprüngliche Anfrage läuft mit neuem Bearer-Token weiter.
3. Fehlende Zugangsdaten oder Ablehnung (401/403 beim Re-Login) →
   `SessionExpired`-Event → `ShellAppEffectHandler` dispatcht
   `SessionExpiredMessage` → `AppUpdate`: `ConnectionState = NeedsLogin`,
   `CurrentPage = Login` + `NavigateToPageEffect(Login)` (bereits in
   `NeedsLogin` → unverändert).
4. Transport-/Serverfehler beim Re-Login → kein `SessionExpired`, die
   Anfrage scheitert regulär mit `ApiException` (kein falscher
   Anmeldezwang bei Netzproblemen).
5. Die Retry-Strategie selbst (3 Versuche, Backoff 1 s/2 s/4 s; retriable:
   5xx, 408, 429, Transportfehler; keine Wiederholung bei übrigen 4xx) ist in
   `VideoPlayerApiClient.SendWithPolicyAsync` bereits implementiert und
   getestet — keine Änderung (Akzeptanzkriterium 2).

### Wiederherstellung aus Backup (Akzeptanzkriterium 4)

1. `known_servers` und `backup_state.LastConnectedServer` sind
   backup-pflichtig → nach Wiederherstellung ist der zuletzt genutzte Server
   bekannt → `ServersLoadedMessage` liefert `ActiveAddress` → Health-Check →
   `AttemptAutoLoginEffect`.
2. Geheimnisse (`Passkey`, `AuthToken`) liegen in `SecureStorage` und kommen
   nicht ins Backup → `HasCredentialsAsync` = `false` → `MissingCredentials`
   → Anmeldemaske mit vorbefülltem Benutzernamen (`Username` liegt in der
   normalen Ablage und ist kein Geheimnis). Sind die Zugangsdaten
   überraschenderweise doch vorhanden, läuft die stille Anmeldung ohne
   Benutzerinteraktion durch.

Beteiligte Klassen: `App`, `MvuHost`, `AppUpdate`, `AppModel`,
`ConnectionState`, `AuthSessionService`, `AuthAttempt`, `AuthAttemptStatus`,
`AutoLoginCompletedMessage`, `LoginSubmittedMessage`,
`LoginCompletedMessage`, `OfflineDataCheckCompletedMessage`,
`SessionExpiredMessage`, `AttemptAutoLoginEffect`, `AttemptLoginEffect`,
`CheckOfflineDataEffect`, `NavigateToPageEffect`, `ShellAppEffectHandler`,
`AppSecretsService`, `CacheDatabase`, `VideoPlayerApiClient`,
`VideoPlayerApiClientOptions`, `LoginPage`, `LoadingPage`, `HomePage`,
`AppShell`, `MauiProgram`.

## Neue Klassen

### Projekt `VideoPlayer.Mvu` (net10.0)

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `ConnectionState` | enum | Verbindungszustände `Unknown`, `Connecting`, `Connected`, `NeedsLogin`, `NeedsServerSetup`, `Offline` |
| `AuthAttemptStatus` | enum | Ergebnis einer Anmeldung: `Succeeded`, `MissingCredentials`, `InvalidCredentials`, `ServerUnreachable`, `Failed` |
| `AuthAttempt` | record | Ergebnistransport `(AuthAttemptStatus Status, string? Username)`; `Succeeded`-Hilfsproperty |
| `AuthSessionService` | Klasse | Anmeldeorchestrierung: `LoginAsync(uri, user, pass)`, `TryStoredLoginAsync(uri)`, gehaltener `Client` mit verdrahtetem `TokenRefresher` (stille Neuanmeldung), `SessionExpired`-Event; injizierbare `Func<Uri, HttpClient>`-Fabrik für Tests |
| `AutoLoginCompletedMessage` | record (`AppMessage`) | `(AuthAttemptStatus Status, string? Username)` |
| `LoginSubmittedMessage` | record (`AppMessage`) | `(string Username, string Password)` — „Anmelden" getippt |
| `LoginCompletedMessage` | record (`AppMessage`) | `(AuthAttemptStatus Status)` |
| `OfflineDataCheckCompletedMessage` | record (`AppMessage`) | `(bool HasOfflineData)` |
| `SessionExpiredMessage` | record (`AppMessage`) | Stille Neuanmeldung endgültig fehlgeschlagen → `NeedsLogin` |
| `AttemptAutoLoginEffect` | record (`AppEffect`) | `(string ServerAddress)` — gespeicherte Zugangsdaten prüfen/anmelden |
| `AttemptLoginEffect` | record (`AppEffect`) | `(string ServerAddress, string Username, string Password)` |
| `CheckOfflineDataEffect` | record (`AppEffect`) | Offline-Daten-Bestand prüfen |

Enum-Änderung: `AppPage` erhält `Login` (keine Menü-Route).

### Projekt `VideoPlayer.Maui`

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `Pages/LoginPage.xaml(.cs)` | ContentPage | Anmeldemaske: Benutzername (vorbefüllt), Passwort, „Anmelden", Inline-Fehler, „Anderen Server wählen"; dispatcht `LoginSubmittedMessage`/`NavigateToMessage` |

## Änderungen an bestehenden Klassen

### `AppModel` (record, `VideoPlayer.Mvu`)

- **Neue Eigenschaften:**
  - `ConnectionState` (`ConnectionState`, Default `Connecting`)
  - `IsLoginRunning` (`bool`) — Anmeldeversuch läuft
  - `LoginError` (`string?`) — Inline-Fehlertext der Anmeldemaske
  - `LoginUsername` (`string?`) — Vorbefüllung des Benutzernamens
  - `HasOfflineData` (`bool`) — Ergebnis der Offline-Daten-Prüfung
  - `IsOffline` (`bool`, berechnet) — `ConnectionState == Offline` (Bindung
    des Offline-Hinweises)
  - `StartupStatusText` (`string`, berechnet) — während des Starts mit
    erreichbarem Server „Anmeldung beim Server läuft …", sonst
    `ServerHealthText`

### `AppUpdate` (statische Klasse, `VideoPlayer.Mvu`)

- **Neue Fehlertexte (Konstanten):** `CredentialsRequiredError`,
  `InvalidCredentialsError`, `LoginUnreachableError`, `LoginFailedError`,
  `SessionExpiredError`; private Hilfsmethode `LoginErrorFor(status)`.
- **Neue Message-Behandlung:** `LoginSubmittedMessage`,
  `LoginCompletedMessage`, `AutoLoginCompletedMessage`,
  `OfflineDataCheckCompletedMessage`, `SessionExpiredMessage` (Verzweigungen
  siehe Programmabläufe).
- **Geänderte Behandlung:**
  - `StartupRequestedMessage`: zusätzlich `ConnectionState = Connecting`.
  - `ServersLoadedMessage`: ohne Adresse zusätzlich
    `ConnectionState = NeedsServerSetup`.
  - `ServerHealthCheckCompletedMessage`: erreichbar →
    `[AttemptAutoLoginEffect]`; nicht erreichbar → `[CheckOfflineDataEffect]`
    (keine direkte Home-Navigation mehr).
  - `ServerCheckCompletedMessage` (Erfolg): Effekte
    `[PersistActiveServerEffect, AttemptAutoLoginEffect]`,
    `IsServerCheckRunning` bleibt `true`, `ConnectionState = Connecting` —
    keine direkte Home-Navigation.

### `CacheDatabase` (`VideoPlayer.Storage`)

- **Neue Methode:** `HasOfflineDataAsync()` → `Task<bool>` — `true`, wenn
  `download_manifest`, `cached_lists` oder `cached_favorites` mindestens einen
  Eintrag enthalten.

### `ShellAppEffectHandler` (`VideoPlayer.Maui`)

- **Neue Effekt-Zweige:** `AttemptAutoLoginEffect` →
  `AuthSessionService.TryStoredLoginAsync` → `AutoLoginCompletedMessage`;
  `AttemptLoginEffect` → `AuthSessionService.LoginAsync` →
  `LoginCompletedMessage`; `CheckOfflineDataEffect` →
  `CacheDatabase.HasOfflineDataAsync` → `OfflineDataCheckCompletedMessage`
  (jeweils Rückdispatch auf dem Main-Thread, Fehler → defensives
  Fehlerergebnis statt Hängenbleiben).
- **`NavigateAsync`:** Sonderfall `AppPage.Login` analog `ServerSetup`
  (Window-Root `LoginPage` vor der Shell; `Shell.Current.GoToAsync("login")`
  in der Shell).
- **Neu:** Abonniert `AuthSessionService.SessionExpired` → dispatcht
  `SessionExpiredMessage` (Host wird erst im Ereignis aufgelöst — keine
  DI-Zirkularität: `MvuHost` hängt vom Handler, nicht umgekehrt).

### `AppShell` (`VideoPlayer.Maui`)

- `Routing.RegisterRoute("login", typeof(Pages.LoginPage))` im Konstruktor.

### `LoadingPage` (`VideoPlayer.Maui`)

- XAML-Bindung `Model.ServerHealthText` → `Model.StartupStatusText` (zeigt
  während der Auto-Anmeldung „Anmeldung beim Server läuft …").

### `HomePage` (`VideoPlayer.Maui`)

- XAML: Offline-Hinweis-Label `OfflineBanner` (`AutomationId`, Text
  „Offline-Modus — keine Verbindung zum Server. Gespeicherte Inhalte werden
  angezeigt."), `IsVisible="{Binding Model.IsOffline}"`.

### `MauiProgram` (`VideoPlayer.Maui`)

- Registrierungen: `AuthSessionService` (Singleton), `Pages.LoginPage`
  (Transient).

## Datenbankmigrationen

Keine — alle Tabellen existieren; `HasOfflineDataAsync` liest nur.

## Validierungsregeln

| Feld / Objekt | Regel | Fehlerfall |
|---------------|-------|------------|
| Anmeldemaske Benutzername/Passwort | beide nicht leer | Inline-Fehlertext `CredentialsRequiredError`, kein Effekt |
| `LoginSubmittedMessage` ohne `ActiveServerAddress` | darf nicht vorkommen (Maske nur mit Server erreichbar) | Defensiv: Übergang zu `NeedsServerSetup` + `NavigateToPageEffect(ServerSetup)` |
| Auto-Login/Manuelles Login | `VideoPlayerApiClient.LoginAsync` erfolgreich | Statusklassifikation: 401/403 → `InvalidCredentials`; Statuscode-freie `ApiException`/Transport/Timeout → `ServerUnreachable`; sonstige `ApiException` → `Failed` |

## Konfigurationsänderungen

Keine — bestehende Mechanismen (`SecureStorage`, `VIDEOPLAYER_SERVER_URL`)
werden genutzt.

## Seiteneffekte und Risiken

- **Start-Workflow:** `ServerHealthCheckCompletedMessage` navigiert nicht
  mehr selbst; betroffene Tests: `AppUpdateTests_Startup.
  Update_HealthCheckCompleted_NavigatesToHome`,
  `MvuHostTests.DispatchAsync_UpdatesModelAndExecutesEffects` (Effektliste).
- **Serverwechsel:** Erfolgreiche Kandidatenprüfung endet nicht mehr direkt
  auf `Home`, sondern in der (Auto-)Anmeldung; betroffener Test:
  `AppUpdateTests_ServerManagement.
  Update_ServerCheckCompleted_Success_PersistsAndNavigatesHome`.
- **Contract-Tests:** `VideoPlayerMauiContractTests.AppShell_ContainsAllMenuEntries`
  bleibt bei sechs FlyoutItems — `LoginPage` kommt nur als registrierte Route
  `login` dazu (Muster wie `serversetup`).
- **DI-Zirkularität:** `AuthSessionService` hängt nur an `AppSecretsService`;
  die `SessionExpired`-Verdrahtung im `ShellAppEffectHandler` löst `MvuHost`
  erst beim Ereignis auf — kein Zyklus `MvuHost → IAppEffectHandler`.
- **Refresh-Client:** Die stille Neuanmeldung nutzt einen eigenen kurzlebigen
  `HttpClient` — kein geteilter Zustand mit dem laufenden Request.
- **SessionExpired zur Laufzeit:** Wechsel auf `AppPage.Login` aus dem
  laufenden Betrieb — geduldet, da die Anmeldung fachlich zwingend ist.
- **Umgebungsvariable:** `VIDEOPLAYER_SERVER_URL` bleibt „konfiguriert" — ohne
  Zugangsdaten landet auch der Dev-Pfad korrekt auf der Anmeldemaske.

## Umsetzungsreihenfolge

1. **MVU-Grundlagen:** `ConnectionState`, `AuthAttemptStatus`, `AuthAttempt`,
   `AppPage.Login`, neue Messages und Effekte anlegen; `AppModel`-Erweiterung.
   - Voraussetzungen: Keine (Typen beruhen auf vorhandenen Basistypen).
2. **`AuthSessionService` in `VideoPlayer.Mvu`** implementieren (Login,
   Auto-Login, Refresh-Verdrahtung, `SessionExpired`).
   - Voraussetzungen: Schritt 1; `AppSecretsService`, `VideoPlayerApiClient`
     (vorhanden).
3. **`AppUpdate`-Verzweigungen** implementieren (vier Zustandsausgänge,
   Login-Pfade, `SessionExpired`).
   - Voraussetzungen: Schritte 1–2.
4. **`CacheDatabase.HasOfflineDataAsync`** in `VideoPlayer.Storage`.
   - Voraussetzungen: Keine.
5. **App-Rand (`VideoPlayer.Maui`):** `LoginPage`, `ShellAppEffectHandler`
   (neue Effekte, `SessionExpired`, `NavigateAsync`-Sonderfall),
   `AppShell`-Route `login`, `LoadingPage`-Bindung, `HomePage`-Offline-Banner,
   `MauiProgram`-Registrierungen.
   - Voraussetzungen: Schritte 1–4.
6. **Tests:** neue `AppUpdateTests_Connection`, `AuthSessionServiceTests`,
   `OfflineData`-Tests, `ConnectionWorkflowContractTests`; betroffene
   Bestandstests anpassen.
   - Voraussetzungen: Schritte 1–5.
7. **Build + Testlauf** (gesamte Solution bauen, dann Tests).
   - Voraussetzungen: Schritt 6.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `Update_Startup_SetsConnecting` | `AppUpdateTests_Connection` | `StartupRequested` setzt `ConnectionState = Connecting` |
| `Update_HealthCheckCompleted_Reachable_StartsAutoLogin` | `AppUpdateTests_Connection` | erreichbar → `AttemptAutoLoginEffect` mit aktiver Adresse, bleibt `Loading`/`Connecting` |
| `Update_HealthCheckCompleted_Unreachable_StartsOfflineDataCheck` | `AppUpdateTests_Connection` | nicht erreichbar → `CheckOfflineDataEffect`, bleibt `Loading` |
| `Update_AutoLoginCompleted_Success_ConnectsAndNavigatesHome` | `AppUpdateTests_Connection` | `Succeeded` → `Connected`, `Home`, `NavigateToPageEffect` |
| `Update_AutoLoginCompleted_MissingCredentials_ShowsLoginWithPrefill` | `AppUpdateTests_Connection` | `MissingCredentials` → `NeedsLogin`, `Login`, `LoginUsername` vorbefüllt, kein `LoginError` |
| `Update_AutoLoginCompleted_Failed_ShowsLoginWithError` | `AppUpdateTests_Connection` | `InvalidCredentials`/`Failed` → `NeedsLogin`, `Login`, `LoginError` gesetzt |
| `Update_OfflineDataCheckCompleted_WithData_GoesOfflineHome` | `AppUpdateTests_Connection` | `hasData` → `Offline`, `Home`, `HasOfflineData` |
| `Update_OfflineDataCheckCompleted_WithoutData_GoesServerSetup` | `AppUpdateTests_Connection` | ohne Daten → `NeedsServerSetup`, `ServerSetup` |
| `Update_ServersLoaded_WithoutServer_NeedsServerSetupState` | `AppUpdateTests_Connection` | Zustand `NeedsServerSetup` beim Erststart |
| `Update_LoginSubmitted_Empty_ShowsCredentialsError` | `AppUpdateTests_Connection` | leere Felder → `CredentialsRequiredError`, keine Effekte |
| `Update_LoginSubmitted_Valid_StartsLogin` | `AppUpdateTests_Connection` | `AttemptLoginEffect` mit Adresse+Zugangsdaten, `IsLoginRunning` |
| `Update_LoginCompleted_Success_ConnectsAndNavigatesHome` | `AppUpdateTests_Connection` | `Connected`, `Home` |
| `Update_LoginCompleted_InvalidCredentials_ShowsErrorAndStays` | `AppUpdateTests_Connection` | `LoginError`, bleibt `Login`/`NeedsLogin` |
| `Update_SessionExpired_WhenConnected_GoesToLogin` | `AppUpdateTests_Connection` | `Connected` → `NeedsLogin` + `NavigateToPageEffect(Login)` |
| `Update_SessionExpired_WhenNeedsLogin_IsIgnored` | `AppUpdateTests_Connection` | Doppel-Behandlung vermeiden |
| `Update_ServerCheckCompleted_Success_StartsAutoLogin` | `AppUpdateTests_Connection` | nach Serverwechsel: `PersistActiveServerEffect` + `AttemptAutoLoginEffect` statt Home-Navigation |
| `MvuHost`-Sequenz „Start → verbunden" | `MvuHostTests` (angepasst) | vollständige Effektkette bis `NavigateToPageEffect(Home)` via `AutoLoginCompletedMessage` |
| `LoginAsync_Success_StoresCredentialsTokenAndClient` | `AuthSessionServiceTests` | `SetCredentialsAsync`+`SetAuthTokenAsync` über `AppSecretsService` (In-Memory-Stores), `Client` gesetzt, `Succeeded` |
| `LoginAsync_InvalidCredentials_StoresNothing` | `AuthSessionServiceTests` | 401 → `InvalidCredentials`, keine Ablage |
| `LoginAsync_Unreachable_MapsStatus` | `AuthSessionServiceTests` | Transportfehler → `ServerUnreachable` |
| `TryStoredLoginAsync_NoCredentials_ReturnsMissing` | `AuthSessionServiceTests` | `MissingCredentials` + gespeicherter `Username` für Vorbefüllung |
| `TryStoredLoginAsync_Success` | `AuthSessionServiceTests` | stille Anmeldung mit gespeicherten Zugangsdaten |
| `Client_401_SilentRelogin_ResendsWithNewToken` | `AuthSessionServiceTests` | produktive `TokenRefresher`-Verdrahtung: 401 → `api/auth/login` mit gespeicherten Zugangsdaten → Neuversand mit neuem Token, Token persistiert |
| `SilentRelogin_Rejected_RaisesSessionExpired` | `AuthSessionServiceTests` | 401 beim Re-Login → `SessionExpired`, Ergebnis `null` |
| `HasOfflineDataAsync_*` | `LocalDatabaseTests` (Erweiterung) | leer → `false`; Eintrag in `cached_lists`/`download_manifest` → `true` |
| `LoginPage_ExistsWithCredentialsInput` u. a. | `ConnectionWorkflowContractTests` | XAML-Controls/AutomationIds, `IsPassword` auf Passwortfeld, Dispatch der Messages, Route `login`, DI-Registrierungen, Effekt-Handler-Zweige |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `AppUpdateTests_Startup.Update_HealthCheckCompleted_NavigatesToHome` | Health-Check-Abschluss stößt jetzt Auto-Login bzw. Offline-Prüfung an statt direkt zu navigieren — wird durch die neuen Tests in `AppUpdateTests_Connection` ersetzt/angepasst |
| `MvuHostTests.DispatchAsync_UpdatesModelAndExecutesEffects` | Effektkette endet mit `AttemptAutoLoginEffect` + `AutoLoginCompletedMessage` → `NavigateToPageEffect(Home)` |
| `AppUpdateTests_ServerManagement.Update_ServerCheckCompleted_Success_PersistsAndNavigatesHome` | Erfolgspfad emittiert `PersistActiveServerEffect` + `AttemptAutoLoginEffect`, `CurrentPage` bleibt `ServerSetup` bis zum Login-Abschluss |

### E2E-Tests (primärer Funktionsnachweis)

Das Repository enthält kein UI-Automatisierungs-Framework (siehe
`inventory/tests.md`); das Testprojekt kann `VideoPlayer.Maui` nicht
referenzieren. Als primärer Funktionsnachweis der Benutzerflüsse dienen
daher — wie in den Schritten 2–4 etabliert — **vollständige
Message-Sequenz-Tests auf MVU-Ebene** (Dispatch-Folgen über
`MvuHost`/`AppUpdate` vom Auslöser bis zur resultierenden
Navigation, inklusive aller Modell-Zwischenstände) plus **Contract-Tests** der
Oberflächen-Verdrahtung (XAML-Bedienelemente, Route, DI). Diese Tests sind die
E2E-Szenarien dieses Schritts.

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| Pflicht | Start → Server erreichbar → Auto-Login → Startseite | `AppUpdateTests_Connection.StartupFlow_*` (Sequenz `StartupRequested → ServersLoaded → HealthCheckCompleted → AutoLoginCompleted`) | Start-Workflow „verbunden" | Benutzerfluss vom App-Start bis zur Startseite |
| Pflicht | Start → Server erreichbar → keine/fehlerhafte Zugangsdaten → Anmeldemaske → manuelle Anmeldung → Startseite | `AppUpdateTests_Connection.StartupFlow_LoginRequired_*` + `LoginFlow_*` | „Anmeldung erforderlich" + Anmeldefluss | Benutzerfluss inkl. Masken-Eingabeaktion |
| Pflicht | Start → kein Server hinterlegt → Servereinrichtung → Adresse prüfen → Anmeldemaske | `AppUpdateTests_Connection.StartupFlow_ServerSetup_*` | „Servereinrichtung erforderlich" | Erststart-Benutzerfluss |
| Pflicht | Start → Server nicht erreichbar → mit Offline-Daten → Startseite (Offline) / ohne → Serverauswahl | `AppUpdateTests_Connection.StartupFlow_Offline_*` | „offline" | Verzweigung des Start-Workflows |
| Pflicht | API-Aufruf mit abgelaufenem Token → stille Neuanmeldung → Aufruf läuft weiter | `AuthSessionServiceTests.Client_401_SilentRelogin_ResendsWithNewToken` | Token-Ablauf ohne manuelle Anmeldung | Ende-zu-Ende über echten HTTP-Stub inkl. Retry-Pipeline |
| Pflicht | Wiederherstellung: Serverdaten vorhanden, Geheimnisse fehlen → Verbindung zum letzten Server + Anmeldeaufforderung | `AppUpdateTests_Connection.RestoreFlow_*` + `AuthSessionServiceTests.TryStoredLoginAsync_NoCredentials_ReturnsMissing` | Backup-Wiederherstellung | Kombiniert Ablage- und Workflow-Ebene |
| Pflicht | Wiederholungsstrategie bei fehlerhaften Serverantworten | `VideoPlayerApiClientTests_RetryPolicy` (Bestand, unverändert) + `AuthSessionServiceTests` | Retry statt Sofort-Fehlschlag | Bereits E2E über HTTP-Stub; bleibt als Nachweis bestehen |

Anzupassende bestehende E2E-/Sequenz-Tests: siehe „Betroffene bestehende
Tests" (`MvuHostTests`, `AppUpdateTests_Startup`, `AppUpdateTests_ServerManagement`).

## Offene Punkte

Keine.
