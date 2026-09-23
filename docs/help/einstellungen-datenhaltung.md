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

- **Automatische Downloads** — schaltet den automatischen Abruf neuer Episoden
  ein oder aus.
- **Anzahl vorzuladender Episoden** — wie viele Episoden vorgehalten werden
  (1–10, Standard 3).
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
  (`previews/`), Startseiten-Metadaten, Episodenlisten, Download-Manifest und
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
