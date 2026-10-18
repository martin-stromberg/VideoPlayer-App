# Test-Ergebnisse

## Ergebnis

**Status:** Keine Fehler

## Zusammenfassung

- Gesamt: 1392
- Bestanden: 1392
- Fehlgeschlagen: 0
- Übersprungen: 0

Ausführung:

- `VideoPlayer.Maui.Tests` (`dotnet test --no-build`, net10.0): 1381 Tests, alle bestanden.
- `VideoPlayer.E2E.Tests` (xUnit-v3-Runner `VideoPlayer.E2E.Tests.exe -class …`, Klassen `PlaybackTests`, `PlaylistPlaybackTests`, `ContinueWatchingPlaybackTests`): 11 Tests, alle bestanden (111,4 s).

## Testabdeckung

**Abdeckung:** 89,7 % (Zeilenabdeckung, coverlet/XPlat Code Coverage über `VideoPlayer.Maui.Tests`)

| Datei | Abdeckung |
|-------|-----------|
| `VideoPlayer.ApiClient/Discovery/MdnsServerDiscovery.cs` | 0,0 % |
| `VideoPlayer.Mvu/DownloadListItem.cs` | 55,2 % |
| `VideoPlayer.Mvu/PlaylistDetailsState.cs` | 65,5 % |
| `msTools.DeveloperApi/DevApiRequest.cs` | 66,7 % |
| `VideoPlayer.Mvu/ProfilePageState.cs` | 70,0 % |
| `VideoPlayer.Storage/FileLoggerProvider.cs` | 71,4 % |
| `VideoPlayer.ApiClient/Discovery/DiscoveredServer.cs` | 75,0 % |
| `VideoPlayer.Mvu/SourceItem.cs` | 75,0 % |
| `VideoPlayer.Mvu/MediaTypeNames.cs` | 77,3 % |
| `VideoPlayer.Mvu/ServerEventsService.cs` | 78,0 % |
| `VideoPlayer.ApiClient/Models/MediaEntry.cs` | 78,4 % |
| `VideoPlayer.Mvu/MediaDetailsView.cs` | 78,9 % |
| `msTools.DeveloperApi/DevApiHttpServer.cs` | 79,1 % |
| `VideoPlayer.ApiClient/Discovery/ServerDiscoveryService.cs` | 79,4 % |

## Fehlende Tests

Quelle: `Coverage-Daten`

- `VideoPlayer.ApiClient/Discovery/MdnsServerDiscovery.cs` — 0 % Abdeckung
