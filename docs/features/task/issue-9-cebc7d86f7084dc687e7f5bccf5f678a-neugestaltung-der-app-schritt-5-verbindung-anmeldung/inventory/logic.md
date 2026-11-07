# Logik

## `AppUpdate` (statisch, reine Update-Funktion)

Datei: `VideoPlayer.Mvu/AppUpdate.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `Update(model, message)` | public | Bildet `AppMessage` auf `AppTransition` (neues `AppModel` + `AppEffect`-Liste) ab |
| `UpdateStartupRequested` | private | Setzt `IsStartupRunning`/`ServerHealth=Checking`; Effekte `InitializeStorageEffect`, `LoadSettingsEffect`, `LoadServersEffect`; Doppel-Dispatch wird ignoriert |
| `UpdateServersLoaded` | private | Mit aktiver Adresse + `IsStartupRunning` → `CheckServerHealthEffect`; ohne Adresse → `CurrentPage=ServerSetup` + `NavigateToPageEffect(ServerSetup)`; Nachladen ohne Start → nur Model |
| `UpdateHealthCheckCompleted` | private | `IsStartupRunning=false`, `ServerHealth` aus Ergebnis, navigiert **immer** nach `Home` (keine Offline-/Anmelde-Verzweigung vorhanden) |
| `UpdateServerCheckCompleted` | private | Erfolg: persistiert Kandidaten (`PersistActiveServerEffect`) und navigiert nach `Home` — **ohne** Anmeldung; Misserfolg: `ServerSetupError`, bleibt auf der Seite |
| `UpdateAddressSubmitted` / `UpdateServerCandidateSelected` | private | Normalisierung via `ServerAddress.TryNormalize`, Fehlertext `InvalidAddressError`, dann `CheckCandidateServerEffect` |
| `UpdateDiscoveryRequested` / `UpdateDiscoveryCompleted` | private | Discovery-Start/-Abschluss im Model |
| `UpdateNavigateTo` / `UpdateNavigated` | private | Navigation zu Menüseiten; `AppPage.Loading` wird abgelehnt |

Fehlertexte als Konstanten: `InvalidAddressError`, `UnreachableServerError`.

## `MvuHost`

Datei: `VideoPlayer.Mvu/MvuHost.cs` — serialisierter Dispatch (SemaphoreSlim),
`Model` unveränderlich, `ModelChanged`/`PropertyChanged`-Events, führt Effekte
sequenziell über `IAppEffectHandler` aus.

## `AppSecretsService`

Datei: `VideoPlayer.Storage/AppSecretsService.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `GetUsername()` | public | Benutzername aus normaler Ablage (`Username`-Schlüssel) |
| `GetPasswordAsync()` | public | Passwort aus sicherer Ablage (inkl. Legacy-Migration) |
| `HasCredentialsAsync()` | public | `true`, wenn Benutzername **und** Passwort vorhanden |
| `SetCredentialsAsync(u, p)` | public | Speichert Benutzername (normal) + Passwort (sicher) |
| `ClearCredentialsAsync()` | public | Entfernt Benutzername, Passwort und Token |
| `GetAuthTokenAsync()` | public | Token aus sicherer Ablage (inkl. Legacy-Migration) |
| `SetAuthTokenAsync(token)` | public | Speichert Token sicher, entfernt Legacy-Rest |
| `ClearAuthTokenAsync()` | public | Entfernt Token aus beiden Ablagen |

## `KnownServersService`

Datei: `VideoPlayer.Storage/KnownServersService.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `GetAllAsync()` | public | Bekannte Server, zuletzt verwendet zuerst |
| `GetLastConnectedAddressAsync()` | public | Zuletzt verbundener Server aus `backup_state` |
| `SetActiveServerAsync(address, displayName?)` | public | Upsert `known_servers` + `LastConnectedServer` |
| `MigrateLegacyServerAddressAsync()` | public | Einmalige Übernahme des Schritt-3-Schlüssels |

## `CacheDatabase` / `BackupDatabase`

Dateien: `VideoPlayer.Storage/CacheDatabase.cs`, `BackupDatabase.cs` —
`Lazy<SQLiteAsyncConnection>`, `InitializeAsync()` legt Tabellen idempotent an.
Keine „Offline-Daten vorhanden"-Abfrage vorhanden.

## `ServerHealthProbe` (`IServerHealthProbe`)

Datei: `VideoPlayer.Maui/Services/ServerHealthProbe.cs` — erstellt
kurzlebigen `HttpClient` (10 s Timeout) + `VideoPlayerApiClient` und ruft
`CheckHealthAsync`; Overload für Kandidatenadressen. Fehler → `false`.

## `ServerAddressProvider` (`IServerAddressProvider`)

Datei: `VideoPlayer.Maui/Services/ServerAddressProvider.cs` —
`GetConfiguredServerAddressAsync` (zuletzt verbundener Server →
`VIDEOPLAYER_SERVER_URL` → `null`), `GetServerAddressAsync` (zusätzlich
IIS-Default `http://localhost/videoplayer/` als letzter Fallback).

## `ShellAppEffectHandler` (`IAppEffectHandler`)

Datei: `VideoPlayer.Maui/Services/ShellAppEffectHandler.cs` — führt Effekte
aus; dispatcht Ergebnis-Messages über `MvuHost` auf dem Main-Thread zurück.

| Effekt | Verhalten |
|--------|-----------|
| `CheckServerHealthEffect` | `IServerHealthProbe.CheckAsync` → `ServerHealthCheckCompletedMessage` |
| `NavigateToPageEffect` | `NavigateAsync`: `ServerSetup` als Window-Root (vor Shell) bzw. `Shell.Current.GoToAsync("serversetup")`; Menüseiten via `//{route}` |
| `InitializeStorageEffect` | `AppStorageLayout` + beide Datenbanken initialisieren |
| `LoadSettingsEffect` / `PersistSettingsEffect` | `AppSettingsService` |
| `LoadServersEffect` | Migration + Liste + `GetConfiguredServerAddressAsync` → `ServersLoadedMessage` |
| `DiscoverServersEffect` | `ServerDiscoveryService.DiscoverAsync` → `ServerDiscoveryCompletedMessage` |
| `CheckCandidateServerEffect` | `IServerHealthProbe.CheckAsync(uri)` → `ServerCheckCompletedMessage` |
| `PersistActiveServerEffect` | `KnownServersService.SetActiveServerAsync` |

Alle Zweige fangen Ausnahmen (`Debug.WriteLine`) — die App darf nicht hängen
bleiben.

## `App` / `AppShell` / `MauiProgram` (`VideoPlayer.Maui`)

- `App.CreateWindow`: Root = `LoadingPage`; einmaliger Dispatch
  `StartupRequestedMessage`.
- `AppShell`: sechs FlyoutItems (`home`…`settings`), registrierte Route
  `serversetup` (kein FlyoutItem); `Navigated` → `NavigatedMessage`.
- `MauiProgram`: DI-Singletons u. a. `MvuHost`, `IAppEffectHandler`,
  `AppSecretsService`, `KnownServersService`, `IServerHealthProbe`;
  Transients für alle Pages. Kein Anmeldedienst, keine `LoginPage`
  registriert.

## `VideoPlayerApiClient`

Datei: `VideoPlayer.ApiClient/VideoPlayerApiClient.cs`

| Methode | Kurzbeschreibung |
|---------|------------------|
| `LoginAsync(email, password, ct)` | `POST api/auth/login`; setzt Token via `SetAccessToken`; `allowReauth=false` (kein Refresh im Login selbst) |
| `CheckHealthAsync(ct)` | `GET api/health`; `false` bei `ApiException` |
| `SetAccessToken`/`ClearAccessToken`/`CurrentToken` | Bearer-Token am `HttpClient` |
| `SendWithPolicyAsync` (private) | 1 Initialversuch + bis zu 3 Wiederholungen (1 s/2 s/4 s) für 5xx, 408, 429 und Transportfehler; bei 401 einmalige Token-Erneuerung über `options.TokenRefresher` mit Neuversand außerhalb des Retry-Budgets; danach `ApiException` |

`VideoPlayerApiClientOptions.TokenRefresher` ist im produktiven Code **nicht
verdrahtet** — die stille Neuanmeldung existiert nur als Extension Point
(getestet in `VideoPlayerApiClientTests_TokenRefresh`).

## Fachliche Referenz Altbestand (nicht Teil der neuen App)

`VideoWebPlayer.Maui/Services/ConnectionService.cs` +
`IConnectionService.cs`: `ConnectionState`-Enum (`Connected`,
`NeedsServerSetup`, `NeedsLogin`, `Offline`);
`StartConnectionWorkflowAsync`: kein Server → `NeedsServerSetup`; HealthCheck
fehlgeschlagen → `Offline`; keine Credentials → `NeedsLogin`; Login →
`Connected`/`NeedsLogin`. `AuthService.LoginAsync` speichert Token+Credentials.
`VideoWebPlayer.Maui/LoginPage.xaml`: Benutzername + Passwort (`IsPassword`),
Login-/Abbrechen-Button, StatusLabel. Dient als fachliche Referenz für
Zustände und Maskenaufbau — wird nicht wiederverwendet.
