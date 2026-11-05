# Übersetzte Anforderung: tvOS-App — MVU-Grundgerüst, Plattform-Anbindung, Navigation und Fokus

Entwicklungsschritt 2 des Projekts „Apple TV-App (tvOS)" (Issue #86).
Vorarbeit (Schritt 1): Projekt `VideoPlayer.TvOS` mit TFM `net10.0-tvos` hinter
dem `IncludeTvOsTarget`-Schalter, minimalem Einstieg (`Program.cs`,
`AppDelegate.cs`, `MainViewController.cs`, `Info.plist`) und macOS-CI-Job
`build-tvos` existiert bereits.

## Fachliche Zusammenfassung

Die tvOS-App erhält ihren fachlichen Rahmen: Der plattformneutrale
MVU-Zustandskern (`MvuHost` mit serialisiertem `DispatchAsync`,
`AppUpdate.Update` als reine Update-Funktion, `AppModel`, `AppEffect`) wird an
den tvOS-App-Lebenszyklus (`AppDelegate`) angeschlossen und durch einen
tvOS-seitigen `IAppEffectHandler` ergänzt, der die Effekte am App-Rand ausführt.
Dafür werden die plattformspezifischen Anschlüsse geschaffen, die die
plattformneutralen Dienste benötigen: `ISettingsStore` über Benutzer-
Standardwerten (`NSUserDefaults`), `ISecretStore` über dem Schlüsselbund
(`SecItem`), `AppStorageLayout` mit tvOS-Verzeichniswurzeln,
`INetworkStatusMonitor` über `NWPathMonitor` sowie eine Server-Adressquelle
inklusive der bestehenden Entwicklungs-Fallbacks (`VIDEOPLAYER_SERVER_URL`,
`VIDEOPLAYER_PROFILE`). Zusätzlich entsteht das Lean-back-Navigationsgerüst:
Seitenrahmen mit Menü- und Inhaltsbereich, vollständig über die Siri Remote
steuerbar (tvOS-Focus-Engine), mit Abbildung der modellgetriebenen
Navigationszustände (Ladeanzeige, Verbindungszustände). Nicht für tvOS
vorgesehene Modellbereiche (Kamera-QR-Scan, Mini-Player/PiP, Auto-Download,
Inhalts-/Playlist-Verwaltung, Protokollversand) bleiben bewusst unverdrahtet und
fehlerfrei.

## Betroffene Klassen und Komponenten

- **Neu (Plattform-Adapter, `VideoPlayer.TvOS`):**
  - `NSUserDefaultsSettingsStore` (`ISettingsStore`-Adapter, Vorlage `PreferencesSettingsStore`)
  - `KeychainSecretStore` (`ISecretStore`-Adapter auf `Security.SecItem`, Vorlage `SecureStorageSecretStore`)
  - tvOS-Ablage-Layout-Provider (tvOS-Pendant zu `AppStorageLayoutProvider`: tvOS hat kein Backup-Konzept; lokale Daten sind jederzeit räumbar → `Library/Caches`-basierte Wurzeln)
  - `TvosNetworkStatusMonitor` (`INetworkStatusMonitor` via `Network.NWPathMonitor`, Vorlage `ConnectivityNetworkStatusMonitor`; `IsWifi` = WLAN oder Ethernet)
- **Neu (MVU-/App-Rand, `VideoPlayer.TvOS`):**
  - `TvosAppEffectHandler` (`IAppEffectHandler`, Gegenstück zu `ShellAppEffectHandler`: deckt die Effekte des Start- und Verbindungs-Workflows ab; übrige Effekte → protokolliertes No-Op)
  - Composition Root ohne DI-Container (Pendant zu `MauiProgram`; manuelle Verdrahtung aller plattformneutralen Dienste)
  - Navigation/Dispatch-Hilfen: Main-Thread-Dispatch (`NSOperationQueue.MainQueue`/`DispatchQueue.MainQueue`), Navigator für die modellgetriebene Seitenwahl
- **Neu (UI, `VideoPlayer.TvOS`):**
  - Seitenrahmen mit Menü- und Inhaltsbereich (Siri-Remote-bedienbar über die tvOS-Focus-Engine)
  - Ladeanzeige beim Start (`AppModel.StartupStatusText`/`ConnectionState`)
  - Darstellung der Verbindungszustände (`Connecting`/`Connected`/`NeedsLogin`/`NeedsServerSetup`/`Offline`)
  - Platzhalter-Inhaltsseiten je Menübereich (`AppRoutes.MenuPages`: Home, Favorites, Playlists, Downloads, Profile, Settings)
- **Geändert:**
  - `VideoPlayer.TvOS/AppDelegate.cs` — Aufbau der Komposition, `StartupRequestedMessage`-Dispatch, Lebenszyklus-Hooks
  - `VideoPlayer.TvOS/MainViewController.cs` — ersetzt die leere Startansicht durch den Seitenrahmen
  - `VideoPlayer.Maui` — die faktisch plattformneutralen Dienste (`IServerAddressProvider`/`ServerAddressProvider`, `IServerHealthProbe`/`ServerHealthProbe`, `SignalRServerEventsConnection`) werden an eine wiederverwendbare Stelle verschoben, damit tvOS sie ohne MAUI-Referenz nutzen kann
  - `VideoPlayer.Maui.csproj` / `VideoPlayer.Mvu.csproj` — `Microsoft.AspNetCore.SignalR.Client`-Referenz folgt der Verschiebung
  - `VideoPlayer.Maui.Tests` — Contract-Tests mit Dateipfad-Assertions auf die verschobenen Dateien; neue Contract-Tests für das tvOS-Grundgerüst
- **Bewusst unverdrahtet:** Kamera-QR-Scan, Mini-Player/PiP, Auto-Download, Inhalts-/Playlist-Verwaltung (alle `Save/Delete/Move/Set/Add`-Playlist-Effekte), Protokollversand (`SendLogProtocolEffect`), Downloads und Wiedergabe — der Effekt-Handler protokolliert sie und wirft nicht.

## Implementierungsansatz

- `FinishedLaunching` baut die Komposition (alle Singletons manuell verdrahtet,
  analog `MauiProgram`), setzt den `MainViewController` als `Window.RootViewController`
  und dispatcht `StartupRequestedMessage` in den `MvuHost`; `ModelChanged` rendert
  den Navigationszustand auf den Seitenrahmen.
- `TvosAppEffectHandler` übernimmt pro Effekt das Muster des
  `ShellAppEffectHandler` (Ergebnis-Messages via Main-Thread in den
  `MvuHost`-Zyklus): `InitializeStorageEffect`, `LoadSettingsEffect`,
  `PersistSettingsEffect`, `LoadServersEffect`, `CheckServerHealthEffect`,
  `CheckOfflineDataEffect`, `AttemptAutoLoginEffect`, `AttemptLoginEffect`,
  `StartNetworkMonitoringEffect`, `LoadHomeListsEffect`,
  `ConnectServerEventsEffect`, `DisconnectServerEventsEffect`,
  `RunFullSyncEffect`, `NavigateToPageEffect`, `NavigateBackEffect`,
  `LoadSourcesEffect` sowie die Server-Einrichtungseffekte
  (`DiscoverServersEffect`, `CheckCandidateServerEffect`,
  `PersistActiveServerEffect`, `ExchangePairingCodeEffect`,
  `CheckBootstrapServerEffect`, `ExchangeBootstrapTicketEffect`) und
  `LogoutEffect`/`LoadProfileInfoEffect` als Verbindungs-Endpunkte.
- Menü-/Inhaltsrahmen als `UITabBarController` (tvOS-idiomatisch, Focus-Engine
  ab Werk) oder äquivalenter Split-Rahmen; `NavigateToPageEffect` bildet
  `AppPage` auf Root-Wechsel (Loading/ServerSetup/Login) bzw. Tab-Auswahl ab.
- Die vier faktisch plattformneutralen Klassen/Interfaces werden von
  `VideoPlayer.Maui/Services/` nach `VideoPlayer.Mvu/` verschoben (Namespace
  `VideoPlayer.Mvu`), inklusive der `Microsoft.AspNetCore.SignalR.Client`-
  Paketreferenz — betroffene Contract-Tests werden angepasst.
- Lokale Validierung: bestehende `VideoPlayer.Maui.Tests`-Suite + neue
  Contract-Tests (Muster `TvOsProjectContractTests`) für Datei-/Verdrahtungs-
  assertions des tvOS-Projekts; Kompiliernachweis tvOS via macOS-CI nach Push.

## Konfiguration

- `VIDEOPLAYER_SERVER_URL` — Entwicklungs-Fallback der Server-Basisadresse (wie MAUI).
- `VIDEOPLAYER_PROFILE` — Entwicklungs-Profil (Profil-Unterverzeichnisse/-Schlüsselpräfixe, wie MAUI).
- `Info.plist` — keine neuen Schlüssel nötig (`NSLocalNetworkUsageDescription`/`NSBonjourServices`/`NSAppTransportSecurity` sind bereits aus Schritt 1 gesetzt).

## Offene Fragen

- Keine fachlich ungeklärten Punkte; technische Detailentscheidungen
  (Tab-Bar vs. benutzerdefinierter Split-Rahmen, tvOS-Verzeichniswurzeln unter
  `Library/Caches`, Keychain-Service/Account-Benennung, Verschiebungsziel der
  plattformneutralen MAUI-Dienste) werden in der Umsetzungsplanung festgelegt.
