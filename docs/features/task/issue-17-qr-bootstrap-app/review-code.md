# Code-Review — App-seitiges QR-/Ticket-Onboarding (Issue 17, Schritt 5b)

Geprüft: Diff gegen Basisbranch `project/issue-17-testbarkeit-und-onboarding`
(Produktionscode + Tests, 2026-09-24).

## Geprüfte Bereiche

### Parser (`VideoPlayer.Mvu/BootstrapTicketParser.cs`)

- URL-Erkennung über `"://"`, dann `Uri.TryCreate` + Scheme-Whitelist
  (http/https) — konsistent zu `ServerAddress`-Regeln; kein
  `PairingCode.TryNormalize` auf dem Ticket (opak, case-sensitiv).
- `pairing`-Endsegment: `OrdinalIgnoreCase`, Trailing-Slash tolerant,
  Unterpfade bleiben über `basePath`-Slice + `TryNormalize` erhalten.
- Ticket-Extraktion: `t` case-insensitiv, erstes Vorkommen,
  `Uri.UnescapeDataString` — opak dekodiert, kein Truncating.

**Befund 1 (behoben):** `Uri.UnescapeDataString` wirft
`UriFormatException` bei Eingaben > 32 766 Zeichen — ein solches Ticket
hätte `TryParse` aus der `Update`-Schleife heraus crashen lassen.
Fix: `try/catch` → `null` (ungültige Eingabe statt Absturz).

### ApiClient (`DevicePairingClient`, `VideoPlayerApiClient`, Models)

- `BootstrapAsync` teilt `PostJsonAsync` mit `ExchangeAsync`: kein
  `X-API-Key`, kein Bearer, kein Retry — verifiziert per Request-
  Aufzeichnung in `DevicePairingClientBootstrapTests`.
- Payload-Validierung: alle vier Pflichtfelder werden geprüft;
  unvollständige Pakete → `InvalidOperationException` → `Failed`
  (keine Teil-Session).
- `RefreshSessionAsync`/`LogoutSessionAsync` mit `allowReauth: false` —
  der Refresh-Aufruf löst selbst keine 401-Erneuerung aus
  (Rekursionsschutz), getestet.
- camelCase-Verträge per `JsonPropertyName` gepinnt; `deviceName`
  bei `null` weggelassen (Server-Fallback).

### AuthSessionService

- `SilentReloginAsync` → `TryRefreshSessionAsync` vor dem
  Credential-Pfad: Erfolg persistiert Token + rotierten Refresh-Token
  in einem Schritt (keine inkonsistente Halbrotation — beide Writes
  erfolgen erst nach vollständig validierter Antwort).
- 401/403 am Refresh-Endpunkt → `HandleGateRejectionAsync` (Gate-Probe):
  Gerätewiderruf bleibt Gerätewiderruf; Gate-ok → Refresh-Token
  verwerfen + Credential-Fallback.
- `RevokeRefreshTokenAsync` best-effort: kurzlebiger Client (Gate-Header
  automatisch via Konstruktor), Fehler nur Debug-Log — lokale Abmeldung
  hängt nicht am Serverkontakt; Endpoint-Idempotenz passt.
- Secrets gehen ausschließlich über `AppSecretsService` (SecretStore) —
  keine Tokens in `settings`/Logs; Debug-Ausgaben enthalten nur
  Fehlermeldungen, keine Tokenwerte.

### MVU (`AppUpdate`, Messages/Effects/Model)

- Doppel-Submit-Guard, Stale-Guards (`!IsBootstrapRunning` → Unchanged)
  an beiden asynchronen Einstiegspunkten — deckungsgleich mit dem
  Pairing-Muster.
- `BootstrapServerChecked` persistiert den Server erst nach erfolgreicher
  Prüfung und sequenziert `PersistActiveServerEffect` vor
  `ExchangeBootstrapTicketEffect` (Gate-Provider geladen, bevor das
  Ticket-Session-Paket verarbeitet wird).
- `DeviceTokenRevoked`/Servercheck-Übergänge räumen den Bootstrap-
  Kontext (kein Zombie-Laufzustand).

### Effekt-Handler (`ShellAppEffectHandler`)

- `ExchangeBootstrapTicketAsync`: Session-Persistenz nur bei aktuellem
  Kontext (`IsBootstrapRunning` + normalisierter Adressvergleich);
  `SetPairedTokenIfCurrentAsync` als atomarer zweiter Guard; veralteter
  Kontext → Geräte-Token unter eigener Adresse, Session verworfen.
- Status-Mapping vollständig (401/400/429/404/null/sonst) — deckt sich
  mit `BootstrapErrorFor`.
- Abbruchtoken wird durchgereicht; `OperationCanceledException` bei
  angefordertem Abbruch wird nicht als Fehler gemappt.

### UI (`ServerSetupPage`, `QrScannerPage`)

- Alle Steuerelemente werden ausschließlich aus `UpdateUi(model)`
  gesteuert; Dispatches nur via Messages — kein direkter
  Service-Zugriff aus der Page.
- `OnQrScanClicked`: `BarcodeScanning.IsSupported` → Permission →
  `PushModalAsync`; Fehlschläge als `DisplayAlert` mit Ersatzweg
  (bewusste, geplante UI-lokale Ausnahme — der Fluss startet erst mit
  gelesenem Code).
- `QrScannerPage` komplett hinter `#if ANDROID || IOS` — kein
  Compile-/Laufzeitpfad auf Windows; ZXing-Paket target-konditioniert.

**Befund 2 (behoben):** das Einmal-Flag `handled` in
`OnBarcodesDetected` war ein einfaches `bool` — das Detection-Event
kann aus dem Analyse-Loop parallel feuern. Fix:
`Interlocked.Exchange(ref handled, 1)`.

### E2E-Infrastruktur

- `MockVideoServer`: Bootstrap (Ticket + Kurzcode-Alias), Refresh
  (atomare Rotation, Gate-geschützt), Logout (idempotent) — deckt den
  5a-Vertrag ab; `MockPairingCrypto.EncryptPayload` verallgemeinert.
- `AppInstance`: `profile`-Parameter für Neustart-Tests +
  `Dispose(deleteProfile:)` — bestehende Aufrufer unverändert
  (Default-Verhalten identisch).

## Befunde

| # | Befund | Status |
|---|--------|--------|
| 1 | `UnescapeDataString`-Überlängen-Ausnahme in `ExtractTicket` → Crash statt `false` | **behoben** (`try/catch` → `null`) |
| 2 | Nicht-atomares `handled`-Flag in `QrScannerPage.OnBarcodesDetected` → potenzielles Doppel-Dispatch | **behoben** (`Interlocked.Exchange`) |

**Status: Keine offenen Befunde** (beide Fundstellen in dieser Iteration behoben).
