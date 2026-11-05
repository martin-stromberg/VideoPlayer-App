# Bestandsaufnahme: tvOS-Wiedergabe mit Fortschritts-Tracking, Auto-Fortsetzung und Spurwahl (Schritt 7)

Analysiert wurde die vorhandene Wiedergabe-Infrastruktur (plattformneutraler MVU-Kern, MAUI-Referenzimplementierung, tvOS-App-Gerüst) bezogen auf den Projektschritt 7 aus `docs/projects/task/issue-86-5dcdc7d5ef27411897f0ec9129c87614-apple-tv-app-tvos-lean-back-pl/project-plan.md`.

## Zusammenfassung

- **Plattformneutrale Wiedergabelogik ist vollständig vorhanden** und unverändert nutzbar: `PlayerState` (Quelle, Position, Pufferung, `IsWatched`, `EndWindowStreak`, Serien-Fortsetzungsliste `UpcomingEpisodes`, Playlist-Kontext mit `PlaylistId`/`PlaylistEntryId`/`PlaylistPosition`/`PlaylistTotalCount`/`PlaylistNextExhausted`, `ProgressReportPlaylistId`, `DownloadExpiryProtected`), `AppUpdate`-Handler für `PlayRequestedMessage`, `PlayerPageOpenedMessage`, `PlaybackSourceResolvedMessage`, `PlaybackProgressMessage` (5-s-Takt-Semantik, 0,5-s-Throttling, Endfenster mit `EndWindowRequiredTicks = 2`, `appliedMediaId`-Guard), `PlaybackEndedMessage` (Plausibilitätsfenster `PlaybackEndedToleranceSeconds = 300`), `PlaybackFailedMessage` (Rückfall), `PlayerClosedMessage` (Abschluss-Fortschritt + Reset), Playlist-Navigation (`PlaylistNextRequestedMessage`, `PlaylistPreviousRequestedMessage`, `PlaylistNavigationCompletedMessage`, `AdvancePlaylistEntry`).
- **Alle benötigten Effekte/Messages existieren bereits**: `NavigateToPlayerEffect`, `ResolvePlaybackSourceEffect`, `LoadUpcomingEpisodesEffect`, `ReportPlaybackProgressEffect`, `StartPlaylistPlaybackEffect`, `LoadNextPlaylistEntryEffect`, `LoadPreviousPlaylistEntryEffect`, `AdvancePlaylistEntryEffect`, `ProtectDownloadExpiryEffect`, `ExpireWatchedDownloadEffect`.
- **Alle benötigten plattformneutralen Dienste existieren**: `PlaybackSourceService` (lokale Datei vor Stream, Fortsetzungsposition, `allowServerStream`, `resumePositionSecondsOverride`), `ProgressReportService` (Server-Meldung bzw. `pending_progress`-Einreihung), `UpcomingEpisodesService` (Serienkontext ohne Detailseite), `PlaylistService` (`StartPlaybackAsync`/`GetNextEntryAsync`/`GetPreviousEntryAsync`/`AdvanceAsync`), `DownloadService` (`ExtendExpiryAsync`/`ExpireAfterAsync`).
- **tvOS-App ist verdrahtet bis zur Wiedergabe-Kante**: `TvosNavigator` (modale Stapel-Präsentation über `TopmostPresented`, `Render`-Verteilung an `IModelRenderable`-Seiten), `MediaDetailsViewController` dispatcht bereits `PlayRequestedMessage`, `HomePageViewController` dispatcht `ContinueWatchingItemSelectedMessage` (abspielbare Typen starten per `StartPlayback`; playlist-gebundene Einträge per `StartPlaylistPlaybackEffect`). `NavigateToPlayerEffect` zeigt bisher den `PreparedTargetViewController`-Platzhalter; `ResolvePlaybackSourceEffect` und alle übrigen Player-Effekte laufen ins protokollierte No-Op.
- **MAUI-Referenz `PlayerPage.xaml.cs`**: liefert das zu übersetzende Ereignis-Muster — 5-s-`Dispatcher`-Timer meldet nur bei `Playing`; `StateChanged` meldet Zustandswechsel sofort; `MediaOpened` merkt den Fortsetzungs-Seek bis `ReadyToPlay`; `MediaEnded`/`MediaFailed` dispatcht Ende/Fehler; `OnDisappearing` dispatcht `PlayerClosedMessage`; Quellanwendung mit `appliedLocation`/`appliedMediaId`/`sourceApplyFailed`/`pendingResumePosition`.
- **tvOS-Kompilierung** erfolgt ausschließlich über den macOS-CI-Job `build-tvos` (Push-Trigger `task/issue-86-**`); lokal ist nur der degradierte `net10.0`-Hüllen-Build möglich.

Test-Ausgangszustand: `dotnet test VideoPlayer.Maui.Tests` — 1488/1488 bestanden, keine bekannten Fehlschläge; Nachweis in [inventory/tests.md](inventory/tests.md).

## Details

- [Modelle und Zustand](inventory/models.md) — `PlayerState`, `PlaybackSource`, `AppModel`-Bezüge
- [Logik und Dienste](inventory/logic.md) — `AppUpdate`-Player-Handler, Dienste, tvOS-Verdrahtung, MAUI-Referenz
- [Enums](inventory/enums.md) — `PlaybackSourceKind`
- [Interfaces](inventory/interfaces.md) — `IModelRenderable`, `IAppEffectHandler`
- [Tests](inventory/tests.md) — Test-Ausgangszustand und bestehende Player-Testabdeckung
