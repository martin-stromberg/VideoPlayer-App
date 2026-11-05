# Tasks: tvOS-Wiedergabe mit Fortschritts-Tracking, Auto-Fortsetzung und Spurwahl (Schritt 7)

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Komposition | `PlaybackSourceService`-Singleton `PlaybackSources` in `AppComposition` | Erledigt | — |
| 2 | Komposition | `UpcomingEpisodesService`-Singleton `UpcomingEpisodes` in `AppComposition` | Erledigt | — |
| 3 | Komposition | `PlaylistService`-Singleton `Playlists` in `AppComposition` | Erledigt | — |
| 4 | Komposition | `DownloadService`-Singleton `Downloads` in `AppComposition` (ohne Backend) | Erledigt | — |
| 5 | UI | `TvosPlayerInfoViewController` anlegen (Info-Tab: Titel, Playlist-Label, Gesehen-Status) | Erledigt | — |
| 6 | UI | `TvosPlayerViewController` anlegen (`AVPlayerViewController` + `IModelRenderable`: Quellanwendung mit `appliedLocation`/`appliedMediaId`/`sourceApplyFailed`) | Erledigt | — |
| 7 | UI | Fortsetzungs-Seek bei `ReadyToPlay` (KVO `status`, gemerkte `ResumePosition`) | Erledigt | — |
| 8 | UI | 5-s-`AddPeriodicTimeObserver` → `PlaybackProgressMessage` (nur `Playing`; Zustandswechsel via `timeControlStatus`-KVO/`PlaybackStalled` sofort) | Erledigt | — |
| 9 | UI | `DidPlayToEndTime` → `PlaybackEndedMessage`; `FailedToPlayToEndTime`/`status == Failed` → `PlaybackFailedMessage` | Erledigt | — |
| 10 | UI | `PlayerPageOpenedMessage` bei `ViewDidAppear`; `PlayerClosedMessage` beim Dismissal | Erledigt | — |
| 11 | UI | Playlist next/previous über `TransportBarCustomMenuItems` (`CanNavigateNext`/`CanNavigatePrevious`); `CustomInfoViewControllers` mit Info-Tab | Erledigt | — |
| 12 | UI | Fehler-/Ladezustand: `UIAlertController` bei `HasError && !IsResolving` (Schließen → Dismiss); `UIActivityIndicatorView` bei `IsResolving` im `ContentOverlayView` | Erledigt | — |
| 13 | Navigation | `TvosNavigator.PresentPlayer()` + `playerPage`-Render-Verteilung + Freigabe-Callback | Erledigt | — |
| 14 | Effekt-Handler | `NavigateToPlayerEffect` → `PresentPlayer` (statt Platzhalter) | Erledigt | — |
| 15 | Effekt-Handler | `ResolvePlaybackSourceEffect` → `ResolvePlaybackSourceAsync` (`allowServerStream = !IsOffline`) → `PlaybackSourceResolvedMessage` | Erledigt | — |
| 16 | Effekt-Handler | `LoadUpcomingEpisodesEffect` → `LoadUpcomingEpisodesAsync` → `UpcomingEpisodesLoadedMessage` | Erledigt | — |
| 17 | Effekt-Handler | `ReportPlaybackProgressEffect` → `ReportPlaybackProgressAsync` (`ProgressReportService`) | Erledigt | — |
| 18 | Effekt-Handler | `StartPlaylistPlaybackEffect` → `StartPlaylistPlaybackAsync` → `PlaylistPlaybackStartedMessage` | Erledigt | — |
| 19 | Effekt-Handler | `LoadNext`/`LoadPrevious`/`AdvancePlaylistEntryEffect` → `NavigatePlaylistEntryAsync` → `PlaylistNavigationCompletedMessage` | Erledigt | — |
| 20 | Effekt-Handler | `ProtectDownloadExpiryEffect`/`ExpireWatchedDownloadEffect` → `DownloadService.ExtendExpiryAsync`/`ExpireAfterAsync` + `ItemChanged`/`ItemRemoved`-Verdrahtung | Erledigt | — |
| 21 | Tests | `TvOsPlaybackContractTests` anlegen (5 Contract-Tests) | Erledigt | — |
| 22 | CI | Push → `build-tvos` grün (Run-ID dokumentieren) | Erledigt | — |
