# Logik und Dienste (Wiedergabe)

## `AppUpdate` — Player-Nachrichtenbehandlung
Datei: `VideoPlayer.Mvu/AppUpdate.cs` (Dispatch-Tabelle Z. 294–302; Konstanten `PlaybackEndedToleranceSeconds = 300` Z. 89, `EndWindowRequiredTicks = 2` Z. 98, `PlaybackUnavailableError` Z. 75)

| Methode | Nachricht | Kurzbeschreibung |
|---------|-----------|------------------|
| `UpdatePlayRequested` | `PlayRequestedMessage` | Guard `movie`/`tvshowepisode` + `StartPlayback` (Player-State + `NavigateToPlayerEffect` + `ResolvePlaybackSourceEffect`) |
| `UpdateContinueWatchingItemSelected` | `ContinueWatchingItemSelectedMessage` | Direktwiedergabe abspielbarer Einträge; Playlist-Einträge → `StartPlaylistPlaybackEffect`; Episode mit `ShowId` → `LoadUpcomingEpisodesEffect` |
| `UpdatePlayerPageOpened` | `PlayerPageOpenedMessage` | Setzt `Attempted` |
| `UpdatePlaybackSourceResolved` | `PlaybackSourceResolvedMessage` | Identitäts-Guard; Quelle übernehmen oder Rückfall (`AdvancePlaylistEntry`/`AdvanceToNextEpisode`) |
| `UpdatePlaybackProgress` | `PlaybackProgressMessage` | `MediaId`-Guard (nachlaufende Takte), Position/Throttling → `ReportPlaybackProgressEffect`; Wiedergabe-Schutz → `ProtectDownloadExpiryEffect` (lokale Datei, einmalig); `EvaluateEndWindow` |
| `EvaluateEndWindow` | (intern) | `EndWindowStreak`-Aufbau nur bei plausiblen Takten im Fenster; `IsWatched` ab Streak ≥ 2; optional `ExpireWatchedDownloadEffect` |
| `UpdatePlaybackEnded` | `PlaybackEndedMessage` | Plausibilität (Restlaufzeit ≤ 300 s); „angesehen" + End-Fortschrittsmeldung; Auto-Fortsetzung → `AdvancePlaylistEntry`/`AdvanceToNextEpisode` |
| `UpdatePlaybackFailed` | `PlaybackFailedMessage` | Fehler + Rückfall wie Quell-Fehlschlag |
| `UpdatePlayerClosed` | `PlayerClosedMessage` | Abschluss-Fortschritt (`Position > 0`) + `PlayerState.Empty` |
| `UpdatePlaylistPlaybackStarted` | `PlaylistPlaybackStartedMessage` | Player-State mit Playlist-Kontext; `NavigateToPlayerEffect` + `ResolvePlaybackSourceEffect` (mit `StartPositionSeconds`) |
| `UpdatePlaylistNextRequested` / `UpdatePlaylistPreviousRequested` | `PlaylistNextRequestedMessage`/`PlaylistPreviousRequestedMessage` | Guards (`IsPlaylistPlayback`, `!IsResolving`, `!PlaylistNextExhausted`); `LoadNext/PreviousPlaylistEntryEffect` |
| `UpdatePlaylistNavigationCompleted` | `PlaylistNavigationCompletedMessage` | Identitäts-Guard (`PlaylistId`+`CurrentEntryId`); Wechsel auf Folgeeintrag + `ResolvePlaybackSourceEffect`; 204 → `PlaylistNextExhausted` |
| `UpdateUpcomingEpisodesLoaded` | `UpcomingEpisodesLoadedMessage` | Übernimmt Fortsetzungsliste nur bei noch gleicher Episode |

## Plattformneutrale Dienste

### `PlaybackSourceService` (`VideoPlayer.Mvu/PlaybackSourceService.cs`)
- `ResolveAsync(mediaType, mediaId, cancellationToken, allowServerStream, resumePositionSecondsOverride)` — lokale Datei aus `download_manifest` (state `completed` + Datei existiert) vor `api/items/{typ}/{id}/stream?access_token=…`; Fortsetzungsposition online aus Weiterschauen-Liste, sonst aus `cached_lists`; offline nur lokale Datei.

### `ProgressReportService` (`VideoPlayer.Mvu/ProgressReportService.cs`)
- `ReportProgressAsync(videoType, videoId, position, duration, playlistId, ct)` — online `ReportPlaybackProgressAsync` des API-Clients; offline/überholbarer Fehler → `PendingProgressSyncService.EnqueueAsync` (persistente `pending_progress`-Queue, Nachsendung per `ServerSyncService`); permanente 4xx verworfen.

### `UpcomingEpisodesService` (`VideoPlayer.Mvu/UpcomingEpisodesService.cs`)
- `LoadAsync(mediaId, showId, ct)` — Serienkontext online `api/items/tvshow/{id}` (+ `ShowDetailsCache`-Auffrischung) bzw. aus Cache; liefert `PlayerEpisodeRef`-Liste nach der Episode.

### `PlaylistService` (`VideoPlayer.Mvu/PlaylistService.cs`)
- `StartPlaybackAsync(playlistId, entryId, ct)` → `PlaylistPlaybackStartInfo` (`POST api/playlists/{id}/play`)
- `GetNextEntryAsync`/`GetPreviousEntryAsync`/`AdvanceAsync(playlistId, currentEntryId, ct)` → `PlaylistNavigationItem?` (`POST …/play/next|previous|advance`; `null` bei 204)

### `DownloadService` (`VideoPlayer.Mvu/DownloadService.cs`)
- `ExtendExpiryAsync(mediaType, mediaId, remainingDuration, ct)` — Wiedergabe-Schutz (`PlaybackProtectionFactor = 2`); `ExpireAfterAsync(mediaType, mediaId, delay, ct)` — Ablauf-Verkürzung gesehener Downloads; Events `ItemChanged`/`ItemRemoved`.
- Ctor: `(AuthSessionService, AppSecretsService, CacheDatabase, AppStorageLayout, INetworkStatusMonitor, ILogger?, IDownloadBackend? = null)` — ohne Backend = prozessinterner HTTP-Stream.

## tvOS-Verdrahtung (Bestand)

### `TvosAppEffectHandler` (`VideoPlayer.TvOS/Services/TvosAppEffectHandler.cs`)
- Verdrahtet: Start-/Verbindungs-Workflow, Sync, Startseiten-Listen, Quellen-Browsing, Detailseiten (inkl. `ToggleFavoriteEffect`), `EnqueueDownloadEffect` (vorbereiteter Hinweis ohne Einreihen), `NavigateToPlayerEffect` (Platzhalter `PreparedTargetViewController`), `StartPlaylistPlaybackEffect` (Platzhalter), `LogoutEffect`.
- No-Op (`default`, protokolliert): alle übrigen Effekte — u. a. `ResolvePlaybackSourceEffect`, `LoadUpcomingEpisodesEffect`, `ReportPlaybackProgressEffect`, `LoadNext/Previous/AdvancePlaylistEntryEffect`, `ProtectDownloadExpiryEffect`, `ExpireWatchedDownloadEffect`, `TogglePictureInPictureEffect`.
- `DispatchMessageAsync` marshalt auf den Hauptthread (`MainThreadInvoker`).

### `TvosNavigator` (`VideoPlayer.TvOS/Ui/TvosNavigator.cs`)
- `PresentMediaDetails` (modaler Stapel, `UIModalPresentationStyle.FullScreen`), `PresentSourceBrowser` (Einmal-Instanz), `PresentPreparedTarget` (Platzhalter, Update statt Stapeln), `TopmostPresented()` als Präsentationsanker, `Render(AppModel)` verteilt an `IModelRenderable`-Seiten (`serverSetupPage`, `loginPage`, `tabBar`, `sourceBrowserPage`, `detailPages`).

### `AppComposition` (`VideoPlayer.TvOS/AppComposition.cs`)
- Singletons u. a.: `AuthSession`, `Secrets`, `CacheDatabase`, `BackupDatabase`, `StorageLayout`, `NetworkMonitor`, `ServerSync`, `PendingProgress`, `ProgressReports`, `HomeLists`, `SourceBrowser`, `MediaDetails`, `Favorites`, `OfflineContinueWatching`, `PosterImages`, `ServerDiscovery`; `CreateRuntime` verdrahtet `TvosNavigator` + `TvosAppEffectHandler` + `MvuHost` (Dispatch auf Hauptthread; `ModelChanged` → `navigator.Render`).

### `MediaDetailsViewController` / `HomePageViewController`
- `OnPlay` dispatcht `PlayRequestedMessage` (bereits verdrahtet); `OnItemSelected` dispatcht `ContinueWatchingItemSelectedMessage` für Weiterschauen-Einträge (bereits verdrahtet).

## MAUI-Referenz `PlayerPage` (`VideoPlayer.Maui/Pages/PlayerPage.xaml.cs`)

- `ProgressTickInterval = 5 s`, `ResumeSeekTimeout = 10 s`; `appliedLocation`/`appliedMediaId`/`sourceApplyFailed`/`pendingResumePosition`/`timerRunning`/`subscribed`.
- `OnAppearing`: `PlayerPageOpenedMessage` + Timer-Start; `OnDisappearing`: `PlayerClosedMessage`.
- Tick meldet nur bei `Playing`; `StateChanged` meldet sofort (`ReportPosition` → `PlaybackProgressMessage` mit `appliedMediaId`).
- `UpdateUi`: Quellanwendung identitätsgebunden (`appliedLocation`-Vergleich, `sourceApplyFailed`-Erzwingung); `IsValidSourceLocation` (http/https bzw. nicht leer); `SanitizeForLog` (`AppLogService.MaskAccessToken` — URL niemals protokollieren).
- `MediaOpened` → Fortsetzungs-Seek merken, bei `Playing`/`Paused` anwenden (Timeout-tolerant); `MediaEnded` → `PlaybackEndedMessage`; `MediaFailed` → `PlaybackFailedMessage`.
- Playlist-UI: `PlaylistLabel`-Kontextzeile, Prev/Next-Buttons (`CanNavigatePrevious`/`CanNavigateNext`) → `PlaylistPreviousRequestedMessage`/`PlaylistNextRequestedMessage`.
