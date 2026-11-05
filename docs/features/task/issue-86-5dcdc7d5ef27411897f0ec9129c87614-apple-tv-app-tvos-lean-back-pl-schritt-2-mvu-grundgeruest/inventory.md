# Bestandsaufnahme: tvOS-App — MVU-Grundgerüst, Plattform-Anbindung, Navigation und Fokus

Inventar des Ist-Stands zur Anforderung `requirement.md` (Entwicklungsschritt 2
der Issue-#86-Projektmappe). Ergänzt die projektweite Bestandsaufnahme unter
`docs/projects/task/issue-86-5dcdc7d5ef27411897f0ec9129c87614-apple-tv-app-tvos-lean-back-pl/inventory/`
— deren Ergebnisse (Modelle, Dienste, API-Client, CI) gelten weiterhin und
werden hier nicht wiederholt. Keine Planung, keine Implementierungsvorschläge.

## Zusammenfassung

- **tvOS-Projekt (Schritt 1):** `VideoPlayer.TvOS/` enthält `Program.cs`
  (`UIApplication.Main` → `AppDelegate`), `AppDelegate.cs` (Fenster +
  `MainViewController`), `MainViewController.cs` (dunkle Startansicht „Heimkino
  TV", keine Funktion) und `Info.plist` (Bonjour-/ATS-Schlüssel). TFM
  `net10.0-tvos` hinter `IncludeTvOsTarget` (Default: nur macOS); unter Windows
  degradiert das Projekt zu einer leeren `net10.0`-Bibliothek ohne Quelldateien
  (`EnableDefaultCompileItems=false`) — tvOS-Code kann lokal nicht kompiliert
  werden.
- **MVU-Kern komplett plattformneutral** in `VideoPlayer.Mvu` (`net10.0`):
  `MvuHost` serialisiert Dispatchs per `SemaphoreSlim`-Gate und führt Effekte
  sequenziell über `IAppEffectHandler` aus. `AppUpdate.Update` bildet ~135
  Message-Typen auf `AppModel` + ~75 `AppEffect`-Subtypen ab.
- **Start-Workflow** (`StartupRequestedMessage`): Effekte
  `InitializeStorage` → `LoadSettings` → `LoadServers` →
  `StartNetworkMonitoring` → `LoadHomeLists`; Folgekette über die Handler-
  Rückmeldungen: `ServersLoadedMessage` → `CheckServerHealthEffect` →
  `ServerHealthCheckCompletedMessage` → je nach Ergebnis
  `AttemptAutoLoginEffect` (→ `ConnectServerEventsEffect` +
  `RunFullSyncEffect` + `NavigateToPageEffect(Home)`) oder
  `CheckOfflineDataEffect` bzw. `NavigateToPageEffect(ServerSetup|Login|Home)`.
- **Plattform-Anschlüsse fehlen tvOS-seitig:** `ISettingsStore` (MAUI:
  `PreferencesSettingsStore`/`Preferences`), `ISecretStore` (MAUI:
  `SecureStorageSecretStore`/`SecureStorage`), `AppStorageLayout`-Wurzeln
  (MAUI: `AppStorageLayoutProvider`/`FileSystem`), `INetworkStatusMonitor`
  (MAUI: `ConnectivityNetworkStatusMonitor`/`Connectivity`) existieren nur als
  MAUI-Implementierungen — tvOS braucht eigene Adapter (`NSUserDefaults`,
  `SecItem`/Schlüsselbund, `NSFileManager`-Verzeichnisse, `NWPathMonitor`).
- **Faktisch plattformneutral, aber in `VideoPlayer.Maui` angesiedelt:**
  `IServerAddressProvider`/`ServerAddressProvider` (inkl.
  `VIDEOPLAYER_SERVER_URL`-Fallback), `IServerHealthProbe`/`ServerHealthProbe`
  (nur ApiClient+Storage+Logging) und `SignalRServerEventsConnection` (nur
  SignalR-Client + `IServerEventsConnection`). Ohne Verschiebung kann tvOS sie
  nicht nutzen; Contract-Tests prüfen aktuell deren Dateiort
  (`PairingContractTests`, `ServerSetupContractTests`,
  `VideoPlayerMauiContractTests`, `NetworkSyncContractTests`).
- **Kein DI-Container im tvOS-Projekt:** `MauiProgram` verdrahtet alle Dienste
  über `Microsoft.Extensions.DependencyInjection`; das tvOS-Projekt referenziert
  bisher keinerlei NuGet-Pakete (Contract-Test
  `TvOsProjectContractTests.TvOsProject_HasNoPackageReferencesOrMaui`).
- **Navigation/Bedienung:** tvOS-Focus-Engine steuert die Siri Remote;
  `UITabBarController` liefert Menü+Inhaltsrahmen ab Werk. `AppRoutes.MenuPages`
  definiert sechs Menüseiten; `ServerSetup`/`Login` sind routenlose
  Zustandsseiten (Window-Root-Wechsel beim MAUI-Pendant).
- **tvOS-Speicher-Realität:** kein dauerhaftes Dateisystem — Benutzer-
  Standardwerte (`NSUserDefaults`, ~500 KB) und Schlüsselbund persistieren,
  `Library/Caches` ist der einzige größere Ablageort und jederzeit räumbar
  (kein `FileSystem.AppDataDirectory`-Äquivalent, kein Backup-/SkipBackup-
  Konzept).
- **Tests:** `VideoPlayer.Maui.Tests` (xunit.v3, `net10.0`) enthält
  `TvOsProjectContractTests` (prüft Solution-Mitgliedschaft, Referenzmenge,
  TFM-Schalter, CI-Job, leeres Projektgerüst) sowie die plattformneutrale
  `AppUpdateTests_*`-Suite (~1440 Tests beim Projekt-Inventory). Neue
  Verdrahtung ist Windows-seitig nur über Contract-Tests (Quelltext-Assertions)
  absicherbar; Laufzeitverhalten des MVU-Kerns ist bereits durch die
  bestehende Suite abgedeckt.
- **tvOS-Bindings (recherchiert, dotnet/macios):** `Network.NWPathMonitor`
  (`SnapshotHandler: Action<NWPath>`, `SetQueue(DispatchQueue)`, `Start()`,
  `Cancel()`, `CurrentPath`), `NWPath.Status` (`NWPathStatus.Satisfied`),
  `NWPath.UsesInterfaceType(NWInterfaceType.Wifi|.Wired)`; `Security.SecItem`
  (`Add`/`Delete`/`QueryAsData`/`QueryAsRecord` mit `SecRecord(SecKind.
  GenericPassword)`); `Foundation.NSUserDefaults.StandardUserDefaults`.

## Details

- [Interfaces](inventory/interfaces.md) — Plattform- und Dienst-Contracts, Standort, tvOS-Lücke
- [Logik](inventory/logic.md) — `MvuHost`, `AppUpdate`-Startfluss, `ShellAppEffectHandler`-Muster, `MauiProgram`-Komposition, tvOS-Einstieg
- [Modelle und Enums](inventory/models.md) — `AppModel`-Startzustand, `AppPage`, `ConnectionState`, `AppRoutes`
- [Tests](inventory/tests.md) — tvOS-Contract-Tests, Startup-Update-Tests, Tests mit Pfad-Assertions auf zu verschiebende Dateien
