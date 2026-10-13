# Datenmodell

Datenmodellklassen, die von den fünf Mängeln der Anforderung betroffen sind.

## `SourceBrowserState`

Datei: `VideoPlayer.Mvu/SourceBrowserState.cs`

Zustand der Quellen-Browsing-Seite (`AppModel.SourceBrowser`, `AppModel.cs:182`).

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `PageSize` | `const int` = 20 | Seitengröße des Nachladens (Wert des Altbestands) |
| `Empty` | `static SourceBrowserState` | Ausgangszustand (keine Quelle geöffnet) |
| `SourceId` | `long` | Id der geöffneten Quelle; 0 = kein Browser aktiv |
| `SourceName` | `string` | Anzeigename der Quelle (Seitentitel) |
| `Genres` | `IReadOnlyList<SourceGenreItem>` | Genre-Leiste inkl. „Alle"-Eintrag (Id = 0); FA-1: leere Liste → Leiste rendert unsichtbar |
| `SelectedGenreId` | `long` | Aktuell gewählter Genre-Filter; 0 = „Alle" |
| `SearchText` | `string` | Aktiver (abgeschickter) Suchtext; FA-2: wird per `UpdateUi` ins Feld zurückgespiegelt und überschreibt unbestätigte Eingabe — es existiert kein separates Feld für getippte, noch nicht abgeschickte Eingabe |
| `Items` | `IReadOnlyList<SourceMediaItem>` | Geladene Einträge, kumuliert über alle Seiten; FA-3: wird beim Anhängen per `Concat().ToList()` komplett ersetzt |
| `NextPage` | `int` | Nächste zu ladende Seite (0-basiert) |
| `HasMore` | `bool` | Es gibt (vermutlich) weitere Seiten |
| `IsLoading` | `bool` | Erste Seite / gefilterte Liste lädt (Ersetzen) |
| `IsLoadingMore` | `bool` | Eine Folgeseite lädt (Anhängen) |
| `Attempted` | `bool` | Mindestens ein Ladeversuch abgeschlossen |
| `Error` | `string?` | Fehlertext des letzten Ersetz-Laufs; `null` = kein Fehler |
| `IsActive` | `bool` (abgeleitet) | `SourceId > 0` |
| `HasItems` | `bool` (abgeleitet) | `Items.Count > 0` |
| `HasError` | `bool` (abgeleitet) | `Error is not null` |
| `ShowEmpty` | `bool` (abgeleitet) | `Attempted && !IsLoading && !HasError && !HasItems` |

## `SourceGenreItem`

Datei: `VideoPlayer.Mvu/SourceGenreItem.cs`

Eintrag der Genre-Leiste; „Alle" ist der künstliche Eintrag mit `Id` = 0.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `long` (required) | Genre-Id; 0 = „Alle" (kein Filter — `genreId` wird nicht gesendet) |
| `Name` | `string` (required) | Anzeigename |
| `IsSelected` | `bool` | Das Genre ist der aktuell gewählte Filter (steuert die Chip-Optik per XAML-DataTrigger) |

## `SourceMediaItem`

Datei: `VideoPlayer.Mvu/SourceMediaItem.cs`

Eintrag des Quellen-Medienrasters (`ItemsCollection` der `SourceOverviewPage`).

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `long` (required) | Medien-Id (Navigationsziel der Detailseite) |
| `Title` | `string` (required) | Anzeigetitel |
| `MediaType` | `string` (required) | Normalisierter Medientyp (CLR-Konvention); „Film"-Einträge → `moviecollection` |
| `PosterPictureId` | `long?` | Bild-Id für `api/pictures/{id}`; `null` = Titel-Fallback |
| `HasPoster` | `bool` (abgeleitet) | `PosterPictureId is > 0` |

## `SourceItem`

Datei: `VideoPlayer.Mvu/SourceItem.cs`

Eintrag der Quellen-Übersicht (Navigationsmenü).

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `long` (required) | Quellen-Id (Navigationsziel der Browsing-Seite) |
| `Name` | `string` (required) | Anzeigename |
| `IconPictureId` | `long?` | Bild-Id für `api/sourceicons/{id}`; `null` = kein Icon |
| `HasIcon` | `bool` (abgeleitet) | `IconPictureId is > 0` |

## `SourceItemsPage`

Datei: `VideoPlayer.Mvu/SourceBrowserService.cs:10-13`

Ergebnis-Datensatz einer geladenen Eintragsseite (Rückgabe von `SourceBrowserService.LoadItemsAsync`).

| Feld | Typ | Beschreibung / Zweck |
|------|-----|----------------------|
| `Page` | `int` | Geladene Seite (0-basiert) |
| `Items` | `IReadOnlyList<SourceMediaItem>` | Einträge der Seite |
| `HasMore` | `bool` | `true`, solange weitere Seiten erwartet werden (Seite voll gefüllt) |

## `HomeMediaItem`

Datei: `VideoPlayer.Mvu/HomeMediaItem.cs`

Eintrag der Startseiten-Listen (Weiterschauen, Favoriten, Neu im Programm) — FA-4: trägt bereits die Serienkontext-Hints der Detailnavigation.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `long` (required) | Medien-Id (Navigationsziel) |
| `MediaType` | `string` (required) | Medientyp — Server-Konvention (Weiterschauen: `movie`, `show`, `season`, `episode`, `collection`) oder CLR-Konvention (`moviecollection`, `tvshow`, `tvshowseason`, `tvshowepisode`); normalisiert via `MediaTypeNames.Normalize` |
| `Title` | `string` (required) | Anzeigetitel |
| `PosterPictureId` | `long?` | Bild-Id für `api/pictures/{id}` |
| `Progress` | `double?` | Fortschritt 0–1 (nur Weiterschauen); `null` = kein Balken |
| `ShowId` | `long?` | Id der zugehörigen Serie (Episoden-/Staffel-Einträge) — Detail-Hint |
| `SeasonId` | `long?` | Id der zugehörigen Staffel — Detail-Hint |
| `EpisodeId` | `long?` | Id der Episode selbst — Detail-Hint |
| `PlaylistId` | `long?` | Playlist-Id des Wiedergabekontexts (nur Weiterschauen) |
| `PlaylistName` | `string?` | Name der Playlist |
| `PlaylistEntryId` | `long?` | Id des Playlist-Eintrags; `null` → Serienkontext-Pfad |
| `IsPlaceholder` | `bool` | Offline-Platzhalter fehlender Episoden (nicht abspielbar) |
| `IsSelectable` | `bool` (abgeleitet) | `!IsPlaceholder` |
| `CardOpacity` | `double` (abgeleitet) | 0,55 bei Platzhalter, sonst 1,0 |
| `HasPoster` | `bool` (abgeleitet) | `PosterPictureId is > 0` |
| `ShowProgress` | `bool` (abgeleitet) | `Progress is > 0` |

## `PlayerState`

Datei: `VideoPlayer.Mvu/PlayerState.cs`

Zustand der Wiedergabeseite (`AppModel.Player`, `AppModel.cs:209`).

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Empty` | `static PlayerState` | Ausgangszustand (keine Wiedergabe angefordert) |
| `MediaType` | `string?` | Medientyp (normalisiert, CLR-Konvention) |
| `MediaId` | `long` | Medien-Id |
| `Title` | `string?` | Anzeigetitel |
| `Source` | `PlaybackSource?` | Aufgelöste Quelle; `null` während der Auflösung / bei Fehlschlag |
| `IsResolving` | `bool` | Quellauflösung läuft |
| `Attempted` | `bool` | Seite geöffnet, Wiedergabeversuch gestartet |
| `Position` | `TimeSpan` | Aktuelle Wiedergabeposition (letzter Takt) — FA-5: Bezugsgröße eines Plausibilitätschecks |
| `Duration` | `TimeSpan` | Gesamtdauer — FA-5: Bezugsgröße eines Plausibilitätschecks |
| `IsPlaying` | `bool` | Wiedergabe läuft |
| `IsBuffering` | `bool` | Pufferung |
| `IsWatched` | `bool` | „Angesehen"-Markierung (Endfenster erreicht bzw. Ende) |
| `IsPictureInPicture` | `bool` | PiP-/Mini-Player-Modus |
| `UpcomingEpisodes` | `IReadOnlyList<PlayerEpisodeRef>` | Fortsetzungsliste (Auto-Fortsetzung / Fehler-Rückfall) |
| `HasNextEpisode` | `bool` (abgeleitet) | `UpcomingEpisodes.Count > 0` |
| `PlaylistId` | `long?` | Playlist-Kontext; `null` = Serien-/Einzelkontext |
| `PlaylistName` | `string?` | Name der Playlist |
| `PlaylistEntryId` | `long` | Aktuell gespielter Playlist-Eintrag; 0 ohne Playlist |
| `PlaylistPosition` | `int` | 1-basierte Position in der Playlist; 0 ohne Playlist |
| `PlaylistTotalCount` | `int` | Gesamtzahl der Playlist-Einträge; 0 ohne Playlist |
| `PlaylistNextExhausted` | `bool` | Server meldete keinen Folgeeintrag (unterdrückt Vorwärts-Versuche) |
| `IsPlaylistPlayback` | `bool` (abgeleitet) | `PlaylistId is > 0 && PlaylistEntryId > 0` |
| `CanNavigateNext` | `bool` (abgeleitet) | Manuelle Vorwärts-Navigation möglich (Heuristik) |
| `CanNavigatePrevious` | `bool` (abgeleitet) | `IsPlaylistPlayback && PlaylistPosition > 1` |
| `PlaylistLabel` | `string` (abgeleitet) | Kontextzeile der Playlist-Wiedergabe |
| `Error` | `string?` | Fehlertext der letzten Störung; `null` = kein Fehler |
| `HasError` | `bool` (abgeleitet) | `Error is not null` |
| `LastReportedPositionSeconds` | `double` | Zuletzt gemeldete Position (Throttling); Initial −1 |
| `IsActive` | `bool` (abgeleitet) | `MediaId > 0 && MediaType is not null` |

## `PlayerEpisodeRef`

Datei: `VideoPlayer.Mvu/PlayerEpisodeRef.cs`

Verweis auf eine Folge-Episode der Fortsetzungsliste.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `MediaId` | `long` (required) | Episoden-Id |
| `Title` | `string` (required) | Anzeigetitel |

## `PlaybackSource`

Datei: `VideoPlayer.Mvu/PlaybackSource.cs`

Aufgelöste Wiedergabequelle (Rückgabe von `PlaybackSourceService.ResolveAsync`).

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Kind` | `PlaybackSourceKind` (required) | `ServerStream` (Stream-URL) oder `LocalFile` (Download-Datei) |
| `Location` | `string` (required) | Stream-URL (inkl. Zugriffstoken) bzw. Dateipfad |
| `ResumePosition` | `TimeSpan?` | Fortsetzungsposition beim Start; `null` = von Anfang an |

## `MediaDetailsState`

Datei: `VideoPlayer.Mvu/MediaDetailsState.cs`

Zustand der Detailseite (`AppModel.MediaDetails`, `AppModel.cs:185`).

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Empty` | `static MediaDetailsState` | Ausgangszustand |
| `MediaType` | `string?` | Angefragter Medientyp (normalisiert) |
| `MediaId` | `long` | Angefragte Medien-Id |
| `IsLoading` | `bool` | Details werden geladen |
| `Attempted` | `bool` | Mindestens eine Anfrage gestellt |
| `Error` | `string?` | Fehlertext des letzten Ladeversuchs |
| `IsFavoriteUpdating` | `bool` | Favoriten-Umschaltung läuft |
| `FavoriteError` | `string?` | Fehler der Favoriten-Umschaltung |
| `DownloadNotice` / `DownloadNoticeIsError` | `string?` / `bool` | Hinweis der letzten Download-Anforderung |
| `PlaylistTargets` | `IReadOnlyList<PlaylistListItem>?` | Eigene Playlists („Zu Playlist") |
| `IsLoadingPlaylistTargets` | `bool` | Ziele werden geladen |
| `PlaylistPickerPending` | `bool` | Auswahlblatt soll öffnen |
| `IsAddingToPlaylist` | `bool` | Hinzufügen läuft |
| `AddToPlaylistNotice` / `AddToPlaylistNoticeIsError` | `string?` / `bool` | Hinweis der letzten Hinzufügung |
| `Details` | `MediaDetailsView?` | Geladenes Anzeigemodell; `null` während des Ladens / bei Fehler |
| `HeaderImageBytes` | `byte[]?` | Kopfbereichs-Bild (flüchtig) |
| `IsHeaderImageLoading` | `bool` | Kopfbild lädt |
| `HasHeaderImage` / `HasDetails` / `HasError` | `bool` (abgeleitet) | Anzeige-Hilfen |

## `MediaDetailsView`

Datei: `VideoPlayer.Mvu/MediaDetailsView.cs`

Anzeigemodell der Detailseite — FA-4: Zielansicht der Weiterschauen-Navigation; liefert über `Seasons` auch den Serienkontext für `NextEpisodeQueue` (`AppUpdate.cs:3623`).

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `MediaType` / `MediaId` | `string` / `long` (required) | Identität des navigierten Mediums |
| `Title` | `string` (required) | Anzeigetitel (bei Serienkontext der Serienname) |
| `Plot` / `GenreNames` / `ReleaseYear` | `string?` / `string?` / `int?` | Inhaltliche Angaben |
| `BannerPictureId` / `PosterPictureId` | `long?` | Bild-Ids |
| `IsFavorite` | `bool` | Favoriten-Markierung |
| `HasLocalFile` | `bool` | Lokale Download-Datei vorhanden (nur Offline-Snapshot) |
| `IsOfflineSnapshot` | `bool` | Ansicht aus lokalen Daten ohne Server gebaut |
| `ShowId` | `long?` | Id der angezeigten Serie (Episoden-/Staffel-Kontext) |
| `Seasons` | `IReadOnlyList<SeasonItem>` | Staffeln der Serie (leer = kein Serienkontext) |
| `SelectedSeasonId` | `long?` | Vorgewählte Staffel; `null` = erste |
| `HighlightedEpisodeId` / `HighlightedEpisodeTitle` | `long?` / `string?` | Navigierte Episode (Markierung + Titel) |
| `SelectedEpisodeId` | `long?` | Im Kopfbereich ausgewählte Episode |
| `Movies` | `IReadOnlyList<CollectionMovieItem>` | Filme einer Kollektion |
| `HasSeasons` / `HasMovies` | `bool` (abgeleitet) | Serien-/Kollektionskontext vorhanden |
| `IsPlayable` | `bool` (abgeleitet) | `MediaType is "movie" or "tvshowepisode"` — steuert den „Abspielen"-Schalter |
| `SelectedSeason` / `SelectedSeasonEpisodes` / `SelectedEpisode` | abgeleitet | Auflösung der Vorwahl |
| `HeaderTitle` / `HeaderMeta` / `HeaderPlot` | abgeleitet | Kopfbereichs-Texte |
| `HeaderEpisodeId` / `HeaderPictureId` | `long?` (abgeleitet) | Bildquelle des Kopfbereichs |

### `SeasonItem` / `EpisodeItem` (in `MediaDetailsView.cs`)

`SeasonItem`: `Id`, `Number`, `Name`, `BannerPictureId`, `Year`, `Episodes` (`IReadOnlyList<EpisodeItem>`).
`EpisodeItem`: `Id`, `Number`, `Title`, `Plot`, `ReleaseYear`, `PosterPictureId`, `IsHighlighted`, `WatchedAt` (lokale Angesehen-Markierung, wird von `AppUpdate.MarkEpisodeWatchedInDetails` gesetzt), `HasLocalFile`, `IsPlaceholder` (Offline-Platzhalter) sowie abgeleitete `IsWatched`, `DisplayTitle`, `HasPoster`.

## `AppSettings` (relevante Eigenschaften)

Datei: `VideoPlayer.Storage/AppSettings.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `AutoResumeEnabled` | `bool` (Standard `true`) | Auto-Fortsetzung (Auto-Vorlauf) an/aus — FA-5: Schalter des Folgevideo-Starts |
| `AutoResumeRemainingSeconds` | `int` (Standard 300, Clamp 30–3600) | Restlaufzeit-Fenster des Auto-Vorlaufs im Takt-Pfad (`UpdatePlaybackProgress`) |
| `WatchedEndWindowSeconds` | `int` (Standard 300, Clamp 30–3600) | „Angesehen"-Fenster im Fortschritts-Takt |
| `AutoDeleteWatchedEpisodes` | `bool` | Gesehene Episoden-Downloads löschen |

## API-Client-Modelle (`VideoPlayer.ApiClient/Models`)

### `SourceGenres` / `Genre` (Datei: `MediaSource.cs`)

Antwort von `GET api/SourceGenres/{id}` (`GetSourceGenresAsync`).

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `SourceGenres.SourceId` | `long` | Id der Quelle |
| `SourceGenres.SourceName` | `string` | Name der Quelle |
| `SourceGenres.Genres` | `List<Genre>` | Genres der Quelle (kann leer sein — FA-1-Diagnosepunkt) |
| `Genre.Id` | `long` | Genre-Id |
| `Genre.Name` | `string` | Genre-Name |
| `Genre.IconUrl` | `string?` | Icon-URL (wird in `SourceBrowserService` nicht verwendet) |

### `MediaSource` (Datei: `MediaSource.cs`)

Medienquelle (`GET api/Sources`): `CreatedAt`, `ClassifiedAt`, `Changed`, `LastScannedAt`, `IconPictureId` — Basis `MediaEntry` (trägt `Id`, `Name` u. a.).

### `MediaItemSummary` (Datei: `Models/MediaItemSummary.cs`)

Eintrag von `GET api/items` — verwendet von `GetSourceItemsAsync` / `SourceBrowserService.LoadItemsAsync` (Felder `Id`, `Type`, `Title`, `PictureId` werden gemappt).
