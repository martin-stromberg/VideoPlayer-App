# Tasks: Quellen-Browsing und Medien-Detailseiten

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Datenmodell | Anzeige-Records in `VideoPlayer.Mvu` anlegen: `SourceItem`, `SourcesOverviewState`, `SourceGenreItem`, `SourceMediaItem`, `SourceBrowserState` (`PageSize = 20`), `SeasonItem`, `EpisodeItem`, `CollectionMovieItem`, `MediaDetailsView`, `MediaDetailsState`, `MediaTypeNames` | Offen | `AppUpdateTests_Sources`, `AppUpdateTests_MediaDetails` |
| 2 | Logik | `SourceBrowserService` (`LoadSourcesAsync`, `LoadGenresAsync` inkl. „Alle", `LoadItemsAsync` mit Paging/Suche/Genre) | Offen | `SourceBrowserServiceTests` |
| 3 | Logik | `MediaDetailsService` (`LoadAsync`: Movie/Collection/TvShow/Episode/Season inkl. Serienkontext, Kollektions-404-Fallback) | Offen | `MediaDetailsServiceTests` |
| 4 | Logik | `FavoritesService` (`ToggleAsync` → `ToggleFavoriteAsync` + `SyncFavoritesAsync`, typkorrekter `MediaEntry`) | Offen | `FavoritesServiceTests` |
| 5 | MVU | Messages in `AppMessage.cs` ergänzen (Quellen, Browser, Details, Staffelwahl, Favoriten-Toggle) | Offen | `AppUpdateTests_*` |
| 6 | MVU | Effekte in `AppEffect.cs` ergänzen (`LoadSourcesEffect`, `NavigateToSourceOverviewEffect`, `LoadSourceGenresEffect`, `LoadSourceItemsEffect`, `LoadMediaDetailsEffect`, `ToggleFavoriteEffect`); `NavigateToMediaDetailsEffect` um `ShowId`/`SeasonId`/`EpisodeId` erweitern | Offen | `AppUpdateTests_*` |
| 7 | MVU | `AppModel` um `SourcesOverview`/`SourceBrowser`/`MediaDetails` erweitern | Offen | `AppUpdateTests_*` |
| 8 | MVU | `AppUpdate`: alle neuen Cases inkl. Guards/Dedup/Rücksetzen | Offen | `AppUpdateTests_Sources`, `AppUpdateTests_MediaDetails` |
| 9 | MVU | `HomeMediaItem` + `HomeListService`: `ShowId`/`SeasonId`/`EpisodeId`-Hints aus eingebettetem Graph | Offen | `HomeListServiceTests` |
| 10 | App-Rand | `ShellAppEffectHandler`: Cases für neue Effekte + `CachedListsChangedMessage` nach Toggle | Offen | `SourcesContractTests` |
| 11 | UI | `PosterImageService.GetSourceIconSourceAsync` + `CachedSourceIconImage`-Control | Offen | `SourcesContractTests` |
| 12 | UI | `SourcesPage` ausbauen (Quellenliste, Offline-Hinweis, Lade-/Fehler-/Leerzustand) | Offen | `SourcesContractTests` |
| 13 | UI | `SourceOverviewPage` neu (Genre-Leiste, Suche, Raster, `RemainingItemsThreshold`) | Offen | `SourcesContractTests` |
| 14 | UI | `MediaDetailsPage` ausbauen (Banner/Poster, Metadaten, Stern, Picker + Episodenliste, Filmraster, Fehlerzustand; Query-Properties `show`/`season`/`episode`) | Offen | `SourcesContractTests` |
| 15 | Verdrahtung | `AppShell.xaml.cs`: Route `sourceoverview`; `MauiProgram`: Dienste + Seite registrieren | Offen | `SourcesContractTests` |
| 16 | Tests | `AppUpdateTests_Sources` + `AppUpdateTests_MediaDetails` (MVU-Sequenzen) | Offen | Testlauf |
| 17 | Tests | `SourceBrowserServiceTests`, `MediaDetailsServiceTests`, `FavoritesServiceTests` | Offen | Testlauf |
| 18 | Tests | `SourcesContractTests` (XAML-/Route-/DI-/Handler-Verdrahtung) | Offen | Testlauf |
| 19 | Doku | `docs/help/` (neue Seite Quellen + Details), `README.md`, `docs/RELEASE_NOTES.md`, `changes.log` | Offen | — |
