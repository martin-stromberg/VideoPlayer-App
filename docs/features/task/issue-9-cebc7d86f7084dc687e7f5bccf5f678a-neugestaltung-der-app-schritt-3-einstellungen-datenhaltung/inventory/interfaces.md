# Interfaces

## Neue App

### `IAppEffectHandler`
Datei: `VideoPlayer.Mvu/IAppEffectHandler.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `HandleAsync` | `AppEffect`, `CancellationToken` | `Task` | Führt MVU-Effekte am App-Rand aus; Implementierung `ShellAppEffectHandler` |

### `IServerAddressProvider`
Datei: `VideoPlayer.Maui/Services/IServerAddressProvider.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetServerAddress` | – | `Uri` | Basisadresse des VideoPlayer-Servers |

### `IServerHealthProbe`
Datei: `VideoPlayer.Maui/Services/IServerHealthProbe.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `CheckAsync` | `CancellationToken` | `Task<bool>` | Erreichbarkeitsprüfung via `VideoPlayerApiClient.CheckHealthAsync` |

## Referenz Alt-App

### `ISettingsService` (`VideoWebPlayer.Maui/Services/ISettingsService.cs`)
Einstellungs-Contract der Alt-App: `ServerAddress`, Credentials (`Username` in
`Preferences`, `Passkey`/`AuthToken` in `SecureStorage`), Retention-Settings,
Server-Discovery (mDNS/UDP). Deckt die neu geforderten Einstellungen **nicht**
ab (kein Auto-Download/Auto-Fortsetzung/WLAN-only/Zeitfenster/Kontingent/
Auto-Löschung) — wird nicht wiederverwendet.

## Nicht vorhanden

- Keine Abstraktion für `Preferences`/`SecureStorage` in der neuen App
  (`ServerAddressProvider` greift direkt auf `Preferences.Default` zu).
- Keine Abstraktion für Ablageorte/`FileSystem`.
