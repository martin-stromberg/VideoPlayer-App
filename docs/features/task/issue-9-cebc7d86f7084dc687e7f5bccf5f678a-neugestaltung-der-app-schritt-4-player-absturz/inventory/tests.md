# Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-09-22, ca. 22:36 (lokale Zeit, Windows)
- Branch und Commit-ID: `task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app-schritt-4-player-absturz`, `2f370bafb679aa5669583ff7ca45aea712a589a7`
- Uncommittete Änderungen im getesteten Stand: keine Quelländerungen; untracked sind nur `docs/features/task/issue-9-customer-feedback.md` (Fremddatei des Anwenders) und das neu angelegte Feature-Verzeichnis dieses Schritts
- Testumgebung und Runtime-/SDK-Versionen: .NET SDK 10.0.401, Windows, MAUI-Workloads vorhanden
- Ermittelte Testsuiten und Quellen der Testbefehle: einzige Suite `VideoWebPlayer.Maui.Tests` (xUnit v3); DoD verlangt `dotnet build VideoPlayer.App.sln` Debug+Release und `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj`

## Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| Baseline Build Debug | `dotnet build VideoPlayer.App.sln -c Debug` | Repo-Root | 0 | — | 0 Buildfehler (281 Warnungen, Altbestand) | — | [baseline-build-debug.log](test-results/baseline-build-debug.log) |
| Baseline Test | `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj -c Debug --logger "console;verbosity=normal"` | Repo-Root | 0 | 669 | 0 | 0 | [baseline-dotnet-test.log](test-results/baseline-dotnet-test.log) |

## Nachgewiesene bestehende Testfehler

Keine — der Ausgangslauf ist vollständig grün (669/669).

## Testlücken und Ausführungsprobleme

- Keine Geräte-/UI-Automatisierung im Repository: Die MAUI-Schicht
  (`VideoPlayer.Maui`) kann im Testprojekt nicht instanziiert werden;
  Seiten werden über Quelltext-Contract-Tests gepinnt
  (Konvention, dokumentiert in `PlayerContractTests.cs:7-13`).
- Der konkrete Absturz (`COMException 0x8001010E` im nativen
  `MediaPlayerElement`) ist damit nicht als automatisierter Test
  reproduzierbar — Nachweis erfolgt über Contract-Tests der
  Korrekturstruktur (Unsubscribe, UI-Thread-Marshal) plus
  dokumentierten Geräte-Workflow.

## Testklassen (player-relevant)

### `PlayerContractTests`

Datei: `VideoWebPlayer.Maui.Tests/PlayerContractTests.cs`

- `PlayerPage_RouteRegistered` — Route `player` in `AppShell`
- `MauiApp_UsesMediaElement` — Toolkit-Paket und `UseMauiCommunityToolkitMediaElement`
- `IosInfoPlist_DeclaresAudioBackgroundMode` — iOS-Hintergrund-Audio
- `PlayerPage_UsesMediaElementWithControls` — MediaElement-Verdrahtung in XAML
- `PlayerPage_DispatchesPlaybackMessages` — alle Player-Messages inkl. `SeekTo`
- `PlayerPage_OverlayChromeDoesNotCoverPlaybackSurface` — Hit-Test-Pin (Schritt 3)
- `PictureInPictureService_UsesCompactOverlayOnWindows` — Mini-Player
- `MediaDetailsPage_HasPlayButton` — Wiedergabe-Einstieg
- `EffectHandler_HandlesPlayerEffects` — Player-Effekte verdrahtet

### `AppUpdateTests_Player`

Datei: `VideoWebPlayer.Maui.Tests/AppUpdateTests_Player.cs`

Deckt den MVU-Teil ab (PlayRequested, Progress-Throttling, Watched-
Schwelle, Auto-Fortsetzung, PlayerClosed) — unverändert relevant, da der
MVU-Zyklus nicht angefasst wird.

## Hilfsmethoden

### `RepositoryRoot` (`VideoWebPlayer.Maui.Tests/Stubs/`)

- `RepositoryRoot.Find()` — liefert das Repo-Root für Quelltext-Contract-Tests.
