# MVU-Verbindungs-Workflow — Bestandsaufnahme

Alles plattformneutral (`net10.0`). Quellen: `VideoPlayer.Mvu/AppMessage.cs`,
`AppEffect.cs`, `AppModel.cs`, `AppUpdate.cs`, `AppPage.cs`, `AppRoutes.cs`,
`ConnectionState.cs`, `PairingStatus.cs`, `BootstrapStatus.cs`, `AuthAttempt.cs`.

## Modellfelder (`AppModel`, `VideoPlayer.Mvu/AppModel.cs`)

| Feld | Typ | Rolle im Verbindungsfluss |
|------|-----|---------------------------|
| `CurrentPage` | `AppPage` | `Loading`/`ServerSetup`/`Login` = Workflow-Seiten ohne Menü |
| `IsStartupRunning` | `bool` | Start-Workflow läuft |
| `ServerHealth` | `ServerHealthStatus` | `Unknown`/`Checking`/`Reachable`/`Unreachable` |
| `ServerHealthText` | `string` | Anzeigetext des Serverstatus |
| `StartupStatusText` | `string` | Anzeigetext der Ladeanzeige |
| `ConnectionState` | `ConnectionState` | `Unknown`/`Connecting`/`Connected`/`NeedsLogin`/`NeedsServerSetup`/`Offline` |
| `IsOffline` | `bool` | `ConnectionState == Offline` |
| `IsOnline` | `bool` | letzter gemeldeter Netzwerkstatus |
| `IsLoginRunning` | `bool` | Anmeldeversuch läuft |
| `LoginError` | `string?` | Inline-Fehler der Anmeldemaske |
| `LoginUsername` | `string?` | bekannter Benutzername (Vorbefüllung) |
| `HasOfflineData` | `bool` | Offline-Datenbestand vorhanden |
| `IsSyncRunning` | `bool` | Voll-Sync läuft |
| `KnownServers` | `IReadOnlyList<KnownServer>` | bekannte Server (zuletzt verwendet zuerst) |
| `ActiveServerAddress` | `string?` | kanonische Adresse des aktiven Servers |
| `DiscoveredServers` | `IReadOnlyList<DiscoveredServer>` | Erkennungsergebnis |
| `IsDiscoveryRunning` | `bool` | Erkennung läuft |
| `DiscoveryAttempted` | `bool` | Erkennung lief bereits |
| `IsServerCheckRunning` | `bool` | Kandidatenprüfung läuft |
| `ServerSetupError` | `string?` | Fehler der Einrichtung |
| `IsPairingRequired` | `bool` | Server verlangt Geräte-Pairing |
| `IsPairingRunning` | `bool` | Pairing-Austausch läuft |
| `PairingError` | `string?` | Fehler der Pairing-Eingabe |
| `IsBootstrapRunning` | `bool` | Ticket-Prüfung/-Austausch läuft |
| `BootstrapError` | `string?` | Fehler der Kopplungs-Eingabe |
| `StorageNotice` | `string?` | Hinweis der Ablage-Wiederherstellung |

## Vom UI zu dispatchbare Messages (`AppMessage.cs`)

| Message | Effekt/Auswirkung (Update-Handler) |
|---------|----------------------------------|
| `StartupRequestedMessage` | Start-Workflow neu anstoßen (Effekte: `InitializeStorage`, `LoadSettings`, `LoadServers`, `StartNetworkMonitoring`, `LoadHomeLists`) |
| `LoadServersRequestedMessage` | `LoadServersEffect` (Serververwaltung neu laden) |
| `ServerDiscoveryRequestedMessage` | `IsDiscoveryRunning` + `DiscoverServersEffect` |
| `ServerAddressSubmittedMessage(Text)` | Normalisierung → `CheckCandidateServerEffect`; ungültig → `ServerSetupError` |
| `DiscoveredServerSelectedMessage(Address, DisplayName)` | wie Adresseingabe → `CheckCandidateServerEffect` |
| `ServerSwitchRequestedMessage(Address, DisplayName)` | identischer Handler (`UpdateServerCandidateSelected`) — „Server wechseln" |
| `PairingCodeSubmittedMessage(Code)` | Formatprüfe → `ExchangePairingCodeEffect`; Fehler → `PairingError` |
| `BootstrapTicketSubmittedMessage(Text)` | `BootstrapTicketParser`: Ticket-URL → `CheckBootstrapServerEffect`; Kurzcode → `ExchangeBootstrapTicketEffect` (gegen `ActiveServerAddress`); Fehler → `BootstrapError` |
| `LoginSubmittedMessage(Username, Password)` | `AttemptLoginEffect`; leere Felder → `LoginError` |
| `NavigateToMessage(Page)` | zustandsgesteuert; in `NeedsLogin`/`NeedsServerSetup` sind Menüseiten gesperrt, Workflow-Seiten erlaubt |
| `NavigatedMessage(Page)` | native Navigationsbestätigung; in `NeedsLogin`/`NeedsServerSetup` Rücklenkung zur Workflow-Seite |
| `LogoutRequestedMessage` | nur aus `Connected`/`Offline`: `DisconnectServerEventsEffect` + `LogoutEffect`, Modell-Reset, → `AppPage.Login` |

## Ergebnis-Messages des App-Rands (vom Effekt-Handler dispatcht)

`ServersLoadedMessage`, `ServerDiscoveryCompletedMessage`,
`ServerCheckCompletedMessage`, `PairingCompletedMessage`,
`BootstrapServerCheckedMessage`, `BootstrapCompletedMessage`,
`AutoLoginCompletedMessage`, `LoginCompletedMessage`,
`ServerHealthCheckCompletedMessage`, `OfflineDataCheckCompletedMessage`,
`NetworkAvailabilityChangedMessage`, `SessionExpiredMessage`,
`DeviceTokenRevokedMessage`, `ServerEventReceivedMessage`,
`ServerEventsConnectionClosedMessage`, `SynchronizationCompletedMessage`,
`CachedListsChangedMessage`, `HomeListsLoadedMessage`,
`StorageRecoveredMessage`, `SettingsLoadedMessage`.

## Wesentliche Update-Übergänge (`AppUpdate.cs`)

- `UpdateServersLoaded` (Z. 396): aktiver Server → `CheckServerHealthEffect` (nur beim Start); kein Server → `NeedsServerSetup` + `CurrentPage = ServerSetup`.
- `UpdateHealthCheckCompleted` (Z. 598): erreichbar + `PairingRequired` → Einrichtungsseite; erreichbar → `AttemptAutoLoginEffect`; unerreichbar → `CheckOfflineDataEffect`.
- `UpdateOfflineDataCheckCompleted` (Z. 652): Offline-Daten → `Offline` + Home; sonst → `NeedsServerSetup` + `ServerSetup`.
- `UpdateServerCheckCompleted` (Z. 506): geprüfter Server → `PersistActiveServerEffect` + `AttemptAutoLoginEffect`; `PairingRequired` → Einrichtungsseite mit Pairing-Eingabe; Fehler → `ServerSetupError`.
- `UpdatePairingCodeSubmitted`/`UpdatePairingCompleted` (Z. 684/714): Erfolg → `AttemptAutoLoginEffect`; Fehler → `PairingError` (`PairingErrorFor` bildet `PairingStatus` auf Texte ab: ungültig/abgelaufen, Sperrung, nicht unterstützt, unerreichbar, sonstig).
- `UpdateBootstrapTicketSubmitted`/`UpdateBootstrapServerChecked`/`UpdateBootstrapCompleted` (Z. 759/802/851): Ticket-URL → Serverprüfung → `PersistActiveServer` + `ExchangeBootstrapTicket`; Kurzcode → direkter Austausch; Erfolg → `AttemptAutoLoginEffect`; Fehler → `BootstrapError` (`BootstrapErrorFor`).
- `UpdateAutoLoginCompleted` (Z. 938): Erfolg → `EnterConnected`; `GateRejected` → `UpdateDeviceTokenRevoked`; sonst → `NeedsLogin` (+ `LoginError`, bleibt auf `ServerSetup`, wenn dort gestartet).
- `UpdateLoginCompleted` (Z. 999): Erfolg → `EnterConnected`; `GateRejected` → Re-Pairing; sonst `LoginError` (`LoginErrorFor`: abgelehnt, unerreichbar u. a.).
- `UpdateDeviceTokenRevoked` (Z. 899): → `NeedsServerSetup` mit `IsPairingRequired` + `PairingError = DeviceRevokedError`, `DisconnectServerEventsEffect`.
- `UpdateSessionExpired` (Z. 1290): → `NeedsLogin` + `AppPage.Login` + `SessionExpiredError`.
- `EnterConnected` (Z. 1032): `LoadSourcesEffect`, `ConnectServerEventsEffect`, `RunFullSyncEffect`, `ResumeDownloadsEffect`, Hero/Details-Nachladen; von Workflow-Seiten → `AppPage.Home`.
- `UpdateLogoutRequested` (Z. 3548): `DisconnectServerEventsEffect` + `LogoutEffect`, Sitzungs-Reset → `AppPage.Login`.

## Dienste und Fehlerstatus

- `PairingStatus`: `Succeeded`, `InvalidCode`, `RateLimited`, `InvalidRequest`, `NotSupported`, `ServerUnreachable`, `Failed` (`PairingStatus.cs`).
- `BootstrapStatus`: `Succeeded`, `InvalidTicket`, `RateLimited`, `InvalidRequest`, `NotSupported`, `ServerUnreachable`, `Failed`.
- `AuthAttemptStatus` (`AuthAttempt.cs`): `Succeeded`, `InvalidCredentials`, `GateRejected`, `ServerUnreachable`, `MissingCredentials`, `Failed`.
- `AuthSessionService.TryStoredLoginAsync`/`LoginAsync`/`LogoutAsync` (`AuthSessionService.cs`), `DeviceTokenService` (`DeviceTokenService.cs`), `KnownServersService` (`VideoPlayer.Storage`), `AppSecretsService` (`VideoPlayer.Storage`), `ServerDiscoveryService` + `DiscoveredServer` (`VideoPlayer.ApiClient/Discovery`), `DevicePairingClient` + `PairingCrypto` (`VideoPlayer.ApiClient`), `KnownServer` (`VideoPlayer.Storage/KnownServer.cs`, Felder `Address`, `DisplayName`, `LastUsedAtUtc`, `NameOrAddress`).
