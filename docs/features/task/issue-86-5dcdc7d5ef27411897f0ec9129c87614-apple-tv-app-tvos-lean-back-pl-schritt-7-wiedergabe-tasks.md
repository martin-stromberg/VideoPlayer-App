# Tasks: tvOS-Wiedergabe mit Fortschritts-Tracking, Auto-Fortsetzung und Spurwahl (Schritt 7)

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Komposition | `PlaybackSourceService`-Singleton `PlaybackSources` in `AppComposition` | Offen | — |
| 2 | Komposition | `UpcomingEpisodesService`-Singleton `UpcomingEpisodes` in `AppComposition` | Offen | — |
| 3 | Komposition | `PlaylistService`-Singleton `Playlists` in `AppComposition` | Offen | — |
| 4 | Komposition | `DownloadService`-Singleton `Downloads` in `AppComposition` (ohne Backend) | Offen | — |
| 5 | UI | `TvosPlayerInfoViewController` anlegen (Info-Tab: Titel, Playlist-Label, Gesehen-Status) | Offen | — |
| 6 | UI | `TvosPlayerViewController` anlegen (`AVPlayerViewController` + `IModelRenderable`: Quellanwendung mit `appliedLocation`/`appliedMediaId`/`sourceApplyFailed`) | Offen | — |
| 7 | UI | Fortsetzungs-Seek bei `ReadyToPlay` (KVO `status`, gemerkte `ResumePosition`) | Offen | — |
| 8 | UI | 5-s-`AddPeriodicTimeObserver` → `PlaybackProgressMessage` (nur `Playing`; Zustandswechsel via `timeControlStatus`-KVO/`PlaybackStalled` sofort) | Offen | — |
| 9 | UI | `DidPlayToEndTime` → `PlaybackEndedMessage`; `FailedToPlayToEndTime`/`status == Failed` → `PlaybackFailedMessage` | Offen | — |
| 10 | UI | `PlayerPageOpenedMessage` bei `ViewDidAppear`; `PlayerClosedMessage` beim Dismissal | Offen | — |
| 11 | UI | Playlist next/previous über `TransportBarCustomMenuItems` (`CanNavigateNext`/`CanNavigatePrevious`); `CustomInfoViewControllers` mit Info-Tab | Offen | — |
| 12 | UI | Fehler-/Ladezustand: `UIAlertController` bei `HasError && !IsResolving` (Schließen → Dismiss); `UIActivityIndicatorView` bei `IsResolving` im `ContentOverlayView` | Offen | — |
| 13 | Navigation | `TvosNavigator.PresentPlayer()` + `playerPage`-Render-Verteilung + Freigabe-Callback | Offen | — |
| 14 | Effekt-Handler | `NavigateToPlayerEffect` → `PresentPlayer` (statt Platzhalter) | Offen | — |
| 15 | Effekt-Handler | `ResolvePlaybackSourceEffect` → `ResolvePlaybackSourceAsync` (`allowServerStream = !IsOffline`) → `PlaybackSourceResolvedMessage` | Offen | — |
| 16 | Effekt-Handler | `LoadUpcomingEpisodesEffect` → `LoadUpcomingEpisodesAsync` → `UpcomingEpisodesLoadedMessage` | Offen | — |
| 17 | Effekt-Handler | `ReportPlaybackProgressEffect` → `ReportPlaybackProgressAsync` (`ProgressReportService`) | Offen | — |
| 18 | Effekt-Handler | `StartPlaylistPlaybackEffect` → `StartPlaylistPlaybackAsync` → `PlaylistPlaybackStartedMessage` | Offen | — |
| 19 | Effekt-Handler | `LoadNext`/`LoadPrevious`/`AdvancePlaylistEntryEffect` → `NavigatePlaylistEntryAsync` → `PlaylistNavigationCompletedMessage` | Offen | — |
| 20 | Effekt-Handler | `ProtectDownloadExpiryEffect`/`ExpireWatchedDownloadEffect` → `DownloadService.ExtendExpiryAsync`/`ExpireAfterAsync` + `ItemChanged`/`ItemRemoved`-Verdrahtung | Offen | — |
| 21 | Tests | `TvOsPlaybackContractTests` anlegen (5 Contract-Tests) | Offen | — |
| 22 | CI | Push → `build-tvos` grün (Run-ID dokumentieren) | Offen | — |
