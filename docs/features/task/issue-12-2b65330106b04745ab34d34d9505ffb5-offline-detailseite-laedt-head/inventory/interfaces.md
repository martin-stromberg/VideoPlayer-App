# Interfaces

## `INetworkStatusMonitor`

Datei: `VideoPlayer.Mvu/INetworkStatusMonitor.cs` (Implementierung am App-Rand: `VideoPlayer.Maui/Services/ConnectivityNetworkStatusMonitor.cs` über `Microsoft.Maui.Networking.Connectivity`)

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `IsOnline` | – | `bool` | Letzter bekannter Netzwerkstatus |
| `IsWifi` | – | `bool` | Ungedrosseltes Festnetz-Profil (WLAN/Ethernet) |
| `AvailabilityChanged` | – | `event EventHandler<bool>` | Meldet jeden Wechsel des Netzwerkstatus |
| `Start` | – | `void` | Startet die Überwachung (idempotent) |

Kontext: `ShellAppEffectHandler` abonniert `AvailabilityChanged` und dispatcht `NetworkAvailabilityChangedMessage` — der Startpunkt der Reconnect-Kette, an deren Ende `EnterConnected` steht. Die Netzwerkerkennung ist OS-seitig (`Connectivity`); ein Simulieren von Offline/Online in UI-E2E-Tests ist über die Dev-API (`/model`, `/tree`, `/invoke`, `/set-text`) nicht möglich.
