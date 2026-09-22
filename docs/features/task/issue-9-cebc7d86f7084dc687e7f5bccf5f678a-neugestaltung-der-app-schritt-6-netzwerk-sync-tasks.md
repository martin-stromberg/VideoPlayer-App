# Tasks: Netzwerkstatus-Erkennung und Synchronisation

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Datenmodell | `PendingProgressSyncEntry.DurationTicks` ergänzen | Offen | — |
| 2 | Datenmodell | `BackupDatabase.InitializeAsync`: Spaltenmigration `DurationTicks` via `PRAGMA table_info` + `ALTER TABLE` | Offen | — |
| 3 | Datenmodell | `PendingProgressSyncService` anlegen (`EnqueueAsync`, `GetPendingAsync` coalesciert/chronologisch, `RemoveForMediaAsync`, `CountAsync`) | Offen | — |
| 4 | Abstraktionen | `INetworkStatusMonitor` in `VideoPlayer.Mvu` anlegen | Offen | — |
| 5 | Abstraktionen | `IServerEventsConnection` + `NewVideosScannedEventArgs` in `VideoPlayer.Mvu` anlegen | Offen | — |
| 6 | Abstraktionen | `ServerEventKind` + `ServerEventReceivedEventArgs` in `VideoPlayer.Mvu` anlegen | Offen | — |
| 7 | Logik | `ServerEventsService` in `VideoPlayer.Mvu` anlegen (idempotenter Connect, Hub-URL, Token-Provider, Event-Übersetzung, `ConnectionClosed`) | Offen | — |
| 8 | Logik | `ProgressReportService` in `VideoPlayer.Mvu` anlegen (Senden, Einreihen bei Offline/Fehler, Verwerfen bei permanentem 4xx) | Offen | — |
| 9 | Logik | `ServerSyncService` + `SyncResult` in `VideoPlayer.Mvu` anlegen (Voll-Sync, Teil-Syncs, Pending-Drain) | Offen | — |
| 10 | MVU | `AppModel` um `IsOnline` und `IsSyncRunning` erweitern | Offen | — |
| 11 | MVU | Neue Messages (`NetworkAvailabilityChanged`, `ServerEventReceived`, `ServerEventsConnectionClosed`, `SynchronizationCompleted`) anlegen | Offen | — |
| 12 | MVU | Neue Effekte (`StartNetworkMonitoring`, `ConnectServerEvents`, `DisconnectServerEvents`, `RunFullSync`, drei Teil-Sync-Effekte) anlegen | Offen | — |
| 13 | MVU | `AppUpdate`-Zweige für die neuen Messages + Navigationsregel „nur von Workflow-Seiten" + erweiterte Login-Effekte | Offen | — |
| 14 | App-Rand | `ConnectivityNetworkStatusMonitor` in `VideoPlayer.Maui` anlegen | Offen | — |
| 15 | App-Rand | `SignalRServerEventsConnection` in `VideoPlayer.Maui` anlegen + Paket `Microsoft.AspNetCore.SignalR.Client` 10.0.10 referenzieren | Offen | — |
| 16 | App-Rand | `ShellAppEffectHandler` um neue Effekt-Zweige und Event-Verdrahtung erweitern | Offen | — |
| 17 | App-Rand | `MauiProgram`: DI-Registrierung der neuen Dienste | Offen | — |
| 18 | Tests | `AppUpdateTests_NetworkSync` anlegen | Offen | — |
| 19 | Tests | `PendingProgressSyncServiceTests` anlegen (reale SQLite-Ablage) | Offen | — |
| 20 | Tests | `LocalDatabaseTests` um Migrations-Test `DurationTicks` erweitern | Offen | — |
| 21 | Tests | `ProgressReportServiceTests` anlegen | Offen | — |
| 22 | Tests | `ServerSyncServiceTests` anlegen | Offen | — |
| 23 | Tests | `ServerEventsServiceTests` anlegen (Fake-Connection-Stub) | Offen | — |
| 24 | Tests | `NetworkSyncContractTests` anlegen (DI, Paketreferenz, Effekt-Zweige) | Offen | — |
| 25 | Tests | `AppUpdateTests_Startup`/`AppUpdateTests_Connection` an neue Effektlisten anpassen | Offen | — |
| 26 | Build | Windows-Build der neuen App + vollständiger Testlauf | Offen | — |
