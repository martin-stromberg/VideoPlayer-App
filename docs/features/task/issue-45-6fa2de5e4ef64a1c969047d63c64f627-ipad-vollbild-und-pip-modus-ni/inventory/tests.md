# Tests — Bestandsaufnahme (Issue #45)

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-10-12 08:48 +02:00 (CEST, lokaler
  Windows-Rechner)
- Branch und Commit-ID:
  `task/issue-45-6fa2de5e4ef64a1c969047d63c64f627-ipad-vollbild-und-pip-modus-ni`,
  `c30604a296c1b070b23959115ee1866168dbb90b` („feat: Direkte Wiedergabe
  aus Weiterschauen, Protokollversand und Playlist-Support (Issue #43)
  (#44)")
- Uncommittete Änderungen im getesteten Stand: ausschließlich neue,
  ungetrackte Dateien unter
  `docs/features/task/issue-45-…/`
  (`requirement.md`, diese Bestandsaufnahme inkl. Test-Log) — **keine**
  Änderungen an Produktivcode, Tests oder Konfiguration
  (`git status --short` zeigt nur `?? docs/features/task/issue-45-…/`).
- Testumgebung und Runtime-/SDK-Versionen: .NET SDK `10.0.401`;
  installierte Workloads `maui-windows` 10.0.20, `android` 36.1.69,
  `ios` 26.5.10318, `maccatalyst` 26.5.10318; Teststack xUnit v3
  (`xunit.v3` 3.2.2, `Microsoft.NET.Test.Sdk` 18.8.1).
- Ermittelte Testsuiten und Quellen der Testbefehle:
  - `VideoWebPlayer.Maui.Tests` (Unit-/Contract-Tests, `net10.0`) —
    Befehl aus `README.md` („Bauen und Testen") und CI
    (`.github/workflows/pr-checks.yml`: `dotnet test … --no-build
    --configuration Release`).
  - `VideoPlayer.E2E.Tests` (Windows-E2E, `net10.0-windows`, FlaUI
    UIA3 + `MockVideoServer` + Developer-API) — Befehl und
    Voraussetzungen aus `docs/help/e2e-tests.md`; nicht Teil des
    CI-PR-Gates.

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| 1 | `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj --configuration Release` | Repository-Wurzel | `0` | 1256 | 0 | 0 | [test-results/dotnet-test-unit-release.log](test-results/dotnet-test-unit-release.log) |

Dauer des Testteils ~31 s. Der Lauf enthielt den Build aller
referenzierten Projekte (Release); bekannte, nicht testrelevante
Compiler-Warnungen im Altbestand `VideoWebPlayer.Client` (CS0108,
CS8618 — preexisting, kein Testfehler).

### Nachgewiesene bestehende Testfehler

Keine — der Baseline-Lauf der Unit-/Contract-Testsuite war vollständig
grün. Die E2E-Suite wurde nicht ausgeführt (siehe unten); über ihren
Ausgangszustand liegt kein Nachweis vor.

### Testlücken und Ausführungsprobleme

- **`VideoPlayer.E2E.Tests` nicht ausgeführt.** Die Suite baut die
  Windows-App in Debug, startet pro Test einen eigenen
  `VideoPlayer.Maui.exe`-Prozess mit sichtbarem Fenster und steuert ihn
  per FlaUI/UIA3; laut `docs/help/e2e-tests.md` ~20–25 min Laufzeit,
  sequenziell, Desktop-Session erforderlich. Umfang aktuell: 53
  `[Fact]`-/`[Theory]`-Attribute über 13 Testklassen in
  `VideoPlayer.E2E.Tests/Tests/` (die Doku nennt noch 19 Tests aus der
  Einführung; Playlist-/Protokoll-Suiten kamen später hinzu). Die
  Suite ist Windows-only und deckt das iOS-Overlay-Problem dieser
  Anforderung ohnehin nicht ab; sie wurde in der Bestandsaufnahme
  nicht ausgeführt (Konsistenz mit der bisherigen
  Bestandsaufnahme-Praxis, z. B. Schritt-3-Protokollversand). Über
  ihren Ausgangszustand wird keine Aussage getroffen — ein späterer
  Fehlschlag dort gilt nicht automatisch als preexisting.
- **Keine iOS-UI-Automatisierung im Repository.** Die Bedienbarkeit
  der nativen `AVPlayerViewController`-Transportleiste auf dem iPad
  ist automatisiert nicht prüfbar — die Konvention des Projekts sind
  quelltextbasierte Contract-Tests (`PlayerContractTests`), die
  Geräteverifikation läuft manuell über TestFlight. Dokumentierte
  Einschränkung, siehe Klassenkommentar `PlayerContractTests`
  (`VideoWebPlayer.Maui.Tests/PlayerContractTests.cs:6-14`) und
  `docs/help/wiedergabe-pip.md`.
- **Contract-Lücke für das iOS-Hit-Test-Problem:** der bestehende Pin
  `PlayerPage_OverlayChromeDoesNotCoverPlaybackSurface` prüft
  `VerticalOptions="Start"` am `OverlayChrome`-Layout und
  `InputTransparent="True"` am inneren `Border` — **nicht** die
  Eingabedurchlässigkeit des Elternlayouts selbst, die laut
  Anforderung unter iOS die Eingaben der nativen Buttons abfängt.

## Testklassen

### `PlayerContractTests`

Datei: `VideoWebPlayer.Maui.Tests/PlayerContractTests.cs` — 16
`[Fact]`s, quelltextbasierte Contract-Tests (die MAUI-Schicht ist im
Testprojekt nicht instanziierbar; geprüft werden XAML-/Quelltexte über
`XDocument` bzw. `Contains`-Assertionen, Muster
`SourcesContractTests`).

- `PlayerPage_RouteRegistered` — Route `player` in `AppShell.xaml.cs`
- `MauiApp_UsesMediaElement` — `CommunityToolkit.Maui.MediaElement`
  im csproj, `UseMauiCommunityToolkitMediaElement` +
  `PlaybackSourceService` + `IPictureInPictureService` in
  `MauiProgram.cs`
- `IosInfoPlist_DeclaresAudioBackgroundMode` — `UIBackgroundModes` /
  `audio` in `Platforms/iOS/Info.plist` (iOS-PiP-Voraussetzung)
- `PlayerPage_UsesMediaElementWithControls` — `MediaElement`,
  `ShouldAutoPlay`, `ShouldShowPlaybackControls="True"`,
  Media-Event-Handler
- `PlayerPage_DispatchesPlaybackMessages` — alle Player-Messages,
  `SeekTo`, `PlaybackSourceKind.LocalFile`/`MediaSource.FromFile`
- **`PlayerPage_OverlayChromeDoesNotCoverPlaybackSurface`** — der für
  diese Anforderung zentrale Pin: parst `PlayerPage.xaml` als XML und
  prüft `VerticalOptions="Start"` am `OverlayChrome`-Layout,
  `InputTransparent="True"` am inneren `Border`, kein `Button` im Band,
  `PipButton` als eigenes `PlayerRoot`-Element mit
  `Clicked="OnPipClicked"` (Kommentar verweist auf den
  Windows-Hit-Test-Blocker; iOS-Fall noch nicht abgedeckt)
- `PlayerPage_TapShowsTransportControlsOnWindows` — `#if WINDOWS`
  Tap-Gesture auf `PlayerRoot` → `TransportControls?.Show()` in
  `OnPlayerTapped`
- `PictureInPictureService_UsesCompactOverlayOnWindows` —
  `#if WINDOWS`, `AppWindowPresenterKind.CompactOverlay`/`Default`
- `MediaDetailsPage_HasPlayButton` — „Abspielen"-Verdrahtung der
  Detailseite
- `PlayerPage_UnsubscribesModelChangedOnDisappearing` —
  `ModelChanged`-Abo im Sichtbarkeitszyklus
- `PlayerPage_MarshalsNativeElementAccessOnMainThread` —
  `BeginInvokeOnMainThread` in `OnMediaOpened`/`OnMediaStateChanged`,
  genau ein `SeekTo(` im Code-Behind
- `PlayerPage_ResumeSeek_WaitsForReadyState` —
  `pendingResumePosition`-Gate auf `Playing`/`Paused`,
  `ResumeSeekTimeout`
- `PlayerPage_NativeAccessesAreFailureIsolated` — try/catch in
  `UpdateUi`, `ApplyResumeSeekAsync`, `ReportPosition`,
  `StartProgressTimer`, `TryApplyPendingResumeSeek`
- `PlayerPage_FailedSourceApply_KeepsStopPathReachable` —
  `sourceApplyFailed`-Invalidierung statt `appliedLocation = null`
- `PlayerPage_IgnoresModelChangesAfterDisappearing` —
  `subscribed`-Guards in allen Handlern
- `EffectHandler_HandlesPlayerEffects` — `NavigateToPlayerEffect`,
  `ResolvePlaybackSourceEffect`, `ReportPlaybackProgressEffect`,
  `TogglePictureInPictureEffect`, `ProgressReportService` in
  `ShellAppEffectHandler.cs`

### `AppUpdateTests_Player`

Datei: `VideoWebPlayer.Maui.Tests/AppUpdateTests_Player.cs` — 30
Tests des MVU-Reducers rund um `PlayerState`. Anforderungsrelevant:

- `PictureInPicture_TogglesFlag` — `PictureInPictureChangedMessage`
  setzt `IsPictureInPicture`
- `PictureInPicture_InactivePlayer_Unchanged` — PiP-Meldung ohne
  aktiven Player ohne Wirkung
- `PlayerClosed_ReportsFinalAndResets` /
  `PlayerClosed_WithoutPosition_ResetsOnly` — Reset-Pfad inkl.
  Overlay-Verlassen
- `PlaybackProgress_*`, `PlaybackEnded_*`, `PlaybackFailed_*` —
  Positions-/Ende-/Fehlerpfade, „angesehen"-Schwelle,
  Auto-Fortsetzung
- `PlaybackSourceResolved_*` — Quellenauflösung inkl.
  Episoden-Rückfall und Stale-Response-Verwerfung

### `AppUpdateTests_PlaylistPlayback`

Datei: `VideoWebPlayer.Maui.Tests/AppUpdateTests_PlaylistPlayback.cs`
— 25 Tests des Playlist-Kontexts (Basis der
`PlaylistNavigationBar`-Sichtbarkeit):

- `NextRequested_InPlaylist_LoadsNextEntry` /
  `PreviousRequested_InPlaylist_LoadsPreviousEntry` —
  `PlaylistNext/PreviousRequestedMessage` → Lade-Effekt
- `NavigationRequested_WithoutPlaylistOrWhileResolving_NoEffect`
- `NavigationCompleted_*` — Medienwechsel, Erschöpfung, Fehler,
  Stale-Identität
- `PlaybackEnded_InPlaylist_AdvancesPlaylist` u. a. Advance-Pfade

## Hilfsmethoden

### `PlayerContractTests` (lokal)

- `Read(params string[] parts)` — liest eine Quelldatei ab der
  Repo-Wurzel (`RepositoryRoot.Find()`)
- `MethodBody(string code, string signature)` — extrahiert den
  geklammerten Methodenrumpf ab einer Signaturmarke (für
  `Contains`/`DoesNotContain`-Assertionen auf Methodenebene)

### `VideoWebPlayer.Maui.Tests/Stubs`

- `RepositoryRoot.Find()` — findet die Repository-Wurzel (Grundlage
  aller Contract-Tests)
- `StubHttpMessageHandler` — HTTP-Stub mit Request-Aufzeichnung
- `InMemorySettingsStore` / `InMemorySecretStore` — In-Memory-Ablagen
- `SqliteDatabaseCollection` / `ClientGateCollection` —
  xUnit-Collections zur Serialisierung von Tests mit echten
  SQLite-Dateien bzw. Client-Gate

### `VideoPlayer.E2E.Tests/Support` (nicht ausgeführt, nur Kontext)

- `AppFixture` — einmaliger Debug-Build der App für alle E2E-Tests
- `AppInstance` — Exe-Start mit isoliertem Profil
  (`VIDEOPLAYER_PROFILE`), FlaUI-Attach
- `UiDriver`, `DevApiClient`, `MockVideoServer` — UI-Steuerung und
  Mock-Backend

## Dokumentationsberührung (für spätere Lifecycle-Schritte)

`docs/help/wiedergabe-pip.md` — Abschnitte „Bedienung in der
Vollansicht" (Zeilen ~24–42, beschreibt `OverlayChrome`/
`InputTransparent`/Mini-Player-Schalter) und „Picture-in-Picture /
Mini-Player" (Zeilen ~113–123, iOS-PiP über native Steuerung) sind die
fachlich betroffenen Stellen; die iOS-Verifikationsgrenze ist dort im
Muster früherer iOS-Fixe zu dokumentieren.
