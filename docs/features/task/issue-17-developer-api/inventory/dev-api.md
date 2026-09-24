# Detail: Endpunkt-Verträge und Semantik der Developer-API

## Gemeinsames

- Bindung `127.0.0.1`, Port systemvergeben (`TcpListener`-Port 0).
- HTTP/1.1, eine Anfrage je Verbindung (`Connection: close`), sequentielle
  Verbindungen; Content-Type `application/json; charset=utf-8`.
- Fehlerantworten: `{"error":"<deutscher Fehlertext>"}` mit Statuscode.
- `devapi.json`: `{"port":N,"pid":N,"startedUtc":"<ISO-8601 UTC>"}`.

## `GET /model`

Response 200 – kuratierter `AppModel`-Auszug (camelCase):

```json
{
  "currentPage": "ServerSetup",
  "connectionState": "NeedsServerSetup",
  "serverHealth": "Unreachable",
  "serverHealthText": "Server: nicht erreichbar",
  "isStartupRunning": false,
  "isLoginRunning": false,
  "loginError": null,
  "loginUsername": "martin",
  "isOnline": true,
  "isOffline": false,
  "hasOfflineData": false,
  "isSyncRunning": false,
  "activeServerAddress": "http://server/videoplayer/",
  "knownServers": [{"address":"…","displayName":"…","nameOrAddress":"…","lastUsedAtUtc":"…"}],
  "discoveredServers": [{"address":"…","name":"…","nameOrAddress":"…","source":"Mdns"}],
  "isDiscoveryRunning": false,
  "discoveryAttempted": true,
  "isServerCheckRunning": false,
  "serverSetupError": null,
  "isPairingRequired": true,
  "isPairingRunning": false,
  "pairingError": null,
  "storageNotice": null,
  "heroTitle": "VideoPlayer",
  "continueWatchingCount": 0,
  "favoritesCount": 0,
  "recentEntriesCount": 0
}
```

## `GET /tree`

Response 200 – Knoten rekursiv:

```json
{
  "automationId": "ConnectButton",
  "type": "Button",
  "isVisible": true,
  "bounds": {"x": 24, "y": 500, "width": 120, "height": 40},
  "text": "Verbinden",
  "children": [ … ],
  "items": [
    {"index": 0, "type": "KnownServer", "text": "Wohnzimmer",
     "detail": "http://server/videoplayer/"}
  ]
}
```

- `items` nur bei `CollectionView` (aus `ItemsSource`; `text` =
  `NameOrAddress`/`Name`/`Title`/`DisplayName`/`Text` → `ToString()`,
  `detail` = `Address` falls vorhanden).
- Text-Priorität bei Elementen: `Label.Text`, `Button.Text`,
  `InputView.Text`, `MenuItem.Text`, sonst `null`; `InputView.Placeholder`
  als `placeholder`.

## `POST /invoke`

Request: `{"automationId":"ConnectButton"}`

- 200 `{"invoked":true,"automationId":"…","type":"Button"}` –
  Element gefunden und Clicked-Äquivalent ausgelöst (auf UI-Thread).
- 404 – `automationId` nicht im Baum.
- 400 – Body ungültig (fehlende/leere `automationId`, kein JSON) oder
  Element nicht auslösbar (kein Button/Switch/CheckBox/MenuItem/
  Command-Träger/TapGestureRecognizer).

Auslöse-Regeln: `Button`/`ImageButton` → `SendClicked()`; `Switch` →
`IsToggled`-Flip; `CheckBox` → `IsChecked`-Flip; `MenuItem` →
`IMenuItemController.Activate()`/`Command`; `ICommandSource` →
`Command.Execute(CommandParameter)`; `TapGestureRecognizer` mit `Command`
→ Ausführung.

## `POST /set-text`

Request: `{"automationId":"AddressEntry","text":"http://server/"}`

- 200 `{"set":true,"automationId":"…","type":"Entry"}` – `Text` gesetzt
  (feuert `TextChanged`) auf UI-Thread.
- 404 – `automationId` nicht im Baum.
- 400 – Body ungültig oder Element ist kein `InputView`.

## Statuscode-Tabelle

| Situation | Code |
|-----------|------|
| Erfolg | 200 |
| Ungültiger Request (kein JSON, fehlende Felder, Element nicht auslösbar/nicht textfähig) | 400 |
| Pfad unbekannt / Element nicht gefunden | 404 |
| Pfad bekannt, Methode falsch (z. B. `POST /model`) | 405 |
| Interner Fehler im Handler | 500 |
