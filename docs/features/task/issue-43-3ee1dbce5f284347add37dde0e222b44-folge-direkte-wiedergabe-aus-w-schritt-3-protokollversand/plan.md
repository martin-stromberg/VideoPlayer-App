# Umsetzungsplan: Protokollierung und Protokollversand per Mail

## Übersicht

Die Einstellungsseite erhält einen Abschnitt „Protokoll" mit dem neuen, persistenten Schalter „Protokollierung" (`AppSettings.LoggingEnabled`) und — nur bei aktivierter Protokollierung — dem Button „Protokoll senden". Bei aktivierter Protokollierung schreibt die App ein Programmprotokoll als Datei `videoplayer.log` in ein neues `LogsDirectory` des `AppStorageLayout` (nicht backup-pflichtig). Der Versand läuft als MVU-Effekt über `Email.ComposeAsync` mit `EmailAttachment` an `mstromberg84+heimkino@gmail.com`; schlägt der Mailentwurf fehl, greift der `Share`-Fallback, schlägt auch dieser, landet ein verständlicher Fehlertext als Inline-Hinweis auf der Einstellungsseite.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Datei-Logging | Neuer `ILoggerProvider` (`FileLoggerProvider`/`FileLogger`) + schreibender Dienst `AppLogService` in `VideoPlayer.Storage` | Testbarkeit: `VideoWebPlayer.Maui.Tests` kann `VideoPlayer.Storage` referenzieren, `VideoPlayer.Maui` nicht. `Microsoft.Extensions.Logging.Abstractions` wird als Paketreferenz in `VideoPlayer.Storage` ergänzt (Version 10.0.10, deckungsgleich mit `Microsoft.Extensions.Logging.Debug` in `VideoPlayer.Maui`). |
| Protokoll-Inhalt | Provider an `builder.Logging` angedockt (Framework-/Bibliotheks-Logs) + `ILogger<ShellAppEffectHandler>` injiziert und die 51 vorhandenen `Debug.WriteLine`-Fehlerpfade des Effekt-Handlers auf `logger.LogError(ex, …)` umgestellt | Das Protokoll muss belastbaren Inhalt haben; der Effekt-Handler ist der App-Rand mit allen Fehlerpfaden. Ersetzen statt doppelt schreiben: unter `#if DEBUG` bleibt `AddDebug()` die Debugger-Sicht. |
| Mindestlevel | `FileLogger.IsEnabled(level)` ⇐ `level >= LogLevel.Information && level != LogLevel.None` | Begrenzt Trace/Debug-Rauschen der Framework-Quellen; Fehlerpfade und Marker bleiben enthalten. |
| Umschalten zur Laufzeit | `AppLogService` hält `IsEnabled` als geteilten Zustand; `ShellAppEffectHandler` ruft `SetEnabled` nach `Load`/`Save` der Einstellungen; `MauiProgram` übergibt den Startwert synchron aus `AppSettingsService.Load()` | `PersistSettingsEffect`/`LoadSettingsEffect` laufen bereits über den Handler — kein neuer Effekt für das Umschalten nötig; sofortiges Wirksamwerden ohne Neustart. |
| Ablageort | `AppStorageLayout.LogsDirectory` = `Path.Combine(NoBackupDirectory, "logs")`, Datei `videoplayer.log`; Größenrotation (> 1 MiB → `videoplayer.previous.log`) | Protokolle sind Diagnosedaten, kein Backup-Gut; profil-isoliert durch `NoBackupDirectory`-Scoping. |
| Protokollversand | Neuer `SendLogProtocolEffect` aus `SendLogProtocolRequestedMessage`; Guard `model.Settings.LoggingEnabled`; Ergebnis `SendLogProtocolCompletedMessage(Error)` → `AppModel.LogSendError` | Folgt dem `PersistSettingsEffect`/„StorageNotice"-Muster; Fehleranzeige über Inline-Label auf der Einstellungsseite. |
| Fehlerfall Versand | `Email.ComposeAsync` → `catch` → `Share.RequestAsync(ShareFileRequest)` → `catch` → `LogSendFailedError`; fehlende/leere Protokolldatei → `LogProtocolEmptyError` | Beschlossene Detailentscheidung: verständlicher Hinweis bzw. Share-Fallback. Auf unpackaged Windows wirft `ComposeAsync` typischerweise `COMException` (0x80070032) — der Share-Fallback ist dort der wirksame Weg. |
| E2E-Grenze | E2E deckt Schalter, Button-Sichtbarkeit, Datei-Anlage und Persistenz ab; der Versand-Klick wird bis zur Compose-/Share-Grenze ausgeführt (Plattformdialog liegt außerhalb der App-Automation) | Mail-Client/Share-UI ist plattformseitig — in der Anforderung und im Zusatzkontext so akzeptiert („bis zur ComposeAsync-Grenze"). |

## Programmabläufe

### Protokollierung aktivieren (zur Laufzeit)

1. Anwender schaltet `LoggingSwitch` auf der `SettingsPage` → `OnLoggingToggled` → `Save(s => s with { LoggingEnabled = e.Value })` → `SaveSettingsRequestedMessage`.
2. `AppUpdate.UpdateSaveSettingsRequested` → `PersistSettingsEffect(normalized)` (Modell enthält den neuen Wert sofort → `PopulateFromModel` blendet `SendLogSection` ein).
3. `ShellAppEffectHandler.SaveSettings` → `AppSettingsService.Save(settings)` → `appLog.SetEnabled(settings.LoggingEnabled)` → `AppLogService` legt `LogsDirectory` an und schreibt die Marker-Zeile „Protokollierung aktiviert" in `videoplayer.log`.

Beteiligte Klassen/Komponenten: `SettingsPage`, `MvuHost`, `AppUpdate`, `ShellAppEffectHandler`, `AppSettingsService`, `AppLogService`.

### Protokollierung beim App-Start

1. `MauiProgram.CreateMauiApp` registriert `AppLogService` (Startwert `LoggingEnabled` synchron aus `AppSettingsService.Load()`) und `FileLoggerProvider` als `ILoggerProvider`-Singleton.
2. `LoadSettingsEffect` → `LoadSettingsAsync` → `appLog.SetEnabled(loaded.LoggingEnabled)` gleicht den Zustand ab (idempotent).
3. Alle `ILogger`-Ausgaben (Level ≥ Information) der App landen in `videoplayer.log`.

Beteiligte Klassen/Komponenten: `MauiProgram`, `AppLogService`, `FileLoggerProvider`, `ShellAppEffectHandler`.

### Protokollversand

1. „Protokoll senden" (`SendLogButton`, nur sichtbar bei `LoggingEnabled`) → `OnSendLogClicked` → `SendLogProtocolRequestedMessage`.
2. `AppUpdate` → Guard `!model.Settings.LoggingEnabled` → unverändert; sonst Modell mit `LogSendError = null` + `SendLogProtocolEffect`.
3. `ShellAppEffectHandler.SendLogProtocolAsync`: `AppLogService.GetAttachmentPaths()` (vorhandene `videoplayer*.log`-Dateien) — leer → `SendLogProtocolCompletedMessage(LogProtocolEmptyError)`.
4. Sonst auf dem Hauptthread: `Email.ComposeAsync` mit Betreff/Empfänger/Anhängen → Erfolg → `SendLogProtocolCompletedMessage(null)`.
5. `ComposeAsync` wirft (`FeatureNotSupportedException`/`COMException`/sonstige) → Fallback `Share.RequestAsync` mit der aktuellen Protokolldatei → Erfolg → `SendLogProtocolCompletedMessage(null)`; schlägt auch das fehl → `SendLogProtocolCompletedMessage(LogSendFailedError)`.
6. `SendLogProtocolCompletedMessage` → `model with { LogSendError = Error }` → `SendLogErrorLabel` auf der Einstellungsseite.

Beteiligte Klassen/Komponenten: `SettingsPage`, `AppUpdate`, `ShellAppEffectHandler`, `AppLogService`, `Email`, `Share`.

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `AppLogService` | Klasse (`VideoPlayer.Storage`, sealed) | Schaltbarer Datei-Protokoll-Speicher: `IsEnabled`, `LogFilePath`, `LogFiles` (aktuelle + rotierte Datei), `SetEnabled(bool)`, `Write(LogLevel, string category, string message, Exception?)`, `GetAttachmentPaths()`; Größenrotation > 1 MiB. |
| `FileLoggerProvider` | `ILoggerProvider` (`VideoPlayer.Storage`, sealed) | Erzeugt `FileLogger` je Kategorie; hält `AppLogService`-Referenz. |
| `FileLogger` | `ILogger` (`VideoPlayer.Storage`, sealed) | Formatierter Zeilen-Schrieb `yyyy-MM-dd HH:mm:ss.fff [LEVEL] Kategorie: Meldung` + Exception-Block; `IsEnabled` mit Mindestlevel Information. |
| `SendLogProtocolRequestedMessage` | `AppMessage`-Record (sealed) | „Protokoll senden" wurde gedrückt. |
| `SendLogProtocolCompletedMessage` | `AppMessage`-Record (sealed, `string? Error`) | Ergebnis des Versands (`null` = Erfolg). |
| `SendLogProtocolEffect` | `AppEffect`-Record (sealed) | Versand am App-Rand ausführen. |
| `AppLogServiceTests` | Testklasse | Enable/Disable, Datei-Anlage, Zeilenformat, Rotation, `GetAttachmentPaths`. |
| `FileLoggerTests` | Testklasse | `IsEnabled`-Schwelle, Weitergabe an `AppLogService`, Format. |
| `AppUpdateTests_LogProtocol` | Testklasse | Übergänge: Guard bei deaktivierter Protokollierung, Effekt-Emission, `LogSendError`-Setzen/Räumen. |
| `LogProtocolContractTests` | Testklasse | Quelltext-Verträge: XAML-AutomationIds, Verdrahtung, Handler-Zweig, `MauiProgram`-Registrierung, Empfänger-Konstante, DevApi-Felder. |
| `LogProtocolTests` | E2E-Testklasse | Schalter, Button-Sichtbarkeit, Protokolldatei auf Platte, Persistenz über Neustart, Versand-Klick bis Plattformgrenze. |

## Änderungen an bestehenden Klassen

### `AppSettings` (`VideoPlayer.Storage/AppSettings.cs`)

- **Neue Eigenschaft:** `LoggingEnabled` (`bool`, Standard `false`) — „Protokollierung an/aus".

### `AppSettingsService` (`VideoPlayer.Storage/AppSettingsService.cs`)

- **Neue Konstante:** `LoggingEnabledKey = "LoggingEnabled"`.
- **Geänderte Methoden:** `Load()` liest den Schlüssel; `Save()` schreibt ihn.

### `AppStorageLayout` (`VideoPlayer.Storage/AppStorageLayout.cs`)

- **Neue Eigenschaft:** `LogsDirectory` — `Path.Combine(NoBackupDirectory, "logs")`.
- **Geänderte Methode:** `EnsureDirectoriesExist()` legt zusätzlich `LogsDirectory` an.

### `AppModel` (`VideoPlayer.Mvu/AppModel.cs`)

- **Neues Feld:** `LogSendError` (`string?`) — Inline-Fehlertext des Protokollversands; `null` = kein Hinweis.

### `AppMessage` (`VideoPlayer.Mvu/AppMessage.cs`)

- **Neue Records:** `SendLogProtocolRequestedMessage`, `SendLogProtocolCompletedMessage(string? Error)` — deutsche XML-Doku.

### `AppEffect` (`VideoPlayer.Mvu/AppEffect.cs`)

- **Neuer Record:** `SendLogProtocolEffect` — deutsche XML-Doku.

### `AppUpdate` (`VideoPlayer.Mvu/AppUpdate.cs`)

- **Neue Switch-Zweige:** `SendLogProtocolRequestedMessage` → `UpdateSendLogProtocolRequested` (Guard `LoggingEnabled`, `LogSendError`-Reset, `SendLogProtocolEffect`); `SendLogProtocolCompletedMessage` → `UpdateSendLogProtocolCompleted` (`LogSendError` setzen/räumen).
- **Neue Konstanten:** `LogProtocolEmptyError`, `LogSendFailedError` (deutsche, anwenderverständliche Fehlertexte; Konvention wie `PlaybackUnavailableError`).

### `SettingsPage` (`VideoPlayer.Maui/Pages/SettingsPage.xaml` + `.xaml.cs`)

- **XAML:** Neuer Abschnitt „Protokoll" zwischen „Wiedergabe" und „Server": Schalter `LoggingSwitch` (AutomationId), `SendLogSection` (`IsVisible="False"`) mit Button `SendLogButton` („Protokoll senden", `SecondaryButton`-Stil) und `SendLogErrorLabel` (`ErrorText`-Stil).
- **Code-Behind:** `LoggingSwitch.Toggled += OnLoggingToggled` (→ `Save(s => s with { LoggingEnabled = e.Value })`); `OnSendLogClicked` → `SendLogProtocolRequestedMessage`; `PopulateFromModel` befüllt `LoggingSwitch.IsToggled`, `SendLogSection.IsVisible` (= `model.Settings.LoggingEnabled`) und `SendLogErrorLabel` (= `model.LogSendError`).

### `ShellAppEffectHandler` (`VideoPlayer.Maui/Services/ShellAppEffectHandler.cs`)

- **Neue ctor-Parameter/Felder:** `AppLogService appLog`, `ILogger<ShellAppEffectHandler> logger`.
- **Neuer Switch-Zweig:** `SendLogProtocolEffect` → `SendLogProtocolAsync()` (Hauptthread: `Email.ComposeAsync`, Fallback `Share.RequestAsync`, Fehler → `SendLogProtocolCompletedMessage`).
- **Geänderte Methoden:** `LoadSettingsAsync`/`SaveSettings` rufen `appLog.SetEnabled(settings.LoggingEnabled)` (auch im Fehlerpfad des Ladens: `SetEnabled(false)`-äquivalent über Defaults).
- **Geänderte Diagnoseausgaben:** alle `System.Diagnostics.Debug.WriteLine`-Aufrufe werden durch `logger.LogError(ex, …)` ersetzt (Nachricht ohne `[ShellAppEffectHandler]`-Präfix — die Kategorie trägt `ILogger`).

### `MauiProgram` (`VideoPlayer.Maui/MauiProgram.cs`)

- **Registrierungen:** `AppLogService` (Singleton; Startzustand `LoggingEnabled` aus `AppSettingsService.Load()`), `ILoggerProvider` → `FileLoggerProvider` (über `builder.Logging.Services`/`AddSingleton`).

### `DevApiModelSnapshot` (`VideoPlayer.Mvu/DevApi/DevApiModelSnapshot.cs`)

- **Neue Felder:** `SettingsLoggingEnabled` (`bool`) und `LogSendError` (`string?`); Befüllung in `FromModel` aus `model.Settings.LoggingEnabled`/`model.LogSendError`.

### `DevApiModel` (`VideoPlayer.E2E.Tests/Support/DevApiModels.cs`)

- **Neue Felder:** `settingsLoggingEnabled`, `logSendError` (Spiegel der Snapshot-Felder).

### `UiDriver` (`VideoPlayer.E2E.Tests/Support/UiDriver.cs`)

- **Neue Methode:** `Toggle(automationId)` — `TogglePattern` auf einem UIA-Element auslösen (Fallback: `Invoke`/`Click`).

### `AppInstance` (`VideoPlayer.E2E.Tests/Support/AppInstance.cs`)

- **Neue Hilfe:** `FindLogFiles()` / Profilverzeichnis-Suche unter `%LOCALAPPDATA%` (`*\de.martinstromberg.videos\Data\nobackup\{profil}`) zum Auffinden der Protokolldatei; `DeleteProfileDirectory` räumt zusätzlich das `de.martinstromberg.videos`-Profilverzeichnis (`Data\nobackup\{profil}`, `Cache\{profil}`) ab.

## Datenbankmigrationen

Keine.

## Validierungsregeln

| Feld / Objekt | Regel | Fehlerfall |
|---------------|-------|------------|
| `SendLogProtocolRequestedMessage` | Nur bei `model.Settings.LoggingEnabled` wirksam | Guard → `AppTransition.Unchanged` |
| Protokolldatei vor Versand | Muss existieren und nicht leer sein | `SendLogProtocolCompletedMessage(LogProtocolEmptyError)` |

## Konfigurationsänderungen

| Eintrag | Typ | Standardwert | Zweck |
|---------|-----|--------------|-------|
| `AppSettings.LoggingEnabled` / `LoggingEnabledKey` | `bool` | `false` | Protokollierung an/aus (persistent, profilbezogen) |
| Paket `Microsoft.Extensions.Logging.Abstractions` 10.0.10 in `VideoPlayer.Storage.csproj` | NuGet | – | `ILogger`/`ILoggerProvider`/`LogLevel` für `AppLogService`/`FileLoggerProvider` |
| Paket `Microsoft.Extensions.Logging` 10.0.10 in `VideoWebPlayer.Maui.Tests.csproj` | NuGet | – | `LoggerFactory`-Integrationstest des Providers |

## Seiteneffekte und Risiken

- **Schreibzugriffe:** `AppLogService.Write` läuft synchron unter `lock`; Schreibfehler (`IOException`, `UnauthorizedAccessException`) werden geschluckt — Logging darf die App nie werfen lassen.
- **Aktivierung erst nach Einstellungs-Laden:** Zwischen `CreateMauiApp` und `LoadSettingsEffect` gilt der synchron gelesene Startwert — korrekt, da `Load()` auf `Preferences` synchron arbeitet.
- **Windows-Verhalten `Email.ComposeAsync`:** unpackaged → voraussichtlich `COMException`/nicht unterstützt → Share-Fallback greift; das ist das beschlossene Verhalten.
- **AppInstance-Profilaufräumung:** Das reale `FileSystem.AppDataDirectory` (`%LOCALAPPDATA%\User Name\de.martinstromberg.videos`) liegt außerhalb der bisherigen Löschpfade — wird erweitert, sonst bleiben Protokolle als Testleichen.
- **`EnumCoverageTests`:** Kein neuer Enum — unberührt.
- **CS1591 (Warnung als Fehler):** Alle neuen öffentlichen Member brauchen deutsche XML-Doku.

## Umsetzungsreihenfolge

1. **NuGet-Voraussetzung:** `Microsoft.Extensions.Logging.Abstractions` (10.0.10) in `VideoPlayer.Storage.csproj` ergänzen.
   - Voraussetzungen: Keine (Restore-Fähigkeit gegeben).
2. **`AppSettings.LoggingEnabled` + `AppSettingsService.LoggingEnabledKey`** (Load/Save) ergänzen.
   - Voraussetzungen: Keine.
3. **`AppStorageLayout.LogsDirectory`** + `EnsureDirectoriesExist` erweitern.
   - Voraussetzungen: Keine.
4. **`AppLogService`, `FileLoggerProvider`, `FileLogger`** in `VideoPlayer.Storage` anlegen.
   - Voraussetzungen: Schritte 1 und 3.
5. **MVU:** `AppMessage`/`AppEffect`-Records, `AppModel.LogSendError`, `AppUpdate`-Zweige + Fehlertext-Konstanten.
   - Voraussetzungen: Schritt 2.
6. **`SettingsPage`** (XAML-Abschnitt + Code-Behind-Verdrahtung).
   - Voraussetzungen: Schritt 5.
7. **`ShellAppEffectHandler`:** ctor-Parameter `AppLogService` + `ILogger<ShellAppEffectHandler>`, `SetEnabled`-Aufrufe, `SendLogProtocolAsync`, `Debug.WriteLine`→`logger.LogError`-Umstellung.
   - Voraussetzungen: Schritte 4 und 5.
8. **`MauiProgram`:** `AppLogService`- und `ILoggerProvider`-Registrierung.
   - Voraussetzungen: Schritte 4 und 7.
9. **`DevApiModelSnapshot`** (+ E2E-`DevApiModel`) um `SettingsLoggingEnabled`/`LogSendError` erweitern.
   - Voraussetzungen: Schritt 5.
10. **Unit-/Contract-Tests** schreiben (`AppLogServiceTests`, `FileLoggerTests`, `AppUpdateTests_LogProtocol`, `LogProtocolContractTests`, Erweiterungen `AppSettingsTests`/`AppSettingsServiceTests`/`AppStorageLayoutTests`); NuGet-Voraussetzung im Testprojekt (`Microsoft.Extensions.Logging` 10.0.10).
    - Voraussetzungen: Schritte 1–9.
11. **E2E-Infrastruktur + Tests:** `UiDriver.Toggle`, `AppInstance`-Profilpfad-Hilfe/Aufräumen, `LogProtocolTests`.
    - Voraussetzungen: Schritte 6–9; App-Debug-Build via `AppFixture`.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `Defaults_AreExpected` (erweitert) | `AppSettingsTests` | `LoggingEnabled` Standard `false` |
| `Save_ThenLoad_RoundTrips` (erweitert) | `AppSettingsServiceTests` | `LoggingEnabled` übersteht Roundtrip |
| `Save_LoggingEnabled_WritesKey` | `AppSettingsServiceTests` | Schlüssel `LoggingEnabled` landet im Store |
| `EnsureDirectoriesExist_CreatesAll` (erweitert) | `AppStorageLayoutTests` | `LogsDirectory` wird angelegt |
| `LogsDirectory_LiesUnderNoBackup` | `AppStorageLayoutTests` | Pfad unter `NoBackupDirectory` |
| `Write_WhenDisabled_CreatesNoFile` | `AppLogServiceTests` | Kein Protokoll bei deaktivierter Protokollierung |
| `SetEnabled_True_CreatesLogFileWithMarker` | `AppLogServiceTests` | Datei + Aktivierungs-Marker |
| `Write_WhenEnabled_AppendsFormattedLine` | `AppLogServiceTests` | Zeitstempel/Level/Kategorie/Meldung |
| `SetEnabled_False_StopsWriting` | `AppLogServiceTests` | Marker „deaktiviert", danach keine Einträge |
| `Write_OverMaxBytes_RotatesToPreviousFile` | `AppLogServiceTests` | Rotation auf `videoplayer.previous.log` |
| `GetAttachmentPaths_ReturnsExistingLogFiles` | `AppLogServiceTests` | Versand-Anhangsliste |
| `Log_InformationAndAbove_Written_DebugBelowSkipped` | `FileLoggerTests` | Mindestlevel Information |
| `Log_WithException_AppendsExceptionDetails` | `FileLoggerTests` | Exception-Block im Protokoll |
| `Update_SendLogRequested_WhenDisabled_IsUnchanged` | `AppUpdateTests_LogProtocol` | Guard |
| `Update_SendLogRequested_WhenEnabled_EmitsEffectAndClearsError` | `AppUpdateTests_LogProtocol` | `SendLogProtocolEffect`, `LogSendError`-Reset |
| `Update_SendLogCompleted_SetsAndClearsError` | `AppUpdateTests_LogProtocol` | `LogSendError` aus `Error` |
| `SettingsPage_Markup_And_Wiring` | `LogProtocolContractTests` | AutomationIds, Sektion, `IsVisible`-Schaltung |
| `ShellAppEffectHandler_HandlesSendLogEffect_AndUsesEmailShare` | `LogProtocolContractTests` | Switch-Zweig, `Email.ComposeAsync`, `Share.RequestAsync`, Empfänger `mstromberg84+heimkino@gmail.com`, Hauptthread |
| `MauiProgram_RegistersLogServices` | `LogProtocolContractTests` | `AppLogService`-/`ILoggerProvider`-Registrierung |
| `DevApiSnapshot_ExposesLoggingFields` | `LogProtocolContractTests` bzw. `DevApiModelSnapshotTests` | `SettingsLoggingEnabled`/`LogSendError` im Snapshot |
| `DevApiModel_MirrorsLoggingFields` | `DevApiContractTests`-Konvention | E2E-DTO-Felder |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `AppSettingsTests.Defaults_AreExpected` | Neue Eigenschaft `LoggingEnabled` ergänzen |
| `AppSettingsServiceTests.Save_ThenLoad_RoundTrips` | Neue Eigenschaft in den Roundtrip aufnehmen |
| `AppStorageLayoutTests.EnsureDirectoriesExist_CreatesAll` | Zusätzliches Verzeichnis `LogsDirectory` |
| `VideoPlayerMauiContractTests` (falls `Debug.WriteLine`-Pins in `ShellAppEffectHandler` vorhanden) | Umstellung auf `logger.LogError` prüfen |

### E2E-Tests (primärer Funktionsnachweis)

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| Pflicht | Einstellungen öffnen → Protokollierung einschalten → „Protokoll senden"-Button sichtbar → Protokolldatei existiert mit Inhalt | `VideoPlayer.E2E.Tests/Tests/LogProtocolTests.cs` — `EnableLogging_ShowsSendButton_AndCreatesLogFile` | Schalter aktivierbar; Button nur bei aktivierter Protokollierung; Protokolldatei wird erzeugt | Sichtbarkeitsregel + tatsächliche Datei-Anlage nur über UI-Fluss nachweisbar |
| Pflicht | Protokollierung deaktiviert → kein „Protokoll senden"-Button, keine Protokolldatei | `LogProtocolTests` — `LoggingDisabled_HidesSendButton_AndNoLogFile` | Button ausgeblendet; ohne aktivierte Protokollierung kein Versand/kein Protokoll | Negative Sichtbarkeitsregel über UI |
| Pflicht | Einstellung persistent: App-Neustart mit selbem Profil → Protokollierung weiterhin aktiv, Button sichtbar | `LogProtocolTests` — `LoggingEnabled_PersistsAcrossRestart` | Persistenz der Einstellung | Persistenz über Prozessgrenze nur e2e belegbar |
| Pflicht | „Protokoll senden" klicken → Versand läuft bis zur Plattformgrenze (Mailentwurf oder Share) ohne Absturz; Fehlerpfad würde `logSendError` setzen | `LogProtocolTests` — `SendLog_Click_InvokesPlatformShareOrMail_WithoutCrash` | Versand ausgelöst; App bleibt bedienbar | Compose/Share ist plattformseitig — Nachweis bis `ComposeAsync`-Grenze |

Bestehende E2E-Tests müssen nicht angepasst werden (die Einstellungsseite erhält einen zusätzlichen Abschnitt, keine geänderten Bedienelemente).

## Offene Punkte

Keine.
