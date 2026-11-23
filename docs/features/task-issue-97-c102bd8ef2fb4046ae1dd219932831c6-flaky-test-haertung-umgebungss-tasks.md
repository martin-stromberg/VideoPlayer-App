# Tasks: Flaky-Test-Härtung der Test-Infrastruktur (Issue #97)

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Markierung | `[Trait("Category", "OsInterface")]` auf Klassenebene in `DevApiHttpServerTests` setzen (innerhalb `#if`-Block) | Erledigt | Kein direkter Test (CI-Filter `Category=OsInterface`) |
| 2 | Markierung | Trait in `DevApiServerTests_Metadata` setzen (innerhalb `#if`-Block) | Erledigt | Kein direkter Test (CI-Filter `Category=OsInterface`) |
| 3 | Markierung | Trait in `DevApiMetadataTests` setzen (innerhalb `#if`-Block) | Erledigt | Kein direkter Test (CI-Filter `Category=OsInterface`) |
| 4 | Markierung | Trait in `IosDeploymentResponseFileTests` setzen | Erledigt | Kein direkter Test (CI-Filter `Category=OsInterface`) |
| 5 | Markierung | Trait in `LocalDatabaseTests`, `FileLoggerTests`, `AppLogServiceTests`, `AppStorageLayoutTests` setzen | Erledigt | Kein direkter Test (CI-Filter `Category=OsInterface`) |
| 6 | Markierung | Trait in `AutoDownloadServiceTests`, `UpcomingEpisodesServiceTests`, `DownloadServiceTests_Expiry`, `DownloadServiceReconcileTests` setzen | Erledigt | Kein direkter Test (CI-Filter `Category=OsInterface`) |
| 7 | Markierung | Trait in `ServerSyncServiceTests`, `SeasonalGenreTests`, `ProgressReportServiceTests`, `PlaybackSourceServiceTests`, `PendingProgressSyncServiceTests`, `OfflineContinueWatchingServiceTests` setzen | Erledigt | Kein direkter Test (CI-Filter `Category=OsInterface`) |
| 8 | Markierung | Trait in `MediaDetailsServiceTests`, `KnownServersServiceTests`, `HomeListServiceTests`, `FavoritesServiceTests`, `DownloadServiceTests`, `ContinueWatchingMutationServiceTests` setzen | Erledigt | Kein direkter Test (CI-Filter `Category=OsInterface`) |
| 9 | Markierung | Trait auf Methodenebene an den 3 `DiscoverAsync_*`-Tests in `UdpServerDiscoveryTests` setzen | Erledigt | Kein direkter Test (CI-Filter `Category=OsInterface`) |
| 10 | Markierung | Trait in allen 19 E2E-Testklassen (`BootstrapTests` … `SourceSearchTests`) setzen | Erledigt | `OsInterfaceConventionTests.E2eTestClasses_CarryOsInterfaceTrait` |
| 11 | Markierung | Trait in `E2ETestBase` setzen (Dokumentation der Fixture-Sensitivität) | Erledigt | Kein direkter Test (`E2ETestBase.cs` ist vom Meta-Test-Scan ausgenommen) |
| 12 | Tests | `OsInterfaceConventionTests` in `VideoPlayer.Maui.Tests` anlegen: Regel „Flaky nur mit OsInterface" | Erledigt | `OsInterfaceConventionTests.FlakyTrait_RequiresOsInterface` |
| 13 | Tests | `OsInterfaceConventionTests`: Regel „jede E2E-Testklassen-Datei trägt OsInterface" | Erledigt | `OsInterfaceConventionTests.E2eTestClasses_CarryOsInterfaceTrait` |
| 14 | E2E-Support | `Support/TempDirectoryCleanup.cs` mit `DeleteWithRetry` (3 Versuche, Backoff 200/500/1000 ms, Log-Callback) anlegen | Erledigt | Kein direkter Test (lokaler E2E-Volllauf, Task 26) |
| 15 | E2E-Support | `AppInstance.DeleteProfileDirectory` auf `TempDirectoryCleanup.DeleteWithRetry` umstellen | Erledigt | Kein direkter Test (lokaler E2E-Volllauf, Task 26) |
| 16 | E2E-Support | `AppInstance.TryDelete` um optionalen Log-Parameter erweitern und Aufrufe verdrahten | Erledigt | Kein direkter Test (lokaler E2E-Volllauf, Task 26) |
| 17 | E2E-Support | `AppInstance.StartAsync` auf gemeinsames Deadline-Budget umstellen (`WaitForDevApiPortAsync`, `WaitForMainWindow`, initiales `WaitForModelAsync` teilen Restbudget) | Erledigt | Kein direkter Test (lokaler E2E-Volllauf, Task 26) |
| 18 | Skripte | `scripts/Retry-FlakyTests.ps1` erstellen (TRX-Fehlschlag-Parsing, genau ein gefilterter Re-Run) | Erledigt | Kein direkter Test (CI-Lauf, Task 27) |
| 19 | Skripte | `scripts/Analyze-FlakyTestResults.ps1` erstellen (Artefakt-Historie via `gh`, Flake-Rate, Schwellwert 2/10, Summary/Warning, deduplizierte Quarantäne-Issues) | Erledigt | Kein direkter Test (CI-Lauf, Task 27) |
| 20 | CI | `verify`-Test-Schritt in `pr-checks.yml` um `--filter "Category!=OsInterface"`, `--logger "trx"`, `--results-directory` und `upload-artifact` (`if: always()`) erweitern | Erledigt | Kein direkter Test (CI-Lauf, Task 27) |
| 21 | CI | Job `test-os-interface` in `pr-checks.yml` anlegen (`continue-on-error`, `timeout-minutes: 30`, Job-Permissions, Hauptlauf + Retry- + Upload- + Analyse-Schritt) | Erledigt | Kein direkter Test (CI-Lauf, Task 27) |
| 22 | Doku | `docs/help/stabilitaet/os-interface-tests.md` erstellen (Lanes, Trait-/Flaky-Konvention, Re-Run-Regel, Quarantäne, Debug-Gating-Hinweis) | Erledigt | Kein direkter Test |
| 23 | Doku | `docs/help/index.md` um Eintrag in „Infrastruktur" ergänzen | Erledigt | Kein direkter Test |
| 24 | Doku | `docs/help/e2e-tests.md` um CI-Hinweis und Trait-Verweis ergänzen (inkl. Korrektur der Testzählung) | Erledigt | Kein direkter Test |
| 25 | Verifikation | Lokale gefilterte Läufe (Release `!=`/`=`, Debug DevApi-Filter) ausführen und Testzahlen dokumentieren | Erledigt | Läufe vom 2026-11-22: Release `Category!=OsInterface` 1223/1223 grün + `Category=OsInterface` 263/263 grün (TRX `TestResults/verify`/`os-main`); Debug `Category=OsInterface&FullyQualifiedName~DevApi` 10/10 grün; `dotnet format whitespace --verify-no-changes` clean; Skript-Smoke 15/15 (`test-results.md`) |
| 26 | Verifikation | Einmaliger lokaler E2E-Volllauf als Härtungsnachweis (Deadline-Budget + Cleanup-Retry) | Erledigt | Lauf vom 2026-11-22: 74/74 grün in 7,7 min, 0 Flakes (TRX `TestResults/e2e/e2e-full.trx`, Log `TestResults/e2e-run.log`); Hinweis: App-Build benötigte lokal `IncludeIosTarget=false`/`IncludeAndroidTarget=false`/`IncludeMacCatalystTarget=false` als Env-Vars (net10-iOS-Workload fehlt, s. `test-results.md`) |
| 27 | Verifikation | CI-Lauf beobachten: `verify` grün ohne OsInterface-Tests, `test-os-interface` best-effort mit Artefakten | Erledigt | PR #99, Run 45068002791 (sha `0f65f50`): 4 Versuche — Versuch 1/2/4 komplett grün (verify+os-interface+tvos ✓), Versuch 3 verify+tvos grün, os-interface best-effort rot (Testhost-Crash 0xC0000005 nach 197/197 grünen Tests — Lane hielt wie designed, Artefakte+Analyse liefen); `verify` in allen 4 Versuchen grün |
