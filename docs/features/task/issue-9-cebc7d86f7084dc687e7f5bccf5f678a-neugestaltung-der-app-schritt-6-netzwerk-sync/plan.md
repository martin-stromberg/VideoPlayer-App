# Umsetzungsplan: Netzwerkstatus-Erkennung und Synchronisation

## Übersicht

Die App erkennt Online↔Offline-Wechsel zur Laufzeit über eine plattform-
neutrale Abstraktion der MAUI-`Connectivity`-API, führt den Zustand im
MVU-Model (`IsOnline`, `ConnectionState`) und stellt bei Online-Rückkehr
die Verbindung wieder her: bestehende Reconnect-Kette (Health-Check →
stille Anmeldung), erneuter Aufbau der SignalR-Ereignisverbindung
(`hubs/mediaupdate`) sowie eine Synchronisation (Nachsenden der
`pending_progress`-Warteschlange, vollständiger Metadaten-Sync in die
`CacheDatabase`, Aktualisierung der Weiterschauen-Liste). Eingehende
Server-Ereignisse lösen gezielte Teil-Synchronisationen aus, sodass die
betroffenen Anzeigen über die aktualisierten Caches ohne manuelle
Aktualisierung frisch sind.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|------------------|------------|
| Netzwerkstatus | `INetworkStatusMonitor` in `VideoPlayer.Mvu` (`IsOnline`, `AvailabilityChanged`, `Start()`); Implementierung `ConnectivityNetworkStatusMonitor` in `VideoPlayer.Maui` über `Connectivity.Current.NetworkAccess` (`Internet` oder `ConstrainedInternet` = online) und `ConnectivityChanged` | `Connectivity` ist die vorgegebene, naheliegende Mechanik; die Abstraktion hält den MVU-Kern und die Orchestrierung ohne MAUI-Laufzeit testbar (Testprojekt referenziert `VideoPlayer.Maui` nicht). |
| Ereignisverbindung | `IServerEventsConnection` in `VideoPlayer.Mvu` + `ServerEventsService` (Orchestrierung: Hub-URL `{server}hubs/mediaupdate`, Token über `AuthSessionService.Client.CurrentToken`, idempotenter Connect, Übersetzung der Connection-Events in `EventReceived`/`ConnectionClosed`); Implementierung `SignalRServerEventsConnection` in `VideoPlayer.Maui` über `Microsoft.AspNetCore.SignalR.Client` mit `WithAutomaticReconnect()` | Server-Hub existiert (`/hubs/mediaupdate`, JWT-Pflicht, `access_token`-Query wie vom `AccessTokenProvider` erzeugt) — kein Server-Issue nötig. `HubConnection` ist nicht sinnvoll mockbar; die Abstraktion macht die Orchestrierung laufzeittestbar. Muster folgt dem Altbestand (`SignalRService`). |
| Fortschritts-Warteschlange | `PendingProgressSyncService` in `VideoPlayer.Storage` auf `BackupDatabase`/`pending_progress`; `GetPendingAsync` liefert pro Medium (`VideoType`, `VideoId`) nur die neueste Meldung in chronologischer Reihenfolge, `RemoveForMediaAsync` löscht nach erfolgreichem Senden alle Einträge des Mediums | „Sinnvolle Reihenfolge" = chronologisch mit Coalescing pro Medium (der Server speichert nur die letzte Position); Entität bleibt unverändert backup-pflichtig. |
| Fehlende Videodauer | `PendingProgressSyncEntry` erhält `DurationTicks` (`long`, Default 0); `BackupDatabase.InitializeAsync` migriert bestehende Tabellen per `PRAGMA table_info` + `ALTER TABLE … ADD COLUMN` | Die Server-API verlangt `durationSeconds` zwingend; `sqlite-net` ergänzt Spalten bestehender Tabellen nicht automatisch. |
| Senden von Fortschritten | `ProgressReportService` in `VideoPlayer.Mvu`: online + angemeldeter Client → `ReportPlaybackProgressAsync`; offline, ohne Client oder bei Sendefehler → `EnqueueAsync`. Dauerhafte Clientfehler (4xx außer 401/408/429) werden verworfen statt eingereiht (keine „poison pills"); beim Drain gleiche Regel: Transport-/Serverfehler → Abbruch, Einträge bleiben; permanenter 4xx → Eintrag verwerfen, weiter | Die Warteschlange bleibt über Neustarts erhalten und sendet nur überholbare Fehlschläge nach. Aufrufer ist der Player (Schritt 10) — der Dienst legt die verlangte Sammelstrecke jetzt testbar an. |
| Metadaten-Sync | `ServerSyncService` in `VideoPlayer.Mvu`: `SynchronizeAsync` (1. Pending-Drain, 2. `GetSourcesAsync`+`GetRecentEntriesAsync` → `cached_lists`-Schlüssel `sources`/`recent`, 3. `GetContinueWatchingAsync` → `cached_lists`-Schlüssel `continue_watching`, 4. `GetFavoritesAsync` → `cached_favorites` ersetzen) sowie Teil-Methoden `SyncContinueWatchingAsync`, `SyncFavoritesAsync`, `SyncMetadataAsync`; Ergebnis `SyncResult` | „Vollständiger Metadaten-Sync" deckt die vorhandenen Cache-Tabellen ab; Teil-Syncs bilden die Ereignis-Reaktionen ab. Pending-Drain zuerst, damit die anschließend gelesene Weiterschauen-Liste den nachgesendeten Stand enthält. |
| MVU-Erweiterung | Neue Messages/Effekte (s. u.); `AppModel` um `IsOnline` (Start `true`, wird beim Monitoring-Start mit dem Ist-Wert überschrieben) und `IsSyncRunning` | Zustandswechsel laufen ausschließlich über `AppUpdate` (rein, testbar); Seiteneffekte bleiben Effekt-Daten. |
| Navigation bei Wiederverbindung | `AutoLoginCompleted`/`LoginCompleted` (Erfolg) und `OfflineDataCheckCompleted` (mit Daten) navigieren nur noch dann nach `Home`, wenn `CurrentPage` eine Workflow-Seite ist (`Loading`/`Login`/`ServerSetup`); auf Menüseiten bleibt der Anwender stehen | Ein Netzwerk-Flapping darf den Anwender nicht aus der aktuellen Ansicht reißen (Usability). |
| Offline-Anzeige | Vorhandenes `OfflineBanner` der `HomePage` bleibt unverändert; es wird über `model.IsOffline` jetzt auch zur Laufzeit aktualisiert | UI-Konvention des Projekts; kein neues Bedienelement nötig. |

## Programmabläufe

### App-Start (Monitoring anstoßen)

1. `StartupRequestedMessage` → zusätzlich `StartNetworkMonitoringEffect`
   in der Effektliste.
2. `ShellAppEffectHandler` → `INetworkStatusMonitor.Start()` (abonniert
   `ConnectivityChanged`) und dispatcht `NetworkAvailabilityChangedMessage`
   mit dem Ist-Wert.

### Netzwerkverlust im verbundenen Zustand

1. `ConnectivityChanged` (offline) → `AvailabilityChanged(false)` →
   `NetworkAvailabilityChangedMessage(false)`.
2. `AppUpdate`: `IsOnline = false`; nur bei `ConnectionState.Connected`
   zusätzlich `ConnectionState = Offline` + `DisconnectServerEventsEffect`
   (`CurrentPage` unverändert → `OfflineBanner` wird sichtbar).
3. In `Connecting`, `NeedsLogin`, `NeedsServerSetup` oder bereits
   `Offline` ändert sich nur `IsOnline`.

### Online-Rückkehr aus dem Offline-Zustand

1. `NetworkAvailabilityChangedMessage(true)` → `IsOnline = true`; bei
   `ConnectionState.Offline` mit `ActiveServerAddress`: `ConnectionState
   = Connecting`, `ServerHealth = Checking`, Effekt
   `CheckServerHealthEffect`.
2. `ServerHealthCheckCompletedMessage(true)` → `AttemptAutoLoginEffect`
   (Bestand); bei erneuter Nichterreichbarkeit → `CheckOfflineDataEffect`
   → zurück nach `Offline` (Seite bleibt, siehe Designentscheidung).
3. `AutoLoginCompletedMessage(Succeeded)` → `Connected`,
   `IsSyncRunning = true`, Effekte `ConnectServerEventsEffect`,
   `RunFullSyncEffect` (+ `NavigateToPageEffect(Home)` nur von Workflow-
   Seiten).
4. `ConnectServerEventsEffect` → `ServerEventsService.ConnectAsync`
   (SignalR-Hub, Bearer-Token aus dem angemeldeten Client).
5. `RunFullSyncEffect` → `ServerSyncService.SynchronizeAsync` →
   Rückdispatch `SynchronizationCompletedMessage(result)` →
   `IsSyncRunning = false`.

### Server-Ereignis zur Laufzeit

1. `SignalRServerEventsConnection` empfängt `ContinueWatchingUpdated` /
   `FavoritesChanged` / `NewVideosScanned(sourceId, count)` →
   `ServerEventsService.EventReceived` →
   `ServerEventReceivedMessage(kind, sourceId)` (via `MainThread`).
2. `AppUpdate`: nur im Zustand `Connected` → `SyncContinueWatchingEffect`
   / `SyncFavoritesEffect` / `SyncMetadataEffect`; sonst ignoriert.
3. Der Effekt-Handler führt die Teil-Synchronisation aus; die
   aktualisierten Caches stehen den Anzeigen unmittelbar zur Verfügung.
4. `StatusChanged` wird nur protokolliert (keine Anzeige im aktuellen
   Grundgerüst).

### Verlust der Ereignisverbindung

1. SignalR `WithAutomaticReconnect` überbrückt kurze Unterbrechungen;
   endgültiges `Closed` → `ServerEventsService.ConnectionClosed` →
   `ServerEventsConnectionClosedMessage`.
2. `AppUpdate`: bei `Connected` + `IsOnline` + `ActiveServerAddress` →
   `ConnectServerEventsEffect` (erneuter Verbindungsversuch). Schlägt er
   fehl, wird der Fehler protokolliert — die nächste Gelegenheit ist der
   nächste Netzwerkwechsel; keine Wiederholungsschleife.

### Fortschrittsmeldung (Einreihen/Nachsenden)

1. Aufrufer (Player ab Schritt 10) → `ProgressReportService
   .ReportProgressAsync(videoType, videoId, position, duration)`.
2. Online + angemeldeter Client → `ReportPlaybackProgressAsync`
   (Sekunden aus `TimeSpan`); Erfolg → Ende.
3. Offline/kein Client/überholbarer Fehler → `EnqueueAsync` in
   `pending_progress` (übersteht Neustarts, backup-pflichtig).
4. Beim `SynchronizeAsync` → `FlushPendingProgressAsync`: neueste Meldung
   je Medium in chronologischer Reihenfolge senden; bei Erfolg
   `RemoveForMediaAsync`; Transport-/Serverfehler → Abbruch (Rest bleibt);
   permanenter 4xx → verwerfen und weiter.

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `INetworkStatusMonitor` | Interface (`VideoPlayer.Mvu`) | `IsOnline`, `event EventHandler<bool> AvailabilityChanged`, `Start()` |
| `ConnectivityNetworkStatusMonitor` | Klasse (`VideoPlayer.Maui/Services`) | `Connectivity`-Implementierung des Monitors |
| `IServerEventsConnection` | Interface (`VideoPlayer.Mvu`, `IAsyncDisposable`) | `IsConnected`, `ConnectAsync(Uri, Func<Task<string?>>, ct)`, `DisconnectAsync(ct)`; Events `ContinueWatchingUpdated`, `FavoritesChanged`, `NewVideosScanned`, `Closed` |
| `SignalRServerEventsConnection` | Klasse (`VideoPlayer.Maui/Services`) | SignalR-Implementierung (`HubConnection`, `WithAutomaticReconnect`) |
| `NewVideosScannedEventArgs` | EventArgs (`VideoPlayer.Mvu`) | `SourceId`, `Count` |
| `ServerEventKind` | Enum (`VideoPlayer.Mvu`) | `ContinueWatchingUpdated`, `FavoritesChanged`, `NewVideosScanned` |
| `ServerEventReceivedEventArgs` | EventArgs (`VideoPlayer.Mvu`) | `Kind`, `SourceId`, `Count` |
| `ServerEventsService` | Klasse (`VideoPlayer.Mvu`, `IAsyncDisposable`) | Orchestriert Verbindungsauf-/abbau, Token-Bereitstellung, Event-Übersetzung |
| `PendingProgressSyncService` | Klasse (`VideoPlayer.Storage`) | Warteschlange auf `pending_progress` (`EnqueueAsync`, `GetPendingAsync`, `RemoveForMediaAsync`, `CountAsync`) |
| `ProgressReportService` | Klasse (`VideoPlayer.Mvu`) | Fortschrittsmeldung mit Offline-Einreihung |
| `ServerSyncService` | Klasse (`VideoPlayer.Mvu`) | Voll- und Teil-Synchronisation in die `CacheDatabase` |
| `SyncResult` | Record (`VideoPlayer.Mvu`) | `SentProgressReports`, `RemainingProgressReports`, `MetadataSynced`, `ContinueWatchingSynced`, `FavoritesSynced` |
| `NetworkAvailabilityChangedMessage` | Message | Netzwerkstatus-Wechsel (mit Ist-Wert) |
| `ServerEventReceivedMessage` | Message | Eingegangenes Server-Ereignis (`Kind`, `SourceId`) |
| `ServerEventsConnectionClosedMessage` | Message | Ereignisverbindung endgültig getrennt |
| `SynchronizationCompletedMessage` | Message | Voll-Sync abgeschlossen (`SyncResult`) |
| `StartNetworkMonitoringEffect` | Effekt | Monitoring starten (idempotent) |
| `ConnectServerEventsEffect` | Effekt | Ereignisverbindung aufbauen (`ServerAddress`) |
| `DisconnectServerEventsEffect` | Effekt | Ereignisverbindung trennen |
| `RunFullSyncEffect` | Effekt | Vollständige Synchronisation |
| `SyncContinueWatchingEffect` / `SyncFavoritesEffect` / `SyncMetadataEffect` | Effekte | Teil-Synchronisationen |

## Änderungen an bestehenden Klassen

### `AppModel` (`VideoPlayer.Mvu`)

- **Neue Eigenschaften:** `IsOnline` (`bool`, Start `true`) — letzter
  bekannter Netzwerkstatus; `IsSyncRunning` (`bool`) — Voll-Sync läuft.

### `AppUpdate` (`VideoPlayer.Mvu`)

- **Neue Zweige:** `NetworkAvailabilityChangedMessage`,
  `ServerEventReceivedMessage`, `ServerEventsConnectionClosedMessage`,
  `SynchronizationCompletedMessage` (s. Programmabläufe).
- **Geänderte Zweige:** `StartupRequestedMessage` — Effektliste um
  `StartNetworkMonitoringEffect` erweitert; `AutoLoginCompleted`/
  `LoginCompleted` (Erfolg) — `IsSyncRunning = true`, Effekte
  `ConnectServerEventsEffect` + `RunFullSyncEffect`, Navigation nur von
  Workflow-Seiten; `OfflineDataCheckCompleted` (mit Daten) — Navigation
  nur von Workflow-Seiten.
- **Neue Hilfsfunktion:** `IsWorkflowPage(AppPage)` — `Loading`, `Login`,
  `ServerSetup`.

### `PendingProgressSyncEntry` (`VideoPlayer.Storage`)

- **Neue Spalte:** `DurationTicks` (`long`, Default 0).

### `BackupDatabase` (`VideoPlayer.Storage`)

- **Geänderte Methode:** `InitializeAsync` — nach `CreateTableAsync`
  Spaltenmigration `EnsureColumnAsync("pending_progress", "DurationTicks",
  "bigint NOT NULL DEFAULT 0")` via `PRAGMA table_info`.

### `ShellAppEffectHandler` (`VideoPlayer.Maui`)

- **Neue Konstruktor-Abhängigkeiten:** `INetworkStatusMonitor`,
  `ServerEventsService` — Events (`AvailabilityChanged`, `EventReceived`,
  `ConnectionClosed`) werden im Konstruktor abonniert und als Messages in
  den `MvuHost` dispatcht (Muster wie `SessionExpired`).
- **Neue Effekt-Zweige:** `StartNetworkMonitoringEffect` (Monitor starten
  + Ist-Wert dispatcht), `ConnectServerEventsEffect`,
  `DisconnectServerEventsEffect`, `RunFullSyncEffect` (→
  `SynchronizationCompletedMessage`), `SyncContinueWatchingEffect`,
  `SyncFavoritesEffect`, `SyncMetadataEffect`.

### `MauiProgram` (`VideoPlayer.Maui`)

- **Neue Registrierungen:** `INetworkStatusMonitor`,
  `Func<IServerEventsConnection>` (Factory → `SignalRServerEventsConnection`),
  `ServerEventsService`, `PendingProgressSyncService`,
  `ProgressReportService`, `ServerSyncService`.

### `VideoPlayer.Maui.csproj`

- **Neue Paketreferenz:** `Microsoft.AspNetCore.SignalR.Client` 10.0.10
  (gleiche Linie wie die übrigen 10.0.10-Pakete und der Altbestand).

## Datenbankmigrationen

| Migrationsname | Betroffene Tabellen/Spalten | Beschreibung |
|----------------|----------------------------|--------------|
| `pending_progress.DurationTicks` | `pending_progress` + `DurationTicks bigint NOT NULL DEFAULT 0` | `ALTER TABLE` per `PRAGMA table_info`-Prüfung in `BackupDatabase.InitializeAsync`; Neuinstallationen erhalten die Spalte über `CreateTableAsync` |

## Validierungsregeln

Keine — keine neuen Benutzereingaben.

## Konfigurationsänderungen

Keine.

## Seiteneffekte und Risiken

- **Login-Abschluss-Effekte:** `AutoLoginCompleted`/`LoginCompleted`
  (Erfolg) emittieren zusätzliche Effekte und navigieren nur noch von
  Workflow-Seiten — bestehende Tests in `AppUpdateTests_Connection`
  werden angepasst.
- **Startup-Effektliste:** `StartupRequestedMessage` emittiert einen
  zusätzlichen Effekt — `AppUpdateTests_Startup` wird angepasst.
- **Netzwerk-Race beim Start:** `IsOnline = false` vor/abschließendem
  Health-Check stört nicht — der Offline-Übergang greift nur aus
  `Connected`; der Start-Workflow endet ohnehin im Offline-Zweig.
- **SignalR-Paket:** neue NuGet-Abhängigkeit in `VideoPlayer.Maui`
  (wie Altbestand, Version 10.0.10).

## Umsetzungsreihenfolge

1. **`pending_progress`-Erweiterung und Warteschlange**
   - Voraussetzungen: keine (vorhandene `VideoPlayer.Storage`-Basis)
   - `PendingProgressSyncEntry.DurationTicks`,
     `BackupDatabase.InitializeAsync`-Migration,
     `PendingProgressSyncService` neu.

2. **MVU-Abstraktionen und -Dienste**
   - Voraussetzungen: Schritt 1 (`PendingProgressSyncService`)
   - `INetworkStatusMonitor`, `IServerEventsConnection`,
     `NewVideosScannedEventArgs`, `ServerEventKind`,
     `ServerEventReceivedEventArgs`, `ServerEventsService`,
     `ProgressReportService`, `ServerSyncService`, `SyncResult` in
     `VideoPlayer.Mvu`.

3. **MVU-Model, Messages, Effekte, Update-Logik**
   - Voraussetzungen: Schritt 2 (Typen für Signaturen)
   - `AppModel` (`IsOnline`, `IsSyncRunning`), neue Messages/Effekte,
     `AppUpdate`-Zweige und angepasste Navigationsregeln.

4. **App-Rand: Monitor, SignalR, Effekt-Handler, DI**
   - Voraussetzungen: Schritte 2–3; Paket
     `Microsoft.AspNetCore.SignalR.Client` 10.0.10 (in diesem Schritt
     referenziert)
   - `ConnectivityNetworkStatusMonitor`, `SignalRServerEventsConnection`,
     `ShellAppEffectHandler`-Erweiterung, `MauiProgram`-Registrierung,
     csproj-Paketreferenz.

5. **Tests**
   - Voraussetzungen: Schritte 1–4
   - Neue Testklassen (s. u.) und Anpassung betroffener Bestandstests.

6. **Build-Nachweis und Testlauf**
   - Voraussetzungen: Schritte 1–5
   - `dotnet build` der neuen App (Windows-Ziel) und `dotnet test` der
     Testsuite.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft? |
|--------------------|------------|---------------------|
| `StartupRequested_IncludesMonitoringEffect` | `AppUpdateTests_NetworkSync` | `StartNetworkMonitoringEffect` in den Start-Effekten |
| `NetworkLost_FromConnected_EntersOffline*` | `AppUpdateTests_NetworkSync` | `Connected`→`Offline`, `DisconnectServerEventsEffect`, Seite bleibt |
| `NetworkLost_InNeedsLogin_OnlyUpdatesIsOnline` | `AppUpdateTests_NetworkSync` | kein Zustandswechsel außerhalb `Connected` |
| `NetworkRestored_FromOffline_RunsHealthCheck` | `AppUpdateTests_NetworkSync` | `Offline`→`Connecting`/`Checking` + `CheckServerHealthEffect` |
| `NetworkRestored_InNeedsServerSetup_OnlyUpdatesIsOnline` | `AppUpdateTests_NetworkSync` | keine Aktion ohne Server |
| `AutoLoginSucceeded_OnMenuPage_NoForcedNavigation` | `AppUpdateTests_NetworkSync` | `Connected` + `ConnectServerEventsEffect` + `RunFullSyncEffect` + `IsSyncRunning`, ohne `NavigateToPageEffect` |
| `LoginSucceeded_OnLoginPage_NavigatesAndSyncs` | `AppUpdateTests_NetworkSync` | wie oben + `NavigateToPageEffect(Home)` |
| `OfflineDataCheck_OnMenuPage_KeepsPage` | `AppUpdateTests_NetworkSync` | `Offline`, Seite bleibt |
| `ServerEventReceived_MapsToPartialSync` (3 Fälle) | `AppUpdateTests_NetworkSync` | Kind → `SyncContinueWatching`/`SyncFavorites`/`SyncMetadata`-Effekt |
| `ServerEventReceived_WhenNotConnected_Ignored` | `AppUpdateTests_NetworkSync` | keine Effekte |
| `ServerEventsClosed_WhenConnectedOnline_Reconnects` | `AppUpdateTests_NetworkSync` | `ConnectServerEventsEffect` mit aktiver Adresse |
| `SynchronizationCompleted_ClearsSyncFlag` | `AppUpdateTests_NetworkSync` | `IsSyncRunning = false` |
| `Enqueue_*`/`GetPending_Coalesces*`/`RemoveForMedia_*`/`Pending_SurvivesReopen` | `PendingProgressSyncServiceTests` | Warteschlangen-Verhalten inkl. Coalescing, Reihenfolge, Löschung, Neustart-Persistenz (reale SQLite-Datei, `SqliteDatabase`-Collection) |
| `InitializeAsync_AddsDurationTicksToExistingTable` | `LocalDatabaseTests` (Erweiterung) | Migration: altes Schema ohne `DurationTicks` → Spalte ergänzt, Daten erhalten |
| `ReportProgress_Online_SendsDirectly` / `Offline_Enqueues` / `TransportError_Enqueues` / `ClientError_Discards` | `ProgressReportServiceTests` | Sende-/Einreih-/Verwurf-Regeln (`StubHttpMessageHandler`, Fake-Monitor) |
| `SynchronizeAsync_*` (Reihenfolge Pending→Metadaten, Caches befüllt, Fehler bricht Drain ab, kein Client = No-Op) | `ServerSyncServiceTests` | Voll-Sync und Teil-Syncs mit `StubHttpMessageHandler` + realer SQLite-Cache-DB |
| `ConnectAsync_*` (idempotent, Hub-URL, Token-Provider) / `EventReceived_*` / `Disconnect_*` | `ServerEventsServiceTests` | Orchestrierung mit `FakeServerEventsConnection`-Stub |
| `MauiProgram_RegistersSyncServices` / `Csproj_ReferencesSignalRClient` / `EffectHandler_HandlesNewEffects` | `NetworkSyncContractTests` | DI-Registrierung, Paketreferenz und Effekt-Zweige als Quell-Contract |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `AppUpdateTests_Startup.StartupRequested_*` | Effektliste enthält zusätzlich `StartNetworkMonitoringEffect` |
| `AppUpdateTests_Connection.AutoLoginCompleted_Succeeded_*` / `LoginCompleted_Succeeded_*` | Zusätzliche Effekte (`ConnectServerEventsEffect`, `RunFullSyncEffect`), `IsSyncRunning` |
| `AppUpdateTests_Connection.OfflineDataCheckCompleted_WithData_*` | Navigationsverhalten nur von Workflow-Seiten |

### E2E-Tests (primärer Funktionsnachweis)

Es existiert kein E2E-/UI-Automatisierungs-Framework im Repository
(kein FlaUI/Appium/Playwright) — Netzwerkwechsel sind ohnehin keine
direkt auslösbaren UI-Aktionen. Als Funktionsnachweis dienen, wie in den
Schritten 4/5 festgelegt: vollständige Message-Sequenz-Tests über
`AppUpdate`/`MvuHost` (Offline↔Reconnect-Pfade auf Modellebene),
Service-Tests mit `StubHttpMessageHandler`/realer SQLite-Ablage sowie
Contract-Tests für die MAUI-Verdrahtung plus Windows-Build der App.

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| — | — | — | — | Kein E2E-Framework vorhanden; Ersatznachweise s. o. |

## Offene Punkte

Keine.
