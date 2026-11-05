# Logik — Bestandsaufnahme (Schritt 2)

## `MvuHost`
`VideoPlayer.Mvu/MvuHost.cs` — treibt den MVU-Zyklus.

| Member | Sichtbarkeit | Kurzbeschreibung |
|---|---|---|
| `MvuHost(IAppEffectHandler, Func<AppModel,AppMessage,AppTransition>?)` | public | Effekt-Handler Pflicht; Update-Funktion optional (`AppUpdate.Update` Default) |
| `Model` | public get | Aktuelles unveränderliches `AppModel` (Start: `new()`) |
| `ModelChanged` | public event | Nach jedem Update |
| `DispatchAsync(AppMessage, CancellationToken)` | public | Serialisiert per `SemaphoreSlim`-Gate: Update → Model tauschen → Events → Effekte sequenziell an `IAppEffectHandler` |

Events: `PropertyChanged` (`INotifyPropertyChanged`), `ModelChanged`.

## `AppUpdate`
`VideoPlayer.Mvu/AppUpdate.cs` (4448 Zeilen) — statische reine Update-Funktion
`Update(AppModel, AppMessage) → AppTransition`. Start-relevante Pfade:

| Nachricht | Reaktion (Startzustand) |
|---|---|
| `StartupRequestedMessage` | `IsStartupRunning=true`, `ServerHealth=Checking`, `ConnectionState=Connecting`; Effekte `[InitializeStorage, LoadSettings, LoadServers, StartNetworkMonitoring, LoadHomeLists]` (Update ~Z. 330–345) |
| `ServersLoadedMessage` (aktiver Server) | `CheckServerHealthEffect`; ohne Server → `NeedsServerSetup` + `NavigateToPageEffect(ServerSetup)` (~Z. 396–437) |
| `ServerHealthCheckCompletedMessage` | erreichbar → `AttemptAutoLoginEffect`; unerreichbar → `CheckOfflineDataEffect` |
| `OfflineDataCheckCompletedMessage` | Offline-Daten → `Offline`+`NavigateToPageEffect(Home)`; sonst `NeedsServerSetup`/`NeedsLogin` |
| `AutoLoginCompletedMessage` | Erfolg → `Connected` + `ConnectServerEventsEffect` + `RunFullSyncEffect` + `NavigateToPageEffect(Home)` (~Z. 1054–1077); Misserfolg → `NeedsLogin` + `NavigateToPageEffect(Login)` |
| `NavigateToMessage`/`NavigatedMessage` | Menü-Navigation → `NavigateToPageEffect`; `Navigated` synchronisiert `CurrentPage` (~Z. 4405–4443) |
| `NetworkAvailabilityChangedMessage` | `IsOnline`-Wechsel; Offline-Übergang → `DisconnectServerEventsEffect`, Online-Rückkehr → Sync |

## `ShellAppEffectHandler`
`VideoPlayer.Maui/Services/ShellAppEffectHandler.cs` (2396 Zeilen) — MAUI-
Referenzimplementierung des `IAppEffectHandler`; Vorlage der tvOS-Seite.

Start-/Verbindungs-relevante Handler (Verhalten, das tvOS spiegeln muss):

| Methode | Zeile | Verhalten |
|---|---|---|
| `InitializeStorageAsync` | ~1837 | Layout auflösen (legt Verzeichnisse an) + `BackupDatabase`/`CacheDatabase.InitializeWithRecoveryAsync`; Fehler nicht fatal → `StorageRecoveredMessage` bei Recovery/Fehlschlag |
| `LoadSettingsAsync` | ~1885 | `AppSettingsService.Load()` (Fehler → Defaults) → `appLog.SetEnabled` → `SettingsLoadedMessage` |
| `SaveSettings` | ~1905 | `AppSettingsService.Save` + `appLog.SetEnabled` |
| `LoadServersAsync` | ~1920 | `MigrateLegacyServerAddressAsync` + `GetAllAsync` + `GetConfiguredServerAddressAsync`; `DeviceTokenService.LoadForServerAsync`; `IsPairingRequiredAsync` → `ServersLoadedMessage` |
| `RunHealthCheckAsync` | ~2035 | `IServerHealthProbe.CheckAsync`; `GateRejected` → `DiscardCurrentAsync` + einmalige Nachprüfung → ggf. `NotifyRejected` (→ `DeviceTokenRevokedMessage`); sonst `ServerHealthCheckCompletedMessage(reachable, pairingRequired)` |
| `CheckOfflineDataAsync` | ~1795 | `CacheDatabase.HasOfflineDataAsync` (Fehler → `false`) → `OfflineDataCheckCompletedMessage` |
| `StartNetworkMonitoringAsync` | ~531 | `INetworkStatusMonitor.Start()` → Ist-Wert als `NetworkAvailabilityChangedMessage` |
| `ConnectServerEventsAsync`/`DisconnectServerEventsAsync` | ~547/563 | `ServerEventsService.Connect/Disconnect`; Fehler geloggt, nicht fatal |
| `RunFullSyncAsync` | ~577 | `ServerSyncService.SynchronizeAsync` (Fehler → leeres `SyncResult`) → `SynchronizationCompletedMessage` |
| `LoadHomeListsAsync` | ~649 | offline → `OfflineContinueWatchingService`, sonst `HomeListService` → `HomeListsLoadedMessage` |
| `AttemptAutoLoginAsync`/`AttemptLoginAsync` | ~1755/1775 | `AuthSessionService.TryStoredLoginAsync`/`LoginAsync` → `AutoLoginCompletedMessage`/`LoginCompletedMessage` |
| `LoadServersAsync`-Teilmenge `IsPairingRequiredAsync` | ~2095 | `HasDeviceTokenAsync` oder `ClientGate.ResolveStatic()==Placeholder` |
| `NavigateAsync` | ~2336 | `ServerSetup`/`Login` = Window-Root-Wechsel bzw. Shell-Route; Menüseiten → `AppShell` + `//{route}` |
| `DispatchMessageAsync` | ~1742 | `MainThread.InvokeOnMainThreadAsync(host.DispatchAsync)` — Messages zurück in den MVU-Zyklus auf dem UI-Thread |
| Ereignis-Verdrahtung (Konstruktor) | ~78–96 | `authSession.SessionExpired` → `SessionExpiredMessage`; `deviceTokens.DeviceTokenRejected` → `DeviceTokenRevokedMessage`; `networkMonitor.AvailabilityChanged` → `NetworkAvailabilityChangedMessage`; `serverEvents.EventReceived`/`ConnectionClosed` → `ServerEventReceivedMessage`/`ServerEventsConnectionClosedMessage`; `downloads.ItemChanged/ItemRemoved` (tvOS: entfällt — Downloads nicht verdrahtet) |

Nicht für tvOS verdrahtete Effekt-Zweige (Downloads, Playlist-Verwaltung,
Wiedergabe/PiP, Protokollversand, Weiterschauen-Mutationen, Detail-/Quellen-
Nachladeeffekte) laufen im `switch` ohne `default`-Fall — fehlerfrei.

## `MauiProgram`
`VideoPlayer.Maui/MauiProgram.cs` — DI-Komposition (Referenz der zu
spiegelnden Verdrahtung): `IServerAddressProvider`→`ServerAddressProvider`,
`IServerHealthProbe`→`ServerHealthProbe`, `DevicePairingClient`,
`DeviceTokenService`, `AuthSessionService`, `IAppEffectHandler`→
`ShellAppEffectHandler`, `MvuHost`; `AppProfile.FromEnvironment`,
`ISettingsStore`→`PreferencesSettingsStore(Preferences.Default)`,
`ISecretStore`→`SecureStorageSecretStore(SecureStorage.Default)`,
`AppSettingsService`, `AppSecretsService`, `AppStorageLayoutProvider.
CreateLayout`, `AppLogService`+`FileLoggerProvider`+`AddLogging`,
`BackupDatabase`, `CacheDatabase`, `KnownServersService`,
`ServerDiscoveryService.CreateDefault()`, `INetworkStatusMonitor`→
`ConnectivityNetworkStatusMonitor`, `Func<IServerEventsConnection>`→
`SignalRServerEventsConnection`, `ServerEventsService`,
`PendingProgressSyncService`, `ProgressReportService`, `ServerSyncService`,
`HomeListService`, `TimeProvider`, `OfflineContinueWatchingService`,
weitere (spätere Schritte: `SourceBrowserService`, `MediaDetailsService`,
`FavoritesService`, `PlaylistService`, `ProfileInfoService`,
`PlaybackSourceService`, `UpcomingEpisodesService`,
`ContinueWatchingMutationService`, `DownloadService`, `AutoDownloadService`).

## tvOS-Einstieg (Schritt 1, zu erweitern)

| Datei | Stand |
|---|---|
| `VideoPlayer.TvOS/Program.cs` | `UIApplication.Main(args, null, typeof(AppDelegate))` |
| `VideoPlayer.TvOS/AppDelegate.cs` | `FinishedLaunching`: `UIWindow` + `MainViewController`, `MakeKeyAndVisible` — keine Komposition, kein MVU |
| `VideoPlayer.TvOS/MainViewController.cs` | schwarzer Hintergrund + `UILabel` „Heimkino TV" |

## Dienste im Start-/Verbindungspfad (Konstruktor-Signaturen)

| Dienst | Assembly | Abhängigkeiten |
|---|---|---|
| `AppSettingsService` | Storage | `ISettingsStore`, `AppProfile` |
| `AppSecretsService` | Storage | `ISettingsStore`, `ISecretStore`, `AppProfile` |
| `AppStorageLayout` | Storage | `backupRoot`, `noBackupRoot`, `cacheRoot`, `AppProfile` |
| `AppLogService` | Storage | `AppStorageLayout`, `bool enabled` |
| `BackupDatabase`/`CacheDatabase` | Storage | `AppStorageLayout` (+ Logger; `SqliteBootstrap` lazy) |
| `KnownServersService` | Storage | `BackupDatabase`, `ISettingsStore`, `AppProfile` |
| `PendingProgressSyncService` | Storage | `BackupDatabase` (+ Logger) |
| `DeviceTokenService` | Mvu | `AppSecretsService` (+ HttpClient-Fabrik, Logger) |
| `AuthSessionService` | Mvu | `AppSecretsService`, `DeviceTokenService` (+ HttpClient-Fabrik, LoggerFactory); Ereignis `SessionExpired`, Property `Client` |
| `ServerEventsService` | Mvu | `AuthSessionService`, `Func<IServerEventsConnection>` (+ Logger); Ereignisse `EventReceived`, `ConnectionClosed` |
| `ServerSyncService` | Mvu | `AuthSessionService`, `PendingProgressSyncService`, `CacheDatabase` (+ Logger) |
| `ProgressReportService` | Mvu | `AuthSessionService`, `PendingProgressSyncService`, `INetworkStatusMonitor` (+ Logger) |
| `HomeListService` | Mvu | `CacheDatabase` (+ Logger, `TimeProvider`) |
| `OfflineContinueWatchingService` | Mvu | `CacheDatabase`, `BackupDatabase`, `HomeListService`, `AppStorageLayout` (+ `Func<string,bool>`, Logger) |
| `DevicePairingClient` | ApiClient | `HttpClient` (Timeout `RequestTimeout`); `ExchangeAsync`/`BootstrapAsync` |
| `ServerDiscoveryService` | ApiClient | `CreateDefault()` (mDNS via Zeroconf + UDP `:5001`) |
| `ClientGate` | ApiClient | statisch: `DeviceTokenProvider`, `ResolveStatic()`, `ApiTokenPlaceholder` |

## `SignalRServerEventsConnection` / `ServerHealthProbe` / `ServerAddressProvider`
`VideoPlayer.Maui/Services/` — faktisch plattformneutral (Imports:
`Microsoft.AspNetCore.SignalR.Client` bzw. ApiClient/Storage/Logging), aber
nicht für tvOS referenzierbar, da `VideoPlayer.Maui` ein MAUI-Projekt ist.
