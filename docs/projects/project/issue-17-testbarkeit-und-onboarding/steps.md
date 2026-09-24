# Entwicklungsschritte — Projekt Issue #17

Basis: `project-plan.md`. Jeder Schritt läuft als eigener Lifecycle auf einem Schrittbranch vom Projektbranch `project/issue-17-testbarkeit-und-onboarding` und wird nach Abnahme zurückgemergt.

| # | Schritt | Schrittbranch | Repo | Status | Abnahme |
|---|---------|---------------|------|--------|---------|
| 1 | iOS-Deployment-Skript reparieren (MSB1006) | `task/issue-17-ios-deploy-fix` | App | ✅ abgenommen (`5cc6d46`) | `acceptance-schritt-1.md` |
| 2 | Developer-API (Debug/Opt-in, Loopback, Port 0) | `task/issue-17-developer-api` | App | ✅ abgenommen (`728afe3`) | `acceptance-schritt-2.md` |
| 3 | E2E-Tests mit FlaUI (Kombination mit Dev-API) | `task/issue-17-flaui-e2e` | App | ✅ abgenommen (`8796392`) | `acceptance-schritt-3.md` |
| 4 | UI der Serververbindung modernisieren | `task/issue-17-server-setup-ui` | App | ✅ abgenommen (`9c6af1e`) | `acceptance-schritt-4.md` |
| 5a | QR-Bootstrap Server (Ticket, Profilseite, Bootstrap-Endpunkt, Refresh-Token) | `task/issue-231-qr-bootstrap-geraetekopplung-login` | Server `487c3041-…` | ✅ abgenommen (`52fc66c`, Merge/PR = Nutzer) | `acceptance-schritt-5a.md` |
| 5b | QR-Bootstrap App (Scan, Parsing, Exchange, Session) | `task/issue-17-qr-bootstrap-app` | App | in Arbeit | – |

## Entschiedene Punkte (Referenz)

- OP-1: Kombination FlaUI + Dev-API
- OP-2: Klassischer Refresh-Token mit Rotation (serverseitig, Schritt 5a)
- OP-3: `TcpListener` Port 0 + Mini-HTTP, keine Paketabhängigkeit
- OP-4: QR = `<basis>/pairing?t=<ticket>`; App schneidet abschließendes `/pairing`-Segment ab
- OP-5: Kein `dispatch`-Endpunkt (nur `/model`, `/tree`, `/invoke`, `/set-text`)
- OP-6: Neuer Endpunkt `api/pairing/bootstrap`; `exchange` bleibt unverändert
- OP-7: Alle eingeloggten Nutzer + Rate-Limit je User + konfigurierbare Admin-Option (Default aus)

## Verlauf

- **Schritt 1 abgenommen:** MSB1006-Ursache programmatisch belegt (`"`/Zeilenumbruch im Property-Wert zerlegen die rsp-Zeile); Fix = testbare Helper-Datei `iOS-Deployment.ResponseFile.ps1` mit `Assert-PropertyValue` (verständlicher Abbruch statt MSB1006), maskierter Diagnoseausgabe, UTF-8 ohne BOM; 896/896 Tests; Commits `76eb670`, `32bfd5c`, Merge `5cc6d46`. Geräte-/Store-Lauf bleibt manueller Nachweis.
- **Schritt 2 abgenommen:** Developer-API mit exakt 4 Endpunkten (`GET /model`, `GET /tree` inkl. CollectionView-Items, `POST /invoke`, `POST /set-text`), `TcpListener` Loopback:0, `devapi.json`, kompletter `#if DEBUG || DEV_API`-Ausschluss; live in Debug-App verifiziert (echte UI-Kette, Fehlercodes 404/405/400); 926/926 Tests; Commits `b58319b`, `ab81c6f`, Merge `728afe3`.
- **Schritt 3 abgenommen:** `VideoPlayer.E2E.Tests` (xUnit+FlaUI.UIA3) startet echte Debug-Exe mit `VIDEOPLAYER_PROFILE`-Isolation; hermetischer `MockVideoServer` (ECDH/AES-GCM wie echter Server); 10/10 E2E-Tests grün inkl. #15-Serverwechsel-Regression; 926/926 Bestandstests; `invoke` um `itemIndex` erweitert; `ClientGate`-Tokenpfad profil-isoliert; Commits `41dd282`, `eef4863`, Merge `8796392`. Hinweis: lokale `ApplicationVersion`-Änderung wurde revertiert, `.ios-deploy.user.json` jetzt in `.gitignore`.
- **Schritt 4 abgenommen:** `ServerSetupPage` modernisiert — Kicker-Badge, `ActiveServerCard` mit Health-Pill (`ServerHealth`/`ServerHealthText` jetzt verdrahtet), Server-Karten, geführte `PairingSection` („NÄCHSTER SCHRITT"); MVU-Fluss unverändert, Contract-Pins bewusst erweitert; 927/927 + 11/11 E2E, Screenshots in `live-verification/`; Commits `f983df2`, `68b5c2f`, Merge `9c6af1e`.
- **Schritt 5a abgenommen (Server-Repo):** Issue #231; `api/pairing/bootstrap` (öffentlich, ECDH+AES-GCM, Payload = deviceToken+JWT+refreshToken), `api/auth/refresh`+`logout` mit Rotation, `PairingCode.Kind`+`TicketHash` (Kurzcode = Alias), RefreshToken-Entität (gehasht, Reuse-Detection), Profilseite `Account/Manage/Devices` mit QR (QRCoder 1.8.0 MIT); `exchange` unverändert; 447/447 Server-Tests; Commits `83f6f33`, `52fc66c` auf `task/issue-231-…` — **nicht gemergt, PR/Merge ist Nutzer-Entscheidung**; Deployment braucht `dotnet ef database update`.
