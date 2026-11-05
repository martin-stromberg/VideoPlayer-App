# Test-Ergebnisse

## Ergebnis

**Status:** Keine Fehler

## E2E-Abdeckung

Kein tvOS-E2E-Pfad vorhanden — die Plattform ist auf dem Entwicklungsrechner nicht ausführbar; Abdeckung über Contract-Tests (`TvOsPlaybackContractTests`, 5 Tests), die plattformneutralen MVU-Übergangstests (`AppUpdateTests_Player`, `AppUpdateTests_PlaylistPlayback`, `PlaybackSourceServiceTests` u. a.) und den macOS-CI-Job `build-tvos` (Kompiliernachweis).

## Reale End-to-End-Abnahme

| Szenario | Durchgeführt über | Ergebnis |
|----------|-------------------|----------|
| Wiedergabe Server-Stream/lokale Datei auf tvOS | tvOS-Simulator/Gerät — lokal nicht ausführbar; Nachweis über macOS-CI-Build | Verzichtet (Plattform-Beschränkung, dokumentiert im Projektplan Schritt 2/7: „tvOS-Lauffähigkeit wird über den macOS-CI-Build und Simulator geprüft") |

## Zusammenfassung

- Befehl: `dotnet build VideoPlayer.App.sln` + `dotnet test VideoPlayer.Maui.Tests/VideoPlayer.Maui.Tests.csproj --no-build --logger "console;verbosity=minimal"`
- Gesamt: 1492
- Bestanden: 1492
- Fehlgeschlagen: 0
- Übersprungen: 0

Zwischenstand während der Umsetzung: `TvOsHomePageContractTests.TvOsEffectHandler_MapsSelectionEffectsToPreparedTarget` schlug nach der echten Verdrahtung fehl (Platzhalter-Assertion) — Test und Gegenstellen in `TvOsDetailsContractTests`/`TvOsHomePageContractTests` an die reale Verdrahtung angepasst; `PresentPreparedTarget`/`PreparedTargetViewController` als toten Code entfernt.

## Testabdeckung

**Abdeckung:** Nicht messbar (kein Coverage-Collector im Projekt-Lauf eingerichtet; tvOS-Quelldateien unter Windows ohnehin nicht instrumentierbar).

## Fehlende Tests

- `VideoPlayer.TvOS/Ui/TvosPlayerViewController.cs` — tvOS-Code, lokal nicht kompilierbar; abgesichert über `TvOsPlaybackContractTests` (Quell-Contract) und den `build-tvos`-CI-Job
- `VideoPlayer.TvOS/Ui/TvosPlayerInfoViewController.cs` — dto.
