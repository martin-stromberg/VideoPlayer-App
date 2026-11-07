# Serververwaltung und Erststart-Einrichtung (Entwicklungsschritte 4 + 5b)

Die neue VideoPlayer-App verwaltet mehrere bekannte Server, merkt sich den
zuletzt verbundenen Server und startet künftig direkt mit ihm. Beim ersten
Start ohne hinterlegten Server erscheint ein Einrichtungsdialog. Über den
Kopplungs-QR-Code der Serververwaltung (oder die Ticket-Adresse bzw. den
Kurzcode) ist die komplette Einrichtung in einem Schritt möglich — ohne
Adresseingabe, Einmal-Code und Anmeldemaske.

## Erststart-Einrichtung

Ist noch kein Server hinterlegt, zeigt die App nach dem Start die Seite
„Server" statt der Startseite. Die Seite ist in Karten gegliedert:
oben der Kopfbereich mit der Karte des aktiven Servers inklusive
Status-Badge („Server: erreichbar" / „Server: nicht erreichbar"),
darunter die Listen bekannter und im Netzwerk gefundener Server als
auswählbare Karten und im Fuß die Karte „Neuen Server verbinden".
Jeder laufende Schritt nennt seine Phase in Textform („Suche läuft …",
„Verbindung wird geprüft …", „Kopplung läuft …"). Zwei Wege führen
zum Ziel:

- **Server im Netzwerk suchen** — die App sucht im lokalen Netzwerk nach
  VideoPlayer-Servern (mDNS-Dienstsuche auf `_http._tcp.local.` sowie
  UDP-Broadcast `VIDEOWEBPLAYER_DISCOVERY` auf Port 5001). Gefundene Server
  erscheinen als auswählbare Liste; ein Tipp auf einen Eintrag übernimmt
  ihn. Liefert die Suche keinen Treffer, weist ein Hinweis auf die
  manuelle Eingabe hin.
- **Adresse selbst eingeben** — die Serveradresse kann direkt eingegeben
  werden (z. B. `http://server/videoplayer/`; ein fehlendes Schema wird
  automatisch als `http://` ergänzt). „Verbinden" prüft die Eingabe.

Die zwei Wege liegen als Kacheln nebeneinander auf der Seite: links
„Mit QR-Code verbinden" (siehe unten), rechts „Adresse selbst eingeben".
In beiden Fällen wird die Verbindung zunächst geprüft (`GET api/health`);
nur ein erreichbarer Server wird gespeichert und übernommen. Ungültige
Adressen oder nicht erreichbare Server erzeugen einen Inline-Hinweis —
es wird nichts gespeichert. Nach erfolgreicher Einrichtung startet die App
künftig direkt mit dem gespeicherten Server.

## Kopplung per QR-Code oder Kurzcode (Schritt 5b)

Der schnellste Einstieg ist die Karte „Mit Kopplungscode verbinden" —
sie braucht keine Adresseingabe und keine Anmeldung:

- **QR-Code scannen (iOS/Android):** die Serververwaltung
  (Profilseite „Geräte") stellt einen QR-Code bereit, der eine
  Ticket-Adresse der Form `https://<server>/pairing?t=<ticket>`
  enthält. „QR-Code scannen" öffnet eine Kamera-Seite (beim ersten
  Mal fragt das System nach der Kamera-Berechtigung); der gelesene
  Code wird direkt eingelöst.
- **Ticket-Adresse eingeben:** derselbe Inhalt lässt sich manuell
  eingeben oder einfügen — die App zerlegt die Adresse in Server und
  Ticket; Unterpfade der Serveradresse bleiben erhalten.
- **Kurzcode eingeben:** alternativ löst die Serververwaltung den
  Kurzcode (8 Zeichen) aus — dieser Weg nutzt den bereits verbundenen
  Server; ohne verbundenen Server weist ein Hinweis darauf hin.

Bei einer Ticket-Adresse prüft die App den Server zuerst auf
Erreichbarkeit; nur ein erreichbarer Server wird gespeichert und
übernommen. Danach löst `POST api/pairing/bootstrap` das Ticket gegen
ein verschlüsseltes Session-Paket (ECDH P-256, AES-256-GCM — derselbe
Handshake wie das klassische Pairing) ein: Geräte-Token, Anmelde-Token
und Refresh-Token liegen anschließend sicher in der Plattformablage —
die App landet ohne Anmeldemaske direkt auf der Startseite. Ein
8-stelliger Kurzcode geht als Ticket-Alias durch denselben Endpunkt.

Fehler erscheinen inline in der Karte und zusätzlich als Dialog:
unlesbare Eingabe, Server nicht erreichbar (wird dann nicht übernommen),
Ticket ungültig oder abgelaufen, zu viele Versuche, Server ohne
Bootstrap-Endpunkt.
Klappt die Verbindung, wechselt die Seite nicht zur Anmeldemaske —
sie zeigt stattdessen zwei Möglichkeiten nebeneinander: den
**Kurzcode** (wird gegen den verbundenen Server eingelöst) und die
**Anmeldung mit Zugangsdaten** (Benutzername/Passwort). Verlangt der
Server ein Geräte-Pairing, liegt der Einmal-Code-Austausch zuerst in
der Zugangsdaten-Kachel; danach erscheinen dort die Anmeldefelder.
Die klassische Anmeldemaske bleibt für die übrigen Fälle bestehen
(Sitzung abgelaufen, gespeicherter Server beim App-Start); auch dort
steht der Kurzcode-Abschnitt zur Verfügung. Auf Windows gibt es keinen
Kamera-Scan — dort steht die manuelle Eingabe zur Verfügung.

Die Sitzung erneuert sich anschließend selbst: der Refresh-Token wird
bei Bedarf über `POST api/auth/refresh` rotiert (der alte ist ab da
ungültig); das Abmelden widerruft den Refresh-Token zusätzlich am
Server (`POST api/auth/logout`).

## Gerät koppeln (Pairing)

Ist der Server erreichbar, aber weder ein gepaartes Geräte-Token noch ein
statischer Gate-Schlüssel vorhanden, blendet die Serverseite die
hervorgehobene Karte „Gerät koppeln" ein (Badge „NÄCHSTER SCHRITT",
Führungstext mit Weg zur Server-Verwaltung):

- Der Einmal-Code wird in der Verwaltung des Servers (Bereich „Geräte")
  erzeugt und hier eingegeben (in der Regel 8 Zeichen, Großbuchstaben
  und Ziffern ohne 0, O, 1, I und L; Kleinbuchstaben und Leerzeichen
  werden normalisiert). „Koppeln" tauscht den Code über den
  öffentlichen Endpunkt `POST api/pairing/exchange` gegen ein
  verschlüsseltes Geräte-Token (ECDH P-256, AES-256-GCM). Die App meldet
  dabei den Gerätenamen mit — er erscheint in der Geräteliste des
  Servers und erleichtert ein späteres Widerrufen.
- Fehler erscheinen inline: unvollständige oder falsch formatierte
  Eingabe, ungültiger/abgelaufener/bereits verwendeter Code, zu viele
  Versuche (vorübergehende Sperre), Server ohne Pairing-Endpunkt oder
  Verbindungsfehler. Eine erneute Eingabe ist jederzeit möglich; es gibt
  keinen automatischen Rückfall auf einen statischen Token.
- Nach erfolgreicher Kopplung läuft die gewohnte automatische Anmeldung
  an.

Das Geräte-Token liegt in der sicheren Plattformablage
(`AppSecretsService`), profil- und serveradress-spezifisch — es ist nicht
Teil des Geräte-Backups. Verwirft der Server das Token (Gate-Ablehnung),
wird es entfernt, einmalig der statische Schlüssel geprüft und danach die
Pairing-Eingabe mit einem Hinweis erneut angezeigt.

## Server wechseln

In den „Einstellungen" führt der Eintrag „Server wechseln" (Abschnitt
„Server" am Seitenende) zur selben Verwaltungsseite:

- Der **aktive Server** erscheint als eigene Karte im Kopfbereich —
  mit Status-Badge, das die laufende Erreichbarkeitsprüfung und das
  Ergebnis („erreichbar"/„nicht erreichbar") anzeigt.
- Die **bekannten Server** liegen als Kartenliste darunter (zuletzt
  verwendet zuerst; je Karte Name, Adresse und „Zuletzt verwendet").
- Ein Tipp auf einen bekannten Server wechselt nach erfolgreicher
  Verbindungsprüfung zu ihm.
- Neue Server lassen sich über die Netzwerk-Suche oder die manuelle
  Eingabe hinzufügen — sie werden erst nach erfolgreicher Prüfung in die
  Liste aufgenommen.

Der Wechsel verändert ausschließlich die Serverzuordnung; alle übrigen
Einstellungen bleiben unverändert erhalten.

## Ablageort

Bekannte Server und der zuletzt verbundene Server liegen in der
backup-pflichtigen Datenbank `videoplayer.db3` (Tabelle `known_servers`
sowie `backup_state`-Schlüssel `LastConnectedServer`) und sind damit Teil
des Geräte-Backups bzw. unter Windows des Roaming-Profils — siehe
[Einstellungen und lokale Datenhaltung](einstellungen-datenhaltung.md).

Eine in der Einstellungsablage hinterlegte `ServerAddress` aus der
Vorgängerversion wird beim ersten Laden automatisch in die Serverliste
übernommen (Migration) und dort entfernt.

Die Umgebungsvariable `VIDEOPLAYER_SERVER_URL` gilt weiterhin als
konfigurierte Serveradresse (Entwicklungs-Fallback); ohne hinterlegten
Server und ohne die Variable erscheint der Einrichtungsdialog. Die lokale
IIS-Adresse `http://localhost/videoplayer/` bleibt letzter Auflösungs-
Fallback für API-Aufrufe, gilt aber nicht als „hinterlegter Server".

## Technischer Aufbau

- `VideoPlayer.Storage`: `ServerAddress` (kanonische Normalisierung
  `scheme://host[:port][/pfad]/`), `KnownServer`, `KnownServersService`
  (Liste, aktiver Server, Legacy-Migration).
- `VideoPlayer.ApiClient.Discovery`: `DiscoveryProtocol` (Konstanten +
  Antwort-Parsing), `IServerDiscovery`, `UdpServerDiscovery`,
  `MdnsServerDiscovery` (Zeroconf), `ServerDiscoveryService`
  (parallele Probes, Merge + Deduplizierung, Fehlerisolation).
- `VideoPlayer.Mvu`: `AppPage.ServerSetup`, Model-Erweiterung
  (`KnownServers`, `ActiveServerAddress`, `DiscoveredServers`,
  Lauf-/Fehlerflags, `IsPairingRequired`/`IsPairingRunning`/`PairingError`,
  `IsBootstrapRunning`/`BootstrapError`),
  Messages/Effekte für Laden, Discovery, Prüfung, Übernahme, Pairing und
  Bootstrap (`PairingCodeSubmitted`, `PairingCompleted`,
  `DeviceTokenRevoked`, `BootstrapTicketSubmitted`,
  `BootstrapServerChecked`, `BootstrapCompleted` /
  `ExchangePairingCode`, `CheckBootstrapServer`,
  `ExchangeBootstrapTicket`); `PairingCode`
  (Normalisierung/Validierung), `BootstrapTicketParser` (Zerlegung der
  Ticket-Adresse in Server + opakes Ticket bzw. Kurzcode),
  `BootstrapStatus`, `DeviceTokenService` (Provider-Pflege, Gate-Probe
  per `POST api/auth/login` — `api/health` ist serverseitig nicht
  gate-geschützt —, Widerruf-Ereignis `DeviceTokenRejected`);
  `AuthSessionService` (Refresh-Token-first in der stillen
  Neuanmeldung, best-effort Widerruf beim Logout);
  der Start-Workflow lädt die Serververwaltung vor dem Health-Check.
- `VideoPlayer.Maui`: `Pages/ServerSetupPage` (Erststart als Window-Root,
  „Server wechseln" als Shell-Route `serversetup` ohne Menüeintrag;
  kartenbasierte Oberfläche — Aktiver-Server-Karte mit
  `ServerHealth`-Status-Badge, Karten für bekannte/gefundene Server,
  Verbinden-, Kopplungs- und geführte Pairing-Karte, alle Zustände aus
  `AppModel`), `Pages/QrScannerPage` (modale Kamera-Seite auf
  iOS/Android, ZXing.Net.Maui `CameraBarcodeReaderView`),
  `ServerAddressProvider` (zuletzt verbunden → Umgebungsvariable →
  IIS-Default), `ServerHealthProbe.CheckAsync(Uri)` für
  Kandidatenadressen (mit adressspezifisch aufgelöstem Gate-Schlüssel,
  niemals dem Geräte-Token eines anderen Servers),
  `ShellAppEffectHandler`-Zweige.
- `VideoPlayer.Storage`: `AppSecretsService.RefreshTokenKey` —
  Refresh-Token in der sicheren Plattformablage, profilbezogen;
  `ClearCredentialsAsync` räumt ihn mit.
- `VideoPlayer.ApiClient`: `DevicePairingClient` (öffentliche
  Pairing-Endpunkte `exchange` + `bootstrap` ohne Gate-/Bearer-Header
  und ohne Wiederholungen), `VideoPlayerApiClient.RefreshSessionAsync`
  /`LogoutSessionAsync`,
  `ClientGate.DeviceTokenProvider`/`ResolveStatic` (Geräte-Token vor den
  statischen Quellen), `HealthCheckResult` (unterscheidet
  Gate-Ablehnung von Nichterreichbarkeit).

## Bekannte Einschränkung (serverseitig)

Die aktuelle Serverinstallation beantwortet die mDNS-Dienstsuche nicht und
meldet im UDP-Protokoll eine Adresse, die nicht überall verwendbar ist.
Gemeldete Server-Issues:

- [martin-stromberg/VideoPlayer#223](https://github.com/martin-stromberg/VideoPlayer/issues/223) — mDNS-Announce fehlt
- [martin-stromberg/VideoPlayer#224](https://github.com/martin-stromberg/VideoPlayer/issues/224) — UDP-Meldung enthält `localhost`/nicht verwendbare Adresse

Die App enthält bewusst keinen Workaround; gefundene Adressen werden
grundsätzlich per Verbindungsprüfung validiert, sodass unbrauchbare
Meldungen verworfen werden.
