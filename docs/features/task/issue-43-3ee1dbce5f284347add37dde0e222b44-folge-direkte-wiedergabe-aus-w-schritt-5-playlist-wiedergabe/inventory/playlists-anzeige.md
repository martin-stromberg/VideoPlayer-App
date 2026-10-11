# Bestehender Playlist-Anzeige-Pfad (Schritt 4, gemerged)

## Datenfluss

- `VideoPlayerApiClient` (`VideoPlayer.ApiClient/VideoPlayerApiClient.cs`, `#region Playlists` ab :524):
  `GetPlaylistsAsync` (`api/playlists`), `GetPublicPlaylistsAsync` (`api/playlists/public`),
  `GetPlaylistAsync` (`api/playlists/{id}`, 404 → `null`),
  `GetPlaylistEntriesPagedAsync` (`api/playlists/{id}/entries/paged?pageNumber=&pageSize=`),
  `GetPlaylistCoverAsync` (`api/playlists/{id}/cover`, 404 → `null`).
  **Keine Play-Endpunkte vorhanden.**
- DTOs: `VideoPlayer.ApiClient/Models/Playlist.cs`, `PlaylistEntry.cs` (wortgenau zu `DtoPlaylistEntry`), `PlaylistEntriesPagedResult` (in `Playlist.cs` oder eigener Datei — mit `Entries`, `TotalCount`, `HasNextPage`, `PageNumber`, `PageSize`), `PlaylistSortModeValues`.
- `PlaylistService` (`VideoPlayer.Mvu/PlaylistService.cs`): `LoadPlaylistsAsync` → `PlaylistsOverview`, `LoadPlaylistAsync` → `PlaylistListItem?`, `LoadEntriesAsync` → `PlaylistEntriesPage` (mappt `PlaylistEntry` → `PlaylistEntryItem`, filtert `MediaId <= 0`/`MediaType` leer, normalisiert Typen via `MediaTypeNames.Normalize`).
- `PlaylistEntryItem` (`VideoPlayer.Mvu/PlaylistDetailsState.cs:10`): `EntryId`, `MediaId`, `MediaType` (CLR-Konvention), `Title`, `ParentMediaType/Id/Title`, `ResolvedPictureId`, `EpisodeNumber`, `IsAccessible`, `ReleaseDate` + Anzeige-Properties.
- `PlaylistDetailsState` (`PlaylistDetailsState.cs:87`): `PlaylistId`, `Name`, `Description`, `SortMode`, `CoverPictureId`, `Entries`, `NextPage`, `HasMore`, `TotalCount`, `IsLoading`/`IsLoadingMore`/`Attempted`/`Error`, `PageSize = 20`.

## MVU-Nachrichten/-Effekte

- Messages: `PlaylistSelectedMessage` (`AppMessage.cs:502`), `PlaylistDetailsOpenedMessage` (:513), `PlaylistDetailsLoadedMessage` (:524), `PlaylistEntriesLoadedMessage` (:535), `PlaylistEntriesNextPageRequestedMessage` (:545), `PlaylistEntriesRetryRequestedMessage` (:548), `PlaylistEntrySelectedMessage` (:553).
- Effects: `LoadPlaylistsEffect` (`AppEffect.cs:321`), `NavigateToPlaylistDetailsEffect` (:330), `LoadPlaylistDetailsEffect` (:341), `LoadPlaylistEntriesEffect` (:352).
- `UpdatePlaylistEntrySelected` (`AppUpdate.cs:2232`): öffnet die **Medien-Detailseite** mit Show-/Season-Hints aus `ParentMedia*` — XML-Doku der Message sagt ausdrücklich „(Wiedergabe gehört zum Folgeschritt)" → in diesem Schritt zu ändern.

## UI

- `Pages/PlaylistsPage.xaml(.cs)` — Übersicht eigene/öffentliche Playlists.
- `Pages/PlaylistDetailsPage.xaml(.cs)` — Kopfbereich (Cover, Name, Beschreibung, `SummaryText`), `CollectionView` mit Eintragskarten (Poster via `CachedPosterImage`, Titel, `Subtitle`, `MediaTypeText`), Paging über `RemainingItemsThreshold`, Fehler-Panels + Retry. **Kein Abspielen-Element**; `OnEntrySelected` → `PlaylistEntrySelectedMessage`.
- Shell-Route `playlistdetails?id=…&name=…` (`AppRoutes`, `NavigateToPlaylistDetailsEffect`).

## MockVideoServer (E2E)

- `MockPlaylist`/`MockPlaylistEntry` (`MockVideoServer.cs:1474`, `:1507`): `Id`, `Name`, `Description`, `SortMode`, `CoverPictureId`, `CoverPng`, `Genres`, `IsOwner`, `IsPublic`, `Entries` / `Id`, `MediaType` (CLR-Schreibweise, z. B. `"Movie"`, `"TVShowEpisode"`), `MediaId`, `Title`, `ParentMediaType/Id/Title`, `ResolvedPictureId`, `EpisodeNumber`, `IsAccessible`.
- `HandlePlaylistRequest` (`MockVideoServer.cs:628`): **nur GET** — Liste, public, `{id}`, `{id}/cover`, `{id}/entries`, `{id}/entries/paged` (1-basiert). Kein `/play`.
- Stream-Endpunkte `api/items/movie/{id}/stream` und `api/items/tvshowepisode/{id}/stream` liefern `PlaybackVideoPath` nur für die Opt-in-Ids `PlaybackMovieId` bzw. `PlaybackEpisodeId`/`PlaybackNextEpisodeId`; Playlist-Medien-Ids werden aktuell nicht bedient.
