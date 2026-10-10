# Bestandsaufnahme Logik: Pages und MVU-Host

## `MvuHost`

Datei: `VideoPlayer.Mvu/MvuHost.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `DispatchAsync(AppMessage, CancellationToken)` | public | Wendet `AppUpdate.Update` an (serialisiert per `SemaphoreSlim gate`), setzt `Model`, feuert `PropertyChanged` + `ModelChanged` und führt Effekte über `IAppEffectHandler` aus |
| `Model` | public get | Unveränderlicher `AppModel`-Snapshot |

Publizierte Events: `ModelChanged` (nach `gate.Release()` — feuert auf dem Dispatch-Thread), `PropertyChanged`.

## Betroffene Seiten (Abo im Konstruktor, keine Abmeldung)

### `HomePage`

Datei: `VideoPlayer.Maui/Pages/HomePage.xaml.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `HomePage(MvuHost)` | public ctor | `BindingContext = mvuHost`; `ModelChanged += OnModelChanged` (Z. 24); `SizeChanged += OnSizeChanged`; initial `UpdateBanners` + `UpdateHero` |
| `OnModelChanged` | private | Marshallt `UpdateBanners(model)` + `UpdateHero(model)` via `MainThread.BeginInvokeOnMainThread` |
| `UpdateBanners` | private | Sichtbarkeit `OfflineBanner`/`StorageNoticeBanner` |
| `OnSizeChanged` | private | Responsive Hero-Höhe |
| `UpdateHero` | private | Hero-`ImageSource` aus Bytes (Referenzvergleich `appliedHeroBytes`) |
| `OnMediaItemTapped` | private | Dispatch `MediaItemSelectedMessage` |

Abonnierte Events: `MvuHost.ModelChanged` (Konstruktor, ohne `-=`), `SizeChanged` (eigenes, Lebensdauer = Seite).
Kein `OnAppearing`-/`OnDisappearing`-Override vorhanden.

### `MediaDetailsPage`

Datei: `VideoPlayer.Maui/Pages/MediaDetailsPage.xaml.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `MediaDetailsPage(MvuHost)` | public ctor | `BindingContext`; `ModelChanged += OnModelChanged` (Z. 38); `SizeChanged += OnSizeChanged`; initial `UpdateUi` |
| `OnAppearing` | protected override | `requested`-Guard: einmalig `MediaDetailsRequestedMessage` dispatchen |
| `OnModelChanged` | private | Marshallt `UpdateUi(model)` |
| `UpdateUi` | private | Identitätsfilter (`isRequested`), Kopfbereich, Fehler-/Download-Panels, Staffel-/Kollektionsabschnitte |
| `UpdateHeaderImage` | private | Header-`ImageSource` (Referenzvergleich `appliedHeaderBytes`) |
| `OnSizeChanged` | private | Responsive Header-Höhe |
| `UpdateSeasonSection`/`UpdateCollectionSection` | private | Picker-/Listenbefüllung mit `updatingSeasonPicker`-Guard |
| `OnSeasonSelected`, `OnEpisodeSelected`, `OnCollectionMovieSelected`, `OnFavoriteClicked`, `OnPlayClicked`, `OnEpisodePlayClicked`, `OnDownloadClicked`, `OnEpisodeDownloadClicked` | private | Dispatchen der jeweiligen `AppMessage`s |
| `FindEpisodeNumbers` | private static | Staffel-/Episodennummer-Suche |

Abonnierte Events: `MvuHost.ModelChanged` (Konstruktor, ohne `-=`), `SizeChanged`.
`OnDisappearing` fehlt. `QueryProperty`-Setter für `mediaType`/`id`/`title`/`show`/`season`/`episode`.

### `DownloadsPage`

Datei: `VideoPlayer.Maui/Pages/DownloadsPage.xaml.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `DownloadsPage(MvuHost)` | public ctor | `BindingContext`; stabile `ObservableCollection<DownloadItemViewModel> downloadItems` als `ItemsSource`; `ModelChanged += OnModelChanged` (Z. 32); initial `UpdateUi` |
| `OnAppearing` | protected override | Dispatch `DownloadsPageOpenedMessage` |
| `OnModelChanged` | private | Marshallt `UpdateUi(model)` |
| `UpdateUi` | private | `DownloadListSync.Sync`, `OfflineBanner`, `QuotaLabel`, `ErrorPanel`, `EmptyLabel` |
| `OnCancelClicked`, `OnRetryDownloadClicked`, `OnDeleteClicked`, `OnRetryClicked` | private | Dispatchen der Download-Nachrichten |

Abonnierte Events: `MvuHost.ModelChanged` (Konstruktor, ohne `-=`).
`OnDisappearing` fehlt.

### `FavoritesPage`

Datei: `VideoPlayer.Maui/Pages/FavoritesPage.xaml.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `FavoritesPage(MvuHost)` | public ctor | `BindingContext`; `ModelChanged += OnModelChanged` (Z. 25); initial `UpdateUi` |
| `OnAppearing` | protected override | Dispatch `FavoritesPageOpenedMessage` |
| `OnModelChanged` | private | Marshallt `UpdateUi(model)` |
| `UpdateUi` | private | `OfflineBanner`, `ErrorPanel`, `EmptyLabel` |
| `OnFavoriteSelected`, `OnRemoveClicked`, `OnRetryClicked` | private | Dispatchen der Favoriten-Nachrichten |

Abonnierte Events: `MvuHost.ModelChanged` (Konstruktor, ohne `-=`).
`OnDisappearing` fehlt.

### `SourceOverviewPage`

Datei: `VideoPlayer.Maui/Pages/SourceOverviewPage.xaml.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `SourceOverviewPage(MvuHost)` | public ctor | `BindingContext`; `ModelChanged += OnModelChanged` (Z. 27); initial `UpdateUi` |
| `OnAppearing` | protected override | Bedingt `SourceBrowserOpenedMessage` (neue Quelle oder noch nicht geladen) |
| `OnModelChanged` | private | Marshallt `UpdateUi(model)` |
| `UpdateUi` | private | Fehler-/Leer-Panels, `MediaSearchBar`-Rückspiegelung |
| `OnGenreSelected`, `OnSearchButtonPressed`, `OnMediaItemSelected`, `OnRemainingItemsThresholdReached`, `OnRetryClicked` | private | Dispatchen der Browser-Nachrichten |

Abonnierte Events: `MvuHost.ModelChanged` (Konstruktor, ohne `-=`).
`OnDisappearing` fehlt. `QueryProperty`-Setter für `id`/`name`.

### `ProfilePage`

Datei: `VideoPlayer.Maui/Pages/ProfilePage.xaml.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `ProfilePage(MvuHost)` | public ctor | `BindingContext`; `ModelChanged += OnModelChanged` (Z. 24); initial `UpdateUi` |
| `OnAppearing` | protected override | Dispatch `ProfilePageOpenedMessage` |
| `OnModelChanged` | private | Marshallt `UpdateUi(model)` |
| `UpdateUi` | private | `ProfileOfflineBanner`, `UserInfoBorder` |
| `OnLogoutClicked` | private | Dispatch `LogoutRequestedMessage` |

Abonnierte Events: `MvuHost.ModelChanged` (Konstruktor, ohne `-=`).
`OnDisappearing` fehlt.

## Referenzimplementierungen (bereits korrekt)

### `PlayerPage`

Datei: `VideoPlayer.Maui/Pages/PlayerPage.xaml.cs`

- `subscribed`-Feld (`volatile bool`): `true` in `OnAppearing`, `false` in `OnDisappearing`.
- `OnAppearing`: `mvuHost.ModelChanged -= OnModelChanged;` dann `+=`, danach `PlayerPageOpenedMessage`-Dispatch, `UpdateUi`, `StartProgressTimer`.
- `OnDisappearing`: erst `subscribed = false` + `ModelChanged -=`, dann `RestoreAsync` + `PlayerClosedMessage`-Dispatch.
- `OnModelChanged` und alle Media-Event-Handler prüfen `subscribed` vor Elementzugriff/Dispatch.
- Konstruktor enthält kein `ModelChanged +=` (nur `UpdateUi(mvuHost.Model)`).

### `SettingsPage`

Datei: `VideoPlayer.Maui/Pages/SettingsPage.xaml.cs`

- `OnAppearing`: `ModelChanged -=`/`+=`, danach `PopulateFromModel`.
- `OnDisappearing`: `ModelChanged -=`.
- Kein `subscribed`-Feld (kein Marshaling — `OnModelChanged` ruft `PopulateFromModel` direkt).

### `LoginPage` / `ServerSetupPage`

Dateien: `VideoPlayer.Maui/Pages/LoginPage.xaml.cs`, `VideoPlayer.Maui/Pages/ServerSetupPage.xaml.cs`

- Beide: `OnAppearing` mit `-=` vor `+=`, `OnDisappearing` mit `-=`; `OnModelChanged` marshallt `UpdateUi` via `MainThread.BeginInvokeOnMainThread`. Kein `subscribed`-Guard.

## Nicht betroffene Abonnenten

### `AppShell`

Datei: `VideoPlayer.Maui/AppShell.xaml.cs`

- `mvuHost.ModelChanged += OnModelChanged` im Konstruktor (Z. 84) ohne Abmeldung — die Shell lebt für die gesamte App-Laufzeit (eine Instanz), daher kein Leck. Nicht Teil der Anforderung.

### `LoadingPage`

- Kein Abo.
