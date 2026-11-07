# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan vollständig

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| Protokollierung in den Einstellungen (de-)aktivierbar, Einstellung persistent | `AppSettings.LoggingEnabled` + `AppSettingsService`-Schlüssel + `LoggingSwitch` auf `SettingsPage` (Plan: „Neue Klassen", „Änderungen", Programmablauf „Protokollierung aktivieren") | Unit (`AppSettingsServiceTests`-Roundtrip, Key-Test) + E2E `LoggingEnabled_PersistsAcrossRestart` | Abgedeckt |
| Bei aktivierter Protokollierung wird ein Programmprotokoll als Datei erzeugt | `AppLogService` + `FileLoggerProvider`/`FileLogger`, `LogsDirectory`, `MauiProgram`-Registrierung, `SetEnabled`-Umschaltung zur Laufzeit | Unit (`AppLogServiceTests`, `FileLoggerTests`) + E2E `EnableLogging_ShowsSendButton_AndCreatesLogFile` (Datei auf Platte) | Abgedeckt |
| „Protokoll senden"-Button nur bei aktivierter Protokollierung sichtbar; öffnet Mailentwurf mit Protokoll als Anhang | `SendLogSection`/`SendLogButton` mit `IsVisible`-Bindung an `LoggingEnabled`; MVU-Kette `SendLogProtocolRequestedMessage` → `SendLogProtocolEffect` → `Email.ComposeAsync` mit `EmailAttachment` | Contract-Test (`LogProtocolContractTests`) + E2E `EnableLogging_*`/`LoggingDisabled_*`/`SendLog_Click_*` | Abgedeckt |
| Mailentwurf an `mstromberg84+heimkino@gmail.com` | Empfänger-Konstante im `SendLogProtocolAsync`-Pfad des `ShellAppEffectHandler` | `LogProtocolContractTests` pinnt Empfänger und `Email.ComposeAsync`/`EmailAttachment` | Abgedeckt |
| Ohne Protokollierung kein Versand; ohne Mail-Client verständlicher Hinweis | Update-Guard auf `LoggingEnabled`; `Email.ComposeAsync`→`Share`-Fallback→`LogSendFailedError` als Inline-Hinweis | `AppUpdateTests_LogProtocol` (Guard), E2E `LoggingDisabled_*`, `SendLog_Click_*` (bis Plattformgrenze) | Abgedeckt |

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Schalter aktivieren → Button sichtbar → Protokolldatei entsteht | `LogProtocolTests.EnableLogging_ShowsSendButton_AndCreatesLogFile` | Abgedeckt |
| Deaktiviert → Button unsichtbar, keine Protokolldatei | `LogProtocolTests.LoggingDisabled_HidesSendButton_AndNoLogFile` | Abgedeckt |
| Persistenz über App-Neustart | `LogProtocolTests.LoggingEnabled_PersistsAcrossRestart` | Abgedeckt |
| Versand-Klick → Mail-/Share-Plattformaufruf ohne Absturz | `LogProtocolTests.SendLog_Click_InvokesPlatformShareOrMail_WithoutCrash` | Abgedeckt bis zur Plattformgrenze (Mail-Compose/Share-UI ist nicht automatisierbar — in der Anforderung akzeptiert) |

## Hinweise

- Der E2E-Nachweis des Mailentwurfs endet am `Email.ComposeAsync`/`Share`-Aufruf (Plattformdialoge außerhalb der App). Der Fehlerpfad ist zusätzlich über `logSendError` im Modell/DevApi beobachtbar; auf dieser Windows-Umgebung ist kein deterministischer „kein Mail-Client"-Zustand herstellbar — dokumentiert als Testlücke mit Begründung.
- Auf unpackaged Windows wirft `Email.ComposeAsync` erfahrungsgemäß `COMException` (0x80070032); der `Share`-Fallback ist dort der effektive Pfad — Designentscheidung trägt das.
