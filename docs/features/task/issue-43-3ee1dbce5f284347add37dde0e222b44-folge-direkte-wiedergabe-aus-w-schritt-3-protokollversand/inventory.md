# Bestandsaufnahme: Protokollierung und Protokollversand per Mail

Analysiert wurde der Bereich Einstellungen/Ablage/MVU-Verdrahtung bezogen auf die übersetzte Anforderung (`requirement.md`): aktivierbares Datei-Protokoll und Versand per `Email.ComposeAsync`.

## Zusammenfassung

- **Einstellungen:** `AppSettings` (Record, 9 `init`-Eigenschaften + `Normalize()`) wird über `AppSettingsService` (Schlüssel-Konstanten + `ISettingsStore` mit Profil-Präfix) persistent gehalten; UI = `SettingsPage` (XAML-Abschnitte Darstellung/Downloads/Wiedergabe/Server; Code-Behind dispatcht `SaveSettingsRequestedMessage`).
- **Logging:** Es existiert **kein** Datei-Logging. Diagnose läuft ausschließlich über `System.Diagnostics.Debug.WriteLine` (129 Aufrufe; 51 davon in `ShellAppEffectHandler`) plus `builder.Logging.AddDebug()` unter `#if DEBUG` in `MauiProgram`. `ILogger` kommt nur als optionale Option des `VideoPlayerApiClient` vor; kein Projekt referenziert `Microsoft.Extensions.Logging.Abstractions` direkt (transitiv via `Microsoft.Extensions.Logging.Debug` 10.0.10 in `VideoPlayer.Maui`).
- **Mail/Share:** `Microsoft.Maui.Essentials` 10.0.90 ist in `VideoPlayer.Maui` referenziert → `Email.ComposeAsync`/`EmailAttachment` und `Share.RequestAsync` ohne neue Pakete nutzbar. Bisher keinerlei Verwendung im Code.
- **Ablageort:** `AppStorageLayout` bietet `BackupDirectory`/`NoBackupDirectory`/`TempDirectory` (profil-isoliert); `AppStorageLayoutProvider.CreateLayout` legt sie an. Ein Logs-Verzeichnis existiert noch nicht.
- **MVU:** `AppMessage`/`AppEffect`-Records (deutsch, XML-Doku, CS1591 als Fehler), `AppUpdate.Update` als statisches Switch, `ShellAppEffectHandler.HandleAsync` als Switch am App-Rand. Fehlerhinweise laufen über Modell-Felder (Vorbild `StorageNotice` → Banner auf `HomePage`).
- **Testbarkeit:** `VideoWebPlayer.Maui.Tests` (net10.0, xUnit v3) referenziert `VideoPlayer.Storage`/`VideoPlayer.Mvu` — neue Logik gehört für Unit-Tests dorthin; `VideoPlayer.Maui` ist nicht referenzierbar → UI-/Verdrahtungs-Pins per Quelltext-Contract-Tests (Konvention: `ContinueWatchingContextMenuContractTests`, `MauiSettingsContractTests`).

Test-Ausgangszustand: `dotnet test VideoWebPlayer.Maui.Tests` auf Commit `a841a75` → **1104 erfolgreich, 0 fehlgeschlagen, 0 übersprungen** (Nachweis: [baseline-unittests.txt](inventory/test-results/baseline-unittests.txt)). E2E-Baseline wurde aus Zeitgründen nicht vorab ausgeführt (siehe [tests.md](inventory/tests.md)).

## Details

- [Datenmodell und Ablage](inventory/models.md)
- [Logik, MVU und UI-Verdrahtung](inventory/logic.md)
- [Tests](inventory/tests.md)
