# Detail: Backend-Optionen für serverabhängige Szenarien

## Beobachtungen am echten Server-Checkout

Repo `D:\Repositories\softwareschmiede\487c3041-d72b-486e-811f-9ed8db04630b`
(`VideoWebPlayer`, ASP.NET Core, SQLite):

- `api/health` (`HealthController.cs`): **kein** `ApiTokenCheck` —
  antwortet immer 200 „OK". Die Gate-Ablehnung spielt für die App nur
  bei `api/auth/login` (`ApiTokenCheck(ApiTokenScope.MauiOnly)`:
  akzeptiert `Jwt:ApiToken:Maui`-Konfiguration **oder** gepaarte
  Geräte-Tokens via `IDeviceTokenService.IsValidDeviceTokenAsync`)
  und den `[BearerTokenCheck]`-Endpunkten eine Rolle.
- `api/pairing/exchange` (`PairingController`/`PairingService`):
  öffentlich; serverseitige Krypto = ECDH P-256,
  `DeriveKeyFromHmac(clientPub, SHA256, null, null, null)`,
  AES-256-GCM `nonce(12)‖ct‖tag(16)` — deckungsgleich mit
  `PairingCrypto` der App.
- Pairing-Codes entstehen in der Admin-UI/Datenbank (Tabelle der
  Einmal-Codes) — für automatisierte Tests müsste ein Code pro Lauf
  per SQLite-Seed oder Admin-API erzeugt werden.

## Option A — echter Server-Checkout

Pro: echter Endpunkt, kein Nachbau.
Contra: externe Abhängigkeit (Server muss laufen, IIS/SQLite-Instanz,
Port 83), Seed-Daten je Lauf (Pairing-Code erzeugen + auslesen),
User-Account mit bekanntem Passwort nötig, Zustand des Entwickler-
Systems beeinflusst die Tests (nicht hermetisch), kein Teil dieses
Repos → Versionsdrift.

## Option B — Mock-Server im Testprojekt

Minimaler `HttpListener` (oder `TcpListener`) auf `127.0.0.1:0`:

| Route | Verhalten |
|-------|-----------|
| `GET api/health` | 200 `"OK"` (ungeatet wie der echte Server) |
| `POST api/pairing/exchange` | Code-Prüfung gegen konfigurierten Code; 200 `{serverPublicKey,encryptedToken}` (ECDH/AES-GCM, ~40 Zeilen Bordmittel); 401 bei falschem Code; optional 429 |
| `POST api/auth/login` | Gate-Check `X-API-Key` == ausgestelltes Geräte-Token → 401 sonst; Credentials-Check → 200 `{token,expires}` oder 401 |
| `GET api/favorites`, `api/continue-watching`, `api/Sources`, `api/items/recent` | `[]` (200) — auch für Token-Validierung (`TryStoredTokenAsync`) |
| alles andere | 404 JSON (kein 5xx → keine Client-Retries) |

Pro: hermetisch, deterministisch, kein Seed, schnell, im Repo versioniert.
Contra: Nachbau des Pairing-Handshake (~klein, Risiko gering: Vertrag
ist an beiden Seiten dokumentiert und mit dem echten Server verifiziert
— die App-Seite `PairingCrypto` ist per Unit-Test gepinnt).

## Empfehlung im Plan

**Option B** für alle automatisierten Szenarien; der echte Server bleibt
als dokumentierter manueller Nachweis (Anleitung in der Doku:
lokaler `VideoWebPlayer` + Admin-UI-Pairing-Code). Die Mock-Krypto
bildet exakt den belegten Server-Vertrag ab (`PairingService.cs` des
Server-Repos als Referenz gelesen).
