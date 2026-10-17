# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan vollständig

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis | Status |
|--------------------|-------------------|--------------|--------|
| Größe je Eintrag | `SizeBytes`/`SizeText` | `DownloadListItem_SizeText_*` | Abgedeckt |
| Alle löschen mit Rückfrage | `DeleteAllAsync` + `DisplayAlertAsync` | `DeleteAll_RemovesFilesAndManifest`, `UpdateDownloadsDeleteAllRequested_Effect`, Contract-Test | Abgedeckt |
| Restdaten sichtbar + bereinigbar | `PurgeOrphansAsync` + Button | `PurgeOrphans_RemovesOnlyOrphaned`, `UpdateDownloadsPurgeOrphansRequested_Effect` | Abgedeckt |
| Offline verfügbar | lokale Manifest-/Datei-Operationen | Service-Tests ohne Server | Abgedeckt |

## E2E-Abdeckung

| Fluss | Status |
|-------|--------|
| Lösch-/Bereinigungsaktionen | Nicht erforderlich mit Begründung (Projektkonvention: MVU-Übergangs- + Contract-Tests; kein Downloads-E2E-Szenario vorhanden) |
