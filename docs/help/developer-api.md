# Developer-API für UI-Automatisierung

Die App `VideoPlayer.Maui` enthält im **Debug-Build** (oder mit explizitem
Opt-in über das Define `DEV_API`, niemals im Release) eine lokale
Developer-API: einen minimalen HTTP-Server, der strukturierte
UI-Analyse und -Steuerung für automatisierte Tests ermöglicht
(Projekt Issue #17, Schritt 2).

## Aktivierung und Ablage

- **Nur Debug/Opt-in:** Sämtlicher Code steht unter
  `#if DEBUG || DEV_API` – der Release-Build enthält die API nicht.
  Opt-in für andere Konfigurationen über das zusätzliche Define
  `DEV_API` beim Build.
- **Bindung:** ausschließlich `127.0.0.1` (Loopback), der Port wird vom
  Betriebssystem vergeben (`TcpListener(IPAddress.Loopback, 0)`).
- **Metadaten:** Beim Start schreibt die App `devapi.json` ins
  Programmverzeichnis (`AppContext.BaseDirectory`, bei der unpackageden
  Windows-App neben der Exe); bei nicht schreibbarem Ort als Fallback ins
  App-Datenverzeichnis. Inhalt:

  ```json
  {"port":50146,"pid":10960,"startedUtc":"2026-09-24T07:48:10.4931232Z"}
  ```

- **Lebenszyklus:** Der Server startet mit dem Hauptfenster
  (`App.CreateWindow`) und stoppt beim Schließen (`Window.Destroying`).
  Jede Verbindung beantwortet genau eine Anfrage (`Connection: close`);
  aufeinanderfolgende Verbindungen werden sequentiell bedient.

## Endpunkte

### `GET /model`

Kuratierter JSON-Snapshot des unveränderlichen `AppModel`
(`MvuHost.Model`): `currentPage`, `connectionState`, `serverHealth`/
`serverHealthText`, Start-/Login-/Sync-Flags, `activeServerAddress`,
`knownServers`/`discoveredServers`, Discovery-/Check-Flags,
`serverSetupError`, Pairing-Flags/`pairingError`,
Bootstrap-Flags/`bootstrapError`, `storageNotice`,
`heroTitle` sowie Zähler der Startseiten-Listen. Tokens und Passwörter
stehen per Konstruktion nicht im Modell.

### `GET /tree`

Logischer Elementbaum der aktuellen Seite über `IVisualTreeElement`:
je Knoten `automationId`, `type`, `isVisible`, `bounds`
(`x`/`y`/`width`/`height`), `text` (Label/Button/InputView/MenuItem) und
`placeholder` (Eingabefelder). `CollectionView`-Einträge werden aus der
`ItemsSource` abgeleitet (`items` mit `index`, `type`, `text`, `detail`)
– das schließt die UIA-Lücke, unter der Windows-Automation keine
Listeneinträge sieht.

### `POST /invoke`

```json
{"automationId":"ConnectButton"}
{"automationId":"KnownServersView","itemIndex":0}
```

Findet das Element per AutomationId und löst auf dem UI-Thread das
Clicked-Äquivalent aus: `SendClicked()` auf `Button`/`ImageButton`,
Toggle auf `Switch`/`CheckBox`, `Activate()` auf `MenuItem`, sonst
`Command`-Ausführung (inkl. Tap-Gesten mit Command).

Mit optionalem `itemIndex` wird stattdessen der Eintrag einer
`CollectionView` ausgewählt (`SelectedItem` gesetzt → echtes
`SelectionChanged`-Event mit dem regulären App-Nachrichtenfluss) —
der koordinatenfreie Listenpfad für Einträge, die Windows-UIA nicht
zuverlässig adressiert.

Antworten: `200 {"invoked":true,…}`, `404` Element nicht gefunden,
`400` Element nicht auslösbar, `itemIndex` auf Nicht-CollectionView
oder außerhalb der Liste bzw. Body ungültig.

### `POST /set-text`

```json
{"automationId":"AddressEntry","text":"http://server/videoplayer/"}
```

Setzt `Text` eines `InputView` (Entry/Editor/SearchBar) auf dem
UI-Thread – der Setter feuert `TextChanged` wie eine echte Eingabe.
Antworten: `200 {"set":true,…}`, `404`, `400` kein Eingabefeld.

### Statuscodes

| Code | Bedeutung |
|------|-----------|
| 200 | Erfolg |
| 400 | Ungültiger Body / Element nicht auslösbar / keine Texteingabe / kaputte Anfrage |
| 404 | Endpunkt oder AutomationId unbekannt |
| 405 | Methode für den Pfad nicht erlaubt (z. B. `POST /model`) |
| 500 | Interner Fehler (z. B. kein aktives Fenster) |

Fehlerantworten tragen `{"error":"<deutscher Text>"}`.

## Hinweise

- Es existiert **kein** `POST /dispatch` – bewusste Entscheidung: Die
  API bleibt auf der echten UI-Eingabekette, Automatisierung wirkt wie
  echte Bedienung.
- Implementierung: plattformneutraler Kern in `VideoPlayer.Mvu/DevApi/`
  (Parser, Router, Server, Modell-Snapshot, Metadaten), MAUI-gebundene
  Verdrahtung in `VideoPlayer.Maui/DevApi/` (Visual Tree, Dienst).
- Primärer Einsatzzweck sind die FlaUI-E2E-Tests (Projekt Schritt 3):
  FlaUI für Fenster/Klicks, Dev-API für Zustands-Assertions und
  Listen-Interaktion — siehe `docs/help/e2e-tests.md`.
