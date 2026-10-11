# Tasks: Direkte Wiedergabe aus der Weiterschauen-Liste

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | MVU | `ContinueWatchingItemSelectedMessage`-Record in `AppMessage.cs` anlegen | Offen | — |
| 2 | MVU | `UpcomingEpisodesLoadedMessage`-Record in `AppMessage.cs` anlegen | Offen | — |
| 3 | MVU | `LoadUpcomingEpisodesEffect`-Record in `AppEffect.cs` anlegen | Offen | — |
| 4 | Logik | `UpcomingEpisodesService` (`VideoPlayer.Mvu`) mit Online-(`GetTvShowAsync` + `ShowDetailsCache.WriteAsync`) und Offline-Auflösung (`ShowDetailsCache.ReadAsync`) anlegen | Offen | — |
| 5 | Logik | `UpcomingEpisodesService` in `MauiProgram.cs` als Singleton registrieren | Offen | — |
| 6 | MVU | `StartPlayback`-Helfer in `AppUpdate` aus `UpdatePlayRequested` extrahieren | Offen | — |
| 7 | MVU | `UpdateContinueWatchingItemSelected` in `AppUpdate` implementieren (Guard, Direktstart, Details-Fallback, `LoadUpcomingEpisodesEffect` bei Episode+`ShowId`) | Offen | — |
| 8 | MVU | `UpdateUpcomingEpisodesLoaded` in `AppUpdate` implementieren (Identitäts-Guard, `UpcomingEpisodes` übernehmen) | Offen | — |
| 9 | MVU | Zwei neue Cases in `AppUpdate.Update`-Switch ergänzen | Offen | — |
| 10 | UI | `HomePage.xaml`: Weiterschauen-Karussell `AutomationId="ContinueWatchingCarousel"` + `ItemTapped="OnContinueWatchingItemTapped"` | Offen | — |
| 11 | UI | `HomePage.xaml.cs`: `OnContinueWatchingItemTapped`-Handler (Dispatch `ContinueWatchingItemSelectedMessage`) | Offen | — |
| 12 | Logik | `ShellAppEffectHandler`: `LoadUpcomingEpisodesEffect`-Case + `LoadUpcomingEpisodesAsync` | Offen | — |
| 13 | DevApi | `DevApiModelSnapshot`: `PlayerMediaId` + `PlayerUpcomingEpisodeCount` ergänzen | Offen | — |
| 14 | E2E-Infrastruktur | `DevApiModels.cs`: `playerMediaId` + `playerUpcomingEpisodeCount` ergänzen | Offen | — |
| 15 | E2E-Infrastruktur | `MockVideoServer`: Episoden-/Show-Opt-in-Fixture + `api/items/tvshow/{id}` + `api/items/tvshowepisode/{id}[/stream]` + `RequestedPaths` | Offen | — |
| 16 | Tests | `AppUpdateTests_ContinueWatching` anlegen (Direktstart/Fallback/Guards/`UpcomingEpisodesLoaded`) | Offen | — |
| 17 | Tests | `UpcomingEpisodesServiceTests` anlegen (Online/Offline/leere Fälle, `SqliteDatabase`-Collection) | Offen | — |
| 18 | Tests | `HomePageContractTests` um getrennte Handler + `AutomationId` + Effekt-Verdrahtung erweitern | Offen | — |
| 19 | Tests | `DevApiModelSnapshot`-Test um neue Player-Felder erweitern | Offen | — |
| 20 | E2E-Tests | `ContinueWatchingPlaybackTests`: Film-Direktstart ab Fortsetzungsposition ohne Detailaufruf | Offen | — |
| 21 | E2E-Tests | `ContinueWatchingPlaybackTests`: Episoden-Direktstart → `UpcomingEpisodes` + Wechsel zur Folge-Episode | Offen | — |
| 22 | E2E-Tests | `ContinueWatchingPlaybackTests`: `show`-Eintrag → Detailseite (`HeaderSection`), kein Player | Offen | — |
