# Tasks: Download-Reihenfolge bei Playlist-Bezug (Schritt 7)

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Datenmodell | `DownloadPlaylistContext`-Record anlegen (`PlaylistId`, `PlaylistEntryId`, `PlaylistSortOrder`) | Offen | – |
| 2 | Datenmodell | `DownloadManifestEntry` um `PlaylistId`/`PlaylistEntryId`/`PlaylistSortOrder` (je `long?`) erweitern | Offen | – |
| 3 | Datenbank | `CacheDatabase.InitializeAsync` um drei `EnsureColumnAsync`-Aufrufe für die Playlist-Spalten ergänzen | Offen | – |
| 4 | Logik | `DownloadService.EnqueueAsync` um optionalen `playlistContext`-Parameter + `ApplyPlaylistContext` erweitern | Offen | – |
| 5 | Logik | `AutoDownloadService.RunAsync`: Playlist-Pfad bei `PlaylistId > 0 && PlaylistEntryId > 0` (Film: Selbst + Folge-Einträge; Episode: Folge-Einträge) | Offen | – |
| 6 | Logik | `AutoDownloadService.EnqueueNextPlaylistEntriesAsync` implementieren (Paging über `entries/paged`, Filter ladbar/zugänglich, Playlist-Reihenfolge, Serien-Hints aus `ParentMedia*`, Fallback Serien-Pfad) | Offen | – |
| 7 | Tests | `AutoDownloadServiceTests`: Playlist-Reihenfolge, Überspringen nicht ladbarer Einträge, Manifest-Playlist-Felder, Film-Selbst+Fortsetzung, Fallback-Fälle | Offen | – |
| 8 | Tests | `LocalDatabaseTests`: Migration der Playlist-Spalten auf bestehender `download_manifest` | Offen | – |
| 9 | Tests | Komplette Unit-Testsuite grün (`dotnet test VideoWebPlayer.Maui.Tests`) | Offen | – |
