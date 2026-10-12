# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### PlayerPage.xaml.cs (PlayerPage)

- **Geheimnis im Protokoll (kritisch)** — `UpdateUi`, Zeile ~244: `FailPlaybackStart($"Ungültige Quell-Adresse ({player.Source.Kind}): '{location}'")`. `location` ist `PlaybackSource.Location` und enthält bei `ServerStream` die komplette URL inklusive `?access_token=…`. Durch die Umstellung von `Debug.WriteLine` auf `logger.LogError` landet das Anmelde-Token jetzt in `videoplayer.log` — der Datei, die per E-Mail versandt wird. Der Contract-Pin `PlayerPage_LogsPlaybackFailure` verfehlt das: Er prüft nur `logger.Log`-Zeilen auf das Literal „ocation", das Leck sitzt aber eine Aufrufebene höher im `FailPlaybackStart`-Argument.

  Empfehlung: `location` aus der Meldung entfernen bzw. auf `uri.GetLeftPart(UriPartial.Path)` (Query gestripped) reduzieren; Contract-Pin so erweitern, dass kein `FailPlaybackStart`-Aufruf `location` interpoliert.

- **Mögliches Geheimnis im Protokoll** — `OnMediaFailed`, Zeile ~410: `logger.LogError("Wiedergabe fehlgeschlagen: {LogMessage}", e.ErrorMessage)`. `MediaFailedEventArgs.ErrorMessage` ist plattformgeneriert und kann die abgelehnte Quell-URL enthalten — damit erneut das `access_token`-Query. Analoges Restrisiko in `FailPlaybackStart("Setzen der Quelle fehlgeschlagen: {ex.Message}")` und `"Zurücksetzen der Quelle fehlgeschlagen: {ex.Message}"` (Exception-Messages können die URL enthalten).

  Empfehlung: In `OnMediaFailed` nur `Kind`/Medientyp/-Id protokollieren statt der Roh-Fehlermeldung, oder die Meldung vor dem Loggen auf URL-Anteile prüfen/bereinigen.

### ProgressReportService.cs + PendingProgressSyncService.cs (Fortschrittstakt)

- **Sparsamkeitsverstoß — Einträge pro 5-s-Takt bei Offline-Wiedergabe** — `ProgressReportService.ReportProgressAsync` wird über `ReportPlaybackProgressEffect` im Fortschrittstakt (alle ~5 s) aufgerufen. Bei Offline/nicht angemeldet schreiben `pendingQueue.EnqueueAsync` (Information „eingereiht") und die Zeilen 99–102 („in die Warteschlange eingereiht") **zwei Einträge pro Takt**; bei überholbarem Sendefehler kommt noch die Warning dazu (drei Einträge). Eine Offline-Wiedergabe erzeugt damit hunderte Einträge — genau der Fortschrittstakt, den Anforderung und Plan explizit ausschließen.

  Empfehlung: Das `Information`-Einreihe-Log streichen oder auf den Zustandswechsel begrenzen (nur die erste Einreihung eines Mediums); die `EnqueueAsync`-Meldung im `PendingProgressSyncService` entfällt ohnehin (doppelt, siehe Schicht-Duplikate).

### PendingProgressSyncService.cs / ServerSyncService.cs (pro-Item-Logs)

- **Sparsamkeitsverstoß — Eintrag pro wartendem Eintrag** — `RemoveForMediaAsync` (Zeile ~108) loggt je entferntem Eintrag; `ServerSyncService.FlushPendingProgressAsync` ruft es pro Element der Flush-Schleife auf → N Einträge pro Sync. Zusätzlich schreibt `GetPendingAsync` (Zeile ~82) bei jedem Sync „N ausstehende Fortschrittsmeldungen gelesen", obwohl `SyncResult.RemainingProgressReports` denselben Zähler bereits im Ergebnis-Eintrag trägt.

  Empfehlung: Per-Item-Eintrag in `RemoveForMediaAsync` und Lese-Eintrag in `GetPendingAsync` entfernen; die aggregierte Zählung verbleibt bei `FlushPendingProgressAsync` („{SentCount} wartende Fortschritte nachgesendet") bzw. `SynchronizeAsync`.

### SignalRServerEventsConnection.cs (Ereignis-Breadcrumbs)

- **Sparsamkeitsrisiko — Eintrag pro Hub-Ereignis** — `ConnectAsync` loggt `ContinueWatchingUpdated`, `FavoritesChanged` und `NewVideosScanned` je eintreffendem Ereignis als `Information`. `ContinueWatchingUpdated` wird serverseitig u. a. bei eingehenden Fortschrittsmeldungen ausgelöst — die eigene App meldet Fortschritt im 5-s-Takt, womit das Ereignis faktisch zum Fortschrittstakt im Protokoll werden kann.

  Empfehlung: Ereignis-Einträge auf `Debug` absenken (vom `FileLogger`-`Information`-Filter verworfen) oder streichen; `Reconnecting`/`Reconnected`/`Closed` sind dagegen sinnvolle seltene Breadcrumbs und können bleiben.

### Schicht-übergreifende Doppeleinträge (Doppelte Protokollierung)

- **Doppelter Code / Sparsamkeit** — Dasselbe Ereignis wird auf zwei bis drei Ebenen mit nahezu identischen Meldungen protokolliert; jeder Vorgang erzeugt mehrfache Einträge:

  - Ereignisverbindung aufbauen/trennen: `SignalRServerEventsConnection` (aufgebaut/getrennt) + `ServerEventsService.ConnectAsync/DisconnectAsync` + `ShellAppEffectHandler.ConnectServerEventsAsync/DisconnectServerEventsAsync` → 3×
  - Verbindung geschlossen: `SignalRServerEventsConnection` („endgültig geschlossen") + `ServerEventsService.OnClosed` → 2×
  - Synchronisation abgeschlossen: `ServerSyncService.SynchronizeAsync` + `ShellAppEffectHandler` RunFullSync (identische Zähler) → 2×
  - Anmeldung/Auto-Anmeldung/Abmeldung: `AuthSessionService` + `ShellAppEffectHandler` („…abgeschlossen/ausgeführt") → 2×
  - Sitzung abgelaufen / Geräte-Token abgelehnt: `AuthSessionService.OnSessionExpired` bzw. `DeviceTokenService.NotifyRejected` + Handler-Events → 2×
  - Download einplanen/abbrechen/löschen: `DownloadService` (`Logged`, „Abbruch angefordert", „entfernt") + Handler („Einplanung", „abgebrochen", „gelöscht") → 2× (Abbruch sogar 3× inkl. `Cancelled`-Zustandseintrag)
  - Fortschritt einreihen: `PendingProgressSyncService.EnqueueAsync` + `ProgressReportService` → 2×
  - PiP-Umschaltung: `PictureInPictureService.ToggleAsync` + `ShellAppEffectHandler.TogglePictureInPictureAsync` → 2×
  - Netzverfügbarkeit: `ConnectivityNetworkStatusMonitor.OnConnectivityChanged` + `ShellAppEffectHandler.OnNetworkAvailabilityChanged` → 2×
  - Serverprüfung: `ServerHealthProbe` („Serverprüfung {Host}") + `ShellAppEffectHandler` („Erreichbarkeitsprüfung {ServerAddress}") → 2×
  - Auto-Download-Lauf: `AutoDownloadService.RunAsync` + `ShellAppEffectHandler.RunAutoDownloadsAsync` → 2×

  Empfehlung: Pro Ereignis genau eine Schicht protokollieren lassen. Konsistente Regel: der Dienst mit dem Ergebniskontext (Zähler, Quellart, Status) schreibt den Eintrag; der `ShellAppEffectHandler` beschränkt sich auf seine vorhandenen `LogError`-Pfade — oder umgekehrt Handler-Breadcrumb statt Dienst-Eintrag, nicht beides.

### DownloadService.cs (Level-Einordnung)

- **Inkonsistente Stufe** — `EnqueueAsync` schreibt über die lokale Funktion `Logged` **alle** Ergebnisse als `Information`, auch `IsError`-Notices (`NoticeInvalidMedia`, `NoticeQuotaExceeded`). Nach der eigenen Stufenregel sind das überholbare Einzelfehler → `Warning`.

  Empfehlung: In `Logged` `enqueueResult.IsError ? LogWarning : LogInformation` verzweigen.

- **Level-/Zustandsduplikate** — `SetStateAsync` protokolliert jeden Zustandswechsel inklusive `Queued`-/`Cancelled`-Zwischenstände; kombiniert mit den Handler-Einträgen entstehen pro Auftrag mehrere Einträge (Einplanung + `Queued` + `Downloading` + `Completed`/`Cancelled`). Das Zustands-Logging selbst ist vertretbar, sollte aber mit der Entscheidung zu den Schicht-Duplikaten abgestimmt werden.

### ShellAppEffectHandler.cs (TogglePictureInPictureAsync)

- **Falscher Erfolgs-Breadcrumb nach Fehlschlag** — Zeile ~508: Der `LogInformation`-Eintrag „PiP-Umschaltung abgeschlossen: {IsActive}" steht **hinter** dem `catch`; schlägt `ToggleAsync` fehl, wird zusätzlich zum `LogError` „abgeschlossen: inaktiv" protokolliert — ein irreführender Erfolgs-Eintrag.

  Empfehlung: Den Eintrag in den Erfolgszweig verschieben (z. B. nur wenn keine Exception auftrat) oder bei Fehlschlag nicht schreiben.

### AutoDownloadService.cs (EnqueueNextPlaylistEntriesAsync)

- **Magic-Return statt klarer Kontrakt** — Die Methode liefert jetzt `int`: `-1` kodiert „Playlist-Pfad nicht zutreffend (Serien-Fallback)", `0` „gefunden, nichts eingereiht". Drei Bedeutungen in einem numerischen Rückgabewert; der Aufrufer dekodiert mit `>= 0`/`Math.Max(0, …)`.

  Empfehlung: `int?` (null = Fallback auf Serien-Pfad) oder `(bool handled, int enqueued)`-Tupel — der bisherige `bool`-Rückgabewert war lesbarer.

- **Ungenaue Zählung** — `enqueued` zählt jedes `!IsError`-Ergebnis, also auch `NoticeAlreadyQueued`/`NoticeAlreadyPresent` — die Abschlussmeldung „N Aufträge eingeplant" kann deutlich mehr melden als tatsächlich neu eingeplant wurde.

  Empfehlung: Nur `NoticeEnqueued` zählen oder die Meldung entsprechend formulieren.

### ServerHealthProbe.cs (Verdrahtung)

- **Inkonsistente Logger-Verdrahtung** — Der für die Probe erzeugte `VideoPlayerApiClient` (Zeile ~63) erhält kein `Logger` in `VideoPlayerApiClientOptions`, während `AuthSessionService.CreateClient` und die Refresh-/Widerrufs-Clients verdrahtet wurden. Wiederholungs-/401-Warnungen der Probe laufen damit weiter am Protokoll vorbei.

  Empfehlung: Dem Options-Objekt ebenfalls `logger` (Kategorie `VideoPlayerApiClient` via `ILoggerFactory` oder den `ILogger<ServerHealthProbe>`) mitgeben — sofern beabsichtigt; andernfalls bewusst dokumentieren.

### LogProtocolContractTests.cs (Testqualität)

- **Schwache Assertions durch `SliceMethod`** — `SliceMethod` schneidet ab Signatur bis zur nächsten Zeile `\n    private `. Bei `public`-Methoden umfasst der Slice folgende `public`-Member mit: In `PictureInPictureService_LogsToggleRestore` reicht der `ToggleAsync`-Slice bis zur nächsten `private`-Member-Deklaration und deckt damit auch `RestoreAsync` ab — die Assertion ist auch erfüllt, wenn nur `RestoreAsync` protokolliert.

  Empfehlung: `SliceMethod` am Ende der Methode abschneiden (nächste Member-Deklaration unabhängig von der Sichtbarkeit) oder die Slices gezielt auf den Methodenrumpf begrenzen.

### PendingProgressSyncServiceTests.cs / PendingProgressSyncService.cs (kosmetisch)

- **Meldungsformulierung** — „{RemovedCount} Fortschrittsmeldungen … entfernt" ergibt bei einem Eintrag „1 Fortschrittsmeldungen" (Test pinnt das sogar fest). Rein kosmetisch; beim Entfernen der pro-Item-Meldung (s. o.) ohnehin obsolet.

## Geprüfte Dateien

Liste aller geprüften Dateien:
- `VideoPlayer.E2E.Tests/Tests/LogProtocolTests.cs`
- `VideoPlayer.Maui/App.xaml.cs`
- `VideoPlayer.Maui/AppShell.xaml.cs`
- `VideoPlayer.Maui/MauiProgram.cs`
- `VideoPlayer.Maui/Pages/PlayerPage.xaml.cs`
- `VideoPlayer.Maui/Pages/ServerSetupPage.xaml.cs`
- `VideoPlayer.Maui/Pages/SettingsPage.xaml.cs`
- `VideoPlayer.Maui/Services/ConnectivityNetworkStatusMonitor.cs`
- `VideoPlayer.Maui/Services/PictureInPictureService.cs`
- `VideoPlayer.Maui/Services/PosterImageService.cs`
- `VideoPlayer.Maui/Services/SecureStorageSecretStore.cs`
- `VideoPlayer.Maui/Services/ServerHealthProbe.cs`
- `VideoPlayer.Maui/Services/ShellAppEffectHandler.cs`
- `VideoPlayer.Maui/Services/SignalRServerEventsConnection.cs`
- `VideoPlayer.Mvu/AuthSessionService.cs`
- `VideoPlayer.Mvu/AutoDownloadService.cs`
- `VideoPlayer.Mvu/ContinueWatchingMutationService.cs`
- `VideoPlayer.Mvu/DeviceTokenService.cs`
- `VideoPlayer.Mvu/DownloadService.cs`
- `VideoPlayer.Mvu/HomeListService.cs`
- `VideoPlayer.Mvu/MediaDetailsService.cs`
- `VideoPlayer.Mvu/OfflineContinueWatchingService.cs`
- `VideoPlayer.Mvu/PlaybackSourceService.cs`
- `VideoPlayer.Mvu/ProfileInfoService.cs`
- `VideoPlayer.Mvu/ProgressReportService.cs`
- `VideoPlayer.Mvu/ServerEventsService.cs`
- `VideoPlayer.Mvu/ServerSyncService.cs`
- `VideoPlayer.Mvu/ShowDetailsCache.cs`
- `VideoPlayer.Mvu/UpcomingEpisodesService.cs`
- `VideoPlayer.Mvu/VideoPlayer.Mvu.csproj`
- `VideoPlayer.Storage/BackupDatabase.cs`
- `VideoPlayer.Storage/CacheDatabase.cs`
- `VideoPlayer.Storage/PendingProgressSyncService.cs`
- `VideoWebPlayer.Maui.Tests/AuthSessionServiceTests.cs`
- `VideoWebPlayer.Maui.Tests/AutoDownloadServiceTests.cs`
- `VideoWebPlayer.Maui.Tests/DownloadServiceTests.cs`
- `VideoWebPlayer.Maui.Tests/LogProtocolContractTests.cs`
- `VideoWebPlayer.Maui.Tests/PairingContractTests.cs`
- `VideoWebPlayer.Maui.Tests/PendingProgressSyncServiceTests.cs`
- `VideoWebPlayer.Maui.Tests/PlaybackSourceServiceTests.cs`
- `VideoWebPlayer.Maui.Tests/ProgressReportServiceTests.cs`
- `VideoWebPlayer.Maui.Tests/ServerEventsServiceTests.cs`
- `VideoWebPlayer.Maui.Tests/ServerSyncServiceTests.cs`
- `VideoWebPlayer.Maui.Tests/Stubs/RecordedLogEntry.cs` (neu, untracked)
- `VideoWebPlayer.Maui.Tests/Stubs/RecordingLoggerProvider.cs` (neu, untracked)
