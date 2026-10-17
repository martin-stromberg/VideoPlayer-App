# Plan-Review (Umsetzung ↔ Plan)

## Ergebnis

**Status:** Abgeschlossen — Plan vollständig umgesetzt, zwei begründete Abweichungen dokumentiert.

## Geplant → Umgesetzt

| Plan-Baustein | Umsetzung | Ergebnis |
|---------------|-----------|----------|
| `AppSettings.LoggingEnabled` (Standard aus) | `VideoPlayer.Storage/AppSettings.cs` | Umgesetzt; `AppSettingsTests.Defaults_AreExpected` gepinnt |
| `AppSettingsService.LoggingEnabledKey` (profil-präfixt) | `VideoPlayer.Storage/AppSettingsService.cs` | Umgesetzt; Load/Save-Roundtrip-Tests |
| `AppStorageLayout.LogsDirectory` | `AppStorageLayout.cs` unter `NoBackupDirectory/logs` | Umgesetzt; `EnsureDirectoriesExist` legt das Verzeichnis an |
| `AppLogService` (schaltbarer Datei-Schreiber, Rotation) | `VideoPlayer.Storage/AppLogService.cs` | Umgesetzt: Marker-Zeilen bei Aktivierung/Deaktivierung, Rotation auf `videoplayer.previous.log` ab 1 MiB, `GetAttachmentPaths` für den Versand |
| `FileLoggerProvider`/`FileLogger` (ILogger→Datei) | `VideoPlayer.Storage/FileLoggerProvider.cs`, `FileLogger.cs` | Umgesetzt; Mindestlevel `Information` gegen Framework-Rauschen |
| MVU-Nachrichten/-Effekte | `SendLogProtocolRequestedMessage`, `SendLogProtocolCompletedMessage`, `SendLogProtocolEffect`, `AppModel.LogSendError`, Update-Guard + Fehlerkonstanten | Umgesetzt; Unit-Tests |
| Einstellungsseite „Protokoll"-Abschnitt | `LoggingSwitch`, `SendLogSection` (IsVisible-Bindung), `SendLogButton`, `SendLogErrorLabel` | Umgesetzt; Verdrahtung per Contract-Test gepinnt |
| `Email.ComposeAsync` + `EmailAttachment` + Share-Fallback | `ShellAppEffectHandler.SendLogProtocolAsync` (Main-Thread, Empfänger-Konstante, FeatureNotSupported/COM/InvalidOp → `Share.RequestAsync`, Total-Fehlschlag → `LogSendFailedError`) | Umgesetzt; Contract-Test pinnt Empfänger und API-Aufrufe |
| Laufzeit-Umschaltung ohne Neustart | `SetEnabled` aus `LoadSettingsAsync` (Startup-Synchronisation) und `SaveSettings` | Umgesetzt |
| Debug.WriteLine-Umbau | Alle 51 Aufrufe im `ShellAppEffectHandler` → `logger.LogError` | Umgesetzt (kein `Debug.WriteLine` mehr in der Datei) |
| DI-Registrierung | `AppLogService`-Factory (Startwert aus `AppSettingsService.Load()`), `FileLoggerProvider` als `ILoggerProvider`-Singleton in `MauiProgram` | Umgesetzt |
| E2E-Szenarien | `LogProtocolTests`: Button-Sichtbarkeit aus/an, Protokolldatei entsteht, Persistenz über Neustart, Versand bis zur Compose-/Share-Grenze | Umgesetzt, 4/4 grün |
| DevApi-Beobachtbarkeit | `settingsLoggingEnabled`, `logSendError` in Snapshot + E2E-Modell | Umgesetzt |

## Abweichungen vom Plan (begründet)

1. **`UiDriver.EnterText` um Rücklese-Verify erweitert:** Der ursprüngliche Plan sah keine Änderung an `EnterText` vor. Ein Flake (leeres Feld bei Button-Klick) erforderte eine Verifikationsschleife; dabei zeigte sich, dass Passwortboxen den Wert ausblenden — sie bleiben auf dem `Enter`-Pfad ohne Rücklesen. Vorteil: die Absicherung wirkt auf alle E2E-Tests.
2. **`UiDriver.WaitForVisible` statt generellem Scrollen:** Der Button liegt unterhalb des sichtbaren Bereichs der Einstellungs-ScrollView; ein erstes „scroll den ersten scrollbaren Bereich" erwies sich als nicht deterministisch (Flyout-Navigation ist ebenfalls scrollbar). Stattdessen wird das Zielelement über `ScrollItemPattern.ScrollIntoView` eingerollt.

## Offene Restpunkte

Keine.
