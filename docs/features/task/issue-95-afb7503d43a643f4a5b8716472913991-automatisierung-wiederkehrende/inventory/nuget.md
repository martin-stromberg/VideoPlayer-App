# NuGet-Abhängigkeiten (Bestand für die wöchentliche Paketprüfung)

Stand: Commit `0a8edbad9031e5f726477d343dae566a6b778bf9`.
Erfasst wurden alle `<PackageReference>`-Einträge der zehn
`.csproj`-Dateien im Repo-Root (kein `src/`-Verzeichnis).

## Paketquellen und zentrale Verwaltung

- `NuGet.config` (Repo-Root): einzige Quelle
  `https://api.nuget.org/v3/index.json`, `packageSources` werden mit
  `<clear />` zurückgesetzt — keine privaten Feeds, keine
  Source-Mappings.
- **Kein** `Directory.Packages.props` — keine zentrale
  Paketverwaltung (CPM); Versionen stehen dezentral in den
  `.csproj`-Dateien.
- **Kein** `Directory.Build.props`, **kein** `global.json` im
  Repository (das SDK-Pinning per `global.json` existiert nur als
  Laufzeit-Artefakt in den macOS-Jobs der Merge-Workflows).

## Direkte Paketreferenzen je Projekt

### `VideoPlayer.ApiClient` (`net10.0`)

| Paket | Version |
|-------|---------|
| `Microsoft.Extensions.Logging.Abstractions` | 10.0.10 |
| `Zeroconf` | 3.7.16 |

### `VideoPlayer.Maui` (MAUI-App)

| Paket | Version |
|-------|---------|
| `Microsoft.Maui.Controls` | 10.0.90 |
| `Microsoft.Maui.Essentials` | 10.0.90 |
| `CommunityToolkit.Maui.MediaElement` | 10.0.0 |
| `Microsoft.Extensions.Logging.Debug` | 10.0.10 |
| `ZXing.Net.Maui.Controls` | 0.10.4 |

### `VideoPlayer.Mvu` (`net10.0`)

| Paket | Version |
|-------|---------|
| `Microsoft.AspNetCore.SignalR.Client` | 10.0.10 |
| `Microsoft.Extensions.Logging.Abstractions` | 10.0.10 |

### `VideoPlayer.Storage` (`net10.0`)

| Paket | Version |
|-------|---------|
| `Microsoft.Extensions.Logging.Abstractions` | 10.0.10 |
| `sqlite-net-pcl` | 1.11.285 |
| `SQLitePCLRaw.bundle_green` | 2.1.11 |
| `SQLitePCLRaw.provider.internal` | 2.1.11 |
| `SQLitePCLRaw.lib.e_sqlite3` | 3.53.3 |
| `SQLitePCLRaw.lib.e_sqlite3.android` | 2.1.13 |

### `VideoPlayer.TvOS` (`net10.0` unter Windows / `net10.0-tvos` unter macOS)

| Paket | Version |
|-------|---------|
| `SQLitePCLRaw.lib.e_sqlite3.tvos` | 2.1.11 |

### `VideoPlayer.TvOS.EsqliteStub` (`net10.0`)

Keine Paketreferenzen.

### `msTools.DeveloperApi` (`net10.0`)

Keine Paketreferenzen.

### `msTools.DeveloperApi.Maui` (MAUI-Bibliothek)

| Paket | Version |
|-------|---------|
| `Microsoft.Maui.Controls` | `$(MauiPackageVersion)` = 10.0.90 |
| `Microsoft.Maui.Essentials` | `$(MauiPackageVersion)` = 10.0.90 |

### `VideoPlayer.Maui.Tests` (`net10.0`)

| Paket | Version |
|-------|---------|
| `coverlet.collector` | 10.0.1 |
| `Microsoft.Extensions.Logging` | 10.0.10 |
| `Microsoft.NET.Test.Sdk` | 18.8.1 |
| `xunit.runner.visualstudio` | 3.1.5 |
| `xunit.v3` | 3.2.2 |

### `VideoPlayer.E2E.Tests` (`net10.0-windows`)

| Paket | Version |
|-------|---------|
| `coverlet.collector` | 10.0.1 |
| `FlaUI.UIA3` | 5.0.0 |
| `Microsoft.NET.Test.Sdk` | 18.8.1 |
| `xunit.runner.visualstudio` | 3.1.5 |
| `xunit.v3` | 3.2.2 |

## Bekannter Sicherheitsbefund im Ausgangszustand

Der Restore-Lauf vom 2026-11-15 (Nachweis:
[`test-results/01-restore.log`](test-results/01-restore.log)) meldet
die Warnung **NU1903**:

- Paket `Microsoft.AspNetCore.SignalR.Common` **10.0.10** (transitiv
  über `Microsoft.AspNetCore.SignalR.Client` 10.0.10 in
  `VideoPlayer.Mvu`) weist eine bekannte Sicherheitsanfälligkeit mit
  **hohem** Schweregrad auf:
  <https://github.com/advisories/GHSA-334f-vvp4-7g6p>
- Gemeldet für die Projekte `VideoPlayer.Mvu`,
  `VideoPlayer.Maui.Tests`, `VideoPlayer.TvOS` und `VideoPlayer.Maui`
  (transitive Auflösung je Projekt).
- Der Build schlägt deshalb nicht fehl (Warnung, kein Fehler; zwei
  NU1903-Warnungen auch im Release-Build der Test-Assembly, siehe
  [`test-results/02-build-tests.log`](test-results/02-build-tests.log)).

Dieser Befund ist ein konkreter, bereits bestehender
Prüfgegenstand für die geplante wöchentliche NuGet-Prüfung bzw. das
monatliche Sicherheitsreview.
