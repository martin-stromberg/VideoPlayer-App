# Inventar — Modelle und Zustände

## Vorhandene DTOs (VideoPlayer.ApiClient/Models)

- `Playlist`: `Id`, `Name`, `Description`, `SortMode`, `CreatedAt`, `UpdatedAt`,
  `Genres` (`Genre[]`), `AllGenreIds` (`long[]`), `GenresManuallyOverridden`,
  `CoverPictureId`, `CoverPictureIsUserUploaded`, `IsPublic`, `IsOwner`
- `PlaylistEntry`: `Id`, `PlaylistId`, `MediaType`, `MediaId`, `MediaTitle`,
  `ParentMediaType`, `ParentMediaId`, `ParentMediaTitle`, `AddedAt`,
  `ResolvedPictureId`, `IsAccessible`, `SortOrder` (`long?`), `ReleaseDate`,
  `Plot`, `EpisodeNumber`
- `GenreOption`: `Id`, `Name` — `GetGenreOptionsAsync` existiert bereits
- `PlaylistSortModeValues`: `Manual`, `ByReleaseDate`, `GetDisplayLabel`
- `PlaylistEntriesPagedResult`, `PlaylistPlaybackStart`, `PlaylistNavigationResult`

## Fehlende DTOs (Anlage erforderlich)

- `CreatePlaylistRequest { Name, Description?, SortMode }`
- `UpdatePlaylistRequest { Name, Description? }`
- `AddPlaylistEntryRequest { MediaType, MediaId }`
- `PlaylistAddResult { AddedCount, SkippedCount, Message? }`
- `PlaylistConflictResponse { IsContinueWatchingConfirmationRequired, IsLossOfDataConfirmationRequired, IsCoverDeletionConfirmationRequired }` — 409-Antwort für Entfernen- und Sortiermodus-Konflikte
- `ReorderPlaylistEntryRequest { NewSortOrder }` (für `order` und `move-between`)
- `ChangeSortModeRequest { NewSortMode, ConfirmLossOfManualOrder }`
- `SetPlaylistGenresRequest { GenreIds }`
- `SetPlaylistPublicRequest { IsPublic }`
- `PlaylistMaxSortOrderResult { MaxSortOrder }`

## Vorhandene MVU-Records (VideoPlayer.Mvu)

- `PlaylistListItem`: `PlaylistId`, `Name`, `Description`, `SortMode`,
  `CoverPictureId`, `GenreText`, `IsPublic`, `IsOwner`
  → **fehlt**: `AllGenreIds`, `GenresManuallyOverridden`
- `PlaylistEntryItem`: `EntryId`, `MediaId`, `MediaType`, `Title`,
  `ParentMediaType`, `ParentMediaId`, `ParentMediaTitle`, `ResolvedPictureId`,
  `EpisodeNumber`, `IsAccessible`, `ReleaseDate`
  → **fehlt**: `SortOrder` (für move-between-Zielermittlung)
- `PlaylistDetailsState`: `PlaylistId`, `Name`, `Description`, `SortMode`,
  `CoverPictureId`, `Entries`, `NextPage`, `HasMore`, `TotalCount`, `IsLoading`,
  `IsLoadingMore`, `Attempted`, `Error`
  → **fehlt**: `IsOwner`, `IsPublic`, `IsAdmin`, `GenresManuallyOverridden`,
  `AllGenreIds`, Verwaltungs-Flags (`IsManaging`, `ManageNotice`,
  `PendingRemoveConflict`, `PendingSortModeConflict`)
- `PlaylistsPageState`: `Items`, `IsLoading`, `Attempted`, `Error`
  → keine Änderung nötig (Anlegen über geteiltes Formular-State)
- `MediaDetailsState`: `MediaType`, `MediaId`, `IsLoading`, `Attempted`,
  `Error`, `IsFavoriteUpdating`, `FavoriteError`, `DownloadNotice`,
  `DownloadNoticeIsError`, `Details`, `HeaderImageBytes`, `IsHeaderImageLoading`
  → **fehlt**: `PlaylistTargets`, `IsLoadingPlaylistTargets`,
  `PlaylistPickerPending`, `IsAddingToPlaylist`, `AddToPlaylistNotice`,
  `AddToPlaylistNoticeIsError`
- `AppModel` → **fehlt**: `PlaylistForm` (`PlaylistFormState`),
  `PlaylistGenreEditor` (`PlaylistGenreEditorState`)

## Neue MVU-Records (Anlage erforderlich)

- `PlaylistFormState { IsOpen, EditingPlaylistId?, Name, Description, SortMode, IsSaving, Error }` —
  `EditingPlaylistId = null` = Anlage (Sortiermodus wählbar), sonst Bearbeiten
  (Sortiermodus gesperrt wie Server-UI)
- `PlaylistGenreEditorState { IsOpen, PlaylistId, Options, IsLoading, IsSaving, Error }`
- `PlaylistGenreOptionItem { Id, Name, IsSelected }`

## JWT-Claim

- `IsAdmin` (`"True"`/`"False"`) im Server-JWT (`AuthorizationTokenService.CreateToken`);
  Leseweg = `ProfileInfoService`-Payload-Dekodierung → neuer interner Helfer
  `ReadIsAdminClaim`; `PlaylistService` erhält `AppSecretsService` und bietet
  `IsCurrentUserAdminAsync()`.
