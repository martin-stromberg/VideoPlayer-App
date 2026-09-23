# Modelle und Entitäten

## API-Modelle (`VideoPlayer.ApiClient/Models/`)

### `ContinueWatchingEntry`
Datei: `VideoPlayer.ApiClient/Models/ContinueWatching.cs`

| Eigenschaft | Typ | Bedeutung |
|-------------|-----|-----------|
| `MediaType` | `string` | Serverseitiger Medientyp (z. B. `movie`, `tvshowepisode`) |
| `Entry` | `MediaEntry?` | Das Medium selbst (polymorph) |
| `PositionSeconds` | `long` | Wiedergabeposition |
| `DurationSeconds` | `long?` | Gesamtdauer |
| `Title` | `string` | Anzeigetitel des Eintrags |
| `PosterPictureId` | `long?` | Bild-Id für `api/pictures/{id}` |

Wird über `VideoPlayerApiClient.GetContinueWatchingAsync` (`GET api/continue-watching`)
geladen; gecacht als JSON unter `cached_lists`-Schlüssel `continue_watching`.

### `FavoriteEntry`
Datei: `VideoPlayer.ApiClient/Models/FavoriteEntry.cs`

| Eigenschaft | Typ | Bedeutung |
|-------------|-----|-----------|
| `Id` | `long` | Favoriten-Id (nicht die Medien-Id) |
| `Entry` | `MediaEntry?` | Das favorisierte Medium |
| `CreatedAt` | `DateTime` | Zeitpunkt der Markierung |

Geladen via `GetFavoritesAsync` (`GET api/favorites`); gecacht als
`CachedFavoritesEntry`-Zeilen (ein Eintrag je Favorit, `Json` = serialisierter
`FavoriteEntry`).

### `RecentEntry`
Datei: `VideoPlayer.ApiClient/Models/RecentEntry.cs`

| Eigenschaft | Typ | Bedeutung |
|-------------|-----|-----------|
| `Id` | `long` | Eintrags-Id |
| `MediaSourceId` | `long` | Zugehörige Quelle |
| `PublishedAt` | `DateTime` | Veröffentlichungsdatum |
| `CreatedAt` | `DateTime?` | Einstelldatum |
| `Type` | `RecentEntryKind` | Art des Inhalts |
| `Entry` | `MediaEntry?` | Das Medium |

Geladen via `GetRecentEntriesAsync` (`GET api/items/recent`); gecacht unter
`cached_lists`-Schlüssel `recent`.

### `MediaEntry` (polymorphe Hierarchie)
Datei: `VideoPlayer.ApiClient/Models/MediaEntry.cs`

`[JsonPolymorphic("$type")]` mit Diskriminatoren `mediaEntry`, `movie`,
`movieCollection`, `show`, `season`, `episode` → CLR-Typen `MediaEntry`,
`Movie`, `MovieCollection`, `TvShow`, `TvShowSeason`, `TvShowEpisode`.
Serialisierung mit Default-Optionen schreibt den Diskriminator mit — die
gecachten JSON-Payloads sind damit verlustfrei deserialisierbar.

Gemeinsame Eigenschaften: `Id`, `Name`, `MediaSourceId`, `ReleaseDate`,
`PosterPictureId`, `BannerPictureId`, `FanartPictureId`, `IsFavorite` u. a.
`TvShowEpisode` besitzt zusätzlich `Season` (`TvShowSeason`, mit `Show`),
`Number`, `Plot`, `GeneratedBackgroundPictureId`; `TvShowSeason` besitzt
`Number`, `Show`, `Episodes`; `Movie` besitzt `Collection`
(`MovieCollection`), `GenreNames`, `Plot`.

## Ablage-Entitäten (`VideoPlayer.Storage/Entities/`)

### `CachedListEntry` (Tabelle `cached_lists`)
Datei: `VideoPlayer.Storage/Entities/CachedListEntry.cs`

| Spalte | Typ | Bedeutung |
|--------|-----|-----------|
| `Key` | `string` (PK) | Cache-Schlüssel (`sources`, `recent`, `continue_watching` — Konstanten in `ServerSyncService`) |
| `Json` | `string` | Serialisierte Liste (`JsonSerializer.Serialize(payload)`) |
| `UpdatedAtUtc` | `DateTime` | Schreibzeitpunkt |

### `CachedFavoritesEntry` (Tabelle `cached_favorites`)
Datei: `VideoPlayer.Storage/Entities/CachedFavoritesEntry.cs`

| Spalte | Typ | Bedeutung |
|--------|-----|-----------|
| `Key` | `string` (PK) | `{videoType}:{videoId}` (Kleinschreibung des CLR-Typnamens) |
| `VideoId` | `int` | Medien-Id |
| `VideoType` | `string` | CLR-Typname kleingeschrieben (`movie`, `tvshow`, …) |
| `Json` | `string` | Serialisierter `FavoriteEntry` |
| `UpdatedAtUtc` | `DateTime` | Schreibzeitpunkt |

## Ablageorte (`AppStorageLayout`)

- `CacheDatabasePath` → `NoBackupDirectory/videoplayer-cache.db3`
- `PreviewsDirectory` → `NoBackupDirectory/previews` — für
  Vorschaubilder vorgesehen, wird bereits angelegt, noch ungenutzt.
  Nicht backup-pflichtig (iOS/MacCatalyst: `NSFileManager.SetSkipBackupAttribute`).
