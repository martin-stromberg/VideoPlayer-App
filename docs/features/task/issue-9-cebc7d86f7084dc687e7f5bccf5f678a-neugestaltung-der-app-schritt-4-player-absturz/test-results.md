# Test-Ergebnisse

## Ergebnis

**Status:** Keine Fehler

## Zusammenfassung

- Gesamt: 672
- Bestanden: 672
- Fehlgeschlagen: 0
- Übersprungen: 0

| Lauf | Befehl | Ergebnis | Nachweis |
|------|--------|----------|----------|
| Build Debug | `dotnet build VideoPlayer.App.sln -c Debug` | Erfolgreich, 0 Fehler (270 Warnungen, Altbestand/Plattform) | [build-debug.log](build-debug.log) |
| Build Release | `dotnet build VideoPlayer.App.sln -c Release` | Erfolgreich, 0 Fehler (339 Warnungen, Altbestand/Plattform) | [build-release.log](build-release.log) |
| Volle Testsuite | `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj -c Debug` | **672/672 bestanden** (669 Ausgangsstand + 3 neue Contract-Tests), 0 fehlgeschlagen, 0 übersprungen | [test-run-full.log](test-run-full.log) |
| Negativprobe | `dotnet test --filter` der drei neuen Contract-Tests auf dem Ausgangsstand (`PlayerPage.xaml.cs` per `git stash` zurückgenommen) | **Alle 3 fehlgeschlagen wie erwartet** (Abo im Konstruktor, kein Marshal, kein `subscribed`-Guard) — die Pins erfassen die tatsächliche Korrektur; danach wiederhergestellt und grün | — |

## Neue Tests dieses Schritts

- `PlayerContractTests.PlayerPage_UnsubscribesModelChangedOnDisappearing`
- `PlayerContractTests.PlayerPage_MarshalsNativeElementAccessOnMainThread`
- `PlayerContractTests.PlayerPage_IgnoresModelChangesAfterDisappearing`

## Absturz-Nachweis (Akzeptanzkriterium)

Die `COMException 0x8001010E` (`RPC_E_WRONG_THREAD`) entstand durch
`SeekTo` aus dem `MediaOpened`-Event, das auf einem Threadpool-Thread
feuern kann, sowie durch Modell-Reaktionen verwaister `PlayerPage`-
Instanzen auf ihrem losgelösten `MediaElement`. Beide Ursachen sind
beseitigt und per Contract-Test gepinnt (s. o.).

Ein automatisierter Gerätetest existiert im Repository nicht
(dokumentierte Lücke — die MAUI-Schicht ist im Testprojekt nicht
instanziierbar). Dokumentierter Geräte-Workflow zur manuellen
Verifikation:

1. App starten, Detailseite eines Mediums öffnen, **„Abspielen"**.
2. Wiedergabe läuft (ggf. Fortsetzungsposition wird angesprungen).
3. Zurück zur Detailseite, anderes (oder dasselbe) Medium **„Abspielen"**.
4. Wiederholen — die Wiedergabe startet beliebig oft ohne Absturz;
   Fortschrittsmeldung (Position wandert in „Weiterschauen") und
   „angesehen"-Markierung am Ende funktionieren unverändert.

## Testabdeckung

**Abdeckung:** Nicht messbar (kein Coverage-Collector-Lauf; Abdeckung
wird projektweit nicht ausgewertet — Konvention der Vor-Schritte).

## Fehlende Tests

- `VideoPlayer.Maui/Pages/PlayerPage.xaml.cs` — keine instanziierbaren
  Unit-Tests möglich (MAUI-Laufzeit); Abdeckung über Contract-Tests,
  wie für alle Seiten des Projekts.
