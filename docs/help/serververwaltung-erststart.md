# Serververwaltung und Erststart-Einrichtung (Entwicklungsschritt 4)

Die neue VideoPlayer-App verwaltet mehrere bekannte Server, merkt sich den
zuletzt verbundenen Server und startet künftig direkt mit ihm. Beim ersten
Start ohne hinterlegten Server erscheint ein Einrichtungsdialog.

## Erststart-Einrichtung

Ist noch kein Server hinterlegt, zeigt die App nach dem Start die Seite
„Server" statt der Startseite. Zwei Wege führen zum Ziel:

- **Server im Netzwerk suchen** — die App sucht im lokalen Netzwerk nach
  VideoPlayer-Servern (mDNS-Dienstsuche auf `_http._tcp.local.` sowie
  UDP-Broadcast `VIDEOWEBPLAYER_DISCOVERY` auf Port 5001). Gefundene Server
  erscheinen als auswählbare Liste; ein Tipp auf einen Eintrag übernimmt
  ihn. Liefert die Suche keinen Treffer, weist ein Hinweis auf die
  manuelle Eingabe hin.
- **Adresse manuell eingeben** — die Serveradresse kann direkt eingegeben
  werden (z. B. `http://server/videoplayer/`; ein fehlendes Schema wird
  automatisch als `http://` ergänzt). „Verbinden" prüft die Eingabe.

In beiden Fällen wird die Verbindung zunächst geprüft (`GET api/health`);
nur ein erreichbarer Server wird gespeichert und übernommen. Ungültige
Adressen oder nicht erreichbare Server erzeugen einen Inline-Hinweis —
es wird nichts gespeichert. Nach erfolgreicher Einrichtung startet die App
künftig direkt mit dem gespeicherten Server.

## Gerät koppeln (Pairing)

Ist der Server erreichbar, aber weder ein gepaartes Geräte-Token noch ein
statischer Gate-Schlüssel vorhanden, blendet die Serverseite den Abschnitt
„Gerät koppeln" ein:

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

- **Aktiver Server** und die Liste **bekannter Server** (zuletzt verwendet
  zuerst) werden angezeigt.
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
  Lauf-/Fehlerflags, `IsPairingRequired`/`IsPairingRunning`/`PairingError`),
  Messages/Effekte für Laden, Discovery, Prüfung, Übernahme und Pairing
  (`PairingCodeSubmitted`, `PairingCompleted`, `DeviceTokenRevoked` /
  `ExchangePairingCode`); `PairingCode` (Normalisierung/Validierung),
  `DeviceTokenService` (Provider-Pflege, Gate-Probe per
  `POST api/auth/login` — `api/health` ist serverseitig nicht
  gate-geschützt —, Widerruf-Ereignis `DeviceTokenRejected`);
  der Start-Workflow lädt die Serververwaltung vor dem Health-Check.
- `VideoPlayer.Maui`: `Pages/ServerSetupPage` (Erststart als Window-Root,
  „Server wechseln" als Shell-Route `serversetup` ohne Menüeintrag,
  Pairing-Abschnitt mit Code-Eingabe),
  `ServerAddressProvider` (zuletzt verbunden → Umgebungsvariable →
  IIS-Default), `ServerHealthProbe.CheckAsync(Uri)` für
  Kandidatenadressen (mit adressspezifisch aufgelöstem Gate-Schlüssel,
  niemals dem Geräte-Token eines anderen Servers),
  `ShellAppEffectHandler`-Zweige.
- `VideoPlayer.ApiClient`: `DevicePairingClient` (öffentlicher
  Pairing-Endpunkt ohne Gate-/Bearer-Header und ohne Wiederholungen),
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
