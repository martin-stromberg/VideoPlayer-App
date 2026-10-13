# Logik

Logikklassen und ihre für die Anforderung relevanten Methoden. Die MVU-Nachrichten werden zentral in `AppUpdate.Update` (`AppUpdate.cs:166-289`) per Switch-Ausdruck geroutet; die relevanten Zuordnungen:

| Nachricht | Update-Methode |
|-----------|----------------|
| `MediaItemSelectedMessage` | `UpdateMediaItemSelected` (navigiert per `NavigateToMediaDetailsEffect` mit `ShowId`/`SeasonId`/`EpisodeId`-Hints, `AppUpdate.cs:1330-1344`) |
| `ContinueWatchingItemSelectedMessage` | `UpdateContinueWatchingItemSelected` |
| `SourceSelectedMessage` / `SourceBrowserOpenedMessage` | `UpdateSourceSelected` / `UpdateSourceBrowserOpened` |
| `SourceGenresLoadedMessage` | `UpdateSourceGenresLoaded` |
| `SourceItemsLoadedMessage` | `UpdateSourceItemsLoaded` |
| `SourceGenreSelectedMessage` | `UpdateSourceGenreSelected` |
| `SourceSearchSubmittedMessage` | `UpdateSourceSearchSubmitted` |
| `SourceItemsNextPageRequestedMessage` / `SourceItemsRetryRequestedMessage` | `UpdateSourceItemsNextPageRequested` / `UpdateSourceItemsRetryRequested` |
| `SourceMediaItemSelectedMessage` | `UpdateSourceMediaItemSelected` (`NavigateToMediaDetailsEffect` ohne Hints, `AppUpdate.cs:1741-1752`) |
| `PlayRequestedMessage` | `UpdatePlayRequested` |
| `PlayerPageOpenedMessage` | `UpdatePlayerPageOpened` |
| `PlaybackSourceResolvedMessage` | `UpdatePlaybackSourceResolved` |
| `PlaybackProgressMessage` | `UpdatePlaybackProgress` |
| `PlaybackEndedMessage` | `UpdatePlaybackEnded` |
| `PlaybackFailedMessage` | `UpdatePlaybackFailed` |
| `PlayerClosedMessage` | `UpdatePlayerClosed` |
| `UpcomingEpisodesLoadedMessage` | `UpdateUpcomingEpisodesLoaded` |

## `AppUpdate` (`VideoPlayer.Mvu/AppUpdate.cs`)

Zentrale, reine Update-Funktion des MVU-Kreislaufs. Konstante `PlaybackUnavailableError` = `"Das Video kann nicht abgespielt werden."` (`AppUpdate.cs:75`).

### Quellen-Browsing (FA-1, FA-2, FA-3)

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `NewBrowser(long, string)` | `private static` | Baut einen frischen `SourceBrowserState` (`IsLoading = true`, `HasMore = true`) (`AppUpdate.cs:1525-1531`) |
| `UpdateSourceSelected(AppModel, SourceSelectedMessage)` | `private static` | Browser initialisieren + `NavigateToSourceOverviewEffect` + `LoadSourceGenresEffect` + `LoadSourceItemsEffect(Seite 0, Append: false)` (`AppUpdate.cs:1533-1547`) |
| `UpdateSourceBrowserOpened(AppModel, SourceBrowserOpenedMessage)` | `private static` | Rückkehr auf dieselbe Quelle → unverändert (Zustand/Listenposition bleiben); sonst Browser initialisieren + Genre-/Seiten-Ladung ohne Navigation (`AppUpdate.cs:1549-1571`) |
| `UpdateSourceGenresLoaded(AppModel, SourceGenresLoadedMessage)` | `private static` | Übernimmt `Genres` (mit `IsSelected` passend zu `SelectedGenreId`); verwirft Antworten verlassener Quellen (`AppUpdate.cs:1573-1593`) |
| `UpdateSourceItemsLoaded(AppModel, SourceItemsLoadedMessage)` | `private static` | FA-3-Kern: bei `Append` werden `Items` per `Concat().ToList()` **komplett neu zusammengesetzt** (Referenzwechsel der `ItemsSource`); veraltete Anhänge-Antworten (`Page != NextPage`) werden verworfen; Fehler beim Anhängen lassen die Liste stehen, beim Ersetzen wird `Error` gesetzt (`AppUpdate.cs:1595-1648`) |
| `ReloadBrowserItems(AppModel, SourceBrowserState)` | `private static` | Filter-/Suchwechsel: Liste zurücksetzen, ab Seite 0 mit `SearchText`/`GenreId` laden (`LoadSourceItemsEffect`, `Append: false`) (`AppUpdate.cs:1654-1676`) |
| `UpdateSourceGenreSelected(AppModel, SourceGenreSelectedMessage)` | `private static` | Genre-Wechsel → `SelectedGenreId` + `IsSelected`-Flags setzen → `ReloadBrowserItems`; kein Neuladen bei identischer Auswahl (`AppUpdate.cs:1678-1694`) |
| `UpdateSourceSearchSubmitted(AppModel, SourceSearchSubmittedMessage)` | `private static` | FA-2-Bezugspunkt: `SearchText` (getrimmt) wird **nur hier** ins Modell übernommen → `ReloadBrowserItems`; identischer Text → unverändert (`AppUpdate.cs:1696-1706`) |
| `UpdateSourceItemsNextPageRequested(AppModel)` | `private static` | Guards: inaktiv/`!HasMore`/`IsLoading`/`IsLoadingMore` → unverändert; sonst `IsLoadingMore = true` + `LoadSourceItemsEffect(NextPage, Append: true)` (`AppUpdate.cs:1708-1724`) |
| `UpdateSourceItemsRetryRequested(AppModel)` | `private static` | Retry: mit Einträgen → ausstehende Folgeseite, sonst kompletter Neu-Lauf (`AppUpdate.cs:1726-1739`) |
| `UpdateSourceMediaItemSelected(AppModel, SourceMediaItemSelectedMessage)` | `private static` | Rasterauswahl → `NavigateToMediaDetailsEffect` ohne Kontext-Hints (`AppUpdate.cs:1741-1752`) |

### Weiterschauen-Navigation und Wiedergabe (FA-4, FA-5)

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `UpdateMediaItemSelected(AppModel, MediaItemSelectedMessage)` | `private static` | Auswahl aus normalen Startseiten-Listen → `NavigateToMediaDetailsEffect` mit `ShowId`/`SeasonId`/`EpisodeId` (`AppUpdate.cs:1330-1344`) |
| `UpdateContinueWatchingItemSelected(AppModel, ContinueWatchingItemSelectedMessage)` | `private static` | FA-4-Kern: `movie`/`tvshowepisode` → `StartPlayback` (Direktwiedergabe, bei Episode mit `ShowId` zusätzlich `LoadUpcomingEpisodesEffect`); Playlist-Eintrag → `StartPlaylistPlaybackEffect`; übrige Typen → `NavigateToMediaDetailsEffect` mit Hints (`AppUpdate.cs:1357-1401`) |
| `IsSelectableEntry(HomeMediaItem)` | `private static` | Guard: `Id > 0`, `MediaType` nicht leer, `IsSelectable` (`AppUpdate.cs:1456-1457`) |
| `UpdateUpcomingEpisodesLoaded(AppModel, UpcomingEpisodesLoadedMessage)` | `private static` | Übernimmt die Folge-Episoden nur, wenn der Player noch dieselbe Episode spielt (`AppUpdate.cs:1468-1481`) |
| `UpdatePlayRequested(AppModel, PlayRequestedMessage)` | `private static` | Wiedergabe-Anforderung der Detailseite → `StartPlayback` (nur `movie`/`tvshowepisode`, `MediaId > 0`) (`AppUpdate.cs:3573-3582`) |
| `StartPlayback(AppModel, string, long, string?)` | `private static` | Gemeinsamer Startpfad: `PlayerState` belegen (inkl. `UpcomingEpisodes` aus `NextEpisodeQueue`) + `NavigateToPlayerEffect` + `ResolvePlaybackSourceEffect` (`AppUpdate.cs:3596-3612`) |
| `NextEpisodeQueue(AppModel, long)` | `private static` | Folge-Episoden aus `MediaDetails.Details.Seasons` (staffelübergreifend sortiert, alle nach `mediaId`); ohne Serienkontext leer (`AppUpdate.cs:3623-3645`) |
| `UpdatePlaybackSourceResolved(AppModel, PlaybackSourceResolvedMessage)` | `private static` | Quelle übernehmen bzw. Rückfall auf `AdvancePlaylistEntry`/`AdvanceToNextEpisode` mit `PlaybackUnavailableError`; verwirft abgelöste Identitäten (`AppUpdate.cs:3655-3690`) |
| `AdvanceToNextEpisode(AppModel, PlayerState, string?)` | `private static` | Wechsel zur nächsten Episode der Fortsetzungsliste (Fehler-Rückfall + Auto-Fortsetzung); ohne Folge-Episode Fehlerzustand (`AppUpdate.cs:3703-3745`) |
| `AdvancePlaylistEntry(AppModel, PlayerState, string?)` | `private static` | Playlist-Pendant: `AdvancePlaylistEntryEffect(PlaylistId, PlaylistEntryId)` (`AppUpdate.cs:2580-2595`) |
| `UpdatePlaybackProgress(AppModel, PlaybackProgressMessage)` | `private static` | Positions-Takt: Position/Dauer/Flags übernehmen, `ReportPlaybackProgressEffect` bei Positionsänderung (>0,5 s), `IsWatched` ab `WatchedEndWindowSeconds`-Restzeit, Auto-Vorlauf bei `AutoResumeEnabled` + `IsPlaying` + Restlaufzeit ≤ `AutoResumeRemainingSeconds` (Playlist- oder Serienpfad) (`AppUpdate.cs:3760-3843`) |
| `UpdatePlaybackEnded(AppModel)` | `private static` | FA-5-Kern: **ungeprüfter** Abschluss-Fluss — `IsWatched = true`, Position auf `Duration` gesetzt, `ReportPlaybackProgressEffect` (Endposition), ggf. `DeleteWatchedDownloadEffect`, Auto-Vorlauf (`AdvancePlaylistEntry`/`AdvanceToNextEpisode`). Einziger Guard: `player.IsActive` — **kein Positions-/Dauer-Check, kein Debounce** (`AppUpdate.cs:3845-3906`) |
| `MarkEpisodeWatchedInDetails(AppModel, PlayerState)` | `private static` | Überträgt den Gesehen-Stand als `WatchedAt` auf die Episodenliste der geladenen Details (`AppUpdate.cs:3921-3958`) |
| `UpdatePlaybackFailed(AppModel)` | `private static` | Abspielfehler → `AdvancePlaylistEntry`/`AdvanceToNextEpisode` mit `PlaybackUnavailableError` (`AppUpdate.cs:3960-3977`) |
| `UpdatePlayerClosed(AppModel)` | `private static` | Abschluss-Fortschritt melden, PiP beenden, Player-State zurücksetzen (`AppUpdate.cs:3994` ff.) |
| `UpdatePlayerPageOpened(AppModel)` | `private static` | `Attempted`-Flag setzen (`AppUpdate.cs:3647-3653`) |

## `SourceOverviewPage` (`VideoPlayer.Maui/Pages/SourceOverviewPage.xaml(.cs)`)

Browsing-Seite einer Quelle (Shell-Route `sourceoverview?id=…&name=…`, QueryProperty-Setter `SourceId`/`SourceName`). Abonniert `mvuHost.ModelChanged` nur solange sichtbar (`subscribed`-Flag).

XAML (`SourceOverviewPage.xaml`):

- `MediaSearchBar` (SearchBar, Zeile 17-19): `SearchButtonPressed="OnSearchButtonPressed"` — kein `TextChanged`-Handler.
- `GenresCollection` (CollectionView, Zeile 24-58): `ItemsSource="{Binding Model.SourceBrowser.Genres}"`, `SelectionChanged="OnGenreSelected"`, Chip-Optik über `IsSelected`-DataTrigger — **keine `IsVisible`-Bindung** (FA-1).
- `InitialLoadingIndicator`, `ErrorPanel` (+ „Erneut versuchen"), `EmptyLabel` (Zeilen 60-83).
- `ItemsCollection` (CollectionView, Zeile 87-140): `ItemsSource="{Binding Model.SourceBrowser.Items}"`, `RemainingItemsThreshold="6"`, `RemainingItemsThresholdReached="OnRemainingItemsThresholdReached"`, `GridItemsLayout` Span 3, `CachedPosterImage`-Karten; Footer mit `FooterErrorPanel` + `FooterLoadingIndicator` (`IsLoadingMore`).

Code-Behind (`SourceOverviewPage.xaml.cs`):

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `OnAppearing()` | `protected override` | ModelChanged-Abo, `UpdateUi`, `SourceBrowserOpenedMessage` nur bei Quellenwechsel/fehlendem `Attempted` (Zeile 48-66) |
| `OnDisappearing()` | `protected override` | Abo abmelden (Zeile 69-74) |
| `OnModelChanged(object?, EventArgs)` | `private` | `UpdateUi` gemarshalt auf den Main-Thread (Zeile 76-86) |
| `UpdateUi(AppModel)` | `private` | FA-2-Kern: Fehler-/Leer-Panels steuern und **`MediaSearchBar.Text = browser.SearchText` bei Ungleichheit zurückspiegeln** — überschreibt unbestätigte Eingabe bei jedem Modell-Tick (Zeile 88-102) |
| `OnGenreSelected(object?, SelectionChangedEventArgs)` | `private` | `SourceGenreSelectedMessage(genre.Id)` dispatcht, Auswahl zurückgesetzt (Zeile 104-115) |
| `OnSearchButtonPressed(object?, EventArgs)` | `private` | `SourceSearchSubmittedMessage(MediaSearchBar.Text)` — einziger Weg, Text ins Modell zu übernehmen (Zeile 117-120) |
| `OnMediaItemSelected(object?, SelectionChangedEventArgs)` | `private` | `SourceMediaItemSelectedMessage` (Zeile 122-133) |
| `OnRemainingItemsThresholdReached(object?, EventArgs)` | `private` | `SourceItemsNextPageRequestedMessage` — FA-3-Auslöser (Zeile 135-138) |
| `OnRetryClicked(object?, EventArgs)` | `private` | `SourceItemsRetryRequestedMessage` (Zeile 140-143) |

## `HomePage` (`VideoPlayer.Maui/Pages/HomePage.xaml(.cs)`) — FA-4

Startseite: `MediaCarousel`-Steuerelemente für „Weiterschauen" (`AutomationId="ContinueWatchingCarousel"`, `ItemTapped="OnContinueWatchingItemTapped"`, `LongPressEnabled`, `ItemLongPressed="OnContinueWatchingItemLongPressed"`), „Favoriten" und „Neu im Programm" (`ItemTapped="OnMediaItemTapped"`) — `HomePage.xaml:150-164`.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `OnMediaItemTapped(object?, HomeMediaItem)` | `private` | `MediaItemSelectedMessage` → Detailseite (`.xaml.cs:103-108`) |
| `OnContinueWatchingItemTapped(object?, HomeMediaItem)` | `private` | `ContinueWatchingItemSelectedMessage` — Update entscheidet über Direktwiedergabe bzw. Detailseite (`.xaml.cs:110-115`) |
| `OnContinueWatchingItemLongPressed(object?, HomeMediaItem)` | `private` | Kontextmenü „Ausblenden"/„Überspringen" (2-s-Long-Press, Issue #43) — laut Anforderung unverändert zu lassen (`.xaml.cs:117-140`) |

## `MediaCarousel` (`VideoPlayer.Maui/Controls/MediaCarousel.xaml(.cs)`)

Wiederverwendbares horizontales Karten-Karussell.

| Element | Beschreibung |
|---------|--------------|
| `Items` (BindableProperty) | `IReadOnlyList<HomeMediaItem>?` — Einträge; leer = Abschnitt ausgeblendet |
| `LongPressEnabled` (BindableProperty) | Schaltet die 2-s-Long-Press-Erkennung frei (Standard: aus) |
| `ItemTapped` (Event) | `EventHandler<HomeMediaItem>` — Auswahl eines Eintrags |
| `ItemLongPressed` (Event) | `EventHandler<HomeMediaItem>` — Long-Press auf einen Eintrag |

## `MediaDetailsPage` (`VideoPlayer.Maui/Pages/MediaDetailsPage.xaml(.cs)`) — FA-4-Zielseite

Detailseite (Shell-Route `mediadetails?mediaType=…&id=…&title=…[&show=…&season=…&episode=…]`, QueryProperty-Setter `MediaType`/`MediaId`/`MediaTitle`/`ShowId`/`SeasonId`/`EpisodeId`). Zeigt je nach Typ Basisdetails, Serienansicht (Staffel-Picker + Episodenliste via `BindableLayout`) oder Kollektion. `OnAppearing` fordert einmalig `MediaDetailsRequestedMessage` mit allen Hints an (`.xaml.cs:82-109`); `UpdateUi` (`.xaml.cs:131-198`) zeigt `PlayButton` nur bei `details.IsPlayable` (offline zusätzlich `HasLocalFile`).

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `OnPlayClicked(object?, EventArgs)` | `private` | „Abspielen" → `PlayRequestedMessage(details.MediaType, details.MediaId, Titel)` (`.xaml.cs:440-453`) |
| `OnEpisodePlayClicked(object?, EventArgs)` | `private` | „Abspielen" aus der Episodenliste → `PlayRequestedMessage("tvshowepisode", …)` (`.xaml.cs:455-467`) |
| `OnEpisodeTapped(object?, TappedEventArgs)` | `private` | Episoden-Auswahl → `MediaItemSelectedMessage` mit Show-/Season-/Episode-Hints → `NavigateToMediaDetailsEffect` (`.xaml.cs:318-339`) |
| `OnCollectionMovieSelected` | `private` | Film der Kollektion → `MediaItemSelectedMessage` (`.xaml.cs:341-361`) |
| `OnSeasonSelected` | `private` | `SeasonSelectedMessage` mit Echo-Guards (`updatingSeasonPicker`, `SelectedItem`-Vergleich) (`.xaml.cs:299-316`) |
| `OnFavoriteClicked` / `OnAddToPlaylistClicked` / `OnDownloadClicked` / `OnEpisodeDownloadClicked` | `private` | `FavoriteToggleRequestedMessage` / `PlaylistAddTargetsRequestedMessage` / `DownloadRequestedMessage` |

Bestehender „Detailansicht + Player"-Ablauf: `PlayerPage` wird per `NavigateToPlayerEffect` auf den Stack geschoben — „Player schließen" kehrt zur darunterliegenden Seite (heute `HomePage` bei Direktwiedergabe) zurück.

## `PlayerPage` (`VideoPlayer.Maui/Pages/PlayerPage.xaml(.cs)`) — FA-5

Wiedergabeseite (Route `player`). `MediaElement`-Events in XAML verdrahtet: `MediaOpened`, `MediaEnded="OnMediaEnded"`, `MediaFailed`, `StateChanged` (`.xaml:13-20`); Fortschrittstakt 5 s (`ProgressTickInterval`, `StartProgressTimer` → `ReportPosition` → `PlaybackProgressMessage`).

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `StartProgressTimer()` / `ReportPosition()` | `private` | 5-s-Takt während `Playing` → `PlaybackProgressMessage(Position, Duration, IsPlaying, IsBuffering)` (`.xaml.cs:133-185`) |
| `OnMediaOpened(object?, EventArgs)` | `private` | Fortsetzungs-Seek (`pendingResumePosition`) merken; wird erst bei `Playing`/`Paused` angewendet (`.xaml.cs:319-349`) |
| `TryApplyPendingResumeSeek()` / `ApplyResumeSeekAsync(TimeSpan)` | `private` | Fortsetzungs-Seek mit 10-s-Timeout (`ResumeSeekTimeout`); Fehler → `PlaybackFailedMessage` (`.xaml.cs:359-415`) |
| `OnMediaEnded(object?, EventArgs)` | `private` | **FA-5-Kern:** dispatcht `PlaybackEndedMessage` ungeprüft bei jedem `MediaEnded`-Event — kein Positions-/Dauer-Vergleich, kein Debounce (`.xaml.cs:417-424`) |
| `OnMediaFailed(object?, MediaFailedEventArgs)` | `private` | `PlaybackFailedMessage` mit bereinigter Fehlermeldung (Token-Maskierung) (`.xaml.cs:426-433`) |
| `OnMediaStateChanged(object?, MediaStateChangedEventArgs)` | `private` | Zustandswechsel → `ReportPosition` + `TryApplyPendingResumeSeek` auf dem UI-Thread (`.xaml.cs:435-452`) |
| `UpdateUi(AppModel)` | `private` | Quellenwechsel anwenden (Setz-/Leerpfad mit `appliedLocation`/`sourceApplyFailed`-Guards, `IsValidSourceLocation`-Prüfung), Overlay-/Lade-/Fehler-Anzeigen, Playlist-Navigation (`.xaml.cs:201-261`) |
| `OnPipClicked` / `OnPreviousClicked` / `OnNextClicked` | `private` | `PictureInPictureRequestedMessage` / `PlaylistPreviousRequestedMessage` / `PlaylistNextRequestedMessage` |
| `OnPlayerTapped` | `private` | Windows: Transportsteuerung per Tap einblenden (`#if WINDOWS`) |

## `SourceBrowserService` (`VideoPlayer.Mvu/SourceBrowserService.cs`)

Lädt Quellen-Browsing-Daten über `AuthSessionService.Client`.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `AllGenresName` | `public const string` = `"Alle"` | Anzeigename des „Alle"-Filtereintrags |
| `LoadSourcesAsync(CancellationToken)` | `public` | `GetSourcesAsync` → `SourceItem`-Liste (wirft `InvalidOperationException` ohne Client) |
| `LoadGenresAsync(long, CancellationToken)` | `public` | `GetSourceGenresAsync(sourceId)` → `SourceGenreItem`-Liste mit vorangestelltem „Alle" (`Id = 0`, `IsSelected = true`); ohne Client nur „Alle"; ungültige Server-Genres (`Id <= 0` / leerer Name) werden übersprungen — FA-1: bei leerer Serverliste Ergebnis `[Alle]` (`SourceBrowserService.cs:74-99`) |
| `LoadItemsAsync(long, int, string?, long?, CancellationToken)` | `public` | `GetSourceItemsAsync(sourceId, page, PageSize, searchText, genreId)` → `SourceItemsPage` mit Mapping; `HasMore` = volle Seite (`SourceBrowserService.cs:115-148`) |
| `MapSummaryType(string)` | `internal static` | Legacy-Typen → CLR-Konvention (`movie`/`collection` → `moviecollection`, `show` → `tvshow` u. a.; unbekannt → `tvshow`) |

## `UpcomingEpisodesService` (`VideoPlayer.Mvu/UpcomingEpisodesService.cs`)

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `LoadAsync(long mediaId, long showId, CancellationToken)` | `public` | Folge-Episoden der gestarteten Episode (alle nach `mediaId`, staffelübergreifend sortiert); online `GetTvShowAsync` + `ShowDetailsCache`-Auffrischung, sonst Cache-Fallback; Fehler → leere Liste (`UpcomingEpisodesService.cs:49-99`) |

Wird aufgerufen von `ShellAppEffectHandler.LoadUpcomingEpisodesAsync` (Ergebnis: `UpcomingEpisodesLoadedMessage`).

## `MediaDetailsService` (`VideoPlayer.Mvu/MediaDetailsService.cs`)

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `LoadAsync(string mediaType, long mediaId, string? fallbackTitle, long? showId, long? seasonId, long? episodeId, CancellationToken)` | `public` | Detaildaten je Typ über die `api/items/{typ}/{id}`-Endpunkte; Episoden-/Staffel-Aufrufe werden per Hints in den Serienkontext gebracht; ohne Client Offline-Snapshot aus Download-Manifest + `ShowDetailsCache` (`MediaDetailsService.cs:77-104`) |

## `OfflineContinueWatchingService` (`VideoPlayer.Mvu/OfflineContinueWatchingService.cs`)

Bildet die Offline-„Weiterschauen"-Liste aus dem Download-Manifest (nur `Completed` + vorhandene Datei) inklusive Platzhaltern fehlender Folge-Episoden.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `LoadHomeListsAsync(CancellationToken)` | `public` | Weiterschauen (offline) + Favoriten/Neuheiten aus Cache |
| `LoadContinueWatchingAsync(CancellationToken)` | `internal` | `HomeMediaItem`-Liste aus Manifesteinträgen; Episoden tragen `ShowId`/`EpisodeId`, Platzhalter `IsPlaceholder = true` (→ `IsSelectable = false`) |

## `ShellAppEffectHandler` (`VideoPlayer.Maui/Services/ShellAppEffectHandler.cs`)

Implementiert `IAppEffectHandler` — führt die MVU-Effekte am App-Rand aus (Dispatch per Switch in `HandleAsync`, `ShellAppEffectHandler.cs:182-290`).

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `NavigateToMediaDetailsAsync(NavigateToMediaDetailsEffect)` | `private` | `Shell.GoToAsync("mediadetails?mediaType=…&id=…&title=…[&show&season&episode]")` — Hints nur bei `> 0` angehängt (Zeile 687-719) |
| `NavigateToSourceOverviewAsync(NavigateToSourceOverviewEffect)` | `private` | `Shell.GoToAsync("sourceoverview?id=…&name=…")` mit Dedup-Guard `IsTopmostSourceOverview` (Zeile 721-748) |
| `IsTopmostSourceOverview` / `IsTopmostRouteWithId` | `private static` | Prüft oberste Shell-Route auf gleiche Route + `id` (Zeile 753-796) |
| `LoadSourceGenresAsync(LoadSourceGenresEffect, CancellationToken)` | `private` | FA-1: `SourceBrowserService.LoadGenresAsync` → `SourceGenresLoadedMessage`; **Ladefehler-Fallback: Liste nur „Alle"** (kein Fehler-Flag, Fehlschlag nur im Log) (Zeile 816-833) |
| `LoadSourceItemsAsync(LoadSourceItemsEffect, CancellationToken)` | `private` | `LoadItemsAsync` → `SourceItemsLoadedMessage`; Fehler → leere Seite + `Error`-Text (Zeile 835-853) |
| `NavigateToPlayerAsync()` | `private` | `Shell.GoToAsync("player")` (Zeile 395-410) |
| `ResolvePlaybackSourceAsync(ResolvePlaybackSourceEffect, CancellationToken)` | `private` | `PlaybackSourceService.ResolveAsync` (offline nur lokale Dateien) → `PlaybackSourceResolvedMessage` (Zeile 412-452) |
| `LoadUpcomingEpisodesAsync(LoadUpcomingEpisodesEffect, CancellationToken)` | `private` | `UpcomingEpisodesService.LoadAsync` → `UpcomingEpisodesLoadedMessage`; Fehler → leere Liste (Zeile 463-477) |
| `ReportPlaybackProgressAsync(ReportPlaybackProgressEffect, CancellationToken)` | `private` | `ProgressReportService.ReportProgressAsync` (Zeile 479-498) |

## `VideoPlayerApiClient` (`VideoPlayer.ApiClient/VideoPlayerApiClient.cs`) — nur Diagnose FA-1

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetSourcesAsync` | `(ct)` | `Task<IReadOnlyList<MediaSource>>` | `GET api/Sources` (Zeile 214-217) |
| `GetSourceAsync` | `(sourceId, ct)` | `Task<MediaSource>` | `GET api/Sources/{id}` |
| `GetSourceGenresAsync` | `(sourceId, ct)` | `Task<SourceGenres>` | `GET api/SourceGenres/{id}` — wirft bei `sourceId <= 0` (Zeile 233-237) |
| `GetSourceItemsAsync` | `(mediaSourceId, page, pageSize, searchText, genreId, ct)` | `Task<IReadOnlyList<MediaItemSummary>>` | `GET api/items?mediaSourceId=…&page=…&size=…[&search][&genreId]` — `search`/`genreId` nur bei belegtem Wert (Zeile 378-401) |
| `GetTvShowAsync` | `(id, ct)` | `Task<TvShow>` | `GET api/items/tvshow/{id}` — Serienkontext für `UpcomingEpisodesService` |

## `MediaTypeNames` (`VideoPlayer.Mvu/MediaTypeNames.cs`)

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `Normalize(string?)` | `public static` | Server- und CLR-Konvention → CLR-Konvention (`movie`, `moviecollection`, `tvshow`, `tvshowseason`, `tvshowepisode`) |
| `ToServerName(string?)` | `public static` | Beliebige Konvention → kanonischer Server-Enum-Name (`Movie`, `TVShow`, …) |
