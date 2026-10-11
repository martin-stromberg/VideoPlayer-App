# Umsetzungsplan: Download-Reihenfolge bei Playlist-Bezug (Schritt 7)

## Übersicht

Der automatische Download-Vorlauf (`AutoDownloadService`) soll für Weiterschauen-Einträge mit Playlist-Bezug (`PlaylistId` + `PlaylistEntryId`, seit Schritt 5 am `HomeMediaItem`) die Folge-Videos aus der **Playlist-Reihenfolge** (`api/playlists/{id}/entries/paged`, Server-Reihenfolge) statt aus der Serienstruktur einreihen. Der Playlist-Bezug wird zusätzlich bis ins Download-Manifest (`download_manifest`) durchgereicht (Playlist-Identität/Eintrags-Id/Sort-Order). Die Warteschlange bleibt FIFO — die korrekte Einreihe-Reihenfolge ist damit zugleich die Ausführungsreihenfolge. Einträge ohne Playlist-Bezug und manuelle Downloads bleiben unverändert.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Reihenfolge-Sicherstellung | Korrekte **Einreihe-Reihenfolge** im Vorlauf; `NextQueuedEntryAsync` bleibt FIFO (`OrderBy(e => e.Id)`) | FIFO spiegelt die Einplanung bereits 1:1; ein Umsortieren bestehender Aufträge ist nicht gefordert und würde fremde Downloads überholen. |
| Playlist-Bezug am Manifest | Nullable Spalten `PlaylistId`, `PlaylistEntryId`, `PlaylistSortOrder` an `DownloadManifestEntry` + `EnsureColumnAsync`-Migration | Anforderung verlangt das Durchreichen des Playlist-Bezugs „bis in den Download-Kontext"; die Spalten dokumentieren die Herkunft und folgen exakt dem etablierten Upgrade-Muster (Serienkontext-Spalten). |
| Kontext-Übergabe | Neuer Record `DownloadPlaylistContext` (analog `DownloadSeriesContext`) als optionaler Parameter von `EnqueueAsync` | Bestehendes Übergabemuster; `DownloadSeriesContext` bleibt parallel bestehen (Episoden-Einträge können beide Kontexte tragen — Playlist für Reihenfolge/Herkunft, Serie für Offline-Platzhalter). |
| Folge-Ermittlung | `client.GetPlaylistEntriesPagedAsync` direkt im `AutoDownloadService` (statt `PlaylistService`) | Der Service arbeitet wie bei `GetTvShowAsync` direkt auf dem API-Client; `PlaylistService` liefert nur Anzeige-Records und würde eine unnötige Abhängigkeit einführen. |
| Selbst-Einreihen bei Filmen | Playlist-gebundener Film reiht weiterhin sich selbst ein (wie bisher) **plus** die nächsten `PreloadEpisodeCount` abspielbaren Playlist-Einträge | Filme aus der Weiterschauen werden heute selbst geladen; die Playlist-Fortsetzung ist das Analogon zum Episoden-Vorlauf. |
| Playlist-Abruf statt Serien-Abruf | Bei erfolgreich aufgelöstem Playlist-Bezug wird `api/items/tvshow/{showId}` **nicht** mehr abgerufen | Die Playlist-Reihenfolge ersetzt die Serienreihenfolge vollständig; der Serienabruf wäre ein zusätzlicher Request ohne Reihenfolge-Nutzen (Serien-Hints kommen aus `ParentMedia*`-Feldern der Einträge). |
| Fallback | `PlaylistEntryId` fehlt, Eintrag nicht mehr in der Playlist oder Playlist-Abruf schlägt fehl → bisheriges Serien-Verhalten (Episode) bzw. Selbst-Einreihung (Film) | Identisch zur Wiedergabe-Logik von Schritt 5: ohne gültigen Playlist-Eintragsbezug gilt der Serienkontext-Pfad. |

## Programmabläufe

### Automatischer Vorlauf mit Playlist-Bezug

1. `AutoDownloadService.RunAsync` iteriert die Weiterschauen-Liste (Cache-Kopie via `HomeListService`).
2. Trägt `item` einen gültigen Playlist-Bezug (`PlaylistId > 0` **und** `PlaylistEntryId > 0`):
   - Film (`movie`): `EnqueueAsync` für den Film selbst mit `DownloadPlaylistContext` (wie bisher, plus Playlist-Felder), danach Schritt 3.
   - Episode (`tvshowepisode`) oder Film: `EnqueueNextPlaylistEntriesAsync(item, settings, ct)` für die Folge-Einträge.
3. `EnqueueNextPlaylistEntriesAsync` blättert `api/playlists/{PlaylistId}/entries/paged` (Seitengröße als Konstante, z. B. 50) in Server-Reihenfolge, bis der Eintrag `Id == item.PlaylistEntryId` gefunden ist; danach werden die nächsten Einträge gesammelt, die ladbar sind (`MediaType` normalisiert `movie`/`tvshowepisode`, `IsAccessible`), bis `PreloadEpisodeCount` erreicht oder die Playlist endet (`HasNextPage == false`).
4. Je gesammeltem Eintrag: `EnqueueAsync` mit `DownloadPlaylistContext(PlaylistId, EntryId, SortOrder)`; bei Episoden zusätzlich `DownloadSeriesContext` aus den `ParentMedia*`-Feldern, sofern `ParentMediaType == "tvshow"` (ShowId/ShowTitle) — `SeasonNumber` bleibt unbekannt (`null`), `EpisodeNumber` aus dem Eintrag.
5. Wird `item.PlaylistEntryId` in keiner Seite gefunden oder schlägt der Playlist-Abruf fehl: Rückfall auf `EnqueueNextEpisodesAsync` (Episode) — Fehler werden wie bisher je Eintrag protokolliert und übersprungen.
6. Ohne Playlist-Bezug: unverändertes Verhalten (`movie` → Selbst, `tvshowepisode` → Serien-Folge-Episoden).

Beteiligte Klassen/Komponenten: `AutoDownloadService`, `VideoPlayerApiClient.GetPlaylistEntriesPagedAsync`, `DownloadService.EnqueueAsync`, `DownloadPlaylistContext`, `DownloadSeriesContext`, `HomeMediaItem`.

### Einreihen und Persistieren des Playlist-Bezugs

1. `DownloadService.EnqueueAsync` erhält den optionalen Parameter `playlistContext` (nach `seriesContext`).
2. `ApplyPlaylistContext(entry, playlistContext)` schreibt `PlaylistId`/`PlaylistEntryId`/`PlaylistSortOrder` in den Manifest-Eintrag — nur in leere Felder (`??=`-Muster wie `ApplySeriesContext`), für alle ladablen Typen (Filme können Playlist-Bezug tragen).
3. `CacheDatabase.InitializeAsync` ergänzt `EnsureColumnAsync`-Aufrufe für die drei neuen Spalten.

Beteiligte Klassen/Komponenten: `DownloadService`, `DownloadManifestEntry`, `CacheDatabase`, `DownloadPlaylistContext`.

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `DownloadPlaylistContext` | Record (`VideoPlayer.Mvu`) | Playlist-Bezug eines Downloads: `PlaylistId` (`long?`), `PlaylistEntryId` (`long?`), `PlaylistSortOrder` (`long?`) — Gegenstück zu `DownloadSeriesContext`. |

## Änderungen an bestehenden Klassen

### `DownloadManifestEntry` (Datenmodellklasse)

- **Neue Eigenschaften:** `PlaylistId` (`long?`), `PlaylistEntryId` (`long?`), `PlaylistSortOrder` (`long?`) — Herkunfts-/Reihenfolge-Bezug playlist-gebundener Downloads; `null` ohne Playlist-Bezug.

### `CacheDatabase`

- **Geänderte Methoden:** `InitializeAsync` — drei zusätzliche `EnsureColumnAsync("download_manifest", …)`-Aufrufe für `PlaylistId` (`bigint NULL`), `PlaylistEntryId` (`bigint NULL`), `PlaylistSortOrder` (`bigint NULL`).

### `DownloadService`

- **Geänderte Methoden:** `EnqueueAsync` — neuer optionaler Parameter `DownloadPlaylistContext? playlistContext = null` (nach `seriesContext`).
- **Neue Methoden:** `ApplyPlaylistContext(DownloadManifestEntry, DownloadPlaylistContext?)` — private statische Methode nach dem Muster von `ApplySeriesContext` (`??=`, kein Typ-Zwang, da Filme Playlist-Bezug tragen können).

### `AutoDownloadService`

- **Geänderte Methoden:** `RunAsync` — Dispatch auf Playlist-Pfad bei `item.PlaylistId > 0 && item.PlaylistEntryId > 0` (Film: Selbst-Einreihung mit Playlist-Kontext + Folge-Einträge; Episode: Folge-Einträge); sonst bisheriges Verhalten.
- **Neue Methoden:** `EnqueueNextPlaylistEntriesAsync(HomeMediaItem item, AppSettings settings, CancellationToken ct)` — paginiertes Sammeln der nächsten ladbaren Playlist-Einträge und Einreihen in Server-Reihenfolge; private Konstante für die Seitengröße (z. B. `PlaylistEntriesPageSize = 50`).

## Datenbankmigrationen

| Migrationsname | Betroffene Tabellen/Spalten | Beschreibung der Änderung |
|----------------|----------------------------|---------------------------|
| `EnsureColumnAsync`-Nachzug (Bestandsmuster) | `download_manifest`: `PlaylistId`, `PlaylistEntryId`, `PlaylistSortOrder` (je `bigint NULL`) | Spalten werden beim Initialisieren in bestehenden Installationen per `ALTER TABLE … ADD COLUMN` nachgezogen — kein Datenverlust, kein Schema-Versionswechsel. |

## Validierungsregeln

| Feld / Objekt | Regel | Fehlerfall |
|---------------|-------|------------|
| `HomeMediaItem.PlaylistId`/`PlaylistEntryId` | Playlist-Pfad nur bei `PlaylistId > 0` **und** `PlaylistEntryId > 0` | Bezug unvollständig → Serien-/Selbst-Verhalten (Rückfall, kein Fehler). |
| Playlist-Eintrag | Einreihen nur bei normalisiertem `MediaType` `movie`/`tvshowepisode` und `IsAccessible` | Nicht ladbare/nicht zugängliche Einträge werden übersprungen (zählen nicht auf `PreloadEpisodeCount` — sie sind nicht ladbare Lücken, wie nicht abspielbare Server-Einträge). |
| Playlist-Abruf | `item.PlaylistEntryId` muss in den gelieferten Seiten gefunden werden | Nicht gefunden/Abruf-Fehler → Rückfall Serien-Pfad. |

## Konfigurationsänderungen

Keine — `PreloadEpisodeCount` steuert die Anzahl der Playlist-Folge-Einträge mit.

## Seiteneffekte und Risiken

- **Manifest-Schema:** Neue Spalten sind nullable und werden per etabliertem `EnsureColumnAsync`-Muster nachgezogen — Bestandsinstallationen bleiben funktionsfähig; `sqlite-net` erstellt die Spalten bei Neuanlage automatisch.
- **`OfflineContinueWatchingService`:** Liest `ShowId`/`SeasonNumber`/`EpisodeNumber` unverändert; playlist-gebundene Episoden-Downloads tragen weiterhin Serien-Hints aus `ParentMedia*` (soweit bekannt) — Offline-Platzhalter bleiben serienbasiert (Playlists sind Online-Feature, kein Offline-Kontext gefordert).
- **Dedup:** Playlist-Einträge, die bereits im Manifest stehen (z. B. manuell geladen), werden nicht doppelt eingeplant — deren Playlist-Felder bleiben ggf. leer (Re-Enqueue füllt nur leere Felder).
- **Zusätzliche Requests:** Pro playlist-gebundenem Eintrag ein oder mehrere `entries/paged`-Abrufe statt des `tvshow`-Abrufs — vergleichbares Volumen; Throttling unverändert.
- **Öffentliche/fremde Playlists:** Weiterschauen-Einträge können aus fremden (öffentlichen) Playlists stammen — `GET entries/paged` ist lesend und liefert die Server-Reihenfolge unabhängig vom Besitz.
- **Abweichende Medientypen in Playlists:** Einträge auf `tvshow`/`tvshowseason`/`moviecollection` sind nicht ladbar und werden übersprungen — sie blockieren die Reihenfolge der Folge-Einträge nicht.

## Umsetzungsreihenfolge

1. **`DownloadPlaylistContext`-Record anlegen**
   - Voraussetzungen: Keine.
   - Beschreibung: Neue Datei `VideoPlayer.Mvu/DownloadPlaylistContext.cs` mit deutschen XML-Doku-Kommentaren (CS1591-Konvention).

2. **Manifest-Entität und Schema-Migration erweitern**
   - Voraussetzungen: Keine.
   - Beschreibung: `DownloadManifestEntry` um `PlaylistId`/`PlaylistEntryId`/`PlaylistSortOrder` (je `long?`, XML-Doku) erweitern; `CacheDatabase.InitializeAsync` um drei `EnsureColumnAsync`-Aufrufe ergänzen.

3. **`DownloadService.EnqueueAsync` um `playlistContext` erweitern**
   - Voraussetzungen: Schritte 1 und 2.
   - Beschreibung: Optionaler Parameter, `ApplyPlaylistContext`-Methode nach `??=`-Muster, XML-Doku anpassen.

4. **`AutoDownloadService` Playlist-Vorlauf implementieren**
   - Voraussetzungen: Schritt 3; `VideoPlayerApiClient.GetPlaylistEntriesPagedAsync` (vorhanden).
   - Beschreibung: Playlist-Pfad in `RunAsync`, `EnqueueNextPlaylistEntriesAsync` mit Paging/Filterung/Fallback; Klassen-Doku des Services anpassen.

5. **Unit-Tests schreiben**
   - Voraussetzungen: Schritte 1–4; Testinfrastruktur (`StubHttpMessageHandler`, `AutoDownloadServiceTests`, `LocalDatabaseTests`) vorhanden.
   - Beschreibung: Neue Testfälle gemäß Abschnitt „Tests" (Playlist-Reihenfolge, Film-Selbst+Fortsetzung, Überspringen, Fallback, Manifest-Felder, Schema-Migration).

6. **Build + komplette Unit-Testsuite ausführen**
   - Voraussetzungen: Schritte 1–5.
   - Beschreibung: `dotnet test VideoWebPlayer.Maui.Tests` muss grün bleiben.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `Run_PlaylistEpisode_EnqueuesPlaylistOrder` | `AutoDownloadServiceTests` | Weiterschauen-Episode mit `PlaylistId`/`PlaylistEntryId`: Folge-Einträge werden in Playlist-Reihenfolge eingeplant (Manifest-Ids in Playlist-Reihenfolge); `entries/paged` wird abgefragt, `api/items/tvshow` nicht. |
| `Run_PlaylistEpisode_SkipsNonDownloadableEntries` | `AutoDownloadServiceTests` | Nicht ladbare (`tvshow`, `tvshowseason`) und `IsAccessible == false` Einträge werden übersprungen; die nächsten ladbaren Folge-Einträge werden eingeplant. |
| `Run_PlaylistEpisode_PersistsPlaylistContext` | `AutoDownloadServiceTests` | Manifest-Einträge tragen `PlaylistId`/`PlaylistEntryId`/`PlaylistSortOrder`; Serien-Hints aus `ParentMedia*` bei Episoden. |
| `Run_PlaylistMovie_EnqueuesSelfAndNextEntries` | `AutoDownloadServiceTests` | Playlist-gebundener Film: Manifest enthält den Film selbst plus die nächsten `PreloadEpisodeCount` ladbaren Playlist-Einträge in Playlist-Reihenfolge. |
| `Run_PlaylistEntryNotFound_FallsBackToSeries` | `AutoDownloadServiceTests` | `PlaylistEntryId` nicht in den Seiten enthalten → Serien-Vorlauf wie bisher (`api/items/tvshow`-Abruf, Serienreihenfolge). |
| `Run_PlaylistEntryMissing_FallsBackToSeries` | `AutoDownloadServiceTests` | `PlaylistId` gesetzt, `PlaylistEntryId == null` → Serien-Verhalten. |
| `CacheDatabase_MigratesPlaylistContextOntoExistingManifest` | `LocalDatabaseTests` | Bestehende `download_manifest` ohne Playlist-Spalten wird per `InitializeAsync` erweitert; Spalten sind beschreibbar (Muster von `CacheDatabase_MigratesSeriesContextOntoExistingManifest`). |
| `PlaylistPage` (Hilfsmethode) | `AutoDownloadServiceTests` | Baut eine `entries/paged`-JSON-Antwort (`PlaylistEntry[]` + `hasNextPage`/`totalCount`) für den Stub. |

### Betroffene bestehende Tests

Keine — die Signatur-Erweiterung ist ein optionaler Parameter; das Verhalten ohne Playlist-Bezug bleibt unverändert (Bestandstests `Run_EpisodeCandidate_*`, `Run_MovieCandidate_*` decken dies ab).

### E2E-Tests (primärer Funktionsnachweis)

Es sind keine E2E-Tests erforderlich: Die Anforderung ändert ausschließlich Service-Layer-Logik (Einreihe-Reihenfolge des Hintergrund-Vorlaufs) — es gibt **keinen neuen oder geänderten Benutzerfluss** und keine UI-Änderung. Die betroffene Funktion ist nicht über eine Nutzeraktion erreichbar (der Vorlauf läuft gedrosselt im Hintergrund); es existiert generell kein E2E-Test für Downloads. Der Funktionsnachweis erfolgt über die Unit-Tests mit echter SQLite-Ablage und HTTP-Stub — diese decken Einplanungsreihenfolge und Manifest-Persistenz vollständig ab.

Bestehende E2E-Tests angepasst: Keine.

## Offene Punkte

Keine.
