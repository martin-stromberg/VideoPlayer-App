# Interfaces — Bestandsaufnahme

## `ISecretStore`
Datei: `VideoPlayer.Storage/ISecretStore.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetAsync` | `key: string` | `Task<string?>` | Geheimnis aus der sicheren Ablage lesen (`null` = nicht vorhanden). |
| `SetAsync` | `key: string`, `value: string` | `Task` | Geheimnis speichern. |
| `RemoveAsync` | `key: string` | `Task<bool>` | Geheimnis entfernen (`true` = Eintrag entfernt). |

Plattform-Äquivalent von `SecureStorage` (iOS-Keychain, Windows-Credential-Ablage); Inhalte gelangen nicht ins Cloud-Backup. Implementierung: `SecureStorageSecretStore` (VideoPlayer.Maui); Test-Fake: `InMemorySecretStore`. **Zielablage des Geräte-Tokens laut Anforderung — unverändert nutzbar.**

## `ISettingsStore`
Datei: `VideoPlayer.Storage/ISettingsStore.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `Contains` | `key: string` | `bool` | Schlüssel vorhanden? |
| `GetString` | `key: string`, `defaultValue: string` | `string` | Zeichenfolge lesen. |
| `GetInt` | `key: string`, `defaultValue: int` | `int` | Ganzzahl lesen. |
| `GetBool` | `key: string`, `defaultValue: bool` | `bool` | Wahrheitswert lesen. |
| `Set` | `key: string`, `value: string/int/bool` | `void` | Wert speichern (drei Überladungen). |
| `Remove` | `key: string` | `void` | Schlüssel entfernen. |

Nicht-geheime Ablage (`Preferences` via `PreferencesSettingsStore`); Test-Fake: `InMemorySettingsStore`. Für das Geräte-Token **nicht** vorgesehen (Anforderung: kein Preferences).

## `IAppEffectHandler`
Datei: `VideoPlayer.Mvu/IAppEffectHandler.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `HandleAsync` | `effect: AppEffect`, `cancellationToken: CancellationToken` | `Task` | Führt einen MVU-Effekt am App-Rand aus. Implementierung: `ShellAppEffectHandler` — dort muss der Pairing-Effekt (`ExchangePairingCodeEffect` o. ä.) einen neuen Zweig erhalten. |

## `IServerHealthProbe`
Datei: `VideoPlayer.Maui/Services/IServerHealthProbe.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `CheckAsync` | `cancellationToken` | `Task<bool>` | Erreichbarkeit der aufgelösten Adresse prüfen. |
| `CheckAsync` | `baseAddress: Uri`, `cancellationToken` | `Task<bool>` | Konkrete Kandidatenadresse prüfen (Einrichtung/Wechsel/Discovery). Implementierung `ServerHealthProbe` erzeugt `new VideoPlayerApiClient(httpClient)` ohne Options → `ClientGate.Resolve()` greift. |

## `IServerAddressProvider`
Datei: `VideoPlayer.Maui/Services/IServerAddressProvider.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetConfiguredServerAddressAsync` | `cancellationToken` | `Task<Uri?>` | Hinterlegte/konfigurierte Adresse (letzter verbundener Server → `VIDEOPLAYER_SERVER_URL`); `null` = Erststart. |
| `GetServerAddressAsync` | `cancellationToken` | `Task<Uri>` | Effektive Adresse inkl. IIS-Default `http://localhost/videoplayer/` — nie `null`. |

## `INetworkStatusMonitor` / `IServerEventsConnection` (Kontext)
Dateien: `VideoPlayer.Mvu/INetworkStatusMonitor.cs`, `VideoPlayer.Mvu/IServerEventsConnection.cs`

Netzwerkstatus-Überwachung (`AvailabilityChanged`-Event) und SignalR-Ereignisverbindung — für den Pairing-Flow nur indirekt relevant (Online/Offline-Übergänge des Verbindungsworkflows).
