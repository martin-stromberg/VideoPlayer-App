# CI-Landschaft — Bestandsaufnahme (iOS-Pfad als tvOS-Vorlage)

Kondensierte Sicht auf die für diesen Schritt relevanten CI-Fakten. Vollständige
Vorlage: `docs/projects/task/issue-86-5dcdc7d5ef27411897f0ec9129c87614-apple-tv-app-tvos-lean-back-pl/inventory/ci.md`.

## `.github/workflows/pr-checks.yml` (Ziel des neuen Jobs)

- Trigger: `pull_request` nach `main`, `staging`.
- Ein einziger Job `verify` auf `windows-latest`: `actions/setup-dotnet@v4` (`10.0.x`)
  → `dotnet workload install maui --skip-manifest-update` → `dotnet restore`
  (gesamte Solution!) → `dotnet build VideoPlayer.Maui.Tests` → `dotnet build
  VideoPlayer.Maui -f net10.0-windows10.0.19041.0` → `dotnet test
  VideoPlayer.Maui.Tests --no-build` → `dotnet format whitespace --verify-no-changes`
  → Branch-Guard.
- **Kritisch für diesen Schritt:** `dotnet restore` läuft auf Windows über die gesamte
  `VideoPlayer.App.sln`. Ein tvOS-Projekt mit festem `net10.0-tvos`-TFM würde dort mit
  `NETSDK1147` (Workload `tvos` unter Windows nicht installierbar) brechen — ebenso
  jeder lokale `dotnet restore`/`dotnet build`/`dotnet test` auf der Solution.
  → Das tvOS-Projekt braucht ein konditionales TFM (siehe `projects.md`).

## iOS-Build-Jobs als Muster (`staging-merge.yml`, `main-merge.yml`)

- `build-ios` in `main-merge.yml` läuft auf `macos-latest` (Release-Xcode 26.x) mit:
  1. `global.json`-Pin des SDK-Bands: `{"sdk":{"version":"10.0.100","rollForward":"latestPatch","allowPrerelease":false}}`
  2. Workload-Rollback per `printf '{"microsoft.net.sdk.ios": "26.5.10318"}' > "$RUNNER_TEMP/workload-rollback.json"`
     + `dotnet workload update --from-rollback-file` + `dotnet workload install maui --skip-manifest-update`
  3. `dotnet build VideoPlayer.Maui -f net10.0-ios -r iossimulator-arm64 -p:EnableCodeSigning=false`
     (Codesigning explizit aus — Runner ohne Zertifikate; iOS-SDK 27 meldet sonst
     „No valid iOS code signing keys").
- `staging-merge.yml` verwendet stattdessen das Runner-Label `xcode-27` mit
  `{"microsoft.net.sdk.ios": "27.1.10018"}` (Xcode-27.1-Paket).
- `package-ios`-Jobs (signiert, TestFlight) sind hinter `vars.IOS_SIGNING_ENABLED`
  + `released == 'true'` gegatet — für diesen Schritt **nicht** relevant.

## Verifizierte NuGet-Fakten für das tvOS-Pinning (gegen nuget.org geprüft)

- Workload-Manifest `microsoft.net.sdk.tvos.manifest-10.0.100` existiert u. a. in den
  Versionen `26.5.10318`, `27.0.10722`, `27.0.10742`.
- Pack `Microsoft.tvOS.Sdk.net10.0_26.5` in Version `26.5.10318` existiert
  (deckungsgleich zur iOS-Pinning-Version des `main-merge.yml`-Pfads).
- Eine tvOS-Band-`27.1`-Pendants zum iOS-Pinning `27.1.10018` existiert **nicht**
  (`Microsoft.tvOS.Sdk.net10.0_27.1` → 404). Der `xcode-27`-Runner käme damit nur über
  das `27.0`-Manifest in Frage; für diesen Schritt wird `macos-latest` + `26.5.10318`
  gewählt (Release-Xcode, analog `main-merge.yml`).
- Lokale Workload-Liste dieses Rechners: `android`, `ios` 26.5.10318, `maccatalyst`,
  `maui-windows` — **kein tvOS**, unter Windows auch nicht installierbar.

## Weitere Referenzen

- `.github/scripts/Set-Version.ps1` — Versionsstempel (für Gerüst-Build nicht nötig).
- `.github/actions/package-ios/action.yml` — Muster für spätere Signatur-Composite-Action
  (Projektschritt 11, hier ausgenommen).
- `scripts/iOS-Deployment.ps1` — lokales iOS-Deployment (tvOS-Pendant ebenfalls
  Projektschritt 11).
