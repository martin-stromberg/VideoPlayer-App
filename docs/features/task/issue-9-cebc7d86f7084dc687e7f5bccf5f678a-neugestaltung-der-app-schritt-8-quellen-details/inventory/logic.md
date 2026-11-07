# Logik-Bestand — MVU, Dienste, UI (Schritt 8)

## MVU-Kern (`VideoPlayer.Mvu`)

| Baustein | Stand | Erweiterung Schritt 8 |
|----------|-------|------------------------|
| `AppModel` | record, `with`-Updates; Startseiten-Listen + `IsOffline`/`IsOnline` | neue Zustands-Records: Quellenliste, Quellen-Browser, Medien-Details |
| `AppMessage` | records; u. a. `MediaItemSelectedMessage`, `CachedListsChangedMessage`, `ServerEventReceivedMessage` | Messages für Quellen-Laden, Browser (Genre/Suche/Paging), Details, Favoriten-Toggle |
| `AppEffect` | records; u. a. `LoadHomeListsEffect`, `NavigateToMediaDetailsEffect(MediaType, MediaId, Title)` | Effekte für Quellen-/Genre-/Item-Laden, Detail-Laden, Favoriten-Toggle, Quellen-Overview-Navigation |
| `AppUpdate.Update` | switch auf Messages; Validierung in `UpdateMediaItemSelected` (`Id > 0`, `MediaType` nicht leer) | neue `Update…`-Methoden; Zustandswechsel rein |
| `MvuHost` | serialisierte Dispatchs (`SemaphoreSlim`), `Model` + `ModelChanged`, `INotifyPropertyChanged` für `{Binding Model.*}` | unverändert — Seiten binden wie `HomePage` |
| `HomeListService` | liest `cached_lists`/`cached_favorites` → `HomeLists` | unverändert; wird nach Favoriten-Toggle indirekt neu gelesen |
| `ServerSyncService` | `SyncFavoritesAsync` ersetzt `cached_favorites`; `MediaTypeOf` = CLR-Name kleingeschrieben | unverändert — neue `FavoritesService` nutzt dieselbe Schlüsselkonvention |
| `AuthSessionService` | `Client` (angemeldeter `VideoPlayerApiClient` oder `null`), `ClientBaseAddress`, `SessionExpired`-Event | Basis der neuen Lade-/Favoriten-Dienste |
| `INetworkStatusMonitor` | `IsOnline`, `AvailabilityChanged`, `Start()` | `IsOffline` im Model steuert die Quellen-Seite |

## App-Rand (`VideoPlayer.Maui`)

| Baustein | Stand | Erweiterung Schritt 8 |
|----------|-------|------------------------|
| `ShellAppEffectHandler` | `HandleAsync`-Switch auf `AppEffect`; Ergebnisse via `DispatchMessageAsync` (MainThread → `MvuHost.DispatchAsync`); Fehler → Debug-Log + Fehler-Message | neue Cases: Quellen, Genres, Eintragsseite, Details, Favoriten-Toggle, Quellen-Overview-Navigation |
| `AppShell.xaml(.cs)` | FlyoutItems + `Routing.RegisterRoute` für `serversetup`, `login`, `mediadetails`; `Navigated` → `NavigatedMessage` | Registrierung `sourceoverview` |
| `Pages/SourcesPage` | Platzhalter | Quellenliste + Offline-Hinweis + Lade-/Fehlerzustand |
| `Pages/SourceOverviewPage` | — | neu: Genre-Leiste, Suche, Medien-Raster, `RemainingItemsThreshold`-Nachladen |
| `Pages/MediaDetailsPage` | Platzhalter; `QueryProperty` `mediaType`/`id`/`title` | echte Detailansicht je Medientyp + Favoriten-Stern |
| `Pages/HomePage` | `BindingContext = MvuHost`, `ModelChanged` → UI-Update, `OnMediaItemTapped` → Dispatch | Muster für die neuen Seiten |
| `Controls/CachedPosterImage` | `PictureId`-Property → `PosterImageService` | Wiederverwendung für Poster/Banner; Icon-Variante für `api/sourceicons/{id}` |
| `Services/PosterImageService` | `GetImageSourceAsync` → `previews/{id}.poster` | Erweiterung um `GetSourceIconSourceAsync` (`{id}.sourceicon`) |
| `MauiProgram` | Singleton-Dienste + Transient-Seiten | Registrierung `SourceBrowserService`/`MediaDetailsService`/`FavoritesService`/`SourceOverviewPage` |

## Altbestand-Referenz (`VideoWebPlayer.Maui`, nicht veränderbar)

| Baustein | Übernommenes Muster |
|----------|---------------------|
| `SourceOverviewPage`/`SourceOverviewViewModel` | Genre-Leiste mit künstlichem „Alle"-Eintrag (`Id = 0` = kein Filter), `PageSize = 20`, `HasMore` bis Seite < PageSize, Karten 150×~200 mit Titelzeile `#23232a`/`#FFDDAA`, Akzent `#FFB84D`; `Type == "Movie"` → Kollektionsdetails, sonst Seriendetails |
| `TVShowDetailsPage` + `TVShowDetailsViewModel` | Staffel-`Picker` (`ItemDisplayBinding Name`), Episoden-`CollectionView` (Bild 120×80, Titel fett, Plot 3 Zeilen), Favoriten-Stern `★`/`☆` farbcodiert (`#FFB84D` aktiv), Jahr/Genres/Plot-Zeile |
| `MovieCollectionDetailsPage` + `MovieCollectionDetailsViewModel` | Filme-Raster (`GridItemsLayout` Span 2), Filmdetails des gewählten Eintrags, Favoriten-Stern |
| `HomePageViewModel.GetEntryDetails` | Episode/Season → Serienansicht mit Vorwahl; Movie mit Collection → Kollektion |

## Server-Ereignis-Verdrahtung (Schritt 6/7, unverändert)

`ServerEventReceivedMessage` → `SyncContinueWatchingEffect`/
`SyncFavoritesEffect`/`SyncMetadataEffect` → Teil-Sync →
`CachedListsChangedMessage` → `LoadHomeListsEffect` →
`HomeListsLoadedMessage`. Favoriten-Änderungen aus der Detailseite
schließen sich an: nach erfolgreichem Server-Toggle dispatcht der
Effekt-Handler `CachedListsChangedMessage`, sodass die Startseiten-
Favoritenliste ohne manuelle Aktualisierung folgt.
