# Services / Logik (`VideoWebPlayer.Maui/Services/`)

Alle Klassen liegen unter `VideoWebPlayer.Maui/Services/`. DI-Registrierung in `VideoWebPlayer.Maui/MauiProgram.cs`; mehrere Klassen sind zusätzlich Singletons mit statischem `Instance`-Accessor und greifen über `App.ServiceProvider` (statischer Service-Locator in `App.xaml.cs`) auf Dienste zu.

## `SettingsService` (`ISettingsService`)

Datei: `VideoWebPlayer.Maui/Services/SettingsService.cs` — Singleton (`AddSingleton<ISettingsService, SettingsService>`)

Persistenz über `Preferences.Default` (nicht geheim) und `SecureStorage.Default` (geheim). Schlüssel werden mit Profil-Präfix versehen (`GetPrefixedKey` → `ProfileManager.Instance.GetPreferencesPrefix()`). `GetMigratedSecureValue` migriert Legacy-Werte von `Preferences` nach `SecureStorage`.

| Member | Speicher | Beschreibung |
|--------|----------|--------------|
| `ServerAddress` / `HasServerAddress()` / `SetServerAddress` / `ClearServerAddress` | `Preferences`, Key `ServerAddress` | Einzelne Serveradresse (keine Serverliste vorhanden) |
| `GetUsername()` / `GetPassword()` / `HasCredentials()` / `SetCredentials` / `ClearCredentials` | `Preferences` (`Username`), `SecureStorage` (`Passkey`) | Credentials; `ClearCredentials` löscht auch den Auth-Token |
| `GetAuthToken()` / `SetAuthToken` / `ClearAuthToken` | `SecureStorage`, Key `AuthToken` | JWT; `SetAuthToken` entfernt evtl. Legacy-Preference |
| `PlaybackCacheRetentionDays` | `Preferences`, Default 1 | Aufbewahrung Cache-Downloads, Clamp 1–365 |
| `WatchlistCacheRetentionDays` | `Preferences`, Default 3 | Aufbewahrung Weiterschauen-Downloads |
| `DownloadRetentionDays` | `Preferences`, Default 7 | Aufbewahrung regulärer Downloads |
| `DiscoverServersAsync(timeoutMs = 3000)` | – | mDNS via `ZeroconfResolver.ResolveAsync("_http._tcp.local.")`, liefert `http://{ip}:{port}` |
| `DiscoverServersUdpAsync(port = 5001, timeoutMs = 2000)` | – | UDP-Broadcast `VIDEOWEBPLAYER_DISCOVERY` auf Port 5001; Antworten mit Präfix `VIDEOWEBPLAYER_SERVER:` werden als Adresse übernommen |

Nicht vorhanden: `KnownServers`-Liste, `LastConnectedServer`, Einstellungen für automatische Downloads / Auto-Fortsetzung / WLAN-only / Zeitfenster / Speicherplatzkontingent / Auto-Löschung (alle von der Anforderung gefordert).

## `ConnectionService` (`IConnectionService`)

Datei: `VideoWebPlayer.Maui/Services/ConnectionService.cs` — Singleton

| Methode | Beschreibung |
|---------|--------------|
| `TryConnectAsync(baseAddress, ct)` | Normalisiert Adresse (ergänzt `http://`), erstellt kurzlebigen `HttpClient` (8 s Timeout) mit Bearer-Token oder `X-API-Key`, ruft `VideoWebPlayerClient.HealthCheckAsync()` |
| `StartConnectionWorkflowAsync(viewModel, services, ct)` | Setzt `viewModel.IsLoading`/`IsOfflineMode`; Ablauf: Serveradresse vorhanden? → `NeedsServerSetup`; HealthCheck fehlgeschlagen → `Offline`; keine Credentials → `NeedsLogin`; `IAuthService.LoginAsync` → bei Erfolg `Connected`, sonst `NeedsLogin` |

`ConnectionState`-Enum (in `IConnectionService.cs`): `Connected`, `NeedsServerSetup`, `NeedsLogin`, `Offline`.

Nicht vorhanden: automatische Wiederverbindung bei Netzwerkwechsel (keine `Connectivity`-Nutzung im gesamten Projekt), Auswahl aus mehreren bekannten Servern.

## `AuthService` (`IAuthService`)

Datei: `VideoWebPlayer.Maui/Services/AuthService.cs` — Singleton; delegiert Speicherung an `ISettingsService`.

| Methode | Beschreibung |
|---------|--------------|
| `HasCredentials()` / `GetCredentials()` / `SaveCredentials` / `ClearCredentials` | Delegation an `ISettingsService` |
| `LoginAsync(username, password, ct)` | Nutzt `VideoWebPlayerClient` aus `App.ServiceProvider` (Fallback: temporärer Client mit Gate-Header); ruft `AuthenticateAsync`, speichert Token + Credentials; Rückgabe `(success, errorMessage)` |

## `ProfileManager`

Datei: `VideoWebPlayer.Maui/Services/ProfileManager.cs` — Singleton (`Instance`)

- `CurrentProfile` (Default `"default"`, wird aus Umgebungsvariable `VIDEOWEBPLAYER_PROFILE` gelesen, lowercase).
- `GetDatabaseFileName()`: `downloads.db3` bzw. `downloads_{profil}.db3`.
- `GetPreferencesPrefix()`: `""` bzw. `"{profil}_"`.

## `ClientDatabase`

Datei: `VideoWebPlayer.Maui/Services/ClientDatabase.cs` — Singleton (`Instance`)

- `SQLiteAsyncConnection` (`sqlite-net-pcl`) auf `FileSystem.AppDataDirectory/downloads[_profil].db3`.
- Erstellt Tabellen `Downloads` (`DownloadedVideo`) und `CarouselCache` (`CachedCarouselItem`) im Konstruktor (blockierend via `.Wait()`).
- `internal SQLiteAsyncConnection Database` + `internal SemaphoreSlim Lock` — beide werden von `DownloadManager` und `ElementCacheService` geteilt.
- Es gibt keine Tabelle/Queue für unsynchronisierte Fortschrittsmeldungen und kein Download-Manifest (beide von der Anforderung gefordert).
- Ablageort ist `AppDataDirectory`; eine Backup-Klassifikation bzw. ein Backup-Ausschluss existiert nicht (kein `IsExcludedFromBackup`, kein `CacheDirectory`-Einsatz im Code).

## `DownloadManager`

Datei: `VideoWebPlayer.Maui/Services/DownloadManager.cs` — Singleton (`Instance`), ca. 790 Zeilen

- Download-Verzeichnis: `FileSystem.AppDataDirectory/videos[_profil]`; Bilder unter `AppDataDirectory/images`.
- `MigrateAsync()`: ergänzt fehlende Spalten (`LocalPosterImagePath`, `LocalBannerImagePath`) und normalisiert `VideoType`-Werte (Legacy `Movie`/`episode` → `MediaTypes`-Konstanten).
- `NormalizeVideoType(videoType)` (`internal static`): Mapping auf `MediaTypes.Movie`/`MediaTypes.Episode`.
- `RequestVideoAsync(videoId, videoType, title)` → `VideoRequest`; `ResolveVideoSourceAsync` liefert lokale Datei (falls `Completed`-Download existiert) sonst Stream-URL + Fortsetzungsposition vom Server (`FetchResumePositionFromServerAsync` via `GetContinueWatchingAsync`); bei Stream wird automatisch ein Cache-Download gequeued.
- `QueueDownloadAsync(request, retentionType)` → erstellt `DownloadTask`, lädt Metadaten (`FetchVideoMetadataAsync`), enqueued in `DownloadQueue.Instance`; `ExpiresAt` je nach `DownloadRetentionType` aus den Retention-Settings.
- `SaveCompletedDownloadAsync(task, fileSizeBytes)`: lädt Poster/Banner lokal, Insert in DB, publiziert `DownloadCompletedEvent`.
- `UpdatePlaybackPositionAsync(...)`: speichert Position lokal in DB **und** sendet via `client.ReportPlaybackProgressAsync` an den Server (Fehler non-blocking geschluckt — keine Outbox).
- `DeleteDownloadAsync`, `GetDownloadAsync`, `GetAllDownloadsAsync` (nur `Completed`), `GetPlaybackPositionAsync`.
- `CleanupExpiredDownloadsAsync` + `SchedulePeriodicCleanupAsync` (alle 6 h): löscht abgelaufene Downloads inkl. Dateien, publiziert `DownloadDeletedEvent`.
- `SetEventPublisher(IPublishNotificationEvent)`: wird in `App.InitializeAfterServices` verdrahtet.
- Nicht vorhanden: parallele Downloads (Queue arbeitet sequenziell), Wiederaufnahme abgebrochener Downloads, Speicherplatzkontingent, Auto-Löschung gesehener Episoden, WLAN-only-Bedingung.

## `DownloadQueue`

Datei: `VideoWebPlayer.Maui/Services/DownloadQueue.cs` — Singleton (`Instance`), rein in-memory (`ConcurrentQueue<DownloadTask>` + `HashSet` Dedup)

- `EnqueueDownload(DownloadTask)`, `IsInQueue(videoId, videoType)`.
- `ProcessQueueAsync`: Hintergrund-Loop, verarbeitet **jeweils einen** Download (kein Parallelismus); HTTP-Timeout 30 min; schreibt Datei gestreamt; bei Fehler wird die Teildatei gelöscht; kein Retry/Resume.
- Events: `DownloadProgress` (`DownloadProgressEventArgs`), `DownloadCompleted` (`DownloadCompletedEventArgs` mit `Success`/`ErrorMessage`) — klassische .NET-Events, nicht das `NotificationEvent`-System.
- Weitere Typen in der Datei: `DownloadTask`, `DownloadProgressEventArgs`, `DownloadCompletedEventArgs`.

## `SignalRService`

Datei: `VideoWebPlayer.Maui/Services/SignalRService.cs` — Singleton, `IAsyncDisposable`

- `ConnectAsync(serverAddress, token)`: Hub `{server}/hubs/mediaupdate`, Bearer via `AccessTokenProvider`, `WithAutomaticReconnect()`.
- Empfangene Hub-Methoden: `ContinueWatchingUpdated`, `FavoritesChanged`, `NewVideosScanned(sourceId, count)`, `StatusChanged(message)` → jeweils als .NET-Event weitergereicht.
- `Closed`-Handler: nach 5 s manueller `StartAsync`-Versuch; `Reconnecting`/`Reconnected` nur Logging.
- `DisconnectAsync()`, `IsConnected`.

## `NotificationEventService` (`IPublishNotificationEvent`, `ISubscribeNotificationEvent`)

Datei: `VideoWebPlayer.Maui/Services/Events/NotificationEventService.cs` — Singleton

- Pub/Sub per `ConcurrentDictionary<Type, List<Delegate>>`; `Publish<TEvent>` ruft alle Handler synchron auf (Fehler pro Handler geschluckt).
- Übersetzt `SignalRService`-Events in `NotificationEvent`s: `ContinueWatchingUpdated` → `ContinueWatchingUpdatedEvent`, `FavoritesChanged` → `FavoritesChangedEvent`, `NewVideosScanned` → `NewVideosScannedEvent`, `StatusChanged` → `StatusChangedEvent`.

### Event-Typen (`Services/Events/`)

Basisklasse `NotificationEvent` (abstract, `Timestamp`). Konkrete Events:

| Event | Datei | Payload |
|-------|-------|---------|
| `ContinueWatchingUpdatedEvent` | `ContinueWatchingUpdatedEvent.cs` | – |
| `FavoritesChangedEvent` | `FavoritesChangedEvent.cs` | – |
| `NewVideosScannedEvent` | `NewVideosScannedEvent.cs` | `SourceId`, `Count` |
| `StatusChangedEvent` | `NewVideosScannedEvent.cs` (gleiche Datei) | `Message` |
| `DownloadCompletedEvent` | `DownloadCompletedEvent.cs` | `Download` (`DownloadedVideo`) |
| `DownloadDeletedEvent` | `DownloadDeletedEvent.cs` | `VideoId`, `VideoType`, `Title` |

Publiziert werden `DownloadCompletedEvent`/`DownloadDeletedEvent` von `DownloadManager`; die SignalR-abgeleiteten Events von `NotificationEventService` selbst. Abonniert u. a. von `MainPage` (UI-Refresh), `NotificationTicker` (Ticker-Meldungen), `WatchlistDownloadCoordinatorService`.

## `ElementCacheService`

Datei: `VideoWebPlayer.Maui/Services/ElementCacheService.cs` — Singleton (`Instance`)

- `GetCachedItemsAsync(carouselName)` / `SaveCachedItemsAsync(carouselName, items)` auf Tabelle `CarouselCache` (`CachedCarouselItem`), ersetzt jeweils den gesamten Carousel-Inhalt.
- Wird von `HomePageViewModel` genutzt (Startseiten-Listen aus Cache vor Server-Antwort).
- Kein Vorschaubild-Cache und kein Episodenlisten-Cache als eigene Dienste vorhanden.

## `WatchlistDownloadCoordinatorService`

Datei: `VideoWebPlayer.Maui/Services/WatchlistDownloadCoordinatorService.cs` — Singleton (DI)

- Abonniert `ContinueWatchingUpdatedEvent` → `SyncAsync(force: false)`; `RunOnStartupAsync()` → `SyncAsync(force: true)` (aufgerufen in `App.InitializeAfterStartupWorkflowOnline`).
- `SyncAsync`: `SemaphoreSlim` gegen parallele Läufe, 10-s-Throttle; lädt `GetContinueWatchingAsync` und queuet fehlende Einträge als `DownloadRetentionType.Watchlist` über `DownloadManager`.
- Referenzimplementierung für „automatische Downloads der Weiterschauen-Liste" — jedoch ohne Anzahl-Limit, WLAN-only-Prüfung oder Fortschritts-Nachsync.

## `AuthorizedImageHttpClientHandler`

Datei: `VideoWebPlayer.Maui/Services/AuthorizedImageHttpClientHandler.cs`

- `HttpClientHandler`, der `Authorization: Bearer` zu Requests hinzufügt (für `UriImageSource`-Bild-Requests gedacht, laut Kommentar).
- **Wird im Bestandscode nicht referenziert** (keine Registrierung gefunden) — ungenutzte Klasse.

## `App` / `MauiProgram` (Startlogik)

Dateien: `VideoWebPlayer.Maui/App.xaml.cs`, `VideoWebPlayer.Maui/MauiProgram.cs`

- `MauiProgram.CreateMauiApp()`: `UseMauiCommunityToolkit()`, `UseMauiCommunityToolkitMediaElement(true)`; typed `HttpClient` für `MauiVideoWebPlayerClient`; DI-Registrierungen (Singletons: `ISettingsService`, `IConnectionService`, `IAuthService`, `SignalRService`, `WatchlistDownloadCoordinatorService`, `NotificationEventService` + Interfaces; Transients: `ServerSetupPage`, `SettingsPage`, `LoginPage`, `LoadingPage`, `MainPage`, `HomePage`). Setzt `App.ServiceProvider`.
- `App.CreateWindow`: Root = `AppShell`.
- `App.InitializeAfterServices`: setzt `IPublishNotificationEvent` am `DownloadManager`.
- `App.InitializeAfterStartupWorkflowOnline`: startet `WatchlistDownloadCoordinatorService` (einmalig) und `InitializeSignalRAsync` (pollt bis zu 30 s auf Credentials/Token).
- `App.OnUnauthorizedReceived`: Re-Login mit gespeicherten Credentials, bei Misserfolg `SwitchToOfflineModeAsync` (HomePage mit `IsOfflineMode` + Alert).
- `App.SafeDisplayAlertAsync`: Thread-sichere Dialog-Hilfe.

## Nicht vorhanden (für die Anforderung relevant)

- Kein `Microsoft.Maui.Networking.Connectivity`-Einsatz — keine Online/Offline-Erkennung im Code.
- Keine Synchronisierungskomponente für Metadaten-Sync oder Nachsenden unsynchronisierter Fortschritte.
- Keine Outbox/Queue für Offline-Fortschrittsmeldungen.
- Keine Multi-Server-Verwaltung (nur eine `ServerAddress`).
- Kein PiP-Code, keine dedizierte Player-Page.
