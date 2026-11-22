# Übersetzte Anforderungsbeschreibung – Issue #97: Flaky-Test-Härtung

## Fachliche Zusammenfassung

Die Test-Infrastruktur des Projekts (`VideoPlayer.Maui.Tests`, `VideoPlayer.E2E.Tests`, Workflow `.github/workflows/pr-checks.yml`) wird nach der Blaupause aus `martin-stromberg/Softwareschmiede` (Issue #428 / PR #432) gehärtet. Umgebungssensitive Tests (echte Prozesse, UI-Automation, Netzwerk-Sockets/Listener, Dateisystem) werden per `[Trait("Category", "OsInterface")]` markiert und in einer eigenen, nicht blockierenden CI-Lane (`test-os-interface`, `continue-on-error: true`) ausgeführt; die reguläre Lane filtert sie per `--filter "Category!=OsInterface"` heraus. Ergänzend werden Poll-Deadlines konsistent gestaffelt, das Cleanup (`DeleteProfileDirectory`) mit Retry/Backoff und Protokollierung versehen, eine `Flaky`-Retry-/Quarantäne-Konvention eingeführt und eine TRX-basierte Flake-Statistik aufgebaut.

## Fachlicher Scope

- **Verarbeitungseinheit:** Ein CI-Testlauf der beiden xUnit-v3-Testprojekte `VideoPlayer.Maui.Tests` (Unit-/Integrations-Tests) und `VideoPlayer.E2E.Tests` (FlaUI.UIA3-End-to-End-Tests). Beobachtet/verarbeitet wird die Menge der Testklassen hinsichtlich ihrer Umgebungssensitivität sowie das Verhalten der Test-Infrastruktur bei Timeouts und Cleanup.
- **Auslösende Ereignisse:** Jeder Pull Request auf `main`/`staging` triggert `pr-checks.yml`; zusätzlich lokale `dotnet test`-Ausführungen durch Entwickler. Die Härtung selbst ist ein einmaliger Umbau von Workflow, Test-Markierungen und Support-Code — kein Laufzeit-Feature der App.
- **Erwartete Wirkung:** Die blockierende `verify`-Lane läuft deterministisch ohne umgebungssensitive Tests; Flakes schlagen als `test-os-interface`-Job sichtbar, aber nicht PR-blockierend fehl (best effort). TRX-Artefakte und ein Auswerteschritt machen Flake-Raten messbar; ein definierter Schwellwert erzeugt Quarantäne-Issues. Nachweis: mindestens 3 komplette grüne CI-Durchläufe.
- **Umgang mit fehlenden Objekten:**
  - `OsInterface`-Trait-Markierungen: **erzeugen** (existieren derzeit im gesamten Testbestand nicht).
  - Job `test-os-interface` in `pr-checks.yml`: **erzeugen**.
  - TRX-Logger/`--results-directory` und Artefakt-Upload: **erzeugen** (Schritte in beiden Lanes ergänzen).
  - `scripts/Retry-FlakyTests.ps1` und `scripts/Analyze-FlakyTestResults.ps1`: **erzeugen** (analog Blaupause; im Repo existiert noch kein vergleichbares Skript).
  - `TempDirectoryCleanup`-/Retry-Helfer für `DeleteProfileDirectory`: **erzeugen bzw. bestehende Methode erweitern** — Restfehler protokollieren statt kommentarlos zu schlucken.
  - Hilfsseite unter `docs/help/` (Vorlage `docs/help/stabilitaet/os-interface-tests.md`): **erzeugen**.
  - Vorhandene Härtung (`Kill(entireProcessTree: true)`, Best-Effort-`Dispose` in `AppInstance`/`AppFixture`, Diagnose-Ausgabe Phase/State/ExitCode, `AggregateException`-Maskierungsregel in `E2ETestBase`): **wiederverwenden** und darauf aufbauen.
- **Scope-Ausschlüsse:**
  - Kein Blanket-Retry der Gesamtsuite — nur ein automatisierter Re-Run markierter Fehlschläge in der Best-Effort-Lane.
  - Keine Änderung an App-Laufzeitcode; Scope ist ausschließlich Test-Infrastruktur, CI-Workflow, Skripte und Doku.
  - Aufnahme von `VideoPlayer.E2E.Tests` in CI ist **nicht** automatisch Teil des Scope (FlaUI/UIA3 benötigt interaktive Desktop-Session; gehostete `windows-latest`-Runner laufen nicht-interaktiv) — Entscheidung offen, ggf. Lane-Trennung nur für `Maui.Tests`.
  - Kein Anspruch auf vollständige Deterministik echter UI-/Prozess-Tests; Zielbild ist „selten, lokalisiert, selbstdiagnostizierend" (Grundsatz der Blaupause).
  - ConPTY/`PseudoConsole`- und NTFS-Junction-/Reparse-Point-Heuristiken der Vorlage entfallen — kommen in diesem Repo nicht vor.

## Betroffene Klassen und Komponenten

- **Workflow:** `.github/workflows/pr-checks.yml` — Schritt `Test` im Job `verify` um `--filter "Category!=OsInterface"` plus `--logger trx`/`--results-directory` und Artefakt-Upload erweitern; neuer Job `test-os-interface` (`windows-latest`, `continue-on-error: true`, großzügiges `timeout-minutes`) mit `--filter "Category=OsInterface"`.
- **Test-Markierungen (`VideoPlayer.Maui.Tests`, audit-pflichtig):** Kandidaten für `[Trait("Category", "OsInterface")]` — `UdpServerDiscoveryTests` (UDP-Sockets), `DevApiHttpServerTests`/`DevApi*Tests` (HTTP-Listener auf `127.0.0.1`), `LocalDatabaseTests`, `FileLoggerTests`, Temp-Verzeichnis-Szenarien; Markierung bevorzugt auf Klassenebene, wo die gesamte Fixture umgebungssensitiv ist.
- **Test-Markierungen (`VideoPlayer.E2E.Tests`):** Gesamte Suite umgebungssensitiv (`Process.Start`, FlaUI/UI-Automation via `UiDriver`, `AppInstance`, `AppFixture`, `MockVideoServer` mit `TcpListener`, isolierte App-Profile via `VIDEOPLAYER_PROFILE`).
- **Support-Code `VideoPlayer.E2E.Tests/Support`:** `AppInstance` (Startup-Timeout Standard 45 s, `WaitForExit(10_000)`, `DeleteProfileDirectory` mit Retry/Backoff + Protokollierung), `UiDriver` und `DevApiClient` (Poll-Budgets anpassen/staffeln), `AppFixture`/`E2ETestBase` (Cleanup-Regel: Cleanup-Fehler darf Assert nie als `AggregateException` überdecken).
- **Neue Skripte:** `scripts/Retry-FlakyTests.ps1` (genau ein automatisierter Re-Run markierter Fehlschläge in der Best-Effort-Lane), `scripts/Analyze-FlakyTestResults.ps1` (Fehlschlag-then-Rerun-grün-Rate pro Test aus TRX).
- **Tests:** Neuer Meta-/Contract-Test, der die Regel „`Flaky`-Markierung nur in Kombination mit `OsInterface`" absichert; Platz (in `Maui.Tests` vs. eigener Contract-Test) ist offen.
- **Doku:** Neue Seite unter `docs/help/` (Lane-Regeln, Trait-Konvention, Retry-Regel inkl. `gh run rerun --job <id>` nur für den OS-Job, Quarantäne-Prozess); bestehende `docs/help/e2e-tests.md` ggf. um CI-Hinweis ergänzen.

## Implementierungsansatz

- **Audit zuerst:** Alle Test-Dateien beider Projekte gegen die repo-angepasste Heuristik prüfen (`Process.Start`, FlaUI/UIA, Clipboard, echte Netzwerk-Endpunkte, echte Dateisystem-Szenarien, eigene Poll-Zeitbudgets); Ergebnisliste der zu markierenden Klassen im Umsetzungs-PR dokumentieren.
- **xUnit v3 nutzen:** Beide Testprojekte verwenden `xunit.v3` 3.2.2 — `[Trait]`-Attribut auf Klassen-/Methodenebene, Filterung per `dotnet test --filter "Category[!|=]=OsInterface"`.
- **Deadline-Staffelung:** Host-Poll-Deadlines > Kind-/Phasen-Budget sicherstellen (Vorlagen-Fehler: Host 30 s vs. Kind 90 s). Konkret prüfen: `AppInstance`-Startup-Timeout (45 s), `UiDriver`-/`DevApiClient`-Poll-Budgets, `WaitForExit(10_000)` beim Shutdown; Diagnose-Ausgabe beibehalten.
- **Cleanup-Erweiterung statt Neubau:** Auf `Kill(entireProcessTree: true)` und Best-Effort-`Dispose` aufsetzen; `DeleteProfileDirectory` um Retry/Backoff + Logging ergänzen (Blaupause: `TempDirectoryCleanup`-Helfer).
- **Retry-Konvention:** `Flaky` nur zusammen mit `OsInterface`, per Meta-Test abgesichert; manuelle Wiederholung nur per `gh run rerun --job <id>` des OS-Jobs.
- **Abhängigkeiten:** Keine neuen App-Abhängigkeiten; GitHub-Actions-Standard-Features (`continue-on-error`, `actions/upload-artifact`). Blaupause-Referenz: `martin-stromberg/Softwareschmiede` PR #432.

## Konfiguration

- Filterung über `dotnet test`-CLI-Parameter (xUnit-v3-Traits) — keine Laufzeitkonfiguration der App.
- Quarantäne-Schwellwert als konfigurierbarer Parameter der Auswertung denkbar; konkreter Wert offen (Vorschlag: ≥ 2 Flake-Fehlschläge eines Tests innerhalb von 10 Läufen), im Umsetzungs-PR festzulegen.

## Offene Fragen

1. **E2E-Suite in CI aufnehmen?** Optionen: (a) als `OsInterface`-Job in `pr-checks.yml` — Risiko: keine interaktive Desktop-Session auf gehosteten Runnern (Machbarkeit ggf. per Spike); (b) bewusst lokal belassen, Lane-Trennung nur für `Maui.Tests`; (c) Self-hosted-Runner. Entscheidung vor Umsetzung erforderlich.
2. **Schwellwert für Quarantäne:** Fehlschlag-then-Rerun-grün-Rate, ab der automatisch ein Quarantäne-Issue erzeugt wird — im Umsetzungs-PR festlegen.
3. **Platz des Konventions-Meta-Tests:** In `Maui.Tests` (blockierende Lane) oder als eigener Contract-Test?
4. **Dokumentationsort:** `docs/help/` liegt nahe; ob eine `stabilitaet/`-Unterstruktur wie in der Blaupause gewünscht ist, ist offen.
