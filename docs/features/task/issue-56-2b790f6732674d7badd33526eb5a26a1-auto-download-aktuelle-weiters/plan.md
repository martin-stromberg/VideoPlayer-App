# Umsetzungsplan: Auto-Download lädt die aktuelle Weiterschauen-Episode mit und zählt sie ins Episoden-Budget

## Übersicht

Der automatische Download-Vorlauf (`AutoDownloadService.RunAsync`) plant für Episoden-Einträge der Weiterschauen-Liste künftig auch die **aktuelle Episode** (`item.Id`) als erste Kandidatin ein — auf dem Serien-Pfad (`EnqueueNextEpisodesAsync`) ebenso wie auf dem Playlist-Pfad (`EnqueueNextPlaylistEntriesAsync`). `AppSettings.PreloadEpisodeCount` begrenzt damit die Gesamtzahl der Kandidaten je Weiterschauen-Eintrag **inklusive** der aktuellen Episode; bei playlist-gebundenen Filmen zählt der Film selbst ebenfalls ins Budget (Film + Folge-Einträge ≤ `PreloadEpisodeCount`). Betroffen ist ausschließlich `VideoPlayer.Mvu/AutoDownloadService.cs` plus Dokumentation (`docs/help/downloads.md`) und `AutoDownloadServiceTests`; `DownloadService`, Trigger-Kette (`RunAutoDownloadsEffect`), `AppSettings`, Manifest und der `DeleteWatchedDownloadEffect`-Pfad bleiben unverändert.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Selbst-Einreihung im Serien-Pfad | Kandidatenmenge beginnt ab `index` der gefundenen aktuellen Episode statt `index + 1` (`episodes.Skip(index).Take(settings.PreloadEpisodeCount)`) | Die aktuelle Episode erhält denselben Titel- (`{showName} – {episode.Name}`) und `DownloadSeriesContext`-Aufbau wie Folge-Episoden; kleinstmögliche Änderung ohne neue Methode oder neuen Ablauf |
| Selbst-Einreihung im Playlist-Pfad | Der gefundene aktuelle Eintrag (`entry.Id == item.PlaylistEntryId`) durchläuft **stets** denselben Ladbarkeitsfilter wie Folge-Einträge und wird als erster Kandidat über `entry.MediaId`, `PlaylistEntryTitle(entry)`, `SeriesContextOf(entry)`, `DownloadPlaylistContext(item.PlaylistId, entry.Id, entry.SortOrder)` eingeplant — **ohne neuen Parameter** | Anwender-Entscheidung (Punkt 2): der playlist-gebundene Film zählt ins `PreloadEpisodeCount`-Budget (Film + Folge-Einträge ≤ `PreloadEpisodeCount`). Beide Aufrufpfade (Film und Episode) teilen damit identische Semantik — die ursprünglich geplante Unterscheidung per `includeCurrentEntry`-Parameter entfällt. Im Film-Zweig ist der eigene Eintrag bereits direkt eingereiht; der erneute `EnqueueAsync`-Aufruf trifft auf die Deduplizierung (`NoticeAlreadyQueued`/`NoticeAlreadyRunning`), verbraucht aber dennoch einen `remaining`-Platz — exakt die geforderte Budget-Wirkung ohne Sonderlogik |
| Episoden-Eintrag ohne `ShowId` und ohne Playlist-Treffer (Orphan) | Selbst-Einreihung analog zum Film-Zweig: `EnqueueAsync("tvshowepisode", item.Id, item.Title, …)` mit `DownloadPlaylistContext(item.PlaylistId, item.PlaylistEntryId, null)` bei Playlist-Bindung, sonst ohne Kontexte | Anwender-Entscheidung (Punkt 1): die aktuelle Episode wird dennoch direkt eingereiht — der Selbst-Download benötigt keinen Serienkontext, nur die Folge-Auflösung entfällt |
| Ladbarkeit des aktuellen Playlist-Eintrags | Ein nicht ladbarer aktueller Eintrag (`IsAccessible == false` oder Typ ∉ `movie`/`tvshowepisode`) wird wie ein Folge-Eintrag übersprungen — ohne Budget-Verbrauch, `found` bleibt gesetzt (kein Serien-Rückfall) | Anwender-Entscheidung (Punkt 3): dieselbe Behandlung wie Folge-Einträge; im Film-Zweig führt dieser Randfall dazu, dass die Folge-Einträge das volle Budget erhalten, weil der eigene Eintrag keinen Platz verbraucht |
| Budget-Semantik von `PreloadEpisodeCount` | Bestehende Zählweise je Kandidat (Serien-Pfad `Take`, Playlist-Pfad `remaining--` unabhängig vom `Notice`) wird unverändert auf die aktuelle Episode bzw. den aktuellen Eintrag ausgeweitet | Konsistent (Anwender-Entscheidung, Punkt 4): auch deduplizierte/abgelehnte Kandidaten — etwa eine bereits geladene aktuelle Episode oder der bereits eingereihte Film — verbrauchen Budget; Deduplizierung übernimmt `DownloadService.EnqueueAsync` |

## Programmabläufe

### Vorlauf-Lauf (`RunAsync`) — Dispatch je Weiterschauen-Eintrag

1. Guards unverändert: `AutoDownloadEnabled` aus, `WifiOnlyDownloads && !IsWifi`, `Client is null`, Reentrancy-Sperre (`runInProgress`), Throttle (`MinRunInterval`, umgangen bei `force`).
2. `homeLists.LoadHomeListsAsync().ContinueWatching` iterieren; `type = MediaTypeNames.Normalize(item.MediaType)`; `hasPlaylistBinding = item.PlaylistId is > 0 && item.PlaylistEntryId is > 0`.
3. **Film-Zweig (`type == "movie"`):** Film selbst wie bisher direkt einreihen (`EnqueueAsync("movie", item.Id, item.Title, …)`, mit `DownloadPlaylistContext` bei Bindung); bei Bindung `EnqueueNextPlaylistEntriesAsync(item, settings, …)` — der gefundene eigene Eintrag läuft nun ebenfalls durch die Kandidatenbehandlung: Der erneute `EnqueueAsync`-Aufruf wird von der Deduplizierung verworfen, verbraucht aber einen `remaining`-Platz — Film + Folge-Einträge zusammen ≤ `PreloadEpisodeCount`.
4. **Episoden-Zweig (`type == "tvshowepisode"`):**
   - Bei Playlist-Bindung zuerst `EnqueueNextPlaylistEntriesAsync(item, settings, …)` — Rückgabe ≠ `null` → Anzahl übernehmen, Eintrag abgeschlossen (der gefundene Eintrag ist erste Kandidatin).
   - Rückgabe `null` (kein Eintrag gefunden / Ladefehler) oder keine Bindung → bei `item.ShowId is > 0` `EnqueueNextEpisodesAsync` (Serien-Pfad).
   - Weder Playlist-Treffer noch `ShowId` → **Selbst-Einreihung** der aktuellen Episode (siehe „Orphan-Fallback").
5. Fehlerbehandlung unverändert: `OperationCanceledException` durchreichen; `ApiException`, `HttpRequestException`, `TaskCanceledException`, `InvalidOperationException` je Eintrag als Warning protokollieren und den Lauf fortsetzen; Abschluss-Log mit `EnqueuedCount`.

Beteiligte Klassen/Komponenten: `AutoDownloadService`, `HomeListService`, `HomeMediaItem`, `HomeLists`, `MediaTypeNames`, `DownloadService`, `AuthSessionService`.

### Serien-Pfad (`EnqueueNextEpisodesAsync`) — aktuelle Episode als erste Kandidatin

1. Guard unverändert (`Client is null || item.ShowId is not > 0` → `0`).
2. `client.GetTvShowAsync(item.ShowId.Value)` → `ShowDetailsCache.WriteAsync(cache, show, …)` (Cache-Ablage unverändert).
3. Flache, staffelübergreifend geordnete Episodenliste aufbauen (`OrderBy` Staffelnummer, dann Episodennummer) — unverändert.
4. `index = episodes.FindIndex(e => e.Episode.Id == item.Id)`; `index < 0` → `0` zurück — unverändert.
5. **Neu:** Kandidaten = `episodes.Skip(index).Take(settings.PreloadEpisodeCount)` — die aktuelle Episode ist die erste Kandidatin; bei `PreloadEpisodeCount = 1` wird ausschließlich sie eingeplant.
6. Je Kandidat unverändert: Titel `{showName} – {episode.Name}` (Fallback `{showName} – Episode {episode.Number}`), Kontext `new DownloadSeriesContext(show.Id, showName, season.Number, episode.Number)` → `downloads.EnqueueAsync("tvshowepisode", episode.Id, …)`; nur `NoticeEnqueued` zählt in `enqueued`.
7. Log-Text und XML-Dokumentation anpassen („Folge-Episoden" → aktuelle und folgende Episoden); Rückgabe der Anzahl.

Beteiligte Klassen/Komponenten: `AutoDownloadService`, `VideoPlayerApiClient` (`GetTvShowAsync`), `ShowDetailsCache`, `DownloadService`, `DownloadSeriesContext`.

### Playlist-Pfad (`EnqueueNextPlaylistEntriesAsync`) — gefundener Eintrag als erste Kandidatin

1. Guards unverändert (`Client is null`, `PlaylistId`/`PlaylistEntryId` nicht `> 0` → `null` = Serien-Rückfall).
2. Seitenschleife unverändert: `GetPlaylistEntriesPagedAsync(item.PlaylistId.Value, page, PlaylistEntriesPageSize, …)` solange `remaining = settings.PreloadEpisodeCount > 0`; Ladefehler (`ApiException`/`HttpRequestException`/`InvalidOperationException`/`JsonException`) → Warning + `null`.
3. Einträge vor dem aktuellen Eintrag werden unverändert übersprungen.
4. **Neu:** der gefundene aktuelle Eintrag wird nicht mehr nur markiert (`found = true`) und übersprungen, sondern fällt stets als erster Kandidat in die reguläre Kandidatenbehandlung: Ladbarkeitsfilter (`Normalize(entry.MediaType)` ∈ `movie`/`tvshowepisode` und `entry.IsAccessible`) → bei Ladbarkeit `EnqueueAsync(type, entry.MediaId, PlaylistEntryTitle(entry), settings, SeriesContextOf(entry), new DownloadPlaylistContext(item.PlaylistId, entry.Id, entry.SortOrder))` und `remaining--`. Im Film-Zweig landet der eigene Eintrag dabei in der `EnqueueAsync`-Deduplizierung (bereits direkt eingereiht) und verbraucht trotzdem einen Budgetplatz; ein nicht ladbarer aktueller Eintrag wird wie ein Folge-Eintrag übersprungen (kein Budget-Verbrauch, `found` bleibt gesetzt).
5. Folge-Einträge danach wie bisher in Server-Reihenfolge bis `remaining` erschöpft; `!result.HasNextPage` beendet die Schleife.
6. `!found` → `null` (Serien-Rückfall) unverändert; sonst Anzahl der `NoticeEnqueued`-Ergebnisse; Log-/XML-Texte anpassen.

Beteiligte Klassen/Komponenten: `AutoDownloadService`, `VideoPlayerApiClient` (`GetPlaylistEntriesPagedAsync`), `PlaylistEntry`, `PlaylistEntriesPagedResult`, `PlaylistEntryTitle`, `SeriesContextOf`, `DownloadPlaylistContext`, `DownloadService`.

### Orphan-Fallback (Episoden-Eintrag ohne Playlist-Treffer und ohne `ShowId`)

1. Erreicht der Episoden-Zweig weder den Playlist- noch den Serien-Pfad (`playlistResult is null && item.ShowId is not > 0`), wird die aktuelle Episode direkt eingeplant: `EnqueueAsync("tvshowepisode", item.Id, item.Title, settings, seriesContext: null, playlistContext: hasPlaylistBinding ? new DownloadPlaylistContext(item.PlaylistId, item.PlaylistEntryId, null) : null)` — symmetrisch zur Selbst-Einreihung des Film-Zweigs.
2. `NoticeEnqueued` zählt in `enqueued`; Dedup/Quota übernimmt `DownloadService` unverändert.

Beteiligte Klassen/Komponenten: `AutoDownloadService`, `DownloadService`, `DownloadPlaylistContext`.

## Neue Klassen

Keine.

## Änderungen an bestehenden Klassen

### `AutoDownloadService` (Klasse, `VideoPlayer.Mvu/AutoDownloadService.cs`)

- **Geänderte Methoden:**
  - `RunAsync` — Episoden-Zweig: neuer Fallback-Zweig (Selbst-Einreihung der Orphan-Episode, siehe „Orphan-Fallback"). Die Aufrufe von `EnqueueNextPlaylistEntriesAsync` bleiben signaturgleich (Film- und Episoden-Zweig) — die neue Budget-Semantik ergibt sich allein aus der Methode: Im Film-Zweig wird der eigene Eintrag dedupliziert und verbraucht dennoch einen `remaining`-Platz.
  - `EnqueueNextEpisodesAsync` — Kandidatenmenge `episodes.Skip(index)` statt `Skip(index + 1)`; XML-Dokumentation und Log-Text an die neue Semantik „aktuelle und folgende Episoden" anpassen.
  - `EnqueueNextPlaylistEntriesAsync` — der gefundene aktuelle Eintrag wird stets als erster Kandidat der bestehenden Ladbarkeits-/Einreih-Behandlung zugeführt und verbraucht einen Platz des `remaining`-Budgets (**kein neuer Parameter** — der ursprünglich geplante `includeCurrentEntry`-Schalter ist durch die Anwender-Entscheidung zum Film-Budget obsolet); XML-Dokumentation und Log-Text anpassen („aktuelle und folgende Einträge").
- **Geänderte XML-Dokumentation:** Klassen-`<summary>` („nächste Folge-Videos" → „aktuelle und folgende Videos/Episoden"; Film-Satz um die Budget-Regel „Film + Folge-Einträge ≤ `PreloadEpisodeCount`" ergänzen).

### `DownloadService` (`VideoPlayer.Mvu/DownloadService.cs`)

Keine Änderung — `EnqueueAsync` (Dedup, Kontingent-Vorabprüfung, `DownloadEnqueueResult`/`NoticeEnqueued`) wird unverändert wiederverwendet.

### `docs/help/downloads.md` (Dokumentation)

- Abschnitt „Automatische Downloads (Vorlauf)" (Z. 85–97): Formulierung „die nächsten **N** Episoden" auf „die aktuelle Episode und die folgenden Episoden — insgesamt bis zu **N** je Weiterschauen-Eintrag" anpassen; Playlist-Absatz entsprechend („ab dem aktuellen Playlist-Eintrag" → „ab einschließlich des aktuellen Playlist-Eintrags") und den Satz zum playlist-gebundenen Film präzisieren („lädt sich selbst plus die folgenden Playlist-Einträge — der Film zählt dabei ins Budget der **N** Einträge").

## Datenbankmigrationen

Keine.

## Validierungsregeln

Keine — es entstehen keine neuen Eingaben; der bestehende Ladbarkeitsfilter (`movie`/`tvshowepisode`, `IsAccessible`) und die `EnqueueAsync`-Ablehnungen (`NoticeInvalidMedia`, `NoticeQuotaExceeded`) gelten unverändert auch für die aktuelle Episode.

## Konfigurationsänderungen

Keine. Bestehende Einstellungen gelten unverändert: `AppSettings.AutoDownloadEnabled`, `AppSettings.PreloadEpisodeCount` (1–10, Default 3 — Semantik präzisiert: Kandidaten inklusive aktueller Episode bzw. inklusive des playlist-gebundenen Films; die `SettingsPage`-Beschriftung „Anzahl vorzuladender Episoden" bleibt zutreffend), `AppSettings.WifiOnlyDownloads`, `AppSettings.DownloadQuotaGb`, `AppSettings.AutoDeleteWatchedEpisodes`.

## Seiteneffekte und Risiken

- **Budget-Semantik für Anwender:** Bei unverändertem `PreloadEpisodeCount` wird je Episoden-Eintrag eine Folge-Episode weniger vorgeladen, dafür zusätzlich die aktuelle Episode; bei playlist-gebundenen Filmen wird ein Folge-Eintrag weniger geladen (Film + Folge-Einträge ≤ `PreloadEpisodeCount`). Die Gesamtzahl der Kandidaten je Eintrag bleibt gleich, das Kontingent-Verhalten (`DownloadQuotaGb`) ist unverändert durchgesetzt.
- **Wechselspiel mit „als gesehen löschen":** Die jetzt ebenfalls eingeladene aktuelle Episode kann nach dem Schauen über `DeleteWatchedDownloadEffect` entfernt werden — erwünschtes Verhalten; die nächste Weiterschauen-Episode wird beim nächsten Lauf dann selbst aktuelle Episode. Kein Risiko einer Endlosschleife: gesehene Episoden verlassen die Weiterschauen-Liste.
- **`OfflineContinueWatchingService`/`ShowDetailsCache`:** Manifest-Einträge der aktuellen Episode tragen nun Serien-/Playlist-Kontext (Playlist-Pfad: `SeriesContextOf`/`DownloadPlaylistContext`; Serien-Pfad: `DownloadSeriesContext`) — konsistent zu Folge-Episoden, kein Risiko.
- **Doppelter `EnqueueAsync`-Aufruf im Film-Zweig:** Der playlist-gebundene Film wird einmal direkt (`item.Id`, `item.Title`, `DownloadPlaylistContext` mit `SortOrder = null`) und einmal als Playlist-Kandidat (`entry.MediaId`, `PlaylistEntryTitle(entry)`, `SortOrder = entry.SortOrder`) eingerеiht; die Deduplizierung verwirft den zweiten Aufruf, sodass der Manifest-Eintrag vom ersten Aufruf stammt (Playlist-`SortOrder` des Films bleibt wie bisher `null`). Bei inkonsistenten Daten (`entry.MediaId != item.Id`) würde ein abweichendes Medium eingeplant — verbraucht ebenfalls nur Budget, kein Risiko.
- **Bestandstests:** Mehrere `AutoDownloadServiceTests` erwarten das alte „nur Folge-Episoden"-Verhalten und brechen vorgesehen (siehe „Betroffene bestehende Tests"); `Run_PlaylistMovie_EnqueuesSelfAndNextEntries` bricht nun ebenfalls (Erwartung: 3 Einträge → 2 bei `PreloadEpisodeCount = 2`).
- **Einreih-Reihenfolge:** Die aktuelle Episode wird vor den Folge-Episoden eingereiht — Manifest-/FIFO-Reihenfolge setzt sie damit vor die Folge-Episoden (gewünscht: Priorität auf dem laufenden Titel).

## Umsetzungsreihenfolge

1. **`EnqueueNextPlaylistEntriesAsync` erweitern**
   - Voraussetzungen: Keine.
   - Beschreibung: Den gefundenen aktuellen Eintrag stets als ersten Kandidaten der bestehenden Ladbarkeits-/Einreih-Behandlung zuführen (`remaining--` je Kandidat, auch bei Dedup-Treffer); XML-Dokumentation und Log-Text anpassen.

2. **`EnqueueNextEpisodesAsync` anpassen**
   - Voraussetzungen: Keine.
   - Beschreibung: Kandidatenmenge auf `episodes.Skip(index).Take(settings.PreloadEpisodeCount)` ändern; XML-Dokumentation und Log-Text anpassen.

3. **`RunAsync` aktualisieren**
   - Voraussetzungen: Schritt 1 (aktuelle Eintrag ist Kandidat — Film-Budget ergibt sich daraus).
   - Beschreibung: Orphan-Fallback (Selbst-Einreihung bei `playlistResult is null && item.ShowId is not > 0`) einbauen; die Aufrufe von `EnqueueNextPlaylistEntriesAsync` bleiben unverändert (kein neuer Parameter).

4. **Dokumentation anpassen**
   - Voraussetzungen: Schritte 1–3 (Verhalten final).
   - Beschreibung: Klassen-`<summary>` des `AutoDownloadService` und `docs/help/downloads.md` (Abschnitt „Automatische Downloads (Vorlauf)") auf „aktuelle und folgende Episoden" sowie die Film-Budget-Regel umformulieren.

5. **Bestandstests anpassen**
   - Voraussetzungen: Schritte 1–3.
   - Beschreibung: Die unter „Betroffene bestehende Tests" gelisteten `AutoDownloadServiceTests` auf die neue Kandidatenmenge umstellen.

6. **Neue Tests schreiben**
   - Voraussetzungen: Schritte 1–3.
   - Beschreibung: Neue Fälle gemäß „Neue Tests" (Selbst-Einreihung Serien-/Playlist-Pfad, Budget-Zählung inkl. Dedup, Orphan-Fallback, nicht ladbarer aktueller Eintrag).

7. **Testlauf**
   - Voraussetzungen: Schritte 5–6.
   - Beschreibung: `dotnet build VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj --configuration Release` und `dotnet test … --no-build --configuration Release` — alle Tests erfolgreich.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `Run_EpisodeCandidate_EnqueuesCurrentFirst` | `AutoDownloadServiceTests` | Serien-Pfad: die aktuelle Episode ist der erste Manifest-Eintrag (Manifest-Reihenfolge = Einplanungsreihenfolge), danach folgen die Folge-Episoden; Titel „{Serie} – {Episode}" und `ShowId`/`SeasonNumber`/`EpisodeNumber` auch am Selbst-Eintrag |
| `Run_EpisodeCandidate_AlreadyDownloaded_DeduplicatesAndCountsBudget` | `AutoDownloadServiceTests` | Bereits `completed` vorhandene aktuelle Episode (Eintrag + Datei über `EnqueueAsync` mit Stub-Stream oder direktem Manifest-/Datei-Aufbau) wird dedupliziert (`NoticeAlreadyPresent`, kein Doppel-Eintrag) und verbraucht trotzdem Budget — bei `PreloadEpisodeCount = 2` landen nur aktuelle + eine Folge-Episode im Manifest |
| `Run_PlaylistEpisode_EnqueuesCurrentEntry` | `AutoDownloadServiceTests` | Playlist-Pfad: der gefundene aktuelle Eintrag steht selbst im Manifest mit `PlaylistId`, `PlaylistEntryId`, `PlaylistSortOrder` und Serien-Hints aus `ParentMedia*`; er verbraucht Budget (bei `PreloadEpisodeCount = 2`: aktueller Eintrag + ein Folge-Eintrag) |
| `Run_PlaylistEpisode_CurrentEntryNotAccessible_SkipsSelf` | `AutoDownloadServiceTests` | Aktueller Playlist-Eintrag mit `IsAccessible = false` bzw. nicht ladbarem Typ wird wie ein Folge-Eintrag übersprungen (kein Budget-Verbrauch), `found` bleibt gesetzt — kein Serien-Rückfall, kein `api/items/tvshow`-Request; Folge-Einträge füllen das Budget |
| `Run_PlaylistEntryNotFoundNoShow_EnqueuesSelf` | `AutoDownloadServiceTests` | Playlist-Bindung, aktueller Eintrag nicht gefunden und kein `ShowId` → Selbst-Einreihung der Episode mit `DownloadPlaylistContext` (`PlaylistId`, `PlaylistEntryId`), kein Serienkontext |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `Run_EpisodeCandidate_EnqueuesUpcomingEpisodes` (`AutoDownloadServiceTests`) | Erwartung ändert sich: bei `PreloadEpisodeCount = 2` Einträge 32 (aktuell) + 33 statt 33 + 34 |
| `Run_EpisodeCandidate_CrossesIntoNextSeason` (`AutoDownloadServiceTests`) | Bei `PreloadEpisodeCount = 1` nur noch die aktuelle Episode; Staffelwechsel-Nachweis erfordert `PreloadEpisodeCount = 2` → Einträge 36 (aktuell) + 37 (S2E1, Serienkontext) |
| `Run_PlaylistEpisode_EnqueuesPlaylistOrder` (`AutoDownloadServiceTests`) | Erwartung: Einträge 32 (aktuell, Eintrag 101) + 99 (Eintrag 102) statt 99 + 33; SortOrder-Nachweis ggf. auf Eintrag 102 verlegen |
| `Run_PlaylistEpisode_SkipsNonDownloadableEntries` (`AutoDownloadServiceTests`) | Erwartung: Einträge 32 (aktuell) + 62 statt 62 + 63 |
| `Run_PlaylistEpisode_PersistsSeriesHints` (`AutoDownloadServiceTests`) | Bei `PreloadEpisodeCount = 1` nur noch die aktuelle Episode im Manifest; Serien-Hint-Nachweis erfordert `PreloadEpisodeCount = 2` (Folge-Eintrag 102 weiterhin geprüft) oder Parent-Angaben am aktuellen Eintrag |
| `Run_PlaylistMovie_EnqueuesSelfAndNextEntries` (`AutoDownloadServiceTests`) | **Neu betroffen (Anwender-Entscheidung Punkt 2):** der Film zählt ins Budget — der eigene Playlist-Eintrag (Eintrag 101) läuft durch die Kandidatenbehandlung, wird per `EnqueueAsync`-Dedup verworfen und verbraucht den ersten `remaining`-Platz; bei `PreloadEpisodeCount = 2` landen daher Film 7 + ein Folge-Eintrag 8 im Manifest statt 7 + 8 + 9. Geprüft wird: genau 2 Einträge (7, 8), `PlaylistId = 5` an beiden, kein Doppel-Eintrag für VideoId 7 |
| `Run_PlaylistEntryNotFound_FallsBackToSeries` (`AutoDownloadServiceTests`) | Serien-Rückfall enthält nun die aktuelle Episode: Einträge 32 + 33 statt 33 + 34 |
| `Run_PlaylistEntryMissing_FallsBackToSeries` (`AutoDownloadServiceTests`) | Bei `PreloadEpisodeCount = 1` nur noch die aktuelle Episode (32) statt Folge-Episode 33 |
| `Run_EpisodeWithoutSeason_SkipsCandidate` (`AutoDownloadServiceTests`) | Verhaltensänderung (Orphan-Selbst-Einreihung): in `Run_EpisodeWithoutSeason_EnqueuesSelf` umwandeln — ein Manifest-Eintrag (VideoId 32, `tvshowepisode`, ohne Serienkontext), kein `api/items/tvshow`-Request; Stream-Response für den gestarteten Transfer stubben oder Fehlschlag tolerieren |

### E2E-Tests (primärer Funktionsnachweis)

Keine E2E-Tests erforderlich. Die Anforderung berührt keinen Benutzerfluss: Es gibt keine neue oder geänderte Benutzerinteraktion, kein UI-Element und keinen geänderten Trigger — der Vorlauf ist ein Hintergrunddienst, dessen Auslöser (Synchronisation, Listenänderung, Einschalten der Einstellung) und dessen sichtbare Oberfläche (Einstellungs-Schalter, Downloads-Liste) unverändert bleiben. Der einzige Unterschied liegt in der internen Kandidatenauswahl des `AutoDownloadService`, die sich ausschließlich über den Manifest-Inhalt beobachten lässt und über die UI nicht separat auslösbar ist. Für Downloads existiert im Repository zudem keine E2E-Abdeckung und keine UI-E2E-Konvention für diesen Bereich (`VideoPlayer.E2E.Tests` enthält kein Download-Szenario und ist nicht CI-Bestandteil); das Verhalten des Vorlaufs wird projektkonventionell über `AutoDownloadServiceTests` mit echter SQLite-Ablage nachgewiesen (so bereits bei den Download-Schritten aus Issue #9/#51).

Bestehende E2E-Tests müssen angepasst werden: Keine.

## Offene Punkte

Keine — alle vier Klärungsfragen sind durch die Anwender-Antworten entschieden und als Designentscheidungen eingearbeitet (Orphan-Fallback bleibt; Film zählt ins `PreloadEpisodeCount`-Budget; nicht ladbarer aktueller Eintrag wird übersprungen; deduplizierte aktuelle Episode verbraucht Budget).
