# Tasks: Auto-Download lädt die aktuelle Weiterschauen-Episode mit und zählt sie ins Episoden-Budget

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Logik | `AutoDownloadService.EnqueueNextPlaylistEntriesAsync`: gefundenen aktuellen Eintrag (`entry.Id == item.PlaylistEntryId`) stets als ersten Kandidaten der bestehenden Ladbarkeits-/Einreih-Behandlung zuführen (`entry.MediaId`, `PlaylistEntryTitle`, `SeriesContextOf`, `DownloadPlaylistContext(item.PlaylistId, entry.Id, entry.SortOrder)`, `remaining--` je Kandidat — auch bei Dedup-Treffer); XML-Dokumentation und Log-Text anpassen | Offen | — |
| 2 | Logik | `AutoDownloadService.EnqueueNextEpisodesAsync`: Kandidatenmenge auf `episodes.Skip(index).Take(settings.PreloadEpisodeCount)` ändern (aktuelle Episode als erste Kandidatin); XML-Dokumentation und Log-Text anpassen | Offen | — |
| 3 | Logik | `AutoDownloadService.RunAsync`: Orphan-Fallback — Episoden-Eintrag ohne Playlist-Treffer und ohne `ShowId` direkt einreihen (`EnqueueAsync("tvshowepisode", item.Id, item.Title, …)`, `DownloadPlaylistContext` bei Playlist-Bindung) | Offen | — |
| 4 | Dokumentation | `AutoDownloadService`-Klassen-`<summary>` und Methoden-Dokumentation auf „aktuelle und folgende Episoden/Einträge" umformulieren; Film-Budget-Regel („Film + Folge-Einträge ≤ `PreloadEpisodeCount`") ergänzen | Offen | — |
| 5 | Dokumentation | `docs/help/downloads.md` (Abschnitt „Automatische Downloads (Vorlauf)"): Formulierung „nächste N Episoden" / „ab dem aktuellen Playlist-Eintrag" auf „aktuelle und folgende Episoden inklusive des aktuellen Eintrags" anpassen; Satz zum playlist-gebundenen Film um die Budget-Regel präzisieren | Offen | — |
| 6 | Tests | `AutoDownloadServiceTests.Run_EpisodeCandidate_EnqueuesUpcomingEpisodes` anpassen (Erwartung: Einträge 32 + 33 statt 33 + 34) | Offen | — |
| 7 | Tests | `AutoDownloadServiceTests.Run_EpisodeCandidate_CrossesIntoNextSeason` anpassen (`PreloadEpisodeCount = 2`; Erwartung: Einträge 36 + 37, S2E1-Serienkontext) | Offen | — |
| 8 | Tests | `AutoDownloadServiceTests.Run_PlaylistEpisode_EnqueuesPlaylistOrder` anpassen (Erwartung: Einträge 32 + 99; Playlist-Bezug/`PlaylistSortOrder` am passenden Eintrag prüfen) | Offen | — |
| 9 | Tests | `AutoDownloadServiceTests.Run_PlaylistEpisode_SkipsNonDownloadableEntries` anpassen (Erwartung: Einträge 32 + 62) | Offen | — |
| 10 | Tests | `AutoDownloadServiceTests.Run_PlaylistEpisode_PersistsSeriesHints` anpassen (`PreloadEpisodeCount = 2`, Serien-Hints am Folge-Eintrag 102 weiter prüfen) | Offen | — |
| 11 | Tests | `AutoDownloadServiceTests.Run_PlaylistMovie_EnqueuesSelfAndNextEntries` anpassen (Film zählt ins Budget: Erwartung bei `PreloadEpisodeCount = 2` Einträge 7 + 8 statt 7 + 8 + 9; kein Doppel-Eintrag für VideoId 7, `PlaylistId = 5` an beiden) | Offen | — |
| 12 | Tests | `AutoDownloadServiceTests.Run_PlaylistEntryNotFound_FallsBackToSeries` anpassen (Erwartung: Einträge 32 + 33 ohne `PlaylistId`) | Offen | — |
| 13 | Tests | `AutoDownloadServiceTests.Run_PlaylistEntryMissing_FallsBackToSeries` anpassen (Erwartung: nur aktuelle Episode 32) | Offen | — |
| 14 | Tests | `AutoDownloadServiceTests.Run_EpisodeWithoutSeason_SkipsCandidate` zu `Run_EpisodeWithoutSeason_EnqueuesSelf` umwandeln (ein Manifest-Eintrag VideoId 32 ohne Serienkontext; kein `api/items/tvshow`-Request; Stream-Response stubben oder Transfer-Fehlschlag tolerieren) | Offen | — |
| 15 | Tests | `AutoDownloadServiceTests.Run_EpisodeCandidate_EnqueuesCurrentFirst` neu schreiben (aktuelle Episode als erster Manifest-Eintrag, Titel/Serienkontext am Selbst-Eintrag) | Offen | — |
| 16 | Tests | `AutoDownloadServiceTests.Run_EpisodeCandidate_AlreadyDownloaded_DeduplicatesAndCountsBudget` neu schreiben (Dedup der fertig geladenen aktuellen Episode, Budget-Verbrauch bleibt) | Offen | — |
| 17 | Tests | `AutoDownloadServiceTests.Run_PlaylistEpisode_EnqueuesCurrentEntry` neu schreiben (aktueller Eintrag im Manifest mit `PlaylistId`/`PlaylistEntryId`/`PlaylistSortOrder`/Serien-Hints; Budget-Verbrauch) | Offen | — |
| 18 | Tests | `AutoDownloadServiceTests.Run_PlaylistEpisode_CurrentEntryNotAccessible_SkipsSelf` neu schreiben (nicht ladbarer aktueller Eintrag übersprungen, `found` bleibt → kein Serien-Rückfall; Folge-Einträge füllen Budget) | Offen | — |
| 19 | Tests | `AutoDownloadServiceTests.Run_PlaylistEntryNotFoundNoShow_EnqueuesSelf` neu schreiben (Playlist-Bindung ohne Treffer und ohne `ShowId` → Selbst-Einreihung mit `DownloadPlaylistContext`) | Offen | — |
| 20 | Tests | Testlauf: `dotnet build` + `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj --no-build --configuration Release` — alle Tests erfolgreich | Offen | — |
