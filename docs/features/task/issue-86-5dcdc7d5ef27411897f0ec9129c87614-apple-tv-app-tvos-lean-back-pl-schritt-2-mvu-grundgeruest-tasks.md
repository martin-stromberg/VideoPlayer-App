# Tasks: tvOS-App — MVU-Grundgerüst, Plattform-Anbindung, Navigation und Fokus

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Verschiebung | `IServerAddressProvider`/`ServerAddressProvider`/`IServerHealthProbe`/`ServerHealthProbe`/`SignalRServerEventsConnection` von `VideoPlayer.Maui/Services/` nach `VideoPlayer.Mvu/` (Namespace `VideoPlayer.Mvu`) | Offen | — |
| 2 | Konfiguration | `Microsoft.AspNetCore.SignalR.Client` von `VideoPlayer.Maui.csproj` nach `VideoPlayer.Mvu.csproj` verschieben | Offen | — |
| 3 | Tests | Pfad-Assertions in `NetworkSyncContractTests`, `PairingContractTests`, `ServerSetupContractTests`, `SettingsStorageContractTests`, `VideoPlayerMauiContractTests` anpassen | Offen | — |
| 4 | Plattform-Adapter | `NSUserDefaultsSettingsStore` (`ISettingsStore`/`NSUserDefaults`) anlegen | Offen | — |
| 5 | Plattform-Adapter | `KeychainSecretStore` (`ISecretStore`/`SecKeyChain` GenericPassword) anlegen | Offen | — |
| 6 | Plattform-Adapter | `TvosStorageLayoutProvider` (`Library/Caches`-Wurzeln + Verzeichnisse) anlegen | Offen | — |
| 7 | Plattform-Adapter | `TvosNetworkStatusMonitor` (`INetworkStatusMonitor`/`NWPathMonitor`) anlegen | Offen | — |
| 8 | Plattform-Adapter | `TvosLoggerFactory` + `AppLogger<T>` (ILoggerFactory/ILogger<T>-Adapter) anlegen | Offen | — |
| 9 | Plattform-Adapter | `MainThreadInvoker` (`InvokeOnMainThreadAsync` via `DispatchQueue.MainQueue`) anlegen | Offen | — |
| 10 | Effekt-Handler | `TvosAppEffectHandler` anlegen: Start-/Verbindungs-Effekte (~22 Zweige) + `default:`-No-Op + Ereignis-Abonnements | Offen | — |
| 11 | UI | `LoadingViewController` (Indikator + `StartupStatusText`) anlegen | Offen | — |
| 12 | UI | `ConnectionStatePageViewController` (Titel/Fehlertext/„Erneut prüfen"-Button) anlegen | Offen | — |
| 13 | UI | `PagePlaceholderViewController` (Titel+Untertitel je Menüseite) anlegen | Offen | — |
| 14 | UI | `MenuTabBarController` (`UITabBarController`, `DidSelect`→`NavigatedMessage`) anlegen | Offen | — |
| 15 | UI | `TvosNavigator` (modellgetriebene Abbildung `CurrentPage`→UI) anlegen | Offen | — |
| 16 | UI | `MainViewController` zum Kind-Container umbauen | Offen | — |
| 17 | Komposition | `AppComposition` (manuelle Dienst-Verdrahtung, `MvuHost`) anlegen | Offen | — |
| 18 | Lebenszyklus | `AppDelegate`: Komposition, `StartupRequestedMessage`, `OnActivated`→`HomeListsReloadRequestedMessage` | Offen | — |
| 19 | Tests | `TvOsAppContractTests` anlegen (Adapter, Effekt-Abdeckung, Komposition, Delegate, UI-Rahmen, Verschiebung, keine Schreib-Effekte) | Offen | — |
| 20 | Validierung | `dotnet build` Solution + `dotnet test` `VideoPlayer.Maui.Tests` grün; `dotnet format whitespace` | Offen | — |
| 21 | Dokumentation | `docs/help/tv-app/` erweitern, `changes.log`, `README.md` | Offen | — |
