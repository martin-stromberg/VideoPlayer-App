# Bestandsaufnahme: Verwaiste Download-Daten erkennen und bereinigen

Analyse der Download-Ablage (`VideoPlayer.Mvu`/`VideoPlayer.Storage`) bezogen auf den
Manifest↔Dateisystem-Abgleich. Vertiefte Detailanalyse liegt in der Projekt-
Bestandsaufnahme unter `docs/projects/task/issue-51-…-download-optimieren/inventory/`.

## Zusammenfassung

- `DownloadService.GetUsedBytes()` summiert alle Dateien in `layout.VideosDirectory` —
  inklusive Dateien ohne Manifest-Eintrag (verwaiste Restdaten werden mitgezählt, aber
  nirgends ausgewiesen).
- Dateinamen folgen dem Muster `{typ}_{mediaId}.mp4` (`FilePathFor`), gespeichert in
  `DownloadManifestEntry.FilePath`.
- Teildateien werden auf allen verwalteten Fehlerpfaden via `TryDeleteFile` entfernt —
  **nicht** bei Prozessabbruch. Einträge mit `State = failed`/`cancelled` behalten
  `FilePath`, obwohl die Datei gelöscht wurde (oder Reste liegen können).
- Es gibt keinen Verzeichnis-Scan gegen das Manifest — keine Waisen-Erkennung.
- `ResumeInterruptedAsync` (App-Start, Verbindungs-/Online-Wechsel) reaktiviert
  `queued`/`downloading`/`failed`-Einträge — der Transfer beginnt wegen `FileMode.Create`
  ab Byte 0 und überschreibt Reste desselben Mediums.
- „Fertig"-Einträge ohne Datei: `EnqueueAsync` erkennt `completed && !fileExists` bereits
  als verwaist und reiht erneut ein — aber nur bei erneuter Anforderung; die Liste zeigt
  weiterhin „Fertig".
- `DownloadsPageState.QuotaText` zeigt `UsedBytes` (= Dateisystem-Summe) — stimmt nach
  Abgleich mit dem Bestand überein, enthält aber keine Unterscheidung für Restdaten.
- Test-Ausgangszustand: 1335 Tests bestanden, 0 Fehler (siehe tests.md).

## Details

- [Datenmodell](inventory/models.md)
- [Logik](inventory/logic.md)
- [Tests](inventory/tests.md)
