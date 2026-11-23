# OsInterface-Tests und Flake-Quarantäne

Umgebungssensitive Tests — echte Prozesse, UI-Automation,
Netzwerk-Sockets/-Listener, echte Dateisystem-Szenarien — sind per
`[Trait("Category", "OsInterface")]` markiert und laufen in einer
eigenen, nicht blockierenden CI-Lane. Zielbild: Flakes sind selten,
lokalisiert und selbstdiagnostizierend (Issue #97).

## CI-Lanes

`pr-checks.yml` führt pro Pull Request auf `main`/`staging` zwei
Test-Lanes aus:

| Job | Filter | Wirkung |
|-----|--------|---------|
| `verify` | `--filter "Category!=OsInterface"` | blockierend; nur deterministische Tests |
| `test-os-interface` | `--filter "Category=OsInterface"` | best effort (`continue-on-error: true`, `timeout-minutes: 30`); Fehlschläge blockieren den PR nicht |

Beide Lanes bauen und testen `VideoPlayer.Maui.Tests` in Release und
laden ihre TRX-Ergebnisse als Artefakte hoch (`verify-trx` bzw.
`os-interface-trx`). In `test-os-interface` folgen auf den Hauptlauf
(`TestResults/os-main`):

1. **Re-Run** — `scripts/Retry-FlakyTests.ps1` parst die
   Hauptlauf-TRX und wiederholt genau die fehlgeschlagenen Tests
   **einmal** (`TestResults/os-rerun`). Als fehlgeschlagen zählt die
   explizite Outcome-Menge `Failed`, `Error`, `Timeout` (zentral in
   `scripts/FlakyTestTrx.ps1`, mit der Auswertung geteilt) —
   `NotExecuted` o. ä. löst keinen Re-Run aus. Kein Re-Run bei 0
   Fehlschlägen; kein Blanket-Retry der Gesamtsuite.
2. **Upload** — `os-interface-trx` enthält beide
   Ergebnisverzeichnisse und ist die Quelle der Flake-Historie.
3. **Auswertung** — `scripts/Analyze-FlakyTestResults.ps1` lädt die
   Artefakte der letzten 10 Läufe und berechnet pro Test die Rate
   „Hauptlauf fehlgeschlagen, Re-Run bestanden" (Step-Summary +
   `::warning`). Steht die Artefakt-Historie für weniger als 10
   Läufe zur Verfügung, werden nur die vorhandenen ausgewertet.

## Trait-Konvention

- `Category=OsInterface` auf **Klassenebene**, wo die gesamte
  Fixture umgebungssensitiv ist; auf **Methodenebene**, wo nur
  einzelne Tests die Umgebung brauchen (Beispiel: nur die drei
  `DiscoverAsync_*`-Tests in `UdpServerDiscoveryTests` tragen das
  Trait, die `ResolveAnnouncedAddress_*`-Tests bleiben blockierend).
- `[Trait("Flaky", "true")]` kennzeichnet bekannte Flakes und darf
  **nur** zusammen mit `Category=OsInterface` vergeben werden —
  sonst läge ein bekannter Flake in der blockierenden Lane.
- Der Meta-Test `OsInterfaceConventionTests` (in
  `VideoPlayer.Maui.Tests`, selbst ohne Trait → blockierende Lane)
  erzwingt beides per Quelltext-Scan: `Flaky` nur mit
  `OsInterface`, und jede Testklasse unter
  `VideoPlayer.E2E.Tests/Tests/` trägt `OsInterface`.

## Re-Run-Regel

Ein automatisierter Re-Run erfolgt ausschließlich innerhalb der
Best-Effort-Lane (Schritt „Re-run failed tests once"). Manuelle
Wiederholungen laufen nur über den OS-Job, nie über die gesamte
Suite:

```bash
gh run rerun --job <job-id>
```

## Quarantäne-Prozess

Flake-Definition: ein Test schlägt im Hauptlauf fehl (Outcome
`Failed`, `Error` oder `Timeout`) und ist im Einmal-Re-Run grün.
Schwellwert: **≥ 2 Flakes desselben Tests
innerhalb der letzten 10 ausgewerteten Läufe** → `::warning` +
Eintrag in der Step-Summary und automatisch ein dedupliziertes
Issue mit Label `flaky-test` (kein Duplikat, wenn bereits ein
offenes Issue mit dem Testnamen im Titel existiert). Quarantäne ist
ein Issue-Prozess — Tests werden nicht per `Skip` ausgeblendet.

## Hinweise und Grenzen

- **Debug-gegatete Tests:** `DevApiHttpServerTests`,
  `DevApiServerTests_Metadata` und `DevApiMetadataTests` stehen
  unter `#if DEBUG || DEV_API` und werden in den Release-Lanes gar
  nicht kompiliert. Lokal abgedeckt sind sie per
  `dotnet test VideoPlayer.Maui.Tests -c Debug --filter "Category=OsInterface&FullyQualifiedName~DevApi"`.
- **E2E-Suite:** `VideoPlayer.E2E.Tests` läuft nicht in der CI
  (FlaUI/UIA3 braucht eine interaktive Desktop-Session) — die
  Klassenmarkierung dient lokalen gefilterten Läufen, siehe
  [Windows-End-to-End-Tests](../e2e-tests.md).
- **Deadline-Staffelung (E2E):** `AppInstance.StartAsync` teilt ein
  gemeinsames Startup-Budget (Standard 45 s) auf devapi-Poll,
  Fenster-Attach und erstes Modell — Kind-Wartezeiten bekommen das
  Restbudget statt eigener Vollbudgets, damit die Host-Deadline nicht
  strukturell überschritten wird. `DevApiClient` 20 s und `UiDriver`
  15 s bleiben darunter gestaffelt; höhere Testdatei-Budgets (bis
  120 s) sind bewusste testlokale Übersteuerungen. Die
  Shutdown-Wartezeiten (`CloseMainWindow` + `WaitForExit(10_000)`,
  danach Prozessbaum-Kill + 5 s Exit-Wartezeit, 5 s
  stdout/stderr-Drain) sind sequentielle Fallbacks ohne
  Host/Kind-Verschachtelung und bleiben bewusst unverändert.
- **Profil-Cleanup (E2E):** `AppInstance.DeleteProfileDirectory`
  löscht die Profil-Kandidatenverzeichnisse je Kandidat über
  `TempDirectoryCleanup.DeleteWithRetry` — bis zu drei Versuche mit
  gestaffeltem Backoff (200/500/1000 ms), retrywürdig sind nur
  `IOException`/`UnauthorizedAccessException` (gesperrte Handles,
  z. B. noch offene SQLite-Dateien). Jeder Versuch und ein
  verbleibender Restfehler werden protokolliert — das Cleanup bleibt
  best-effort, aber nicht mehr kommentarlos.
- **Lokale Flake-Auswertung:** `Analyze-FlakyTestResults.ps1` läuft
  ohne `GH_TOKEN` rein lokal über `-ArtifactDirectory` (ein
  Unterordner je Lauf mit `os-main`-/`os-rerun`-TRX) — dann ohne
  Artefakt-Download und ohne Quarantäne-Issues. Fenster und
  Schwellwert sind über `-RunLimit` (10) bzw. `-FlakeThreshold` (2)
  einstellbar.

## Lokale gefilterte Läufe

```bash
# Blockierende Teilmenge (wie verify)
dotnet test VideoPlayer.Maui.Tests -c Release --filter "Category!=OsInterface"

# OsInterface-Teilmenge (wie test-os-interface)
dotnet test VideoPlayer.Maui.Tests -c Release --filter "Category=OsInterface"

# Debug-gegatete DevApi-Tests
dotnet test VideoPlayer.Maui.Tests -c Debug --filter "Category=OsInterface&FullyQualifiedName~DevApi"

# E2E-Suite (interaktive Session, ~20–25 min)
dotnet test VideoPlayer.E2E.Tests -c Debug --filter "Category=OsInterface"
```
