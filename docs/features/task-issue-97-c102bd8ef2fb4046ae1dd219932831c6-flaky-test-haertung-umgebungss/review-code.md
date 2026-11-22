# Code-Review

## Ergebnis

**Status:** Keine Befunde

Abschlussreview nach vier Befund-Iterationen (6 → 3 → 2 → 1 Befund,
alle gefixt). Mit frischem Blick auf den Gesamt-Diff keine
substanziellen Korrektheitsprobleme mehr gefunden; verbleibende
Beobachtungen sind Hinweise ohne praktische Auswirkung.

## Verifikation des Iteration-4-Fixes

- **`Analyze-FlakyTestResults.ps1` `Get-RunFlakes` (Z. 58–63):** Die
  Member-Enumeration `(Get-ChildItem …).FullName` wurde wie empfohlen
  durch `@($mainTrx)`-Sammeln mit `Count`-Guard ersetzt. Empirisch
  nachgestellt: `-ArtifactDirectory` mit einem Lauf-Verzeichnis, dessen
  `os-main` existiert, aber keine `.trx` enthält → sauberer Durchlauf,
  korrekte Summary („Ausgewertete Laeufe: 1; Flakes: 0"), Exit 0 —
  keine `PropertyNotFoundException` mehr unter StrictMode.

## Stichproben-Verifikationen (alles bestanden)

- **Meta-Test `OsInterfaceConventionTests`:** `dotnet test -c Debug
  --filter "FullyQualifiedName~OsInterfaceConventionTests"` → 2/2 grün.
- **Lane-Filter wirksam:** `Category=OsInterface` → 273 Tests grün;
  `Category!=OsInterface` → 1245 Tests grün (VSTest-Adapter,
  xunit.runner.visualstudio). `OsInterfaceConventionTests` selbst läuft
  unmarkiert in der blockierenden Lane — bewusst so.
- **Trait-Platzierung:** alle 19 `*Tests.cs` unter
  `VideoPlayer.E2E.Tests/Tests` plus `E2ETestBase` mit genau einem
  `[Trait("Category", "OsInterface")]`; 24 Maui-Testdateien
  klassenweise (reine 1-Zeilen-Adds); `UdpServerDiscoveryTests`
  methodenweise an den drei `DiscoverAsync_*`-Tests;
  `DevApiHttpServerTests`, `DevApiServerTests_Metadata`,
  `DevApiMetadataTests` korrekt innerhalb der `#if DEBUG || DEV_API`-
  Blöcke.
- **Workflow `pr-checks.yml`:** `verify` filtert `Category!=OsInterface`
  + lädt `verify-trx`; `test-os-interface` mit `continue-on-error`,
  `timeout-minutes: 30`, Job-Permissions `contents/actions: read`,
  `issues: write`; `if: always()`-Kaskade Re-Run → Upload → Analyse mit
  `GH_TOKEN`. Artefakt-LCA `TestResults` liefert `os-main/` +
  `os-rerun/` an der Artefakt-Wurzel — deckt sich mit `Get-RunFlakes`
  (`gh run download --dir $target` → `$target/os-main`).
- **Skripte (Dot-Sourcing + StrictMode):** `FlakyTestTrx.ps1` via
  `$PSScriptRoot` eingebunden; `Get-TrxFailedTests` mit
  `Write-Output -NoEnumerate` (HashSet bleibt intakt — `.Count`,
  `-join`, Pipeline-Enumeration an allen Aufrufern korrekt);
  explizite Fehlschlag-Outcome-Menge Failed/Error/Timeout geteilt;
  `gh`-Aufrufe prüfen `$LASTEXITCODE` mit `::warning`-Kontext;
  `@()`-Wraps an allen zählenden Stellen; `2>$null` nur an gedämpften
  `gh`-Aufrufen; kein `nul`-Redirect.
- **`AppInstance`:** gemeinsames `deadline`-Budget über devapi-Poll,
  `WaitForMainWindow` und `WaitForModelAsync`; `remaining <= 0` →
  kontextierte `TimeoutException` vor dem Modell-Snapshot;
  Konstruktor-`WaitForMainWindow` wirft sofort bei erschöpfter
  Deadline → `catch` → Kill + Diagnose → rethrow. `TryDelete` mit
  Log-Callback an beiden Stellen; `DeleteProfileDirectory` →
  `TempDirectoryCleanup.DeleteWithRetry` mit Log-Spur.
- **`TempDirectoryCleanup`:** Backoff-Index `Math.Min(attempt - 1,
  Length - 1)` stimmt mit Doku überein; nur `IOException`/
  `UnauthorizedAccessException` retrywürdig; Restfehler protokolliert.
- **Repo-Konventionen/Doku:** `TestResults/` via `.gitignore`-Regel
  `[Tt]est[Rr]esult*/` abgedeckt (`git check-ignore` verifiziert);
  `os-interface-tests.md` deckt sich mit Implementierung (Outcome-
  Menge, Schwellwert 2/10, Lane-Filter, Debug-Gating, DevApiClient
  20 s / UiDriver 15 s-Staffelung verifiziert); `e2e-tests.md` und
  `index.md` konsistent verlinkt.

## Hinweise (keine Befunde)

- `OsInterfaceConventionTests.StripComments` wertet `//`/`/*` nicht
  in String-Literalen aus — theoretischer Scanner-Edge; betrifft
  reale Attributzeilen nicht und das Repo enthält derzeit 0
  `Flaky`-Vorkommen.
- `Get-TrxTestOutcomes` aggregiert pro FQN (letzter Outcome gewinnt
  bei Theory-Mehrfachergebnissen) — bewusste FQN-Granularität; der
  Re-Run-Filter deckt ohnehin alle Fälle des FQN ab.
- Die Skripte setzen PowerShell 7 voraus (`??`-Operator, `-File`-
  Parameter etc.) — in der CI via `pwsh` gewährleistet; unter
  Windows PowerShell 5.1 scheitern sie laut beim Parsen.
- `TempDirectoryCleanup.DeleteWithRetry`: `DirectoryNotFoundException`
  (Race zwischen `Exists`-Check und `Delete`) gilt als retrybar —
  kosmetisch, funktional harmlos.

## Geprüfte Dateien

Liste aller geprüften Dateien:

- `.github/workflows/pr-checks.yml`
- `scripts/FlakyTestTrx.ps1` (neu)
- `scripts/Retry-FlakyTests.ps1` (neu)
- `scripts/Analyze-FlakyTestResults.ps1` (neu)
- `VideoPlayer.E2E.Tests/Support/AppInstance.cs`
- `VideoPlayer.E2E.Tests/Support/TempDirectoryCleanup.cs` (neu)
- `VideoPlayer.E2E.Tests/Tests/E2ETestBase.cs` und alle 19
  E2E-Testklassen (`BootstrapTests.cs` … `SourceSearchTests.cs`)
- `VideoPlayer.Maui.Tests/OsInterfaceConventionTests.cs` (neu)
- `VideoPlayer.Maui.Tests/UdpServerDiscoveryTests.cs` sowie alle 24
  klassenmarkierten Testdateien
- `docs/help/stabilitaet/os-interface-tests.md` (neu),
  `docs/help/e2e-tests.md`, `docs/help/index.md`
- Lifecycle-Artefakte unter `docs/features/task-issue-97-…/`
  (nur Konsistenzsichtung)
