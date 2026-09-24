# Windows-End-to-End-Tests (FlaUI)

Das Projekt `VideoPlayer.E2E.Tests` führt echte End-to-End-Tests gegen
die Windows-App aus (Issue #17, Schritt 3): Jeder Test startet einen
eigenen `VideoPlayer.Maui.exe`-Prozess, steuert die Oberfläche über
FlaUI/UI-Automation und prüft den App-Zustand über die
[Developer-API](developer-api.md). Als Backend dient ein in den Tests
mitlaufender lokaler Mock-Server — kein reales Server-Deployment nötig.

## Voraussetzungen

- **Windows** mit Desktop-Session (die Tests brauchen sichtbare Fenster;
  auf Windows lassen sie sich lokal oder per RDP ausführen).
- Die Tests führen den **Debug-Build** selbst aus
  (`dotnet build VideoPlayer.Maui -f net10.0-windows10.0.19041.0 -c Debug`)
  und starten anschließend
  `VideoPlayer.Maui/bin/Debug/net10.0-windows10.0.19041.0/win-x64/VideoPlayer.Maui.exe`.
- Die Tests laufen **sequentiel** (`[Collection("E2E")]` ohne
  Parallelisierung): Fensterfokus und `devapi.json` sind exklusiv.

## Ausführen

```bash
dotnet test VideoPlayer.E2E.Tests/VideoPlayer.E2E.Tests.csproj
```

Erwartetes Ergebnis: 10 Tests, alle bestanden (Laufzeit ca. 15–20 min,
geprägt von App-Starts und echten Wartezeiten, u. a. beim
nicht-erreichbaren Server).

## Architektur

- **`AppFixture`** — einmaliger Debug-Build der App für alle Tests.
- **`AppInstance`** — startet die Exe mit isoliertem Environment:
  `VIDEOPLAYER_PROFILE=e2e-<test>-<guid>` (eigenes App-Daten- und
  Token-Verzeichnis), entfernt geerbtes `VIDEOWEBPLAYER_MAUI_API_TOKEN`,
  wartet auf `devapi.json` mit passender PID, attacht FlaUI.UIA3 auf
  das Hauptfenster und räumt in `Dispose` Prozess (Kill-Baum nach
  `CloseMainWindow`), Metadatei und Profilverzeichnisse auf.
- **`DevApiClient`** — `GET /model`-Polling mit Timeout und Letztstand
  im Fehlertext, `GET /tree`, `POST /invoke` (inkl. `itemIndex` für
  `CollectionView`), `POST /set-text`.
- **`MockVideoServer`** — `TcpListener` auf `127.0.0.1` mit OS-Port;
  implementiert `api/health`, `api/pairing/exchange` (ECDH P-256 +
  AES-256-GCM, deckungsgleich mit `PairingCrypto`), `api/auth/login`
  und die Listen-Endpunkte, die Home/Synchronisation benötigen.
  Unterscheidet Gate-Ablehnung (`application/problem+json`) von
  Credential-Ablehnung (`text/plain`) wie der echte Server.
- **`UiDriver`** — AutomationId-Suche, Texteingabe per Value-Pattern,
  Button-Invoke, Flyout-Navigation per `SelectionItem` — bewusst ohne
  Koordinatenklicks.

## Szenarien (10 Tests)

- **Server-Einrichtung** (4): Erststart zeigt die Einrichtungsseite;
  ungültige Adresse → Validierungsfehler; nicht erreichbarer Server →
  sichtbarer Fehler; erreichbarer Server → „Pairing erforderlich".
- **Pairing** (3): ungültiges Format → lokaler Formatfehler; falscher
  Code → Pairing-Fehler vom Server; gültiger Code → Anmeldemaske.
- **Anmeldung** (2): falsches Passwort → sichtbarer Login-Fehler;
  gültige Anmeldung → Startseite.
- **Serverwechsel** (1): Server A einrichten (Pairing + Anmeldung),
  über Einstellungen auf Server B wechseln, dann Server A aus der
  Liste bekannter Server per `/invoke` `itemIndex` auswählen →
  Rückkehr zur Startseite mit Server A (Regression aus Issue #15).

## Grenzen und Hinweise

- Nur Windows (UIA3); Android/iOS sind nicht abgedeckt.
- Der Mock bildet den real getesteten API-Ausschnitt ab; ein optionaler
  Lauf gegen einen echten Server bleibt ein manueller Nachweis
  (Backend-Abwägung in `inventory/backend-optionen.md` der Feature-Doku).
- `devapi.json` liegt neben der Exe — parallel gestartete App-Instanzen
  aus demselben Verzeichnis würden sich die Metadatei überbieten;
  die sequentielle Collection verhindert das innerhalb der Suite.
- CI-Ausführung ist optional und nicht vorkonfiguriert: gehostete
  Runner haben keinen zuverlässigen interaktiven Desktop.
