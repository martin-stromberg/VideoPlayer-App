# Modelle — API und Ablage (Schritt 8)

## `VideoPlayer.ApiClient.Models`

### `MediaSource : MediaEntry` (`Models/MediaSource.cs`)

| Mitglied | Typ | Verwendung Schritt 8 |
|----------|-----|----------------------|
| `Id`, `Name` (von `MediaEntry`) | `long`, `string` | Quellenliste |
| `IconPictureId` | `long?` | Quellen-Icon via `api/sourceicons/{id}` |
| `CreatedAt`, `ClassifiedAt`, `Changed`, `LastScannedAt` | `DateTime`/`bool` | Metadaten — nicht angezeigt |

### `SourceGenres`, `Genre`, `GenreOption` (`Models/MediaSource.cs`)

| Mitglied | Typ | Verwendung |
|----------|-----|-----------|
| `SourceGenres.SourceId`/`SourceName`/`Genres` | `long`/`string`/`List<Genre>` | `api/SourceGenres/{id}` |
| `Genre.Id`/`Name`/`IconUrl` | `long`/`string`/`string?` | Genre-Leiste (Name sichtbar) |
| `GenreOption.Id`/`Name` | `long`/`string` | `api/items/genres` (nicht benötigt) |

### `MediaItemSummary` (`Models/MediaItemSummary.cs`)

`Type` (Server-Schreibweise, z. B. `"movie"`), `Id`, `Title`,
`Description`, `Url`, `CreatedAt`, `PictureId`, `ItemCount` —
Einträge der paginierten Quellen-Liste `api/items`.

### `MediaEntry`-Hierarchie (`Models/MediaEntry.cs`)

| Typ | Relevante Mitglieder |
|-----|----------------------|
| `MediaEntry` (Basis) | `Id`, `Name`, `MediaSourceId`, `ReleaseDate`, `PremieredAt`, `EndedAt`, `PosterPictureId`, `BannerPictureId`, `FanartPictureId`, `IsFavorite` |
| `Movie` | `GenreNames`, `Plot`, `Collection` (`MovieCollection?`) |
| `MovieCollection` | `Movies` (`Movie[]`) |
| `TvShow` | `Seasons` (`TvShowSeason[]`), `GenreNames`, `Plot` |
| `TvShowSeason` | `Number`, `Show` (`TvShow?`), `Episodes` (`TvShowEpisode[]`) |
| `TvShowEpisode` | `Season` (`TvShowSeason?`), `Number`, `Plot`, `GeneratedBackgroundPictureId` |
| `Picture` | `Id`, `MediaItemId`, `Type`, `Data`, `ContentType` |

`$type`-Diskriminator: `movie`, `movieCollection`, `show`, `season`,
`episode` (belegt durch `MediaEntrySerializationTests`).

### `FavoriteEntry` (`Models/FavoriteEntry.cs`)

`Id` (Favoriten-Eintrags-Id für `remove`), `Entry` (`MediaEntry?`),
`CreatedAt`.

### `ContinueWatchingEntry` (`Models/ContinueWatching.cs`)

`MediaType` (Server-Schreibweise `movie`/`show`/`season`/`episode`/
`collection`), `Entry`, `PositionSeconds`, `DurationSeconds`, `Title`,
`PosterPictureId` — eingebettete `Entry`-Objekte tragen `Season`/`Show`
mit (Episode → Serie auflösbar).

## `VideoPlayer.Storage.Entities`

### `CachedFavoritesEntry`

| Mitglied | Typ | Konvention |
|----------|-----|-----------|
| `Key` (PK) | `string` | `"{clrtype}:{id}"` — `ServerSyncService.MediaTypeOf` |
| `VideoId` | `int` | Medium-Id |
| `VideoType` | `string` | CLR-Typname kleingeschrieben |
| `Json` | `string` | Serialisierte `FavoriteEntry` |
| `UpdatedAtUtc` | `DateTime` | Sortierung (Startseite: neueste zuerst) |

### `CachedListEntry`

`Key` (`sources`, `recent`, `continue_watching`), `Json`,
`UpdatedAtUtc` — die `sources`-Liste wird von `SyncMetadataAsync`
gefüllt; Schritt 8 lädt die Quellenliste live (Offline = Hinweis), der
Cache-Schlüssel bleibt für Metadaten-Sync bestehen.

## `VideoPlayer.Mvu`-Modelle (Bestand)

| Typ | Rolle |
|-----|-------|
| `AppModel` | Gesamtmodel; `IsOffline`, `IsOnline`, `ConnectionState`, Startseiten-Listen |
| `HomeMediaItem` | `Id`, `MediaType`, `Title`, `PosterPictureId`, `Progress` |
| `HomeLists` | `ContinueWatching`, `Favorites`, `Recent` |
| `AppPage`/`AppRoutes` | Menüseiten ↔ Routen (`sources` u. a.); `mediadetails` ohne `AppPage` |
| `AppTransition` | `(Model, Effects)`; `Unchanged` |
| `ConnectionState` | `Connecting`, `Connected`, `NeedsLogin`, `NeedsServerSetup`, `Offline` |
| `ServerEventKind` | `ContinueWatchingUpdated`, `FavoritesChanged`, `NewVideosScanned` |
