# Übersetzte Anforderung: Protokollierung und Protokollversand per Mail

Quelle: Issue #43, Entwicklungsschritt 3 von 8 („Protokollierung und Protokollversand per Mail").
Basisbranch: `task/issue-43-3ee1dbce5f284347add37dde0e222b44-folge-direkte-wiedergabe-aus-w`

## Fachliche Zusammenfassung

Die App erhält eine aktivierbare Datei-Protokollierung: Eine neue, persistente
Einstellung „Protokollierung" (Schalter auf der `SettingsPage`) schaltet das
Schreiben eines Programmprotokolls in eine Logdatei unterhalb des
`AppStorageLayout` frei bzw. sperrt es. Bei aktivierter Protokollierung zeigt
die Einstellungsseite einen „Protokoll senden"-Button, der per
`Email.ComposeAsync` einen Mailentwurf mit der Protokolldatei als Anhang an
`mstromberg84+heimkino@gmail.com` öffnet. Ist kein Mail-Client verfügbar,
erhält der Anwender einen verständlichen Hinweis (Fallback: `Share`).

## Betroffene Klassen und Komponenten

- **Datenmodell / Einstellungen:**
  - `VideoPlayer.Storage.AppSettings` — neue Eigenschaft `LoggingEnabled` (`bool`, Standard `false`).
  - `VideoPlayer.Storage.AppSettingsService` — neuer Ablageschlüssel `LoggingEnabledKey`, Laden/Speichern.
  - `VideoPlayer.Storage.AppStorageLayout` — neues Protokollverzeichnis (nicht backup-pflichtig).
- **Logik / Services (neu):**
  - Datei-Logging-Dienst in `VideoPlayer.Storage` (schreibt zeitgestempelte Zeilen in die Protokolldatei; ein-/ausschaltbar zur Laufzeit).
  - `ILoggerProvider`/`ILogger`-Implementierung in `VideoPlayer.Storage` (benötigt Paket `Microsoft.Extensions.Logging.Abstractions`), damit das MAUI-`ILoggingBuilder`-System und injizierbare `ILogger<T>` ins Protokoll schreiben.
- **MVU:**
  - `AppMessage`: neue Records „Protokollversand angefordert" und „Protokollversand abgeschlossen" (Fehlertext).
  - `AppEffect`: neuer Record für den Protokollversand.
  - `AppModel`: neues Feld für die Fehler-/Hinweis-Anzeige des Protokollversands.
  - `AppUpdate`: neue Switch-Zweige mit Guard (Versand nur bei aktivierter Protokollierung).
- **UI:**
  - `SettingsPage.xaml` — neuer Abschnitt „Protokoll" mit Schalter `LoggingSwitch`, Button `SendLogButton` (nur bei aktivierter Protokollierung sichtbar) und Fehler-/Hinweis-Label.
  - `SettingsPage.xaml.cs` — Verdrahtung der neuen Bedienelemente auf `MvuHost.DispatchAsync`.
  - `ShellAppEffectHandler` — Ausführung des Versand-Effekts (`Email.ComposeAsync`, Fallback `Share`), Aktivierung/Deaktivierung des Datei-Loggings beim Laden/Speichern der Einstellungen, Routing der bestehenden `Debug.WriteLine`-Diagnosemeldungen zusätzlich in den `ILogger`.
  - `MauiProgram` — Registrierung von Protokolldienst und `ILoggerProvider`; Startzustand aus den persistenten Einstellungen.
- **Developer-API / E2E:**
  - `DevApiModelSnapshot` + E2E-`DevApiModel`: neue Felder für `LoggingEnabled` und den Versand-Fehlertext (Beobachtbarkeit im E2E).
- **Tests:**
  - Unit-Tests `VideoWebPlayer.Maui.Tests` (Einstellung, Protokolldienst, `AppUpdate`-Übergänge, Quelltext-Verträge der MAUI-Verdrahtung).
  - E2E-Tests `VideoPlayer.E2E.Tests` (Sichtbarkeit des Buttons, Schalter-Interaktion, Protokolldatei entsteht, Persistenz über Neustart; Mail-Compose selbst ist plattformseitig).

## Implementierungsansatz

- Die Einstellung folgt der bestehenden Konvention: `AppSettings`-Eigenschaft + `AppSettingsService`-Schlüssel + Profil-Präfix über `ISettingsStore` (`Preferences`).
- Das Datei-Logging wird als `ILoggerProvider` umgesetzt und in `MauiProgram.CreateMauiApp` registriert; der Schaltzustand stammt synchron aus `AppSettingsService.Load()` und wird zur Laufzeit beim Laden/Speichern der Einstellungen im `ShellAppEffectHandler` nachgeführt.
- Ablageort: neues `LogsDirectory` unterhalb des nicht backup-pflichtigen Verzeichnisses des `AppStorageLayout` (profil-isoliert); die Protokolldatei heißt `videoplayer.log` (Größenrotation zur Begrenzung).
- Protokollversand als MVU-Effekt: `SettingsPage` dispatcht eine Anforderungs-Nachricht; `AppUpdate` emittiert den Effekt (Guard auf aktivierte Protokollierung); `ShellAppEffectHandler` führt `Email.ComposeAsync` mit `EmailAttachment` auf dem Hauptthread aus; `FeatureNotSupportedException`/sonstige Fehler → Fallback `Share.RequestAsync` bzw. verständlicher Fehlertext im Modell (Inline-Hinweis auf der Einstellungsseite).
- Empfänger: `mstromberg84+heimkino@gmail.com` (Konstante).

## Konfiguration

Benutzereinstellung auf Anwendungsebene (pro App-Profil persistent über `ISettingsStore`/`Preferences`): `LoggingEnabled`, Standard `false`.

## Offene Fragen

Keine — Versandweg (`Email.ComposeAsync`), Fehlerverhalten (Hinweis bzw. `Share`-Fallback), Sichtbarkeitsregel (Button nur bei aktivierter Protokollierung) und Empfängeradresse sind in der Anforderung verbindlich beschlossen.
