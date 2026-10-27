# Inventur: Datenmodell

## `MediaEntry` (API-Client-Modell)

Datei: `VideoPlayer.ApiClient/Models/MediaEntry.cs`

Basisklasse der Medien-Hierarchie, JSON-Diskriminator `$type`
(`mediaEntry`, `movie`, `movieCollection`, `show`, `season`, `episode`).
Deserialisierung in `VideoPlayerApiClient` mit `PropertyNameCaseInsensitive`.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `long` | ID des Eintrags |
| `Name` | `string` | Anzeigename |
| `MediaSourceId` | `long` | Quell-Id |
| `ReleaseDate` | `DateTime?` | Veröffentlichungsdatum |
| `PremieredAt` / `EndedAt` | `DateTime?` | Serienzeitraum |
| `PosterPictureId` / `BannerPictureId` / `FanartPictureId` | `long?` | Bild-Ids |
| `PosterPicture` / `BannerPicture` / `FanartPicture` | `Picture?` | eingebettete Bilder |
| `IsFavorite` | `bool` | Favoriten-Markierung |
| `IsManuallyEdited` | `bool` | manuell bearbeitete Metadaten |
| ~~`WatchedAt`~~ | — | **fehlt** — Server sendet es (`MediaEntryDto.WatchedAt`, `DateTime?`), STJ ignoriert es still |

Ableitungen: `Movie` (`GenreNames`, `Plot`, `Collection`), `MovieCollection`
(`Movies`), `TvShow` (`Seasons`, `GenreNames`, `Plot`), `TvShowSeason`
(`Number`, `Show`, `Episodes`), `TvShowEpisode` (`Season`, `Number`, `Plot`,
`GeneratedBackgroundPictureId`). `Picture` ist eigenständig (kein `MediaEntry`).

Server-Quelle (Server-Repo, nur Referenz): `WatchedStatusService.EnrichAsync`
befüllt `WatchedAt` für Episoden der Serienantwort, die Einzel-Episode und
Kollektions-Filme (`ItemsController.cs` ~Z. 690-720).

## `EpisodeItem` (MVU-Anzeigemodell)

Datei: `VideoPlayer.Mvu/MediaDetailsView.cs` (Z. 33-82)

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `long` (required) | Episoden-Id |
| `Number` | `int` (required) | Episodennummer |
| `Title` | `string` (required) | Anzeigetitel |
| `Plot` | `string?` | Handlung |
| `ReleaseYear` | `int?` | Erscheinungsjahr |
| `PosterPictureId` | `long?` | Bild-Id für `api/pictures/{id}` |
| `IsHighlighted` | `bool` | navigierte Episode |
| `HasLocalFile` | `bool` | lokale Download-Datei (Offline) |
| `IsPlaceholder` | `bool` | Offline-Platzhalter |
| `ShowUnavailable` | `bool` (abgeleitet) | „Nicht heruntergeladen"-Zeile |
| `DisplayTitle` | `string` (abgeleitet) | „{Nr}. {Titel}" |
| `HasPoster` | `bool` (abgeleitet) | Bild ladbar |
| ~~`WatchedAt` / `IsWatched`~~ | — | **fehlt** |

Erzeugt wird `EpisodeItem` ausschließlich in `MediaDetailsService`
(`MapSeasons` online, `MapOfflineShow` offline) — keine weiteren
Konstruktionsstellen im Produktivcode.

## `SeasonItem` / `MediaDetailsView`

Datei: `VideoPlayer.Mvu/MediaDetailsView.cs`

- `SeasonItem.Episodes` (`IReadOnlyList<EpisodeItem>`) trägt die Episodenliste
  der Staffel-Ansicht; `MediaDetailsView.SelectedSeasonEpisodes` liefert die
  Liste der gewählten Staffel an die Seite.
- `MediaDetailsView` selbst trägt kein Watched-Feld — nicht erforderlich, das
  Badge lebt ausschließlich auf Episoden-Ebene der Liste.

## `CachedShowEpisode` (Offline-Cache)

Datei: `VideoPlayer.Mvu/ShowDetailsCache.cs` (Z. 11-21)

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `long` (required) | Episoden-Id |
| `Number` | `int` (required) | Episodennummer |
| `Title` | `string` (required) | Anzeigetitel |
| ~~`WatchedAt`~~ | — | **fehlt** — JSON-Ablage (`cached_lists`, Schlüssel `show:{id}`); fehlende Felder deserialisieren tolerant zu `null` |

## `PlayerState` (Wiedergabe-Zustand)

Datei: `VideoPlayer.Mvu/PlayerState.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `IsWatched` | `bool` | Episode/Film hat die „angesehen"-Schwelle erreicht bzw. wurde zu Ende gespielt — gesetzt in `UpdatePlaybackProgress`/`UpdatePlaybackEnded` |
| `MediaType` / `MediaId` | `string?` / `long` | Identität des gespielten Mediums |
| `IsActive` | `bool` (abgeleitet) | Wiedergabe läuft |

## Server-DTO (Referenz, Server-Repo `fd729906`)

`VideoWebPlayer.Client/Models/MediaEntryDto.cs` Z. 51:
`public DateTime? WatchedAt { get; set; }` — wird per
`WatchedStatusService.EnrichAsync` auf Episoden (Serienantwort + Einzelabruf)
und Kollektions-Filmen gesetzt. Hinweis: das gleichnamige DTO in
`VideoWebPlayer.Client` dieses Repos ist ein anderes (Legacy-Client, ohne
`WatchedAt`) und für die neue App nicht relevant.
