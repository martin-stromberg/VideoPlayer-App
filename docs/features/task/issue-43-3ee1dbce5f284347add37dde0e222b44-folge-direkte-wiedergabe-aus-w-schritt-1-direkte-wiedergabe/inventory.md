# Bestandsaufnahme: Direkte Wiedergabe aus der Weiterschauen-Liste

Analyse des Tap-/Wiedergabe-Pfads der Startseiten-Listen und der Folgevideo-Auflösung, bezogen auf die Anforderung in `requirement.md` (Entwicklungsschritt 1 des Projektplans Issue #43).

## Zusammenfassung

- **Vorhanden:** Der komplette Wiedergabepfad — `PlayRequestedMessage` → `UpdatePlayRequested` (`PlayerState` + `NavigateToPlayerEffect` + `ResolvePlaybackSourceEffect`) → `PlaybackSourceService.ResolveAsync` (ermittelt die Fortsetzungsposition selbst aus `api/continue-watching` bzw. `cached_lists`) → `PlayerPage` mit verzögertem Fortsetzungs-Seek. Ein direkter Wiedergabestart bekommt Position und Quelle also ohne Zusatz.
- **Vorhanden:** `PlayerState.UpcomingEpisodes` samt Verbrauch durch `AdvanceToNextEpisode` (Auto-Fortsetzung/Fehler-Rückfall) — aber die Befüllung erfolgt ausschließlich aus `model.MediaDetails` der geladenen Detailseite; ohne Detailansicht bleibt sie leer.
- **Vorhanden:** Bausteine der detailseiten-unabhängigen Serienkontext-Auflösung: `VideoPlayerApiClient.GetTvShowAsync` (`api/items/tvshow/{id}`), `ShowDetailsCache` (Offline-Serienstruktur, `OrderedEpisodes`/`NextEpisodeAfterId`), `ShowId`-Hint am `HomeMediaItem`, Musterimplementierung `AutoDownloadService.EnqueueNextEpisodesAsync`.
- **Fehlt:** Unterscheidbarkeit der Herkunftsliste am Auslösepunkt — `MediaCarousel.ItemTapped` liefert nur das `HomeMediaItem`; alle drei Karussells teilen `HomePage.OnMediaItemTapped` → `MediaItemSelectedMessage` → `NavigateToMediaDetailsEffect`. Dem Weiterschauen-Karussell fehlt zudem eine `AutomationId` (E2E-Zugriff).
- **Fehlt:** MVU-Nachricht/-Effekt für den Weiterschauen-Direktstart und für die asynchrone Fortsetzungslisten-Ladung, die dazugehörige Logik (Online `GetTvShowAsync` + `ShowDetailsCache.WriteAsync`, Offline `ShowDetailsCache.ReadAsync`) sowie der Effekt-Handler im `ShellAppEffectHandler`.
- **Fehlt (E2E):** `DevApiModelSnapshot` enthält kein Fortsetzungslisten-Feld (Folgevideo-Beleg nicht messbar); `MockVideoServer` kennt nur den `movie`-Wiedergabefluss — kein `episode`-Weiterschauen-Eintrag, kein `api/items/tvshow/{id}`, kein `api/items/tvshowepisode/{id}/stream`.
- **Achtung Contract-Test:** `HomePageContractTests.HomePage_SelectionDispatchesMediaItemSelected` pinnt `MediaItemSelectedMessage` im `HomePage`-Code-Behind — bleibt für die übrigen Listen gültig, wird um die Weiterschauen-Dispatch-Variante zu ergänzen sein.

Test-Ausgangszustand: Build 0 Fehler; `VideoWebPlayer.Maui.Tests` 1056/1056 bestanden, `VideoPlayer.E2E.Tests` 19/19 bestanden — keine bekannten Fehlschläge ([Nachweis](inventory/tests.md)).

## Details

- [Datenmodell](inventory/models.md)
- [Logik](inventory/logic.md)
- [Interfaces](inventory/interfaces.md)
- [Tests](inventory/tests.md)
