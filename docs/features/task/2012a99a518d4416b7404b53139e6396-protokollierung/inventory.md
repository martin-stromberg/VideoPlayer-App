# Bestandsaufnahme: Protokollierung schreibt keine Ablauf-Einträge (Release)

Analyse des Protokoll-Features (`AppLogService`/`FileLogger`/DI-Verdrahtung) bezogen auf die Fehlermeldung aus `issue.md`: installierte App schreibt nur Marker-Zeilen, keine `ILogger`-Facheinträge.

## Zusammenfassung

- **Vorhanden und korrekt:** `AppLogService` (schaltbarer Datei-Schreiber, Marker, 1-MiB-Rotation, Token-Maskierung, Versand-Anhänge), `FileLogger`/`FileLoggerProvider` (`ILogger`-Adapter ab `Information`), ~75 `ILogger<T>`-Log-Stellen in Mvu/Maui/Storage, Einstellung `AppSettings.LoggingEnabled` inkl. Laufzeit-Umschaltung über `ShellAppEffectHandler.SetEnabled`, E2E- und Unit-Abdeckung.
- **Defektstelle:** `MauiProgram.CreateMauiApp()` aktiviert die `Microsoft.Extensions.Logging`-Pipeline nur implizit über `builder.Logging.AddDebug()` unter `#if DEBUG`. In Release registriert `MauiAppBuilder` `NullLoggerFactory`/`NullLogger<>` — DI-registrierte `ILoggerProvider` werden nie aufgenommen; jeder `ILogger<T>`-Aufruf läuft ins Leere. Empirisch in einer `net10.0`-Reproduktion mit `MauiApp.CreateBuilder()` verifiziert (`NullLoggerFactory`, keine Dateieinträge; `builder.Services.AddLogging()` → `LoggerFactory`, Einträge erscheinen).
- **Konsistentes Fehlerbild:** Marker-Zeilen gehen am `ILogger` vorbei (`AppLogService.WriteMarker`/`SetEnabled`) — sie sind die einzigen Inhalte der gemeldeten Protokolldatei.
- **Test-Ausgangszustand:** Unit-Suite 1280/1280 grün (Release-Konfiguration); E2E-Baseline läuft separat (Ergebnis in `inventory/tests.md`).

## Details

- [Datenmodell / Einstellungen](inventory/models.md)
- [Logik und Verdrahtung](inventory/logic.md)
- [Interfaces](inventory/interfaces.md)
- [Tests](inventory/tests.md)
