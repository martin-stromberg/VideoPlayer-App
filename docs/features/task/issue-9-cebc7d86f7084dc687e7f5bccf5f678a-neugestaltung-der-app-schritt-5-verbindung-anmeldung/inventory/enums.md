# Enums

## `AppPage`

Datei: `VideoPlayer.Mvu/AppPage.cs`

| Wert | Bedeutung |
|------|-----------|
| `Loading` | Start-Ladeanzeige (kein Menüeintrag) |
| `Home` | Startseite (Menüroute `home`) |
| `Sources` | Quellen (`sources`) |
| `Favorites` | Favoriten (`favorites`) |
| `Downloads` | Downloads (`downloads`) |
| `Profile` | Profil (`profile`) |
| `Settings` | Einstellungen (`settings`) |
| `ServerSetup` | Server-Einrichtung/-verwaltung (keine Menüroute; Window-Root beim Erststart bzw. Route `serversetup`) |

Kein `Login`-Wert vorhanden.

## `ServerHealthStatus`

Datei: `VideoPlayer.Mvu/ServerHealthStatus.cs`

| Wert | Bedeutung |
|------|-----------|
| `Unknown` | Noch nicht geprüft |
| `Checking` | Erreichbarkeitsprüfung läuft |
| `Reachable` | Server erreichbar |
| `Unreachable` | Server nicht erreichbar |

## `DiscoverySource`

Datei: `VideoPlayer.ApiClient/Discovery/DiscoverySource.cs` — `Mdns`, `Udp`
(Herkunft eines gefundenen Server-Kandidaten; nicht direkt betroffen).

## Referenz (Altbestand): `ConnectionState`

Datei: `VideoWebPlayer.Maui/Services/IConnectionService.cs` — `Connected`,
`NeedsServerSetup`, `NeedsLogin`, `Offline`. In der neuen App existiert kein
Verbindungszustand-Enum; das Pendant ist anzulegen.
