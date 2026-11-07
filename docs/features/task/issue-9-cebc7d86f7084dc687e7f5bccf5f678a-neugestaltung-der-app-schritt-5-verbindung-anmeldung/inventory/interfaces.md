# Interfaces

## `IAppEffectHandler`

Datei: `VideoPlayer.Mvu/IAppEffectHandler.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `HandleAsync` | `AppEffect effect`, `CancellationToken` | `Task` | Führt einen MVU-Effekt am App-Rand aus |

## `IServerHealthProbe`

Datei: `VideoPlayer.Maui/Services/IServerHealthProbe.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `CheckAsync` | `CancellationToken` | `Task<bool>` | Prüft die aktuell aufgelöste Serveradresse |
| `CheckAsync` | `Uri baseAddress`, `CancellationToken` | `Task<bool>` | Prüft eine konkrete Kandidatenadresse |

## `IServerAddressProvider`

Datei: `VideoPlayer.Maui/Services/IServerAddressProvider.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetConfiguredServerAddressAsync` | `CancellationToken` | `Task<Uri?>` | Hinterlegte/konfigurierte Adresse; `null` = kein Server hinterlegt |
| `GetServerAddressAsync` | `CancellationToken` | `Task<Uri>` | Effektive Adresse inkl. IIS-Default-Fallback |

## `ISecretStore`

Datei: `VideoPlayer.Storage/ISecretStore.cs` — sichere Ablage (SecureStorage);
Inhalte nicht im Cloud-Backup.

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetAsync` | `string key` | `Task<string?>` | Geheimnis lesen |
| `SetAsync` | `string key`, `string value` | `Task` | Geheimnis schreiben |
| `RemoveAsync` | `string key` | `Task<bool>` | Geheimnis entfernen |

## `ISettingsStore`

Datei: `VideoPlayer.Storage/ISettingsStore.cs` — normale Ablage
(`GetString`/`GetInt`/`GetBool`, `Set`-Overloads, `Remove`, `Contains`).

## `IServerDiscovery`

Datei: `VideoPlayer.ApiClient/Discovery/IServerDiscovery.cs` —
`DiscoverAsync(CancellationToken) → IReadOnlyList<DiscoveredServer>`
(bestehende Discovery; nicht direkt betroffen).

## `VideoPlayerApiClientOptions.TokenRefresher` (Extension Point)

Datei: `VideoPlayer.ApiClient/VideoPlayerApiClientOptions.cs` —
`Func<CancellationToken, Task<AuthToken?>>`; wird bei 401 pro Anfrage höchstens
einmal aufgerufen. Aktuell nirgends produktiv gesetzt.
