# Tests

## Test-Ausgangszustand vor der Umsetzung

- **Zeitpunkt (mit Zeitzone):** 2026-10-12, ~18:07–18:12 Uhr +02:00 (W. Europe Standard Time)
- **Branch und Commit-ID:** `task/0fe871d252fd4670b6b1427e123148b2-protokollierung`, Commit `0b607a632f4c35a4c4e6ff44135e3ad1d176d6bd`
- **Uncommittete Änderungen im getesteten Stand:** keine Code-Änderungen; lediglich das untracked Verzeichnis `docs/features/task/0fe871d252fd4670b6b1427e123148b2-protokollierung/` (Anforderungs- und Inventar-Artefakte). `git diff`/`git stash list` leer.
- **Testumgebung und Runtime-/SDK-Versionen:** Windows-Host, .NET SDK `10.0.401` (einzige installierte SDK-Version); Workloads `maui-windows` 10.0.20, `android`, `ios`, `maccatalyst` installiert. xUnit v3 (Pakete `xunit.v3` 3.2.2, `Microsoft.NET.Test.Sdk` 18.8.1, `coverlet.collector` 10.0.1).
- **Ermittelte Testsuiten und Quellen der Testbefehle:**
  - `VideoWebPlayer.Maui.Tests` (net10.0, xUnit v3) — CI-Testbefehl aus `.github/workflows/pr-checks.yml`: `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj --no-build --configuration Release` (hier mit Build statt `--no-build` ausgeführt, da kein vorausgehender Build existierte).
  - `VideoPlayer.E2E.Tests` (net10.0-windows, FlaUI.UIA3) — projektüblicher Aufruf laut `docs/features/task/issue-17-flaui-e2e/test-results.md`: `dotnet test VideoPlayer.E2E.Tests/VideoPlayer.E2E.Tests.csproj`. Nicht CI-Bestandteil (kein `dotnet test` auf dieses Projekt in `.github/workflows/`). `AppFixture` baut `VideoPlayer.Maui` einmalig als Debug-Build (`net10.0-windows10.0.19041.0`, bis zu 10 min) und startet die Exe pro Test sichtbar unter FlaUI-Steuerung (Collection `E2E`, sequentiell).

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| Unit-Baseline | `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj --configuration Release --logger "trx;LogFileName=unittest-baseline.trx" --results-directory docs/features/task/0fe871d252fd4670b6b1427e123148b2-protokollierung/inventory/test-results` | Repo-Root | 0 | 1257 | 0 | 0 | [Console-Log](test-results/unittest-baseline-console.txt), [TRX-Report](test-results/unittest-baseline.trx) |

### Nachgewiesene bestehende Testfehler

Keine — der Baseline-Lauf ist vollständig erfolgreich (1257/1257, TRX `outcome="Passed"` für alle Einträge). Damit existiert kein nachgewiesener „preexisting"-Fehlschlag; jeder später auftretende Fehlschlag ist zu untersuchen.

### Testlücken und Ausführungsprobleme

- **`VideoPlayer.E2E.Tests` nicht ausgeführt:** Die Suite baut `VideoPlayer.Maui` als Windows-Debug-Build und steuert die gestartete App sichtbar per FlaUI/UIA3 (Fensterfokus, Tastatur/Maus-Eingriffe auf dem Desktop, u. a. `Keyboard.Press(ESCAPE)` in `LogProtocolTests`). Ausführung nicht CI-üblich und auf diesem Host nicht gestartet — dokumentierte Lücke, keine Aussage über deren Zustand. Die Log-Tests der Suite (`Tests/LogProtocolTests.cs`, 4 Facts) verifizieren u. a., dass nach dem Aktivieren eine nicht leere Protokolldatei existiert (`WaitForLogFileAsync`, Länge > 0); sie prüfen **nicht**, ob das Protokoll über die Marker-Zeile hinaus Inhalt enthält — die aktuelle Funktionslücke wird damit nicht erkannt.
- **Keine weiteren Testlücken:** Keine übersprungenen oder deaktivierten Unit-Tests (0 skipped), keine Build-/Setup-Fehler im Baseline-Lauf (Build-Warnungen CS0108/CS8618 in `VideoWebPlayer.Client` sind vorhanden, aber nicht testrelevant).

## Testklassen

### `AppLogServiceTests`
Datei: `VideoWebPlayer.Maui.Tests/AppLogServiceTests.cs` (`IDisposable`, isoliertes Temp-Verzeichnis pro Testklasse)

- `Write_WhenDisabled_CreatesNoFile` — deaktiviert: keine Datei
- `Ctor_WhenEnabled_CreatesLogFileWithMarker` — Startmarker
- `SetEnabled_True_CreatesLogFileWithMarker` / `SetEnabled_False_StopsWriting` — Umschalten + Marker
- `Write_WhenEnabled_AppendsFormattedLine` — Format `[LEVEL] Kategorie: …`
- `Write_WithException_AppendsExceptionDetails` — Exception-Anhang
- `Write_OverMaxBytes_RotatesToPreviousFile` — 1-MiB-Rotation
- `GetAttachmentPaths_ReturnsExistingLogFiles` / `_WithoutFiles_ReturnsEmpty` — Versand-Dateiliste
- `LogFilePath_IsProfileScoped` — Profil-Isolation

### `FileLoggerTests`
Datei: `VideoWebPlayer.Maui.Tests/FileLoggerTests.cs` (`IDisposable`, Temp-Verzeichnis)

- `IsEnabled_RespectsMinimumLevel` — Trace/Debug/None aus, ab Information an
- `IsEnabled_WhenServiceDisabled_ReturnsFalse` — Schalter respektiert
- `Log_WritesThroughToLogFile` — Durchreichen an `AppLogService`
- `Log_BelowMinimumLevel_Skipped` — Filter
- `LoggerFactory_WithProvider_WritesToLogFile` — `LoggerFactory` + `FileLoggerProvider`-Integration

### `LogProtocolContractTests`
Datei: `VideoWebPlayer.Maui.Tests/LogProtocolContractTests.cs` (Quelltext-Contract-Tests über `RepositoryRoot.Find()` + `File.ReadAllText`, weil `VideoPlayer.Maui` nicht referenzierbar ist)

- `SettingsPage_Xaml_ContainsLoggingSection` — Schalter/Versand-UI vorhanden
- `SettingsPage_CodeBehind_WiresLoggingControls` — Toggled/SendLog-Handler verdrahtet
- `ShellAppEffectHandler_HandlesSendLogEffect_ViaEmailAndShareFallback` — Versand-Pfad gepinnt; enthält auch `DoesNotContain("System.Diagnostics.Debug.WriteLine")` für die Datei
- `MauiProgram_RegistersLogServices` — DI-Registrierung gepinnt
- `DevApiSnapshot_ExposesLoggingFields` — `settingsLoggingEnabled`/`logSendError` gepinnt
- `AppUpdate_GuardsSendLogProtocol` — Guard und Fehlertexte gepinnt

### `AppUpdateTests_LogProtocol`
Datei: `VideoWebPlayer.Maui.Tests/AppUpdateTests_LogProtocol.cs`

- `Update_SendLogRequested_WhenDisabled_IsUnchanged` — Guard ohne Protokollierung
- `Update_SendLogRequested_WhenEnabled_EmitsEffectAndClearsError` — `SendLogProtocolEffect` + Fehler-Reset
- `Update_SendLogCompleted_WithError_SetsLogSendError` / `_WithoutError_ClearsLogSendError`

### Dienst-Tests (für Konstruktor-Signaturen der MVU-Dienste relevant)

Die Dienste werden in Tests direkt mit Fakes erzeugt — ~39 `new …Service(…)`-Aufrufe. Betroffen bei Logger-Erweiterung: `AuthSessionServiceTests`, `AuthSessionServiceRefreshTests`, `ServerSyncServiceTests`, `ProgressReportServiceTests`, `ServerEventsServiceTests`, `DownloadServiceTests`, `AutoDownloadServiceTests`, `PlaybackSourceServiceTests`, `HomeListServiceTests`, `MediaDetailsServiceTests`, `OfflineContinueWatchingServiceTests`, `UpcomingEpisodesServiceTests`, `ContinueWatchingMutationServiceTests`, `DeviceTokenServiceTests`, `ProfileInfoServiceTests`, `PendingProgressSyncServiceTests`, `FavoritesServiceTests`, `SourceBrowserServiceTests`, `PlaylistServiceTests`(+`_Management`) — sämtlich zuletzt erfolgreich.

### `VideoPlayerApiClientTests_RetryPolicy` / `_TokenRefresh` / `_SessionRefresh` / `_Authentication`
Datei: `VideoWebPlayer.Maui.Tests/` — Wiederholungs-/401-Pfade des Clients, die den `Logger`-Haken ansprechen (Logging selbst wird dort nicht geprüft).

### `LogProtocolTests` (E2E)
Datei: `VideoPlayer.E2E.Tests/Tests/LogProtocolTests.cs` (Collection `E2E`, nicht im Baseline-Lauf)

- `LoggingDisabled_HidesSendButton_AndNoLogFile`
- `EnableLogging_ShowsSendButton_AndCreatesLogFile`
- `LoggingEnabled_PersistsAcrossRestart`
- `SendLog_Click_InvokesPlatformShareOrMail_WithoutCrash`

## Hilfsmethoden

### `Stubs` (`VideoWebPlayer.Maui.Tests/Stubs/`)

- `InMemorySecretStore` — In-Memory-`ISecretStore`-Fake (`Keys` zur Isolationsprüfung)
- `InMemorySettingsStore` — In-Memory-`ISettingsStore`-Fake
- `StubHttpMessageHandler` — aufzeichnender HTTP-Stub (Queue + `Responder`-Delegate, `Requests`-Liste mit `RecordedRequest`)
- `RepositoryRoot` — findet den Repo-Root für Contract-Tests
- `SqliteDatabaseCollection` — Collection-Definition (serielle DB-Tests, echte SQLite-Dateien)
- `ClientGateCollection` — Collection-Definition (serielle `ClientGate`-Tests)

### `VideoPlayer.E2E.Tests/Support/`

- `AppFixture`/`E2ECollectionDefinition` — baut die App einmalig (Debug, max. 10 min), liefert Exe-Pfad; sequentielle `E2E`-Collection
- `AppInstance` — startet/steuert eine App-Instanz (`Ui`, `DevApi`, `FindLogFile`, `Profile`, `Dispose(deleteProfile)`)
- `DevApiClient`/`DevApiModels` — Modell-Abfrage über die Developer-API (`GetModelAsync`, `WaitForModelAsync`)
- `UiDriver` — FlaUI-Helfer (`OpenFlyoutItem`, `Toggle`, `ClickButton`, `WaitForVisible`)
- `MockVideoServer`/`MockPairingCrypto` — simulierter Server inkl. Pairing
- `E2ETestBase` — gemeinsame Schritte (`StartAppAsync`, `ConnectToPairingRequiredAsync`, `PairAsync`, `LoginAsync`)
