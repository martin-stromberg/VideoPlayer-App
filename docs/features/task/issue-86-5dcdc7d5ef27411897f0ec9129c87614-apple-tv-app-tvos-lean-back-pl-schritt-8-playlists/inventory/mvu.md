# Bestandsaufnahme — plattformneutraler MVU-Kern (`VideoPlayer.Mvu`)

## Zustände

- `AppModel.PlaylistsPage` (`PlaylistsPageState`, `PlaylistsPageState.cs`):
  flache Zeilenliste `Items` aus Sektions-Headern
  (`PlaylistOverviewItem.Header`, Titel „Meine Playlists"/„Öffentliche
  Playlists") und Eintragszeilen (`PlaylistOverviewItem.Entry`,
  `PlaylistListItem`); `IsLoading`, `Attempted`, `Error`, `ShowEmpty`,
  `HasItems`. Reines Online-Feature — `UpdatePlaylistsPageOpened`
  setzt offline sofort einen Fehlerzustand (`PlaylistsOfflineError`).
- `AppModel.PlaylistDetails` (`PlaylistDetailsState`,
  `PlaylistDetailsState.cs`): `PlaylistId`, `Name`, `Description`,
  `SortMode`/`SortModeText`, `CoverPictureId`/`CoverPlaylistId`,
  `GenreText`, `Entries` (kumuliert), `NextPage`, `HasMore`,
  `TotalCount`, `IsLoading`, `IsLoadingMore`, `Attempted`, `Error`,
  `SummaryText`, `ShowEmpty`. `PageSize` = 20.
- `PlaylistEntryItem`: `EntryId`, `MediaId`, `MediaType` (normalisiert),
  `Title`, `ParentMediaType`/`ParentMediaId`/`ParentMediaTitle`,
  `ResolvedPictureId` (direkt an `api/pictures/{id}`), `EpisodeNumber`,
  `IsAccessible`, `ReleaseDate`, `SortOrder`, `Subtitle`
  („Serie · Episode N" bzw. Elterntitel bzw. Medientyp-Text),
  `MediaTypeText`.

## Messages (`AppMessage.cs`)

Lesend: `PlaylistsPageOpenedMessage`,
`PlaylistsLoadedMessage(Own, Public, Error)`,
`PlaylistSelectedMessage(Item)`,
`PlaylistDetailsOpenedMessage(PlaylistId, Name)`,
`PlaylistDetailsLoadedMessage(PlaylistId, Info, IsAdmin)`,
`PlaylistEntriesLoadedMessage(PlaylistId, Page, Items, HasMore, TotalCount, Append, Error)`,
`PlaylistEntriesNextPageRequestedMessage`,
`PlaylistEntriesRetryRequestedMessage`,
`PlaylistEntrySelectedMessage(Item)`,
`PlaylistPlaybackStartRequestedMessage(PlaylistId, EntryId?)`,
`PlaylistPlaybackStartedMessage(PlaylistId, Start?, Error)`,
`PlaylistNextRequestedMessage`/`PlaylistPreviousRequestedMessage`,
`PlaylistNavigationCompletedMessage(...)`.

Verwaltung (ausgeschlossen): `PlaylistCreateRequestedMessage`,
`PlaylistEditRequestedMessage`, Formular-/Lösch-/Verschiebe-/
Genre-/Öffentlich-Messages — ohne tvOS-Oberfläche und ohne
Effekt-Verdrahtung.

## Update-Übergänge (`AppUpdate.cs`, Z. 2134–2609)

- `PlaylistsPageOpenedMessage` → `IsLoading` + `LoadPlaylistsEffect`
  (offline: direkter Fehlerstand).
- `PlaylistSelectedMessage` → `PlaylistDetails = NewPlaylistDetails(item)`
  + `NavigateToPlaylistDetailsEffect` + `LoadPlaylistDetailsEffect`
  + `LoadPlaylistEntriesEffect(id, 1, Append: false)` — nur online.
- `PlaylistDetailsOpenedMessage` → Wiederherstellungs-Lauf
  (Kopfdaten + Seite 1), identitätsgededuppt.
- `PlaylistEntriesNextPageRequestedMessage` →
  `LoadPlaylistEntriesEffect(id, NextPage, Append: true)`
  (deduppt über `IsLoadingMore`/`HasMore`).
- `PlaylistEntriesRetryRequestedMessage` → Seite 1 bzw. Folgeseite erneut.
- `PlaylistEntrySelectedMessage` → `StartPlaylistPlaybackEffect` für
  zugängliche `movie`/`tvshowepisode`-Einträge; sonst
  `NavigateToMediaDetailsEffect` mit Show-/Season-Hints.
- `PlaylistPlaybackStartRequestedMessage` → `StartPlaylistPlaybackEffect`.
- `PlaylistPlaybackStartedMessage` → `PlayerState` mit Playlist-Kontext
  + `NavigateToPlayerEffect` + `ResolvePlaybackSourceEffect`.
- `PlaylistNext/PreviousRequestedMessage` → next/previous-Effekte;
  `PlaylistNavigationCompletedMessage` → Folgeeintrag + Quellauflösung.

Fehlerkonstanten: `AppUpdate.PlaylistsLoadError`,
`AppUpdate.PlaylistEntriesLoadError`.

## Dienste (`PlaylistService.cs`)

Lesend: `LoadPlaylistsAsync` (eigene + öffentliche),
`LoadPlaylistAsync` (Kopfdaten), `LoadEntriesAsync(id, page)`
(`entries/paged`, PageSize 20), `StartPlaybackAsync(id, entryId?)`
(`play`), `GetNextEntryAsync`, `GetPreviousEntryAsync`, `AdvanceAsync`,
`IsCurrentUserAdminAsync` (Claim-Lesung für den Öffentlich-Schalter —
auf tvOS ohne Oberfläche, die Nachricht trägt `IsAdmin` trotzdem).

Nicht verdrahten: `CreatePlaylistAsync`, `UpdatePlaylistAsync`,
`DeletePlaylistAsync`, Eintrags-/Genre-/Öffentlich-Methoden,
`LoadGenreOptionsAsync`, `LoadOwnPlaylistsAsync`.
