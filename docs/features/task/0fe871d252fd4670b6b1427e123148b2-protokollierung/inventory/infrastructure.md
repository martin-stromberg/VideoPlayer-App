# Protokoll-Infrastruktur

Bestehende, laut Anforderung unverändert bleibende Infrastruktur des Datei-Protokolls. Alle Klassen liegen in `VideoPlayer.Storage` (Namespace `VideoPlayer.Storage`); die Registrierung erfolgt in `VideoPlayer.Maui/MauiProgram.cs`.

## `AppLogService`
Datei: `VideoPlayer.Storage/AppLogService.cs`

Schaltbarer Datei-Speicher des Programmprotokolls; schreibt zeitgestempelte Zeilen in `videoplayer.log` unterhalb von `AppStorageLayout.LogsDirectory` (nicht backup-pflichtig, profil-isoliert). Sämtliche Schreibfehler werden geschluckt — die Protokollierung darf die App niemals werfen lassen.

| Member | Typ / Sichtbarkeit | Beschreibung |
|--------|--------------------|--------------|
| `MaxLogFileBytes` | `public const long` | 1_048_576 (1 MiB) — Rotationsschwelle |
| `LogFileName` | `public const string` | `videoplayer.log` |
| `PreviousLogFileName` | `public const string` | `videoplayer.previous.log` |
| `AppLogService(AppStorageLayout, bool)` | `public` | Startzustand; bei `enabled=true` Marker `=== Protokollierung beim Start aktiv ===` |
| `IsEnabled` | `public bool` | Schaltzustand (Lock-geschützt) |
| `LogFilePath` / `PreviousLogFilePath` | `public string` | Pfade der aktuellen bzw. rotierten Protokolldatei |
| `SetEnabled(bool)` | `public void` | Umschalten; schreibt Marker `=== Protokollierung aktiviert ===` bzw. `=== Protokollierung deaktiviert ===` |
| `Write(LogLevel, string category, string message, Exception?)` | `public void` | Hängt Zeile `yyyy-MM-dd HH:mm:ss.fff [LEVEL] Kategorie: Meldung` an; rotiert vorher bei > 1 MiB; No-Op wenn deaktiviert |
| `GetAttachmentPaths()` | `public IReadOnlyList<string>` | Vorhandene, nicht leere Protokolldateien für den Mail-Versand (aktuelle zuerst) |
| `WriteMarker(string)` | `private void` | Marker-Zeile ohne Schaltprüfung |
| `RotateIfNeeded()` | `private void` | Move `videoplayer.log` → `videoplayer.previous.log` (Vorgänger wird gelöscht) |
| `LevelName(LogLevel)` | `private static string` | TRACE/DEBUG/INFO/WARN/ERROR/CRIT |

Wird aufgerufen von: `FileLogger.Log` (Schreiben), `ShellAppEffectHandler` (`SetEnabled` an Zeilen 1670/1684, `GetAttachmentPaths`/`IsEnabled`/`LogFilePath` in `SendLogProtocolAsync`), `MauiProgram` (Erzeugung).

## `FileLogger`
Datei: `VideoPlayer.Storage/FileLogger.cs`

`ILogger`-Implementierung, die formatierte Einträge an `AppLogService.Write` weiterreicht.

| Methode | Sichtbarkeit | Beschreibung |
|---------|--------------|--------------|
| `FileLogger(string category, AppLogService log)` | `public` | Kategorie + Zieldienst |
| `BeginScope<TState>(TState)` | `public` | Liefert `null` (keine Scopes) |
| `IsEnabled(LogLevel)` | `public` | `log.IsEnabled && logLevel >= Information && != None` — Mindestlevel-Filter als Rausch-Schutz |
| `Log<TState>(…)` | `public` | No-Op unterhalb des Levels; sonst `log.Write(level, category, formatter(state, exception), exception)` |

## `FileLoggerProvider`
Datei: `VideoPlayer.Storage/FileLoggerProvider.cs`

`ILoggerProvider`, der `FileLogger`-Instanzen erzeugt; in `MauiProgram` als `ILoggerProvider`-Singleton registriert.

| Methode | Sichtbarkeit | Beschreibung |
|---------|--------------|--------------|
| `FileLoggerProvider(AppLogService)` | `public` | Zieldienst (Schaltzustand und Schreibziel) |
| `CreateLogger(string categoryName)` | `public` | `new FileLogger(categoryName, log)` |
| `Dispose()` | `public` | Kein Zustand |

## `AppStorageLayout.LogsDirectory`
Datei: `VideoPlayer.Storage/AppStorageLayout.cs` (Zeile 78)

`LogsDirectory` = `Path.Combine(NoBackupDirectory, "logs")` — nicht backup-pflichtig, profil-isoliert via `Scoped()`. `EnsureDirectoriesExist()` (Zeile ~93) legt das Verzeichnis mit an.

## `AppSettings.LoggingEnabled`
Datei: `VideoPlayer.Storage/AppSettings.cs` (Zeile 57)

`public bool LoggingEnabled { get; init; }` — persistenter Benutzerschalter (Standard `false`), auf der `SettingsPage` als „Protokollierung"-Schalter gebunden (`LoggingSwitch`, `OnLoggingToggled` → `SaveSettingsRequestedMessage`).

## DI-Registrierung (`MauiProgram.CreateMauiApp`)
Datei: `VideoPlayer.Maui/MauiProgram.cs`

| Zeilen | Registrierung |
|--------|---------------|
| 71–74 | `AddSingleton(sp => new AppLogService(sp.GetRequiredService<AppStorageLayout>(), sp.GetRequiredService<AppSettingsService>().Load().LoggingEnabled))` — Startzustand synchron aus der Einstellung |
| 75–76 | `AddSingleton<ILoggerProvider>(sp => new FileLoggerProvider(sp.GetRequiredService<AppLogService>()))` — danach steht `ILogger<T>`/`ILoggerFactory` für die gesamte DI zur Verfügung |
| 174–176 | `#if DEBUG` → `builder.Logging.AddDebug()` — Debugger-Sicht, unabhängig vom Datei-Protokoll |

Alle von der Anforderung betroffenen Dienste sind bereits als Singletons registriert (u. a. `AuthSessionService` Z. 53, `ServerEventsService` Z. 89, `PendingProgressSyncService` Z. 90, `ProgressReportService` Z. 91, `ServerSyncService` Z. 92, `HomeListService` Z. 97, `OfflineContinueWatchingService` Z. 98, `PosterImageService` Z. 99, `SourceBrowserService` Z. 103, `MediaDetailsService` Z. 104, `FavoritesService` Z. 105, `PlaylistService` Z. 108, `ProfileInfoService` Z. 111, `PlaybackSourceService` Z. 116, `UpcomingEpisodesService` Z. 120, `ContinueWatchingMutationService` Z. 123, `DownloadService` Z. 129, `AutoDownloadService` Z. 130); Seiten als `AddTransient` (u. a. `PlayerPage` Z. 149, `SettingsPage` Z. 139, `ServerSetupPage` Z. 140). Eine zusätzliche Konstruktor-Abhängigkeit `ILogger<T>` wird von der MAUI-DI automatisch aufgelöst; `VideoPlayer.Mvu` nutzt `Microsoft.Extensions.Logging.Abstractions` bislang nur transitiv über `VideoPlayer.Storage`.
