# Build-, Projekt- und CI-Konfiguration sowie Umgebung

## Projekte und Solution

`VideoPlayer.App.sln` enthält drei Projekte:

| Projekt | Datei | Target Framework(s) | Rolle |
|---------|-------|---------------------|-------|
| `VideoWebPlayer.Client` | `VideoWebPlayer.Client/VideoWebPlayer.Client.csproj` | `net10.0` | Plattformneutraler API-Client + DTOs |
| `VideoWebPlayer.Maui` | `VideoWebPlayer.Maui/VideoWebPlayer.Maui.csproj` | `net10.0-android`; zusätzlich `net10.0-ios`, `net10.0-maccatalyst` (nicht auf Linux), `net10.0-windows10.0.19041.0` (nur auf Windows) | MAUI-App |
| `VideoWebPlayer.Maui.Tests` | `VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj` | `net10.0` | xUnit-Tests; referenziert **nur** `VideoWebPlayer.Client` (kein Verweis auf das MAUI-Projekt) |

### `VideoWebPlayer.Maui.csproj` — relevante Einstellungen

- `UseMaui`, `SingleProject`, `ImplicitUsings`, `Nullable` aktiviert; `MauiXamlInflator=SourceGen` (XAML-Source-Generator).
- `ApplicationId`: `com.companyname.videowebplayer.maui` (Template-Default), `ApplicationDisplayVersion 1.0`, `WindowsPackageType=None` (unpackaged Windows-App).
- `SupportedOSPlatformVersion`: iOS/MacCatalyst 15.0, Android 21.0, Windows 10.0.17763.0.
- `UserSecretsId` vorhanden.
- `MauiClientApiToken`: MSBuild-Property (Default-Platzhalter `<MAUI_CLIENT_API_TOKEN>`), wird als Assembly-Metadata `VideoWebPlayer.Maui.ApiToken` geschrieben und von `MauiClientConfiguration` gelesen.
- iOS: `CodesignKey`/`CodesignProvision` gesetzt.
- Paketreferenzen: `CommunityToolkit.Maui` 15.0.0, `CommunityToolkit.Maui.MediaElement` 10.0.0, `Microsoft.AspNetCore.SignalR.Client` 10.0.10, `Microsoft.Extensions.Configuration.UserSecrets` 10.0.10, `Microsoft.Maui.Controls`/`Essentials` 10.0.90, `Microsoft.Extensions.Logging.Debug` 10.0.10, `SkiaSharp` 4.151.1, `sqlite-net-pcl` 1.11.285, `SQLitePCLRaw.bundle_green` 2.1.11 + `lib.e_sqlite3` (3.53.3 / android 2.1.13), `Zeroconf` 3.7.16.
- `ProjectReference` auf `VideoWebPlayer.Client`.

### `VideoWebPlayer.Maui.Tests.csproj`

- `net10.0`, `IsPackable=false`; Pakete: `xunit.v3` 3.2.2, `xunit.runner.visualstudio` 3.1.5, `Microsoft.NET.Test.Sdk` 18.8.1, `coverlet.collector` 10.0.1.

### Weitere Konfiguration

- `NuGet.config`: nur `https://api.nuget.org/v3/index.json`.
- `.github/scripts/Set-Version.ps1`: setzt Versionsnummern für Release-Builds (von CI aufgerufen).
- `README.md`: beschreibt Voraussetzungen, Build-/Testbefehle und Branching; verweist auf den API-Vertrag im Server-Repository (`docs/API.md`, nicht in diesem Repo).
- `.gitignore`: Standard-VS/dotnet-Ignore; uncommitted wurde `issue.md` ergänzt.

## CI-Workflows (`.github/workflows/`)

| Workflow | Trigger | Relevante Schritte |
|----------|---------|--------------------|
| `pr-checks.yml` („PR Checks") | PR → `main`, `staging` | `windows-latest`, .NET 10.0.x, `dotnet workload install maui`; `dotnet restore`; Build Client + Tests + MAUI `net10.0-android` + MAUI `net10.0-windows10.0.19041.0` (Release); `dotnet test VideoWebPlayer.Maui.Tests --no-build -c Release`; `dotnet format whitespace --verify-no-changes`; PRs nach `main` nur von `staging` erlaubt |
| `staging-merge.yml` („Staging Prerelease") | Push `staging` | semantic-release (Prerelease-Tag) → Publish Android (APK) + Windows (MSIX) auf `windows-latest`, iOS-Build (`net10.0-ios`, `iossimulator-arm64`) auf `macos-latest`; Draft-PR `staging` → `main` |
| `main-merge.yml` („Main Release") | Push `main` | Gleiche Builds wie staging, Final-Release-Tag, Upload der Artifacts, Back-Merge-PR `main` → `staging` |

## Lokale Umgebung (Stand des Inventory-Laufs)

- Betriebssystem: Windows (Zeitzone „W. Europe Standard Time", UTC+02:00).
- .NET SDK: `10.0.401` (`dotnet --version`).
- Installierte Workloads (`dotnet workload list`, Manifeste aus SDK 10.0.400 / VS 18.10.12210.168):
  - `android` 36.1.69/10.0.100
  - `ios` 26.5.10318/10.0.100
  - `maccatalyst` 26.5.10318/10.0.100
  - `maui-windows` 10.0.20/10.0.100
- Nachgewiesene Build-Fähigkeit auf dieser Maschine: `net10.0-windows10.0.19041.0` und `net10.0-android` bauen erfolgreich in Release (Logs unter `test-results/`).
- Nicht möglich auf dieser Maschine: iOS-/MacCatalyst-Builds (erfordern macOS/Xcode; CI nutzt `macos-latest`).
- `NuGet.config` deckt die benötigten Paketquellen ab; `dotnet restore` auf die gesamte Solution läuft fehlerfrei.

## Einschränkungen der Umgebung (Beobachtungen)

- Das Testprojekt referenziert das MAUI-Projekt nicht — MAUI-spezifischer Code ist nur über Quelltext-Contract-Tests (`File.ReadAllText` auf `.cs`-Dateien, s. `MauiSettingsContractTests`) prüfbar, nicht über kompilierte Referenzen.
- Windows-Build ist unpackaged (`WindowsPackageType=None`) — kein MSIX im Debug-/Release-Build-Pfad lokal erzeugt.
- `dotnet format whitespace --verify-no-changes` ist Teil der PR-Checks (Lint-Gate).
