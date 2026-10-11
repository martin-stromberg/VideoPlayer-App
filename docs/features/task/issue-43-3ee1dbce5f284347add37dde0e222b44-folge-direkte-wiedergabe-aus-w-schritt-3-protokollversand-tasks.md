# Tasks: Protokollierung und Protokollversand per Mail

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Voraussetzung | `Microsoft.Extensions.Logging.Abstractions` 10.0.10 in `VideoPlayer.Storage.csproj` referenzieren | Offen | — |
| 2 | Einstellung | `AppSettings.LoggingEnabled` (`bool`, Standard `false`) ergänzen | Offen | `AppSettingsTests.Defaults_AreExpected` |
| 3 | Einstellung | `AppSettingsService.LoggingEnabledKey` + `Load()`/`Save()`-Einträge | Offen | `AppSettingsServiceTests.Save_ThenLoad_RoundTrips` |
| 4 | Ablage | `AppStorageLayout.LogsDirectory` (`NoBackupDirectory/logs`) + `EnsureDirectoriesExist` | Offen | `AppStorageLayoutTests.EnsureDirectoriesExist_CreatesAll` |
| 5 | Logging | `AppLogService` anlegen (Enable/Disable, `LogFilePath`, `LogFiles`/`GetAttachmentPaths`, `Write`, Rotation) | Offen | `AppLogServiceTests` |
| 6 | Logging | `FileLoggerProvider` + `FileLogger` anlegen (`ILoggerProvider`/`ILogger`, Mindestlevel Information) | Offen | `FileLoggerTests` |
| 7 | MVU | `SendLogProtocolRequestedMessage` + `SendLogProtocolCompletedMessage(string? Error)` in `AppMessage` | Offen | `AppUpdateTests_LogProtocol` |
| 8 | MVU | `SendLogProtocolEffect` in `AppEffect` | Offen | `AppUpdateTests_LogProtocol` |
| 9 | MVU | `AppModel.LogSendError` (`string?`) | Offen | `AppUpdateTests_LogProtocol` |
| 10 | MVU | `AppUpdate`: Switch-Zweige + `UpdateSendLogProtocolRequested`/`UpdateSendLogProtocolCompleted` + Konstanten `LogProtocolEmptyError`/`LogSendFailedError` | Offen | `AppUpdateTests_LogProtocol` |
| 11 | UI | `SettingsPage.xaml`: Abschnitt „Protokoll" (`LoggingSwitch`, `SendLogSection`, `SendLogButton`, `SendLogErrorLabel`) | Offen | `LogProtocolContractTests` |
| 12 | UI | `SettingsPage.xaml.cs`: `OnLoggingToggled`, `OnSendLogClicked`, `PopulateFromModel`-Erweiterung | Offen | `LogProtocolContractTests` |
| 13 | Effekt-Handler | `ShellAppEffectHandler`: ctor-Parameter `AppLogService` + `ILogger<ShellAppEffectHandler>` | Offen | `LogProtocolContractTests` |
| 14 | Effekt-Handler | `SetEnabled`-Aufrufe in `LoadSettingsAsync`/`SaveSettings` | Offen | `LogProtocolContractTests` |
| 15 | Effekt-Handler | `SendLogProtocolAsync` (`Email.ComposeAsync` → `Share.RequestAsync`-Fallback → `SendLogProtocolCompletedMessage`, Hauptthread, Empfänger-Konstante) | Offen | `LogProtocolContractTests` |
| 16 | Effekt-Handler | `Debug.WriteLine`-Aufrufe (51×) durch `logger.LogError(ex, …)` ersetzen | Offen | `LogProtocolContractTests` |
| 17 | Registrierung | `MauiProgram`: `AppLogService` (Startwert aus `AppSettingsService.Load()`) + `ILoggerProvider`/`FileLoggerProvider` | Offen | `LogProtocolContractTests` |
| 18 | DevApi | `DevApiModelSnapshot` um `SettingsLoggingEnabled`/`LogSendError` erweitern | Offen | `DevApiModelSnapshotTests`/`LogProtocolContractTests` |
| 19 | DevApi | E2E-`DevApiModel` um `settingsLoggingEnabled`/`logSendError` erweitern | Offen | `LogProtocolTests` |
| 20 | Tests | `Microsoft.Extensions.Logging` 10.0.10 in `VideoWebPlayer.Maui.Tests.csproj` | Offen | `FileLoggerTests` |
| 21 | Tests | `AppLogServiceTests` neu | Offen | — |
| 22 | Tests | `FileLoggerTests` neu | Offen | — |
| 23 | Tests | `AppUpdateTests_LogProtocol` neu | Offen | — |
| 24 | Tests | `LogProtocolContractTests` neu | Offen | — |
| 25 | Tests | Bestehende Testklassen erweitern (`AppSettingsTests`, `AppSettingsServiceTests`, `AppStorageLayoutTests`) | Offen | — |
| 26 | E2E-Infrastruktur | `UiDriver.Toggle(automationId)` (TogglePattern, Fallback Invoke/Click) | Offen | `LogProtocolTests` |
| 27 | E2E-Infrastruktur | `AppInstance`: Profilpfad-Hilfe für Protokolldatei + `DeleteProfileDirectory` um `de.martinstromberg.videos`-Pfade erweitern | Offen | `LogProtocolTests` |
| 28 | E2E-Tests | `LogProtocolTests`: `EnableLogging_ShowsSendButton_AndCreatesLogFile` | Offen | — |
| 29 | E2E-Tests | `LogProtocolTests`: `LoggingDisabled_HidesSendButton_AndNoLogFile` | Offen | — |
| 30 | E2E-Tests | `LogProtocolTests`: `LoggingEnabled_PersistsAcrossRestart` | Offen | — |
| 31 | E2E-Tests | `LogProtocolTests`: `SendLog_Click_InvokesPlatformShareOrMail_WithoutCrash` | Offen | — |
