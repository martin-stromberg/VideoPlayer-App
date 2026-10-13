# Umsetzungsplan: Protokollierung — `ILogger`-Pipeline in der Release-App aktivieren

## Übersicht

Die installierte App schreibt nur Marker-Zeilen ins Protokoll, weil `MauiProgram.CreateMauiApp()` die `Microsoft.Extensions.Logging`-Pipeline nie explizit aktiviert: `MauiAppBuilder` liefert ohne `builder.Logging`-Konfiguration eine `NullLoggerFactory`, die DI-registrierte `ILoggerProvider` ignoriert. Im Debug übernimmt `builder.Logging.AddDebug()` die Aktivierung — im Release läuft sie ins Leere. Umgesetzt wird eine einzige, konfigurationsfreie Aktivierung der Pipeline in `MauiProgram` plus ein Contract-Pin in `LogProtocolContractTests`.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Aktivierung der Logging-Pipeline | `builder.Services.AddLogging()` in `CreateMauiApp()` | Registriert die echte `LoggerFactory` (TryAdd) und schlägt damit die `NullLoggerFactory`-Vorgabe — verifiziertes Verhalten. Neutrale Aktivierung ohne Nebeneffekte: `builder.Logging.SetMinimumLevel(...)`/`AddFilter(...)` würden zusätzlich globale Filter setzen und den Debug-Provider beeinflussen; `builder.Logging.AddProvider(instanz)` erfordert eine eagre Instanz und würde `AppLogService` vom DI-Singleton (Laufzeit-Umschaltung via `SetEnabled`) entkoppeln. |
| Platzierung | Unmittelbar vor/mit der `ILoggerProvider`-Registrierung im Abschnitt „Programmprotokoll" | Zusammengehöriger Kommentar dokumentiert die `NullLoggerFactory`-Falle, damit die Zeile nicht als redundant wahrgenommen und entfernt wird. |

## Programmabläufe

### Protokollierung im Release (Zielzustand)

1. `CreateMauiApp()` registriert über `builder.Services.AddLogging()` die echte `LoggerFactory` und über `AddSingleton<ILoggerProvider>` den `FileLoggerProvider`.
2. Beim ersten `ILogger<T>`-Resolve baut DI die `LoggerFactory` mit `IEnumerable<ILoggerProvider>` → `FileLoggerProvider` → `AppLogService` (Start-Schaltzustand aus `AppSettingsService.Load()`).
3. `ILogger<T>`-Aufrufe (z. B. `App.RunStartupAsync` → „App-Start: Start-Workflow ausgelöst.") laufen über `Logger` → `FileLogger` (Gatter: `IsEnabled` + `>= Information`) → `AppLogService.Write` → `videoplayer.log`.
4. Schalter bleibt unverändert: `ShellAppEffectHandler` → `AppLogService.SetEnabled`.

Beteiligte Klassen: `MauiProgram`, `AppLogService`, `FileLoggerProvider`, `FileLogger`, `LoggerFactory` (Framework).

## Neue Klassen

Keine.

## Änderungen an bestehenden Klassen

### `MauiProgram` (static class, `VideoPlayer.Maui/MauiProgram.cs`)

- **Geänderte Methoden:** `CreateMauiApp()` — zusätzlicher Aufruf `builder.Services.AddLogging()` im Programmprotokoll-Abschnitt mit erklärendem Kommentar (MAUI-`NullLoggerFactory`-Vorgabe wird überschrieben; ohne die Zeile nimmt DI den `ILoggerProvider` nie auf).

### `LogProtocolContractTests` (`VideoWebPlayer.Maui.Tests`)

- **Neue Methoden:** `MauiProgram_ActivatesLoggingPipeline` — Quelltext-Pin, der die Aktivierungszeile in `MauiProgram.cs` verlangt (Muster wie `MauiProgram_RegistersLogServices`).

## Datenbankmigrationen

Keine.

## Validierungsregeln

Keine.

## Konfigurationsänderungen

Keine — `AppSettings.LoggingEnabled` bleibt der einzige Schalter (unverändert).

## Seiteneffekte und Risiken

- **Debug-Verhalten:** `builder.Services.AddLogging()` registriert `ILoggerFactory` via `TryAdd` und ändert den Debug-Pfad nicht — `AddDebug()` liefert weiterhin den `DebugLoggerProvider` zusätzlich zum `FileLoggerProvider` (empirisch verifiziert: beide Provider aktiv, Einträge fließen).
- **Doppelter `CreateMauiApp`-Aufruf (plattformabhängig):** Jeder gebaute `MauiApp`-Provider instantiiert `AppLogService` einmal → ein Start-Marker je Instanz. Mit der Aktivierung schreiben beide Pfade ins selbe Protokoll; kein zusätzliches Risiko (Append-Modus, `lock` im Dienst).
- **Framework-Rauschen:** `FileLogger` filtert weiterhin unter `Information`; die Aktivierung ändert keine Mindestlevel — Eintragsvolumen unverändert.
- **Testbarkeit des Release-Pfads:** Unit-/E2E-Tests laufen gegen Debug-Builds; der Release-Fehler ist dort strukturell nicht reproduzierbar. Nachweis daher über den Contract-Pin plus den in der Bestandsaufnahme dokumentierten Reproduktionsnachweis (`MauiAppBuilder` ohne Logging-Konfiguration → `NullLoggerFactory`).

## Umsetzungsreihenfolge

1. **`builder.Services.AddLogging()` in `CreateMauiApp()` ergänzen**
   - Voraussetzungen: Keine (Namespace `Microsoft.Extensions.Logging`/`Microsoft.Extensions.DependencyInjection` bereits importiert bzw. über SDK implizit verfügbar — `AddLogging` ist in `Microsoft.Extensions.Logging` unter `Microsoft.Extensions.DependencyInjection`).
   - Beschreibung: Einzeilige Aktivierung im Programmprotokoll-Block von `MauiProgram.cs` inkl. Kommentar, der den `NullLoggerFactory`-Hintergrund erklärt.

2. **Contract-Pin `MauiProgram_ActivatesLoggingPipeline` in `LogProtocolContractTests` ergänzen**
   - Voraussetzungen: Schritt 1 (der Pin prüft den eingefügten Aufruf).
   - Beschreibung: Neuer `[Fact]` nach dem Muster von `MauiProgram_RegistersLogServices` (`ReadFile("VideoPlayer.Maui", "MauiProgram.cs")` + `Assert.Contains("AddLogging()", ...)` im Programmprotokoll-Kontext).

3. **Testsuiten laufen lassen**
   - Voraussetzungen: Schritte 1–2.
   - Beschreibung: `dotnet test VideoWebPlayer.Maui.Tests` (1280 + 1 Tests) und `dotnet test VideoPlayer.E2E.Tests` (u. a. `LoggingEnabled_RealFlows_WriteDiagnosticEntries` — bleibt der Debug-Nachweis des `ILogger`-Pfads).

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `MauiProgram_ActivatesLoggingPipeline` | `LogProtocolContractTests` | `MauiProgram.cs` aktiviert die Logging-Pipeline explizit (`builder.Services.AddLogging()`), damit DI-registrierte `ILoggerProvider` in Release aufgenommen werden — pinnt genau die Fehlerlücke. |

### Betroffene bestehende Tests

Keine.

### E2E-Tests (primärer Funktionsnachweis)

Die Anforderung enthält **keinen neuen oder geänderten Benutzerfluss**: Keine UI-Änderung, kein neuer Schalter, kein geändertes Verhalten an Bedienelementen — der Defekt liegt in der DI-Aktivierung, die für den Benutzer unsichtbar ist. Der primäre Funktionsnachweis des `ILogger`-Pfads existiert bereits als E2E-Fact `LogProtocolTests.LoggingEnabled_RealFlows_WriteDiagnosticEntries` (reale Anmeldung + Wiedergabe → `[INFO]`-Eintrag in `videoplayer.log`); er deckt den Benutzerfluss „Protokollierung aktiv → Aktionen → Protokolldatei" im Debug-Build vollständig ab. Ein zusätzliches E2E-Szenario ist nicht erforderlich — die Release-Lücke kann durch keinen UI-Test differenziert werden (gleiche Codezeile, anderes `#if`), sie wird stattdessen durch den Contract-Pin `MauiProgram_ActivatesLoggingPipeline` gesichert.

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| – | Keine bestehenden E2E-Tests betroffen. |

## Offene Punkte

Keine.
