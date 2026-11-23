# Tasks: tvOS-Projektgerüst und frühe Build-Validierung über macOS-CI

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Projektgerüst | `VideoPlayer.TvOS/VideoPlayer.TvOS.csproj` anlegen (konditionales `net10.0-tvos`-TFM via `IncludeTvOsTarget`, App-Metadaten, Konventionen, vier `ProjectReference`s) | Erledigt | `TvOsProjectContractTests.TvOsProject_ReferencesOnlyPlatformNeutralLibraries`, `TvOsProject_TargetsTvOsOnlyConditionally` |
| 2 | Projektgerüst | `VideoPlayer.TvOS/Program.cs` anlegen (`UIApplication.Main`-Einstieg) | Erledigt | `TvOsProjectContractTests.TvOsApp_StartsWithWindowAndStartView` |
| 3 | Projektgerüst | `VideoPlayer.TvOS/AppDelegate.cs` anlegen (`UIApplicationDelegate`, `Window`, `FinishedLaunching`) | Erledigt | `TvOsProjectContractTests.TvOsApp_StartsWithWindowAndStartView` |
| 4 | Projektgerüst | `VideoPlayer.TvOS/MainViewController.cs` anlegen (leere Startansicht mit Titel) | Erledigt | `TvOsProjectContractTests.TvOsApp_StartsWithWindowAndStartView` |
| 5 | Projektgerüst | `VideoPlayer.TvOS/Info.plist` anlegen (Bundle-Metadaten, `UIDeviceFamily=3`, mDNS-vorbereitende Schlüssel) | Erledigt | Kein direkter Test |
| 6 | Solution | `VideoPlayer.TvOS` in `VideoPlayer.App.sln` aufnehmen (GUID + Debug/Release-Konfigurationen) | Erledigt | `TvOsProjectContractTests.TvOsProject_IsPartOfSolution` |
| 7 | CI | Job `build-tvos` in `.github/workflows/pr-checks.yml` ergänzen (macos-latest, SDK-Band-Pin, tvOS-Rollback `26.5.10318`, Simulator-Build ohne Signierung) | Erledigt | `TvOsProjectContractTests.PrChecksWorkflow_ContainsTvOsSimulatorBuildJob` |
| 8 | Kompatibilität | Paketkompatibilität unter `net10.0-tvos` dokumentieren (SQLite-Stack, Zeroconf inkl. Ausweichvorschlag) — Prüfergebnis in `inventory/projects.md`, Dauer-Doku in `docs/help/` | Erledigt | Kein direkter Test (Dokumentationsartefakt) |
| 9 | Tests | `TvOsProjectContractTests` anlegen (`VideoPlayer.Maui.Tests`): Solution-Mitgliedschaft, Referenzmenge, TFM-Schalter, CI-Job-Vorhandensein | Erledigt | `dotnet test VideoPlayer.Maui.Tests` 1446/1446 |
| 10 | Validierung | Windows-Validierung: `dotnet restore`/`build`/`dotnet test` der Solution ohne tvOS-Workload | Erledigt | Solution-Build 0 Fehler; `dotnet test` 1446/1446 |
| 11 | Validierung | Workflow-YAML-Syntax prüfen | Erledigt | PyYAML-Parse erfolgreich (Jobs `verify`, `build-tvos`) |
| 12 | Dokumentation | `docs/help/`-Bereich für die tvOS-App anlegen (Beschreibung, Installation/CI, Kompatibilität), `changes.log`, README | Offen | — (Lifecycle-Schritt 9) |
