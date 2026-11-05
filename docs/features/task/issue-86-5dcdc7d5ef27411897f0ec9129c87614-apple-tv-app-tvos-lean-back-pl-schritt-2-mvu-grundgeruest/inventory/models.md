# Modelle und Enums — Bestandsaufnahme (Schritt 2)

## `AppModel`
`VideoPlayer.Mvu/AppModel.cs` — unveränderliches Gesamtmodell; Start-/
Navigations-relevante Eigenschaften:

| Eigenschaft | Typ | Startwert | Beschreibung |
|---|---|---|---|
| `CurrentPage` | `AppPage` | `Loading` | Angezeigte Seite |
| `IsStartupRunning` | `bool` | `true` | Start-Workflow läuft |
| `ServerHealth` | `ServerHealthStatus` | `Unknown` | Erreichbarkeitsprüfung (`Unknown`/`Checking`/`Reachable`/`Unreachable`) |
| `ConnectionState` | `ConnectionState` | `Connecting` | Verbindungszustand |
| `IsLoginRunning`/`LoginError`/`LoginUsername` | `bool`/`string?`/`string?` | — | Anmeldezustand |
| `IsPairingRequired`/`IsPairingRunning`/`PairingError` | `bool`/`bool`/`string?` | — | Pairing-Zustand der Einrichtungsseite |
| `IsBootstrapRunning`/`BootstrapError` | `bool`/`string?` | — | Ticket-Bootstrap |
| `HasOfflineData` | `bool` | `false` | Lokale Offline-Daten vorhanden |
| `IsOnline`/`IsOffline` | `bool` | `true`/abgeleitet | Netzstatus |
| `KnownServers`/`ActiveServerAddress`/`DiscoveredServers`/`IsDiscoveryRunning`/`ServerSetupError` | Listen/`string?`/`bool` | — | Serververwaltung/-einrichtung |
| `StartupStatusText` | `string` (berechnet) | — | Ladeanzeige-Text („Anmeldung beim Server läuft …" bzw. `ServerHealthText`) |
| `StorageNotice` | `string?` | `null` | Ablage-Wiederherstellungs-Hinweis |
| `SourcesOverview`/`SourceBrowser`/`MediaDetails`/`FavoritesPage`/`PlaylistsPage`/`PlaylistDetails`/`Downloads`/`Profile`/`Player` | Seitenzustands-Records | Initial/Empty | Inhaltsbereiche späterer Schritte |

## `AppPage` (Enum)
`VideoPlayer.Mvu/AppPage.cs`: `Loading` (kein Menüeintrag), `Home`,
`Favorites`, `Playlists`, `Downloads`, `Profile`, `Settings` (Menüseiten),
`ServerSetup`, `Login` (routenlose Zustandsseiten, MAUI: Window-Root bzw.
geschobene Shell-Route).

## `ConnectionState` (Enum)
`VideoPlayer.Mvu/ConnectionState.cs`: `Unknown`, `Connecting`, `Connected`,
`NeedsLogin`, `NeedsServerSetup`, `Offline` — steuert die Navigation.

## `ServerHealthStatus` (Enum)
`VideoPlayer.Mvu/ServerHealthStatus.cs`: `Unknown`, `Checking`, `Reachable`,
`Unreachable` (laut `AppModel.ServerHealthText`-Mapping).

## `AppRoutes`
`VideoPlayer.Mvu/AppRoutes.cs` — statische Zuordnung: `MenuPages` =
`[Home, Favorites, Playlists, Downloads, Profile, Settings]` (Reihenfolge des
Hauptmenüs); `ToRoute(AppPage)` → `home`/`favorites`/`playlists`/`downloads`/
`profile`/`settings`; `PushedRoutes` = `serversetup`, `login`;
`TryGetPage(location)` löst Shell-Orte zurück auf `AppPage`.

## `AppEffect`/`AppMessage`
`VideoPlayer.Mvu/AppEffect.cs` (~75 `sealed record`-Subtypen) und
`AppMessage.cs` (~135 Subtypen) — abstrakte Basistypen; der Effekt-Handler
switcht über die konkreten Records. Start-/Verbindungs-Effekte:
`InitializeStorage`, `LoadSettings`, `PersistSettings`, `LoadServers`,
`CheckServerHealth`, `CheckOfflineData`, `AttemptAutoLogin`, `AttemptLogin`,
`StartNetworkMonitoring`, `LoadHomeLists`, `ConnectServerEvents`,
`DisconnectServerEvents`, `RunFullSync`, `NavigateToPage`, `NavigateBack`,
`DiscoverServers`, `CheckCandidateServer`, `PersistActiveServer`,
`ExchangePairingCode`, `CheckBootstrapServer`, `ExchangeBootstrapTicket`,
`Logout`, `LoadProfileInfo`, `LoadSources`, Teil-Syncs
(`SyncContinueWatching`/`SyncFavorites`/`SyncMetadata`).

## `AppProfile`
`VideoPlayer.Storage/AppProfile.cs`: `EnvironmentVariable` =
`VIDEOPLAYER_PROFILE`; `DefaultName` = `default`; `DirectoryName` (leer beim
Standardprofil); `PrefixKey(key)` für profil-isolierte Ablageschlüssel;
`FromEnvironment()`/`Create(name)`.
