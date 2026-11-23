# Logik — Bestandsaufnahme

Dateipfade relativ zum Repository-Root. Alle Klassen in `VideoPlayer.Mvu`, `VideoPlayer.Storage`, `VideoPlayer.ApiClient` und `msTools.DeveloperApi` sind plattformneutral (`net10.0`, keine MAUI-Abhängigkeit). MAUI-gebundene Klassen stehen im Abschnitt „App-Rand (VideoPlayer.Maui)".

# MVU-Kern (`VideoPlayer.Mvu`)

## `MvuHost`
`VideoPlayer.Mvu/MvuHost.cs` — MVU-Ausführung (`INotifyPropertyChanged`).

| Member | Sichtbarkeit | Beschreibung |
|---|---|---|
| `MvuHost(IAppEffectHandler, Func<AppModel,AppMessage,AppTransition>?)` | public | Injiziert Effekt-Handler und Update-Funktion (Default `AppUpdate.Update`) |
| `Model` | public get | Aktuelles unveränderliches `AppModel` |
| `ModelChanged` | public event | Nach jeder Modelländerung |
| `PropertyChanged` | public event | `INotifyPropertyChanged` für Bindungen |
| `DispatchAsync(AppMessage, CancellationToken)` | public | Serialisiert Updates über `SemaphoreSlim gate`, feuert Events, führt Effekte der Reihe nach aus |

## `AppUpdate`
`VideoPlayer.Mvu/AppUpdate.cs` (4448 Zeilen) — `static` Update-Funktion.

| Member | Sichtbarkeit | Beschreibung |
|---|---|---|
| `Update(AppModel, AppMessage)` | public | Zentrale `switch`-Verteilung auf ~135 private `Update*`-Handler |
| Fehlerkonstanten (`InvalidAddressError`, `PlaybackUnavailableError`, `LogSendFailedError`, Playlist-/Pairing-/Bootstrap-Texte …) | public const | Inline-Fehlertexte |
| `PlaybackEndedToleranceSeconds` = 300 | private const | Toleranzfenster der Ende-Plausibilität |
| `EndWindowRequiredTicks` = 2 | private const | Bestätigte Fenstereintritte für „angesehen" |
| `UpdatePlaybackProgress` | private | Takt-Verarbeitung: Positionsupdate, Fortschrittsmeldung (`ReportPlaybackProgressEffect`, >0,5 s Änderung), Wiedergabe-Schutz (`ProtectDownloadExpiryEffect` bei `LocalFile`), `EvaluateEndWindow` |
| `EvaluateEndWindow` / `IsProgressTickPlausible` | private | Streak-Logik `EndWindowStreak` (plausible Takte im `WatchedEndWindowSeconds`-Fenster, ≥2 bestätigt → `IsWatched`; optional `ExpireWatchedDownloadEffect` bei `AutoDeletePlayedDownloads`) |
| `UpdatePlaybackEnded` / `IsPlaybackEndPlausible` | private | Ende nur bei Restlaufzeit ≤ 300 s plausibel → `IsWatched`, End-Fortschrittsmeldung, Auto-Resume (`AutoResumeEnabled` → `AdvancePlaylistEntry` im Playlist-Kontext sonst `AdvanceToNextEpisode`) |
| `UpdatePlaybackFailed` | private | Fehler-Rückfall auf nächsten Eintrag (Playlist-`advance` bzw. Serienliste) |
| `UpdateNetworkAvailabilityChanged` | private | Offline-Übergang / Online-Rückkehr (`CheckServerHealthEffect`); bei bestehender Verbindung `ResumeDownloadsEffect` |
| `UpdateLoginCompleted`/`UpdateAutoLoginCompleted` | private | Verbindungsaufbau: `LoadSourcesEffect`, `ConnectServerEventsEffect`, `RunFullSyncEffect`, `ResumeDownloadsEffect`, Hero-/Header-Nachladen |
| `UpdateSynchronizationCompleted` / `UpdateCachedListsChanged` | private | `LoadHomeListsEffect`, Quellen-Reload, `RunAutoDownloadsEffect` bei `AutoDownloadEnabled` |
| `UpdateDownloadsPageOpened` | private | `LoadDownloadsEffect` (öffnet Abgleich beim Seitenaufruf) |
| `UpdatePlayRequested` / `UpdatePlaybackSourceResolved` | private | Wiedergabestart (`ResolvePlaybackSourceEffect`, `LoadUpcomingEpisodesEffect`, `NavigateToPlayerEffect`) |
| `MarkEpisodeWatchedInDetails` | private | Gesehen-Badge der Detailseite ohne Reload |
| weitere ~110 `Update*`-Handler | private | Server-Setup/Discovery/Pairing/Bootstrap, Login/Logout/SessionExpired, Home-Listen, Quellen-Browser (Genre/Suche/Paging), Detailseite, Favoriten, Playlists (lesend **und** gesamter Verwaltungsfluss), Profil, Downloads (Enqueue/Cancel/Retry/Delete/Purge/DeleteFailed), PiP, Navigation, Protokollversand |

**Bestehender Abgleich-/Bereinigungs-Trigger:** `ResumeDownloadsEffect` wird bei Verbindungsaufbau (Start) und Online-Rückkehr ausgelöst; `LoadDownloadsEffect` beim Öffnen der Downloads-Seite. **Es existiert kein Lebenszyklus-/Aktivierungs-Trigger** (keine Message für `WillEnterForeground`/`DidBecomeActive`); `window.Resumed` in `App.xaml.cs` dispatched nur `HomeListsReloadRequestedMessage`.

## `AppMessage` / `AppEffect`
`VideoPlayer.Mvu/AppMessage.cs` (1174 Zeilen, 135 Messages), `VideoPlayer.Mvu/AppEffect.cs` (805 Zeilen, ~75 Effekte) — abstrakte `record`-Basistypen mit `sealed record`-Subtypen; Namensschema `*Message`/`*Effect`. Download-/Player-relevante: `DownloadsPageOpenedMessage`, `DownloadRequestedMessage`, `EnqueueDownloadEffect`, `ResumeDownloadsEffect`, `PurgeOrphanedDownloadsEffect`, `ExpireWatchedDownloadEffect`, `ProtectDownloadExpiryEffect`, `PlaybackProgressMessage(MediaId, Position, Duration, IsPlaying, IsBuffering)`, `PlaybackEndedMessage`, `PlaybackFailedMessage`, `ReportPlaybackProgressEffect`, `LoadUpcomingEpisodesEffect`, `StartPlaylistPlaybackEffect`, `AdvancePlaylistEntryEffect`, `NavigateToPlayerEffect`, `PlayerClosedMessage`.

# Mvu-Services (`VideoPlayer.Mvu`)

## `AuthSessionService`
`VideoPlayer.Mvu/AuthSessionService.cs` (595 Zeilen)

| Member | Sichtbarkeit | Beschreibung |
|---|---|---|
| `Client` / `ClientBaseAddress` | public get | Angemeldeter `VideoPlayerApiClient` samt Basisadresse |
| `SessionExpired` | public event | Endgültig abgelehnte stille Neuanmeldung |
| `TryStoredLoginAsync(Uri, ct)` | public | Automatische Anmeldung mit gespeicherten Daten (`AuthAttempt`) |
| `LoginAsync(...)` | public | Manuelle Anmeldung (E-Mail/Passwort) |
| `LogoutAsync` | public | Abmeldung (Refresh-Token-Widerruf best effort) |
| interne Refresh-Logik | private | `api/auth/refresh` mit rotiertem Refresh-Token, Fallback Re-Login; verdrahtet `TokenRefresher` in `VideoPlayerApiClientOptions` |

## `DeviceTokenService`
`VideoPlayer.Mvu/DeviceTokenService.cs` (340 Zeilen)

| Member | Sichtbarkeit | Beschreibung |
|---|---|---|
| `CurrentServerAddress` | public get | Aktuell geladener Serverkontext |
| `DeviceTokenRejected` | public event | Serverseitig verworfenes Geräte-Token (Gate-Probe 401) |
| `LoadForServerAsync(string?, ct)` | public | Geräte-Token des Servers laden → `ClientGate.DeviceTokenProvider` |
| `SetPairedTokenIfCurrentAsync(...)` | public | Neues Pairing-Token sicher hinterlegen |
| `DiscardCurrentAsync` / `DiscardIfCurrentAsync(string)` | public | Token verwerfen (lokal/bei Serverwechsel) |
| `IsGateRejectedAsync(string, ct)` | public | Gate-Probe gegen den Server (401 → verworfen) |
| `NotifyRejected` | public | Ereignis auslösen |

## `ServerDiscoveryService` (siehe ApiClient-Abschnitt), `BootstrapTicketParser`, `PairingCode`
`BootstrapTicketParser.cs` (`static TryParse` → `BootstrapTicketInput`, Konstanten `PairingSegment`=`pairing`, `TicketParameter`=`t`), `PairingCode.cs` (`TryNormalize`, `MinLength`=4, `Alphabet` ohne I/L/O/0/1).

## `HomeListService`
`VideoPlayer.Mvu/HomeListService.cs` (368 Zeilen)

| Methode | Sichtbarkeit | Beschreibung |
|---|---|---|
| `LoadHomeListsAsync` | public | Drei Startseiten-Listen + saisonale Genres aus `cached_lists`/`cached_favorites` (Cache-First); beschädigte Einträge → `DiscardedCaches` |
| `LoadFavoritesAsync` / `LoadRecentAsync` / `LoadFavoriteItemsAsync` / `LoadSeasonalGenresAsync` | public | Einzel-Loader |
| `FavoritesCacheName` = `cached_favorites` | internal const | Cache-Tabellenname |
| `ReadListAsync<T>` / `ReadFavoritesAsync` / `Discard*` | private | JSON-Lesen, Verwerfen beschädigter Zeilen |
| `MapAll/MapContinueWatching/MapEntry/MapFavorite/MediaHints/MediaTypeOf` | private static | Server-DTO → `HomeMediaItem`/`FavoriteListItem` |

## `ServerSyncService`
`VideoPlayer.Mvu/ServerSyncService.cs` (271 Zeilen)

| Member | Sichtbarkeit | Beschreibung |
|---|---|---|
| `SourcesCacheKey`/`RecentCacheKey`/`SeasonalGenresCacheKey`/`ContinueWatchingCacheKey` | public const | `sources`, `recent`, `seasonal_genres`, `continue_watching` |
| `SynchronizeAsync` | public | Voll-Sync (Quellen, Recent, saisonale Genres, Weiterschauen, Favoriten, ausstehende Fortschritte) → `SyncResult` |
| `FlushPendingProgressAsync` | public | `pending_progress`-Outbox senden (`RemoveForMediaAsync` bei Erfolg/410) |
| `SyncMetadataAsync` / `SyncSeasonalGenresAsync` / `SyncContinueWatchingAsync` / `SyncFavoritesAsync` | public | Teil-Synchronisationen (Cache-Schreiben) |

## `ServerEventsService`
`VideoPlayer.Mvu/ServerEventsService.cs` (222 Zeilen) — `IAsyncDisposable`; `HubPath` = `hubs/mediaupdate`.

| Member | Sichtbarkeit | Beschreibung |
|---|---|---|
| `EventReceived` | public event | `ServerEventReceivedEventArgs` (Kind/SourceId/Count) |
| `ConnectionClosed` | public event | Endgültige Trennung |
| `ConnectAsync(Uri, ct)` / `DisconnectAsync` | public | Verbindungssteuerung über `Func<IServerEventsConnection>`; Token via `AuthSessionService` |

## `SourceBrowserService`
`VideoPlayer.Mvu/SourceBrowserService.cs` — `AllGenresName` = `Alle`.

| Methode | Beschreibung |
|---|---|
| `LoadSourcesAsync` | `api/Sources` → `SourceItem` |
| `LoadGenresAsync(sourceId)` | `api/SourceGenres/{id}` + synthetisches „Alle" |
| `LoadItemsAsync(sourceId, genreId?, searchText, page, ...)` | `api/items` seitenweise → `SourceItemsPage` |
| `MapSummaryType` | internal static, Server-Typ → CLR-Konvention |

## `MediaDetailsService`
`VideoPlayer.Mvu/MediaDetailsService.cs` (653 Zeilen) — lädt `MediaDetailsView` online (ApiClient) oder offline (Cache+Manifest): `LoadAsync`, `LoadOfflineAsync`, `LoadCollectionOrMovieAsync`, `LoadSeasonInShowContextAsync`, `LoadEpisodeAsync`, `FetchShowAsync`, `LoadManifestEntriesAsync`, `IsCachedFavoriteAsync`, `FindCachedTitleAsync`, `HasLocalFile`, `MapOfflineShow`, `MapMovie`/`MapCollection`/`MapShow`/`MapSeasons`. Injizierbar: `fileExists`.

## `FavoritesService` / `ContinueWatchingMutationService`
`FavoritesService.cs`: `ToggleAsync(mediaType, mediaId, title)` (`api/favorites/toggle`, Cache-Update via `ServerSyncService`), `RemoveFavoriteAsync(favoriteId)` (`api/favorites/remove`), `CreateEntry` (internal).
`ContinueWatchingMutationService.cs`: `HideAsync`, `SkipAsync` (`api/continue-watching/hide|skip`; offline → lokale Cache-Entfernung).

## `PlaylistService`
`VideoPlayer.Mvu/PlaylistService.cs` (797 Zeilen)

| Methode | Endpunkt/ Zweck |
|---|---|
| `LoadPlaylistsAsync` | `api/playlists` + `api/playlists/public` → `PlaylistsOverview` |
| `LoadPlaylistAsync(id)` | `api/playlists/{id}` |
| `LoadEntriesAsync(id, page)` | `entries/paged` → `PlaylistEntriesPage` |
| `StartPlaybackAsync(id, entryId?)` | `POST …/play` → `PlaylistPlaybackStartInfo` |
| `GetNextEntryAsync` / `GetPreviousEntryAsync` / `AdvanceAsync` | `play/next`, `play/previous`, `play/advance` (204 → `null`) |
| `IsCurrentUserAdminAsync` | `IsAdmin`-Claim des Tokens |
| `LoadOwnPlaylistsAsync`, `LoadGenreOptionsAsync`, `CreatePlaylistAsync`, `UpdatePlaylistAsync`, `DeletePlaylistAsync`, `AddMediaToPlaylistAsync`, `RemoveEntryAsync`, `MoveEntryToBeginningAsync`, `MoveEntryToEndAsync`, `MoveEntryBetweenAsync`, `ChangeSortModeAsync`, `SetGenresAsync`, `ResetGenresAsync`, `SetPublicAsync` | **Verwaltungsbereich (Schreiben) — für tvOS unverdrahtet** |
| `IsContinueWatchingConflict`/`IsLossOfDataConflict`/`ReadConflictFlag` | private static — 409-Konflikt-Erkennung |
| `MapPlaylist`/`MapPlaylists` | internal/private static — DTO→`PlaylistListItem` |

## `PlaybackSourceService`
`VideoPlayer.Mvu/PlaybackSourceService.cs` — `ResolveAsync(mediaType, mediaId, playlistEntry?)` → `PlaybackSource`: bevorzugt lokale Datei aus Manifest (`DownloadManifestPaths.Resolve` + `fileExists`), sonst Stream-URL mit Access-Token (`api/items/{typ}/{id}/stream`); `ResolveResumePositionAsync` (Weiterschauen-Cache/`pending_progress`), `ResolveLocalFileAsync`, `ReadCachedResumePositionAsync`, `FindResumePosition`.

## `UpcomingEpisodesService`
`VideoPlayer.Mvu/UpcomingEpisodesService.cs` — `LoadAsync(showId?, seasonNumber?, episodeNumber?, …)` → `IReadOnlyList<PlayerEpisodeRef>` (nächste Episoden; online via ApiClient, offline via `ShowDetailsCache`).

## `ProgressReportService`
`VideoPlayer.Mvu/ProgressReportService.cs` — `ReportProgressAsync(videoType, videoId, position, duration, playlistId?, ct)`: sendet via `VideoPlayerApiClient.ReportPlaybackProgressAsync`; bei Offline/überholbarem Fehler → `PendingProgressSyncService.EnqueueAsync`; permanente 4xx → verwerfen (`IsPermanentClientError`, internal static); `loggedOutcomes`-Dedup. Injizierbar: `networkMonitor`.

## `DownloadService`
`VideoPlayer.Mvu/DownloadService.cs` (1722 Zeilen) — Auftragsverwaltung + Transfer-Engine.

| Member | Sichtbarkeit | Beschreibung |
|---|---|---|
| `MaxParallelDownloads` = 2 | public const | Parallele Transfers |
| `ErrorQuotaExceeded`=`quota_exceeded`, `ErrorNoConnection`=`no_connection`, `ErrorNetwork`=`network`, `ErrorIo`=`io`, `ErrorHttpPrefix`=`http_`, `ErrorMissingFile`=`missing_file` | public const | Fehlercodes im Manifest |
| `NoticeAlreadyPresent/…Running/…Queued/…Enqueued/NoticeQuotaExceeded/NoticeInvalidMedia` | public const | Hinweistexte |
| `PlaybackProtectionFactor` = 2 | public const | Wiedergabe-Schutz-Faktor des Ablaufdatums |
| `ItemChanged` / `ItemRemoved` | public events | `DownloadItemEventArgs`/`DownloadRemovedEventArgs` → MVU-Messages |
| `LoadItemsAsync` | public | Manifest → `DownloadListItem`-Liste |
| `GetUsedBytes` | public | Belegung des Video-Verzeichnisses |
| `EnqueueAsync(request, settings)` | public | Einplanen/Deduplizieren/Quota-Vorprüfung, `PumpQueue` |
| `CancelAsync` / `DeleteAsync` | public | Abbrechen / Löschen (Datei + Zeile) |
| `ExpireAfterAsync(type, id, delay)` | public | Ablaufdatum verkürzen („nach Abspielen löschen") |
| `ExtendExpiryAsync(type, id, remaining)` | public | Wiedergabe-Schutz: `jetzt + 2×Restdauer` hochsetzen |
| `ResumeInterruptedAsync(settings)` | public | Verwaiste `queued`/`downloading`/Fehler (≠Quota) neu einplanen, `PumpQueue` |
| `CompleteExternalTransferAsync(mediaKey, errorCode, tempPath, settings)` | public | Abschluss systemseitiger Transfers (iOS-Hintergrund-Session) |
| `DeleteAllAsync` / `DeleteFailedAsync` | public | Sammel-Löschungen |
| `PurgeOrphansAsync` | public | Restdateien ohne Manifest-Eintrag entfernen (Anwenderaktion) |
| `ReconcileAsync(settings?, autoDownloadCandidateKeys?)` | public | **Abgleich Ablageort↔Manifest**: `ScanFilesAsync` (Teildateien entfernen, Orphans zählen, Pfad-Heilung Container-Umzug), `CorrectCompletedWithoutFileAsync` (`completed` ohne Datei → `failed`/`missing_file`), `RunExpiryPassAsync` (abgelaufene entfernen bzw. Auto-Kandidaten verlängern, `Origin`→`auto`) |
| `PumpQueue`, `RunTransferAsync`, `SetStateAsync`, `UpdateManifestEntryAsync`, `FindEntryAsync`, `FilePathFor`, `TryDeleteFile` | private | FIFO-Warteschlange (`manifestGate`/`pumpGate`, `runningTransfers`-CTS, `lastProgressEmit`-Throttle 250 ms, `QuotaScanIntervalChunks`=8×80 KiB Neumessung, WLAN-Bedingung) |
| `ManifestKey(type, id)` | internal static | `{type}_{mediaId}`-Schlüssel |

## `AutoDownloadService`
`VideoPlayer.Mvu/AutoDownloadService.cs` (702 Zeilen) — **für tvOS nicht verdrahtet (Nicht-Ziel)**; dokumentiert wegen Reconcile-Kopplung: `MinRunInterval` = 60 s, `RunAsync(settings, force)` (Vorlauf: Weiterschauen→Kandidaten→Enqueue mit Serien-/Playlist-Kontext, Schreiben `ShowDetailsCache`/`PlaylistEntriesCache`), `GetAutoDownloadCandidateKeysAsync(settings)` (Kandidatenmenge für den Verlängerungs-Pass in `ReconcileAsync`).

## `OfflineContinueWatchingService`
`VideoPlayer.Mvu/OfflineContinueWatchingService.cs` — `LoadHomeListsAsync`/`LoadContinueWatchingAsync`: Offline-Weiterschauen aus Manifest + `pending_progress` + `ShowDetailsCache`/`PlaylistEntriesCache`; fehlende Episoden als `IsPlaceholder`.

## `ProfileInfoService`
`VideoPlayer.Mvu/ProfileInfoService.cs` — `LoadAsync` → `ProfileInfo`; `ReadTokenClaims`/`ReadIsAdminClaim` (internal static, JWT-Claims).

## `HttpDownloadBackend`
`VideoPlayer.Mvu/HttpDownloadBackend.cs` — `TransferAsync` per `HttpClient`-Stream (Fallback-Backend).

## `DownloadListSync` / `SourceItemListSync`
`DownloadListSync.cs`, `SourceItemListSync.cs` — `static Sync(...)`: In-Place-Synchronisation gebundener Listen (ViewModel-Pflege ohne Neuaufbau).

## `ShowDetailsCache` / `PlaylistEntriesCache`
`ShowDetailsCache.cs` (259 Z.), `PlaylistEntriesCache.cs` (155 Z.) — `static`: `KeyFor`, `WriteAsync`, `ReadAsync`; ShowDetailsCache zusätzlich `OrderedEpisodes`, `NextEpisodeAfter`, `NextEpisodeAfterId`.

# Storage (`VideoPlayer.Storage`)

## `AppSettingsService`
`AppSettingsService.cs` — `Load()`/`Save(AppSettings)` über `ISettingsStore`; Schlüssel-Konstanten je Einstellung (u. a. `WifiOnlyDownloadsKey`, `DownloadQuotaGbKey`, `DownloadRetentionDaysKey`, `AutoDeletePlayedDownloadsKey`, `AutoDeleteWatchedEpisodesKey` (Alt-Schlüssel), `HomeHeroEnabledKey`, `LoggingEnabledKey`); `AppProfile`-Präfix.

## `AppSecretsService`
`AppSecretsService.cs` — Schlüssel: `UsernameKey`, `PasswordKey` (`Passkey`), `AuthTokenKey`, `RefreshTokenKey`, `DeviceTokenKey` (serveradress-spezifisch). Methoden: `GetUsername`, `GetPasswordAsync`, `HasCredentialsAsync`, `SetCredentialsAsync`, `ClearCredentialsAsync`, `GetAuthTokenAsync`/`SetAuthTokenAsync`/`ClearAuthTokenAsync`, `GetRefreshTokenAsync`/`Set…`/`Clear…`, `GetDeviceTokenAsync`/`HasDeviceTokenAsync`/`SetDeviceTokenAsync`/`ClearDeviceTokenAsync`; `GetMigratedSecretAsync` (Legacy-Migration normale→sichere Ablage).

## `KnownServersService`
`KnownServersService.cs` — `GetAllAsync` (zuletzt verwendet zuerst), `GetLastConnectedAddressAsync` (`backup_state`), `SetActiveServerAsync(address, displayName)` (Upsert + LastConnected), `MigrateLegacyServerAddressAsync`.

## `BackupDatabase` / `CacheDatabase`
`BackupDatabase.cs`: `Connection` (`SQLiteAsyncConnection`), `InitializeAsync` (Tabellen `known_servers`, `backup_state`, `pending_progress` + Migration `DurationTicks`/`PlaylistId`; Korruption→Neuinit mit `StorageNotice`-Meldepfad).
`CacheDatabase.cs`: `Connection`, `InitializeAsync` (`cached_lists`, `cached_favorites`, `download_manifest`).

## `PendingProgressSyncService`
`PendingProgressSyncService.cs` — `EnqueueAsync(videoType, videoId, position, duration, playlistId?)`, `GetPendingAsync`, `CountAsync`, `RemoveForMediaAsync` (Outbox, Tabelle `pending_progress`).

## `AppLogService` / `FileLogger` / `FileLoggerProvider` / `SqliteBootstrap`
`AppLogService.cs`: `MaxLogFileBytes` = 1 MiB, `LogFileName`/`PreviousLogFileName`, `SetEnabled`, `Write(level, category, message, ex)` (Rotation), `MaskAccessToken` (statisch). `FileLogger`/`FileLoggerProvider` (`ILogger`-Implementierung, schaltbar). `SqliteBootstrap.cs` (Init-Helfer, 19 Z.).

# ApiClient (`VideoPlayer.ApiClient`)

## `VideoPlayerApiClient`
`VideoPlayerApiClient.cs` (1278 Zeilen) — Endpunkte (Auswahl, vollständig siehe Grep-Ergebnis): `LoginAsync` (`api/auth/login`), `RefreshSessionAsync` (`api/auth/refresh`), `LogoutSessionAsync` (`api/auth/logout`), `CheckHealthAsync` (`api/health`), `GetSourcesAsync`/`GetSourceAsync`/`GetSourceGenresAsync` (`api/Sources*`), `GetRecentEntriesAsync` (`api/items/recent`), `GetSeasonalGenresAsync` (`api/home/seasonal-genres`), `GetContinueWatchingAsync` (`api/continue-watching`), `ReportPlaybackProgressAsync` (`api/continue-watching/progress`), `HideContinueWatchingEntryAsync`/`SkipContinueWatchingEntryAsync` (`…/hide`, `…/skip`), `GetFavoritesAsync`/`ToggleFavoriteAsync`/`RemoveFavoriteAsync` (`api/favorites*`), `GetSourceItemsAsync` (`api/items` mit Paging/Suche/Genre), `GetMovieAsync`/`GetMovieCollectionAsync`/`GetTvShowAsync`/`GetTvShowEpisodeAsync` (`api/items/{typ}/{id}`), `GetGenreOptionsAsync` (`api/items/genres`), `GetPictureAsync`/`GetSourceIconAsync`/`GetHeroBackgroundAsync`/`GetEpisodeBackgroundImageAsync` (`api/pictures*`, `api/sourceicons`, `api/episodes/{id}/background-image`), Playlist-Lesen (`api/playlists`, `…/public`, `{id}`, `entries/paged`, `cover`, `POST {id}/play`, `play/next|previous|advance`), Playlist-Verwaltung (CRUD, entries, move, sort-mode, genres, public), `SetAccessToken`/`ClearAccessToken`/`SetApiKey`, Retry-Policy via `TokenRefresher`/`DelayAsync`. Stream-Endpunkt `api/items/{typ}/{id}/stream` wird von `PlaybackSourceService`/`DownloadService` direkt adressiert (URL-Aufbau, kein eigenes Client-Methoden-Paar für Bytes).

## `DevicePairingClient`
`DevicePairingClient.cs` — `ExchangeEndpoint`=`api/pairing/exchange`, `BootstrapEndpoint`=`api/pairing/bootstrap`, `RequestTimeout`=15 s; `ExchangeAsync(baseAddress, code, deviceName)` → entschlüsseltes Geräte-Token; `BootstrapAsync(baseAddress, ticket, deviceName)` → `BootstrapResult` (DeviceToken+Token+Expires+RefreshToken).

## `PairingCrypto` (internal)
`PairingCrypto.cs` — `CreateKeyPair` (ECDH P-256), `ExportPublicKeyBase64` (SPKI), `DecryptToken` (ECDH→AES-256-GCM).

## `ClientGate`
`ClientGate.cs` — `HeaderName`=`X-API-Key`, `DeviceTokenProvider` (statisch, je Anfrage), Auflösungskette: DeviceToken → `VIDEOWEBPLAYER_MAUI_API_TOKEN`-Env → Assembly-Metadatum (`VideoPlayer.ApiClient.ApiToken`, per csproj `VideoPlayerApiToken` gesetzt) → `client-api-token.txt`; `TokenFileDirectory`/`TokenFilePath`, `Resolve`-Überladungen (internal).

## Discovery
`Discovery/ServerDiscoveryService.cs`: `ServerDiscoveryService(IEnumerable<IServerDiscovery>)`, `CreateDefault()` (mDNS+UDP), `DiscoverAsync` (parallel, dedupliziert nach Adresse). `MdnsServerDiscovery` (Zeroconf `ResolveAsync`, `_http._tcp.local.`). `UdpServerDiscovery` (Broadcast `:5001`, `QueryText`=`VIDEOWEBPLAYER_DISCOVERY`, Antwort-Präfix `VIDEOWEBPLAYER_SERVER:`, `ResolveAnnouncedAddress` internal). `DiscoveryProtocol`-Konstanten.

# Developer-API (`msTools.DeveloperApi`, `#if DEBUG || DEV_API`)

`DevApiServer`/`DevApiHttpServer` (Loopback-HTTP-Server, `Port`, `Start`/`Dispose`), `DevApiRouter` (`Map(method,path,handler)`, `RouteAsync`), `DevApiRouterExtensions.MapModel`, `DevApiRequest`/`DevApiResponse`/`DevApiMetadata` (`devapi.json`), `DevApiRequestReader` (Limits 64 KiB Header/1 MiB Body). `msTools.DeveloperApi.Maui/MauiDevApiService` (MAUI-Handler: Elementbaum `/tree`, `/invoke`, `/set-text`). `VideoPlayer.Maui/DevApi/DevApiService` (Startet Server mit `DevApiModelSnapshot.FromModel`-Provider). **Plattformneutraler Kern wiederverwendbar; MAUI-Teil nicht.**

# App-Rand (`VideoPlayer.Maui`)

## `ShellAppEffectHandler`
`VideoPlayer.Maui/Services/ShellAppEffectHandler.cs` (2396 Zeilen) — `IAppEffectHandler`-Implementierung: `switch` über alle `AppEffect`-Subtypen → DI-Dienste; abonniert `AuthSessionService.SessionExpired`, `DeviceTokenService.DeviceTokenRejected`, `INetworkStatusMonitor.AvailabilityChanged`, `ServerEventsService.EventReceived`/`ConnectionClosed`, `DownloadService.ItemChanged`/`ItemRemoved` → dispatched Messages über `MainThread` in `MvuHost`. Download-Pfade: `LoadDownloadsAsync` → `ReconcileWithExpiryPassAsync` (Kandidaten + `ReconcileAsync`) beim Seitenaufruf; `ResumeDownloadsAsync` → Reconcile + `ResumeInterruptedAsync`; `ExpireWatchedDownloadAsync`, `ProtectDownloadExpiryAsync`, `RunAutoDownloadsAsync`, Sammel-Löschungen; Protokollversand via Mail/Share (`LogProtocolRecipient`).

## Plattform-Adapter
`PreferencesSettingsStore` (`ISettingsStore`→`Preferences`), `SecureStorageSecretStore` (`ISecretStore`→`SecureStorage`), `AppStorageLayoutProvider.CreateLayout(AppProfile)` (`#if WINDOWS/ANDROID/IOS||MACCATALYST`-Wurzeln), `ConnectivityNetworkStatusMonitor` (`INetworkStatusMonitor`→`Connectivity`), `SignalRServerEventsConnection` (`IServerEventsConnection`→SignalR), `ServerHealthProbe`, `ServerAddressProvider` (`VIDEOPLAYER_SERVER_URL`, Default `http://localhost/videoplayer/`), `PictureInPictureService`, `PosterImageService`, `IosBackgroundDownloadBackend` (iOS `NSUrlSession`-Hintergrund; `AppDelegate.HandleEventsForBackgroundUrlSession` → `CompleteExternalTransferAsync`).

## `App` / `MauiProgram` / `PlayerPage`
`App.xaml.cs`: `CreateWindow` → `LoadingPage` + `StartupRequestedMessage`; `window.Resumed` → `HomeListsReloadRequestedMessage`; DevApi in DEBUG. `MauiProgram.cs` (217 Z.): DI-Registrierung aller Services/Adapter, `#if IOS` → `IosBackgroundDownloadBackend` als `IDownloadBackend`. `PlayerPage.xaml.cs` (499 Z.): `ProgressTickInterval` = 5 s Dispatcher-Timer → `PlaybackProgressMessage` mit `appliedMediaId`-Bindung; `MediaEnded`→`PlaybackEndedMessage`, Fehler→`PlaybackFailedMessage`, PiP-Buttons, Playlist-Navigation.
