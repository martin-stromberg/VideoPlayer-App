# Interfaces — Bestandsaufnahme

Dateipfade relativ zum Repository-Root. Plattform-Abstraktionen sind fett markiert, wenn für tvOS eine neue Implementierung nötig ist (Stand: nur MAUI-Implementierungen vorhanden).

## `IAppEffectHandler`
`VideoPlayer.Mvu/IAppEffectHandler.cs` — MVU-Effekt-Ausführung am App-Rand.

| Methode | Parameter | Rückgabewert | Zweck |
|---|---|---|---|
| `HandleAsync` | `AppEffect effect`, `CancellationToken` | `Task` | Einzelnen Effekt ausführen (Navigation, Server-Aufrufe) |

Implementierung heute: `ShellAppEffectHandler` (`VideoPlayer.Maui/Services/ShellAppEffectHandler.cs`, 2396 Zeilen — deckt alle ~75 `AppEffect`-Subtypen per `switch` ab). **tvOS benötigt ein Gegenstück.**

## `IDownloadBackend`
`VideoPlayer.Mvu/IDownloadBackend.cs` — Transfer-Backend der Downloads.

| Methode | Parameter | Rückgabewert | Zweck |
|---|---|---|---|
| `TransferAsync` | `url`, `token`, `targetPath`, `wifiOnly`, `IProgress<(long Downloaded, long Total)> progress`, `CancellationToken` | `Task<DownloadBackendResult>` | Download-Transfer ausführen; Auftragsverwaltung bleibt im `DownloadService` |

`DownloadBackendResult(long BytesWritten, long TotalBytes, string? ErrorCode)` — Fehlercodes `network`, `http_*`.

Implementierungen: `HttpDownloadBackend` (`VideoPlayer.Mvu/HttpDownloadBackend.cs`, prozessinterner `HttpClient`-Stream — plattformneutral), `IosBackgroundDownloadBackend` (`VideoPlayer.Maui/Platforms/iOS/IosBackgroundDownloadBackend.cs`, `NSUrlSession`-Hintergrund-Session, `#if IOS`, `SessionIdentifier` = `videoplayer.downloads`, Delegate `INSUrlSessionDownloadDelegate`, `CompletionSink` → `DownloadService.CompleteExternalTransferAsync`). **tvOS: Prüfpunkt — Hintergrund-`NSURLSession` vs. Vordergrund-Transfer (offene Frage in requirement.md).**

## `INetworkStatusMonitor`
`VideoPlayer.Mvu/INetworkStatusMonitor.cs`

| Member | Typ | Zweck |
|---|---|---|
| `IsOnline` | `bool` | Letzter bekannter Netzwerkstatus |
| `IsWifi` | `bool` | Ungedrosseltes Netz (WLAN/Ethernet) — Grundlage `WifiOnlyDownloads` |
| `AvailabilityChanged` | `event EventHandler<bool>?` | Wechsel des Netzwerkstatus |
| `Start()` | `void` | Überwachung starten (idempotent) |

Implementierung: `ConnectivityNetworkStatusMonitor` (`VideoPlayer.Maui/Services/`, via `Microsoft.Maui.Networking.Connectivity`). **tvOS benötigt eigenen Adapter** (z. B. NWPathMonitor — tvOS ist typischerweise Ethernet/WLAN, `IsWifi`-Semantik zu klären).

## `IServerEventsConnection`
`VideoPlayer.Mvu/IServerEventsConnection.cs` — `IAsyncDisposable`.

| Member | Typ | Zweck |
|---|---|---|
| `IsConnected` | `bool` | Verbindung aufgebaut |
| `ConnectAsync` | `Uri hubAddress`, `Func<Task<string?>> accessTokenProvider`, `CancellationToken` → `Task` | Hub-Verbindung aufbauen (`hubs/mediaupdate`) |
| `DisconnectAsync` | `CancellationToken` → `Task` | Trennen |
| `ContinueWatchingUpdated` / `FavoritesChanged` / `NewVideosScanned` / `StatusChanged` / `Closed` | Events | Server-Ereignisse; `Closed` = endgültige Trennung |

Implementierung: `SignalRServerEventsConnection` (`VideoPlayer.Maui/Services/`, SignalR-Client mit `WithAutomaticReconnect`). **tvOS: SignalR-Client ist plattformneutral referenzierbar — Adapter ggf. nur zur Lebenszyklus-Steuerung.**

## `ISettingsStore`
`VideoPlayer.Storage/ISettingsStore.cs` — nicht-geheime Schlüssel/Wert-Ablage (Äquivalent `Preferences`).

| Methode | Parameter | Rückgabewert | Zweck |
|---|---|---|---|
| `Contains` | `key` | `bool` | Schlüssel vorhanden? |
| `GetString` | `key`, `defaultValue` | `string` | Zeichenfolge lesen |
| `GetInt` | `key`, `defaultValue` | `int` | Ganzzahl lesen |
| `GetBool` | `key`, `defaultValue` | `bool` | Wahrheitswert lesen |
| `Set` | `key`, `value` (`string`/`int`/`bool`, 3 Überladungen) | `void` | Wert schreiben |
| `Remove` | `key` | `void` | Schlüssel entfernen |

Implementierung: `PreferencesSettingsStore` (`VideoPlayer.Maui/Services/`, kapselt `IPreferences`). Test-Stubs: `InMemorySettingsStore`. **tvOS benötigt Adapter (z. B. `NSUserDefaults`).**

## `ISecretStore`
`VideoPlayer.Storage/ISecretStore.cs` — sichere Ablage (Äquivalent `SecureStorage`: Keychain/Credential-Locker; Inhalte nicht im Cloud-Backup).

| Methode | Parameter | Rückgabewert | Zweck |
|---|---|---|---|
| `GetAsync` | `key` | `Task<string?>` | Geheimnis lesen (`null` = nicht vorhanden) |
| `SetAsync` | `key`, `value` | `Task` | Geheimnis speichern |
| `RemoveAsync` | `key` | `Task<bool>` | Geheimnis entfernen |

Implementierung: `SecureStorageSecretStore` (`VideoPlayer.Maui/Services/`, kapselt `SecureStorage` mit Profil-/Fehlerbehandlung). Test-Stub: `InMemorySecretStore`. **tvOS benötigt Keychain-Adapter (`SecItem`).**

## `IServerDiscovery`
`VideoPlayer.ApiClient/Discovery/IServerDiscovery.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---|---|---|---|
| `DiscoverAsync` | `CancellationToken` | `Task<IReadOnlyList<DiscoveredServer>>` | Ein Discovery-Weg (mDNS/UDP); Fehler → leere Liste |

Implementierungen: `MdnsServerDiscovery` (Zeroconf, `_http._tcp.local.`), `UdpServerDiscovery` (Broadcast `:5001`, `VIDEOWEBPLAYER_DISCOVERY`-Protokoll). Aggregat: `ServerDiscoveryService` (parallel, dedupliziert). **tvOS: Zeroconf-Kompatibilität zu prüfen (offene Frage).**

## `IServerHealthProbe`
`VideoPlayer.Maui/Services/IServerHealthProbe.cs` — MAUI-seitig.

| Methode | Parameter | Rückgabewert | Zweck |
|---|---|---|---|
| `CheckAsync` | `CancellationToken` | `Task<HealthCheckResult>` | Gesundheitsprüfung des konfigurierten Servers |
| `CheckAsync` | `Uri baseAddress`, `CancellationToken` | `Task<HealthCheckResult>` | Gesundheitsprüfung eines Kandidaten |

Implementierung: `ServerHealthProbe` (via `ServerAddressProvider` + `VideoPlayerApiClient.CheckHealthAsync` → `api/health`).

## `IServerAddressProvider`
`VideoPlayer.Maui/Services/IServerAddressProvider.cs` — MAUI-seitig.

| Methode | Parameter | Rückgabewert | Zweck |
|---|---|---|---|
| `GetConfiguredServerAddressAsync` | `CancellationToken` | `Task<Uri?>` | Aktiv konfigurierte Adresse (`null` = keine) |
| `GetServerAddressAsync` | `CancellationToken` | `Task<Uri>` | Adresse mit Fallback (`VIDEOPLAYER_SERVER_URL` → `http://localhost/videoplayer/`) |

Implementierung: `ServerAddressProvider` (`EnvironmentVariable` = `VIDEOPLAYER_SERVER_URL`).

## `IPictureInPictureService`
`VideoPlayer.Maui/Services/IPictureInPictureService.cs` — MAUI-seitig (für tvOS nicht im MVP-Scope; `PlayerState.IsPictureInPicture` ist dennoch im Modell).

| Methode/Property | Typ | Zweck |
|---|---|---|
| `IsToggleSupported` / `IsActive` | `bool` | Plattform-Fähigkeit/Zustand |
| `ToggleAsync` | `Task<bool>` | PiP umschalten |
| `RestoreAsync` | `Task` | Aus PiP zurückkehren |

Implementierung: `PictureInPictureService` (MAUI `MediaElement`-Integration).

## Kein Interface, aber injizierbare Plattform-Anschlüsse
- `Func<HttpClient>` (Download-Transfer), `Func<string,bool> fileExists`, `Func<DateTime> utcNow`, `Func<IServerEventsConnection> connectionFactory`, `Func<CancellationToken,Task<AuthToken?>> TokenRefresher` — Test-/Plattformhaken in `DownloadService`, `PlaybackSourceService`, `MediaDetailsService`, `ServerEventsService`, `VideoPlayerApiClientOptions`.
- `ClientGate.DeviceTokenProvider` (`VideoPlayer.ApiClient/ClientGate.cs`) — statischer Hook, liefert je Anfrage das Geräte-Token für den `X-API-Key`-Header (verdrahtet auf `DeviceTokenService`).

## `DevApiEndpointHandler`
`msTools.DeveloperApi/DevApiEndpointHandler.cs` — `delegate Task<DevApiResponse>(DevApiRequest, CancellationToken)`; Routing-Kontrakt der Developer-API.
