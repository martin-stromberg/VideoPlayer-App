# Bestandsaufnahme: Auto-Download lädt die aktuelle Weiterschauen-Episode mit und zählt sie ins Episoden-Budget

Bestandsaufnahme des automatischen Download-Vorlaufs (`AutoDownloadService` und angrenzende Download-/Weiterschauen-Bausteine) bezogen auf die Anforderung aus [requirement.md](requirement.md) (Issue #56): Die aktuell abspielbare Episode eines Weiterschauen-Eintrags soll künftig ebenfalls vorab geladen werden und ins `PreloadEpisodeCount`-Budget zählen.

## Zusammenfassung

- `AutoDownloadService.RunAsync` (`VideoPlayer.Mvu/AutoDownloadService.cs`) iteriert die gecachte Weiterschauen-Liste (`HomeLists.ContinueWatching`, `HomeMediaItem`) und verzweigt auf `movie` bzw. `tvshowepisode` (normalisiert über `MediaTypeNames.Normalize`).
- **Film-Zweig:** der Film selbst (`item.Id`) wird bereits direkt über `DownloadService.EnqueueAsync` eingeplant (Z. 140–157); bei Playlist-Bezug folgen zusätzlich die Folge-Einträge der Playlist (`EnqueueNextPlaylistEntriesAsync`).
- **Episoden-Zweig (Serien-Pfad):** `EnqueueNextEpisodesAsync` (Z. 201–254) lädt die Serienstruktur (`api/items/tvshow/{showId}`), schreibt sie über `ShowDetailsCache.WriteAsync` in den Cache und reiht ab `index + 1` der gefundenen aktuellen Episode bis zu `PreloadEpisodeCount` **Folge-Episoden** ein — die aktuelle Episode selbst (`item.Id`) wird **nicht** eingeplant.
- **Episoden-Zweig (Playlist-Pfad):** `EnqueueNextPlaylistEntriesAsync` (Z. 276–359) lädt `api/playlists/{id}/entries/paged` seitenweise, setzt `found` beim aktuellen Eintrag (`entry.Id == item.PlaylistEntryId`) und reiht nur **folgende** ladbare, zugängliche Einträge (`movie`/`tvshowepisode`, `IsAccessible`) bis zum `remaining`-Budget ein; `remaining` zählt je Kandidat unabhängig vom Einreihe-Ergebnis. Bei nicht gefundenem Eintrag oder Ladefehler → `null` → Rückfall auf den Serien-Pfad.
- `PreloadEpisodeCount` begrenzt heute nur die Folge-Episoden (Serien-Pfad: `Take`, Playlist-Pfad: `remaining`); `AppSettings` hat kein weiteres einschlägiges Feld (Default 3, Schranken 1–10 via `Normalize`).
- `DownloadService.EnqueueAsync` (Z. 244–385) bringt bereits alles mit, was der Selbst-Download der aktuellen Episode benötigt: Deduplizierung (`NoticeAlreadyPresent`/`NoticeAlreadyRunning`/`NoticeAlreadyQueued`), Quota-Vorabprüfung, optionale Kontexte `DownloadSeriesContext`/`DownloadPlaylistContext` und das Ergebnis `DownloadEnqueueResult`/`NoticeEnqueued`.
- Trigger-Kette ist vorhanden: `AppUpdate` emittiert `RunAutoDownloadsEffect` nach Sync (`UpdateSynchronizationCompleted`, Z. 1138), nach Cache-Listenänderung (`UpdateCachedListsChanged`, Z. 1155) und beim Einschalten der Einstellung (`UpdateSaveSettingsRequested`, Z. 349, `Force: true`); `ShellAppEffectHandler.RunAutoDownloadsAsync` (Z. 1610–1623) ruft `RunAsync`. Der `DeleteWatchedDownloadEffect`-Pfad (Z. 3833/3928 → `DeleteDownloadAsync(..., completedOnly: true)`) ist vom Vorlauf getrennt.
- Dokumentation `docs/help/downloads.md` beschreibt den Vorlauf als „die nächsten **N** Episoden" bzw. „Folge-Einträge … ab dem aktuellen Playlist-Eintrag" (Z. 85–97) — deckt die aktuelle Episode noch nicht ab.
- Fehlende Testabdeckung für die Anforderung: kein Test prüft bisher, dass die aktuelle Episode selbst eingeplant wird; alle bestehenden `AutoDownloadServiceTests` erwarten das heutige „nur Folge-Episoden"-Verhalten (u. a. `Run_EpisodeCandidate_EnqueuesUpcomingEpisodes`, `Run_PlaylistEpisode_*`).

Test-Ausgangszustand: `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj --no-build --configuration Release` am 2026-10-17 auf Commit `14ca0b3` — **1326/1326 Tests erfolgreich, 0 Fehlschläge, 0 übersprungen** (Exit-Code 0). Die FlaUI-Suite `VideoPlayer.E2E.Tests` wurde nicht ausgeführt (interaktive Desktop-Session erforderlich, nicht CI-Bestandteil). Details und Nachweise unter [Tests](inventory/tests.md).

## Details

- [Datenmodell](inventory/models.md)
- [Logik](inventory/logic.md)
- [Enums](inventory/enums.md)
- [Interfaces](inventory/interfaces.md)
- [Tests](inventory/tests.md)
