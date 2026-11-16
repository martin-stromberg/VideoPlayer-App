# Tests und Test-Ausgangszustand

Die Anforderung betrifft ausschließlich GitHub-Actions-Workflows und
Dokumentation — es existieren (und sind geplant) keine automatisierten
Tests für Workflow-Dateien. Nachfolgend der projektweite
Test-Ausgangszustand vor der Umsetzung sowie der Bestand an
Testklassen/-hilfen.

## Test-Ausgangszustand vor der Umsetzung

- **Zeitpunkt (mit Zeitzone):** 2026-11-15, ca. 00:09–00:22 Uhr +01:00
  (CET, lokale Rechnerzeit)
- **Branch und Commit-ID:** Branch
  `task/issue-95-afb7503d43a643f4a5b8716472913991-automatisierung-wiederkehrende`
  (Basis `staging`), Commit
  `0a8edbad9031e5f726477d343dae566a6b778bf9`
  („refactor: Download-Ablauf-Pass mit Auto-Download-Schutz ins Service kapseln (#94)")
- **Uncommittete Änderungen im getesteten Stand:** keine geänderten
  Dateien; lediglich das neue, untracked Verzeichnis
  `docs/features/task/issue-95-afb7503d43a643f4a5b8716472913991-automatisierung-wiederkehrende/`
  (Prozess-Artefakte `requirement.md`, `todo.md` sowie die im Lauf
  entstehenden Inventory-Dateien und Test-Nachweise). Der getestete
  Code-Stand entspricht damit vollständig dem genannten Commit.
- **Testumgebung und Runtime-/SDK-Versionen:** Windows, interaktive
  Konsolensitzung (Sitzung 1, `SESSIONNAME=Console`, `explorer.exe`
  aktiv); .NET SDK **10.0.403**; installierte Workloads `android`
  36.1.69, `ios` 26.5.10318, `maccatalyst` 26.5.10318,
  `maui-windows` 10.0.20 (jeweils SDK-Band 10.0.400); `gh` CLI 2.92.0;
  Testframework xunit.v3 3.2.2 mit `Microsoft.NET.Test.Sdk` 18.8.1.
- **Ermittelte Testsuiten und Quellen der Testbefehle:** Die
  projektübliche Prüfkette steht in `.github/workflows/pr-checks.yml`
  (Job `verify`): `dotnet restore` → `dotnet build
  VideoPlayer.Maui.Tests/VideoPlayer.Maui.Tests.csproj --no-restore
  --configuration Release` → `dotnet test … --no-build --configuration
  Release` → Lint `dotnet format whitespace --verify-no-changes`.
  Zusätzlich existiert die E2E-Suite `VideoPlayer.E2E.Tests` (FlaUI),
  dokumentiert in `docs/help/e2e-tests.md` mit Befehl `dotnet test
  VideoPlayer.E2E.Tests/VideoPlayer.E2E.Tests.csproj`; sie läuft ohne
  externe Infrastruktur (lokaler Mock-Server), benötigt aber eine
  Windows-Desktop-Session mit sichtbaren Fenstern.

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|---------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| 1 (Restore) | `dotnet restore VideoPlayer.App.sln` | Repo-Root | 0 | – | – | – | [01-restore.log](test-results/01-restore.log) |
| 2 (Build) | `dotnet build VideoPlayer.Maui.Tests/VideoPlayer.Maui.Tests.csproj --no-restore --configuration Release` | Repo-Root | 0 | – | – | – | [02-build-tests.log](test-results/02-build-tests.log) |
| 3 (Unit-Tests) | `dotnet test VideoPlayer.Maui.Tests/VideoPlayer.Maui.Tests.csproj --no-build --configuration Release` | Repo-Root | 0 | 1484 | 0 | 0 | [03-test-maui-tests.log](test-results/03-test-maui-tests.log) |
| 4 (E2E) | `dotnet test VideoPlayer.E2E.Tests/VideoPlayer.E2E.Tests.csproj` (Debug-Build der App erfolgt in `AppFixture` selbst) | Repo-Root | 0 | 74 | 0 | 0 | [04-test-e2e.log](test-results/04-test-e2e.log) |
| 5 (Lint) | `dotnet format whitespace --verify-no-changes --no-restore` | Repo-Root | 0 | – | – | – | [05-lint-format.log](test-results/05-lint-format.log) |

Hinweise zu den Läufen:

- Lauf 1 meldet die Warnung **NU1903** (bekannte Hoch-Severity-
  Sicherheitsanfälligkeit im transitiven Paket
  `Microsoft.AspNetCore.SignalR.Common` 10.0.10,
  [GHSA-334f-vvp4-7g6p](https://github.com/advisories/GHSA-334f-vvp4-7g6p))
  für vier Projekte — Warnung, kein Fehler; Details in
  [nuget.md](nuget.md).
- Lauf 4 (E2E): 74 Tests in 11 min 32 s, sequentiell
  (`[Collection("E2E")]`), jeder Test startete eine eigene
  `VideoPlayer.Maui.exe`-Instanz in der sichtbaren Konsolensitzung;
  die App-Prozesse wurden nach dem Lauf sauber aufgeräumt.
- Lauf 5 (Lint) ist Teil der CI-Prüfkette, aber kein Testlauf — als
  zusätzlicher Ausgangsnachweis mit aufgenommen; Ergebnis: keine
  Formatierungsabweichungen (nur eine unspezifische
  Arbeitsbereichs-Warnung beim Laden).

### Nachgewiesene bestehende Testfehler

Keine. Beide Testsuiten sind im Ausgangszustand vollständig grün
(1484 + 74 Tests, 0 Fehlschläge, 0 Übersprungen).

### Testlücken und Ausführungsprobleme

- **Keine nicht ausgeführten oder übersprungenen Tests.** Beide im
  Repository vorhandenen Suiten wurden vollständig ausgeführt.
- **Nicht lokal nachvollziehbare CI-Anteile (kein Testlauf, sondern
  Plattform-Grenzen):** die macOS-Jobs der CI (tvOS-Simulator-Build in
  `pr-checks.yml`, iOS-/tvOS-Builds in den Merge-Workflows) sowie die
  Release-/Package-Jobs können unter Windows nicht ausgeführt werden;
  sie sind für diese Bestandsaufnahme nicht relevant, da die
  Anforderung keinen App-Code betrifft.
- Die Anforderung selbst führt **keine testbaren Code-Artefakte** ein
  (Workflow-YAML); eine Verifikation der neuen Workflows ist nur über
  GitHub-Actions-Läufe möglich — dafür sieht die Anforderung Probeläufe
  vor (Offene Frage `workflow_dispatch` in `requirement.md`).

## Testklassen

### `VideoPlayer.Maui.Tests` (xunit.v3, `net10.0`, 131 Dateien, 1484 Tests)

Themengruppen (Benennung im Original):

- **MVU-Update-Tests:** `AppUpdateTests_*` (24 Dateien) — Zustands-
  übergänge des Update-Flusses je Feature-Bereich (Bootstrap,
  Connection, ContinueWatching, Downloads, Navigation, Offline,
  Pairing, Player, Playlists, Settings, Sources, Startup u. a.).
- **Contract-Tests:** `BootstrapContractTests`, `ClientGateTests`,
  `ConnectionWorkflowContractTests`, `ContinueWatchingContextMenuContractTests`,
  `DesignTokensContractTests`, `DevApiContractTests`,
  `DownloadsContractTests`, `FavoritesProfileContractTests`,
  `LogProtocolContractTests`, `PairingContractTests`,
  `SourcesContractTests`, `TvOsCompileContractTests` u. a. — pinnen
  XAML-/API-/Konventions-Verträge fest.
- **Service-Tests:** `AppLogServiceTests`, `AppSecretsServiceTests`,
  `AppSettingsServiceTests`, `AuthSessionServiceTests(-Refresh)`,
  `AutoDownloadServiceTests`, `ContinueWatchingMutationServiceTests`,
  `DeviceTokenServiceTests`, `DownloadServiceTests(_Expiry/Reconcile)`,
  `DownloadListSyncTests`, `FavoritesServiceTests`,
  `PlaylistServiceTests_Management` u. a.
- **Developer-API-Tests:** `DevApi*` (Router, HttpServer, Metadata,
  ModelSnapshot, RequestReader).
- **API-Client-Tests:** `VideoPlayerApiClientTests_*` (Authentication,
  Endpoints, PlaylistAdmin, Playlists, RetryPolicy, SessionRefresh,
  TokenRefresh).

### `VideoPlayer.E2E.Tests` (xunit.v3, `net10.0-windows`, 74 Tests, FlaUI/UIA3)

Testklassen in `VideoPlayer.E2E.Tests/Tests/` (je eine Datei, Basis
`E2ETestBase`): `BootstrapTests` (6), `ContinueWatchingContextMenuTests` (3),
`ContinueWatchingPlaybackTests` (3), `DownloadsTests` (3),
`LogProtocolTests` (7), `LoginTests` (2), `PairingTests` (3),
`PlaybackTests` (2), `PlaylistManagementTests` (11),
`PlaylistPlaybackTests` (6), `PlaylistProgressReportTests` (2),
`PlaylistsTests` (4), `SeasonalGenresTests` (6), `SeriesDetailsTests` (3),
`ServerSetupTests` (5), `ServerSwitchTests` (1), `SourceBrowsingTests` (3),
`SourcePagingTests` (1), `SourceSearchTests` (3).

## Hilfsmethoden

### `VideoPlayer.Maui.Tests/Stubs/`

- `FakeNetworkMonitor`, `InMemorySecretStore`, `InMemorySettingsStore`
  — In-Memory-Ersatz für Plattformdienste.
- `StubHttpMessageHandler` — HTTP-Stub für API-Client-Tests.
- `RecordingLoggerProvider`/`RecordedLogEntry` — Log-Aufzeichnung für
  Logger-Tests.
- `ManifestTestHelpers` — Testdaten für Download-Manifeste.
- `ClientGateCollection`, `SqliteDatabaseCollection` — xunit-
  Collections (geteilte bzw. isolierte Test-Infrastruktur).
- `RepositoryRoot` — Auflösung des Repo-Root-Pfads aus dem Test-Kontext.

### `VideoPlayer.E2E.Tests` (`AppFixture.cs`, `Support/`, `Tests/E2ETestBase.cs`)

- `AppFixture` — einmaliger Debug-Build von `VideoPlayer.Maui` für
  alle Tests (xunit Collection Fixture).
- `AppInstance` — startet die App-Exe mit isoliertem Profil
  (`VIDEOPLAYER_PROFILE=e2e-…`), wartet auf `devapi.json`, attacht
  FlaUI.UIA3, räumt Prozess/Profile in `Dispose` auf.
- `DevApiClient`/`DevApiModels` — Zugriff auf die Developer-API
  (`GET /model`, `GET /tree`, `POST /invoke`, `POST /set-text`).
- `MockVideoServer`/`MockPairingCrypto` — lokaler TCP-Mock-Server
  (Health, Pairing/Bootstrap inkl. ECDH+AES-GCM, Auth, Listen- und
  Stream-Endpunkte); keine externe Infrastruktur nötig.
- `UiDriver` — FlaUI-Hilfen für Fenster-/Elementzugriff.
- `E2ETestBase` — gemeinsame Basisklasse der E2E-Testklassen.
- `RepositoryRoot` — Pfadauflösung zum Repo-Root.
