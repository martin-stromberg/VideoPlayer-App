# Detail: Test-Ausgangszustand

## Ausgangslauf (vor jeder Aenderung)

- Datum/Uhrzeit: 2026-02-09 (lokale Zeit, UTC+1)
- Branch: `task/issue-17-ios-deploy-fix`
- Commit: `0546a54` (docs: Projektplan Issue 17 - Testbarkeit und Onboarding)
- Uncommittete Aenderungen zum Laufzeitpunkt:
  `VideoPlayer.Maui/VideoPlayer.Maui.csproj` (ApplicationVersion-Bump,
  nicht Teil dieses Schritts), `docs/projects/…/todo.md` (Projekt-Todo),
  untracked: `.ios-deploy.user.json`, `status.html`, `steps.md`
  (Projektverzeichnis)
- Umgebung: Windows, dotnet SDK 10.0.401, pwsh 7.6.6,
  Windows PowerShell 5.1 vorhanden
- Befehl: `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj`
  (Arbeitsverzeichnis: Repo-Root)
- Exit-Code: 0
- Ergebnis: **883 erfolgreich, 0 fehlgeschlagen, 0 uebersprungen,
  883 gesamt** (Dauer ~8 s)

Kompilierungswarnungen (CS0108, CS8618 in `VideoWebPlayer.Client`) sind
bekannter, preexistierender Zustand und fuehren zu keinem Fehlschlag.

## Bestehende Testabdeckung im Bereich

- `PairingContractTests.IosDeploymentScript_PropagatesAndMasksApiToken`
  (`VideoWebPlayer.Maui.Tests/PairingContractTests.cs` Z. 163–172):
  pinnt per Quelltext-Pruefung, dass das Skript `VideoPlayerApiToken`
  und `VIDEOWEBPLAYER_MAUI_API_TOKEN` enthaelt und der Token maskiert
  wird.
- **Luecke:** Kein Test prueft die Response-Datei-Erzeugung
  (`Add-PropertyLine`/`New-ResponseFile`) oder das Verhalten bei
  unzulaessigen Zeichen — genau die Stelle des MSB1006-Fehlers.
- `RepositoryRoot.Find()` (`Stubs/RepositoryRoot.cs`) liefert den
  Repo-Root fuer Quelltext-Contract-Tests; `pwsh` steht auf dem
  Entwicklungsrechner zur Verfuegung — ein Ausfuehrungstest der
  Skript-Logik ist moeglich, wenn sie in eine separat dot-source-bare
  Datei extrahiert wird.
