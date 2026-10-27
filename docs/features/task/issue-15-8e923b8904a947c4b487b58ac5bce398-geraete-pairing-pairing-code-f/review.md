# Plan-Review

## Ergebnis

**Status:** Offene Aufgaben vorhanden

Fünfte Prüfung (Review-Runde 4 nach `review.1.md`/`review.2.md`/`review.3.md`). Geprüft wurde die Implementierung im Working Tree (HEAD = `9575d76` Feature-Commit) gegen `plan.md` inklusive des Nachtrags "Korrektur der Gate-Probe". Diese Runde hat die Befunde aus `review-code.3.md` und `review-usability.3.md` abgearbeitet: gezieltes `ClearDeviceTokenAsync(address)` bei `GateRejected` in `CheckCandidateServerAsync`, atomarer Vergleich/Reset in `DiscardIfCurrentAsync` (unter `providerLock`, Store-Löschung nach Release, `ThrowIfNullOrWhiteSpace`), Entfernung des toten `SetPairedTokenAsync` (ersetzt durch die bestehende stale-sichere `SetPairedTokenIfCurrentAsync`; Testaufrufe auf `SetDeviceTokenAsync` + `LoadForServerAsync`-Hilfsfunktionen umgestellt) und Entfernung von `MaxLength="32"` am `PairingCodeEntry` (Usability-Befund). Verifikation: `dotnet test VideoWebPlayer.Maui.Tests -c Release` → **873/873 Tests bestanden, 0 Fehler, 0 übersprungen** (ein Test weniger: `SetPairedToken_StoresAndActivatesToken` entfiel mit der entfernten Methode).

## Umgesetzte Planelemente

### Neue Klassen / Objekte

- [x] `PairingExchangeRequest` (Datenmodellklasse, `VideoPlayer.ApiClient/Models/PairingExchangeRequest.cs`) - `code`, `clientPublicKey`, `deviceName` mit `JsonPropertyName`-camelCase; `deviceName` wird bei `null` weggelassen (`JsonIgnoreCondition.WhenWritingNull`)
- [x] `PairingExchangeResponse` (Datenmodellklasse, `VideoPlayer.ApiClient/Models/PairingExchangeResponse.cs`) - `serverPublicKey`, `encryptedToken` (camelCase)
- [x] `PairingCrypto` (interne statische Klasse, `VideoPlayer.ApiClient/PairingCrypto.cs`) - flüchtiges ECDH-nistP256-Paar (`CreateKeyPair`), `ExportSubjectPublicKeyInfo` → Base64, `DeriveKeyFromHmac` (SHA256), AES-256-GCM-Entschlüsselung `nonce(12)‖ct‖tag(16)`; `CryptographicException` bei Format-/Entschlüsselungsfehlern; testbar über `InternalsVisibleTo` (`VideoPlayer.ApiClient.csproj` Z. 20)
- [x] `DevicePairingClient` (Klasse, `VideoPlayer.ApiClient/DevicePairingClient.cs`) - `ExchangeAsync(Uri, string, string?, CancellationToken)` mit injizierbarem `HttpClient`, `POST api/pairing/exchange` ohne Gate-/Bearer-Header, ohne Retry/Reauth, wirft `ApiException` (Status bzw. `null` bei Transport-/Timeoutfehler); `RequestTimeout` = 15 s (in `MauiProgram` verdrahtet)
- [x] `HealthCheckResult` (Enum, `VideoPlayer.ApiClient/HealthCheckResult.cs`) - `Reachable`, `GateRejected`, `Unreachable`
- [x] `PairingStatus` (Enum, `VideoPlayer.Mvu/PairingStatus.cs`) - `Succeeded`, `InvalidCode`, `RateLimited`, `InvalidRequest`, `NotSupported`, `ServerUnreachable`, `Failed`
- [x] `PairingCode` (statische Klasse, `VideoPlayer.Mvu/PairingCode.cs`) - `TryNormalize` mit Trim/Großschreibung, `MinLength = 4`, Alphabet `ABCDEFGHJKMNPQRSTUVWXYZ23456789` (Längenprüfung gegenüber Plan "Länge 8" gelockert - siehe Hinweise)
- [x] `DeviceTokenService` (Klasse, `VideoPlayer.Mvu/DeviceTokenService.cs`) - `LoadForServerAsync`, `SetPairedTokenIfCurrentAsync` (atomarer Stale-Guard unter `providerLock`), `DiscardCurrentAsync`, `DiscardIfCurrentAsync` (atomarer Adressvergleich + Provider-Reset unter `providerLock`, Store-Löschung nach Release), `CurrentServerAddress`-Tracking, `ClientGate.DeviceTokenProvider`-Pflege, `NotifyRejected()` → `DeviceTokenRejected`-Ereignis; `IsGateRejectedAsync` gemäß **Nachtrag**: `POST api/auth/login` mit `{}`-Body, nur `X-API-Key`-Header (adressspezifisch via `ResolveGateKeyAsync`: Geräte-Token der geprüften Adresse, sonst `ResolveStatic()`), Auswertung `401` + `application/problem+json` → `true`, `401` + `text/plain`/`400`/`429`/Transportfehler → `false`, angeforderter Abbruch wird durchgereicht. `SetPairedTokenAsync` (Plan-Element Nr. 13) wurde in Runde 4 entfernt - toter Code ohne Produktivaufrufer, Funktionalität vollständig durch `SetPairedTokenIfCurrentAsync` abgedeckt (siehe Hinweise).

### Neue MVU-Nachrichten und Effekte

- [x] `PairingCodeSubmittedMessage(string Code)`, `PairingCompletedMessage(PairingStatus Status)`, `DeviceTokenRevokedMessage` (`AppMessage.cs`)
- [x] `ExchangePairingCodeEffect(string ServerAddress, string Code)` (`AppEffect.cs`)
- [x] `ServerCheckCompletedMessage` / `ServerHealthCheckCompletedMessage` / `ServersLoadedMessage` - neues Feld `PairingRequired` (`IsReachable` bleibt `bool`)

### Änderungen an bestehenden Klassen

- [x] `ClientGate.DeviceTokenProvider` (`Func<string?>?`, statisch setzbar) - vorhanden (`ClientGate.cs`)
- [x] `ClientGate.Resolve()` - wertet `DeviceTokenProvider` mit höchster Priorität aus (Geräte-Token > Env > Metadata > Datei > Platzhalter); Diagnosezeile um `device=`-Quelle erweitert
- [x] `ClientGate.ResolveStatic()` (`public static`) - nur statische Quellen; interne Überladung `Resolve(string? deviceToken, string? environmentValue, string? metadataValue, string? fileValue)` vorhanden
- [x] `VideoPlayerApiClient.SetApiKey(string apiKey)` - ersetzt `X-API-Key`-Header, Guard via `ArgumentException.ThrowIfNullOrWhiteSpace`
- [x] `VideoPlayerApiClient.CheckHealthAsync` - Rückgabetyp `Task<HealthCheckResult>`; Erfolg → `Reachable`, `ApiException` 401 → `GateRejected`, sonstige → `Unreachable`
- [x] `AppSecretsService.DeviceTokenKey` + `Get/Set/Clear/HasDeviceTokenAsync(serverAddress)` - nur `ISecretStore`, Schlüssel `profile.PrefixKey("DeviceToken:" + normalisierte Adresse)`
- [x] `AppModel.IsPairingRunning` / `IsPairingRequired` / `PairingError` - vorhanden (`AppModel.cs`)
- [x] `AppUpdate.BeginServerCheck` - setzt `PairingError`/`IsPairingRequired`/`IsPairingRunning` zurück
- [x] `AppUpdate.UpdateServerCheckCompleted` - Erfolg + `PairingRequired` → `IsPairingRequired = true`, `NeedsServerSetup`/`ServerSetup`, `PersistActiveServerEffect`, kein `AttemptAutoLoginEffect`; Erfolg ohne `PairingRequired` → bisheriger Pfad; Misserfolg → Pairing-Felder zurückgesetzt
- [x] `AppUpdate.UpdateHealthCheckCompleted` - erreichbar + `PairingRequired` → `NeedsServerSetup`/`ServerSetup` (`IsStartupRunning = false`, `ServerHealth = Reachable`); sonst `AttemptAutoLoginEffect`; unerreichbar → Pairing-Felder zurückgesetzt + `CheckOfflineDataEffect`
- [x] `AppUpdate.UpdateServersLoaded` - `IsPairingRequired = message.PairingRequired`; `PairingError`/`IsPairingRunning` bleiben unverändert
- [x] `AppUpdate.UpdatePairingCodeSubmitted` - `PairingCode.TryNormalize`-Fehler → `PairingError = PairingCodeFormatError` ohne Effekt; gültig → `IsPairingRunning` + `ExchangePairingCodeEffect`; Guards gegen Doppel-Submit (`IsPairingRunning`) und fehlende `ActiveServerAddress`
- [x] `AppUpdate.UpdatePairingCompleted` - Stale-Result-Guard (`!IsPairingRunning` → `Unchanged`); `Succeeded` → `AttemptAutoLoginEffect`; Fehler-Mapping je Status (`PairingErrorFor`); `NotSupported` → `PairingNotSupportedError`, `IsPairingRunning = false`, kein `AttemptAutoLoginEffect`-Fallback
- [x] `AppUpdate.UpdateDeviceTokenRevoked` - `NeedsServerSetup` + `ServerSetup`-Navigation + `DeviceRevokedError` + `IsPairingRequired` + Lauf-Flags/`ServerHealth = Reachable` + `DisconnectServerEventsEffect` + Reset von `SourcesOverview`/`SourceBrowser`/`HomeHero`; Idempotenz-Guard bei bereits aktivem Pairing-Zustand
- [x] `AppUpdate`-Fehlerkonstanten - `InvalidPairingCodeError`, `PairingRateLimitedError`, `PairingNotSupportedError`, `PairingFailedError`, `DeviceRevokedError` (zusätzlich `PairingCodeFormatError` für lokale Formatfehler, siehe Hinweise)
- [x] `AuthSessionService` - `DeviceTokenService` per Konstruktor-Injection; `SilentReloginAsync` prüft bei 401/403 `IsGateRejectedAsync` → gezieltes `ClearDeviceTokenAsync` + `DiscardIfCurrentAsync` + `NotifyRejected` statt `SessionExpired` (`HandleGateRejectionAsync`); dieselbe Trennung auch in `TryStoredTokenAsync`/`LoginCoreAsync` (Ergebnis `AuthAttemptStatus.GateRejected`)
- [x] `IServerHealthProbe.CheckAsync` (beide Überladungen) → `Task<HealthCheckResult>`; `ServerHealthProbe` reicht durch, Catch-All → `Unreachable`; Gate-Schlüssel adressspezifisch aufgelöst (`ResolveGateKeyAsync`: Geräte-Token der geprüften Adresse, sonst `ResolveStatic()`)
- [x] `ShellAppEffectHandler` - `DeviceTokenRejected`-Abo → `DeviceTokenRevokedMessage` via `DispatchMessageAsync` (`OnDeviceTokenRejected`)
- [x] `ShellAppEffectHandler.ExchangePairingCodeAsync` - `DeviceInfo`-Gerätename (`DeviceInfo.Name`, Fallback `Manufacturer`+`Model`, Trim 200), `DevicePairingClient.ExchangeAsync`, Erfolg → `SetPairedTokenIfCurrentAsync` + `authSession.Client?.SetApiKey` (nur bei aktuellem Pairing-Kontext; sonst reine Ablage via `SetDeviceTokenAsync`), Status-Mapping 401→`InvalidCode`, 429→`RateLimited`, 400→`InvalidRequest`, 404→`NotSupported`, `null`→`ServerUnreachable`, sonst→`Failed`
- [x] `ShellAppEffectHandler.RunHealthCheckAsync` - `GateRejected` → `DiscardCurrentAsync` + einmalige Re-Probe → `Reachable`/`Unreachable` normal gemeldet, erneut `GateRejected` → `NotifyRejected()`; `PairingRequired` nur bei `Reachable`
- [x] `ShellAppEffectHandler.CheckCandidateServerAsync` - `GateRejected` → gezieltes `secrets.ClearDeviceTokenAsync(address)` (tote Token nicht-aktiver Kandidaten werden entfernt) + `DiscardIfCurrentAsync(address)` (Provider-Reset) + `Success = true`; danach `IsPairingRequiredAsync` mit bereinigter Ablage → korrektes `PairingRequired` in `ServerCheckCompletedMessage`
- [x] `ShellAppEffectHandler.LoadServersAsync` - Geräte-Token des aktiven Servers laden (`LoadForServerAsync`), Provider setzen, `PairingRequired` in `ServersLoadedMessage`
- [x] `ShellAppEffectHandler.PersistActiveServerAsync` - Provider-Umstellung via `LoadForServerAsync` vor dem nachfolgenden Auto-Login-Effekt
- [x] `ShellAppEffectHandler` - neue Abhängigkeiten `DevicePairingClient`, `DeviceTokenService`, `AppSecretsService` per Konstruktor-Injection
- [x] `ServerSetupPage.xaml` - Pairing-Abschnitt `PairingSection` im Fußbereich mit `PairingCodeEntry` (AutomationId, `Keyboard.Text`, **ohne `MaxLength`** - serverseitig konfigurierbare Codelänge wird nicht clientseitig abgeschnitten, Usability-Befund Runde 4), `IsSpellCheckEnabled="False"`, `IsTextPredictionEnabled="False"`), `PairButton` (`OnPairClicked`), `PairingIndicator`, `PairingErrorLabel` (`VpDanger`); Sichtbarkeit an `IsPairingRequired`; Hinweistext ohne feste Stellenzahl
- [x] `ServerSetupPage.xaml.cs` - `UpdateUi` bindet Sichtbarkeit/Indicator/Button- und Entry-Enabled/`PairingError`; `OnPairClicked` dispatcht `PairingCodeSubmittedMessage`
- [x] `MauiProgram` - `DevicePairingClient` (Singleton mit eigenem `HttpClient`, `Timeout = DevicePairingClient.RequestTimeout`) und `DeviceTokenService` (Singleton) registriert
- [x] `scripts/iOS-Deployment.ps1` - Parameter `[string]$VideoPlayerApiToken` (Default `$env:VIDEOWEBPLAYER_MAUI_API_TOKEN`), `Add-PropertyLine`/`Add-MaskedDisplay` in allen drei Response-Datei-Blöcken (`Invoke-Build`, `Invoke-Run`, `Invoke-DeviceViaSsh`), Maskierung in `Add-MaskedDisplay`

### Tests und Dokumentation

- [x] `ClientGateTests` - Geräte-Token-Priorität (`Resolve_PrefersDeviceTokenOverEnvironment`, `Resolve_PublicOverload_ConsultsDeviceTokenProvider`), Platzhalter-Überspringen, `ResolveStatic_IgnoresDeviceTokenProvider`, Provider-Reset-Isolation (`[Collection("ClientGate")]` via `Stubs/ClientGateCollection.cs`)
- [x] `VideoPlayerApiClientTests_Authentication` - `CheckHealthAsync_ReturnsReachableOnSuccess`, `CheckHealthAsync_ReturnsGateRejectedOnUnauthorized`, `CheckHealthAsync_ReturnsUnreachableOnFailure` (Theory)/`OnTransportError`, `SetApiKey_UpdatesClientGateHeader`, `SetApiKey_RejectsNullOrEmpty`
- [x] `DevicePairingClientTests` (neu) - ohne Gate-/Bearer-Header, camelCase-Payload, `deviceName`-Weglassen bei `null`, Fehler-Mapping 400/401/404/429/500 ohne Retry, Transportfehler → `ApiException` ohne Status, Roundtrip-Entschlüsselung, Code-Guard
- [x] `PairingCryptoTests` (neu) - Roundtrip, SPKI-Import, Base64-/Längen-/Manipulations-/Fremdschlüssel-Fehler
- [x] `PairingCodeTests` (neu) - Validierung/Normalisierung/Alphabet inkl. serverseitig konfigurierbarer Längen (`TryNormalize_AcceptsServerConfiguredLengths`, `TryNormalize_RejectsBelowMinLength`)
- [x] `AppSecretsServiceTests` - nur `ISecretStore`, Server-/Profil-Isolation, Adress-Normalisierung, Clear, `null`-Rückgabe ohne Eintrag
- [x] `DeviceTokenServiceTests` (neu) - Provider-Setzung/-Rücksetzung, `DiscardIfCurrent`-Adressvergleich, `SetPairedTokenIfCurrent` (aktuell vs. veraltet), `NotifyRejected`-Ereignis, `IsGateRejected_*` gemäß korrigierter Probe (401 problem+json → `true`; 401 text/plain, 400, 429, Transportfehler → `false`; adressspezifischer Schlüssel; Abbruch-/Timeout-Semantik); Setup-Helfer `PairAsync` (`SetDeviceTokenAsync` + `LoadForServerAsync`) ersetzt die entfernte `SetPairedTokenAsync`
- [x] `AppUpdateTests_Pairing` (neu) - Submit-/Completed-Flow inkl. `NotSupported` ohne Fallback, Stale-Result-Guard, Doppel-Submit-Guard, `DeviceTokenRevoked`-Navigation/Idempotenz
- [x] `AppUpdateTests_ServerManagement` / `AppUpdateTests_Startup` / `AppUpdateTests_Connection` - `PairingRequired`-Verzweigungen, Reset-Semantik, `DeviceTokenRevoked` im Start-Pfad (`Update_DeviceTokenRevoked_DuringStartup_ShowsPairingInputWithError`), `GateRejected`-Auto-/Login-Übergänge
- [x] `AuthSessionServiceTests` - `TokenRefresh_GateRejected_RaisesDeviceTokenRejected_NotSessionExpired`, `TokenRefresh_GateProbeAccepted_RaisesSessionExpired`, `GateRejected`-Pfade von `TryStoredLogin`/`LoginAsync` inkl. Fremd-Server-Fall (`LoginAsync_GateRejected_ForNonCurrentServer_KeepsActiveTokenAndStaysSilent`); Setup-Helfer `PairDeviceAsync` ersetzt die entfernte `SetPairedTokenAsync`
- [x] `EnumCoverageTests` - `HealthCheckResult_AllValuesAreCovered`, `PairingStatus_AllValuesAreCovered` (`enum-coverage-check`-Hook in `.githooks/pre-commit`/`pre-push` vorhanden)
- [x] `PairingContractTests` (neu, Quelltext-Verträge) - UI-Abschnitt/AutomationIds/Dispatch, Kein-`MaxLength`-Vertrag am `PairingCodeEntry`, Effekt-Zweig inkl. Stale-Guard, `GateRejected`-Zweige in `RunHealthCheckAsync`/`CheckCandidateServerAsync`, DI-Registrierungen, `iOS-Deployment.ps1`-Vertrag, `ServerHealthProbe`-Rückgabetyp und adressspezifische Schlüsselauflösung, Login-Endpunkt der Gate-Probe
- [x] Bestehende Tests an neue Signaturen angepasst (`AppUpdateTests_*`, `MvuHostTests`, `AppUpdateTests_Offline`) - kompilieren und laufen (873/873)
- [x] Dokumentation - `docs/help/api-client/api.md`/`installation.md`/`troubleshooting.md`/`ablauf-technisch.md` an `HealthCheckResult`/Pairing/`SetApiKey` angepasst, Zwischenlösung statischer Token dokumentiert (Skript-Kommentar + `installation.md`), `serververwaltung-erststart.md`/`verbindung-anmeldung.md` ergänzt

## Offene Aufgaben

- [ ] Manueller Pairing-Lauf gegen Testserver auf echtem Ziel (Tasks-Datei Nr. 42) - **teilweise umgesetzt:** `live-verification.md` belegt den kryptografischen und API-vertraglichen Teil mit dem produktiven `DevicePairingClient`/`PairingCrypto` gegen den IIS-Testserver (Code → ECDH → AES-GCM → Token → Login 200, Negativfälle 401, `PairedDevices`-Einträge). Offen bleiben die gerätegebundenen Anteile `SecureStorage`-Persistenz, `DeviceInfo`-Gerätename und die UI-Bedienung auf einem echten Ziel (iOS/Android/Windows) - im Plan als manueller Nachweis vorgesehen, in dieser Umgebung nicht ausführbar.

## Hinweise

- Abweichungen/Erweiterungen gegenüber dem Plan sind sämtlich durch die Review-Runden begründet und plangemäß konsistent:
  - `PairingCode.TryNormalize` prüft nur `MinLength = 4` + Alphabet statt exakt Länge 8; `PairingCodeEntry` trägt inzwischen gar kein `MaxLength` mehr (Usability-Befund Runde 4: die serverseitige `Pairing:CodeLength`-Option darf nicht clientseitig überstimmt werden - der Plan hatte diesen Fall selbst als Einschränkung benannt).
  - `SetPairedTokenAsync` (Plan-Element Nr. 13) entfernt: kein Produktivaufrufer, und die unbedingte `CurrentServerAddress`-Setzung ohne Stale-Prüfung war ein Risiko (Code-Review-Befund Runde 4). Der Produktivpfad nutzt ausschließlich `SetPairedTokenIfCurrentAsync`; Tests ersetzen die Vorrichtung über `secrets.SetDeviceTokenAsync` + `LoadForServerAsync`.
  - `DiscardIfCurrentAsync` ist jetzt atomar: Adressvergleich und Provider-Rücksetzung unter `providerLock`, Store-Löschung nach dem Release, `ArgumentException.ThrowIfNullOrWhiteSpace` (Code-Review-Befund Runde 4).
  - `CheckCandidateServerAsync` löscht bei `GateRejected` das nachweislich tote Geräte-Token der geprüften Adresse gezielt (`secrets.ClearDeviceTokenAsync`), auch wenn der Kandidat nicht aktiv ist (Code-Review-Befund Runde 4).
  - `PairingCodeFormatError` für lokale Formatfehler statt `InvalidPairingCodeError` (Usability-Befund); `InvalidPairingCodeError` bleibt dem serverseitigen 401 vorbehalten; `PairingNotSupportedError` und Hinweistexte ohne feste Stellenzahl.
  - Neuer Enum-Wert `AuthAttemptStatus.GateRejected` (`VideoPlayer.Mvu/AuthAttempt.cs`) samt `GateRejected`-Übergängen in `UpdateAutoLoginCompleted`/`UpdateLoginCompleted` - schließt die Auto-Login-/Login-Lücke des widerrufenen Geräte-Tokens (Code-Review-Befund).
  - Adressspezifische Gate-Schlüssel-Auflösung (`ResolveGateKeyAsync`) in `ServerHealthProbe` **und** `DeviceTokenService.IsGateRejectedAsync` - verhindert Credential-Weitergabe an fremde Adressen (Code-Review-Befund).
  - Atomarer `SetPairedTokenIfCurrentAsync`-Guard unter `providerLock` plus Stale-Context-Prüfung der Seiteneffekte in `ExchangePairingCodeAsync`; gezieltes `ClearDeviceTokenAsync` der abgelehnten Adresse in `HandleGateRejectionAsync` (Code-Review-Befunde).
  - `DevicePairingClient.RequestTimeout` (15 s) statt `HttpClient`-Default 100 s; `IsSpellCheckEnabled`/`IsTextPredictionEnabled` am `PairingCodeEntry` (Usability-Befunde).
  - Testisolation des statischen `ClientGate.DeviceTokenProvider` über die xUnit-Collection `ClientGate` (`Stubs/ClientGateCollection.cs`).
- `HealthCheckResult.GateRejected` über `CheckHealthAsync` (`api/health`) bleibt Defense-in-depth für Server mit gated Health-Endpunkt; auf dem Referenzserver ist `api/health` ungeschützt - der primäre Widerrufs-Detektor ist die korrigierte Login-Probe in `DeviceTokenService.IsGateRejectedAsync` (Plan-Nachtrag umgesetzt und per `PairingContractTests.DeviceTokenService_GateProbe_UsesLoginEndpoint` vertraglich abgesichert).
- Die Tasks-Datei (`issue-15-...-tasks.md`) ist aktuell: Nr. 1-41 `Erledigt`, Nr. 42 `Offen` (Teilnachweis per `live-verification.md` vermerkt).
