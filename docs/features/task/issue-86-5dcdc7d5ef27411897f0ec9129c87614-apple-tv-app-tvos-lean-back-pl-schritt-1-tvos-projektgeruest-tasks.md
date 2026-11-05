# Tasks: tvOS-Projektgerüst und frühe Build-Validierung über macOS-CI

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Projektgerüst | `VideoPlayer.TvOS/VideoPlayer.TvOS.csproj` anlegen (konditionales `net10.0-tvos`-TFM via `IncludeTvOsTarget`, App-Metadaten, Konventionen, vier `ProjectReference`s) | Offen | — |
| 2 | Projektgerüst | `VideoPlayer.TvOS/Program.cs` anlegen (`UIApplication.Main`-Einstieg) | Offen | — |
| 3 | Projektgerüst | `VideoPlayer.TvOS/AppDelegate.cs` anlegen (`UIApplicationDelegate`, `Window`, `FinishedLaunching`) | Offen | — |
| 4 | Projektgerüst | `VideoPlayer.TvOS/MainViewController.cs` anlegen (leere Startansicht mit Titel) | Offen | — |
| 5 | Projektgerüst | `VideoPlayer.TvOS/Info.plist` anlegen (Bundle-Metadaten, `UIDeviceFamily=3`, mDNS-vorbereitende Schlüssel) | Offen | — |
| 6 | Solution | `VideoPlayer.TvOS` in `VideoPlayer.App.sln` aufnehmen (GUID + Debug/Release-Konfigurationen) | Offen | — |
| 7 | CI | Job `build-tvos` in `.github/workflows/pr-checks.yml` ergänzen (macos-latest, SDK-Band-Pin, tvOS-Rollback `26.5.10318`, Simulator-Build ohne Signierung) | Offen | — |
| 8 | Kompatibilität | Paketkompatibilität unter `net10.0-tvos` dokumentieren (SQLite-Stack, Zeroconf inkl. Ausweichvorschlag) — Prüfergebnis in `inventory/projects.md`, Dauer-Doku in `docs/help/` | Offen | — |
| 9 | Tests | `TvOsProjectContractTests` anlegen (`VideoPlayer.Maui.Tests`): Solution-Mitgliedschaft, Referenzmenge, TFM-Schalter, CI-Job-Vorhandensein | Offen | — |
| 10 | Validierung | Windows-Validierung: `dotnet restore`/`build`/`dotnet test` der Solution ohne tvOS-Workload | Offen | — |
| 11 | Validierung | Workflow-YAML-Syntax prüfen | Offen | — |
| 12 | Dokumentation | `docs/help/`-Bereich für die tvOS-App anlegen (Beschreibung, Installation/CI, Kompatibilität), `changes.log`, README | Offen | — |
