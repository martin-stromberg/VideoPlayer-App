# Anforderung – QR-/Ticket-Onboarding (Bootstrap-Kopplung) in der App

Quelle: Projektplan Issue #17, Schritt 5b (`docs/projects/project/issue-17-testbarkeit-und-onboarding/project-plan.md`), Server-Vertrag aus `acceptance-schritt-5a.md` (Server-Branch `task/issue-231-qr-bootstrap-geraetekopplung-login`, Issue #231, **nicht gemergt**).

## Ziel

Ein Nutzer kann die App starten, auf iOS/Android den QR-Code aus der Server-Profilseite „Gerät koppeln" scannen oder auf allen Plattformen (inkl. Windows) die Ticket-Adresse bzw. den Kurzcode manuell eingeben. Die App prüft die Server-Erreichbarkeit, speichert den Server, führt den verschlüsselten Bootstrap-Austausch durch, speichert Gerätetoken + JWT-Session + Refresh-Token sicher und landet **direkt auf Home** – ohne Adress- und ohne Passworteingabe.

## Server-Vertrag (bindend, aus Schritt 5a)

- `POST /api/pairing/bootstrap` – öffentlich/anonym wie `exchange`:
  - Request: `{ "ticket": "...", "clientPublicKey": "...", "deviceName": "..." }`
  - Response: `{ "serverPublicKey": "...", "encryptedPayload": "..." }`
  - Entschlüsseltes Payload: `{ "deviceToken": "...", "token": "...", "expires": "...", "refreshToken": "..." }`
  - `ticket` akzeptiert das lange opake Geheimnis **oder** den 8-Zeichen-Kurzcode-Alias.
  - Krypto identisch zu `exchange`: ECDH P-256 + AES-256-GCM, Format `nonce(12) || ciphertext || tag(16)` – bestehende `PairingCrypto`-Routine wiederverwenden.
  - Fehler: 400 = `Ungültiger Bootstrap-Request.`, 401 = `Ungültiges oder abgelaufenes Pairing-Ticket.`, 429 = Rate-Limit/IP-Block.
- `POST /api/auth/refresh` – `[ApiTokenCheck(MauiOnly)]` (Gate-Header nötig): Request `{ "refreshToken" }`, Response `{ "token", "expires", "refreshToken" }`; Rotation + Reuse-Detection serverseitig.
- `POST /api/auth/logout` – Request `{ "refreshToken" }`, idempotent HTTP 200.
- QR-Payload: `https://<server>/pairing?t=<ticket>` – das abschließende `/pairing`-Segment gehört **nicht** zur Serveradresse (OP-4); Unterpfad bleibt erhalten: `https://host/videoplayer/pairing?t=X` → `https://host/videoplayer/`.

## Funktionale Anforderungen

1. **QR-URL-Parser** (plattformneutral, testbar):
   - Nur `http`/`https`; Pfad muss mit `/pairing` enden (nur letztes Segment abschneiden, Unterpfad erhalten).
   - `t=` opak extrahieren (Query-Encoding respektieren); **kein** `PairingCode.TryNormalize` auf lange Tickets.
   - Ablehnen: nicht-http(s), fehlendes `/pairing`, fehlender/leerer `t`-Parameter, leere/kaputte Eingabe.
   - Kurzcode-Eingabe (keine URL): opak, geht direkt an `bootstrap` gegen den aktiven Server.
2. **Kamera-Scan** – nur iOS/Android:
   - ZXing.Net.Maui.Controls 0.10.4 (MIT, 14.08.2026, >7 Tage alt, net10.0-Assets) – Version pinnen, Entscheidung dokumentieren.
   - Windows: **kein** Scan (ZXing ohne Windows-Kamera-Support; App ist unpackaged → keine Webcam-Capability) → manueller Einstieg bleibt.
   - iOS `NSCameraUsageDescription`, Android `CAMERA`-Permission; Laufzeit-Permission anfordern.
   - Kamera-Page schließt sich nach erfolgreichem Scan.
3. **Fluss-Reihenfolge**:
   - QR/Ticket-URL → Basisadresse + Ticket; **zuerst** Erreichbarkeit prüfen → Server als aktiv speichern + `KnownServers` → **danach** Bootstrap-Exchange.
   - Kurzcode → direkt Bootstrap gegen aktiven Server (Server wurde bereits verbunden/geprüft).
   - Persistieren: `deviceToken` via `AppSecretsService`/SecureStorage (Device-Gate aktivieren), JWT `token` + `expires` via `AuthSessionService`, `refreshToken` sicher.
   - Direkt auf Home (bestehender Connected-Übergang); keine Adress-/Passwortabfrage nach erfolgreichem Bootstrap.
4. **Refresh-Pfad** (`AuthSessionService`):
   - Vor Credential-Fallback `POST /api/auth/refresh` mit gespeichertem Refresh-Token; rotierte Tokens persistieren.
   - Refresh-401 (kein Gate-Problem) → Refresh-Token löschen, bisheriger Credential-Fallback.
   - Logout → `POST /api/auth/logout` (best effort) + Refresh-Token löschen.
5. **UI** (`ServerSetupPage`, modernisiert in Schritt 4):
   - Neue Sektion: Ticket-Adresse/Kurzcode-Eingabe (alle Plattformen) + „QR-Code scannen" (nur iOS/Android) + Loading-/Fehlerzustände.
   - Verständliche Fehlertexte: Server nicht erreichbar, Ticket ungültig/abgelaufen, Kurzcode ungültig, Rate-Limit (429), Server ohne Bootstrap-Support (404).
   - Fallbacks bleiben: Admin-Code (`exchange`), manuelle Serveradresse, E-Mail/Passwort-Login. `PairingCodeEntry` weiterhin ohne `MaxLength`.
6. **MVU/Sicherheit**: alles über `AppModel`/`AppMessage`/`AppEffect` + Handler; **keine** Secrets im Dev-API-`/model`-Snapshot.

## Akzeptanzkriterien

- Manueller Kurzcode-Flug auf Windows endet auf Home (E2E-nachgewiesen); URL-Paste-Flug ebenfalls.
- Parser deckt alle Ablehnungsfälle ab (Unit-Tests).
- Bootstrap-Client verschlüsselt/entschlüsselt korrekt gegen Mock-Server (ECDH/AES-GCM wie echter Server).
- Refresh rotiert und persistiert; Logout ruft `auth/logout` und löscht den Refresh-Token.
- Dev-API-Snapshot bleibt secret-frei; Scan-Button nur auf iOS/Android sichtbar.
- Beide Testsuiten grün; Screenshots der UI-Zustände via FlaUI/Dev-API-Harness.

## Nicht-Ziele / Restrisiken

- Kein Windows-Kamera-Scan (dokumentiert).
- Echter iOS-/Android-Kamera-Lauf = manueller Restnachweis.
- Live-Lauf gegen den Server-Branch nur mit Scratch-DB; sonst manueller Restnachweis.
