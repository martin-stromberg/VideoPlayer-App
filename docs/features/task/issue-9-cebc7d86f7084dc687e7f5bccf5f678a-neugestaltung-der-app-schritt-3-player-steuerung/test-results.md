# Test-Ergebnisse — Player-Steuerung in der Vollansicht (Schritt 3)

## Ergebnis

**Status:** Keine Fehler

## Ausgeführte Prüfungen

| Lauf | Befehl | Ergebnis | Nachweis |
|------|--------|----------|----------|
| Build Debug | `dotnet build VideoPlayer.App.sln -c Debug` | Erfolgreich, 0 Fehler (270 Warnungen, alle Altbestand/Plattform — keine neuen durch diesen Schritt) | [build-debug.log](build-debug.log) |
| Build Release | `dotnet build VideoPlayer.App.sln -c Release` | Erfolgreich, 0 Fehler (339 Warnungen, Altbestand/Plattform) | [build-release.log](build-release.log) |
| Volle Testsuite | `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj -c Debug` | **669/669 bestanden**, 0 fehlgeschlagen, 0 übersprungen | [test-run-full.log](test-run-full.log) |
| Negativprobe | `dotnet test --filter PlayerPage_OverlayChromeDoesNotCoverPlaybackSurface` auf dem Ausgangsstand (XAML-Korrektur per `git stash` zurückgenommen) | **Fehlgeschlagen wie erwartet** an der `VerticalOptions`-Assertion — der Pin erfasst die tatsächliche Blockade-Struktur; danach wiederhergestellt und grün | — |

## Neue Tests

- `PlayerContractTests.PlayerPage_OverlayChromeDoesNotCoverPlaybackSurface` —
  pinnt, dass `OverlayChrome` die Videofläche nicht mehr vollständig belegt
  (`VerticalOptions="Start"`), das Kopfband eingabedurchlässig ist
  (`InputTransparent="True"`) und der Mini-Player-Schalter davon ausgenommen
  bleibt (`InputTransparent="False"`, `Clicked="OnPipClicked"`).

## E2E-Abdeckung

| Szenario | Test / Testklasse | Ergebnis |
|----------|-------------------|----------|
| Steuerelemente in der Vollansicht bedienbar | `PlayerContractTests.PlayerPage_OverlayChromeDoesNotCoverPlaybackSurface` (Contract-Pin — keine UI-/E2E-Infrastruktur im Repository, dokumentierte Projektkonvention) | Bestanden |

## Zusammenfassung

- Gesamt: 669
- Bestanden: 669
- Fehlgeschlagen: 0
- Übersprungen: 0

## Testabdeckung

**Abdeckung:** Nicht messbar (kein Coverage-Collector konfiguriert; die
Änderung betrifft eine XAML-Layout-Eigenschaft, die nur über den
Contract-Pin und die Endabnahme auf dem Gerät nachweisbar ist).

## Fehlgeschlagene Tests

Keine.
