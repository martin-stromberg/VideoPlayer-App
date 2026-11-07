# Bestandsaufnahme — Altbestand-Referenz Downloads (`VideoWebPlayer.Maui`)

Fachliche Referenz für die Neuimplementierung — nicht als Code-Basis.
Quelle der Detailbefunde:
`docs/projects/task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app/inventory/`
(`services.md`, `models.md`, `interfaces.md`).

## `DownloadQueue`

Datei: `VideoWebPlayer.Maui/Services/DownloadQueue.cs` — Singleton,
rein in-memory (`ConcurrentQueue<DownloadTask>` + `HashSet`-Dedup).

- `EnqueueDownload(DownloadTask)`, `IsInQueue(videoId, videoType)`.
- `ProcessQueueAsync`: Hintergrund-Loop, verarbeitet **jeweils einen**
  Download (kein Parallelismus); `HttpClient` mit Bearer-Header,
  Timeout 30 min; `GetAsync` mit `ResponseHeadersRead`, streamt in
  `FileStream` (8192-Byte-Puffer); Fortschritt über
  `DownloadProgress`-Event (Prozent aus `ContentLength`).
- Bei Fehler: Teildatei wird gelöscht, `DownloadCompleted` mit
  `Success=false` + Fehlermeldung. **Kein Retry, kein Range-Resume.**
- `DownloadTask` trägt `VideoId`, `VideoType`, `Title`, `StreamUrl`,
  `LocalFilePath`, `RetentionType`, `ExpiresAt` sowie Metadaten
  (Plot, Genre, Jahr, Staffel-/Episodennummer, Serienname,
  Poster-/Banner-URL).

## `DownloadManager`

Datei: `VideoWebPlayer.Maui/Services/DownloadManager.cs` — Singleton,
SQLite-Tabelle `Downloads` (`DownloadedVideo`).

- `RequestVideoAsync` → lokale Datei (Completed + Datei vorhanden) oder
  Stream-URL + Fortsetzungsposition; bei Stream wird automatisch ein
  Cache-Download eingereiht.
- `QueueDownloadAsync(request, retentionType)` — Dedup gegen DB
  (Completed + Datei) und Queue; Zielname
  `{type}_{id}_{ticks}.mp4` unter `videos[_profil]`; `ExpiresAt` je
  Retention-Einstellung; Metadaten-Abruf.
- `BuildStreamUrl` — `{server}/api/items/{typ}/{id}/stream?access_token=…`.
- `SaveCompletedDownloadAsync` — lädt Poster/Banner lokal, Insert,
  `DownloadCompletedEvent`.
- `DeleteDownloadAsync`, `GetDownloadAsync`, `GetAllDownloadsAsync`
  (nur Completed), `CleanupExpiredDownloadsAsync` (6-h-Timer,
  `DownloadDeletedEvent`).
- `DownloadStatus`: `Queued`, `Downloading`, `Completed`, `Failed`.
- `DownloadRetentionType`: `Cache`, `Download`, `Watchlist` —
  Retention-Tage-Einstellungen (im neuen Konzept durch das
  GB-Kontingent + Auto-Löschung ersetzt).

## `WatchlistDownloadCoordinatorService`

Datei: `VideoWebPlayer.Maui/Services/WatchlistDownloadCoordinatorService.cs`

- Reagiert auf `ContinueWatchingUpdatedEvent` + `RunOnStartupAsync`
  (Throttle 10 s, `SemaphoreSlim` gegen Parallel-Läufe).
- Lädt `GetContinueWatchingAsync`, dedupliziert gegen Bestand
  (Completed + Datei) und Queue, reiht als `Watchlist`-Download ein.
- Lädt die **angezeigten Einträge selbst** (nicht die Folge-Episoden) —
  die neue Anforderung verlangt das Vorladen der nächsten Episoden
  (Serienkontext); die Dedup-/Trigger-Muster sind übernehmbar.

## `DownloadedVideo` (Modell)

Datei: `VideoWebPlayer.Maui/Models/DownloadedVideo.cs` — Tabelle
`Downloads`: `VideoId`, `VideoType`, `Title`, `LocalFilePath`,
`FileSizeBytes`, `DownloadedAt`, `ExpiresAt`, `Status`, `RetentionType`,
`PlaybackPositionSeconds`, `DurationSeconds`, Poster-/Banner-Pfade.

## Übernahme-Entscheidungen für die Neu-App

| Altbestand-Muster | Übernahme in Schritt 11 |
|-------------------|-------------------------|
| Stream-URL-Bau (`api/items/{typ}/{id}/stream?access_token=`) | Übernommen (bereits in `PlaybackSourceService` etabliert) |
| Fortschritt per `ContentLength` + Stream-Kopie | Übernommen (Download-Dienst, Fortschritts-Messages) |
| Teildatei-Löschung bei Fehler | Übernommen |
| Dedup gegen Queue + Bestand | Übernommen (Manifest-basiert) |
| Sequenzielle Queue | **Ersetzt** durch parallele Downloads (Semaphore-Begrenzung) |
| Kein Resume | **Ersetzt** durch automatische Wiederaufnahme = Neuauftragsaufnahme aus dem persistenten Manifest (kein Range-Support — Teildatei wird verworfen, dokumentierte Einschränkung) |
| Retention-Tage | **Ersetzt** durch GB-Kontingent + optionale Auto-Löschung gesehener Episoden |
| Event-getriebene Watchlist-Synchronisation | Übernommen als MVU-Anstoß (nach Sync/Continue-Watching-Ereignis) |
