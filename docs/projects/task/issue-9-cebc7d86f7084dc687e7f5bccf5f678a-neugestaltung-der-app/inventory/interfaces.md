# Interfaces

## `ISettingsService`

Datei: `VideoWebPlayer.Maui/Services/ISettingsService.cs` — Implementierung: `SettingsService`

| Methode / Property | Parameter | Rückgabewert | Zweck |
|--------------------|-----------|--------------|-------|
| `ServerAddress` | – | `string?` | Aktuelle Serveradresse |
| `HasServerAddress()` | – | `bool` | Serveradresse vorhanden? |
| `SetServerAddress(address)` | `string` | `void` | Serveradresse setzen |
| `ClearServerAddress()` | – | `void` | Serveradresse löschen |
| `GetUsername()` / `GetPassword()` | – | `string` | Gespeicherte Credentials lesen |
| `HasCredentials()` | – | `bool` | Credentials vollständig? |
| `SetCredentials(username, password)` | `string, string` | `void` | Credentials speichern |
| `ClearCredentials()` | – | `void` | Credentials + Token löschen |
| `GetAuthToken()` | – | `string?` | JWT lesen |
| `SetAuthToken(token)` | `string` | `void` | JWT speichern |
| `ClearAuthToken()` | – | `void` | JWT löschen |
| `PlaybackCacheRetentionDays` | get/set | `int` | Aufbewahrung Playback-Cache (Tage) |
| `WatchlistCacheRetentionDays` | get/set | `int` | Aufbewahrung Weiterschauen-Downloads (Tage) |
| `DownloadRetentionDays` | get/set | `int` | Aufbewahrung regulärer Downloads (Tage) |
| `DiscoverServersAsync(timeoutMs)` | `int = 3000` | `Task<IReadOnlyList<string>>` | mDNS-Servererkennung (Zeroconf) |
| `DiscoverServersUdpAsync(port, timeoutMs)` | `int = 5001`, `int = 2000` | `Task<IReadOnlyList<string>>` | UDP-Broadcast-Servererkennung |

## `IConnectionService`

Datei: `VideoWebPlayer.Maui/Services/IConnectionService.cs` — Implementierung: `ConnectionService`; enthält auch Enum `ConnectionState`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `TryConnectAsync` | `string baseAddress, CancellationToken` | `Task<bool>` | HealthCheck gegen Server |
| `StartConnectionWorkflowAsync` | `HomePageViewModel, IServiceProvider, CancellationToken` | `Task<ConnectionState>` | Start-Workflow (Server → Login → Offline) — koppelt an konkretes ViewModel |

## `IAuthService`

Datei: `VideoWebPlayer.Maui/Services/IAuthService.cs` — Implementierung: `AuthService`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `HasCredentials()` | – | `bool` | Credentials vorhanden? |
| `GetCredentials()` | – | `(string username, string password)` | Credentials lesen |
| `LoginAsync` | `string, string, CancellationToken` | `Task<(bool success, string? errorMessage)>` | Login gegen Server |
| `ClearCredentials()` | – | `void` | Credentials löschen |

## `IPublishNotificationEvent`

Datei: `VideoWebPlayer.Maui/Services/Events/IPublishNotificationEvent.cs` — Implementierung: `NotificationEventService`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `Publish<TEvent>` | `TEvent notificationEvent` (`where TEvent : NotificationEvent`) | `void` | Event an alle Subscriber |

## `ISubscribeNotificationEvent`

Datei: `VideoWebPlayer.Maui/Services/Events/ISubscribeNotificationEvent.cs` — Implementierung: `NotificationEventService`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `Subscribe<TEvent>` | `Action<TEvent> handler` | `void` | Handler für Event-Typ registrieren |
| `Unsubscribe<TEvent>` | `Action<TEvent> handler` | `void` | Handler entfernen |

## `IMediaBannerViewModel`

Datei: `VideoWebPlayer.Maui/Components/MediaBannerPlayer.xaml.cs` (Zeilen 355–366) — implementiert von `TVShowDetailsViewModel` und `MovieCollectionDetailsViewModel`

| Methode / Property | Parameter | Rückgabewert | Zweck |
|--------------------|-----------|--------------|-------|
| `GetVideoInfoForPlaybackAsync` | – | `Task<(long VideoId, string VideoType, string Title)?>` | Liefert Abspiel-Infos an `MediaBannerPlayer` |
| `ShouldShowPlayButton` | – | `bool` | Sichtbarkeit des Play-Buttons |
