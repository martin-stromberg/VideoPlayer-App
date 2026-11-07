# Tests — Bestandsaufnahme

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-09-23, ~12:47–12:52 MESZ (UTC+02:00)
- Branch und Commit-ID: `task/issue-15-8e923b8904a947c4b487b58ac5bce398-geraete-pairing-pairing-code-f`, Commit `87f5faf5b5608abcd6c1560bc3f55883dd2a0106` („Neugestaltung der App (#14)")
- Uncommittete Änderungen im getesteten Stand: nur das neue, untracked Verzeichnis `docs/features/task/issue-15-…/requirement.md` (keine Codeänderungen); die Inventar-Dateien entstanden erst nach dem Lauf.
- Testumgebung und Runtime-/SDK-Versionen: Windows, .NET SDK 10.0.401; Testprojekt `net10.0` (xunit.v3 3.2.2, Microsoft.NET.Test.Sdk 18.8.1, coverlet.collector 10.0.1)
- Ermittelte Testsuiten und Quellen der Testbefehle: einzige Testsuite ist `VideoWebPlayer.Maui.Tests` (Testbefehl aus `.github/workflows/pr-checks.yml`: `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj --configuration Release` nach Release-Build; die MAUI-Projekte `VideoPlayer.Maui`/`VideoWebPlayer.Maui` werden in CI nur gebaut, nicht getestet — das Testprojekt referenziert kein MAUI-Projekt, MAUI-Artefakte werden über Quelltext-Contract-Tests geprüft).

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| Ausgangslauf | `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj --configuration Release` | Repo-Wurzel `D:\Repositories\softwareschmiede\8e923b89-04a9-47c4-b487-b58ac5bce398` | 0 | 743 | 0 | 0 | [dotnet-test-release-2026-09-23.log](test-results/dotnet-test-release-2026-09-23.log) |

Build-Warnungen des Laufs (keine Fehler, bestehender Stand): `CS1570` in `VideoPlayer.Storage/KnownServersService.cs` (ungültiger XML-Kommentar, Zeile 9), `CS0108`/`CS8618` in `VideoWebPlayer.Client/Models/*` (Altbestand).

### Nachgewiesene bestehende Testfehler

Keine — alle 743 Tests des Ausgangslaufs waren erfolgreich.

### Testlücken und Ausführungsprobleme

- Die MAUI-Projekte (`VideoPlayer.Maui`, `VideoWebPlayer.Maui`) sind vom Testprojekt nicht referenzierbar (MAUI-Zielframeworks); ihre Verdrahtung (XAML, DI-Registrierung, Effekt-Handler-Zweige, Routen) wird ausschließlich über Quelltext-Contract-Tests abgedeckt — Laufzeitverhalten dieser Projekte ist ungetestet.
- Krypto-Funktionalität (ECDH nistP256, AES-256-GCM) existiert noch nicht und ist folglich ungetestet; ein serverseitiger Referenz-Testvektor liegt in diesem Repo nicht vor.
- Keine Pairing-bezogenen Tests vorhanden (kein `PairingContractTests`, keine `ExchangePairingCode`-Abdeckung).

## Testklassen

### `ClientGateTests` (`ClientGateTests.cs`)
- `Resolve_PrefersEnvironmentVariable` — Umgebungsvariable schlägt Metadaten.
- `Resolve_FallsBackToAssemblyMetadata` — Metadaten-Fallback.
- `Resolve_FallsBackToPlaceholder` (Theory) — leere Quellen → Platzhalter.
- `Resolve_FallsBackToTokenFile` — Token-Datei greift bei leeren Env/Metadaten.
- `Resolve_PrefersEnvironmentAndMetadataOverFile` (Theory) — Env/Metadaten vor Datei.
- `Resolve_SkipsPlaceholderValues` (Theory) — Platzhalter-Werte werden übersprungen.
- **Erweiterungspunkt:** Geräte-Token-Quelle mit höchster Priorität (Anforderung AC 4/7).

### `AppUpdateTests_Startup` (`AppUpdateTests_Startup.cs`)
- `Update_StartupRequested_SetsCheckingAndLoadsStorageSettingsServers` / `…_WhenAlreadyChecking_NoEffects`
- `Update_ServersLoaded_WithActiveServer_DuringStartup_EmitsHealthCheck` / `…_AfterStartup_OnlyUpdatesModel` / `…_WithoutServer_NavigatesToSetup` / `…_AlreadyOnSetup_NoNavigation`
- `Update_HealthCheckCompleted_Reachable_EmitsAutoLogin` / `…_WithoutActiveServer_GoesToSetup` / `…_Unreachable_EmitsOfflineDataCheck`
- **Erweiterungspunkt:** Verzweigung „Server erreichbar + kein Geräte-Token → Pairing-Eingabe".

### `AppUpdateTests_ServerManagement` (`AppUpdateTests_ServerManagement.cs`)
- `Update_LoadServersRequested_EmitsLoadEffect`, `Update_DiscoveryRequested_*`, `Update_DiscoveryCompleted_TakesOverCandidates`
- `Update_AddressSubmitted_Invalid_SetsErrorWithoutEffects` (Theory), `Update_AddressSubmitted_Valid_StartsCheckWithNormalizedAddress`
- `Update_DiscoveredServerSelected_StartsCheck`, `Update_ServerSwitchRequested_StartsCheck`
- `Update_ServerCheckCompleted_Failure_ShowsErrorAndStays`, `Update_ServerCheckCompleted_Success_PersistsAndStartsAutoLogin`, `…_ExistingServer_MovesToTop`
- **Erweiterungspunkt:** `ServerCheckCompleted`-Erfolgszweig muss künftig (ohne Geräte-Token) die Pairing-Eingabe statt direkt `AttemptAutoLoginEffect` ansteuern — dieser Test bildet den heutigen Zielzustand ab.

### `AppUpdateTests_Connection` (`AppUpdateTests_Connection.cs`)
- `OfflineDataCheckCompleted_*` (Offline vs. ServerSetup)
- `AutoLoginCompleted_Succeeded_ConnectsAndNavigatesHome`, `…_MissingCredentials_NavigatesToLoginWithReloginNotice`, `…_Failed_NavigatesToLoginWithError` (Theory), `…_FromServerSetup_StopsCheckIndicator`
- `LoginSubmitted_*`, `LoginCompleted_*`
- `SessionExpired_FromConnected_NavigatesToLogin`, `…_ResetsSourcesOverviewAndBrowser`, `…_WhenAlreadyNeedsLogin_NoEffects` — Muster für den Re-Pairing-Flow (anderes Ziel: `ServerSetup` statt `Login`).
- `NavigateTo_MenuPage_BlockedWhileWorkflowPending` (Theory), `NavigateTo_ServerSetup_AllowedFromLogin`, `Navigated_MenuPageWhileWorkflowPending_NavigatesBack` (Theory), `Navigated_MenuPage_WhenConnected_UpdatesModel`
- `ServerCheckCompleted_Success_EmitsPersistAndAutoLogin`, `…_ResetsSourcesOverviewAndBrowser`

### `ServerSetupContractTests` (`ServerSetupContractTests.cs`)
Quelltext-Vertrag der `ServerSetupPage` (XAML/AutomationIds/Code-Behind), `SettingsPage`-Serverwechsel, `AppShell`-Route `serversetup`, `MauiProgram`-Registrierungen, `ShellAppEffectHandler`-Effekt-Zweige, `DiscoveryProtocol`, `KnownServersService`-Persistenz.
- **Erweiterungspunkt:** neue Pairing-Eingabeelemente (AutomationId), neue Nachrichten/Handler-Zweige.

### `ConnectionWorkflowContractTests` (`ConnectionWorkflowContractTests.cs`)
Quelltext-Vertrag: `LoginPage` (Felder, AutomationIds, Dispatches), `AppShell`-Route `login`, `LoadingPage`-Binding, `HomePage`-Offline-Banner, `MauiProgram`-Auth-Registrierungen, `ShellAppEffectHandler`-Connection-Zweige (`AttemptAutoLoginEffect`, `SessionExpiredMessage` u. a.), `AuthSessionService`-Verdrahtung (`TokenRefresher`, `AppSecretsService`, `api/auth/login`).

### `AppSecretsServiceTests` (`AppSecretsServiceTests.cs`)
- `Secrets_GoToSecretStore_Only`, `Username_GoesToSettingsStore`, `SetAuthToken_RemovesLegacyPreference`, `GetAuthToken_MigratesLegacyPreference`, `ClearCredentials_RemovesAll`, `Secrets_AreProfileIsolated`, `HasCredentials_RequiresUsernameAndPassword`
- **Erweiterungspunkt:** Geräte-Token-Schlüssel (nur `ISecretStore`, Profil-/Server-Präfix).

### `AuthSessionServiceTests` (`AuthSessionServiceTests.cs`)
- `TryStoredLogin_*` (8 Tests), `LoginAsync_*`, `LogoutAsync_*`, `TokenRefresh_*` (inkl. `SessionExpired`-Auslösung bei fehlenden/abgelehnten Zugangsdaten, nicht bei transienten Fehlern).

### `VideoPlayerApiClientTests_Authentication` (`VideoPlayerApiClientTests_Authentication.cs`)
- `LoginAsync_PostsCredentialsAndSetsBearerToken`, `AllRequests_ContainClientGateHeader` (**relevant: beweist heute, dass jeder Request den Gate-Header trägt — der öffentliche Pairing-Endpunkt muss davon ausgenommen werden können**), `Constructor_WithoutBaseAddress_Throws`, `CheckHealthAsync_*`.

### `VideoPlayerApiClientTests_TokenRefresh` (`VideoPlayerApiClientTests_TokenRefresh.cs`)
- `Unauthorized_RefreshesTokenOnce_AndResends`, `…_RefreshFails_Throws`, `…_WithoutRefresher_Throws`, `…_AfterSuccessfulRefresh_Throws`, `Login_SkipsReauthentication`.

### `VideoPlayerApiClientTests_Endpoints` / `…_RetryPolicy`
- `Endpoint_UsesExpectedMethodAndPath` (Theory über alle Endpunkte), Deserialisierungstests, Bearer-Token-Nachweise, Retry-Intervalle/-Fälle (inkl. 429 als wiederholbar).

### `EnumCoverageTests` (`EnumCoverageTests.cs`)
- Deckt `RecentEntryKind`, `PlaybackSourceKind`, `RecentEntryType` und die Legacy-Enums (Quelltext-Vertrag) ab; neue Enum-Werte benötigen eigene Testabdeckung (Git-Hook `enum-coverage-check`).

### `MvuHostTests` (`MvuHostTests.cs`)
- Serialisierte Dispatches, Effektausführung — Rahmen für Pairing-Message-Flow-Tests.

## Hilfsmethoden (Stubs, `VideoWebPlayer.Maui.Tests/Stubs/`)

### `InMemorySecretStore`
- `GetAsync`/`SetAsync`/`RemoveAsync`, `Keys` — In-Memory-`ISecretStore`-Fake; für Geräte-Token-Tests direkt nutzbar.

### `InMemorySettingsStore`
- In-Memory-`ISettingsStore`-Fake.

### `StubHttpMessageHandler`
- `Enqueue(HttpResponseMessage)` / `Enqueue(HttpStatusCode, jsonBody)` / `EnqueueJson` / `EnqueueException` / `Enqueue(responder)` / `EnqueueAsyncResponder` — Antwort-Queue;
- `Requests: List<RecordedRequest>` mit `Method`, `PathAndQuery`, `Authorization`, **`ApiKey` (aufgezeichneter `X-API-Key`-Header)**, `Body` — ideal für den Nachweis „Pairing-Request ohne Gate-Header" und für die camelCase-Payload-Prüfung.

### `RecordingDelays` / `ApiClientTestFactory`
- `RecordingDelays.Delay` zeichnet Retry-Warteintervalle auf; `ApiClientTestFactory.CreateClient(handler, configure)` baut `VideoPlayerApiClient` mit `ApiKey = "test-api-key"` gegen `http://testserver/`.

### `RepositoryRoot`
- `Find()` — findet die Repo-Wurzel (`VideoPlayer.App.sln`) für Quelltext-Contract-Tests.

### `SqliteDatabaseCollection`
- xunit-Collection für SQLite-bezogene Tests (`BackupDatabase`/`CacheDatabase`).
