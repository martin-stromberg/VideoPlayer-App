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
- Läuft das Anmelde-Token während der Nutzung ab, meldet sich die App im
  Hintergrund erneut mit den gespeicherten Zugangsdaten an und setzt die
  ursprüngliche Anfrage fort — ohne dass eine erneute Eingabe nötig ist.
  Nur wenn die gespeicherten Zugangsdaten fehlen oder vom Server
  abgelehnt werden, erscheint die Anmeldemaske mit dem Hinweis „Die
  Anmeldung ist abgelaufen."

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
| App bleibt im Offline-Modus | Server erreichbar machen oder über „Einstellungen → Serververwaltung" einen anderen Server wählen |
| Anmeldemaske nach Backup-Wiederherstellung | Passwort erneut eingeben — Geheimnisse werden aus Sicherheitsgründen nicht gesichert |

## Technischer Aufbau

- `VideoPlayer.Mvu`: `ConnectionState` (`Unknown`/`Connecting`/
  `Connected`/`NeedsLogin`/`NeedsServerSetup`/`Offline`) im `AppModel`;
  Verzweigungen in `AppUpdate`; `AuthSessionService` (Anmeldung über
  `VideoPlayerApiClient.LoginAsync`, Persistenz via `AppSecretsService`,
  `TokenRefresher`-Verdrahtung für die stille Neuanmeldung,
  `SessionExpired`-Ereignis); neue Messages/Effekte
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
