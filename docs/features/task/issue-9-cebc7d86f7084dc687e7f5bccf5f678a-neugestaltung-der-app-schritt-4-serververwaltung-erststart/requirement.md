# Übersetzte Anforderung: Serververwaltung und Erststart-Einrichtung

Branch: `task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app-schritt-4-serververwaltung-erststart`

## Fachliche Zusammenfassung

Die neue App `VideoPlayer.Maui` (MVU über `VideoPlayer.Mvu`, Persistenz über
`VideoPlayer.Storage`, API über `VideoPlayer.ApiClient`) erhält eine
Mehr-Server-Verwaltung: eine Liste bekannter Server wird in der
backup-pflichtigen `BackupDatabase` (`known_servers`) gespeichert, der zuletzt
verbundene Server wird persistiert (`backup_state` / `LastConnectedServer`) und
beim App-Start automatisch verwendet. Beim Erststart ohne hinterlegten Server
erscheint ein Einrichtungsdialog mit zwei Wegen: manuelle Eingabe einer
Serveradresse und automatische Erkennung im lokalen Netzwerk über das
bestehende Discovery-Protokoll (mDNS-Dienstsuche `_http._tcp.local.` sowie
UDP-Broadcast `VIDEOWEBPLAYER_DISCOVERY` → `VIDEOWEBPLAYER_SERVER:<adresse>`
auf Port 5001). Gefundene Server werden zur Auswahl angeboten; nur eine
erfolgreich per `CheckHealthAsync` geprüfte Verbindung wird gespeichert. In den
Einstellungen gibt es einen bewusst unauffälligen Eintrag „Server wechseln",
der bekannte Server listet, das Hinzufügen erlaubt und den Wechsel ohne Verlust
der übrigen Einstellungen ermöglicht.

## Akzeptanzkriterien (aus der Anforderung)

1. Erststart ohne hinterlegten Server führt zum Einrichtungsdialog.
2. Automatisch gefundene Server werden zur Auswahl angeboten.
3. Nach erfolgreicher Einrichtung startet die App künftig direkt mit dem
   gespeicherten Server.
4. „Server wechseln" listet bekannte Server, erlaubt das Hinzufügen und den
   Wechsel ohne Verlust der übrigen Einstellungen.

## Betroffene Klassen und Komponenten

- **Datenmodell (bestehend, `VideoPlayer.Storage`):** `KnownServerEntry`
  (Tabelle `known_servers`: `Id`, `Address`, `DisplayName`, `LastUsedAtUtc`),
  `BackupStateEntry` (Tabelle `backup_state`, Schlüssel
  `LastConnectedServerKey = "LastConnectedServer"`) — beide bereits in
  `BackupDatabase` angelegt, noch ohne Zugriffsschicht.
- **Logikklassen / Services (neu):** Serververwaltungsdienst (bekannte Server
  lesen/hinzufügen, zuletzt verbundenen Server lesen/setzen — in
  `VideoPlayer.Storage` über `BackupDatabase`); Discovery-Dienst für
  mDNS-Dienstsuche und UDP-Broadcast-Abfrage; Verbindungsprüfung einer
  konkreten Adresse (Adresse normalisieren + `VideoPlayerApiClient.CheckHealthAsync`).
- **Interfaces (neu):** Abstraktion der Serververwaltung und der
  Netzwerk-Discovery, damit die Logik plattformneutral testbar bleibt
  (Testprojekt kann kein MAUI referenzieren).
- **MVU (erweitern, `VideoPlayer.Mvu`):** `AppModel` um Serverzustand
  (kein Server hinterlegt → Erststart-Einrichtung, Liste bekannter/gefundener
  Server, Prüfstatus); neue `AppMessage`/`AppEffect`-Typen für Serverliste
  laden, Server prüfen/speichern/wechseln und Discovery ausführen;
  `AppPage` ggf. um die Einrichtungsseite; Start-Workflow verzweigt bei
  fehlendem Server in den Einrichtungsdialog statt in die Health-Prüfung
  gegen die Default-Adresse.
- **UI (`VideoPlayer.Maui`):** Einrichtungsdialog/-seite beim Erststart
  (manuelle Adresseingabe + „Automatisch suchen" + Auswahl gefundener
  Server); unauffälliger Eintrag „Server wechseln" auf der `SettingsPage`,
  der die Serververwaltung öffnet (bekannte Server anzeigen, hinzufügen,
  wechseln).
- **Discovery-Verifikation (extern):** serverseitige Beantwortung des
  Discovery-Protokolls am lokalen IIS „VideoPlayer" verifizieren und
  dokumentieren; fehlende serverseitige Beantwortung als Issue im
  Server-Repository melden statt Workaround in der App.
- **Tests (`VideoWebPlayer.Maui.Tests`):** Serververwaltung (Roundtrips über
  `BackupDatabase`, zuletzt verbundener Server), Discovery-Protokoll-Parsing
  und -Ablauf (über Abstraktionen/Stubs), MVU-Übergänge (Erststart-Verzweigung,
  Einrichtung, Wechsel), Contract-Tests auf die neuen/geänderten
  XAML-/DI-Artefakte.

## Implementierungsansatz

- Serverliste und zuletzt verbundener Server sind backup-pflichtig → Ablage in
  `BackupDatabase` (Entitäten vorhanden); Zugriff über einen neuen
  `KnownServersService` in `VideoPlayer.Storage` (wie `AppSettingsService`
  plattformneutral und testbar).
- `ServerAddressProvider` wird auf die Serververwaltung umgestellt: zuletzt
  verbundener Server (backup-pflichtig) statt des einzelnen
  `Preferences`-Schlüssels `ServerAddress`; Env-Variable
  `VIDEOPLAYER_SERVER_URL` bleibt als Fallback; der implizite IIS-Default
  greift nicht mehr als „hinterlegter Server" (Erststart-Erkennung!).
- Discovery wird neu implementiert (kein Altbestand-Code): UDP-Broadcast
  `VIDEOWEBPLAYER_DISCOVERY` → `VIDEOWEBPLAYER_SERVER:<adresse>` auf Port 5001
  ist in `net10.0` plattformneutral realisierbar (`System.Net.Sockets`);
  mDNS über das Zeroconf-Paket (Alt-App nutzt `Zeroconf 3.7.16`).
  Gefundene Adressen werden vor der Übernahme per `CheckHealthAsync` geprüft.
- Start-Workflow: `StartupRequestedMessage` lädt Einstellungen **und**
  Serververwaltung; ohne hinterlegten Server → `CurrentPage = ServerSetup`
  (Einrichtungsdialog); mit Server → bisherige Health-Prüfung → `Home`.
- „Server wechseln" in den Einstellungen öffnet denselben
  Serververwaltungs-Dialog; Wechsel aktualisiert `LastConnectedServer` und
  stößt eine neue Health-Prüfung an, ohne `AppSettings` anzutasten.

## Konfiguration

- `VIDEOPLAYER_SERVER_URL` (bestehend): Fallback-Serveradresse, wenn kein
  Server hinterlegt ist (Entwicklung/Tests).
- Discovery-Parameter als Konstanten: UDP-Port 5001, Abfrage-/Antwort-Schema
  (`VIDEOWEBPLAYER_DISCOVERY` / `VIDEOWEBPLAYER_SERVER:`), mDNS-Diensttyp
  `_http._tcp.local.`, Timeouts.

## Offene Fragen

Keine — die Anforderung und der Projektkontext geben Vorgehen, Ablageort und
Protokollparameter verbindlich vor. Das Ergebnis der Discovery-Verifikation
(gegebenenfalls zu meldende Server-Issues) wird dokumentiert.
