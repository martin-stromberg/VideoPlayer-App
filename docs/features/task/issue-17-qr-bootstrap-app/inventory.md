# Bestandsaufnahme – QR-/Ticket-Onboarding (Schritt 5b)

Stand: Branch `task/issue-17-qr-bootstrap-app` @ `53f84ee`. Baseline: **927/927** `VideoWebPlayer.Maui.Tests` (gemessen), **11/11** `VideoPlayer.E2E.Tests` (aus Schritt-4-Abnahme).

Detaildokumente in `inventory/`:

- `api-krypto.md` – `DevicePairingClient`, `PairingCrypto`, `ServerAddress`, `VideoPlayerApiClient`
- `mvu-auth.md` – `AppUpdate`/`AppMessage`/`AppEffect`, `AuthSessionService`, `AppSecretsService`, `ShellAppEffectHandler`
- `ui.md` – `ServerSetupPage`, Manifeste, `MauiProgram`, Dev-API-Snapshot
- `tests.md` – Testprojekte, Stubs, Mock-Server, Contract-Pins

## Kernaussagen

1. **Krypto/Transport existiert**: `PairingCrypto` (ECDH P-256 + AES-256-GCM, `nonce‖ct‖tag`) entschlüsselt bereits das `exchange`-Payload; `DecryptToken` liefert Klartext-String → für Bootstrap JSON-deserialisieren. `DevicePairingClient` sendet ohne Gate-/Bearer-Header an `api/pairing/exchange` → gleiche Bauweise für `api/pairing/bootstrap` erweitern.
2. **`ServerAddress.TryNormalize`** normalisiert `scheme://host[:port]/pfad/` – der Parser muss das `/pairing`-Segment **vor** der Normalisierung abschneiden (Unterpfad erhalten).
3. **MVU-Fluss existiert für Pairing**: `PairingCodeSubmittedMessage → ExchangePairingCodeEffect → PairingCompletedMessage → AttemptAutoLoginEffect → EnterConnected`. Bootstrap bekommt parallele Messages/Effekte (`BootstrapTicketSubmitted`, `CheckBootstrapServer`, `BootstrapServerChecked`, `ExchangeBootstrapTicket`, `BootstrapCompleted`), wobei `PersistActiveServerEffect` **vor** dem Exchange läuft, damit `SetPairedTokenIfCurrentAsync` den Gate aktiviert.
4. **AuthSessionService** besitzt `SilentReloginAsync` (Credential-Fallback via `api/auth/login`). Erweiterung: Refresh-Token-Versuch vor dem Credential-Pfad; `LogoutAsync` ruft `api/auth/logout`. Neue Secrets-Keys `RefreshToken` (profilbezogen, nicht serverbezogen – wie `AuthToken`).
5. **UI**: `ServerSetupPage` wurde in Schritt 4 modernisiert (Karten, `PairingSection` nur bei `IsPairingRequired`). Neue, **immer sichtbare** Ticket-Sektion nötig, weil die QR-URL die Serveradresse selbst mitbringt; Kurzcode braucht aktiven Server. Scan-Button per `#if ANDROID || IOS`.
6. **Dev-API**: `/model`-Snapshot bleibt secret-frei; neue Flags `IsBootstrapRunning`/`BootstrapError` sind unkritisch (Text/Status, keine Secrets).
7. **E2E**: `MockVideoServer` besitzt bereits ECDH/AES-GCM-Helfer (`MockPairingCrypto`) – Bootstrap/Refresh/Logout-Routen ergänzen; Windows-E2E testet den manuellen Kurzcode-/URL-Einstieg.
8. **Paket**: ZXing.Net.Maui.Controls **0.10.4** – MIT, veröffentlicht 14.08.2026 (>7 Tage), net10.0-Assets (android36.0/ios26.0), Windows ohne Kamera-Support → nur iOS/Android.

## Risiken

- `POST /api/auth/refresh` ist **gate-geschützt** (`ApiTokenCheck MauiOnly`): Refresh-Aufruf muss über einen Client mit Gate-Header laufen (`VideoPlayerApiClient` löst `ClientGate` automatisch auf). Ein 401 am Refresh kann Gate-Ablehnung ODER ungültiges Refresh-Token sein → `HandleGateRejectionAsync` prüft `api/auth/login`-Probe wie im bestehenden Pfad.
- Refresh-Token gehört zur Session → `ClearCredentialsAsync` löscht ihn mit (Logout/Kontowechsel konsistent).
- Scan-API nur unter `#if ANDROID || IOS`; PackageReference auf die mobilen TFMs konditionieren, damit der Windows-Build das Paket nicht braucht.
- Server-Branch #231 ist **nicht gemergt** → Live-Verifikation nur gegen lokal gestarteten Server mit Scratch-DB oder als manueller Restnachweis.
