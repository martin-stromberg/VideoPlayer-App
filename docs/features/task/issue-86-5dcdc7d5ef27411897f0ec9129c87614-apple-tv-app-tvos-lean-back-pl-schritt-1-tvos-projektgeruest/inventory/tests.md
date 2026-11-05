# Tests — Ausgangszustand vor der Umsetzung

## Rahmendaten

- **Zeitpunkt (mit Zeitzone):** 2026-11-05, ~17:10–17:15 UTC (18:10–18:15 +0100)
- **Branch:** `task/issue-86-5dcdc7d5ef27411897f0ec9129c87614-apple-tv-app-tvos-lean-back-pl-schritt-1-tvos-projektgeruest`
- **Commit:** `b4acc4f` — „chore: Schritt 1 in Arbeit gesetzt"
- **Uncommittete Änderungen im getesteten Stand:** nur `docs/` (Feature-Artefakte dieser
  Bearbeitung, Projekt-`status.html`), `.work/` (untracked) — keine Code-Änderungen
- **Testumgebung:** Windows, .NET SDK 10.0.403; Workloads `android`, `ios` 26.5.10318,
  `maccatalyst`, `maui-windows` — **keine tvOS-Workload**
- **Ermittelte Testsuiten (Quelle: `pr-checks.yml`, csproj-Dateien):**
  - `VideoPlayer.Maui.Tests` — xunit.v3, `net10.0`, Unit-/Contract-Tests der
    plattformneutralen Projekte
  - `VideoPlayer.E2E.Tests` — xunit.v3, `net10.0-windows`, FlaUI-UI-Tests (Windows-only)

## Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|---------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| Unit/Contract-Baseline | `dotnet test VideoPlayer.Maui.Tests/VideoPlayer.Maui.Tests.csproj --nologo` | Repo-Root | 0 | 1440 | 0 | 0 | [baseline-maui-tests.log](test-results/baseline-maui-tests.log) |
| E2E-Baseline (Referenzlauf der Projekt-Bestandsaufnahme, selber Commit-Stand der Codebasis) | `dotnet test VideoPlayer.E2E.Tests/VideoPlayer.E2E.Tests.csproj --nologo` | Repo-Root | 1 | 71 | 3 | 0 | [e2e-tests-dotnet-test.log](../../../../projects/task/issue-86-5dcdc7d5ef27411897f0ec9129c87614-apple-tv-app-tvos-lean-back-pl/inventory/test-results/e2e-tests-dotnet-test.log) |

Der E2E-Referenzlauf stammt aus der Projekt-Bestandsaufnahme vom selben Tag
(Commit `f16d68a`, danach nur Dokumentations-Commits) und wird hier als dokumentierter
Ausgangslauf übernommen — die getestete Codebasis ist identisch (keine Code-Commits seit
`f16d68a`).

## Nachgewiesene bestehende Testfehler (preexisting, aus dem E2E-Referenzlauf)

| Test-ID | Suite / Dateipfad | Fehlerbild | Lauf und Nachweis |
|---------|-------------------|------------|-------------------|
| `PlaylistPlaybackTests.StreamFailure_AdvancesToNextPlaylistEntry` | `VideoPlayer.E2E.Tests/Tests/PlaylistPlaybackTests.cs` | `AggregateException`: App-Prozess beendet, Exit `-1073741189` (`0xC000027B`) | E2E-Referenzlauf, [Log](../../../../projects/task/issue-86-5dcdc7d5ef27411897f0ec9129c87614-apple-tv-app-tvos-lean-back-pl/inventory/test-results/e2e-tests-dotnet-test.log) |
| `SourceSearchTests.SubmitViaEnter_FiltersFromFirstPage` | `VideoPlayer.E2E.Tests/Tests/SourceSearchTests.cs` | `TimeoutException` in `DevApiClient.WaitForModelAsync` | E2E-Referenzlauf, [Log](../../../../projects/task/issue-86-5dcdc7d5ef27411897f0ec9129c87614-apple-tv-app-tvos-lean-back-pl/inventory/test-results/e2e-tests-dotnet-test.log) |
| `SourceSearchTests.SubmittedText_RemainsVisibleAfterReturn` | `VideoPlayer.E2E.Tests/Tests/SourceSearchTests.cs` | `TimeoutException` in `DevApiClient.WaitForModelAsync` | E2E-Referenzlauf, [Log](../../../../projects/task/issue-86-5dcdc7d5ef27411897f0ec9129c87614-apple-tv-app-tvos-lean-back-pl/inventory/test-results/e2e-tests-dotnet-test.log) |

## Testlücken und Ausführungsprobleme

- **Keine tvOS-Testinfrastruktur:** kein `net*‑tvos`-TFM, keine tvOS-Workload auf dem
  Entwicklungsrechner, kein UI-Test-Framework für tvOS. Für diesen Schritt ist der
  macOS-CI-Kompilier-Job der Funktionsnachweis.
- `VideoPlayer.E2E.Tests` ist Windows-only (FlaUI/MAUI-Windows-Build) — für die
  tvOS-App nicht wiederverwendbar.
