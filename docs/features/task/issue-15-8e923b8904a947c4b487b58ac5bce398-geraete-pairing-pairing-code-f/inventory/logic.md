# Logik — Bestandsaufnahme

## `ClientGate`
Datei: `VideoPlayer.ApiClient/ClientGate.cs`

Statische, plattformneutrale Auflösung des Client-Gate-Schlüssels (`X-API-Key`).

| Methode / Member | Sichtbarkeit | Kurzbeschreibung |
|------------------|-------------|------------------|
| `HeaderName` | `public const` | `"X-API-Key"` |
| `ApiTokenEnvironmentVariable` | `public const` | `"VIDEOWEBPLAYER_MAUI_API_TOKEN"` |
| `ApiTokenAssemblyMetadataKey` | `public const` | `"VideoPlayer.ApiClient.ApiToken"` |
| `ApiTokenPlaceholder` | `public const` | `"<MAUI_CLIENT_API_TOKEN>"` |
| `ApiTokenFileName` | `public const` | `"client-api-token.txt"` |
| `TokenFileDirectory` / `TokenFilePath` | `public static` | `%LocalAppData%/VideoPlayer/` bzw. voller Pfad der Token-Datei |
| `Resolve()` | `public static` | Synchron: Umgebungsvariable → Assembly-Metadata → Token-Datei → Platzhalter (Werte gleich dem Platzhalter gelten als nicht konfiguriert). Schreibt im DEBUG-Build eine gehashte Diagnosezeile nach `%TEMP%/videoplayer-clientgate.log`. |
| `Resolve(string?, string?)` / `Resolve(string?, string?, string?)` | `internal static` | Testbare Überladungen (per `InternalsVisibleTo` auf `VideoWebPlayer.Maui.Tests`). |
| `FirstConfigured(string?)` | `private static` | Trim + Leer-/Platzhalter-Filter. |
| `ReadMetadataValue()` / `ReadTokenFile()` | `private static` | Quellenleser (Metadata-Attribut bzw. Datei, IO-/Zugriffsfehler → `null`). |

Aufrufer von `Resolve()`: `VideoPlayerApiClient`-Konstruktor (Zeile 68–72) und damit indirekt `ServerHealthProbe.CheckAsync` (erzeugt `new VideoPlayerApiClient(httpClient)` ohne Options) sowie `AuthSessionService.CreateClient`/`SilentReloginAsync` (Options ohne `ApiKey` → Fallback `Resolve()`).

**Befund:** Keine Geräte-Token-/SecureStorage-Quelle. `Resolve()` ist synchron; `ISecretStore.GetAsync` ist asynchron — die in der Anforderung geforderte Priorität „Geräte-Token > Env > Metadata > Datei > Platzhalter" ist im bestehenden Aufrufpfad noch nicht darstellbar.

## `VideoPlayerApiClient`
Datei: `VideoPlayer.ApiClient/VideoPlayerApiClient.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| Konstruktor `(HttpClient, VideoPlayerApiClientOptions?)` | `public` | Setzt `BaseAddress` (wenn leer) und ergänzt `X-API-Key` aus `options.ApiKey` oder `ClientGate.Resolve()` in `DefaultRequestHeaders` — **nur einmalig, kein Update-Mechanismus**. Wirft `InvalidOperationException` ohne Basisadresse. |
| `CurrentToken` | `public` | Aktueller Bearer-Token (aus `DefaultRequestHeaders.Authorization`). |
| `SetAccessToken(AuthToken)` / `ClearAccessToken()` | `public` | Bearer-Token setzen/entfernen. |
| `LoginAsync(email, password)` | `public` | `POST api/auth/login` (`allowReauth: false`), setzt Bearer-Token. |
| `CheckHealthAsync()` | `public` | `GET api/health`; `ApiException` → `false`. |
| `GetSourcesAsync`, `GetSourceAsync`, `GetSourceGenresAsync`, `GetRecentEntriesAsync`, `GetContinueWatchingAsync`, `ReportPlaybackProgressAsync`, `HideContinueWatchingEntryAsync`, `SkipContinueWatchingEntryAsync`, `GetFavoritesAsync`, `ToggleFavoriteAsync`, `RemoveFavoriteAsync`, `GetSourceItemsAsync`, `GetMovieAsync`, `GetMovieCollectionAsync`, `GetTvShowAsync`, `GetTvShowEpisodeAsync`, `GetGenreOptionsAsync`, `GetPictureAsync`, `GetSourceIconAsync`, `GetHeroBackgroundAsync`, `GetEpisodeBackgroundImageAsync` | `public` | Fach-Endpunkte; alle über `SendWithPolicyAsync`. |
| `SendWithPolicyAsync(endpoint, requestFactory, allowReauth, ct)` | `private` | Feste Wiederholungsstrategie: 1 Versuch + bis zu 3 Wiederholungen (1 s/2 s/4 s) bei 5xx, Transportfehlern, Timeout, **408 und 429**; bei **401** einmalig `TokenRefresher` + Neuversand (außerhalb des Retry-Budgets, nur wenn `allowReauth`). |
| `IsRetriable` | `private static` | `(int)status >= 500 \|\| 408 \|\| 429` — **429 gilt als wiederholbar** (für den Pairing-Endpunkt relevant: IP-Sperre würde erst nach bis zu 4 Versuchen als `ApiException` sichtbar). |
| `TryRefreshTokenAsync` | `private` | Ruft `options.TokenRefresher` unter `SemaphoreSlim`-Sperre; setzt Bearer bei Erfolg. Unterscheidet nicht zwischen Gate-401 (Geräte-Token) und Bearer-401 (Benutzer-JWT). |
| `CreateJsonPost` / `DeserializeAsync` / `CreateApiExceptionAsync` | `private static` | JSON-POST (`StringContent`, PascalCase-Payload), Deserialisierung (case-insensitiv), `ApiException` mit `Endpoint`/`StatusCode`/`ResponseBody`. |

**Befund:** Keine `ExchangePairingCodeAsync`-Methode, kein Weg, `X-API-Key`/Bearer für einen einzelnen Request zu unterdrücken (Header sitzen in `DefaultRequestHeaders`), kein Update des Gate-Headers nach dem Konstruktor.

## `ApiException`
Datei: `VideoPlayer.ApiClient/ApiException.cs`

`HttpRequestException` mit `Endpoint` (string), `StatusCode` (erbt `HttpRequestException.StatusCode`), `ResponseBody`. Trägt die für das Pairing relevanten Statuscodes 400/401/429 (429 allerdings erst nach Ausschöpfung der Wiederholungen).

## `VideoPlayerApiClientOptions`
Datei: `VideoPlayer.ApiClient/VideoPlayerApiClientOptions.cs`

`BaseAddress` (Uri?), `ApiKey` (string?, Standard `ClientGate.Resolve()`), `TokenRefresher` (`Func<CancellationToken, Task<AuthToken?>>`, max. einmal pro Anfrage), `DelayAsync` (Test-/Timing-Hook), `Logger`.

## `AppSecretsService`
Datei: `VideoPlayer.Storage/AppSecretsService.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `UsernameKey` / `PasswordKey` / `AuthTokenKey` | `public const` | Ablageschlüssel `"Username"` (normale Ablage), `"Passkey"` und `"AuthToken"` (sichere Ablage) — jeweils `AppProfile.PrefixKey`-präfixiert. **Kein Geräte-Token-Schlüssel vorhanden.** |
| `GetUsername()` | `public` | Benutzername aus `ISettingsStore`. |
| `GetPasswordAsync()` / `HasCredentialsAsync()` / `SetCredentialsAsync(u, p)` / `ClearCredentialsAsync()` | `public` | Zugangsdaten; `Clear*` entfernt auch das Auth-Token. |
| `GetAuthTokenAsync()` / `SetAuthTokenAsync(token)` / `ClearAuthTokenAsync()` | `public` | Benutzer-Anmelde-Token in `ISecretStore`, inkl. Legacy-Migration aus der normalen Ablage. |
| `GetMigratedSecretAsync(key)` | `private` | Liest sicheren Wert; migriert Legacy-Preference → `ISecretStore`. |

## `KnownServersService`
Datei: `VideoPlayer.Storage/KnownServersService.cs`

Persistiert bekannte Server und den zuletzt verbundenen Server in der `BackupDatabase` (Tabellen `known_servers`, `backup_state`).

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `GetAllAsync()` | `public` | Alle bekannten Server, zuletzt verwendet zuerst. |
| `GetLastConnectedAddressAsync()` | `public` | Aktive Serveradresse (`BackupStateEntry.LastConnectedServerKey`) oder `null`. |
| `SetActiveServerAsync(address, displayName)` | `public` | Upsert in `known_servers` + Setzen des zuletzt verbundenen Servers. Aufgerufen aus `PersistActiveServerAsync` des `ShellAppEffectHandler`. |
| `MigrateLegacyServerAddressAsync()` | `public` | Einmalige Migration des `ServerAddress`-Settings-Schlüssels. |

## `AuthSessionService`
Datei: `VideoPlayer.Mvu/AuthSessionService.cs`

Orchestriert Anmeldung, Token-Persistenz und stille Neuanmeldung. Hält den angemeldeten `VideoPlayerApiClient` (`Client`, `ClientBaseAddress`).

| Methode / Member | Sichtbarkeit | Kurzbeschreibung |
|------------------|-------------|------------------|
| `TryStoredLoginAsync(serverAddress)` | `public` | Gespeichertes Auth-Token via `api/favorites` validieren → sonst Zugangsdaten-Login → sonst `MissingCredentials`. |
| `LoginAsync(serverAddress, username, password)` | `public` | Manuelle Anmeldung; speichert Zugangsdaten + Token. |
| `LogoutAsync()` | `public` | Client freigeben, `ClearCredentialsAsync`. |
| `SessionExpired` | `public event` | Wird ausgelöst, wenn die stille Neuanmeldung endgültig scheitert (fehlende/abgelehnte Zugangsdaten) — aktuell der einzige „zurück zum Workflow"-Mechanismus am Service-Rand; erzeugt `SessionExpiredMessage` → `NeedsLogin` (nicht `NeedsServerSetup`). |
| `CreateClient(serverAddress)` | `private` | Erzeugt `VideoPlayerApiClient` mit `TokenRefresher = SilentReloginAsync`; **kein `ApiKey` gesetzt** → Konstruktor-Fallback `ClientGate.Resolve()`. |
| `SilentReloginAsync(serverAddress)` | `private` | Neuanmeldung über kurzlebigen eigenen Client; bei 401/403 → `ClearAuthTokenAsync` + `SessionExpired`. |
| `CreateHttpClient` | `private static` | `HttpClient` mit `BaseAddress` + 30 s Timeout. |

**Befund:** Der Gate-401-Fall (widerrufenes Geräte-Token) ist nicht vom Bearer-401-Fall unterscheidbar — `SilentReloginAsync` würde bei Gate-Ablehnung ebenfalls `api/auth/login` probieren (das laut Vertrag vom Geräte-Token-Scope `MauiOnly` abgedeckt wird) und bei erneutem 401 `SessionExpired` auslösen → falsche Zielseite (`Login` statt `ServerSetup`).

## `AppUpdate`
Datei: `VideoPlayer.Mvu/AppUpdate.cs` (2103 Zeilen; hier nur der Verbindungs-Workflow)

Reine Update-Funktion `Update(AppModel, AppMessage) → AppTransition` (Switch über ~70 Nachrichtentypen).

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `Update` | `public static` | Zentrale Nachrichtenverteilung; unbekannte Nachrichten → `Unchanged`. |
| `UpdateStartupRequested` | `private static` | Start: `Connecting`/`Checking` + Effekte `InitializeStorage`, `LoadSettings`, `LoadServers`, `StartNetworkMonitoring`, `LoadHomeLists`. |
| `UpdateServersLoaded` | `private static` | Mit aktiver Adresse beim Start → `CheckServerHealthEffect`; ohne Server → `NeedsServerSetup` + `ServerSetup`-Seite. |
| `UpdateAddressSubmitted` / `UpdateServerCandidateSelected` | `private static` | Adresse normalisieren (`ServerAddress.TryNormalize`); ungültig → `InvalidAddressError`; sonst `BeginServerCheck`. |
| `BeginServerCheck` | `private static` | `IsServerCheckRunning = true`, Fehler zurücksetzen, `CheckCandidateServerEffect`. |
| `UpdateServerCheckCompleted` | `private static` | Erfolg → Server in `KnownServers` vorne einordnen, `ActiveServerAddress` setzen, `Connecting`, Effekte `PersistActiveServerEffect` + `AttemptAutoLoginEffect` — **hier fehlt die Pairing-Verzweigung**; Misserfolg → `UnreachableServerError`. |
| `UpdateHealthCheckCompleted` | `private static` | Erreichbar → `AttemptAutoLoginEffect`; nicht erreichbar → `CheckOfflineDataEffect`. |
| `UpdateOfflineDataCheckCompleted` | `private static` | Offline-Daten → `Offline`/Home; sonst → `NeedsServerSetup`/ServerSetup. |
| `UpdateAutoLoginCompleted` / `UpdateLoginSubmitted` / `UpdateLoginCompleted` | `private static` | Erfolg → `EnterConnected`; sonst `NeedsLogin` + Login-Seite + Fehlertext (`LoginErrorFor`). |
| `EnterConnected` | `private static` | `Connected`, Effekte `LoadSources`, `ConnectServerEvents`, `RunFullSync`, `ResumeDownloads`, Hero-Bild; Navigation Home nur von Workflow-Seiten. |
| `UpdateSessionExpired` | `private static` | → `NeedsLogin` + `SessionExpiredError`, `DisconnectServerEventsEffect`, Navigation zur Login-Seite. Muster für den Re-Pairing-Flow, aber anderes Ziel. |
| `GoToServerSetup` | `private static` | Hilfsübergang → `NeedsServerSetup` + ServerSetup-Seite. |
| `UpdateNavigateTo` / `UpdateNavigated` | `private static` | Zustandsgesteuerte Navigation; Menüseiten blockiert, solange `NeedsLogin`/`NeedsServerSetup`. |
| `UpdateLogoutRequested` | `private static` | Nur aus `Connected`/`Offline`; `NeedsLogin` + `LogoutEffect`. |
| Konstanten | `public const` | `InvalidAddressError`, `UnreachableServerError`, `CredentialsRequiredError`, `InvalidCredentialsError`, `LoginUnreachableError`, `LoginFailedError`, `ReloginRequiredNotice`, `SessionExpiredError`, `PlaybackUnavailableError` — Muster für Pairing-Fehlertexte. |

**Befund:** Kein Pairing-Nachrichten-/Effekt-Handling; der Fehlertext-Mechanismus (`ServerSetupError`) existiert und wird von der Seite bereits angezeigt.

## `MvuHost`
Datei: `VideoPlayer.Mvu/MvuHost.cs`

| Methode / Member | Sichtbarkeit | Kurzbeschreibung |
|------------------|-------------|------------------|
| `Model` | `public` | Aktuelles unveränderliches `AppModel`. |
| `ModelChanged` / `PropertyChanged` | `public event` | Nach jedem Update. |
| `DispatchAsync(message, ct)` | `public` | Serialisiert Updates (`SemaphoreSlim gate`) und führt Effekte sequenziell über `IAppEffectHandler` aus. |

## `ShellAppEffectHandler`
Datei: `VideoPlayer.Maui/Services/ShellAppEffectHandler.cs` (1149 Zeilen)

`IAppEffectHandler`-Implementierung am App-Rand; Switch über alle `AppEffect`-Typen.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `HandleAsync(effect, ct)` | `public` | Verteilt Effekte auf private Handler-Methoden. **Kein Pairing-Effekt-Zweig.** |
| `CheckCandidateServerAsync` | `private` | `IServerHealthProbe.CheckAsync(Uri)` → `ServerCheckCompletedMessage`. |
| `PersistActiveServerAsync` | `private` | `KnownServersService.SetActiveServerAsync`. |
| `AttemptAutoLoginAsync` / `AttemptLoginAsync` | `private` | `AuthSessionService.TryStoredLoginAsync`/`LoginAsync` → `AutoLoginCompletedMessage`/`LoginCompletedMessage`. |
| `RunHealthCheckAsync` | `private` | `healthProbe.CheckAsync()` → `ServerHealthCheckCompletedMessage`. |
| `LoadServersAsync` | `private` | `KnownServersService` + `IServerAddressProvider.GetConfiguredServerAddressAsync` → `ServersLoadedMessage`. |
| `OnSessionExpired` / `DispatchSessionExpiredAsync` | `private` | `AuthSessionService.SessionExpired` → `SessionExpiredMessage` über MainThread in den MVU-Zyklus. Muster für ein „Geräte-Token widerrufen"-Ereignis. |
| `DispatchMessageAsync` | `private` | Zentraler Rückdispatch über `MvuHost` auf dem MainThread. |
| `NavigateAsync(page)` | `private` | `ServerSetup`/`Login` ohne Menüeintrag: Window-Root beim Erststart bzw. Shell-Routen `serversetup`/`login`. |

Abonnierte Events: `AuthSessionService.SessionExpired`, `INetworkStatusMonitor.AvailabilityChanged`, `ServerEventsService.EventReceived`/`ConnectionClosed`, `DownloadService.ItemChanged`/`ItemRemoved`.

## `ServerHealthProbe`
Datei: `VideoPlayer.Maui/Services/ServerHealthProbe.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `CheckAsync(ct)` | `public` | Prüft die über `IServerAddressProvider` aufgelöste Adresse. |
| `CheckAsync(baseAddress, ct)` | `public` | Erzeugt kurzlebigen `HttpClient` (10 s Timeout) + `new VideoPlayerApiClient(httpClient)` (**ohne Options** → `ClientGate.Resolve()` im Konstruktor) → `CheckHealthAsync`. |

## `ServerAddressProvider`
Datei: `VideoPlayer.Maui/Services/ServerAddressProvider.cs`

`GetConfiguredServerAddressAsync`: letzter verbundener Server → `VIDEOPLAYER_SERVER_URL` → `null`. `GetServerAddressAsync`: zusätzlich Fallback `http://localhost/videoplayer/`.

## `SecureStorageSecretStore`
Datei: `VideoPlayer.Maui/Services/SecureStorageSecretStore.cs`

`ISecretStore`-Adapter auf `ISecureStorage` (`SecureStorage.Default`): `GetAsync` (Fehler → `null` + Debug-Log), `SetAsync` (leerer Wert → `Remove`), `RemoveAsync`. Direkt für das Geräte-Token nutzbar.

## `PreferencesSettingsStore`
Datei: `VideoPlayer.Maui/Services/PreferencesSettingsStore.cs`

`ISettingsStore`-Adapter auf `IPreferences` — explizit nur für **nicht-geheime** Werte (das Geräte-Token darf hier nicht landen).

## `ServerSetupPage`
Dateien: `VideoPlayer.Maui/Pages/ServerSetupPage.xaml` + `.xaml.cs`

- XAML: Kopfbereich (Titel, `ActiveServerLabel`), `KnownServersView` + `DiscoverButton`/`DiscoveryIndicator`/`DiscoveryEmptyLabel` + `DiscoveredServersView`, Fußbereich mit `AddressEntry` (Keyboard `Url`), `ConnectButton`, `CheckIndicator`, `ErrorLabel`. **Kein Pairing-Code-Eingabefeld.**
- Code-Behind: `UpdateUi(AppModel)` bindet `IsServerCheckRunning`/`ServerSetupError`/Listen; dispatcht `ServerDiscoveryRequestedMessage`, `ServerAddressSubmittedMessage`, `DiscoveredServerSelectedMessage`, `ServerSwitchRequestedMessage`; `OnAppearing` dispatcht `LoadServersRequestedMessage`.

## `LoginPage`
Dateien: `VideoPlayer.Maui/Pages/LoginPage.xaml.cs`

Enthält den Ausstieg zur Servereinrichtung: `OnServerSetupClicked` dispatcht `NavigateToMessage(AppPage.ServerSetup)` — bestehender Pfad, über den auch ein Re-Pairing-Flow die `ServerSetupPage` erreichen kann.

## `AppShell`
Datei: `VideoPlayer.Maui/AppShell.xaml.cs`

Registriert die Shell-Routen `serversetup`, `login`, `mediadetails`, `sourceoverview`, `player` (kein Flyout-Eintrag für Setup/Login); synchronisiert Shell-Navigation als `NavigatedMessage`.

## `MauiProgram`
Datei: `VideoPlayer.Maui/MauiProgram.cs`

DI-Registrierungen (Auswahl, für den Pairing-Flow relevant):

- `IServerAddressProvider` → `ServerAddressProvider`, `IServerHealthProbe` → `ServerHealthProbe`
- `AuthSessionService`, `IAppEffectHandler` → `ShellAppEffectHandler`, `MvuHost`
- `AppProfile.FromEnvironment()`, `ISettingsStore` → `PreferencesSettingsStore(Preferences.Default)`, `ISecretStore` → `SecureStorageSecretStore(SecureStorage.Default)`
- `AppSettingsService`, `AppSecretsService`, `BackupDatabase`, `CacheDatabase`, `KnownServersService`, `ServerDiscoveryService.CreateDefault()`
- Seiten: `ServerSetupPage`, `LoginPage` u. a. als Transient

**Befund:** Keine Registrierung eines Pairing-Diensts, kein `DeviceInfo`-Zugriff im gesamten Projekt (`Microsoft.Maui.Devices.DeviceInfo` wird nirgends verwendet — der Gerätename für `deviceName` ist neu zu ermitteln).

## `MauiClientConfiguration` (Legacy-Altbestand)
Datei: `VideoWebPlayer.Maui/Services/MauiClientConfiguration.cs`

Eigener statischer Gate-Mechanismus der alten App `VideoWebPlayer.Maui`: `X-API-Key` aus `VIDEOWEBPLAYER_MAUI_API_TOKEN` → Assembly-Metadata `VideoWebPlayer.Maui.ApiToken` → Platzhalter (`ApplyClientGateHeader(HttpClient)`; genutzt in `AuthService`, `ConnectionService`, `MauiProgram` des Altbestands). Vom `ClientGate`/`VideoPlayerApiClient` getrennt — die Anforderung betrifft die neue App; der Altbestand bleibt unverändert, sofern er weiter ausgeliefert wird.
