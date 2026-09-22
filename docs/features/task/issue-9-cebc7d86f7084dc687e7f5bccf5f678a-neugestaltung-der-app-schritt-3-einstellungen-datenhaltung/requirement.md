# Übersetzte Anforderung: Einstellungsverwaltung und lokale Datenhaltung mit Backup-Trennung

Branch: `task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app-schritt-3-einstellungen-datenhaltung`

## Fachliche Zusammenfassung

Die neue App `VideoPlayer.Maui` (MVU über `VideoPlayer.Mvu`) erhält eine persistente,
profil-isolierte Einstellungsverwaltung und eine klar strukturierte lokale Datenablage.
Die vorhandene `SettingsPage` (Grundgerüst aus Entwicklungsschritt 2) wird zu einer
bedienbaren Einstellungsseite ausgebaut, über die alle benutzerbezogenen Einstellungen
gelesen und geändert werden können; Änderungen laufen über den MVU-Zyklus
(Message → Update → Effekt → Persistierung am App-Rand). Parallel wird die lokale
Datenablage in backup-pflichtige und nicht backup-pflichtige Bereiche getrennt
(lokale Datenbank(en) plus Dateiablagen), primär für iOS umgesetzt (Backup-Ausschluss
bzw. Cache-Ablage) und auf Windows über ein Roaming-Äquivalent abgebildet.

## Akzeptanzkriterien (aus der Anforderung)

1. Alle genannten Einstellungen bleiben über App-Neustarts erhalten und sind über
   die Einstellungsseite änderbar.
2. Das Speicherplatzkontingent ist in Gigabyte pflegbar und mit 5 GB vorbelegt.
3. Die Backup-Klassifikation der Ablageorte ist umgesetzt und nachweisbar
   (z. B. durch Tests oder Plattformattribute).
4. Geheimnisse liegen weder in der normalen Einstellungsablage noch gehen sie ins
   Cloud-Backup (sichere Plattformablage; nach Wiederherstellung ggf. erneute
   Anmeldung erforderlich).
5. Unterschiedliche Profile (Isolation über Umgebungsvariable) beeinflussen sich
   nicht gegenseitig.

## Zu speichernde Einstellungen (benutzerbezogen, über Einstellungsseite pflegbar)

| Einstellung | Typ | Anmerkung |
|-------------|-----|-----------|
| Automatische Downloads | an/aus (bool) | Schalter |
| Anzahl vorzuladender Episoden | Ganzzahl | Anzahl |
| Auto-Fortsetzung | an/aus (bool) | Schalter |
| Zeitfenster Auto-Fortsetzung | Zeitdauer | Greift später bei Restlaufzeit unterhalb des Fensters |
| WLAN-only-Downloads | an/aus (bool) | Schalter |
| Zeitfenster „als angesehen" | Zeitdauer | Video gilt als angesehen, wenn Ende abzüglich Fenster erreicht |
| Speicherplatzkontingent Downloads | Gigabyte | Standardwert 5 GB |
| Automatische Löschung gesehener Episoden | an/aus (bool) | optional, Schalter |

## Betroffene Klassen und Komponenten

- **Datenmodellklassen (neu):** Einstellungsmodell (immutable Wertmenge aller
  Einstellungen, in `VideoPlayer.Mvu` bzw. einer plattformneutralen
  Ablagebibliothek); Entitäten der lokalen Datenbank(en) für Serverliste,
  zuletzt verbundener Server, Profildaten (ohne Geheimnisse), unsynchronisierte
  Fortschritte (backup-pflichtig) sowie Startseiten-Metadaten, Episodenlisten,
  Favoriten-Kopie und Download-Manifest (nicht backup-pflichtig).
- **Logikklassen / Services (neu):** Einstellungsdienst (lesen/schreiben,
  Profil-Präfixe, Defaults), Geheimnisablage-Dienst (Zugangsdaten, Anmelde-Token
  über `SecureStorage`), Profilverwaltung (Umgebungsvariable), Ablageort-Layout
  (Backup-/Cache-Trennung), Datenbankverwaltung (SQLite wie Referenz:
  `sqlite-net-pcl`).
- **Interfaces (neu):** Abstraktionen für normale Einstellungsablage
  (`Preferences`-Äquivalent), sichere Ablage (`SecureStorage`-Äquivalent) und
  Ablageort-Auflösung — damit die Persistenz logisch testbar bleibt (das
  Testprojekt kann keine MAUI-Ziele referenzieren).
- **MVU (erweitern):** `AppModel` um den geladenen Einstellungszustand; neue
  Messages (Einstellungen laden/geändert/gespeichert) und Effekte
  (Persistenz am App-Rand) in `VideoPlayer.Mvu`; `ShellAppEffectHandler`
  (in `VideoPlayer.Maui`) führt die Persistenz-Effekte aus.
- **UI-Komponenten:** `SettingsPage.xaml(.cs)` — Schalter und numerische
  Eingabefelder für alle genannten Einstellungen mit verständlichen
  Beschriftungen; Validierung numerischer Eingaben.
- **Tests:** xUnit-Tests in `VideoWebPlayer.Maui.Tests` für Einstellungsmodell
  (Defaults, Normalisierung), Persistenz (Roundtrip über Fakes der Ablage-
  Abstraktionen), Profil-Isolation, Ablageort-Layout/Backup-Klassifikation
  und Contract-Tests auf `SettingsPage.xaml`, `MauiProgram.cs` und die
  Plattform-Integration (iOS-Backup-Ausschluss).

## Implementierungsansatz

- Plattformneutrale Teile (Einstellungsmodell, Ablage-Abstraktionen,
  Ablageort-Layout, Profil-Logik, Datenbank-Struktur) werden so gelegt, dass sie
  aus dem `net10.0`-Testprojekt testbar sind — analog zur Entscheidung aus
  Schritt 2 (MVU-Kern in `VideoPlayer.Mvu`).
- MAUI-gebundene Adapter (`Preferences`, `SecureStorage`, `FileSystem`,
  iOS-Backup-Ausschluss via Plattform-API) liegen in `VideoPlayer.Maui` und
  werden per DI verdrahtet.
- Einstellungs-Änderungen laufen über den MVU-Zyklus: UI-Event → Message →
  `AppUpdate` → `PersistSettings`-Effekt → `IAppEffectHandler` schreibt über
  den Einstellungsdienst.
- `ServerAddressProvider` (liest derzeit `Preferences` direkt) wird auf die
  profilbewusste Ablage-Abstraktion umgestellt.
- Backup-Trennung: backup-pflichtige Ablagen unter einem gesicherten
  Stammverzeichnis; nicht backup-pflichtige Ablagen unter einem vom Backup
  ausgeschlossenen Stammverzeichnis (iOS: `NSFileManager.SetSkipBackupAttribute`
  bzw. `FileSystem.CacheDirectory` für Temp-Cache); Windows: backup-pflichtige
  Daten unter dem Roaming-Profilverzeichnis (`%APPDATA%`), nicht
  backup-pflichtige unter dem lokalen Profilverzeichnis — Roaming-Äquivalent
  prüfen und nutzen, sofern vorhanden.
- Profil-Isolation über Umgebungsvariable (neu: `VIDEOPLAYER_PROFILE`; Referenz
  der Alt-App: `VIDEOWEBPLAYER_PROFILE` mit `GetPreferencesPrefix`/
  `GetDatabaseFileName`).

## Konfiguration

- Benutzereinstellungen: plattformeigene Einstellungsablage (`Preferences`),
  profil-präfixierte Schlüssel.
- Geheimnisse: `SecureStorage` (iOS-Keychain / Windows-Credential-Manager
  bzw. plattformeigene sichere Ablage) — nicht in `Preferences`, nicht im
  Cloud-Backup.
- Umgebungsvariable `VIDEOPLAYER_PROFILE` wählt das aktive Profil
  (Standard: `default`).

## Offene Fragen

Keine — die Anforderung ist vollständig und eigenständig; konkrete
Standardwerte und Schranken der nicht näher spezifizierten Einstellungen werden
als Designentscheidungen im Plan festgelegt.
