# Test-Ergebnisse

Datum: 2026-09-23
Befehl: `dotnet build VideoPlayer.App.sln --configuration Release` (0 Fehler, 2 Warnungen) → `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj --configuration Release --collect:"XPlat Code Coverage" --logger "console;verbosity=normal"`

## Ergebnis

**Status:** Keine Fehler

## E2E-Abdeckung

Das Repo enthält keine UI-/E2E-Testinfrastruktur (Testprojekt referenziert keine MAUI-Projekte); der Funktionsnachweis erfolgt plan- und plan-check-konform über MVU-Update-Tests plus Quelltext-Contract-Tests. Alle geplanten Szenarien sind vorhanden und bestanden — teilweise unter in der Umsetzung leicht umbenannten Testnamen (in Klammern vermerkt).

| Szenario | Test / Testklasse | Ergebnis |
|----------|-------------------|----------|
| Server verbinden → Code eingeben → gekoppelt → Anmeldung läuft (AC 1, 2) | `AppUpdateTests_Pairing` (`Update_PairingCodeSubmitted_Valid_StartsExchange`, `Update_PairingCompleted_Succeeded_ContinuesAutoLogin`, `Update_ServerCheckCompleted_Success_PairingRequired_ShowsPairingInput`, `Update_HealthCheckCompleted_Reachable_PairingRequired_GoesToSetup`, `Update_ServersLoaded_SetsPairingFlags`) + `PairingContractTests`/`ServerSetupContractTests` (AutomationIds, Dispatches, DI) | Bestanden |
| Ungültiger/abgelaufener Code → Inline-Fehler, erneute Eingabe (AC 7) | `AppUpdateTests_Pairing.Update_PairingCompleted_MapsErrors` (Theory) + `Update_PairingCodeSubmitted_Invalid_SetsFormatErrorWithoutEffects` (umbenannt von `..._SetsErrorWithoutEffects`) + `DevicePairingClientTests.ExchangeAsync_MapsErrorStatus_WithoutRetry` | Bestanden |
| 429 IP-Sperre → Hinweis, kein automatisches Wiederholen (AC 7) | `DevicePairingClientTests.ExchangeAsync_MapsErrorStatus_WithoutRetry` (genau 1 Request bei 429) + `AppUpdateTests_Pairing.Update_PairingCompleted_MapsErrors` (`RateLimited`) | Bestanden |
| Widerrufenes Geräte-Token → Re-Pairing-Flow mit Fehlertext, Laufzeit und Start/Health-Check (AC 5) | `AppUpdateTests_Pairing.Update_DeviceTokenRevoked_NavigatesToSetupWithError` + `Update_DeviceTokenRevoked_AlreadyInPairingState_IsIgnored`/`_OnSetupPage_SkipsNavigation` + `AppUpdateTests_Startup.Update_DeviceTokenRevoked_DuringStartup_ShowsPairingInputWithError` + `DeviceTokenServiceTests.IsGateRejected_*` (korrigierte Login-Probe: ProblemJson→true, PlainText/BadRequest/429/Transport→false) + `AuthSessionServiceTests.TokenRefresh_GateRejected_RaisesDeviceTokenRejected_NotSessionExpired` (umbenannt von `SilentRelogin_...`) + `PairingContractTests.EffectHandler_HealthCheck_HandlesGateRejected` + `DeviceTokenService_GateProbe_UsesLoginEndpoint` | Bestanden |
| Token-Persistenz in `SecureStorage` (nicht Preferences) (AC 3) | `AppSecretsServiceTests.DeviceToken_GoesToSecretStore_Only` (umbenannt von `GetDeviceToken_GoesToSecretStore_Only`), `SetDeviceToken_IsolatedPerServer`, `ClearDeviceToken_RemovesOnlyThatServer`, `GetDeviceToken_WithoutEntry_ReturnsNull` | Bestanden |
| Server ohne Pairing-Endpunkt → `PairingNotSupportedError`, kein Fallback (AC 8) | `AppUpdateTests_Pairing.Update_PairingCompleted_NotSupported_ShowsError_WithoutFallback` (kein `AttemptAutoLoginEffect`) | Bestanden |
| `ClientGate.Resolve()`-Priorität Geräte-Token > Env > Metadata > Datei (AC 4) | `ClientGateTests`: `Resolve_PrefersDeviceTokenOverEnvironment`, `Resolve_FallsBack_WhenDeviceTokenProviderReturnsPlaceholderOrNull` (Theory), `ResolveStatic_IgnoresDeviceTokenProvider` | Bestanden |
| Pairing-Endpunkt-Vertrag + Krypto (AC 2, 6) | `DevicePairingClientTests` (Header-Freiheit, camelCase-Payload, Roundtrip) + `PairingCryptoTests` (`DecryptToken_Roundtrip_RecoversPlaintextToken`, `DecryptToken_*`-Negativfälle) + `PairingCodeTests` | Bestanden |
| `HealthCheckResult`-Enum (`CheckHealthAsync`/`IServerHealthProbe`) | `VideoPlayerApiClientTests_Authentication.CheckHealthAsync_ReturnsGateRejectedOnUnauthorized`, `CheckHealthAsync_ReturnsUnreachableOnFailure` (Theory), `CheckHealthAsync_ReturnsUnreachableOnTransportError`, `CheckHealthAsync_ReturnsReachableOnSuccess` + `EnumCoverageTests.HealthCheckResult_AllValuesAreCovered`/`PairingStatus_AllValuesAreCovered` | Bestanden |
| `SetApiKey` für Re-Pairing | `VideoPlayerApiClientTests_Authentication.SetApiKey_UpdatesClientGateHeader` (umbenannt von `SetApiKey_ReplacesGateHeader_OnSubsequentRequests`), `SetApiKey_RejectsNullOrEmpty` (Theory) | Bestanden |
| Echter Gerätefluss gegen Testserver (manuell, AC 1–6 gesamt) | `live-verification.md` (2026-09-23, IIS-Testserver): echter `DevicePairingClient`+`PairingCrypto` gegen `POST api/pairing/exchange` (200, Token entschlüsselt), Negativfälle 401, `PairedDevices`-Einträge, Geräte-Token als `X-API-Key` am MauiOnly-Gate (200 + JWT), Code-Wiederverwendung geblockt | Bestanden (manuell, serverseitig verifiziert); gerätespezifische Anteile (`SecureStorage`, `DeviceInfo`, UI auf echtem Ziel) in dieser Umgebung nicht prüfbar |

## Zusammenfassung

- Gesamt: 862
- Bestanden: 862
- Fehlgeschlagen: 0
- Übersprungen: 0

## Testabdeckung

**Abdeckung:** 86,6 % (Zeilen, Cobertura: `VideoWebPlayer.Maui.Tests/TestResults/5b364a03-3ced-442c-998d-d4330a4a55ea/coverage.cobertura.xml`)

| Datei | Abdeckung |
|-------|-----------|
| `VideoWebPlayer.Client/VideoWebPlayerClient.cs` | 0,0 % |
| `VideoWebPlayer.Client/Models/AuthenticationRequest.cs` | 0,0 % |
| `VideoWebPlayer.Client/Models/AuthorizationToken.cs` | 0,0 % |
| `VideoWebPlayer.Client/Models/DtoRecentEntry.cs` | 0,0 % |
| `VideoWebPlayer.Client/Models/DtoSource.cs` | 0,0 % |
| `VideoWebPlayer.Client/Models/FavoriteEntry.cs` | 0,0 % |
| `VideoWebPlayer.Client/Models/ImpersonateRequest.cs` | 0,0 % |
| `VideoWebPlayer.Client/Models/MediaEntryDto.cs` | 0,0 % |
| `VideoWebPlayer.Client/Models/DtoMovie.cs` | 37,8 % |
| `VideoPlayer.ApiClient/Discovery/MdnsServerDiscovery.cs` | 0,0 % |
| `VideoPlayer.ApiClient/ClientGate.cs` | 60,7 % |
| `VideoPlayer.ApiClient/ApiException.cs` | 66,7 % |
| `VideoPlayer.ApiClient/Discovery/DiscoveredServer.cs` | 75,0 % |
| `VideoPlayer.ApiClient/Discovery/ServerDiscoveryService.cs` | 77,3 % |
| `VideoPlayer.ApiClient/Models/MediaEntry.cs` | 78,4 % |
| `VideoPlayer.Mvu/DownloadListItem.cs` | 60,0 % |
| `VideoPlayer.Mvu/ProfilePageState.cs` | 70,0 % |
| `VideoPlayer.Mvu/SourceItem.cs` | 75,0 % |

## Fehlende Tests

Quelle: `Coverage-Daten` (Dateien mit 0 % Zeilenabdeckung)

- `VideoWebPlayer.Client/VideoWebPlayerClient.cs` – 0 % Abdeckung (Legacy-Client der Server-Lösung; wird vom Testprojekt nicht ausgeführt, die App nutzt `VideoPlayer.ApiClient`)
- `VideoWebPlayer.Client/Models/AuthenticationRequest.cs` – 0 % Abdeckung
- `VideoWebPlayer.Client/Models/AuthorizationToken.cs` – 0 % Abdeckung
- `VideoWebPlayer.Client/Models/DtoRecentEntry.cs` – 0 % Abdeckung
- `VideoWebPlayer.Client/Models/DtoSource.cs` – 0 % Abdeckung
- `VideoWebPlayer.Client/Models/FavoriteEntry.cs` – 0 % Abdeckung
- `VideoWebPlayer.Client/Models/ImpersonateRequest.cs` – 0 % Abdeckung
- `VideoWebPlayer.Client/Models/MediaEntryDto.cs` – 0 % Abdeckung
- `VideoPlayer.ApiClient/Discovery/MdnsServerDiscovery.cs` – 0 % Abdeckung (Zeroconf/mDNS-Bindung nicht testbar; `ServerDiscoveryService`/`DiscoveryProtocolTests` decken die Protokollebene ab)

Hinweis: Alle gemeldeten Abdeckungslücken sind Bestand und nicht durch diese Anforderung entstanden; die neue Pairing-Funktionalität (`DevicePairingClient`, `PairingCrypto`, `DeviceTokenService`, Pairing-MVU-Übergänge) ist durch Tests abgedeckt.
