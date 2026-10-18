# Eingehende Referenzen auf den Altbestand

## Solution

`VideoPlayer.App.sln` — drei Projekteinträge mit `ProjectConfigurationPlatforms`-Blöcken:

| Projekt | GUID | Aktion |
|---------|------|--------|
| `VideoWebPlayer.Client` | `{3512A538-C692-417A-BB07-D05876F4C7CD}` | Eintrag + 12 Konfigurationszeilen entfernen |
| `VideoWebPlayer.Maui` | `{FE005A1B-DA1D-41B6-9D81-95DE9F910523}` | Eintrag + 12 Konfigurationszeilen entfernen |
| `VideoWebPlayer.Maui.Tests` | `{6FC81FCF-EB2A-4680-8B64-E048455D5527}` | **bleibt** (Testprojekt der neuen App) |

## Projektdateien

| Datei | Referenz | Aktion |
|-------|----------|--------|
| `VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj` | `<ProjectReference Include="..\VideoWebPlayer.Client\VideoWebPlayer.Client.csproj" />` | Zeile entfernen |
| `VideoPlayer.ApiClient/VideoPlayer.ApiClient.csproj` | `<InternalsVisibleTo Include="VideoWebPlayer.Maui.Tests" />` | **behalten** — bezieht sich auf das Test-Assembly, nicht den Altbestand |
| `VideoPlayer.Mvu/VideoPlayer.Mvu.csproj` | `<InternalsVisibleTo Include="VideoWebPlayer.Maui.Tests" />` | **behalten** — analog |

## Quelltext-Referenzen außerhalb des Altbestands

| Datei | Art der Referenz |
|-------|------------------|
| `VideoWebPlayer.Maui.Tests/ContinueWatchingEventIngressTests.cs` | `using VideoWebPlayer.Client.Models;` — testet `ContinueWatchingDto`, `DtoMovie`, `ContinueWatchingMutationResult` |
| `VideoWebPlayer.Maui.Tests/EnumCoverageTests.cs` | `Enum.GetValues<RecentEntryType>()` (Typ aus `VideoWebPlayer.Client`, globaler Namespace) + `Read("VideoWebPlayer.Maui", …)` für 4 Legacy-Enums |
| `VideoWebPlayer.Maui.Tests/MauiSettingsContractTests.cs` | `File.ReadAllText` auf 6 Dateien unter `VideoWebPlayer.Maui/` |
| `VideoPlayer.ApiClient/Models/PlaylistSortModeValues.cs` | XML-Doc-Kommentar: „spiegelt `VideoWebPlayer.Data.PlaylistSortMode`" |

## CI/CD

| Datei | Altbestand-Anteile |
|-------|--------------------|
| `.github/workflows/pr-checks.yml` | Step `Build Client` (`dotnet build VideoWebPlayer.Client/…`), Step `Build MAUI Windows` (`dotnet build VideoWebPlayer.Maui/…`); `Build Tests`/`Test`/`Lint` betreffen `VideoWebPlayer.Maui.Tests` → **bleiben** |
| `.github/workflows/main-merge.yml` | `build-windows`: `Publish Android`, `Publish Windows`, `Upload Android artifact`, `Upload Windows artifact` (alle `VideoWebPlayer.Maui`); `build-ios`: `Build iOS` (Altbestand) + `Upload iOS artifact` (zippt `VideoWebPlayer.Maui/bin/Release/net10.0-ios/*.app`); `Build iOS (VideoPlayer.Maui)` → **bleibt**; `back-merge.needs` bleibt unverändert |
| `.github/workflows/staging-merge.yml` | identische Struktur wie `main-merge.yml` (`build-ios` auf Runner `xcode-27`, `-p:EnableCodeSigning=false`) |
| `.github/actions/package-ios` | kein Altbestand-Bezug (arbeitet auf `VideoPlayer.Maui`) |

## Dokumentation

| Datei | Altbestand-Bezug |
|-------|------------------|
| `README.md` | Projektstruktur-Tabelle: Zeile „`VideoWebPlayer.Maui` \| Altbestand der bisherigen App — wird schrittweise durch `VideoPlayer.Maui` abgelöst" |
| `docs/help/app-grundgeruest.md` | Satz: „bislang vorhandene App (`VideoWebPlayer.Maui`) bleibt unverändert bestehen" |
| `docs/help/einstellungen-datenhaltung.md` | „(Ausnahmen: Altbestand `VideoWebPlayer.Maui`, Discovery in …)" |
| `docs/help/erscheinungsbild.md` | „`DesignTokensContractTests` (`VideoWebPlayer.Maui.Tests`) pinnen die …" — bezieht sich aufs Testprojekt → bleibt fachlich korrekt |

## Nicht referenziert / irrelevant

- `VideoPlayer.E2E.Tests` — keine `VideoWebPlayer`-Referenz
- `scripts/iOS-Deployment.ps1` — arbeitet ausschließlich auf `VideoPlayer.Maui`; env `VIDEOWEBPLAYER_MAUI_API_TOKEN` bleibt (wird von `ClientGate.Resolve()` gelesen)
- `scripts/iOS-Deployment.ResponseFile.ps1` — Kommentar nennt `VideoWebPlayer.Maui.Tests/IosDeploymentResponseFileTests.cs` (Testprojekt bleibt → Pfad korrekt)
- `.githooks/` — keine Altbestand-Referenzen; `enum-coverage-check.py` prüft Enum-Nennungen in Testdateien (siehe tests.md)
- `docs/projects/`, `docs/features/` — historische Artefakte, unverändert
- `changes.log` — Historiendatei, erhält nur neuen Eintrag
