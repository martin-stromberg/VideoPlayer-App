# Bestandsaufnahme: Absturz beim Start eines Videos unter iOS

Analysiert wurde der komplette Wiedergabe-Startpfad der App
`de.martinstromberg.videos` (Prozess `VideoPlayer.Maui`, MVU-Architektur)
bezogen auf den TestFlight-Crash-Report `EXC_CRASH (SIGABRT)` auf
Thread 0, zusammen mit den Crash-Artefakten, der iOS-Plattformkonfiguration
und den Build-/Symbolisierungs-Artefakten.

## Zusammenfassung

- **Wiedergabe-Startpfad vollständig vorhanden:** „Abspielen"/▶ auf
  `MediaDetailsPage` → `PlayRequestedMessage` → `AppUpdate` (neuer
  `PlayerState` + `NavigateToPlayerEffect` + `ResolvePlaybackSourceEffect`)
  → `ShellAppEffectHandler` (Shell-Route `player` +
  `PlaybackSourceService`) → `PlayerPage` mit
  `CommunityToolkit.Maui.MediaElement` 10.0.0 (`AVPlayer`/
  `AVPlayerViewController` auf iOS). Quelle: lokale Download-Datei
  (`LocalFile`/`MediaSource.FromFile`) oder Server-Stream mit
  `access_token` (`ServerStream`/`MediaSource.FromUri`); Fortsetzungsposition
  aus Weiterschauen-Liste/Cache → `SeekTo` in `OnMediaOpened`.
- **Stärkster Crash-Verdächtiger (am Code + Toolkit-Quelle verifiziert):**
  `MediaElementHandler.MapSeekRequested` ist `async void`; die
  iOS-`PlatformSeek` wirft `InvalidOperationException`, solange
  `AVPlayer.CurrentItem` `null` bzw. `Status != ReadyToPlay` ist. Das
  Toolkit feuert `MediaOpened` **vor** `ReadyToPlay` (bekanntes
  Issue #3249) — die appseitige `SeekTo` auf `ResumePosition > 0`
  trifft genau dieses Fenster → unbehandelte Ausnahme im `async void`
  → `abort()` → SIGABRT auf Thread 0. Betroffen wären nur Medien mit
  Weiterschauen-Stand — deckt sich mit „Absturz beim Start eines
  Videos" (kein Resume → kein `SeekTo` → kein Absturzpfad).
  Zweiter Toolkit-Pfad: `throw` im `Player.Seek`-Completion-Callback
  bei `complete == false`.
- **Crash-Build identisch mit aktuellem Player-Code:** Build 213 =
  Commit `a9a5a75` (Buildnummer = `git rev-list --count`; Verifizierung
  über Git-Historie). `PlayerPage.xaml.cs` ist zwischen diesem Commit
  und HEAD unverändert — die bekannten Härtungen (verwaiste-Seiten-
  Guard `subscribed`, `MainThread`-Marshal von `SeekTo`/Positionszugriff,
  Lifecycle-Abo in `OnAppearing`/`OnDisappearing` — Fixes aus
  „Schritt 4 Player-Absturz" für Windows) waren im Absturz-Build
  bereits enthalten. Der iOS-Absturz liegt damit außerhalb dieser
  abgesicherten Pfade.
- **Appseitige Absturzquellen im Startpfad:** native `VideoElement`-
  Zugriffe in `UpdateUi` (`Source =`, `Stop()`) und `SeekTo` laufen
  zwar korrekt auf dem UI-Thread, aber vollständig **ohne
  try/catch** — jede durchschlagende Ausnahme (verwaltet oder aus dem
  `async void` des Handlers) ist fatal. Es gibt keine globalen
  Exception-Hooks (`Program.cs`, `AppDelegate.cs`, `App.xaml.cs`
  registrieren keinen `UnhandledException`-/`UnobservedTaskException`-
  Handler).
- **Symbolisierung blockiert:** Der CI-Store-Build
  (`.github/actions/package-ios`) archiviert nur die `.ipa` — es gibt
  **keine dSYM-Erzeugung/-Archivierung** in CI oder
  `scripts/iOS-Deployment.ps1`. Der Report bleibt unsymbolisiert, bis
  der Build auf `a9a5a75` reproduziert und die dSYM aus dem
  Build-Output verwendet wird (auch die Frage aus `requirement.md`
  „Sind dSYMs verfügbar?" — im Repo/CI: nein).
- **Fehlerpfad-Konvention bereits vorhanden:** Quell-/Abspielfehler
  landen als `PlayerState.Error` → `ErrorPanel`/`ErrorLabel` —
  `PlaybackFailedMessage` (`MediaFailed`) und fehlgeschlagene
  Quellauflösung nehmen diesen Weg inkl. Episoden-Rückfall
  (`AdvanceToNextEpisode`).
- **Lücke:** Keine Tests für das iOS-Laufzeitverhalten des
  `MediaElement` (nur quelltextbasierte Contract-Tests); kein Test
  pinnt Fehlerisolierung am nativen Player-Rand.

## Details

- [Codepfad beim Start eines Videos](inventory/playback-start-path.md) —
  Dispatch-Kette, Effekt-Ausführung, MediaElement/AVPlayer-Aufbau,
  Seek-Ablauf und die am Code verifizierten Crash-Verdächtigen (§6).
- [Crash-Artefakte und Build-/Symbolisierungslage](inventory/crash-artifacts.md) —
  `feedback.json`/`crashlog.crash` ausgewertet, Commit-Zuordnung,
  dSYM-Befund, iOS-Plist, Toolkit-Version/-Befunde.
- [Datenmodell](inventory/models.md) — `PlayerState`, `PlaybackSource`,
  `PlayerEpisodeRef`, Player-Messages/-Effekte, `DownloadManifestEntry`,
  `CachedListEntry`, `ContinueWatchingEntry`.
- [Logik](inventory/logic.md) — `PlayerPage`, `ShellAppEffectHandler`
  (Player-Teile), `AppUpdate`-Player-Übergänge, `PlaybackSourceService`,
  `ProgressReportService`, `MvuHost`, Routing, iOS-Einstieg,
  `PictureInPictureService`.
- [Enums](inventory/enums.md) — `PlaybackSourceKind`, `AppPage` (kein
  Player-Eintrag), externes `MediaElementState`.
- [Interfaces](inventory/interfaces.md) — `IAppEffectHandler`,
  `IPictureInPictureService`, Toolkit-`IMediaElement`.
- [Tests](inventory/tests.md) — `AppUpdateTests_Player`,
  `PlayerContractTests`, `PlaybackSourceServiceTests`, angrenzende
  Contract-Tests, Stubs.
