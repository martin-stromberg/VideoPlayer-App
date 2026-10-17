# Abnahmeprüfung – Entwicklungsschritt 1

Geprüft: `git diff` Basisbranch → Schritt-Branch, Quelldateien und Testnachweise
(`DownloadServiceReconcileTests`, `AppUpdateTests_Downloads`, `DownloadsContractTests`,
1350/1350 Tests grün).

## Ergebnis

**Status:** Anforderung vollständig erfüllt

## Abweichungen

Keine.

## Hinweise

- AK 1: Datei ohne Manifest-Eintrag → als Restdaten erkannt/gezählt
  (`OrphanedBytes`/`OrphanedFileCount`), nicht gelöscht —
  `Reconcile_OrphanedFile_IsCountedAndKept`.
- AK 2: „fertig" ohne Datei → `failed`/`missing_file`, erneut einplanbar —
  `Reconcile_CompletedWithoutFile_MarkedMissing`.
- AK 3: Belegungsanzeige = tatsächlicher Dateibestand; Restdaten separat
  ausgewiesen (`OrphanedLabel`).
- AK 4: `completed`- und laufende Transfer-Dateien unberührt —
  `Reconcile_CompletedFile_IsKept`, `Reconcile_RunningTransferFile_IsKept`.
- Abgleich-Aufrufe: `LoadDownloadsAsync` (Seitenaufruf) und
  `ResumeDownloadsAsync` (App-Start/Verbindung) — wie gefordert.
- Stakeholder-Entscheidung (Restdaten bleiben sichtbar, Bereinigung per
  Aktion in Schritt 2) eingehalten.
