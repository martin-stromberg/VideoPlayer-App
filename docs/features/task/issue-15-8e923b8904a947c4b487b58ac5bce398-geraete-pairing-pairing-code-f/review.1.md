# Plan-Review

## Ergebnis

**Status:** Offene Aufgaben vorhanden

## Umgesetzte Planelemente

### Neue Klassen / Objekte

- [x] `PairingExchangeRequest` (Datenmodellklasse, `VideoPlayer.ApiClient/Models/PairingExchangeRequest.cs`) — `code`, `clientPublicKey`, `deviceName` mit `JsonPropertyName`-camelCase-Attributen, `deviceName` wird bei `null` weggelassen
- [x] `PairingExchangeResponse` (Datenmodellklasse, `VideoPlayer.ApiClient/Models/PairingExchangeResponse.cs`) — `serverPublicKey`, `encryptedToken` (camelCase)
- [x] `PairingCrypto` (interne statische Klasse, `VideoPlayer.ApiClient/PairingCrypto.cs`) — flüchtiges ECDH-nistP256-Paar, `ExportSubjectPublicKeyInfo`→Base64, `DeriveKeyFromHmac` (SHA256), AES-256-GCM-Entschlüsselung `nonce(12)‖ct‖tag(16)`; testbar über vorhandenes `InternalsVisibleTo` im csproj
- [x] `DevicePairingClient` (Klasse, `VideoPlayer.ApiClient/DevicePairingClient.cs`) — `ExchangeAsync(Uri, string, string?, CancellationToken)` mit injizierbarem `HttpClient`, `POST api/pairing/exchange` ohne Gate-/Bearer-Header, ohne Retry/Reauth, wirft `ApiException` (Status bzw. `null` bei Transportfehler)
- [x] `HealthCheckResult` (Enum, `VideoPlayer.ApiClient/HealthCheckResult.cs`) — `Reachable`, `GateRejected`, `Unreachable`
- [x] `PairingStatus` (Enum, `VideoPlayer.Mvu/PairingStatus.cs`) — `Succeeded`, `InvalidCode`, `RateLimited`, `InvalidRequest`, `NotSupported`, `ServerUnreachable`, `Failed`
- [x] `PairingCode` (statische Klasse, `VideoPlayer.Mvu/PairingCode.cs`) — `TryNormalize` mit Trim/Großschreibung, Länge 8, Alphabet `ABCDEFGHJKMNPQRSTUVWXYZ23456789`
- [x] `DeviceTokenService` (Klasse, `VideoPlayer.Mvu/DeviceTokenService.cs`) — `LoadForServerAsync`, `SetPairedTokenAsync`, `DiscardCurrentAsync`, `DiscardIfCurrentAsync`, `CurrentServerAddress`-Tracking, `ClientGate.DeviceTokenProvider`-Pflege, `IsGateRejectedAsync` über injizierbare `HttpClient`-Fabrik, `NotifyRejected()` → `DeviceTokenRejected`-Ereignis

### Neue MVU-Nachrichten und Effekte

- [x] `PairingCodeSubmittedMessage(string Code)` (`AppMessage.cs`)
- [x] `PairingCompletedMessage(PairingStatus Status)` (`AppMessage.cs`)
- [x] `DeviceTokenRevokedMessage` (`AppMessage.cs`)
- [x] `ExchangePairingCodeEffect(string ServerAddress, string Code)` (`AppEffect.cs`)

### Änderungen an bestehenden Klassen

- [x] `ClientGate.DeviceTokenProvider` (`Func<string?>?`, statisch setzbar) — vorhanden (`ClientGate.cs`)
- [x] `ClientGate.Resolve()` — wertet `DeviceTokenProvider` mit höchster Priorität aus (Geräte-Token > Env > Metadata > Datei > Platzhalter); Diagnosezeile um `device=`-Quelle erweitert
- [x] `ClientGate.ResolveStatic()` (`public static`) — nur statische Quellen
- [x] `ClientGate.Resolve(string? deviceToken, string? environmentValue, string? metadataValue, string? fileValue)` — interne Überladung vorhanden
- [x] `VideoPlayerApiClient.SetApiKey(string apiKey)` — ersetzt `X-API-Key`-Header, Guard via `ArgumentException.ThrowIfNullOrWhiteSpace`
- [x] `VideoPlayerApiClient.CheckHealthAsync` — Rückgabetyp `Task<HealthCheckResult>`; Erfolg → `Reachable`, `ApiException` 401 → `GateRejected`, sonstige → `Unreachable`
- [x] `AppSecretsService.DeviceTokenKey` (`"DeviceToken"`) — Konstante vorhanden; effektiver Schlüssel `profile.PrefixKey("DeviceToken:" + normalisierte Adresse)`
- [x] `AppSecretsService.GetDeviceTokenAsync` / `SetDeviceTokenAsync` / `ClearDeviceTokenAsync` / `HasDeviceTokenAsync` — nur `ISecretStore`, server- und profil-bezogen
- [x] `AppModel.IsPairingRunning` / `IsPairingRequired` / `PairingError` — vorhanden
- [x] `ServerCheckCompletedMessage` / `ServerHealthCheckCompletedMessage` / `ServersLoadedMessage` — neues `PairingRequired`-Feld (`IsReachable` bleibt `bool`)
- [x] `AppUpdate.BeginServerCheck` — setzt `PairingError`/`IsPairingRequired`/`IsPairingRunning` zurück
- [x] `AppUpdate.UpdateServerCheckCompleted` — Erfolg + `PairingRequired` → Pairing-Eingabe (`PersistActiveServerEffect`, kein `AttemptAutoLoginEffect`); Erfolg ohne → bisheriger Pfad; Misserfolg → Pairing-Felder zurückgesetzt
- [x] `AppUpdate.UpdateHealthCheckCompleted` — erreichbar + `PairingRequired` → `NeedsServerSetup`/`ServerSetup` (`IsStartupRunning = false`, `ServerHealth = Reachable`); unerreichbar → Pairing-Felder zurückgesetzt
- [x] `AppUpdate.UpdateServersLoaded` — `IsPairingRequired = message.PairingRequired`; `PairingError` bleibt unberührt
- [x] `AppUpdate.UpdatePairingCodeSubmitted` — `PairingCode.TryNormalize`, Fehler → `InvalidPairingCodeError` ohne Effekt; gültig → `IsPairingRunning` + `ExchangePairingCodeEffect`; Guard ohne `ActiveServerAddress`
- [x] `AppUpdate.UpdatePairingCompleted` — Stale-Result-Guard (`!IsPairingRunning` → `Unchanged`); `Succeeded` → `AttemptAutoLoginEffect`; Fehler-Mapping inkl. `NotSupported` → `PairingNotSupportedError` ohne Fallback
- [x] `AppUpdate.UpdateDeviceTokenRevoked` — `NeedsServerSetup` + `ServerSetup`-Navigation + `DeviceRevokedError` + `IsPairingRequired` + Lauf-Flags/`ServerHealth = Reachable` + `DisconnectServerEventsEffect` + Reset von `SourcesOverview`/`SourceBrowser`/`HomeHero`; Idempotenz-Guard
- [x] `AppUpdate`-Fehlerkonstanten — `InvalidPairingCodeError`, `PairingRateLimitedError`, `PairingNotSupportedError`, `PairingFailedError`, `DeviceRevokedError`
- [x] `AuthSessionService` — neue Abhängigkeit `DeviceTokenService` (Konstruktor-Injection); `SilentReloginAsync` prüft bei 401/403 `IsGateRejectedAsync` → `DiscardCurrentAsync` + `NotifyRejected` statt `SessionExpired`
- [x] `IServerHealthProbe.CheckAsync` (beide Überladungen) → `Task<HealthCheckResult>`; `ServerHealthProbe` reicht durch, Catch-All → `Unreachable`
- [x] `ShellAppEffectHandler` — Event-Handler `DeviceTokenRejected` → `DeviceTokenRevokedMessage`; `ExchangePairingCodeEffect`-Zweig `ExchangePairingCodeAsync` (Gerätename via `DeviceInfo` mit Fallback + Trim 200, `DevicePairingClient.ExchangeAsync`, Erfolg → `SetDeviceTokenAsync` + `SetPairedTokenAsync` + `SetApiKey`, Status-Mapping 401→`InvalidCode`, 429→`RateLimited`, 400→`InvalidRequest`, 404→`NotSupported`, `null`→`ServerUnreachable`, sonst→`Failed`)
- [x] `ShellAppEffectHandler.RunHealthCheckAsync` — `HealthCheckResult`-Verzweigung; `GateRejected` → `DiscardCurrentAsync` + einmalige Re-Probe; erneut `GateRejected` → `NotifyRejected()`; `PairingRequired` nur bei `Reachable`
- [x] `ShellAppEffectHandler.CheckCandidateServerAsync` — `GateRejected` → `DiscardIfCurrentAsync` + `Success = true`; `PairingRequired`-Ermittlung und Meldung
- [x] `ShellAppEffectHandler.LoadServersAsync` — Geräte-Token laden, Provider setzen (`LoadForServerAsync`), `PairingRequired` in `ServersLoadedMessage`
- [x] `ShellAppEffectHandler.PersistActiveServerAsync` — Provider-Umstellung via `LoadForServerAsync` vor dem Auto-Login
- [x] `ShellAppEffectHandler` — neue Abhängigkeiten `DevicePairingClient`, `DeviceTokenService`, `AppSecretsService` per Konstruktor-Injection
- [x] `ServerSetupPage.xaml` — Pairing-Abschnitt `PairingSection` im Fußbereich mit `PairingCodeEntry` (AutomationId, `Keyboard.Text`, `MaxLength=8`), `PairButton` (`OnPairClicked`), `PairingIndicator`, `PairingErrorLabel` (`VpDanger`)
- [x] `ServerSetupPage.xaml.cs` — `UpdateUi` bindet Sichtbarkeit an `IsPairingRequired`, Indicator/Button-Enabled an `IsPairingRunning`, `PairingError`; `OnPairClicked` dispatcht `PairingCodeSubmittedMessage`
- [x] `MauiProgram` — `DevicePairingClient` (Singleton mit eigenem `HttpClient`) und `DeviceTokenService` (Singleton) registriert
- [x] `scripts/iOS-Deployment.ps1` — Parameter `[string]$VideoPlayerApiToken` (Default `$env:VIDEOWEBPLAYER_MAUI_API_TOKEN`), `Add-PropertyLine`/`Add-MaskedDisplay` in allen drei Response-Datei-Blöcken (`Invoke-Build`, `Invoke-Run`, `Invoke-DeviceViaSsh`), Maskierung in `Add-MaskedDisplay`

### Tests

- [x] `ClientGateTests`: `Resolve_PrefersDeviceTokenOverEnvironment`, `Resolve_FallsBack_WhenDeviceTokenProviderReturnsPlaceholderOrNull` (Theory), `Resolve_PublicOverload_ConsultsDeviceTokenProvider`, `ResolveStatic_IgnoresDeviceTokenProvider`, Provider-Reset (try/finally)
- [x] `VideoPlayerApiClientTests_Authentication`: `CheckHealthAsync_ReturnsReachableOnSuccess`, `CheckHealthAsync_ReturnsUnreachableOnFailure` (Theory 404/500), `CheckHealthAsync_ReturnsUnreachableOnTransportError`, `CheckHealthAsync_ReturnsGateRejectedOnUnauthorized`, `SetApiKey_UpdatesClientGateHeader`, `SetApiKey_RejectsNullOrEmpty` (Theory)
- [x] `DevicePairingClientTests` (neu): `ExchangeAsync_SendsRequest_WithoutGateAndBearerHeaders`, `ExchangeAsync_SendsCamelCasePayload`, `ExchangeAsync_OmitsDeviceName_WhenNull`, `ExchangeAsync_DecryptsToken_Roundtrip`, `ExchangeAsync_MapsErrorStatus_WithoutRetry` (Theory 400/401/404/429/500, `Assert.Single(Requests)` = kein Retry), `ExchangeAsync_TransportError_MapsToApiExceptionWithoutStatus`, `ExchangeAsync_RequiresCode`
- [x] `PairingCryptoTests` (neu): `DecryptToken_Roundtrip_RecoversPlaintextToken`, `ExportPublicKeyBase64_ProducesImportableSpki`, `DecryptToken_InvalidBase64_Throws`, `DecryptToken_TooShortPayload_Throws`, `DecryptToken_TamperedPayload_Throws`, `DecryptToken_WrongServerKey_Throws`
- [x] `AppSecretsServiceTests`: `DeviceToken_GoesToSecretStore_Only`, `SetDeviceToken_IsolatedPerServer`, `DeviceToken_IsProfileIsolated`, `DeviceToken_NormalizesServerAddress`, `ClearDeviceToken_RemovesOnlyThatServer`, `GetDeviceToken_WithoutEntry_ReturnsNull`
- [x] `DeviceTokenServiceTests` (neu): `LoadForServer_SetsClientGateProvider`, `LoadForServer_WithoutToken_FallsBackToStatic`, `LoadForServer_NullAddress_UsesStaticResolution`, `SetPairedToken_StoresAndActivatesToken`, `DiscardCurrent_ResetsProviderAndClearsStore`, `DiscardIfCurrent_OnlyDiscardsMatchingServer`, `IsGateRejected_UnauthorizedProbe_ReturnsTrue`, `IsGateRejected_ReachableProbe_ReturnsFalse`, `IsGateRejected_TransportError_ReturnsFalse`, `NotifyRejected_RaisesDeviceTokenRejected`
- [x] `PairingCodeTests` (neu): Validierung/Normalisierung/Alphabet
- [x] `AppUpdateTests_ServerManagement`: `Update_ServerCheckCompleted_Success_PairingRequired_ShowsPairingInput`, `Update_ServerCheckCompleted_Failure_ResetsPairingFields`, `Update_AddressSubmitted_ResetsPairingFields` (Legacy-Flow ohne Pairing durch `Update_ServerCheckCompleted_Success_PersistsAndStartsAutoLogin` mit `PairingRequired: false` abgedeckt)
- [x] `AppUpdateTests_Startup`: `Update_ServersLoaded_SetsPairingFlags`, `Update_HealthCheckCompleted_Reachable_PairingRequired_GoesToSetup`, `Update_HealthCheckCompleted_Unreachable_ResetsPairingFields`, `Update_DeviceTokenRevoked_DuringStartup_ShowsPairingInputWithError`
- [x] `AppUpdateTests_Pairing` (neu): `Update_PairingCodeSubmitted_Invalid_SetsErrorWithoutEffects` (Theory), `Update_PairingCodeSubmitted_Valid_StartsExchange`, `Update_PairingCompleted_Succeeded_ContinuesAutoLogin`, `Update_PairingCompleted_MapsErrors` (Theory), `Update_PairingCompleted_NotSupported_ShowsError_WithoutFallback`, `Update_PairingCompleted_StaleResult_WhenNotRunning_IsIgnored`, `Update_DeviceTokenRevoked_NavigatesToSetupWithError`, `Update_DeviceTokenRevoked_OnSetupPage_SkipsNavigation`, `Update_DeviceTokenRevoked_AlreadyInPairingState_IsIgnored`
- [x] `AuthSessionServiceTests`: `TokenRefresh_GateRejected_RaisesDeviceTokenRejected_NotSessionExpired`, `TokenRefresh_GateProbeAccepted_RaisesSessionExpired`
- [x] `EnumCoverageTests`: `HealthCheckResult_AllValuesAreCovered`, `PairingStatus_AllValuesAreCovered`
- [x] `PairingContractTests` (neu, Quelltext-Verträge): `ServerSetupPage_ContainsPairingSection`, `ServerSetupPage_PairingControlsHaveAutomationIds`, `ServerSetupPage_PairButton_DispatchesPairingCodeSubmitted`, `EffectHandler_HandlesPairingEffects`, `EffectHandler_HealthCheck_HandlesGateRejected` (inkl. `DiscardCurrentAsync`/Re-Probe/`NotifyRejected` und `DiscardIfCurrentAsync`), `MauiProgram_RegistersPairingServices`, `DevicePairingClient_UsesPublicEndpoint_WithoutAuthHeaders`, `IosDeploymentScript_PropagatesAndMasksApiToken`, `ServerHealthProbe_ReturnsHealthCheckResult`
- [x] Bestehende Tests an neue Signaturen angepasst (`ServerCheckCompletedMessage`, `ServerHealthCheckCompletedMessage`, `ServersLoadedMessage`, `CheckHealthAsync`): `AppUpdateTests_ServerManagement`, `AppUpdateTests_Connection`, `AppUpdateTests_Startup`, `AppUpdateTests_Offline`, `MvuHostTests`, `VideoPlayerApiClientTests_Authentication`
- [x] Dokumentation: `docs/help/api-client/api.md`/`installation.md`/`troubleshooting.md`/`ablauf-technisch.md` an `HealthCheckResult`/Pairing angepasst, Zwischenlösung statischer Token dokumentiert (Skript-Kommentar + `installation.md`), `serververwaltung-erststart.md`/`verbindung-anmeldung.md` ergänzt

## Offene Aufgaben

- [ ] Manueller Pairing-Lauf gegen Testserver auf echtem Ziel (iOS/Android/Windows) und Ergebnis protokollieren — im Plan als manueller Nachweis vorgesehen (SecureStorage/DeviceInfo nur auf echten Zielen prüfbar); im Working Tree liegt kein Testprotokoll vor.

## Hinweise

- Der planmäßig als neue Tests benannte `Update_ServerCheckCompleted_Success_WithoutPairing_KeepsLegacyFlow` existiert nicht unter diesem Namen; das Szenario ist durch die bestehenden, auf `PairingRequired: false` umgestellten Tests `Update_ServerCheckCompleted_Success_PersistsAndStartsAutoLogin` (`AppUpdateTests_ServerManagement`) und `ServerCheckCompleted_Success_EmitsPersistAndAutoLogin` (`AppUpdateTests_Connection`) abgedeckt.
- Die im Plan den Dateien `ServerSetupContractTests`/`ConnectionWorkflowContractTests` zugeordneten Quelltext-Vertragsprüfungen wurden in der neuen Datei `PairingContractTests.cs` gebündelt — inhaltlich vollständig (AutomationIds, Dispatch, Effekt-Zweig, `GateRejected`-Zweige, DI, Deployment-Skript).
- Der Testname `SilentRelogin_GateRejected_RaisesDeviceTokenRejected_NotSessionExpired` aus dem Plan ist als `TokenRefresh_GateRejected_RaisesDeviceTokenRejected_NotSessionExpired` umgesetzt (gleicher Prüfgegenstand: `SilentReloginAsync` läuft über den `TokenRefresher`).
- Kompilierbarkeit verifiziert: `dotnet build VideoWebPlayer.Maui.Tests -c Release` → 0 Fehler (nur vorhandene CS8618-/CS0108-Warnungen in `VideoWebPlayer.Client`).
- Kleinigkeit ohne Planbezug: `ExchangePairingCodeAsync` ruft `secrets.SetDeviceTokenAsync` und anschließend `deviceTokens.SetPairedTokenAsync` auf — letzteres schreibt denselben Schlüssel erneut (doppelte Persistenz, funktional harmlos).
