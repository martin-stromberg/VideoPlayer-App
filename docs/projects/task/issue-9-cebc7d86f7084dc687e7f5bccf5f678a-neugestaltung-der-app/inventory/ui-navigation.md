# UI / Navigation (`VideoWebPlayer.Maui`)

**Architektur:** Der Bestandscode folgt **MVVM** (`ViewModels/` mit `INotifyPropertyChanged`, `ObservableCollection`, Commands), nicht MVU. Kein MVU-Framework (`Fabulous`, `Comet` o. ä.) referenziert.

## Shell / Navigation

`VideoWebPlayer.Maui/AppShell.xaml` + `AppShell.xaml.cs`:

- `Shell` mit `FlyoutBehavior="Flyout"` (Hamburgermenü vorhanden).
- Ein einziges `FlyoutItem` „Home" → Route `MainPage`; ein `MenuItem` „Einstellungen" → `OnShellSettingsClicked` pusht `SettingsPage`.
- `AppShell`-Konstruktor lädt nach ~200 ms Wartezeit via `RequestSourcesAsync()` die Quellen und hängt jede Quelle als `MenuItem` ins Flyout (Klick → `SourceOverviewPage`).
- Route `"home"` → `HomePage` per `Routing.RegisterRoute`.
- Nicht vorhanden als Menüeinträge/Seiten: „Quellen"-Übersichtsseite (nur dynamische Quellen-Items), „Favoriten"-Seite, „Downloads"-Seite, „Profil"-Seite.

## Pages

| Page | Datei(en) | Zweck / Inhalt |
|------|-----------|----------------|
| `MainPage` | `MainPage.xaml(.cs)` | Einstieg (Route `MainPage`); `RunStartupWorkflowAsync` orchestriert `IConnectionService.StartConnectionWorkflowAsync` → `ServerSetupPage`/`LoginPage` modal bzw. Offline-`HomePage`; abonniert `NotificationEvent`s für UI-Refresh; `RetryStartupWorkflow()` für erneuten Versuch |
| `HomePage` | `HomePage.xaml(.cs)` | Startseite mit `PageChrome`, 4× `MediaCarousel` (Weiterschauen, Favoriten, Neu im Programm, Heruntergeladene Videos), Quellenliste, Offline-Banner/Reconnect |
| `LoadingPage` | `LoadingPage.xaml(.cs)` | Ladeanzeige; Konstruktor nimmt `message` entgegen |
| `LoginPage` | `LoginPage.xaml(.cs)` | Login-Formular, ruft `IAuthService.LoginAsync` |
| `ServerSetupPage` | nur `ServerSetupPage.xaml.cs` (**kein XAML** — UI wird im Code gebaut) | Serveradresse eingeben, „Automatisch suchen" (mDNS + UDP via `ISettingsService`), `DisplayActionSheet` zur Auswahl, `Cancel` beendet App via `Environment.Exit(0)` |
| `SettingsPage` | `SettingsPage.xaml(.cs)` | Nur drei numerische Aufbewahrungs-Einstellungen (Playback-Cache, Weiterschauen, Downloads in Tagen); kein „Server wechseln", keine Download-/Wiedergabe-Optionen |
| `SourceOverviewPage` | `SourceOverviewPage.xaml(.cs)` | Genre-Filter + paginierte Items einer Quelle |
| `TVShowDetailsPage` | `TVShowDetailsPage.xaml(.cs)` | Serien-Details mit `MediaBannerPlayer`, Season-/Episodenwahl, Favoriten-Toggle |
| `MovieCollectionDetailsPage` | `MovieCollectionDetailsPage.xaml(.cs)` | Collection-Details analog |
| `App` | `App.xaml(.cs)` | s. [services.md](services.md) |

## Komponenten (`Components/`)

| Komponente | Datei(en) | Zweck |
|------------|-----------|-------|
| `MediaBannerPlayer` | `MediaBannerPlayer.xaml(.cs)` | Banner + `CommunityToolkit.Maui.MediaElement` (`ShouldAutoPlay`, `ShouldShowPlaybackControls`); `OnPlayTapped` → `IMediaBannerViewModel.GetVideoInfoForPlaybackAsync` → `DownloadManager.RequestVideoAsync`; Positionsmeldung alle 5 s (`System.Timers.Timer`) mit 0,5-s-Epsilon gegen Doppelmeldung via `DownloadManager.UpdatePlaybackPositionAsync`; **kein PiP** |
| `MediaCarousel` | `MediaCarousel.xaml(.cs)` | Horizontale Liste mit Long-Press (`TouchBehavior` aus CommunityToolkit) → `MediaCarouselViewModel.ExecuteLongPress` |
| `NotificationTicker` | `NotificationTicker.xaml(.cs)` | Ticker-Banner; abonniert `DownloadCompleted/Deleted`, `ContinueWatchingUpdated`, `NewVideosScanned`, `StatusChanged` + `DownloadQueue.DownloadProgress` |
| `PageChrome` | nur `PageChrome.xaml.cs` (kein XAML) | Layout-Rahmen: `HeaderTitle`, `HeaderActions`, `Body`, `Footer` als BindableProperties |

## ViewModels (`ViewModels/`)

| ViewModel | Datei | Kern-Members |
|-----------|-------|--------------|
| `HomePageViewModel` | `HomePageViewModel.cs` (750 Z.) | Carousels `ContinueWatching` („Weiterschauen"), `Favorites`, `RecentEntries` („Neu im Programm"), `Downloads` („Heruntergeladene Videos"); `Sources`; `IsLoading`, `IsOfflineMode`, `IsOnline`; `LoadDataAsync` (Cache zuerst via `ElementCacheService`, dann Server), `RefreshDataAsync`, `TryReconnectAsync`, `SetOfflineMode`; lädt Quellen-Icons und Bilder über den Client |
| `MediaCarouselViewModel` | `MediaCarouselViewModel.cs` | `Title`, `Kind` (`CarouselKind`), `Items`, `IsLoading`, `HasItems`; `ItemPressed`-Event + `MediaItemPressedEventArgs`; `AddItem`, `BeginUpdate`/`EndUpdate` (Batch-Update) |
| `MediaItemViewModel` | `MediaItemViewModel.cs` | `Title`, `ImageUrl`, `ImageSource`, `PosterPictureId`, `EntryId`, `MediaType`, `SeasonId`, `EpisodeId`, `IsFromCache`, `DeleteDownloadCommand` |
| `MediaSourceViewModel` | `MediaSourceViewModel.cs` | `Id`, `Name`, `Icon`, `IconPictureId`, `IconImageSource` |
| `SourceOverviewViewModel` | `SourceOverviewViewModel.cs` | `SourceId`, `SourceName`, `Genres`, `Items`, `SelectedGenre`, `IsLoadingMore`; `LoadGenresAsync`, `LoadMoreItemsAsync` (Paging); enthält `GenreViewModel` |
| `TVShowDetailsViewModel` | `TVShowDetailsViewModel.cs` (804 Z.) | `Seasons`, `Episodes`, `SelectedSeason/Episode`, Favoriten-Toggles (Show/Season/Episode), `LoadDataAsync`, `RefreshDownloadStatusAsync`, `FindEpisode`; implementiert `IMediaBannerViewModel` |
| `TVShowEpisodeViewModel` | `TVShowEpisodeViewModel.cs` | `Title`, `Plot`, `EpisodeId`, `EpisodeNumber`, `SeasonNumber`, `IsDownloaded`, `IsFavorite` |
| `MovieCollectionDetailsViewModel` | `MovieCollectionDetailsViewModel.cs` | `Movies`, `SelectedMovie`, Favoriten-Toggle, `LoadDataAsync`; implementiert `IMediaBannerViewModel` |

## Converters (`Converters/`)

`InvertedBoolConverter`, `IsNotNullConverter`, `IsNullConverter` (`IValueConverter`).

## Plattformordner (`Platforms/`)

- `Android/`: `MainActivity.cs`, `MainApplication.cs`, `AndroidManifest.xml`, `Resources/values/colors.xml`
- `iOS/`: `AppDelegate.cs`, `Program.cs`, `Info.plist`, `Resources/PrivacyInfo.xcprivacy`
- `MacCatalyst/`: `AppDelegate.cs`, `Program.cs`, `Info.plist`, `Entitlements.plist`
- `Windows/`: `App.xaml(.cs)`, `Package.appxmanifest` (Capability `runFullTrust`), `app.manifest`; `WindowsPackageType=None` im csproj (unpackaged)

Keine plattformspezifische Backup-/Speicherort-Logik in den Plattformordnern vorhanden.

## Nicht vorhanden (für die Anforderung relevant)

- MVU-Architektur (Bestand ist MVVM).
- Picture-in-Picture (kein PiP-Code; `MediaElement` nur in `MediaBannerPlayer` eingebettet).
- Dedizierte Seiten für „Favoriten", „Downloads", „Profil", Quellen-Übersichtsseite als Menüpunkt.
- „Server wechseln"-Funktion und Multi-Server-Verwaltung in `SettingsPage`.
- Google-Stitch-Designvorgabe liegt nicht im Repository (kein Design-Asset/Styleguide gefunden; `Resources/Styles/Colors.xaml`, `Styles.xaml` sind MAUI-Template-Styles).
