# Bestandsaufnahme: Flaky-Test-Härtung (Issue #97)

Bestandsaufnahme der Test-Infrastruktur beider Testprojekte (`VideoPlayer.Maui.Tests`, `VideoPlayer.E2E.Tests`), der GitHub-Workflows, der vorhandenen Skripte und der Hilfe-Dokumentation bezogen auf die Anforderung in `requirement.md` (Trait-Markierung `OsInterface`, CI-Lane-Trennung, Deadline-Staffelung, Cleanup-Härtung, Retry-/Quarantäne-Konvention, TRX-Statistik, Doku).

## Zusammenfassung

- **Keine Trait-/Category-Markierung vorhanden:** Im gesamten Testbestand beider Projekte existiert kein einziges `[Trait]`-Attribut. Beide Projekte verwenden `xunit.v3` 3.2.2, `--filter "Category…"` ist damit nutzbar.
- **Keine Lane-Trennung in CI:** `.github/workflows/pr-checks.yml` führt im Job `verify` genau einen `dotnet test`-Aufruf von `VideoPlayer.Maui.Tests` (Release) aus — ohne Filter, ohne TRX-Logger, ohne Artefakt-Upload. `VideoPlayer.E2E.Tests` wird in keinem Workflow gebaut oder ausgeführt.
- **Umgebungssensitive Tests laufen in der blockierenden Lane:** Reale Sockets/Listener (`UdpServerDiscoveryTests`, `DevApiHttpServerTests`, `DevApiServerTests_Metadata`), echter Prozessstart (`IosDeploymentResponseFileTests` startet `pwsh`) und mindestens 22 Testklassen mit realem Dateisystem unter `Path.GetTempPath()` (`LocalDatabaseTests`, `FileLoggerTests`, 17 Klassen der `SqliteDatabase`-Collection u. a.) stehen unmarkiert in `Maui.Tests`. Details: [OsInterface-Audit](inventory/os-interface-audit.md).
- **Teilhärtung im E2E-Support bereits vorhanden:** `AppInstance` nutzt `Kill(entireProcessTree: true)`, Best-Effort-`Dispose`, Prozess-Diagnose (`process.json` + `stdout.log`/`stderr.log` unter `.work/e2e-process-logs/`) und meldet native Exits in eine getrennte Aufräumphase (`reportShutdownFailure` → `AggregateException` in `E2ETestBase.DisposeAsync`). `DeleteProfileDirectory` hingegen schluckt Restfehler kommentarlos — ohne Retry/Backoff und ohne Protokollierung.
- **Deadline-Landschaft heterogen:** `AppInstance`-Startup 45 s (darin sequentiell `WaitForDevApiPortAsync` ≤45 s + `WaitForMainWindow` eigenes 30-s-Budget + `WaitForModelAsync` erneut ≤45 s), `DevApiClient.WaitForModelAsync` Standard 20 s, `UiDriver`-Standard 15 s, Shutdown `WaitForExit(10_000)` + Kill-Wartezeit 5 s; Testdateien übersteuern mit 30/45/60/120 s. Eine explizite Staffelungskonvention Host > Kind existiert nicht.
- **Keine Skripte/Doku zur Flake-Behandlung:** `scripts/` enthält nur Deployment-Skripte; `Retry-FlakyTests.ps1`/`Analyze-FlakyTestResults.ps1` fehlen. `docs/help/` kennt kein `stabilitaet/`-Verzeichnis; `e2e-tests.md` enthält keinen CI-Hinweis.
- **Flake-Nachweise vorhanden:** Unter `docs/features/task/38598ab90d844bb0bbc42a195643b6e3-saisonabhaengge-listen/` dokumentierte WinRT-Finalizer-/Stowed-Exception-Abstürze (`0xC0000005`, `0xC000027B`) beim Fixture-Shutdown sowie lokale Läufe mit 69/72 bzw. 70/72 bestandenen E2E-Tests.

Test-Ausgangszustand: Release-Lauf `VideoPlayer.Maui.Tests` (CI-Pendant): **1484/1484 bestanden, 0 fehlgeschlagen, 0 übersprungen**. Debug-Lauf mit Filter `FullyQualifiedName~DevApi` (deckt die nur unter `DEBUG || DEV_API` kompilierten Listener-Tests ab): **48/48 bestanden**. `VideoPlayer.E2E.Tests` kompiliert fehlerfrei; Ausführung nicht erfolgt (benötigt interaktive Desktop-Session und ~20–25 min; läuft auch in CI nicht). Nachweis: [Tests](inventory/tests.md).

## Details

- [CI-Workflows und Skripte](inventory/ci-workflows.md) — `pr-checks.yml`, `staging-merge.yml`, `main-merge.yml`, `.github/scripts/`, `.github/actions/`, `scripts/`
- [E2E-Infrastruktur](inventory/e2e-infrastruktur.md) — `AppFixture`, `E2ETestBase`, `AppInstance`, `UiDriver`, `DevApiClient`, `MockVideoServer`, `RepositoryRoot`, `MockPairingCrypto`, `DevApiModels` inkl. Deadline- und Cleanup-Befund
- [OsInterface-Audit](inventory/os-interface-audit.md) — Kandidatenliste beider Testprojekte mit Befund je Klasse
- [Doku und Flake-Nachweise](inventory/doku-und-flake-nachweise.md) — `docs/help/`-Struktur und dokumentierte Flake-Belege
- [Tests](inventory/tests.md) — Test-Ausgangszustand, Läufe, Lücken, Testklassen- und Hilfsmethoden-Übersicht
