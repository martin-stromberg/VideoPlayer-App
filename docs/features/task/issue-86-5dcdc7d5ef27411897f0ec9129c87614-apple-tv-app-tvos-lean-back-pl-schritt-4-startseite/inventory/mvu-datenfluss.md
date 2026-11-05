# MVU-Datenfluss der Startseite — Bestandsaufnahme

## Model-Felder (`VideoPlayer.Mvu/AppModel.cs`)

| Feld | Typ | Füllquelle |
|---|---|---|
| `ContinueWatching` | `IReadOnlyList<HomeMediaItem>` | `HomeListsLoadedMessage` → `UpdateHomeListsLoaded` (AppUpdate.cs:1335) |
| `Favorites` | `IReadOnlyList<HomeMediaItem>` | dto. |
| `RecentEntries` | `IReadOnlyList<HomeMediaItem>` | dto. |
| `SeasonalGenres` | `IReadOnlyList<HomeSeasonalSection>` | dto. |
| `IsOffline` | `bool` (abgeleitet) | `ConnectionState == Offline` |
| `StorageNotice` | `string?` | `StorageRecoveredMessage` |
| `IsSyncRunning` | `bool` | Sync-Laufzustand |

## `HomeMediaItem` (`VideoPlayer.Mvu/HomeMediaItem.cs`)

`sealed record`: `Id`, `MediaType` (Server- oder CLR-Konvention, kleingeschrieben), `Title`, `PosterPictureId` (`api/pictures/{id}`, nullable), `Progress` (0–1, nur Weiterschauen), `ShowId`/`SeasonId`/`EpisodeId`, `PlaylistId`/`PlaylistName`/`PlaylistEntryId`, `IsPlaceholder` (Offline-Platzhalter). Abgeleitet: `IsSelectable => !IsPlaceholder`, `CardOpacity` (0,55/1,0), `HasPoster`, `ShowProgress`.

## `HomeSeasonalSection` (`VideoPlayer.Mvu/HomeSeasonalSection.cs`)

`GenreId`, `Name`, `ExpiresAt` (UTC-Ablaufgrenze, exklusiv), `Items` (`IReadOnlyList<HomeMediaItem>`), `AutomationId` (`SeasonalGenre{GenreId}`).

## Lade-Auslöser (alle bereits verdrahtet oder im Update enthalten)

| Auslöser | Update | Effekte |
|---|---|---|
| `HomeListsReloadRequestedMessage` | AppUpdate.cs:213 | `LoadHomeListsEffect` |
| `CachedListsChangedMessage` | AppUpdate.cs:1159 | `LoadHomeListsEffect`, `LoadSourcesEffect` (bei Verbindung), `RunAutoDownloadsEffect` (unverdrahtet → No-Op) |
| `SynchronizationCompletedMessage` | `UpdateSynchronizationCompleted` | `LoadHomeListsEffect`, Quellen-Reload |
| `ServerEventReceivedMessage` | AppUpdate.cs:1260 | je Kind: `SyncContinueWatchingEffect` / `SyncFavoritesEffect` / `SyncMetadataEffect` (nur `Connected`) |
| Login-/Sync-Abschluss | `UpdateLoginCompleted`/`UpdateAutoLoginCompleted`/`UpdateSynchronizationCompleted` | u. a. `LoadHomeListsEffect` |
| `HomeRefreshRequestedMessage` | AppUpdate.cs:214 | `RunFullSyncEffect` (nur Connected & nicht laufend) — derzeit kein UI-Auslöser auf tvOS nötig |
| `DiscardedCaches` | `UpdateHomeListsLoaded` | `RunFullSyncEffect` bei Verbindung |

`TvosAppEffectHandler.LoadHomeListsAsync` (Zeile 654): `model.IsOffline` → `OfflineContinueWatchingService.LoadHomeListsAsync`, sonst `HomeListService.LoadHomeListsAsync`; Fehler → `HomeLists.Empty` + `HomeListsLoadedMessage`. Teil-Syncs (`RunPartialSyncAsync`, Zeile 639) melden abschließend `CachedListsChangedMessage`.

## Auswahl-Nachrichten und ihre Effekte

| Message | Update | Erzeugte Effekte |
|---|---|---|
| `MediaItemSelectedMessage(HomeMediaItem)` | AppUpdate.cs:1360 | `NavigateToMediaDetailsEffect(MediaType, Id, Title, ShowId, SeasonId, EpisodeId)` — Guard: `Id > 0`, `MediaType` belegt, `IsSelectable` |
| `ContinueWatchingItemSelectedMessage(HomeMediaItem)` | AppUpdate.cs:1392 | nicht abspielbar → `NavigateToMediaDetailsEffect`; Playlist-Bezug → `StartPlaylistPlaybackEffect(PlaylistId, PlaylistEntryId)`; sonst `NavigateToMediaDetailsEffect` (außer Offline-Episode ohne ShowId) + `NavigateToPlayerEffect` + `ResolvePlaybackSourceEffect`, ggf. `LoadUpcomingEpisodesEffect` |
| `NavigateToPageEffect(AppPage)` | — | im tvOS-Handler auf `navigator.NavigateTo` verdrahtet (Zeile 86) |

MAUI-Referenz: `ShellAppEffectHandler.NavigateToMediaDetailsAsync` (Zeile 700) navigiert zur Shell-Route `mediadetails?...`; `NavigateToPlayerEffect` → Route `player`. Für tvOS existiert keine Detail-/Player-Seite (Schritte 5/6 des Projektplans).

## `MediaTypeNames.Normalize`

`AppUpdate` nutzt `MediaTypeNames.Normalize` zur Vereinheitlichung (Server-→CLR-Konvention); für die Startseiten-Anzeige selbst nicht nötig — die Eintrags-Messages tragen den Original-Typ weiter.
