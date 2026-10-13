# Übersetzte Anforderung: Protokollierung schreibt keine Ablauf-Einträge in der installierten App

Quelle: `issue.md` (Aufgaben-ID `2012a99a-518d-4416-b740-4b53139e6396`), Fehlermeldung zu PR #47.

## Fachliche Zusammenfassung

Das in PR #47 eingeführte Programmprotokoll enthält in der installierten (Release-)App ausschließlich die Marker-Zeilen `=== Protokollierung aktiviert ===` und `=== Protokollierung beim Start aktiv ===` — keine einzige fachliche Ablauf-Zeile (z. B. „App-Start", Wiedergabe-Ereignisse). Ursache: `MauiAppBuilder` registriert ohne explizite `builder.Logging`-Konfiguration `NullLoggerFactory`/`NullLogger<T>`; sämtliche über `ILogger<T>` injizierten Protokollierungsaufrufe laufen ins Leere. Da `builder.Logging.AddDebug()` unter `#if DEBUG` steht, fehlt im Release-Build jede Logging-Aktivierung — der `ILoggerProvider`-Singleton (`FileLoggerProvider`) wird nie aufgenommen. Marker-Zeilen sind davon unbetroffen, weil `AppLogService` sie direkt (ohne `ILogger`) schreibt.

## Betroffene Klassen und Komponenten

- **Verdrahtung (Defektstelle):**
  - `VideoPlayer.Maui/MauiProgram.cs` — `CreateMauiApp()` muss die `Microsoft.Extensions.Logging`-Pipeline (echte `LoggerFactory`) explizit aktivieren, damit der registrierte `ILoggerProvider` (`FileLoggerProvider`) aufgenommen wird. Aktuell geschieht das nur implizit über `builder.Logging.AddDebug()` unter `#if DEBUG`.
- **Bereits korrekt implementiert (kein Änderungsbedarf):**
  - `VideoPlayer.Storage/AppLogService` — Datei-Schreiber mit Schalter, Marker, Rotation, Maskierung.
  - `VideoPlayer.Storage/FileLogger` / `FileLoggerProvider` — `ILogger`-Adapter, Mindestlevel `Information`.
  - Alle ~75 `ILogger<T>`-Log-Stellen in `VideoPlayer.Mvu`, `VideoPlayer.Maui` und `VideoPlayer.Storage` (aus PR #47).
- **Tests:**
  - `VideoWebPlayer.Maui.Tests` — neuer/pinnder Contract-Test, der die Aktivierung der Logging-Pipeline in `MauiProgram` verlangt (Quelltext-Pin wie `LogProtocolContractTests`).
  - `VideoPlayer.E2E.Tests` — der bestehende Fact `LogProtocolTests.LoggingEnabled_RealFlows_WriteDiagnosticEntries` deckt den End-to-End-Nachweis bereits ab, läuft aber nur gegen den Debug-Build (Dokumentation der Release-Lücke).

## Implementierungsansatz

- Eine einzige, konfigurationsfreie Aktivierung in `MauiProgram.CreateMauiApp`: `builder.Services.AddLogging()` (oder eine äquivalente `builder.Logging`-Konfiguration) **unbedingt** ausführen — dadurch registriert DI die echte `LoggerFactory` statt der `NullLoggerFactory`-Vorgabe, die dann die per `AddSingleton<ILoggerProvider>` registrierten Provider (`FileLoggerProvider`, im Debug zusätzlich `DebugLoggerProvider`) aufnimmt.
- Verhalten im Debug bleibt unverändert (`AddDebug()`-Provider plus `FileLoggerProvider`); im Release fließen Einträge ab `Information` über `FileLogger` in `videoplayer.log`.
- Alternative `builder.Logging.SetMinimumLevel(...)` aktiviert ebenfalls, setzt aber zusätzlich ein globales Mindestlevel — unerwünschter Nebeneffekt auf den Debug-Provider; daher die neutrale `AddLogging()`-Aktivierung bevorzugen.
- Schaltzustand (`LoggingEnabled`) bleibt unverändert ausschließlich im `AppLogService`.

## Konfiguration

Keine neue Konfiguration — bestehende Benutzereinstellung `AppSettings.LoggingEnabled` bleibt der einzige Schalter.

## Offene Fragen

Keine — Reproduktion des DI-Verhaltens (`MauiApp.CreateBuilder` ohne `builder.Logging`-Konfiguration → `NullLoggerFactory`) ist verifiziert; Fix-Richtung eindeutig.
