# Plan-Review

Geprüft: `plan.md` gegen die Implementierung (Selbstprüfung ohne Unteragenten —
dokumentierte Abweichung, es stehen keine Unteragenten zur Verfügung).
Alle Planelemente wurden im Code gesucht und vollständig gelesen.

## Ergebnis

**Status:** Vollständig umgesetzt

## Umgesetzte Planelemente

### Projekt `VideoPlayer.Mvu` (net10.0)

- [x] `SourceItem` — `Id`, `Name`, `IconPictureId`, `HasIcon`.
- [x] `SourcesOverviewState` — `IsLoading`, `Attempted`, `Error`,
  `Sources`, `HasSources`/`ShowEmpty`, `Initial`-Singleton.
- [x] `SourceGenreItem` — `Id` (0 = „Alle"), `Name`, `IsSelected`.
- [x] `SourceMediaItem` — `Id`, `Title`, `MediaType` (normalisiert),
  `PosterPictureId`, `HasPoster`.
- [x] `SourceBrowserState` — `SourceId`, `SourceName`, `Genres`,
  `SelectedGenreId`, `SearchText`, `Items`, `NextPage`, `HasMore`,
  `IsLoading`, `IsLoadingMore`, `Attempted`, `Error`, `IsActive`,
  `HasItems`, `ShowEmpty`, `const PageSize = 20`, `Empty`-Singleton.
- [x] `MediaDetailsView` + `SeasonItem`, `EpisodeItem`,
  `CollectionMovieItem` — alle geplanten Eigenschaften inkl.
  `FavoriteStarText`, `MediaTypeText`, `YearGenreLine`, `SelectedSeason`,
  `SelectedSeasonEpisodes`, `IsFavorite` auf allen Ebenen.
- [x] `MediaDetailsState` — `MediaType`, `MediaId`, `IsLoading`,
  `Attempted`, `Error`, `IsFavoriteUpdating`, `FavoriteError`,
  `Details`, `Empty`-Singleton.
- [x] `MediaTypeNames` — `Normalize` (Server-/CLR-Konventionen auf
  CLR-Konvention).
- [x] `SourceBrowserService` — `LoadSourcesAsync`, `LoadGenresAsync`
  („Alle" voran), `LoadItemsAsync` (Suche/Genre/Seitengröße 20,
  `HasMore` bei voller Seite); `MapSummaryType` spiegelt das
  Altbestand-Mapping.
- [x] `MediaDetailsService` — `LoadAsync` je Medientyp inkl.
  Kollektions-404-Fallback auf `movie`, Serienkontext über
  `show`-Hint oder `Season.Show`, Basisdetails-Fallback für Episoden,
  Fehler für Staffeln ohne Serie.
- [x] `FavoritesService` — `ToggleAsync` (typkorrekter `MediaEntry`,
  danach `SyncFavoritesAsync`); `CreateEntry`-Mapping.
- [x] Messages — alle geplanten (`SourcesPageOpened` …
  `FavoriteToggleCompleted`); `SourceItemsLoadedMessage` trägt
  zusätzlich `Page`/`HasMore` (für Stale-Guard und Paging-Fortschritt).
- [x] Effekte — alle geplanten; `NavigateToMediaDetailsEffect` um
  `ShowId`/`SeasonId`/`EpisodeId` erweitert (kompatibel).
- [x] `AppModel` — `SourcesOverview`, `SourceBrowser`, `MediaDetails`.
- [x] `AppUpdate` — alle Cases gemäß „Programmabläufe": Offline-Reset
  ohne Effekt, Dedup bei gleichem Ziel, Stale-Guards per
  `SourceId`/`MediaType`+`MediaId`, Append-Fehler lassen Liste stehen,
  Filter/Suche setzen Liste zurück, Staffelwahl rein im Model,
  Favoriten-Guards.
- [x] `HomeMediaItem`/`HomeListService` — Serienkontext-Hints
  (`ShowId`/`SeasonId`/`EpisodeId`) aus dem eingebetteten Graphen.

### Projekt `VideoPlayer.Maui`

- [x] `SourcesPage` — Quellenliste (Icon + Name), Offline-Banner
  (gebunden an `Model.IsOffline`), Lade-/Fehler-/Leerzustände,
  „Erneut laden"; `OnAppearing` → `SourcesPageOpenedMessage`.
- [x] `SourceOverviewPage` (Route `sourceoverview?id=…&name=…`) —
  `SearchBar`, horizontale Genre-Leiste mit „Alle" und
  Auswahl-`DataTrigger`, Medien-Raster (`GridItemsLayout` Span 3),
  `RemainingItemsThreshold`-Nachladen, Fehler-Retry (Erstladung und
  Folgeseiten-Footer), Leerzustand.
- [x] `MediaDetailsPage` — Kopfbereich (Bild, Titel, Typ/Jahr, Genres,
  Favoriten-Stern `★`/`☆`), Handlung, Serienansicht (`Picker` +
  Episodenliste mit Bildern/Plots), Kollektionsansicht (Filmraster
  mit Tap-Navigation zum Film), Episoden-Tap navigiert im
  Serienkontext; Lade-/Fehlerzustände.
- [x] `CachedSourceIconImage` + `PosterImageService.GetSourceIconSourceAsync`
  (`{id}.sourceicon` im nicht backup-pflichtigen `PreviewsDirectory`,
  eigener Memory-Cache).
- [x] `ShellAppEffectHandler` — alle neuen Effekte;
  `ToggleFavoriteEffect` dispatcht danach `CachedListsChangedMessage`
  (Startseiten-Favoriten folgen automatisch); Detail-Route trägt die
  Serienkontext-Parameter.
- [x] `AppShell` — Route `sourceoverview` registriert.
- [x] `MauiProgram` — `SourceBrowserService`, `MediaDetailsService`,
  `FavoritesService`, `SourceOverviewPage` registriert.

### Tests

- [x] `AppUpdateTests_Sources` (18 Tests) — Öffnen online/offline/
  nicht verbunden/ladend, Quellenwahl-Effekte, Browser-Wiederherstellung,
  Stale-Genre-Guard, Genre-/Such-Reload, Paging (Erfolg, Stale-Append,
  Append-Fehler, Retry beider Arten), Eintragsauswahl.
- [x] `AppUpdateTests_MediaDetails` (11 Tests) — Detail-Anfrage mit
  Hints, ungültige Identität, Dedup, geladen/Fehler/Stale, Staffelwahl,
  Favoriten-Toggle (Erfolg, Fehlschlag, fremdes Medium).
- [x] `SourceBrowserServiceTests` (7 Tests) — Quellenliste = Server-API
  (inkl. Icon-Ids), „Alle"-Eintrag, Query-Parameter der Eintragsseite,
  `HasMore`-Logik, Typ-Mapping.
- [x] `MediaDetailsServiceTests` (11 Tests) — Film, Kollektion
  (Sortierung, 404-Fallback), Serie mit Staffeln/Episoden, Episode mit
  `show`-Hint, Auflösung über eingebettete `Season.Show`,
  Basisdetails ohne Serie, Staffel ohne Hint → Fehler, unbekannter Typ.
- [x] `FavoritesServiceTests` (4 Tests + Theory) — Toggle-Request +
  Cache-Erneuerung aus `api/favorites`, Entfernen leert die Kopie,
  offline → `null` ohne Requests, `CreateEntry`-Typzuordnung.
- [x] `SourcesContractTests` (9 Tests) — XAML-Verdrahtung aller drei
  Seiten, Route-Registrierung, Effekt-Handler, DI-Registrierungen.

## Abweichungen / Verfeinerungen zum Plan

- `SourceItemsLoadedMessage` trägt zusätzlich `Page` und `HasMore`
  (im Plan implizit über das Service-Ergebnis) — nötig für den
  Stale-Append-Guard und den `NextPage`-Fortschritt; plan-konforme
  Verfeinerung.
- `CollectionMovieItem.IsFavorite` wurde nachträglich ergänzt
  (im Plan gelistet, erstes Mapping ausgelassen — nachgezogen).
- UI-Automatisierung: weiterhin nicht verfügbar (kein Framework im
  Repository) — Ersatz gemäß Plan über MVU-Message-Sequenz-Tests und
  XAML-Contract-Tests; dokumentiert in `test-results.md`.

## Fazit

Alle Planelemente sind im Code vorhanden und durch Tests abgedeckt;
die beiden Verfeinerungen sind plan-konform und dokumentiert.
