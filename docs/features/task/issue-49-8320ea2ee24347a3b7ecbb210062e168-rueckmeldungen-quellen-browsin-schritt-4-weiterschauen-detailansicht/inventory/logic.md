# Logik – Weiterschauen-Auswahl und Detail-/Wiedergabe-Navigation

## `AppUpdate` (partial, MVU-Update)

Datei: `VideoPlayer.Mvu/AppUpdate.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `Update(AppModel, AppMessage)` | `public static` | Zentrale Update-Funktion; dispatcht `ContinueWatchingItemSelectedMessage` → `UpdateContinueWatchingItemSelected` (Zeile 193) |
| `UpdateContinueWatchingItemSelected(AppModel, ContinueWatchingItemSelectedMessage)` | `private static` | **Kernstelle der Anforderung** (Zeilen 1357–1401): Guard `IsSelectableEntry`; nicht abspielbare Typen → `NavigateToMediaDetailsEffect` mit `ShowId`/`SeasonId`/`EpisodeId`-Hints; Playlist-Bezug (`PlaylistId`+`PlaylistEntryId` > 0) → `StartPlaylistPlaybackEffect`; `movie`/`tvshowepisode` → `StartPlayback` (Direktwiedergabe, ohne Detailseite), bei Episode mit `ShowId` zusätzlich `LoadUpcomingEpisodesEffect` |
| `UpdateMediaItemSelected(AppModel, MediaItemSelectedMessage)` | `private static` | Auswahl regulärer Listen-Einträge → `NavigateToMediaDetailsEffect` (Zeilen 1329–1344); unverändert bleibender Referenzpfad |
| `UpdateContinueWatchingHideRequested` / `UpdateContinueWatchingSkipRequested` | `private static` | Kontextmenü „Ausblenden"/„Überspringen" (Zeilen 1412–1447) — unverändert zu lassen |
| `IsSelectableEntry(HomeMediaItem)` | `private static` | Guard: `Id > 0`, `MediaType` nicht leer, `IsSelectable` (Platzhalter ausgenommen), Zeilen 1456–1457 |
| `UpdatePlayRequested(AppModel, PlayRequestedMessage)` | `private static` | „Abspielen" der Detailseite → `StartPlayback` (Zeilen 3573–3582) — Direktwiedergabe an anderer Stelle, bleibt |
| `StartPlayback(AppModel, mediaType, mediaId, title)` | `private static` | Gemeinsamer Wiedergabestart (Zeilen 3596–3612): `PlayerState` belegen (`UpcomingEpisodes` aus `NextEpisodeQueue`), Effekte `[NavigateToPlayerEffect, ResolvePlaybackSourceEffect]` |
| `NextEpisodeQueue(AppModel, mediaId)` | `private static` | Fortsetzungsliste aus `model.MediaDetails.Details` (Serienkontext, Zeilen 3623–3645); leer, wenn keine/andere Details geladen |
| `UpdateMediaDetailsRequested` | `private static` | `MediaDetailsRequestedMessage` → `MediaDetailsState` neu belegen + `LoadMediaDetailsEffect` (Zeilen 1754–1797); Doppellauf-Guard bei identischer Identität |
| `UpdateMediaDetailsLoaded` | `private static` | Übernahme geladener Details + Kopfbild-Nachlauf (Zeilen 1799–1822) |
| `UpdatePlayerClosed(AppModel)` | `private static` | `PlayerClosedMessage` (Zeilen 3994–4016): PiP verlassen, Abschluss-Fortschritt melden, `Player = PlayerState.Empty` — Navigation selbst läuft über den Shell-Pop |
| `UpdateUpcomingEpisodesLoaded` | `private static` | `UpcomingEpisodesLoadedMessage` → `Player.UpcomingEpisodes`, wenn Player-Identität passt (siehe Tests `StaleOrOtherMedia_Discarded`) |

## `MvuHost`

Datei: `VideoPlayer.Mvu/MvuHost.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `DispatchAsync(AppMessage, CancellationToken)` | `public` | Wendet Update an, publiziert `ModelChanged`, führt Effekte **sequenziell in Listenreihenfolge** aus (Zeilen 64–67) — Grundlage der Stapel-Reihenfolge Detailseite → Player |

## `ShellAppEffectHandler` (Effektausführung am App-Rand)

Datei: `VideoPlayer.Maui/Services/ShellAppEffectHandler.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `HandleAsync(AppEffect, CancellationToken)` | `public` | Effekt-Switch; `NavigateToMediaDetailsEffect` (Z. 181–183), `NavigateToPlayerEffect` (Z. 283–285), `NavigateBackEffect` (Z. 277–279), `LoadUpcomingEpisodesEffect` (Z. 289–291), `StartPlaylistPlaybackEffect` (Z. 223–225) |
| `NavigateToMediaDetailsAsync(NavigateToMediaDetailsEffect)` | `private` | Baut Route `mediadetails?mediaType=…&id=…&title=…[&show=…&season=…&episode=…]` und pusht via `Shell.Current.GoToAsync` (Zeilen 687–719) |
| `NavigateToPlayerAsync()` | `private` | `GoToAsync("player")` — pusht die `PlayerPage` auf den Stack (Zeilen 395–410) |
| `NavigateBackAsync()` | `private` | `GoToAsync("..")` — Pop zur darunterliegenden Seite (Zeilen 1349 ff.) |
| `LoadMediaDetailsAsync(LoadMediaDetailsEffect, …)` | `private` | Ruft `MediaDetailsService.LoadAsync`, dispatcht `MediaDetailsLoadedMessage` (Zeilen 855–875) |
| `LoadUpcomingEpisodesAsync(LoadUpcomingEpisodesEffect, …)` | `private` | Lädt Serienkontext für die Fortsetzungsliste, dispatcht `UpcomingEpisodesLoadedMessage` |

## `MediaDetailsService`

Datei: `VideoPlayer.Mvu/MediaDetailsService.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `LoadAsync(mediaType, mediaId, fallbackTitle, showId, seasonId, episodeId, ct)` | `public` | Ohne angemeldeten Client → `LoadOfflineAsync`; sonst je Typ `MapMovie`/`MapShow`/`LoadSeasonInShowContextAsync`/`LoadEpisodeAsync`/Kollektion (Zeilen 77–104) |
| `LoadEpisodeAsync(…)` | `private` | Episoden-Details: Serie über `showId`-Hint oder eingebetteten `Season.Show`-Bezug auflösen → Serienansicht mit Vorwahl; ohne Serie Basisdetails der Episode (Zeilen 272–330) |
| `LoadOfflineAsync(…)` | `private` | Offline-Snapshot: `movie` aus Manifest/Titel-Fallback; `tvshowepisode` über `showId`-Hint oder `entry?.ShowId` aus dem Manifest → `ShowDetailsCache` → `MapOfflineShow`, sonst Basisdetails; `tvshow`/`tvshowseason` nur mit Serienkontext, sonst `InvalidOperationException` → Fehlertext der Seite (Zeilen 124–213) |

## `OfflineContinueWatchingService`

Datei: `VideoPlayer.Mvu/OfflineContinueWatchingService.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `LoadHomeListsAsync(CancellationToken)` | `public` | Offline-Startseiten-Listen (Weiterschauen aus Manifest, Favoriten/Neuheiten aus Cache) |
| `LoadContinueWatchingAsync(CancellationToken)` | `internal` | Nur `Completed`-Downloads mit vorhandener Datei; Episoden-Einträge tragen `ShowId = row.ShowId` und `EpisodeId = row.VideoId` (kein `SeasonId` am direkten Eintrag); Folge-Episoden ohne Download als `IsPlaceholder`-Einträge mit `ShowId`/`SeasonId`/`EpisodeId` (Zeilen 101–171) |

## `MediaDetailsPage`

Datei: `VideoPlayer.Maui/Pages/MediaDetailsPage.xaml.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `OnAppearing()` | `protected override` | ModelChanged-Abo; dispatcht einmalig `MediaDetailsRequestedMessage(mediaType, mediaId, title, showId, seasonId, episodeId)` — erneut nur bei Identitätswechsel des globalen Detail-Slots (Zeilen 82–109) |
| `OnPlayClicked` / `OnEpisodePlayClicked` | `private` | `PlayRequestedMessage` → `UpdatePlayRequested` → `StartPlayback` (Direktwiedergabe von der Detailseite, bleibt) |

## `PlayerPage`

Datei: `VideoPlayer.Maui/Pages/PlayerPage.xaml.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `OnDisappearing()` | `protected override` | Beim Verlassen (Schließen/Pop): PiP-Rücksicherung + `PlayerClosedMessage` (Zeilen 112–125); der Shell-Pop kehrt zur darunterliegenden Route zurück |

## `HomePage`

Datei: `VideoPlayer.Maui/Pages/HomePage.xaml.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `OnContinueWatchingItemTapped(object?, HomeMediaItem)` | `private` | Tap → `ContinueWatchingItemSelectedMessage` (Zeilen 110–115) |
| `OnContinueWatchingItemLongPressed(object?, HomeMediaItem)` | `private` | Kontextmenü „Ausblenden"/„Überspringen" (2-s-Long-Press) — unverändert (Zeilen 117–140) |

## Shell-Routen

Datei: `VideoPlayer.Maui/AppShell.xaml.cs`

- `mediadetails` → `MediaDetailsPage` (Z. 73), `player` → `PlayerPage` (Z. 86), beide „geschoben auf den Stack".
- `OnShellNavigated` → `AppRoutes.TryGetPage` → `NavigatedMessage`; `mediadetails`/`player` sind keine `AppPage`-Routen — `CurrentPage` bleibt auf der darunterliegenden Menüseite (z. B. `Home`).

## Relevante Nachrichten/Effekte

- `ContinueWatchingItemSelectedMessage(HomeMediaItem Item)` — `AppMessage.cs:258`
- `MediaDetailsRequestedMessage(mediaType, mediaId, title, showId, seasonId, episodeId)` — `AppMessage.cs:385`
- `PlayRequestedMessage(mediaType, mediaId, title)` — `AppMessage.cs:968`
- `PlayerClosedMessage` — `AppMessage.cs:1030`
- `UpcomingEpisodesLoadedMessage(mediaId, episodes)` — `AppMessage.cs:289`
- `NavigateToMediaDetailsEffect(MediaType, MediaId, Title, ShowId?, SeasonId?, EpisodeId?)` — `AppEffect.cs:196-202`
- `NavigateToPlayerEffect` — `AppEffect.cs:604`; `ResolvePlaybackSourceEffect`; `LoadUpcomingEpisodesEffect(MediaId, ShowId)` — `AppEffect.cs:636`; `StartPlaylistPlaybackEffect(PlaylistId, EntryId)` — `AppEffect.cs:366`
