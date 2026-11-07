# Inventory – MVU, Auth, Storage, Effekt-Handler

## `VideoPlayer.Mvu/AppUpdate.cs` (2339 Zeilen)

Pairing-relevante Zustände (AppModel): `IsPairingRequired`, `IsPairingRunning`, `PairingError`, `ServerSetupError`, `IsServerCheckRunning`, `ActiveServerAddress`, `KnownServers`, `ServerHealth`.

- `UpdatePairingCodeSubmitted` (Z. 479): Doppel-Submit-Guard (`IsPairingRunning`) → `PairingCode.TryNormalize` (lokal, Formatfehler ohne Server) → `ExchangePairingCodeEffect(ActiveServerAddress, code)`; ohne aktiven Server → `GoToServerSetup`.
- `UpdatePairingCompleted` (Z. 509): Stale-Guard `!IsPairingRunning → Unchanged`; `Succeeded` → `Connecting` + `AttemptAutoLoginEffect`; Fehler → `PairingErrorFor(status)` (InvalidCode/RateLimited/NotSupported/ServerUnreachable/Failed → Konstanten Z. 35–48).
- `UpdateServerCheckCompleted` (Z. 309): Erfolg → `KnownServers` vorne einfügen + `PersistActiveServerEffect`; `PairingRequired` → `NeedsServerSetup`/ServerSetup-Page; sonst `Connecting` + `[PersistActiveServer, AttemptAutoLogin]` (Effekte laufen **sequenziell**).
- `UpdateAutoLoginCompleted` (Z. 591): `Succeeded` → `EnterConnected`; `GateRejected` → `UpdateDeviceTokenRevoked`; sonst Login-Maske.
- `BeginServerCheck`/`UpdateHealthCheckCompleted` räumen den Pairing-Kontext (`IsPairingRequired=false`, `PairingError=null`) — Bootstrap-Felder müssen dort ebenfalls zurückgesetzt werden.
- Fehler-Konstanten für Bootstrap neu: ungültige Eingabe (Parser), „Kurzcode braucht aktiven Server", ungültig/abgelaufen (400+401), 429, 404, nicht erreichbar, sonstiger Fehler.

## Messages/Effects (`AppMessage.cs`, `AppEffect.cs`)

- Bestehend: `PairingCodeSubmittedMessage`, `PairingCompletedMessage(PairingStatus)`; `CheckCandidateServerEffect`, `PersistActiveServerEffect`, `ExchangePairingCodeEffect`, `AttemptAutoLoginEffect`.
- Neu: `BootstrapTicketSubmittedMessage(string RawInput)`, `BootstrapServerCheckedMessage(string Address, string? DisplayName, bool Success, string Ticket)` (oder Ticket im laufenden Kontext), `BootstrapCompletedMessage(BootstrapStatus)`; Effekte `CheckBootstrapServerEffect(string Address, string Ticket)`, `ExchangeBootstrapTicketEffect(string ServerAddress, string Ticket)`. `BootstrapStatus` als eigenes Enum (semantisch eigen: `InvalidTicket` für 400/401, `RateLimited`, `NotSupported`, `ServerUnreachable`, `Failed`, `Succeeded`).

## `VideoPlayer.Mvu/AuthSessionService.cs` (412 Zeilen)

- `TryStoredLoginAsync`: gespeicherter Token → `TryStoredTokenAsync` (Validierung via `api/favorites`, TokenRefresher läuft im Call) → Credentials-Fallback (`LoginCoreAsync`) → `MissingCredentials`.
- `SilentReloginAsync` (Z. 303): Credentials-Pfad nur; **erweitern um Refresh-Token-Versuch zuerst**: `GetRefreshTokenAsync` → `RefreshSessionAsync` → `SetAuthTokenAsync` + `SetRefreshTokenAsync` (Rotation) → `AuthToken` zurück. Bei 401/403: `HandleGateRejectionAsync` → Gate → `null`; sonst Refresh-Token löschen → Credentials-Pfad weiter (kein `SessionExpired`, wenn Credentials noch greifen).
- `LogoutAsync` (Z. 159): aktuell nur lokales Räumen + Kommentar „kein Logout-Endpunkt". **Ändern**: vor `ClearCredentialsAsync` best-effort `POST api/auth/logout` mit gespeichertem Refresh-Token (kurzlebiger Client, Gate-Header automatisch); Fehler schlucken (Debug.WriteLine). `ClearCredentialsAsync` löscht Refresh-Token mit.
- `CreateClient` verdrahtet `TokenRefresher` → `SilentReloginAsync` — Refresh greift damit automatisch in jedem 401-Pfad (Token-Validierung und laufende Requests).
- Neu in `AuthSessionService`: `AdoptBootstrapSessionAsync` **nicht nötig** — Persistenz läuft im Effekt-Handler (`SetAuthTokenAsync`/`SetRefreshTokenAsync`), danach validiert `AttemptAutoLoginEffect` → `TryStoredTokenAsync` wie im Pairing-Fluss (gleicher Codepfad, kein Sonderweg).

## `VideoPlayer.Storage/AppSecretsService.cs` (176 Zeilen)

- Profilpräfix via `profile.PrefixKey`; Secrets nur in `ISecretStore` (SecureStorage), Legacy-Migration aus `settings`.
- `AuthTokenKey` profilbezogen (nicht serverbezogen) — **Refresh-Token gleich**: `RefreshTokenKey = "RefreshToken"` + `GetRefreshTokenAsync`/`SetRefreshTokenAsync`/`ClearRefreshTokenAsync`. `ClearCredentialsAsync` (Z. 72) erweitern um Refresh-Token-Löschung (Session-Einheit).

## `VideoPlayer.Maui/Services/ShellAppEffectHandler.cs` (1376 Zeilen)

- `ExchangePairingCodeAsync` (Z. 1213): Exchange → Stale-Guard (`IsPairingRunning` + Adressvergleich) → `SetPairedTokenIfCurrentAsync` + `Client?.SetApiKey` → `PairingCompletedMessage`; `ApiException.StatusCode`-Mapping → `PairingStatus`.
- `PersistActiveServerAsync` (Z. 1114): `KnownServersService.SetActiveServerAsync` + `deviceTokens.LoadForServerAsync` — läuft vor dem Bootstrap-Effekt → Gate-Provider ist bereit, wenn `SetPairedTokenIfCurrentAsync` greift.
- `CheckCandidateServerAsync` (Z. 1067): `healthProbe.CheckAsync` + `IsPairingRequiredAsync` — für Bootstrap-URL-Check wiederverwenden (nur Erreichbarkeit nötig: `result != Unreachable`).
- `LogoutAsync` (Z. 742) ruft `authSession.LogoutAsync`.
- `GetDeviceName()` (Z. 1296) wiederverwendbar für `deviceName` im Bootstrap-Request.
- Bootstrap-Handler `ExchangeBootstrapTicketAsync`: Exchange → Stale-Guard analog (`IsBootstrapRunning` + Adresse) → `SetPairedTokenIfCurrentAsync` + `SetApiKey` → `secrets.SetAuthTokenAsync` + `SetRefreshTokenAsync` → `BootstrapCompletedMessage`; Status-Mapping analog.
