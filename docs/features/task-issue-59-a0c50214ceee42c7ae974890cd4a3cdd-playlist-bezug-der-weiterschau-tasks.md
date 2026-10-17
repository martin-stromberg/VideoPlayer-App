# Tasks: Playlist-Bezug in der Fortschrittsmeldung (Weiterschauen)

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Datenmodell | `PlaybackProgressReport` um `PlaylistId` (`long?`) erweitern | Offen | – |
| 2 | API-Client | `VideoPlayerApiClient.ReportPlaybackProgressAsync` um optionalen Parameter `playlistId` erweitern und in den Request-Body übernehmen | Offen | – |
| 3 | Persistenz | `PendingProgressSyncEntry` um `PlaylistId` (`long?`) erweitern | Offen | – |
| 4 | Persistenz | `BackupDatabase.InitializeAsync`: `EnsureColumnAsync` für `pending_progress`.`PlaylistId` ergänzen | Offen | – |
| 5 | Persistenz | `PendingProgressSyncService.EnqueueAsync` um optionalen Parameter `playlistId` erweitern und beim Insert befüllen | Offen | – |
| 6 | MVU | `ReportPlaybackProgressEffect` um Record-Parameter `PlaylistId` (`long?`, ohne Standardwert) erweitern | Offen | – |
| 7 | MVU | `AppUpdate.UpdatePlaybackProgress`: `ReportPlaybackProgressEffect` mit `PlaylistId` aus `PlayerState` befüllen (Guard `IsPlaylistPlayback`) | Offen | – |
| 8 | MVU | `AppUpdate.UpdatePlaybackEnded`: `ReportPlaybackProgressEffect` mit `PlaylistId` befüllen (Snapshot des endenden Stands) | Offen | – |
| 9 | MVU | `AppUpdate.UpdatePlayerClosed`: `ReportPlaybackProgressEffect` mit `PlaylistId` befüllen | Offen | – |
| 10 | Logik | `ProgressReportService.ReportProgressAsync` um optionalen Parameter `playlistId` erweitern und an Direktversand bzw. `EnqueueAsync` weiterreichen | Offen | – |
| 11 | Logik | `ServerSyncService.FlushPendingProgressAsync`: gespeicherte `PlaylistId` beim Nachsenden mitgeben | Offen | – |
| 12 | App-Rand | `ShellAppEffectHandler.ReportPlaybackProgressAsync`: Effekt-Feld `PlaylistId` an `ProgressReportService` weitergeben | Offen | – |
| 13 | Tests | `VideoPlayerApiClientTests_Endpoints`: Contract-Test — Request-Body enthält `PlaylistId` (PascalCase); Gegenprobe ohne Kontext | Offen | – |
| 14 | Tests | `AppUpdateTests_PlaylistPlayback`: Takt-/Ende-/Schließen-Effekt trägt `PlaylistId` im Playlist-Kontext | Offen | – |
| 15 | Tests | `AppUpdateTests_Player`: kontextlose Meldung trägt `PlaylistId = null` | Offen | – |
| 16 | Tests | `ProgressReportServiceTests`: Online-Direktversand und Offline-Einreihen mit `PlaylistId` | Offen | – |
| 17 | Tests | `PendingProgressSyncServiceTests`: Speicherung der Spalte, Dedup bei Kontextwechsel, Persistenz über DB-Neuöffnung | Offen | – |
| 18 | Tests | Storage-Test: `EnsureColumnAsync`-Migration zieht `PlaylistId`-Spalte in bestehender `pending_progress`-Tabelle nach | Offen | – |
| 19 | Tests | `ServerSyncServiceTests`: Flush sendet gespeicherte `PlaylistId` im Request-Body | Offen | – |
| 20 | E2E-Tests | `MockVideoServer`: Progress-Senke erfasst Request-Bodies (`ProgressReportBodies`, Muster `LoginRequestBodies`) | Offen | – |
| 21 | E2E-Tests | E2E-Test: Playlist-Wiedergabe → erster Progress-POST enthält `playlistId` der Mock-Playlist (case-insensitive Auswertung) | Offen | – |
| 22 | E2E-Tests | E2E-Gegenprobe: kontextlose Wiedergabe → Progress-POST ohne Playlist-Bezug (`playlistId` `null`/fehlend) | Offen | – |
