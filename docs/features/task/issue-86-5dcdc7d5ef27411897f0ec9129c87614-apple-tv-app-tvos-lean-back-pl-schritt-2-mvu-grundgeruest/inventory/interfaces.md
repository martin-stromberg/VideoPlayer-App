# Interfaces — Bestandsaufnahme (Schritt 2)

Dateipfade relativ zum Repository-Root. **Fett** = für tvOS ist ein neuer
Adapter oder eine Verschiebung nötig.

## `IAppEffectHandler`
`VideoPlayer.Mvu/IAppEffectHandler.cs` — einzige Plattform-Anlaufstelle des
MVU-Kerns.

| Methode | Parameter | Rückgabewert | Zweck |
|---|---|---|---|
| `HandleAsync` | `AppEffect effect`, `CancellationToken` | `Task` | Einzelnen Effekt am App-Rand ausführen |

Implementierung: `ShellAppEffectHandler` (`VideoPlayer.Maui/Services/`,
2396 Zeilen, deckt alle `AppEffect`-Subtypen per `switch` ab; nicht-abgedeckte
Fälle fallen still durch). **tvOS-Gegenstück erforderlich.**

## `ISettingsStore`
`VideoPlayer.Storage/ISettingsStore.cs` — nicht-geheime Schlüssel/Wert-Ablage.

| Methode | Parameter | Rückgabewert |
|---|---|---|
| `Contains` | `key` | `bool` |
| `GetString` | `key`, `defaultValue` | `string` |
| `GetInt` | `key`, `defaultValue` | `int` |
| `GetBool` | `key`, `defaultValue` | `bool` |
| `Set` | `key`, `string`/`int`/`bool` (3 Überladungen) | `void` |
| `Remove` | `key` | `void` |

Implementierung: `PreferencesSettingsStore` (`VideoPlayer.Maui/Services/`,
kapselt `IPreferences`). **tvOS-Adapter auf `NSUserDefaults` erforderlich.**

## `ISecretStore`
`VideoPlayer.Storage/ISecretStore.cs` — sichere Ablage (Keychain-Äquivalent;
Inhalte nicht im Cloud-Backup).

| Methode | Parameter | Rückgabewert |
|---|---|---|
| `GetAsync` | `key` | `Task<string?>` (`null` = nicht vorhanden) |
| `SetAsync` | `key`, `value` | `Task` |
| `RemoveAsync` | `key` | `Task<bool>` |

Implementierung: `SecureStorageSecretStore` (`VideoPlayer.Maui/Services/`,
kapselt `ISecureStorage`; `GetAsync` fängt Fehler → `null`; `SetAsync` mit
leerem Wert → `Remove`). **tvOS-Adapter auf `Security.SecItem` (Schlüsselbund,
`SecKind.GenericPassword`) erforderlich.**

## `AppStorageLayout` (Klasse, kein Interface)
`VideoPlayer.Storage/AppStorageLayout.cs` — drei Wurzeln vom Aufrufer
plattformspezifisch aufgelöst: `backupRoot`, `noBackupRoot`, `cacheRoot`;
profil-isolierte Unterverzeichnisse, `EnsureDirectoriesExist()`. MAUI-Auflösung:
`AppStorageLayoutProvider` (`VideoPlayer.Maui/Services/`, `FileSystem.
AppDataDirectory`/`CacheDirectory` + `SetSkipBackupAttribute` unter iOS/
MacCatalyst, `%APPDATA%` unter Windows, `NoBackupFilesDir` unter Android).
**tvOS-Wurzelauflösung erforderlich — kein Backup-Konzept, `Library/Caches`-
basiert; Profil via `AppProfile.FromEnvironment()` (`VIDEOPLAYER_PROFILE`).**

## `INetworkStatusMonitor`
`VideoPlayer.Mvu/INetworkStatusMonitor.cs`

| Member | Typ | Zweck |
|---|---|---|
| `IsOnline` | `bool` | Letzter bekannter Netzwerkstatus |
| `IsWifi` | `bool` | Ungedrosseltes Netz (WLAN/Ethernet) — Grundlage `WifiOnlyDownloads` |
| `AvailabilityChanged` | `event EventHandler<bool>?` | Wechsel des Netzwerkstatus |
| `Start()` | `void` | Überwachung starten (idempotent) |

Implementierung: `ConnectivityNetworkStatusMonitor` (`VideoPlayer.Maui/
Services/`, MAUI `Connectivity`; `IsWifi` zählt WiFi **und** Ethernet).
**tvOS-Adapter auf `Network.NWPathMonitor` erforderlich** (`SnapshotHandler`
+ `SetQueue` + `Start`; `NWPath.Status == NWPathStatus.Satisfied`;
`UsesInterfaceType(Wifi|Wired)`).

## `IServerAddressProvider`
`VideoPlayer.Maui/Services/IServerAddressProvider.cs` — **faktisch
plattformneutral** (keine MAUI-Abhängigkeit).

| Methode | Rückgabewert | Zweck |
|---|---|---|
| `GetConfiguredServerAddressAsync` | `Task<Uri?>` | Hinterlegte Adresse (`null` = Erststart) |
| `GetServerAddressAsync` | `Task<Uri>` | Mit Fallback: `VIDEOPLAYER_SERVER_URL` → `http://localhost/videoplayer/` |

Implementierung `ServerAddressProvider` (nur `VideoPlayer.Storage`-
Abhängigkeit: `KnownServersService`, `ServerAddress`). **Lage MAUI-seitig —
für tvOS-Nutzung an neutrale Stelle verschieben oder duplizieren.**

## `IServerHealthProbe`
`VideoPlayer.Maui/Services/IServerHealthProbe.cs` — **faktisch
plattformneutral.**

| Methode | Parameter | Rückgabewert |
|---|---|---|
| `CheckAsync` | `CancellationToken` | `Task<HealthCheckResult>` |
| `CheckAsync` | `Uri baseAddress`, `CancellationToken` | `Task<HealthCheckResult>` |

Implementierung `ServerHealthProbe` (`VideoPlayerApiClient.CheckHealthAsync`
→ `api/health`; Gate-Schlüssel adressspezifisch via `AppSecretsService` +
`ClientGate.ResolveStatic`; `HealthCheckResult` in `VideoPlayer.ApiClient`).
**Lage MAUI-seitig — wie `IServerAddressProvider`.**

## `IServerEventsConnection`
`VideoPlayer.Mvu/IServerEventsConnection.cs` — `IAsyncDisposable`; Ereignisse
`ContinueWatchingUpdated`, `FavoritesChanged`, `NewVideosScanned`,
`StatusChanged`, `Closed`; `ConnectAsync(Uri, Func<Task<string?>>, ct)`,
`DisconnectAsync`, `IsConnected`.

Implementierung: `SignalRServerEventsConnection` (`VideoPlayer.Maui/
Services/`, `Microsoft.AspNetCore.SignalR.Client`, Hub `hubs/mediaupdate`,
`WithAutomaticReconnect`). **Faktisch plattformneutral, aber MAUI-seitig —
SignalR-Client ist net10.0-kompatibel und auf tvOS verwendbar.**

## Plattformneutrale Injektions-Hooks (kein Interface)
- `Func<IServerEventsConnection>` — Konstruktorparameter von `ServerEventsService` (VideoPlayer.Mvu).
- `ClientGate.DeviceTokenProvider` (`VideoPlayer.ApiClient/ClientGate.cs`) — statischer Hook, von `DeviceTokenService` verdrahtet.
- `ILoggerFactory`/`ILogger<T>` — optionale Konstruktorparameter der Dienste (`NullLogger`-Fallbacks); `FileLoggerProvider`/`AppLogService` in `VideoPlayer.Storage`.
