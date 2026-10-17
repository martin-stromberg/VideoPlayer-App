# Bestandsaufnahme: E2E-Tests mit FlaUI gegen VideoPlayer.Maui.exe

Analysiert wurde der Stand des Branches (Schritt 1+2 bereits gemergt):
Developer-API, MVU-Fluss der Server-Einrichtung/-Anmeldung, App-Start der
unpackageden Windows-Exe, Profil-Isolation, Testlandschaft und die beiden
Backend-Optionen für serverabhängige Szenarien. Zusätzlich wurde ein
Live-Smoke-Test der laufenden Debug-App durchgeführt.

## Zusammenfassung

- **Umgebung verifiziert (Smoke-Test 2026-09-24):** Die vorhandene
  Debug-Exe
  `VideoPlayer.Maui/bin/Debug/net10.0-windows10.0.19041.0/win-x64/VideoPlayer.Maui.exe`
  startet unter `VIDEOPLAYER_PROFILE=e2esmoke`, schreibt `devapi.json`
  ins Exe-Verzeichnis (`{"port":64450,"pid":…}`) und beantwortet
  `GET /model` (`currentPage:"ServerSetup"`, `connectionState:
  "NeedsServerSetup"`) sowie `GET /tree` (voller Elementbaum der
  ServerSetupPage inkl. AutomationIds). Interaktiver Desktop ist
  vorhanden (mehrere Fenster-Prozesse laufen).
- **Dev-API-Vertrag (aus Schritt 2):** `GET /model` liefert den
  kuratierten `DevApiModelSnapshot` (camelCase-Felder, siehe
  `inventory/dev-api-vertraege.md`); `GET /tree` liefert Knoten mit
  `automationId`/`type`/`isVisible`/`bounds`/`text`/`placeholder`/
  `children`/`items` (CollectionView-Items aus `ItemsSource` abgeleitet);
  `POST /invoke` feuert Clicked-Äquivalent per `automationId`;
  `POST /set-text` setzt `InputView.Text` inkl. `TextChanged`.
  **Kein `dispatch`-Endpunkt** (Projekt-Entscheid OP-5).
- **Lücke für „bekannten Server wählen":** `/invoke` adressiert nur
  AutomationIds von Visual-Elementen; CollectionView-Items sind lesbar
  (`/tree`-`items` mit `index`/`text`/`detail`), aber **nicht
  auslösbar** — die Item-Auswahl braucht eine kleine Erweiterung von
  `/invoke` (optionales `itemIndex` auf dem CollectionView-Knoten →
  `SelectedItem` setzen, feuert `SelectionChanged` → echte
  `OnKnownSelectionChanged`-Kette). Details: `inventory/dev-api-vertraege.md`.
- **MVU-Fluss (verifiziert am Code):** Erststart ohne Server →
  `ServerSetupPage`; Adresse → `ConnectButton` → `CheckCandidateServerEffect`
  → `GET api/health` (200=erreichbar, 401=GateRejected, sonst
  unerreichbar→`serverSetupError`); bei Erfolg: `pairingRequired` =
  kein Geräte-Token UND kein statischer Gate-Schlüssel →
  `IsPairingRequired` → `PairingSection` sichtbar; Pairing-Code →
  `POST api/pairing/exchange` (ECDH P-256 + AES-256-GCM) → Geräte-Token
  → Auto-Login → Login-Seite (keine gespeicherten Credentials) oder
  Home. Details: `inventory/mvu-fluss.md`.
- **Statischer Gate-Schlüssel auf diesem Rechner vorhanden:**
  Benutzer-Umgebungsvariable `VIDEOWEBPLAYER_MAUI_API_TOKEN` ist
  gesetzt! Der Test-Launcher **muss** diese Variable im Kindprozess
  entfernen, sonst gilt `IsPairingRequired=false` und alle
  Pairing-Szenarien scheitern. Assembly-Metadata ist standardmäßig der
  Platzhalter `<MAUI_CLIENT_API_TOKEN>`; Token-Datei
  `%LocalAppData%/VideoPlayer/client-api-token.txt` existiert hier
  nicht.
- **Backend-Entscheidungsgrundlage:** echter Server-Checkout existiert
  (`487c3041-…`), aber Seed-/Pairing-Code-Erzeugung braucht DB- oder
  Admin-UI-Zugriff; `api/health` ist dort **ungeatet** (immer 200) —
  die Pairing-Erforderlichkeit entscheidet die App lokal
  (`IsPairingRequiredAsync`). Mock-Server ist klein: `api/health`→200,
  `api/pairing/exchange` mit ECDH/AES-GCM (~40 Zeilen, nur
  Bordmittel), `api/auth/login` (Gate `X-API-Key`=Geräte-Token +
  Credentials → `{token, expires}`), leere Listen-Endpunkte
  (`api/Sources`, `api/items/recent`, `api/continue-watching`,
  `api/favorites`). Nach dem Login ruft die App genau diese Endpunkte
  ab (Sync); SignalR `hubs/mediaupdate` und `api/pictures/hero-background`
  dürfen fehlschlagen (werden abgefangen). Details:
  `inventory/backend-optionen.md`.
- **FlaUI-Version:** `FlaUI.UIA3` 5.0.0 (2025-02-25, deutlich >7 Tage
  alt, 1,5 M Downloads) — bewährt; xUnit v3 3.2.2 +
  `xunit.runner.visualstudio` 3.1.5 + `Microsoft.NET.Test.Sdk` 18.8.1
  wie `VideoWebPlayer.Maui.Tests`.
- **Test-Isolation:** `VIDEOPLAYER_PROFILE` erzeugt eigenes
  Unterverzeichnis (`%APPDATA%\VideoPlayer\<profil>`) + Präfix für
  Preferences-/SecureStorage-Schlüssel → eigene `known_servers`,
  eigene Geräte-Tokens, eigene Credentials. Zusätzlich `devapi.json`
  pro Lauf neu lesen (Port ist systemvergeben).
- **Contract-Tests beachten:** `DevApiContractTests` pinnt genau 4
  `router.Map(`-Aufrufe und das Fehlen von „dispatch" — die
  `/invoke`-Erweiterung hält diese Grenze ein (gleicher Endpunkt,
  erweiterter Body).

## Details

- [Dev-API-Verträge und Item-Auswahl-Lücke](inventory/dev-api-vertraege.md)
- [MVU-Fluss: Server-Setup → Pairing → Login → Home](inventory/mvu-fluss.md)
- [Backend-Optionen (echter Server vs. Mock)](inventory/backend-optionen.md)
- [Umgebungsnachweise (Smoke-Test, Pfade, Paketversionen)](inventory/umgebung.md)
