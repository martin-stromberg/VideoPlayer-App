# Inventory – Tests

## `VideoWebPlayer.Maui.Tests` (Baseline 927/927, net10.0, xUnit)

- `DevicePairingClientTests`: ECDH/AES-GCM-Test-Responder (Server-Schlüsselpaar im Test, `DeriveKeyFromHmac` + `AesGcm.Encrypt` — spiegelt `MockPairingCrypto`); prüft Request-JSON, Header-Freiheit, Entschlüsselung, HTTP-/Transportfehler → für `BootstrapAsync` analog erweitern (Payload-JSON verschlüsseln, Felder prüfen).
- `AppUpdateTests_Pairing`: MVU-Übergänge (Doppel-Submit, Formatfehler, Effekte, Stale-Guard, Fehler-Mapping) → neue `AppUpdateTests_Bootstrap` mit denselben Mustern.
- `AuthSessionServiceTests`: `StubHttpMessageHandler` (queued Responses + Request-Recording), in-memory `ISettingsStore`/`ISecretStore`-Fakes → Refresh-Rotation, Refresh-401→Credential-Fallback, Logout-Aufruf, Gate-vs-Refresh-401-Trennung testbar.
- `PairingContractTests`: Source-Contract-Pins (AutomationIds, `MaxLength`-Verbot, kein `ClientGate`/`Authorization` in `DevicePairingClient`, kein `SendWithPolicyAsync` dort) — Bootstrap-Pins ergänzen (neue AutomationIds, Ticket-Parser-Nutzung, `PairingCode.TryNormalize` darf im Bootstrap-Pfad nicht auf die QR-URL angewandt werden, Dev-API secret-frei).
- `DeviceTokenServiceTests`, `ServerAddressTests` vorhanden → `BootstrapTicketParserTests` daneben.

## `VideoPlayer.E2E.Tests` (Baseline 11/11, xUnit + FlaUI UIA3)

- `MockVideoServer`: TcpListener Loopback:0; Routen `GET /api/health`, `POST /api/pairing/exchange` (ECDH via `MockPairingCrypto`), `POST /api/auth/login` (Gate via `X-API-Key`==DeviceToken, problem+json bei Gate-Ablehnung), Listen-Endpunkte (Gate+Bearer).
- **Erweiterung**: `POST /api/pairing/bootstrap` (Ticket+Kurzcode-Alias → verschlüsseltes `{deviceToken, token, expires, refreshToken}`), `POST /api/auth/refresh` (Rotation: neuer Bearer + neuer Refresh-Token), `POST /api/auth/logout` (200). Diagnose-Zähler `BootstrapCount`, `RefreshCount`, `LogoutCount`.
- `E2ETestBase`: `StartAppAsync` (isoliertes Profil), `ConnectToPairingRequiredAsync`, `PairAsync`, `LoginAsync` — neue Helper für Ticket-Eingabe.
- `DevApiModels.DevApiModel` spiegelt den Snapshot → neue Felder `IsBootstrapRunning`/`BootstrapError` ergänzen.
- Neue `BootstrapTests`: (a) manueller Kurzcode-Flug auf Windows → Home/Connected (Pflicht); (b) Ticket-URL-Paste-Flug (vollständiger QR-Ersatz ohne Kamera) → Home; ggf. (c) Fehlerfall ungültiges Ticket.
- Screenshots: bestehender Harness (`live-verification/` der Schritte 3–4, `%TEMP%\shot-harness`) für die neuen UI-Zustände wiederverwenden.

## Contract-Pins, die respektiert werden müssen

- `PairingCodeEntry` ohne `MaxLength`, `IsSpellCheckEnabled/IsTextPredictionEnabled="False"` — gleiches für `BootstrapTicketEntry`.
- `DevicePairingClient` ohne `ClientGate.HeaderName`/`Authorization`/`SendWithPolicyAsync` — Bootstrap-Methode in dieser Klasse hält die Pin-Regeln (öffentlicher Endpunkt).
- `Assert.DoesNotContain("-stellig", xaml)` auf `ServerSetupPage.xaml` → Wortwahl „Kurzcode" statt „8-stelliger Code".
- Dev-API-Snapshot: keine Ticket-/Token-Felder.
