# Tasks: Weiterschauen-Eintrag öffnet Titel-Detailansicht mit Wiedergabe

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Logik | `UpdateContinueWatchingItemSelected`: `NavigateToMediaDetailsEffect` mit Kontext-Hints vor `StartPlayback`-Übergang für `movie`/`tvshowepisode` ohne Playlist-Bezug (`AppUpdate.cs`) | Erledigt | `AppUpdateTests_ContinueWatching.ContinueWatchingItemSelected_Movie_OpensDetailsAndStartsPlayback` |
| 2 | Logik | `UpdateContinueWatchingItemSelected`: Offline-Guard — `IsOffline` + `tvshowepisode` ohne `ShowId` behält Direktwiedergabe; `LoadUpcomingEpisodesEffect` bleibt bei Episoden mit `ShowId` | Erledigt | `AppUpdateTests_ContinueWatching.ContinueWatchingItemSelected_OfflineEpisodeWithoutShowId_StartsPlaybackWithoutDetails`, `…_OfflineEpisodeWithShowId_OpensDetailsAndStartsPlayback`, `…_Episode_OpensDetailsAndStartsPlaybackWithShowLoad` |
| 3 | Logik | XML-Doc von `UpdateContinueWatchingItemSelected` an das neue Verhalten anpassen | Erledigt | Kein direkter Test — Kompilierung der Doc-Kommentare per Build |
| 4 | Tests | `AppUpdateTests_ContinueWatching`: Direktstart-Tests auf neue Effekt-Reihenfolge (Detail-Effekt an Position 0) umstellen und umbenennen | Erledigt | `dotnet test` — 1329 erfolgreich |
| 5 | Tests | `AppUpdateTests_ContinueWatching`: neue Tests Offline-Episode ohne/mit `ShowId`, Offline-Film, Hints-Durchreichung am Detail-Effekt | Erledigt | `dotnet test` — 1329 erfolgreich (3 neue Tests) |
| 6 | Tests | `AppUpdateTests_PlaylistPlayback`: Index-Assertions der Serienpfad-Tests an die neue Effekt-Reihenfolge anpassen | Erledigt | `ContinueWatchingSelected_WithoutPlaylist_KeepsSeriesPath`, `…_PlaylistWithoutEntry_KeepsSeriesPath` |
| 7 | E2E-Tests | `ContinueWatchingPlaybackTests`: Film-Test auf Detail+Nachweis umstellen (Detail-Endpunkt aufgerufen, Rückkehr zur Detailansicht) | Erledigt | `TapContinueWatchingMovie_OpensDetailsAndStartsPlaybackAtResume` (E2E-Ausführung: siehe test-results.md) |
| 8 | E2E-Tests | `ContinueWatchingPlaybackTests`: Episoden-Test — Negativ-Assertions zu Detailaufrufen umkehren, Folgevideo-Verhalten weiter nachweisen | Erledigt | `TapContinueWatchingEpisode_OpensDetailsAndStartsPlaybackAndAdvancesToNextEpisode` (E2E-Ausführung: siehe test-results.md) |
| 9 | Tests | Unit-Testlauf `dotnet test VideoWebPlayer.Maui.Tests` grün; E2E-Lauf sofern ausführbar | Erledigt | 1329/1329 erfolgreich; E2E siehe test-results.md |
