# Umsetzungsplan: Programmprotokoll mit diagnostisch verwertbarem Inhalt

## Übersicht

Die bestehende Protokoll-Infrastruktur (`AppLogService`, `FileLogger`, `FileLoggerProvider`; `ILoggerProvider`-Singleton in `MauiProgram`) bleibt unverändert — sie erhält schlicht keine Einträge, weil nur der `ShellAppEffectHandler` einen `ILogger` injiziert bekommt und ausschließlich auf Fehlerpfaden schreibt. Der Plan ergänzt sparsame `ILogger`-Aufrufe entlang der fachlichen Abläufe (Start, Anmeldung, Serververbindung, Wiedergabe, Synchronisation, Downloads, Online-/Offline-Wechsel, Fehler), stellt die ~75 verbliebenen `Debug.WriteLine`-Stellen der App-Projekte auf injizierte Logger um und verdrahtet den vorhandenen, bislang ungenutzten `VideoPlayerApiClientOptions.Logger`-Haken. Betroffen sind `VideoPlayer.Mvu` (14 Dienste + statischer `ShowDetailsCache`), `VideoPlayer.Storage` (3 Klassen), `VideoPlayer.Maui` (11 Klassen + `MauiProgram`), `VideoPlayer.Mvu.csproj` (neue Paketreferenz), `VideoWebPlayer.Maui.Tests` und `VideoPlayer.E2E.Tests`.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Logger-Injektion in `VideoPlayer.Mvu`-/`VideoPlayer.Storage`-Diensten | Optionaler, nachfolgender Konstruktorparameter `ILogger<T>? logger = null` (bzw. `ILoggerFactory?`), intern auf `NullLogger<T>.Instance`/`NullLoggerFactory.Instance` normiert | Die ~39 direkten `new …Service(…)`-Erzeugungsstellen in `VideoWebPlayer.Maui.Tests` (inkl. der `internal`-Testüberladungen) bleiben ohne Anpassung kompilierbar; die MAUI-DI löst `ILogger<T>`/`ILoggerFactory` über `builder.Logging` automatisch auf und füllt die Parameter. Spiegelt die bestehende Optionalitäts-Konvention von `VideoPlayerApiClientOptions.Logger`. |
| Logger-Injektion in `VideoPlayer.Maui`-Klassen | Erforderlicher Konstruktorparameter `ILogger<T>` | Diese Klassen (`App`, `AppShell`, Seiten, Dienste) werden ausschließlich über DI erzeugt; das Testprojekt referenziert `VideoPlayer.Maui` nicht (Contract-Tests prüfen Quelltext), daher entsteht kein Test-Bruch und die Signatur bleibt eindeutig. |
| `AuthSessionService` | Ein zusätzlicher Parameter `ILoggerFactory? loggerFactory = null` statt `ILogger<T>` | Aus der Fabrik lassen sich der eigene `ILogger<AuthSessionService>`, der `ILogger<VideoPlayerApiClient>` für `VideoPlayerApiClientOptions.Logger` und der Logger für das intern erzeugte `DeviceTokenService` ableiten — ein Parameter deckt alle drei Bedarfe. DI wählt weiterhin den längsten auflösbaren Konstruktor (der mit `DeviceTokenService`). |
| `ShowDetailsCache` (statische Klasse) | `ILogger?`-Parameter an `WriteAsync`/`ReadAsync` (Standard `null`), Aufrufer reichen ihren Dienst-Logger durch | Ein Umbau in einen Instanz-Dienst wäre für zwei Catch-Blöcke überdimensioniert; Methodenparameter halten die statische Hilfsklasse unverändert instanzenlos und testkompatibel. |
| Breadcrumb-Platzierung | `logger.LogInformation`-Einträge in den Ausführungsmethoden des `ShellAppEffectHandler` (z. B. `RunFullSyncAsync`, `ResolvePlaybackSourceAsync`) — ein Eintrag je ausgewähltem Effekt, mit Ergebnis-Kontext | Die Methoden kennen das Ergebnis (Zähler, Quellart, Status); ein pauschaler Eintrag im `HandleAsync`-Switch hätte keinen Ergebniskontext. Der Switch bleibt der benannte Breadcrumb-Punkt — ein Eintrag pro Effekt, nicht pro MVU-Nachricht. |
| Level-Einordnung der `Debug.WriteLine`-Umstellung | Catch-Block mit Fallback/überholbarem Fehler → `LogWarning` mit Exception; Catch-Block, der den Vorgang scheitern lässt → `LogError` mit Exception; Zustandsmeldung (z. B. `ServerEventsService.OnStatusChanged`) → `LogInformation` | Trennt fachliche Breadcrumbs von überholbaren Störungen und Fehlschlägen — deckt sich mit der Anforderungs-Stufung (Information = Verlauf, Warning = überholbarer Einzelfehler, Error = Fehlschlag). |
| Sparsamkeits- und Geheimnisregeln | Ausschlussliste statt genereller Protokollierung: kein 5-s-Fortschrittstakt (`PlayerPage.ReportPosition`/`PlaybackProgressMessage`), kein `ReportPlaybackProgressEffect`-Eintrag, keine Einträge pro Listeneintrag/Bildabruf (`PosterImageService` nur Warn-/Fehlerpfade), keine Einträge pro MVU-Nachricht; niemals `PlaybackSource.Location` (enthält `access_token`), Benutzernamen (`AuthAttempt.Username`), Passwörter, Tokens oder Anmeldedaten. Inhalte: Medientyp/-Id immer; Anzeigetitel nur bei Wiedergabe- und Download-Auftrags-Einträgen (verfügbar über `PlayerState.Title`, `EnqueueDownloadEffect.Title`, `DownloadListItem.Title`); Quellart (`PlaybackSourceKind`), Status und Zähler | „Wenig, aber verwertbar" ist Kern der Anforderung; die Titel-Freigabe an genau zwei Stellen ist die vom Anwender bestätigte Balance aus Diagnosewert und Datensparsamkeit beim E-Mail-Versand; die 1-MiB-Rotation und die `IsEnabled`-Frühprüfung des `FileLogger` schützen Datei- und Laufzeitkosten zusätzlich. |
| `Debug.WriteLine`-Umstellungsumfang | Volle Umstellung aller ~75 Aufrufe in `VideoPlayer.Mvu` (53), `VideoPlayer.Maui` (20) und `VideoPlayer.Storage` (2); ausgenommen bleiben `VideoWebPlayer.Maui` (Altbestand) und die 3 Discovery-Stellen in `VideoPlayer.ApiClient`; ein Contract-Test pinnt den Restbestand | Vom Anwender bestätigt: `Debug.WriteLine` ist im Release-Betrieb unsichtbar — jede verbliebene Stelle wäre verlorene Diagnose; die Ausnahmen besitzen keine Protokoll-Verdrahtung bzw. liegen außerhalb der App-Projekte. |
| Geltungsbereich Alt-App `VideoWebPlayer.Maui` | Keine Protokoll-Ergänzungen in `VideoWebPlayer.Maui` | Vom Anwender bestätigt: dort existiert kein Protokoll-Schalter und keine `AppLogService`-/`FileLogger`-Verdrahtung (durch die Bestandsaufnahme belegt). |
| Kopfdaten beim Aktivieren | Keine zusätzlichen Kopfeinträge (App-Version, Betriebssystem) beim Einschalten der Protokollierung | Vom Anwender bestätigt: Sparsamkeit hat Vorrang; die App-Version ist dem E-Mail-Versand-Kontext entnehmbar. |
| Testnachweis der `VideoPlayer.Maui`-Breadcrumbs (Online-/Offline-Wechsel, PiP, Wiedergabe-Fehlschlag, Logout-Breadcrumb) | Quelltext-Pins in `LogProtocolContractTests` (für `ConnectivityNetworkStatusMonitor.cs`, `PictureInPictureService.cs`, `PlayerPage.xaml.cs`, `ShellAppEffectHandler.cs`) plus Unit-Test `LogoutAsync_WritesEntry` mit `RecordingLoggerProvider` für `AuthSessionService.LogoutAsync`; optionaler Zusatz: Teilflüsse im E2E-Fact | `VideoPlayer.Maui` ist vom Testprojekt nicht referenzierbar und die E2E-Suite schaltet Netzverfügbarkeit, PiP und Medienfehlschlag nicht um — die Pins folgen der bestehenden Contract-Test-Konvention und schließen die Testlücken der benannten Abläufe; `LogoutAsync` ist als `VideoPlayer.Mvu`-Dienst zusätzlich direkt unit-testbar. |

## Programmabläufe

### Breadcrumbs der Effekt-Ausführung

1. `MvuHost` dispatcht einen `AppEffect` → `ShellAppEffectHandler.HandleAsync` → `switch` in den Ausführungszweig.
2. In der jeweiligen `*Async`-Methode wird nach dem Ergebnis ein `LogInformation`-Eintrag geschrieben (Message-Template mit benannten Platzhaltern):
   - `InitializeStorageAsync` → Ablage initialisiert.
   - Serverprüfung (`CheckServerHealth`-Pfad) → konfigurierte Adresse + Ergebnis (`HealthCheckResult`-Status, kein Query-Inhalt).
   - `AttemptAutoLoginAsync`/`AttemptLoginAsync` → Anmeldeergebnis (`AuthAttemptStatus`; niemals Zugangsdaten).
   - `ConnectServerEventsAsync`/`DisconnectServerEventsAsync` → Verbindungsauf-/-abbau der Ereignisverbindung.
   - `RunFullSyncAsync` sowie `SyncContinueWatching`-/`SyncFavorites`-/`SyncMetadata`-Pfade → Sync-Beginn/-Ergebnis mit den `SyncResult`-Zählern (`SentProgressReports`, `RemainingProgressReports`, Sync-Flags).
   - `ResolvePlaybackSourceAsync` → Medientyp/-Id + Anzeigetitel (`Player.Title` aus `MvuHost.Model`, sofern zur `MediaId` passend belegt) + `PlaybackSourceKind` (nie `Location`).
   - `EnqueueDownloadAsync`/`CancelDownloadAsync`/`DeleteDownloadAsync`/Resume-/Auto-Download-Pfade → Medientyp/-Id + Anzeigetitel (`EnqueueDownloadEffect.Title` bzw. `DownloadListItem.Title`) + `DownloadEnqueueResult`-Notice bzw. Zustand.
   - `LogoutAsync` → Abmeldung.
   - `TogglePictureInPicture`-Pfad → PiP-Umschaltung (Ergebnis).
   - `SendLogProtocolAsync` → Versand-Ergebnis (bestehender `LogInformation`-Fallback bleibt).
3. Die abonnierten Ereignisse erhalten Einträge auf passendem Level: `authSession.SessionExpired` → `LogWarning` (Sitzung abgelaufen), `deviceTokens.DeviceTokenRejected` → `LogWarning`, `networkMonitor.AvailabilityChanged` → `LogInformation` (Online-/Offline-Wechsel).
4. Die vorhandenen ~60 `LogError`-Pfade bleiben unverändert.

Beteiligte Klassen/Komponenten: `ShellAppEffectHandler`, `AppEffect`, `AppLogService`/`FileLogger` (unverändert), `SyncResult`, `AuthAttemptStatus`, `PlaybackSourceKind`.

### Anmeldung, Sitzung und API-Client-Diagnose

1. `MauiProgram` löst `AuthSessionService` auf → DI füllt den neuen `ILoggerFactory?`-Parameter.
2. `AuthSessionService.TryStoredLoginAsync`/`LoginAsync` protokollieren Verlauf und Ergebnis (`AuthAttemptStatus`, Serveradresse-Host ohne Credentials) auf `Information`; stille Neuanmeldung (`SilentReloginAsync`), Sitzungsablauf (`SessionExpired`) und Abmeldung (`LogoutAsync`) erhalten Einträge (`Information` bzw. `Warning` bei Ablehnung).
3. `CreateClient` setzt `VideoPlayerApiClientOptions.Logger = loggerFactory?.CreateLogger<VideoPlayerApiClient>()`; die kurzlebigen Clients in `TryRefreshSessionAsync`, `SilentReloginAsync` und `RevokeRefreshTokenAsync` erhalten denselben Logger → die vorhandenen `LogWarning`-Ausgaben des `VideoPlayerApiClient` (Transportfehler-Wiederholung, 401/Token-Erneuerung, wiederholbarer Status) landen im Protokoll.
4. `DeviceTokenService` erhält `ILogger<DeviceTokenService>?` (beim internen `new` aus `AuthSessionService` via `loggerFactory?.CreateLogger<DeviceTokenService>()` durchgereicht) und protokolliert Gate-Abweisung/Token-Verwerfen.
5. Die 9 bzw. 6 `Debug.WriteLine`-Stellen beider Dienste werden ersetzt.

Beteiligte Klassen/Komponenten: `AuthSessionService`, `DeviceTokenService`, `VideoPlayerApiClient`, `VideoPlayerApiClientOptions`, `MauiProgram`.

### Wiedergabe

1. `ResolvePlaybackSourceEffect` → `ShellAppEffectHandler.ResolvePlaybackSourceAsync` → `PlaybackSourceService.ResolveAsync` → Eintrag mit Medientyp/-Id, Anzeigetitel (der Handler liest `Player.Title` aus `MvuHost.Model` — `StartPlayback` setzt `PlayerState.Title` vor dem Effekt), gewählter Quellart (`ServerStream`/`LocalFile`) und Fortsetzungsposition; `Location` bleibt unprotokolliert.
2. `PlayerPage` (neuer `ILogger<PlayerPage>`-Parameter) protokolliert `OnMediaOpened` (inkl. Anzeigetitel aus `mvuHost.Model.Player.Title` und Fortsetzungs-Seek aus `ApplyResumeSeekAsync`), `OnMediaEnded`, `OnMediaFailed`/`FailPlaybackStart` (`logMessage`, kein URL-Inhalt) — nicht den Fortschrittstakt.
3. `PictureInPictureService.ToggleAsync`/`RestoreAsync` protokollieren die Umschaltung.
4. Die 5 `Debug.WriteLine`-Stellen der `PlayerPage` sowie je eine in `App`/`PictureInPictureService` werden ersetzt (die beiden Fortschrittstakt-Catches als `LogWarning`, nicht periodisch erfolgreiche Pfade).

Beteiligte Klassen/Komponenten: `PlaybackSourceService`, `PlaybackSource`, `PlayerPage`, `PictureInPictureService`, `ShellAppEffectHandler`.

### Synchronisation und Fortschrittsmeldungen

1. `RunFullSyncEffect` → `ShellAppEffectHandler.RunFullSyncAsync` → `ServerSyncService.SynchronizeAsync` → `LogInformation` mit den `SyncResult`-Zählern; die Teil-Syncs (Metadaten, Weiterschauen, Favoriten) bleiben im Aggregat-Eintrag.
2. `PendingProgressSyncService` (Storage, neuer `ILogger<PendingProgressSyncService>?`) protokolliert Einreihen/Entfernen nachgesendeter Fortschritte (Anzahl, nicht pro Eintrag) und `ServerSyncService.FlushPendingProgressAsync` das Nachsende-Ergebnis.
3. `ProgressReportService` protokolliert Einreihen in die Warteschlange (`Information`) und Verwerfen bei dauerhaftem Client-Fehler (`Warning`).
4. `AutoDownloadService.RunAsync` protokolliert Lauf-Ergebnis bzw. Throttle-Auslassung.

Beteiligte Klassen/Komponenten: `ServerSyncService`, `PendingProgressSyncService`, `ProgressReportService`, `AutoDownloadService`, `ShellAppEffectHandler`.

### Downloads

1. `EnqueueDownloadEffect` → `DownloadService.EnqueueAsync` → `LogInformation` (Medientyp/-Id, Anzeigetitel aus dem `title`-Parameter, `DownloadEnqueueResult.Notice`).
2. Zustandswechsel eines Auftrags (`Downloading` → `Completed`/`Failed`/`Cancelled`) → `LogInformation` mit Medientyp/-Id + `DownloadListItem.Title` bzw. `LogWarning` bei Transfer-Fehlschlag; die 7 `Debug.WriteLine`-Stellen (Manifest-/Datei-Fehlschläge, Event-Handler-Fehler) werden zu `LogWarning`/`LogError` umgestellt.
3. `AutoDownloadService` protokolliert Lauf-Start/-Ergebnis und Enqueue-Ergebnisse aggregiert (nicht pro Kandidat).

Beteiligte Klassen/Komponenten: `DownloadService`, `AutoDownloadService`, `DownloadState`, `ShellAppEffectHandler`.

### Serverereignisse, Netzstatus und Serverprüfung

1. `ServerEventsService.ConnectAsync`/`DisconnectAsync` → `LogInformation` (Auf-/Abbau, `HubPath` ohne Token); `OnStatusChanged` → `LogInformation` (bisher `Debug.WriteLine`); `ConnectionClosed` → `LogInformation`.
2. `SignalRServerEventsConnection` erhält `ILogger<SignalRServerEventsConnection>` über die `MauiProgram`-Fabrik (`sp => () => new SignalRServerEventsConnection(sp.GetRequiredService<ILogger<SignalRServerEventsConnection>>())`) und protokolliert Hub-Ereignisse (`ContinueWatchingUpdated`, `FavoritesChanged`, `NewVideosScanned` mit `SourceId`/`Count`, `Closed`) sowie Reconnect-Fehlschläge.
3. `ConnectivityNetworkStatusMonitor` (`ILogger<ConnectivityNetworkStatusMonitor>`) protokolliert `AvailabilityChanged`-Wechsel (`IsOnline`/`IsWifi`).
4. `ServerHealthProbe` protokolliert Prüfergebnisse (Adresse-Host + `HealthCheckResult`, keine Credentials).

Beteiligte Klassen/Komponenten: `ServerEventsService`, `SignalRServerEventsConnection`, `IServerEventsConnection`, `ConnectivityNetworkStatusMonitor`, `ServerHealthProbe`, `ShellAppEffectHandler`.

### App-Start

1. `MauiProgram.CreateMauiApp` erzeugt `AppLogService` (bestehend: Startmarker bei aktiviertem Schalter).
2. `App` (neuer `ILogger<App>`-Parameter) protokolliert in `RunStartupAsync` den Start-Workflow (Auslösung `StartupRequestedMessage`).
3. `AppShell` protokolliert Navigations- und Quellenmenü-Fehlerpfade (5 `Debug.WriteLine` → `LogWarning`/`LogError`); `AppShell`-Quellenwechsel erhält einen `Information`-Eintrag.

Beteiligte Klassen/Komponenten: `App`, `AppShell`, `MauiProgram`, `StartupRequestedMessage`.

### Umstellung `Debug.WriteLine` → `ILogger`

Sämtliche `System.Diagnostics.Debug.WriteLine`-Aufrufe der App-Projekte (`VideoPlayer.Mvu` 53, `VideoPlayer.Maui` 20, `VideoPlayer.Storage` 2) werden durch Logger-Aufrufe ersetzt — Ersetzen, nicht doppelt schreiben (Muster des `ShellAppEffectHandler`-Umbaus; vom Anwender bestätigter voller Umfang). Die Meldungstexte verlieren das `[Klassenname]`-Präfix (die Logger-Kategorie trägt den Typnamen). `VideoWebPlayer.Maui` (Altbestand, kein Protokoll-Schalter) und die drei Discovery-Stellen in `VideoPlayer.ApiClient` bleiben ausdrücklich unberührt; der Restbestand wird über den Contract-Test `NoDebugWriteLine_RemainsInAppSources` gepinnt. Die `#if DEBUG`-`AddDebug()`-Sicht bleibt unverändert.

Beteiligte Klassen/Komponenten: alle oben genannten Dienste sowie `MediaDetailsService`, `HomeListService`, `OfflineContinueWatchingService`, `UpcomingEpisodesService`, `ProfileInfoService`, `ContinueWatchingMutationService`, `ShowDetailsCache`, `CacheDatabase`, `BackupDatabase`, `SettingsPage`, `ServerSetupPage`, `PosterImageService`, `SecureStorageSecretStore`.

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `RecordedLogEntry` | Datenmodellklasse (Testprojekt `VideoWebPlayer.Maui.Tests/Stubs/`) | Ein protokollierter Eintrag: `LogLevel`, `Category`, `Message` (formatiert), `Exception` — Prüfbasis der neuen Logger-Tests. |
| `RecordingLoggerProvider` | `ILoggerProvider` + `ILoggerFactory`-Ersatz (Testprojekt) | Erzeugt `ILogger`-Instanzen je Kategorie, die Einträge in eine gemeinsame Liste schreiben; wird Tests als `ILoggerFactory`/`ILogger<T>`-Quelle in die neuen optionalen Konstruktorparameter injiziert. |

## Änderungen an bestehenden Klassen

### `AuthSessionService` (`VideoPlayer.Mvu/AuthSessionService.cs`)

- **Neue Konstruktorparameter:** `ILoggerFactory? loggerFactory = null` am umfangreichsten Konstruktor; die Convenience-Konstruktoren reichen den Wert durch und geben `loggerFactory?.CreateLogger<DeviceTokenService>()` an das intern erzeugte `DeviceTokenService` weiter.
- **Geänderte Methoden:** `TryStoredLoginAsync`, `LoginAsync`, `LogoutAsync`, `SilentReloginAsync`, `TryRefreshSessionAsync`, `RevokeRefreshTokenAsync`, `HandleGateRejectionAsync` — `Debug.WriteLine` → `ILogger`; zusätzlich `Information`-Einträge für Anmeldeergebnis (`AuthAttemptStatus`), stille Neuanmeldung, `SessionExpired`, Abmeldung.
- **Geänderte Methode:** `CreateClient` setzt `VideoPlayerApiClientOptions.Logger = loggerFactory?.CreateLogger<VideoPlayerApiClient>()`; gleiche Verdrahtung in den kurzlebigen Refresh-/Widerrufs-Clients.

### `DeviceTokenService` (`VideoPlayer.Mvu/DeviceTokenService.cs`)

- **Neue Konstruktorparameter:** `ILogger<DeviceTokenService>? logger = null` an beiden Konstruktoren.
- **Geänderte Methoden:** `LoadForServerAsync`, `SetPairedTokenIfCurrentAsync`, `DiscardCurrentAsync`, `DiscardIfCurrentAsync`, `IsGateRejectedAsync`, `NotifyRejected` — `Debug.WriteLine` → `ILogger`; `Information`/`Warning`-Einträge für Token-Verwerfen und Gate-Abweisung (`DeviceTokenRejected`).

### `ServerSyncService` (`VideoPlayer.Mvu/ServerSyncService.cs`)

- **Neue Konstruktorparameter:** `ILogger<ServerSyncService>? logger = null`.
- **Geänderte Methoden:** `SynchronizeAsync` — `Information`-Eintrag mit den `SyncResult`-Zählern; `FlushPendingProgressAsync` — Nachsende-Ergebnis; `Debug.WriteLine` → `ILogger`.

### `PendingProgressSyncService` (`VideoPlayer.Storage/PendingProgressSyncService.cs`)

- **Neue Konstruktorparameter:** `ILogger<PendingProgressSyncService>? logger = null`.
- **Geänderte Methoden:** `EnqueueAsync`, `GetPendingAsync`, `RemoveForMediaAsync` — `Information`-Einträge mit Anzahl/Medien-Id (nicht pro Eintrag aggregierte Mengen); Fehlerpfade → `LogWarning`.

### `ProgressReportService` (`VideoPlayer.Mvu/ProgressReportService.cs`)

- **Neue Konstruktorparameter:** `ILogger<ProgressReportService>? logger = null`.
- **Geänderte Methoden:** `ReportProgressAsync` — `Information` beim Einreihen in die Warteschlange, `Warning` beim Verwerfen nach `IsPermanentClientError`; `Debug.WriteLine` → `ILogger`. Der periodische Aufrufer (`ReportPlaybackProgressEffect`-Pfad) bleibt unprotokolliert.

### `ServerEventsService` (`VideoPlayer.Mvu/ServerEventsService.cs`)

- **Neue Konstruktorparameter:** `ILogger<ServerEventsService>? logger = null`.
- **Geänderte Methoden:** `ConnectAsync`, `DisconnectAsync`, `DisposeAsync` — `Information`-Einträge für Verbindungsauf-/-abbau; `OnStatusChanged` — `Debug.WriteLine` → `LogInformation`; Fehlerpfade (`ConnectionClosed`-/`EventReceived`-Handler, Dispose) → `LogWarning`/`LogError`.

### `DownloadService` (`VideoPlayer.Mvu/DownloadService.cs`)

- **Neue Konstruktorparameter:** `ILogger<DownloadService>? logger = null` am öffentlichen und am `internal`-Testkonstruktor.
- **Geänderte Methoden:** `EnqueueAsync` (`Information` mit Medientyp/-Id + Anzeigetitel aus dem `title`-Parameter + `Notice`), `CancelAsync`, `DeleteAsync`, `ResumeInterruptedAsync`, Transfer-/Manifest-Pfade — Zustandswechsel als `Information` (mit `DownloadListItem.Title`), Fehlschläge als `Warning`/`Error`; alle 7 `Debug.WriteLine` ersetzt.

### `AutoDownloadService` (`VideoPlayer.Mvu/AutoDownloadService.cs`)

- **Neue Konstruktorparameter:** `ILogger<AutoDownloadService>? logger = null` (beide Konstruktoren).
- **Geänderte Methoden:** `RunAsync` — `Information`-Eintrag für Lauf-Ergebnis (Anzahl eingereihte Aufträge) und Throttle-Auslassung; `EnqueueNextEpisodesAsync`/`EnqueueNextPlaylistEntriesAsync` — aggregierte Einträge; `Debug.WriteLine` → `ILogger`.

### `PlaybackSourceService` (`VideoPlayer.Mvu/PlaybackSourceService.cs`)

- **Neue Konstruktorparameter:** `ILogger<PlaybackSourceService>? logger = null` (beide Konstruktoren).
- **Geänderte Methoden:** `ResolveAsync` — `Information`-Eintrag mit `mediaType`/`mediaId`, `PlaybackSourceKind` und `ResumePosition`; **niemals** `PlaybackSource.Location` (`access_token`-Query). Den Anzeigetitel ergänzt der `ShellAppEffectHandler`-Breadcrumb aus `PlayerState.Title` (der Dienst kennt den Titel nicht). `Debug.WriteLine` → `ILogger`.

### `MediaDetailsService` (`VideoPlayer.Mvu/MediaDetailsService.cs`)

- **Neue Konstruktorparameter:** `ILogger<MediaDetailsService>? logger = null`.
- **Geänderte Methoden:** `LoadAsync` — `Debug.WriteLine` → `ILogger` (Fehlerpfade als `Warning`); `ShowDetailsCache`-Aufrufe reichen den Logger durch.

### `HomeListService` (`VideoPlayer.Mvu/HomeListService.cs`)

- **Neue Konstruktorparameter:** `ILogger<HomeListService>? logger = null`.
- **Geänderte Methoden:** `LoadHomeListsAsync`, `LoadFavoritesAsync`, `LoadRecentAsync`, `LoadFavoriteItemsAsync` — `Debug.WriteLine` → `ILogger` (Fehlerpfade als `Warning`; keine Einträge pro Listenelement).

### `OfflineContinueWatchingService` (`VideoPlayer.Mvu/OfflineContinueWatchingService.cs`)

- **Neue Konstruktorparameter:** `ILogger<OfflineContinueWatchingService>? logger = null` (beide Konstruktoren).
- **Geänderte Methoden:** `LoadHomeListsAsync` — `Debug.WriteLine` → `ILogger`; `ShowDetailsCache.ReadAsync`-Aufruf reicht den Logger durch.

### `UpcomingEpisodesService` (`VideoPlayer.Mvu/UpcomingEpisodesService.cs`)

- **Neue Konstruktorparameter:** `ILogger<UpcomingEpisodesService>? logger = null`.
- **Geänderte Methoden:** `LoadAsync` — `Debug.WriteLine` → `ILogger`; `ShowDetailsCache`-Aufrufe reichen den Logger durch.

### `ShowDetailsCache` (`VideoPlayer.Mvu/ShowDetailsCache.cs`)

- **Neue Methodenparameter:** `ILogger? logger = null` an `WriteAsync` und `ReadAsync` (Klasse bleibt statisch); beide `Debug.WriteLine`-Fehlerpfade → `logger?.LogWarning(ex, …)` mit dem Cache-Schlüssel.

### `ProfileInfoService` (`VideoPlayer.Mvu/ProfileInfoService.cs`)

- **Neue Konstruktorparameter:** `ILogger<ProfileInfoService>? logger = null`; `Debug.WriteLine` → `ILogger` (keine personenbezogenen Profildaten im Eintragstext).

### `ContinueWatchingMutationService` (`VideoPlayer.Mvu/ContinueWatchingMutationService.cs`)

- **Neue Konstruktorparameter:** `ILogger<ContinueWatchingMutationService>? logger = null`; `HideAsync`/`SkipAsync` protokollieren Ergebnis (`Information`/`Warning`).

### `CacheDatabase` / `BackupDatabase` (`VideoPlayer.Storage/`)

- **Neue Konstruktorparameter:** `ILogger<CacheDatabase>?` bzw. `ILogger<BackupDatabase>?` (Standard `null`); die `Debug.WriteLine`-Stelle im Recovery-Pfad von `InitializeWithRecoveryAsync` → `LogWarning` mit Exception. Signaturänderungen nur optional → die Singleton-Registrierung (`AddSingleton<CacheDatabase>`) sowie direkte Test-Erzeugungen bleiben kompilierbar.

### `ShellAppEffectHandler` (`VideoPlayer.Maui/Services/ShellAppEffectHandler.cs`)

- **Neue Aufrufe:** `logger.LogInformation`-Einträge in den oben gelisteten Effekt-Ausführungsmethoden sowie `LogWarning`-Einträge in den `SessionExpired`-/`DeviceTokenRejected`-Handlern und `LogInformation` im `AvailabilityChanged`-Handler; die 71 `LogError`-Pfade und der bestehende `LogInformation`-Fallback bleiben. Wiedergabe-/Download-Einträge enthalten den Anzeigetitel (`Player.Title` aus `MvuHost.Model` bzw. `EnqueueDownloadEffect.Title`).
- **Keine** neuen Konstruktorparameter (`ILogger<ShellAppEffectHandler>` vorhanden).

### `App` (`VideoPlayer.Maui/App.xaml.cs`)

- **Neue Konstruktorparameter:** `ILogger<App> logger` (erforderlich).
- **Geänderte Methoden:** `RunStartupAsync` — `Information`-Eintrag des Start-Workflows; `Debug.WriteLine` → `ILogger`.

### `AppShell` (`VideoPlayer.Maui/AppShell.xaml.cs`)

- **Neue Konstruktorparameter:** `ILogger<AppShell> logger` (erforderlich).
- **Geänderte Methoden:** Navigated-Dispatch, Quellen-Menü-Aufbau, Quellen-Neuladen, Quellenauswahl, Quellen-Icon — `Debug.WriteLine` → `LogWarning`/`LogError`; `Information`-Eintrag beim Quellenwechsel (Quellname, keine URLs).

### `PlayerPage` (`VideoPlayer.Maui/Pages/PlayerPage.xaml.cs`)

- **Neue Konstruktorparameter:** `ILogger<PlayerPage> logger` (erforderlich).
- **Geänderte Methoden:** `OnMediaOpened` (`Information` inkl. Anzeigetitel aus `mvuHost.Model.Player.Title` und Fortsetzungs-Seek), `OnMediaEnded` (`Information`), `OnMediaFailed`/`FailPlaybackStart` (`LogWarning`/`LogError` mit `logMessage`), `ApplyResumeSeekAsync` (`Information`); Fortschrittstakt-Catches → `LogWarning`; **kein** Eintrag im 5-s-Takt.

### `SettingsPage` (`VideoPlayer.Maui/Pages/SettingsPage.xaml.cs`)

- **Neue Konstruktorparameter:** `ILogger<SettingsPage> logger` (erforderlich); `DispatchSaveAsync`-catch → `ILogger`.

### `ServerSetupPage` (`VideoPlayer.Maui/Pages/ServerSetupPage.xaml.cs`)

- **Neue Konstruktorparameter:** `ILogger<ServerSetupPage> logger` (erforderlich); QR-Scan-Start-Fehlschlag → `ILogger`.

### `ServerHealthProbe` (`VideoPlayer.Maui/Services/ServerHealthProbe.cs`)

- **Neue Konstruktorparameter:** `ILogger<ServerHealthProbe> logger` (erforderlich); `CheckAsync`-Pfade → `ILogger` (Ergebnis als `Information`, Fehlschläge als `Warning`).

### `SignalRServerEventsConnection` (`VideoPlayer.Maui/Services/SignalRServerEventsConnection.cs`)

- **Neue Konstruktorparameter:** `ILogger<SignalRServerEventsConnection> logger` (erforderlich); Hub-Status/`Closed`/Reconnect-Ereignisse → `ILogger` (`Information`/`Warning`).

### `ConnectivityNetworkStatusMonitor` (`VideoPlayer.Maui/Services/ConnectivityNetworkStatusMonitor.cs`)

- **Neuer Konstruktor:** `ILogger<ConnectivityNetworkStatusMonitor> logger` (erforderlich); `AvailabilityChanged`-Auslösung → `LogInformation` (Online-/Offline-Wechsel).

### `PosterImageService` (`VideoPlayer.Maui/Services/PosterImageService.cs`)

- **Neue Konstruktorparameter:** `ILogger<PosterImageService> logger` (erforderlich); die 3 `Debug.WriteLine`-Fehlerpfade → `LogWarning` — **keine** Erfolgseinträge pro Bildabruf (Masseneintrags-Ausschluss).

### `PictureInPictureService` (`VideoPlayer.Maui/Services/PictureInPictureService.cs`)

- **Neue Konstruktorparameter:** `ILogger<PictureInPictureService> logger` (erforderlich); `ToggleAsync`/`RestoreAsync` → `Information` (Ergebnis); Fehlerpfad → `ILogger`.

### `SecureStorageSecretStore` (`VideoPlayer.Maui/Services/SecureStorageSecretStore.cs`)

- **Neue Konstruktorparameter:** `ILogger<SecureStorageSecretStore> logger` (erforderlich); Lesefehlschlag → `LogWarning` (Schlüsselname ohne Wert).

### `MauiProgram` (`VideoPlayer.Maui/MauiProgram.cs`)

- **Geänderte Registrierungen:** `Func<IServerEventsConnection>`-Lambda erhält `sp`-Parameter und erzeugt `SignalRServerEventsConnection` mit `ILogger<SignalRServerEventsConnection>`; `ISecretStore`-Lambda reicht `ILogger<SecureStorageSecretStore>` durch. Übrige Registrierungen (`AddSingleton<T>()`-Auflösung) bleiben — die optionalen/erforderlichen Logger-Parameter werden automatisch gefüllt.

## Datenbankmigrationen

Keine.

## Validierungsregeln

Keine — es werden keine neuen Benutzereingaben verarbeitet. Die Sparsamkeits-/Geheimnisregeln sind als Designentscheidung dokumentiert und über Tests abgesichert.

## Konfigurationsänderungen

| Eintrag | Typ | Standardwert | Zweck |
|---------|-----|--------------|-------|
| `PackageReference` `Microsoft.Extensions.Logging.Abstractions` 10.0.10 in `VideoPlayer.Mvu.csproj` | NuGet | – | `ILogger<T>`/`ILoggerFactory`/`NullLogger` in den MVU-Diensten (bisher nur transitiv über `VideoPlayer.Storage` verfügbar — explizite Referenz wie bei `VideoPlayer.ApiClient`) |

Keine `appsettings`-/`AppSettings`-Änderung: Das Verhalten bleibt vollständig am bestehenden Schalter `AppSettings.LoggingEnabled` gekoppelt; die `Information`-Mindestschwelle des `FileLogger` ist die einzige Level-Steuerung.

## Seiteneffekte und Risiken

- **DI-Konstruktorwahl bei `AuthSessionService`:** Drei öffentliche Konstruktoren — DI wählt den längsten auflösbaren (mit `DeviceTokenService`, restliche Parameter optional). Der neue `ILoggerFactory?`-Nachfolgeparameter ist optional; die Auswahl bleibt eindeutig. In der Implementierung prüfen, dass die Convenience-Konstruktoren die Fabrik konsistent durchreichen.
- **Test-Erzeugungsstellen:** Durch optionale Logger-Parameter bleiben die ~39 `new …Service(…)`-Aufrufe in `VideoWebPlayer.Maui.Tests` kompilierbar; neue Logger-Assertions laufen über `RecordingLoggerProvider`. `ShowDetailsCache.WriteAsync`/`ReadAsync` erhalten optionale Parameter — die vier Aufrufer-Dienste reichen ihren Logger durch, Fremdaufrufe (Tests) bleiben gültig.
- **Geheimnisse:** `PlaybackSource.Location` enthält `access_token` — strikt nur `Kind`/Medientyp/-Id (+ Titel auf Handler-Ebene) protokollieren; Anmeldeflüsse protokollieren Status, nie Zugangsdaten/Token und auch nicht `AuthAttempt.Username`. Abgesichert durch dedizierte Geheimnis-Tests.
- **Rauschen/Menge:** `PosterImageService` nur auf Warnpfaden, kein Fortschrittstakt, keine pro-Eintrag-Logs — sonst würde die 1-MiB-Rotation nutzbare Einträge schnell herausrotieren.
- **`LogProtocolContractTests`:** Der Pin `DoesNotContain("System.Diagnostics.Debug.WriteLine")` für `ShellAppEffectHandler.cs` bleibt wahr; neue `LogInformation`-/`LogWarning`-Aufrufe ändern nichts am Contract. Der beschlossene volle Umstellungsumfang wird über den neuen Contract-Test `NoDebugWriteLine_RemainsInAppSources` gepinnt (siehe Tests).
- **Framework-Rauschen:** `builder.Logging` hängt den `FileLoggerProvider` an alle Kategorien — Framework-Quellen (MAUI, SignalR) können Einträge auf `Information` erzeugen; akzeptiert (diagnostisch oft nützlich), da `FileLogger.IsEnabled` bei deaktiviertem Protokoll früh filtert.
- **Kosten bei deaktiviertem Protokoll:** `Log*`-Aufrufe sind billig (`FileLogger.IsEnabled` → `false`, kein Formatieren); `NullLogger` in Tests ist kostenfrei.
- **CS1591 (Warnung als Fehler):** Neue öffentliche/interne Member und Parameter brauchen deutsche XML-Doku.
- **`EnumCoverageTests`:** Kein neuer Enum — unberührt.

## Umsetzungsreihenfolge

1. **Paketreferenz:** `Microsoft.Extensions.Logging.Abstractions` 10.0.10 in `VideoPlayer.Mvu.csproj` ergänzen.
   - Voraussetzungen: Keine (Restore-fähig; Version deckungsgleich mit `VideoPlayer.Storage`/`VideoPlayer.ApiClient`).
2. **Test-Hilfsmittel:** `RecordedLogEntry`/`RecordingLoggerProvider` in `VideoWebPlayer.Maui.Tests/Stubs/` anlegen.
   - Voraussetzungen: `Microsoft.Extensions.Logging` 10.0.10 im Testprojekt (vorhanden).
3. **Storage-Logger:** `PendingProgressSyncService`, `CacheDatabase`, `BackupDatabase` — optionale `ILogger<T>?`-Parameter, `Debug.WriteLine`-Ersetzung, Einträge.
   - Voraussetzungen: Keine (Paketreferenz in `VideoPlayer.Storage` vorhanden).
4. **MVU-Logger (Kern):** `DeviceTokenService`, dann `AuthSessionService` (`ILoggerFactory?`, `CreateClient`-Verdrahtung inkl. kurzlebiger Clients), danach `ServerSyncService`, `ProgressReportService`, `ServerEventsService`, `DownloadService`, `AutoDownloadService`, `PlaybackSourceService`.
   - Voraussetzungen: Schritt 1.
5. **MVU-Logger (übrige Dienste):** `MediaDetailsService`, `HomeListService`, `OfflineContinueWatchingService`, `UpcomingEpisodesService`, `ProfileInfoService`, `ContinueWatchingMutationService`; `ShowDetailsCache`-Parameter + Durchreichen.
   - Voraussetzungen: Schritt 1.
6. **MAUI-Schicht:** `App`, `AppShell`, `PlayerPage`, `SettingsPage`, `ServerSetupPage`, `ServerHealthProbe`, `SignalRServerEventsConnection`, `ConnectivityNetworkStatusMonitor`, `PosterImageService`, `PictureInPictureService`, `SecureStorageSecretStore` — `ILogger<T>`-Parameter + Umstellung/neue Einträge.
   - Voraussetzungen: Keine (`ILogger<T>` über `Microsoft.Extensions.Logging.Debug`/MAUI-Logging verfügbar).
7. **`MauiProgram`-Lambdas:** `Func<IServerEventsConnection>`- und `ISecretStore`-Fabrik auf `sp`-basierte Logger-Injektion umstellen.
   - Voraussetzungen: Schritt 6.
8. **`ShellAppEffectHandler`-Breadcrumbs:** `LogInformation`/`LogWarning`-Einträge an den gelisteten Effekt-Pfaden und Ereignis-Handlern.
   - Voraussetzungen: Schritte 3–6 (die protokollierten Ergebniswerte stammen aus den Diensten).
9. **Unit-/Contract-Tests:** neue Logger-Assertion-Tests (`RecordingLoggerProvider`, u. a. `LogoutAsync_WritesEntry`) + Contract-Pins in `LogProtocolContractTests` (Debug-Restbestand `NoDebugWriteLine_RemainsInAppSources` sowie die Breadcrumb-Pins `ConnectivityMonitor_LogsAvailabilityChange`, `PictureInPictureService_LogsToggleRestore`, `PlayerPage_LogsPlaybackFailure`, `ShellAppEffectHandler_LogsLogoutBreadcrumb`, siehe Tests).
   - Voraussetzungen: Schritte 2–8.
10. **E2E-Test:** `LogProtocolTests` um den Inhalts-Nachweis `LoggingEnabled_RealFlows_WriteDiagnosticEntries` erweitern (Pflicht); optionale Teilflüsse Abmeldung/PiP/Wiedergabe-Fehlschlag können in denselben Fact einfließen.
    - Voraussetzungen: Schritte 3–8; App-Debug-Build via `AppFixture`.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `RecordedLogEntry`, `RecordingLoggerProvider` | `VideoWebPlayer.Maui.Tests/Stubs/` | Aufzeichnender `ILoggerProvider`/`ILoggerFactory`-Ersatz: Liste der Einträge mit Level, Kategorie, formatierter Meldung, Exception. |
| `LoginAsync_WritesEntries_WithoutCredentials` | `AuthSessionServiceTests` | Anmelde-Verlauf landet als `Information`-Eintrag; aufgezeichnete Meldungen enthalten weder Passwort, Token noch Benutzernamen. |
| `CreateClient_WiresApiClientLogger` | `AuthSessionServiceTests` / `VideoPlayerApiClientTests_RetryPolicy`-Kontext | Bei gesetzter `ILoggerFactory` trägt der erzeugte `VideoPlayerApiClient` einen `Logger` in `VideoPlayerApiClientOptions` → 401-/Wiederholungs-Warnungen werden aufgezeichnet. |
| `SynchronizeAsync_WritesResultCounters` | `ServerSyncServiceTests` | `Information`-Eintrag enthält `SyncResult`-Zähler (`SentProgressReports`/`RemainingProgressReports`). |
| `ReportProgressAsync_QueueAndDiscard_Logged` | `ProgressReportServiceTests` | Einreihen → `Information`; Verwerfen bei `IsPermanentClientError` → `Warning`. |
| `ConnectAsync_DisconnectAsync_StatusChanged_Logged` | `ServerEventsServiceTests` | Verbindungsauf-/-abbau und `StatusChanged` als `Information`; `ConnectionClosed` protokolliert. |
| `EnqueueAsync_TransferOutcome_Logged` | `DownloadServiceTests` | Auftragseingang/Abschluss/Fehlschlag erzeugen Einträge mit Medientyp/-Id und Anzeigetitel, nicht pro Teilschritt. |
| `ResolveAsync_LogsKind_NotLocation` | `PlaybackSourceServiceTests` | Eintrag enthält `PlaybackSourceKind` und Medientyp/-Id; aufgezeichnete Meldung enthält **nicht** `access_token` und nicht die vollständige `Location`. |
| `Enqueue_Remove_LoggedAggregated` | `PendingProgressSyncServiceTests` | Einreihen/Entfernen schreiben Anzahl-Einträge. |
| `RunAsync_LogsOutcome` | `AutoDownloadServiceTests` | Lauf-Ergebnis bzw. `MinRunInterval`-Auslassung als `Information`. |
| `LogoutAsync_WritesEntry` | `AuthSessionServiceTests` | Abmeldung landet als `Information`-Eintrag (`RecordingLoggerProvider`); aufgezeichnete Meldungen enthalten keine Zugangsdaten/Token-Werte. |
| `ConnectivityMonitor_LogsAvailabilityChange` | `LogProtocolContractTests` (Erweiterung) | Quelltext-Pins: `ConnectivityNetworkStatusMonitor.cs` enthält `ILogger<ConnectivityNetworkStatusMonitor>`-Injektion und `LogInformation` bei der `AvailabilityChanged`-Auslösung; `ShellAppEffectHandler.cs` enthält `LogInformation` im `AvailabilityChanged`-Handler (Online-/Offline-Wechsel — nicht testreferenzierbar, nicht E2E-automatisierbar). |
| `PictureInPictureService_LogsToggleRestore` | `LogProtocolContractTests` (Erweiterung) | Quelltext-Pins: `PictureInPictureService.cs` protokolliert `ToggleAsync`/`RestoreAsync` mit `LogInformation`; `ShellAppEffectHandler.cs` enthält den `TogglePictureInPicture`-Breadcrumb. |
| `PlayerPage_LogsPlaybackFailure` | `LogProtocolContractTests` (Erweiterung) | Quelltext-Pin: `PlayerPage.xaml.cs` enthält `LogWarning`/`LogError` in `OnMediaFailed`/`FailPlaybackStart` (mit `logMessage`, ohne `Location`-Inhalt). |
| `ShellAppEffectHandler_LogsLogoutBreadcrumb` | `LogProtocolContractTests` (Erweiterung) | Quelltext-Pin: `ShellAppEffectHandler.cs` enthält `LogInformation` im `LogoutAsync`-Pfad (Abmelde-Breadcrumb neben dem `AuthSessionService`-Eintrag). |
| `NoDebugWriteLine_RemainsInAppSources` | `LogProtocolContractTests` (Erweiterung) | Quelltext-Scan: `System.Diagnostics.Debug.WriteLine` kommt in `VideoPlayer.Mvu`, `VideoPlayer.Maui` und `VideoPlayer.Storage` nicht mehr vor (Altbestand `VideoWebPlayer.Maui` und `VideoPlayer.ApiClient`-Discovery ausgenommen). |

### Betroffene bestehende Tests

Keine — die Logger-Parameter sind optional (`= null`) bzw. nur in Klassen erforderlich, die das Testprojekt nicht referenziert (`VideoPlayer.Maui`); alle ~39 direkten `new …Service(…)`-Erzeugungsstellen bleiben kompilierbar. `AppLogServiceTests`, `FileLoggerTests`, `LogProtocolContractTests` und `AppUpdateTests_LogProtocol` bleiben unverändert gültig.

### E2E-Tests (primärer Funktionsnachweis)

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| Pflicht | Protokollierung einschalten → App-Neustart mit selbem Profil → Auto-Anmeldung und Wiedergabe laufen → Protokolldatei enthält mehr als die Marker-Zeile (≥ 1 `[INFO]`-Facheintrag; kein `access_token`, kein Benutzername/Passwort) | `VideoPlayer.E2E.Tests/Tests/LogProtocolTests.cs` — neuer Fact `LoggingEnabled_RealFlows_WriteDiagnosticEntries` | Bei aktivierter Protokollierung erzeugen reale Abläufe (Anmeldung, Wiedergabe) verwertbare Einträge jenseits der Marker — das Kerndefizit der Anforderung | Nur der reale UI-/Prozessfluss erzeugt die Kette `MvuHost` → `ShellAppEffectHandler` → Dienste → `FileLogger` → Datei; Unit-Tests prüfen Einzelausgaben, nicht die End-zu-End-Verdrahtung in die Datei |
| Optional | Teilfluss im selben Fact bzw. eigenem Fact: Abmeldung über die UI auslösen → Protokolldatei enthält Abmelde-Eintrag | `LogProtocolTests.cs` | Abmeldung sichtbar im Protokoll | UI-auslösbarer Flow; primärer Nachweis per `LogoutAsync_WritesEntry` (unit-testbar) und `ShellAppEffectHandler_LogsLogoutBreadcrumb` — E2E-Zusatz erhöht nur das Vertrauen in die Endverdrahtung |
| Optional | Teilfluss: PiP-Umschaltung während der Wiedergabe → Protokolleintrag | `LogProtocolTests.cs` | PiP-Umschaltung sichtbar im Protokoll | UI-auslösbar, aber plattformabhängig instabil; primärer Nachweis per `PictureInPictureService_LogsToggleRestore`-Contract-Pin |
| Optional | Teilfluss oder eigener Fact: Wiedergabe-Fehlschlag (z. B. `MockVideoServer` liefert nicht abspielbare Quelle) → `Warning`/`Error`-Eintrag in der Datei | `LogProtocolTests.cs` bzw. `PlaybackTests`-Umfeld | Wiedergabe-Fehlschlag sichtbar im Protokoll | anwendersichtbarer Fehlerfall; primärer Nachweis per `PlayerPage_LogsPlaybackFailure`-Contract-Pin, da der `MediaElement`-Fehlerpfad nicht unit-testbar ist |

Der Online-/Offline-Wechsel ist **nicht** als E2E-Szenario geplant: Die Netzverfügbarkeit lässt sich über die UI-Automatisierung (FlaUI) nicht umschalten — Nachweis ausschließlich per `ConnectivityMonitor_LogsAvailabilityChange`-Contract-Pins. Die optionalen Teilflüsse (Abmeldung, PiP, Wiedergabe-Fehlschlag) können in den Pflicht-Fact integriert werden, ohne dass neue Infrastruktur nötig ist; ihre primären Nachweise sind die oben genannten Unit-Tests/Contract-Pins, da `PlayerPage`, `PictureInPictureService` und `ConnectivityNetworkStatusMonitor` in `VideoPlayer.Maui` liegen und vom Unit-Testprojekt nicht referenziert werden können.

Bestehende E2E-Tests müssen nicht angepasst werden: Die vier `LogProtocolTests`-Facts und die übrige Suite werden durch rein additive Logger-Aufrufe nicht beeinflusst; der neue Test nutzt das vorhandene `StartAppAsync(profile:)`-Neustartmuster sowie das `MockVideoServer`-Wiedergabe-Setup nach Vorbild von `PlaybackTests` (Hilfsmethoden `NewServer`/`OpenDetailsAndPlayAsync` im neuen Fact bzw. als private Helfer der Testklasse duplizieren bzw. in `E2ETestBase` heben — kein bestehender Test betroffen).

## Offene Punkte

Keine — alle zuvor offenen Punkte wurden vom Anwender beantwortet und sind in den Designentscheidungen und Planabschnitten verankert (Anzeigetitel-Regel, voller `Debug.WriteLine`-Umstellungsumfang mit Ausnahmen, Ausschluss der Alt-App `VideoWebPlayer.Maui`, Verzicht auf Kopfdaten).
