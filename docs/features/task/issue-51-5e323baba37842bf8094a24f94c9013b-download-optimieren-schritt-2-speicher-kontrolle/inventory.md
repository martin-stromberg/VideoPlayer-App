# Bestandsaufnahme: Speichertransparenz und Löschkontrolle

## Zusammenfassung

- `DownloadManifestEntry.SizeBytes` existiert, wird aber nicht in `DownloadListItem`
  übernommen → keine Größenanzeige je Eintrag.
- `DownloadService.DeleteAsync` löscht einzelne Downloads; „alle löschen" fehlt.
- `ReconcileAsync` (Schritt 1) erkennt Restdaten (`OrphanedBytes` bis zur Seite);
  eine Löschaktion fehlt.
- Bestätigungsdialog-Muster: `DisplayAlertAsync` im Code-Behind
  (`PlaylistDetailsPage.ConfirmDeleteAsync`), Entscheidung als Message zurück ins MVU.
- Test-Ausgangszustand: 1350 Tests grün (Schritt-1-Stand).

## Details

Details liegen in der Projekt-Bestandsaufnahme
(`docs/projects/task/issue-51-…/inventory/`) und in der Schritt-1-Feature-
Bestandsaufnahme (Git-Historie des Schritt-Branches).
