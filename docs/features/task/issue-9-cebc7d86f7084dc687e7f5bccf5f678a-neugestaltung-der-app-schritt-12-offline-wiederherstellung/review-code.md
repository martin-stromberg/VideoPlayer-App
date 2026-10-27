# Code-Review

Geprüfte Änderungen: alle auf diesem Zweig geänderten Quell- und
Testdateien des Schritts 12 (siehe `git diff` gegen den Planungscommit
`77d8f41`).

## Ergebnis

**Status:** Ein Befund behoben; keine offenen Befunde

## Findings

### Befund 1 (behoben): `ShowDetailsCache.ReadAsync` las nie einen Eintrag

- **Beschreibung:** Die Abfrage verwendete `KeyFor(showId)`
  **innerhalb** des sqlite-net-`Where`-Prädikats:
  `Where(e => e.Key == KeyFor(showId))`. sqlite-net übersetzt
  Methodenaufrufe nicht — zur Laufzeit entstand SQL
  `... WHERE Key = keyfor(...)`, das mit
  `SQLiteException: no such function: keyfor` scheiterte. Der
  `catch`-Block schluckte die Ausnahme und lieferte `null` —
  sämtliche Offline-Serienkontexte (Platzhalter, Offline-
  Detailseite) wären in der Praxis immer leer gewesen. Der neue Test
  `ContinueWatching_EpisodeWithMissingNext_ShowsPlaceholder`
  erwischte den Fehler.
- **Korrektur:** der Schlüssel wird vor der Abfrage in eine lokale
  Variable gehoben (`var key = KeyFor(showId);`), die sqlite-net als
  Parameter binden kann (`ShowDetailsCache.cs`). Alle Aufrufer
  (`OfflineContinueWatchingService`, `MediaDetailsService`) gehen
  über diese Methode — keine weiteren Stellen betroffen; gleiche
  Aufrufmuster im übrigen Code geprüft (übrige `Where`-Prädikate
  verwenden Konstanten/Spaltenvergleiche).

## Geprüfte Aspekte

### MVU-Reinheit

- [x] Zustand ausschließlich in `AppModel` (`IsOffline`, `IsOnline`,
  `HasOfflineData`, `IsSyncRunning`, `StorageNotice`, Listen-
  Eigenschaften).
- [x] Übergänge rein in `AppUpdate` — keine IO, keine Dienste;
  neue Messages/Effekte folgen dem Benennungs- und Form-Muster.
- [x] Seiteneffekte ausschließlich in `ShellAppEffectHandler`
  (Ablage-Initialisierung, Sync, Download-Pumpe, Quellenauflösung).
- [x] Keine Parallel-Navigation/-Zustandsverwaltung: Offline-
  Verzweigung läuft über `ConnectionState`/`AppPage`.

### Offline-Korrektheit

- [x] `OfflineContinueWatchingService` filtert ausschließlich
  `completed` + `fileExists` — fehlende Dateien werden nie als
  abspielbar markiert.
- [x] `PlaybackSourceService` mit `allowServerStream: false` erzeugt
  garantiert keine Server-URL und führt keinen HTTP-Request aus
  (Test-Stub zählt Requests: 0).
- [x] Fortschritts-Priorität: lokale Outbox (`pending_progress`)
  vor Server-Cache — passend zum Offline-Prinzip „letzter lokaler
  Stand gewinnt".
- [x] Offline-Pfade sind einzeln abgesichert (`try/catch` je Quelle)
  — ein kaputter Teilschritt erzeugt eine leere Teilliste statt
  eines Absturzes.

### Wiederherstellung / Auth

- [x] Token-Validierung vor Credential-Login; 401/403 verwerfen das
  Token, Transportfehler führen zu `ServerUnreachable` —
  `SessionExpired` nur bei fehlenden/abgelehnten Credentials.
- [x] Geheimnisse bleiben außerhalb der Backup-Ablage
  (`AppSecretsService` unverändert im Secret-Store); `KnownServers`/
  Benutzername in der Backup-DB — letzter Server wird wiederverwendet.
- [x] Recovery-Initialisierung: Datei quarantänisieren, Pool/
  Lazy-Connection zurücksetzen, neu aufbauen; zweiter Fehlschlag
  propagiert (bewusster Abbruch statt stiller Daten-Käse).

### Migrationen / Persistenz

- [x] `EnsureColumnAsync` für `pending_progress.DurationTicks`
  (`BackupDatabase`) und die vier Serienkontext-Spalten
  (`CacheDatabase`) — Bestandsdatenbanken werden migriert, nicht
  ersetzt (Test mit Schritt-11-Schema).
- [x] Neue Schlüssel `show:{id}` in `cached_lists` (nicht backup-
  pflichtig) — korrekt als Cache klassifiziert.

### UI-Verdrahtung

- [x] Alle neuen XAML-Elemente werden im Code-Behind geschaltet;
  Contract-Tests prüfen Namen/Bindings (`OfflineRecoveryContractTests`).
- [x] `ModelChanged`-Abonnements der geänderten Seiten vorhanden;
  kein UI-Thread-Zugriff außerhalb `MainThread` im Handler.

### Konsistenz / Stil

- [x] Deutsche XML-Doku an allen neuen öffentlichen Membern.
- [x] Neue Aufzählungswerte vermieden (Plan-Vorgabe) — Kennzeichnung
  über Flags/Records, `EnumCoverageTests` unverändert grün.
- [x] Tests folgen dem Bestandsmuster (echte SQLite-Ablage,
  `SqliteDatabase`-Collection, Pool-Reset + Temp-Verzeichnis im
  `Dispose`).

## Risiken / Hinweise

- Offline-Platzhalter hängen von der gecachten Serienstruktur ab —
  eine Serie, deren Detailseite nie online geöffnet wurde, erzeugt
  keine Platzhalter (akzeptiert: bewusst „best effort", Dokumentation
  in der Hilfe).
- `*.corrupt-*`-Dateien werden nicht automatisch aufgeräumt
  (Nachvollziehbarkeit; liegen im App-Datenverzeichnis).
