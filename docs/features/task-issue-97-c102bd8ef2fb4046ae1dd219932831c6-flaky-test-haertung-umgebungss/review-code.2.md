# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### Analyze-FlakyTestResults.ps1

- **Inkonsistente Fehlschlag-Definition (Fehlerbehandlung/Logik)** — In
  `Get-RunFlakes` (Z. 86–89) zählt ein Test nur als Hauptlauf-Fehlschlag,
  wenn `outcome -eq "Failed"`. Das widerspricht der in Iteration 2
  eingeführten Definition in `Retry-FlakyTests.ps1` (Z. 77), die bewusst
  `outcome != 'Passed'` re-runnt, weil `Error`/`Timeout` (Testhost- und
  Fixture-Abbrüche) „klassische Flakes" sind. Konsequenz: Ein Test, der
  im Hauptlauf mit `Error`/`Timeout`/`Aborted` endet und im Re-Run grün
  wird, wird zwar wiederholt, taucht aber nie in der Flake-Statistik und
  damit nie im Quarantäne-Issue auf — genau die Flake-Klasse, die die
  Lane einfangen soll.

  Empfehlung: Hauptlauf-Fehlschläge auf dieselbe Outcome-Menge wie den
  Re-Run ausweiten — z. B. `outcome != 'Passed'`, abzüglich
  nicht-ausführungsbezogener Stati (`NotExecuted`, `InProgress`,
  `Pending`, `Executing`), oder explizite Menge
  `Failed|Error|Timeout|Aborted|Inconclusive|NotRunnable|Disconnected`.

- **Doppelter Code** — (Befund aus Iteration 1, unverändert) Die
  TRX-Auflösung (TestDefinitions → `className.name`-FQN, Results →
  Outcome, `testName`-Regex-Fallback) ist weiterhin nahezu identisch in
  `Get-TestOutcomes` (Z. 43–71) und `Retry-FlakyTests.ps1` (Z. 59–86)
  dupliziert; ein Schema- oder Parsing-Fix müsste an zwei Stellen
  gepflegt werden — die obige Outcome-Inkonsistenz ist bereits ein
  Beleg für genau dieses Risiko.

  Empfehlung: Gemeinsames Helper-Skript (z. B. `scripts/lib/Trx.ps1`)
  per Dot-Sourcing einbinden oder zumindest einen gegenseitigen
  Kommentarverweis setzen.

### OsInterfaceConventionTests.cs

- **Testqualität (Scan-Lücke)** — `TestProjectSources()` (Z. 23–36)
  scannt `VideoPlayer.Maui.Tests` nicht-rekursiv (nur Top-Level-`*.cs`),
  `VideoPlayer.E2E.Tests` dagegen mit `SearchOption.AllDirectories`. Ein
  `[Trait("Flaky", …)]` in einem Unterverzeichnis des Maui-Testprojekts
  (heute `Stubs/`, künftig beliebige Ordner) entgeht der Prüfung —
  ebenso eine `*Tests.cs`-Datei in einem Unterverzeichnis von
  `VideoPlayer.E2E.Tests/Tests` der zweiten Regel
  (`Directory.GetFiles(testsDir, "*Tests.cs")`, Z. 92, nicht-rekursiv).

  Empfehlung: Beide Scans rekursiv (`SearchOption.AllDirectories`)
  ausführen und `bin`/`obj` ausschließen, damit die Konvention auch bei
  künftiger Ordnerstruktur lückenlos greift.

## Verifikation der Iteration-2-Nachbesserungen

Alle sechs gemeldeten Fixes sind verifiziert:

1. `Analyze-FlakyTestResults.ps1`: `$LASTEXITCODE`-Prüfung nach jedem
   `gh`-Aufruf (Z. 128, 139, 207, 219, 235, 256) mit `::warning`-
   Kontext; Erfolgsmeldung nur bei Exit 0.
2. `OsInterfaceConventionTests.cs`: `AttributeBlock`/`IsBlockLine`/
   `StripComments` ersetzt das Datei-`Contains()`; auskommentierte und
   fremde Attributblöcke zählen nicht mehr. Testlauf: 2/2 grün.
3. Kommentar zu `E2eTestClasses_CarryOsInterfaceTrait` korrigiert — die
   falsche `E2ETestBase.cs`-Ausnahme ist entfernt.
4. `AppInstance.StartAsync` (Z. 120–127): abgelaufenes Startup-Budget
   wirft `TimeoutException` mit Budget-Kontext; `WaitForDevApiPortAsync`
   und `WaitForMainWindow` teilen korrekt die Host-Deadline.
5. `Retry-FlakyTests.ps1` (Z. 77): `UnitTestResult[@outcome!='Passed']`
   deckt `Error`/`Timeout` ab; Begründung im Kommentar dokumentiert.
6. `TempDirectoryCleanup.cs` (Z. 26–28): Doku benennt die mit
   `maxAttempts=3` wirksamen Stufen 200/500 ms korrekt.

## Weitere geprüfte Punkte (ohne Befund)

- **Trait-Platzierung:** 20 klassenweite Traits in `VideoPlayer.E2E.Tests`
  (19 Testklassen + `E2ETestBase`), 24 klassenweite + 3 methodenweise
  Traits in `VideoPlayer.Maui.Tests` (`DiscoverAsync_*` korrekt
  methodenlokal, die `ResolveAnnouncedAddress_*`-Tests bleiben
  blockierend); `DevApiContractTests` bewusst unmarkiert (deterministischer
  Quelltext-Contract-Test).
- **Workflow-YAML:** parst fehlerfrei; `continue-on-error`,
  `timeout-minutes: 30`, Job-Permissions (`contents`/`actions: read`,
  `issues: write`) und `if: always()`-Kaskade Hauptlauf → Re-Run →
  Upload → Analyse konsistent; `TestResults/` durch `.gitignore`-Regel
  `[Tt]est[Rr]esult*` abgedeckt.
- **Deadline-Logik:** gemeinsames `deadline`-Budget über devapi-Poll,
  Fenster-Attach und erstes Modell; `remaining <= 0` explizit abgefangen.
- **Cleanup:** `TempDirectoryCleanup.DeleteWithRetry` mit Log-Spur und
  korrekter Backoff-Indexierung (`attempt-1`, geklemmt an
  `Backoff.Length-1`); `TryDelete` mit optionalem Log-Callback an beiden
  Aufrufstellen verdrahtet.
- **Build/Tests:** `dotnet build` beider Testprojekte 0 Fehler;
  `OsInterfaceConventionTests` 2/2 grün (Debug).
- **Dokumentation:** `os-interface-tests.md`, `e2e-tests.md` und
  `index.md` konsistent zu Code und Workflow (Filter, Schwellwert 2/10,
  Trait-Konvention, Debug-Gating).

## Geprüfte Dateien

Liste aller geprüften Dateien:

- `.github/workflows/pr-checks.yml`
- `VideoPlayer.E2E.Tests/Support/AppInstance.cs`
- `VideoPlayer.E2E.Tests/Support/TempDirectoryCleanup.cs` (neu)
- `VideoPlayer.E2E.Tests/Tests/E2ETestBase.cs` und alle 19 E2E-Testklassen
- `VideoPlayer.Maui.Tests/OsInterfaceConventionTests.cs` (neu)
- `VideoPlayer.Maui.Tests/UdpServerDiscoveryTests.cs` sowie alle 24
  klassenmarkierten Testdateien
- `scripts/Retry-FlakyTests.ps1` (neu)
- `scripts/Analyze-FlakyTestResults.ps1` (neu)
- `docs/help/stabilitaet/os-interface-tests.md` (neu),
  `docs/help/e2e-tests.md`, `docs/help/index.md`
- Lifecycle-Artefakte unter `docs/features/task-issue-97-…/`
  (nur Konsistenzsichtung)
