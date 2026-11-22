# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### Analyze-FlakyTestResults.ps1 (Get-RunFlakes)

- **Fehlerbehandlung / Strict-Mode-Robustheit** — Z. 58–59 liest
  `(Get-ChildItem -LiteralPath $mainDir -Filter "*.trx" -File).FullName`
  per Member-Enumeration direkt in den `-TrxPaths`-Parameter. Existiert
  `os-main` (die `Test-Path`-Wache in Z. 54 greift), enthält aber keine
  `.trx`-Datei, wirft die Member-Enumeration unter
  `Set-StrictMode -Version Latest` eine `PropertyNotFoundException`
  („Die Eigenschaft FullName kann in diesem Objekt nicht gefunden
  werden") — mit `$ErrorActionPreference = "Stop"` bricht die gesamte
  Auswertung ab statt den Lauf zu überspringen. Empirisch verifiziert
  (leeres `os-main`-Verzeichnis → throw). Erreichbar im dokumentierten
  lokalen `-ArtifactDirectory`-Modus (teilweise abgelegte
  Artefakt-Verzeichnisse); in der CI-Download-Strecke faktisch nur, wenn
  ein Artefakt ein `os-main` ohne `.trx` enthielte. Es ist dieselbe
  Strict-Mode-Fehlerklasse wie der soeben gefixte
  HashSet-Unrolling-Befund — dieses Vorkommen wurde übersehen.

  Empfehlung: Wie in `Retry-FlakyTests.ps1` (Z. 52–56) zuerst in ein
  Array sammeln und auf 0 prüfen:
  ```powershell
  $trxFiles = @(Get-ChildItem -LiteralPath $mainDir -Filter "*.trx" -File)
  if ($trxFiles.Count -eq 0) { return @() }
  $failed = Get-TrxFailedTests -TrxPaths $trxFiles.FullName
  ```

## Verifikation der Iteration-3-Fixes

Beide gemeldeten Fixes sind verifiziert:

1. **`FlakyTestTrx.ps1` `Write-Output -NoEnumerate $failed`** — unter
   `Set-StrictMode -Version Latest` empirisch geprüft: 0 Treffer →
   `HashSet` mit `Count=0`, 1 Treffer → `Count=1` (`Contains` trifft),
   2 Treffer → `Count=2`. Dot-Sourcing-Aufrufer korrekt:
   `$failed.Count` (Retry Z. 66, Analyze Z. 60), `-join` (Retry Z. 72)
   und Pipeline-Enumeration (Retry Z. 74) arbeiten auf dem intakten
   HashSet. `Get-TrxTestOutcomes` liefert die Hashtable un-enumiert
   (`type=Hashtable`, `.GetEnumerator()` nutzbar) — `return $outcomes`
   ist hier unproblematisch, weil PowerShell `IDictionary` nicht
   auspackt.
2. **`OsInterfaceConventionTests.cs` `IsUnderBuildOutputDirectory`** —
   prüft jetzt Segmente von `Path.GetRelativePath(directory, file)`
   (Z. 53–55); `bin`/`obj` oberhalb des Klons beeinflussen den Scan
   nicht mehr. Meta-Tests danach ausgeführt: 2/2 grün (Debug-Build
   fehlerfrei).

## Weitere geprüfte Punkte (ohne Befund)

- **Trait-Platzierung:** alle 19 `*Tests.cs` unter
  `VideoPlayer.E2E.Tests/Tests` plus `E2ETestBase` tragen je genau ein
  `[Trait("Category", "OsInterface")]`; 24 Maui-Testdateien klassenweise
  (reine 1-Zeilen-Additionen, 27 Insertions, keine anderen Änderungen),
  `UdpServerDiscoveryTests` methodenweise an den drei
  `DiscoverAsync_*`-Tests; `DevApi*` -Traits korrekt innerhalb der
  `#if DEBUG || DEV_API`-Blöcke; `DevApiContractTests` und
  `OsInterfaceConventionTests` selbst bewusst unmarkiert (laufen in der
  blockierenden Lane).
- **Konventions-Meta-Test:** `AttributeBlock`/`IsBlockLine`/
  `StripComments` ordnen Traits deklarationsbezogen zu; `Flaky` ohne
  `OsInterface` im selben Block würde erkannt (aktuell 0
  `Flaky`-Vorkommen im Repo, plangemäß); rekursiver Scan mit
  bin/obj-Ausschluss greift.
- **Workflow `test-os-interface`:** `continue-on-error: true`,
  `timeout-minutes: 30`, Permissions (`contents/actions: read`,
  `issues: write`), `if: always()`-Kaskade Re-Run → Upload → Analyse,
  `GH_TOKEN`-Mapping; `verify`-Lane mit `Category!=OsInterface` +
  `verify-trx`-Upload. Artefakt-LCA `TestResults` liefert
  `os-main/` + `os-rerun/` an der Artefakt-Wurzel — deckt sich mit
  `Get-RunFlakes`.
- **`AppInstance`:** gemeinsames `deadline`-Budget über devapi-Poll,
  `WaitForMainWindow` und `WaitForModelAsync`; `remaining <= 0` wirft
  kontextierte `TimeoutException` (Z. 121–127); `TryDelete` mit
  Log-Callback an beiden Stellen; `DeleteProfileDirectory` nutzt
  `TempDirectoryCleanup.DeleteWithRetry` mit Log-Spur.
- **`TempDirectoryCleanup`:** Backoff-Indexierung (`attempt-1`,
  geklemmt) stimmt mit der Doku überein; nur
  `IOException`/`UnauthorizedAccessException` retrywürdig; Restfehler
  protokolliert statt geschluckt.
- **Skript-Robustheit sonst:** `gh`-Aufrufe prüfen `$LASTEXITCODE` mit
  `::warning`-Kontext; `@()`-Wraps an allen zählenden Stellen;
  Dot-Sourcing-Pfade via `$PSScriptRoot`; `2>$null` nur an gedämpften
  `gh`-Aufrufen mit anschließender Exit-Prüfung.
- **Repo-Konventionen:** `TestResults/` via `.gitignore`-Regel
  `[Tt]est[Rr]esult*/` abgedeckt (`git check-ignore` verifiziert);
  kein `nul`-Redirect.
- **Doku:** `os-interface-tests.md` deckt sich mit Implementierung
  (Outcome-Menge Failed/Error/Timeout, Schwellwert 2/10, Lane-Filter,
  Debug-Gating, lokale Filter-Kommandos); `e2e-tests.md` und `index.md`
  konsistent verlinkt.

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
