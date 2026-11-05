# Bestehende Tests — Bestandsaufnahme

Testsuite: `VideoPlayer.Maui.Tests` (`net10.0`, xUnit; läuft unter
Windows). tvOS-Code kann nicht kompiliert werden — die Absicherung
erfolgt über Quell-Contract-Tests, die die tvOS-Quelldateien lesen
(Muster: `RepositoryRoot.Find()` + `File.ReadAllText`).

## Verbindungs-Workflow (plattformneutral, bereits abgedeckt)

### `VideoPlayer.Maui.Tests/AppUpdateTests_Connection.cs`
- Start-/Verbindungs-Übergänge, Erreichbarkeitsprüfung, automatische
  Anmeldung, Offline-Erkennung, Serverwechsel, Sitzungsablauf.

### `VideoPlayer.Maui.Tests/AppUpdateTests_ServerManagement.cs`
- `LoadServers*`/`ServersLoadedMessage`, Discovery-Messages,
  Kandidatenprüfung, `ServerSwitchRequestedMessage`,
  `ServerAddressSubmittedMessage` (Normalisierung/Fehler).

### `VideoPlayer.Maui.Tests/AppUpdateTests_Pairing.cs`,
`PairingContractTests.cs`, `PairingCodeTests.cs`,
`DevicePairingClientTests.cs`, `DevicePairingClientBootstrapTests.cs`,
`BootstrapContractTests.cs`, `BootstrapTicketParserTests.cs`,
`DeviceTokenServiceTests.cs`
- Pairing-/Bootstrap-Übergänge, Fehlerstatus-Mapping, Token-Verwaltung.

### `VideoPlayer.Maui.Tests/AuthSessionServiceTests.cs`,
`AuthSessionServiceRefreshTests.cs`, `VideoPlayerApiClientTests_Authentication.cs`,
`VideoPlayerApiClientTests_TokenRefresh.cs`,
`VideoPlayerApiClientTests_SessionRefresh.cs`
- Anmeldung, stille Neuanmeldung, Refresh-Rotation, `SessionExpired`.

### `VideoPlayer.Maui.Tests/ConnectionWorkflowContractTests.cs`,
`ServerSetupContractTests.cs`, `ServerDiscoveryServiceTests.cs`,
`UdpServerDiscoveryTests.cs`, `KnownServersServiceTests.cs`,
`ServerAddressTests.cs`, `DiscoveryProtocolTests.cs`
- Verträge des Verbindungsflusses, Serververwaltung, Erkennung.

## tvOS-Contract-Tests (Quell-Tests)

### `VideoPlayer.Maui.Tests/TvOsProjectContractTests.cs`
- Solution-Mitgliedschaft, Referenzmenge, `IncludeTvOsTarget`-Schalter,
  Einstieg (`UIApplication.Main`, `MakeKeyAndVisible`), CI-Job `build-tvos`.

### `VideoPlayer.Maui.Tests/TvOsAppContractTests.cs`
- `TvOsProject_ProvidesPlatformAdapters` — NSUserDefaults/Keychain/
  NWPathMonitor-Verwendung.
- `TvOsEffectHandler_CoversStartupAndConnectionEffects` — alle 23
  Effekt-Zweige + Ergebnis-Messages + Ereignis-Abonnements.
- `TvOsEffectHandler_DoesNotWireExcludedAreas` — keine Verwaltungs-/
  Download-/PiP-/Protokoll-Effekte, kein `MediaElement`.
- `TvOsApp_CompositionWiresCoreServices` — Kompositionswurzel.
- `TvOsApp_DelegateStartsMvuLoop` — Lebenszyklus-Verdrahtung.
- `TvOsUi_UsesTabBarAndModelDrivenNavigation` — `UITabBarController`,
  `AppRoutes.MenuPages`, `NavigatedMessage`, `AppPage.ServerSetup`,
  `AppPage.Login`, `UIActivityIndicatorView`, `UIButton`
  (`ConnectionStatePageViewController.cs`), `AddChildViewController`.
- `PlatformNeutralServices_LiveInMvu` — verschobene Dienste +
  SignalR-Paket.

## Hilfsklassen

- `VideoPlayer.Maui.Tests/Stubs/RepositoryRoot.cs` — `Find()` liefert den
  Repository-Root der Quelldateien.

## E2E

- `VideoPlayer.E2E.Tests` (`net10.0-windows`, FlaUI): spielt den
  Verbindungsfluss gegen die MAUI-App durch — für tvOS existiert kein
  Windows-ausführbarer E2E-Pfad; die tvOS-Lauffähigkeit prüft der
  macOS-CI-Job (Simulator-Build).
