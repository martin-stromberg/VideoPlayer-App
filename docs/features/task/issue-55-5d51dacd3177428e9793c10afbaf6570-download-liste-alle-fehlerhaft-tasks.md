# Tasks: Alle fehlerhaften Downloads auf einmal löschen

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Logik | `DownloadService.DeleteFailedAsync` ergänzen | Erledigt | `DownloadServiceTests.DeleteFailed_FailedAndCancelled_RemovesOnlyThose`, `DeleteFailed_NoMatchingEntries_ReturnsZero` |
| 2 | Logik | `DownloadsPageState.HasFailedItems` ergänzen | Erledigt | `DownloadsContractTests.DownloadsPageState_HasFailedItems_OnlyFailedOrCancelled` |
| 3 | Logik | `DownloadsDeleteFailedRequestedMessage` anlegen | Erledigt | `AppUpdateTests_Downloads.DownloadsDeleteFailed_WithFailedItems_EmitsEffect` (Dispatch würde ohne Message nicht kompilieren) |
| 4 | Logik | `DeleteFailedDownloadsEffect` anlegen | Erledigt | `AppUpdateTests_Downloads.DownloadsDeleteFailed_WithFailedItems_EmitsEffect` |
| 5 | Logik | `AppUpdate`: Dispatch + `UpdateDownloadsDeleteFailedRequested` | Erledigt | `AppUpdateTests_Downloads.DownloadsDeleteFailed_*` |
| 6 | Logik | `ShellAppEffectHandler`: Switch-Fall + `DeleteFailedDownloadsAsync` | Erledigt | `DownloadsContractTests.DownloadsPage_WiresDeleteFailed` |
| 7 | UI | `DownloadsPage`: `DeleteFailedButton` + Gruppierung + `UpdateUi`/`OnDeleteFailedClicked` | Erledigt | `DownloadsContractTests.DownloadsPage_WiresDeleteFailed`, E2E `DownloadsTests.DownloadsPage_DeleteFailed_RemovesFailedEntry` |
| 8 | UI | AutomationIds ergänzen (`DeleteFailedButton`, `DeleteAllButton`, `DownloadsCollection`, `EmptyLabel`, `DownloadButton` auf `MediaDetailsPage`) | Erledigt | E2E `DownloadsTests.DownloadsPage_DeleteFailed_RemovesFailedEntry` (nutzt alle Ids) |
| 9 | Tests | Service-Tests `DeleteFailed_*` in `DownloadServiceTests` | Erledigt | Laufen grün (siehe `test-results.md`) |
| 10 | Tests | Transition-Tests `DownloadsDeleteFailed_*` in `AppUpdateTests_Downloads` | Erledigt | Laufen grün (siehe `test-results.md`) |
| 11 | Tests | Contract-Tests in `DownloadsContractTests` | Erledigt | Laufen grün (siehe `test-results.md`) |
| 12 | E2E-Tests | `DownloadsTests`: Fehler-Löschfluss + Sichtbarkeitsregel | Erledigt | `DownloadsTests.DownloadsPage_DeleteFailed_RemovesFailedEntry` (grün, siehe `e2e-downloads.log`) |
