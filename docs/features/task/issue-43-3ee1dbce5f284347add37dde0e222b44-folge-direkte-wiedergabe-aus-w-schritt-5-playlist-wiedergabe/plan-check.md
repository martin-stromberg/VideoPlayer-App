# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan vollständig

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| Wiedergabe aus Playlist-Detailansicht (ab gewähltem Eintrag bzw. erstem abspielbarem Eintrag) | `PlaylistDetailsPlayButton` → `PlaylistPlaybackStartRequestedMessage(PlaylistId, null)` → `StartPlaylistPlaybackEffect` (`entryId=null` = erster abspielbarer, Server-seitig); Eintrags-Tap auf abspielbaren+zugänglichen Eintrag → `StartPlaylistPlaybackEffect(PlaylistId, EntryId)` via geändertem `UpdatePlaylistEntrySelected` | 2 Pflicht-E2E (Button-Start inkl. nicht abspielbarem Ersteintrag, Eintrags-Tap) + Unit `PlaylistPlaybackStartRequested_DispatchesStartEffect`, `PlaylistEntrySelected_Playable_StartsPlaylistPlayback` | Abgedeckt |
| Vor-/Rückwärts-Navigation in laufender Playlist-Wiedergabe | `PlayerPreviousButton`/`PlayerNextButton` (nur bei `IsPlaylistPlayback`, `CanNavigate*`-Heuristik) → `LoadPreviousPlaylistEntryEffect`/`LoadNextPlaylistEntryEffect` → `PlaylistNavigationCompletedMessage` → Medienwechsel + `ResolvePlaybackSourceEffect` mit `StartPositionSeconds` | Pflicht-E2E (Vor/Zurück) + Unit `NextRequested_InPlaylist_*`, `PreviousRequested_InPlaylist_*`, `NavigationCompleted_*` | Abgedeckt |
| Auto-Fortsetzung am Ende / Fehler-Rückfall auf nächsten Playlist-Eintrag | `UpdatePlaybackProgress`/`UpdatePlaybackEnded` → `AdvancePlaylistEntryEffect` bei `IsPlaylistPlayback` (Guard `!IsResolving && !PlaylistNextExhausted`); `UpdatePlaybackSourceResolved`(Quelle null)/`UpdatePlaybackFailed` → `AdvancePlaylistEntryEffect` | 2 Pflicht-E2E (Auto-Fortsetzung via `play/advance`, Fehler-Rückfall bei 404-Stream) + Unit `PlaybackEnded_InPlaylist_*`, `PlaybackFailed_InPlaylist_*`, `SourceResolveFailed_InPlaylist_*` | Abgedeckt |
| Weiterschauen-Eintrag mit Playlist-Bezug → Folgevideo aus Playlist-Reihenfolge | `ContinueWatchingEntry` +3 Felder → `HomeMediaItem` → `UpdateContinueWatchingItemSelected`-Guard → `StartPlaylistPlaybackEffect(PlaylistId, PlaylistEntryId)`; Folgevideo via `advance` | Pflicht-E2E mit Pfad-Nachweis (`/play`-Aufrufe, kein `api/items/tvshow/{id}`) + Unit `ContinueWatchingSelected_WithPlaylist_StartsPlaylistPlayback` | Abgedeckt |
| Weiterschauen ohne Playlist-Bezug unverändert (Serienkontext) | Guard greift nur bei `PlaylistId > 0 && PlaylistEntryId > 0`; sonst bisheriger `StartPlayback` + `LoadUpcomingEpisodesEffect`-Pfad | Unit `ContinueWatchingSelected_WithoutPlaylist_KeepsSeriesPath` + bestehender E2E `TapContinueWatchingEpisode_StartsPlaybackAndAdvancesToNextEpisode` | Abgedeckt |

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Button-Start aus Detailansicht (erster abspielbarer Eintrag) | `PlaylistPlaybackTests` — Playlists-Menü → Detail → `PlaylistDetailsPlayButton` → `PlayerIsActive && PlayerMediaId == ersterAbspielbarer` | Abgedeckt |
| Eintrags-Tap startet ab gewähltem Eintrag | `PlaylistPlaybackTests` — `InvokeAsync("PlaylistEntriesCollection", index)` → `PlayerMediaId == MediaIdDesEintrags` | Abgedeckt |
| Manuelle Vor-/Rückwärts-Navigation | `PlaylistPlaybackTests` — `InvokeAsync("PlayerNextButton")`/`("PlayerPreviousButton")` → `PlayerMediaId`-Wechsel + `WasPathRequested("/api/playlists/{id}/play/next")` | Abgedeckt |
| Auto-Fortsetzung (`play/advance`) | `PlaylistPlaybackTests` — kurzes Testvideo → `WasPathRequested("/api/playlists/{id}/play/advance")` + `PlayerMediaId`-Wechsel | Abgedeckt |
| Fehler-Rückfall | `PlaylistPlaybackTests` — Stream 404 → `play/advance`-Aufruf + Wechsel auf Folge-Eintrag | Abgedeckt |
| CW mit Playlist-Bezug | `PlaylistPlaybackTests` — `InvokeAsync("ContinueWatchingCarousel", index)` → `play?entryId` + `play/advance`, negativ: kein `api/items/tvshow/{id}` | Abgedeckt |
| CW ohne Playlist-Bezug | bestehender `ContinueWatchingPlaybackTests` (Regression) | Abgedeckt (bestehend) |

## Hinweise

- Die Mock-Erweiterungen (POST-Play-Endpunkte, Stream-Bedienung für Playlist-Medien, Playlist-Felder im CW-Opt-in, `ResumePositionSeconds` pro Eintrag) sind Voraussetzung der E2E-Szenarien und im Plan als eigener Schritt 7 verankert.
- `DevApiModelSnapshot`/`DevApiModels`-Erweiterung (`PlayerPlaylistId`/`PlayerPlaylistEntryId`/`PlayerPlaylistPosition`) stützt die Playlist-Kontext-Nachweise; der Mediumswechsel ist zusätzlich über `PlayerMediaId` beweisbar.
- `PlaylistNextExhausted` + `IsResolving`-Guard wurden in die Planung aufgenommen, um mehrfache `advance`-Versendung im Fortsetzungsfenster bzw. Retry-Schleifen nach einer 204-Antwort zu verhindern.
- Keine neuen Enums geplant → `EnumCoverageTests` unverändert.
- CS1591: alle neuen öffentlichen Member benötigen deutsche XML-Doku (Konvention, im Plan über Nachrichten-/Effekt-Records und Modelle abgedeckt).
