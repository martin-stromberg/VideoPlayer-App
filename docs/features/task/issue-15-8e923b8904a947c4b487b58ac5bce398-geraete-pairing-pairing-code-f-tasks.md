# Tasks: Geräte-Pairing per Einmal-Code und gerätegebundenes Token

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | ApiClient | `PairingExchangeRequest`-DTO anlegen (`code`, `clientPublicKey`, `deviceName`, camelCase via `JsonPropertyName`) | Offen | - |
| 2 | ApiClient | `PairingExchangeResponse`-DTO anlegen (`serverPublicKey`, `encryptedToken`, camelCase) | Offen | - |
| 3 | ApiClient | `PairingCrypto` implementieren (flüchtiges ECDH-nistP256-Paar, SPKI/DER-Base64-Export, `DeriveKeyFromHmac`-Ableitung, AES-256-GCM-Entschlüsselung `nonce(12)‖ct‖tag(16)`) | Offen | - |
| 4 | ApiClient | `DevicePairingClient` implementieren (`ExchangeAsync` → `POST api/pairing/exchange` über eigenen `HttpClient` ohne `X-API-Key`/Bearer, ohne Retry/Reauth) | Offen | - |
| 5 | ApiClient | `ClientGate.DeviceTokenProvider` (statischer `Func<string?>?`) einführen und in `Resolve()` mit höchster Priorität auswerten | Offen | - |
| 6 | ApiClient | `ClientGate.ResolveStatic()` und interne Überladung `Resolve(deviceToken, env, meta, file)` ergänzen; Diagnosezeile um Geräte-Token-Quelle erweitern | Offen | - |
| 7 | ApiClient | `HealthCheckResult`-Enum anlegen (`Reachable`, `GateRejected`, `Unreachable`) | Offen | - |
| 8 | ApiClient | `VideoPlayerApiClient.CheckHealthAsync`: Rückgabetyp `Task<bool>` → `Task<HealthCheckResult>` (Erfolg → `Reachable`, `ApiException` 401 → `GateRejected`, sonstige `ApiException` → `Unreachable`) | Offen | - |
| 9 | ApiClient | `VideoPlayerApiClient.SetApiKey(string)` ergänzen (nachträgliches Aktualisieren des `X-API-Key`-Headers, Guard gegen `null`/leer) | Offen | - |
| 10 | Storage | `AppSecretsService`: `DeviceTokenKey`-Konstante und `Get/Set/Clear/HasDeviceTokenAsync(serverAddress)` mit server-bezogenem, profil-präfixiertem Schlüssel (nur `ISecretStore`) | Offen | - |
| 11 | MVU | `PairingStatus`-Enum anlegen (`Succeeded`, `InvalidCode`, `RateLimited`, `InvalidRequest`, `NotSupported`, `ServerUnreachable`, `Failed`) | Offen | - |
| 12 | MVU | `PairingCode`-Helper mit `TryNormalize` (Trim, Großschreibung, Länge 8, Alphabet `ABCDEFGHJKMNPQRSTUVWXYZ23456789`) anlegen | Offen | - |
| 13 | MVU | `DeviceTokenService` implementieren (`LoadForServerAsync`/`SetPairedTokenAsync`/`DiscardCurrentAsync`/`DiscardIfCurrentAsync` via `AppSecretsService`, `ClientGate.DeviceTokenProvider`-Pflege mit `CurrentServerAddress`-Tracking, `IsGateRejectedAsync`-Probe über injizierbare `HttpClient`-Fabrik, `NotifyRejected` → `DeviceTokenRejected`-Ereignis) | Offen | - |
| 14 | MVU | `AppModel` um `IsPairingRunning`, `IsPairingRequired`, `PairingError` erweitern | Offen | - |
| 15 | MVU | `AppMessage`: `PairingCodeSubmittedMessage`, `PairingCompletedMessage`, `DeviceTokenRevokedMessage` anlegen | Offen | - |
| 16 | MVU | `ServerCheckCompletedMessage`, `ServerHealthCheckCompletedMessage`, `ServersLoadedMessage` um `PairingRequired`-Feld erweitern | Offen | - |
| 17 | MVU | `AppEffect`: `ExchangePairingCodeEffect(ServerAddress, Code)` anlegen | Offen | - |
| 18 | MVU | `AppUpdate`: Pairing-Übergänge implementieren (`UpdatePairingCodeSubmitted`, `UpdatePairingCompleted` inkl. Stale-Result-Guard `!IsPairingRunning → Unchanged`, `UpdateDeviceTokenRevoked` inkl. `IsStartupRunning = false`/`ServerHealth = Reachable`, `PairingRequired`-Verzweigungen, Fehlerkonstanten) | Offen | - |
| 19 | MVU | `AppUpdate`: Reset-Semantik der Pairing-Felder festlegen (`BeginServerCheck` und `UpdateServerCheckCompleted`-Misserfolg leeren `PairingError`/`IsPairingRequired`/`IsPairingRunning`; Erfolg übernimmt `PairingRequired`; `UpdateHealthCheckCompleted` analog) | Offen | - |
| 20 | MVU | `AuthSessionService.SilentReloginAsync`: bei endgültigem 401 `DeviceTokenService.IsGateRejectedAsync` prüfen und ggf. `DiscardCurrentAsync` + `NotifyRejected` statt `SessionExpired` auslösen | Offen | - |
| 21 | Maui-UI | `ServerSetupPage.xaml`: Pairing-Abschnitt (`PairingCodeEntry`, `PairButton`, `PairingIndicator`, `PairingErrorLabel`, AutomationIds, Sichtbarkeit an `IsPairingRequired`) ergänzen | Offen | - |
| 22 | Maui-UI | `ServerSetupPage.xaml.cs`: `OnPairClicked` (Dispatch `PairingCodeSubmittedMessage`) und `UpdateUi`-Bindings für die neuen Modelfelder | Offen | - |
| 23 | Maui-Dienste | `IServerHealthProbe.CheckAsync` (beide Überladungen) → `Task<HealthCheckResult>`; `ServerHealthProbe` reicht das Ergebnis durch (Catch-All → `Unreachable`) | Offen | - |
| 24 | Maui-Dienste | `ShellAppEffectHandler`: `ExchangePairingCodeEffect`-Zweig (`DeviceInfo`-Gerätename, `DevicePairingClient`, Token-Persistenz, `PairingCompletedMessage`-Status-Mapping) | Offen | - |
| 25 | Maui-Dienste | `ShellAppEffectHandler`: `PairingRequired` in `CheckCandidateServerAsync`, `RunHealthCheckAsync`, `LoadServersAsync` ermitteln und melden; Provider in `LoadServersAsync`/`PersistActiveServerAsync` setzen | Offen | - |
| 26 | Maui-Dienste | `ShellAppEffectHandler`: `GateRejected`-Verzweigungen — `RunHealthCheckAsync` (`DiscardCurrentAsync` + einmalige Re-Probe + `NotifyRejected` bei erneutem `GateRejected`) und `CheckCandidateServerAsync` (`GateRejected` → `Success = true` + `DiscardIfCurrentAsync`) | Offen | - |
| 27 | Maui-Dienste | `ShellAppEffectHandler`: `DeviceTokenRejected`-Event abonnieren → `DeviceTokenRevokedMessage` dispatchen | Offen | - |
| 28 | Maui-Dienste | `MauiProgram`: `DevicePairingClient` und `DeviceTokenService` registrieren; neue Abhängigkeiten von `AuthSessionService`/`ShellAppEffectHandler` verdrahten | Offen | - |
| 29 | Deployment | `scripts/iOS-Deployment.ps1`: Parameter `-VideoPlayerApiToken` (Default Env `VIDEOWEBPLAYER_MAUI_API_TOKEN`) und `Add-PropertyLine`/`Add-MaskedDisplay`-Durchreichung in allen Build-/Publish-Response-Dateien | Offen | - |
| 30 | Dokumentation | Zwischenlösung dokumentieren (statischer Token via `-p:VideoPlayerApiToken` / `iOS-Deployment.ps1 -VideoPlayerApiToken` als Publish-/Legacy-Zwischenlösung); `docs/help/api-client/*` an `HealthCheckResult`-Rückgabetyp anpassen | Offen | - |
| 31 | Tests | `ClientGateTests`: Geräte-Token-Priorität, Platzhalter-Überspringen, `ResolveStatic`, Provider-Reset-Isolation | Offen | - |
| 32 | Tests | `VideoPlayerApiClientTests_Authentication`: `CheckHealthAsync`-Result-Tests (401 → `GateRejected`, 404/500 → `Unreachable`), `SetApiKey`-Tests (Header-Ersetzung an Folge-Requests, Guard `null`/leer) | Offen | - |
| 33 | Tests | `DevicePairingClientTests` (neu): Request ohne Gate-/Bearer-Header, camelCase-Payload, Fehlercode-Mapping 400/401/404/429, kein Retry bei 429 | Offen | - |
| 34 | Tests | `PairingCryptoTests` (neu): ECDH/AES-GCM-Roundtrip, SPKI-Import, Nonce/Tag-Layout, Fehlerfälle | Offen | - |
| 35 | Tests | `AppSecretsServiceTests`: Geräte-Token nur in `ISecretStore`, Server-/Profil-Isolation, Clear | Offen | - |
| 36 | Tests | `DeviceTokenServiceTests` (neu): Provider-Setzung/-Rücksetzung, `DiscardIfCurrent`-Adressvergleich, `NotifyRejected`-Ereignis, `IsGateRejectedAsync`-Probe | Offen | - |
| 37 | Tests | `AppUpdateTests_*`: Pairing-Message-Flow (Erfolg, `InvalidCode`, `RateLimited`, `NotSupported` → `PairingNotSupportedError` ohne Fallback, `DeviceTokenRevoked`-Navigation inkl. Start-Pfad `IsStartupRunning = true`), `PairingRequired`-Verzweigungen, Reset-Semantik (`BeginServerCheck`/Check-Misserfolg), Stale-Result-Guard | Offen | - |
| 38 | Tests | `AuthSessionServiceTests`: 401 mit Gate-Probe → `DeviceTokenRejected` statt `SessionExpired` (Testdouble für `DeviceTokenService`) | Offen | - |
| 39 | Tests | `EnumCoverageTests`/`enum-coverage-check`: alle `PairingStatus`- und `HealthCheckResult`-Werte in Testdateien abdecken | Offen | - |
| 40 | Tests | `ServerSetupContractTests`/`ConnectionWorkflowContractTests`/`VideoPlayerMauiContractTests`: Pairing-AutomationIds, Dispatches, Effekt-Zweig, `GateRejected`-Zweige in `RunHealthCheckAsync`/`CheckCandidateServerAsync`, DI-Registrierungen, `iOS-Deployment.ps1`-Vertrag | Offen | - |
| 41 | Tests | Bestehende Tests an geänderte Signaturen anpassen (`ServerCheckCompletedMessage` in `AppUpdateTests_ServerManagement` + `AppUpdateTests_Connection` Z. ~433/465, `ServerHealthCheckCompletedMessage`/`ServersLoadedMessage` in `AppUpdateTests_Startup`, `AppUpdateTests_Offline`, `MvuHostTests`; `CheckHealthAsync`-Bool-Assertions) | Offen | - |
| 42 | Tests | Manueller Pairing-Lauf gegen Testserver auf echtem Ziel (iOS/Android/Windows) und Ergebnis protokollieren | Offen | - |
