# Anforderung: Speichertransparenz und Löschkontrolle in der Download-Verwaltung

Quelle: Entwicklungsschritt 2 des Projektplans „Download optimieren" (Issue #51).

## Fachliche Zusammenfassung

Die Downloads-Seite zeigt die Dateigröße je Eintrag und bietet zwei neue Aktionen:
„Alle Downloads löschen" (mit Rückfrage) und „Speicher bereinigen" für erkannte
Restdaten ohne Manifest-Eintrag (mit freizugebender Größe). Alles offline verfügbar.

## Akzeptanzkriterien

1. Jeder Listeneintrag zeigt eine Größenangabe (fertig: Dateigröße; laufend:
   übertragene/erwartete Größe).
2. „Alle Downloads löschen" bricht alle Aufträge ab, entfernt alle Dateien und leert
   das Manifest — mit vorheriger Rückfrage.
3. Erkannte Restdaten sind mit Größe sichtbar und per Aktion „Speicher bereinigen"
   entfernbar.
4. Alle Funktionen arbeiten ohne Serververbindung (lokale Daten).

## Betroffene Klassen und Komponenten

- `DownloadListItem` (+ `SizeBytes`, Anzeige-Ableitung `SizeText`)
- `DownloadService` (`ToItem` ergänzt `SizeBytes`; neu `PurgeOrphansAsync`,
  `DeleteAllAsync`)
- Neue Messages: `DownloadsPurgeOrphansRequestedMessage`,
  `DownloadsDeleteAllRequestedMessage`
- Neue Effekte: `PurgeOrphanedDownloadsEffect`, `DeleteAllDownloadsEffect`
- `AppUpdate`, `ShellAppEffectHandler`, `DownloadsPage` (XAML + Code-Behind,
  `DisplayAlertAsync`-Rückfrage wie `PlaylistDetailsPage.ConfirmDeleteAsync`)
- Tests: `DownloadServiceReconcileTests`/`DownloadServiceTests`,
  `AppUpdateTests_Downloads`, `DownloadsContractTests`

## Nicht-Ziele

Keine iOS-Hintergrund-Downloads (Schritt 3). Kein Altbestand `VideoWebPlayer.Maui`.
