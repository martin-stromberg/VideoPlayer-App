# Live-Verifikation gegen IIS-Testserver

Datum: 2026-09-23
Server: `http://localhost:83` (IIS-Site "VideoPlayer", staging mit Issue-228-Feature, Deployment `D:\Dashboard\VideoPlayer`, DB `Data\WebVideoPlayer.db`)

## Durchgeführte Prüfungen

| # | Prüfung | Ergebnis |
|---|---------|----------|
| 1 | Pairing-Code direkt in `PairingCodes`-Tabelle eingefügt (`CodeHash` = SHA-256-Hex-Uppercase, `ExpiresAtUtc` = +15 min, `CreatedByUserId` = devin-User `60103ea7-…`) | OK |
| 2 | PowerShell-Referenzimplementierung des Vertrags (ECDH nistP256, SPKI/DER Base64, `DeriveKeyFromHmac` SHA256, AES-256-GCM `nonce(12)‖ct‖tag(16)`) gegen `POST /api/pairing/exchange` mit Code `DEVIN42X` | 200; Token entschlüsselt: 43 Zeichen = `Base64Url(32B)` ✓ |
| 3 | **Echter Produktivcode** `DevicePairingClient`+`PairingCrypto` (Konsolen-Harness mit Projektreferenz, `%TEMP%\pairing-harness`) mit Code `VERIFY42` | 200; Token entschlüsselt ✓ |
| 4 | Negativfälle mit echtem Client: unbekannter Code `ZZZZZZZZ` → `ApiException` 401 "Ungültiger oder abgelaufener Pairing-Code."; bereits verbrauchter Code `DEVIN42X` → 401 | ✓ |
| 5 | `PairedDevices`-Tabelle: beide Geräte mit `deviceName` ("Devin Verifikations-PC", "Devin Harness-PC") und `CreatedByUserId` | ✓ |
| 6 | Geräte-Token als `X-API-Key` an `POST api/auth/login` (MauiOnly-Gate) mit `devin@example.com` | 200 + JWT ✓ |
| 7 | Code-Wiederverwendung serverseitig geblockt (`ConsumedAtUtc` gesetzt, zweite Verwendung → 401) | ✓ |

## Befund aus der Live-Verifikation (in `review-code.md` übernommen)

- `GET api/health` ist **nicht** durch `ApiTokenCheck` geschützt (antwortet 200 ohne/ mit ungültigem `X-API-Key`) → eine Gate-Probe auf `api/health` kann niemals 401 liefern.
- Einziger MauiOnly-gated Endpunkt: `POST api/auth/login`. Dort unterscheidbar:
  - Gate-Ablehnung (`UnauthorizedResult` aus `ApiTokenCheckAttribute`) → `401` mit `Content-Type: application/problem+json`, Body = ProblemDetails (`"title":"Unauthorized"`).
  - Credential-Fehler (`Unauthorized(ex.Message)` aus der Action) → `401` mit `Content-Type: text/plain`, Body = Klartext ("Benutzer nicht gefunden." / "Ungültige Zugangsdaten.").
  - Gate passiert + Model-Validation → `400`.
- `LoginIpBlockService.RegisterFailure` wird ausschließlich im `PairingController` aufgerufen → Login-Probes erzeugen keine IP-Sperr-Fehlversuche.
- Nebenbefund: das auf dieser Maschine konfigurierte statische Token (`VIDEOWEBPLAYER_MAUI_API_TOKEN`, Machine-Scope) wird von diesem Server abgelehnt (401 problem+json) — gehört vermutlich zu einer anderen Serverinstanz.

## Abdeckung Task 42 (manueller Gerätetest)

Der kryptografische und API-vertragliche Teil des Pairing-Flows ist hiermit **gegen den realen Server verifiziert** (Punkte 2–6 oben). Nicht verifizierbar in dieser Umgebung bleiben die gerätespezifischen Anteile: `SecureStorage`-Persistenz, `DeviceInfo`-Gerätename und die UI-Bedienung auf echten Zielen (iOS/Android/Windows-App).
