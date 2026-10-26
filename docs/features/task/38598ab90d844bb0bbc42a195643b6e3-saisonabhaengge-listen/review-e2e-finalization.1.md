# Unabhängiges Review der E2E-Prozessdiagnose

## Status: Befunde vorhanden

Geprüft am: 2026-10-26. Umfang ausschließlich neuer Diff in `VideoPlayer.E2E.Tests/Support/AppInstance.cs` und `VideoPlayer.E2E.Tests/Tests/E2ETestBase.cs` gegenüber HEAD; keine Produktänderungen geprüft oder vorgenommen.

## Befunde

- [ ] **P2 – Ein erfolgloser oder überholter Killversuch kann einen nativen Abbruch als zulässigen Fixture-Kill maskieren.** In `AppInstance.Dispose(bool)` wird `forcedKill` bereits **vor** `process.Kill(...)` gesetzt. Der allgemeine Catch setzt denselben Wert ebenfalls, auch wenn der dortige Killversuch fehlschlägt. `WriteProcessDiagnosticsCore` akzeptiert danach jeden Nichtnull-Exitcode, sofern nur dieses Flag gesetzt ist. Stirbt die App beispielsweise nach dem abgelaufenen `WaitForExit(10_000)`, aber vor dem Killaufruf, selbst mit `0xC0000005`, kann der Kill wegen des bereits beendeten Prozesses fehlschlagen; die Klassifikation gibt trotzdem keinen Fixturefehler zurück. Ebenso wird ein fehlgeschlagener Kill als erzwungene Beendigung protokolliert. Die Ausnahme muss einen tatsächlich durch die Fixture verursachten Prozessabschluss belegen und darf einen eigenständigen nativen Fehlercode nicht aufgrund eines bloßen Killversuchs ausnehmen. Diagnose sollte Killversuch und nachgewiesenen Killabschluss unterscheiden. Für diesen Race-/Klassifikationsfall fehlt bislang ein Gegenbeispiel im Implementierernachweis.

## Unabhängiger Nachweis

Temporäre, nicht versionierte Probe: `.work/shutdown-review-probe/ReviewProbe.csproj`. Ein echter separater Kindprozess beendet sich selbst mit dem bekannten Exitcode `unchecked((int)0xC0000005)`; dies erzeugt keinen absichtlichen Speicherzugriffsfehler. Die vorhandene private Diagnosemethode wird für exakt denselben bereits natürlich beendeten Prozess einmal ohne und einmal mit gesetztem `forcedKill` aufgerufen. Ergebnis:

```text
Natural child exit=0xC0000005; forcedKill=False; guardReported=True
Natural child exit=0xC0000005; forcedKill=True; guardReported=False
```

Das belegt die fehlerhafte Klassifikation; die Aufrufreihenfolge im tatsächlichen Dispose-Code liefert den beschriebenen Racepfad zu dem Flag. Probe mit Windows-SDK-Zugriff erfolgreich ausgeführt, Exitcode 0. Nur ein Probe-Compilerhinweis wegen des transitiven xUnit-Einstiegspunktgenerators. Kein vollständiger E2E-Lauf wiederholt.

## Weitere Prüfung

- Die Fehlerqueue ist Instanzzustand der xUnit-Testklasse; reguläre Testinstanzen sammeln keine Fehler anderer Tests. `ConcurrentQueue` verträgt unterschiedliche Callbackthreads.
- Fehlerübergabe in der getrennten `IAsyncLifetime.DisposeAsync`-Phase erhält den primären Testfehler, statt ihn durch ein `using`-Dispose zu ersetzen. Die vorhandenen Gegenbeispielberichte sind hierzu konsistent.
- stdout/stderr werden unmittelbar nach dem Prozessstart asynchron gelesen. Prozess-ID, Profil, Exitcode und Shutdown-Zeitpunkte werden pro Kindprozess abgelegt; Diagnosefehler gelangen in die Cleanup-Fehleraggregation.
- Kindprozesse werden weiterhin nur über den konkret gestarteten `Process` und dessen Prozessbaum beendet. Keine neuen Eingriffe in fremde Systemdialoge, Windows- oder Firewalleinstellungen.
- Produktcode, Pakete und bestehende Benutzerfluss-Assertions bleiben unverändert. Ein vollständiger Suite-Lauf mit Guard sollte nach Korrektur des Befunds erfolgen.

Keine Fixes oder Commits durch den Reviewer.
