# Tasks: Serververwaltung und Erststart-Einrichtung

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Datenmodell | `ServerAddress` (Normalisierung `TryNormalize`/`Normalize`) in `VideoPlayer.Storage` anlegen | Erledigt | `ServerAddressTests` |
| 2 | Datenmodell | `KnownServer`-Record in `VideoPlayer.Storage` anlegen | Erledigt | `KnownServersServiceTests` |
| 3 | Logik | `KnownServersService` in `VideoPlayer.Storage` anlegen (`GetAllAsync`, `SetActiveServerAsync`, `GetLastConnectedAddressAsync`, `MigrateLegacyServerAddressAsync`) | Erledigt | `KnownServersServiceTests` |
| 4 | Logik | `DiscoveryProtocol` (Konstanten + `TryParseResponse`) in `VideoPlayer.ApiClient.Discovery` anlegen | Erledigt | `DiscoveryProtocolTests` |
| 5 | Datenmodell | `DiscoveredServer`-Record + `DiscoverySource`-Enum anlegen | Erledigt | `ServerDiscoveryServiceTests` |
| 6 | Logik | `IServerDiscovery`-Interface anlegen | Erledigt | `ServerDiscoveryServiceTests` (Fake-Implementierung) |
| 7 | Logik | `UdpServerDiscovery` implementieren (Broadcast, Responder-Normalisierung, Test-Konstruktor) | Erledigt | `UdpServerDiscoveryTests` (Loopback-Roundtrip) |
| 8 | Logik | `MdnsServerDiscovery` implementieren (Zeroconf `_http._tcp.local.`; Paketreferenz `Zeroconf 3.7.16` in `VideoPlayer.ApiClient.csproj`) | Erledigt | Contract-Test `Discovery_UsesDocumentedProtocol` |
| 9 | Logik | `ServerDiscoveryService` (Composite, Merge + Dedupe) anlegen | Erledigt | `ServerDiscoveryServiceTests` |
| 10 | MVU | `AppPage.ServerSetup` ergänzen | Erledigt | `AppUpdateTests_Startup` |
| 11 | MVU | `AppModel` erweitern (`KnownServers`, `ActiveServerAddress`, `DiscoveredServers`, `IsDiscoveryRunning`, `DiscoveryAttempted`, `IsServerCheckRunning`, `ServerSetupError`); Referenz `VideoPlayer.Mvu` → `VideoPlayer.ApiClient` ergänzen | Erledigt | `AppUpdateTests_ServerManagement` |
| 12 | MVU | Neue Messages in `AppMessage.cs` (`ServersLoaded`, `LoadServersRequested`, `ServerDiscoveryRequested`, `ServerDiscoveryCompleted`, `ServerAddressSubmitted`, `DiscoveredServerSelected`, `ServerSwitchRequested`, `ServerCheckCompleted`) | Erledigt | `AppUpdateTests_ServerManagement` |
| 13 | MVU | Neue Effekte in `AppEffect.cs` (`LoadServersEffect`, `DiscoverServersEffect`, `CheckCandidateServerEffect`, `PersistActiveServerEffect`) | Erledigt | `AppUpdateTests_ServerManagement` |
| 14 | MVU | `AppUpdate` erweitern (Start-Verzweigung, Discovery, Prüf-/Persist-/Navigationspfade) | Erledigt | `AppUpdateTests_ServerManagement`, `AppUpdateTests_Startup` |
| 15 | Logik | `IServerAddressProvider`/`ServerAddressProvider` auf async + `KnownServersService` umstellen (`GetConfiguredServerAddressAsync`, `GetServerAddressAsync`, Env-/Default-Fallback) | Erledigt | `ServerAddressProvider_UsesKnownServersService` |
| 16 | Logik | `IServerHealthProbe`/`ServerHealthProbe` um `CheckAsync(Uri, ct)`-Overload erweitern | Erledigt | `NewApp_HealthCheckWiredToApiClient`, `EffectHandler_HandlesAllServerEffects` |
| 17 | Logik | `ShellAppEffectHandler` um die vier neuen Effekte + `ServerSetup`-Navigation erweitern | Erledigt | `EffectHandler_HandlesAllServerEffects` |
| 18 | UI | `ServerSetupPage.xaml(.cs)` erstellen (bekannte Server, „Automatisch suchen", Ergebnisliste, manuelle Eingabe, Status-/Fehleranzeige, AutomationIds) | Erledigt | `ServerSetupContractTests`, Windows-Build + Smoke-Run |
| 19 | UI | `SettingsPage`: unauffälliger Eintrag „Server wechseln" + Dispatch `NavigateToMessage(ServerSetup)` | Erledigt | `SettingsPage_ContainsDiscreetServerSwitchEntry` |
| 20 | Konfiguration | `AppShell.xaml.cs`: `Routing.RegisterRoute("serversetup", ...)`; `MauiProgram`: `KnownServersService`, Discovery, `ServerSetupPage` registrieren | Erledigt | `AppShell_RegistersServerSetupRoute_WithoutFlyoutItem`, `MauiProgram_RegistersServerManagementServices` |
| 21 | Tests | `ServerAddressTests` anlegen | Erledigt | — |
| 22 | Tests | `KnownServersServiceTests` anlegen (Upsert, Dedupe, LastConnected, Migration, Ablageklassifikation) | Erledigt | — |
| 23 | Tests | `DiscoveryProtocolTests` anlegen | Erledigt | — |
| 24 | Tests | `UdpServerDiscoveryTests` anlegen (Loopback-Roundtrip, Fremd-Datagramme, Responder-Normalisierung) | Erledigt | — |
| 25 | Tests | `ServerDiscoveryServiceTests` anlegen (Merge, Dedupe, Fehlerrobustheit) | Erledigt | — |
| 26 | Tests | `AppUpdateTests_ServerManagement` anlegen (alle Verzweigungen und Flüsse) | Erledigt | — |
| 27 | Tests | `ServerSetupContractTests` anlegen (XAML-Controls, Dispatch-Verdrahtung, Route, DI, sechs FlyoutItems) | Erledigt | — |
| 28 | Tests | `AppUpdateTests_Startup` + `MvuHostTests` an neue Effektliste anpassen | Erledigt | — |
| 29 | Tests | `SettingsStorageContractTests` anpassen/erweitern (Provider, Effekt-Handler) | Erledigt | — |
| 30 | Verifikation | Discovery-Verifikation dokumentieren + Server-Issues #223/#224 gemeldet | Erledigt | `inventory/discovery-verification.md` |
