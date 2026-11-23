# Abnahmeprüfung – Entwicklungsschritt 2

## Ergebnis

**Status:** Anforderung vollständig erfüllt

## Hinweise

Prüfung anhand des Diffs `task/...-apple-tv-app-tvos-lean-back-pl...HEAD` (35 Dateien, +2186/−43) sowie der geänderten Quelldateien im Ist-Stand.

**(a) MVU-Verdrahtung am tvOS-Lebenszyklus — erfüllt:**
- `VideoPlayer.TvOS/AppDelegate.cs:26-44`: `FinishedLaunching` erstellt `AppComposition`, erzeugt über `CreateRuntime(main)` `TvosNavigator` + `TvosAppEffectHandler` + `MvuHost` und dispatcht `StartupRequestedMessage`; `OnActivated` (Z. 47-56) dispatcht `HomeListsReloadRequestedMessage`.
- `VideoPlayer.TvOS/AppComposition.cs`: Kompositionswurzel ohne DI-Container; `CreateRuntime` (Z. 179-210) verbindet Navigator mit `MainViewController`, abonniert `MvuHost.ModelChanged` → `TvosNavigator.Render` (hauptthread-marshalliert via `MainThreadInvoker`); serieller Dispatch liegt im plattformneutralen `MvuHost` (`VideoPlayer.Mvu/MvuHost.cs:44-68`, SemaphoreSlim).
- Start-Workflow-Kette verifiziert: `AppUpdate.cs:344` emittiert `InitializeStorage`/`LoadSettings`/`LoadServers`/`StartNetworkMonitoring`/`LoadHomeLists`; `UpdateServersLoaded` → `CheckServerHealth`; `UpdateHealthCheckCompleted` → `AttemptAutoLogin`/`CheckOfflineData`/`NavigateToPage`. Alle abgedeckt.

**(b) tvOS-`IAppEffectHandler` — erfüllt:**
- `VideoPlayer.TvOS/Services/TvosAppEffectHandler.cs:81-159`: 23 Effekt-Zweige (Ablage, Einstellungen, Serververwaltung, Discovery, Kandidatenprüfung, Pairing/Bootstrap, Auto-/Login, Offline-Prüfung, Netzwerk-Monitoring, Ereignisverbindung, Voll-/Teilsync, Startseiten-Listen, `CheckServerHealth`, `NavigateToPage`).
- `default:`-Zweig (Z. 153-158) protokolliert nicht verdrahtete Effekte als `LogDebug`-No-Op — fehlerfrei. Ereignis-Rückmeldungen des Dienst-Rands (`SessionExpired`, `DeviceTokenRejected`, `AvailabilityChanged`, `EventReceived`, `ConnectionClosed`) sind verdrahtet (Z. 71-75, 669-682).

**(c) Plattform-Adapter — erfüllt:**
- `NSUserDefaultsSettingsStore` (`ISettingsStore`, `Services/NSUserDefaultsSettingsStore.cs`), `KeychainSecretStore` (`ISecretStore` via `SecKeyChain`/`SecKind.GenericPassword`, `Services/KeychainSecretStore.cs`), `TvosStorageLayoutProvider` (`AppStorageLayout` unter `Library/Caches`, `Services/TvosStorageLayoutProvider.cs`), `TvosNetworkStatusMonitor` (`INetworkStatusMonitor` via `NWPathMonitor`, `Services/TvosNetworkStatusMonitor.cs`).
- Server-Adressquelle: `VideoPlayer.Mvu/ServerAddressProvider.cs:15` nutzt `VIDEOPLAYER_SERVER_URL` mit IIS-Fallback; `AppProfile.FromEnvironment` (`VideoPlayer.Storage/AppProfile.cs:12,46`) nutzt `VIDEOPLAYER_PROFILE`; beides in `AppComposition` verdrahtet (Z. 24, 48).

**(d) Navigations-/Fokus-Grundgerüst — erfüllt:**
- Seitenrahmen: `Ui/MenuTabBarController.cs` (`UITabBarController`, nativ Focus-Engine-/Siri-Remote-tauglich), Menüeinträge folgen `AppRoutes.MenuPages`, Auswahl → `NavigatedMessage`; `UpdateNavigated` (`AppUpdate.cs:4427-4447`) emittiert keinen Folge-Effekt → kein Feedback-Loop.
- Modellgetriebene Zustände: `Ui/TvosNavigator.cs` bildet `CurrentPage` auf Ladeanzeige (`Ui/LoadingViewController.cs` mit `UIActivityIndicatorView` + `StartupStatusText`/`StorageNotice`), Verbindungsseiten (`Ui/ConnectionStatePageViewController.cs` mit fokussierbarem „Erneut prüfen"-Button → `StartupRequestedMessage`) oder Tab-Rahmen ab; `MainViewController.cs` als Embed-/Remove-Container.

**(e) Mvu-Verschiebungen MAUI-kompatibel — erfüllt:**
- `IServerAddressProvider`, `IServerHealthProbe`, `ServerAddressProvider`, `ServerHealthProbe`, `SignalRServerEventsConnection` von `VideoPlayer.Maui/Services/` nach `VideoPlayer.Mvu/` (Namespace `VideoPlayer.Mvu`) verschoben; `Microsoft.AspNetCore.SignalR.Client` ins Mvu-csproj verlagert (`VideoPlayer.Mvu.csproj`); die verschobenen Dateien enthalten keine MAUI-Abhängigkeiten (geprüft: kein `Microsoft.Maui`/`Preferences`/`SecureStorage`/`Connectivity`).
- MAUI-Verwendungen unverändert lauffähig: `VideoPlayer.Maui/MauiProgram.cs:44-99` und `Services/ShellAppEffectHandler.cs` importieren `using VideoPlayer.Mvu;`; keine verwaisten Verweise auf den alten Namespace; Pfad-Assertions in fünf Contract-Testdateien angepasst.

**(f) Keine Schreibzugriffe auf Inhalte — erfüllt:**
- Content-Mutations-Effekte (`ToggleFavorite`, `RemoveFavorite`, `Hide/SkipContinueWatching`, sämtliche Playlist-/Download-Effekte, `SendLogProtocol`, `TogglePictureInPicture`, Player/Playback) sind nicht verdrahtet; lokale Schreibzugriffe beschränken sich auf Einstellungen, Schlüsselbund, Serververwaltung und Cache-Ablage — für Start/Verbindungsaufbau erforderlich. Server-seitige Inhalts-Mutationen existieren nicht.

**Tests-Nachweis:**
- `dotnet test VideoPlayer.Maui.Tests` (net10.0) lokal ausgeführt: **1453/1453 grün, 0 Fehler** (inkl. der neuen `TvOsAppContractTests` mit 7 Quell-Contract-Tests). Dabei kompilierte auch `VideoPlayer.Mvu` fehlerfrei.
- Doku-Ungenauigkeit (keine Anforderungsabweichung): `changes.log` und die Tasks-Datei nennen „1421/1421 grün"; der tatsächliche Stand ist 1453.

**Einschränkungen (keine Abweichungen):**
- tvOS-Kompiliernachweis ausstehend bis zum Push: CI-Job `build-tvos` (`.github/workflows/pr-checks.yml:50-86`) baut `net10.0-tvos` auf macOS; für diesen Stand lag zum Prüfzeitpunkt noch kein CI-Lauf vor. Simulator-Start/Bedienbarkeit ist damit plausibilisiert (UIKit-Standardverhalten, Binding-Verwendung per Recherche verifiziert, u. a. `NSUserDefaults`-Indexer), aber nicht lokal ausgeführt worden.
- MAUI-Windows-Build lokal nicht möglich (Workload fehlt); die net10.0-Testsuite deckt die plattformneutrale Seite der Verschiebung ab.
- Warnung NU1903 beim Testlauf: `Microsoft.AspNetCore.SignalR.Common` 10.0.10 weist eine bekannte Sicherheitsanfälligkeit auf — die Paketversion wurde unverändert nur ins Mvu-Projekt verlagert (Bestandsproblem, nicht durch diesen Schritt eingeführt).
- Der Menürahmen enthält einen „Downloads"-Tab als reinen Platzhalter ohne jede Download-Funktion oder -Verdrahtung (folgt dem geteilten `AppRoutes.MenuPages`); die nicht-vorgesehenen Funktionsbereiche bleiben fachlich ohne Oberfläche.
