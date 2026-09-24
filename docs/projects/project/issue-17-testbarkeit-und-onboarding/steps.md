# Entwicklungsschritte — Projekt Issue #17

Basis: `project-plan.md`. Jeder Schritt läuft als eigener Lifecycle auf einem Schrittbranch vom Projektbranch `project/issue-17-testbarkeit-und-onboarding` und wird nach Abnahme zurückgemergt.

| # | Schritt | Schrittbranch | Repo | Status | Abnahme |
|---|---------|---------------|------|--------|---------|
| 1 | iOS-Deployment-Skript reparieren (MSB1006) | `task/issue-17-ios-deploy-fix` | App | ✅ abgenommen (`5cc6d46`) | `acceptance-schritt-1.md` |
| 2 | Developer-API (Debug/Opt-in, Loopback, Port 0) | `task/issue-17-developer-api` | App | ✅ abgenommen (`728afe3`) | `acceptance-schritt-2.md` |
| 3 | E2E-Tests mit FlaUI (Kombination mit Dev-API) | `task/issue-17-flaui-e2e` | App | in Arbeit | – |
| 4 | UI der Serververbindung modernisieren | `task/issue-17-server-setup-ui` | App | ausstehend | – |
| 5a | QR-Bootstrap Server (Ticket, Profilseite, Bootstrap-Endpunkt, Refresh-Token) | eigener Task-Branch im Server-Repo | Server `487c3041-…` | ausstehend (Vorab: issue-228-Merge prüfen) | – |
| 5b | QR-Bootstrap App (Scan, Parsing, Exchange, Session) | `task/issue-17-qr-bootstrap-app` | App | ausstehend (nach 5a-Vertrag, empfohlen nach 4) | – |

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
