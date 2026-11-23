# Plan-Review

## Ergebnis

**Status:** Vollständig umgesetzt

## Umgesetzte Planelemente

- [x] `TvosPlayerViewController` (`AVPlayerViewController` + `IModelRenderable`) — angelegt (`VideoPlayer.TvOS/Ui/TvosPlayerViewController.cs`): Quellanwendung mit `appliedLocation`/`appliedMediaId`/`sourceApplyFailed`/`pendingResumePosition`, Fortsetzungs-Seek bei `ReadyToPlay`, `AddPeriodicTimeObserver` (5 s) → `PlaybackProgressMessage`, `ObserveDidPlayToEndTime`/`ObserveFailedToPlayToEndTime`/`status == Failed` → Ende/Fehler-Messages, `PlayerPageOpenedMessage`/`PlayerClosedMessage`, `TransportBarCustomMenuItems` (Playlist next/previous), `CustomInfoViewControllers`, Fehler-`UIAlertController`, Lade-`UIActivityIndicatorView` im `ContentOverlayView`, `AppLogService.MaskAccessToken`-Bereinigung
- [x] `TvosPlayerInfoViewController` — angelegt (`VideoPlayer.TvOS/Ui/TvosPlayerInfoViewController.cs`): Titel, `PlaylistLabel`, „angesehen"-Hinweis, Lade-/Pufferstatus
- [x] `TvosNavigator.PresentPlayer()` + `playerPage`-Render-Verteilung + `lastModel`-Vorgriff + `ReleasePlayerPage`-Callback — erweitert; `PresentPreparedTarget` und `PreparedTargetViewController` entfernt (tot, alle Ziele real)
- [x] `TvosAppEffectHandler` — `NavigateToPlayerEffect` → `PresentPlayer`; `ResolvePlaybackSourceEffect` → `ResolvePlaybackSourceAsync` (`allowServerStream = !IsOffline`, `StartPositionSeconds`-Override) → `PlaybackSourceResolvedMessage`; `LoadUpcomingEpisodesEffect` → `UpcomingEpisodesLoadedMessage`; `ReportPlaybackProgressEffect` → `ProgressReports`; `StartPlaylistPlaybackEffect` → `PlaylistPlaybackStartedMessage`; `LoadNext`/`LoadPrevious`/`AdvancePlaylistEntryEffect` → `NavigatePlaylistEntryAsync` → `PlaylistNavigationCompletedMessage`; `ProtectDownloadExpiryEffect`/`ExpireWatchedDownloadEffect` → `DownloadService`; `ItemChanged`/`ItemRemoved`-Verdrahtung
- [x] `AppComposition` — Singletons `PlaybackSources`, `UpcomingEpisodes`, `Playlists`, `Downloads`; Player-Logger an den Navigator
- [x] `TvOsPlaybackContractTests` — 5 Contract-Tests angelegt; bestehende `TvOsHomePageContractTests`/`TvOsDetailsContractTests` an die reale Verdrahtung angepasst

## Offene Aufgaben

Keine.

## Hinweise

- Die tvOS-Kompilierbarkeit der nativen Bindings (`AVPlayerItem.Notifications.Observe*`, `AddPeriodicTimeObserver`, `TransportBarCustomMenuItems`, `CustomInfoViewControllers`) ist lokal nicht prüfbar — Nachweis ausschließlich über den macOS-CI-Job `build-tvos`.
