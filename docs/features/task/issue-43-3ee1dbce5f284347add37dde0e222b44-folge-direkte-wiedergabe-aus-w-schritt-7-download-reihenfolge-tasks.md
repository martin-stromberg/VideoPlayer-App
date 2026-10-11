# Tasks: Download-Reihenfolge bei Playlist-Bezug (Schritt 7)

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Datenmodell | `DownloadPlaylistContext`-Record anlegen (`PlaylistId`, `PlaylistEntryId`, `PlaylistSortOrder`) | Erledigt | `AutoDownloadServiceTests.Run_PlaylistEpisode_EnqueuesPlaylistOrder` |
| 2 | Datenmodell | `DownloadManifestEntry` um `PlaylistId`/`PlaylistEntryId`/`PlaylistSortOrder` (je `long?`) erweitern | Erledigt | `LocalDatabaseTests.CacheDatabase_MigratesPlaylistContextOntoExistingManifest` |
| 3 | Datenbank | `CacheDatabase.InitializeAsync` um drei `EnsureColumnAsync`-Aufrufe für die Playlist-Spalten ergänzen | Erledigt | `LocalDatabaseTests.CacheDatabase_MigratesPlaylistContextOntoExistingManifest` |
| 4 | Logik | `DownloadService.EnqueueAsync` um optionalen `playlistContext`-Parameter + `ApplyPlaylistContext` erweitern | Erledigt | `AutoDownloadServiceTests.Run_PlaylistEpisode_PersistsSeriesHints` |
| 5 | Logik | `AutoDownloadService.RunAsync`: Playlist-Pfad bei `PlaylistId > 0 && PlaylistEntryId > 0` (Film: Selbst + Folge-Einträge; Episode: Folge-Einträge) | Erledigt | `AutoDownloadServiceTests.Run_PlaylistMovie_EnqueuesSelfAndNextEntries`, `Run_PlaylistEpisode_EnqueuesPlaylistOrder` |
| 6 | Logik | `AutoDownloadService.EnqueueNextPlaylistEntriesAsync` implementieren (Paging, Filter, Playlist-Reihenfolge, Serien-Hints, Fallback) | Erledigt | `AutoDownloadServiceTests.Run_PlaylistEpisode_SkipsNonDownloadableEntries`, `Run_PlaylistEntryNotFound_FallsBackToSeries`, `Run_PlaylistEntryMissing_FallsBackToSeries` |
| 7 | Tests | `AutoDownloadServiceTests`: Playlist-Reihenfolge, Überspringen, Manifest-Felder, Film-Selbst+Fortsetzung, Fallback-Fälle | Erledigt | 6 neue Tests, alle grün |
| 8 | Tests | `LocalDatabaseTests`: Migration der Playlist-Spalten auf bestehender `download_manifest` | Erledigt | `CacheDatabase_MigratesPlaylistContextOntoExistingManifest` grün |
| 9 | Tests | Komplette Unit-Testsuite grün (`dotnet test VideoWebPlayer.Maui.Tests`) | Erledigt | 1285 erfolgreich, 0 fehlgeschlagen |
