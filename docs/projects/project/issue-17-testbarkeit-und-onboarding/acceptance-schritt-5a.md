# Abnahme Schritt 5a — QR-Bootstrap Server

Server-Issue: https://github.com/martin-stromberg/VideoPlayer/issues/231
Branch: `task/issue-231-qr-bootstrap-geraetekopplung-login` im Server-Repo (`487c3041-…`), Basis `origin/staging` (enthält Issue #228/PR #229).
Commits: `83f6f33` plan, `52fc66c` feat (48 Dateien, +4491/−730). Nicht gepusht, nicht gemergt — Merge/PR ist Nutzer-Entscheidung.

## Ergebnis

- **`POST /api/pairing/bootstrap`** (öffentlich wie `exchange`): `{ ticket, clientPublicKey, deviceName? }` → `200 { serverPublicKey, encryptedPayload }`. `ticket` = langes Secret **oder** 8-stelliger Kurzcode-Alias. Entschlüsselter Payload: `{ deviceToken, token, expires, refreshToken }`.
- **`POST /api/auth/refresh`** + **`POST /api/auth/logout`** (`ApiTokenCheck(MauiOnly)`): Rotation atomar `{ token, expires, refreshToken }`, Logout idempotent.
- **`api/pairing/exchange` unverändert** — konsumiert keine Bootstrap-Aliase (Test belegt).
- `PairingCode` + `Kind` (`AdminCode`/`BootstrapTicket`/`DeviceInitiated` — TV-Richtung als Modell-Reservat) + `TicketHash`; Migration `AddPairingBootstrapAndRefreshTokens` per `dotnet ef database update` verifiziert.
- `RefreshToken`-Entität: SHA-256-Hash, `ReplacedByHash`-Verkettung, Reuse-Detection mit Familiensperre; Kaskade bei Geräte-Löschen.
- Profilseite `Account/Manage/Devices`: „Gerät koppeln" mit QR (QRCoder 1.8.0, MIT), Kurzcode, Anleitung, Restlaufzeit, Loopback-Warnung. QR = `scheme://host/pairing?t=<ticket>` aus Request.
- Fehler: `400 "Ungültiger Bootstrap-Request."`, `401 "Ungültiges oder abgelaufenes Pairing-Ticket."`, `429` (IP-Sperre).
- Konfiguration: `Pairing:BootstrapTicketTtlMinutes` (5), `BootstrapMaxTicketsPerHour` (10), `BootstrapAdminOnly` (false), `Auth:RefreshTokenTtlDays` (30).

## Verifikation

- **447/447** `VideoPlayer.Tests` (Baseline 407, +40) + MarkdownLinkCheck 6/6 + beide Playwright-E2E.
- Screenshot-Beleg der Geräteseite (`pairing-bootstrap-devices-page.png`, lokaler Testoutput).
- Zwei Defekte während Tests gefunden und behoben (stale `RevokedAtUtc` nach `ExecuteUpdateAsync` → `AsNoTracking`; leeres EditForm-Model → Plain-Form).
- Bekannte, dokumentierte Einschränkungen: QR-Host aus Request-Headern (Header-Vertrauen akzeptiert); Deployment benötigt `dotnet ef database update`.

## Akzeptanzkriterien (Issue #231)

1. ✅ Profilseite „Gerät koppeln" mit QR + 8-Zeichen-Text + Anleitung + Rate-Limit.
2. ✅ `api/pairing/bootstrap` atomar, verschlüsselt, öffentlich; drei Tokens im Payload.
3. ✅ Datenmodell trägt beide Pairing-Richtungen (`DeviceInitiated` reserviert).
4. ✅ Refresh-Endpunkt mit Rotation + Widerruf.
5. ✅ Admin-Only-Option konfigurierbar; `docs/API.md` aktualisiert.

## Vertrag für Schritt 5b (App)

- Bootstrap-Request/Response und Fehler-Strings wie oben; Ciphertext-Layout kompatibel zu `exchange` (gleiche Entschlüsselungsroutine).
- QR-Parsing: `/pairing`-Suffix abschneiden, `t=` opak; Kurzcode-Eingabe an denselben Endpunkt.
- Refresh: `refreshToken` aus Bootstrap-Antwort speichern; bei `expires`/401 vor Credential-Fallback `POST /api/auth/refresh`; rotierten Token persistieren; `POST /api/auth/logout` beim Abmelden.
