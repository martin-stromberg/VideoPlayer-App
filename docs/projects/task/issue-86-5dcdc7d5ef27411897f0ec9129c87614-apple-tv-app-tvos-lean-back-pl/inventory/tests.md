# Tests — Bestandsaufnahme

## Ausgangszustand

- **Zeitpunkt der Testläufe:** 2026-11-05, 07:07–08:30 UTC (08:07–09:30 +0100, Windows lokal)
- **Branch:** `task/issue-86-5dcdc7d5ef27411897f0ec9129c87614-apple-tv-app-tvos-lean-back-pl`
- **Commit:** `f16d68a422c7da72dfcdb6326709b8d552f5f6f1` — „feat: Downloadbereinigung mit Ablaufdatum, Cache-Herkunft und Wiedergabeschutz (#85)" (2026-10-27)
- **Uncommittete Änderungen:** nur das neue Verzeichnis `docs/projects/task/issue-86-…/` (diese Bestandsaufnahme); keine Code-Änderungen
- **Umgebung:** Windows (interaktive Sitzung, `Environment.UserInteractive = True`), .NET SDK 10.0.403; Workloads: `android`, `ios` 26.5.10318/10.0.100, `maccatalyst`, `maui-windows` 10.0.20/10.0.100 — **keine tvOS-Workload**
- **Ermittelte Testsuiten (aus `README.md`, `pr-checks.yml`, csproj):**
  - `VideoPlayer.Maui.Tests` — xunit.v3, `net10.0`, Unit-/Contract-Tests der plattformneutralen Projekte (`Mvu`, `Storage`, `ApiClient`, `msTools.DeveloperApi`) plus XAML-/Skript-Contract-Tests; ~1244 `[Fact]`/`[Theory]`-Attribute (1440 ausgeführte Tests inkl. Theory-Datenzeilen). CI-Befehl: `dotnet test VideoPlayer.Maui.Tests/VideoPlayer.Maui.Tests.csproj`.
  - `VideoPlayer.E2E.Tests` — xunit.v3, `net10.0-windows`, FlaUI.UIA3-Windows-UI-Tests; `AppFixture` baut `VideoPlayer.Maui` selbständig als Debug-Build (`net10.0-windows10.0.19041.0`, ~10-Min-Timeout) und startet die Exe mit lokalem `MockVideoServer` + Developer-API; sequentiell (`E2E`-Collection). ~74 `[Fact]`/`[Theory]`-Attribute.

## Testläufe

| Lauf | Befehl | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|---|---|---|---|---|---|---|---|
| Unit/Contract | `dotnet test VideoPlayer.Maui.Tests/VideoPlayer.Maui.Tests.csproj --nologo` | Repo-Root | 0 | 1440 | 0 | 0 | [maui-tests-dotnet-test.log](test-results/maui-tests-dotnet-test.log) |
| E2E (FlaUI) | `dotnet test VideoPlayer.E2E.Tests/VideoPlayer.E2E.Tests.csproj --nologo` | Repo-Root | 1 | 71 | 3 | 0 | [e2e-tests-dotnet-test.log](test-results/e2e-tests-dotnet-test.log) |

## Nachgewiesene Testfehler

| Test | Suite/Datei | Fehlermeldung (Kurzform) | Nachweis |
|---|---|---|---|
| `VideoPlayer.E2E.Tests.Tests.PlaylistPlaybackTests.StreamFailure_AdvancesToNextPlaylistEntry` | `VideoPlayer.E2E.Tests/Tests/PlaylistPlaybackTests.cs` | `System.AggregateException`: App-Prozess unerwartet beendet, Exit `-1073741189` (`0xC000027B`); Prozess-Logs unter `.work/e2e-process-logs/20261105-073024709-27608` | [Log](test-results/e2e-tests-dotnet-test.log) |
| `VideoPlayer.E2E.Tests.Tests.SourceSearchTests.SubmitViaEnter_FiltersFromFirstPage` | `VideoPlayer.E2E.Tests/Tests/SourceSearchTests.cs` (Zeile 81) | `System.TimeoutException` in `DevApiClient.WaitForModelAsync` (Zeile 138): Suche „Komöd" abgeschickt → gefilterte Trefferliste ab Seite 0 nicht erreicht; Modell zeigt `sourceBrowserSearchText:""`, Einträge ungefiltert | [Log](test-results/e2e-tests-dotnet-test.log) |
| `VideoPlayer.E2E.Tests.Tests.SourceSearchTests.SubmittedText_RemainsVisibleAfterReturn` | `VideoPlayer.E2E.Tests/Tests/SourceSearchTests.cs` (Zeile 114) | `System.TimeoutException` in `DevApiClient.WaitForModelAsync` (Zeile 138): Suche „Komöd" abgeschickt nicht erreicht; selbes Symptom | [Log](test-results/e2e-tests-dotnet-test.log) |

Einordnung (reine Beobachtung, keine Diagnose): Der Absturz `0xC000027B` (`STOWED_EXCEPTION`) betrifft den Playlist-Fehler-Rückfall; die beiden `SourceSearchTests`-Timeouts deuten auf ein UI-Eingabeproblem unter FlaUI hin (Enter-/Text-Übermittlung kam im Modell nicht an) — mögliche Umgebungs-/Timing-Abhängigkeit der UI-Automatisierung kann nicht ausgeschlossen werden; die Einheitstests desselben Suchpfads sind grün.

## Testlücken / Ausführungsprobleme

- **Keine tvOS-spezifischen Tests:** es existiert weder ein tvOS-Projekt noch eine tvOS-Testinfrastruktur (kein `net…-tvos`-TFM, keine tvOS-Workload auf diesem Rechner, kein UI-Test-Framework für tvOS). Offene Frage in `requirement.md`: Nutzung von `msTools.DeveloperApi` unter tvOS für UI-Automatisierung.
- **Abdeckung beachten:** Die Unit-Suite heißt `VideoPlayer.Maui.Tests`, prüft aber großteils die plattformneutrale Logik (`AppUpdate`-Transitionen, Services, Storage, ApiClient, Developer-API) — sie ist ohne MAUI-Runtime ausführbar und damit für ein tvOS-Projekt direkt wiederverwendbar, sofern die Test-Projektreferenzen auf die `net10.0`-Bibliotheken bleiben.
- **E2E-Suite ist Windows-only:** `net10.0-windows` + FlaUI UIA3; kein Pendant für iOS/Android/tvOS vorhanden.

## Bestehende Testklassen / Hilfsmethoden für den Anforderungsbereich

### Test-Stubs (`VideoPlayer.Maui.Tests/Stubs/`)
`InMemorySettingsStore` (`ISettingsStore`), `InMemorySecretStore` (`ISecretStore`), `FakeNetworkMonitor` (`INetworkStatusMonitor`), `StubHttpMessageHandler`, `RecordingLoggerProvider` + `RecordedLogEntry`, `ManifestTestHelpers` (Manifest-/Datei-Helfer für Download-Tests), `RepositoryRoot` (Repo-Pfad für Contract-Tests), `ClientGateCollection` + `SqliteDatabaseCollection` (xUnit-Collections: statischer `ClientGate.DeviceTokenProvider`-Zustand bzw. geteilte SQLite-Dateien — Serialisierung).

### Fachlich relevante Testdateien (Auswahl nach Anforderungsbereich)

- **MVU-Update-Transitionen (`AppUpdateTests_*`):** `Startup`, `Connection`, `Navigation`, `Pairing`, `Bootstrap`, `ServerManagement`, `HomeLists`, `HomeHero`, `Sources`, `MediaDetails`, `DetailsHeader`, `FavoritesProfile`, `Playlists`, `PlaylistManagement`, `PlaylistPlayback`, `Player`, `Downloads`, `NetworkSync`, `Offline`, `ContinueWatching`, `ContinueWatchingActions`, `LogProtocol`, `Settings`.
- **Downloads:** `DownloadServiceTests`, `DownloadServiceTests_Expiry` (Ablaufdatum/Bereinigung), `DownloadServiceReconcileTests` (missing_file/Orphans/Teildateien/Verlängerung), `AutoDownloadServiceTests`, `DownloadListSyncTests`, `DownloadsContractTests`, `OfflineContinueWatchingServiceTests`.
- **Wiedergabe/Fortschritt:** `ProgressReportServiceTests`, `PendingProgressSyncServiceTests`, `PlaybackSourceServiceTests`, `UpcomingEpisodesServiceTests`, `PlayerContractTests`, `ContinueWatchingMutationServiceTests`.
- **Pairing/Discovery/Session:** `DevicePairingClientTests`, `DevicePairingClientBootstrapTests`, `PairingCryptoTests`, `PairingCodeTests`, `PairingContractTests`, `BootstrapTicketParserTests`, `BootstrapContractTests`, `ClientGateTests`, `DeviceTokenServiceTests`, `AuthSessionServiceTests`, `AuthSessionServiceRefreshTests`, `ServerDiscoveryServiceTests`, `UdpServerDiscoveryTests`, `DiscoveryProtocolTests`, `ConnectionWorkflowContractTests`.
- **Storage/Einstellungen:** `AppSettingsTests`, `AppSettingsServiceTests`, `AppSecretsServiceTests`, `KnownServersServiceTests`, `ServerAddressTests`, `AppStorageLayoutTests`, `AppProfileTests`, `LocalDatabaseTests`, `SettingsStorageContractTests`, `AppLogServiceTests`, `FileLoggerTests`.
- **Playlists:** `PlaylistServiceTests`, `PlaylistServiceTests_Management`, `VideoPlayerApiClientTests_Playlists`, `VideoPlayerApiClientTests_PlaylistAdmin`.
- **Sonstige:** `VideoPlayerApiClientTests_*` (Authentication, Endpoints, RetryPolicy, SessionRefresh, TokenRefresh), `HomeListServiceTests`, `SourceBrowserServiceTests`, `SourceItemListSyncTests`, `SourceOverviewSearchContractTests`, `MediaDetailsServiceTests`, `MediaEntrySerializationTests`, `SeasonalGenreTests`, `EnumCoverageTests`, `MvuHostTests`, `DevApi*Tests` (HttpServer, Router, Router_MapModel, ModelSnapshot, Metadata, RequestReader, Contract), `IosDeploymentResponseFileTests`, `VideoPlayerMauiContractTests`, `PageLifecycleContractTests`, `DesignTokensContractTests`, `HomePageContractTests`, `MediaDetailsPageContractTests`, `ServerSetupContractTests`, `SourcesContractTests`, `FavoritesProfileContractTests`, `OfflineRecoveryContractTests`, `NetworkSyncContractTests`, `ContinueWatchingContextMenuContractTests`, `LogProtocolContractTests`.

### E2E-Testklassen (`VideoPlayer.E2E.Tests/Tests/`)
`Bootstrap`, `Login`, `Pairing`, `ServerSetup`, `ServerSwitch`, `SourceBrowsing`, `SourcePaging`, `SourceSearch`, `SeasonalGenres`, `SeriesDetails`, `ContinueWatchingPlayback`, `ContinueWatchingContextMenu`, `Playback`, `Playlists`, `PlaylistManagement`, `PlaylistPlayback`, `PlaylistProgressReport`, `Downloads`, `LogProtocol` (Basis `E2ETestBase`, Infrastruktur `AppFixture`, `AppInstance`, `UiDriver`, `DevApiClient`, `MockVideoServer`, `MockPairingCrypto`).
