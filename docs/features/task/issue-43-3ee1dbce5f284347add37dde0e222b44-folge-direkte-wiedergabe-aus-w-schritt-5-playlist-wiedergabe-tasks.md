# Tasks: Playlist-Wiedergabe und Folgevideo-Auflösung

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | ApiClient | `ContinueWatchingEntry` um `PlaylistId`, `PlaylistName`, `PlaylistEntryId` erweitern | Erledigt | `HomeListServiceTests.LoadHomeLists_MapsContinueWatchingPlaylistContext` |
| 2 | ApiClient | DTO `PlaylistPlaybackStart` anlegen | Erledigt | `PlaylistServiceTests.StartPlaybackAsync_*` |
| 3 | ApiClient | DTO `PlaylistNavigationResult` anlegen | Erledigt | `PlaylistServiceTests.Navigation_*` |
| 4 | ApiClient | `VideoPlayerApiClient.StartPlaylistPlaybackAsync` + POST-ohne-Body/204-Helfer | Erledigt | `PlaylistServiceTests` |
| 5 | ApiClient | `VideoPlayerApiClient.GetNextPlaylistEntryAsync` | Erledigt | `PlaylistServiceTests.Navigation_*` |
| 6 | ApiClient | `VideoPlayerApiClient.GetPreviousPlaylistEntryAsync` | Erledigt | `PlaylistServiceTests.Navigation_*` |
| 7 | ApiClient | `VideoPlayerApiClient.AdvancePlaylistAsync` | Erledigt | `PlaylistServiceTests.Navigation_*` |
| 8 | MVU-Modell | `HomeMediaItem` um `PlaylistId`, `PlaylistName`, `PlaylistEntryId` erweitern | Erledigt | `AppUpdateTests_PlaylistPlayback.ContinueWatchingSelected_*` |
| 9 | MVU-Modell | `PlayerState` um Playlist-Felder + Ableitungen | Erledigt | `AppUpdateTests_PlaylistPlayback.*` |
| 10 | MVU-Modell | Records `PlaylistPlaybackStartInfo` + `PlaylistNavigationItem` | Erledigt | `PlaylistServiceTests` |
| 11 | Logik | `PlaylistService` um `StartPlaybackAsync`, `GetNextEntryAsync`, `GetPreviousEntryAsync`, `AdvanceAsync` | Erledigt | `PlaylistServiceTests` |
| 12 | Logik | `HomeListService.MapContinueWatching` um Playlist-Felder erweitern | Erledigt | `HomeListServiceTests` |
| 13 | Logik | `PlaybackSourceService.ResolveAsync` um `resumePositionSecondsOverride` | Erledigt | `PlaybackSourceServiceTests.ResolveAsync_ResumeOverride_*` |
| 14 | MVU | `AppMessage`: 5 neue Nachrichten | Erledigt | `AppUpdateTests_PlaylistPlayback.*` |
| 15 | MVU | `AppEffect`: 4 neue Effekte + `ResolvePlaybackSourceEffect.StartPositionSeconds` | Erledigt | `AppUpdateTests_PlaylistPlayback.*` |
| 16 | MVU | `AppUpdate`: neue Update-Methoden + Änderungen | Erledigt | `AppUpdateTests_PlaylistPlayback.*` |
| 17 | Effekt-Handler | `ShellAppEffectHandler`: `StartPlaylistPlaybackAsync` + `NavigatePlaylistEntryAsync` + Override-Weitergabe | Erledigt | E2E `PlaylistPlaybackTests` |
| 18 | UI | `PlaylistDetailsPage`: „Abspielen"-Button im Kopfbereich | Erledigt | E2E `PlayButton_*` |
| 19 | UI | `PlayerPage`: Vor/Zurück-Buttons + Playlist-Positionszeile | Erledigt | E2E `NextAndPreviousButtons_*` |
| 20 | DevApi | `DevApiModelSnapshot` + `DevApiModels` um Playlist-Felder | Erledigt | E2E-Assertions (`PlayerPlaylistId`/`EntryId`/`Position`) |
| 21 | E2E-Infra | `MockVideoServer`: POST `play`/`next`/`previous`/`advance` | Erledigt | E2E `PlaylistPlaybackTests` |
| 22 | E2E-Infra | `MockPlaylistEntry.ResumePositionSeconds` + Stream-Bedienung für Playlist-Medien-Ids + `StreamUnavailable` | Erledigt | E2E `PlaylistPlaybackTests` |
| 23 | E2E-Infra | `MockVideoServer`: Playlist-Felder im Weiterschauen-Opt-in | Erledigt | E2E `ContinueWatchingPlaylistEntry_*` |
| 24 | Tests | `AppUpdateTests_PlaylistPlayback` (alle Update-Pfade) | Erledigt | 20 Tests grün |
| 25 | Tests | `PlaylistService`-Playback-Tests (Mapping, 204) | Erledigt | 5 Tests grün |
| 26 | Tests | `HomeListService`-Mapping-Test (Playlist-Bezug) | Erledigt | 1 Test grün |
| 27 | Tests | `PlaybackSourceService`-Override-Test | Erledigt | 1 Test grün |
| 28 | E2E-Tests | `PlaylistPlaybackTests`: 6 Szenarien | Erledigt | 6/6 grün |
| 29 | Doku | `docs/help/`-Dokumentation Playlist-Wiedergabe | Offen | – |
| 30 | Doku | README + RELEASE_NOTES + changes.log | Offen | – |
