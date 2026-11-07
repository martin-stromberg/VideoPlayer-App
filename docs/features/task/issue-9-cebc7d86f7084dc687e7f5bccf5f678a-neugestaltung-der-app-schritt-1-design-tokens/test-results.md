# Test-Ergebnisse

## Ergebnis

**Status:** Keine Fehler

Ausgeführte Befehle:

| Befehl | Ergebnis |
|--------|----------|
| `dotnet build VideoPlayer.App.sln --configuration Debug` | 0 Fehler (272 Warnungen, sämtlich Bestand: Legacy-App `VideoWebPlayer.Maui` und generierter XAML-SourceGen-Code) |
| `dotnet build VideoPlayer.App.sln --configuration Release` | 0 Fehler (339 Warnungen, allesamt Bestand) |
| `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj` | 657 bestanden, 0 fehlgeschlagen, 0 übersprungen |
| `dotnet format whitespace VideoPlayer.App.sln --verify-no-changes` | sauber, Exit 0 |

Baseline vor der Änderung: 647 Tests grün
(`inventory/test-results/baseline-dotnet-test.txt`). Jetzt: 657 grün —
die 10 neuen `DesignTokensContractTests` sind enthalten.

## Zusammenfassung

- Gesamt: 657
- Bestanden: 657
- Fehlgeschlagen: 0
- Übersprungen: 0

## E2E-Abdeckung

Der Plan (`plan.md`, Abschnitt „E2E-Tests") fordert begründet keine
E2E-Szenarien: reine Design-Umstellung ohne neuen/geänderten
Benutzerfluss; im Repository existiert keine Rendering-/E2E-
Infrastruktur für MAUI (etablierte Konvention: MVU-Tests +
quelltextbasierte Contract-Tests). Der Funktionsnachweis läuft über die
10 neuen Contract-Tests; die Bedienung bleibt durch die unverändert
grüne Gesamtsuite abgesichert.

## Testabdeckung

**Abdeckung:** 82,4 % Zeilen (XPlat Code Coverage / coverlet.collector;
`VideoWebPlayer.Maui.Tests/TestResults/*/coverage.cobertura.xml`)

| Paket | Abdeckung |
|-------|-----------|
| VideoPlayer.Storage | 98,7 % |
| VideoPlayer.Mvu | 88,2 % |
| VideoPlayer.ApiClient | 86,2 % |
| VideoWebPlayer.Client | 5,6 % |

Das geänderte Projekt `VideoPlayer.Maui` (MAUI-UI) ist per se nicht
instrumentierbar und wird durch die neuen quelltextbasierten
Contract-Tests abgesichert.

## Fehlende Tests

Quelle: `Coverage-Daten` (0 %-Abdeckung, alle Bestand — keine dieser
Dateien wurde in diesem Schritt angefasst oder ist für die Design-
Umstellung relevant):

- `VideoPlayer.ApiClient/Discovery/MdnsServerDiscovery.cs` — 0 % Abdeckung
- `VideoPlayer.ApiClient/Models/MediaEntry.cs` — 0 % Abdeckung
- `VideoPlayer.ApiClient/Models/MediaSource.cs` — 0 % Abdeckung
- `VideoPlayer.Mvu/MediaDetailsService.cs` — 0 % Abdeckung
- `VideoPlayer.Mvu/ServerEventsService.cs` — 0 % Abdeckung
- `VideoWebPlayer.Client/Models/AuthenticationRequest.cs` — 0 % Abdeckung
- `VideoWebPlayer.Client/Models/AuthorizationToken.cs` — 0 % Abdeckung
- `VideoWebPlayer.Client/Models/DtoMovie.cs` — 0 % Abdeckung
- `VideoWebPlayer.Client/Models/DtoRecentEntry.cs` — 0 % Abdeckung
- `VideoWebPlayer.Client/Models/DtoSource.cs` — 0 % Abdeckung
- `VideoWebPlayer.Client/Models/FavoriteEntry.cs` — 0 % Abdeckung
- `VideoWebPlayer.Client/Models/ImpersonateRequest.cs` — 0 % Abdeckung
- `VideoWebPlayer.Client/Models/MediaEntryDto.cs` — 0 % Abdeckung
- `VideoWebPlayer.Client/VideoWebPlayerClient.cs` — 0 % Abdeckung
