# Logik

## `DownloadService`
Datei: `VideoPlayer.Mvu/DownloadService.cs` (973 Zeilen)

Plattformneutraler Download-Dienst: persistente Aufträge im Manifest,
Warteschlange mit max. 2 parallelen Transfers (`MaxParallelDownloads`),
prozessinterner `HttpClient`-Stream → `FileStream` (`FileMode.Create`).

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `LoadItemsAsync` | public | Alle Manifest-Einträge als `DownloadListItem`-Liste |
| `GetUsedBytes` | public | Summe **aller** Dateien im Video-Verzeichnis (inkl. verwaister Dateien ohne Manifest-Eintrag) |
| `EnqueueAsync` | public | Auftrag einreihen/deduplizieren/reaktivieren; Vorab-Quota-Prüfung; startet `PumpQueue` |
| `CancelAsync` | public | Wartende/laufende Aufträge abbrechen (Teildatei wird entfernt) |
| `DeleteAsync` | public | Einzelnen Download löschen: Transfer abbrechen, Datei + Manifest-Eintrag entfernen |
| `DeleteIfCompletedAsync` | public | Nur abgeschlossene Downloads löschen (Auto-Löschen gesehener Episoden) |
| `ResumeInterruptedAsync` | public | Wartende/verwaist-laufende/fehlgeschlagene (außer Quota) Aufträge wieder einreihen; Transfer ab Byte 0 |
| `PumpQueue(Async)` | private | Nächste wartende Aufträge starten (max. 2 parallel, WLAN-Bedingung, laufende Quota-Prüfung) |
| `NextQueuedEntryAsync` | private | Ältester wartender Manifest-Eintrag |
| `TransferAsync` | private | Eigentlicher Transfer: URL bauen, `HttpClient.SendAsync` (ResponseHeadersRead), chunked Datei schreiben, Fortschritt gedrosselt persistieren, Quota laufend prüfen |
| `ReportProgressThrottledAsync` | private | Fortschritt max. alle `ProgressInterval` ins Manifest |
| `SetStateAsync` / `SetStateIfPresentAsync` | private | Zustandswechsel persistieren + `ItemChanged`; „IfPresent" unterdrückt Events für gelöschte Einträge |
| `FindEntryAsync` | private | Manifest-Eintrag nach Typ+Id |
| `BuildStreamUrlAsync` | private | `api/items/{type}/{id}/stream?access_token=…` |
| `TryDeleteFile` | private | Datei löschen, Fehler nur geloggt |
| `RaiseItemChanged` / `RaiseItemRemoved` | private | Events `ItemChanged`/`ItemRemoved` |
| `CreateHttpClient` | private | `HttpClient` mit `Timeout.InfiniteTimeSpan` |

Publizierte Events: `ItemChanged` (`DownloadItemEventArgs`), `ItemRemoved`
(`DownloadRemovedEventArgs`).

Fehlercodes: `quota_exceeded`, `no_connection`, `network`, `io`, `http_<status>`.

Wichtig für die Anforderung:
- Der Transfer läuft im App-Prozess. Bei iOS-Suspendierung sterben die
  Sockets → der Download schlägt fehl bzw. bleibt hängen; es gibt keine
  Hintergrund-Session.
- `TryDeleteFile` räumt Teildateien auf allen verwalteten Fehlerpfaden —
  **nicht** aber bei Prozessabbruch (Kill/Suspend-Entzug) oder bei Verlust
  der Cache-Datenbank (Manifest weg, Dateien bleiben → verwaist).
- `FileMode.Create` überschreibt eine gleichnamige Restdatei beim erneuten
  Download desselben Mediums (Teildatei desselben Auftrags verwaist nicht
  dauerhaft, solange derselbe Auftrag erneut läuft).
- Es gibt **keinen** Scan des Video-Verzeichnisses gegen das Manifest
  (keine Waisen-Erkennung) und kein „Alle löschen".

## `AutoDownloadService`
Datei: `VideoPlayer.Mvu/AutoDownloadService.cs`

Automatischer Vorlauf der Weiterschauen-Episoden über `DownloadService.EnqueueAsync`;
gedrosselt (`MinRunInterval` = 60 s), respektiert WLAN-Bedingung und Kontingent.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `RunAsync` | public | Einen Vorlauf ausführen (Sync-/Ereignis-getriggert, `force` umgeht Drossel) |

## `DownloadListSync`
Datei: `VideoPlayer.Mvu/DownloadListSync.cs`

Statischer Abgleich `ObservableCollection<DownloadItemViewModel>` ↔
`IReadOnlyList<DownloadListItem>` (in-place Updates, Move/Insert/Remove).

## `ShellAppEffectHandler` (Ausschnitt)
Datei: `VideoPlayer.Maui/Services/ShellAppEffectHandler.cs`

Verdrahtung MVU ↔ Dienste. Download-relevante Effekte:
`LoadDownloadsEffect` → `LoadDownloadsAsync` (lädt Liste + `GetUsedBytes`
+ Quota → `DownloadsLoadedMessage`), `EnqueueDownloadEffect`,
`CancelDownloadEffect`, `DeleteDownloadEffect`, `ResumeDownloadsEffect`,
`RunAutoDownloadsEffect`, `DeleteWatchedDownloadEffect`.
Abonniert `downloads.ItemChanged` → `DownloadItemChangedMessage`,
`downloads.ItemRemoved` → `DownloadDeletedMessage`.

`ResumeDownloadsEffect` wird ausgelöst bei Serververbindung/Online-Wechsel
(`AppUpdate` ~Z. 1045, 1118) — also nach App-Start und Netzwerk-Rückkehr.

## `DownloadsPage`
Dateien: `VideoPlayer.Maui/Pages/DownloadsPage.xaml`, `.xaml.cs`

Menüseite „Downloads": `QuotaLabel` (Belegung), `OfflineBanner`,
`ErrorPanel`, `EmptyLabel`, `CollectionView` je Eintrag mit Titel,
Zustandstext, Medientyp, Fortschrittsbalken/Prozent, Fehlertext und den
Schaltern **Abbrechen** / **Erneut versuchen** / **Löschen**.
`DownloadsPageOpenedMessage` lädt die Liste beim Öffnen.

## `AppStorageLayout` / `AppStorageLayoutProvider`
Dateien: `VideoPlayer.Storage/AppStorageLayout.cs`,
`VideoPlayer.Maui/Services/AppStorageLayoutProvider.cs`

- `VideosDirectory` = `{NoBackupDirectory}/videos` — auf iOS unterhalb von
  `FileSystem.AppDataDirectory/nobackup`, per `SetSkipBackupAttribute` vom
  Backup ausgenommen.
- Manifest liegt in `videoplayer-cache.db3` (Cache-DB, nicht
  backup-pflichtig). Geht die Cache-DB verloren (OS-Bereinigung,
  Neuinstallation, Profilwechsel), bleiben die Videodateien ohne
  Manifest-Eintrag zurück → verwaist, werden aber von `GetUsedBytes`
  mitgezählt.

## `INetworkStatusMonitor`
Datei: `VideoPlayer.Mvu/INetworkStatusMonitor.cs`

Abstraktion des Netzwerkstatus (`IsWifi`) für die WLAN-Bedingung.
