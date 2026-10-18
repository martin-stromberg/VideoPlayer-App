# Tasks: Altbestand VideoWebPlayer.Maui / VideoWebPlayer.Client entfernen

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Tests | `ContinueWatchingEventIngressTests.cs` löschen | Offen | — |
| 2 | Tests | `MauiSettingsContractTests.cs` löschen | Offen | — |
| 3 | Tests | `EnumCoverageTests.cs`: 5 Legacy-Testfälle, ungenutzte Hilfsmitglieder/Usings und Legacy-Doc-Absatz entfernen | Offen | — |
| 4 | Tests | `LogProtocolContractTests.cs`: Altbestand-Erwähnung im XML-Doc entfernen | Offen | — |
| 5 | Projekte | `ProjectReference` auf `VideoWebPlayer.Client` aus Test-`.csproj` entfernen | Offen | — |
| 6 | Projekte | Testprojekt umbenennen: `git mv VideoWebPlayer.Maui.Tests VideoPlayer.Maui.Tests`, `.csproj` umbenennen, Namespaces/Usings/XML-Docs in allen Dateien anpassen, `SettingsStorageContractTests`-Pfad aktualisieren | Offen | — |
| 7 | Projekte | `InternalsVisibleTo` in `VideoPlayer.ApiClient.csproj` und `VideoPlayer.Mvu.csproj` auf `VideoPlayer.Maui.Tests` aktualisieren | Offen | — |
| 8 | Projekte | Verzeichnis `VideoWebPlayer.Maui/` per `git rm -r` löschen | Offen | — |
| 9 | Projekte | Verzeichnis `VideoWebPlayer.Client/` per `git rm -r` löschen | Offen | — |
| 10 | Solution | `VideoPlayer.App.sln`: Altbestand-Einträge + Konfigurationszeilen entfernen; Testprojekt-Eintrag umbenennen | Offen | — |
| 11 | CI/CD | `pr-checks.yml`: Altbestand-Build-Steps entfernen; Testpfade auf `VideoPlayer.Maui.Tests` umstellen | Offen | — |
| 12 | CI/CD | `main-merge.yml`: `build-windows` entfernen, `build-ios` Altbestand-Build streichen + Upload-Pfad auf `VideoPlayer.Maui`, `needs` anpassen | Offen | — |
| 13 | CI/CD | `staging-merge.yml`: analog zu `main-merge.yml` | Offen | — |
| 14 | CI/CD | `Set-Version.ps1`: Altbestand-csproj aus `$csprojPaths` entfernen, Kommentar anpassen | Offen | — |
| 15 | Dokumentation | `README.md`: Altbestand-Zeile entfernen; Test-Kommando und Projektzeile auf `VideoPlayer.Maui.Tests` umstellen | Offen | — |
| 16 | Dokumentation | `docs/help/app-grundgeruest.md`: Altbestand-Bestandssatz anpassen | Offen | — |
| 17 | Dokumentation | `docs/help/einstellungen-datenhaltung.md`: Ausnahme `VideoWebPlayer.Maui` streichen | Offen | — |
| 18 | Dokumentation | `docs/help/erscheinungsbild.md`: Testprojekt-Name aktualisieren | Offen | — |
| 19 | Dokumentation | `VideoPlayer.ApiClient/Models/PlaylistSortModeValues.cs`: XML-Doc-Bezug auf `VideoWebPlayer.Data` neutral formulieren | Offen | — |
| 20 | Dokumentation | `scripts/iOS-Deployment.ResponseFile.ps1`: Kommentar-Pfad auf `VideoPlayer.Maui.Tests` aktualisieren | Offen | — |
| 21 | Verifikation | `dotnet restore`, `dotnet build` und `dotnet test` des Testprojekts fehlerfrei; `dotnet format whitespace --verify-no-changes` grün; Repo-Suche nach `VideoWebPlayer` ohne Altbestand-Treffer | Offen | — |
