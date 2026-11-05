# tvOS-App-Rand — Ist-Zustand (`VideoPlayer.TvOS`)

Projekt: `VideoPlayer.TvOS/VideoPlayer.TvOS.csproj` — `net10.0-tvos` hinter
`IncludeTvOsTarget`-Schalter (unter Windows degradierte leere Hülle;
Kompiliernachweis via macOS-CI-Job `build-tvos` in `.github/workflows/pr-checks.yml`).

## Vorhandene Dateien

| Datei | Inhalt |
|-------|--------|
| `Program.cs` | `UIApplication.Main(args, null, typeof(AppDelegate))` |
| `AppDelegate.cs` | `FinishedLaunching`: `AppComposition.Create()` + `CreateRuntime(main)` + `StartupRequestedMessage`; `OnActivated`: `HomeListsReloadRequestedMessage` |
| `AppComposition.cs` | Kompositionswurzel ohne DI: Plattform-Adapter + alle plattformneutralen Dienste inkl. `DevicePairingClient Pairing`, `ServerDiscoveryService ServerDiscovery` (`CreateDefault()` = mDNS + UDP) |
| `MainViewController.cs` | Container: `ShowLoading()`, `Show(UIViewController)` (Embed-/Remove), `UpdateLoadingStatus` |
| `Ui/LoadingViewController.cs` | Ladeanzeige (`UIActivityIndicatorView` + Status-/Hinweistext) |
| `Ui/TvosNavigator.cs` | `Attach`, `NavigateTo(AppPage)` (`Loading`/`ServerSetup`/`Login` als Einzelansicht, Menüseiten via `MenuTabBarController.Select`), `Render(AppModel)` (ruft `NavigateTo`, `UpdateLoadingStatus`, `UpdateStatus` der Zustandsseiten); `RetryStartup` → `StartupRequestedMessage` |
| `Ui/MenuTabBarController.cs` | `UITabBarController` über `AppRoutes.MenuPages`; jede Auswahl → `NavigatedMessage`; `Select(AppPage)` für programmatische Navigation |
| `Ui/PagePlaceholderViewController.cs` | generische Menüseiten (Titel + Untertitel) |
| `Ui/ConnectionStatePageViewController.cs` | Platzhalter der Verbindungsseiten: Titel, Status-/Fehlertext, „Erneut prüfen"-Button → Retry-Callback. **Keine Eingaben, keine Serverlisten.** |
| `Services/TvosAppEffectHandler.cs` | `IAppEffectHandler`: 23 Effekt-Zweige des Verbindungs-Workflows (siehe unten) + `default:`-No-Op mit `LogDebug`; Rückmelde-Verdrahtung `SessionExpired`/`DeviceTokenRejected`/`AvailabilityChanged`/`EventReceived`/`ConnectionClosed` |
| `Services/NSUserDefaultsSettingsStore.cs` | `ISettingsStore` → `NSUserDefaults` |
| `Services/KeychainSecretStore.cs` | `ISecretStore` → `SecKeyChain`/`SecKind.GenericPassword` |
| `Services/TvosStorageLayoutProvider.cs` | `AppStorageLayout` unter `Library/Caches` |
| `Services/TvosNetworkStatusMonitor.cs` | `INetworkStatusMonitor` → `NWPathMonitor` (Wifi/Wired, `NWPathStatus.Satisfied`) |
| `Services/MainThreadInvoker.cs` | Hauptthread-Marshal (`InvokeOnMainThread[Async]`) |
| `Services/AppLogger.cs`, `TvosLoggerFactory.cs` | Logging-Anbindung an `AppLogService`/`FileLoggerProvider` |
| `Info.plist` | `UIDeviceFamily=3`, `NSBonjourServices _http._tcp`, `NSLocalNetworkUsageDescription`, `NSAppTransportSecurity.NSAllowsArbitraryLoads` |

## Vom `TvosAppEffectHandler` bereits ausgeführte Effekte

`InitializeStorageEffect`, `LoadSettingsEffect`, `PersistSettingsEffect`,
`LoadServersEffect`, `DiscoverServersEffect`, `CheckCandidateServerEffect`,
`PersistActiveServerEffect`, `CheckServerHealthEffect`,
`ExchangePairingCodeEffect`, `CheckBootstrapServerEffect`,
`ExchangeBootstrapTicketEffect`, `AttemptAutoLoginEffect`,
`AttemptLoginEffect`, `CheckOfflineDataEffect`,
`StartNetworkMonitoringEffect`, `ConnectServerEventsEffect`,
`DisconnectServerEventsEffect`, `RunFullSyncEffect`,
`SyncContinueWatchingEffect`, `SyncFavoritesEffect`, `SyncMetadataEffect`,
`LoadHomeListsEffect`, `NavigateToPageEffect`.

**Nicht verdrahtet (relevant für diesen Schritt):** `LogoutEffect` (für
„Abmelden" benötigt). **Nicht verdrahtet (nicht Teil dieses Schritts):**
`LoadSourcesEffect` (Quellenmenü folgt in Schritt 5), Download-/Player-/
Playlist-/Protokoll-Effekte (bewusstes No-Op).

## Lücken gegenüber der Anforderung

- `AppPage.ServerSetup`: nur Statustext + Retry — keine Serverlisten
  (bekannt/gefunden), keine Adresseingabe, keine Ticket-/Pairing-/
  Zugangsdaten-Eingabe.
- `AppPage.Login`: nur Statustext + Retry — keine Zugangsdaten-/Kurzcode-
  Eingabe, kein Ausstieg zur Einrichtung.
- Kein Einstieg „Server wechseln" im Menürahmen (`Settings`-Platzhalter)
  und kein „Abmelden" (`Profile`-Platzhalter).
- Keine Fernbedienungs-Rücknavigation vom Einrichtungsdialog zurück zum
  Menürahmen (Siri-Remote-Menü-Taste).
