# Abnahme Schritt 5b — QR-/Ticket-Onboarding in der App

Schrittbranch: `task/issue-17-qr-bootstrap-app` → gemergt in `241bef6`.
Lifecycle-Artefakte: `docs/features/task/issue-17-qr-bootstrap-app/` (im Repo erhalten, Projektkonvention).
Server-Vertrag: Issue #231 (Server-Branch `task/issue-231-…`, Merge ist Nutzer-Entscheidung).

## Ergebnis

- **`BootstrapTicketParser`** (`VideoPlayer.Mvu`): parst `https://<server>/pairing?t=<ticket>` — `/pairing`-Suffix abschneiden (Unterpfad-sicher), `t=` opak; Kurzcode (8 Zeichen, festes Alphabet) direkt als Ticket.
- **`api/pairing/bootstrap`-Client** (`DevicePairingClient`/`VideoPlayerApiClient`): gleiche ECDH-P-256/AES-256-GCM-Entschlüsselung wie `exchange`; Payload = `{ deviceToken, token, expires, refreshToken }`.
- **Refresh-Pfad** (`AuthSessionService`): `POST api/auth/refresh` vor Credential-Fallback, Rotation persistiert; `POST api/auth/logout` widerruft; bei Refresh-401 → bisheriger Login-Fallback.
- **UI:** `ServerSetupPage` um Karte „Mit Kopplungscode verbinden" ergänzt (Ticket-Adresse oder Kurzcode, „Kopplungscode einlösen"); `QrScannerPage` unter `#if ANDROID || IOS` (Kamera-Scan mit Permission), Windows = manueller Einstieg.
- **MVU:** neue Messages/Effects für Bootstrap, `isBootstrapRunning`/`bootstrapError` im `/model`-Snapshot (keine Secrets).
- **Flow:** Basisadresse prüfen → als aktiven Server + `KnownServers` speichern → erst dann Exchange → direkt auf Home, ohne Adress-/Passworteingabe.
- **Fehler-Mapping:** 400/401/429 + Erreichbarkeit als verständliche Meldungen.
- **Fallbacks erhalten:** Admin-Code (`exchange`), manuelle Servereingabe, E-Mail/Passwort-Login unverändert.

## Verifikation

- **1013/1013** `VideoWebPlayer.Maui.Tests` (+86 neue: Parser, Contract, MVU-Transitions, Refresh-Rotation, Bootstrap-Client).
- **17/17** `VideoPlayer.E2E.Tests` (~1,5 min; `MockVideoServer` um bootstrap/refresh/logout erweitert; `BootstrapCount=1, LoginCount=0, PairingExchangeCount=0` belegt: kein Login nötig).
- Windows-Build 0 Warnungen/0 Fehler; 4 Screenshots (Erststart, laufende Prüfung, Inline-Fehler, direkter Home-Einstieg).
- Reviews: vollständig/keine Befunde; 2 in Iteration behoben (`Uri.UnescapeDataString`-Crash, QR-Erkennungs-Race via `Interlocked.Exchange`).

## Akzeptanzkriterien

1. ✅ QR-Parser mit `/pairing`-Konvention, Unterpfad-sicher.
2. ✅ Kamera-Scan iOS/Android (Paket gepinnt + dokumentiert), Windows = manueller Einstieg.
3. ✅ Bootstrap-Exchange mit Geräte-Token + JWT + Refresh-Token; direkter Home-Einstieg.
4. ✅ Refresh-Rotation in `AuthSessionService`, Logout-Widerruf.
5. ✅ Fehler verständlich (Erreichbarkeit, ungültig/abgelaufen, Kurzcode, 429).
6. ✅ Fallbacks unverändert.

## Restnachweise (bewusst offen)

- Echter iOS-/Android-Kamera-Lauf (Scanner kompiliert nur dort; Verdrahtung per Contract-Tests gepinnt).
- Live-Lauf App ↔ echter Server — hängt vom Merge des Server-Branches `task/issue-231-…` ab; gegen `MockVideoServer` ist der volle Ablauf verifiziert.
