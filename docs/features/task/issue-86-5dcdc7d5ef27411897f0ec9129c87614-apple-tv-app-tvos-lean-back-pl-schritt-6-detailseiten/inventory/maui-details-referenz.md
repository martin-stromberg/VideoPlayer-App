# Bestand: MAUI-Referenz der Detailseite

## `MediaDetailsPage` (`VideoPlayer.Maui/Pages/MediaDetailsPage.xaml.cs`)

- `OnAppearing` dispatched `MediaDetailsRequestedMessage` einmal je
  Instanz — und erneut, wenn der globale Detail-Slot inzwischen eine
  andere Identität beschreibt (Rückkehr von geschobener Detailseite).
- Identitäts-Guard: nur rendern, wenn `state.MediaType`/`MediaId` zur
  eigenen Identität passen (sonst leer).
- Kopfbereich: `HeaderTitle`, `HeaderMeta`, `HeaderPlot`,
  `HeaderImageBytes` (Referenzvergleich `appliedHeaderBytes`).
- Favoriten-Stern: `FavoriteStarText`, deaktiviert bei
  `IsFavoriteUpdating` oder `IsOfflineSnapshot`.
- „Abspielen": `IsPlayable && (!IsOfflineSnapshot || HasLocalFile)` →
  `PlayRequestedMessage(type, id, HighlightedEpisodeTitle ?? Title)`.
- „Herunterladen": `IsPlayable && !IsOfflineSnapshot` →
  `DownloadRequestedMessage` mit Serienkontext (`ShowId`, `ShowTitle`,
  `SeasonNumber`, `EpisodeNumber`, `PictureId`).
- Hinweise: `DownloadNotice`/`DownloadNoticeIsError`, `FavoriteError`,
  `Error` + Fehlerpanel.
- Staffel-Picker: `Seasons` → Auswahl `SeasonSelectedMessage` mit
  Echo-Guard (programmatische Vorwahl nicht als Benutzerwahl werten).
- Episoden-Tap (nicht `IsPlaceholder`): `MediaItemSelectedMessage` mit
  `tvshowepisode` + Show/Season/Episode-Hints → neue Detailseite.
- Episoden-„Abspielen": `PlayRequestedMessage("tvshowepisode", id, title)`.
- Kollektions-Film-Tap: `MediaItemSelectedMessage` `movie`.
- „Zu Playlist"-Aktion (PlaylistAddTargets…) — für tvOS ausgeschlossen
  (keine Playlist-Verwaltung).

## `ShellAppEffectHandler` (`VideoPlayer.Maui/Services/`)

- `LoadMediaDetailsEffect` → `MediaDetailsService.LoadAsync` →
  `MediaDetailsLoadedMessage` (Fehler → Error-Text).
- `LoadMediaDetailsHeaderEffect` → `client.GetEpisodeBackgroundImageAsync`
  bzw. `client.GetPictureAsync` → `MediaDetailsHeaderImageLoadedMessage`;
  Fehlschlag = `null` (kein Fehlerzustand).
- `ToggleFavoriteEffect` → `FavoritesService.ToggleAsync` →
  `FavoriteToggleCompletedMessage` + bei Erfolg
  `CachedListsChangedMessage` (erneuerte Favoriten-Kopie).
- `EnqueueDownloadEffect` → `DownloadService.EnqueueAsync` →
  `DownloadEnqueueCompletedMessage`.
- `FavoritesService.ToggleAsync`: `api/favorites/toggle` (polymorpher
  `MediaEntry` via `FavoritesService.CreateEntry`) + `SyncFavoritesAsync`
  (lokale Kopie autoritativ vom Serverstand).
