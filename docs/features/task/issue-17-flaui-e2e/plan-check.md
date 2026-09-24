# Plan-Prüfung: E2E-Tests mit FlaUI

Status: **Plan vollständig**

Geprüft: `plan.md` gegen `requirement.md` (Akzeptanzkriterien des
Projektplans Schritt 3) und den Testbedarf.

## Abdeckung der Akzeptanzkriterien

| Kriterium | Abgedeckt durch |
|-----------|-----------------|
| 1. E2E-Projekt baut + startet die Exe selbstständig, RID verifiziert | `AppFixture` (Lazy `dotnet build` Debug) + `AppInstance` (Exe-Pfad `win-x64` in `inventory/umgebung.md` verifiziert) |
| 2a. Server-Setup (Adresse → Verbinden → `connectionState`/Fehler) | `FreshStart_ShowsServerSetup`, `InvalidAddress_ShowsError`, `UnreachableServer_ShowsError`, `ReachableServer_RequiresPairing` |
| 2b. Pairing-Flow (Code → Koppeln → Erfolg/Fehler) | `MalformedCode_ShowsFormatError`, `WrongCode_ShowsError` (401→`InvalidPairingCodeError`), `ValidCode_ShowsLoginPage` |
| 2c. Serverwechsel (#15-Regression, Rückkehr Startseite) | `ServerSwitch_ReturnsToHome` (manuelle Adresse B + Item-Auswahl A) |
| 2d. Anmeldung (Credentials → Home) | `ValidLogin_ShowsHome`, `WrongPassword_ShowsError` |
| 3. `dotnet test` lokal + Isolation via `VIDEOPLAYER_PROFILE` + CI dokumentiert | `AppInstance` setzt Profil je Lauf; CI als optional dokumentiert (Verifikation-Abschnitt) |
| 4. Keine Koordinatenklicks für CollectionView-Items | `/invoke`-Erweiterung `itemIndex` (UI-Thread `SelectedItem` → echte SelectionChanged-Kette) |

## Konsistenz-Prüfungen

- **Treiberstrategie (festgelegt):** FlaUI für Start/Klicks/Eingaben,
  Dev-API für Assertions + Listen — im Plan eingehalten.
- **Kein `dispatch`:** keine neue Route; `itemIndex` erweitert den
  bestehenden `/invoke`-Body → `DevApiContractTests` (4 `router.Map(`,
  kein „dispatch") bleiben gültig. Geprüft gegen
  `VideoWebPlayer.Maui.Tests/DevApiContractTests.cs`.
- **Gate-Falle:** `VIDEOWEBPLAYER_MAUI_API_TOKEN` (Benutzer-Env auf
  diesem Rechner gesetzt) wird im Kindprozess entfernt — dokumentiert
  in `inventory/umgebung.md`, im Launcher umgesetzt. Ohne das wären
  Pairing-Tests falsch-grün-fähig (kein Pairing nötig).
- **Timing-Risiko:** `Unreachable`-Pfad hat Client-Retries (1+2+4 s) —
  Warte-Timeouts von 20–40 s eingeplant, kein festes Sleep.
- **Pairing-Vertrag:** Mock bildet `POST api/pairing/exchange` mit
  ECDH P-256 (`DeriveKeyFromHmac`, SHA-256) + AES-256-GCM
  `nonce(12)‖ct‖tag(16)` ab — deckungsgleich mit `PairingCrypto`
  (App) und `PairingService` (Server-Repo).
- **Login-Gate:** Mock prüft `X-API-Key` == ausgestelltes Geräte-Token
  (`MauiOnly`-Semantik des echten Servers).
- **Nach-Login-Endpunkte:** Mock liefert `[]` für
  `api/favorites`/`api/continue-watching`/`api/Sources`/
  `api/items/recent`; 404 (nie 5xx) für alles andere → keine
  Retry-Verzögerungen, SignalR-/Hero-Fehlschläge sind unkritisch
  (gefangen).
- **Test-Isolation:** Profil je App-Instanz → eigene `known_servers`,
  Geräte-Tokens, Credentials; `devapi.json` je Lauf neu gelesen.
- **FlaUI-Paket:** 5.0.0 gepinnt (2025-02-25 — deutlich >7 Tage alt).

## Festgestellte Lücken — geschlossen

- Item-Auswahl in `KnownServersView` war ohne Koordinatenklick nicht
  möglich → `/invoke` `itemIndex`-Erweiterung geplant (im Plan).
- `CheckIndicator`/`PairingSection` haben keine `AutomationId` →
  Sichtbarkeits-Assertions über `/model`-Flags statt `/tree`-Knoten
  (dokumentiert in `inventory/dev-api-vertraege.md`).

## Nicht-Ziele (bewusst)

- Discovery-Button-Szenario (mDNS/UDP nicht hermetisch mockbar).
- CI-Workflow-Ausführung (Billing + kein Desktop) — dokumentiert.
- Echter Server-Test automatisiert — manueller Nachweisweg dokumentiert.
