# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan vollständig

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| AK 1: Tap auf abspielbaren Weiterschauen-Eintrag (`movie`/`episode`) startet Wiedergabe direkt, ohne Detailansicht | Eigener `ItemTapped`-Handler + `ContinueWatchingItemSelectedMessage` → `UpdateContinueWatchingItemSelected` → `StartPlayback` (`NavigateToPlayerEffect` + `ResolvePlaybackSourceEffect`); Umsetzungsreihenfolge 1–5 | `AppUpdateTests_ContinueWatching.*` + E2E „Film-Direktstart ohne Detailaufruf" (`RequestedPaths`-Nachweis) | Abgedeckt |
| AK 2: Wiedergabe beginnt an der Fortsetzungsposition | `PlaybackSourceService.ResolveAsync` unverändert (Position kommt automatisch mit); verzögerter Seek in `PlayerPage` unverändert | E2E „`PlayerPositionSeconds >= Resume`" + bestehender `PlaybackSourceServiceTests` | Abgedeckt |
| AK 3: Andere Listen öffnen weiterhin nur die Detailansicht | Strukturelle Trennung: nur Weiterschauen-Karussell bekommt neuen Handler/Message-Typ; `MediaItemSelectedMessage`/`SourceMediaItemSelectedMessage`-Pfade unverändert | Bestehender E2E `PlaybackTests.PlayFromRecent_*` (Recent → Detailseite) + Contract-Test-Verbleib von `MediaItemSelectedMessage` | Abgedeckt |
| AK 4: Nicht abspielbare Weiterschauen-Einträge (`show`/`season`/`collection`) öffnen Detailansicht | `Normalize`-Verzweigung → `NavigateToMediaDetailsEffect` inkl. Serien-Hints | `ContinueWatchingItemSelected_NonPlayable_NavigatesToDetails` (Theory) + E2E `show`-Eintrag → `HeaderSection` | Abgedeckt |
| AK 5: Folgevideo bleibt erhalten; Serienkontext ohne Detailansicht | `LoadUpcomingEpisodesEffect` + `UpcomingEpisodesService` (`GetTvShowAsync`/`ShowDetailsCache`) + `UpcomingEpisodesLoadedMessage` → `UpcomingEpisodes` mit Identitäts-Guard | `UpcomingEpisodesServiceTests` + `UpcomingEpisodesLoaded_*` + E2E (Upcoming-Count > 0, Wechsel zur Folge-Episode, `api/items/tvshow/{id}`-Abruf, kein `tvshowepisode`-Detailaufruf) | Abgedeckt |

## Fehlende oder unvollständige Testanforderungen

— (Status `Plan vollständig`)

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Weiterschauen-Film → Direktwiedergabe ab Fortsetzungsposition (AK 1+2) | `ContinueWatchingPlaybackTests`: Film-Tap → `PlayerIsPlaying` + `PlayerPositionSeconds >= Resume`; `RequestedPaths` ohne Movie-Detailaufruf | Abgedeckt (geplant) |
| Weiterschauen-Episode → Folgevideo ohne Detailseite (AK 5) | `ContinueWatchingPlaybackTests`: Episoden-Tap → `PlayerUpcomingEpisodeCount > 0`, Wechsel `PlayerMediaId` → Folge-Episode; `api/items/tvshow/{id}`-Abruf, kein `tvshowepisode`-Detailaufruf | Abgedeckt (geplant) |
| Weiterschauen-`show` → Detailansicht (AK 4) | `ContinueWatchingPlaybackTests`: `HeaderSection` sichtbar, `PlayerIsActive == false` | Abgedeckt (geplant) |
| „Neu im Programm" → Detailansicht (AK 3) | Bestehender `PlaybackTests.PlayFromRecent_StartsPlaybackAtResumePosition` | Abgedeckt (bestehend) |

## Fehlende oder unvollständige Planbestandteile

— (Status `Plan vollständig`)

## Hinweise

- Die Unterscheidbarkeit der Herkunftsliste ist minimal-invasiv gelöst (eigener Handler + eigene Message statt Control-Attribut); `MediaCarousel` bleibt wortgleich.
- `UpdatePlayRequested` teilt den Wiedergabe-Startpfad über `StartPlayback` — keine divergierenden Wiedergabepfade.
- Race-Betrachtung Serienkontext-Nachladen: `MediaId`-Guard in `UpdateUpcomingEpisodesLoaded` ist konsistent zu `UpdatePlaybackSourceResolved`; das Verhalten bei spät eintreffender Antwort ist damit definiert (verwerfen).
- Offline-Erstanwendung: Offline-Weiterschauen-Einträge tragen `ShowId` aus dem Download-Manifest; `ShowDetailsCache`-Fallback sichert die Folgeliste auch ohne Client.
- `EnumCoverageTests` ist nicht betroffen (keine neuen Enums).
