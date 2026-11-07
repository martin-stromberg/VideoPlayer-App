# Umsetzungsplan: Speichertransparenz und Löschkontrolle

## Übersicht

Größenanzeige je Download-Eintrag, Aktion „Alle Downloads löschen" (mit
`DisplayAlertAsync`-Rückfrage) und Bereinigungs-Aktion für erkannte Restdaten
auf der Downloads-Seite.

## Designentscheidungen

| Bereich | Ansatz | Begründung |
|---------|--------|------------|
| Rückfrage | `DisplayAlertAsync` im Code-Behind vor dem Dispatch (Muster: `PlaylistDetailsPage.ConfirmDeleteAsync`) | bestehende Konvention |
| Restdaten-Bereinigung | `DownloadService.PurgeOrphansAsync` löscht nur Dateien ohne Manifest-Bezug; danach Liste neu laden | trennt verwaltete Downloads von Restdaten; Wiederverwendung der Abgleich-Regeln aus Schritt 1 |
| Größenanzeige | `DownloadListItem.SizeBytes` + abgeleitetes `SizeText` (Format „x,y GB/MB") | manifest-getrieben, offline verfügbar |

## Programmabläufe

### Alle Downloads löschen

1. `DownloadsDeleteAllRequestedMessage` → `UpdateDownloadsDeleteAllRequested`
   → `DeleteAllDownloadsEffect` (mit `Settings` für Pump-Stop).
2. `ShellAppEffectHandler.DeleteAllDownloadsAsync`: `downloads.DeleteAllAsync()`
   bricht laufende Transfers ab, löscht alle Dateien und das Manifest; danach
   `DownloadsPageOpenedMessage`-Äquivalent neu laden (Dispatch `DownloadsPageOpenedMessage`).
3. Rückfrage erfolgt im Code-Behind (`OnDeleteAllClicked` → `DisplayAlertAsync`).

### Speicher bereinigen (Restdaten)

1. `DownloadsPurgeOrphansRequestedMessage` → `PurgeOrphanedDownloadsEffect` →
   `PurgeOrphansAsync` löscht Restdateien → danach Liste + Belegung neu laden
   (`OrphanedBytes` = 0).

### Größenanzeige

- `ToItem` übernimmt `SizeBytes` aus dem Manifest; `SizeText` zeigt bei
  `Completed` die Dateigröße, bei `Downloading` `DownloadedBytes/TotalBytes`
  formatiert, sonst `null` (kein Label).
- Neues `Label` im Eintrags-Template (`{Binding SizeText}`, sichtbar wenn
  gesetzt).

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `DownloadsDeleteAllRequestedMessage` | AppMessage | Anwender löst Gesamtlöschung aus (nach Rückfrage) |
| `DownloadsPurgeOrphansRequestedMessage` | AppMessage | Bereinigung der Restdaten anfordern |
| `DeleteAllDownloadsEffect` | AppEffect | Alle Downloads entfernen |
| `PurgeOrphanedDownloadsEffect` | AppEffect | Restdaten ohne Manifest-Bezug löschen |

## Änderungen an bestehenden Klassen

- `DownloadService`: `ToItem` + `SizeBytes`; `DeleteAllAsync()` (alle Transfers
  abbrechen, Dateien + Manifest löschen, `ItemRemoved` je Eintrag);
  `PurgeOrphansAsync()` (Restdateien löschen → `long` freigegebene Bytes).
- `DownloadListItem`: `SizeBytes`, `SizeText`.
- `AppUpdate`: Handler `UpdateDownloadsDeleteAllRequested`,
  `UpdateDownloadsPurgeOrphansRequested` (Dispatch der Effekte).
- `ShellAppEffectHandler`: `DeleteAllDownloadsAsync`, `PurgeOrphanedDownloadsAsync`
  (jeweils mit anschließendem Neu-Laden über `LoadDownloadsAsync`).
- `DownloadsPage.xaml(.cs)`: `SizeLabel` im Eintrag, `DeleteAllButton` +
  `PurgeOrphansButton` (unter `OrphanedLabel`), Handler mit
  `DisplayAlertAsync`-Rückfrage für „Alle löschen"; Bereinigen mit Rückfrage
  (freizugebende Größe im Dialog).

## Datenbankmigrationen

Keine.

## Validierungsregeln

Keine (Rückfrage-Dialoge keine Eingabefelder).

## Konfigurationsänderungen

Keine.

## Seiteneffekte und Risiken

- `DeleteAllAsync` während laufender Transfers: Abbruch aller
  `runningTransfers`, `PumpQueue` darf danach nichts Neues starten (Queue
  leer). Doppelte `ItemRemoved`/Zustands-Events tolerierbar.
- `PurgeOrphansAsync` läuft nur bei sichtbarer Seite — konservativ keine
  Transfers betroffen (nur Dateien ohne Manifest-Bezug).

## Umsetzungsreihenfolge

1. `DownloadListItem` + `ToItem` (`SizeBytes`/`SizeText`) — Voraussetzungen: keine
2. `DownloadService.DeleteAllAsync`/`PurgeOrphansAsync` — Voraussetzungen: keine
3. Messages + Effekte + `AppUpdate`-Handler — Voraussetzungen: Schritt 2
4. `ShellAppEffectHandler`-Verdrahtung — Voraussetzungen: Schritt 3
5. `DownloadsPage` XAML + Code-Behind — Voraussetzungen: Schritte 1, 3
6. Tests — Voraussetzungen: Schritte 1–5

## Tests

### Neue Tests

| Test | Testklasse | Was wird geprüft |
|------|------------|-------------------|
| `DeleteAll_RemovesFilesAndManifest` | `DownloadServiceReconcileTests` | alle Dateien + Einträge weg, laufende abgebrochen |
| `PurgeOrphans_RemovesOnlyOrphaned` | `DownloadServiceReconcileTests` | Manifest-Dateien bleiben, Reste weg, Bytes-Rückgabe |
| `UpdateDownloadsDeleteAllRequested_Effect` | `AppUpdateTests_Downloads` | `DeleteAllDownloadsEffect` wird erzeugt |
| `UpdateDownloadsPurgeOrphansRequested_Effect` | `AppUpdateTests_Downloads` | `PurgeOrphanedDownloadsEffect` wird erzeugt |
| `DownloadListItem_SizeText_*` | `DownloadsContractTests` | Format/Anzeige je Zustand |
| `DownloadsPage_HasDeleteAllAndPurge` | `DownloadsContractTests` | neue Buttons + Handler + Binding |

### Betroffene bestehende Tests

Keine (additive Änderungen).

### E2E-Tests

Der geänderte Benutzerfluss (Downloads-Seite mit Lösch-/Bereinigungsaktionen)
ist über die bestehende Konvention abgesichert: MVU-Übergangstests +
Contract-Tests; ein Downloads-E2E-Szenario existiert nicht. Begründung wie in
Schritt 1: UI-E2E für Downloads ist im Repository nicht eingerichtet; die
Nachweise liegen über xUnit-Abdeckung der Messages/Effekte und der Seite.

## Offene Punkte

Keine.
