# Tasks: tvOS-App — MVU-Grundgerüst, Plattform-Anbindung, Navigation und Fokus

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Verschiebung | `IServerAddressProvider`/`ServerAddressProvider`/`IServerHealthProbe`/`ServerHealthProbe`/`SignalRServerEventsConnection` von `VideoPlayer.Maui/Services/` nach `VideoPlayer.Mvu/` (Namespace `VideoPlayer.Mvu`) | Erledigt | `TvOsAppContractTests.PlatformNeutralServices_LiveInMvu` |
| 2 | Konfiguration | `Microsoft.AspNetCore.SignalR.Client` von `VideoPlayer.Maui.csproj` nach `VideoPlayer.Mvu.csproj` verschieben | Erledigt | `TvOsAppContractTests.PlatformNeutralServices_LiveInMvu` |
| 3 | Tests | Pfad-Assertions in `NetworkSyncContractTests`, `PairingContractTests`, `ServerSetupContractTests`, `SettingsStorageContractTests`, `VideoPlayerMauiContractTests` anpassen | Erledigt | Testsuite 1421/1421 grün |
| 4 | Plattform-Adapter | `NSUserDefaultsSettingsStore` (`ISettingsStore`/`NSUserDefaults`) anlegen | Erledigt | `TvOsAppContractTests.TvOsProject_ProvidesPlatformAdapters` |
| 5 | Plattform-Adapter | `KeychainSecretStore` (`ISecretStore`/`SecKeyChain` GenericPassword) anlegen | Erledigt | `TvOsAppContractTests.TvOsProject_ProvidesPlatformAdapters` |
| 6 | Plattform-Adapter | `TvosStorageLayoutProvider` (`Library/Caches`-Wurzeln + Verzeichnisse) anlegen | Erledigt | `TvOsAppContractTests.TvOsProject_ProvidesPlatformAdapters` |
| 7 | Plattform-Adapter | `TvosNetworkStatusMonitor` (`INetworkStatusMonitor`/`NWPathMonitor`) anlegen | Erledigt | `TvOsAppContractTests.TvOsProject_ProvidesPlatformAdapters` |
| 8 | Plattform-Adapter | `TvosLoggerFactory` + `AppLogger<T>` (ILoggerFactory/ILogger<T>-Adapter) anlegen | Erledigt | `TvOsAppContractTests.TvOsApp_CompositionWiresCoreServices` (Verdrahtung) |
| 9 | Plattform-Adapter | `MainThreadInvoker` (`InvokeOnMainThreadAsync` via `DispatchQueue.MainQueue`) anlegen | Erledigt | Kein direkter Test (wird vom Handler genutzt) |
| 10 | Effekt-Handler | `TvosAppEffectHandler` anlegen: Start-/Verbindungs-Effekte (~22 Zweige) + `default:`-No-Op + Ereignis-Abonnements | Erledigt | `TvOsAppContractTests.TvOsEffectHandler_CoversStartupAndConnectionEffects`, `TvOsEffectHandler_DoesNotWireExcludedAreas` |
| 11 | UI | `LoadingViewController` (Indikator + `StartupStatusText`) anlegen | Erledigt | `TvOsAppContractTests.TvOsUi_UsesTabBarAndModelDrivenNavigation` |
| 12 | UI | `ConnectionStatePageViewController` (Titel/Fehlertext/„Erneut prüfen"-Button) anlegen | Erledigt | `TvOsAppContractTests.TvOsUi_UsesTabBarAndModelDrivenNavigation` |
| 13 | UI | `PagePlaceholderViewController` (Titel+Untertitel je Menüseite) anlegen | Erledigt | `TvOsAppContractTests.TvOsUi_UsesTabBarAndModelDrivenNavigation` |
| 14 | UI | `MenuTabBarController` (`UITabBarController`, `DidSelect`→`NavigatedMessage`) anlegen | Erledigt | `TvOsAppContractTests.TvOsUi_UsesTabBarAndModelDrivenNavigation` |
| 15 | UI | `TvosNavigator` (modellgetriebene Abbildung `CurrentPage`→UI) anlegen | Erledigt | `TvOsAppContractTests.TvOsUi_UsesTabBarAndModelDrivenNavigation` |
| 16 | UI | `MainViewController` zum Kind-Container umbauen | Erledigt | `TvOsAppContractTests.TvOsUi_UsesTabBarAndModelDrivenNavigation` |
| 17 | Komposition | `AppComposition` (manuelle Dienst-Verdrahtung, `MvuHost`) anlegen | Erledigt | `TvOsAppContractTests.TvOsApp_CompositionWiresCoreServices` |
| 18 | Lebenszyklus | `AppDelegate`: Komposition, `StartupRequestedMessage`, `OnActivated`→`HomeListsReloadRequestedMessage` | Erledigt | `TvOsAppContractTests.TvOsApp_DelegateStartsMvuLoop` |
| 19 | Tests | `TvOsAppContractTests` anlegen (Adapter, Effekt-Abdeckung, Komposition, Delegate, UI-Rahmen, Verschiebung, keine Schreib-Effekte) | Erledigt | Testsuite 1421/1421 grün |
| 20 | Validierung | `dotnet build` + `dotnet test` `VideoPlayer.Maui.Tests` grün; `dotnet format whitespace` | Erledigt | `test-results.md` |
| 21 | Dokumentation | `docs/help/tv-app/` erweitern, `changes.log`, `README.md` | Erledigt | — |
