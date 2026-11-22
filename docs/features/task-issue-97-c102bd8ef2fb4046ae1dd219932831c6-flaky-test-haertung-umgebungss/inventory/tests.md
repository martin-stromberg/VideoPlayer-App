# Tests: Bestand und Ausgangszustand

## Test-Ausgangszustand vor der Umsetzung

- **Zeitpunkt (mit Zeitzone):** 2026-11-22, ~19:50–20:00 Uhr (UTC+01:00)
- **Branch und Commit-ID:** `task/issue-97-c102bd8ef2fb4046ae1dd219932831c6-flaky-test-haertung-umgebungss` @ `0a8edbad9031e5f726477d343dae566a6b778bf9` (`refactor: Download-Ablauf-Pass mit Auto-Download-Schutz ins Service kapseln (#94)`)
- **Uncommittete Änderungen im getesteten Stand:** nur untracked `docs/features/task-issue-97-…/` (requirement.md + diese Bestandsaufnahme inkl. Testergebnisse); keine geänderten getrackten Dateien. Zusätzlich wurden `bin/`/`obj/`-Artefakte durch die Builds erzeugt (gitignore-abgedeckt).
- **Testumgebung und Runtime-/SDK-Versionen:** Windows, `dotnet --version` = **11.0.100** (kein `global.json` im Repo → SDK-Rollforward auf 11.0.100; CI verwendet `10.0.x` per `actions/setup-dotnet`). Installierte Workloads: `maui-windows` 11.0.0-preview.6.26360.8, `ios`, `maccatalyst`, `android` (VS 18.11). Laufzeit .NET 10/11 per SDK 11.
- **Ermittelte Testsuiten und Quellen der Testbefehle:**
  - `VideoPlayer.Maui.Tests` (xunit.v3 3.2.2, `net10.0`): Befehl aus `.github/workflows/pr-checks.yml` Job `verify`, Schritt `Test` — `dotnet test VideoPlayer.Maui.Tests/VideoPlayer.Maui.Tests.csproj --no-build --configuration Release`. Zusätzlich ein Debug-Lauf mit `DevApi`-Filter, weil DevApi-Tests nur unter `DEBUG || DEV_API` kompilieren (CI-Release baut sie nicht).
  - `VideoPlayer.E2E.Tests` (xunit.v3 3.2.2, `net10.0-windows`, FlaUI.UIA3 5.0.0): kein CI-Befehl vorhanden; lokaler Befehl laut `docs/help/e2e-tests.md` = `dotnet test VideoPlayer.E2E.Tests/VideoPlayer.E2E.Tests.csproj`. Nicht ausgeführt (siehe Lücken).

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| 1 (Release, CI-Pendant) | `dotnet test VideoPlayer.Maui.Tests/VideoPlayer.Maui.Tests.csproj --no-build --configuration Release --logger "console;verbosity=normal" --logger "trx;…maui-tests-release-baseline.trx" --results-directory docs/features/task-issue-97-…/inventory/test-results` (vorangehend `dotnet restore` + `dotnet build … -c Release`, beide Exit 0) | Repo-Wurzel | 0 | 1484 | 0 | 0 | [Log](test-results/dotnet-test-release-baseline.log), [TRX](test-results/maui-tests-release-baseline.trx) |
| 2 (Debug, DevApi-Filter) | `dotnet test VideoPlayer.Maui.Tests/VideoPlayer.Maui.Tests.csproj --configuration Debug --filter "FullyQualifiedName~DevApi"` (gleiche Logger/Results-Optionen) | Repo-Wurzel | 0 | 48 | 0 | 0 | [Log](test-results/dotnet-test-debug-devapi.log), [TRX](test-results/maui-tests-debug-devapi.trx) |
| 3 (Build-Prüfung E2E) | `dotnet build VideoPlayer.E2E.Tests/VideoPlayer.E2E.Tests.csproj --configuration Debug` | Repo-Wurzel | 0 | — | — | — | Build: 0 Warnungen, 0 Fehler (kein Testlauf) |

Hinweis Lauf 1: Der `--no-build`-Schritt nutzt den Release-Build; Laufzeit ~1 min 40 s. Lauf 2 kompiliert in Debug ein und enthält u. a. `DevApiHttpServerTests` (5 Tests, echter Loopback-Listener) und `DevApiServerTests_Metadata` (3 Tests) — die in Lauf 1 gar nicht existieren.

### Nachgewiesene bestehende Testfehler

**Keine.** In beiden dokumentierten Läufen ist kein Test fehlgeschlagen (1484/1484 bzw. 48/48). Eine Aussage über die nicht ausgeführte E2E-Suite wird nicht getroffen — siehe nächster Abschnitt.

### Testlücken und Ausführungsprobleme

- **`VideoPlayer.E2E.Tests` nicht ausgeführt.** Gründe: (a) FlaUI/UIA3 benötigt eine interaktive/sichtbare Windows-Desktop-Session; (b) die Suite baut die MAUI-Windows-App selbst (`AppFixture`, ~Minuten) und läuft dokumentiert 20–25 min; (c) sie ist auch in CI nicht Bestandteil — der dokumentierte Ausgangszustand für sie bleibt damit „unbekannt". Build-Kompilierbarkeit in Debug ist nachgewiesen (0 Fehler).
- **Release-Lauf enthält die `#if DEBUG || DEV_API`-Testdateien nicht** (`DevApiHttpServerTests`, `DevApiServerTests_Metadata`, `DevApiRouterTests`, `DevApiRouterTests_MapModel`, `DevApiRequestReaderTests`, `DevApiModelSnapshotTests`, `DevApiMetadataTests`) — diese wurden separat per Lauf 2 (Debug) abgedeckt. **Konsequenz für die Lane-Trennung:** im CI-Release fehlen die umgebungssensitivsten Maui.Tests-Listener-Tests bereits heute komplett.
- Keine Build-, Setup- oder Infrastrukturfehler in den ausgeführten Läufen.

## Testklassen (Bestand, anforderungsrelevant)

### `VideoPlayer.Maui.Tests` (~135 Dateien, 1317 `[Fact]`/`[Theory]`-Attribute)

Relevante Gruppen (vollständige Aufschlüsselung: [OsInterface-Audit](os-interface-audit.md)):

- **Netzwerk/Prozess/Dateisystem-Kandidaten:** `UdpServerDiscoveryTests`, `DevApiHttpServerTests`, `DevApiServerTests_Metadata`, `DevApiMetadataTests`, `IosDeploymentResponseFileTests`, `LocalDatabaseTests`, `FileLoggerTests`, `AppLogServiceTests`, `AppStorageLayoutTests` + 17 `SqliteDatabase`-Collection-Klassen mit Temp-Roots.
- **`ClientGate`-Collection:** `ClientGateTests`, `DeviceTokenServiceTests`, `AuthSessionServiceTests`, `AuthSessionServiceRefreshTests` (statischer Gate-Zustand).
- **Contract-Tests:** 31 Dateien lesen Repo-Quellen via `Stubs/RepositoryRoot.Find()` (z. B. `DevApiContractTests`, `TvOs*ContractTests`, `*ContractTests`).
- **Übrige Service-/Modell-Tests:** `StubHttpMessageHandler`-basiert, in-process.

### `VideoPlayer.E2E.Tests` (19 Testklassen, 74 `[Fact]`/`[Theory]`)

`BootstrapTests`, `ContinueWatchingContextMenuTests`, `ContinueWatchingPlaybackTests`, `DownloadsTests`, `LogProtocolTests`, `LoginTests`, `PairingTests`, `PlaybackTests`, `PlaylistManagementTests`, `PlaylistPlaybackTests`, `PlaylistProgressReportTests`, `PlaylistsTests`, `SeasonalGenresTests`, `SeriesDetailsTests`, `ServerSetupTests`, `ServerSwitchTests`, `SourceBrowsingTests`, `SourcePagingTests`, `SourceSearchTests` — alle `[Collection("E2E")]` via `E2ETestBase`, sequentiell.

## Hilfsmethoden (anforderungsrelevant)

### `AppInstance` (`VideoPlayer.E2E.Tests/Support/AppInstance.cs`)

- `StartAsync` — Prozessstart mit isoliertem Profil; Startup-Timeout Standard 45 s.
- `Dispose(bool)` — CloseMainWindow → `WaitForExit(10_000)` → `Kill(entireProcessTree: true)` + `WaitForExit(5_000)`; Diagnose + Cleanup.
- `DeleteProfileDirectory` — **ohne** Retry/Backoff, Restfehler kommentarlos geschluckt (Erweiterungspunkt der Anforderung).
- `WriteProcessDiagnostics` — `stdout.log`/`stderr.log`/`process.json` unter `.work/e2e-process-logs/`.
- `WaitForDevApiPortAsync` (200-ms-Poll), `WaitForMainWindow` (eigenes 30-s-Budget), `TryDelete`, `Sanitize`.

### `DevApiClient` (`Support/DevApiClient.cs`)

- `WaitForModelAsync` — 250-ms-Poll, Standard 20 s, Letztzustand im `TimeoutException`-Text.
- `GetModelAsync`, `GetTreeAsync`, `InvokeAsync`, `LongPressAsync`, `SetTextAsync`.

### `UiDriver` (`Support/UiDriver.cs`)

- Poll-Helfer mit 15-s-Defaults: `FindByAutomationId`, `EnterText`, `WaitForVisible`, `OpenFlyoutItem`, `ClickByName`, `WaitForNameAbsent`, `SelectComboBoxItem`, `GetComboBoxItemNames`, `GoBack` (Alt+Links-Fallback), `ScrollToEnd` (30-s-Deadline + Mausrad-Fallback), `GetVerticalScrollPercent`, `DumpTree`.

### `MockVideoServer` (`Support/MockVideoServer.cs`)

- `TcpListener`-Mock der Server-API mit Diagnose-Zählern/-Listen; `Dispose` mit 5-s-AcceptLoop-Wait.

### Stubs `VideoPlayer.Maui.Tests/Stubs/`

- `SqliteDatabaseCollection` / `ClientGateCollection` — Collection-Definitionen zur Serialisierung nicht-thread-sicherer Ressourcen.
- `StubHttpMessageHandler`, `InMemorySettingsStore`, `InMemorySecretStore`, `FakeNetworkMonitor`, `RecordingLoggerProvider`/`RecordedLogEntry`, `ManifestTestHelpers`, `RepositoryRoot`.

### Test-interne Poll-Helfer (Maui.Tests)

- `DevApiHttpServerTests.WaitForRequestAsync` (50 × 20 ms), `DevApiServerTests_Metadata.WaitForMetadataAsync` (100 × 20 ms), `AutoDownloadServiceTests`-Deadline-Schleife, `DownloadServiceReconcileTests`-Deadline-Schleifen (15 s), `IosDeploymentResponseFileTests.PwshAvailable`/`RunHelperScript`.
