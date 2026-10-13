# Detail: Interfaces (Protokollierung)

## `Microsoft.Extensions.Logging.ILoggerProvider` (Framework)
Verwendungsort: `VideoPlayer.Maui/MauiProgram.cs`

| Aspekt | Ist-Zustand |
|--------|-------------|
| Registrierung | `builder.Services.AddSingleton<ILoggerProvider>(sp => new FileLoggerProvider(sp.GetRequiredService<AppLogService>()))` |
| Implementierung | `FileLoggerProvider` (`VideoPlayer.Storage`) |
| Wirksamkeit | **Nur bei aktivierter Logging-Pipeline:** MAUIs `MauiAppBuilder` liefert ohne `builder.Logging`-Konfiguration/`AddLogging()` eine `NullLoggerFactory`, die DI-registrierte `ILoggerProvider` ignoriert (empirisch verifiziert) |

## `Microsoft.Extensions.Logging.ILogger<T>` / `ILogger` (Framework)
Verwendungsort: injiziert in `App`, `AppShell`, Seiten, `ShellAppEffectHandler`, MAUI-Dienste (Pflichtparameter) und MVU-/Storage-Dienste (optionale Parameter, intern `NullLogger`)

| Aspekt | Ist-Zustand |
|--------|-------------|
| Auflösung im Debug | `Logger<T>` über echte `LoggerFactory` (aktiviert durch `builder.Logging.AddDebug()` unter `#if DEBUG`) |
| Auflösung im Release | `NullLogger<T>` über `NullLoggerFactory` — alle Aufrufe verworfen |
