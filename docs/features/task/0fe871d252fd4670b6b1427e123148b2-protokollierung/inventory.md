# Bestandsaufnahme: Programmprotokoll mit diagnostisch verwertbarem Inhalt

Analysiert wurde die Protokoll-Infrastruktur (`VideoPlayer.Storage`), die Effekt-Ausführung (`ShellAppEffectHandler`), die MVU-Dienste (`VideoPlayer.Mvu`), die MAUI-Seiten/-Dienste (`VideoPlayer.Maui`) und der API-Client (`VideoPlayer.ApiClient`) bezogen auf die übersetzte Anforderung (`requirement.md`): bei aktivierter Protokollierung sollen fachliche Abläufe in `videoplayer.log` landen — aktuell steht dort nur die Marker-Zeile.

## Zusammenfassung

- **Protokoll-Infrastruktur vollständig vorhanden und funktionsfähig:** `AppLogService` (schaltbarer Datei-Speicher, 1-MiB-Rotation, Versand-Dateiliste), `FileLogger`/`FileLoggerProvider` (`ILogger`-Brücke mit `Information`-Mindestlevel) und die DI-Verdrahtung in `MauiProgram.CreateMauiApp` (`ILoggerProvider`-Singleton). Der Schaltzustand folgt `AppSettings.LoggingEnabled`; `SetEnabled` wird in `ShellAppEffectHandler.LoadSettingsAsync`/`SaveSettings` aufgerufen.
- **Der Flaschenhals bestätigt sich im Code:** Der einzige injizierte `ILogger` steckt im `ShellAppEffectHandler` — dort 71 `logger.LogError`-Aufrufe (ausschließlich Fehlerpfade) plus genau ein `logger.LogInformation` (Teilen-Fallback des Protokollversands). Erfolgspfade erzeugen keinen einzigen Protokolleintrag.
- **`Debug.WriteLine`-Bestand deckt sich mit der Anforderung:** 53 Aufrufe in `VideoPlayer.Mvu` (u. a. `AuthSessionService` 9, `DownloadService` 7, `DeviceTokenService` 6, `ServerEventsService` 5, `HomeListService` 5), 20 in `VideoPlayer.Maui` (u. a. `PlayerPage`/`AppShell` je 5), 2 in `VideoPlayer.Storage` (`CacheDatabase`, `BackupDatabase`). Kein `Debug.WriteLine` im `ShellAppEffectHandler` (per `LogProtocolContractTests` gepinnt).
- **Logger-Haken des API-Clients ungenutzt:** `VideoPlayerApiClientOptions.Logger` existiert; `VideoPlayerApiClient.SendWithPolicyAsync` ruft `options.Logger?.LogWarning` an drei Stellen (Transportfehler-Wiederholung, 401/Token-Erneuerung, wiederholbarer Status). `AuthSessionService.CreateClient` und die kurzlebigen Refresh-/Widerrufs-Clients setzen `Logger` nicht.
- **Secrets-Risiko beachten:** `PlaybackSourceService.ResolveAsync` hängt das Anmelde-Token als `access_token`-Query an die Stream-URL (`PlaybackSource.Location`) — die Quell-Adresse darf nicht ins Protokoll.
- **`VideoWebPlayer.Maui` (Altbestand)** enthält keinerlei Protokoll-Verdrahtung (kein `AppLogService`/`FileLogger`/`LoggingEnabled`/`ILoggerProvider`) — bestätigt die Annahme, dass dort nichts zu tun ist.
- **`VideoPlayer.Mvu.csproj`** referenziert `Microsoft.Extensions.Logging.Abstractions` bisher nicht direkt (nur transitiv über `VideoPlayer.Storage`).

Test-Ausgangszustand: `dotnet test VideoWebPlayer.Maui.Tests` (Release) auf Commit `0b607a6` → **1257 erfolgreich, 0 fehlgeschlagen, 0 übersprungen** (Nachweis: [tests.md](inventory/tests.md), [TRX](inventory/test-results/unittest-baseline.trx)). Die E2E-Suite `VideoPlayer.E2E.Tests` wurde nicht ausgeführt (GUI-Automatisierung per FlaUI, kein CI-Bestandteil) — dokumentierte Testlücke.

## Details

- [Protokoll-Infrastruktur](inventory/infrastructure.md)
- [Logik: Effekt-Handler, MVU-Dienste, MAUI-Schicht, API-Client](inventory/logic.md)
- [Datenmodell und MVU-Nachrichten](inventory/models.md)
- [Enums](inventory/enums.md)
- [Interfaces](inventory/interfaces.md)
- [Tests](inventory/tests.md)
