# Datenmodell — Bestandsaufnahme

## Server-DTOs (Referenz `martin-stromberg/VideoPlayer`, `VideoWebPlayer.Client/Models/` — wortgenau übernommen)

### `DtoPlaylist` (Server-Repo: `VideoWebPlayer.Client/Models/DtoPlaylist.cs`)

| Eigenschaft | Typ | Beschreibung |
|-------------|-----|--------------|
| `Id` | `long` | Id der Playlist |
| `Name` | `string` | Name der Playlist |
| `Description` | `string?` | Beschreibung der Playlist |
| `SortMode` | `string` | Sortiermodus (`PlaylistSortModeValues.ByReleaseDate`/`Manual`) |
| `CreatedAt` | `DateTime` | Anlagezeitpunkt |
| `UpdatedAt` | `DateTime` | Zeitpunkt der letzten Änderung |
| `Genres` | `DtoGenreOption[]` | Anzeige-Genres (gekappte, häufigkeitssortierte Auswahl) |
| `AllGenreIds` | `long[]` | Vollständige Genre-Id-Menge (abgeleitet oder manuell) |
| `GenresManuallyOverridden` | `bool` | Manueller Genre-Override des Besitzers |
| `CoverPictureId` | `long?` | Bild-Id des Covers (Upload oder Collage); `null` = kein Cover — steuert Cover vs. Platzhalter (`api/playlists/{id}/cover`) |
| `CoverPictureIsUserUploaded` | `bool` | Cover vom Besitzer hochgeladen (`true`) vs. generiert (`false`) |
| `IsPublic` | `bool` | Öffentlich-Flag (für alle angemeldeten Nutzer sichtbar, read-only) |
| `IsOwner` | `bool` | Anfragender Nutzer ist Besitzer (Default `false`, fail-safe) |

### `DtoPlaylistEntry` (`VideoWebPlayer.Client/Models/DtoPlaylistEntry.cs`)

| Eigenschaft | Typ | Beschreibung |
|-------------|-----|--------------|
| `Id` | `long` | Eintrags-Id |
| `PlaylistId` | `long` | Zugehörige Playlist |
| `MediaType` | `string` | Typ der Referenz (`MediaTypeValues`: `Movie`, `TVShowEpisode`, `TVShowSeason`, `TVShow`, `MovieCollection`) |
| `MediaId` | `long` | Id der Referenz |
| `MediaTitle` | `string` | Titel der Referenz |
| `ParentMediaType` | `string?` | Typ des Elternelements (z. B. Serie einer Staffel) |
| `ParentMediaId` | `long?` | Id des Elternelements |
| `ParentMediaTitle` | `string?` | Titel des Elternelements |
| `AddedAt` | `DateTime` | Zeitpunkt des Hinzufügens |
| `ResolvedPictureId` | `long?` | Serverseitig aufgelöste Bild-Id (Poster→Banner→Fanart); `null` = kein Bild |
| `IsAccessible` | `bool` | Nutzer hat Zugriff (Default `true`) |
| `SortOrder` | `long?` | Manuelle Sortierung (nur bei `SortMode == Manual` belegt) |
| `ReleaseDate` | `DateTime?` | Erscheinungs-/Premierendatum |
| `Plot` | `string?` | Handlungsbeschreibung |
| `EpisodeNumber` | `int?` | Episodennummer (nur `TVShowEpisode`) |

### `DtoPlaylistEntriesPagedResult` (`VideoWebPlayer.Client/Models/DtoPlaylistEntriesPagedResult.cs`)

| Eigenschaft | Typ | Beschreibung |
|-------------|-----|--------------|
| `Entries` | `DtoPlaylistEntry[]` | Einträge der angeforderten Seite (serverseitig sortiert) |
| `TotalCount` | `int` | Gesamtzahl der Einträge über alle Seiten |
| `HasNextPage` | `bool` | Weitere Seite vorhanden |
| `PageNumber` | `int` | 1-basierte Nummer der gelieferten Seite |
| `PageSize` | `int` | Einträge pro Seite |

### `DtoGenreOption` (in `VideoWebPlayer.Client/Models/DtoMovie.cs:100`)

| Eigenschaft | Typ | Beschreibung |
|-------------|-----|--------------|
| `Id` | `long` | Genre-Id |
| `Name` | `string` | Anzeigename |

→ Deckt sich strukturell mit dem vorhandenen `VideoPlayer.ApiClient.Models.GenreOption` (`MediaSource.cs:57`) — Wiederverwendung.

### `PlaylistSortModeValues` (`VideoWebPlayer.Client/Models/PlaylistSortModeValues.cs`)

| Konstante | Wert | Bedeutung |
|-----------|------|-----------|
| `ByReleaseDate` | `"ByReleaseDate"` | Nach Erscheinungsdatum sortiert |
| `Manual` | `"Manual"` | Manuell sortiert |

Methode `GetDisplayLabel(string?)` → `"Manuell"` / `"Nach Erscheinungsdatum"` (deutsches Label; identische Anforderung an die App-Seite).

### `MediaTypeValues` (`VideoWebPlayer.Client/Models/MediaTypeValues.cs`)

String-Konstanten `Movie`, `TVShowEpisode`, `TVShowSeason`, `TVShow`, `MovieCollection` — PascalCase-Serverwerte; die App-normalisierte Form liefert `MediaTypeNames.Normalize` (kleingeschrieben, CLR-Konvention).

## Server-Endpunkte (`PlaylistsController.cs`, `[Route("api/playlists")]`)

| Route | Signatur | Verwendung im Schritt |
|-------|----------|------------------------|
| `GET api/playlists` | `[FromQuery] long? genreId` | Eigene Playlists (Liste `DtoPlaylist`) |
| `GET api/playlists/public` | `[FromQuery] long? genreId` | Öffentliche Playlists |
| `GET api/playlists/{id}` | — | Einzelne Playlist (`DtoPlaylist`, 404 wenn nicht vorhanden) |
| `GET api/playlists/{id}/entries` | — | Alle Einträge (unsortiert-Abruf; `entries/paged` ist der Anzeige-Endpunkt) |
| `GET api/playlists/{id}/entries/paged` | `pageNumber` (1-basiert, Default 1), `pageSize` (Default `PlaylistSettings.DefaultPageSize`, max `MaxPageSize`) | Paginierte Einträge in Server-Reihenfolge (`DtoPlaylistEntriesPagedResult`) |
| `GET api/playlists/{id}/cover` | — | Cover-Bilddaten; 404, wenn kein Cover gesetzt |
| (nicht in diesem Schritt) | `POST …/play`, `…/entries`-Mutationen, Cover-Verwaltung | Folgeschritte 5+ — DTO-Modelle werden nur geteilt, wenn benötigt |

## Bestehende Anzeige-Records der App (Wiederverwendungsmuster)

### `VideoPlayer.Mvu.SourceBrowserState` (Record, `SourceBrowserState.cs`)

`Empty`-Singleton, `SourceId`, `SourceName`, `Genres`, `SelectedGenreId`, `SearchText`, `Items`, `NextPage` (0-basiert), `HasMore`, `IsLoading`, `IsLoadingMore`, `Attempted`, `Error`, abgeleitet `IsActive`/`HasItems`/`HasError`/`ShowEmpty`. Seitengröße als Konstante `PageSize = 20`.

### `VideoPlayer.Mvu.FavoritesPageState` (Record, `FavoritesPageState.cs`)

`Initial`-Singleton, `IsLoading`, `Attempted`, `Error`, `Items`, abgeleitet `HasItems`/`HasError`/`ShowEmpty`. `FavoriteListItem` zeigt das Muster `required`-Initializer + abgeleitete Anzeigeeigenschaften (`MediaTypeText`, `HasPoster`).

### `VideoPlayer.Mvu.AppModel` (Record, `AppModel.cs`)

Zustandsfelder je Seite (`SourcesOverview`, `SourceBrowser`, `MediaDetails`, `FavoritesPage`, `Downloads`, `Profile`, `Player`) — neue Playlist-Zustände folgen diesem Muster.

### `VideoPlayer.Mvu.HomeMediaItem` / `SourceMediaItem` / `SourceGenreItem` / `SourceItem`

Anzeige-Records mit `Id`, `Title`, `PosterPictureId`, `MediaType` — Muster für `PlaylistListItem`/`PlaylistEntryItem`.
