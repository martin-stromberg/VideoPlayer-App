# Code-Review

Geprüft werden alle neuen/geänderten Quelldateien gegenüber dem Basisbranch
`task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app`
(Selbstprüfung ohne Unteragenten — dokumentierte Abweichung).

## Geänderte/neue Quelldateien

### `VideoPlayer.Mvu`

- `INetworkStatusMonitor.cs` — minimale Abstraktion (`IsOnline`,
  `AvailabilityChanged`, idempotentes `Start()`); hält den Kern
  plattformneutral und testbar.
- `IServerEventsConnection.cs` — Verbindungsabstraktion inkl.
  `NewVideosScannedEventArgs`; dokumentiert, dass kurze Unterbrechungen
  die Implementierung selbst überbrückt (`WithAutomaticReconnect`) und
  `Closed` nur die endgültige Trennung meldet.
- `ServerEventKind.cs` — Enum + `ServerEventReceivedEventArgs`.
- `ServerEventsService.cs` — Orchestrierung mit Semaphore-serialisiertem
  Auf-/Abbau; idempotenter Connect pro Serveradresse; Token-Provider
  liest `authSession.Client?.CurrentToken` bei jedem (Neu-)Verbinden;
  Hub-Pfad als `HubPath`-Konstante. `OnClosed` räumt nur bei der
  aktuellen Verbindung auf (`ReferenceEquals`-Guard gegen nachgereichte
  Events ersetzter Verbindungen) und disposed die getrennte Verbindung;
  gescheiterte `ConnectAsync`-Versuche disposen die neu erzeugte
  Verbindung. Event-Invokes sind gegen Handler-Fehler abgesichert.
- `ProgressReportService.cs` — Direktversand nur bei vorhandenem Client
  **und** `IsOnline`; überholbare Fehler (Transport, Timeout, 5xx, 401
  nach ausgeschöpfter Erneuerung, 408/429) → Einreihung; permanente 4xx
  → Verwurf (`IsPermanentClientError`, `internal` für Tests). Keine
  „poison pills" in der Warteschlange.
- `ServerSyncService.cs` — `SynchronizeAsync` sendet wartende
  Fortschritte **vor** den Metadaten (die danach gelesene
  Weiterschauen-Liste enthält den nachgesendeten Stand); Drain bricht
  bei Transport-/Serverfehlern ab, ohne Einträge zu löschen; permanente
  4xx verwerfen nur den betroffenen Eintrag. Teil-Syncs schreiben in die
  vorhandenen Cache-Tabellen (`cached_lists` per `InsertOrReplace`,
  `cached_favorites` als vollständiger Ersatz); `TrySyncAsync` kapselt
  Fehlerbehandlung (Abbruch nur bei `OperationCanceledException`);
  `MediaTypeOf` bildet die `MediaEntry`-Hierarchie auf die
  Server-Typnamen ab (`movie`, `tvshowepisode`, …).
- `AppModel.cs` — `IsOnline` (Start `true`, wird beim Monitoring-Start
  mit dem Ist-Wert überschrieben), `IsSyncRunning`.
- `AppMessage.cs`/`AppEffect.cs` — vier Messages / sieben Effekte als
  `sealed record`, konsistent zum Bestand.
- `AppUpdate.cs` — erschöpfende Behandlung der neuen Messages;
  `EnterConnected` bündelt den Übergang in den verbundenen Zustand
  (Ereignisverbindung + Voll-Sync + Workflow-seitenabhängige
  Navigation); Offline-Übergang nur aus `Connected`;
  Online-Rückkehr aus `Offline` startet die bekannte
  Health-Check→Login-Kette; Server-Ereignisse wirken nur in
  `Connected`; `SessionExpired` trennt die Ereignisverbindung;
  `IsWorkflowPage` verhindert Navigationssprünge bei
  Wiederverbindungen zur Laufzeit.

### `VideoPlayer.Storage`

- `Entities/PendingProgressSyncEntry.cs` — `DurationTicks` ergänzt
  (Server-API verlangt die Dauer je Meldung).
- `BackupDatabase.cs` — `EnsureColumnAsync` migriert fehlende Spalten
  per `PRAGMA table_info` + `ALTER TABLE`; `TableInfoRow` mappt die
  Spalte `name` des PRAGMA-Ergebnisses (sqlite-net mappt
  kleingeschrieben auf die Property).
- `PendingProgressSyncService.cs` — `EnqueueAsync` ersetzt den Stand
  eines Mediums transaktional (`DELETE` + `INSERT` in
  `RunInTransactionAsync`) — pro Medium nur der neueste Fortschritt;
  `GetPendingAsync` liefert chronologisch; `RemoveForMediaAsync`
  löscht alle Einträge des Mediums nach erfolgreichem Senden.

### `VideoPlayer.Maui`

- `Services/ConnectivityNetworkStatusMonitor.cs` — `NetworkAccess
  .Internet`/`ConstrainedInternet` = online; `Start()` abonniert
  `ConnectivityChanged` einmalig.
- `Services/SignalRServerEventsConnection.cs` — `HubConnection` mit
  `AccessTokenProvider` (Token wird vom Transport je Verbindung neu
  abgefragt — passt zur stillen Token-Erneuerung) und
  `WithAutomaticReconnect`; alle vier Server-Events gemappt; `Closed`
  meldet die endgültige Trennung; gescheitertes `StartAsync` disposed
  die Verbindung.
- `Services/ShellAppEffectHandler.cs` — neue Abhängigkeiten und sieben
  neue Effekt-Zweige; alle Netzwerk-/Ereignis-Callbacks dispatch
  Messages über `MainThread.InvokeOnMainThreadAsync` in den MVU-Zyklus
  (gleiches Muster wie `SessionExpired`); `RunFullSyncEffect` dispatcht
  `SynchronizationCompletedMessage` auch im Fehlerfall, damit
  `IsSyncRunning` zuverlässig zurückkippt; Fehler in Teil-Syncs und
  Ereignisverbindung werden protokolliert, nicht weitergegeben
  (kein Absturzpfad).
- `MauiProgram.cs` — Registrierung aller neuen Dienste als Singletons;
  `Func<IServerEventsConnection>`-Factory, damit jede Verbindung eine
  frische `HubConnection` erhält.
- `VideoPlayer.Maui.csproj` — `Microsoft.AspNetCore.SignalR.Client`
  10.0.10 (Linie der übrigen 10.0.10-Pakete, wie Altbestand).

### `VideoWebPlayer.Maui.Tests`

- `AppUpdateTests_NetworkSync.cs` — alle neuen Übergänge.
- `PendingProgressSyncServiceTests.cs` — reale SQLite-Warteschlange
  (Collection `SqliteDatabase` serialisiert den Zugriff).
- `ProgressReportServiceTests.cs`, `ServerSyncServiceTests.cs` —
  `StubHttpMessageHandler` mit aufgezeichneten Requests, echte
  `AuthSessionService`-Anmeldung, reale SQLite-Dateien.
- `ServerEventsServiceTests.cs` — `FakeServerEventsConnection`-Stub.
- `NetworkSyncContractTests.cs` — Quell-Contract der MAUI-Verdrahtung.
- `LocalDatabaseTests.cs` — Migrations-Test des alten Schemas.
- `AppUpdateTests_Startup.cs`, `AppUpdateTests_Connection.cs`,
  `MvuHostTests.cs` — an die verlängerten Effektlisten angepasst.

## Geprüfte Stil- und Qualitätspunkte

- Alle neuen Typen/Member tragen deutsche XML-Dokumentation
  (Projektkonvention).
- Keine Geheimnisse im Model: das Bearer-Token bleibt im
  `AuthSessionService`/`VideoPlayerApiClient` und wird der
  Ereignisverbindung nur als Provider-Funktion übergeben.
- Keine UI-Aktion ohne Handler: alle `Clicked`-Verdrahtungen haben
  Code-Behind-Handler; es gibt keine `RaiseUiActionRequested`-Aktionen
  in dieser Implementierung (das Muster existiert in der MAUI-App
  nicht — Blazor-bezogene Prüfung entfällt).
- Lebensdauer: `ServerEventsService` ist `IAsyncDisposable` und wird
  als Singleton gehalten; Verbindungen werden bei Trennung, `Closed`
  und Connect-Fehlschlag disposed.
- Datenverlust-Freiheit: Warteschlangen-Einträge werden erst nach
  erfolgreichem Senden entfernt; Abbruch bei Transportfehler lässt den
  Rest stehen; `pending_progress` liegt in der backup-pflichtigen
  Datenbank.
- `dotnet build` (alle Projekte, Windows-TFM inkl. XAML-SourceGen):
  0 Fehler, keine neuen Warnungen; `dotnet test`: 324/324 bestanden.

## Feststellungen

Keine offenen Befunde. Hinweis ohne Korrekturbedarf: Ein erneuter
`ConnectServerEventsEffect` nach `Closed` kann scheitern, wenn das
Netzwerk gerade wieder weg ist — der Fehler wird protokolliert, und die
nächste Gelegenheit (nächster Online-Wechsel oder erneutes `Closed`)
baut die Verbindung wieder auf. Bewusst keine Wiederholungsschleife.
