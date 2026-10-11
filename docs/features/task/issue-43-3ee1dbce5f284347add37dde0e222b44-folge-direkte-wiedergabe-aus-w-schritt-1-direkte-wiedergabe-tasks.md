# Tasks: Direkte Wiedergabe aus der Weiterschauen-Liste

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | MVU | `ContinueWatchingItemSelectedMessage`-Record in `AppMessage.cs` anlegen | Erledigt | `AppUpdateTests_ContinueWatching.ContinueWatchingItemSelected_Movie_StartsPlayback` |
| 2 | MVU | `UpcomingEpisodesLoadedMessage`-Record in `AppMessage.cs` anlegen | Erledigt | `AppUpdateTests_ContinueWatching.UpcomingEpisodesLoaded_MatchingPlayer_SetsQueue` |
| 3 | MVU | `LoadUpcomingEpisodesEffect`-Record in `AppEffect.cs` anlegen | Erledigt | `AppUpdateTests_ContinueWatching.ContinueWatchingItemSelected_Episode_StartsPlaybackWithShowLoad` |
| 4 | Logik | `UpcomingEpisodesService` (`VideoPlayer.Mvu`) mit Online-(`GetTvShowAsync` + `ShowDetailsCache.WriteAsync`) und Offline-Auflösung (`ShowDetailsCache.ReadAsync`) anlegen | Erledigt | `UpcomingEpisodesServiceTests` (5 Tests: Online/Cache/Fallback/Leer-Fälle) |
| 5 | Logik | `UpcomingEpisodesService` in `MauiProgram.cs` als Singleton registrieren | Erledigt | `HomePageContractTests.ShellAppEffectHandler_HandlesLoadUpcomingEpisodesEffect` |
| 6 | MVU | `StartPlayback`-Helfer in `AppUpdate` aus `UpdatePlayRequested` extrahieren | Erledigt | `AppUpdateTests_Player.PlayRequested_*` (unverändert grün) + `ContinueWatchingItemSelected_Movie_StartsPlayback` |
| 7 | MVU | `UpdateContinueWatchingItemSelected` in `AppUpdate` implementieren (Guard, Direktstart, Details-Fallback, `LoadUpcomingEpisodesEffect` bei Episode+`ShowId`) | Erledigt | `AppUpdateTests_ContinueWatching` (8 Fälle) |
| 8 | MVU | `UpdateUpcomingEpisodesLoaded` in `AppUpdate` implementieren (Identitäts-Guard, `UpcomingEpisodes` übernehmen) | Erledigt | `UpcomingEpisodesLoaded_MatchingPlayer_SetsQueue` / `_StaleOrOtherMedia_Discarded` |
| 9 | MVU | Zwei neue Cases in `AppUpdate.Update`-Switch ergänzen | Erledigt | Alle `AppUpdateTests_ContinueWatching`-Tests |
| 10 | UI | `HomePage.xaml`: Weiterschauen-Karussell `AutomationId="ContinueWatchingCarousel"` + `ItemTapped="OnContinueWatchingItemTapped"` | Erledigt | `HomePageContractTests.HomePage_ContinueWatchingCarousel_DispatchesOwnMessage` |
| 11 | UI | `HomePage.xaml.cs`: `OnContinueWatchingItemTapped`-Handler (Dispatch `ContinueWatchingItemSelectedMessage`) | Erledigt | `HomePageContractTests.HomePage_ContinueWatchingCarousel_DispatchesOwnMessage` |
| 12 | Logik | `ShellAppEffectHandler`: `LoadUpcomingEpisodesEffect`-Case + `LoadUpcomingEpisodesAsync` | Erledigt | `HomePageContractTests.ShellAppEffectHandler_HandlesLoadUpcomingEpisodesEffect` + E2E Episoden-Test |
| 13 | DevApi | `DevApiModelSnapshot`: `PlayerMediaId` + `PlayerUpcomingEpisodeCount` ergänzen | Erledigt | `DevApiModelSnapshotTests.DevApiModelSnapshot_MapsUpcomingEpisodes` |
| 14 | E2E-Infrastruktur | `DevApiModels.cs`: `playerMediaId` + `playerUpcomingEpisodeCount` ergänzen | Erledigt | `ContinueWatchingPlaybackTests.*` (Modell-Assertions) |
| 15 | E2E-Infrastruktur | `MockVideoServer`: Episoden-/Show-Opt-in-Fixture + `api/items/tvshow/{id}` + `api/items/tvshowepisode/{id}[/stream]` + `RequestedPaths` | Erledigt | `ContinueWatchingPlaybackTests` (3 Tests) — Hinweis: `PlaybackShowEntryId` heißt implementiert `ContinueWatchingShowId` |
| 16 | Tests | `AppUpdateTests_ContinueWatching` anlegen (Direktstart/Fallback/Guards/`UpcomingEpisodesLoaded`) | Erledigt | 8 Tests bestanden |
| 17 | Tests | `UpcomingEpisodesServiceTests` anlegen (Online/Offline/leere Fälle, `SqliteDatabase`-Collection) | Erledigt | 5 Tests bestanden |
| 18 | Tests | `HomePageContractTests` um getrennte Handler + `AutomationId` + Effekt-Verdrahtung erweitern | Erledigt | 3 neue Tests bestanden |
| 19 | Tests | `DevApiModelSnapshot`-Test um neue Player-Felder erweitern | Erledigt | `DevApiModelSnapshot_MapsUpcomingEpisodes` bestanden |
| 20 | E2E-Tests | `ContinueWatchingPlaybackTests`: Film-Direktstart ab Fortsetzungsposition ohne Detailaufruf | Erledigt | `TapContinueWatchingMovie_StartsPlaybackAtResume_WithoutDetails` bestanden |
| 21 | E2E-Tests | `ContinueWatchingPlaybackTests`: Episoden-Direktstart → `UpcomingEpisodes` + Wechsel zur Folge-Episode | Erledigt | `TapContinueWatchingEpisode_StartsPlaybackAndAdvancesToNextEpisode` bestanden |
| 22 | E2E-Tests | `ContinueWatchingPlaybackTests`: `show`-Eintrag → Detailseite (`HeaderSection`), kein Player | Erledigt | `TapContinueWatchingShowEntry_OpensDetails_NotPlayback` bestanden |
