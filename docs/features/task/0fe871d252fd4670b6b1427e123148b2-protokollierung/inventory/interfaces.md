# Interfaces

## `IAppEffectHandler`
Datei: `VideoPlayer.Mvu/IAppEffectHandler.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `HandleAsync` | `AppEffect`, `CancellationToken` | `Task` | Führt einen MVU-Effekt am App-Rand aus (einzige Implementierung: `ShellAppEffectHandler`) |

## `INetworkStatusMonitor`
Datei: `VideoPlayer.Mvu/INetworkStatusMonitor.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `IsOnline` | — | `bool` | `NetworkAccess.Internet`/`ConstrainedInternet` |
| `IsWifi` | — | `bool` | WiFi oder Ethernet (WLAN-Bedingung der Downloads) |
| `AvailabilityChanged` | — | `event EventHandler<bool>` | Online-/Offline-Wechsel — abonniert von `ShellAppEffectHandler` |
| `Start` | — | `void` | Beginnt die Beobachtung (`StartNetworkMonitoringEffect`) |

Implementierung: `ConnectivityNetworkStatusMonitor` (`VideoPlayer.Maui/Services`).

## `IServerEventsConnection`
Datei: `VideoPlayer.Mvu/IServerEventsConnection.cs` (`IAsyncDisposable`)

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `IsConnected` | — | `bool` | Verbindungszustand |
| `ConnectAsync` | `Uri hubAddress`, `Func<Task<string?>> accessTokenProvider`, `CancellationToken` | `Task` | Verbindungsaufbau zum Hub `hubs/mediaupdate` |
| `DisconnectAsync` | `CancellationToken` | `Task` | Trennen |
| `ContinueWatchingUpdated` | — | `event EventHandler` | Server-Event |
| `FavoritesChanged` | — | `event EventHandler` | Server-Event |
| `NewVideosScanned` | — | `event EventHandler<NewVideosScannedEventArgs>` | Server-Event (SourceId/Count) |
| `StatusChanged` | — | `event EventHandler<string>` | Hub-Statusmeldung — wird von `ServerEventsService.OnStatusChanged` aktuell per `Debug.WriteLine` ausgegeben |
| `Closed` | — | `event EventHandler` | Endgültige Trennung (nach Auto-Reconnect) |

Implementierung: `SignalRServerEventsConnection` (`VideoPlayer.Maui/Services`); Erzeugung über `Func<IServerEventsConnection>` in `MauiProgram` (Z. 87–88).

## `ISecretStore`
Datei: `VideoPlayer.Storage/ISecretStore.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetAsync` | `string key` | `Task<string?>` | Geheimnis lesen (Tokens, Zugangsdaten) |
| `SetAsync` | `string key`, `string value` | `Task` | Geheimnis schreiben |
| `RemoveAsync` | `string key` | `Task<bool>` | Geheimnis entfernen |

Implementierung: `SecureStorageSecretStore` (`VideoPlayer.Maui/Services`); Test-Fake: `InMemorySecretStore`.

## `ISettingsStore`
Datei: `VideoPlayer.Storage/ISettingsStore.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `Contains` | `string key` | `bool` | Schlüssel vorhanden |
| `GetString` / `GetInt` / `GetBool` | `string key`, Default | `string`/`int`/`bool` | Einstellung lesen |
| `Set` | `string key`, `string`/`int`/`bool` | `void` | Einstellung schreiben |
| `Remove` | `string key` | `void` | Einstellung entfernen |

Implementierung: `PreferencesSettingsStore`; Test-Fake: `InMemorySettingsStore`.

## `IServerHealthProbe`
Datei: `VideoPlayer.Maui/Services/IServerHealthProbe.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `CheckAsync` | `CancellationToken` | `Task<HealthCheckResult>` | Prüft den konfigurierten Server |
| `CheckAsync` | `Uri baseAddress`, `CancellationToken` | `Task<HealthCheckResult>` | Prüft eine Kandidatenadresse |

Implementierung: `ServerHealthProbe`.

## `IPictureInPictureService`
Datei: `VideoPlayer.Maui/Services/IPictureInPictureService.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `IsToggleSupported` | — | `bool` | Plattform kann PiP umschalten (Sichtbarkeit des Schalters) |
| `IsActive` | — | `bool` | PiP-/Mini-Player aktiv |
| `ToggleAsync` | — | `Task<bool>` | Umschalten (Windows-Overlay) |
| `RestoreAsync` | — | `Task` | Vollfenster wiederherstellen (beim Verlassen der PlayerPage) |

Implementierung: `PictureInPictureService`.

## `IServerAddressProvider`
Datei: `VideoPlayer.Maui/Services/IServerAddressProvider.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetConfiguredServerAddressAsync` | `CancellationToken` | `Task<Uri?>` | Konfigurierte Adresse (ggf. `null`) |
| `GetServerAddressAsync` | `CancellationToken` | `Task<Uri>` | Aufgelöste Adresse (wirft ohne Konfiguration) |

Implementierung: `ServerAddressProvider`.

## `VideoPlayerApiClientOptions.Logger` (Eigenschafts-Haken)
Datei: `VideoPlayer.ApiClient/VideoPlayerApiClientOptions.cs` (Z. 38)

`public ILogger? Logger { get; set; }` — optionaler Logger für Diagnoseausgaben des `VideoPlayerApiClient` (Wiederholungen, Token-Erneuerung); aktuell nirgends gesetzt.
