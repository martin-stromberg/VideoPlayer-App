# Tests — Ausgangszustand und bestehende Abdeckung

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-09-22, ca. 22:10 Uhr (lokale Zeit, Windows)
- Branch und Commit-ID: `task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app-schritt-3-player-steuerung` @ `61cc2745303f308019840c10ae1903f36c8fe910` (`step: Schritt 2 fertig, Schritt 3 (Player-Steuerung) in Arbeit`)
- Uncommittete Änderungen im getesteten Stand: keine an Produktiv- oder Testcode; einzige untracked Datei ist die fremde Notiz `docs/features/task/issue-9-customer-feedback.md` (wird nicht angefasst). Die Lifecycle-Artefakte dieses Schritts (`docs/features/...schritt-3-player-steuerung/`) entstehen parallel und beeinflussen den Teststand nicht.
- Testumgebung und Runtime-/SDK-Versionen: Windows, .NET SDK 10.0.401, xUnit.net
- Ermittelte Testsuiten und Quellen der Testbefehle: `VideoWebPlayer.Maui.Tests` (einzige Testsuite der Solution `VideoPlayer.App.sln`; Befehl aus README/DoD). MAUI-App-Projekt ist im Testprojekt nicht referenziert — Player-Schicht wird über Quelltext-Contract-Tests gepinnt.

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| Baseline | `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj --logger "console;verbosity=normal"` | Repo-Stamm | 0 | 668 | 0 | 0 | [baseline-dotnet-test.log](test-results/baseline-dotnet-test.log) |

### Nachgewiesene bestehende Testfehler

Keine — der Baseline-Lauf war vollständig grün (668/668).

### Testlücken und Ausführungsprobleme

- Es existieren keine Rendering-, UI-Automatisierungs- oder E2E-Tests
  im Repository (etablierte und dokumentierte Projektkonvention). Die
  MAUI-Schicht kann im Testprojekt nicht instanziiert werden; Layout-
  und Verdrahtungsregeln werden daher über Quelltext-Contract-Tests
  abgesichert (Muster: `SourcesContractTests`, `DesignTokensContractTests`,
  `PlayerContractTests`).
- Die eigentliche Bedienbarkeit der nativen Transportsteuerung ist
  plattformabhängiges Verhalten und kann nur auf dem Gerät
  (Windows-Vollansicht) final abgenommen werden; der Contract-Test
  pinnt die verursachende Struktur (Overlay belegt nicht die ganze
  Fläche).

## Testklassen (für diesen Bereich relevant)

### `PlayerContractTests` (`VideoWebPlayer.Maui.Tests/PlayerContractTests.cs`)

- `PlayerPage_RouteRegistered` — Route „player" in `AppShell.xaml.cs`
- `MauiApp_UsesMediaElement` — Toolkit-Paket, `UseMauiCommunityToolkitMediaElement`, `PlaybackSourceService`, `IPictureInPictureService` in `MauiProgram.cs`
- `IosInfoPlist_DeclaresAudioBackgroundMode` — `UIBackgroundModes`/`audio` in iOS `Info.plist`
- `PlayerPage_UsesMediaElementWithControls` — `MediaElement`, `ShouldAutoPlay`, `ShouldShowPlaybackControls="True"`, Media-Event-Handler in `PlayerPage.xaml`
- `PlayerPage_DispatchesPlaybackMessages` — alle Player-Messages + `SeekTo` + `PlaybackSourceKind.LocalFile`/`MediaSource.FromFile` im Code-Behind
- `PictureInPictureService_UsesCompactOverlayOnWindows` — Windows-CompactOverlay im PiP-Dienst
- `MediaDetailsPage_HasPlayButton` — „Abspielen"-Verdrahtung der Detailseite
- `EffectHandler_HandlesPlayerEffects` — Player-Effekte in `ShellAppEffectHandler`

### `AppUpdateTests_Player` (`VideoWebPlayer.Maui.Tests/AppUpdateTests_Player.cs`)

- MVU-Übergänge der Player-Messages (u. a. `IsPictureInPicture`-Zustand) — unverändert grün zu halten.

## Hilfsmethoden

### `RepositoryRoot` (`VideoWebPlayer.Maui.Tests/Stubs/`)

- `Find()` — liefert das Repository-Stammverzeichnis; Basis aller Quelltext-Contract-Tests.
