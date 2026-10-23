# Abnahme Schritt 3 — E2E-Tests mit FlaUI

Schrittbranch: `task/issue-17-flaui-e2e` → gemergt in `8796392`.
Lifecycle-Artefakte: `docs/features/task/issue-17-flaui-e2e/` (im Repo erhalten, Konvention wie Issue #15).

## Ergebnis

- **Neues Projekt `VideoPlayer.E2E.Tests`** (xUnit v3 + FlaUI.UIA3, net10.0-windows) in der Solution; startet die echte Debug-`VideoPlayer.Maui.exe` je Test mit isoliertem `VIDEOPLAYER_PROFILE` und garantierter Prozess-Aufräumung.
- **Kombinationsstrategie umgesetzt (OP-1):** FlaUI bedient die UI über echte UIA-Patterns (Value/Invoke/SelectionItem, **keine Koordinatenklicks**); Zustands-Assertions über die Developer-API (`GET /model`); `devapi.json`-Portdiscovery mit PID-Prüfung.
- **Backend-Entscheidung: hermetischer `MockVideoServer`** (`TcpListener` Loopback, OS-Port): `api/health`, `api/pairing/exchange` (ECDH P-256 + AES-256-GCM, deckungsgleich mit `PairingCrypto`), `api/auth/login`, Listen-Endpunkte; unterscheidet Gate- (`problem+json`) vs. Credential-Ablehnung (`text/plain`) wie der echte Server. Deterministisch, kein Server-Checkout nötig, Kryptovertrag echt durchgespielt. Real-Server-Lauf bleibt dokumentierter manueller Nachweis.
- **Szenarien (10 Tests):** Server-Einrichtung (4: Erststart, ungültige Adresse, nicht erreichbar mit sichtbarem Fehler, erreichbar→Pairing), Pairing (3: Formatfehler, falscher Code 401, gültiger Code→Anmeldemaske), Anmeldung (2: falsches Passwort→Inline-Fehler, gültig→Home/`Connected`), Serverwechsel (1: **#15-Regression** — Einstellungen→Server wechseln→bekannten Server per `itemIndex` wählen→Rückkehr zur Startseite).

## Kleinere Produktivänderungen (im Schritt entstanden, fachlich sinnvoll)

- `POST /invoke` um optionales `itemIndex` erweitert → `CollectionView.SelectedItem` (echtes `SelectionChanged`) — koordinatenfreie Listen-Auswahl.
- `ClientGate.TokenFileDirectory` profil-isoliert (`VIDEOPLAYER_PROFILE`-Unterverzeichnis) — isolierte Profile erben nicht mehr die `client-api-token.txt` des Standardprofils; Standardprofil-Pfad bytegleich.

## Testergebnis

- **E2E: 10/10 bestanden** (17 min, echte App-Starts, interaktive Windows-Session).
- **Bestandssuite: 926/926 grün** — keine Regression.

## Akzeptanzkriterien

1. ✅ E2E-Projekt baut und startet `VideoPlayer.Maui.exe` selbstständig (Debug-Build, `devapi.json`-Discovery).
2. ✅ Szenarien abgedeckt inkl. Serverwechsel-Regression (#15).
3. ✅ `dotnet test` lokal lauffähig; `VIDEOPLAYER_PROFILE`-Isolation; CI als optional dokumentiert (kein Desktop auf GH-Runnern).
4. ✅ Keine Koordinatenklicks für CollectionView-Items (`itemIndex` über Dev-API).

## Offene Nachweise / Hinweise

- Real-Server-Lauf optional (manueller Nachweis); CI-Ausführung blockiert durch Billing + fehlenden Desktop — dokumentiert.
- Shell-Verhalten beobachtet: nach Serverwechsel bleibt `serversetup` als geschobene Route im Einstellungen-Tab (bewusstes Shell-Stack-Verhalten, kein Fehler dieses Schritts — in `review-code.md` vermerkt).
- Laufzeit ~17 min pro Volltest.

## Commits

- `41dd282` plan, `eef4863` feat (30 Dateien, +2384/−26), Merge `8796392`.

## Hinweis außerhalb des Schrittumfangs

- Der Schritt-Lifecycle hat die lokale uncommitted `ApplicationVersion`-Änderung (1→2) in `VideoPlayer.Maui.csproj` revertiert und `.ios-deploy.user.json` in `.gitignore` aufgenommen. Dem Anwender gemeldet — bei gewolltem Build-Bump Rückmeldung abwarten (Deploy-Skript bump-t beim nächsten `store`-Lauf ohnehin erneut).
