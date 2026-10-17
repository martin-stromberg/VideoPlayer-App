# Umsetzungsplan: Playlist-Bezug in der Fortschrittsmeldung (Weiterschauen)

## Übersicht

Der Playlist-Wiedergabekontext (`PlayerState.PlaylistId`) wird entlang der gesamten Fortschritts-Meldungsstrecke `POST api/continue-watching/progress` durchgereicht: `ReportPlaybackProgressEffect` → `ShellAppEffectHandler` → `ProgressReportService` → `VideoPlayerApiClient` → `PlaybackProgressReport` (JSON-Body) — inklusive der persistenten Offline-Warteschlange `pending_progress` und des Nachsendepfads `ServerSyncService.FlushPendingProgressAsync`. Der Server-Vertrag ist verifiziert (Referenz-Repo `VideoPlayer-server`): `ContinueWatchingController.ProgressRequest` akzeptiert **ausschließlich** `PlaylistId` (`long?`); `PlaylistEntryId` wird serverseitig beim Ausliefern der `ContinueWatchingDto`s aus der Playlist-Mitgliedschaft abgeleitet und wird **nicht** gesendet. Es gibt keine UI-Änderung und keine neue Benutzerinteraktion; betroffen sind `VideoPlayer.ApiClient`, `VideoPlayer.Mvu`, `VideoPlayer.Storage`, `VideoPlayer.Maui` (App-Rand) sowie die E2E-Infrastruktur (`MockVideoServer`).

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Umfang des transportierten Playlist-Bezugs | Nur `PlaylistId` (`long?`) im Fortschrittsreport — `PlaylistEntryId` wird **nicht** gesendet | Verifizierter Server-Vertrag: `ProgressRequest` (`VideoWebPlayer/Controllers/ContinueWatchingController.cs` Z. 54–59) kennt nur `PlaylistId`; der Server validiert den Zugriff (`ValidatePlaylistAccessAsync`), normalisiert die Bindung (`NormalizePlaylistBindingAsync` — Medium nicht mehr in Playlist → Bezug verworfen) und upsertet über (UserId, MovieId, TVShowEpisodeId, PlaylistId) (`ContinueWatchingService.ReportProgressAsync` Z. 269–286). Die ursprünglich angenommene `PlaylistEntryId` würde serverseitig ignoriert |
| Parameterübergabe entlang der Strecke | Ein optionaler `long?`-Parameter `playlistId` (Standard `null`) an `ReportPlaybackProgressAsync`, `ReportProgressAsync` und `EnqueueAsync` — kein neues Value Object | Nur ein zusätzliches Feld; alle betroffenen Methoden folgen dem bestehenden Muster flacher Parameterlisten; `null` kennzeichnet „kein Playlist-Kontext" und hält bestehende Aufrufstellen (inkl. Tests) kompilierbar |
| `ReportPlaybackProgressEffect` | Neuer Record-Parameter `PlaylistId` (`long?`) am Ende **ohne** Standardwert | Der Compiler zwingt alle drei Erzeugungsstellen in `AppUpdate` zur bewussten Befüllung; der Effekt wird nur in `AppUpdate` konstruiert (Tests lesen per Pattern-Matching) |
| `pending_progress`-Spalte | Eine nullable `bigint`-Spalte `PlaylistId` via `EnsureColumnAsync` | `NULL` bildet „kein Kontext" korrekt ab und spiegelt die `long?`-Semantik des API-Felds; das `EnsureColumnAsync`-Muster (`DurationTicks`) ist vorhanden; SQLite erlaubt `ADD COLUMN` ohne Default für nullable Spalten. Da nur `PlaylistId` transportiert wird, genügt **eine** neue Spalte |
| Befüllungs-Guard | `player.IsPlaylistPlayback ? player.PlaylistId : null` an allen drei Erzeugungsstellen | `IsPlaylistPlayback` (`PlaylistId is > 0 && PlaylistEntryId > 0`, `PlayerState.cs` Z. 90) ist der etablierte fachliche Guard für den Playlist-Wiedergabekontext; Snapshot-Semantik bleibt erhalten, da der Effekt mit dem zum Meldezeitpunkt gültigen `PlayerState` befüllt wird |
| JSON-Feldname | `PlaylistId` (PascalCase) — der bestehenden Serialisierungskonvention des Clients folgend | `VideoPlayerApiClient.JsonOptions` (Z. 30–33) setzt nur `PropertyNameCaseInsensitive`, keine Naming-Policy → PascalCase-Feldnamen im Body (bestätigt durch `ProgressReportServiceTests.ReportProgress_Online_SendsDirectlyWithoutQueueing`, der `MediaType`/`MediaId` PascalCase prüft). Die Server-API-Dokumentation zeigt camelCase-Beispiele (`mediaType`, `mediaId`, `playlistId`), ASP.NET Core bindet JSON-Felder jedoch case-insensitiv (Web-Defaults) — der PascalCase-Body funktioniert bereits produktiv |

## Programmabläufe

### Fortschrittsmeldung im Playlist-Kontext (online)

1. `PlayerPage` meldet im 5-s-Takt bzw. beim Ende/Schließen weiterhin nur Position/Dauer (`PlaybackProgressMessage`/`PlaybackEndedMessage`/`PlayerClosedMessage` — unverändert).
2. `AppUpdate.UpdatePlaybackProgress` / `UpdatePlaybackEnded` / `UpdatePlayerClosed` erzeugen den `ReportPlaybackProgressEffect` neu mit `PlaylistId` aus dem aktuellen `PlayerState` (nur bei `IsPlaylistPlayback`, sonst `null`).
3. `ShellAppEffectHandler.ReportPlaybackProgressAsync` reicht das neue Effekt-Feld an `ProgressReportService.ReportProgressAsync` weiter.
4. `ProgressReportService.ReportProgressAsync` übergibt es beim Online-Direktversand an `VideoPlayerApiClient.ReportPlaybackProgressAsync`.
5. `VideoPlayerApiClient` befüllt `PlaybackProgressReport.PlaylistId`; `PostJsonAsync` serialisiert PascalCase-Feldnamen (`"PlaylistId"`, `null`-Werte werden mitserialisiert — Default-Verhalten von `JsonOptions`). Der Server bindet das Feld case-insensitiv an `ProgressRequest.PlaylistId`.

Beteiligte Klassen/Komponenten: `PlayerPage`, `AppUpdate`, `ReportPlaybackProgressEffect`, `ShellAppEffectHandler`, `ProgressReportService`, `VideoPlayerApiClient`, `PlaybackProgressReport`

### Offline-Einreihen und Nachsenden (fehlerfreier Kontexterhalt)

1. Bei Offline, fehlendem Client oder überholbarem Sendefehler ruft `ProgressReportService.ReportProgressAsync` `PendingProgressSyncService.EnqueueAsync` mit dem Playlist-Wert auf.
2. `EnqueueAsync` ersetzt per Transaktion den Vorgänger-Eintrag desselben Mediums (`DELETE … WHERE VideoType=? AND VideoId=?` + `Insert`) und befüllt die neue Spalte `PlaylistId` — Deduplikationssemantik „nur der letzte Stand zählt" bleibt unverändert, der neueste Kontext gewinnt (vom Anwender bestätigt: keine Änderung der Deduplikation).
3. Beim nächsten Sync liest `ServerSyncService.FlushPendingProgressAsync` die Einträge (`GetPendingAsync`) und sendet sie via `client.ReportPlaybackProgressAsync` inklusive der gespeicherten `PlaylistId` nach; danach `RemoveForMediaAsync` wie bisher.
4. `BackupDatabase.InitializeAsync` zieht die neue Spalte in bestehenden Datenbanken über `EnsureColumnAsync` nach (neu angelegte Tabellen enthalten sie direkt über `CreateTableAsync<PendingProgressSyncEntry>`).

Beteiligte Klassen/Komponenten: `ProgressReportService`, `PendingProgressSyncService`, `PendingProgressSyncEntry`, `BackupDatabase`, `ServerSyncService`, `VideoPlayerApiClient`

### Kontextlose Wiedergabe (unverändert)

Wiedergabe ohne Playlist-Kontext (`StartPlayback`-Pfad, `IsPlaylistPlayback == false`) erzeugt den Effekt mit `PlaylistId = null`; der Body enthält `"PlaylistId": null` — der Server behandelt fehlende/`null`-Werte als kontextlos (Parameterdefault `PlaylistId = null` im `ProgressRequest`), fachlich identisch zum Ist-Verhalten.

### Serverseitige Verarbeitung (verifizierter Vertrag, kein Client-Aufwand)

Der Server (`VideoPlayer-server`, `ContinueWatchingService.ReportProgressAsync` Z. 269–286) verarbeitet `PlaylistId` so: `ValidatePlaylistAccessAsync` lehnt Meldungen auf fremde/gesperrte Playlists ab (`403`), `NormalizePlaylistBindingAsync` verwirft den Bezug, wenn das Medium nicht (mehr) in der Playlist steht, und upsertet über den Schlüssel (UserId, MovieId, TVShowEpisodeId, PlaylistId) — ein playlist-gebundener Weiterschauen-Eintrag wird also fortgeschrieben statt eines zweiten, kontextlosen Eintrags. `PlaylistEntryId` in den ausgelieferten `ContinueWatchingDto`s leitet der Server aus der Playlist-Mitgliedschaft ab (Z. 240–251). Clientseitig ist dafür nichts zu tun — die Information dient der Planbegründung.

### E2E-Nachweis über die Mock-Senke

1. `MockVideoServer` erfasst in der Progress-Senke (`POST /api/continue-watching/progress`, Z. 448–462) den Request-Body — analog zum bestehenden Muster `LoginRequestBodies` (Z. 2119–2122): thread-sichere `List<string>` (z. B. `ProgressReportBodies`), optional mit komfortabler Auswertehilfe.
2. Ein E2E-Test startet eine Playlist-Wiedergabe (bestehende Fixture der `PlaylistPlaybackTests`), wartet per Polling auf den ersten Progress-POST (5-s-Takt) und prüft das `playlistId`-Feld (case-insensitive Auswertung via `JsonDocument`) im erfassten Body gegen die erwartete Mock-Playlist-Id.
3. Optionaler Gegenprobe: Fortschrittsmeldung einer kontextlosen Wiedergabe enthält keine bzw. `null`-`playlistId`.

Beteiligte Klassen/Komponenten: `MockVideoServer`, `PlaylistPlaybackTests` bzw. neue Testklasse im E2E-Projekt

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| — | — | Keine neuen Produktivklassen erforderlich (reine Feld-/Parametererweiterung); eine neue E2E-Testklasse (z. B. `PlaylistProgressReportTests`) ist möglich, alternativ Erweiterung der bestehenden `PlaylistPlaybackTests` |

## Änderungen an bestehenden Klassen

### `PlaybackProgressReport` (Datenmodellklasse, `VideoPlayer.ApiClient/Models/ContinueWatching.cs`)

- **Neue Eigenschaften:** `PlaylistId` (`long?`) — Id der Playlist des Wiedergabekontexts, `null` ohne Kontext; wird wie die bestehenden Felder PascalCase (`"PlaylistId"`) serialisiert und serverseitig case-insensitiv an `ProgressRequest.PlaylistId` gebunden

### `VideoPlayerApiClient` (Klasse)

- **Geänderte Methoden:** `ReportPlaybackProgressAsync` (Z. 269–289) — neuer optionaler Parameter `long? playlistId = null` (vor `cancellationToken`); befüllt die neue `PlaybackProgressReport`-Eigenschaft. Bestehende Aufrufstellen bleiben kompilierbar.

### `ReportPlaybackProgressEffect` (MVU-Effekt-Record, `VideoPlayer.Mvu/AppEffect.cs` Z. 651–655)

- **Neue Record-Parameter:** `PlaylistId` (`long?`) — am Ende der Parameterliste, ohne Standardwert (Compiler zwingt alle Erzeugungsstellen); XML-Doc des neuen Parameters analog der bestehenden.

### `AppUpdate` (statische Klasse, zentraler MVU-Reducer)

- **Geänderte Methoden:** `UpdatePlaybackProgress` (Z. 3816–3817) — Effekt zusätzlich mit Playlist-Kontext aus `updated` (`updated.IsPlaylistPlayback ? updated.PlaylistId : null`) befüllen; läuft dem `AdvancePlaylistEntryEffect` weiterhin voraus (Snapshot-Semantik: Meldung gilt dem endenden Eintrag)
- **Geänderte Methoden:** `UpdatePlaybackEnded` (Z. 3919–3920) — gleiche Befüllung aus `updated`
- **Geänderte Methoden:** `UpdatePlayerClosed` (Z. 4082–4083) — gleiche Befüllung aus `player`

### `ProgressReportService` (Klasse)

- **Geänderte Methoden:** `ReportProgressAsync` — neuer optionaler Parameter `long? playlistId = null`; Weitergabe an `client.ReportPlaybackProgressAsync` (Direktversand) und an `pendingQueue.EnqueueAsync` (Einreihen). Fehler-/Outcome-Logik unverändert.

### `PendingProgressSyncEntry` (Datenmodellklasse, `VideoPlayer.Storage/Entities/`)

- **Neue Eigenschaften:** `PlaylistId` (`long?`) — Playlist-Bezug der wartenden Meldung, `NULL` ohne Kontext

### `PendingProgressSyncService` (Klasse)

- **Geänderte Methoden:** `EnqueueAsync` — neuer optionaler Parameter `long? playlistId = null`; der `Insert` befüllt die neue Spalte. Deduplikation (`VideoType`/`VideoId`) unverändert.

### `BackupDatabase` (Klasse)

- **Geänderte Methoden:** `InitializeAsync` — ein zusätzlicher `EnsureColumnAsync`-Aufruf: `("pending_progress", "PlaylistId", "bigint")` (nullable Spalte; Muster wie `DurationTicks`, Z. 45)

### `ServerSyncService` (Klasse)

- **Geänderte Methoden:** `FlushPendingProgressAsync` — beim Nachsenden `entry.PlaylistId` an `client.ReportPlaybackProgressAsync` mitgeben (Z. 122–127)

### `ShellAppEffectHandler` (App-Rand, `VideoPlayer.Maui/Services/`)

- **Geänderte Methoden:** `ReportPlaybackProgressAsync` (Z. 485–504) — `report.PlaylistId` an `ProgressReportService.ReportProgressAsync` weitergeben

### `MockVideoServer` (E2E-Infrastruktur)

- **Neue Member:** `ProgressReportBodies` (`List<string>`, thread-sicher wie `LoginRequestBodies`) — erfasste Request-Bodies der Progress-Senke in Reihenfolge; optional `GetProgressReports()`-Hilfsmethode zum Auswerten (`JsonDocument`, case-insensitive Feldnamensuche für `playlistId`)
- **Geänderte Elemente:** Progress-Senke (`POST /api/continue-watching/progress`, Z. 448–462) — `request.Body` vor der 204-Antwort in `ProgressReportBodies` ablegen; Antwortverhalten unverändert

## Datenbankmigrationen

| Migrationsname | Betroffene Tabellen/Spalten | Beschreibung der Änderung |
|----------------|----------------------------|---------------------------|
| `EnsureColumnAsync`-Aufruf in `BackupDatabase.InitializeAsync` | `pending_progress`.`PlaylistId` | Eine nullable `bigint`-Spalte per `ALTER TABLE … ADD COLUMN` in bestehenden Datenbanken nachziehen (Muster wie `DurationTicks`); neue Datenbanken erhalten die Spalte über `CreateTableAsync<PendingProgressSyncEntry>` |

## Validierungsregeln

| Feld / Objekt | Regel | Fehlerfall |
|---------------|-------|------------|
| `ReportPlaybackProgressEffect.PlaylistId` | Nur befüllen, wenn `player.IsPlaylistPlayback` gilt (`PlaylistId > 0 && PlaylistEntryId > 0`); sonst `null` | Ohne Guard könnte ein halb aufgelöster Kontextzustand (Playlist-Bezug ohne aktiven Eintrag) einen serverseitig nicht auflösbaren Bezug melden; `NormalizePlaylistBindingAsync` würde ihn zwar serverseitig verwerfen, der Guard verhindert ihn aber bereits strukturell |

Es ist keine Eingabevalidierung nötig: der Wert stammt aus dem internen `PlayerState` (serverbestätigte `PlaylistId`), nicht aus Benutzereingaben. Serverseitige Validierung (Zugriff, Mitgliedschaft) ist verifiziert vorhanden (`ValidatePlaylistAccessAsync`, `NormalizePlaylistBindingAsync`).

## Konfigurationsänderungen

Keine.

## Seiteneffekte und Risiken

- **Server-Vertrag (verifiziert):** Der Progress-Body erhält für alle Meldungen ein zusätzliches JSON-Feld `"PlaylistId"` (bei kontextloser Wiedergabe `null`). Der Server-Vertrag ist gegen die Referenz (`VideoPlayer-server`) geprüft: `ProgressRequest` kennt das Feld (`ContinueWatchingController.cs` Z. 54–59); Zugriffsprüfung (`PlaylistAccessDeniedException` → 403) und Normalisierung (Bezug verworfen, wenn Medium nicht mehr in der Playlist) erfolgen serverseitig. Einziger fachlicher Nebeneffekt: Meldungen auf eine Playlist, auf die der Anwender keinen (Schreib-)Zugriff mehr hat, werden mit `403` beantwortet — das fällt in `ProgressReportService` in den bestehenden „permanenter 4xx → verworfen"-Pfad und ist unproblematisch.
- **`OfflineContinueWatchingService`:** Liest `pending_progress` direkt (`LoadPendingProgressAsync`) — die neue Spalte wird ignoriert, kein Eingriff nötig.
- **Backup/Wiederherstellung:** `pending_progress` ist backup-pflichtig. Eine ältere DB wird per `EnsureColumnAsync` migriert; eine neuere DB in einer älteren App-Version hat eine Zusatzspalte, die `sqlite-net` beim Lesen ignoriert (abwärtskompatible Richtung unkritisch).
- **`ReportPlaybackProgressEffect`:** Die Record-Gleichheit umfasst das neue Member — betroffen sind nur explizite Konstruktionen (nur `AppUpdate`); Tests lesen per Pattern-Matching, kein Bruch.
- **Deduplikation bei Kontextwechsel:** Spielt dasselbe Medium abwechselnd playlist-gebunden und kontextlos, gewinnt der zuletzt eingereihte Stand (bestehende „letzter Stand zählt"-Semantik — vom Anwender bestätigt, keine Änderung).
- **Altbestand `VideoWebPlayerClient`/`VideoWebPlayer.Maui`:** Weist dieselbe Lücke auf, bleibt bewusst unverändert (Anforderung betrifft `VideoPlayer.Maui`). Hinweis: Der Anwender hat im Repo `martin-stromberg/VideoPlayer-App` bereits Issue #60 zur Entfernung des Altbestands gestellt — keine Aktion in diesem Issue erforderlich.
- **`PlayerState.PlaylistEntryId`:** Bleibt unverändert relevant (Guard `IsPlaylistPlayback`, Playlist-Navigation) — wird nur nicht mehr im Fortschrittsreport gesendet.

## Umsetzungsreihenfolge

1. **`PlaybackProgressReport` und `VideoPlayerApiClient` erweitern**
   - Voraussetzungen: Keine
   - Beschreibung: Neue Eigenschaft `PlaylistId` (`long?`) anlegen; `ReportPlaybackProgressAsync` um den optionalen Parameter `playlistId` erweitern und in den Body übernehmen.

2. **Persistenz: `PendingProgressSyncEntry`, `BackupDatabase`, `PendingProgressSyncService`**
   - Voraussetzungen: `EnsureColumnAsync`-Migrationsmuster in `BackupDatabase` (vorhanden, Z. 94–103)
   - Beschreibung: Nullable Eigenschaft `PlaylistId` am Entry; ein `EnsureColumnAsync`-Aufruf in `InitializeAsync`; `EnqueueAsync`-Signatur erweitern und Insert befüllen.

3. **MVU-Effekt und Befüllung in `AppUpdate`**
   - Voraussetzungen: `PlayerState.PlaylistId`/`IsPlaylistPlayback` (vorhanden)
   - Beschreibung: `ReportPlaybackProgressEffect` um `PlaylistId` (`long?`, ohne Default) erweitern; alle drei Erzeugungsstellen (`UpdatePlaybackProgress`, `UpdatePlaybackEnded`, `UpdatePlayerClosed`) mit dem `IsPlaylistPlayback`-Guard befüllen.

4. **Service-Durchreichung: `ProgressReportService`, `ServerSyncService`, `ShellAppEffectHandler`**
   - Voraussetzungen: Schritte 1–3 (erweiterte Signaturen von `ReportPlaybackProgressAsync` und `EnqueueAsync`; neues Effekt-Feld)
   - Beschreibung: `ReportProgressAsync` um optionalen `playlistId`-Parameter erweitern und an Direktversand bzw. Einreihen weiterreichen; `FlushPendingProgressAsync` gibt `entry.PlaylistId` mit; `ShellAppEffectHandler.ReportPlaybackProgressAsync` reicht das Effekt-Feld durch.

5. **Unit-/Contract-Tests**
   - Voraussetzungen: Schritte 1–4; Testinfrastruktur `StubHttpMessageHandler` (Body-Aufzeichnung), `SqliteDatabaseCollection`, `PlayingInPlaylist`-Fixture (vorhanden)
   - Beschreibung: Neue Tests gemäß Abschnitt „Tests" (API-Body, drei Meldungspfade, Direktversand/Einreihen, Queue-Persistenz, Flush).

6. **E2E: `MockVideoServer` Body-Erfassung + Playlist-Fortschritts-E2E-Test**
   - Voraussetzungen: Schritte 1–4 (App sendet das Feld); `MockVideoServer` mit Body-Aufzeichnungsmuster `LoginRequestBodies` (vorhanden); `VideoPlayer.E2E.Tests`-Infrastruktur (`E2ETestBase`, `AppFixture`, vorhanden)
   - Beschreibung: `ProgressReportBodies` in der Senke erfassen; E2E-Test: Playlist-Wiedergabe starten, auf Progress-POST warten, `playlistId` im Body prüfen.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `ReportPlaybackProgress_SendsPlaylistId` | `VideoPlayerApiClientTests_Endpoints` | Request-Body von `POST api/continue-watching/progress` enthält `PlaylistId` (PascalCase) bei Übergabe des Parameters |
| `ReportPlaybackProgress_WithoutPlaylist_OmitsContext` | `VideoPlayerApiClientTests_Endpoints` | Aufruf ohne Playlist-Parameter → Body ohne bzw. mit `null`-`PlaylistId` (Rückwärtsverhalten) |
| `PlaybackProgress_InPlaylist_ReportsPlaylistId` | `AppUpdateTests_PlaylistPlayback` | Takt-`ReportPlaybackProgressEffect` trägt die `PlaylistId` des `PlayerState` (Fixture `PlayingInPlaylist`) |
| `PlaybackEnded_InPlaylist_ReportsPlaylistId` | `AppUpdateTests_PlaylistPlayback` | Abschluss-Effekt trägt die zum Meldezeitpunkt gültige `PlaylistId` des endenden Stands (Snapshot-Semantik neben `AdvancePlaylistEntryEffect`) |
| `PlayerClosed_InPlaylist_ReportsPlaylistId` | `AppUpdateTests_PlaylistPlayback` | Schließen-Effekt trägt `PlaylistId` |
| `PlaybackProgress_WithoutPlaylist_ReportsNullPlaylistId` | `AppUpdateTests_Player` | Kontextlose Meldung trägt `PlaylistId = null` |
| `ReportProgress_Online_SendsPlaylistId` | `ProgressReportServiceTests` | Direktversand: aufgezeichneter Request-Body enthält `PlaylistId` |
| `ReportProgress_Offline_EnqueuesPlaylistId` | `ProgressReportServiceTests` | Einreihen: `pending_progress`-Eintrag speichert `PlaylistId` |
| `Enqueue_StoresPlaylistId` / `Enqueue_SameMedia_LatestContextWins` / `Pending_PlaylistId_SurvivesDatabaseReopen` | `PendingProgressSyncServiceTests` | Persistenz der neuen Spalte, Deduplikations-Semantik bei Kontextwechsel, Überleben über DB-Neuöffnung |
| `PendingProgress_PlaylistColumn_MigratedOnExistingDatabase` | `PendingProgressSyncServiceTests` (oder neue Storage-Testklasse) | `EnsureColumnAsync` zieht die Spalte in einer ohne sie angelegten `pending_progress`-Tabelle nach |
| `FlushPendingProgress_SendsStoredPlaylistId` | `ServerSyncServiceTests` | Nachgesendeter Request-Body enthält die gespeicherte `PlaylistId` |
| `ProgressReportBodies` (Hilfsmember) | `MockVideoServer` (E2E-Support) | Thread-sichere Liste der empfangenen Progress-Request-Bodies (Muster `LoginRequestBodies`) |
| `PlaylistPlayback_ProgressReport_IncludesPlaylistId` | `PlaylistPlaybackTests` oder neue `PlaylistProgressReportTests` (E2E) | Happy Path: nach Playlist-Wiedergabestart enthält der erste Progress-POST das `playlistId`-Feld (case-insensitive Auswertung) mit der Mock-Playlist-Id |
| `Playback_ProgressReport_WithoutPlaylist_HasNullPlaylistId` | `PlaylistProgressReportTests` (E2E) | Gegenprobe: kontextlose Wiedergabe sendet `playlistId` als `null`/fehlend |

### Betroffene bestehende Tests

Keine — alle Signaturerweiterungen sind abwärtskompatibel (optionale `= null`-Parameter); `ReportPlaybackProgressEffect` wird in Tests nur per Pattern-Matching gelesen, nicht konstruiert; die `MockVideoServer`-Senke bleibt bei 204 und nimmt lediglich eine zusätzliche Aufzeichnung vor. Neue Assertions werden als neue Testmethoden ergänzt (siehe oben); bestehende Tests wie `PlaybackProgress_ReportsChangedPosition` oder `ReportProgress_Online_SendsDirectlyWithoutQueueing` bleiben unverändert gültig.

### E2E-Tests (primärer Funktionsnachweis)

Die Anforderung ist ein über eine Nutzeraktion erreichbarer Ablauf (Playlist-Wiedergabe starten → Fortschrittsmeldung geht an den Server), aber kein sichtbarer UI-Effekt: Der Nachweis, dass `PlaylistId` tatsächlich im HTTP-Body ankommt, kann durch Unit-Tests nur bis zur Service-Schicht geführt werden — die Verdrahtung durch den App-Rand (`ShellAppEffectHandler` → `ProgressReportService` → echter HTTP-Request) wird erst im E2E-Lauf gegen die `MockVideoServer`-Senke end-to-end belegt. Deshalb wird die Senke um eine Body-Aufzeichnung erweitert und der Empfang des `playlistId`-Felds als E2E-Szenario geplant (Pflicht).

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| Pflicht | Playlist-Wiedergabe starten und laufen lassen → der erste `POST /api/continue-watching/progress` enthält `playlistId` der aktiven Playlist-Wiedergabe | `VideoPlayer.E2E.Tests/Tests/PlaylistPlaybackTests.cs` oder neue `PlaylistProgressReportTests.cs` | Meldung im Playlist-Kontext führt die `PlaylistId` | Nachweis der kompletten Kette inkl. App-Rand (`ShellAppEffectHandler`) und realem HTTP-Versand — Unit-Tests decken nur Reducer/Service-Schichten isoliert ab |
| Soll | Kontextlose Wiedergabe (z. B. Film direkt) → Progress-POST ohne Playlist-Bezug (`null`-Feld) | `PlaylistProgressReportTests.cs` | Wiedergabe ohne Playlist-Kontext bleibt unverändert | Regression gegen versehentliche Kontextbefüllung; über denselben realen Meldepfad auslösbar |

Welche bestehenden E2E-Tests müssen angepasst werden?

Keine — die `MockVideoServer`-Erweiterung ist rein additiv (Aufzeichnung der Bodies, unveränderte 204-Antwort); `PlaylistPlaybackTests`, `ContinueWatchingPlaybackTests` und `PlaybackTests` laufen unverändert weiter.

## Offene Punkte

Keine.
