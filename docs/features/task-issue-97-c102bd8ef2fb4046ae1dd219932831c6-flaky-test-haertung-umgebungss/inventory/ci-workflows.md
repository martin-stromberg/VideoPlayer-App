# CI-Workflows und Skripte

## `.github/workflows/pr-checks.yml`

Einziger Workflow, der Tests ausführt. Trigger: `pull_request` auf `main`/`staging`, plus `push` auf `task/issue-86-**` (tvOS-Verifikation während Projektphase). `permissions: contents: read`.

### Job `verify` (Zeilen 16–50)

- `runs-on: windows-latest`, kein `timeout-minutes`, kein `continue-on-error`.
- Schritte: `actions/checkout@v4` → `actions/setup-dotnet@v4` (`dotnet-version: '10.0.x'`) → `dotnet workload install maui --skip-manifest-update` → `dotnet restore` → `dotnet build VideoPlayer.Maui.Tests --no-restore -c Release` → `dotnet build VideoPlayer.Maui -c Release -f net10.0-windows10.0.19041.0` → **Test** → `dotnet format whitespace --verify-no-changes --no-restore` → Branch-Guard für `main`.
- Schritt `Test` (Zeile 39–40): `dotnet test VideoPlayer.Maui.Tests/VideoPlayer.Maui.Tests.csproj --no-build --configuration Release`
  - **Kein** `--filter` — umgebungssensitive Tests laufen in der blockierenden Lane mit.
  - **Kein** `--logger trx`, **kein** `--results-directory`, **kein** `actions/upload-artifact` — keine maschinenlesbaren Testergebnisse, keine Flake-Statistik möglich.
  - Wichtig: Release-Konfiguration — die `#if DEBUG || DEV_API`-gegaten `DevApi*Tests` kompilieren in dieser Lane gar nicht erst mit (siehe [OsInterface-Audit](os-interface-audit.md)).
- Kein `test-os-interface`-Job, kein Retry-/Re-Run-Schritt.

### Job `build-tvos` (Zeilen 55–91)

`macos-latest`, tvOS-Simulator-Build ohne Codesignierung; pinnt SDK-Band `10.0.100` per `global.json` und tvOS-Manifest `26.5.10318` per Rollback-Datei. Kein Testbezug.

## `.github/workflows/staging-merge.yml`

`push` auf `staging`: Semantic-Release (`ubuntu-latest`), `build-ios` (`xcode-27`, SDK-Pinning `10.0.100`, iOS-Manifest `27.1.10018`), optional `package-ios`/`package-tvos` (Gate per `IOS_SIGNING_ENABLED`/`TVOS_SIGNING_ENABLED`, `timeout-minutes: 45`), `draft-pr` nach `main`. Kein `dotnet test`, keine Test-Artefakte.

## `.github/workflows/main-merge.yml`

`push` auf `main`: analog `staging-merge.yml` (`release`, `build-ios` auf `macos-latest` mit iOS-Manifest `26.5.10318`, `package-ios`, `package-tvos`, `back-merge` PR nach `staging`). Kein `dotnet test`.

## `.github/scripts/`

- `Set-Version.ps1` — Versionsersetzung für Release-Builds (wird von `build-ios`/`build-tvos` aufgerufen).
- `semantic-release-with-retry.sh` — Retry-Wrapper um semantic-release; räumt bei Fehlschlag verwaiste Tags auf. Der einzige vorhandene Retry-Mechanismus im Repo; kein Testbezug.

## `.github/actions/`

- `package-ios/`, `package-tvos/` — Composite-Actions für signierte Store-Builds (nur Referenzen aus den Merge-Workflows).

## `scripts/`

Vorhanden: `Retag-Esqlite3-For-Tvos.py`, `iOS-Deployment.ps1`, `iOS-Deployment.ResponseFile.ps1`, `tvOS-Deployment.ps1` — ausschließlich Deployment.

**Nicht vorhanden:** `Retry-FlakyTests.ps1`, `Analyze-FlakyTestResults.ps1` oder vergleichbare Test-Auswerte-/Retry-Skripte (Anforderung: zu erzeugen).

## Befunde zur Anforderung

- Job `test-os-interface` (`windows-latest`, `continue-on-error: true`, großzügiges `timeout-minutes`): **fehlt**.
- `--filter "Category!=OsInterface"` im `Test`-Schritt: **fehlt**.
- TRX-Logger/`--results-directory` + `actions/upload-artifact` in beiden Lanes: **fehlt** (`upload-artifact` wird im gesamten Repo nirgends verwendet).
- `E2E.Tests` in CI: **nicht gebaut, nicht ausgeführt** — offene Entscheidung laut Anforderung (interaktive Desktop-Session auf gehosteten Runnern).
