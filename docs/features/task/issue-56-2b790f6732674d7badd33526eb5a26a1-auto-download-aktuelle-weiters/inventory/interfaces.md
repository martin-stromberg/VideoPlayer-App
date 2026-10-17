# Interfaces — Bestandsaufnahme

## `INetworkStatusMonitor`
Datei: `VideoPlayer.Mvu/INetworkStatusMonitor.cs` (Z. 8–29)

Netzwerkstatus-Abstraktion (Implementierung am App-Rand über MAUI-Connectivity); im `AutoDownloadService` für die WLAN-Bedingung (`WifiOnlyDownloads && !IsWifi`) und in `DownloadService` beim Transfer-Start genutzt.

| Member | Parameter | Rückgabewert | Zweck |
|--------|-----------|--------------|-------|
| `IsOnline` | — | `bool` | Letzter bekannter Netzwerkstatus. |
| `IsWifi` | — | `bool` | Ungedrosseltes Festnetz-Profil (WLAN oder Ethernet) — Grundlage von „Downloads nur im WLAN". |
| `AvailabilityChanged` | — | `event EventHandler<bool>?` | Ereignis bei Netzwerk-Wechsel. |
| `Start()` | — | `void` | Startet die Überwachung (idempotent). |

## `IDownloadBackend`
Datei: `VideoPlayer.Mvu/IDownloadBackend.cs` (Z. 19–41)

Transfer-Abstraktion des `DownloadService` (iOS: systemseitige Hintergrund-Session; sonst prozessinterner `HttpClient`-Stream). Für den Vorlauf nur indirekt relevant — die Einreihe-Semantik (`EnqueueAsync`) liegt vollständig im `DownloadService`.

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `TransferAsync` | `string url`, `string? token`, `string targetPath`, `bool wifiOnly`, `IProgress<(long Downloaded, long Total)> progress`, `CancellationToken` | `Task<DownloadBackendResult>` | Führt den Datei-Transfer aus und meldet Fortschritt. |

Zugehöriger Record `DownloadBackendResult(long BytesWritten, long TotalBytes, string? ErrorCode)` (Z. 10).

## Test-Infrastruktur-Contracts (`VideoPlayer.Storage`)

Von den Test-Stubs implementiert (siehe [tests.md](tests.md) → Hilfsmethoden):

| Interface | Einsatz |
|-----------|---------|
| `ISettingsStore` | Nicht-geheime Ablage — `InMemorySettingsStore` für `AppSecretsService` in `AutoDownloadServiceTests`. |
| `ISecretStore` | Geheimnis-Ablage — `InMemorySecretStore` für `AppSecretsService` in `AutoDownloadServiceTests`. |

Hinweis: Der Vorlauf nutzt den konkreten `VideoPlayerApiClient` über `AuthSessionService.Client` — es existiert kein eigenes Interface für die verwendeten Methoden `GetTvShowAsync`/`GetPlaylistEntriesPagedAsync`.
