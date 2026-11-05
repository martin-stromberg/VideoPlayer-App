# Umsetzungsplan: tvOS-Startseite mit fokussierbaren Karussells (Schritt 4)

Eingaben: `requirement.md`, `inventory.md` + Detaildokumente.

## Zielbild

`AppPage.Home` erhält eine echte modellgetriebene Seite (`IModelRenderable`) mit vertikal gestapelten, horizontal scrollenden `UICollectionView`-Karussells („Weiterschauen" 16:9, „Favoriten" und „Neu im Programm" sowie saisonale Sektionen 2:3). Die Auswahl dispatcht die bestehenden MVU-Messages; die daraus resultierenden Navigations-Effekte werden auf einem modalen Platzhalter-Ziel abgebildet. Poster laufen über einen tvOS-eigenen, dreistufigen Bild-Cache (`UIImage` statt `ImageSource`).

## Arbeitsschritte

### 1. `VideoPlayer.TvOS/Services/TvosPosterImageService.cs` (neu)

- Dreistufiger Poster-Cache analog `PosterImageService` (MAUI): `ConcurrentDictionary<long, UIImage>` → Datei `{id}.poster` in `AppStorageLayout.PreviewsDirectory` → `AuthSessionService.Client.GetPictureAsync` + `File.WriteAllBytesAsync`.
- `Task<UIImage?> GetImageAsync(long pictureId, CancellationToken)`; Fehlschlag → `null` + `LogWarning` (kein Fehlerzustand); `UIImage.LoadFromData(NSData.FromArray(bytes))` bzw. `NSData.FromFile` für Dateitreffer.
- Konstruktor: `(AuthSessionService, AppStorageLayout, ILogger<TvosPosterImageService>)`.

### 2. `VideoPlayer.TvOS/Ui/HomeMediaItemCell.cs` (neu)

- `UICollectionViewCell`, `public static readonly NSString ReuseId`; Konstruktor `HomeMediaItemCell(ObjCRuntime.NativeHandle)` (Pattern für `RegisterClassForCells`).
- Subviews in `ContentView`: `UIImageView` (Poster, `CornerRadius` ~10, `ClipsToBounds`), zentriertes Fallback-`UILabel` (Titel, wenn kein Bild), `UILabel` Titel unter dem Poster, `UIProgressView` am Posterrand (`Hidden` ↔ `item.ShowProgress`).
- `Update(HomeMediaItem, TvosPosterImageService)`: Alpha = `item.CardOpacity`; Bild-Generations-Check über `appliedPictureId`; asynchrones Nachladen, Rückschrieb via `MainThreadInvoker.InvokeOnMainThread`.
- `CanBecomeFocused => true`; `DidUpdateFocus` → `AddCoordinatedAnimations`: `CGAffineTransform.MakeScale(1.06)` bei Fokus, `MakeIdentity` sonst; `PrepareForReuse` setzt Bild/Fortschritt zurück.

### 3. `VideoPlayer.TvOS/Ui/HomeCarouselView.cs` (neu)

- `UIView` mit `UILabel` (Sektionstitel via `TvosControls.Heading`) + `UICollectionView` (`UICollectionViewFlowLayout`, `ScrollDirection.Horizontal`, feste `ItemSize`, `MinimumInteritemSpacing`), `ClipsToBounds = false` (Fokus-Skalierung darf überlaufen), `RemembersLastFocusedIndexPath = true`, `BackgroundColor = Clear`.
- `RegisterClassForCells(typeof(HomeMediaItemCell), HomeMediaItemCell.ReuseId)`; interne `HomeCarouselSource : UICollectionViewSource` (`GetItemsCount`, `GetCell` mit Cast `(HomeMediaItemCell)DequeueReusableCell`, `ItemSelected` → Callback).
- `SetItems(IReadOnlyList<HomeMediaItem>)`: `SequenceEqual`-Vergleich (Records) → `ReloadData` nur bei Änderung; leere Liste → `Hidden = true` der ganzen Sektion.

### 4. `VideoPlayer.TvOS/Ui/HomePageViewController.cs` (neu)

- `UIViewController, IModelRenderable`; Konstruktor `(Func<AppMessage, Task> dispatch, TvosPosterImageService posters)`.
- Aufbau `ViewDidLoad`: schwarzer Hintergrund; `UIScrollView` (an Rändern gepinnt) + vertikaler `UIStackView`-Inhalt: `TvosControls.Title("Startseite")`, Offline-Banner (`IsOffline`), `StorageNotice`-Banner, dynamische `HomeCarouselView`-Sektionen.
- `Render(AppModel)`: `lastModel`-Cache + `IsViewLoaded`-Guard (kein erzwungenes View-Laden für unsichtbare Tabs); Banner aktualisieren; Sektions-Deskriptoren bauen (`Weiterschauen`/`Favorites`/`Neu im Programm`/je `SeasonalGenres`-Eintrag, leere ausgelassen); bei geänderter Schlüsselliste Sektionen neu aufbauen, sonst `SetItems` pro Sektion; `ScheduleExpiry`.
- `ViewWillAppear`: `HomeListsReloadRequestedMessage` dispatchen + `ScheduleExpiry`; `ViewDidDisappear`: Timer verwerfen (`System.Threading.Timer` via `TimeProvider.System.CreateTimer`, Logik wie MAUI `ScheduleExpiry`: nächstes `ExpiresAt`, gekappt auf 1 min, abgelaufen → `HomeListsReloadRequestedMessage`).
- Auswahl-Callbacks: Weiterschauen-Sektion → `ContinueWatchingItemSelectedMessage`, alle übrigen → `MediaItemSelectedMessage`; `IsPlaceholder`-Einträge bleiben fokussierbar (abgedunkelt), die Auswahl wird nicht dispatcht (`IsSelectable`-Guard liegt zusätzlich im Update).

### 5. `VideoPlayer.TvOS/Ui/PreparedTargetViewController.cs` (neu)

- Vollbild-Platzhalter für vorbereitete Navigationsziele: `TvosControls.Title`/`Heading` + Titel des Mediums + Hinweistext + fokussierbarer „Zurück"-Button (`DismissViewController`).
- `PressesBegan` mit `UIPressType.Menu` → `DismissViewController(true, null)` (Muster `ServerSetupViewController`).
- `Update(title, detail)` aktualisiert Texte (`IsViewLoaded`-Guard) — erlaubt dem Navigator die Wiederverwendung bei aufeinanderfolgenden Effekten.

### 6. Verdrahtung

- `AppComposition`: `PosterImages`-Eigenschaft (`TvosPosterImageService` nach `AuthSession` erstellen); `CreateRuntime` → `new TvosNavigator(Dispatch, PosterImages)`.
- `TvosNavigator`: Konstruktor um `TvosPosterImageService` erweitern; `tabBar ??= new MenuTabBarController(dispatch, posters)`; neue Methode `PresentPreparedTarget(string title, string detail)` — präsentiert `PreparedTargetViewController` modal auf `main` (`UIModalPresentationStyle.FullScreen`); ist bereits einer präsentiert, wird er per `Update` aktualisiert statt gestapelt (Effekt-Folge Detail→Player).
- `MenuTabBarController`: Konstruktor um `TvosPosterImageService` erweitern; `AppPage.Home => new HomePageViewController(dispatch, posters)`.
- `TvosAppEffectHandler` — drei neue Fälle vor `default:`:
  - `NavigateToMediaDetailsEffect details` → `navigator.PresentPreparedTarget(details.Title, "Die Detailansicht folgt in einem späteren Entwicklungsschritt.")` (Hauptthread).
  - `NavigateToPlayerEffect` → `navigator.PresentPreparedTarget(model.Player.Title ?? "Wiedergabe", "Die Wiedergabe folgt in einem späteren Entwicklungsschritt.")`.
  - `StartPlaylistPlaybackEffect` → `navigator.PresentPreparedTarget("Wiedergabe", "Die Playlist-Wiedergabe folgt in einem späteren Entwicklungsschritt.")`.
  - `ResolvePlaybackSourceEffect`, `LoadUpcomingEpisodesEffect`, `LoadMediaDetailsEffect` bleiben bewusst No-Op (kein UI-/Datenbedarf dieses Schritts).

### 7. Tests (`VideoPlayer.Maui.Tests/TvOsHomePageContractTests.cs`, neu)

Quell-Contract-Tests nach `TvOsConnectionContractTests`-Muster:
- `HomePageViewController`: `UICollectionView`, `IModelRenderable`, `ContinueWatching`/`Favorites`/`RecentEntries`/`SeasonalGenres`, `ContinueWatchingItemSelectedMessage`, `MediaItemSelectedMessage`, `HomeListsReloadRequestedMessage`, `IsOffline`, `StorageNotice`, `IsPlaceholder`.
- `HomeMediaItemCell`: `UICollectionViewCell`, `UIImageView`, `UIProgressView`, `CanBecomeFocused`, `DidUpdateFocus`, `PrepareForReuse`.
- `HomeCarouselView`: `UICollectionViewFlowLayout`, `UICollectionViewSource`, `RegisterClassForCells`, `RemembersLastFocusedIndexPath`, `ItemSelected`, `ReloadData`, `SequenceEqual`.
- `TvosPosterImageService`: `GetPictureAsync`, `PreviewsDirectory`, `UIImage`, `ConcurrentDictionary`.
- Verdrahtung: `MenuTabBarController` enthält `HomePageViewController`; `AppComposition` enthält `TvosPosterImageService`; `TvosNavigator` enthält `PresentPreparedTarget` + `PreparedTargetViewController`; `TvosAppEffectHandler` enthält `case NavigateToMediaDetailsEffect`, `case NavigateToPlayerEffect`, `case StartPlaylistPlaybackEffect`.
- Negativ: `TvosAppEffectHandler` verdrahtet keine Mutations-Effekte (`ContinueWatchingHideEffect`/`ContinueWatchingSkipEffect`/`ToggleFavoriteEffect` — Strings dürfen nicht vorkommen).
- `PreparedTargetViewController`: `PressesBegan`, `UIPressType.Menu`, `DismissViewController`, `UIModalPresentationStyle.FullScreen`.

## Abgrenzung / bewusst offen

- Kein Hero-Bereich, kein Kontextmenü (Ausblenden/Überspringen), keine Favoriten-Markierung — gehören nicht zum Schritt; zugehörige Messages/Effekte bleiben unverdrahtet.
- `ResolvePlaybackSourceEffect`/`NavigateToPlayerEffect` ohne echte Player-Seite — Platzhalter ist ausdrücklich erlaubt.
- Keine Änderungen an `VideoPlayer.Mvu`/`VideoPlayer.Storage`/`VideoPlayer.ApiClient` nötig — der Datenpfad ist vollständig vorhanden.
- Kompiliernachweis `net10.0-tvos` läuft ausschließlich über den macOS-CI-Job (nach Push).

## Offene Punkte

(keine)
