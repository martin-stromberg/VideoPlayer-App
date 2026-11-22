# Plan-Review

## Ergebnis

**Status:** Offene Aufgaben vorhanden

Alle Implementierungs-Planelemente sind vollständig umgesetzt. Offen sind ausschließlich die drei Verifikationsaufgaben (25–27), die keinen Code erzeugen und teilweise erst nach Commit/PR ausführbar sind.

## Umgesetzte Planelemente

- [x] `[Trait("Category", "OsInterface")]` auf Klassenebene in allen 24 geplanten `VideoPlayer.Maui.Tests`-Klassen — vorhanden (`DevApiHttpServerTests` Z. 19, `DevApiServerTests_Metadata` Z. 15, `DevApiMetadataTests` Z. 14 — jeweils innerhalb des `#if DEBUG || DEV_API`-Blocks; `IosDeploymentResponseFileTests`, `LocalDatabaseTests`, `FileLoggerTests`, `AppLogServiceTests`, `AppStorageLayoutTests`, `AutoDownloadServiceTests`, `UpcomingEpisodesServiceTests`, `ServerSyncServiceTests`, `SeasonalGenreTests`, `ProgressReportServiceTests`, `PlaybackSourceServiceTests`, `PendingProgressSyncServiceTests`, `OfflineContinueWatchingServiceTests`, `MediaDetailsServiceTests`, `KnownServersServiceTests`, `HomeListServiceTests`, `FavoritesServiceTests`, `DownloadServiceTests`, `DownloadServiceTests_Expiry`, `DownloadServiceReconcileTests`, `ContinueWatchingMutationServiceTests`)
- [x] Methodenebene-Trait an exakt den drei `DiscoverAsync_*`-Tests in `UdpServerDiscoveryTests` (Z. 64, 93, 119); die fünf `ResolveAnnouncedAddress_*`-Tests bleiben unmarkiert — vorhanden
- [x] `[Trait("Category", "OsInterface")]` auf allen 19 E2E-Testklassen unter `VideoPlayer.E2E.Tests/Tests/` plus `E2ETestBase` (Z. 14) — vorhanden
- [x] `TempDirectoryCleanup` (statische Helferklasse, `VideoPlayer.E2E.Tests/Support/TempDirectoryCleanup.cs`) — angelegt; `DeleteWithRetry(path, maxAttempts = 3, Action<string>? log)` mit Backoff 200/500/1000 ms, retry nur bei `IOException`/`UnauthorizedAccessException`, Protokollierung aller Versuche/Restfehler
- [x] `OsInterfaceConventionTests` (`VideoPlayer.Maui.Tests/OsInterfaceConventionTests.cs`, ohne eigenes Trait → blockierende Lane) — angelegt; beide Regeln über `Stubs/RepositoryRoot.Find()`:
  - [x] `FlakyTrait_RequiresOsInterface` — scannt beide Testprojekte, eigene Datei vom Scan ausgenommen
  - [x] `E2eTestClasses_CarryOsInterfaceTrait` — prüft jede `*Tests.cs` unter `VideoPlayer.E2E.Tests/Tests/` (`E2ETestBase.cs` greift das Muster nicht)
- [x] `scripts/Retry-FlakyTests.ps1` — angelegt; TRX-Parsing (`UnitTestResult outcome='Failed'` → `TestMethod className.name`), Filter `FullyQualifiedName~…` mit `|`, `&`-Verknüpfung mit `-Filter`, genau ein Re-Run; Exit 0 bei 0 TRX/0 Fehlschlägen und bei Re-Run-Testfehlschlägen, Exit 1 bei fehlendem `-TrxDirectory`
- [x] `scripts/Analyze-FlakyTestResults.ps1` — angelegt; `-RunLimit 10`/`-FlakeThreshold 2` mit `ValidateRange(1,1000)`, `os-main`/`os-rerun`-Verzeichnisstruktur, Flake-Definition „Hauptlauf Failed, Re-Run Passed", `gh run list`/`gh run download` bei `GH_TOKEN`, lokale Auswertung per `-ArtifactDirectory`, Step-Summary + `::warning`, deduplizierte `gh issue create` mit Label `flaky-test` (Label wird ggf. angelegt)
- [x] `pr-checks.yml` Job `verify`: Test-Schritt erweitert um `--filter "Category!=OsInterface"`, `--logger "trx"`, `--results-directory TestResults/verify` (Z. 42); neuer Schritt `Upload test results` mit `upload-artifact@v4`, `if: always()`, Name `verify-trx` (Z. 44–50) — vorhanden
- [x] `pr-checks.yml` neuer Job `test-os-interface` (Z. 66–113): `windows-latest`, `continue-on-error: true`, `timeout-minutes: 30`, Job-Permissions `contents: read`/`actions: read`/`issues: write`; Schritte Checkout → setup-dotnet → Workload → Restore → Build (Release) → Hauptlauf `--filter "Category=OsInterface"` nach `TestResults/os-main` → `Retry-FlakyTests.ps1` (`if: always()`) → Upload `os-interface-trx` (`if: always()`, `os-main` + `os-rerun`) → `Analyze-FlakyTestResults.ps1` (`if: always()`, `GH_TOKEN`) — vorhanden
- [x] `AppInstance.StartAsync` — einmaliges `deadline`-Budget (`startupTimeout ?? 45 s`, Z. 84); Restbudget an `WaitForDevApiPortAsync` (Z. 110), `WaitForMainWindow` (Z. 46 via Konstruktor) und initiales `WaitForModelAsync` (Z. 120: `deadline - DateTime.UtcNow`)
- [x] `AppInstance.WaitForMainWindow` — `DateTime deadline`-Parameter statt festem 30-s-Budget (Z. 398–411)
- [x] `AppInstance.DeleteProfileDirectory` — ruft je Kandidat `TempDirectoryCleanup.DeleteWithRetry(dir, log: Console.Error.WriteLine)` (Z. 331); kein stiller `catch` mehr
- [x] `AppInstance.TryDelete` — optionaler `Action<string>? log`-Parameter mit Protokollierung (Z. 419–428); beide Aufrufstellen verdrahtet (Z. 87, 186)
- [x] `docs/help/stabilitaet/os-interface-tests.md` — angelegt (Lanes, Trait-/Flaky-Konvention, Re-Run-Regel inkl. `gh run rerun --job`, Quarantäne-Schwellwert, Debug-Gating-Hinweis, Deadline-Staffelung, lokale Filterbefehle)
- [x] `docs/help/index.md` — Eintrag „OsInterface-Tests und Flake-Quarantäne" im Abschnitt „Infrastruktur"
- [x] `docs/help/e2e-tests.md` — CI-Hinweis (keine interaktive Desktop-Session, Issue #97), Trait-Verweis, Zählung auf „74 Testfälle in 19 Testklassen" korrigiert

## Offene Aufgaben

- [ ] Task 25 `Lokale gefilterte Läufe` — teilweise umgesetzt: Release-Läufe `Category!=OsInterface` (1223 grün) und `Category=OsInterface` (263 grün) wurden ausgeführt (TRX-Nachweis in `TestResults/verify` bzw. `TestResults/os-main`, Stand 2026-11-22); es fehlen der Debug-Lauf `--filter "Category=OsInterface&FullyQualifiedName~DevApi"` und die dokumentierte Nennung der Testzahlen (Summe 1223 + 263 = 1486 = Baseline 1484 + 2 neue Meta-Tests — plausibel, aber nicht dokumentiert)
- [ ] Task 26 `Lokaler E2E-Volllauf` — fehlt vollständig: kein Nachweis eines kompletten `dotnet test VideoPlayer.E2E.Tests`-Laufs (Härtungsnachweis für Deadline-Budget + Cleanup-Retry)
- [ ] Task 27 `CI-Lauf beobachten` — fehlt vollständig: erst nach Commit/PR möglich (`verify` grün ohne OsInterface-Tests, `test-os-interface` best-effort mit `verify-trx`/`os-interface-trx`-Artefakten)

## Hinweise

- Verzeichnis `TestResults/` (TRX-Nachweise der lokalen Läufe) liegt uncommitted im Repo-Root — beim Commit ggf. nicht mit aufnehmen bzw. `.gitignore` prüfen.
- `Analyze-FlakyTestResults.ps1` erwartet `os-main`/`os-rerun` auf Artefakt-Wurzelebene; `upload-artifact@v4` legt bei den Pfaden `TestResults/os-main` + `TestResults/os-rerun` genau diese Struktur an (gemeinsamer Vorfahre `TestResults/`) — konsistent.
- Tasks 26/27 sind strukturell Downstream-Arbeit (E2E braucht interaktive Session, CI-Nachweis braucht den PR-Lauf); sie gehören in die Verifikationsphase, nicht in die Nachimplementierung.
- Keine `[Trait("Flaky", …)]`-Vorkommen im Repo — konsistent mit der Planvorgabe „aktuell 0 Anwendungen".
