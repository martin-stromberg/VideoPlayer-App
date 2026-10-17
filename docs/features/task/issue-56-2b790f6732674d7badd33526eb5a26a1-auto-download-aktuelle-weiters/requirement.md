# Übersetzte Anforderung: Auto-Download lädt die aktuelle Weiterschauen-Episode mit und zählt sie ins Episoden-Budget

Quelle: Issue #56 — Folgethema zu Issue #51 (Download optimieren) und dem Auto-Download-Vorlauf aus dem Weiterschauen-Feature (#43).

## Fachliche Zusammenfassung

Der automatische Download-Vorlauf (`AutoDownloadService.RunAsync`) reiht für Episoden-Einträge der Weiterschauen-Liste (`HomeLists.ContinueWatching`, `HomeMediaItem`) bislang nur die **Folge-Episoden** ein — über den Serienkontext (`EnqueueNextEpisodesAsync`, Serienstruktur via `api/items/tvshow/{showId}`) bzw. bei Playlist-Bezug über die Playlist-Reihenfolge (`EnqueueNextPlaylistEntriesAsync`, `api/playlists/{id}/entries/paged`). Die jeweils aktuell abspielbare Episode eines Weiterschauen-Eintrags wird neu als erste Kandidatin ebenfalls über `DownloadService.EnqueueAsync` eingeplant; `AppSettings.PreloadEpisodeCount` begrenzt damit die Gesamtzahl der Kandidaten je Weiterschauen-Eintrag **inklusive** der aktuellen Episode (bisher: nur Folge-Episoden). Film-Einträge laden bereits den Film selbst und bleiben unverändert; Speicherkontingent (`AppSettings.DownloadQuotaGb`), WLAN-Bedingung (`AppSettings.WifiOnlyDownloads`), Deduplizierung in `DownloadService` und die Reihenfolge/Priorität der Einplanung gelten unverändert. Der „als gesehen löschen"-Vorlauf (`DeleteWatchedDownloadEffect` bei `AppSettings.AutoDeleteWatchedEpisodes`) entfernt weiterhin nur gesehene Episoden.

## Betroffene Klassen und Komponenten

- `VideoPlayer.Mvu/AutoDownloadService.cs`
  - `RunAsync` — Episoden-Zweig (`MediaTypeNames.Normalize(item.MediaType) == "tvshowepisode"`): die aktuelle Episode (`item.Id`) wird als erste Kandidatin eingeplant, bevor Folge-Episoden das Restbudget füllen.
  - `EnqueueNextEpisodesAsync` — Kandidatenmenge beginnt am gefundenen `index` der aktuellen Episode statt bei `index + 1`; die gefundene Position liefert Titel (`{showName} – {episode.Name}`) und `DownloadSeriesContext(show.Id, showName, season.Number, episode.Number)` auch für den Selbst-Download.
  - `EnqueueNextPlaylistEntriesAsync` — der gefundene aktuelle Eintrag (`entry.Id == item.PlaylistEntryId`) wird selbst eingereiht (`entry.MediaId`, `PlaylistEntryTitle`, `SeriesContextOf(entry)`, `DownloadPlaylistContext(item.PlaylistId, entry.Id, entry.SortOrder)`) und verbraucht einen Platz des `remaining`-Budgets; Folge-Einträge danach wie bisher. Rückfall auf den Serien-Pfad (`null`-Rückgabe bei nicht gefundenem Eintrag oder Ladefehler) bleibt bestehen.
  - Film-Zweig unverändert (lädt `item.Id` bereits selbst, Playlist-Folge-Einträge wie bisher).
- `VideoPlayer.Mvu/DownloadService.cs` — keine Änderung erwartet: `EnqueueAsync` (Deduplizierung, Kontingent-Prüfung, `DownloadEnqueueResult`/`NoticeEnqueued`) wird unverändert wiederverwendet; ggf. XML-Dokumentation.
- Berührt, ohne Änderung: `HomeListService`/`HomeMediaItem` (liefern bereits `Id`, `ShowId`, `SeasonId`, `PlaylistId`, `PlaylistEntryId`), `AppSettings` (kein neues Feld), Trigger-Kette `AppUpdate` → `RunAutoDownloadsEffect` → `ShellAppEffectHandler.RunAutoDownloadsAsync`, `ShowDetailsCache` (Cache-Ablage der Serienstruktur), `DeleteWatchedDownloadEffect`-Pfad.
- Dokumentation: `docs/help/downloads.md` beschreibt den Vorlauf als „nächste Episoden" — Formulierung auf „aktuelle und folgende Episoden" anpassen (Dokumentationsschritt).
- Tests: `VideoWebPlayer.Maui.Tests/AutoDownloadServiceTests.cs` — Bestandstests an die neue Kandidatenmenge anpassen (u. a. `Run_EpisodeCandidate_EnqueuesUpcomingEpisodes`, `Run_EpisodeCandidate_CrossesIntoNextSeason`, `Run_PlaylistEpisode_EnqueuesPlaylistOrder`, `Run_PlaylistEpisode_SkipsNonDownloadableEntries`, `Run_PlaylistEpisode_PersistsSeriesHints`, `Run_PlaylistEntryNotFound_FallsBackToSeries`) und um neue Fälle erweitern: aktuelle Episode wird mitgeladen (Serien- und Playlist-Pfad), sie zählt ins `PreloadEpisodeCount`-Budget, Reihenfolge/Priorität der Folge-Episoden unverändert, bereits heruntergeladene aktuelle Episode wird dedupliziert.

## Implementierungsansatz

- Erweiterung der bestehenden Privatmethoden des `AutoDownloadService` — keine neue Klasse, kein neues Interface, kein neuer `AppEffect`: Trigger (`RunAutoDownloadsEffect` nach Synchronisation bzw. beim Einschalten der Einstellung mit `Force`), Throttle (`MinRunInterval`) und Einreihe-Dedup werden wiederverwendet.
- Serien-Pfad: nach `episodes.FindIndex(e => e.Episode.Id == item.Id)` die Kandidaten ab `index` (statt `index + 1`) mit `Take(settings.PreloadEpisodeCount)` einreihen; die aktuelle Episode erhält denselben Titel- und `DownloadSeriesContext`-Aufbau wie Folge-Episoden.
- Playlist-Pfad: beim Auffinden von `entry.Id == currentEntryId` den aktuellen Eintrag selbst einreihen und `remaining` verbrauchen, sofern die bisherigen Ladbarkeitsregeln (`movie`/`tvshowepisode`, `IsAccessible`) erfüllt sind; anschließend füllen die folgenden Einträge das Restbudget in Server-Reihenfolge wie bisher.
- Budget-Semantik: `PreloadEpisodeCount` zählt Kandidaten, nicht nur erfolgreich eingeplante Aufträge — die bestehende Zählweise (`Take` bzw. `remaining--` je Kandidat unabhängig vom `Notice`) wird auf die aktuelle Episode ausgeweitet; Deduplizierung übernimmt `DownloadService.EnqueueAsync`.
- Fehlerbehandlung und Protokollierung unverändert: `OperationCanceledException` durchreichen, überholbare Fehler (`ApiException`, `HttpRequestException`, `TaskCanceledException`, `InvalidOperationException`) je Eintrag als Warning protokollieren und den Lauf fortsetzen.

## Konfiguration

Keine neue Konfiguration. Bestehende Einstellungen gelten unverändert: `AppSettings.AutoDownloadEnabled`, `AppSettings.PreloadEpisodeCount` (1–10, Default 3 — Semantik präzisiert: inklusive aktueller Episode; die `SettingsPage`-Beschriftung „Anzahl vorzuladender Episoden" bleibt zutreffend), `AppSettings.WifiOnlyDownloads`, `AppSettings.DownloadQuotaGb`, `AppSettings.AutoDeleteWatchedEpisodes`.

## Offene Fragen

- Episoden-Eintrag ohne `ShowId` und ohne Playlist-Bezug: bisher wird der Eintrag komplett übersprungen (Test `Run_EpisodeWithoutSeason_SkipsCandidate`). Soll die aktuelle Episode dennoch direkt eingereiht werden — der Selbst-Download benötigt keinen Serienkontext — oder bleibt der Eintrag ohne Kontext weiterhin komplett übersprungen? (Annahme: die aktuelle Episode ist immer erste Kandidatin; nur die Folge-Auflösung entfällt.)
- Playlist-gebundener Film: zählt der Film selbst künftig ins `PreloadEpisodeCount`-Budget der Playlist-Folge-Einträge? (Annahme: nein — „Filme bleiben vom bisherigen Verhalten abgedeckt", d. h. Film selbst plus bis zu `PreloadEpisodeCount` Folge-Einträge.)
- Playlist-gebundene aktuelle Episode, deren Playlist-Eintrag nicht `IsAccessible` ist oder einen nicht ladbaren Typ hat: wie Folge-Einträge überspringen oder dennoch einreihen? (Annahme: dieselben Ladbarkeitsregeln wie für Folge-Einträge anwenden; der Weiterschauen-Eintrag ist definitionsgemäß abspielbar, der Fall dürfte nicht auftreten.)
- Zählt eine deduplizierte bzw. bereits heruntergeladene aktuelle Episode ins Kandidaten-Budget? (Annahme: ja — konsistent zur bestehenden Zählweise je Kandidat statt je erfolgreicher Einplanung.)
