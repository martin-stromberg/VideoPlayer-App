# Tasks: Flaky-Test-Härtung der Test-Infrastruktur (Issue #97)

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Markierung | `[Trait("Category", "OsInterface")]` auf Klassenebene in `DevApiHttpServerTests` setzen (innerhalb `#if`-Block) | Offen | — |
| 2 | Markierung | Trait in `DevApiServerTests_Metadata` setzen (innerhalb `#if`-Block) | Offen | — |
| 3 | Markierung | Trait in `DevApiMetadataTests` setzen (innerhalb `#if`-Block) | Offen | — |
| 4 | Markierung | Trait in `IosDeploymentResponseFileTests` setzen | Offen | — |
| 5 | Markierung | Trait in `LocalDatabaseTests`, `FileLoggerTests`, `AppLogServiceTests`, `AppStorageLayoutTests` setzen | Offen | — |
| 6 | Markierung | Trait in `AutoDownloadServiceTests`, `UpcomingEpisodesServiceTests`, `DownloadServiceTests_Expiry`, `DownloadServiceReconcileTests` setzen | Offen | — |
| 7 | Markierung | Trait in `ServerSyncServiceTests`, `SeasonalGenreTests`, `ProgressReportServiceTests`, `PlaybackSourceServiceTests`, `PendingProgressSyncServiceTests`, `OfflineContinueWatchingServiceTests` setzen | Offen | — |
| 8 | Markierung | Trait in `MediaDetailsServiceTests`, `KnownServersServiceTests`, `HomeListServiceTests`, `FavoritesServiceTests`, `DownloadServiceTests`, `ContinueWatchingMutationServiceTests` setzen | Offen | — |
| 9 | Markierung | Trait auf Methodenebene an den 3 `DiscoverAsync_*`-Tests in `UdpServerDiscoveryTests` setzen | Offen | — |
| 10 | Markierung | Trait in allen 19 E2E-Testklassen (`BootstrapTests` … `SourceSearchTests`) setzen | Offen | — |
| 11 | Markierung | Trait in `E2ETestBase` setzen (Dokumentation der Fixture-Sensitivität) | Offen | — |
| 12 | Tests | `OsInterfaceConventionTests` in `VideoPlayer.Maui.Tests` anlegen: Regel „Flaky nur mit OsInterface" | Offen | — |
| 13 | Tests | `OsInterfaceConventionTests`: Regel „jede E2E-Testklassen-Datei trägt OsInterface" | Offen | — |
| 14 | E2E-Support | `Support/TempDirectoryCleanup.cs` mit `DeleteWithRetry` (3 Versuche, Backoff 200/500/1000 ms, Log-Callback) anlegen | Offen | — |
| 15 | E2E-Support | `AppInstance.DeleteProfileDirectory` auf `TempDirectoryCleanup.DeleteWithRetry` umstellen | Offen | — |
| 16 | E2E-Support | `AppInstance.TryDelete` um optionalen Log-Parameter erweitern und Aufrufe verdrahten | Offen | — |
| 17 | E2E-Support | `AppInstance.StartAsync` auf gemeinsames Deadline-Budget umstellen (`WaitForDevApiPortAsync`, `WaitForMainWindow`, initiales `WaitForModelAsync` teilen Restbudget) | Offen | — |
| 18 | Skripte | `scripts/Retry-FlakyTests.ps1` erstellen (TRX-Fehlschlag-Parsing, genau ein gefilterter Re-Run) | Offen | — |
| 19 | Skripte | `scripts/Analyze-FlakyTestResults.ps1` erstellen (Artefakt-Historie via `gh`, Flake-Rate, Schwellwert 2/10, Summary/Warning, deduplizierte Quarantäne-Issues) | Offen | — |
| 20 | CI | `verify`-Test-Schritt in `pr-checks.yml` um `--filter "Category!=OsInterface"`, `--logger "trx"`, `--results-directory` und `upload-artifact` (`if: always()`) erweitern | Offen | — |
| 21 | CI | Job `test-os-interface` in `pr-checks.yml` anlegen (`continue-on-error`, `timeout-minutes: 30`, Job-Permissions, Hauptlauf + Retry- + Upload- + Analyse-Schritt) | Offen | — |
| 22 | Doku | `docs/help/stabilitaet/os-interface-tests.md` erstellen (Lanes, Trait-/Flaky-Konvention, Re-Run-Regel, Quarantäne, Debug-Gating-Hinweis) | Offen | — |
| 23 | Doku | `docs/help/index.md` um Eintrag in „Infrastruktur" ergänzen | Offen | — |
| 24 | Doku | `docs/help/e2e-tests.md` um CI-Hinweis und Trait-Verweis ergänzen (inkl. Korrektur der Testzählung) | Offen | — |
| 25 | Verifikation | Lokale gefilterte Läufe (Release `!=`/`=`, Debug DevApi-Filter) ausführen und Testzahlen dokumentieren | Offen | — |
| 26 | Verifikation | Einmaliger lokaler E2E-Volllauf als Härtungsnachweis (Deadline-Budget + Cleanup-Retry) | Offen | — |
| 27 | Verifikation | CI-Lauf beobachten: `verify` grün ohne OsInterface-Tests, `test-os-interface` best-effort mit Artefakten | Offen | — |
