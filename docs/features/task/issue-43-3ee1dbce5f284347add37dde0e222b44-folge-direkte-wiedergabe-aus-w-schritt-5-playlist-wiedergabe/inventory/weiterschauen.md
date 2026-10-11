# Weiterschauen-Datenpfad und Playlist-Bezug

## Datenmodell

- `ContinueWatchingEntry` (`VideoPlayer.ApiClient/Models/ContinueWatching.cs:6`): `MediaType`, `Entry` (`MediaEntry?`), `PositionSeconds`, `DurationSeconds`, `Title`, `PosterPictureId`. **Keine Playlist-Felder** — Server liefert `playlistId`, `playlistName`, `playlistEntryId` (siehe `server-api.md`). Deserialisierung `PropertyNameCaseInsensitive` (`VideoPlayerApiClient.cs:26`).
- `HomeMediaItem` (`VideoPlayer.Mvu/HomeMediaItem.cs:9`): `Id`, `MediaType`, `Title`, `PosterPictureId`, `Progress`, `ShowId`, `SeasonId`, `EpisodeId`, `IsPlaceholder` (+ abgeleitete Anzeige-Props). **Kein Playlist-Bezug.**

## Mapping-Pfad

- `ServerSyncService` schreibt die Server-`ContinueWatchingEntry`-Liste als JSON in `cached_lists` (`continue_watching`-Schlüssel); neue DTO-Felder werden dadurch automatisch mitgespeichert (keine Schema-Änderung nötig).
- `HomeListService.MapContinueWatching` (`HomeListService.cs:244`): `ContinueWatchingEntry` → `HomeMediaItem` (Fortschritt aus `PositionSeconds`/`DurationSeconds`, Serienhints via `MediaHints(media)`).
- `OfflineContinueWatchingService` (`OfflineContinueWatchingService.cs:93`): Offline-Liste aus `download_manifest` + Platzhalter; nutzt `ContinueWatchingEntry` nur für Fortschritt (`LoadCachedContinueWatchingAsync`, `ResolveProgress`). Playlist-Bezug offline ohnehin nicht nutzbar (Playlist = Online-Feature).

## Auswahl-Pfad (Schritt 1, gemerged)

- `ContinueWatchingItemSelectedMessage` → `UpdateContinueWatchingItemSelected` (`AppUpdate.cs:1268`):
  `movie`/`tvshowepisode` → `StartPlayback`; bei Episode + `ShowId` zusätzlich `LoadUpcomingEpisodesEffect(item.Id, item.ShowId)` → `UpcomingEpisodesService` lädt `api/items/tvshow/{showId}` online (Cache-Fallback `ShowDetailsCache`) → `UpcomingEpisodesLoadedMessage` befüllt `PlayerState.UpcomingEpisodes`.
- Kontextmenü hide/skip: `ContinueWatchingHideRequestedMessage`/`ContinueWatchingSkipRequestedMessage` → `HideContinueWatchingEntryEffect`/`SkipContinueWatchingEntryEffect` → `ContinueWatchingMutationService` (`api/continue-watching/hide|skip`) — unverändert; neue DTO-Felder beeinflussen die Mutation nicht (Endpunkte erwarten `mediaType`/`mediaId`).

## E2E-Bezug

- `MockVideoServer.ListOverride` (`MockVideoServer.cs:537`): Weiterschauen-Einträge für `movie` (Opt-in `PlaybackMovieId` + `PlaybackResumePositionSeconds`), `episode` (`PlaybackEpisodeId` mit eingebettetem `season`/`show`-Graph → ShowId-Hint) und `show` (`ContinueWatchingShowId`). Für Playlist-Bezug müsste der Episode-/Movie-Eintrag um `playlistId`/`playlistName`/`playlistEntryId` erweiterbar sein (Opt-in-Eigenschaften).
- `ContinueWatchingPlaybackTests` deckt Direktstart Film/Episode + Serienkontext ab — Muster für die neuen Playlist-E2E-Szenarien.
