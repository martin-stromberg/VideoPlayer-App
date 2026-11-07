# Tasks: Netzwerkstatus-Erkennung und Synchronisation

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Datenmodell | `PendingProgressSyncEntry.DurationTicks` ergänzen | Erledigt | `PendingProgressSyncServiceTests.Enqueue_StoresPositionAndDuration` |
| 2 | Datenmodell | `BackupDatabase.InitializeAsync`: Spaltenmigration `DurationTicks` via `PRAGMA table_info` + `ALTER TABLE` | Erledigt | `LocalDatabaseTests.BackupDatabase_MigratesDurationTicksOntoExistingTable` |
| 3 | Datenmodell | `PendingProgressSyncService` anlegen (`EnqueueAsync` pro Medium neuester Stand, `GetPendingAsync` chronologisch, `RemoveForMediaAsync`, `CountAsync`) | Erledigt | `PendingProgressSyncServiceTests` (5 Tests) |
| 4 | Abstraktionen | `INetworkStatusMonitor` in `VideoPlayer.Mvu` anlegen | Erledigt | indirekt über `ProgressReportServiceTests` (Fake-Monitor) |
| 5 | Abstraktionen | `IServerEventsConnection` + `NewVideosScannedEventArgs` in `VideoPlayer.Mvu` anlegen | Erledigt | `ServerEventsServiceTests` (Fake-Connection) |
| 6 | Abstraktionen | `ServerEventKind` + `ServerEventReceivedEventArgs` in `VideoPlayer.Mvu` anlegen | Erledigt | `ServerEventsServiceTests.EventReceived_TranslatesConnectionEvents` |
| 7 | Logik | `ServerEventsService` in `VideoPlayer.Mvu` anlegen (idempotenter Connect, Hub-URL, Token-Provider, Event-Übersetzung, `ConnectionClosed`) | Erledigt | `ServerEventsServiceTests` (6 Tests) |
| 8 | Logik | `ProgressReportService` in `VideoPlayer.Mvu` anlegen (Senden, Einreihen bei Offline/Fehler, Verwerfen bei permanentem 4xx) | Erledigt | `ProgressReportServiceTests` (5 Tests) |
| 9 | Logik | `ServerSyncService` + `SyncResult` in `VideoPlayer.Mvu` anlegen (Voll-Sync, Teil-Syncs, Pending-Drain) | Erledigt | `ServerSyncServiceTests` (6 Tests) |
| 10 | MVU | `AppModel` um `IsOnline` und `IsSyncRunning` erweitern | Erledigt | `AppUpdateTests_NetworkSync` |
| 11 | MVU | Neue Messages (`NetworkAvailabilityChanged`, `ServerEventReceived`, `ServerEventsConnectionClosed`, `SynchronizationCompleted`) anlegen | Erledigt | `AppUpdateTests_NetworkSync` |
| 12 | MVU | Neue Effekte (`StartNetworkMonitoring`, `ConnectServerEvents`, `DisconnectServerEvents`, `RunFullSync`, drei Teil-Sync-Effekte) anlegen | Erledigt | `AppUpdateTests_NetworkSync` |
| 13 | MVU | `AppUpdate`-Zweige für die neuen Messages + Navigationsregel „nur von Workflow-Seiten" + erweiterte Login-Effekte | Erledigt | `AppUpdateTests_NetworkSync`, `AppUpdateTests_Connection` |
| 14 | App-Rand | `ConnectivityNetworkStatusMonitor` in `VideoPlayer.Maui` anlegen | Erledigt | `NetworkSyncContractTests.NetworkMonitor_UsesMauiConnectivity` |
| 15 | App-Rand | `SignalRServerEventsConnection` in `VideoPlayer.Maui` anlegen + Paket `Microsoft.AspNetCore.SignalR.Client` 10.0.10 referenzieren | Erledigt | `NetworkSyncContractTests` (`Csproj_ReferencesSignalRClient`, `SignalRConnection_MapsServerHubEvents`) |
| 16 | App-Rand | `ShellAppEffectHandler` um neue Effekt-Zweige und Event-Verdrahtung erweitern | Erledigt | `NetworkSyncContractTests.EffectHandler_HandlesAllNetworkSyncEffects` |
| 17 | App-Rand | `MauiProgram`: DI-Registrierung der neuen Dienste | Erledigt | `NetworkSyncContractTests.MauiProgram_RegistersNetworkAndSyncServices` |
| 18 | Tests | `AppUpdateTests_NetworkSync` anlegen | Erledigt | 13 Tests/Theory-Fälle |
| 19 | Tests | `PendingProgressSyncServiceTests` anlegen (reale SQLite-Ablage) | Erledigt | 5 Tests |
| 20 | Tests | `LocalDatabaseTests` um Migrations-Test `DurationTicks` erweitern | Erledigt | 1 Test |
| 21 | Tests | `ProgressReportServiceTests` anlegen | Erledigt | 5 Tests |
| 22 | Tests | `ServerSyncServiceTests` anlegen | Erledigt | 6 Tests |
| 23 | Tests | `ServerEventsServiceTests` anlegen (Fake-Connection-Stub) | Erledigt | 6 Tests |
| 24 | Tests | `NetworkSyncContractTests` anlegen (DI, Paketreferenz, Effekt-Zweige) | Erledigt | 5 Tests |
| 25 | Tests | `AppUpdateTests_Startup`/`AppUpdateTests_Connection`/`MvuHostTests` an neue Effektlisten anpassen | Erledigt | bestehen alle |
| 26 | Build | Windows-Build der neuen App + vollständiger Testlauf | Erledigt | Build 0 Fehler/0 Warnungen; `dotnet test` (Release + Coverage): 324/324 bestanden — `final-dotnet-test.log` |
