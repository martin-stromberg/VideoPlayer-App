# Tasks: Altbestand VideoWebPlayer.Maui / VideoWebPlayer.Client entfernen

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Tests | `ContinueWatchingEventIngressTests.cs` löschen | Erledigt | Kein direkter Test — Datei testete nur Altbestand-DTOs |
| 2 | Tests | `MauiSettingsContractTests.cs` löschen | Erledigt | Kein direkter Test — Datei las nur Altbestand-Quellen |
| 3 | Tests | `EnumCoverageTests.cs`: 5 Legacy-Testfälle, ungenutzte Hilfsmitglieder/Usings und Legacy-Doc-Absatz entfernen | Erledigt | `EnumCoverageTests.*` (4 verbliebene Fälle), `enum-coverage-check --strict` grün |
| 4 | Tests | `LogProtocolContractTests.cs`: Altbestand-Erwähnung im XML-Doc entfernen | Erledigt | `NoDebugWriteLine_RemainsInAppSources` |
| 5 | Projekte | `ProjectReference` auf `VideoWebPlayer.Client` aus Test-`.csproj` entfernen | Erledigt | `dotnet build` des Testprojekts grün (0 Warnungen) |
| 6 | Projekte | Testprojekt umbenennen: `git mv`, `.csproj`, Namespaces/Usings/XML-Docs, `SettingsStorageContractTests`-Pfad | Erledigt | 1334/1334 Tests grün unter `VideoPlayer.Maui.Tests`-Assembly; `StorageProject_IsReferencedEverywhere` |
| 7 | Projekte | `InternalsVisibleTo` in `VideoPlayer.ApiClient.csproj` und `VideoPlayer.Mvu.csproj` aktualisieren | Erledigt | `internal`-Zugriff der Tests funktioniert (Build + Testlauf grün) |
| 8 | Projekte | Verzeichnis `VideoWebPlayer.Maui/` per `git rm -r` löschen | Erledigt | Repo-Suche ohne Treffer |
| 9 | Projekte | Verzeichnis `VideoWebPlayer.Client/` per `git rm -r` löschen | Erledigt | Repo-Suche ohne Treffer |
| 10 | Solution | `VideoPlayer.App.sln`: Altbestand-Einträge + Konfigurationszeilen entfernen; Testprojekt-Eintrag umbenennen | Erledigt | `dotnet restore`/`build` über Solution-Pfade grün |
| 11 | CI/CD | `pr-checks.yml`: Altbestand-Build-Steps entfernen; Testpfade auf `VideoPlayer.Maui.Tests` umstellen | Erledigt | Kein direkter Test — YAML-Review |
| 12 | CI/CD | `main-merge.yml`: `build-windows` entfernen, `build-ios` Altbestand-Build streichen + Upload-Pfad auf `VideoPlayer.Maui`, `needs` anpassen | Erledigt | Kein direkter Test — YAML-Review |
| 13 | CI/CD | `staging-merge.yml`: analog zu `main-merge.yml` | Erledigt | Kein direkter Test — YAML-Review |
| 14 | CI/CD | `Set-Version.ps1`: Altbestand-csproj aus `$csprojPaths` entfernen, Kommentar anpassen | Erledigt | Kein direkter Test — Skript-Review |
| 15 | Dokumentation | `README.md`: Altbestand-Zeile entfernen; Test-Kommando und Projektzeile auf `VideoPlayer.Maui.Tests` umstellen | Erledigt | Repo-Suche |
| 16 | Dokumentation | `docs/help/app-grundgeruest.md`: Altbestand-Bestandssatz anpassen | Erledigt | Repo-Suche |
| 17 | Dokumentation | `docs/help/einstellungen-datenhaltung.md`: Ausnahme `VideoWebPlayer.Maui` streichen | Erledigt | Repo-Suche |
| 18 | Dokumentation | `docs/help/erscheinungsbild.md`: Testprojekt-Name aktualisieren | Erledigt | Repo-Suche |
| 19 | Dokumentation | `PlaylistSortModeValues.cs`: XML-Doc-Bezug auf `VideoWebPlayer.Data` neutral formulieren | Erledigt | Build ohne CS-Warnungen |
| 20 | Dokumentation | `scripts/iOS-Deployment.ResponseFile.ps1`: Kommentar-Pfad auf `VideoPlayer.Maui.Tests` aktualisieren | Erledigt | `IosDeploymentResponseFileTests` grün |
| 21 | Verifikation | `dotnet restore`, `dotnet build` und `dotnet test` fehlerfrei; `dotnet format` grün; Repo-Suche ohne Altbestand-Treffer | Erledigt | 1334/1334 Tests, 0 Warnungen, Format sauber |
