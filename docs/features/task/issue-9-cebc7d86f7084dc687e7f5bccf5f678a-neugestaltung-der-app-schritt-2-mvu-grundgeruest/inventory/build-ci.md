# Build-, Projekt- und CI-Konfiguration sowie Umgebung

## Projekte und Solution

`VideoPlayer.App.sln` enthält vier Projekte:

| Projekt | Datei | Target Framework(s) | Rolle |
|---------|-------|---------------------|-------|
| `VideoWebPlayer.Client` | `VideoWebPlayer.Client/VideoWebPlayer.Client.csproj` | `net10.0` | Alt-API-Client + DTOs (wird später entfernt) |
| `VideoWebPlayer.Maui` | `VideoWebPlayer.Maui/VideoWebPlayer.Maui.csproj` | `net10.0-android`; `net10.0-ios`, `net10.0-maccatalyst` (nicht Linux); `net10.0-windows10.0.19041.0` (nur Windows) | Alt-App (MVVM) |
| `VideoWebPlayer.Maui.Tests` | `VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj` | `net10.0` | xUnit-Tests; referenziert `VideoWebPlayer.Client` + `VideoPlayer.ApiClient` |
| `VideoPlayer.ApiClient` | `VideoPlayer.ApiClient/VideoPlayer.ApiClient.csproj` | `net10.0` | Neuer plattformneutraler API-Client |

### `VideoWebPlayer.Maui.csproj` — relevante Einstellungen (Vorlage)

- `UseMaui`, `SingleProject`, `ImplicitUsings`, `Nullable`; `MauiXamlInflator=SourceGen`.
- `OutputType=Exe`; `ApplicationId=com.companyname.videowebplayer.maui`;
  `ApplicationDisplayVersion 1.0`, `ApplicationVersion 1`; `WindowsPackageType=None`.
- `SupportedOSPlatformVersion`: iOS/MacCatalyst 15.0, Android 21.0, Windows 10.0.17763.0.
- iOS: `CodesignKey`/`CodesignProvision` gesetzt (WildCard Development).
- Pakete u. a.: `Microsoft.Maui.Controls`/`Essentials` 10.0.90,
  `Microsoft.Extensions.Logging.Debug` 10.0.10, `CommunityToolkit.Maui` 15.0.0,
  `CommunityToolkit.Maui.MediaElement` 10.0.0, `SkiaSharp`, `sqlite-net-pcl`,
  `SQLitePCLRaw.*`, `Zeroconf`, `SignalR.Client`.
- `ProjectReference` auf `VideoWebPlayer.Client`.

### `VideoWebPlayer.Maui.Tests.csproj`

- `net10.0`, `IsPackable=false`; `xunit.v3` 3.2.2, `xunit.runner.visualstudio` 3.1.5,
  `Microsoft.NET.Test.Sdk` 18.8.1, `coverlet.collector` 10.0.1.
- `ProjectReference`: `VideoPlayer.ApiClient`, `VideoWebPlayer.Client`.

### Weitere Konfiguration

- `NuGet.config`: nur `https://api.nuget.org/v3/index.json`.
- `.github/scripts/Set-Version.ps1`: schreibt `ApplicationDisplayVersion`/`ApplicationVersion`
  fest in `VideoWebPlayer.Maui/VideoWebPlayer.Maui.csproj` (hardcodierter Pfad).
- `.gitignore`: Standard-VS/dotnet-Ignore; **uncommittete Änderung** (`issue.md` ergänzt) —
  gehört nicht zu diesem Schritt und wird nicht committet. `*.log` ist ignoriert →
  Nachweis-Logs mit `git add -f` stagen.

## CI-Workflows (`.github/workflows/`)

| Workflow | Trigger | Relevante Schritte |
|----------|---------|--------------------|
| `pr-checks.yml` („PR Checks") | PR → `main`, `staging` | `windows-latest`, .NET 10.0.x, `dotnet workload install maui`; `dotnet restore`; Build Client + Tests + MAUI `net10.0-android` + `net10.0-windows10.0.19041.0` (Release, jeweils `VideoWebPlayer.Maui`); `dotnet test`; `dotnet format whitespace --verify-no-changes`; PRs nach `main` nur von `staging` |
| `staging-merge.yml` („Staging Prerelease") | Push `staging` | semantic-release → Publish Android (APK) + Windows (MSIX) auf `windows-latest`, iOS-Build `net10.0-ios`/`iossimulator-arm64` auf `macos-latest` (jeweils `VideoWebPlayer.Maui`); Artifact-Upload; Draft-PR `staging` → `main` |
| `main-merge.yml` („Main Release") | Push `main` | wie staging, Final-Release + Back-Merge-PR |

## Lokale Umgebung (verifiziert in diesem Lauf)

- Windows-Rechner, .NET SDK `10.0.401`; Workloads `android`, `ios`, `maccatalyst`,
  `maui-windows` installiert (siehe Projekt-Inventory `build-environment.md`).
- Lokal baubar: `net10.0-windows10.0.19041.0`, `net10.0-android`.
- Lokal **nicht** baubar: iOS/MacCatalyst (macOS/Xcode erforderlich; CI `macos-latest`).
- **IIS:** `W3SVC` läuft; Default-Site liefert eine „Anwendungslandschaft"-Seite unter
  `http://localhost/`. Die VideoPlayer-Anwendung ist unter `http://localhost/videoplayer`
  eingerichtet (404 auf `/api/health` am Root vs. **500** unter `/videoplayer/*`),
  antwortet aber derzeit mit **HTTP 500** auf alle Pfade — serverseitiges Problem.
  Ein Live-Aufruf belegt somit die Erreichbarkeit des Endpunkts (Verbindung kommt
  zustande), nicht eine positive Health-Antwort.

## Einschränkungen der Umgebung

- Das Testprojekt kann das MAUI-Projekt nicht referenzieren → MAUI-Code nur über
  Contract-Tests (Dateiinhalte) prüfbar; MVU-Logik sollte daher in eine `net10.0`-fähige
  Form gebracht werden, wenn sie unit-testbar sein soll.
- Windows-Build ist unpackaged (`WindowsPackageType=None`).
- `dotnet format whitespace --verify-no-changes` ist Teil der PR-Checks (Lint-Gate).
