# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### FlakyTestTrx.ps1 (Get-TrxFailedTests) — betrifft Retry-FlakyTests.ps1 und Analyze-FlakyTestResults.ps1

- **Fehlerbehandlung / Rückgabe-Semantik** — `Get-TrxFailedTests`
  (Z. 58–73) gibt ein `HashSet[string]` per `return $failed` zurück.
  PowerShell **unrollt `IEnumerable`-Rückgabewerte** beim Verlassen
  der Funktion: bei 0 Treffern erhält der Aufrufer `$null`, bei
  genau 1 Treffer ein `string`, erst ab 2 Treffern ein `object[]`.
  Damit bricht `$failed.Count` unter `Set-StrictMode -Version Latest`
  in genau den beiden häufigsten Fällen:

  - `scripts/Retry-FlakyTests.ps1` Z. 64–66: Hauptlauf komplett grün
    (0 Fehlschläge — der Normalfall) oder genau 1 Fehlschlag →
    `PropertyNotFoundException` („Eigenschaft Count kann in diesem
    Objekt nicht gefunden werden") → **Exit 1** statt der
    vorgesehenen „kein Re-Run"-Meldung mit Exit 0. Reproduziert:
    TRX mit nur `Passed`-Results → Skript-Exit 1.
  - `scripts/Analyze-FlakyTestResults.ps1` `Get-RunFlakes`
    (Z. 58–60): identischer Aufruf — ein Lauf mit 0 oder 1
    Fehlschlägen in `os-main` lässt die gesamte Auswertung
    (`$ErrorActionPreference = "Stop"`) abbrechen, statt „keine
    Flakes" zu melden.
  - Ab 2 Fehlschlägen funktioniert der Code zufällig (`object[]`
    hat `Count`), weshalb der Fehler im Smoke-Test mit zwei
    Fehlschlägen nicht auffällt. Regression der Iteration-3-
    Auslagerung: der frühere Inline-Code baute die Menge im
    Skript-Scope ohne Funktionsgrenze.

  Empfehlung: Die Menge un-enumeriert zurückgeben — in
  `Get-TrxFailedTests` `return ,$failed` (Komma-Operator) oder
  `Write-Output $failed -NoEnumerate`; alternativ an beiden
  Aufrufstellen `@(Get-TrxFailedTests ...)` wrappen und
  konsistent mit Array-Semantik weiterarbeiten. Danach Smoke-Test
  mit 0, 1 und ≥2 Fehlschlägen wiederholen.

### OsInterfaceConventionTests.cs (IsUnderBuildOutputDirectory)

- **Testqualität (stilles Vakuum)** — `IsUnderBuildOutputDirectory`
  (Z. 49–53) prüft die Segmente des **gesamten absoluten Pfads**
  auf `bin`/`obj`. Liegt der Repository-Klon selbst unter einem
  Verzeichnis mit einem dieser Namen (z. B. `C:\work\bin\repos\…`),
  liefert `SourceFiles` null Dateien und beide Konventionstests
  laufen kommentarlos grün — die Konvention wäre dann wirkungslos,
  ohne dass es auffällt.

  Empfehlung: Die Segment-Prüfung nur auf den Pfad relativ zum
  Scan-Verzeichnis anwenden, z. B.
  `Path.GetRelativePath(directory, file).Split(...)` in
  `SourceFiles` (Basisverzeichnis an
  `IsUnderBuildOutputDirectory` durchreichen).

## Verifikation der Iteration-3-Fixes

- **Dot-Sourcing:** `. (Join-Path $PSScriptRoot "FlakyTestTrx.ps1")`
  in beiden Skripten korrekt; Smoke-Test beweist Auflösung.
- **Outcome-Menge:** `Failed|Error|Timeout` zentral in
  `$script:TrxFailureOutcomes`; verifiziert, dass `Failed`+`Error`
  in der Menge landen, `Passed`+`NotExecuted` nicht. Flake-Logik
  „main fehlgeschlagen ∩ rerun `Passed`" per synthetischer TRX
  verifiziert (1 Flake korrekt erkannt, `Error`-im-Re-Run nicht).
- **Rekursiver Scan:** `SearchOption.AllDirectories` + bin/obj-
  Ausschluss greift (s. Befund 2 zum Pfad-Randfall); generierte
  Dateien unter `obj/` stören nicht.
- **`Write-Host -f`-Fix:** Z. 71–72 korrekt geklammert.

## Weitere geprüfte Punkte (ohne Befund)

- **Workflow-YAML:** `test-os-interface`-Job konsistent —
  `continue-on-error: true`, `timeout-minutes: 30`, Job-Permissions
  (`contents/actions: read`, `issues: write`), `if: always()`-Kaskade
  Re-Run → Upload → Analyse, `GH_TOKEN` korrekt gemappt;
  `verify`-Lane mit `Category!=OsInterface` + `verify-trx`-Upload.
- **`AppInstance`:** gemeinsames `deadline`-Budget über devapi-Poll,
  `WaitForMainWindow` und `WaitForModelAsync`; `remaining <= 0` wirft
  kontextierte `TimeoutException`; `TryDelete`/`DeleteWithRetry` mit
  Log-Callback an allen Aufrufstellen verdrahtet.
- **`TempDirectoryCleanup`:** Backoff-Indexierung
  (`attempt-1`, geklemmt) und Doku (200/500 ms bei `maxAttempts=3`)
  stimmen; nur `IOException`/`UnauthorizedAccessException`
  retrywürdig.
- **Trait-Platzierung:** 19 E2E-Klassen + `E2ETestBase` markiert;
  24 Klassenmarkierungen + 3 Methodenmarkierungen in
  `VideoPlayer.Maui.Tests`; Traits der `#if DEBUG || DEV_API`-Dateien
  korrekt innerhalb des Blocks; `DevApiContractTests` scannt nur
  Produktivverzeichnisse — unberührt.
- **Doku:** `os-interface-tests.md` deckt sich mit Implementierung
  (Outcome-Menge, Schwellwert 2/10, Lane-Filter, Debug-Gating);
  `e2e-tests.md`-Angabe „74 Testfälle in 19 Klassen" verifiziert
  (74 `[Fact]`/`[Theory]` gezählt).
- **Repo-Konventionen:** `TestResults/` per `.gitignore`-Regel
  `[Tt]est[Rr]esult*/` abgedeckt (`git check-ignore` verifiziert);
  `2>$null` in den Skripten ist PowerShell-konform.
- **Build/Test:** `dotnet test VideoPlayer.Maui.Tests -c Debug
  --filter OsInterfaceConventionTests` → 2/2 grün (Build fehlerfrei).

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
