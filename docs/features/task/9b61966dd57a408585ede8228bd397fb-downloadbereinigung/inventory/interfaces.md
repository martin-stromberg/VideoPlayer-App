# Interfaces

## `IDownloadBackend`
Datei: `VideoPlayer.Mvu/IDownloadBackend.cs`

Abstraktion des Datei-Transfers; Auftragsverwaltung (Manifest, Warteschlange, Kontingent) bleibt im `DownloadService`. Implementierungen: `HttpDownloadBackend` (prozessinterner Stream), `IosBackgroundDownloadBackend` (iOS `NSUrlSession`-Hintergrund-Session).

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `TransferAsync` | `url` (string), `token` (string?), `targetPath` (string), `wifiOnly` (bool), `progress` (IProgress\<(long Downloaded, long Total)\>), `cancellationToken` | `Task<DownloadBackendResult>` | Lädt `url` nach `targetPath`, meldet Fortschritt; `ErrorCode` im Ergebnis |

## `INetworkStatusMonitor`
Datei: `VideoPlayer.Mvu/INetworkStatusMonitor.cs`

Netzwerkstatus-Erkennung (App-Rand: `ConnectivityNetworkStatusMonitor` über `Microsoft.Maui.Networking.Connectivity`); genutzt von `DownloadService` (WLAN-Bedingung der Pumpe) und `AutoDownloadService` (Lauf-Guard).

| Member | Typ | Zweck |
|--------|-----|-------|
| `IsOnline` | `bool` (Property) | Letzter bekannter Netzwerkstatus |
| `IsWifi` | `bool` (Property) | Ungedrosseltes Festnetz-Profil (WLAN/Ethernet) — Grundlage von `WifiOnlyDownloads` |
| `AvailabilityChanged` | `event EventHandler<bool>?` | Meldet Netzwerkwechsel → `NetworkAvailabilityChangedMessage` |
| `Start()` | Methode | Startet die Überwachung (idempotent) |

## `IAppEffectHandler`
Datei: `VideoPlayer.Mvu/IAppEffectHandler.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `HandleAsync` | `effect` (AppEffect), `cancellationToken` | `Task` | Führt einen MVU-Effekt am App-Rand aus (Implementierung: `ShellAppEffectHandler`) |

## `ISettingsStore`
Datei: `VideoPlayer.Storage/ISettingsStore.cs`

Nicht-geheime Schlüssel/Wert-Ablage (Preferences; Implementierung `PreferencesSettingsStore`), Grundlage der `AppSettings`-Persistenz.

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `Contains` | `key` | `bool` | Schlüssel vorhanden? |
| `GetString`/`GetInt`/`GetBool` | `key`, `defaultValue` | `string`/`int`/`bool` | Wert mit Default lesen |
| `Set` (überladen) | `key`, `value` | `void` | Wert schreiben |
| `Remove` | `key` | `void` | Schlüssel entfernen |
