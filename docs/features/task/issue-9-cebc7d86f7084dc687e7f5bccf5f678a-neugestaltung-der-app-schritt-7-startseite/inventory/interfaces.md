# Interfaces

## `IAppEffectHandler`
Datei: `VideoPlayer.Mvu/IAppEffectHandler.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `HandleAsync` | `AppEffect effect`, `CancellationToken` | `Task` | Führt einen MVU-Effekt am App-Rand aus. Implementierung: `ShellAppEffectHandler` (`VideoPlayer.Maui/Services`) — `switch` über alle `AppEffect`-Subtypen. |

## `IServerEventsConnection`
Datei: `VideoPlayer.Mvu/IServerEventsConnection.cs` (`IAsyncDisposable`)

| Methode/Event | Parameter | Rückgabewert | Zweck |
|---------------|-----------|--------------|-------|
| `IsConnected` | – | `bool` | Verbindung aufgebaut |
| `ConnectAsync` | `Uri hubAddress`, `Func<Task<string?>> accessTokenProvider`, `CancellationToken` | `Task` | Hub-Verbindung aufbauen |
| `DisconnectAsync` | `CancellationToken` | `Task` | Verbindung trennen |
| `ContinueWatchingUpdated` | – | `event EventHandler` | Server-Ereignis Weiterschauen |
| `FavoritesChanged` | – | `event EventHandler` | Server-Ereignis Favoriten |
| `NewVideosScanned` | – | `event EventHandler<NewVideosScannedEventArgs>` | Server-Ereignis neue Inhalte (`SourceId`, `Count`) |
| `StatusChanged` | – | `event EventHandler<string>` | Statusmeldung (nur protokolliert) |
| `Closed` | – | `event EventHandler` | Endgültige Trennung |

Implementierung: `SignalRServerEventsConnection` (`VideoPlayer.Maui/Services`,
Hub `hubs/mediaupdate`, `WithAutomaticReconnect`); Orchestrierung in
`ServerEventsService` (bereits verdrahtet — keine Änderung nötig).

## `INetworkStatusMonitor`
Datei: `VideoPlayer.Mvu/INetworkStatusMonitor.cs` — Online/Offline-Erkennung;
unverändert, keine Erweiterung nötig.

## Übrige Storage-Abstraktionen
`ISettingsStore`, `ISecretStore` (`VideoPlayer.Storage`) — unverändert.
Für den Bild-Cache ist **kein** Interface im Kern nötig: der Bild-Dienst ist
eine reine App-Rand-Komponente (`VideoPlayer.Maui`), der MVU-Kern transportiert
nur die `PosterPictureId`.
