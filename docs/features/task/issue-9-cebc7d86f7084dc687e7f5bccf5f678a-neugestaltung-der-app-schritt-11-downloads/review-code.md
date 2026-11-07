# Code-Review

Geprüfte Änderungen: alle auf diesem Zweig geänderten Quell- und
Testdateien des Schritts 11 (siehe `git diff` gegen den Planungscommit
`c107eeb`).

## Ergebnis

**Status:** Drei Befunde behoben; keine offenen Befunde

## Findings

### Befund 1 (behoben): Teildatei-Löschung bei Quota-Überschreitung schlug fehl

- **Beschreibung:** Bei Überschreiten des Kontingents während der
  Übertragung rief die Schleife `TryDeleteFile(entry.FilePath)` auf,
  **bevor** der `FileStream` geschlossen war (`FileShare.None`).
  `File.Delete` scheiterte mit `IOException`, wurde vom Filter
  geschluckt — die Teildatei blieb auf der Platte liegen (Test
  `Enqueue_QuotaExceededDuringTransfer_FailsAndRemovesPartial`
  bewies es).
- **Korrektur:** die Schleife merkt sich `quotaExceeded` und bricht
  ab; Datei-Löschung und `failed`-Markierung erfolgen erst nach dem
  Schließen der `await using`-Blöcke.

### Befund 2 (behoben): `IsWifi` schloss Ethernet aus

- **Beschreibung:** `ConnectivityNetworkStatusMonitor.IsWifi` prüfte
  ausschließlich `ConnectionProfile.WiFi`. `WifiOnlyDownloads` ist
  standardmäßig aktiv — auf per Ethernet angebundenen Geräten wären
  sämtliche Downloads dauerhaft blockiert gewesen.
- **Korrektur:** `IsWifi` akzeptiert `WiFi` **oder** `Ethernet`
  (ungedrosselte Festnetz-Profile); Schnittstellen- und
  Implementierungs-Dokumentation angepasst.

### Befund 3 (behoben): Parallele Transfers konnten das Kontingent ungebremst überschreiten

- **Beschreibung:** Jeder Transfer prüfte `usedAtStart + downloaded`
  gegen das Kontingent — `usedAtStart` ist ein Snapshot zum
  Transfer-Start, das Wachstum des jeweils anderen Transfers blieb
  unsichtbar. Zwei parallele Downloads konnten das Kontingent
  gemeinsam beliebig weit überschreiten.
- **Korrektur:** die Verzeichnisbelegung wird alle 8 Chunks
  (`QuotaScanIntervalChunks`, ≈ 640 KiB) neu gemessen; die Prüfung
  addiert das eigene Wachstum seit der Messung — die Überschreitung
  bleibt auf ein Bruchteil eines MB je Transfer begrenzt.

## Geprüfte Aspekte

### MVU-Reinheit

- Alle neuen Übergänge in `AppUpdate` sind reine Zustandsfunktionen;
  Seiteneffekte laufen ausschließlich über `AppEffect`-Records
  (`LoadDownloadsEffect`, `EnqueueDownloadEffect`,
  `CancelDownloadEffect`, `DeleteDownloadEffect`,
  `ResumeDownloadsEffect`, `RunAutoDownloadsEffect`,
  `DeleteWatchedDownloadEffect`). Seiten dispatchen nur Messages —
  keine Geschäftslogik im Code-Behind.

### Guards und Veraltungs-Schutz

- `UpdateDownloadRequested`: `MediaId <= 0` und nicht ladbare Typen
  verworfen; nicht `Connected` → Hinweis statt Effekt.
- `UpdateDownloadsPageOpened`: Doppel-Laden wird mit `IsLoading`
  abgefangen; `DownloadsLoadedMessage` setzt den Stand atomar.
- `UpdateDownloadCancelRequested`/`…Retry`/`…Delete` prüfen
  `CanCancel`/`CanRetry`/`CanDelete` — veraltete Listeneinträge
  lösen keine Effekte aus.
- `UpdateDownloadEnqueueCompleted` zeigt den Hinweis nur, wenn die
  angezeigte Detailseite noch zum Medium gehört.
- `EnqueueAsync` dedupliziert `completed`+Datei, `queued` und aktiv
  `downloading`; verwaiste/fehlgeschlagene/abgebrochene Einträge
  werden auf `queued` zurückgesetzt (Retry-Pfad).
- `SetStateIfPresentAsync` verhindert, dass ein zwischenzeitlich
  gelöschter Download wieder in der Liste auftaucht
  (Löschen-während-Transfer-Race).

### Nebenläufigkeit

- `manifestGate` serialisiert Manifest-Schreibzugriffe;
  `pumpGate` serialisiert die Engine-Pumpe;
  `runningTransfers` (`ConcurrentDictionary`) koppelt Abbruch an
  laufende CTS; `Interlocked` sperrt den Auto-Download-Lauf gegen
  Doppeltrigger.
- Abbruch: `CancelAsync` storniert laufende Transfers über den
  registrierten CTS (Zustand `cancelled` vom Transfer selbst
  gemeldet); wartende Aufträge werden direkt `cancelled`.
- `StubHttpMessageHandler` wurde threadsicher gemacht (Lock um
  Queue + Request-Log) und um einen asynchronen Responder mit
  CancellationToken erweitert — Voraussetzung für die
  Parallel- und Abbruch-Tests (Content-Ströme propagieren das
  Request-Token nicht, siehe Befund-Historie).

### Fehlerbehandlung und Persistenz

- HTTP-Fehler → `http_{code}`, `HttpRequestException` →
  `network`/`http_*`, `IOException` → `io`, fehlende Verbindung →
  `no_connection`, Kontingent → `quota_exceeded`; alle Wege setzen
  `failed` + Code + löschen die Teildatei (nach Stream-Schluss).
- `OperationCanceledException` ohne Benutzerabbruch → `failed`/
  `network` (Timeout-/Transportabbruch), mit Benutzerabbruch →
  `cancelled` — die Unterscheidung ist korrekt getrennt.
- `ResumeInterruptedAsync` normalisiert verwaiste `downloading`→
  `queued`, nimmt `queued` und nicht-Quota-`failed` wieder auf;
  `cancelled` und Quota-Fehlschläge bleiben bewusst unberührt;
  nicht lesbares Manifest → protokolliert + kein Absturz.
- sqlite-net: `VideoId` (`int`) wird vor der Abfrage auf `int`
  gebracht (`Where(e => e.VideoId == id)`) — kein
  `Convert`-Ausdruck in der SQL-Übersetzung.

### Ressourcen und Ablage

- Downloads landen ausschließlich im no-backup-`VideosDirectory`
  (`{typ}_{id}.mp4`); `Directory.CreateDirectory` vor jedem
  Transfer; `TryDeleteFile` bei Abbruch/Fehler/Löschen.
- `HttpClient` pro Transfer `using` mit `Timeout.InfiniteTimeSpan`
  (kein 100-s-Default bei großen Dateien);
  `ResponseHeadersRead` streamt ohne Vorab-Pufferung.
- `FileStream` async mit `FileShare.None`; `ReportProgressThrottled`
  drosselt Manifest-Schreiben + Event auf ~250 ms.
- `DeleteIfCompletedAsync` räumt nur `completed`-Einträge — ein
  laufender/wartender Download derselben Episode wird durch die
  Gesehen-Löschung nicht angetastet.
- `AutoDownloadService`: Lauf-Sperre + 60-s-Throttle, `force` für
  Einschalt-/Trigger-Pfade; Fehler je Kandidat protokolliert und
  übersprungen — kein Gesamtfehlschlag.

### DI/Verdrahtung

- `MauiProgram`: `DownloadService`/`AutoDownloadService` Singletons;
  alle Abhängigkeiten (`AuthSessionService`, `AppSecretsService`,
  `CacheDatabase`, `AppStorageLayout`, `INetworkStatusMonitor`,
  `HomeListService`) bereits als Singletons registriert.
- `ShellAppEffectHandler`: Event-Subscriptions in der
  Initialisierung, saubere Effekt-Zuordnung; Fehler der Hintergrund-
  Operationen werden protokolliert und als Fehler-Messages/
  Hinweise in den MVU-Zyklus gemeldet.

## Bekannte, dokumentierte Einschränkungen

- Kein Range-Resume: der Server-Stream liefert keinen
  `Accept-Ranges`-Nachweis — „Wiederaufnahme" startet die
  Übertragung erneut ab Byte 0 (im Plan festgehalten, im
  `ResumeDownloadsEffect`-XML-Kommentar dokumentiert).
- Quota-Genauigkeit: zwischen zwei Neumessungen kann die Belegung
  um < ~1,3 MB überschritten werden (Messintervall +
  Puffer-Rest) — praktisch vernachlässigbar bei GB-Kontingenten.
- `AutoDeleteWatchedEpisodes` wirkt auf `IsWatched`-Übergang und
  Wiedergabe-Ende; ein vorzeitiges Schließen des Players ohne
  erreichte Schwelle löst keine Löschung aus (das Medium gilt dann
  auch serverseitig nicht als gesehen — konsistent).
