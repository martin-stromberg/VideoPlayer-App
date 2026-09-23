# Plan-Review

## Ergebnis

**Status:** Offene Aufgaben vorhanden

Dritte Review-Runde. Geprüft wurde die uncommitted Implementierung im Working Tree (HEAD = Planungscommit `c6d6fc3`) gegen `plan.md` inklusive des Nachtrags „Korrektur der Gate-Probe" (Probe per `POST api/auth/login` mit `{}`-Body und `Content-Type`-Auswertung statt `api/health`). Die Befunde der vorherigen Reviews (`review.1.md`, `review-code.1.md`, `review-usability.1.md`) sind abgearbeitet. Verifikation: `dotnet test VideoWebPlayer.Maui.Tests -c Release` → **862/862 Tests bestanden, 0 Fehler**.

## Umgesetzte Planelemente

### Neue Klassen / Objekte

- [x] `PairingExchangeRequest` (Datenmodellklasse, `VideoPlayer.ApiClient/Models/PairingExchangeRequest.cs`) — `code`, `clientPublicKey`, `deviceName` mit `JsonPropertyName`-camelCase; `deviceName` wird bei `null` weggelassen (`JsonIgnoreCondition.WhenWritingNull`)
- [x] `PairingExchangeResponse` (Datenmodellklasse, `VideoPlayer.ApiClient/Models/PairingExchangeResponse.cs`) — `serverPublicKey`, `encryptedToken` (camelCase)
- [x] `PairingCrypto` (interne statische Klasse, `VideoPlayer.ApiClient/PairingCrypto.cs`) — flüchtiges ECDH-nistP256-Paar, `ExportSubjectPublicKeyInfo` → Base64, `DeriveKeyFromHmac` (SHA256), AES-256-GCM-Entschlüsselung `nonce(12)‖ct‖tag(16)`; `CryptographicException` bei Format-/Entschlüsselungsfehlern
- [x] `DevicePairingClient` (Klasse, `VideoPlayer.ApiClient/DevicePairingClient.cs`) — `ExchangeAsync(Uri, string, string?, CancellationToken)` mit injizierbarem `HttpClient`, `POST api/pairing/exchange` ohne Gate-/Bearer-Header, ohne Retry/Reauth, wirft `ApiException` (Status bzw. `null` bei Transportfehler)
- [x] `HealthCheckResult` (Enum, `VideoPlayer.ApiClient/HealthCheckResult.cs`) — `Reachable`, `GateRejected`, `Unreachable`
- [x] `PairingStatus` (Enum, `VideoPlayer.Mvu/PairingStatus.cs`) — `Succeeded`, `InvalidCode`, `RateLimited`, `InvalidRequest`, `NotSupported`, `ServerUnreachable`, `Failed`
- [x] `PairingCode` (statische Klasse, `VideoPlayer.Mvu/PairingCode.cs`) — `TryNormalize` mit Trim/Großschreibung, Länge 8, Alphabet `ABCDEFGHJKMNPQRSTUVWXYZ23456789`
- [x] `DeviceTokenService` (Klasse, `VideoPlayer.Mvu/DeviceTokenService.cs`) — `LoadForServerAsync`, `SetPairedTokenAsync`, `DiscardCurrentAsync`, `DiscardIfCurrentAsync`, `CurrentServerAddress`-Tracking, `ClientGate.DeviceTokenProvider`-Pflege, `NotifyRejected()` → `DeviceTokenRejected`-Ereignis; `IsGateRejectedAsync` gemäß **Nachtrag** korrigiert: `POST api/auth/login` mit `{}`-Body, nur `X-API-Key`-Header, Auswertung `401` + `application/problem+json` → `true`, `401` + `text/plain`/`400`/`429`/Transportfehler → `false`, angeforderter Abbruch wird durchgereicht

### Neue MVU-Nachrichten und Effekte

- [x] `PairingCodeSubmittedMessage(string Code)`, `PairingCompletedMessage(PairingStatus Status)`, `DeviceTokenRevokedMessage` (`AppMessage.cs`)
- [x] `ExchangePairingCodeEffect(string ServerAddress, string Code)` (`AppEffect.cs`)
- [x] `ServerCheckCompletedMessage` / `ServerHealthCheckCompletedMessage` / `ServersLoadedMessage` — neues Feld `PairingRequired` (`IsReachable` bleibt `bool`)

### Änderungen an bestehenden Klassen

- [x] `ClientGate.DeviceTokenProvider` (`Func<string?>?`, statisch setzbar) — vorhanden (`ClientGate.cs` Z. 58)
- [x] `ClientGate.Resolve()` — wertet `DeviceTokenProvider` mit höchster Priorität aus (Geräte-Token > Env > Metadata > Datei > Platzhalter); Diagnosezeile um `device=`-Quelle erweitert
- [x] `ClientGate.ResolveStatic()` (`public static`) — nur statische Quellen; interne Überladung `Resolve(string? deviceToken, string? environmentValue, string? metadataValue, string? fileValue)` vorhanden
- [x] `VideoPlayerApiClient.SetApiKey(string apiKey)` — ersetzt `X-API-Key`-Header, Guard via `ArgumentException.ThrowIfNullOrWhiteSpace`
- [x] `VideoPlayerApiClient.CheckHealthAsync` — Rückgabetyp `Task<HealthCheckResult>`; Erfolg → `Reachable`, `ApiException` 401 → `GateRejected`, sonstige → `Unreachable`
- [x] `AppSecretsService.DeviceTokenKey` + `Get/Set/Clear/HasDeviceTokenAsync(serverAddress)` — nur `ISecretStore`, Schlüssel `profile.PrefixKey("DeviceToken:" + normalisierte Adresse)`
- [x] `AppModel.IsPairingRunning` / `IsPairingRequired` / `PairingError` — vorhanden
- [x] `AppUpdate.BeginServerCheck` — setzt `PairingError`/`IsPairingRequired`/`IsPairingRunning` zurück
- [x] `AppUpdate.UpdateServerCheckCompleted` — Erfolg + `PairingRequired` → `IsPairingRequired = true`, `NeedsServerSetup`/`ServerSetup`, `PersistActiveServerEffect`, kein `AttemptAutoLoginEffect`; Erfolg ohne `PairingRequired` → bisheriger Pfad; Misserfolg → Pairing-Felder zurückgesetzt
- [x] `AppUpdate.UpdateHealthCheckCompleted` — erreichbar + `PairingRequired` → `NeedsServerSetup`/`ServerSetup` (`IsStartupRunning = false`, `ServerHealth = Reachable`); sonst `AttemptAutoLoginEffect`; unerreichbar → Pairing-Felder zurückgesetzt + `CheckOfflineDataEffect`
- [x] `AppUpdate.UpdateServersLoaded` — `IsPairingRequired = message.PairingRequired`; `PairingError`/`IsPairingRunning` bleiben unverändert
- [x] `AppUpdate.UpdatePairingCodeSubmitted` — `PairingCode.TryNormalize`-Fehler → `PairingError` ohne Effekt; gültig → `IsPairingRunning` + `ExchangePairingCodeEffect`; Guards gegen Doppel-Submit (`IsPairingRunning`) und fehlende `ActiveServerAddress`
- [x] `AppUpdate.UpdatePairingCompleted` — Stale-Result-Guard (`!IsPairingRunning` → `Unchanged`); `Succeeded` → `AttemptAutoLoginEffect`; Fehler-Mapping je Status; `NotSupported` → `PairingNotSupportedError`, `IsPairingRunning = false`, kein `AttemptAutoLoginEffect`-Fallback
- [x] `AppUpdate.UpdateDeviceTokenRevoked` — `NeedsServerSetup` + `ServerSetup`-Navigation + `DeviceRevokedError` + `IsPairingRequired` + Lauf-Flags/`ServerHealth = Reachable` + `DisconnectServerEventsEffect` + Reset von `SourcesOverview`/`SourceBrowser`/`HomeHero`; Idempotenz-Guard bei bereits aktivem Pairing-Zustand
- [x] `AppUpdate`-Fehlerkonstanten — `InvalidPairingCodeError`, `PairingRateLimitedError`, `PairingNotSupportedError`, `PairingFailedError`, `DeviceRevokedError` (zusätzlich `PairingCodeFormatError` für lokale Formatfehler, siehe Hinweise)
- [x] `AuthSessionService` — `DeviceTokenService` per Konstruktor-Injection; `SilentReloginAsync` prüft bei 401/403 `IsGateRejectedAsync` → `DiscardCurrentAsync` + `NotifyRejected` statt `SessionExpired`; dieselbe Trennung über `HandleGateRejectionAsync` auch in `TryStoredTokenAsync`/`LoginCoreAsync` (Ergebnis `AuthAttemptStatus.GateRejected`)
- [x] `IServerHealthProbe.CheckAsync` (beide Überladungen) → `Task<HealthCheckResult>`; `ServerHealthProbe` reicht durch, Catch-All → `Unreachable`; Gate-Schlüssel wird adressspezifisch aufgelöst (`ResolveGateKeyAsync`: Geräte-Token der geprüften Adresse, sonst `ResolveStatic()`)
- [x] `ShellAppEffectHandler` — `DeviceTokenRejected`-Abo → `DeviceTokenRevokedMessage` via `DispatchMessageAsync`/MainThread (`OnDeviceTokenRejected`)
- [x] `ShellAppEffectHandler.ExchangePairingCodeAsync` — `DeviceInfo`-Gerätename (`DeviceInfo.Name`, Fallback `Manufacturer`+`Model`, Trim 200), `DevicePairingClient.ExchangeAsync`, Erfolg → `SetPairedTokenAsync` + `authSession.Client?.SetApiKey` (nur bei aktuellem Pairing-Kontext; sonst reine Ablage via `SetDeviceTokenAsync`), Status-Mapping 401→`InvalidCode`, 429→`RateLimited`, 400→`InvalidRequest`, 404→`NotSupported`, `null`→`ServerUnreachable`, sonst→`Failed`
- [x] `ShellAppEffectHandler.RunHealthCheckAsync` — `GateRejected` → `DiscardCurrentAsync` + einmalige Re-Probe → `Reachable`/`Unreachable` normal gemeldet, erneut `GateRejected` → `NotifyRejected()`; `PairingRequired` nur bei `Reachable`
- [x] `ShellAppEffectHandler.CheckCandidateServerAsync` — `GateRejected` → `DiscardIfCurrentAsync(address)` + `Success = true`; danach `PairingRequired` ermittelt und in `ServerCheckCompletedMessage` gemeldet
- [x] `ShellAppEffectHandler.LoadServersAsync` — Geräte-Token des aktiven Servers laden (`LoadForServerAsync`), Provider setzen, `PairingRequired` in `ServersLoadedMessage`
- [x] `ShellAppEffectHandler.PersistActiveServerAsync` — Provider-Umstellung via `LoadForServerAsync` vor dem nachfolgenden Auto-Login-Effekt
- [x] `ShellAppEffectHandler` — neue Abhängigkeiten `DevicePairingClient`, `DeviceTokenService`, `AppSecretsService` per Konstruktor-Injection
- [x] `ServerSetupPage.xaml` — Pairing-Abschnitt `PairingSection` im Fußbereich mit `PairingCodeEntry` (AutomationId, `Keyboard.Text`, `MaxLength=8`), `PairButton` (`OnPairClicked`), `PairingIndicator`, `PairingErrorLabel` (`VpDanger`); Sichtbarkeit an `IsPairingRequired`
- [x] `ServerSetupPage.xaml.cs` — `UpdateUi` bindet Sichtbarkeit/Indicator/Button-Enabled/`PairingError`; `OnPairClicked` dispatcht `PairingCodeSubmittedMessage`
- [x] `MauiProgram` — `DevicePairingClient` (Singleton mit eigenem `HttpClient`) und `DeviceTokenService` (Singleton) registriert
- [x] `scripts/iOS-Deployment.ps1` — Parameter `[string]$VideoPlayerApiToken` (Default `$env:VIDEOWEBPLAYER_MAUI_API_TOKEN`), `Add-PropertyLine`/`Add-MaskedDisplay` in allen drei Response-Datei-Blöcken (`Invoke-Build`, `Invoke-Run`, `Invoke-DeviceViaSsh`), Maskierung in `Add-MaskedDisplay`

### Tests und Dokumentation

- [x] `ClientGateTests` — Geräte-Token-Priorität, Platzhalter-Überspringen, `ResolveStatic`, Provider-Reset-Isolation (`[Collection("ClientGate")]` via `Stubs/ClientGateCollection.cs`)
- [x] `VideoPlayerApiClientTests_Authentication` — `CheckHealthAsync`-Result-Tests (`Reachable`/`GateRejected`/`Unreachable` inkl. Transportfehler), `SetApiKey_UpdatesClientGateHeader`, `SetApiKey_RejectsNullOrEmpty`
- [x] `DevicePairingClientTests` (neu) — ohne Gate-/Bearer-Header, camelCase-Payload, Fehler-Mapping 400/401/404/429/500 ohne Retry, Transportfehler → `ApiException` ohne Status, Roundtrip-Entschlüsselung
- [x] `PairingCryptoTests` (neu) — Roundtrip, SPKI-Import, Base64-/Längen-/Manipulations-/Fremdschlüssel-Fehler
- [x] `AppSecretsServiceTests` — nur `ISecretStore`, Server-/Profil-Isolation, Adress-Normalisierung, Clear
- [x] `DeviceTokenServiceTests` (neu) — Provider-Setzung/-Rücksetzung, `DiscardIfCurrent`-Adressvergleich, `NotifyRejected`-Ereignis, `IsGateRejected_*` gemäß korrigierter Probe (401 problem+json → `true`; 401 text/plain, 400, 429, Transportfehler → `false`; Abbruch-Semantik)
- [x] `PairingCodeTests` (neu) — Validierung/Normalisierung/Alphabet
- [x] `AppUpdateTests_Pairing` (neu) — Submit-/Completed-Flow inkl. `NotSupported` ohne Fallback, Stale-Result-Guard, `DeviceTokenRevoked`-Navigation
- [x] `AppUpdateTests_ServerManagement` / `AppUpdateTests_Startup` / `AppUpdateTests_Connection` — `PairingRequired`-Verzweigungen, Reset-Semantik, `DeviceTokenRevoked` im Start-Pfad, `GateRejected`-Auto-/Login-Übergänge
- [x] `AuthSessionServiceTests` — `TokenRefresh_GateRejected_RaisesDeviceTokenRejected_NotSessionExpired`, `TokenRefresh_GateProbeAccepted_RaisesSessionExpired`, `GateRejected`-Pfade von `TryStoredLogin`/`LoginAsync`
- [x] `EnumCoverageTests` — `HealthCheckResult_AllValuesAreCovered`, `PairingStatus_AllValuesAreCovered`
- [x] `PairingContractTests` (neu, Quelltext-Verträge) — UI-Abschnitt/AutomationIds/Dispatch, Effekt-Zweig inkl. Stale-Guard, `GateRejected`-Zweige in `RunHealthCheckAsync`/`CheckCandidateServerAsync`, DI-Registrierungen, `iOS-Deployment.ps1`-Vertrag, `ServerHealthProbe`-Rückgabetyp und adressspezifische Schlüsselauflösung, Login-Endpunkt der Gate-Probe
- [x] Bestehende Tests an neue Signaturen angepasst (`AppUpdateTests_*`, `MvuHostTests`, `AppUpdateTests_Offline`)
- [x] Dokumentation — `docs/help/api-client/api.md`/`installation.md`/`troubleshooting.md`/`ablauf-technisch.md` an `HealthCheckResult`/Pairing/`SetApiKey` angepasst, Zwischenlösung statischer Token dokumentiert (Skript-Kommentar + `installation.md`), `serververwaltung-erststart.md`/`verbindung-anmeldung.md` ergänzt

## Offene Aufgaben

- [ ] Manueller Pairing-Lauf gegen Testserver auf echtem Ziel (Tasks-Datei Nr. 42) — **teilweise umgesetzt:** `live-verification.md` belegt den kryptografischen und API-vertraglichen Teil mit dem produktiven `DevicePairingClient`/`PairingCrypto` gegen den IIS-Testserver (Code → ECDH → AES-GCM → Token → Login 200, Negativfälle 401, `PairedDevices`-Einträge). Offen bleiben die gerätegebundenen Anteile `SecureStorage`-Persistenz, `DeviceInfo`-Gerätename und die UI-Bedienung auf einem echten Ziel (iOS/Android/Windows) — im Plan als manueller Nachweis vorgesehen, in dieser Umgebung nicht ausführbar.

## Hinweise

- Abweichungen/Erweiterungen gegenüber dem Plan sind sämtlich durch die vorherigen Review-Runden begründet und plangemäß konsistent:
  - Neuer Enum-Wert `AuthAttemptStatus.GateRejected` (`VideoPlayer.Mvu/AuthAttempt.cs`) samt `GateRejected`-Übergängen in `UpdateAutoLoginCompleted`/`UpdateLoginCompleted` — schließt die Auto-Login-/Login-Lücke des widerrufenen Geräte-Tokens (Code-Review-Befund).
  - `PairingCodeFormatError` für lokale Formatfehler statt `InvalidPairingCodeError` (Usability-Befund); `InvalidPairingCodeError` bleibt dem serverseitigen 401 vorbehalten.
  - `ServerHealthProbe` löst den Gate-Schlüssel adressspezifisch auf (`ResolveGateKeyAsync`) statt den globalen Provider zu nutzen — verhindert Credential-Weitergabe an fremde Kandidatenadressen (Code-Review-Befund).
  - Stale-Context-Guard der Seiteneffekte in `ExchangePairingCodeAsync` (`pairingCurrent`-Prüfung) und Doppel-Submit-Guard in `UpdatePairingCodeSubmitted` (Code-Review-Befunde).
  - Testisolation des statischen `ClientGate.DeviceTokenProvider` über die xUnit-Collection `ClientGate` (`Stubs/ClientGateCollection.cs`, angewendet in `ClientGateTests`, `DeviceTokenServiceTests`, `AuthSessionServiceTests`).
- `HealthCheckResult.GateRejected` über `CheckHealthAsync` (`api/health`) bleibt Defense-in-depth für Server mit gated Health-Endpunkt; auf dem Referenzserver ist `api/health` ungeschützt — der primäre Widerrufs-Detektor ist die korrigierte Login-Probe in `DeviceTokenService.IsGateRejectedAsync` (Plan-Nachtrag umgesetzt).
