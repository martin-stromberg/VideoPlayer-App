# Tests und exakter E2E-Betrieb

## Vorhandene Testabdeckung

- `VideoPlayer.Maui.Tests/HomeListServiceTests.cs`: plattformneutrale Tests für Cache-Mapping der vorhandenen Home-Listen.
- `VideoPlayer.Maui.Tests/HomePageContractTests.cs`: Quelltext-Contract-Tests zur HomePage-Struktur und den drei Listenbindungen.
- `VideoPlayer.E2E.Tests/Support/MockVideoServer.cs`: lokaler HTTP-Mock implementiert vorhandene Home-/Sync-Endpunkte; `GET api/items/recent` liefert konfigurierbare Neuheiten. Genre-Optionen und SourceGenres sind für Playlist-Editor bzw. Quellen-Browsing vorhanden, nicht als saisonale Home-Listen.
- In `VideoPlayer.E2E.Tests/Tests/` gibt es aktuell 19 Testdateien. Die Doku `docs/help/e2e-tests.md` nennt an einer Stelle „19 Tests“, später aber „Szenarien (25 Tests)“; die Zählung in der Dokumentation ist uneinheitlich. Für Planung/Ergebnis ist die tatsächlich ausgeführte Testausgabe maßgeblich.

## Exakter lokaler Windows-E2E-Ablauf

Voraussetzung laut `docs/help/e2e-tests.md`: Windows mit Desktop-Session, installiertem passenden .NET SDK/Workloads und verfügbarem Windows-MAUI-Build. Die Tests bauen den Debug-App-Target selbst und starten die gebaute EXE pro Test isoliert. Der konkrete Befehl aus dem Repository lautet:

```powershell
dotnet test VideoPlayer.E2E.Tests/VideoPlayer.E2E.Tests.csproj
```

Die Suite läuft sequenziell (`[Collection("E2E")]`), startet einen eigenen `VideoPlayer.Maui.exe`-Prozess, automatisiert UIA3/FlaUI und prüft den MVU-Zustand über die Developer-API. `MockVideoServer` lauscht lokal auf Loopback und einem dynamischen Port; keine echte Serverbereitstellung ist für bestehende Szenarien nötig. Aktuell wird der Recent-Karussell-Fluss etwa in Wiedergabe-Szenarien über `AutomationId="RecentCarousel"` per Developer-API `/invoke` plus `itemIndex` ausgewählt.

## Abdeckungslücke für diese Anforderung

Für den saisonalen Fall fehlen Mock-Vertrag, saisonale Testdaten und ein E2E-Szenario, das Anzeige, Auswahl und Zeitfenster prüft. Ein Mock-E2E kann die mobile Integration testen. Die korrekte Saisonwahl/Zeitraumfilterung muss zusätzlich am tatsächlichen Serververtrag geprüft werden; ohne serverseitige Komponente lässt sich diese Logik nicht lokal in der bestehenden Suite beweisen. Der oben angegebene Befehl wurde in dieser Bestandsaufnahme nicht ausgeführt.
