# OsInterface-Audit (Ist-Stand)

Audit der Testdateien beider Projekte gegen die repo-angepasste Heuristik der Anforderung: echte Prozesse, UI-Automation, Netzwerk-Sockets/Listener, echte Dateisystem-Szenarien, eigene Poll-Zeitbudgets. **Ist-Stand: kein einziges `[Trait]`-Attribut im gesamten Testbestand** — die Kategorien `OsInterface`/`Flaky` existieren nicht; ein Meta-Test für die `Flaky`-nur-mit-`OsInterface`-Regel existiert ebenfalls nicht.

## `VideoPlayer.Maui.Tests` — klare Kandidaten

### Echte Netzwerk-Endpunkte

| Klasse | Datei | Befund |
|--------|-------|--------|
| `UdpServerDiscoveryTests` | `UdpServerDiscoveryTests.cs` | 3 von 8 Tests nutzen echte `UdpClient`-Sockets auf Loopback (`DiscoverAsync_*`, Zeilen 63–125); die 5 `ResolveAnnouncedAddress_*`-Tests sind rein in-process. Klassenmarkierung würde die puren Tests mitnehmen. |
| `DevApiHttpServerTests` | `DevApiHttpServerTests.cs` | Echter `DevApiHttpServer`-Listener auf `127.0.0.1` (OS-Port) + `HttpClient`/`TcpClient` (Zeilen 32, 51, 97). 5 Tests. **Nur unter `#if DEBUG \|\| DEV_API` kompiliert** — in der Release-CI-Lane nicht vorhanden. |
| `DevApiServerTests_Metadata` | `DevApiServerTests_Metadata.cs` | Echter `DevApiServer`-Listener + Schreiben/Lesen von `devapi.json` im Temp-Verzeichnis; `WaitForMetadataAsync` mit eigenem Poll-Budget (100 × 20 ms). 3 Tests. Ebenfalls `#if DEBUG \|\| DEV_API`. |
| `DevApiMetadataTests` | `DevApiMetadataTests.cs` | Schreibt `devapi-{guid}.json` unter `Path.GetTempPath()` (kein Listener). `#if DEBUG \|\| DEV_API`. |

### Echter Prozessstart / Umgebungsabhängigkeit

| Klasse | Datei | Befund |
|--------|-------|--------|
| `IosDeploymentResponseFileTests` | `IosDeploymentResponseFileTests.cs` | `Process.Start("pwsh", …)` für `pwsh`-Verfügbarkeitsprüfung und Ausführung generierter `.ps1`-Hilfsskripte aus dem Temp-Verzeichnis (Zeilen 137–187); `Assert.SkipUnless(PwshAvailable(), …)` — umgebungsabhängig per Definition. |

### Echtes Dateisystem (Temp-Verzeichnisse)

23 Dateien legen eigene Wurzeln unter `Path.GetTempPath()` an; alle räumen per `Directory.Delete(root, recursive: true)` im Dispose (ohne Retry) — dieselbe Flake-Klasse wie `DeleteProfileDirectory`:

| Klasse | Besonderheit |
|--------|--------------|
| `LocalDatabaseTests` | Reale SQLite-Dateien (sqlite-net-pcl + bundle_green); `[Collection("SqliteDatabase")]`; `ResetPool()` im Dispose. |
| `FileLoggerTests` | Reale Logdatei-Schreibzugriffe; **keine** Collection — läuft parallel zu anderen Klassen. |
| `AppLogServiceTests` | Temp-Logablage; keine Collection. |
| `AppStorageLayoutTests` | Temp-Verzeichnislayout; keine Collection. |
| `AutoDownloadServiceTests` | Temp-Root + `SqliteDatabase`-Collection + eigener Poll-Helper (`deadline` + `Task.Delay(50)`, Zeilen 135–139). |
| `UpcomingEpisodesServiceTests` | Temp-Root + `SqliteDatabase`-Collection + `Thread.Sleep(100)`-Poll (Zeile 67). |
| `ServerSyncServiceTests` | Temp-Root + `SqliteDatabase`-Collection. |
| `SeasonalGenreTests` | Temp-Root + `SqliteDatabase`-Collection. |
| `ProgressReportServiceTests` | Temp-Root + `SqliteDatabase`-Collection. |
| `PlaybackSourceServiceTests` | Temp-Root + `SqliteDatabase`-Collection. |
| `PendingProgressSyncServiceTests` | Temp-Root + `SqliteDatabase`-Collection. |
| `OfflineContinueWatchingServiceTests` | Temp-Root + `SqliteDatabase`-Collection. |
| `MediaDetailsServiceTests` | Temp-Root + `SqliteDatabase`-Collection. |
| `KnownServersServiceTests` | Temp-Root + `SqliteDatabase`-Collection + `Task.Delay(5)` für Zeitstempel-Differenzierung (Zeilen 84, 99). |
| `HomeListServiceTests` | Temp-Root + `SqliteDatabase`-Collection. |
| `FavoritesServiceTests` | Temp-Root + `SqliteDatabase`-Collection. |
| `DownloadServiceTests` | Temp-Root + `SqliteDatabase`-Collection. |
| `DownloadServiceTests_Expiry` | Temp-Root + `SqliteDatabase`-Collection + `Thread.Sleep(100)`-Poll (Zeile 64). |
| `DownloadServiceReconcileTests` | Temp-Root + `SqliteDatabase`-Collection + eigene Deadline-Schleifen (15 s + `Task.Delay(50)`, Zeilen 240–246; `Thread.Sleep(100)`, Zeile 67). |
| `ContinueWatchingMutationServiceTests` | Temp-Root + `SqliteDatabase`-Collection. |
| `DevApiServerTests_Metadata`, `DevApiMetadataTests`, `IosDeploymentResponseFileTests` | siehe oben (Netzwerk/Prozess + Dateisystem). |

### Grenzfälle / voraussichtlich nicht markierungswürdig

- **Contract-Tests** (31 Dateien mit `Stubs/RepositoryRoot.Find()`): lesen nur Repo-Quelldateien — hermetisch nach Checkout, i. d. R. kein `OsInterface`.
- `DevApiRouterTests`, `DevApiRouterTests_MapModel`, `DevApiRequestReaderTests`, `DevApiModelSnapshotTests`: rein in-process (Router/In-Memory-Streams), nur `#if`-gegatet.
- `ClientGate`-Collection (`ClientGateTests`, `DeviceTokenServiceTests`, `AuthSessionServiceTests`, `AuthSessionServiceRefreshTests`): serialisiert wegen statischem `ClientGate.DeviceTokenProvider` — Threading-, nicht Umgebungssensitivität.
- `AppSecretsServiceTests`, `AppSettingsServiceTests`, `AppProfileTests`, `AppSettingsTests`: `InMemorySettingsStore`/`InMemorySecretStore`-Stubs, kein echtes Dateisystem.
- `StubHttpMessageHandler`-basierte Service-Tests (große Mehrheit): HTTP wird in-process simuliert.

## `VideoPlayer.E2E.Tests` — gesamte Suite

Alle 19 Testklassen unter `Tests/` (74 `[Fact]`/`[Theory]`; dazu die abstrakte Basisklasse `E2ETestBase`) laufen über `E2ETestBase` in der sequentiellen `E2E`-Collection und sind per Fixture umgebungssensitiv:

- `Process.Start` der echten `VideoPlayer.Maui.exe` (`AppInstance`), `dotnet build`-Kindprozess (`AppFixture`)
- FlaUI/UIA3-Automation inkl. echter Tastatur-/Maus-Fallbacks (`UiDriver.GoBack` Alt+Links, `ScrollToEnd` Mausrad)
- `TcpListener` auf Loopback (`MockVideoServer`), HTTP-Loopback (`DevApiClient`)
- Reales Dateisystem: Profil-Verzeichnisse unter `%APPDATA%`/`%LOCALAPPDATA%`, Prozesslogs unter `.work/e2e-process-logs/`, `devapi.json` im Exe-Verzeichnis, `Assets/testvideo.mp4`
- Eigene Poll-Budgets in Testdateien: `LogProtocolTests.WaitForLogContentAsync` (20 s), `SourceSearchTests` (15/30 s), `SourcePagingTests` (10 s), `SourceBrowsingTests` (15 s), `PlaylistsTests` (10 s), `PlaylistPlaybackTests` (15 s), `DownloadsTests` (15/30 s), `SeriesDetailsTests` (30 s + `Task.Delay(5 s)`), `SeasonalGenresTests` (30–45 s via `WaitForModelAsync`)

Testklassen: `BootstrapTests`, `ContinueWatchingContextMenuTests`, `ContinueWatchingPlaybackTests`, `DownloadsTests`, `LogProtocolTests`, `LoginTests`, `PairingTests`, `PlaybackTests`, `PlaylistManagementTests`, `PlaylistPlaybackTests`, `PlaylistProgressReportTests`, `PlaylistsTests`, `SeasonalGenresTests`, `SeriesDetailsTests`, `ServerSetupTests`, `ServerSwitchTests`, `SourceBrowsingTests`, `SourcePagingTests`, `SourceSearchTests` — plus die abstrakte Basisklasse `E2ETestBase`.

**Konsequenz:** Klassenmarkierung auf `E2ETestBase`-Ebene bzw. Assembly-weiter Konvention naheliegend (Trait-Vererbung auf `[Collection]`/Basisklasse prüfen); die Suite läuft aktuell ohnehin nicht in CI.

## Nicht vorhandene Vorlagen-Heuristiken (bestätigt)

- Kein ConPTY/`PseudoConsole`-Code im Repo.
- Keine NTFS-Junction-/Reparse-Point-Nutzung in den Tests.
