# Tests — Bestandsaufnahme (Schritt 2)

## Testprojekte

- `VideoPlayer.Maui.Tests` (xunit.v3, `net10.0`, ~1440 Tests) — plattformneutrale
  Logik- und Contract-Tests; Contract-Tests lesen Quelldateien über
  `Stubs/RepositoryRoot.Find()` (Sucht `VideoPlayer.App.sln` aufwärts).
- `VideoPlayer.E2E.Tests` (FlaUI, `net10.0-windows`) — Windows-E2E, für tvOS
  nicht anwendbar.
- Hilfsklassen in `VideoPlayer.Maui.Tests/Stubs/`: `InMemorySettingsStore`,
  `InMemorySecretStore`, `FakeNetworkMonitor`, `StubHttpMessageHandler`,
  `RecordedLogEntry`, `RecordingLoggerProvider`, `RepositoryRoot`.

## Bestehende tvOS-Contract-Tests

`VideoPlayer.Maui.Tests/TvOsProjectContractTests.cs` — liest
`VideoPlayer.TvOS.csproj`, `Program.cs`, `AppDelegate.cs`, `pr-checks.yml`:

| Test | Prüfung |
|---|---|
| `TvOsProject_IsPartOfSolution` | csproj in `VideoPlayer.App.sln` |
| `TvOsProject_ReferencesOnlyPlatformNeutralLibraries` | exakt 4 `ProjectReference`s (msTools.DeveloperApi, ApiClient, Mvu, Storage) |
| `TvOsProject_HasNoPackageReferencesOrMaui` | **keine `PackageReference`, kein `UseMaui`** im tvOS-csproj |
| `TvOsProject_TargetsTvOsOnlyConditionally` | `IncludeTvOsTarget`-Schalter, `net10.0-tvos`, net10.0-Fallback |
| `TvOsApp_StartsWithWindowAndStartView` | `UIApplication.Main`+`AppDelegate`, `MakeKeyAndVisible`, `MainViewController` im Delegate |
| `PrChecksWorkflow_ContainsTvOsSimulatorBuildJob` | `build-tvos`-Job: macos-latest, Workload-Rollback, `tvossimulator-arm64`, `EnableCodeSigning=false` |

## Start-/Verbindungs-Update-Tests (plattformneutral, decken den MVU-Fluss ab)

- `AppUpdateTests_Startup.cs` — Effektfolge des `StartupRequestedMessage`
  (Initialize→Settings→Servers→Monitoring→HomeLists), `ServersLoaded` →
  HealthCheck, Erststart → `NavigateToPageEffect(ServerSetup)`
- `AppUpdateTests_Connection.cs`, `AppUpdateTests_Offline.cs`,
  `AppUpdateTests_ServerManagement.cs`, `AppUpdateTests_Pairing.cs`,
  `AppUpdateTests_Bootstrap.cs`, `AppUpdateTests_Navigation.cs`,
  `AppUpdateTests_NetworkSync.cs` — übrige Verbindungs-/Navigations-Pfade
- `ConnectionWorkflowContractTests.cs` — Contract-Test des Verbindungs-Workflows

## Tests mit Dateipfad-Assertions auf verschiebungsrelevante Dateien

Werden `IServerAddressProvider`/`ServerAddressProvider`/
`IServerHealthProbe`/`ServerHealthProbe`/`SignalRServerEventsConnection` aus
`VideoPlayer.Maui/Services/` verschoben, sind folgende Contract-Tests
anzupassen:

| Testdatei | Stelle | Assertion auf |
|---|---|---|
| `NetworkSyncContractTests.cs` | Z. 75 | `Services/SignalRServerEventsConnection.cs` (Hub-Events, `WithAutomaticReconnect`); `Csproj_ReferencesSignalRClient` prüft `Microsoft.AspNetCore.SignalR.Client` im **Maui**-csproj |
| `PairingContractTests.cs` | Z. 183–184, 227 | `Services/ServerHealthProbe.cs`, `Services/IServerHealthProbe.cs` |
| `ServerSetupContractTests.cs` | Z. 183 | `Services/ServerHealthProbe.cs` (`CheckHealthAsync`) |
| `SettingsStorageContractTests.cs` | Z. 111 | `Services/ServerAddressProvider.cs` (`KnownServersService`, kein `Preferences.Default`) |
| `VideoPlayerMauiContractTests.cs` | Z. 116 | Existenz `VideoPlayer.Maui/Services/ServerHealthProbe.cs` |

## Weitere betroffene Tests

- `LogProtocolContractTests.cs:136` — liest `Services/ConnectivityNetworkStatusMonitor.cs`
  (`ILogger<ConnectivityNetworkStatusMonitor>`); die Datei verbleibt MAUI-seitig
  — **nicht betroffen**, sofern sie nicht verschoben wird.
- `SettingsStorageContractTests.cs` — prüft `PreferencesSettingsStore`,
  `SecureStorageSecretStore`, `AppStorageLayoutProvider` in `MauiProgram`
  — **nicht betroffen** (MAUI-Adapter bleiben).
- `EnumCoverageTests.cs`, `AppUpdateTests_*` — berühren nur `VideoPlayer.Mvu`-
  Quellcode; Namespace-/Ortsverschiebung von Diensten innerhalb Mvu ist für
  die Suite transparent, solange `using VideoPlayer.Mvu` passt.

## Lokale Ausführbarkeit unter Windows

`dotnet build`/`dotnet test` der Solution läuft — das tvOS-Projekt degradiert
zu einer leeren `net10.0`-Bibliothek (`IncludeTvOsTarget=false`), sodass die
tvOS-Quelldateien lokal **nicht** kompiliert werden. Kompiliernachweis tvOS
ausschließlich über den macOS-CI-Job `build-tvos` (wirkt erst nach Push).
