# Abnahmeprüfung – Entwicklungsschritt 6

## Ergebnis

**Status:** Anforderung vollständig erfüllt

## Abweichungen

- Keine.

## Hinweise

Geprüft auf Branch `task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app-schritt-6-netzwerk-sync` (HEAD: `4792ab1`), Diff gegen `task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app` (49 Dateien, +3972/−53).

### Online/Offline-Erkennung verdrahtet

- `INetworkStatusMonitor` (`VideoPlayer.Mvu/INetworkStatusMonitor.cs`) hält den MVU-Kern plattformneutral; die Implementierung `ConnectivityNetworkStatusMonitor` (`VideoPlayer.Maui/Services/ConnectivityNetworkStatusMonitor.cs:16-35`) bildet `NetworkAccess.Internet`/`ConstrainedInternet` als online ab und meldet jeden Wechsel über `Connectivity.Current.ConnectivityChanged`.
- Der Start-Workflow emittiert `StartNetworkMonitoringEffect` (`AppUpdate.cs:85`); der Handler startet den Monitor idempotent und meldet den Ist-Wert als `NetworkAvailabilityChangedMessage` zurück (`ShellAppEffectHandler.cs:111-125`) — der Startzustand geht nicht verloren.
- `UpdateNetworkAvailabilityChanged` (`AppUpdate.cs:392-424`): Netzwerkverlust aus `Connected` → `ConnectionState.Offline` + `DisconnectServerEventsEffect` (Anwender bleibt auf der Seite, Offline-Banner über `model.IsOffline`, `HomePage.xaml.cs:41`); Online-Rückkehr aus `Offline` → `Connecting` + `CheckServerHealthEffect`. In den Workflow-Zuständen (`NeedsLogin`, `NeedsServerSetup`, laufender Start) wird nur `IsOnline` aktualisiert — die laufenden Prüfungen entscheiden über den Ausgang.

### Automatische Wiederverbindung inkl. Echtzeit-Verbindung

- Online-Rückkehr läuft über die normale Kette: `CheckServerHealthEffect` → erreichbar → `AttemptAutoLoginEffect` → `Succeeded` → `EnterConnected` (`AppUpdate.cs:354-384`) emittiert `ConnectServerEventsEffect` + `RunFullSyncEffect`; auf Menüseiten ohne Navigation, von Workflow-Seiten zur Startseite.
- `SignalRServerEventsConnection` (`VideoPlayer.Maui/Services/SignalRServerEventsConnection.cs`) verbindet zu `hubs/mediaupdate` mit `AccessTokenProvider` (aktuelles Bearer-Token aus `AuthSessionService.Client`, `ServerEventsService.cs:72`) und `WithAutomaticReconnect`; `Closed` meldet nur die endgültige Trennung. Hub-Pfad und Ereignisnamen (`ContinueWatchingUpdated`, `FavoritesChanged`, `NewVideosScanned`, `StatusChanged`) entsprechen der Bestands-App (`VideoWebPlayer.Maui/Services/SignalRService.cs:50-68`).
- `ServerEventsConnectionClosedMessage` baut die Verbindung im Zustand `Connected` + `IsOnline` erneut auf (`AppUpdate.cs:445-454`); `ServerEventsService.OnClosed` verwirft nachgereichte Closed-Ereignisse ersetzter Verbindungen (`ServerEventsService.cs:152-177`).
- Server-Ereignisse werden als `ServerEventReceivedMessage` über den MainThread in den MVU-Zyklus dispatched (`ShellAppEffectHandler.cs:184-188`) und lösen gezielte Teil-Synchronisationen aus (`AppUpdate.cs:426-443`): Weiterschauen → `SyncContinueWatchingEffect`, Favoriten → `SyncFavoritesEffect`, neue Inhalte → `SyncMetadataEffect`. Die Aktualisierung der Caches erfolgt ohne manuelle Aktualisierung.

### Synchronisation

- `ServerSyncService.SynchronizeAsync` (`ServerSyncService.cs:55-73`) führt den vollständigen Sync aus: zuerst `FlushPendingProgressAsync` (damit die anschließend gelesene Weiterschauen-Liste den aktuellen Stand enthält), dann `SyncMetadataAsync` (Quellen + „Neu im Programm" nach `cached_lists`), `SyncContinueWatchingAsync` und `SyncFavoritesAsync` (lokale Kopie `cached_favorites` wird ersetzt). Ohne angemeldeten Client No-Op mit ehrlichem `SyncResult`.
- `RunFullSyncEffect` wird bei jedem Eintritt in `Connected` ausgeführt (Start und Wiederverbindung); `SynchronizationCompletedMessage` räumt `IsSyncRunning` (`AppUpdate.cs:62`).

### Persistente Fortschritts-Warteschlange

- Tabelle `pending_progress` auf der backup-pflichtigen `BackupDatabase` (`Entities/PendingProgressSyncEntry.cs`, `BackupDatabase.cs:34`) — Meldungen überstehen App-Neustarts und Backup-Wiederherstellungen (Test `Pending_SurvivesDatabaseReopen` simuliert den Neustart über eine neue Verbindung auf derselben Datei). `EnsureColumnAsync` migriert `DurationTicks` in bestehende Datenbanken nach (Test `BackupDatabase_MigratesDurationTicksOntoExistingTable`).
- `PendingProgressSyncService`: Einreihen mit Coalescing pro Medium (nur neuester Stand), `GetPendingAsync` chronologisch (`UpdatedAtUtc`, älteste zuerst), `RemoveForMediaAsync` erst nach erfolgreichem Senden (`ServerSyncService.cs:95-121`).
- `ProgressReportService` (`ProgressReportService.cs:35-77`): online + Client → Direktversand; offline, ohne Client oder bei überholbarem Fehler → Einreihung. Permanente 4xx (außer 401/408/429) werden verworfen (`IsPermanentClientError`, gleiche Regel beim Drain: Transport-/Serverfehler brechen ab und lassen den Rest in der Schlange). Reihenfolge und Verlustfreiheit sind testabgedeckt.

### Testnachweis

- `dotnet test VideoWebPlayer.Maui.Tests` (net10.0): **324 Tests bestanden, 0 Fehler, 0 übersprungen** (Dauer ~3 s). Neue Suites: `AppUpdateTests_NetworkSync` (16 Fälle), `ServerEventsServiceTests`, `ServerSyncServiceTests`, `ProgressReportServiceTests`, `PendingProgressSyncServiceTests`, `NetworkSyncContractTests`, Migrationstest in `LocalDatabaseTests`; bestehende Start-/Verbindungstests auf die neuen Effekte angepasst.

### Einschränkungen

- `ProgressReportService` hat noch keinen produktiven Aufrufer — die Fortschrittserfassung im Player folgt in einem späteren Schritt (laut Plan Schritt 10); die Sammelstrecke (Einreihen, Persistenz, Drain) ist jetzt vollständig implementiert und getestet.
- Die Listen-Anzeigen (Weiterschauen, Favoriten, Quellen) sind derzeit noch Grundgerüst-Seiten; die Ereignis→Sync→Cache-Kette ist verdrahtet und getestet, sichtbare Aktualisierung der Listendarstellung folgt mit den Inhaltsseiten in späteren Schritten.
- Der erneute Verbindungsaufbau nach `ServerEventsConnectionClosedMessage` erfolgt ohne Backoff — ein enger Wiederholungszyklus ist nur denkbar, wenn der Hub eine Verbindung annimmt und sofort wieder schließt; scheiternde Connect-Versuche erzeugen kein `Closed` und damit keine Schleife.
