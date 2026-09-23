# Plan-Check: Download-Verwaltung und automatische Downloads

Geprüft am: 2026-08-26, gegen `requirement.md` (bestätigte Übersetzung)
und `plan.md` (Abschnitte Übersicht, Designentscheidungen,
Programmabläufe, Betroffene Dateien, Testnachweise, Offene Punkte).

## Abdeckung der Akzeptanzkriterien

- [x] **Mehrere Downloads parallel** → `DownloadService`, Semaphore-
      Engine (`MaxParallelDownloads = 2`) + Pump; Service-Test mit
      verzögertem Stub-Handler.
- [x] **Automatische Wiederaufnahme (auch Neustart)** → Manifest-
      Persistenz + `ResumeInterruptedAsync` über
      `ResumeDownloadsEffect` bei `Connected`-Eintritt und Online-
      Ereignis; dokumentierte Einschränkung Range-Resume (kein Server-
      Support → dokumentierter Neustart ab Byte 0).
- [x] **GB-Kontingent eingehalten (Default 5, konfigurierbar)** →
      `AppSettings.DownloadQuotaGb` existiert bereits; Durchsetzung beim
      Einreihen, beim Start und pro Schreibblock; Tests.
- [x] **Automatische Downloads respektieren Anzahl + WLAN-only** →
      `AutoDownloadService` (`PreloadEpisodeCount` je Weiterschauen-
      Eintrag, Serienkontext via `GetTvShowAsync`),
      `INetworkStatusMonitor.IsWifi`, Schalter `AutoDownloadEnabled`;
      Guards + Tests.
- [x] **Auto-Löschung gesehener Episoden** → `IsWatched`-Setzung in
      `UpdatePlaybackProgress`/`UpdatePlaybackEnded` +
      `AutoDeleteWatchedEpisodes` → `DeleteWatchedDownloadEffect`;
      nur `tvshowepisode`, nur `completed`-Downloads.
- [x] **Downloads-Seite: Status + Fortschritt je Download** →
      `DownloadsPageState`/`DownloadListItem` + `CollectionView` mit
      Statustext, `ProgressBar`, Prozent, Aktionen Abbrechen/Erneut
      versuchen/Löschen; `DownloadsPageOpened`/`LoadDownloads`/
      `ItemChanged`-Fluss.
- [x] **Liste mit Titel, Status, Fortschritt; Abbrechen/Löschen** →
      siehe oben (Abbrechen → `cancelled`, Teildatei wird entfernt;
      Löschen → Datei + Manifest-Zeile).
- [x] **Fehlerbehandlung definiert** → `failed`-Status mit
      maschinenlesbaren `Error`-Codes + deutschen Anzeigetexten,
      „Erneut versuchen".
- [x] **Persistenter, backup-freier Downloadbestand (Manifest)** →
      `download_manifest` in `CacheDatabase` (Nicht-Backup-Root via
      `AppStorageLayout`); Anzeige/Resume lesen ausschließlich das
      Manifest → Bestand jederzeit rekonstruierbar.
- [x] **Lokale Wiedergabe** → unverändert über
      `PlaybackSourceService` (`completed` + existierende Datei →
      `LocalFile`); kein Eingriff nötig, belegt durch bestehende Tests.
- [x] **Download per HttpClient mit Fortschritt** → GET mit
      `ResponseHeadersRead` + Kopierschleife; Range-Resume nicht
      möglich (dokumentierte Einschränkung, Fallback = Neustart).

## Konsistenz-/Lückenprüfung

- Neue `State`-Werte in `download_manifest` (`queued`, `downloading`,
  `cancelled`) beeinflussen `PlaybackSourceService` nicht — er prüft
  ausschließlich `completed` + Datei. ✅
- `Cancelled` als fünfter Status ist zulässig (Statusliste der
  Anforderung ist Minimalmenge); Anzeige „Abgebrochen". ✅
- Alle neuen Enums (`DownloadState`) werden über
  `EnumCoverageTests` abgedeckt (Hook-Vorgabe). ✅
- `INetworkStatusMonitor.IsWifi` bricht `FakeNetworkMonitor` in
  `ProgressReportServiceTests` → im Plan als zu ändernde Datei
  vermerkt. ✅
- Keine neuen Validierungs-/Kindersicherungs-Regeln erforderlich —
  Downloads nur für `movie`/`tvshowepisode` und nur bei `Connected`. ✅
- „Offene Punkte": keine → kein Warten auf Rückmeldung nötig. ✅

## Ergebnis

**Plan vollständig — Umsetzung kann starten.**

Getroffene und dokumentierte Detailentscheidungen: WLAN-only wirkt auf
alle Download-Transfers (Einstellungslabel „Downloads nur im WLAN"),
`PreloadEpisodeCount` gilt je Weiterschauen-Eintrag, abgebrochene
Downloads werden wegen fehlendem Range-Support neu gestartet statt
fortgesetzt, Film-Einträge der Weiterschauen-Liste laden den Film selbst.
