# Tasks: Startseite mit gecachten Medienlisten

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Datenmodell | `HomeMediaItem`-Record in `VideoPlayer.Mvu` anlegen (`Id`, `MediaType`, `Title`, `PosterPictureId`, `Progress`, `ShowProgress`) | Offen | – |
| 2 | Datenmodell | `HomeLists`-Record in `VideoPlayer.Mvu` anlegen (`ContinueWatching`, `Favorites`, `Recent`) | Offen | – |
| 3 | Logik | `HomeListService` in `VideoPlayer.Mvu` anlegen (`LoadHomeListsAsync`, defensives Deserialisieren, Mapping) | Offen | – |
| 4 | MVU | `HomeListsLoadedMessage`, `CachedListsChangedMessage`, `MediaItemSelectedMessage` in `AppMessage.cs` ergänzen | Offen | – |
| 5 | MVU | `LoadHomeListsEffect`, `NavigateToMediaDetailsEffect` in `AppEffect.cs` ergänzen | Offen | – |
| 6 | MVU | `AppModel` um `ContinueWatching`/`Favorites`/`RecentEntries` + `Has…`-Flags erweitern | Offen | – |
| 7 | MVU | `AppUpdate`: neue Cases + `LoadHomeListsEffect` bei `StartupRequested`/`SynchronizationCompleted` | Offen | – |
| 8 | App-Rand | `ShellAppEffectHandler`: Cases für neue Effekte, `CachedListsChangedMessage` nach Teil-Syncs | Offen | – |
| 9 | UI | `PosterImageService` in `VideoPlayer.Maui/Services` (Datei-Cache `PreviewsDirectory`, `GetPictureAsync`) | Offen | – |
| 10 | UI | `CachedPosterImage`-Control in `VideoPlayer.Maui/Controls` (`PictureId`-Property) | Offen | – |
| 11 | UI | `HomePage.xaml(.cs)` ausbauen: drei Listen-Sektionen, Item-Template, Tap-Handler | Offen | – |
| 12 | UI | `MediaDetailsPage` (Platzhalter, Query-Properties `mediaType`/`id`/`title`) anlegen | Offen | – |
| 13 | Verdrahtung | `AppShell`: Route `mediadetails` registrieren; `MauiProgram`: `HomeListService`, `PosterImageService`, `MediaDetailsPage` | Offen | – |
| 14 | Tests | `AppUpdateTests_HomeLists` (Start, Sync, Ereignis-Sequenz, Auswahl) | Offen | – |
| 15 | Tests | `HomeListServiceTests` (reale SQLite-Ablage: Mapping, Leere, Fehlerfälle) | Offen | – |
| 16 | Tests | `HomePageContractTests` (XAML-Struktur, Route, DI, Effekt-Handler) | Offen | – |
| 17 | Tests | `AppUpdateTests_NetworkSync.SynchronizationCompleted_ClearsSyncFlag` an `LoadHomeListsEffect` anpassen | Offen | – |
