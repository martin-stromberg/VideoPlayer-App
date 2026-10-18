# Tasks: Alle fehlerhaften Downloads auf einmal löschen

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Logik | `DownloadService.DeleteFailedAsync` ergänzen | Offen | — |
| 2 | Logik | `DownloadsPageState.HasFailedItems` ergänzen | Offen | — |
| 3 | Logik | `DownloadsDeleteFailedRequestedMessage` anlegen | Offen | — |
| 4 | Logik | `DeleteFailedDownloadsEffect` anlegen | Offen | — |
| 5 | Logik | `AppUpdate`: Dispatch + `UpdateDownloadsDeleteFailedRequested` | Offen | — |
| 6 | Logik | `ShellAppEffectHandler`: Switch-Fall + `DeleteFailedDownloadsAsync` | Offen | — |
| 7 | UI | `DownloadsPage`: `DeleteFailedButton` + Gruppierung + `UpdateUi`/`OnDeleteFailedClicked` | Offen | — |
| 8 | UI | AutomationIds ergänzen (`DeleteFailedButton`, `DeleteAllButton`, `DownloadsCollection`, `EmptyLabel`, `DownloadButton` auf `MediaDetailsPage`) | Offen | — |
| 9 | Tests | Service-Tests `DeleteFailed_*` in `DownloadServiceTests` | Offen | — |
| 10 | Tests | Transition-Tests `DownloadsDeleteFailed_*` in `AppUpdateTests_Downloads` | Offen | — |
| 11 | Tests | Contract-Tests in `DownloadsContractTests` | Offen | — |
| 12 | E2E-Tests | `DownloadsTests`: Fehler-Löschfluss + Sichtbarkeitsregel | Offen | — |
