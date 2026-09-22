# Interfaces

## `IAppEffectHandler`

Datei: `VideoPlayer.Mvu/IAppEffectHandler.cs`

Contract zwischen MVU-Kern und App-Rand; implementiert von `ShellAppEffectHandler` (VideoPlayer.Maui).

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `HandleAsync` | `AppEffect effect`, `CancellationToken cancellationToken` | `Task` | Führt einen einzelnen Effekt aus (Navigation, Server-Aufrufe, Ablage). |

## `INetworkStatusMonitor`

Datei: `VideoPlayer.Mvu/INetworkStatusMonitor.cs`

Abstraktion der Netzwerkstatus-Erkennung; implementiert von `ConnectivityNetworkStatusMonitor` (VideoPlayer.Maui).

| Methode / Member | Parameter | Rückgabewert | Zweck |
|------------------|-----------|--------------|-------|
| `IsOnline` | – | `bool` | Letzter bekannter Netzwerkstatus. |
| `IsWifi` | – | `bool` | Ungedrosseltes Festnetz-Profil (WLAN/Ethernet) für „Downloads nur im WLAN". |
| `AvailabilityChanged` | – | `event EventHandler<bool>` | Meldet jeden Netzwerkwechsel (→ `NetworkAvailabilityChangedMessage`). |
| `Start` | – | `void` | Startet die Überwachung (idempotent). |

## `IServerEventsConnection`

Datei: `VideoPlayer.Mvu/IServerEventsConnection.cs`

Abstraktion der SignalR-Ereignisverbindung; implementiert von `SignalRServerEventsConnection` (VideoPlayer.Maui), orchestriert von `ServerEventsService`.

| Methode / Member | Parameter | Rückgabewert | Zweck |
|------------------|-----------|--------------|-------|
| `IsConnected` | – | `bool` | Verbindung aktuell aufgebaut. |
| `ConnectAsync` | `Uri hubAddress`, `Func<Task<string?>> accessTokenProvider`, `CancellationToken` | `Task` | Baut die Hub-Verbindung auf (Token je (Neu-)Verbindung). |
| `DisconnectAsync` | `CancellationToken` | `Task` | Trennt die Verbindung. |
| `ContinueWatchingUpdated` | – | `event EventHandler` | Server-Ereignis: Weiterschauen aktualisiert. |
| `FavoritesChanged` | – | `event EventHandler` | Server-Ereignis: Favoriten geändert. |
| `NewVideosScanned` | – | `event EventHandler<NewVideosScannedEventArgs>` | Server-Ereignis: neue Inhalte (trägt `SourceId`, `Count`) — erneuert indirekt den Quellen-Cache. |
| `StatusChanged` | – | `event EventHandler<string>` | Allgemeine Statusmeldung des Servers. |
| `Closed` | – | `event EventHandler` | Endgültige Trennung (kein Auto-Reconnect mehr). |
