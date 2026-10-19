# Tests

## Testklassen (`VideoPlayer.Maui.Tests`)

### `AppSettingsTests` (`AppSettingsTests.cs`)
- `Defaults_AreExpected` — prüft alle Standardwerte inkl. `AutoResumeRemainingSeconds == 300` (Z. 17)
- `Normalize_ClampsPreloadEpisodeCount` — Preload-Clamp (Theory)
- `Normalize_ClampsAutoResumeRemainingSeconds` — AutoResume-Clamp (Z. 43–53; Theory 0→30, 4000→3600, 120→120)
- `Normalize_ClampsWatchedEndWindowSeconds` — Watched-Fenster-Clamp (bleibt)
- `Normalize_ClampsDownloadQuotaGb` — Quota-Clamp (bleibt)
- `EndWindow_IsMaxOfBothWindows` — `EndWindow = max(beide Fenster)` (Z. 80–101; Theory mit je zwei Fensterwerten)
- `Normalize_LeavesBooleansUntouched` — Bool-Werte unverändert (bleibt)

### `AppSettingsServiceTests` (`AppSettingsServiceTests.cs`)
- `Load_WhenStoreEmpty_ReturnsDefaults` — Defaults bei leerem Store
- `Save_ThenLoad_RoundTrips` — Roundtrip aller Einstellungen; Fixture enthält `AutoResumeRemainingSeconds = 600` (Z. 37)
- `Save_LoggingEnabled_PersistsAcrossLoad`, `Load_WithoutLoggingKey_KeepsLoggingDisabled`, `Load_WithoutHomeHeroKey_KeepsHeroEnabled`, `Save_HomeHeroDisabled_PersistsAcrossLoad` — einzelne Schlüssel-Persistenz (bleiben)
- `Load_ClampsOutOfRangeStoredValues` — Clamping beim Laden (belegt u. a. `WatchedEndWindowSecondsKey`, bleibt)
- `Save_NormalizesBeforeWriting`, `Profiles_DoNotInterfere`, `UsesProfilePrefixedKeys` — bleiben

### `SettingsStorageContractTests` (`SettingsStorageContractTests.cs`)
- `SettingsPage_ContainsAllSettingControls` — pinnt `ExpectedAutomationIds` inkl. `"AutoResumeRemainingEntry"` (Z. 27) und die Zählung 6 Switches / 4 numerische Entries (Z. 57–61)
- `SettingsPage_WiresMvuHostAndSaveMessage` — `MvuHost`/`SaveSettingsRequestedMessage`/`ModelChanged` im Code-Behind (bleibt)
- `MauiProgram_RegistersStorageServices`, `EffectHandler_HandlesAllStorageEffects`, `ServerAddressProvider_UsesKnownServersService`, `NoDirectPlatformStorageAccess`, `IosBackupExclusion_IsImplemented`, `WindowsRoaming_IsUsedForBackupRoot`, `StorageProject_IsReferencedEverywhere` — unabhängig, bleiben

### `AppUpdateTests_Player` (`AppUpdateTests_Player.cs`)
Fortschrittstakt / „angesehen":
- `PlaybackProgress_ReportsChangedPosition`, `PlaybackProgress_WithoutPlaylist_ReportsNullPlaylistId`, `PlaybackProgress_UnchangedPosition_NoReport` — Basistakt-Verhalten (bleiben)
- `PlaybackProgress_MarksWatchedWithinWindow` — Watched-Markierung ab 2. Fenstertakt (bleibt)
- `PlaybackProgress_WatchedWindow_MarksEpisodeWatchedInDetails` — Watched-Markierung in geladenen Details (bleibt)

Auto-Fortsetzung über den **Takt** (positionsgetrieben — von der Änderung betroffen):
- `PlaybackProgress_AutoResumeAdvancesWhenEnabled` (Z. 334–363) — 2. Fenstertakt → MediaId 32 + `ResolvePlaybackSourceEffect`; Fixture `AutoResumeRemainingSeconds = 300` (Z. 340)
- `PlaybackProgress_AutoResumeDisabled_DoesNotAdvance` (Z. 367–383) — Schalter aus → kein Wechsel im Takt
- `PlaybackProgress_AboveResumeWindow_DoesNotAdvance` (Z. 387–405) — Takt oberhalb des Fensters → Streak-Reset, kein Wechsel; Fixture `AutoResumeRemainingSeconds = 300` (Z. 393)
- `PlaybackProgress_PausedWithinWindow_DoesNotAdvance` (Z. 409–425) — Pausentakt im Fenster → kein Wechsel; Fixture Z. 415
- `PlaybackProgress_NoNextEpisode_DoesNotAdvance` (Z. 429–441) — ohne Folge-Episode kein Wechsel

Endfenster-Plausibilisierung (Streak):
- `PlaybackProgress_SingleWindowTick_DoesNotAdvance` (Z. 447–466) — 1 Takt: Streak 1, kein Watched, kein Wechsel
- `PlaybackProgress_SecondWindowTick_AdvancesAndMarksWatched` (Z. 470–502) — 2. Takt → Wechsel (MediaId 32, `ResolvePlaybackSourceEffect`, Streak 0) + Details-Markierung der beendeten Episode
- `PlaybackProgress_FrozenPosition_ResetsStreak` (Z. 506–537), `PlaybackProgress_ShrunkDuration_ResetsStreak` (Z. 541–575) — Streak-Reset bei implausiblen Takten (bleiben fachlich gültig)
- `PlaybackProgress_PositionJump_SingleTick_NoAdvance` (Z. 579–609) — Sprung ins Fenster: 1. Takt Streak 1, 2. Takt **Wechsel** (assertiert MediaId 32)
- `PlaybackProgress_PausedTick_ResetsStreak` (Z. 613–644) — Pausentakt → Streak 0
- `PlaybackProgress_WrongMediaId_IsDiscarded` (Z. 648–678) — Identitäts-Guard (bleibt)
- `PlaybackProgress_WhileResolving_NoWindowEffects` (Z. 682–704) — IsResolving-Sperre (bleibt)

Ende-Pfad (`PlaybackEndedMessage` — bleiben, werden zum alleinigen Wechsel-Nachweis):
- `PlaybackEnded_MarksWatchedAndAdvances` (Z. 710–730) — Ende → MediaId 32 + Abschlussmeldung
- `PlaybackEnded_WithoutNext_MarksWatchedOnly` (Z. 734–747)
- `PlaybackEnded_MidPlayback_IsIgnored` (Z. 753–769), `PlaybackEnded_UnknownDuration_IsIgnored` (Z. 773–785), `PlaybackEnded_WithinTolerance_Completes` (Z. 789–800), `PlaybackEnded_JustOutsideTolerance_IsIgnored` (Z. 804–815), `PlaybackEnded_AfterAdvance_IgnoresStaleEvent` (Z. 824–843) — Ende-Plausibilität
- `PlaybackEnded_MarksEpisodeWatchedInDetails` (Z. 849–867), `PlaybackEnded_AutoResume_StillMarksFinishedEpisode` (Z. 871–891), `PlaybackEnded_AlreadyWatchedEpisode_KeepsServerTimestamp` (Z. 917–946), `PlaybackEnded_Movie_DoesNotTouchEpisodeList` (Z. 950–964), `PlaybackEnded_UnknownEpisode_LeavesDetailsUnchanged` (Z. 968–979)

Sonstige: `PlaybackFailed_ShowsErrorAndFallsBack`, `PlayerClosed_*`, `PictureInPicture_*`, `Logout_ResetsPlayer`, `PlayRequested_*`, `PlaybackSourceResolved_*` — unabhängig, bleiben.

### `AppUpdateTests_PlaylistPlayback` (`AppUpdateTests_PlaylistPlayback.cs`)
- `PlaybackEnded_InPlaylist_AdvancesPlaylist` (Z. 472–485), `PlaybackEnded_InPlaylist_AutoResumeOff_NoAdvance` (Z. 489–500), `PlaybackEnded_InPlaylist_MidPlayback_IsIgnored` (Z. 508–529), `PlaybackEnded_WithoutPlaylist_KeepsSeriesAdvance` (Z. 629–654), `PlaybackEnded_InPlaylist_ReportsPlaylistId` (Z. 689–700) — Ende-Pfad im Playlist-Kontext (bleiben, Wechsel-Nachweis)
- `PlaybackProgress_NearEnd_TwoTicks_AdvancesPlaylist` (Z. 533–563) — 2. Fenstertakt → `AdvancePlaylistEntryEffect` (**betroffen**)
- `PlaybackProgress_NearEnd_WhileResolving_NoSecondAdvance` (Z. 567–582) — IsResolving-Sperre, Streak 0, kein Advance (bleibt fachlich gültig, Doc-Kommentar betroffen)
- `PlaybackProgress_InPlaylist_ReportsPlaylistId` (Z. 660–681) — Takt trägt PlaylistId; Fixture `AutoResumeEnabled = false` (bleibt)
- Navigation/Fehler-Rückfall: `PlaylistNavigationCompleted_*`, `NavigationCompleted_*`, `PlaybackFailed_InPlaylist_*`, `SourceResolveFailed_InPlaylist_AdvancesPlaylist`, `PlayerClosed_InPlaylist_ReportsPlaylistId` — bleiben

### `AppUpdateTests_Downloads` (`AppUpdateTests_Downloads.cs`)
- `PlaybackProgress_TwoWindowTicks_EmitsAutoDelete` (Z. 505–535) — 2 Fenstertakte → `IsWatched` + `DeleteWatchedDownloadEffect` (bleibt fachlich; kein Advance-Assert)
- `PlaybackProgress_WatchedMovie_NoAutoDelete` (Z. 539–562), `PlaybackProgress_AutoDeleteDisabled_NoEffect` (Z. 566–589) — Watched-Markierung ohne Löschung (bleiben)
- `PlaybackEnded_WatchedEpisode_EmitsAutoDelete` (Z. 593–614) — Ende-Pfad mit Löschung (bleibt)

### `PlayerContractTests` (`PlayerContractTests.cs`)
- `PlayerPage_DispatchesPlaybackMessages` — pinnt u. a. `appliedMediaId`, `PlaybackEndedMessage`, `PlaybackProgressMessage` (Z. 96–115; bleibt)
- `PlayerPage_AppliedMediaId_AssignedAfterSourceSet` — Zuweisung nach Quellenwechsel (Z. 468–479; bleibt)
- `PlayerPage_IgnoresModelChangesAfterDisappearing` — `subscribed`-Guard inkl. `OnMediaEnded` (Z. 488–504; bleibt)
- `PlayerPage_NativeAccessesAreFailureIsolated` — assertet `try`/`catch` im `ReportPosition`-Rumpf (Z. 384–386); **kein** Assert auf `Settings.EndWindow` → voraussichtlich unbetroffen
- Übrige Contract-Tests (Route, MediaElement, Overlay, PiP, Marshaling, Resume-Seek, EffectHandler) — unabhängig, bleiben

## Hilfsmethoden

### `AppUpdateTests_Player`
- `Connected()` — verbundenes `AppModel`
- `ShowDetails()` / `WithDetails(details)` — Serien-Details mit 2 Staffeln à 2 Episoden (31→32→33→34)
- `PlayingState(mediaId, upcoming, duration, position, endWindowStreak)` — PlayerState-Fixture (laufende Wiedergabe, `Source` belegt)
- `WithPlayer(player)` — `Connected()` mit Player-Stand

### `AppUpdateTests_PlaylistPlayback`
- `PlayingInPlaylist(entryId, mediaId, isResolving, nextExhausted)` — Playlist-Fixture (PlaylistId 7, EntryId 5, MediaId 10, `Duration` 100 s)

### `AppUpdateTests_Downloads`
- `ConnectedModel()` — verbundenes `AppModel` für Download-Übergänge

### Stubs
- `InMemorySettingsStore` (`Stubs/InMemorySettingsStore.cs`) — `ISettingsStore`-Stub für `AppSettingsServiceTests`
- `RepositoryRoot` (`Stubs/`) — Repo-Wurzel für Contract-Tests (Quelltext-Prüfung)

## E2E-Tests (`VideoPlayer.E2E.Tests`)

### `PlaylistPlaybackTests` (`Tests/PlaylistPlaybackTests.cs`)
- `PlayButton_StartsPlaybackAtFirstPlayableEntry` (Z. 73–114) — wartet bis das kurze Testvideo per Auto-Fortsetzung zum letzten Eintrag (502) durchläuft; Kommentar beschreibt Fenster-Vorlauf
- `SelectEntry_StartsPlaybackAtThatEntry` (Z. 122–151) — startet direkt am gewählten Eintrag (unbetroffen)
- `NextAndPreviousButtons_NavigatePlaylist` (Z. 162–206) — stabiler Ausgangsstand „letzter Eintrag" via Auto-Fortsetzung; Kommentar „Fortschritts-Takt (5 s) lässt den Wechsel beobachtbar, bevor die Auto-Fortsetzung erneut zugreift"
- `PlaybackEnd_AdvancesToNextPlaylistEntry` (Z. 213–254) — Kommentar „kurzes Testvideo liegt sofort im Fortsetzungsfenster"; Wechsel-Nachweis über `play/advance`-Request + Modell
- `StreamFailure_AdvancesToNextPlaylistEntry` (Z. 262–302) — Fehler-Rückfall (unbetroffen)
- `ContinueWatchingPlaylistEntry_AdvancesByPlaylistOrder_NotSeries` (Z. 312–372) — Playlist-Weiterschauen; Wechsel via `play/advance` (Kommentar „Auto-Fortsetzung")

### `ContinueWatchingPlaybackTests` (`Tests/ContinueWatchingPlaybackTests.cs`)
- `TapContinueWatchingMovie_OpensDetailsAndStartsPlaybackAtResume` (Z. 44–86) — Wiedergabestart + Schließen (unbetroffen)
- `TapContinueWatchingEpisode_OpensDetailsAndStartsPlaybackAndAdvancesToNextEpisode` (Z. 96–155) — Kommentar „kurzes Testvideo liegt im Fortsetzungsfenster"; Wechsel zur Folge-Episode über Fortsetzungsliste
- `TapContinueWatchingShowEntry_OpensDetails_NotPlayback` (Z. 163–186) — unbetroffen

### Infrastruktur
- `E2ETestBase` — Basisklasse (`StartAppAsync`, `LoginOnHomeAsync`, `ConnectToPairingRequiredAsync`, `PairAsync`, `LoginAsync`, `ContinueWatchingIndexAsync`)
- `AppFixture` — geteilte Build-Fixture
- `Support/AppInstance` (`Ui`, `DevApi`, `Process`), `Support/DevApiClient` (`WaitForModelAsync`, `InvokeAsync`, `GetModelAsync`), `Support/UiDriver`, `Support/MockVideoServer` (`PlaybackVideoPath`, `WasPathRequested`, `RequestedPaths`, `StreamUnavailable`, Playlist-/CW-Properties)
- Testvideo: `VideoPlayer.E2E.Tests/Assets/testvideo.mp4` (~82 KB, wenige Sekunden — liegt vollständig innerhalb jedes Fensters; wird als `Content` ins Ausgabeverzeichnis kopiert)
