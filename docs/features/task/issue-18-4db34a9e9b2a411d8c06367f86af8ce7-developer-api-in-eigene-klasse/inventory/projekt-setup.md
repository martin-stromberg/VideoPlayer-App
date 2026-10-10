# Detail: Projektstruktur und Referenzierungswege

## Solution `VideoPlayer.App.sln`

| Projekt | Typ | TargetFramework |
|---------|-----|-----------------|
| `VideoPlayer.ApiClient` | Bibliothek | net10.0 |
| `VideoPlayer.Mvu` | Bibliothek | net10.0 (keine MAUI-Abhängigkeit) |
| `VideoPlayer.Storage` | Bibliothek | net10.0 |
| `VideoWebPlayer.Client` | Bibliothek | net10.0 |
| `VideoPlayer.Maui` | MAUI-App (Exe) | net10.0-android/-ios/-maccatalyst/-windows10.0.19041.0 |
| `VideoWebPlayer.Maui` | MAUI-App (Exe) | net10.0-android/-ios/-maccatalyst/-windows |
| `VideoWebPlayer.Maui.Tests` | Testprojekt (xunit.v3) | net10.0 |
| `VideoPlayer.E2E.Tests` | Testprojekt (FlaUI) | net10.0-windows |

## Konventionen der Bibliotheks-csproj (aus `VideoPlayer.Mvu.csproj`/`VideoWebPlayer.Maui.Tests.csproj`)

- `Microsoft.NET.Sdk`, `<ImplicitUsings>enable</ImplicitUsings>`, `<Nullable>enable</Nullable>`
- `<GenerateDocumentationFile>true</GenerateDocumentationFile>` + `<WarningsAsErrors>CS1591</WarningsAsErrors>` — XML-Doku-Kommentare sind Pflicht (deutschsprachig, `<summary>`)
- `<PackageLicenseFile>LICENSE</PackageLicenseFile>` + `<None Include="../LICENSE" Pack="true" PackagePath="\" />`
- `<Authors>Martin Stromberg</Authors>`, `<Copyright>Copyright (c) Martin Stromberg</Copyright>`
- Testprojekt: `<IsPackable>false</IsPackable>`, Pakete `xunit.v3`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`, `coverlet.collector`, globales `<Using Include="Xunit" />`
- `VideoPlayer.Mvu` gewährt `<InternalsVisibleTo Include="VideoWebPlayer.Maui.Tests" />`

## NuGet-/Paket-Situation

- `NuGet.config`: nur Quelle `nuget` (api.nuget.org) — kein privater Feed konfiguriert.
- Kein `Directory.Build.props`, kein `Directory.Packages.props`, keine zentralen Versions-Pins — Paketversionen stehen in den csproj-Dateien.
- `.releaserc.json` + `.github/workflows/` (semantic-release auf `main`/`staging`): Release-Automatisierung für GitHub-Releases, keine NuGet-Publikationspipeline.
- Projekt-Referenzen innerhalb des Repos sind der bisherige einzige Referenzierungsweg.

## Namenskonventionen

- Projekte/Namespaces: `VideoPlayer.<Bereich>` bzw. `VideoWebPlayer.<Bereich>`; neue Bibliothek bricht bewusst mit `msTools.<Bereich>` (eigenes Präfix des Anwenders).
- Klassen PascalCase mit `DevApi`-Präfix (`DevApiHttpServer`, `DevApiRouter`, …); XML-Doku auf Deutsch.
- Keine `src/`-Verzeichnisebene — Projekte liegen direkt im Repo-Root.

## Hinweis zur Umbenennung

Das Testprojekt heißt `VideoWebPlayer.Maui.Tests`, testet aber faktisch `VideoPlayer.*`-Bibliotheken (historischer Name). Es besitzt keine Projekt-Referenz auf `VideoPlayer.Maui` (MAUI-App) — MAUI-gebundener Code wird nur über Quelltext-Contract-Tests (`RepositoryRoot`) statisch geprüft.
