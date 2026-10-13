# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan vollständig

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| Anwender-Aktionen und Abläufe erscheinen als Facheinträge in `videoplayer.log` (installierte/Release-App, nicht nur Marker) | `builder.Services.AddLogging()` in `MauiProgram.CreateMauiApp()` — aktiviert echte `LoggerFactory`, die `ILoggerProvider` (`FileLoggerProvider` → `AppLogService`) aus DI aufnimmt; Ursache empirisch verifiziert (`NullLoggerFactory` ohne `builder.Logging`-Konfiguration) | `LogProtocolContractTests.MauiProgram_ActivatesLoggingPipeline` (Contract-Pin) + bestehender E2E `LogProtocolTests.LoggingEnabled_RealFlows_WriteDiagnosticEntries` (`[INFO]`-Eintrag nach Login + Wiedergabe) | Abgedeckt |
| Bestehendes Protokollverhalten bleibt (Schalter, Marker, Rotation, Versand) | Keine Änderung an `AppLogService`, `FileLogger`, `ShellAppEffectHandler`; `LoggingEnabled` bleibt einziger Schalter | Bestehende Suiten: `AppLogServiceTests`, `FileLoggerTests`, `LogProtocolContractTests`, `LogProtocolTests` | Abgedeckt |

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| „Protokollierung aktiv → App nutzen (u. a. Video abspielen) → Protokolldatei enthält Ablauf-Einträge" | Bereits vorhanden: `LogProtocolTests.LoggingEnabled_RealFlows_WriteDiagnosticEntries` (Auto-Login + Wiedergabe → `[INFO]`-Eintrag, Geheimnis-Prüfung) — deckt den Fluss im Debug-Build | Abgedeckt |
| Release-spezifische Lücke (`NullLoggerFactory` ohne `AddDebug()`) | Nicht per E2E differenzierbar (identische UI/Abläufe, Unterschied ist `#if`-bedingte DI-Aktivierung); gesichert durch Contract-Pin `MauiProgram_ActivatesLoggingPipeline` | Nicht erforderlich mit Begründung |

## Hinweise

- Die Umsetzung ist bewusst minimal (eine Aktivierungszeile + Contract-Pin); die Reproduktion in der Bestandsaufnahme belegt sowohl Ursache als auch Wirksamkeit von `builder.Services.AddLogging()` (Factory-Typ `LoggerFactory`, `[INFO]`-Eintrag in Datei).
- `builder.Services.AddLogging()` kompiliert ohne zusätzliche usings (`ImplicitUsings` aktiv; `GetRequiredService` aus demselben Namespace `Microsoft.Extensions.DependencyInjection` bereits im Einsatz).
