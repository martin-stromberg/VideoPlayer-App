# Plan-Review

Geprüft: `plan.md` gegen die Implementierung (Selbstprüfung ohne Unteragenten —
dokumentierte Abweichung, es stehen keine Unteragenten zur Verfügung).
Alle Planelemente wurden im Code gesucht und vollständig gelesen.

## Ergebnis

**Status:** Vollständig umgesetzt

## Umgesetzte Planelemente

### Projekt `VideoPlayer.Mvu` (net10.0)

- [x] `INetworkStatusMonitor` — `IsOnline`, `event EventHandler<bool>
  AvailabilityChanged`, idempotentes `Start()`.
- [x] `IServerEventsConnection` (`IAsyncDisposable`) — `IsConnected`,
  `ConnectAsync(Uri, Func<Task<string?>>, ct)`, `DisconnectAsync(ct)`;
  Events `ContinueWatchingUpdated`, `FavoritesChanged`, `NewVideosScanned`,
  `StatusChanged`, `Closed`.
- [x] `NewVideosScannedEventArgs` — `SourceId`, `Count`.
- [x] `ServerEventKind` — `ContinueWatchingUpdated`, `FavoritesChanged`,
  `NewVideosScanned`; plus `ServerEventReceivedEventArgs` (`Kind`,
  `SourceId`, `Count`).
- [x] `ServerEventsService` — Orchestrierung: Hub-URL
  `{server}hubs/mediaupdate` (`HubPath`-Konstante), Token-Provider aus
  `AuthSessionService.Client.CurrentToken`, idempotenter Connect (gleicher
  Server → No-Op), Serverwechsel → Trennen + Neuaufbau, Übersetzung in
  `EventReceived`/`ConnectionClosed`, Semaphore-serialisierter
  Auf-/Abbau, `IAsyncDisposable`.
- [x] `ProgressReportService` — online + angemeldeter Client →
  `ReportPlaybackProgressAsync` (Sekunden aus `TimeSpan`); offline, ohne
  Client oder bei überholbarem Fehler → `EnqueueAsync`; permanente
  Clientfehler (4xx außer 401/408/429) verworfen
  (`IsPermanentClientError`).
- [x] `ServerSyncService` + `SyncResult` — `SynchronizeAsync` (1.
  Pending-Drain, 2. Quellen + „Neu im Programm" → `cached_lists`
  `sources`/`recent`, 3. Weiterschauen → `continue_watching`, 4.
  Favoriten → `cached_favorites` ersetzen) sowie `SyncMetadataAsync`,
  `SyncContinueWatchingAsync`, `SyncFavoritesAsync`,
  `FlushPendingProgressAsync` (Transport-/Serverfehler → Abbruch,
  Einträge bleiben; permanenter 4xx → verwerfen und weiter).
- [x] `AppModel` — `IsOnline` (Start `true`), `IsSyncRunning`.
- [x] Messages — `NetworkAvailabilityChangedMessage`,
  `ServerEventReceivedMessage`, `ServerEventsConnectionClosedMessage`,
  `SynchronizationCompletedMessage`.
- [x] Effekte — `StartNetworkMonitoringEffect`,
  `ConnectServerEventsEffect`, `DisconnectServerEventsEffect`,
  `RunFullSyncEffect`, `SyncContinueWatchingEffect`,
  `SyncFavoritesEffect`, `SyncMetadataEffect`.
- [x] `AppUpdate` — `StartupRequestedMessage` emittiert zusätzlich
  `StartNetworkMonitoringEffect`; `EnterConnected` (Auto- und manueller
  Login, Erfolg) setzt `Connected`, `IsOnline`, `IsSyncRunning` und
  emittiert `ConnectServerEventsEffect` + `RunFullSyncEffect` +
  `NavigateToPageEffect(Home)` nur von Workflow-Seiten;
  `IsWorkflowPage`-Hilfsfunktion (`Loading`, `Login`, `ServerSetup`);
  Offline-Übergang nur aus `Connected` (mit `DisconnectServerEventsEffect`);
  Online-Rückkehr aus `Offline` mit aktivem Server → `Connecting` +
  `CheckServerHealthEffect`; Server-Ereignisse nur in `Connected` →
  gezielte Teil-Sync-Effekte; `ServerEventsConnectionClosedMessage` →
  erneuter `ConnectServerEventsEffect` bei `Connected` + online +
  aktivem Server; `OfflineDataCheckCompleted` (mit Daten) navigiert nur
  von Workflow-Seiten.
- [x] Ergänzend zum Plan: `SessionExpiredMessage` trennt jetzt auch die
  Ereignisverbindung (`DisconnectServerEventsEffect`) — ein abgelaufenes
  Token kann den Hub ohnehin nicht mehr autorisieren.

### Projekt `VideoPlayer.Storage`

- [x] `PendingProgressSyncEntry.DurationTicks` (`long`, dokumentiert).
- [x] `BackupDatabase.InitializeAsync` — `EnsureColumnAsync` migriert
  `DurationTicks` per `PRAGMA table_info` + `ALTER TABLE … ADD COLUMN
  bigint NOT NULL DEFAULT 0` in bestehende Datenbanken (idempotent).
- [x] `PendingProgressSyncService` — `EnqueueAsync`, `GetPendingAsync`
  (chronologisch), `RemoveForMediaAsync`, `CountAsync`.
  Abweichung vom Plan als Verbesserung: `EnqueueAsync` hält pro Medium
  nur den neuesten Stand (transaktional `DELETE`+`INSERT` statt Anhäufen
  + Coalescing beim Lesen) — die Warteschlange bleibt klein, die
  Semantik („neueste Meldung je Medium, älteste zuerst") ist identisch.

### Projekt `VideoPlayer.Maui`

- [x] `ConnectivityNetworkStatusMonitor` — `Connectivity.Current
  .NetworkAccess` (`Internet`/`ConstrainedInternet` = online),
  `ConnectivityChanged`-Abonnement in `Start()` (idempotent).
- [x] `SignalRServerEventsConnection` — `HubConnection` mit
  `AccessTokenProvider` und `WithAutomaticReconnect`; Mapping der
  Server-Events `ContinueWatchingUpdated`, `FavoritesChanged`,
  `NewVideosScanned(sourceId, count)`, `StatusChanged`; `Closed` →
  `Closed`-Event; gescheiterte `StartAsync`-Versuche werden disposed.
- [x] `ShellAppEffectHandler` — neue Konstruktor-Abhängigkeiten
  `INetworkStatusMonitor` + `ServerEventsService` (Events im Konstruktor
  abonniert, Dispatch über `MainThread` wie bei `SessionExpired`); neue
  Effekt-Zweige für alle sieben Effekte; `RunFullSyncEffect` dispatcht
  `SynchronizationCompletedMessage` auch im Fehlerfall (Sync-Flag
  kippt zuverlässig zurück).
- [x] `MauiProgram` — Registrierungen `INetworkStatusMonitor`,
  `Func<IServerEventsConnection>`-Factory, `ServerEventsService`,
  `PendingProgressSyncService`, `ProgressReportService`,
  `ServerSyncService`.
- [x] `VideoPlayer.Maui.csproj` — `Microsoft.AspNetCore.SignalR.Client`
  10.0.10.

### Tests (`VideoWebPlayer.Maui.Tests`)

- [x] `AppUpdateTests_NetworkSync` — alle geplanten MVU-Übergänge
  (Monitoring-Effekt beim Start, Offline-Übergang, Online-Rückkehr,
  Ereignis-Mapping, Reconnect nach `Closed`, Sync-Flag, Sitzungsablauf).
- [x] `PendingProgressSyncServiceTests` — Einreihen, Coalescing,
  Reihenfolge, Löschung je Medium, Persistenz über Datenbank-Reopen
  (reale SQLite-Datei, `SqliteDatabase`-Collection).
- [x] `LocalDatabaseTests` — `BackupDatabase_MigratesDurationTicksOnto
  ExistingTable` (altes Schema ohne Spalte → Migration + Idempotenz).
- [x] `ProgressReportServiceTests` — Direkt-Senden (Payload-Prüfung),
  Offline-Einreihung, fehlender Client, Transportfehler → Einreihung,
  permanenter 4xx → Verwurf.
- [x] `ServerSyncServiceTests` — Voll-Sync inkl. Reihenfolge
  (Fortschritte vor Metadaten), Cache-Befüllung, No-Op ohne Client,
  Abbruch bei Transportfehler, Verwurf bei permanentem Fehler,
  Teilfehler-Reporting, gezielter Weiterschauen-Sync.
- [x] `ServerEventsServiceTests` — Hub-URL + Token-Provider,
  idempotenter Connect, Serverwechsel, Event-Übersetzung,
  `Closed` → `ConnectionClosed` + Reconnect-Möglichkeit,
  Disconnect/Dispose.
- [x] `NetworkSyncContractTests` — DI-Registrierung, Paketreferenz,
  Effekt-Zweige, SignalR-Event-Mapping, Connectivity-Nutzung.
- [x] Bestandstests angepasst: `AppUpdateTests_Startup` (4. Effekt),
  `AppUpdateTests_Connection` (Login-/AutoLogin-/SessionExpired-
  Effektlisten), `MvuHostTests` (Effektkette 6 → 9).

## Abweichungen vom Plan

1. `EnqueueAsync` schreibt nur einen Eintrag pro Medium (transaktional),
   statt Duplikate beim Lesen zu coalescen — siehe oben.
2. `SessionExpiredMessage` trennt zusätzlich die Ereignisverbindung.
3. `EnterConnected` setzt zusätzlich `IsOnline = true` (eine erfolgreiche
   Anmeldung beweist Konnektivität).

## Offene Punkte

Keine.
