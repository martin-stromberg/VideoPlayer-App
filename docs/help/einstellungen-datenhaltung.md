# Einstellungen und lokale Datenhaltung (Entwicklungsschritt 3)

Die neue VideoPlayer-App besitzt eine ausgebaute Einstellungsseite und eine
strukturierte lokale Datenablage mit klarer Backup-Trennung.

## Einstellungsseite

Die Seite „Einstellungen" ist über das Seitenmenü erreichbar. Alle Werte werden
sofort übernommen und bleiben nach dem Neustart der App erhalten.

### Darstellung

- **Kopfbereich anzeigen** — blendet den Hero-Kopfbereich der Startseite
  (servergeneriertes Hintergrundbild mit Titel) ein oder aus
  (Standard: ein).

### Downloads

- **Automatische Downloads** — schaltet den automatischen Vorlauf der
  Weiterschauen-Einträge ein oder aus (aktuelle und folgende Episoden
  bzw. Playlist-Einträge).
- **Anzahl vorzuladender Episoden** — wie viele Titel je
  Weiterschauen-Eintrag vorgehalten werden, einschließlich des
  aktuellen (1–10, Standard 3).
- **Downloads nur im WLAN** — lädt nur, wenn eine WLAN-Verbindung besteht
  (Standard: ein).
- **Speicherplatzkontingent für Downloads (GB)** — Obergrenze des
  Speicherplatzes für Downloads in Gigabyte (1–500, Standard 5).
- **Gesehene Episoden automatisch löschen** — entfernt lokal gespeicherte
  Episoden, nachdem sie angesehen wurden (Standard: aus).

### Wiedergabe

- **Auto-Fortsetzung** — setzt begonnene Videos automatisch an der letzten
  Position fort (Standard: ein).
- **Zeitfenster Auto-Fortsetzung** — Restlaufzeit in Sekunden, unterhalb derer
  nicht mehr fortgesetzt, sondern von vorn gestartet wird (30–3600,
  Standard 300).
- **Als angesehen ab … Sekunden vor Ende** — ein Video gilt als angesehen,
  wenn das Ende abzüglich dieses Fensters erreicht ist (30–3600,
  Standard 300).

### Protokoll

- **Protokollierung** — schreibt ein Programmprotokoll (Diagnoseausgaben
  und Fehler der App) in die Protokolldatei `videoplayer.log` im
  Protokollverzeichnis (`logs/` innerhalb der nicht backup-pflichtigen
  Ablage; Standard: aus). Das Schalten wirkt sofort — beim Ein- und
  Ausschalten wird eine Marker-Zeile ins Protokoll geschrieben. Ab
  1 MiB wird die Datei nach `videoplayer.previous.log` rotiert; beide
  Dateien werden beim Versand angehängt.
- Bei eingeschalteter Protokollierung enthält die Datei sparsame
  Ablauf-Einträge, anhand derer sich der Weg der App nachvollziehen
  lässt: App-Start, Serverprüfung und Quellenwechsel, An- und Abmeldung
  (nur das Ergebnis), Sitzungsablauf, Verbindungsauf- und -abbau der
  Echtzeit-Ereignisse, Auflösung der Wiedergabequelle (Medientyp und
  Quellart, z. B. Server-Stream oder lokale Datei), Wiedergabe-Start,
  -Ende und -Fehlschlag, Bild-im-Bild-Umschaltungen, das Ergebnis der
  Synchronisation mit Zählern, Download-Aufträge und deren Verlauf,
  den automatischen Download-Lauf sowie Wechsel zwischen Online- und
  Offline-Betrieb. Überholbare Störungen (z. B. ein fehlgeschlagener
  Einzelabruf) erscheinen als Warnung, Fehlschläge als Fehlereintrag.
- Das Protokoll bleibt bewusst knapp: der laufende Fortschrittstakt
  der Wiedergabe, einzelne Listeneinträge und Bildabrufe erzeugen
  keine Einträge — sonst würde die Datei unnütz zulaufen. Es enthält
  keine Geheimnisse: Zugangsdaten, Benutzernamen und Anmelde-Tokens
  werden grundsätzlich nicht protokolliert; taucht ein Token-Wert
  doch einmal in einer technischen Fehlermeldung auf, wird er vor
  dem Schreiben maskiert. Die Datei kann daher bedenkenlos an den
  Support weitergegeben werden.
- **Protokoll senden** — nur sichtbar, solange die Protokollierung aktiv
  ist: öffnet einen E-Mail-Entwurf an `mstromberg84+heimkino@gmail.com`
  mit dem Protokoll als Anhang. Kann kein Mailentwurf geöffnet werden
  (kein Mail-Client, unpackaged Windows), greift der Teilen-Dialog als
  Fallback; schlägt auch dieser fehl, erscheint ein verständlicher
  Hinweis in der Karte.

Zahlenfelder werden beim Verlassen oder Bestätigen übernommen. Ungültige
Eingaben werden nicht gespeichert — ein Hinweis erscheint und der letzte
gültige Wert wird wieder angezeigt. Werte außerhalb der zulässigen Bereiche
werden automatisch auf die Grenzen gesetzt.

## Lokale Datenhaltung und Backup-Trennung

Die App trennt ihre Daten in drei Bereiche:

- **Backup-pflichtig** — Serverliste, zuletzt verbundener Server, Profildaten
  ohne Geheimnisse, App-Einstellungen und noch nicht zum Server übermittelte
  Wiedergabefortschritte. Diese Daten liegen in der Datenbank
  `videoplayer.db3` im Backup-Verzeichnis.
  - iOS/MacCatalyst/Android: unterhalb der App-Ablage (Bestandteil des
    Geräte-Backups).
  - Windows: im servergespeicherten (Roaming-)Profilverzeichnis
    `%APPDATA%\VideoPlayer` — das Windows-Äquivalent zum Geräte-Backup.
- **Nicht backup-pflichtig** — Offline-Videos (`videos/`), Vorschaubilder
  (`previews/`), Programmprotokolle (`logs/`), Startseiten-Metadaten,
  Episodenlisten, Download-Manifest und
  die lokale Favoriten-Kopie in `videoplayer-cache.db3`. Diese Daten sind
  jederzeit vom Server neu aufbaubar und werden vom Backup ausgeschlossen:
  - iOS/MacCatalyst: `nobackup`-Verzeichnis mit
    `NSFileManager.SetSkipBackupAttribute` (plattformüblicher Ausschluss).
  - Android: `NoBackupFilesDir`.
  - Windows: lokale App-Ablage ohne Roaming.
- **Temp-Cache** — flüchtige Daten im plattformeigenen Cache-Verzeichnis
  (`FileSystem.CacheDirectory`), das nie gesichert wird und vom System
  aufgeräumt werden kann.

## Geheimnisse

Passwörter, Anmelde-Token und gepaarte Geräte-Token (aus der
Geräte-Kopplung per Einmal-Code, siehe
[Serververwaltung](serververwaltung-erststart.md)) werden ausschließlich
in der sicheren Plattformablage (`SecureStorage`: iOS-Keychain,
Windows-Credential-Ablage) gespeichert. Sie gelangen weder in die
normale Einstellungsablage noch ins Cloud-Backup. Nach einer
Wiederherstellung auf einem neuen Gerät ist daher eine erneute Anmeldung
erforderlich; das Gerät wird zudem erneut gekoppelt.

## Profile

Über die Umgebungsvariable `VIDEOPLAYER_PROFILE` lassen sich getrennte
Profile betreiben (Standard: `default`). Jedes Profil besitzt eigene
Einstellungs-Schlüssel (Präfix) und eigene Ablage-Verzeichnisse — Profile
beeinflussen sich nicht gegenseitig.

## Technischer Aufbau

- `VideoPlayer.Storage` (plattformneutral): `AppSettings`,
  `AppSettingsService`, `AppSecretsService`, `AppProfile`,
  `AppStorageLayout`, `BackupDatabase`, `CacheDatabase` (SQLite über
  `sqlite-net-pcl`).
- `VideoPlayer.Maui`: Plattform-Adapter `PreferencesSettingsStore`,
  `SecureStorageSecretStore`, `AppStorageLayoutProvider`; Registrierung in
  `MauiProgram`; Ausführung über `ShellAppEffectHandler`.
- MVU-Strecke: Start → `InitializeStorageEffect` + `LoadSettingsEffect` →
  `SettingsLoadedMessage`; Änderung auf der Seite →
  `SaveSettingsRequestedMessage` → `PersistSettingsEffect`.

## Technischer Aufbau des Protokolls

- **Senke:** `AppLogService` (schaltbarer Datei-Speicher,
  `SetEnabled` aus dem Einstellungs-Fluss; Marker beim Ein-/Ausschalten,
  Rotation ab `MaxLogFileBytes` = 1 MiB auf
  `videoplayer.previous.log`, `GetAttachmentPaths` für den Versand).
  `FileLogger`/`FileLoggerProvider` bilden die `ILogger`-Brücke
  (Mindestlevel `Information`); `MauiProgram` registriert den Provider
  als `ILoggerProvider`-Singleton und aktiviert die Logging-Pipeline
  explizit über `builder.Services.AddLogging()` — ohne die Aktivierung
  liefert der `MauiAppBuilder` eine `NullLoggerFactory`, die
  DI-registrierte Provider ignoriert (im Debug übernimmt das
  `AddDebug()`, im Release würde die Brücke sonst ins Leere laufen).
  `FileLogger.IsEnabled` filtert bei
  deaktiviertem Protokoll früh — die Aufrufe bleiben kostenneutral.
- **Schreiber:** Alle App-Dienste erhalten injizierte `ILogger<T>`
  (plattformneutrale Dienste optional mit `NullLogger`-Fallback,
  ausschließlich DI-erzeugte MAUI-Klassen als Pflichtparameter;
  `AuthSessionService` nimmt eine `ILoggerFactory`, um neben dem
  eigenen Logger auch `VideoPlayerApiClientOptions.Logger` des
  API-Clients und den intern erzeugten `DeviceTokenService` zu
  versorgen). `ShowDetailsCache` als statische Hilfsklasse erhält den
  Logger als Methodenparameter der Aufrufer. Frühere
  `Debug.WriteLine`-Diagnosen sind vollständig ersetzt
  (Ausnahmen: Altbestand `VideoWebPlayer.Maui`, Discovery in
  `VideoPlayer.ApiClient`); der Contract-Test
  `NoDebugWriteLine_RemainsInAppSources` pinnt den Restbestand.
- **Breadcrumb-Prinzip:** Genau ein Eintrag je Ereignis an genau einer
  Schicht — Dienste protokollieren ihr Ergebnis, der
  `ShellAppEffectHandler` nur noch Fehlerpfade und Handler-eigene
  Ergebnisse (Ablage-Initialisierung, Wiedergabe-Start). Rauschfeste
  Pfade laufen auf `Debug` (unterhalb der `Information`-Schwelle):
  Teil-Synchronisationen nach Hub-Ereignissen, Hub-Ereignis-Eingänge,
  Download-Hinweise ohne Zustandswechsel.
- **Geheimnis-Schutz:** `AppLogService.MaskAccessToken` ersetzt den
  Wert jeder `access_token`-Query in Meldung und Exception-Text am
  Sinkenpunkt; `PlayerPage` bereinigt plattformgenerierte Meldungen
  vorab mit derselben Funktion. Protokolliert werden Medientyp/-Id,
  `PlaybackSourceKind`, Status und Zähler — niemals
  `PlaybackSource.Location`, `AuthAttempt.Username`, Passwörter oder
  Tokens.
