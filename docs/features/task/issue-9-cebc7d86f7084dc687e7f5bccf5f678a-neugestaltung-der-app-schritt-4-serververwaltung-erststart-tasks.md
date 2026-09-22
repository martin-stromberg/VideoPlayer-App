# Tasks: Serververwaltung und Erststart-Einrichtung

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Datenmodell | `ServerAddress` (Normalisierung `TryNormalize`/`Normalize`) in `VideoPlayer.Storage` anlegen | Offen | — |
| 2 | Datenmodell | `KnownServer`-Record in `VideoPlayer.Storage` anlegen | Offen | — |
| 3 | Logik | `KnownServersService` in `VideoPlayer.Storage` anlegen (`GetAllAsync`, `SetActiveServerAsync`, `GetLastConnectedAddressAsync`, `MigrateLegacyServerAddressAsync`) | Offen | — |
| 4 | Logik | `DiscoveryProtocol` (Konstanten + `TryParseResponse`) in `VideoPlayer.ApiClient.Discovery` anlegen | Offen | — |
| 5 | Datenmodell | `DiscoveredServer`-Record + `DiscoverySource`-Enum anlegen | Offen | — |
| 6 | Logik | `IServerDiscovery`-Interface anlegen | Offen | — |
| 7 | Logik | `UdpServerDiscovery` implementieren (Broadcast, Responder-Normalisierung, Test-Konstruktor) | Offen | — |
| 8 | Logik | `MdnsServerDiscovery` implementieren (Zeroconf `_http._tcp.local.`; Paketreferenz `Zeroconf 3.7.16` in `VideoPlayer.ApiClient.csproj`) | Offen | — |
| 9 | Logik | `ServerDiscoveryService` (Composite, Merge + Dedupe) anlegen | Offen | — |
| 10 | MVU | `AppPage.ServerSetup` ergänzen | Offen | — |
| 11 | MVU | `AppModel` erweitern (`KnownServers`, `ActiveServerAddress`, `DiscoveredServers`, `IsDiscoveryRunning`, `IsServerCheckRunning`, `ServerSetupError`); Referenz `VideoPlayer.Mvu` → `VideoPlayer.ApiClient` ergänzen | Offen | — |
| 12 | MVU | Neue Messages in `AppMessage.cs` (`ServersLoaded`, `LoadServersRequested`, `ServerDiscoveryRequested`, `ServerDiscoveryCompleted`, `ServerAddressSubmitted`, `DiscoveredServerSelected`, `ServerSwitchRequested`, `ServerCheckCompleted`) | Offen | — |
| 13 | MVU | Neue Effekte in `AppEffect.cs` (`LoadServersEffect`, `DiscoverServersEffect`, `CheckCandidateServerEffect`, `PersistActiveServerEffect`) | Offen | — |
| 14 | MVU | `AppUpdate` erweitern (Start-Verzweigung, Discovery, Prüf-/Persist-/Navigationspfade) | Offen | — |
| 15 | Logik | `IServerAddressProvider`/`ServerAddressProvider` auf async + `KnownServersService` umstellen (`GetConfiguredServerAddressAsync`, `GetServerAddressAsync`, Env-/Default-Fallback) | Offen | — |
| 16 | Logik | `IServerHealthProbe`/`ServerHealthProbe` um `CheckAsync(Uri, ct)`-Overload erweitern | Offen | — |
| 17 | Logik | `ShellAppEffectHandler` um die vier neuen Effekte + `ServerSetup`-Navigation erweitern | Offen | — |
| 18 | UI | `ServerSetupPage.xaml(.cs)` erstellen (bekannte Server, „Automatisch suchen", Ergebnisliste, manuelle Eingabe, Status-/Fehleranzeige, AutomationIds) | Offen | — |
| 19 | UI | `SettingsPage`: unauffälliger Eintrag „Server wechseln" (`SwitchServerLink`) + Dispatch `NavigateToMessage(ServerSetup)` | Offen | — |
| 20 | Konfiguration | `AppShell.xaml.cs`: `Routing.RegisterRoute("serversetup", ...)`; `MauiProgram`: `KnownServersService`, Discovery, `ServerSetupPage` registrieren | Offen | — |
| 21 | Tests | `ServerAddressTests` anlegen | Offen | — |
| 22 | Tests | `KnownServersServiceTests` anlegen (Upsert, Dedupe, LastConnected, Migration, Profil-Isolation) | Offen | — |
| 23 | Tests | `DiscoveryProtocolTests` anlegen | Offen | — |
| 24 | Tests | `UdpServerDiscoveryTests` anlegen (Loopback-Roundtrip, Fremd-Datagramme, Responder-Normalisierung) | Offen | — |
| 25 | Tests | `ServerDiscoveryServiceTests` anlegen (Merge, Dedupe, Fehlerrobustheit) | Offen | — |
| 26 | Tests | `AppUpdateTests_ServerSetup` anlegen (alle Verzweigungen und Flüsse) | Offen | — |
| 27 | Tests | `ServerSetupContractTests` anlegen (XAML-Controls, Dispatch-Verdrahtung, Route, DI, sechs FlyoutItems) | Offen | — |
| 28 | Tests | `AppUpdateTests_Startup` + `MvuHostTests` an neue Effektliste anpassen | Offen | — |
| 29 | Tests | `SettingsStorageContractTests` anpassen/erweitern (Provider, Effekt-Handler) | Offen | — |
| 30 | Verifikation | Discovery-Verifikation dokumentieren + Server-Issues #223/#224 gemeldet | Erledigt | `inventory/discovery-verification.md` |
