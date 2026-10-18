# Bestandsaufnahme: Alle fehlerhaften Downloads auf einmal löschen

Analyse der Download-Verwaltung (MVU + MAUI-Seite) bezogen auf die Anforderung, fehlerhafte/abgebrochene Downloads gesammelt zu entfernen (Issue #55).

## Zusammenfassung

- Der MVU-Fluss für Sammelaktionen ist vollständig etabliert: `DownloadsDeleteAllRequestedMessage` → `UpdateDownloadsDeleteAllRequested` → `DeleteAllDownloadsEffect` → `ShellAppEffectHandler.DeleteAllDownloadsAsync` → `DownloadService.DeleteAllAsync` + `LoadDownloadsAsync` (aktualisiert Liste und Belegung). Dasselbe Muster existiert für `DownloadsPurgeOrphansRequestedMessage`/`PurgeOrphanedDownloadsEffect`/`PurgeOrphansAsync`.
- `DownloadService.DeleteAllAsync` löscht alle Einträge ungefiltert; eine zustandsgefilterte Variante existiert noch nicht.
- `DownloadState` umfasst `Queued`, `Downloading`, `Completed`, `Failed`, `Cancelled`; persistiert als Kleinbuchstaben über `DownloadStateNames`. `CanRetry` auf `DownloadListItem` deckt bereits `Failed`/`Cancelled` ab.
- `DownloadsPageState` bietet abgeleitete Eigenschaften (`HasItems`, `ShowEmpty`, `OrphanedText`); eine „fehlerhafte Einträge vorhanden"-Ableitung fehlt.
- Die `DownloadsPage` besitzt im Kopfbereich `DeleteAllButton` („Alle löschen", Rückfrage via `DisplayAlertAsync`) sowie `PurgeOrphansButton`; ein gefilterter Lösch-Schalter fehlt.
- Es existiert kein Mechanismus, der `Failed`/`Cancelled`-Einträge gebündelt entfernt — nur Einzel-Löschen (`DeleteAsync`) und Gesamt-Löschen (`DeleteAllAsync`).

Test-Ausgangszustand: siehe [Tests](inventory/tests.md).

## Details

- [Datenmodell](inventory/models.md)
- [Logik](inventory/logic.md)
- [Enums](inventory/enums.md)
- [Interfaces](inventory/interfaces.md)
- [Tests](inventory/tests.md)
