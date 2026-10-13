# Tasks: Weiterschauen-Eintrag öffnet Titel-Detailansicht mit Wiedergabe

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Logik | `UpdateContinueWatchingItemSelected`: `NavigateToMediaDetailsEffect` mit Kontext-Hints vor `StartPlayback`-Übergang für `movie`/`tvshowepisode` ohne Playlist-Bezug (`AppUpdate.cs`) | Offen | — |
| 2 | Logik | `UpdateContinueWatchingItemSelected`: Offline-Guard — `IsOffline` + `tvshowepisode` ohne `ShowId` behält Direktwiedergabe; `LoadUpcomingEpisodesEffect` bleibt bei Episoden mit `ShowId` | Offen | — |
| 3 | Logik | XML-Doc von `UpdateContinueWatchingItemSelected` an das neue Verhalten anpassen | Offen | — |
| 4 | Tests | `AppUpdateTests_ContinueWatching`: Direktstart-Tests auf neue Effekt-Reihenfolge (Detail-Effekt an Position 0) umstellen und umbenennen | Offen | — |
| 5 | Tests | `AppUpdateTests_ContinueWatching`: neue Tests Offline-Episode ohne/mit `ShowId`, Offline-Film, Hints-Durchreichung am Detail-Effekt | Offen | — |
| 6 | Tests | `AppUpdateTests_PlaylistPlayback`: Index-Assertions der Serienpfad-Tests an die neue Effekt-Reihenfolge anpassen | Offen | — |
| 7 | E2E-Tests | `ContinueWatchingPlaybackTests`: Film-Test auf Detail+Nachweis umstellen (Detail-Endpunkt aufgerufen, Rückkehr zur Detailansicht) | Offen | — |
| 8 | E2E-Tests | `ContinueWatchingPlaybackTests`: Episoden-Test — Negativ-Assertions zu Detailaufrufen umkehren, Folgevideo-Verhalten weiter nachweisen | Offen | — |
| 9 | Tests | Unit-Testlauf `dotnet test VideoWebPlayer.Maui.Tests` grün; E2E-Lauf sofern ausführbar | Offen | — |
