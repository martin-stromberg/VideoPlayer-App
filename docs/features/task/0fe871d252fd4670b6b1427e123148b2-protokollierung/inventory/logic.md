# Logik: Effekt-Handler, MVU-Dienste, MAUI-Schicht, API-Client

Übersicht der von der Anforderung betroffenen Logikklassen mit ihren bestehenden Methoden, Logger-Verwendung und `Debug.WriteLine`-Stellen. Konvention im Codebestand: `System.Diagnostics.Debug.WriteLine($"[{Klassenname}] …")` ausschließlich in catch-Blöcken — Fehler werden protokolliert und geschluckt; Erfolgspfade sind still.

## `ShellAppEffectHandler`
Datei: `VideoPlayer.Maui/Services/ShellAppEffectHandler.cs` (2162 Zeilen)

Einzige Klasse der App mit injiziertem `ILogger` (`ILogger<ShellAppEffectHandler>`, Konstruktorparameter Z. 61, Feld Z. 31). Enthält kein `Debug.WriteLine` (per `LogProtocolContractTests` gepinnt).

- **Logging-Bestand:** 71× `logger.LogError(ex, "…")` auf Fehlerpfaden; 1× `logger.LogInformation` (Z. 375: „Mailentwurf nicht verfügbar ({Detail}) — Teilen-Fallback."); kein `LogWarning`.
- **Konstruktor-Abhängigkeiten:** `IServiceProvider`, `IServerHealthProbe`, `AuthSessionService`, `DevicePairingClient`, `DeviceTokenService`, `AppSecretsService`, `INetworkStatusMonitor`, `ServerEventsService`, `DownloadService`, `AutoDownloadService`, `AppLogService`, `ILogger<ShellAppEffectHandler>`.
- **`HandleAsync(AppEffect, CancellationToken)`** (Z. 99–326): `switch` über alle 73 `AppEffect`-Ausprägungen (73 Records in `AppEffect.cs`, je ein `case`-Zweig) — der zentrale, von der Anforderung benannte Breadcrumb-Punkt. Ausgewählte Zweige: `InitializeStorageEffect` → `InitializeStorageAsync` (Z. 1612), `AttemptAutoLoginEffect` → `AttemptAutoLoginAsync` (Z. 1536), `AttemptLoginEffect` → `AttemptLoginAsync` (Z. 1554), `RunFullSyncEffect` → `RunFullSyncAsync` (Z. 540), `ConnectServerEventsEffect` → `ConnectServerEventsAsync` (Z. 514), `ResolvePlaybackSourceEffect` → `ResolvePlaybackSourceAsync` (Z. 412), `EnqueueDownloadEffect` → `EnqueueDownloadAsync` (Z. 1442), `LogoutEffect` → `LogoutAsync` (Z. 1411), `SendLogProtocolEffect` → `SendLogProtocolAsync` (Z. 339).
- **Protokoll-Bezug:** `SendLogProtocolAsync` (Z. 339–393) holt Anhänge via `appLog.GetAttachmentPaths()`, verschickt `Email.ComposeAsync` an `LogProtocolRecipient` (`mstromberg84+heimkino@gmail.com`, Z. 34) mit `Share.RequestAsync`-Fallback und meldet `SendLogProtocolCompletedMessage` (Fehlertexte `AppUpdate.LogProtocolEmptyError`/`LogSendFailedError`). `appLog.SetEnabled(settings.LoggingEnabled)` in `LoadSettingsAsync` (Z. 1670) und `SaveSettings` (Z. 1684).
- **Abonnierte Events:** `authSession.SessionExpired`, `deviceTokens.DeviceTokenRejected`, `networkMonitor.AvailabilityChanged`, `serverEvents.EventReceived`, `serverEvents.ConnectionClosed`, `downloads.ItemChanged`, `downloads.ItemRemoved` — alle über `MainThread`/`DispatchMessageAsync` in den MVU-Zyklus.

## MVU-Dienste (`VideoPlayer.Mvu`)

Kein Dienst nimmt heute einen `ILogger` entgegen; sämtliche Diagnose läuft über `Debug.WriteLine`. Die Konstruktoren sind auch die Test-Erzeugungspunkte (`VideoWebPlayer.Maui.Tests` erzeugt die Dienste direkt — Signaturänderungen treffen ~39 `new …Service(…)`-Aufrufe in den Tests).

| Dienst (Datei in `VideoPlayer.Mvu/`) | Konstruktor-Signatur(en) | `Debug.WriteLine`-Stellen | Wesentliche Methoden / Events |
|------|--------------------------|---------------------------|-------------------------------|
| `AuthSessionService` (`AuthSessionService.cs`) | `(AppSecretsService)`, `(AppSecretsService, Func<Uri,HttpClient>?, Func<TimeSpan,CT,Task>?)`, `(AppSecretsService, DeviceTokenService, Func<Uri,HttpClient>?, Func<TimeSpan,CT,Task>?)` | 9 | `TryStoredLoginAsync`, `LoginAsync`, `LogoutAsync`; `Client`, `ClientBaseAddress`; Event `SessionExpired`; privat: `TryStoredTokenAsync`, `LoginCoreAsync`, `CreateClient` (Z. 319 — erzeugt `VideoPlayerApiClientOptions` **ohne** `Logger`), `SilentReloginAsync`, `TryRefreshSessionAsync`, `RevokeRefreshTokenAsync` (kurzlebige Clients ebenfalls ohne `Logger`), `HandleGateRejectionAsync` |
| `ServerSyncService` (`ServerSyncService.cs`) | `(AuthSessionService, PendingProgressSyncService, CacheDatabase)` | 3 | `SynchronizeAsync` → `SyncResult` (liefert Zähler `SentProgressReports`/`RemainingProgressReports` + Sync-Flags), `FlushPendingProgressAsync`, `SyncMetadataAsync`, `SyncContinueWatchingAsync`, `SyncFavoritesAsync`, privat `TrySyncAsync`; Cache-Schlüssel-Konstanten `SourcesCacheKey`/`RecentCacheKey`/`ContinueWatchingCacheKey` |
| `ProgressReportService` (`ProgressReportService.cs`) | `(AuthSessionService, PendingProgressSyncService, INetworkStatusMonitor)` | 2 | `ReportProgressAsync` (sendet via `client.ReportPlaybackProgressAsync` oder reiht in `pendingQueue` ein), `internal static IsPermanentClientError(ApiException)` |
| `ServerEventsService` (`ServerEventsService.cs`) | `(AuthSessionService, Func<IServerEventsConnection>)` | 5 | `ConnectAsync`, `DisconnectAsync`, `DisposeAsync`; `IsConnected`, `HubPath`; Events `EventReceived`, `ConnectionClosed`; `OnStatusChanged` (Z. 157) schreibt `StatusChanged`-Meldungen nur per `Debug.WriteLine` |
| `DownloadService` (`DownloadService.cs`) | `(AuthSessionService, AppSecretsService, CacheDatabase, AppStorageLayout, INetworkStatusMonitor)` + `internal`-Overload (Test-Injektionen `httpClientFactory`/`fileExists`/`quotaBytesOverride`) | 7 | `LoadItemsAsync`, `GetUsedBytes`, `EnqueueAsync` → `DownloadEnqueueResult`, `CancelAsync`, `DeleteAsync`, `DeleteIfCompletedAsync`, `ResumeInterruptedAsync`; Events `ItemChanged`, `ItemRemoved`; Konstanten `MaxParallelDownloads=2`, `Error*`/`Notice*` |
| `AutoDownloadService` (`AutoDownloadService.cs`) | `(AuthSessionService, HomeListService, DownloadService, INetworkStatusMonitor, CacheDatabase)` + `internal`-Overload (`utcNow`) | 2 | `RunAsync(AppSettings, bool force, CT)` mit `MinRunInterval=60s`-Throttle; privat `EnqueueNextEpisodesAsync`, `EnqueueNextPlaylistEntriesAsync` |
| `PlaybackSourceService` (`PlaybackSourceService.cs`) | `(AuthSessionService, AppSecretsService, CacheDatabase)` + `internal`-Overload (`fileExists`) | 3 | `ResolveAsync(mediaType, mediaId, CT, allowServerStream, resumePositionSecondsOverride)` → `PlaybackSource?`; privat `ResolveResumePositionAsync`, `ReadCachedResumePositionAsync`, `ResolveLocalFileAsync`, `FindResumePosition`. **Achtung:** erzeugte `Location` enthält `?access_token=` (Z. 131) — nicht protokollierbar |
| `MediaDetailsService` (`MediaDetailsService.cs`) | `(AuthSessionService, CacheDatabase)` | 4 | `LoadAsync(mediaType, mediaId, fallbackTitle, showId, seasonId, episodeId, CT)` → `MediaDetailsView` |
| `HomeListService` (`HomeListService.cs`) | `(CacheDatabase)` | 5 | `LoadHomeListsAsync` → `HomeLists`, `LoadFavoritesAsync`, `LoadRecentAsync`, `LoadFavoriteItemsAsync` |
| `OfflineContinueWatchingService` (`OfflineContinueWatchingService.cs`) | `(CacheDatabase, BackupDatabase, HomeListService)` + `internal`-Overload (`fileExists`) | 3 | `LoadHomeListsAsync` → `HomeLists` (Offline-Fortsetzung aus Manifest + Outbox) |
| `UpcomingEpisodesService` (`UpcomingEpisodesService.cs`) | `(AuthSessionService, CacheDatabase)` | 1 | `LoadAsync` → `IReadOnlyList<PlayerEpisodeRef>` |
| `ShowDetailsCache` (`ShowDetailsCache.cs`) | `static` (kein DI-Zustand) | 2 | `KeyFor`, `WriteAsync`, `ReadAsync`, `OrderedEpisodes`, `NextEpisodeAfter`, `NextEpisodeAfterId` |
| `DeviceTokenService` (`DeviceTokenService.cs`) | `(AppSecretsService)`, `(AppSecretsService, Func<Uri,HttpClient>?)` | 6 | `LoadForServerAsync`, `SetPairedTokenIfCurrentAsync`, `DiscardCurrentAsync`, `DiscardIfCurrentAsync`, `IsGateRejectedAsync`, `NotifyRejected`; `CurrentServerAddress`; Event `DeviceTokenRejected` |
| `ProfileInfoService` (`ProfileInfoService.cs`) | `(AppSecretsService, AppProfile)` | 1 | `LoadAsync` → `ProfileInfo` |
| `ContinueWatchingMutationService` (`ContinueWatchingMutationService.cs`) | `(AuthSessionService, ServerSyncService, CacheDatabase)` | 0 | `HideAsync`, `SkipAsync` (beide → `bool`) |
| `FavoritesService` (`FavoritesService.cs`) | `(AuthSessionService, ServerSyncService)` | 0 | `ToggleAsync` → `bool?`, `RemoveFavoriteAsync` → `bool` |
| `SourceBrowserService` (`SourceBrowserService.cs`) | `(AuthSessionService)` | 0 | `LoadSourcesAsync`, `LoadGenresAsync`, `LoadItemsAsync` |
| `PlaylistService` (`PlaylistService.cs`) | `(AuthSessionService, AppSecretsService? = null)` | 0 | Umfangreiche Playlist-API (Laden, Navigation, Mutationen); nicht im Fokus der Anforderung |

Storage-seitig (Namespace `VideoPlayer.Storage`): `PendingProgressSyncService` (`PendingProgressSyncService.cs`, `(BackupDatabase)`; `EnqueueAsync`, `GetPendingAsync`, `CountAsync`, `RemoveForMediaAsync`; **kein** `Debug.WriteLine`), `CacheDatabase`/`BackupDatabase` je 1 `Debug.WriteLine` im Recovery-Pfad von `InitializeWithRecoveryAsync`.

## MAUI-Schicht (`VideoPlayer.Maui`)

Keine Klasse außer `ShellAppEffectHandler` nutzt `ILogger`. Alle folgenden Klassen haben `Debug.WriteLine`-Stellen bzw. sind von der Anforderung für neue Meldungen benannt.

| Klasse (Datei in `VideoPlayer.Maui/`) | Konstruktor / DI | `Debug.WriteLine`-Stellen | Relevante Methoden / Stellen |
|------|------------------|---------------------------|------------------------------|
| `App` (`App.xaml.cs`) | `(MvuHost, IServiceProvider)` — `UseMauiApp<App>` | 1 | `CreateWindow` (Ladeanzeige, DevApi-Start), `RunStartupAsync` → `StartupRequestedMessage` (Z. 59–69; App-Start-Breadcrumb-Punkt) |
| `AppShell` (`AppShell.xaml.cs`) | `(MvuHost, PosterImageService)` — `AddSingleton` | 5 | Navigated-Dispatch (Z. 123), Quellen-Menü-Aufbau (Z. 144), Quellen-Neuladen (Z. 296), Quellenauswahl (Z. 308), Quellen-Icon (Z. 326) |
| `PlayerPage` (`Pages/PlayerPage.xaml.cs`) | `(MvuHost, IPictureInPictureService)` — `AddTransient` | 5 | `OnMediaOpened` (Z. 297, merkt Fortsetzungs-Seek), `OnMediaEnded` → `PlaybackEndedMessage` (Z. 389), `OnMediaFailed` → `PlaybackFailedMessage` (Z. 397), `FailPlaybackStart` (Z. 287, protokolliert `logMessage` per `Debug.WriteLine` Z. 289), `ApplyResumeSeekAsync` (Z. 372); **5-s-Fortschrittstakt** `StartProgressTimer`/`ReportPosition` (Z. 128–180 → `PlaybackProgressMessage`) — laut Anforderung ausdrücklich nicht zu protokollieren |
| `SettingsPage` (`Pages/SettingsPage.xaml.cs`) | `(MvuHost)` — `AddTransient` | 1 | `OnLoggingToggled` → `SaveSettingsRequestedMessage` (Z. 125–128), `OnSendLogClicked` → `SendLogProtocolRequestedMessage` (Z. 160–164), `SendLogSection.IsVisible = settings.LoggingEnabled` (Z. 87), `DispatchSaveAsync`-catch (Z. 210) |
| `ServerSetupPage` (`Pages/ServerSetupPage.xaml.cs`) | `(MvuHost)` — `AddTransient` | 1 | QR-Scan-Start-Fehlschlag (Z. 234) |
| `ServerHealthProbe` (`Services/ServerHealthProbe.cs`) | `(IServerAddressProvider, AppSecretsService)` — `AddSingleton<IServerHealthProbe>` | 2 | `CheckAsync()`/`CheckAsync(Uri)` → `HealthCheckResult` (Z. 63, 88) |
| `SignalRServerEventsConnection` (`Services/SignalRServerEventsConnection.cs`) | Fabrik-Lambda `Func<IServerEventsConnection>` in `MauiProgram` Z. 87–88 | 0 | `ConnectAsync` (`HubConnection` mit `WithAutomaticReconnect`, hub events → `ContinueWatchingUpdated`/`FavoritesChanged`/`NewVideosScanned`/`StatusChanged`/`Closed`), `DisconnectAsync`, `DisposeAsync` |
| `ConnectivityNetworkStatusMonitor` (`Services/ConnectivityNetworkStatusMonitor.cs`) | `AddSingleton<INetworkStatusMonitor>` | 0 | `IsOnline`, `IsWifi`, `Start` → `AvailabilityChanged` bei `ConnectivityChanged` |
| `PosterImageService` (`Services/PosterImageService.cs`) | `(AuthSessionService, AppStorageLayout)` — `AddSingleton` | 3 | `GetImageSourceAsync`, `GetSourceIconSourceAsync`, `GetPlaylistCoverSourceAsync` — **per Bildabruf** (Anforderung: keine Masseneinträge) |
| `PictureInPictureService` (`Services/PictureInPictureService.cs`) | `AddSingleton<IPictureInPictureService>` | 1 | `ToggleAsync` → `bool`, `RestoreAsync`; `IsToggleSupported`, `IsActive` |
| `SecureStorageSecretStore` (`Services/SecureStorageSecretStore.cs`) | `(ISecureStorage)` — `AddSingleton<ISecretStore>` | 1 | `GetAsync`/`SetAsync`/`RemoveAsync` (Lesefehlschlag Z. 33) |

## API-Client (`VideoPlayer.ApiClient`)

`VideoPlayerApiClientOptions` (`VideoPlayerApiClientOptions.cs`) besitzt den Haken `public ILogger? Logger { get; set; }` (Z. 38; Kommentar „Retries, Token-Erneuerung"). `VideoPlayerApiClient.SendWithPolicyAsync` (`VideoPlayerApiClient.cs`, Z. 1142–1209) nutzt ihn an drei Stellen via `options.Logger?.LogWarning`:

| Zeile | Ereignis | Meldung |
|-------|----------|---------|
| 1171 | Transportfehler-Wiederholung | `Transportfehler bei {Endpoint}; Wiederholung {Attempt} nach {Delay}.` |
| 1182 | 401 → Token-Erneuerung | `401 von {Endpoint}; versuche einmalige Token-Erneuerung.` |
| 1200 | Wiederholbarer Status | `Status {StatusCode} von {Endpoint}; Wiederholung {Attempt} nach {Delay}.` |

`AuthSessionService.CreateClient` (Z. 319–328) erzeugt die Options mit `BaseAddress`, `DelayAsync`, `TokenRefresher` — `Logger` bleibt `null`; gleiches gilt für die kurzlebigen Clients in `RevokeRefreshTokenAsync` (Z. 198–202), `SilentReloginAsync` (Z. 365–369) und `TryRefreshSessionAsync` (Z. 433–437). `DevicePairingClient` (MauiProgram Z. 49–51) besitzt keinen Logger-Parameter. `AuthSessionService` hat aktuell keinen `ILoggerFactory`-/`ILogger`-Parameter, um den Haken zu bedienen.

## `Debug.WriteLine`-Bestand (Ist-Zählung)

| Projekt | Aufrufe | Verteilung |
|---------|---------|------------|
| `VideoPlayer.Mvu` | 53 | `AuthSessionService` 9, `DownloadService` 7, `DeviceTokenService` 6, `ServerEventsService` 5, `HomeListService` 5, `MediaDetailsService` 4, `ServerSyncService` 3, `PlaybackSourceService` 3, `OfflineContinueWatchingService` 3, `ShowDetailsCache` 2, `ProgressReportService` 2, `AutoDownloadService` 2, `UpcomingEpisodesService` 1, `ProfileInfoService` 1 |
| `VideoPlayer.Maui` | 20 | `PlayerPage` 5, `AppShell` 5, `PosterImageService` 3, `ServerHealthProbe` 2, `App` 1, `SettingsPage` 1, `ServerSetupPage` 1, `PictureInPictureService` 1, `SecureStorageSecretStore` 1 |
| `VideoPlayer.Storage` | 2 | `CacheDatabase` 1, `BackupDatabase` 1 |
| `VideoPlayer.ApiClient` | 3 | `Discovery/UdpServerDiscovery`, `Discovery/ServerDiscoveryService`, `Discovery/MdnsServerDiscovery` (je 1 — nicht im Fokus der Anforderung) |
| `VideoWebPlayer.Maui` (Altbestand) | 205 | Außerhalb des Geltungsbereichs (kein Protokoll-Schalter) |
