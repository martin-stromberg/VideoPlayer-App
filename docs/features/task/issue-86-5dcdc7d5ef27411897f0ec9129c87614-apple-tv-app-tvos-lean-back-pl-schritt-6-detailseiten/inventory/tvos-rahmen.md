# Bestand: tvOS-Seitenrahmen und Infrastruktur

## `TvosNavigator` (`Ui/TvosNavigator.cs`)

- `NavigateTo(AppPage)`: Loading/ServerSetup/Login als Einzelansicht,
  Menüseiten im `MenuTabBarController`; `Render(model)` verteilt den
  Modellstand an alle Seiten.
- `PresentSourceBrowser(id, name)`: eine wiederverwendete modale
  `SourceBrowserViewController`-Instanz (Duplikat-Guard).
- `PresentPreparedTarget(title, detail)`: modaler Platzhalter; ein
  bereits präsentiertes Ziel wird aktualisiert statt gestapelt.
- `TopmostPresented()`: oberster Controller der Präsentationskette —
  Anker für modale Stapel (Detailseite über dem Browser etc.).

## `TvosAppEffectHandler` (`Services/TvosAppEffectHandler.cs`)

- Verdrahtet: Start-/Verbindungs-Workflow, Sync, Startseiten-Listen,
  Quellen-Browser-Effekte, `NavigateToSourceOverviewEffect` →
  `PresentSourceBrowser`, `NavigateToMediaDetailsEffect` →
  `PresentPreparedTarget` (Zeile ~172), `NavigateToPlayerEffect` +
  `StartPlaylistPlaybackEffect` → `PresentPreparedTarget`.
- Default-Fall: protokolliertes No-Op (u. a. `LoadMediaDetailsEffect`,
  `LoadMediaDetailsHeaderEffect`, `ToggleFavoriteEffect`,
  `EnqueueDownloadEffect`, `ResolvePlaybackSourceEffect`).
- `DispatchMessageAsync` marshalt auf den Hauptthread ins `MvuHost`.

## `AppComposition` (`AppComposition.cs`)

- Vorhanden: `AuthSession`, `CacheDatabase`, `StorageLayout`,
  `ServerSync`, `HomeLists`, `SourceBrowser`, `OfflineContinueWatching`,
  `PosterImages` (`TvosPosterImageService`), `NetworkMonitor`, …
- Fehlt: `MediaDetailsService` (Ctor: AuthSession, CacheDatabase,
  StorageLayout, Logger) und `FavoritesService` (Ctor: AuthSession,
  ServerSyncService).
- `CreateRuntime(main)` verdrahtet `TvosNavigator`, `TvosAppEffectHandler`,
  `MvuHost`; `ModelChanged` → `navigator.Render` (Hauptthread).

## UI-Bausteine (`Ui/`)

- `TvosControls`: `Title`, `Heading`, `Body`, `Error`, `Status`,
  `TextField(placeholder, secure)`, `Button(title, action)`,
  `Section(params UIView[])` — Signaturen geprüft.
- `HomeMediaItemCell`: 2:3-Posterkarte (Update mit `HomeMediaItem`,
  Poster via `TvosPosterImageService.GetImageAsync`, Progress-Balken,
  `CardOpacity`), fokussierbar mit Fokus-Vergrößerung.
- `HomeCarouselView`: Überschrift + horizontale `UICollectionView`
  (`HomeMediaItemCell`), `SetItems(IReadOnlyList<HomeMediaItem>)`.
- `SourceGenreCell`: Text-Chip mit `IsSelected`-Darstellung —
  wiederverwendbar als Staffel-Chip (Mapping `SeasonItem` →
  `SourceGenreItem{Id,Name,IsSelected}`).
- `TvosPosterImageService`: `GetImageAsync(pictureId)` → `UIImage?`
  (Mem → Datei → `api/pictures/{id}`); `GetSourceIconAsync`.
- `IModelRenderable`: `Render(AppModel)` — Seiten-Vertrag.
- Muster modaler Seiten: `SourceBrowserViewController` (Vollbild,
  `ViewWillAppear` → Opened-Message, `PressesBegan` Menü-Taste →
  `DismissViewController`, Identitäts-Guard beim Rendern,
  Lade-/Fehler-/Leer-Zustände mit „Erneut versuchen").

## CI

- `build-tvos` (macOS): `dotnet build VideoPlayer.TvOS … -f net10.0-tvos
  --runtime tvossimulator-arm64 -p:EnableCodeSigning=false`, Trigger
  `push` auf `task/issue-86-**`.
