# Bestandsaufnahme — MAUI-Referenzverhalten

## `PlaylistsPage` (`VideoPlayer.Maui/Pages/PlaylistsPage.xaml[.cs]`)

- `OnAppearing` → `PlaylistsPageOpenedMessage` (bei jedem Erscheinen —
  lädt die Übersicht online neu; offline meldet das Update den
  Fehlerstand). „Erneut versuchen" dispatcht dieselbe Message.
- Flache `CollectionView` über `Model.PlaylistsPage.Items`:
  Header-Zeilen (Sektionstitel) und Karten mit 84×84-Cover
  (`CachedPlaylistCoverImage` → `api/playlists/{id}/cover` über
  `CoverPlaylistId`), Name, `SortModeText`, `GenreText`.
- Auswahl → `PlaylistSelectedMessage(playlist)`; Fehler-Panel und
  Leer-Hinweis wie gehabt. „Neue Playlist"-Aktion entfällt auf tvOS.

## `PlaylistDetailsPage` (`PlaylistDetailsPage.xaml[.cs]`)

- `OnAppearing` → `PlaylistDetailsOpenedMessage(id, name)` nur wenn
  `PlaylistDetails.PlaylistId != id || !Attempted`
  (Wiederherstellungspfad; Rückkehr behält den Stand).
- Kopf: Cover (`CoverPlaylistId`), Name, `SummaryText`, Beschreibung,
  `GenreText`; Aktion „Abspielen" →
  `PlaylistPlaybackStartRequestedMessage(playlistId, null)` —
  der Server löst den ersten abspielbaren Eintrag auf.
- Eintragsliste: `PlaylistEntrySelectedMessage`,
  `RemainingItemsThresholdReached` →
  `PlaylistEntriesNextPageRequestedMessage`, Fehler-Retry →
  `PlaylistEntriesRetryRequestedMessage`; Leer-/Lade-/Fehlerzustände.
- Verwaltungsaktionen (Bearbeiten/Löschen/Sortiermodus/Genre-Editor/
  Öffentlich/Verschieben/Entfernen) entfallen auf tvOS vollständig.

## `ShellAppEffectHandler` (Z. 1003–1098)

- `NavigateToPlaylistDetailsAsync`: Shell-Route
  `playlistdetails?id=…&name=…` mit Topmost-Duplikat-Guard.
- `LoadPlaylistsAsync`: `PlaylistService.LoadPlaylistsAsync` →
  `PlaylistsLoadedMessage(own, public, error)` mit
  `AppUpdate.PlaylistsLoadError`.
- `LoadPlaylistDetailsAsync`: `LoadPlaylistAsync` +
  `IsCurrentUserAdminAsync` (nicht fatal) →
  `PlaylistDetailsLoadedMessage(id, info, isAdmin)`.
- `LoadPlaylistEntriesAsync`: `LoadEntriesAsync(id, page)` →
  `PlaylistEntriesLoadedMessage(id, page.Page, items, hasMore,
  totalCount, append, error)` mit
  `AppUpdate.PlaylistEntriesLoadError`.

## Bildabrufe

- Cover: `VideoPlayerApiClient.GetPlaylistCoverAsync(playlistId)`
  (`api/playlists/{id}/cover`, `byte[]?` — `null` bei 404).
- Eintrags-Poster: `ResolvedPictureId` direkt an
  `api/pictures/{id}` → `TvosPosterImageService.GetImageAsync`.
