# Umsetzungsplan: tvOS-App — MVU-Grundgerüst, Plattform-Anbindung, Navigation und Fokus

## Übersicht

Die tvOS-App `VideoPlayer.TvOS` wird an den plattformneutralen MVU-Kern
(`MvuHost`/`AppUpdate`/`AppModel`/`AppEffect`) angeschlossen: ein
tvOS-seitiger `IAppEffectHandler` führt die Effekte des Start- und
Verbindungs-Workflows am App-Rand aus, neue Plattform-Adapter liefern
`ISettingsStore` (`NSUserDefaults`), `ISecretStore` (Schlüsselbund),
`AppStorageLayout`-Wurzeln und `INetworkStatusMonitor` (`NWPathMonitor`).
Fünf faktisch plattformneutrale Dienste wandern von `VideoPlayer.Maui` nach
`VideoPlayer.Mvu`, damit tvOS sie nutzen kann. Ein `UITabBarController`-
Seitenrahmen bildet Menü und Inhaltsbereich ab; die navigierten Zustände
(Ladeanzeige, Verbindungszustände) sind modellgetrieben.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Seitenrahmen | `UITabBarController` mit einem `UIViewController` je `AppRoutes.MenuPages`-Eintrag | tvOS-idiomatisch: Tab-Leiste + Inhalt sind ohne Zusatzcode per Siri Remote fokussierbar (Focus-Engine); `SelectedIndex`-Mapping entspricht dem Shell-Flyout der MAUI-App |
| Plattformneutrale MAUI-Dienste | Verschiebung von `VideoPlayer.Maui/Services/` nach `VideoPlayer.Mvu/` (Namespace `VideoPlayer.Mvu`): `IServerAddressProvider`, `ServerAddressProvider`, `IServerHealthProbe`, `ServerHealthProbe`, `SignalRServerEventsConnection` | Kein Duplikat, tvOS nutzt denselben bewährten Code (inkl. adressspezifischer Gate-Schlüssel-Auflösung); Mvu referenziert bereits Storage+ApiClient und beherbergt die anderen App-Dienste. Vertragsreferenz: `Microsoft.AspNetCore.SignalR.Client` wandert mit nach `VideoPlayer.Mvu.csproj` |
| Logging ohne DI-Container | Eigene `TvosLoggerFactory : ILoggerFactory` über `FileLoggerProvider` + `AppLogger<T> : ILogger<T>`-Adapter | `LoggerFactory.Create` liegt im Paket `Microsoft.Extensions.Logging` — ein Paketverweis würde den tvOS-Contract (`TvOsProject_HasNoPackageReferencesOrMaui`) brechen; Adapter sind ~60 Zeilen |
| tvOS-Ablagewurzeln | `TvosStorageLayoutProvider` löst alle drei Wurzeln unter `Library/Caches` auf (`NSFileManager.GetUrl(NSSearchPathDirectory.CachesDirectory)`): `videoplayer/data`, `videoplayer/nobackup`, `videoplayer/cache` | tvOS hat kein Backup-/`NoBackupFilesDir`-Konzept; `Library/Caches` ist der einzige größere Ablageort (jederzeit systemräumbar — dokumentierte Einschränkung); die drei Unterverzeichnisse bleiben getrennt, damit die `AppStorageLayout`-Semantik erhalten bleibt |
| `ISecretStore` | `KeychainSecretStore` über `Security.SecKeyChain` + `SecRecord(SecKind.GenericPassword)` (`Service` = Bundle-Id `de.martinstromberg.videos.tv`, `Account` = Schlüssel) | Schlüsselbund-Pendant zu `SecureStorage`; Fehler beim Lesen → `null`, leerer Wert beim Setzen → `Remove` (Semantik von `SecureStorageSecretStore`) |
| `ISettingsStore` | `NSUserDefaultsSettingsStore` über `NSUserDefaults.StandardUserDefaults` | Plattform-Äquivalent zu `Preferences`; bleibt unter dem tvOS-Limit (~500 KB) |
| `INetworkStatusMonitor` | `TvosNetworkStatusMonitor` über `Network.NWPathMonitor` (`SnapshotHandler`, `SetQueue(DefaultGlobalQueue)`, `Start()`); `IsOnline` = `Status == Satisfied`; `IsWifi` = `UsesInterfaceType(Wifi \|\| Wired)` | NWPathMonitor ist die moderne Apple-API und auch die MAUI-Reachability-Grundlage; Ethernet zählt wie im MAUI-Adapter als ungedrosselt |
| Effekt-Abdeckung | `TvosAppEffectHandler` führt die ~22 Effekte des Start-/Verbindungs-Workflows aus (inkl. Pairing-/Bootstrap-/Login-Austausch — Schritt 3 liefert dann nur noch die Oberfläche); alle übrigen Effekte laufen in einen `default:`-Zweig mit `LogDebug`-No-Op | „Nicht verdrahtete Bereiche erzeugen keine Fehler"; keine Schreibzugriffe auf verwaltete Inhalte |
| Dispatch zurück in den MVU-Zyklus | `InvokeOnMainThreadAsync(Func<Task>)`-Helfer über `DispatchQueue.MainQueue` + `TaskCompletionSource` | Pendant zu `MainThread.InvokeOnMainThreadAsync` — UIKit-Objekte sind main-thread-affin; der MvuHost serialisiert Dispatchs selbst |
| Navigation | `TvosNavigator` abonniert `MvuHost.ModelChanged` und stellt `model.CurrentPage` idempotent dar (Loading/ServerSetup/Login als Einzelansicht, Menüseiten als Tab-Auswahl); `NavigateToPageEffect` ruft denselben Pfad; `UITabBarController.DidSelect` dispatcht `NavigatedMessage` | UI bleibt strikt modellgetrieben; `NavigatedMessage`-Rückmeldung spiegelt das Shell-Navigated-Ereignis der MAUI-App |
| Gerätename für Pairing/Bootstrap | `UIDevice.CurrentDevice.Name` (Fallback `Model`) | `DeviceInfo.Name`-Äquivalent ohne MAUI |

## Programmabläufe

### App-Start (modellgetrieben)

1. `UIApplication.Main` → `AppDelegate.FinishedLaunching`: `AppComposition` wird aufgebaut (Adapter, Dienste, `TvosAppEffectHandler`, `MvuHost`), `UIWindow` mit `MainViewController` (Ladeanzeige) sichtbar.
2. `AppDelegate` dispatcht `StartupRequestedMessage` in den `MvuHost`.
3. `AppUpdate` liefert Effekte `InitializeStorage` → `LoadSettings` → `LoadServers` → `StartNetworkMonitoring` → `LoadHomeLists`; der Handler führt sie sequenziell aus und dispatcht die Ergebnis-Messages (`SettingsLoadedMessage`, `ServersLoadedMessage`, `NetworkAvailabilityChangedMessage`, `HomeListsLoadedMessage`, ggf. `StorageRecoveredMessage`) auf dem Hauptthread zurück.
4. `ServersLoadedMessage`: ohne aktiven Server → `NavigateToPageEffect(ServerSetup)` → Navigator zeigt die ServerSetup-Zustandsseite; mit Server → `CheckServerHealthEffect` → `ServerHealthCheckCompletedMessage`.
5. Erreichbar → `AttemptAutoLoginEffect` → `AutoLoginCompletedMessage`; Erfolg → `Connected` + `ConnectServerEventsEffect` + `RunFullSyncEffect` + `NavigateToPageEffect(Home)` → Tab-Rahmen mit Startseite. Misserfolg → `NeedsLogin` → Login-Zustandsseite.
6. Unerreichbar → `CheckOfflineDataEffect` → Offline-Daten → `Offline`+Startseite; sonst `NeedsServerSetup` → Einrichtungsseite.
7. `TvosNavigator` (via `ModelChanged`) stellt jede Modelländerung idempotent dar und aktualisiert Status-Texte.

Beteiligte Klassen: `AppDelegate`, `AppComposition`, `MvuHost`,
`TvosAppEffectHandler`, `TvosNavigator`, `MainViewController`,
`LoadingViewController`, `ConnectionStatePageViewController`,
`MenuTabBarController`, alle neutralen Dienste.

### Menü-Navigation per Fernbedienung

1. Anwender fokussiert die Tab-Leiste (`UITabBarItem` „Startseite",
   „Favoriten", „Playlists", „Downloads", „Profil", „Einstellungen") und
   wählt einen Eintrag.
2. `MenuTabBarController.DidSelect` dispatcht `NavigatedMessage(page)` in
   den `MvuHost`; `AppUpdate` setzt `CurrentPage`.
3. Programmatische Navigation (`NavigateToMessage` → `NavigateToPageEffect`)
   wählt denselben Tab über `TvosNavigator`.

Beteiligte Klassen: `MenuTabBarController`, `PagePlaceholderViewController`,
`TvosNavigator`, `MvuHost`, `AppRoutes`.

### Netzwerkstatus-Überwachung

1. `StartNetworkMonitoringEffect` → `TvosNetworkStatusMonitor.Start()`:
   `NWPathMonitor` mit `SnapshotHandler` auf `DispatchQueue.DefaultGlobalQueue`
   starten (idempotent); danach Ist-Wert (`CurrentPath`/`IsOnline`) als
   `NetworkAvailabilityChangedMessage` zurückmelden.
2. Jede Pfad-Änderung feuert `AvailabilityChanged` → Handler dispatcht
   `NetworkAvailabilityChangedMessage` auf dem Hauptthread.

Beteiligte Klassen: `TvosNetworkStatusMonitor`, `TvosAppEffectHandler`, `MvuHost`.

### Lebenszyklus

- `FinishedLaunching`: Komposition + Fenster + `StartupRequestedMessage`.
- `OnActivated` (Rückkehr in den Vordergrund): `HomeListsReloadRequestedMessage`
  dispatcht (Pendant zum MAUI-`window.Resumed`-Dispatch).

Beteiligte Klassen: `AppDelegate`, `AppComposition`, `MvuHost`.

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `NSUserDefaultsSettingsStore` | Klasse (`VideoPlayer.TvOS/Services/`) | `ISettingsStore` über `NSUserDefaults.StandardUserDefaults` |
| `KeychainSecretStore` | Klasse (`VideoPlayer.TvOS/Services/`) | `ISecretStore` über `SecKeyChain`/`SecRecord` (GenericPassword) |
| `TvosStorageLayoutProvider` | statische Klasse (`VideoPlayer.TvOS/Services/`) | `AppStorageLayout`-Wurzeln unter `Library/Caches` auflösen + Verzeichnisse anlegen |
| `TvosNetworkStatusMonitor` | Klasse (`VideoPlayer.TvOS/Services/`) | `INetworkStatusMonitor` über `NWPathMonitor` |
| `TvosLoggerFactory` | Klasse (`VideoPlayer.TvOS/Services/`) | `ILoggerFactory` über `ILoggerProvider` (Kategorie = Typname) |
| `AppLogger<T>` | Klasse (`VideoPlayer.TvOS/Services/`) | `ILogger<T>`-Adapter auf `ILogger` |
| `TvosAppEffectHandler` | Klasse (`VideoPlayer.TvOS/Services/`) | `IAppEffectHandler` für Start-/Verbindungs-Effekte; Rest No-Op |
| `AppComposition` | Klasse (`VideoPlayer.TvOS/`) | Kompositionswurzel: baut Adapter, Dienste, Handler, `MvuHost` |
| `MainThreadInvoker` | statische Klasse (`VideoPlayer.TvOS/Services/`) | `InvokeOnMainThreadAsync(Func<Task>)` via `DispatchQueue.MainQueue` |
| `TvosNavigator` | Klasse (`VideoPlayer.TvOS/Ui/`) | Abbildung `CurrentPage`→UI (Root-Inhalt/Tab), `NavigatedMessage`-Rückmeldung |
| `LoadingViewController` | Klasse (`VideoPlayer.TvOS/Ui/`) | Ladeanzeige (`UIActivityIndicatorView` + `StartupStatusText`) |
| `ConnectionStatePageViewController` | Klasse (`VideoPlayer.TvOS/Ui/`) | Zustandsseiten ServerSetup/Login: Titel, Status-/Fehlertext, fokussierbarer „Erneut prüfen"-Button (`StartupRequestedMessage`) |
| `MenuTabBarController` | Klasse (`VideoPlayer.TvOS/Ui/`) | `UITabBarController` über `AppRoutes.MenuPages`; `DidSelect` → `NavigatedMessage` |
| `PagePlaceholderViewController` | Klasse (`VideoPlayer.TvOS/Ui/`) | Inhaltsseite mit Titel + Untertitel (Platzhalter späterer Schritte) |
| `TvOsAppContractTests` | Testklasse (`VideoPlayer.Maui.Tests`) | Contract-Tests der tvOS-Verdrahtung (siehe Tests) |

## Änderungen an bestehenden Klassen/Dateien

### `VideoPlayer.TvOS/AppDelegate.cs`
- `FinishedLaunching`: `AppComposition` erzeugen, `MainViewController` +
  `TvosNavigator` verdrahten, `StartupRequestedMessage` dispatchen.
- Neu: `OnActivated` → `HomeListsReloadRequestedMessage` dispatcht.

### `VideoPlayer.TvOS/MainViewController.cs`
- Ersetzt die leere Startansicht: Container, der das aktuell darzustellende
  Kind-ViewController-Element (`LoadingViewController`,
  `ConnectionStatePageViewController`, `MenuTabBarController`) einbettet und
  beim Wechsel austauscht (Embed/Transition-Muster).

### Verschiebungen `VideoPlayer.Maui/Services/` → `VideoPlayer.Mvu/`
- `IServerAddressProvider.cs`, `ServerAddressProvider.cs`,
  `IServerHealthProbe.cs`, `ServerHealthProbe.cs`,
  `SignalRServerEventsConnection.cs` — Namespace `VideoPlayer.Maui.Services`
  → `VideoPlayer.Mvu`; Dateien in den Mvu-Projektordner verschieben.
  Verwendungen in `MauiProgram`/`ShellAppEffectHandler` bleiben kompatibel
  (beide haben `using VideoPlayer.Mvu`).

### `VideoPlayer.Maui.csproj`
- `PackageReference Microsoft.AspNetCore.SignalR.Client` entfernen
  (wandert nach Mvu; transitiv weiter verfügbar).

### `VideoPlayer.Mvu.csproj`
- `PackageReference Microsoft.AspNetCore.SignalR.Client` Version 10.0.10
  hinzufügen.

### `VideoPlayer.Maui.Tests` — Pfad-Assertions anpassen
- `NetworkSyncContractTests.cs`: `MauiFile("Services","SignalRServerEventsConnection.cs")` → `VideoPlayer.Mvu`-Pfad; `Csproj_ReferencesSignalRClient` prüft `VideoPlayer.Mvu.csproj`.
- `PairingContractTests.cs` (Z. 183/227): `ServerHealthProbe.cs`/`IServerHealthProbe.cs` → `VideoPlayer.Mvu`-Pfad.
- `ServerSetupContractTests.cs` (Z. 183): `ServerHealthProbe.cs` → `VideoPlayer.Mvu`-Pfad.
- `SettingsStorageContractTests.cs` (Z. 111): `ServerAddressProvider.cs` → `VideoPlayer.Mvu`-Pfad.
- `VideoPlayerMauiContractTests.cs` (Z. 116): Existenzprüfung → `VideoPlayer.Mvu/ServerHealthProbe.cs`.

## Datenbankmigrationen

Keine. Die bestehenden SQLite-Schemata (`videoplayer.db3`,
`videoplayer-cache.db3`) werden über `InitializeWithRecoveryAsync`
unverändert auf tvOS angelegt.

## Validierungsregeln

Keine neuen Eingabevalidierungen — der `KeychainSecretStore` bildet nur die
`ISecretStore`-Semantik ab; fachliche Validierung verbleibt in den
plattformneutralen Diensten.

## Konfigurationsänderungen

Keine neuen Konfigurationseinträge. Weiterhin wirksam:
`VIDEOPLAYER_SERVER_URL` (in `ServerAddressProvider`, jetzt in Mvu),
`VIDEOPLAYER_PROFILE` (`AppProfile.FromEnvironment`). Keine neuen
`Info.plist`-Schlüssel nötig.

## Seiteneffekte und Risiken

- **MAUI-App:** Typ-/Ortsverschiebung der 5 Dienste — kompiliert unverändert
  (Namespace via `using VideoPlayer.Mvu`); Contract-Test-Anpassungen oben.
  Paket-Wechsel SignalR Maui→Mvu: transitiv identisch.
- **tvOS-Speicher-Realität:** lokale Daten (Datenbanken, Serverliste) sind
  systemräumbar (`Library/Caches`); ein Purge erzwingt Neu-Einrichtung.
  Zugangsdaten/Geräte-Tokens im Schlüsselbund und Einstellungen in
  `NSUserDefaults` sind davon nicht betroffen. Dokumentierte Einschränkung.
- **Kompilierbarkeit:** tvOS-Quellcode wird unter Windows nicht gebaut —
  Syntax-/API-Risiken werden über Contract-Tests (Quell-Assertions) und den
  macOS-CI-Job `build-tvos` abgesichert; letzterer wirkt erst nach Push.
- **Zeroconf/SQLitePCL auf tvOS:** Pakete sind net10.0-referenziert; Laufzeit-
  Verhalten auf tvOS kann erst im Simulator geprüft werden (Risiko akzeptiert,
  Fehler der Discovery werden als leere Liste behandelt).

## Umsetzungsreihenfolge

1. **Verschiebung der fünf plattformneutralen Dateien nach `VideoPlayer.Mvu/`** (Namespace `VideoPlayer.Mvu`) inkl. Paketwechsel `Microsoft.AspNetCore.SignalR.Client` (Maui.csproj → Mvu.csproj)
   - Voraussetzungen: keine
   - Beschreibung: `git mv` der 5 Dateien, Namespace anpassen, csproj-Änderungen; `dotnet build` der Solution muss lokal grün bleiben.
2. **Bestehende Contract-Tests auf neue Pfade/Paketort anpassen**
   - Voraussetzungen: Schritt 1
   - Beschreibung: die sechs in „Änderungen" gelisteten Teststellen auf `VideoPlayer.Mvu`-Pfade bzw. `VideoPlayer.Mvu.csproj` umstellen; Testsuite grün.
3. **Plattform-Adapter anlegen** (`NSUserDefaultsSettingsStore`, `KeychainSecretStore`, `TvosStorageLayoutProvider`, `TvosNetworkStatusMonitor`, `TvosLoggerFactory`+`AppLogger<T>`, `MainThreadInvoker`)
   - Voraussetzungen: keine
   - Beschreibung: neue Dateien unter `VideoPlayer.TvOS/Services/`; lokale Nicht-Kompilierung beachten (Contract-Tests sichern).
4. **`TvosAppEffectHandler` implementieren**
   - Voraussetzungen: Schritt 1 (ServerHealthProbe/ServerAddressProvider in Mvu), Schritt 3 (Adapter)
   - Beschreibung: Effekt-Switch mit Start-/Verbindungs-Zweigen (Ergebnis-Messages via `MainThreadInvoker`), `default:` → `LogDebug`-No-Op; Ereignis-Abonnements wie im MAUI-Handler (SessionExpired, DeviceTokenRejected, AvailabilityChanged, serverEvents).
5. **UI-Grundgerüst** (`LoadingViewController`, `ConnectionStatePageViewController`, `PagePlaceholderViewController`, `MenuTabBarController`, `TvosNavigator`, `MainViewController`-Umbau)
   - Voraussetzungen: keine (UIKit only)
   - Beschreibung: Seitenrahmen + Zustandsseiten + modellgetriebene Abbildung.
6. **`AppComposition` + `AppDelegate`-Verdrahtung**
   - Voraussetzungen: Schritte 3–5
   - Beschreibung: Dienste manuell verdrahten, `FinishedLaunching`/`OnActivated`-Dispatches.
7. **Neue Contract-Tests `TvOsAppContractTests`**
   - Voraussetzungen: Schritte 3–6
   - Beschreibung: Quell-Assertions der Verdrahtung (siehe Tests).
8. **Lokale Validierung:** `dotnet build` der Solution + `dotnet test` `VideoPlayer.Maui.Tests` — alle grün; `dotnet format whitespace` für geänderte Dateien.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft |
|--------------------|------------|---------------------|
| `TvOsProject_ProvidesPlatformAdapters` | `TvOsAppContractTests` | Dateien `NSUserDefaultsSettingsStore.cs` (enthält `NSUserDefaults`), `KeychainSecretStore.cs` (`SecKeyChain`,`GenericPassword`, kein `NSUserDefaults`), `TvosStorageLayoutProvider.cs` (`CachesDirectory`, kein `SetSkipBackupAttribute`), `TvosNetworkStatusMonitor.cs` (`NWPathMonitor`,`Satisfied`,`Wifi`,`Wired`) existieren |
| `TvOsProject_ProvidesMvuWiring` | `TvOsAppContractTests` | `TvosAppEffectHandler.cs` enthält die Start-/Verbindungs-Effekt-Zweige (`InitializeStorageEffect`, `LoadSettingsEffect`, `PersistSettingsEffect`, `LoadServersEffect`, `CheckServerHealthEffect`, `CheckOfflineDataEffect`, `AttemptAutoLoginEffect`, `AttemptLoginEffect`, `StartNetworkMonitoringEffect`, `LoadHomeListsEffect`, `ConnectServerEventsEffect`, `DisconnectServerEventsEffect`, `RunFullSyncEffect`, `NavigateToPageEffect`, `DiscoverServersEffect`, `CheckCandidateServerEffect`, `PersistActiveServerEffect`, `ExchangePairingCodeEffect`, `CheckBootstrapServerEffect`, `ExchangeBootstrapTicketEffect`), Ergebnis-Messages und `default:`-No-Op; Ereignis-Abonnements (`SessionExpired`, `DeviceTokenRejected`, `AvailabilityChanged`, `EventReceived`, `ConnectionClosed`) |
| `TvOsEffectHandler_DoesNotWireExcludedAreas` | `TvOsAppContractTests` | Handler enthält keine Zweige für `SavePlaylistEffect`, `DeletePlaylistEffect`, `EnqueueDownloadEffect`, `RunAutoDownloadsEffect`, `TogglePictureInPictureEffect`, `SendLogProtocolEffect` (keine Schreibzugriffe, keine tvOS-fremden Bereiche) |
| `TvOsApp_CompositionWiresCoreServices` | `TvOsAppContractTests` | `AppComposition.cs` enthält `MvuHost`, `TvosAppEffectHandler`, `KeychainSecretStore`, `NSUserDefaultsSettingsStore`, `TvosNetworkStatusMonitor`, `TvosStorageLayoutProvider`, `ServerAddressProvider`, `ServerHealthProbe`, `AuthSessionService`, `DeviceTokenService`, `ServerEventsService`, `SignalRServerEventsConnection`, `AppStorageLayout`, `AppProfile.FromEnvironment` |
| `TvOsApp_DelegateStartsMvuLoop` | `TvOsAppContractTests` | `AppDelegate.cs` enthält `AppComposition`, `StartupRequestedMessage`, `MakeKeyAndVisible`, `OnActivated`/`HomeListsReloadRequestedMessage` |
| `TvOsUi_UsesTabBarAndFocusablePages` | `TvOsAppContractTests` | `MenuTabBarController.cs` enthält `UITabBarController`, `AppRoutes.MenuPages`, `NavigatedMessage`; `LoadingViewController.cs` enthält `UIActivityIndicatorView`; `ConnectionStatePageViewController.cs` enthält `StartupRequestedMessage` (Erneut-prüfen-Button) |
| `MvuHostsPlatformNeutralServices` | `TvOsAppContractTests` | `ServerHealthProbe.cs`, `IServerHealthProbe.cs`, `ServerAddressProvider.cs`, `IServerAddressProvider.cs`, `SignalRServerEventsConnection.cs` existieren unter `VideoPlayer.Mvu/` mit `namespace VideoPlayer.Mvu`; `VideoPlayer.Mvu.csproj` enthält `Microsoft.AspNetCore.SignalR.Client`; `VideoPlayer.Maui.csproj` nicht mehr |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `NetworkSyncContractTests.SignalRConnection_MapsServerHubEvents` | Datei nach `VideoPlayer.Mvu/` verschoben |
| `NetworkSyncContractTests.Csproj_ReferencesSignalRClient` | Paket in `VideoPlayer.Mvu.csproj` statt `VideoPlayer.Maui.csproj` |
| `PairingContractTests.ServerHealthProbe_ReturnsHealthCheckResult` | Pfad der beiden Dateien |
| `PairingContractTests.ServerHealthProbe_ResolvesGateKeyPerAddress` | Pfad |
| `ServerSetupContractTests` (Z. 183) | Pfad `ServerHealthProbe.cs` |
| `SettingsStorageContractTests.ServerAddressProvider_UsesKnownServersService` | Pfad `ServerAddressProvider.cs` |
| `VideoPlayerMauiContractTests` (Z. 116) | Existenzprüfung `ServerHealthProbe.cs` → neuer Ort |

### E2E-Tests (Pflicht)

Für die geänderten Benutzerabläufe ist auf Windows kein E2E-Test möglich:
`VideoPlayer.E2E.Tests` ist ein FlaUI-Windows-Harness für die MAUI-App; die
tvOS-Oberfläche kann weder lokal noch in CI gestartet werden (nur
Simulator-Build). Der Akzeptanznachweis erfolgt über:
- die plattformneutrale `AppUpdateTests_*`-Suite (modellgetriebener Start- und
  Verbindungsfluss bereits abgedeckt, bleibt unverändert grün) und
- die neuen `TvOsAppContractTests` (tvOS-Verdrahtung, Focus-Rahmen,
  Effekt-Abdeckung) sowie
- den macOS-CI-Job `build-tvos` (Kompiliernachweis nach Push).
Ein tvOS-Simulator-Lauf bleibt manuelle Verifikation; es werden keine neuen
E2E-Tests geschrieben. Bestehende E2E-Tests sind nicht betroffen.

## Offene Punkte

Keine.
