# Bestand: Detailseiten-Kern in `VideoPlayer.Mvu`

## `MediaDetailsService` (`MediaDetailsService.cs`)

- `LoadAsync(mediaType, mediaId, fallbackTitle, showId, seasonId, episodeId, ct)`:
  liefert `MediaDetailsView`. Online über `AuthSessionService.Client`
  (`GetMovieAsync`, `GetMovieCollectionAsync` mit 404-Fallback auf Film,
  `GetTvShowAsync` mit `ShowDetailsCache`-Ablage, `GetTvShowEpisodeAsync`
  mit eingebettetem `Season.Show`-Bezug).
- Offline (`client is null`): `LoadOfflineAsync` baut den Snapshot aus
  Download-Manifest (`DownloadManifestEntry`), `ShowDetailsCache`
  (`CachedShowEntry`) und Favoriten-Kopie (`CachedFavoritesEntry`).
  `movie`/`tvshow`/`tvshowseason`/`tvshowepisode` unterstützt;
  `moviecollection` offline nicht.
- `HasLocalFile` über Manifest-Eintrag + `DownloadState.Completed` +
  `fileExists(DownloadManifestPaths.Resolve(layout, entry))`.
- Konstruktor: `(AuthSessionService, CacheDatabase, AppStorageLayout,
  ILogger?)` — interner Ctor mit `Func<string,bool> fileExists` für Tests.
- Befüllt `WatchedAt` nur auf `EpisodeItem` (Serienantwort/Cache) — das
  `MediaDetailsView` selbst trägt keinen Gesehen-Zeitpunkt.

## `MediaDetailsView` (`MediaDetailsView.cs`)

- Identität: `MediaType`, `MediaId`, `Title`; Metadaten: `Plot`,
  `GenreNames`, `ReleaseYear`, `BannerPictureId`, `PosterPictureId`,
  `IsFavorite`, `HasLocalFile`, `IsOfflineSnapshot`.
- Serienkontext: `ShowId`, `Seasons` (`SeasonItem`: `Id`, `Number`,
  `Name`, `BannerPictureId`, `Year`, `Episodes`), `SelectedSeasonId`,
  `HighlightedEpisodeId`, `HighlightedEpisodeTitle`, `SelectedEpisodeId`,
  abgeleitet: `SelectedSeason`, `SelectedSeasonEpisodes`,
  `SelectedEpisode`, `HeaderEpisodeId`, `HeaderPictureId`, `HeaderTitle`,
  `HeaderMeta`, `HeaderPlot`, `YearGenreLine`.
- `EpisodeItem`: `Id`, `Number`, `Title`, `Plot`, `ReleaseYear`,
  `PosterPictureId` (Still), `IsHighlighted`, `WatchedAt`/`IsWatched`,
  `HasLocalFile`, `IsPlaceholder`/`ShowUnavailable`, `DisplayTitle`,
  `HasPoster`.
- Kollektion: `Movies` (`CollectionMovieItem`: `Id`, `Title`, `Plot`,
  `GenreNames`, `ReleaseYear`, `PosterPictureId`, `IsFavorite`,
  `HasPoster`).
- Aktionen: `IsPlayable` (`movie`/`tvshowepisode`), `FavoriteStarText`,
  `MediaTypeText`.

## State + Messages + Effekte

- `MediaDetailsState` (`AppModel.MediaDetails`): `MediaType`, `MediaId`,
  `IsLoading`, `Attempted`, `Error`, `IsFavoriteUpdating`,
  `FavoriteError`, `DownloadNotice`(+`IsError`), `Details`,
  `HeaderImageBytes`, `IsHeaderImageLoading`, `HasHeaderImage`,
  `HasDetails`, `HasError` (plus Playlist-Felder — auf tvOS nicht
  verdrahtet).
- Messages (`AppMessage.cs`):
  `MediaDetailsRequestedMessage` (Identität + Hints, Seiten-Aufruf),
  `MediaDetailsLoadedMessage`, `MediaDetailsHeaderImageLoadedMessage`,
  `SeasonSelectedMessage`, `FavoriteToggleRequestedMessage`,
  `FavoriteToggleCompletedMessage`, `PlayRequestedMessage`,
  `PlaybackSourceResolvedMessage`, `DownloadRequestedMessage`
  (mit `ShowId`, `ShowTitle`, `SeasonNumber`, `EpisodeNumber`,
  `PictureId`), `DownloadEnqueueCompletedMessage`.
- Effekte (`AppEffect.cs`): `NavigateToMediaDetailsEffect` (Identität +
  Hints), `LoadMediaDetailsEffect`, `LoadMediaDetailsHeaderEffect`,
  `ToggleFavoriteEffect`, `EnqueueDownloadEffect`
  (MediaType/MediaId/Title/Settings + Serienkontext-Properties),
  `NavigateToPlayerEffect`, `ResolvePlaybackSourceEffect`.

## Reducer (`AppUpdate.cs`)

- `MediaDetailsRequestedMessage` (~1802): Dedupe bei gleicher Identität
  + ladend/geladen; sonst `LoadMediaDetailsEffect`.
- `MediaDetailsLoadedMessage` (~1847): Details/Error setzen; stößt
  `WithDetailsHeaderLoad` an (nur `Connected`, Bildquelle vorhanden).
- `SeasonSelectedMessage` (~1937): `SelectedSeasonId` +
  `SelectedEpisodeId` = erste Episode; Kopfbild-Nachlauf bei
  geänderter Quelle.
- `FavoriteToggleRequestedMessage` (~1982): `ToggleFavoriteEffect`;
  `FavoriteToggleCompletedMessage` (~1995): `IsFavorite` in Details
  spiegeln bzw. `FavoriteError`.
- `PlayRequestedMessage` (~3625): nur `movie`/`tvshowepisode` →
  `PlayerState` + `NavigateToPlayerEffect` + `ResolvePlaybackSourceEffect`
  (+ `NextEpisodeQueue` aus dem Detail-Serienkontext).
- `DownloadRequestedMessage` (~4238): nur `movie`/`tvshowepisode`,
  offline → `DownloadNotice`-Fehler; sonst `EnqueueDownloadEffect`.
- `DownloadEnqueueCompletedMessage` (~4274): `DownloadNotice` im
  Detail-State (nur bei Identitäts-Match) + `Downloads.Items`-Upsert.
- `MediaItemSelectedMessage` (~1360): `NavigateToMediaDetailsEffect`
  aus `HomeMediaItem` (inkl. ShowId/SeasonId/EpisodeId-Hints).
