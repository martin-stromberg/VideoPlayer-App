# Interfaces und Abstraktionen

## Bestehende Interfaces (neuer Bestand)

- `IAppEffectHandler` (`VideoPlayer.Mvu`) — `HandleAsync(AppEffect, CancellationToken)`; implementiert von `ShellAppEffectHandler`.
- `IServerHealthProbe`, `IServerAddressProvider` (`VideoPlayer.Maui/Services`) — Erreichbarkeitsprüfung, Adressauflösung.
- `ISettingsStore`, `ISecretStore` (`VideoPlayer.Storage`) — Ablage-Abstraktionen (InMemory-Stubs im Testprojekt vorhanden).
- `IServerDiscovery` (`VideoPlayer.ApiClient.Discovery`) — mDNS/UDP-Erkennung.

## Fehlende Abstraktionen (für diese Anforderung neu)

- `INetworkStatusMonitor` — Kapselung von `Microsoft.Maui.Networking.Connectivity` (plattformneutral im MVU-Projekt, Implementierung in `VideoPlayer.Maui`), damit `AppUpdate`- und Service-Tests ohne MAUI-Laufzeit laufen.
- `IServerEventsConnection` — Abstraktion der Echtzeit-Verbindung (SignalR), damit Verbindungsorchestrierung (`ServerEventsService`) mit Fake-Verbindung testbar bleibt; `HubConnection` ist nicht sinnvoll mockbar.

## Server-seitige Schnittstelle

SignalR-Hub `/hubs/mediaupdate`, JWT-Authorization (Bearer; Token bei
WebSockets als `access_token`-Query — vom `AccessTokenProvider` des
`Microsoft.AspNetCore.SignalR.Client` genau so erzeugt). Ereignisse siehe
[models.md](models.md).
