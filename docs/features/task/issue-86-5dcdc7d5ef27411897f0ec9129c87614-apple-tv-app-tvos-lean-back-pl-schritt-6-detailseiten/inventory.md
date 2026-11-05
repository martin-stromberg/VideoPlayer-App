# Bestandsaufnahme

Feature: tvOS-Detailansichten (Schritt 6)
Branch: `task/issue-86-5dcdc7d5ef27411897f0ec9129c87614-apple-tv-app-tvos-lean-back-pl-schritt-6-detailseiten`

## Übersicht

Der plattformneutrale Detailseiten-Stack (`VideoPlayer.Mvu`) ist
vollständig vorhanden: `MediaDetailsService` (online + Offline-Snapshot),
`MediaDetailsView` (anzeigefertiges Modell inkl. Staffeln/Episoden/
Kollektions-Filme), `MediaDetailsState` im `AppModel` sowie alle Messages
und Effekte des Detail-Lebenszyklus. Der tvOS-Seitenrand fehlt noch:
`NavigateToMediaDetailsEffect` zeigt aktuell `PreparedTargetViewController`,
die Detail-Ladeeffekte (`LoadMediaDetailsEffect`,
`LoadMediaDetailsHeaderEffect`, `ToggleFavoriteEffect`,
`EnqueueDownloadEffect`) laufen im `TvosAppEffectHandler` ins protokollierte
No-Op, und `MediaDetailsService`/`FavoritesService` sind in
`AppComposition` nicht komponiert.

## Detaildokumente

- [inventory/mvu-details.md](inventory/mvu-details.md) — Detailseiten-Kern
  in `VideoPlayer.Mvu` (Service, View-Modell, State, Messages/Effekte,
  Reducer-Verhalten).
- [inventory/maui-details-referenz.md](inventory/maui-details-referenz.md) —
  Umsetzungsmuster der MAUI-Detailseite und des Shell-Effekt-Handlers.
- [inventory/tvos-rahmen.md](inventory/tvos-rahmen.md) — tvOS-Navigator,
  modale Seiten, Controls, Poster-Dienst, Kompositionswurzel,
  Effekt-Handler.
- [inventory/tests-ci.md](inventory/tests-ci.md) — Contract-Tests und
  CI-Kontext (`build-tvos`).

## Wesentliche Befunde

1. `NavigateToMediaDetailsEffect` ist im `TvosAppEffectHandler`
   (Zeile ~172) auf `navigator.PresentPreparedTarget` verdrahtet —
   ersetzbar durch eine echte Detailseite, ohne die übrige
   Navigation zu ändern.
2. `MediaDetailsService` benötigt `AuthSessionService`, `CacheDatabase`,
   `AppStorageLayout` — alles in `AppComposition` vorhanden; der Dienst
   ist noch nicht komponiert.
3. `FavoritesService` benötigt `AuthSessionService` + `ServerSyncService`
   — beide vorhanden; Dienst nicht komponiert.
4. `MediaDetailsView` trägt keinen Gesehen-Zeitpunkt des Mediums selbst
   (nur `EpisodeItem.WatchedAt`) — für die „Gesehen"-Kennzeichnung von
   Film/Eigen-Episode ist eine kleine Erweiterung sinnvoll
   (`WatchedAt`/`IsWatched`, aus `MediaEntry.WatchedAt` befüllt).
5. `NavigateToPlayerEffect` und `StartPlaylistPlaybackEffect` sind bereits
   auf `PresentPreparedTarget` verdrahtet — `PlayRequestedMessage` kann
   von der Detailseite aus direkt dispatched werden (vorbereiteter
   Wiedergabeeinstieg ohne Fehler).
6. `EnqueueDownloadEffect` ist nicht verdrahtet; als vorbereitete Aktion
   kann der Handler direkt eine `DownloadEnqueueCompletedMessage` mit
   Hinweistext melden (bestehender Hinweis-Pfad `DownloadNotice`).
