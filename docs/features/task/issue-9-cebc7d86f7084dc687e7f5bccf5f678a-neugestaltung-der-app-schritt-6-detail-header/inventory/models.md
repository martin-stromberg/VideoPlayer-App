# Bestandsaufnahme — Datenmodelle

## `MediaDetailsState`
Datei: `VideoPlayer.Mvu/MediaDetailsState.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Empty` | `MediaDetailsState` (statisch) | Ausgangszustand (kein Medium angefragt) |
| `MediaType` | `string?` | Angefragter Medientyp (normalisiert, CLR-Konvention) |
| `MediaId` | `long` | Angefragte Medien-Id |
| `IsLoading` | `bool` | Detaildaten werden geladen |
| `Attempted` | `bool` | Mindestens eine Anfrage gestellt |
| `Error` | `string?` | Fehlertext des letzten Ladeversuchs |
| `IsFavoriteUpdating` | `bool` | Favoriten-Umschaltung läuft |
| `FavoriteError` | `string?` | Fehlertext der Favoriten-Umschaltung |
| `DownloadNotice` / `DownloadNoticeIsError` | `string?` / `bool` | Hinweis der letzten Download-Anforderung |
| `Details` | `MediaDetailsView?` | Geladenes Anzeigemodell |
| `HasDetails` / `HasError` | `bool` (berechnet) | Abgeleitete Zustände |

→ Kein Header-Bildzustand vorhanden.

## `MediaDetailsView`
Datei: `VideoPlayer.Mvu/MediaDetailsView.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `MediaType` / `MediaId` | `string` / `long` | Navigiertes Medium (Favoriten-Ziel) |
| `Title` | `string` | Anzeigetitel (bei Serienkontext der Serienname) |
| `Plot` | `string?` | Handlungstext |
| `GenreNames` | `string?` | Genres (serverseitig formatiert) |
| `ReleaseYear` | `int?` | Erscheinungs-/Premierenjahr |
| `BannerPictureId` | `long?` | **bereits aufgelöste Kette** Banner → Fanart → Poster (`MapMovie`/`MapCollection`/`MapShow`; Episode ohne Serie: `GeneratedBackgroundPictureId → Banner → Poster`) |
| `PosterPictureId` | `long?` | Poster-Bild-Id |
| `IsFavorite` | `bool` | Favoriten-Markierung |
| `HasLocalFile` / `IsOfflineSnapshot` | `bool` | Offline-Snapshot-Kennzeichen |
| `ShowId` | `long?` | Angezeigte Serie bei Episoden-/Staffel-Kontext |
| `Seasons` | `IReadOnlyList<SeasonItem>` | Staffeln der Serie |
| `SelectedSeasonId` | `long?` | Vorgewählte Staffel |
| `HighlightedEpisodeId` / `HighlightedEpisodeTitle` | `long?` / `string?` | Navigierte Episode im Serienkontext |
| `Movies` | `IReadOnlyList<CollectionMovieItem>` | Filme einer Kollektion |
| `HasBanner` / `HasPoster` / `HasSeasons` / `HasMovies` | `bool` (berechnet) | Anzeige-Hilfen |
| `FavoriteStarText` | `string` (berechnet) | ★/☆ |
| `IsPlayable` | `bool` (berechnet) | `movie` oder `tvshowepisode` |
| `MediaTypeText` | `string` (berechnet) | Übersetzte Typbezeichnung |
| `SelectedSeason` / `SelectedSeasonEpisodes` | berechnet | Gewählte Staffel / deren Episoden |
| `YearGenreLine` / `HasYearGenreLine` | berechnet | „Jahr \| Genres \| Episodentitel" |

→ Header-spezifische Ableitungen (Titel/Meta/Plot/Bild-Identität je
Kontext) und eine wechselbare Episoden-Auswahl fehlen.

## `SeasonItem`
Datei: `VideoPlayer.Mvu/MediaDetailsView.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `long` | Staffel-Id |
| `Number` | `int` | Staffelnummer |
| `Name` | `string` | Anzeigename („Staffel {n}"-Fallback) |
| `Episodes` | `IReadOnlyList<EpisodeItem>` | Episoden der Staffel |

→ Keine Bild-Ids und kein Staffeljahr vorhanden.

## `EpisodeItem`
Datei: `VideoPlayer.Mvu/MediaDetailsView.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` / `Number` / `Title` | `long` / `int` / `string` | Identität und Anzeige |
| `Plot` | `string?` | Episoden-Handlung |
| `PosterPictureId` | `long?` | `GeneratedBackgroundPictureId ?? PosterPictureId` (aus `MapSeasons`) |
| `IsHighlighted` | `bool` | Navigierte Episode (zum Mapping-Zeitpunkt gesetzt) |
| `HasLocalFile` / `IsPlaceholder` | `bool` | Offline-Snapshot-Zustände |
| `ShowUnavailable` / `DisplayTitle` / `HasPoster` | berechnet | Anzeige-Hilfen |

→ Kein Erscheinungsjahr vorhanden.

## `CollectionMovieItem`
Datei: `VideoPlayer.Mvu/MediaDetailsView.cs`

`Id`, `Title`, `Plot`, `GenreNames`, `ReleaseYear`, `PosterPictureId`,
`IsFavorite`, `HasPoster` — für den Header nicht erweiterungsbedürftig.

## `HomeHeroState` (Muster aus Schritt 5)
Datei: `VideoPlayer.Mvu/HomeHeroState.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Empty` | `HomeHeroState` (statisch) | Ausgangszustand |
| `IsLoading` | `bool` | Hero-Bild wird geladen |
| `ImageBytes` | `byte[]?` | Bilddaten flüchtig im Model |
| `HasImage` | `bool` (berechnet) | Bild liegt vor |

## API-Modelle (`VideoPlayer.ApiClient/Models/MediaEntry.cs`)

| Klasse | Bild-Eigenschaften |
|--------|---------------------|
| `MediaEntry` (Basis) | `PosterPictureId`, `BannerPictureId`, `FanartPictureId`, `PosterPicture`, `BannerPicture`, `FanartPicture`, `ReleaseDate`, `PremieredAt` |
| `Movie` | + `GenreNames`, `Plot`, `Collection` |
| `MovieCollection` | + `Movies` |
| `TvShow` | + `Seasons`, `GenreNames`, `Plot` |
| `TvShowSeason` | + `Number`, `Show`, `Episodes` (erbt alle Bild-Ids; **kein** `Plot`) |
| `TvShowEpisode` | + `Season`, `Number`, `Plot`, **`GeneratedBackgroundPictureId`** |

→ `GeneratedBackgroundPictureId` existiert nur bei Episoden; der
serverseitige Endpunkt `api/episodes/{id}/background-image` liefert
generiertes Bild mit Banner/Fanart-Fallback (`EpisodesController`).
