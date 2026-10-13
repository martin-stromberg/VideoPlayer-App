# UI — Quellen-Übersicht und Vergleichsmuster

## `SourceOverviewPage` (VideoPlayer.Maui)

Dateien: `VideoPlayer.Maui/Pages/SourceOverviewPage.xaml` (+ `.xaml.cs`)

- Grid: `SearchBar` (`MediaSearchBar`, AutomationId), Genre-`CollectionView`
  (`GenresCollection`, `IsVisible` an `ShowGenreFilter`), `ActivityIndicator`
  (`InitialLoadingIndicator` an `IsLoading`), `ErrorPanel` + `EmptyLabel`,
  Medien-`CollectionView` (`ItemsCollection`, AutomationId).
- `ItemsCollection`: `ItemsSource="{Binding Model.SourceBrowser.Items}"` —
  **direktes Binding auf die unveränderliche Modell-Liste; jede neue
  Listeninstanz = Referenzwechsel = Ansichts-Reset** (Ursache des Sprungs).
  `GridItemsLayout` vertikal, `Span="3"`, `RemainingItemsThreshold="6"`,
  `RemainingItemsThresholdReached="OnRemainingItemsThresholdReached"`.
  `DataTemplate` mit `x:DataType="mvu:SourceMediaItem"` (Posterkarte +
  Titel-Label). Footer: `FooterErrorPanel` + `FooterLoadingIndicator` (an `IsLoadingMore`).
- `.xaml.cs`:
  - `UpdateUi(AppModel)` — Sichtbarkeiten (`ErrorPanel`, `FooterErrorPanel`,
    `EmptyLabel`, `ItemsCollection.IsVisible = !ShowEmpty`) + Suchfeld-Spiegelung
    (`mirroredSearchText`, Schritt 2 — unbestätigte Eingabe übersteht Ticks).
  - `OnRemainingItemsThresholdReached` → `SourceItemsNextPageRequestedMessage`.
  - `OnGenreSelected` → `SourceGenreSelectedMessage`; `OnSearchButtonPressed` →
    `SourceSearchSubmittedMessage`; `OnMediaItemSelected` →
    `SourceMediaItemSelectedMessage`; `OnRetryClicked` → `SourceItemsRetryRequestedMessage`.
  - `OnAppearing`/`OnDisappearing` — `ModelChanged`-Abo nur während Sichtbarkeit
    (Muster: SettingsPage); `UpdateUi` läuft via `MainThread.BeginInvokeOnMainThread`.
  - `ItemsCollection.ItemsSource` wird nirgends im Code-Behind gesetzt — kommt
    ausschließlich aus dem XAML-Binding.

## `DownloadsPage` (VideoPlayer.Maui) — Präzedenzmuster

Dateien: `VideoPlayer.Maui/Pages/DownloadsPage.xaml` (+ `.xaml.cs`)

- Seiten-eigene `readonly ObservableCollection<DownloadItemViewModel> downloadItems`
  wird **einmal** im Konstruktor als `ItemsSource` gesetzt (kein XAML-Binding
  auf die Modell-Liste); `UpdateUi` ruft `DownloadListSync.Sync(downloadItems,
  model.Downloads.Items)` — Begründung im Kommentar: „Stabile ItemsSource …
  statt den CollectionView pro Tick über eine neue Listeninstanz zurückzusetzen
  (Flackern)".

## `PlaylistDetailsPage` (VideoPlayer.Maui) — gleiche Fehleranfälligkeit (außerhalb des Umfangs)

`PlaylistDetailsPage.xaml` Z. 121–122: `RemainingItemsThreshold="4"` +
`RemainingItemsThresholdReached` — gleicher Mechanismus; **nicht** Teil dieser
Anforderung (Quellen-Übersicht), nur als Seiteneffekt vermerkt.

## Altbestand `VideoWebPlayer.Maui` (nicht betroffen)

`ViewModels/SourceOverviewViewModel.cs`: `ObservableCollection<MediaItemViewModel> Items`
mit `Items.Add(mediaItem)` pro Eintrag (Z. ~227) — bereits inkrementell.
Separate App-Variante ohne MVU; die Anforderung betrifft den MVU-Pfad.

## DevApi-/E2E-Relevanz

- `DevApiModelSnapshot.SourceBrowserItemTitles` (`VideoPlayer.Mvu/DevApi/DevApiModelSnapshot.cs`)
  spiegelt `model.SourceBrowser.Items.Select(Title)` — E2E-Nachweis des
  Nachladens über den Modell-Snapshot.
- `DevApiVisualTree` (msTools.DeveloperApi.Maui): `GET /tree` liefert je
  `CollectionView` die `Items` aus der `ItemsSource`; `POST /invoke` mit
  `itemIndex` setzt `SelectedItem` (Tap-Äquivalent); Gesten `"tap"`/`"longpress"`.
  Kein Scroll-Trigger und keine Scroll-Positions-Abfrage vorhanden —
  Listeneinträge fehlen unter Windows im UIA-Baum (dokumentierte UIA-Lücke).
- `UiDriver` (FlaUI): `FindByAutomationId`, `WaitForVisible` (mit
  `ScrollItemPattern.ScrollIntoView`), keine Scroll-Positions-Hilfe für
  `CollectionView`-ScrollViewer (UIA `ScrollPattern` wäre der Ansatzpunkt).
