# Inventory – ApiClient, Krypto, Serveradresse

## `VideoPlayer.ApiClient/DevicePairingClient.cs` (123 Zeilen)

- `ExchangeEndpoint = "api/pairing/exchange"`; `RequestTimeout = 15 s`.
- `ExchangeAsync(Uri baseAddress, string code, string? deviceName, CancellationToken)`:
  - `PairingCrypto.CreateKeyPair()` → `PairingExchangeRequest { code, clientPublicKey, deviceName? }` (camelCase, `deviceName` bei null weggelassen).
  - `new Uri(baseAddress, ExchangeEndpoint)` → POST ohne Gate-/Bearer-Header, ohne Retry.
  - Fehler → `ApiException(endpoint, statusCode, body, msg)`; Transport/Timeout → `StatusCode = null`.
  - Erfolg → `PairingExchangeResponse { serverPublicKey, encryptedToken }` → `PairingCrypto.DecryptToken(...)` → Klartext-Token.
- **Erweiterung**: `BootstrapEndpoint = "api/pairing/bootstrap"` + `BootstrapAsync(baseAddress, ticket, deviceName, ct)` → `PairingBootstrapRequest { ticket, clientPublicKey, deviceName? }` → Response `{ serverPublicKey, encryptedPayload }` → `DecryptToken` → JSON → `PairingBootstrapPayload { deviceToken, token, expires, refreshToken }`. Gleiche Fehlerbehandlung; `ApiException.Endpoint` = Bootstrap-Pfad.

## `VideoPlayer.ApiClient/PairingCrypto.cs` (internal, 100 Zeilen)

- `CreateKeyPair()` (ECDH P-256), `ExportPublicKeyBase64` (SPKI/DER), `DecryptToken(keyPair, serverPublicKeyB64, payloadB64)` → UTF-8-Klartext. Layout `nonce(12)‖ct‖tag(16)`, `DeriveKeyFromHmac`(SHA-256).
- Identisch zum Server (`PairingBootstrapService` nutzt dasselbe Verfahren) → **wiederverwenden**, kein neuer Krypto-Code. `DecryptToken` liefert bereits beliebigen Klartext → Bootstrap-Payload-JSON direkt deserialisierbar.

## `VideoPlayer.ApiClient/VideoPlayerApiClient.cs` (659 Zeilen)

- Konstruktor setzt `X-API-Key` aus `options.ApiKey ?? ClientGate.Resolve()` (DeviceTokenProvider vor statischen Quellen) und `BaseAddress`.
- `LoginAsync(email, password)` → `POST api/auth/login`, `allowReauth: false`, `SetAccessToken` bei Erfolg.
- `SendWithPolicyAsync` mit Retry (429/5xx/408: 1/2/4 s) und einmaligem Re-Auth über `options.TokenRefresher`.
- `SetAccessToken`, `ClearAccessToken`, `SetApiKey` vorhanden.
- **Neu**: `RefreshSessionAsync(refreshToken)` → `POST api/auth/refresh` (`{ refreshToken }`, `allowReauth: false`) → `RefreshSessionResult { token, expires, refreshToken }`; `LogoutSessionAsync(refreshToken)` → `POST api/auth/logout` (`{ refreshToken }`, `allowReauth: false`, kein Body-Parsing – idempotent 200). Beide laufen über den normalen Client → Gate-Header automatisch dabei (Endpunkt ist `ApiTokenCheck MauiOnly`).

## `VideoPlayer.ApiClient/Models/`

- `AuthToken { Token, Expires }` — `RefreshSessionResult` als eigenes Modell (`Token`, `Expires`, `RefreshToken`), da `AuthToken` kein Refresh-Feld hat.
- Neu: `PairingBootstrapRequest`, `PairingBootstrapResponse`, `PairingBootstrapPayload`, `RefreshTokenRequest` (intern/anonymes Objekt möglich), `RefreshSessionResult`.

## `VideoPlayer.Storage/ServerAddress.cs`

- `TryNormalize`: `scheme://host[:port][/pfad]/`, ergänzt `http://` bei fehlendem Schema, verwirft Query/Fragment via `GetLeftPart(UriPartial.Path)`.
- **Parser-Regel**: `/pairing`-Segment muss **vor** der Normalisierung abgeschnitten werden (Case-insensitiv, nur das letzte Pfadsegment; `/pairing/` mit Trailing-Slash akzeptieren; Unterpfad erhalten). `t=` aus der Raw-Query extrahieren (`Uri.Query` + manuelles Split/`Uri.UnescapeDataString` – opak, kein `PairingCode.TryNormalize`).

## Gate-Hinweis

`api/auth/refresh` ist `[ApiTokenCheck(MauiOnly)]` → ein 401 kann Gate-Ablehnung **oder** ungültiges Refresh-Token sein. `DeviceTokenService.IsGateRejectedAsync` (Probe = `POST api/auth/login` mit nur Gate-Header → problem+json) trennt beide Fälle — Muster aus `AuthSessionService.HandleGateRejectionAsync` wiederverwenden.
