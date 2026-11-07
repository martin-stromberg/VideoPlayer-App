# Bestandsaufnahme: Verbindungs-Workflow und Anmeldung

Analyse des bestehenden Codes bezogen auf die Anforderung „Verbindungs-Workflow
und Anmeldung" (Entwicklungsschritt 5): MVU-Startworkflow, Geheimnis-/Tokenablage,
API-Client (Retry + Token-Refresh), Serververwaltung aus Schritt 4 sowie die
fachliche Referenz (`ConnectionService`/`AuthService` des Altbestands).

## Zusammenfassung

- **Vorhanden:** Start-Workflow mit Ladeanzeige und Verzweigung „kein Server →
  Servereinrichtung" (`StartupRequestedMessage` → `LoadServersEffect` →
  `ServersLoadedMessage` → `CheckServerHealthEffect` → `ServerHealthCheckCompletedMessage`
  → Navigation nach `Home`). Serververwaltung mit `LastConnectedServer` in der
  backup-pflichtigen `BackupDatabase`. `AppSecretsService` mit sicherer Ablage
  von `Passkey`/`AuthToken` (SecureStorage, nicht im Cloud-Backup) und
  `Username` in der normalen Ablage. `VideoPlayerApiClient` mit
  `LoginAsync` (`api/auth/login`), fester Wiederholungsstrategie (1 s/2 s/4 s,
  retriable: 5xx/408/429/Transport) und einmaliger Token-Erneuerung bei 401
  über `VideoPlayerApiClientOptions.TokenRefresher` (aktuell nirgends verdrahtet).
- **Fehlt noch:** Kein Verbindungszustand im `AppModel` (verbunden /
  Anmeldung / Servereinrichtung / offline), keine automatische Anmeldung beim
  Start, keine Anmeldemaske (`AppPage.Login`/`LoginPage` existieren nicht),
  keine Prüfung „Offline-Daten vorhanden", keine produktive
  `TokenRefresher`-Verdrahtung (stille Neuanmeldung), kein Offline-Zustand bei
  Nichterreichbarkeit (aktuell wird immer nach `Home` navigiert), kein
  Sitzungsablauf-Handling (401 nach fehlgeschlagener Erneuerung → kein
  UI-Übergang).

Test-Ausgangszustand: 229 Tests, 0 Fehlgeschlagen, 0 Übersprungen — Nachweis
und Details in [Tests](inventory/tests.md).

## Details

- [Datenmodell](inventory/models.md)
- [Logik](inventory/logic.md)
- [Enums](inventory/enums.md)
- [Interfaces](inventory/interfaces.md)
- [Tests](inventory/tests.md)
