# Detail: Logikklassen und Verdrahtung (Protokollierung)

## `AppLogService`
Datei: `VideoPlayer.Storage/AppLogService.cs`

| Methode / Member | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `AppLogService(AppStorageLayout, bool enabled)` | public | Startzustand; bei `enabled` schreibt `WriteMarker("=== Protokollierung beim Start aktiv ===")` |
| `IsEnabled` | public (Property) | Schaltzustand unter `lock (gate)` |
| `SetEnabled(bool)` | public | Laufzeit-Umschaltung; schreibt Marker `aktiviert`/`deaktiviert` |
| `Write(LogLevel, category, message, exception?)` | public | Hängt zeitgestempelte Zeile an `videoplayer.log`; rotiert ab 1 MiB; schluckt alle Schreibfehler; maskiert `access_token` |
| `GetAttachmentPaths()` | public | Dateien für den Versand (aktuelle + rotierte) |
| `MaskAccessToken(string)` | public static | Geheimnis-Maskierung |
| `WriteMarker` / `RotateIfNeeded` / `LevelName` | private | Hilfsfunktionen |

Wird aufgerufen von: `FileLogger` (Write), `ShellAppEffectHandler` (`SetEnabled` beim Laden/Speichern der Einstellungen, `GetAttachmentPaths` beim Versand).

## `FileLogger`
Datei: `VideoPlayer.Storage/FileLogger.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `IsEnabled(LogLevel)` | public | `log.IsEnabled && level >= Information && level != None` |
| `Log<TState>(...)` | public | `formatter` → `log.Write(...)` |
| `BeginScope` | public | `null` (keine Scopes) |

## `FileLoggerProvider`
Datei: `VideoPlayer.Storage/FileLoggerProvider.cs`

`ILoggerProvider`-Adapter: `CreateLogger(category)` → `new FileLogger(category, log)`; `Dispose` leer.

## `MauiProgram.CreateMauiApp()`
Datei: `VideoPlayer.Maui/MauiProgram.cs`

Registriert (`MauiProgram.cs`, Zeilen ~69–78):

```csharp
builder.Services.AddSingleton(sp =>
    new AppLogService(
        sp.GetRequiredService<AppStorageLayout>(),
        sp.GetRequiredService<AppSettingsService>().Load().LoggingEnabled));
builder.Services.AddSingleton<ILoggerProvider>(
    sp => new FileLoggerProvider(sp.GetRequiredService<AppLogService>()));
```

- `builder.Logging.AddDebug()` steht in `MauiProgram.cs` (~Zeile 178) unter `#if DEBUG` — einzige `builder.Logging`-Konfiguration.
- **Befund (empirisch verifiziert, Reproduktion `net10.0` + `Microsoft.Maui.Controls` 10.0.20):** `MauiApp.CreateBuilder()` registriert `ILoggerFactory`/`ILogger<>`/`ILogger` standardmäßig als `NullLoggerFactory`/`NullLogger<>`. Eine reine `AddSingleton<ILoggerProvider>`-Registrierung in `builder.Services` reicht **nicht**, um die echte `LoggerFactory` zu aktivieren — `IEnumerable<ILoggerProvider>` wird von der Null-Factory nie ausgewertet. Ohne `builder.Logging`-Konfiguration (bzw. explizites `AddLogging()`) landen alle `ILogger<T>`-Aufrufe im `NullLogger` → **kein einziger Facheintrag in `videoplayer.log`** (Release-Verhalten, da `AddDebug()` entfällt).
- Aktivierung gelingt nachweislich über `builder.Services.AddLogging()`, `builder.Logging.SetMinimumLevel(...)` oder `builder.Logging.AddFilter(...)` — danach liefert DI `LoggerFactory`/`Logger<T>` und `FileLogger` schreibt ins Protokoll.

## `App` (MAUI-Einstieg)
Datei: `VideoPlayer.Maui/App.xaml.cs`

| Methode | Kurzbeschreibung |
|---------|------------------|
| `App(MvuHost, IServiceProvider, ILogger<App>)` | DI-Konstruktor — `ILogger<App>` ist Pflichtparameter |
| `RunStartupAsync` | `logger.LogInformation("App-Start: Start-Workflow ausgelöst.")` vor `StartupRequestedMessage`-Dispatch — der früheste garantierte Facheintrag jedes Starts |

## `ShellAppEffectHandler`
Datei: `VideoPlayer.Maui/Services/ShellAppEffectHandler.cs`

| Stelle | Kurzbeschreibung |
|--------|------------------|
| Konstruktor | injiziert `AppLogService appLog` (Pflicht) + `ILogger<ShellAppEffectHandler>` |
| `LoadSettingsAsync()` (~Zeile 1746) | `appLog.SetEnabled(settings.LoggingEnabled)` nach Einstellungs-Laden |
| `SaveSettings(...)` (~Zeile 1765) | `appLog.SetEnabled(...)` nach erfolgreichem Speichern |
| Versand-Pfad (~Zeile 341, 380) | `appLog.GetAttachmentPaths()`/`LogFilePath` für `Email`/`Share` |

## Beobachtetes Fehlerbild (issue.md)

```
=== Protokollierung aktiviert ===
=== Protokollierung beim Start aktiv ===
=== Protokollierung beim Start aktiv ===
```

- Marker werden ausschließlich über `AppLogService.WriteMarker`/`SetEnabled` direkt geschrieben — ohne `ILogger`; sie belegen: Dateipfad korrekt, Schreibrechte vorhanden, Einstellung `LoggingEnabled = true`.
- Der doppelte Start-Marker deutet auf zwei `AppLogService`-Konstruktionen hin (zwei App-Starts oder doppelter `CreateMauiApp`-Aufruf — MAUI ruft den App-Builder plattformabhängig mehrfach auf); für den Fehler nicht ursächlich.
- Kein `INFO`-/`WARN`-/`ERROR`-Eintrag → der komplette `ILogger<T>`-Kanal (inkl. `App-Start`-Eintrag aus `App.RunStartupAsync`) ist in der installierten App tot — konsistent mit `NullLoggerFactory`.
