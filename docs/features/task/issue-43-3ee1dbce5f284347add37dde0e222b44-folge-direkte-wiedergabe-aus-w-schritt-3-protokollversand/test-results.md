# Testergebnisse

## Unit-/Contract-Tests

- `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj` — **1132/1132 grün** (Baseline: 1104).
  - Neu: `AppLogServiceTests` (11 Szenarien: kein Schreiben deaktiviert, Start-/Aktivierungs-/Deaktivierungs-Marker, formatierte Zeilen, Exception-Anhang, Rotation, Attachment-Pfade, Profil-Isolation), `FileLoggerTests` (Level-Filter, Dienst-Schalter, LoggerFactory-Integration), `AppUpdateTests_LogProtocol` (Guard deaktiviert, Effekt + Fehler-Reset, Abschluss setzt/räumt `LogSendError`), `LogProtocolContractTests` (XAML-Verdrahtung, Effekt-Handler inkl. Empfänger/ComposeAsync/EmailAttachment/Share-Fallback/MainThread, MauiProgram-Registrierung, DevApi-Felder, Update-Guard).
  - Erweitert: `AppSettingsTests` (Standard aus), `AppSettingsServiceTests` (Roundtrip + Schlüssel, Default ohne Schlüssel), `AppStorageLayoutTests` (Verzeichnis-Erzeugung, Pfad unter `NoBackup`), `SettingsStorageContractTests` (neue AutomationIds, 6 Switches).
- `dotnet build VideoPlayer.Maui -f net10.0-windows10.0.19041.0` — 0 Fehler; die einzige Warnung ist die bereits bekannte `CS0618` in `ServerSetupPage.xaml.cs` (vorherbestehend).

## E2E-Tests (primärer Nachweis des Benutzerflusses)

`dotnet test VideoPlayer.E2E.Tests --filter "FullyQualifiedName~LogProtocolTests"` — **4/4 grün**:

| Szenario | Ergebnis |
|----------|----------|
| `LoggingDisabled_HidesSendButton_AndNoLogFile` — kein Versand-Button sichtbar (auch nach Einrollen), keine Protokolldatei | grün |
| `EnableLogging_ShowsSendButton_AndCreatesLogFile` — Schalter an → Button sichtbar, `videoplayer.log` angelegt und befüllt | grün |
| `LoggingEnabled_PersistsAcrossRestart` — gleiche Profil: Schalter-Zustand, Button und Protokolldatei nach Neustart erhalten | grün |
| `SendLog_Click_InvokesPlatformShareOrMail_WithoutCrash` — Versand-Klick löst Mail-/Share-Plattformaufruf aus; App bleibt bedienbar, kein kryptischer Fehlertext | grün |

Regressionsprobe geänderter gemeinsamer Infrastruktur: `ServerSwitchTests` — 1/1 grün (`EnterText`-Verify-Schleife, `Toggle`, `WaitForVisible` berühren bestehende Flows nicht).

## Testgrenzen (dokumentiert)

- Der Mailentwurf/Teilen-Dialog ist ein Plattformfenster außerhalb der App-UI-Hierarchie und damit nicht automatisierbar — der E2E-Test fährt bis zur `Email.ComposeAsync`-/`Share.RequestAsync`-Grenze (Klick, kein Absturz, kein `LogSendError`-Schrotttext). Auf dieser Windows-Umgebung wirft `Email.ComposeAsync` erfahrungsgemäß `COMException` (unpackaged), womit effektiv der Share-Fallback ausgelöst wird.
- Ein deterministischer „gar kein Versandweg"-Zustand ist auf der Testmaschine nicht herstellbar (Share ist immer verfügbar); der Fehlerpfad ist per Unit-/Contract-Tests der Fehlerkonstanten und des `SendLogProtocolCompletedMessage`-Übergangs abgesichert.
