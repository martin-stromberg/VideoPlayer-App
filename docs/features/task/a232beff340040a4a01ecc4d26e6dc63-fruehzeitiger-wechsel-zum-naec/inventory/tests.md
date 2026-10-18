# Tests

## Testklassen

### `AppUpdateTests_Player` — `VideoPlayer.Maui.Tests/AppUpdateTests_Player.cs` (777 Zeilen)

Fortschritts-Takt (Z. 236–318):
- `PlaybackProgress_ReportsChangedPosition` — geänderte Position → `ReportPlaybackProgressEffect`.
- `PlaybackProgress_WithoutPlaylist_ReportsNullPlaylistId` — kontextlose Meldung.
- `PlaybackProgress_UnchangedPosition_NoReport` — Throttling über `LastReportedPositionSeconds`.
- `PlaybackProgress_MarksWatchedWithinWindow` — `IsWatched` bei `remaining <= 300` (einzelner Takt; Grenzfälle 300/301 s).

Auto-Fortsetzung (Z. 320–421) — **alle Tests lösen mit einem einzigen Takt aus** (bei Streak-Prüfung anzupassen):
- `PlaybackProgress_AutoResumeAdvancesWhenEnabled` — `remaining` 1 s im Fenster → `AdvanceToNextEpisode` (`MediaId` 32, `IsResolving`, `ResolvePlaybackSourceEffect`).
- `PlaybackProgress_AutoResumeDisabled_DoesNotAdvance` — Schalter aus.
- `PlaybackProgress_AboveResumeWindow_DoesNotAdvance` — `remaining` 1 s oberhalb.
- `PlaybackProgress_PausedWithinWindow_DoesNotAdvance` — `IsPlaying = false` sperrt.
- `PlaybackProgress_NoNextEpisode_DoesNotAdvance` — leere `UpcomingEpisodes`.

Ende / Fehler / Schließen (Z. 423–464):
- `PlaybackEnded_MarksWatchedAndAdvances` — Ende + Auto-Fortsetzung.
- `PlaybackEnded_WithoutNext_MarksWatchedOnly` — Ende ohne Folge-Episode.

Ende-Plausibilitätsprüfung (Issue 49, Z. 466–560):
- `PlaybackEnded_MidPlayback_IsIgnored` — verfrühtes Ereignis verworfen.
- `PlaybackEnded_UnknownDuration_IsIgnored` — `Duration <= 0` verworfen.
- `PlaybackEnded_WithinTolerance_Completes` / `PlaybackEnded_JustOutsideTolerance_IsIgnored` — Grenzfälle 300/301 s.
- `PlaybackEnded_AfterAdvance_IgnoresStaleEvent` — nachgezogenes Ende-Ereignis des Vorgängers verworfen.

Angesehen-Markierung in Details (Z. 562–692):
- `PlaybackEnded_MarksEpisodeWatchedInDetails`, `PlaybackEnded_AutoResume_StillMarksFinishedEpisode`, `PlaybackProgress_WatchedWindow_MarksEpisodeWatchedInDetails`, `PlaybackEnded_AlreadyWatchedEpisode_KeepsServerTimestamp`, `PlaybackEnded_Movie_DoesNotTouchEpisodeList`, `PlaybackEnded_UnknownEpisode_LeavesDetailsUnchanged`.

Sonstige:
- `PlaybackFailed_ShowsErrorAndFallsBack` — Fehler-Rückfall.
- `PlayerClosed_ReportsFinalAndResets`, `PlayerClosed_WithoutPosition_ResetsOnly`.

### `AppUpdateTests_PlaylistPlayback` — `VideoPlayer.Maui.Tests/AppUpdateTests_PlaylistPlayback.cs` (696 Zeilen)

Fixture `PlayingInPlaylist` (Z. 51–80): `MediaType "movie"`, `Duration` 100 s, `Position` 96 s — **liegt von Anfang an im 300-s-Fenster** (wirksam erst bei Takt).

Auto-Fortsetzung und Fehler-Rückfall (Z. 468–636):
- `PlaybackEnded_InPlaylist_AdvancesPlaylist` — Ende → `AdvancePlaylistEntryEffect`.
- `PlaybackEnded_InPlaylist_AutoResumeOff_NoAdvance` — Schalter aus.
- `PlaybackEnded_InPlaylist_MidPlayback_IsIgnored` — Ende-Plausibilität auch im Playlist-Kontext.
- `PlaybackProgress_NearEnd_InPlaylist_AdvancesPlaylist` — **ein einzelner Takt** (97/100 s) → `AdvancePlaylistEntryEffect`.
- `PlaybackProgress_NearEnd_WhileResolving_NoSecondAdvance` — `IsResolving` unterbindet Zweit-Advance.
- `PlaybackFailed_InPlaylist_AdvancesPlaylist`, `SourceResolveFailed_InPlaylist_AdvancesPlaylist`, `PlaybackFailed_InPlaylist_Exhausted_KeepsErrorState`, `PlaybackEnded_WithoutPlaylist_KeepsSeriesAdvance`.

Playlist-Bezug der Fortschrittsmeldung (Z. 638–695): `PlaybackProgress_InPlaylist_ReportsPlaylistId`, `PlaybackEnded_InPlaylist_ReportsPlaylistId`, `PlayerClosed_InPlaylist_ReportsPlaylistId`.

### `AppUpdateTests_Downloads` — `VideoPlayer.Maui.Tests/AppUpdateTests_Downloads.cs`

Auto-Delete (Z. 465–556):
- `PlaybackProgress_WatchedEpisode_EmitsAutoDelete` — Episode + Schalter → `DeleteWatchedDownloadEffect` (Position 88/90 min, einzelner Takt).
- `PlaybackProgress_WatchedMovie_NoAutoDelete` — Film → kein Effekt.
- `PlaybackProgress_AutoDeleteDisabled_NoEffect` — Schalter aus.
- `PlaybackEnded_WatchedEpisode_EmitsAutoDelete` — Endpfad.

### `PlayerContractTests` — `VideoPlayer.Maui.Tests/PlayerContractTests.cs` (517 Zeilen)

- `PlayerPage_DispatchesPlaybackMessages` (Z. 95–111) — pinnt `PlaybackProgressMessage`/`PlaybackEndedMessage`/`PlaybackFailedMessage`-Dispatch.
- `PlayerPage_NativeAccessesAreFailureIsolated` (Z. 356–397) — Fehlerisolation `UpdateUi`/`ReportPosition`/Timer/Seek-Gate.
- `PlayerPage_ResumeSeekFailure_IsNonFatal` (Z. 411–427) — Seek-Fehlschlag ohne `PlaybackFailedMessage` (Fix `e465907`).
- `PlayerPage_FailedSourceApply_KeepsStopPathReachable` (Z. 439–450) — `sourceApplyFailed`-Invalidierung.
- `PlayerPage_IgnoresModelChangesAfterDisappearing` (Z. 458 ff.) — `subscribed`-Guard verwaister Seiten.

### `AppSettingsTests` — `VideoPlayer.Maui.Tests/AppSettingsTests.cs` (99 Zeilen)

- `Defaults_AreExpected` — `AutoResumeEnabled = true`, `AutoResumeRemainingSeconds`/`WatchedEndWindowSeconds` = 300, `AutoDeleteWatchedEpisodes = false`.
- `Normalize_ClampsAutoResumeRemainingSeconds` / `Normalize_ClampsWatchedEndWindowSeconds` — Clamp 30–3600.

### `DownloadsContractTests` — `VideoPlayer.Maui.Tests/DownloadsContractTests.cs`

- Z. 129/136–138 — pinnt `DeleteWatchedDownloadEffect`-Behandlung bzw. `ItemRemoved` → `DownloadDeletedMessage`-Verdrahtung.

### `DownloadServiceTests` — `VideoPlayer.Maui.Tests/DownloadServiceTests.cs`

- `DeleteIfCompletedAsync`-Test (Z. 591) — löscht nur `Completed`-Einträge.

## E2E-Tests (`VideoPlayer.E2E.Tests`)

| Datei | Test | Bezug |
|-------|------|-------|
| `Tests/PlaylistPlaybackTests.cs` Z. 213 | `PlaybackEnd_AdvancesToNextPlaylistEntry` | Auto-Fortsetzung: kurzes Testvideo liegt „sofort im Fortsetzungsfenster" → `play/advance` zum Folgeeintrag. **Setzt den Fenstereintritt ab dem ersten Takt voraus.** |
| `Tests/PlaylistPlaybackTests.cs` Z. 262 | `StreamFailure_AdvancesToNextPlaylistEntry` | Fehler-Rückfall über `play/advance`. |
| `Tests/PlaylistPlaybackTests.cs` Z. 312 | `ContinueWatchingPlaylistEntry_AdvancesByPlaylistOrder_NotSeries` | Playlist-Kontext schlägt Serienreihenfolge. |
| `Tests/ContinueWatchingPlaybackTests.cs` Z. 96 | `TapContinueWatchingEpisode_OpensDetailsAndStartsPlaybackAndAdvancesToNextEpisode` | Serien-Auto-Fortsetzung über `UpcomingEpisodes`. |
| `Tests/PlaybackTests.cs` Z. 49 | `PlayWithFailingStream_ShowsErrorInsteadOfCrash` | Fehlerpfad, kein Fensterbezug. |

Kein E2E-Test simuliert inkonsistente Positions-/Dauer-Takte oder Stream-Stalls — das Mock-Server-Setup (`Support/MockVideoServer.cs`) streamt ein festes `Assets/testvideo.mp4`; ein Stall-/Ausreißer-Takt-Szenario ist E2E-seitig nicht vorhanden.

## Hilfsmethoden

### `AppUpdateTests_Player`
- `Connected()` (Z. 15–19) — verbundenes `AppModel`.
- `ShowDetails()` (Z. 23–54) — Seriendetails, 2 Staffeln à 2 Episoden (31→34).
- `WithDetails(...)` (Z. 56–66) — Model mit geladenen Details.
- `PlayingState(...)` (Z. 68–87) — `PlayerState` („tvshowepisode"/31, `IsPlaying`, `Duration` 45 min, parametrisierbar: `mediaId`, `upcoming`, `duration`, `position`).
- `WithPlayer(...)` (Z. 89–90).

### `AppUpdateTests_PlaylistPlayback`
- `StartInfo(...)` (Z. 20–38), `NavItem(...)` (Z. 40–49), `PlayingInPlaylist(...)` (Z. 51–80).
