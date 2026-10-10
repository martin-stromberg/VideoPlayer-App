# Detail: HTTP-Vertrag der Developer-API

Quellen: `docs/help/developer-api.md`, `docs/features/task/issue-17-flaui-e2e/inventory/dev-api-vertraege.md`, Implementierung.

## Metadaten (`devapi.json`)

- Ablage: `AppContext.BaseDirectory` (bei unpackageder Windows-App neben der Exe), Fallback `FileSystem.AppDataDirectory`.
- Inhalt: `{"port":<int>,"pid":<int>,"startedUtc":"<iso8601>"}` (camelCase).
- Port: vom OS vergeben (`TcpListener(IPAddress.Loopback, 0)`).
- Protokoll: HTTP/1.1, eine Antwort je Verbindung (`Connection: close`), sequentielle Bedienung.

## Endpunkte

### `GET /model`
camelCase-JSON-Snapshot (ca. 30 Felder): `currentPage`, `connectionState`, `serverHealth`, `serverHealthText`, `isStartupRunning`, `isLoginRunning`, `loginError`, `loginUsername`, `isOnline`, `isOffline`, `hasOfflineData`, `isSyncRunning`, `activeServerAddress`, `knownServers[]`, `discoveredServers[]`, `isDiscoveryRunning`, `discoveryAttempted`, `isServerCheckRunning`, `serverSetupError`, `isPairingRequired`, `isPairingRunning`, `pairingError`, `isBootstrapRunning`, `bootstrapError`, `storageNotice`, `heroTitle`, `continueWatchingCount`, `favoritesCount`, `recentEntriesCount`.

### `GET /tree`
Knoten: `automationId` (string|null), `type`, `isVisible`, `bounds {x,y,width,height}` (nullable), `text`, `placeholder`, `children[]`, `items[]` (nur CollectionView: `{index,type,text,detail}`).

### `POST /invoke`
Body: `{"automationId":"…"}` oder `{"automationId":"…","itemIndex":<n>}` (CollectionView-Auswahl).
Antwort 200: `{"invoked":true,"automationId":"…","type":"…"}` (bzw. mit `itemIndex`).

### `POST /set-text`
Body: `{"automationId":"…","text":"…"}`.
Antwort 200: `{"set":true,"automationId":"…","type":"…"}`.

## Statuscodes

| Code | Bedeutung |
|------|-----------|
| 200 | Erfolg |
| 400 | Ungültiger Body / Element nicht auslösbar / kein Eingabefeld / kaputte Anfrage (`DevApiRequestException`) |
| 404 | Endpunkt oder AutomationId unbekannt |
| 405 | Methode für Pfad nicht erlaubt |
| 500 | Interner Fehler (z. B. kein aktives Fenster) |

Fehlerantworten: `{"error":"<deutscher Text>"}`.

## Sicherheitsinvarianten

- Bindung ausschließlich `127.0.0.1` (kein `IPAddress.Any`).
- Port 0 → systemvergeben.
- Gesamter Codepfad unter `#if DEBUG || DEV_API` (Release enthält die API nicht).
- Keine Tokens/Passwörter im `/model`-Snapshot (durch `Snapshot_ContainsNoSecretFields` abgesichert).
- Kein generischer `dispatch`-Endpunkt (durch `DevApiService_MapsExactlyTheFourEndpoints` abgesichert).
