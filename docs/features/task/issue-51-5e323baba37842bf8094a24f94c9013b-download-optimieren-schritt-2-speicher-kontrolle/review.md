# Plan-Review: Speichertransparenz und Löschkontrolle

**Status:** Vollständig umgesetzt

- `DownloadListItem.SizeBytes`/`SizeText`/`HasSizeText` + `ToItem`-Mapping: umgesetzt.
- `DownloadService.DeleteAllAsync`/`PurgeOrphansAsync`: umgesetzt (Transfers abgebrochen, Dateien + Manifest entfernt; Restdateien ohne Manifest-Bezug gelöscht).
- Messages `DownloadsDeleteAllRequestedMessage`/`DownloadsPurgeOrphansRequestedMessage`, Effekte `DeleteAllDownloadsEffect`/`PurgeOrphanedDownloadsEffect`, `AppUpdate`-Handler, `ShellAppEffectHandler`-Verdrahtung mit Neu-Laden: umgesetzt.
- `DownloadsPage`: `SizeText`-Label je Eintrag, `DeleteAllButton` + `PurgeOrphansButton` mit `DisplayAlertAsync`-Rückfrage: umgesetzt.

## Offene Aufgaben
Keine.
