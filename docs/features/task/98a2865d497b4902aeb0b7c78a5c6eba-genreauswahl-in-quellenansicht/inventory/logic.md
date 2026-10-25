# Logik

## `SourceOverviewPage` (Code-Behind)
Datei: `VideoPlayer.Maui/Pages/SourceOverviewPage.xaml.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `SourceOverviewPage(MvuHost)` | public | `BindingContext = mvuHost`; `ItemsCollection.ItemsSource = browserItems` (stabile `ObservableCollection<SourceMediaItem>`) |
| `OnAppearing` | protected override | `ModelChanged`-Abo (nur sichtbar); dispatcht `SourceBrowserOpenedMessage` bei neuer/erstmaliger Quelle |
| `OnDisappearing` | protected override | Entfernt das `ModelChanged`-Abo |
| `UpdateUi(AppModel)` | private | `SourceItemListSync.Sync`, Sichtbarkeiten (`ErrorPanel`, `FooterErrorPanel`, `EmptyLabel`, `ItemsCollection`), Spiegeln des abgeschickten Suchtexts |
| `OnGenreSelected` | private | `SelectionChanged`-Handler der `GenresCollection`: dispatcht `SourceGenreSelectedMessage(genre.Id)`, setzt `SelectedItem = null` |
| `OnSearchButtonPressed` | private | Dispatcht `SourceSearchSubmittedMessage(MediaSearchBar.Text)` |
| `OnMediaItemSelected` | private | Dispatcht `SourceMediaItemSelectedMessage(item)` |
| `OnRemainingItemsThresholdReached` | private | Dispatcht `SourceItemsNextPageRequestedMessage` |
| `OnRetryClicked` | private | Dispatcht `SourceItemsRetryRequestedMessage` |

Abonnierte Events: `mvuHost.ModelChanged` (nur solange sichtbar).
Publizierte Nachrichten: `SourceBrowserOpenedMessage`, `SourceGenreSelectedMessage`, `SourceSearchSubmittedMessage`, `SourceItemsNextPageRequestedMessage`, `SourceItemsRetryRequestedMessage`, `SourceMediaItemSelectedMessage`.

## `AppUpdate` (Auszug, Quellen-Browser)
Datei: `VideoPlayer.Mvu/AppUpdate.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `UpdateSourceBrowserOpened` | private static | Neue Quelle → `NewBrowser` + `LoadSourceGenresEffect` + `LoadSourceItemsEffect` (Seite 0); Rückkehr auf gleiche Quelle → unverändert |
| `UpdateSourceGenresLoaded` | private static | Übernimmt `Genres` mit `IsSelected`-Mapping; verwirft Antworten verlassener Quellen |
| `UpdateSourceGenreSelected` | private static | `SelectedGenreId` setzen, `IsSelected` neu mappen → `ReloadBrowserItems` (Liste zurücksetzen, ab Seite 0 mit `genreId`-Filter laden) |
| `ReloadBrowserItems` | private static | Liste leeren, `NextPage = 0`, `LoadSourceItemsEffect` mit Suchtext + Genre-Filter |
| `UpdateSourceItemsLoaded` | private static | Ersetz-/Anhänge-Logik, Fehler- und Leer-Zustände |
| `UpdateSourceSearchSubmitted` / `UpdateSourceItemsNextPageRequested` / `UpdateSourceItemsRetryRequested` | private static | Suche / Nachladen / Wiederholen |

## `SourceBrowserService`
Datei: `VideoPlayer.Mvu/SourceBrowserService.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `LoadSourcesAsync` | public | `api/Sources` → `SourceItem`-Liste |
| `LoadGenresAsync` | public | `api/SourceGenres/{id}`; stellt „Alle" (`Id` = 0, `IsSelected` = true) voran |
| `LoadItemsAsync` | public | `api/items` seitenweise (PageSize 20) mit `search`-/`genreId`-Parametern |
| `MapSummaryType` | internal static | Server-Typbezeichner → `moviecollection`/`tvshow`/`tvshowseason`/`tvshowepisode` |

## Developer-API (msTools.DeveloperApi.Maui)
Dateien: `msTools.DeveloperApi.Maui/DevApiVisualTree.cs`, `MauiDevApiService.cs`, `DevApiTreeNode.cs`

| Methode | Kurzbeschreibung |
|---------|------------------|
| `BuildTree(Element)` | Rekursiver Elementbaum für `GET /tree` — inkl. `Bounds` je `VisualElement` |
| `FindByAutomationId` | AutomationId-Lookup im Baum |
| `TryInvoke` | Clicked-Äquivalent (Button, Switch, CheckBox, MenuItem, Command, Tap-Geste mit Command) |
| `TrySelectItem(element, itemIndex, gesture)` | `CollectionView` → `SelectedItem` setzen (feuert `SelectionChanged`); sonst `BindableLayout`-Container im Teilbaum → `TapGestureRecognizer.SendTapped` des indexierten Eintrags; `gesture="longpress"` → `SendItemLongPressed`-Konvention |
| `ItemsOf` | `Items`-Ableitung für `CollectionView` **und** BindableLayout-Container |

Konsequenz: Die Genre-Leiste bleibt über `POST /invoke` (`InvokeAsync("GenresCollection", index)`) steuerbar, wenn sie von `CollectionView` auf einen `BindableLayout`-Container umgebaut wird — Voraussetzung: AutomationId am Container (oder einem Vorfahren) und `TapGestureRecognizer` im Eintragstemplate.

## `MediaCarousel` (Code-Behind)
Datei: `VideoPlayer.Maui/Controls/MediaCarousel.xaml.cs`

`OnItemTapped` (Zeile 115): `sender` ist das getippte Element; `element.BindingContext is HomeMediaItem item` → `ItemTapped`-Event. Dasselbe Muster steht für Genre-Chips zur Verfügung (`SourceGenreItem` statt `HomeMediaItem`).
