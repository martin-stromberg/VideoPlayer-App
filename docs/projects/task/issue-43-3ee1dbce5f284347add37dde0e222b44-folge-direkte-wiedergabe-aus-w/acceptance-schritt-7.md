# Abnahmeprüfung – Entwicklungsschritt 7

## Ergebnis

**Status:** Anforderung vollständig erfüllt

## Abweichungen

- [ ] Keine.

## Hinweise

Geprüft: `git diff ...folge-direkte-wiedergabe-aus-w...HEAD` (Commit `ed637e9`), Quelldateien, Planungsartefakte aus Commit `b6784f1` sowie gefilterter Testlauf. Gelöschte `docs/features/`-Arbeitsartefakte (requirement/inventory/plan/plan-check/todo) sind Workflow-konform im Feature-Commit entfernt worden und blieben bei der Bewertung als Nachweis in der Historie berücksichtigt.

**Akzeptanzkriterium 1 (Ist-Zustand verifiziert und dokumentiert):** Der Befund ist in der Bestandsaufnahme des Planungs-Commits `b6784f1` dokumentiert (`inventory.md`: „Ja — der Ist-Zustand plant Downloads playlist-gebundener Weiterschauen-Einträge in falscher (Serien-)Reihenfolge ein", inkl. Baseline-Testlauf 1278 grün; `inventory/logic.md` benennt, dass `PlaylistId`/`PlaylistEntryId` am `HomeMediaItem` nicht ausgewertet wurden). Gegenprobe am alten Code bestätigt den Befund: `AutoDownloadService.RunAsync` kannte nur den Serienpfad. Das Vorher-Verhalten bleibt zudem in den überlebenden Doku-Artefakten greifbar (`docs/help/downloads.md`: „folgt der **Playlist-Reihenfolge** statt der Serienreihenfolge"; `changes.log`, `RELEASE_NOTES.md`).

**Akzeptanzkriterium 2 (Playlist-Reihenfolge):** `AutoDownloadService.RunAsync` wählt den Playlist-Pfad bei `item.PlaylistId is > 0 && item.PlaylistEntryId is > 0` (`AutoDownloadService.cs` Z. 127, 138-146). `EnqueueNextPlaylistEntriesAsync` (Z. 238-303) paginiert `api/playlists/{id}/entries/paged` (Seitengröße 50) in Server-Reihenfolge, findet den Anker `entry.Id == item.PlaylistEntryId` und reiht die folgenden Einträge ein — nur ladbare Typen (`movie`/`tvshowepisode` via `MediaTypeNames.Normalize`) mit `IsAccessible`; übersprungene Einträge zählen nicht auf `PreloadEpisodeCount` (Z. 282-286, `remaining--` erst nach dem Enqueue). Die Ausführungsreihenfolge ist gesichert: `NextQueuedEntryAsync` ist FIFO über `OrderBy(e => e.Id)` (`DownloadService.cs` Z. 569-577) — Einplanungsreihenfolge = Manifest-Id-Reihenfolge = Startreihenfolge; `Run_PlaylistEpisode_EnqueuesPlaylistOrder` belegt Playlist-Reihenfolge (Film zwischen Episoden) und dass `api/items/tvshow` nicht abgefragt wird. Playlist-gebundene Filme laden sich selbst plus Folge-Einträge (Z. 130-141, Test `Run_PlaylistMovie_EnqueuesSelfAndNextEntries`).

**Akzeptanzkriterium 3 (ohne Playlist-Bezug unverändert):** Der sonst-Zweig ist unverändert (`tvshowepisode` → `EnqueueNextEpisodesAsync`, `movie` → Selbst-Einreihung); `Run_PlaylistEntryMissing_FallsBackToSeries` belegt, dass ohne `PlaylistEntryId` kein Playlist-Request erfolgt. Auch der manuelle Download-Pfad (`EnqueueDownloadEffect` → `ShellAppEffectHandler.EnqueueDownloadAsync`) ist unverändert — er trägt keine Playlist-Felder und ruft `EnqueueAsync` ohne `playlistContext`.

**Fallback:** Vorhanden und sinnvoll — Eintrag in keiner Seite gefunden (`found == false` → Serienpfad), Abruffehler (`ApiException`/`HttpRequestException`/`InvalidOperationException`/`JsonException` → `return false` → Serienpfad; bei Filmen bleibt die Selbst-Einreihung = Altverhalten). Gefundener Anker ohne Folge-Einträge gibt `true` zurück (Playlist-Pfad greift, kein Serienabruf) — deckt sich mit der Schritt-5-Logik der Wiedergabe.

**Migration:** Upgrade-sicher — drei nullable Spalten `PlaylistId`/`PlaylistEntryId`/`PlaylistSortOrder` (`bigint NULL`) per `EnsureColumnAsync` (`PRAGMA table_info` + `ALTER TABLE ADD COLUMN`, `CacheDatabase.cs` Z. 118-127) im etablierten Muster der Serienkontext-Spalten; `LocalDatabaseTests.CacheDatabase_MigratesPlaylistContextOntoExistingManifest` belegt Nachzug auf Altschema, Beschreibbarkeit und Idempotenz. `ApplyPlaylistContext` (`DownloadService.cs` Z. 344-359) nutzt `??=` — Re-Enqueue überschreibt nichts, füllt aber fehlende Playlist-Felder nach.

**Filme vs. Episoden als Playlist-Eintrag:** Beide ladbar; Episoden erhalten Serien-Hints aus `ParentMedia*` (`SeriesContextOf`, Z. 330-346) mit `SeasonNumber = null` (im Eintrag nicht bekannt — dokumentiert; Offline-Platzhalter löst über Medien-Id), Anzeigetitel „Serie – Episode" (`PlaylistEntryTitle`, Z. 312-320).

**Kontext-Verdrahtung:** `playlistContext` wird ausschließlich vom `AutoDownloadService` gespeist — aus `HomeMediaItem.PlaylistId`/`PlaylistEntryId`, befüllt in `HomeListService.MapContinueWatching` (Z. 265-267) aus dem `ContinueWatchingEntry`-DTO (Schritt 5). Das ist die fachlich korrekte Quelle: Der Vorlauf arbeitet auf der gecachten Weiterschauen-Liste, nicht am `PlayerState`. Der manuelle Einzeldownload erhält keinen Playlist-Bezug — fachlich unerheblich (Einzeldarstellung ohne Reihenfolge-Relevanz), keine Verdrahtungslücke im geforderten Umfang.

**Testnachweise:** 6 neue `AutoDownloadServiceTests` (Reihenfolge, Überspringen, Serien-Hints, Film-Selbst+Fortsetzung, beide Rückfallfälle) + 1 Migrations-Test; gefilterter Lauf `AutoDownloadServiceTests|LocalDatabaseTests`: 28/28 grün. Behauptete Gesamtzahl 1285 plausibel (Baseline 1278 + 7 neue). Kein E2E — begründet haltbar: kein Benutzerfluss, reine Hintergrund-Service-Logik; es existiert generell kein E2E-Test für Downloads.

**Kleinigkeiten (keine Kriterien-Abweichungen):** Mehrseiten-Paging ist implementiert, aber nur einseitig getestet (alle Fixtures `hasNextPage: false`). Der Abruffehler-Rückfall ist implementiert, jedoch nur über die Variante „Eintrag nicht gefunden" getestet (identischer `return false`-Pfad). Ein HTTP-Timeout (`TaskCanceledException`) des Playlist-Abrufs wird nicht pro Eintrag zurückgefallen, sondern bricht den Lauf ab — identisch zum bisherigen Fehlermodell des Serienpfads (Retry beim nächsten Trigger). `PlaylistSortOrder` des Selbst-Eintrags playlist-gebundener Filme bleibt `null` (`HomeMediaItem` trägt keine SortOrder). Das Baseline-Testlog ist `*.log`-gitignored (nur lokaler Nachweis); die Baseline ist in `inventory.md` dokumentiert.
