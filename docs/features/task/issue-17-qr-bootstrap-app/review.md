# Plan-Review — App-seitiges QR-/Ticket-Onboarding (Issue 17, Schritt 5b)

Geprüft: Umsetzung gegen `plan.md` und `requirement.md` (2026-09-24).
Verbindlicher Server-Vertrag: `acceptance-schritt-5a.md`
(`POST api/pairing/bootstrap`, `api/auth/refresh`, `api/auth/logout`,
ECDH P-256 + AES-256-GCM, Payload `nonce‖ciphertext‖tag`).

## Abdeckung der Akzeptanzkriterien

| Kriterium | Umsetzung | Status |
|-----------|-----------|--------|
| QR-/Ticket-Onboarding auf der Einrichtungsseite | `BootstrapSection` in `ServerSetupPage.xaml` (Z. 274–321): `QrScanButton` (mobil), `BootstrapTicketEntry`, `BootstrapButton`, `BootstrapIndicator`/`BootstrapRunningLabel`, `BootstrapErrorLabel` — alle mit AutomationIds | erfüllt |
| Ticket-URL-Parsing (`…/pairing?t=…`) | `VideoPlayer.Mvu/BootstrapTicketParser.cs`: http/https, letztes Segment `pairing` (case-insensitiv, Trailing-Slash), `t`-Parameter dekodiert opak, Basisadresse über `ServerAddress.TryNormalize` | erfüllt |
| Kurzcode gegen verbundenen Server | `Kind=ShortCode` → `ExchangeBootstrapTicketEffect` auf `ActiveServerAddress`; ohne Server → `BootstrapNeedsServerError` | erfüllt |
| Erreichbarkeitsprüfung vor Server-Übernahme | `CheckBootstrapServerEffect` → `healthProbe.CheckAsync` (Gate-Ablehnung zählt als erreichbar); erst danach `PersistActiveServerEffect` + `ExchangeBootstrapTicketEffect` | erfüllt |
| Bootstrap-Exchange anonym + verschlüsselt | `DevicePairingClient.BootstrapAsync`: kein `X-API-Key`/Bearer, kein Retry, `ApiException` mit Status; ECDH/AES-GCM wie `exchange` | erfüllt |
| Sichere Persistenz (Geräte-Token, JWT, Refresh-Token) | `ShellAppEffectHandler.ExchangeBootstrapTicketAsync`: `SetPairedTokenIfCurrentAsync` + `SetAuthTokenAsync` + `SetRefreshTokenAsync` nur bei aktuellem Kontext; `AppSecretsService` SecretStore | erfüllt |
| Direkter Einstieg Home ohne Anmeldemaske | `BootstrapCompleted(Succeeded)` → `AttemptAutoLoginEffect` → Token-Validierung → `Connected`/`Home`; E2E `TicketUrl_BootstrapsDirectlyToHome` belegt `LoginCount==0` | erfüllt |
| Refresh-Token-Rotation | `SilentReloginAsync` → `TryRefreshSessionAsync` vor Credentials; rotiertes Paar atomar persistiert; 401→Gate-Probe→`ClearRefreshTokenAsync`+Fallback | erfüllt |
| Logout widerruft Refresh-Token | `AuthSessionService.LogoutAsync` → `RevokeRefreshTokenAsync` (`api/auth/logout`, best-effort) → `ClearCredentialsAsync` inkl. Refresh-Token | erfüllt |
| Manuelle Fallbacks bleiben | Adresseingabe, Admin-Code-Pairing (`PairingSection`) und Credential-Login unverändert; E2E-Suite 17/17 grün | erfüllt |
| Stale-Result-Schutz | `IsBootstrapRunning`-Guards im Update + `SetPairedTokenIfCurrentAsync` + Adressvergleich im Handler; veraltete Session wird verworfen | erfüllt |
| Mobile QR-Scan, Windows manuell | `QrScannerPage` komplett `#if ANDROID \|\| IOS`; `QrScanButton.IsVisible` nur mobil; ZXing-Paket target-konditioniert; Kamera-Permissions beider Manifeste | erfüllt |

## Planpunkte im Einzelnen

- **§1 Parser:** umgesetzt wie geplant — URL-Modus mit
  `pairing`-Endsegment und opak dekodiertem `t`; Kurzcode-Modus ohne
  Whitespace/`:`; kein `PairingCode.TryNormalize` auf dem Ticket.
- **§2 ApiClient:** alle sechs Modelle vorhanden
  (`PairingBootstrapRequest/-Response`, `PairingBootstrapPayload`,
  `BootstrapResult`, `RefreshTokenRequest`, `RefreshSessionResult`);
  `BootstrapAsync` teilt die `PostJsonAsync`-Bauweise mit
  `ExchangeAsync`; `RefreshSessionAsync`/`LogoutSessionAsync` mit
  `allowReauth: false`.
- **§3 Storage:** `RefreshTokenKey` (profilbezogen, SecretStore) +
  Get/Set/Clear; `ClearCredentialsAsync` räumt den Refresh-Token.
- **§4 AuthSessionService:** Refresh-first in `SilentReloginAsync`;
  Gate-Probe auf 401/403 trennt Gerätewiderruf von ungültigem
  Refresh-Token; transient → Credential-Fallback. `LogoutAsync` ruft
  `RevokeRefreshTokenAsync` mit kurzlebigem Client (Gate-Header per
  Konstruktor). `LoginCoreAsync` unverändert.
- **§5 MVU:** `BootstrapStatus`, `IsBootstrapRunning`,
  `BootstrapError`, drei Messages + zwei Effects, alle Übergänge und
  Fehlerkonstanten umgesetzt; Reset in den Servercheck- und
  Widerrufs-Übergängen.
- **§6 Effekt-Handler:** `CheckBootstrapServerAsync` (jede Antwort =
  erreichbar) und `ExchangeBootstrapTicketAsync` (Stale-Guard,
  Kontext-Persistenz, Status-Mapping) wie geplant.
- **§7 UI:** Karte zwischen Verbinden- und Pairing-Karte;
  `OnQrScanClicked` mit `BarcodeScanning.IsSupported` +
  `Permissions.Camera` + `DisplayAlert` (geplante UI-lokale Ausnahme);
  `QrScannerPage` code-only mit `CameraBarcodeReaderView`
  (`TwoDimensional`, `AutoRotate`, `Multiple=false`), Hint + Abbrechen.
- **§8 Dev-API:** `IsBootstrapRunning`/`BootstrapError` im Snapshot
  und im E2E-DTO; keine Secret-Felder (Contract-Test pinnt das).
- **§9 Tests:** `BootstrapTicketParserTests` (15), `DevicePairingClient-
  BootstrapTests` (10), `AppUpdateTests_Bootstrap` (11),
  `AuthSessionServiceRefreshTests` (6), `AppSecretsServiceTests` (+5),
  `VideoPlayerApiClientTests_SessionRefresh` (6), `BootstrapContractTests`
  (9). Mock-Server: Bootstrap/Refresh/Logout + Zähler;
  `MockPairingCrypto.EncryptPayload` verallgemeinert.
  E2E `BootstrapTests` (6): Ticket-URL→Home, Kurzcode→Home,
  ungültiges Ticket, unerreichbarer Server, Formatfehler lokal,
  Neustart-Persistenz — Plan-Pflicht (a/b/c) erfüllt und erweitert.
- **§10 Doku/Verifikation:** Screenshots via Harness in
  `live-verification/`; Rest folgt in Schritt 12.

## Abweichungen vom Plan

- `BootstrapButton`-Text ist „Kopplungscode einlösen" (Plan: „Verbinden") —
  präziser, weil der Button kein Netzwerk-„Verbinden" im bisherigen Sinn
  auslöst; Karten-Überschrift „Mit Kopplungscode verbinden".
- Zusatz über den Plan: `AppInstance.StartAsync(profile:)` +
  `Dispose(deleteProfile:)` für den Neustart-Persistenz-Test —
  Support-Erweiterung ohne Verhaltensänderung bestehender Tests.
- Zusatz-Tests `TokenRefresh_RefreshRejectedByGate_DoesNotFallback`
  dokumentiert das reale Verhalten (Gate-401 → Credential-Versuch
  scheitert ebenfalls am Gate, kein `SessionExpired`).

**Status: Vollständig umgesetzt.**
