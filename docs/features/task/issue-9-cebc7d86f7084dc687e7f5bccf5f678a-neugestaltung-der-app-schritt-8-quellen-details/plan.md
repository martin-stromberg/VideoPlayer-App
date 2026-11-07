# Umsetzungsplan: Quellen-Browsing und Medien-Detailseiten

## Übersicht

Die Menüseite „Quellen" (`SourcesPage`, Route `sources`) zeigt die vom
Server freigegebenen Medienbereiche (`GET api/Sources`) als Liste mit
Icons und Namen — im Offline-Zustand erscheint ein Hinweis statt einer
leeren Liste. Die Auswahl einer Quelle öffnet die neue Seite
`SourceOverviewPage` (Shell-Route `sourceoverview`, auf den
Navigationsstack geschoben): Genre-Leiste mit „Alle"-Eintrag,
Textsuche und ein Medien-Raster mit seitenweisem Nachladen
(`api/items`). Die Auswahl eines Eintrags führt über die bestehende
Route `mediadetails` zur ausgebauten `MediaDetailsPage`: Serien zeigen
Staffel-`Picker` und Episodenliste, Filmkollektionen das Filme-Raster,
Filme/Episoden Basisdetails; ein Favoriten-Stern schaltet die Markierung
über `api/favorites/toggle` um und pflegt die lokale Kopie
`cached_favorites` (nicht backup-pflichtig) über den bestehenden
`ServerSyncService.SyncFavoritesAsync`; danach aktualisiert
`CachedListsChangedMessage` die Startseiten-Favoriten.

Der gesamte Zustand liegt in unveränderlichen Records im `AppModel`
(MVU); die Seiten dispatchen Benutzeraktionen als Messages und binden an
`Model.*` — Muster `HomePage` (Schritt 7).

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|------------------|------------|
| Browser-/Detail-Zustand | Drei verschachtelte unveränderliche Records im `AppModel`: `SourcesOverview` (Quellenliste), `SourceBrowser` (geöffnete Quelle), `MediaDetails` (Detailansicht) — jeweils mit `IsLoading`/`Error`/`Attempted` | Ein Zustand je Oberfläche; sauberes Zurücksetzen per Record-Austausch; plattformneutral testbar; konsistent zu den Startseiten-Listen aus Schritt 7. |
| Quellenliste | Live-Abruf `GetSourcesAsync` beim Öffnen der Seite (`SourcesPageOpenedMessage` in `OnAppearing`); kein Lesen des `cached_lists`-Schlüssels `sources` | „Die Quellenliste entspricht den Daten der Server-API" und „im Offlinebetrieb gibt es keine Quellenliste" — ein Cache würde der geforderten Offline-Einschränkung widersprechen. Der Sync-Schlüssel `sources` bleibt für Metadaten bestehen, wird hier nicht gelesen. |
| Offline-Verhalten Quellen | `SourcesPageOpenedMessage` erzeugt im Zustand `IsOffline` keinen Effekt; die Seite bindet den Hinweis an `Model.IsOffline` | Klare Anwenderaussage statt leerer Liste; identisches Muster zum `OfflineBanner` der Startseite. |
| Quellen-Übergang | `SourceSelectedMessage(SourceItem)` initialisiert `SourceBrowser` (Quelle, leere Liste, `NextPage=0`, `IsLoading`) und erzeugt `NavigateToSourceOverviewEffect` + `LoadSourceGenresEffect` + `LoadSourceItemsEffect(Seite 0)` | Muster `NavigateToMediaDetailsEffect`; Route `sourceoverview?id=…&name=…` trägt nur die Identität — der Zustand liegt im Model, nicht in der Seite. |
| Genre-Leiste | `SourceBrowserService.LoadGenresAsync` liefert die Server-Genres mit vorangestelltem Eintrag „Alle" (`Id = 0`); `SelectedGenreId = 0` bedeutet „kein Filter" (`genreId`-Parameter nur bei `> 0`) | Direkte Übernahme des Altbestand-Musters (`SourceOverviewViewModel`, „Alle" mit Id 0). |
| Textsuche | `SearchBar` auf der Übersichtsseite; `SearchButtonPressed` (und Leeren des Texts) dispatcht `SourceSearchSubmittedMessage(text)` → Liste zurücksetzen, Seite 0 neu laden mit `search`-Parameter | Neue Anforderung (Altbestand hatte keine Suche); der Client trägt den Parameter bereits. Gleicher Text → `Unchanged` (kein unnötiger Reload). |
| Seitenweises Nachladen | `SourceBrowserState.PageSize = 20` (Altbestand-Wert); `NextPage` zählt die nächste zu ladende Seite (0-basiert); `HasMore = items.Count == PageSize`; UI nutzt `CollectionView.RemainingItemsThreshold` + `RemainingItemsThresholdReached` → `SourceItemsNextPageRequestedMessage` | `RemainingItemsThreshold` ist das idiomatische MAUI-Paging (ersetzt die ScrollView-Mathematik des Altbestands); Guards (`IsLoading`/`IsLoadingMore`/`!HasMore`) verhindern Doppelläufe. |
| Medientyp-Mapping der Quelleneinträge | `MediaItemSummary.Type` (Groß-/Kleinschreibung tolerant): `movie`/`moviecollection`/`collection` → `moviecollection`; `show`/`tvshow` → `tvshow`; `season`/`tvshowseason` → `tvshowseason`; `episode`/`tvshowepisode` → `tvshowepisode`; alles andere → `tvshow` | Spiegelt das Altbestand-Verhalten (`Type == "Movie"` → Kollektion, sonst Serie — der Server gruppiert Filme in Kollektionen); Einträge mit `Id <= 0` oder leerem `Type` werden wie im `HomeListService` übersprungen. |
| Kollektions-Fallback | `LoadMediaDetailsEffect` mit `moviecollection`: bei `ApiException` mit Status `404` (oder `MovieCollection` leer/`null`-Pfad nicht erreichbar) erneuter Versuch als `movie` (`GetMovieAsync`) | Robustheit gegenüber Quellen, die Einzelfilme ohne Kollektionshülle listen; ohne Mehraufwand in der UI. |
| Detail-Identität | Route `mediadetails` wird um optionale Query-Parameter `show`, `season`, `episode` erweitert; `HomeMediaItem` erhält `ShowId`/`SeasonId`/`EpisodeId` (`long?`), befüllt aus dem eingebetteten Objektgraphen (`TvShowEpisode.Season.Show.Id` usw.) | Episode/Staffel-Einträge aus Weiterschauen/Favoriten öffnen die Serienansicht mit vorgewählter Staffel/Episode — Altbestand-Muster `TVShowDetailsPage(tvShowId, seasonId, episodeId)`; der `ShowId`-Hint spart den Auflösungs-Request und macht Staffeln überhaupt adressierbar (kein Staffel-Endpunkt). |
| Episode/Staffel im Serienkontext | `MediaDetailsService.LoadAsync`: `tvshowepisode` → `ShowId` bekannt? direkt `GetTvShowAsync`; sonst `GetTvShowEpisodeAsync` → `Season.Show.Id` → `GetTvShowAsync`. Gefundene Episode wird vorgewählt/markiert. `tvshowseason` analog über `ShowId`. Ohne auflösbare Serie: Episode als Basisdetails (eigenes Panel), Staffel als Fehlertext („Keine Details verfügbar") | Serienansicht mit Kontext entspricht der Referenz; Basisdetails-Fallback hält die Seite in jedem Fall anzeigbar. |
| Favoriten-Markierung | Stern-`Button` (`★`/`☆`) auf der Detailseite; `FavoriteToggleRequestedMessage` → `ToggleFavoriteEffect(MediaType, MediaId, Title)` → `FavoritesService.ToggleAsync`: typkorrekter `MediaEntry` (`movie`→`Movie`, `moviecollection`/`collection`→`MovieCollection`, `tvshow`/`show`→`TvShow`, `tvshowseason`/`season`→`TvShowSeason`, `tvshowepisode`/`episode`→`TvShowEpisode`, sonst `MediaEntry`), `ToggleFavoriteAsync` → danach `ServerSyncService.SyncFavoritesAsync` (ersetzt `cached_favorites` autoritativ) → Rückdispatch `FavoriteToggleCompletedMessage` + `CachedListsChangedMessage` | Server bleibt führend; die lokale Kopie folgt über denselben getesteten Pfad wie die Ereignis-Synchronisation — keine doppelte Cache-Schreiblogik. Fehlschlag/kein Client → `IsFavorite = null`-Ergebnis, Stern bleibt unverändert, Fehlerhinweis sichtbar (kein stilles Umschalten). |
| Favoriten-Ziel | Der Stern schaltet das **navigierte** Medium (die Identität der Route) — bei Episode/Staffel im Serienkontext also Episode/Staffel, nicht die Serie | Eindeutige, dokumentierte Semantik; deckt „als Favorit markieren und Markierung entfernen" ab. |
| Bilder | `PosterImageService` um `GetSourceIconSourceAsync` erweitert (`api/sourceicons/{id}`, Datei `{id}.sourceicon` im `PreviewsDirectory`); neues Control `CachedSourceIconImage` (Spiegel von `CachedPosterImage`); Poster/Banner weiter über `CachedPosterImage` | Quellen-Icons nutzen einen eigenen Endpunkt; gleiche Cache-Konvention (nicht backup-pflichtig, Fehler → kein Bild). |
| Seiten-Lebenszyklus | `SourcesPage`/`SourceOverviewPage`/`MediaDetailsPage` melden in `OnAppearing` ihre Identität/Absicht (`SourcesPageOpenedMessage`, `SourceBrowserOpenedMessage`, `MediaDetailsRequestedMessage`); `AppUpdate` dedupliziert (bereits passender Zustand → `Unchanged`) | Zurück-Navigation reaktiviert die Seite mit bestehendem Zustand (Listenposition bleibt), Kontextwechsel lädt frisch; keine Effekte aus XAML-Events ohne MVU. |
| Auswahl von Untereinträgen | Tap auf Kollektions-Film oder Episode dispatcht `MediaItemSelectedMessage` mit einem `HomeMediaItem` (inkl. `ShowId`/`SeasonId`/`EpisodeId`-Hints) | Wiederverwendung der bestehenden Validierung und des `NavigateToMediaDetailsEffect` — ein einziger Detail-Navigationspfad. |

## Programmabläufe

### Quellen-Übersicht öffnen (AK 1, Offline-AK)

1. Menüeintrag „Quellen" → Shell navigiert zur `SourcesPage` →
   `OnAppearing` dispatcht `SourcesPageOpenedMessage`.
2. `IsOffline` → kein Effekt; die Seite zeigt den Offline-Hinweis
   („Offline – die Quellenliste steht ohne Verbindung nicht zur
   Verfügung."). Zusätzlich bleibt eine zuvor geladene Liste sichtbar?
   Nein — im Offline-Zustand wird die Liste geleert
   (`SourcesOverview` zurückgesetzt), damit kein veralteter Stand als
   aktuell erscheint.
3. `Connected` → `SourcesOverview { IsLoading = true, Error = null }` +
   `LoadSourcesEffect`; Handler → `SourceBrowserService.LoadSourcesAsync`
   → Rückdispatch `SourcesLoadedMessage(sources, error)`.
4. Erfolg → Liste mit Icon (`CachedSourceIconImage`) + Name; Fehler →
   Fehlertext + „Erneut laden"-Schaltfläche (dispatcht erneut
   `SourcesPageOpenedMessage`); online + leer → „Keine Quellen
   freigegeben." (klarer Leerzustand, kein Fehler).

### Quelle durchblättern (AK 2)

1. Tap auf Quelle → `SourceSelectedMessage` → `SourceBrowser` wird
   initialisiert (`SourceId`, `SourceName`, `SelectedGenreId = 0`,
   `NextPage = 0`, `IsLoading = true`, `HasMore = true`) + Effekte
   `NavigateToSourceOverviewEffect`, `LoadSourceGenresEffect`,
   `LoadSourceItemsEffect(SourceId, 0, null, null, Append: false)`.
2. `SourceOverviewPage` erscheint (Titel = Quellenname); Genres kommen
   als `SourceGenresLoadedMessage` in die Leiste („Alle" voraus,
   `IsSelected` am „Alle"-Eintrag), Einträge als
   `SourceItemsLoadedMessage` ins Raster.
3. Genre-Tap → `SourceGenreSelectedMessage(id)`: Liste leeren,
   `SelectedGenreId`, `NextPage = 0`, `IsLoading` →
   `LoadSourceItemsEffect(0, search, genreId > 0 ? genreId : null)`.
4. Suche abschicken/leeren → `SourceSearchSubmittedMessage` → gleiche
   Rücksetz-Logik mit `SearchText`.
5. Scrollen ans Ende → `RemainingItemsThresholdReached` →
   `SourceItemsNextPageRequestedMessage` → Guards →
   `LoadSourceItemsEffect(NextPage, Append: true)` → Einträge werden
   angehängt; `HasMore` endet bei Teilliste (`< PageSize`).
6. Fehler beim ersten Laden → Fehlertext + „Erneut laden"
   (`SourceItemsRetryRequestedMessage`); Fehler beim Anhängen →
   `IsLoadingMore` zurückgesetzt, `HasMore` bleibt — das nächste
   Scroll-Ereignis versucht die Seite erneut.
7. `SourceBrowserOpenedMessage` in `OnAppearing`: stimmt
   `SourceBrowser.SourceId` bereits → `Unchanged` (Rückkehr aus
   Details ohne Reload); sonst Neu-Initialisierung + Ladeeffekte ohne
   Navigation (Wiederherstellungspfad).

### Detailseite öffnen (AK 3)

1. Auswahl (Startseite, Quellen-Raster, Kollektions-Film, Episode) →
   `NavigateToMediaDetailsEffect` → Shell-Route
   `mediadetails?mediaType=…&id=…&title=…[&show=…&season=…&episode=…]`.
2. `MediaDetailsPage.OnAppearing` → `MediaDetailsRequestedMessage` →
   `MediaDetails { IsLoading = true, … }` + `LoadMediaDetailsEffect`
   (Dedup: identisches Ziel schon ladend/geladen → `Unchanged`).
3. Handler → `MediaDetailsService.LoadAsync` je Medientyp →
   `MediaDetailsLoadedMessage(details, error)` (Stale-Antworten werden
   per `MediaType`/`MediaId`-Vergleich verworfen).
4. Darstellung je Typ: Banner/Poster (`CachedPosterImage`), Titel,
   Jahr/Genre-Zeile, Handlung, Stern-Schaltfläche; `tvshow`-
   Kontext (auch bei `tvshowseason`/`tvshowepisode` mit Serie) →
   `Picker` „Staffel wählen" (`SeasonItem.Name`, Vorwahl
   `SelectedSeasonId`) + Episodenliste (`Number`, Titel, Plot, Bild,
   Markierung der `HighlightedEpisodeId`); `moviecollection` →
   Raster „Filme in dieser Sammlung" mit Tap-Navigation zum Film.
5. `SeasonSelectedMessage` wechselt die Episodenliste rein im Model
   (keine Serveranfrage — Staffeln/Episoden sind eingebettet).

### Favoriten-Markierung (AK 4)

1. Stern-Tap → `FavoriteToggleRequestedMessage` → Guards
   (`Details` vorhanden, `!IsFavoriteUpdating`) →
   `IsFavoriteUpdating = true` + `ToggleFavoriteEffect`.
2. `FavoritesService.ToggleAsync`: ohne angemeldeten Client →
   `null`-Ergebnis; sonst `ToggleFavoriteAsync(entry)` → bei Erfolg
   `SyncFavoritesAsync` (lokale Kopie = Serverstand).
3. Rückdispatch `FavoriteToggleCompletedMessage(isFavorite)` → Stern
   übernimmt den Server-Zustand bzw. bleibt bei Fehler unverändert +
   `FavoriteError`-Hinweis; danach `CachedListsChangedMessage` →
   `LoadHomeListsEffect` → Startseiten-Favoriten aktualisiert.
4. Konsistenz nach Neustart: Server führt die Markierung
   (`entry.IsFavorite` der Detail- und Listen-Responses), die lokale
   Kopie `cached_favorites` folgt bei Toggle und bei jedem
   (Teil-/Voll-)Sync.

## Neue Klassen

### `VideoPlayer.Mvu`

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `SourceItem` | Record | Quelleneintrag: `Id`, `Name`, `IconPictureId`, `HasIcon` |
| `SourcesOverviewState` | Record | `IsLoading`, `Attempted`, `Error`, `Sources` (`IReadOnlyList<SourceItem>`), `HasSources`, `Initial`-Singleton |
| `SourceGenreItem` | Record | Genre-Eintrag: `Id` (0 = „Alle"), `Name`, `IsSelected` |
| `SourceMediaItem` | Record | Medien-Summary: `Id`, `Title`, `MediaType` (normalisiert), `PosterPictureId`, `HasPoster` |
| `SourceBrowserState` | Record | `SourceId`, `SourceName`, `Genres`, `SelectedGenreId`, `SearchText`, `Items`, `NextPage`, `HasMore`, `IsLoading`, `IsLoadingMore`, `Attempted`, `Error`, `IsActive`, `HasItems`, `const PageSize = 20`, `Empty`-Singleton |
| `MediaDetailsView` | Record | Anzeigemodell: `MediaType` (normalisiert), `MediaId`, `Title`, `Plot`, `GenreNames`, `ReleaseYear`, `BannerPictureId`, `PosterPictureId`, `IsFavorite`, `Seasons`, `SelectedSeasonId`, `HighlightedEpisodeId`, `Movies`; berechnet `SelectedSeason`, `SelectedSeasonEpisodes`, `HasSeasons`, `HasMovies`, `HasBanner`, `HasPoster`, `FavoriteStarText`, `MediaTypeText`, `YearGenreLine`/`HasYearGenreLine` |
| `SeasonItem` | Record | `Id`, `Number`, `Name` (Anzeige „Staffel n" bei leerem Namen), `Episodes` |
| `EpisodeItem` | Record | `Id`, `Number`, `Title`, `Plot`, `PosterPictureId`, `IsHighlighted` |
| `CollectionMovieItem` | Record | `Id`, `Title`, `Plot`, `GenreNames`, `ReleaseYear`, `PosterPictureId`, `IsFavorite` |
| `MediaDetailsState` | Record | `MediaType` (angefragt, normalisiert), `MediaId`, `IsLoading`, `Attempted`, `Error`, `IsFavoriteUpdating`, `FavoriteError`, `Details`, `HasDetails`, `Empty`-Singleton |
| `MediaTypeNames` | statische Klasse | `Normalize(string?)` — vereinheitlicht Server-Schreibweisen (`show`/`season`/`episode`/`collection`) und CLR-Namen (`tvshow`/`tvshowseason`/`tvshowepisode`/`moviecollection`) auf die CLR-Konvention |
| `SourceBrowserService` | Klasse | `LoadSourcesAsync` → `SourceItem[]`; `LoadGenresAsync(sourceId)` → `SourceGenreItem[]` inkl. „Alle"; `LoadItemsAsync(sourceId, page, search, genreId)` → `SourceItemsPage` (`Items`, `HasMore`); auf `AuthSessionService.Client` |
| `SourceItemsPage` | Record | `Page`, `Items` (`IReadOnlyList<SourceMediaItem>`), `HasMore` |
| `MediaDetailsService` | Klasse | `LoadAsync(mediaType, mediaId, title, showId, seasonId, episodeId, ct)` → `MediaDetailsView` — je Typ Movie-/Collection-/Show-/Episode-Pfad inkl. Kollektions-Fallback und Serienkontext |
| `FavoritesService` | Klasse | `ToggleAsync(mediaType, mediaId, title, ct)` → `bool?` (`null` = kein Client); intern `MediaEntryFor` (typkorrekter `MediaEntry`) + `ServerSyncService.SyncFavoritesAsync` |

### `VideoPlayer.Mvu` — Messages (`AppMessage.cs`)

`SourcesPageOpenedMessage`, `SourcesLoadedMessage(IReadOnlyList<SourceItem> Sources, string? Error)`, `SourceSelectedMessage(SourceItem Source)`, `SourceBrowserOpenedMessage(long SourceId, string SourceName)`, `SourceGenresLoadedMessage(long SourceId, IReadOnlyList<SourceGenreItem> Genres)`, `SourceItemsLoadedMessage(long SourceId, int Page, IReadOnlyList<SourceMediaItem> Items, bool Append, string? Error)`, `SourceGenreSelectedMessage(long GenreId)`, `SourceSearchSubmittedMessage(string SearchText)`, `SourceItemsNextPageRequestedMessage`, `SourceItemsRetryRequestedMessage`, `SourceMediaItemSelectedMessage(SourceMediaItem Item)`, `MediaDetailsRequestedMessage(string MediaType, long MediaId, string? Title, long? ShowId, long? SeasonId, long? EpisodeId)`, `MediaDetailsLoadedMessage(string MediaType, long MediaId, MediaDetailsView? Details, string? Error)`, `SeasonSelectedMessage(long SeasonId)`, `FavoriteToggleRequestedMessage`, `FavoriteToggleCompletedMessage(bool? IsFavorite)`.

### `VideoPlayer.Mvu` — Effekte (`AppEffect.cs`)

`LoadSourcesEffect`, `NavigateToSourceOverviewEffect(long SourceId, string SourceName)`, `LoadSourceGenresEffect(long SourceId)`, `LoadSourceItemsEffect(long SourceId, int Page, string? SearchText, long? GenreId, bool Append)`, `LoadMediaDetailsEffect(string MediaType, long MediaId, string? Title, long? ShowId, long? SeasonId, long? EpisodeId)`, `ToggleFavoriteEffect(string MediaType, long MediaId, string Title)`.

### `VideoPlayer.Maui`

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `SourceOverviewPage` | `ContentPage` (Route `sourceoverview`) | Genre-Leiste, `SearchBar`, Medien-Raster, `RemainingItemsThreshold`-Nachladen, Lade-/Fehler-/Leerzustände |
| `CachedSourceIconImage` | `Image`-Subklasse (`Controls`) | `PictureId`-Property → `PosterImageService.GetSourceIconSourceAsync` |

## Änderungen an bestehenden Klassen

### `AppModel` (`VideoPlayer.Mvu`)

- **Neue Eigenschaften:** `SourcesOverview` (`SourcesOverviewState`),
  `SourceBrowser` (`SourceBrowserState`), `MediaDetails`
  (`MediaDetailsState`) — jeweils mit `Initial`/`Empty`-Default.

### `AppUpdate` (`VideoPlayer.Mvu`)

- **Neue Cases:** alle neuen Messages wie unter „Programmabläufe"
  beschrieben (Lade-/Fehlerzustände, Filter/Suche/Paging mit
  Listenrücksetzung, Stale-Guards per `SourceId` bzw.
  `MediaType`+`MediaId`, Staffelwahl, Favoriten-Toggle).
- `SourceSelectedMessage`-Guard: `Source.Id <= 0` → `Unchanged`.
- `MediaItemSelectedMessage`/`SourceMediaItemSelectedMessage` geben die
  `ShowId`/`SeasonId`/`EpisodeId`-Hints an
  `NavigateToMediaDetailsEffect` weiter.

### `NavigateToMediaDetailsEffect` (`VideoPlayer.Mvu`)

- Erweitert um `long? ShowId = null`, `long? SeasonId = null`,
  `long? EpisodeId = null` (bestehender Aufruf bleibt kompatibel).

### `HomeMediaItem` / `HomeListService` (`VideoPlayer.Mvu`)

- `HomeMediaItem` erhält `ShowId`, `SeasonId`, `EpisodeId` (`long?`).
- `HomeListService`: `MapContinueWatching`/`MapEntry` befüllen die Hints
  aus dem eingebetteten Graphen (`TvShowEpisode` → `Season?.Show?.Id`,
  `Season?.Id`, eigene `Id` als `EpisodeId`; `TvShowSeason` →
  `Show?.Id`, eigene `Id` als `SeasonId`).

### `ShellAppEffectHandler` (`VideoPlayer.Maui/Services`)

- **Neue Cases:** `LoadSourcesEffect` → `SourceBrowserService.
  LoadSourcesAsync` → `SourcesLoadedMessage` (Fehler → `Error`-Text);
  `LoadSourceGenresEffect` → `SourceGenresLoadedMessage` (Fehler →
  leere Liste mit „Alle"); `LoadSourceItemsEffect` →
  `SourceItemsLoadedMessage` (Fehler → `Error`-Text, Liste leer);
  `NavigateToSourceOverviewEffect` → `GoToAsync("sourceoverview?id=…&name=…")`
  (MainThread); `LoadMediaDetailsEffect` → `MediaDetailsService.
  LoadAsync` → `MediaDetailsLoadedMessage`; `ToggleFavoriteEffect` →
  `FavoritesService.ToggleAsync` → `FavoriteToggleCompletedMessage` +
  `CachedListsChangedMessage` (nur bei Erfolg bzw. bekanntem Ergebnis —
  bei `null` kein Listen-Reload nötig, aber ebenfalls dispatcht für
  einheitliches Verhalten).

### `AppShell.xaml.cs`

- `Routing.RegisterRoute("sourceoverview", typeof(Pages.SourceOverviewPage))`.

### `SourcesPage` (`VideoPlayer.Maui/Pages`)

- XAML: `BindingContext` = `MvuHost` (`x:DataType`), Offline-Hinweis-
  `Border` (`Model.IsOffline`), `ActivityIndicator` (`Model.
  SourcesOverview.IsLoading`), Fehler-`Label` + „Erneut laden"-`Button`,
  `CollectionView` der Quellen (`Border`-Karte mit `CachedSourceIconImage`
  + Name), Leer-Hinweis bei erfolgreicher leerer Liste.
- Code-Behind: `OnAppearing` → `SourcesPageOpenedMessage`;
  `SelectionChanged` → `SourceSelectedMessage` + Auswahl zurücksetzen;
  Retry-`Button` → `SourcesPageOpenedMessage`.

### `MediaDetailsPage` (`VideoPlayer.Maui/Pages`)

- XAML: Banner/Poster (`CachedPosterImage`), Titel, `MediaTypeText`,
  Jahr/Genre-Zeile, Plot, Stern-`Button` (`★`/`☆`, `DataTrigger` auf
  `Details.IsFavorite`, `IsEnabled` an `!IsFavoriteUpdating`),
  `FavoriteError`-`Label`; Serien-Abschnitt (`Picker` „Staffel wählen",
  Episoden-`CollectionView`), Kollektions-Abschnitt („Filme in dieser
  Sammlung"-`CollectionView`), Lade-/Fehlerzustand + „Erneut laden".
- Code-Behind: zusätzliche `QueryProperty`s `show`, `season`,
  `episode` (`long?`); `OnAppearing` → `MediaDetailsRequestedMessage`;
  `Picker.SelectedIndexChanged` → `SeasonSelectedMessage`;
  Episoden-/Film-Tap → `MediaItemSelectedMessage` (mit Hints);
  Stern-`Clicked` → `FavoriteToggleRequestedMessage`; Retry →
  `MediaDetailsRequestedMessage` erneut.

### `PosterImageService` (`VideoPlayer.Maui/Services`)

- Neue Methode `GetSourceIconSourceAsync(long pictureId, ct)` — gleiche
  Cache-Konvention, Endpunkt `GetSourceIconAsync`, Datei-Suffix
  `{id}.sourceicon`, separater Memory-Cache-Key-Bereich (eigene
  `ConcurrentDictionary` oder Präfix).

### `MauiProgram`

- `AddSingleton<SourceBrowserService>()`, `AddSingleton<MediaDetailsService>()`,
  `AddSingleton<FavoritesService>()`, `AddTransient<Pages.SourceOverviewPage>()`.

## Datenbankmigrationen

Keine — `cached_favorites` wird vom bestehenden `ServerSyncService`
geschrieben; Schema unverändert.

## Validierungsregeln

| Feld / Objekt | Regel | Fehlerfall |
|---------------|-------|------------|
| `SourcesPageOpenedMessage` | nur im Zustand `Connected` ein `LoadSourcesEffect` | Offline/Workflow-Zustände → kein Lauf, Offline-Hinweis sichtbar |
| `SourceSelectedMessage`/`SourceBrowserOpenedMessage` | `SourceId > 0` | `Unchanged` — ohne Quelle kein Browser |
| `SourceGenresLoadedMessage`/`SourceItemsLoadedMessage` | `SourceId` muss dem aktiven Browser entsprechen | Stale-Antwort nach Quellenwechsel → verworfen |
| `SourceItemsNextPageRequestedMessage` | `IsActive && HasMore && !IsLoading && !IsLoadingMore` | `Unchanged` — kein Doppel-Lauf |
| `SourceGenreSelectedMessage`/`SourceSearchSubmittedMessage` | Browser aktiv; identischer Filter/Text → `Unchanged` | verhindert Reload-Schleifen durch `OnAppearing`/`TextChanged` |
| `MediaDetailsRequestedMessage` | `MediaId > 0`, `MediaType` nicht leer; identisches Ziel bereits ladend → `Unchanged` | ungültige Route → Fehlerzustand mit Hinweis |
| `MediaDetailsLoadedMessage` | `MediaId` und normalisierter `MediaType` müssen der aktuellen Anfrage entsprechen | Stale-Antwort → verworfen |
| `FavoriteToggleRequestedMessage` | `Details` vorhanden, `!IsFavoriteUpdating` | `Unchanged` |
| `ToggleFavoriteEffect`-Ergebnis | `null` (kein Client/Fehler) → `IsFavorite` bleibt, `FavoriteError` gesetzt | kein stilles Umschalten ohne Serverbestätigung |
| `SourceMediaItem`-Mapping | `Id <= 0` oder leerer `Type` → Eintrag entfällt | wie `HomeListService` — keine nicht identifizierbaren Einträge |

## Konfigurationsänderungen

Keine.

## Seiteneffekte und Risiken

- **`NavigateToMediaDetailsEffect`-Erweiterung:** neue optionale
  Parameter — `NavigateToMediaDetailsAsync` hängt `show`/`season`/
  `episode` nur an, wenn belegt; bestehender Contract-Test bleibt
  gültig, wird ergänzt.
- **`HomeMediaItem`-Erweiterung:** `required`-Mitglieder bleiben
  unverändert; neue `long?`-Felder haben Default `null` — bestehende
  Tests kompilieren ohne Anpassung.
- **Mehrfach-Navigation:** Details-Route wird pro Auswahl auf den Stack
  geschoben (Serie → Episode → Serie …) — wie im Altbestand; der
  Model-Zustand gehört jeweils der obersten Seite und wird beim
  Zurückkehren per `MediaDetailsRequestedMessage` neu geladen.
- **`RemainingItemsThreshold` auf Windows:** funktioniert auf
  `CollectionView`; Guards verhindern Läufe bei deaktiviertem Browser.
- **Favoriten-Sync nach Toggle:** ein zusätzlicher `GET api/favorites`
  pro Umschaltung — bewusst gewählt für „Server führt, Kopie folgt"
  statt lokaler Upsert-Duplikate.
- **Genre-/Suche-Wechsel während laufendem Ladevorgang:**
  `Append`-Antworten der verdrängten Anfrage werden über `SourceId`-
  Guard nicht gefiltert (gleiche Quelle!) — daher trägt
  `LoadSourceItemsEffect` den Filter-Snapshot und
  `SourceItemsLoadedMessage` `Page`+`Append`; eine veraltete
  `Append`-Antwort kann nur anhängen, wenn `NextPage` passt —
  zusätzlicher Guard: Append-Antwort gilt nur, wenn
  `message.Page == model.SourceBrowser.NextPage`; Replace-Antworten
  (`Append == false`) werden übernommen, wenn die Seite `0` ist
  (Filterwechsel startet immer bei Seite 0).

## Umsetzungsreihenfolge

1. **MVU-Anzeigemodelle + `MediaTypeNames`**
   (`SourceItem`, `SourcesOverviewState`, `SourceGenreItem`,
   `SourceMediaItem`, `SourceBrowserState`, `MediaDetailsView`,
   `SeasonItem`, `EpisodeItem`, `CollectionMovieItem`,
   `MediaDetailsState`) — Voraussetzung für alle weiteren Schritte.
2. **Dienste** `SourceBrowserService`, `MediaDetailsService`,
   `FavoritesService` in `VideoPlayer.Mvu` (Mapping + Client-Zugriff).
3. **MVU-Erweiterung:** Messages, Effekte, `AppModel`-Eigenschaften,
   `AppUpdate`-Cases; `HomeMediaItem`/`HomeListService`-Hints;
   `NavigateToMediaDetailsEffect`-Erweiterung.
4. **App-Rand:** `ShellAppEffectHandler`-Cases,
   `PosterImageService.GetSourceIconSourceAsync`,
   `CachedSourceIconImage`, `AppShell`-Route `sourceoverview`,
   `MauiProgram`-DI.
5. **Seiten:** `SourcesPage`-Ausbau, `SourceOverviewPage` neu,
   `MediaDetailsPage`-Ausbau.
6. **Tests** (s. u.).

## Tests

### Neue Tests

| Test | Testklasse | Prüfung |
|------|------------|---------|
| `SourcesPageOpened_Connected_Loads` | `AppUpdateTests_Sources` | `LoadSourcesEffect`, `IsLoading` |
| `SourcesPageOpened_Offline_NoLoad` | `AppUpdateTests_Sources` | kein Effekt, `IsLoading` falsch |
| `SourcesLoaded_SetsList` / `_Error_SetsError` | `AppUpdateTests_Sources` | Einträge/Fehlertext im Model |
| `SourceSelected_InitializesBrowser_AndNavigates` | `AppUpdateTests_Sources` | Browser-Zustand + `NavigateToSourceOverviewEffect` + `LoadSourceGenresEffect` + `LoadSourceItemsEffect(0)` |
| `SourceSelected_Invalid_NoEffect` | `AppUpdateTests_Sources` | `Id <= 0` → `Unchanged` |
| `SourceGenresLoaded_SetsList_WithAllEntry` | `AppUpdateTests_Sources` | „Alle"-Eintrag, `IsSelected`, Stale-Guard |
| `SourceItemsLoaded_Append_ConcatsAndPages` / `_Replace_Resets` | `AppUpdateTests_Sources` | Anhängen, `NextPage`, `HasMore` bei Teilliste, Stale-Page-Guard |
| `SourceGenreSelected_ReloadsFromPageZero` / `_SameGenre_Unchanged` | `AppUpdateTests_Sources` | Rücksetzen + `LoadSourceItemsEffect` mit `genreId` |
| `SourceSearchSubmitted_ReloadsFromPageZero` / `_SameText_Unchanged` | `AppUpdateTests_Sources` | `search`-Parameter, Leeren setzt zurück |
| `SourceItemsNextPageRequested_LoadsNextPage` / `_Guards` | `AppUpdateTests_Sources` | Append-Effekt; `!HasMore`/ladend → `Unchanged` |
| `SourceItemsRetryRequested_Reloads` | `AppUpdateTests_Sources` | Fehler → neuer Lauf |
| `SourceBrowserOpened_SameSource_Unchanged` / `_OtherSource_Reinitializes` | `AppUpdateTests_Sources` | Rückkehr ohne Reload / Kontextwechsel |
| `SourceMediaItemSelected_NavigatesToDetails` | `AppUpdateTests_Sources` | `NavigateToMediaDetailsEffect` |
| `MediaDetailsRequested_Loads` / `_Invalid_ErrorState` / `_SameTarget_Unchanged` | `AppUpdateTests_MediaDetails` | `LoadMediaDetailsEffect` mit Hints; Dedup |
| `MediaDetailsLoaded_SetsDetails` / `_Stale_Ignored` / `_Error_SetsError` | `AppUpdateTests_MediaDetails` | Übernahme, Stale-Guard, Fehler |
| `SeasonSelected_SwitchesEpisodes` | `AppUpdateTests_MediaDetails` | `SelectedSeasonId` im Model |
| `FavoriteToggleRequested_EmitsEffect` / `_WithoutDetails_Unchanged` | `AppUpdateTests_MediaDetails` | `ToggleFavoriteEffect`, `IsFavoriteUpdating` |
| `FavoriteToggleCompleted_UpdatesFlag` / `_Failure_KeepsStateSetsError` | `AppUpdateTests_MediaDetails` | Stern-Zustand, Fehlerhinweis |
| `LoadSources_MapsEntries` / `_Empty`/ `_NoClient_Null` | `SourceBrowserServiceTests` | `api/Sources`-Mapping via `StubHttpMessageHandler` |
| `LoadGenres_PrependsAll` | `SourceBrowserServiceTests` | „Alle"-Eintrag `Id = 0` |
| `LoadItems_MapsAndPages` (Suchtext/Genre-Parameter, `HasMore`) | `SourceBrowserServiceTests` | Request-URL + `SourceMediaItem`-Mapping inkl. Typ-Normalisierung |
| `Load_Movie` / `_MovieCollection` / `_TvShow` | `MediaDetailsServiceTests` | View-Mapping je Typ (Staffeln/Episoden eingebettet) |
| `Load_Episode_WithShowContext` / `_Standalone` | `MediaDetailsServiceTests` | Serienansicht mit Vorwahl vs. Basisdetails |
| `Load_Season_WithShowHint` | `MediaDetailsServiceTests` | `show`-Hint → Serienansicht, Staffel vorgewählt |
| `Load_Collection_FallsBackToMovie_On404` | `MediaDetailsServiceTests` | Fallback-Pfad |
| `Toggle_AddsAndRemovesFavorite_UpdatesCache` | `FavoritesServiceTests` (`SqliteDatabase`) | `api/favorites/toggle`-Aufruf, `cached_favorites` nach `SyncFavoritesAsync` konsistent |
| `Toggle_WithoutClient_ReturnsNull` | `FavoritesServiceTests` | `null`-Ergebnis |
| `HomeListService_MapsMediaHints` | `HomeListServiceTests` | `ShowId`/`SeasonId`/`EpisodeId` aus eingebettetem Graph |
| `SourcesPage_ShowsOfflineHintAndList` | `SourcesContractTests` | XAML: Offline-Border, `CollectionView` mit `Model.SourcesOverview.Sources`, Icon-Control, Retry |
| `SourceOverview_HasSearchGenreGridAndPaging` | `SourcesContractTests` | `SearchBar`, Genre-`CollectionView`, `GridItemsLayout`, `RemainingItemsThreshold`, Dispatch-Verdrahtung |
| `MediaDetailsPage_ShowsDetailsPerType` | `SourcesContractTests` | Banner (`CachedPosterImage`), Stern-`Button`, `Picker`, Episoden-/Filme-`CollectionView`, Query-Properties `show`/`season`/`episode` |
| `AppShell_RegistersSourceOverviewRoute` | `SourcesContractTests` | `Routing.RegisterRoute("sourceoverview", …)` |
| `ShellAppEffectHandler_HandlesBrowsingAndDetailsEffects` | `SourcesContractTests` | Cases für alle neuen Effekte + `CachedListsChangedMessage` nach Toggle |
| `MauiProgram_RegistersBrowsingServices` | `SourcesContractTests` | DI-Registrierungen |

### Betroffene bestehende Tests

| Test / Testklasse | Grund |
|-------------------|-------|
| `HomePageContractTests.MediaDetails_RouteRegistered_AndPageReadsQueryParameters` | `MediaDetailsPage.xaml.cs` wird umgebaut — Prüfung bleibt bestehen (`mediaType`/`id`/`title` bleiben Query-Properties) |
| `AppUpdateTests_HomeLists.MediaItemSelected_NavigatesToDetails` | `NavigateToMediaDetailsEffect` erhält optionale Parameter — Aufruf/Basisfelder unverändert |

### E2E-Tests (primärer Funktionsnachweis)

Das Repository besitzt kein UI-Automatisierungs-Framework und das
Testprojekt kann `VideoPlayer.Maui` nicht referenzieren (siehe
`inventory/tests.md` — identische Substitution wie in den Schritten
6/7). Benutzerflüsse werden über vollständige MVU-Message-Sequenz-Tests
plus Service-Tests mit `StubHttpMessageHandler`/realer SQLite-Ablage
plus Contract-Tests der XAML-/DI-Verdrahtung nachgewiesen.

| Priorität | Szenario | Test / Nachweis | Akzeptanzkriterium |
|-----------|----------|-----------------|--------------------|
| Pflicht | Quellen-Seite öffnen (online) → Liste der Server-Quellen | Sequenz `SourcesPageOpened` → `LoadSourcesEffect` → `SourcesLoaded` (`AppUpdateTests_Sources`) + `SourceBrowserServiceTests.LoadSources_MapsEntries` + XAML-Contract | AK 1 |
| Pflicht | Quellen-Seite offline → Hinweis statt leerer Liste | `SourcesPageOpened_Offline_NoLoad` + XAML-Contract (Offline-Border an `Model.IsOffline`) | Offline-AK |
| Pflicht | Quelle wählen → Genre-Filter → Suche → Nachladen | Sequenz-Tests `SourceSelected`/`SourceGenresLoaded`/`SourceGenreSelected`/`SourceSearchSubmitted`/`SourceItemsNextPageRequested` (`AppUpdateTests_Sources`) + `SourceBrowserServiceTests` (URL-Parameter) | AK 2 |
| Pflicht | Serie → Staffel wählen → Episode sichtbar; Kollektion → Filme sichtbar | `MediaDetailsServiceTests` (Show-/Collection-Mapping) + `SeasonSelected_SwitchesEpisodes` + XAML-Contract (Picker/Episodenliste/Filmraster) | AK 3 |
| Pflicht | Favorit markieren → Server-Call → lokale Kopie + Startseite folgen | `FavoritesServiceTests` (Toggle + `cached_favorites`) + `FavoriteToggle*` (`AppUpdateTests_MediaDetails`) + Handler-Contract (`CachedListsChangedMessage`) | AK 4 |
| Pflicht | Neustart-Konsistenz | `ServerSyncServiceTests` (Bestand: `SyncFavorites` ersetzt `cached_favorites`) + `HomeListServiceTests` (Favoriten lesen) — Toggle pflegt denselben Pfad | AK 4 |

Bestehende E2E-Tests angepasst: Keine.

## Offene Punkte

| # | Offener Punkt | Empfehlung |
|---|---------------|------------|
| 1 | `MediaItemSummary.Type`-Werte des Servers sind nur eingeschränkt belegt (`"movie"` im Test, `"Movie"`-Vergleich im Altbestand) | Mapping wie im Altbestand (Groß-/Kleinschreibung tolerant): `movie`/`moviecollection`/`collection` → Kollektionsdetails, alles andere → Seriendetails; plus 404-Fallback Kollektion→Film. Deckt Referenzverhalten und Einzelfilme ab. |
| 2 | Staffel-Einträge ohne `ShowId`-Hint (z. B. `cached_favorites` ohne eingebettete `Show`) sind nicht zur Serie auflösbar | Fehlerzustand mit Hinweis auf der Detailseite statt Stillstand; seltene Konstellation, sauber dokumentiert. |
| 3 | Favoriten-Stern-Ziel bei Serienkontext (Serie vs. gewählte Staffel/Episode — Altbestand schaltet die „Auswahl" um) | Der Stern schaltet ausschließlich das navigierte Medium (eindeutig, erfüllt die Anforderung); Episoden sind über ihren Tap eigenständig als Favorit schaltbar. |
