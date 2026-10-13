# Detail: Tests (Protokollierung)

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-10-13, ~18:53–19:30 MESZ (UTC+2)
- Branch und Commit-ID: `task/2012a99a518d4416b7404b53139e6396-protokollierung` @ `863ce7f9d550c43ad9a1573cad1944d37110e6f3`
- Uncommittete Änderungen im getesteten Stand: nur neue Lifecycle-Artefakte unter `docs/features/task/2012a99a518d4416b7404b53139e6396-protokollierung/` — kein Produktivcode geändert
- Testumgebung und Runtime-/SDK-Versionen: Windows, .NET SDK 10.0.401; MAUI-Workloads android 36.1.69, ios 26.5.10318, maccatalyst 26.5.10318, maui-windows 10.0.20
- Ermittelte Testsuiten und Quellen der Testbefehle:
  - `VideoWebPlayer.Maui.Tests` (xUnit, net10.0) — Unit-/Contract-Tests; Befehl `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj`
  - `VideoPlayer.E2E.Tests` (xUnit + FlaUI, net10.0) — Windows-End-to-End; Befehl `dotnet test VideoPlayer.E2E.Tests/VideoPlayer.E2E.Tests.csproj` (baut Debug-App selbst, laut `docs/help/e2e-tests.md` ~20–25 min)

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| Unit-Baseline | `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj -c Release` | Repo-Wurzel | 0 | 1280 | 0 | 0 | [unit-tests-baseline.txt](test-results/unit-tests-baseline.txt) |
| E2E-Baseline | `dotnet test VideoPlayer.E2E.Tests/VideoPlayer.E2E.Tests.csproj` | Repo-Wurzel | siehe unten | – | – | – | [e2e-tests-baseline.txt](test-results/e2e-tests-baseline.txt) |

### Nachgewiesene bestehende Testfehler

| Test-ID inkl. Testfall | Suite / Dateipfad | Fehlerbild / Fehlermeldung | Lauf und Nachweis |
|-----------------------|------------------|---------------------------|-------------------|
| – | – | Keine Testfehler im Unit-Lauf nachgewiesen; E2E-Ergebnis siehe Tabelle oben | – |

### Testlücken und Ausführungsprobleme

- **Release-spezifischer Fehler ohne Release-Nachweis:** Der Defekt (aktivierte Protokollierung ohne `ILogger<T>`-Einträge) tritt nur in der installierten Release-App auf (`NullLoggerFactory`, weil `builder.Logging.AddDebug()` unter `#if DEBUG` steht). Unit- und E2E-Tests laufen gegen Debug-Builds und können den Pfad daher nicht abdecken — die Reproduktion erfolgte über ein eigenes `net10.0`-Konsolenprogramm mit `MauiApp.CreateBuilder()` (Factory-Typ und Dateiinhalt geprüft). Ein Contract-Pin auf die Aktivierung in `MauiProgram` ist als Absicherung vorgesehen.

## Testklassen

### `VideoWebPlayer.Maui.Tests` — `AppLogServiceTests`
- `Write_WhenDisabled_CreatesNoFile`, Toggle-Marker beider Richtungen, Rotation ab `MaxLogFileBytes`, `GetAttachmentPaths`, `MaskAccessToken` — Datei-Schreiber isoliert.

### `VideoWebPlayer.Maui.Tests` — `FileLoggerTests`
- `IsEnabled`-Gatter (Schalter + Mindestlevel), `Log` → `Write`-Weitergabe inkl. Exception; `LoggerFactory_WithProvider_WritesToLogFile` — `LoggerFactory.Create(builder.AddProvider(...))` schreibt `[ERROR]` ins Protokoll (prüft Provider↔Factory-Integration direkt, ohne DI).

### `VideoWebPlayer.Maui.Tests` — `LogProtocolContractTests`
- `MauiProgram_RegistersLogServices` — Quelltext-Pin der Registrierungen (`AddSingleton(sp =>`, `new AppLogService(`, `GetRequiredService<AppSettingsService>().Load().LoggingEnabled`, `AddSingleton<ILoggerProvider>`, `new FileLoggerProvider(sp.GetRequiredService<AppLogService>())`); **kein Pin auf die Logging-Aktivierung** — genau die Lücke des Fehlers.
- `NoDebugWriteLine_RemainsInAppSources`, Breadcrumb-Pins (Konnektivität, PiP, Wiedergabe-Fehlschlag, Abmeldung, Teil-Sync unter `Information`), `AccessTokenMasking_SharedBetweenPageAndSink`, Einstellungs-/UI-Verträge.

### `VideoWebPlayer.Maui.Tests` — `AppUpdateTests_LogProtocol`, `AppSettingsTests`, `AppSettingsServiceTests`, `SettingsStorageContractTests`, `DownloadServiceTests`
- Guard des Versands, Persistenz des Schalters, Download-Log-Stellen.

### `VideoPlayer.E2E.Tests` — `LogProtocolTests`
- `LoggingDisabled_HidesSendButton_AndNoLogFile`, `EnableLogging_ShowsSendButton_AndCreatesLogFile`, `LoggingEnabled_PersistsAcrossRestart`, `SendLog_Click_InvokesPlatformShareOrMail_WithoutCrash`, `LoggingEnabled_RealFlows_WriteDiagnosticEntries` (wartet auf `[INFO]`-Facheintrag nach Auto-Login + Wiedergabe — **der** Nachweis des `ILogger`-Pfads, nur Debug), weitere Teilfluss-Facts (PiP, Abmeldung, Wiedergabe-Fehlschlag als `[ERROR]`).

## Hilfsmethoden / Stubs

### `VideoWebPlayer.Maui.Tests/Stubs`
- `RecordedLogEntry`, `RecordingLoggerProvider` — zeichnen `ILogger`-Aufrufe für Service-Tests auf (DI-frei).
- `RecordingLogger`/`NullLogger`-Ersatz in Service-Konstruktoren (optionale `ILogger<T>?`-Parameter).
