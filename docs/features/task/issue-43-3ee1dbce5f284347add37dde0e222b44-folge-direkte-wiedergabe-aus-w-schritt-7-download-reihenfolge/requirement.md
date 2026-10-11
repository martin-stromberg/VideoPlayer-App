# Übersetzte Anforderung – Download-Reihenfolge bei Playlist-Bezug (Schritt 7 von 8)

## Fachliche Zusammenfassung

Videos, deren Weiterschauen-Eintrag einen Playlist-Bezug trägt (`PlaylistId`/`PlaylistName`/`PlaylistEntryId`, seit Schritt 5 am `ContinueWatchingEntry`-DTO und bis in den `PlayerState` durchgereicht), sollen beim automatischen bzw. manuellen Download in der **Playlist-Reihenfolge** eingeplant und ausgeführt werden — nicht in der Serienreihenfolge. Der bisherige Ist-Zustand (FIFO über Manifest-Id; `AutoDownloadService` reiht Folge-Episoden aus dem Serienkontext `api/items/tvshow/{id}` ein, ohne Playlist-Bezug im `download_manifest`) ist zunächst zu verifizieren und zu dokumentieren; anschließend ist der Playlist-Bezug in den Download-Kontext durchzureichen (z. B. Playlist-Identität/Sort-Order am `DownloadManifestEntry`), damit die Einplanung playlist-gebundener Einträge der Playlist- statt der Serienreihenfolge folgt. Downloads ohne Playlist-Bezug bleiben unverändert.

## Betroffene Klassen und Komponenten

- `VideoPlayer.Storage/Entities/DownloadManifestEntry.cs` — Manifest-Entität `download_manifest`; erhält voraussichtlich Playlist-Felder (Playlist-Identität, ggf. Sort-Order/PlaylistEntryId). **Schema-Änderung → Upgrade-Sicherheit für Bestandsinstallationen prüfen** (SQLite-Schema-Management in `VideoPlayer.Storage` beachten).
- `VideoPlayer.Mvu/DownloadService.cs` — FIFO-Warteschlange (`NextQueuedEntryAsync` → `OrderBy(e => e.Id)`, `MaxParallelDownloads = 2`, `EnqueueAsync`, `ApplySeriesContext`, `DownloadSeriesContext`); Aufnahme und Sortierung des Playlist-Bezugs.
- `VideoPlayer.Mvu/AutoDownloadService.cs` — automatischer Episoden-Vorlauf (`EnqueueNextEpisodesAsync`, bisher Serienreihenfolge über `api/items/tvshow/{showId}`); bei playlist-gebundenen Weiterschauen-Einträgen Folge-Einträge aus der Playlist-Reihenfolge (Playlist-Einträge-Endpunkt `api/playlists/{id}/entries`) einreihen.
- `VideoPlayer.ApiClient` — Playlist-Einträge-Endpunkte und DTOs (aus Schritt 4/5 vorhanden, ggf. Sort-Order-Feld prüfen).
- `ContinueWatchingEntry` / `HomeMediaItem` / `PlayerState` — Playlist-Bezug bereits vorhanden (Schritt 5); Durchreichen in den Download-Kontext.
- MVU-Konvention: ggf. neue/angepasste `AppMessage`/`AppEffect`-Records (deutsch, XML-Doku), `AppUpdate.Update`-Switch, `ShellAppEffectHandler`; `EnumCoverageTests` bei neuen Enums.
- Tests: `VideoWebPlayer.Maui.Tests` (xUnit) für Warteschlangen-/Einplanungslogik; `VideoPlayer.E2E.Tests` (`MockVideoServer`) ggf. um Playlist-Endpunkte erweitern.

## Implementierungsansatz

1. **Verifikation des Ist-Zustands** (explizit gefordert): Prüfen, ob playlist-gebundene Weiterschauen-Einträge heute Downloads in falscher Reihenfolge auslösen (Serien- statt Playlist-Reihenfolge) und ob die Playlist-Wiedergabe den Download-Vorlauf überhaupt beeinflusst; Ergebnis dokumentieren.
2. Playlist-Bezug am `DownloadManifestEntry` persistieren (nullable Felder; Schema-Upgrade tolerant für bestehende DBs).
3. Beim Einreihen (`EnqueueAsync`/`AutoDownloadService`) den Playlist-Kontext des Weiterschauen-Eintrags bzw. der aktuellen Wiedergabe berücksichtigen: Folge-Videos bei Playlist-Bezug aus den Playlist-Einträgen (Server-Reihenfolge/`sortOrder`) statt aus der Serienstruktur bestimmen.
4. Downloads ohne Playlist-Bezug: bisheriges Verhalten (Serienreihenfolge/FIFO) beibehalten.

## Konfiguration

Keine neue Konfiguration erforderlich; der automatische Vorlauf bleibt über die bestehenden Einstellungen (`PreloadEpisodeCount`, WLAN-Bedingung, Kontingent) gesteuert.

## Offene Fragen

- Muss die Playlist-Reihenfolge zusätzlich die Reihenfolge **bereits in der Warteschlange befindlicher** Aufträge ändern (Umsortieren der Queue), oder genügt das korrekte **Einreihen** der Folge-Downloads in Playlist-Reihenfolge (FIFO bleibt)? — Aus der Anforderung ableitbar: Einplanung in richtiger Reihenfolge genügt („eingeplant bzw. eingeplant/ausgeführt"); ein generelles Umsortieren bestehender Aufträge wird nicht gefordert.
- Woher bezieht der `AutoDownloadService` den Playlist-Bezug — aus dem Cache-Eintrag der Weiterschauen-Liste (`ContinueWatchingEntry` mit Playlist-Feldern) oder aus dem aktiven `PlayerState`? — In der Bestandsaufnahme zu klären.
