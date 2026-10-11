# Umsetzungsplan: Playlist-Anbindung und -Anzeige (Übersicht + Detailansicht)

## Übersicht

Die Server-Playlist-API wird an die App angebunden: neue Menüseite „Playlists" (eigene + öffentliche Playlists mit Cover), geschobene Detailseite mit seitenweise nachladbaren Einträgen in Server-Reihenfolge. Umsetzung strikt nach den etablierten MVU- und Seitenmustern (`FavoritesPage` für die Menüseite, `SourceBrowser`/`SourceOverviewPage` für die Paging-Detailansicht). Playlists sind ein reines Online-Feature — kein Offline-Cache; bei fehlender Verbindung verständliche Fehleranzeige.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Übersichtsliste (eigene + öffentliche Playlists) | Eine flache `CollectionView` mit Zeilenrecord `PlaylistOverviewItem` (`IsHeader` + `Playlist`) — Sektionsüberschriften „Meine Playlists"/„Öffentliche Playlists" als eigene Zeilen | `CollectionView` in `ScrollView` ist auf Windows nicht zulässig; zwei getrennte `CollectionView`s zersplittern den Scrollbereich und erschweren die E2E-Selektion; die flache Liste nutzt die `DevApi`-`Items`-Ableitung (Text über `Title`) direkt |
| Cover-Anzeige | `VideoPlayerApiClient.GetPlaylistCoverAsync` (`api/playlists/{id}/cover`, 404 → `null`) + `PosterImageService.GetPlaylistCoverSourceAsync` (Datei-Cache `{id}.playlistcover` + Memory-Cache) + eigenes Control `CachedPlaylistCoverImage` mit `PlaylistId`-Property | `api/playlists/{id}/cover` ist der vom Kunden genannte Endpunkt und deckt auch generierte Collagen ohne `CoverPictureId`-Bildpfad ab; Muster exakt wie `CachedPosterImage`/`GetSourceIconSourceAsync` |
| Eintrags-Poster | `CachedPosterImage` unverändert mit `ResolvedPictureId` (`api/pictures/{id}`) | `ResolvedPictureId` ist bereits serverseitig aufgelöste Bild-Id |
| Detailseite | Geschobene Shell-Route `playlistdetails?id=…&name=…` mit `QueryProperty`, Zustand `PlaylistDetailsState`, Paging über `entries/paged` (1-basiert) | Muster `sourceoverview`/`SourceBrowserState`; `entries/paged` ist der einzige Endpunkt mit definierter Server-Reihenfolge + `HasNextPage` |
| Metadaten der Detailseite | Beim Öffnen aus der Liste werden die bekannten Felder übernommen; `PlaylistDetailsOpenedMessage` (Rückkehr/Wiederherstellung) lädt Kopfdaten via `GET api/playlists/{id}` nach (`LoadPlaylistDetailsEffect` — nicht fatal bei Fehlschlag, wie Genre-Laden) | Vermeidet leere Kopfzeile ohne eigenen Fehlerpfad; folgt dem „nicht fatal"-Muster von `LoadSourceGenresAsync` |
| Offline-Verhalten | `UpdatePlaylistsPageOpened` setzt bei `ConnectionState != Connected` direkt `Error` (verständlicher Text) ohne Effekt; Detail-Paging ebenso geblockt | Festgelegte Annahme: kein Offline-Cache; identisch zur Guard-Logik von `UpdateFavoritesPageOpened` |
| Menüposition | „Playlists" zwischen „Favoriten" und „Downloads" | Inhaltliche Nachbarschaft zu den benutzerbezogenen Listen; Contract-Tests werden entsprechend angepasst |
| Eintragsauswahl | Tap navigiert zur Medien-Detailseite (`NavigateToMediaDetailsEffect` mit `MediaTypeNames.Normalize(entry.MediaType)`, Elternkontext als Show/Season-Hints) | In der Anforderung ausdrücklich zulässig; deckt sich mit dem Verhalten aller übrigen Listen |

## Programmabläufe

### Übersicht „Playlists" öffnen und laden

1. Nutzer wählt „Playlists" im Flyout → Shell navigiert nativ → `NavigatedMessage(AppPage.Playlists)` → `CurrentPage = Playlists`.
2. `PlaylistsPage.OnAppearing` dispatcht `PlaylistsPageOpenedMessage`.
3. `UpdatePlaylistsPageOpened`: Guard `IsLoading`; bei `ConnectionState != Connected` → `Attempted` + `Error` „keine Serververbindung"; sonst `IsLoading` + `LoadPlaylistsEffect`.
4. `ShellAppEffectHandler.LoadPlaylistsAsync`: `PlaylistService.LoadPlaylistsAsync` ruft `GetPlaylistsAsync` und `GetPublicPlaylistsAsync` auf, mappt auf `PlaylistListItem`, meldet `PlaylistsLoadedMessage(own, public, error)`.
5. `UpdatePlaylistsLoaded`: `Items` (flache Zeilenliste mit Sektionsüberschriften) befüllt, `IsLoading`/`Error` zurückgesetzt.
6. Die Seite rendert Sektionszeilen (Header-Template) und Karten (Cover via `CachedPlaylistCoverImage`, Name, Genre-Zeile).
7. Retry: `PlaylistsRetryRequestedMessage` (bzw. erneutes `PlaylistsPageOpenedMessage`) startet denselben Lauf erneut.

Beteiligte Klassen/Komponenten: `AppShell`, `PlaylistsPage`, `AppUpdate`, `ShellAppEffectHandler`, `PlaylistService`, `VideoPlayerApiClient`, `PosterImageService`, `CachedPlaylistCoverImage`.

### Playlist öffnen und Einträge paginiert anzeigen

1. Auswahl in der Übersicht dispatcht `PlaylistSelectedMessage(item)`.
2. `UpdatePlaylistSelected`: `PlaylistDetailsState` mit Kopfdaten befüllt (`IsLoading = true`), Effekte `NavigateToPlaylistDetailsEffect(id, name)` + `LoadPlaylistEntriesEffect(id, 1, Append: false)`.
3. Shell pusht `playlistdetails?id=…&name=…`; `PlaylistDetailsPage` liest die Query-Properties.
4. `LoadPlaylistEntriesAsync` im Effekt-Handler ruft `GetPlaylistEntriesPagedAsync(id, pageNumber, PageSize)` auf, mappt `DtoPlaylistEntry` → `PlaylistEntryItem`, meldet `PlaylistEntriesLoadedMessage(…, hasNextPage, append, error)`.
5. `UpdatePlaylistEntriesLoaded`: Ersetzen (Seite 1) oder Anhängen (Folgeseiten, veraltete Antworten via `Page != NextPage` verworfen); `NextPage = Page + 1`, `HasMore = HasNextPage`.
6. `RemainingItemsThresholdReached` auf der `CollectionView` dispatcht `PlaylistEntriesNextPageRequestedMessage` → Guard `HasMore`/`IsLoading*` → `LoadPlaylistEntriesEffect(id, NextPage, Append: true)`.
7. Wiederherstellung (`PlaylistDetailsOpenedMessage` ohne bekannten Zustand): `LoadPlaylistDetailsEffect` (Kopfdaten, nicht fatal) + `LoadPlaylistEntriesEffect(id, 1, Append: false)`.
8. Fehler: Erstlauf → `ErrorPanel` + „Erneut versuchen" (`PlaylistEntriesRetryRequestedMessage`); Fehler beim Anhängen → Footer-Fehlerzeile, Liste bleibt bestehen.
9. Eintragsauswahl: `PlaylistEntrySelectedMessage` → `NavigateToMediaDetailsEffect` (zulässige Vorwegnahme; keine Wiedergabe — Folgeschritt 5).

Beteiligte Klassen/Komponenten: `PlaylistsPage`, `PlaylistDetailsPage`, `AppUpdate`, `ShellAppEffectHandler`, `PlaylistService`, `VideoPlayerApiClient`, `CachedPosterImage`.

### Cover laden

1. `CachedPlaylistCoverImage.PlaylistId` wird gebunden → `PosterImageService.GetPlaylistCoverSourceAsync`.
2. Memory-Cache → Datei-Cache (`{id}.playlistcover` in `PreviewsDirectory`) → `GetPlaylistCoverAsync` (404 → `null`).
3. Kein Cover/Fehler → `null`, die Karte zeigt den Titel-Fallback (Muster Favoriten-Karte).

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `Playlist` | Datenmodell (`VideoPlayer.ApiClient/Models/Playlist.cs`) | DTO einer Playlist (Felder wie `DtoPlaylist`; `Genres` als `GenreOption[]`) |
| `PlaylistEntry` | Datenmodell (`VideoPlayer.ApiClient/Models/PlaylistEntry.cs`) | DTO eines Playlist-Eintrags (Felder wie `DtoPlaylistEntry`) |
| `PlaylistEntriesPagedResult` | Datenmodell (`VideoPlayer.ApiClient/Models/PlaylistEntry.cs` oder eigene Datei) | DTO der Paged-Antwort |
| `PlaylistSortModeValues` | statische Klasse | String-Konstanten `ByReleaseDate`/`Manual` + `GetDisplayLabel` |
| `PlaylistListItem` | Record (`VideoPlayer.Mvu`) | Anzeige-Eintrag einer Playlist (Id, Name, Description, SortMode, CoverPictureId, Genres-Text, IsPublic, IsOwner) |
| `PlaylistOverviewItem` | Record (`VideoPlayer.Mvu`) | Flache Listenzeile der Übersicht: `IsHeader` + `Title` + `Playlist?` |
| `PlaylistEntryItem` | Record (`VideoPlayer.Mvu`) | Anzeige-Eintrag der Detailseite (EntryId, MediaType normalisiert, MediaId, Title, ParentTitle, ResolvedPictureId, EpisodeNumber, IsAccessible, MediaTypeText) |
| `PlaylistsPageState` | Record (`VideoPlayer.Mvu`) | Zustand der Übersichtsseite (Items, IsLoading, Attempted, Error, abgeleitete HasItems/HasError/ShowEmpty) |
| `PlaylistDetailsState` | Record (`VideoPlayer.Mvu`) | Zustand der Detailseite (PlaylistId, Name, Description, SortMode, CoverPictureId, Entries, NextPage 1-basiert, HasMore, TotalCount, IsLoading, IsLoadingMore, Attempted, Error, PageSize-Konstante) |
| `PlaylistService` | Klasse (`VideoPlayer.Mvu`) | Serverzugriff + DTO→Anzeige-Mapping über `AuthSessionService.Client` |
| `PlaylistEntriesPage` | Record (`VideoPlayer.Mvu`, in `PlaylistService.cs`) | Ergebnisrecord einer Eintragsseite (Muster `SourceItemsPage`) |
| `PlaylistsPage` | Seite (`VideoPlayer.Maui/Pages/PlaylistsPage.xaml[.cs]`) | Menüseite „Playlists" |
| `PlaylistDetailsPage` | Seite (`VideoPlayer.Maui/Pages/PlaylistDetailsPage.xaml[.cs]`) | Detailseite (Route `playlistdetails`) |
| `CachedPlaylistCoverImage` | Control (`VideoPlayer.Maui/Controls/CachedPlaylistCoverImage.cs`) | Bindable `PlaylistId` → Cover-Bildquelle via `PosterImageService` |
| `AppUpdateTests_Playlists` | Testklasse | Update-Logik der Playlist-Zustände |
| `PlaylistServiceTests` | Testklasse | Mapping und Fehlerpfade des Service |
| `PlaylistsContractTests` o. Erweiterung bestehender | Testklasse(n) | Quelltext-Verträge (Menüeintrag, Route, Seiten-Lebenszyklus) |
| `PlaylistsTests` | E2E-Testklasse | Menüeintrag → Übersicht → Detailansicht (Happy Path + Fehleranzeige) |

## Änderungen an bestehenden Klassen

### `VideoPlayerApiClient` (`VideoPlayer.ApiClient`)

- **Neue Methoden (Region `Playlists`):**
  - `GetPlaylistsAsync(long? genreId = null, CancellationToken)` → `IReadOnlyList<Playlist>` — `GET api/playlists` (optional `?genreId=`)
  - `GetPublicPlaylistsAsync(long? genreId = null, CancellationToken)` → `IReadOnlyList<Playlist>` — `GET api/playlists/public`
  - `GetPlaylistAsync(long playlistId, CancellationToken)` → `Playlist?` — `GET api/playlists/{id}`; 404 → `null`
  - `GetPlaylistEntriesPagedAsync(long playlistId, int pageNumber, int pageSize, CancellationToken)` → `PlaylistEntriesPagedResult` — `GET api/playlists/{id}/entries/paged?pageNumber={n}&pageSize={s}` (1-basiert; Guards `pageNumber >= 1`, `pageSize >= 1`)
  - `GetPlaylistCoverAsync(long playlistId, CancellationToken)` → `byte[]?` — `GET api/playlists/{id}/cover`; `ApiException` mit 404 → `null`

### `AppPage` (`VideoPlayer.Mvu`)

- **Neuer Wert:** `Playlists` — Menüseite „Playlists" (XML-Doku).

### `AppRoutes` (`VideoPlayer.Mvu`)

- `Routes[AppPage.Playlists] = "playlists"`; `MenuPages` um `Playlists` erweitert (Position zwischen `Favorites` und `Downloads`).

### `AppModel` (`VideoPlayer.Mvu`)

- **Neue Eigenschaften:** `PlaylistsPage` (`PlaylistsPageState`, Default `Initial`), `PlaylistDetails` (`PlaylistDetailsState`, Default `Empty`).

### `AppMessage` (`VideoPlayer.Mvu`)

- **Neue Records:** `PlaylistsPageOpenedMessage`, `PlaylistsLoadedMessage(IReadOnlyList<PlaylistListItem> Own, IReadOnlyList<PlaylistListItem> Public, string? Error)`, `PlaylistSelectedMessage(PlaylistListItem Item)`, `PlaylistDetailsOpenedMessage(long PlaylistId, string Name)`, `PlaylistDetailsLoadedMessage(long PlaylistId, PlaylistListItem? Info)`, `PlaylistEntriesLoadedMessage(long PlaylistId, int Page, IReadOnlyList<PlaylistEntryItem> Items, bool HasMore, int TotalCount, bool Append, string? Error)`, `PlaylistEntriesNextPageRequestedMessage`, `PlaylistEntriesRetryRequestedMessage`, `PlaylistEntrySelectedMessage(PlaylistEntryItem Item)` — jeweils deutsche XML-Doku.

### `AppEffect` (`VideoPlayer.Mvu`)

- **Neue Records:** `LoadPlaylistsEffect`, `NavigateToPlaylistDetailsEffect(long PlaylistId, string Name)`, `LoadPlaylistDetailsEffect(long PlaylistId)`, `LoadPlaylistEntriesEffect(long PlaylistId, int Page, bool Append)` — jeweils deutsche XML-Doku.

### `AppUpdate` (`VideoPlayer.Mvu`)

- **Switch-Zweige** für alle neuen Messages; **neue Methoden:** `UpdatePlaylistsPageOpened`, `UpdatePlaylistsLoaded`, `UpdatePlaylistSelected`, `UpdatePlaylistDetailsOpened`, `UpdatePlaylistDetailsLoaded`, `UpdatePlaylistEntriesLoaded`, `UpdatePlaylistEntriesNextPageRequested`, `UpdatePlaylistEntriesRetryRequested`, `UpdatePlaylistEntrySelected` — nach den Mustern `UpdateFavoritesPageOpened`/`UpdateSourceBrowserOpened`/`UpdateSourceItemsLoaded`/`UpdateSourceItemsNextPageRequested`/`UpdateSourceMediaItemSelected`.

### `AppShell.xaml`

- Neues `FlyoutItem` `Title="Playlists" Route="playlists"` zwischen „Favoriten" und „Downloads" mit Material-Icon (Glyph `&#xe05f;` playlist_play) und `ShellContent` → `pages:PlaylistsPage`.

### `AppShell.xaml.cs`

- `Routing.RegisterRoute("playlistdetails", typeof(Pages.PlaylistDetailsPage))` — geschobene Route, kein Menüeintrag.

### `ShellAppEffectHandler` (`VideoPlayer.Maui/Services`)

- **Neue Switch-Zweige:** `LoadPlaylistsEffect` → `LoadPlaylistsAsync`; `NavigateToPlaylistDetailsEffect` → `NavigateToPlaylistDetailsAsync` (MainThread, Route `playlistdetails?id=…&name=…`, Duplikat-Guard analog `IsTopmostSourceOverview` → `IsTopmostPlaylistDetails`); `LoadPlaylistDetailsEffect` → `LoadPlaylistDetailsAsync` (nicht fatal); `LoadPlaylistEntriesEffect` → `LoadPlaylistEntriesAsync`.

### `PosterImageService` (`VideoPlayer.Maui/Services`)

- **Neue Methode:** `GetPlaylistCoverSourceAsync(long playlistId, CancellationToken)` → `ImageSource?` — Cache-Datei `{id}.playlistcover`, Memory-Cache, `GetPlaylistCoverAsync` (404 → `null`), Fehler → `null` (Titel-Fallback).

### `MauiProgram` (`VideoPlayer.Maui`)

- `builder.Services.AddSingleton<PlaylistService>()`; `builder.Services.AddTransient<Pages.PlaylistsPage>()`; `builder.Services.AddTransient<Pages.PlaylistDetailsPage>()`.

### `DevApiModelSnapshot` (`VideoPlayer.Mvu/DevApi`, `#if DEBUG || DEV_API`)

- **Neue Felder:** `PlaylistsPageItemCount` (Anzahl Eintragszeilen ohne Header), `PlaylistsPageError`, `PlaylistDetailsEntryCount`, `PlaylistDetailsError` — Spiegelung in `DevApiModel` (`VideoPlayer.E2E.Tests/Support/DevApiModels.cs`).

### `MockVideoServer` (`VideoPlayer.E2E.Tests/Support`)

- `/api/playlists` und `/api/playlists/public` in `IsListEndpoint` aufnehmen; `ListOverride` liefert Opt-in-Konfiguration.
- Neuer Handler-Zweig für `GET /api/playlists/{id}`, `…/entries`, `…/entries/paged` (Query `pageNumber`/`pageSize` auswerten; `pageNumber < 1` → 400 wie der Server), `…/cover` (Bilddaten aus `PlaylistCoverPng`-Bytes oder 404).
- Neue Opt-in-Eigenschaften: `IReadOnlyList<…>` bzw. einfache Properties `OwnPlaylists`/`PublicPlaylists` als konfigurierbare JSON-Repräsentationen (Hilfsrecords `MockPlaylist`/`MockPlaylistEntry` mit den DTO-Feldnamen) sowie `PlaylistsUnavailable` (Playlist-Routen liefern 404 statt Liste — Fehlerpfad-Test ohne Retry-Verzögerung).

## Datenbankmigrationen

Keine.

## Validierungsregeln

| Feld / Objekt | Regel | Fehlerfall |
|---------------|-------|------------|
| `playlistId` (Client-Methoden) | `> 0` (`ArgumentOutOfRangeException.ThrowIfNegativeOrZero`, Konvention) | Ungültige Id |
| `pageNumber` / `pageSize` (`GetPlaylistEntriesPagedAsync`) | `>= 1` | Ungültige Seite |
| Auswahl in `UpdatePlaylistSelected`/`UpdatePlaylistEntrySelected` | `PlaylistId > 0` / `MediaId > 0` + `MediaType` nicht leer | `AppTransition.Unchanged` |

## Konfigurationsänderungen

Keine.

## Seiteneffekte und Risiken

- **Contract-Tests:** `AppRoutesTests.MenuPages_…` und `VideoPlayerMauiContractTests.ExpectedMenuEntries` sind exakt gepinnt und müssen auf 6 Einträge angepasst werden — beabsichtigter Bruch, kein Seiteneffekt.
- **`NavigatedMessage`-Auflösung:** `playlistdetails` liegt in `PushedRoutes`? Nein — geschobene Detailrouten werden bewusst nicht in `AppRoutes` abgebildet (Muster `sourceoverview`/`mediadetails`: die Route ist nicht Teil von `TryGetPage`; die Seite meldet ihren Zustand selbst über `…OpenedMessage`). `//playlists` als letztes Segment bestimmt weiterhin die Menüseite.
- **Paging-Konvention:** `api/items` ist 0-basiert, `entries/paged` 1-basiert — Risiko einer Off-by-one-Verwechslung; der State hält `NextPage` direkt in der Server-Konvention (1-basiert) und der Effekt trägt die Seitennummer unverändert.
- **Shell-Ortsauflösung:** `//playlists/playlistdetails` löst über das tiefste Segment `playlistdetails` keine `AppPage` auf (Route nicht registriert in `TryGetPage`) — `NavigatedMessage` bleibt aus, `CurrentPage` bleibt `Playlists`; identisch zum Verhalten von `mediadetails`/`sourceoverview` (dort `CurrentPage = Home`). Konsistent.
- **E2E-Laufzeit:** Mock-Fehlerpfad via `PlaylistsUnavailable` (404) vermeidet die 7-s-Retry-Verzögerung eines Verbindungsabbruchs.

## Umsetzungsreihenfolge

1. **DTOs und API-Client-Erweiterung**
   - Voraussetzungen: keine (Projekt baut; `StubHttpMessageHandler` existiert)
   - `Playlist.cs` (Playlist + PlaylistEntriesPagedResult), `PlaylistEntry.cs`, `PlaylistSortModeValues.cs` anlegen; `VideoPlayerApiClient` Region `Playlists` ergänzen; Endpunkt-Tests (`VideoPlayerApiClientTests_Endpoints` bzw. neue Datei `VideoPlayerApiClientTests_Playlists`).

2. **MVU-Zustände, Messages, Effects, Service**
   - Voraussetzungen: Schritt 1 (DTOs für das Mapping)
   - `PlaylistListItem`, `PlaylistOverviewItem`, `PlaylistEntryItem`, `PlaylistsPageState`, `PlaylistDetailsState`, `PlaylistService` (`PlaylistEntriesPage`), `AppPage.Playlists`, `AppRoutes`, `AppModel`-Felder, `AppMessage`/`AppEffect`-Records, `AppUpdate`-Zweige + Update-Methoden; Unit-Tests `AppUpdateTests_Playlists`, `PlaylistServiceTests`, `AppRoutesTests`/`VideoPlayerMauiContractTests`/`EnumCoverage`-Anpassung.

3. **UI: Seiten, Menü, DI, Cover-Pipeline**
   - Voraussetzungen: Schritt 2 (Zustände/Messages)
   - `PlaylistsPage.xaml(.cs)`, `PlaylistDetailsPage.xaml(.cs)`, `CachedPlaylistCoverImage`, `PosterImageService.GetPlaylistCoverSourceAsync`, `AppShell.xaml` `FlyoutItem`, `AppShell.xaml.cs` Route, `MauiProgram` DI, `ShellAppEffectHandler`-Zweige; `PageLifecycleContractTests`-Erweiterung; `DevApiModelSnapshot`-Felder.

4. **E2E: Mock-Server + Tests**
   - Voraussetzungen: Schritt 3 (lauffähige Seiten + AutomationIds)
   - `MockVideoServer`-Playlist-Endpunkte + Opt-in-Daten, `DevApiModel`-Spiegelfelder, `Tests/PlaylistsTests.cs` (Happy Path Übersicht+Detail, Fehleranzeige).

5. **Verifikation + Build**
   - Voraussetzungen: Schritte 1–4
   - `dotnet build` der Solution (Windows-TFM), `dotnet test` (Unit/Contract), E2E-Build + `PlaylistsTests`; Existenz-Verifikation gegen `http://localhost:83/` dokumentieren (401-Nachweis bereits erfolgt, wird im Testbericht festgehalten).

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `GetPlaylistsAsync_…` | `VideoPlayerApiClientTests_Playlists` | URL `api/playlists`, JSON→`Playlist`-Mapping inkl. `CoverPictureId`/`IsPublic` |
| `GetPublicPlaylistsAsync_…` | `VideoPlayerApiClientTests_Playlists` | URL `api/playlists/public` |
| `GetPlaylistAsync_NotFound_ReturnsNull` | `VideoPlayerApiClientTests_Playlists` | 404 → `null` statt `ApiException` |
| `GetPlaylistEntriesPagedAsync_…` | `VideoPlayerApiClientTests_Playlists` | URL `api/playlists/7/entries/paged?pageNumber=2&pageSize=20`, Mapping `Entries`/`TotalCount`/`HasNextPage` |
| `GetPlaylistCoverAsync_NotFound_ReturnsNull` / `_ReturnsBytes` | `VideoPlayerApiClientTests_Playlists` | Bildabruf + 404-Toleranz |
| `PlaylistsPageOpened_*` | `AppUpdateTests_Playlists` | Laden nur bei `Connected`; `IsLoading`-Guard; Offline → `Error` ohne Effekt |
| `PlaylistsLoaded_*` | `AppUpdateTests_Playlists` | Befüllung beider Sektionen zu flachen Zeilen; Fehler → `Error`; `ShowEmpty` |
| `PlaylistSelected_*` | `AppUpdateTests_Playlists` | `NavigateToPlaylistDetailsEffect` + `LoadPlaylistEntriesEffect` + State-Initialisierung |
| `PlaylistDetailsOpened_*` | `AppUpdateTests_Playlists` | Re-Entry-Guard; Wiederherstellung (Details + Seite 1) |
| `PlaylistEntriesLoaded_*` | `AppUpdateTests_Playlists` | Replace/Append, veraltete Antworten (`Page != NextPage`), `HasMore` aus `HasNextPage`, Fehler bei Append lässt Liste stehen |
| `PlaylistEntriesNextPageRequested_*` | `AppUpdateTests_Playlists` | Guards (`HasMore`, `IsLoading`), Folgeseiten-Effekt |
| `PlaylistEntriesRetryRequested_*` | `AppUpdateTests_Playlists` | Neu-Lauf bei leerer Liste vs. Folgeseiten-Retry |
| `PlaylistEntrySelected_*` | `AppUpdateTests_Playlists` | `NavigateToMediaDetailsEffect` mit normalisiertem Typ |
| `LoadPlaylistsAsync_Maps*` / `LoadEntriesAsync_Maps*` | `PlaylistServiceTests` | DTO→Anzeige-Mapping (Genre-Text, SortMode-Label, Parent/Episode-Subtitle); `Client == null` → `InvalidOperationException` |
| `MenuPages_…` / `AppShell_ContainsAllMenuEntries` | `AppRoutesTests`, `VideoPlayerMauiContractTests` | 6 Menüeinträge inkl. „Playlists" an Position 3 |
| `PlaylistsPage_*` / `PlaylistDetailsPage_*` | `PageLifecycleContractTests` | `subscribed`-Muster beider neuer Seiten |
| `AppPage_Playlists_*` | `AppRoutesTests` (Roundtrip `//playlists`) | Routenauflösung |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `AppRoutesTests.MenuPages_ContainsAllFiveMenuEntriesInOrder` | 6 Einträge; Umbenennung sinnvoll (`…AllMenuEntriesInOrder`) |
| `VideoPlayerMauiContractTests.ExpectedMenuEntries` | Sechster Eintrag („Playlists", Route `playlists`) |
| `PageLifecycleContractTests` | Zwei neue Seiten-Testpaare |
| `DevApiContractTests` | Nur falls Snapshot-Feldanzahl gepinnt (prüfen; neue Felder sind additiv) |

### E2E-Tests (primärer Funktionsnachweis)

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| Pflicht | Menüeintrag „Playlists" öffnen → eigene + öffentliche Playlists erscheinen | `VideoPlayer.E2E.Tests/Tests/PlaylistsTests.cs` | Menüeintrag + Auflistung beider Arten | Benutzerfluss über Flyout-Navigation, nur UI-erkaufbar |
| Pflicht | Playlist auswählen → Detailansicht zeigt Einträge in Server-Reihenfolge (paging-fähige Liste) | `PlaylistsTests.cs` | Playlist öffnen, paginierte Einträge Server-Reihenfolge | Navigation + Listendarstellung |
| Pflicht | Cover-Anzeige: Playlist mit `CoverPictureId` ruft `api/playlists/{id}/cover` ab (`WasPathRequested`); ohne Cover Titel-Fallback | `PlaylistsTests.cs` | Cover werden angezeigt, sofern geliefert | Bildpipeline Ende-zu-Ende |
| Pflicht | Playlist-Endpunkte nicht verfügbar (`PlaylistsUnavailable`) → verständliche Fehleranzeige + Retry-Button, kein Absturz | `PlaylistsTests.cs` | Fehleranzeige bei fehlender Verbindung | Fehlerpfad über UI sichtbar |

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| Keine | — |

## Offene Punkte

Keine.
