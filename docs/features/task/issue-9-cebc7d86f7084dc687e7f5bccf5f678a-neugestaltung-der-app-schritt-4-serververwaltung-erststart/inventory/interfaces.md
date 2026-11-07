# Interfaces — Bestandsaufnahme

## `IServerAddressProvider`

Datei: `VideoPlayer.Maui/Services/IServerAddressProvider.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetServerAddress` | — | `Uri` | Basisadresse des VideoPlayer-Servers (einzelne Adresse, keine Liste) |

## `IServerHealthProbe`

Datei: `VideoPlayer.Maui/Services/IServerHealthProbe.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `CheckAsync` | `CancellationToken` | `Task<bool>` | Erreichbarkeitsprüfung der aktuell aufgelösten Adresse |

## `IAppEffectHandler`

Datei: `VideoPlayer.Mvu/IAppEffectHandler.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `HandleAsync` | `AppEffect`, `CancellationToken` | `Task` | Effekt-Ausführung am App-Rand (Implementierung: `ShellAppEffectHandler`) |

## `ISettingsStore`

Datei: `VideoPlayer.Storage/ISettingsStore.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `Contains` | `string key` | `bool` | Schlüssel vorhanden? |
| `GetString` | `string key`, `string defaultValue` | `string` | String lesen |
| `GetInt` | `string key`, `int defaultValue` | `int` | Int lesen |
| `GetBool` | `string key`, `bool defaultValue` | `bool` | Bool lesen |
| `Set` | `string key`, `string`/`int`/`bool` | `void` | Wert schreiben |
| `Remove` | `string key` | `void` | Schlüssel entfernen |

## `ISecretStore`

Datei: `VideoPlayer.Storage/ISecretStore.cs`

Asynchrone sichere Ablage (`GetAsync`, `SetAsync`, `RemoveAsync`) —
für diese Anforderung nicht relevant (keine Geheimnisse), aber Muster für
plattformneutrale Abstraktionen.

## Nicht vorhanden (für die Anforderung relevant)

- Kein Interface für die Serververwaltung (bekannte Server, zuletzt verbunden).
- Kein Interface für Netzwerk-Discovery (mDNS/UDP).
- Kein Interface für die Prüfung einer **beliebigen** Serveradresse
  (`IServerHealthProbe` prüft nur die aufgelöste Adresse).
