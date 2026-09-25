# Verbindung und Anmeldung (Entwicklungsschritt 5)

Die neue VideoPlayer-App verbindet sich beim Start automatisch mit dem
zuletzt genutzten Server und meldet sich mit den gespeicherten
Zugangsdaten an. Je nach Ausgang landet sie in einem von vier Zuständen:
**verbunden** (Startseite), **Anmeldung erforderlich** (Anmeldemaske),
**Servereinrichtung erforderlich** (Serverauswahl) oder **offline**
(eingeschränkter Betrieb mit lokalen Daten). Der Zustand steuert die
Navigation — solange die Anmeldung oder die Servereinrichtung aussteht,
sind die Menüseiten nicht erreichbar.

## Startvorgang

Nach dem Start erscheint eine Ladeanzeige mit Statustext
(„Serververbindung wird geprüft …", danach „Anmeldung beim Server läuft
…"). Die App prüft die Erreichbarkeit des zuletzt verbundenen Servers
(`GET api/health`):

- **Server erreichbar, Zugangsdaten vorhanden:** automatische Anmeldung —
  danach Startseite.
- **Server erreichbar, Zugangsdaten fehlen oder werden abgelehnt:**
  Anmeldemaske (siehe unten).
- **Server erreichbar, Geräte-Pairing erforderlich:** der Server verlangt
  ein gerätegebundenes Token und es ist weder eines hinterlegt noch ein
  statischer Gate-Schlüssel konfiguriert — die Einrichtungsseite zeigt die
  Pairing-Eingabe (Einmal-Code aus der Server-Oberfläche), siehe
  [Serververwaltung](serververwaltung-erststart.md). Nach erfolgreicher
  Kopplung läuft die automatische Anmeldung an.
- **Kein Server hinterlegt:** der Einrichtungsdialog aus
  [Schritt 4](serververwaltung-erststart.md) erscheint.
- **Server nicht erreichbar:** mit lokalen Offline-Daten startet die App
  in den Offline-Betrieb; ohne solche Daten erscheint die
  Serverauswahl/Einrichtung.

## Anmeldemaske

Die Anmeldemaske zeigt den aktiven Server und fragt nur Benutzername und
Passwort ab — keine internen Kennungen. Der bekannte Benutzername ist
bereits vorbefüllt. Während des Anmeldeversuchs sind die Eingaben
gesperrt; Fehlermeldungen erscheinen als Inline-Hinweis direkt auf der
Maske („Benutzername und Passwort prüfen", „Server nicht erreichbar",
„erneut versuchen"). Über „Anderen Server einrichten" gelangt man zurück
zur Serverauswahl (bekannte Server, Netzwerk-Suche, manuelle Eingabe).

## Offline-Modus

Ist der Server nicht erreichbar und liegen lokale Daten vor (gecachte
Listen/Favoriten oder Einträge im Download-Manifest), startet die App in
den Offline-Zustand: Die Startseite zeigt einen gelben Hinweis-Banner.
Über „Einstellungen → Serververwaltung" kann ein anderer Server gewählt
werden; nach erfolgreicher Verbindung und Anmeldung läuft die App wieder
im Normalbetrieb.

## Fehlerhafte Serverantworten und Sitzungsablauf

- Anfragen an den Server werden bei Wiederholungsfehlern bis zu dreimal
  wiederholt (Wartezeiten 1 s, 2 s, 4 s): Serverfehler (5xx), Transport-
  fehler, Zeitüberschreitungen sowie „Zeitüberschreitung der Anfrage"
  (408) und „zu viele Anfragen" (429). Eindeutige Clientfehler werden
  nicht wiederholt.
- Läuft das Anmelde-Token während der Nutzung ab, erneuert die App die
  Sitzung im Hintergrund und setzt die ursprüngliche Anfrage fort —
  ohne dass eine erneute Eingabe nötig ist. Liegt ein Refresh-Token
  vor (ab Schritt 5b bei Anmeldung und QR-/Ticket-Kopplung), läuft
  die Erneuerung über `POST api/auth/refresh`; der Server rotiert
  den Refresh-Token dabei und die App ersetzt ihn sofort in der
  sicheren Ablage. Ohne Refresh-Token (ältere Installation oder
  klassischer Login) greift der bisherige Rückfall über die
  gespeicherten Zugangsdaten. Nur wenn beides scheitert — Refresh
  abgelehnt und keine/anfechtbare Zugangsdaten — erscheint die
  Anmeldemaske mit dem Hinweis „Die Anmeldung ist abgelaufen."
- Das Abmelden widerruft den Refresh-Token best-effort am Server
  (`POST api/auth/logout`) und löscht anschließend alle lokalen
  Sitzungsgeheimnisse — auch wenn der Server gerade nicht
  erreichbar ist.
- Lehnt das Client-Gate den bereitgestellten Schlüssel ab (HTTP 401 am
  Gate), unterscheidet die App per Probe, ob das Geräte-Token widerrufen
  wurde oder die Zugangsdaten abgelehnt sind: Ein widerrufenes
  Geräte-Token wird verworfen, einmalig der statische Schlüssel
  nachgeprüft — bleibt die Ablehnung bestehen, erscheint die
  Pairing-Eingabe mit dem Hinweis „Der Geräte-Zugang wurde vom Server
  abgelehnt. Bitte das Gerät erneut koppeln."

## Nach einer Backup-Wiederherstellung

Serverliste und zuletzt verbundener Server sind backup-pflichtig — die
App verbindet sich nach einer Wiederherstellung automatisch mit dem
bekannten Server. Passwort und Anmelde-Token liegen ausschließlich in
der sicheren Plattformablage und gelangen nicht ins Backup: Fehlen sie
nach der Wiederherstellung, erscheint die Anmeldemaske mit vorbefülltem
Benutzernamen — eine erneute Passworteingabe genügt.

## Fehlerbehebung

| Problem | Vorgehen |
|---------|----------|
| „Der Server ist nicht erreichbar" | Server-Adresse und Netzwerk prüfen; auf der Serverseite „Server im Netzwerk suchen" oder die Adresse manuell korrigieren |
| „Anmeldung fehlgeschlagen. Bitte Benutzername und Passwort prüfen." | Zugangsdaten der VideoPlayer-Serverinstallation verwenden (Web-UI-Zugang) |
| „Der Code ist nicht vollständig oder enthält ungültige Zeichen" | Code vollständig eingeben — er besteht aus Großbuchstaben und Ziffern ohne die leicht verwechselbaren Zeichen 0, O, 1, I und L |
| „Der Pairing-Code ist ungültig, abgelaufen oder bereits verwendet." | Neuen Einmal-Code in der Server-Verwaltung (Bereich „Geräte") erzeugen und erneut eingeben |
| „Zu viele Pairing-Versuche." | Vorübergehende Sperre abwarten, dann mit einem neuen Code erneut versuchen |
| „Der Server unterstützt kein Geräte-Pairing" | Serverinstallation aktualisieren oder die Person kontaktieren, die den Server eingerichtet hat — es gibt keinen automatischen Rückfall auf einen statischen Schlüssel |
| „Das Geräte-Pairing ist fehlgeschlagen." | Vorgang mit einem neuen Einmal-Code erneut versuchen; bei anhaltendem Fehler Server-Adresse und Netzwerk prüfen |
| „Der Geräte-Zugang wurde vom Server abgelehnt." | Gerät erneut koppeln — das widerrufene Geräte-Token wurde verworfen |
| „Bitte eine gültige Ticket-Adresse oder einen gültigen Kurzcode eingeben." | QR-Code der Serververwaltung erneut scannen oder die komplette Ticket-Adresse (mit `?t=`) bzw. den 8-stelligen Kurzcode eingeben |
| „Für einen Kurzcode muss zuerst ein Server verbunden sein" | Erst die Serveradresse verbinden — oder die vollständige Ticket-Adresse eingeben |
| „Der Server aus dem Kopplungscode ist nicht erreichbar." | Server starten und Netzwerk prüfen — der Server wird erst nach erfolgreicher Prüfung übernommen |
| „Der Kopplungscode ist ungültig oder abgelaufen." | Neuen Kopplungscode/Ticket in der Serververwaltung (Profilseite „Geräte") erzeugen und erneut versuchen |
| „Zu viele Kopplungsversuche." | Vorübergehende Sperre abwarten, dann mit einem neuen Code erneut versuchen |
| „Der Server unterstützt das Koppeln per Code nicht" | Serverinstallation aktualisieren oder die Person kontaktieren, die den Server eingerichtet hat — alternativ den klassischen Einmal-Code oder die Anmeldemaske verwenden |
| App bleibt im Offline-Modus | Server erreichbar machen oder über „Einstellungen → Serververwaltung" einen anderen Server wählen |
| Anmeldemaske nach Backup-Wiederherstellung | Passwort erneut eingeben — Geheimnisse werden aus Sicherheitsgründen nicht gesichert |

## Technischer Aufbau

- `VideoPlayer.Mvu`: `ConnectionState` (`Unknown`/`Connecting`/
  `Connected`/`NeedsLogin`/`NeedsServerSetup`/`Offline`) im `AppModel`;
  Verzweigungen in `AppUpdate`; `AuthSessionService` (Anmeldung über
  `VideoPlayerApiClient.LoginAsync`, Bootstrap-Session aus
  `CompleteBootstrapAsync` — ohne Passwortpfad —, Persistenz via
  `AppSecretsService` inkl. `RefreshTokenKey`,
  `TokenRefresher`-Verdrahtung: stille Neuanmeldung zuerst über
  `RefreshSessionAsync`, Rückfall auf die Zugangsdaten, Logout mit
  best-effort `LogoutSessionAsync`-Widerruf,
  `SessionExpired`-Ereignis); `DeviceTokenService` (Geräte-Token-Provider
  für `ClientGate`, Gate-Probe `IsGateRejectedAsync` per
  `POST api/auth/login`, `DeviceTokenRejected`-Ereignis) — eine
  Gate-Ablehnung (Status `AuthAttemptStatus.GateRejected`) führt über
  `DeviceTokenRevokedMessage` in die Pairing-Eingabe statt in den
  `SessionExpired`-Pfad; neue Messages/Effekte
  (`AutoLoginCompleted`, `LoginSubmitted`, `LoginCompleted`,
  `OfflineDataCheckCompleted`, `SessionExpired` /
  `AttemptAutoLogin`, `AttemptLogin`, `CheckOfflineData`).
- `VideoPlayer.Storage`: `CacheDatabase.HasOfflineDataAsync()` (Einträge
  in `cached_lists`, `cached_favorites` oder `download_manifest`).
- `VideoPlayer.Maui`: `Pages/LoginPage` (Window-Root beim Start bzw.
  Shell-Route `login`), `ShellAppEffectHandler`-Zweige, Offline-Banner
  auf `HomePage`, `StartupStatusText` auf `LoadingPage`.
- Die Wiederholungsstrategie und die einmalige Token-Erneuerung liegen in
  `VideoPlayerApiClient` (Schritt 1) — unverändert; der Schritt 5
  verdrahtet sie produktiv mit der sicheren Ablage.
