# Abnahmeprüfung – Entwicklungsschritt 3

## Ergebnis

**Status:** Anforderung vollständig erfüllt

## Abweichungen

Keine.

## Hinweise

Prüfung gegen `git diff task/issue-43-…-folge-direkte-wiedergabe-aus-w...HEAD` (Commit `5091165`) plus Quelldateien; Unit-Suite vollständig neu ausgeführt.

### AK 1 — Protokollierung aktivierbar/deaktivierbar, persistent: erfüllt

- `AppSettings.LoggingEnabled` (Standard `false`) — `VideoPlayer.Storage/AppSettings.cs:57`.
- Persistenz über `AppSettingsService` mit Ablageschlüssel `LoggingEnabled` (profil-präfixt) in die nicht-geheime Ablage `ISettingsStore` → `PreferencesSettingsStore(Preferences.Default)` — `AppSettingsService.cs:38, 68, 88`; `MauiProgram.cs:60`.
- Schalter `LoggingSwitch` → `OnLoggingToggled` → `Save(s => s with { LoggingEnabled = e.Value })` → `SaveSettingsRequestedMessage` → `PersistSettingsEffect` → `AppSettingsService.Save` — `SettingsPage.xaml.cs:30, 125-128`; `ShellAppEffectHandler.cs:1160-1173`.
- Roundtrip-Test `Save_LoggingEnabled_PersistsAcrossLoad` belegt Schlüssel-Schreiben und Wiederladen; E2E `LoggingEnabled_PersistsAcrossRestart` belegt die Persistenz über einen echten App-Neustart.

### AK 2 — Programmprotokoll als Datei bei aktivierter Protokollierung: erfüllt

- `AppStorageLayout.LogsDirectory` = `NoBackupDirectory/logs` (nicht backup-pflichtig, profil-isoliert), in `EnsureDirectoriesExist` angelegt — `AppStorageLayout.cs:78-81, 96`.
- `AppLogService` schreibt zeitgestempelte Zeilen (`yyyy-MM-dd HH:mm:ss.fff [LEVEL] Kategorie: Nachricht` + Exception-Anhang) in `videoplayer.log`; Rotation nach `videoplayer.previous.log` oberhalb 1 MiB; alle Schreibfehler geschluckt — `AppLogService.cs:99-125, 169-180`.
- Schaltzustand: Startwert synchron aus `AppSettingsService.Load().LoggingEnabled` im DI-Setup (`MauiProgram.cs:71-74`), Laufzeit-Umschaltung über `SetEnabled` beim Laden (`ShellAppEffectHandler.cs:1153`) und nach erfolgreichem Speichern (`Z. 1167`) — wirkt ohne Neustart; Ein-/Ausschalt-Marker-Zeilen dokumentieren den Wechsel.
- Inhalt plausibel: `FileLoggerProvider`/`FileLogger` als `ILoggerProvider`-Singleton registriert (`MauiProgram.cs:75-76`, Mindestlevel `Information`); 51 `Debug.WriteLine`-Fehlerpfade des `ShellAppEffectHandler` auf `logger.LogError(ex, …)` umgestellt — kein `Debug.WriteLine` mehr in der Datei (Contract-Test pinnt das). Die übrigen `Debug.WriteLine`-Stellen außerhalb des Handlers (Seiten, Dienste in `VideoPlayer.Mvu`) bleiben Debugger-Ausgaben und erreichen die Datei nicht — nicht Teil der Akzeptanzkriterien; die Release-Notes-Formulierung „sämtliche bisherigen Debug.WriteLine-Diagnosen" ist in dieser Hinsicht leicht übertrieben (Doku-Nuance, keine Abweichung).
- Sensible Daten: alle Log-Aufrufe sind statische Meldungen plus Exception-Objekt — keine Tokens, Passwörter oder Zugangsdaten in den Message-Templates (grep-Nachweis über alle 53 `logger.Log*`-Aufrufe des Handlers).

### AK 3 — „Protokoll senden" nur bei aktivierter Protokollierung sichtbar, Mailentwurf mit Anhang: erfüllt

- `SendLogSection` im neuen „Protokoll"-Abschnitt startet mit `IsVisible="False"` und wird aus dem Modell geschaltet: `SendLogSection.IsVisible = settings.LoggingEnabled` — `SettingsPage.xaml` (Protokoll-Abschnitt) bzw. `SettingsPage.xaml.cs:87`.
- Klick dispatcht `SendLogProtocolRequestedMessage` (`SettingsPage.xaml.cs:161-164`) → Guard → `SendLogProtocolEffect` → `SendLogProtocolAsync` → `Email.ComposeAsync` mit `Attachments = … new EmailAttachment(path)` (aktuelle + rotierte Datei über `GetAttachmentPaths`) — `ShellAppEffectHandler.cs:253-255, 270-297`.
- Belegt durch E2E `LoggingDisabled_HidesSendButton_AndNoLogFile` (Button nach Einrollen nicht sichtbar, keine Datei) und `EnableLogging_ShowsSendButton_AndCreatesLogFile`.

### AK 4 — Mailentwurf an mstromberg84+heimkino@gmail.com: erfüllt

- Konstante `LogProtocolRecipient = "mstromberg84+heimkino@gmail.com"` (`ShellAppEffectHandler.cs:34`), verwendet als `To = new List<string> { LogProtocolRecipient }` (`Z. 289`). Exakter String, kein zweiter Empfänger. `LogProtocolContractTests` pinnt die Adresse gegen den Quelltext.

### AK 5 — kein Versand ohne aktivierte Protokollierung; Hinweis ohne Mail-Client: erfüllt

- Doppelter Guard: `UpdateSendLogProtocolRequested` lässt das Modell unverändert, wenn `!model.Settings.LoggingEnabled` (`AppUpdate.cs:242-245`); der Handler bricht zusätzlich bei `!appLog.IsEnabled` bzw. fehlenden Dateien mit `LogProtocolEmptyError` ab (`ShellAppEffectHandler.cs:272-278`).
- Fehlerpfad ohne Mail-Client: `Email.ComposeAsync`-Fehlschlag (`FeatureNotSupportedException`/`COMException`/`InvalidOperationException`, u. a. unpackaged Windows) → `Share.RequestAsync`-Teilen-Fallback (`Z. 299-312`); schlägt auch dieser, meldet `SendLogProtocolCompletedMessage(LogSendFailedError)` den verständlichen Inline-Hinweis „Der Protokollversand ist auf diesem Gerät nicht möglich — …" → `AppModel.LogSendError` → `SendLogErrorLabel` (`AppUpdate.cs:81, 252-257`; `SettingsPage.xaml.cs:88-89`).

### Testnachweise

- Von dieser Prüfung vollständig neu ausgeführt: `dotnet test VideoWebPlayer.Maui.Tests` — **1132/1132 grün** (11 s), deckungsgleich mit der Behauptung. Darin u. a. `AppLogServiceTests` (11: deaktiviert→keine Datei, Marker, Rotation, Attachments, Profil-Isolation), `FileLoggerTests` (5: Level-Filter, Dienst-Schalter, LoggerFactory-Integration), `AppUpdateTests_LogProtocol` (4: Guard, Effekt, Fehler setzen/räumen), `LogProtocolContractTests` (6: XAML-Verdrahtung, Empfänger, Compose/Share, DI, DevApi), erweiterte `AppSettingsServiceTests`/`AppSettingsTests`/`AppStorageLayoutTests`/`SettingsStorageContractTests`.
- E2E: vier Szenarien in `VideoPlayer.E2E.Tests/Tests/LogProtocolTests.cs` quellseitig geprüft (Button-Sichtbarkeit mit Negativ-Assertion, Datei-Erzeugung, Persistenz über Neustart mit realem Profilverzeichnis, Versand-Klick bis zur `Email.ComposeAsync`/`Share`-Plattformgrenze); dokumentiert als 4/4 grün in `test-results.md`. Nicht erneut ausgeführt (laufende App + UIA erforderlich). Der Mailentwurf selbst ist plattformseitig nicht automatisierbar — Testgrenze dokumentiert und sachgerecht.

### Regressionen / Randfälle

- Bestehende Einstellungs-Sektionen unberührt — der Diff an `SettingsPage.xaml`/`.xaml.cs` ist rein additiv; `SettingsStorageContractTests` pinnt jetzt sechs Switches + neue AutomationIds.
- `Debug.WriteLine`-Umbau korrekt: `LogLevel.Error` passt zu den bisherigen Fehlerpfaden; Message-Templates statt Interpolation (`{MessageType}` mit Argument), Kategorie via `ILogger<ShellAppEffectHandler>`.
- `UiDriver`-Härtung (`EnterText`-Verify-Schleife, `Toggle`, `WaitForVisible` mit `ScrollItemPattern`/`IsOffscreen`) und `AppInstance.FindLogFile`/Profil-Aufräumung um `nobackup`/Cache erweitert — unterstützend, keine inhaltliche Änderung.
- Kleinigkeit ohne Abweichungscharakter: der Share-Fallback teilt nur `videoplayer.log` (nicht die rotierte Vorgänger-Datei, die der Mail-Anhang enthält) — die Anforderung „Protokoll als Anhang" ist erfüllt.
