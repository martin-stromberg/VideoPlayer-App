# Logik — Bestandsaufnahme

## `SourceBrowserService` (plattformneutral)
Datei: `VideoPlayer.Mvu/SourceBrowserService.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `LoadSourcesAsync(ct)` | public | `api/Sources` → `IReadOnlyList<SourceItem>` (Id > 0) |
| `LoadGenresAsync(sourceId, ct)` | public | `api/SourceGenres/{id}` + vorangestelltes „Alle" (`AllGenresName`, Id 0, `IsSelected = true`); ohne Client nur „Alle" |
| `LoadItemsAsync(sourceId, page, searchText, genreId, ct)` | public | `api/items` seitenweise (`PageSize` 20) → `SourceItemsPage(Page, Items, HasMore)`; `HasMore` = volle Seite |
| `MapSummaryType` | internal static | Server-Typ → CLR-Typ (`movie`/`collection` → `moviecollection`, …) |

`AllGenresName` = `„Alle"` (public const).

## `AppUpdate`-Handler (plattformneutral, `VideoPlayer.Mvu/AppUpdate.cs`)

| Handler | Zeile | Verhalten |
|---------|-------|-----------|
| `UpdateSourceSelected` | 1581 | `SourceBrowser = NewBrowser` + `NavigateToSourceOverviewEffect`, `LoadSourceGenresEffect`, `LoadSourceItemsEffect(0, null, null, Append: false)` |
| `UpdateSourceBrowserOpened` | 1597 | Gleiche Quelle → `Unchanged` (Zustand/Liste bleibt); fremde Quelle → Neu-Initialisierung ohne Navigations-Effekt |
| `UpdateSourceGenresLoaded` | 1621 | Genres in `SourceBrowser`, Auswahl-Markierung via `SelectedGenreId`; Stale-Guard über `SourceId` |
| `UpdateSourceItemsLoaded` | 1643 | Append: `Page != NextPage` → verwerfen; Fehler bei Append → Liste bleibt; Fehler bei Replace → `Error`; sonst `Items`/`NextPage`/`HasMore` |
| `UpdateSourceGenreSelected` | 1726 | `SelectedGenreId` setzen, `ReloadBrowserItems` (Liste zurück, Seite 0) |
| `UpdateSourceSearchSubmitted` | 1744 | `SearchText` (getrimmt) setzen, `ReloadBrowserItems` |
| `UpdateSourceItemsNextPageRequested` | 1756 | Guards (`HasMore`, `IsLoading*`) → `IsLoadingMore` + `LoadSourceItemsEffect(NextPage, Append: true)` |
| `UpdateSourceItemsRetryRequested` | 1774 | Liste vorhanden → Folgeseite erneut; sonst `ReloadBrowserItems` |
| `UpdateSourceMediaItemSelected` | 1789 | `NavigateToMediaDetailsEffect(MediaType, Id, Title)` |
| `UpdateSourcesReloadRequested` | 1561 | `WithSourcesReload` → `IsLoading` + `LoadSourcesEffect` |
| `WithSourcesReload` | 1187 | Setzt `SourcesOverview.IsLoading`, hängt `LoadSourcesEffect` an (aufgerufen bei Login/Sync/Ereignis) |

`NewBrowser` (Z. 1573): `SourceId`, `SourceName`, `IsLoading = true`, `HasMore = true`.

## `ShellAppEffectHandler` (MAUI-Referenz der Effekt-Verdrahtung)
Datei: `VideoPlayer.Maui/Services/ShellAppEffectHandler.cs`

| Methode | Zeile | Verhalten |
|---------|-------|-----------|
| `LoadSourcesAsync` | 811 | `SourceBrowserService.LoadSourcesAsync` → `SourcesLoadedMessage(sources, error)`; Fehlertext „Die Quellen konnten nicht geladen werden." |
| `NavigateToSourceOverviewAsync` | 734 | Shell-Route `sourceoverview?id=…&name=…` mit Duplikat-Guard |
| `LoadSourceGenresAsync` | 829 | Fehler nicht fatal → Fallback nur „Alle"; `SourceGenresLoadedMessage` |
| `LoadSourceItemsAsync` | 848 | Fehler → leere Seite + Fehlertext „Die Einträge konnten nicht geladen werden."; `SourceItemsLoadedMessage` |

## `TvosAppEffectHandler` (tvOS-Rand — zu erweitern)
Datei: `VideoPlayer.TvOS/Services/TvosAppEffectHandler.cs` (766 Z.)

- `HandleAsync`-Switch: verdrahtet Start-/Verbindungs-/Sync-/Home-Effekte; `NavigateToMediaDetailsEffect`, `NavigateToPlayerEffect`, `StartPlaylistPlaybackEffect` → `navigator.PresentPreparedTarget` (Z. 153–187, über `MainThreadInvoker.InvokeOnMainThreadAsync`).
- `LoadSourcesEffect`, `LoadSourceGenresEffect`, `LoadSourceItemsEffect`, `NavigateToSourceOverviewEffect` fallen aktuell in den `default`-Zweig (protokolliertes No-Op, Z. 191–196).
- Fehler-Muster der privaten Lademethoden: `try/catch` mit `LogError` + Fallback-Ergebnis → `DispatchMessageAsync` (Z. 736) — dispatcht über `services.Host` auf dem Hauptthread.
- `services` (AppComposition) wird zum Auflösen der Dienste genutzt (`services.ServerSync`, `services.HomeLists`, `services.Host.Model`, …).

## `TvosNavigator` (tvOS — zu erweitern)
Datei: `VideoPlayer.TvOS/Ui/TvosNavigator.cs` (143 Z.)

| Member | Beschreibung |
|--------|--------------|
| `NavigateTo(AppPage)` | Loading/ServerSetup/Login als Einzelansicht, Menüseiten via `MenuTabBarController.Select`; idempotent |
| `Render(AppModel)` | `NavigateTo(model.CurrentPage)` + `Render`-Verteilung an Workflow-Seiten und `tabBar` |
| `PresentPreparedTarget(title, detail)` | Modal Full-Screen über `main.PresentViewController`; bereits präsentiertes `PreparedTargetViewController` wird per `Update` aktualisiert statt gestapelt |
| `LastMenuPage` | Rückziel der Menü-Taste auf Workflow-Seiten |

Präsentationsanker ist `main` (`MainViewController`) — bei bereits präsentiertem Controller (Browser-Seite) muss das Folgeziel auf dem obersten präsentierten Controller gestapelt werden.

## `MenuTabBarController`
Datei: `VideoPlayer.TvOS/Ui/MenuTabBarController.cs` — Tabs nach `AppRoutes.MenuPages`; `Render` verteilt an `IModelRenderable`-Kinder; `CreatePageController` mappt `AppPage` → Controller (`HomePageViewController`, `ProfilePageViewController`, `SettingsPageViewController`, sonst `PagePlaceholderViewController`). `IReadOnlyList.IndexOf` existiert nicht — `IndexOfMenuPage` ist eine eigene Schleife (Kompilierfalle).

## `AppComposition`
Datei: `VideoPlayer.TvOS/AppComposition.cs` — manuelle Singleton-Verdrahtung; bisher ohne `SourceBrowserService`. `CreateRuntime` erzeugt `TvosNavigator` + `TvosAppEffectHandler` + `MvuHost`, `ModelChanged` → `navigator.Render` auf dem Hauptthread.

## `TvosPosterImageService`
Datei: `VideoPlayer.TvOS/Services/TvosPosterImageService.cs` — `GetImageAsync(pictureId, ct)` → `UIImage?`; Cache-Konvention: `ConcurrentDictionary<long, UIImage>` → Datei `{pictureId}.poster` in `AppStorageLayout.PreviewsDirectory` → `client.GetPictureAsync`. Fehler → `null` (kein Fehlerzustand).

## `VideoPlayerApiClient` (relevante Endpunkte)
Datei: `VideoPlayer.ApiClient/VideoPlayerApiClient.cs` — `GetPictureAsync(long, ct)` (Z. 470, `api/pictures/{id}`), `GetSourceIconAsync(long, ct)` (Z. 485, `api/sourceicons/{id}`), `GetSourcesAsync`, `GetSourceGenresAsync`, `GetSourceItemsAsync` (Paging/Suche/Genre).
