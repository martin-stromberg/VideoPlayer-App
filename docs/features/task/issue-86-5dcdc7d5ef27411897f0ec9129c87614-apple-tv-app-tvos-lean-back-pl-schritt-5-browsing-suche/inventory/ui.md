# UI-Bausteine — Bestandsaufnahme

## `HomePageViewController` (tvOS)
Datei: `VideoPlayer.TvOS/Ui/HomePageViewController.cs` (311 Z.)

- `UIViewController, IModelRenderable`; `UIScrollView` + vertikaler `UIStackView` (`content`), Sektions-Neuaufbau bei geänderter Deskriptor-Liste (`appliedKeys`), sonst `SetItems` auf den `HomeCarouselView`-Instanzen.
- `ViewWillAppear` dispatcht `HomeListsReloadRequestedMessage` (Cache-First-Auffrischung); saisonale Ablaufgrenzen über `TimeProvider`-Timer.
- Banner für `IsOffline` und `StorageNotice`; `lastModel`-Cache + `IsViewLoaded`-Guard für Render vor `ViewDidLoad`.
- Item-Größen: `ContinueWatchingItemSize` (480×334), `PosterItemSize` (300×514).

## `HomeCarouselView` (tvOS)
Datei: `VideoPlayer.TvOS/Ui/HomeCarouselView.cs` (132 Z.) — `internal sealed UIView`

- Überschrift + horizontale `UICollectionView` (`UICollectionViewFlowLayout`, `ScrollDirection.Horizontal`, `RemembersLastFocusedIndexPath = true`, `ClipsToBounds = false` für Fokus-Vergrößerung).
- `RegisterClassForCell(typeof(HomeMediaItemCell), HomeMediaItemCell.ReuseId)`; innere `UICollectionViewSource` (`GetItemsCount`, `GetCell` → `cell.Update(item, posters)`, `ItemSelected` → Callback).
- `SetItems` mit `SequenceEqual`-Guard vor `ReloadData`.

## `HomeMediaItemCell` (tvOS)
Datei: `VideoPlayer.TvOS/Ui/HomeMediaItemCell.cs` (167 Z.) — `public sealed UICollectionViewCell`

- `const string ReuseId = "HomeMediaItemCell"`; Registrierungs-Konstruktor `HomeMediaItemCell(NativeHandle)`.
- Poster (`UIImageView`, `TvosPosterImageService.GetImageAsync`, Stale-Guard über `appliedPictureId`), Titel-Fallback-Label, Titelzeile, `UIProgressView`.
- `CanBecomeFocused => true`, `DidUpdateFocus` (Skala 1.06, Titelfarbe), `PrepareForReuse`-Reset.
- `Update(HomeMediaItem, TvosPosterImageService)` — Eingabetyp `HomeMediaItem` (aus `SourceMediaItem` abbildbar).

## `TvosControls` (tvOS)
Datei: `VideoPlayer.TvOS/Ui/TvosControls.cs` — `internal static`

| Fabrik | Element |
|--------|---------|
| `Title`/`Heading`/`Body`/`Error`/`Status` | `UILabel`-Varianten (Schrift/Farbe) |
| `TextField(placeholder, secure)` | `UITextField`, Höhe 72 — öffnet tvOS-Systemtastatur (Diktat/Continuity systemseitig) |
| `Button(title, action)` | `UIButtonType.System` + `PrimaryActionTriggered` |
| `Section(children)` | vertikaler `UIStackView`, `Spacing` 14 |

## `PreparedTargetViewController` (tvOS)
Datei: `VideoPlayer.TvOS/Ui/PreparedTargetViewController.cs` — modal präsentiertes vorbereitetes Ziel; `PressesBegan` → `UIPressType.Menu` → `DismissViewController`; `Update(title, detail)` statt Stapeln.

## `PagePlaceholderViewController` (tvOS)
Datei: `VideoPlayer.TvOS/Ui/PagePlaceholderViewController.cs` — Titel+Untertitel-Platzhalter für Menüseiten ohne Inhalt (Favoriten, Playlists, Downloads).

## `IModelRenderable` (tvOS)
Datei: `VideoPlayer.TvOS/Ui/IModelRenderable.cs` — `void Render(AppModel model)`; wird von `MenuTabBarController.Render` an alle Kindcontroller verteilt.

## `MainViewController` (tvOS)
Datei: `VideoPlayer.TvOS/MainViewController.cs` — Fensterrahmen; `Show(controller)` bettet ein Kind ein (idempotent), `ShowLoading`, `UpdateLoadingStatus`.

## MAUI-Referenz `SourceOverviewPage`
Dateien: `VideoPlayer.Maui/Pages/SourceOverviewPage.xaml(.cs)` (158/156 Z.)

- Genre-Leiste mit „Alle", `SearchBar`, Medien-Raster mit Nachladen.
- `OnAppearing` → `SourceBrowserOpenedMessage(sourceId, sourceName)`.
- Genre-Tap → `SourceGenreSelectedMessage`; `SearchButtonPressed` → `SourceSearchSubmittedMessage`; Eintrags-Auswahl → `SourceMediaItemSelectedMessage`; Listenende → `SourceItemsNextPageRequestedMessage`; Retry → `SourceItemsRetryRequestedMessage`.
- `mirroredSearchText`-Muster: `SearchBar.Text` wird nur bei geändertem Modell-Suchtext zurückgeschrieben (Schutz vor Zurücksetzen des eingegebenen Textes).

## MAUI-Referenz Quellenmenü `AppShell`
Datei: `VideoPlayer.Maui/AppShell.xaml.cs` — Quellen als Flyout-Einträge aus `model.SourcesOverview` (`SourceMenuKind`: None/Sources/Error/Loading/Empty), Auswahl → `SourceSelectedMessage`, Fehler-Eintrag → `SourcesReloadRequestedMessage`, Icons via `api/sourceicons`.
