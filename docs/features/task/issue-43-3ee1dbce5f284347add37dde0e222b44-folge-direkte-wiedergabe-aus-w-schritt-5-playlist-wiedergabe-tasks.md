# Tasks: Playlist-Wiedergabe und Folgevideo-Auflösung

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | ApiClient | `ContinueWatchingEntry` um `PlaylistId`, `PlaylistName`, `PlaylistEntryId` erweitern | Offen | – |
| 2 | ApiClient | DTO `PlaylistPlaybackStart` anlegen | Offen | – |
| 3 | ApiClient | DTO `PlaylistNavigationResult` anlegen | Offen | – |
| 4 | ApiClient | `VideoPlayerApiClient.StartPlaylistPlaybackAsync` + POST-ohne-Body/204-Helfer | Offen | – |
| 5 | ApiClient | `VideoPlayerApiClient.GetNextPlaylistEntryAsync` | Offen | – |
| 6 | ApiClient | `VideoPlayerApiClient.GetPreviousPlaylistEntryAsync` | Offen | – |
| 7 | ApiClient | `VideoPlayerApiClient.AdvancePlaylistAsync` | Offen | – |
| 8 | MVU-Modell | `HomeMediaItem` um `PlaylistId`, `PlaylistName`, `PlaylistEntryId` erweitern | Offen | – |
| 9 | MVU-Modell | `PlayerState` um Playlist-Felder + `IsPlaylistPlayback`/`CanNavigateNext`/`CanNavigatePrevious`/`PlaylistLabel` erweitern | Offen | – |
| 10 | MVU-Modell | Records `PlaylistPlaybackStartInfo` + `PlaylistNavigationItem` anlegen | Offen | – |
| 11 | Logik | `PlaylistService` um `StartPlaybackAsync`, `GetNextEntryAsync`, `GetPreviousEntryAsync`, `AdvanceAsync` erweitern | Offen | – |
| 12 | Logik | `HomeListService.MapContinueWatching` um Playlist-Felder erweitern | Offen | – |
| 13 | Logik | `PlaybackSourceService.ResolveAsync` um `resumePositionSecondsOverride` erweitern | Offen | – |
| 14 | MVU | `AppMessage`: `PlaylistPlaybackStartRequestedMessage`, `PlaylistPlaybackStartedMessage`, `PlaylistNextRequestedMessage`, `PlaylistPreviousRequestedMessage`, `PlaylistNavigationCompletedMessage` | Offen | – |
| 15 | MVU | `AppEffect`: `StartPlaylistPlaybackEffect`, `LoadNextPlaylistEntryEffect`, `LoadPreviousPlaylistEntryEffect`, `AdvancePlaylistEntryEffect`; `ResolvePlaybackSourceEffect` + `StartPositionSeconds` | Offen | – |
| 16 | MVU | `AppUpdate`: neue Update-Methoden + Änderungen (ContinueWatching-Guard, PlaylistEntrySelected, Auto-Fortsetzung, Fehler-Rückfall) | Offen | – |
| 17 | Effekt-Handler | `ShellAppEffectHandler`: `StartPlaylistPlaybackAsync` + `NavigatePlaylistEntryAsync` + Override-Weitergabe | Offen | – |
| 18 | UI | `PlaylistDetailsPage`: „Abspielen"-Button im Kopfbereich | Offen | – |
| 19 | UI | `PlayerPage`: Vor/Zurück-Buttons + Playlist-Positionszeile | Offen | – |
| 20 | DevApi | `DevApiModelSnapshot` + `DevApiModels` um `PlayerPlaylistId`/`PlayerPlaylistEntryId`/`PlayerPlaylistPosition` | Offen | – |
| 21 | E2E-Infra | `MockVideoServer`: POST `play`/`next`/`previous`/`advance` | Offen | – |
| 22 | E2E-Infra | `MockPlaylistEntry.ResumePositionSeconds` + Stream-Bedienung für Playlist-Medien-Ids | Offen | – |
| 23 | E2E-Infra | `MockVideoServer`: Playlist-Felder im Weiterschauen-Opt-in | Offen | – |
| 24 | Tests | `AppUpdateTests_PlaylistPlayback` (alle Update-Pfade) | Offen | – |
| 25 | Tests | `PlaylistService`-Playback-Tests (Mapping, 204) | Offen | – |
| 26 | Tests | `HomeListService`-Mapping-Test (Playlist-Bezug) | Offen | – |
| 27 | Tests | `PlaybackSourceService`-Override-Test | Offen | – |
| 28 | E2E-Tests | `PlaylistPlaybackTests`: Start via Button, Start via Eintrag, Vor/Zurück, Auto-Fortsetzung, CW mit Playlist-Bezug, Fehler-Rückfall | Offen | – |
| 29 | Doku | `docs/help/`-Dokumentation Playlist-Wiedergabe | Offen | – |
| 30 | Doku | README + RELEASE_NOTES + changes.log | Offen | – |
