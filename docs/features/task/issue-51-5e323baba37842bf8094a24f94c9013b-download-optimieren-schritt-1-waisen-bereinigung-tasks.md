# Tasks: Verwaiste und unvollständige Download-Daten erkennen und bereinigen

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Datenmodell | `DownloadReconcileResult`-Record anlegen | Offen | — |
| 2 | Logik | `DownloadService.ErrorMissingFile`-Konstante + `ReconcileAsync` implementieren | Offen | — |
| 3 | Logik | `DownloadListItem.ErrorText` um Fall `missing_file` erweitern | Offen | — |
| 4 | MVU | `DownloadsLoadedMessage` um `OrphanedBytes` erweitern | Offen | — |
| 5 | MVU | `DownloadsPageState` um `OrphanedBytes`/`OrphanedText` erweitern | Offen | — |
| 6 | MVU | `AppUpdate.UpdateDownloadsLoaded` übernimmt `OrphanedBytes` | Offen | — |
| 7 | Effekt-Handler | `ShellAppEffectHandler.LoadDownloadsAsync` ruft `ReconcileAsync` + gibt Restdaten weiter | Offen | — |
| 8 | Effekt-Handler | `ShellAppEffectHandler.ResumeDownloadsAsync` ruft `ReconcileAsync` vor Wiederaufnahme | Offen | — |
| 9 | UI | `DownloadsPage.xaml`: `OrphanedLabel` unter `QuotaLabel` | Offen | — |
| 10 | Tests | Neue Testklasse `DownloadServiceReconcileTests` (9 Fälle laut Plan) | Offen | — |
| 11 | Tests | `AppUpdateTests_Downloads` an neue Message-Signatur + `OrphanedBytes`-Fall anpassen | Offen | — |
| 12 | Tests | `DownloadsContractTests` um `missing_file`-/`OrphanedText`-Fälle erweitern | Offen | — |
