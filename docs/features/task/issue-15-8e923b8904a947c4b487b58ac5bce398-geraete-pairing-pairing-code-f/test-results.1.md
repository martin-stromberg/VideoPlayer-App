# Test-Ergebnisse

## Ergebnis

**Status:** Fehler vorhanden

Hinweis: Der einzige Fehlschlag ist ein intermittenter Test-Isolationsfehler (Race Condition auf dem statischen `ClientGate.DeviceTokenProvider` zwischen parallel laufenden Testklassen). Ein unveränderter Wiederholungslauf derselben kompletten Suite war vollständig grün (846/846). Der isolierte Filterlauf der drei beteiligten Testklassen war ebenfalls grün (47/47). Ursache: `ClientGateTests` setzt den statischen Provider, während `DeviceTokenServiceTests`/`AuthSessionServiceTests` ihn parallel zurücksetzen oder neu setzen (kein gemeinsamer xUnit-Collection-/Lock-Mechanismus). Da auf dem Testrechner ein statischer Token konfiguriert ist (`VIDEOWEBPLAYER_MAUI_API_TOKEN`/Token-Datei/Metadata), fällt `Resolve()` in der Rennbedingung auf den echten statischen Schlüssel zurück statt auf `"provider-token"`. Empfehlung: Alle Testklassen, die `ClientGate.DeviceTokenProvider` mutieren, in eine gemeinsame xUnit-Test-Collection legen (z. B. `[Collection("ClientGate")]`).

## Fehlgeschlagene Tests

### ClientGateTests

- **Resolve_PublicOverload_ConsultsDeviceTokenProvider** — `Assert.Equal`: erwartet `"provider-token"`, tatsächlich der statisch aufgelöste Maschinen-Token (`"i/F8htcJc/…"`) — intermittente Race Condition mit parallelen `DeviceTokenProvider`-Mutationen (`ClientGateTests.cs:123`); im Wiederholungslauf bestanden.

## E2E-Abdeckung

Das Repo hat keine UI-/E2E-Infrastruktur; der Funktionsnachweis erfolgt plan- und plan-check-konform über vollständige MVU-Update-Tests plus Quelltext-Contract-Tests. Alle geplanten Szenarien sind vorhanden und wurden ausgeführt.

| Szenario | Test / Testklasse | Ergebnis |
|----------|-------------------|----------|
| Server verbinden → Code eingeben → gekoppelt → Anmeldung (AC 1, 2) | `AppUpdateTests_Pairing` (`Update_PairingCodeSubmitted_Valid_StartsExchange`, `Update_PairingCompleted_Succeeded_ContinuesAutoLogin`) + `PairingContractTests` (`ServerSetupPage_ContainsPairingSection`, `ServerSetupPage_PairingControlsHaveAutomationIds`, `ServerSetupPage_PairButton_DispatchesPairingCodeSubmitted`, `EffectHandler_HandlesPairingEffects`, `MauiProgram_RegistersPairingServices`) | Bestanden |
| Ungültiger/abgelaufener Code → Inline-Fehler, erneute Eingabe (AC 7) | `AppUpdateTests_Pairing` (`Update_PairingCodeSubmitted_Invalid_SetsErrorWithoutEffects` Theory, `Update_PairingCompleted_MapsErrors` Theory) + `DevicePairingClientTests.ExchangeAsync_MapsErrorStatus_WithoutRetry` + `PairingCodeTests` | Bestanden |
| 429 IP-Sperre → Hinweis, kein automatisches Wiederholen (AC 7) | `DevicePairingClientTests.ExchangeAsync_MapsErrorStatus_WithoutRetry(TooManyRequests)` + `AppUpdateTests_Pairing.Update_PairingCompleted_MapsErrors(RateLimited)` | Bestanden |
| Widerrufenes Geräte-Token → Re-Pairing-Flow, Laufzeit **und** Start/Health-Check (AC 5) | `AppUpdateTests_Pairing.Update_DeviceTokenRevoked_NavigatesToSetupWithError`, `AppUpdateTests_Startup.Update_DeviceTokenRevoked_DuringStartup_ShowsPairingInputWithError`, `DeviceTokenServiceTests` (`IsGateRejected_*`, `NotifyRejected_RaisesDeviceTokenRejected`, `DiscardIfCurrent_OnlyDiscardsMatchingServer`, `DiscardCurrent_ResetsProviderAndClearsStore`), `AuthSessionServiceTests.TokenRefresh_GateRejected_RaisesDeviceTokenRejected_NotSessionExpired`, `PairingContractTests.EffectHandler_HealthCheck_HandlesGateRejected` | Bestanden |
| Token-Persistenz in `SecureStorage` (nicht Preferences) (AC 3) | `AppSecretsServiceTests` (`GetDeviceToken_WithoutEntry_ReturnsNull`, `SetDeviceToken_IsolatedPerServer`, `ClearDeviceToken_RemovesOnlyThatServer`, `DeviceToken_IsProfileIsolated`, `DeviceToken_NormalizesServerAddress`) | Bestanden |
| Server ohne Pairing-Endpunkt → `PairingNotSupportedError`, kein Fallback (AC 8) | `AppUpdateTests_Pairing.Update_PairingCompleted_NotSupported_ShowsError_WithoutFallback` + `Update_PairingCompleted_MapsErrors(NotSupported)` | Bestanden |
| Gate-Auflösung/Priorität Geräte-Token > Env > Metadata > Datei (AC 4) | `ClientGateTests` (`Resolve_PrefersDeviceTokenOverEnvironment`, `Resolve_FallsBack_WhenDeviceTokenProviderReturnsPlaceholderOrNull`, `ResolveStatic_IgnoresDeviceTokenProvider`, `Resolve_PublicOverload_ConsultsDeviceTokenProvider` — letzterer im 1. Lauf intermittent fehlgeschlagen, im Wiederholungslauf bestanden) | Bestanden (1 intermittenter Fehlschlag) |
| Krypto-Vertrag ECDH/AES-256-GCM (AC 2) | `PairingCryptoTests` (`DecryptToken_Roundtrip_RecoversPlaintextToken`, `DecryptToken_WrongServerKey_Throws`, `DecryptToken_TamperedPayload_Throws`, `DecryptToken_InvalidBase64_Throws`, `DecryptToken_TooShortPayload_Throws`) + `DevicePairingClientTests.ExchangeAsync_DecryptsToken_Roundtrip` | Bestanden |
| Pairing-Endpunkt öffentlich ohne Gate/Bearer, camelCase-Payload (AC 2, 6) | `DevicePairingClientTests` (`ExchangeAsync_SendsRequest_WithoutGateAndBearerHeaders`, `ExchangeAsync_SendsCamelCasePayload`, `ExchangeAsync_OmitsDeviceName_WhenNull`, `ExchangeAsync_RequiresCode`, `ExchangeAsync_TransportError_MapsToApiExceptionWithoutStatus`) + `PairingContractTests.DevicePairingClient_UsesPublicEndpoint_WithoutAuthHeaders` | Bestanden |
| `HealthCheckResult`-Umstellung + `SetApiKey` (AC 5) | `VideoPlayerApiClientTests_Authentication` (`CheckHealthAsync_ReturnsReachableOnSuccess`, `CheckHealthAsync_ReturnsGateRejectedOnUnauthorized`, `CheckHealthAsync_ReturnsUnreachableOnFailure`, `SetApiKey_UpdatesClientGateHeader`, `SetApiKey_RejectsNullOrEmpty`) + `PairingContractTests.ServerHealthProbe_ReturnsHealthCheckResult` | Bestanden |
| Enum-Abdeckung `HealthCheckResult`/`PairingStatus` | `EnumCoverageTests.HealthCheckResult_AllValuesAreCovered`, `EnumCoverageTests.PairingStatus_AllValuesAreCovered` | Bestanden |
| Deployment-Skript reicht `VideoPlayerApiToken` durch (AC 8) | `PairingContractTests.IosDeploymentScript_PropagatesAndMasksApiToken` | Bestanden |
| Echter Gerätefluss (SecureStorage, DeviceInfo, DI) | Manuell: `scripts/iOS-Deployment.ps1` + Testserver | Nicht ausgeführt (manueller Nachweis lt. Plan ausstehend) |

## Zusammenfassung

- Gesamt: 846
- Bestanden: 845 (Lauf 1, mit Coverage) / 846 (Lauf 2, identischer Build)
- Fehlgeschlagen: 1 (intermittent; im Wiederholungslauf bestanden)
- Übersprungen: 0

Baseline vor Feature: 743 Tests → aktuell 846 Tests (+103).

## Testabdeckung

**Abdeckung:** 86,5 % Zeilenabdeckung gesamt (Cobertura: `VideoPlayer.ApiClient` 86,0 %, `VideoPlayer.Mvu` 92,5 %, `VideoPlayer.Storage` 98,9 %, `VideoWebPlayer.Client` 7,3 %)

| Datei | Abdeckung |
|-------|-----------|
| VideoPlayer.ApiClient/ApiException.cs | 66,7 % |
| VideoPlayer.ApiClient/ClientGate.cs | 60,7 % |
| VideoPlayer.ApiClient/Discovery/DiscoveredServer.cs | 75,0 % |
| VideoPlayer.ApiClient/Discovery/MdnsServerDiscovery.cs | 0,0 % |
| VideoPlayer.ApiClient/Discovery/ServerDiscoveryService.cs | 77,3 % |
| VideoPlayer.ApiClient/Models/MediaEntry.cs | 78,4 % |
| VideoPlayer.Mvu/DownloadListItem.cs | 60,0 % |
| VideoPlayer.Mvu/ProfilePageState.cs | 66,7 % |
| VideoPlayer.Mvu/SourceItem.cs | 75,0 % |
| VideoWebPlayer.Client/VideoWebPlayerClient.cs | 0,0 % |
| VideoWebPlayer.Client/Models/DtoMovie.cs | 37,8 % |
| VideoWebPlayer.Client/Models/AuthenticationRequest.cs | 0,0 % |
| VideoWebPlayer.Client/Models/AuthorizationToken.cs | 0,0 % |
| VideoWebPlayer.Client/Models/DtoRecentEntry.cs | 0,0 % |
| VideoWebPlayer.Client/Models/DtoSource.cs | 0,0 % |
| VideoWebPlayer.Client/Models/FavoriteEntry.cs | 0,0 % |
| VideoWebPlayer.Client/Models/ImpersonateRequest.cs | 0,0 % |
| VideoWebPlayer.Client/Models/MediaEntryDto.cs | 0,0 % |

## Fehlende Tests

Quelle: `Coverage-Daten`

- `VideoPlayer.ApiClient/Discovery/MdnsServerDiscovery.cs` — 0 % Abdeckung (mDNS-Discovery; UDP-basierte `UdpServerDiscoveryTests` existieren, decken diese Klasse aber nicht ab)
- `VideoWebPlayer.Client/**` (gesamtes Legacy-Client-Projekt: `VideoWebPlayerClient.cs`, DTO-Modelle) — 0 % Abdeckung, keine Testdatei gefunden (Altbestand, nicht Teil dieses Features)
