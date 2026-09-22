# Tasks: Startseite mit gecachten Medienlisten

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Datenmodell | `HomeMediaItem`-Record in `VideoPlayer.Mvu` anlegen (`Id`, `MediaType`, `Title`, `PosterPictureId`, `Progress`, `ShowProgress`) | Erledigt | `HomeListServiceTests`, `AppUpdateTests_HomeLists` |
| 2 | Datenmodell | `HomeLists`-Record in `VideoPlayer.Mvu` anlegen (`ContinueWatching`, `Favorites`, `Recent`) | Erledigt | `HomeListServiceTests` |
| 3 | Logik | `HomeListService` in `VideoPlayer.Mvu` anlegen (`LoadHomeListsAsync`, defensives Deserialisieren, Mapping) | Erledigt | `HomeListServiceTests` (7 Tests) |
| 4 | MVU | `HomeListsLoadedMessage`, `CachedListsChangedMessage`, `MediaItemSelectedMessage` in `AppMessage.cs` ergänzen | Erledigt | `AppUpdateTests_HomeLists` |
| 5 | MVU | `LoadHomeListsEffect`, `NavigateToMediaDetailsEffect` in `AppEffect.cs` ergänzen | Erledigt | `AppUpdateTests_HomeLists` |
| 6 | MVU | `AppModel` um `ContinueWatching`/`Favorites`/`RecentEntries` + `Has…`-Flags erweitern | Erledigt | `HomeListsLoaded_SetsListsAndFlags` |
| 7 | MVU | `AppUpdate`: neue Cases + `LoadHomeListsEffect` bei `StartupRequested`/`SynchronizationCompleted` | Erledigt | `StartupRequested_LoadsHomeListsAfterStorageInit`, `SynchronizationCompleted_ClearsSyncFlag` |
| 8 | App-Rand | `ShellAppEffectHandler`: Cases für neue Effekte, `CachedListsChangedMessage` nach Teil-Syncs | Erledigt | `HomePageContractTests.ShellAppEffectHandler_*` |
| 9 | UI | `PosterImageService` in `VideoPlayer.Maui/Services` (Datei-Cache `PreviewsDirectory`, `GetPictureAsync`) | Erledigt | `PosterImageService_UsesNonBackupPreviewDirectory` |
| 10 | UI | `CachedPosterImage`-Control in `VideoPlayer.Maui/Controls` (`PictureId`-Property) | Erledigt | `MediaCarousel_ItemsAreTappable_PostersViaCache` |
| 11 | UI | `HomePage.xaml(.cs)` ausbauen: drei Listen-Sektionen, Item-Template, Tap-Handler | Erledigt (als `MediaCarousel`-Control) | `HomePageContractTests` |
| 12 | UI | `MediaDetailsPage` (Platzhalter, Query-Properties `mediaType`/`id`/`title`) anlegen | Erledigt | `MediaDetails_RouteRegistered_AndPageReadsQueryParameters` |
| 13 | Verdrahtung | `AppShell`: Route `mediadetails` registrieren; `MauiProgram`: `HomeListService`, `PosterImageService`, `MediaDetailsPage` | Erledigt | `HomePageContractTests` |
| 14 | Tests | `AppUpdateTests_HomeLists` (Start, Sync, Ereignis-Sequenz, Auswahl) | Erledigt | 10 Tests |
| 15 | Tests | `HomeListServiceTests` (reale SQLite-Ablage: Mapping, Leere, Fehlerfälle) | Erledigt | 7 Tests |
| 16 | Tests | `HomePageContractTests` (XAML-Struktur, Route, DI, Effekt-Handler) | Erledigt | 7 Tests |
| 17 | Tests | `AppUpdateTests_NetworkSync.SynchronizationCompleted_ClearsSyncFlag` an `LoadHomeListsEffect` anpassen | Erledigt | angepasst + `MvuHostTests`, `AppUpdateTests_Startup` |
