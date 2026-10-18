# Plan-Review

## Ergebnis

**Status:** Offene Aufgaben vorhanden

## Umgesetzte Planelemente

- [x] Feld `EndWindowStreak` (`int`, Default 0) in `PlayerState` — vorhanden (`VideoPlayer.Mvu/PlayerState.cs`, Z. 128–135)
- [x] Feld `MediaId` (`long`) in `PlaybackProgressMessage` — vorhanden (`VideoPlayer.Mvu/AppMessage.cs`, Z. 1002–1014)
- [x] Konstante `EndWindowRequiredTicks = 2` (`private const int`) in `AppUpdate` — vorhanden (`VideoPlayer.Mvu/AppUpdate.cs`, Z. 90–98)
- [x] Methode `IsProgressTickPlausible(PlayerState, PlaybackProgressMessage)` → `bool` in `AppUpdate` — vorhanden, prüft positive Dauer, nicht-negative Position, realen Fortschritt und nicht geschrumpfte Dauer (`AppUpdate.cs`, Z. 4037–4053)
- [x] `UpdatePlaybackProgress`: Identitäts-Guard `message.MediaId != player.MediaId` → `AppTransition.Unchanged` — vorhanden (`AppUpdate.cs`, Z. 3819–3827)
- [x] `UpdatePlaybackProgress`: Streak-Auswertung — plausibler Fenstertakt (`remaining <= max(WatchedEndWindowSeconds, AutoResumeRemainingSeconds)`) → `EndWindowStreak + 1`; alle übrigen Takte inkl. Pausentakte, `IsResolving` und `Duration <= 0` → 0 — vorhanden (`AppUpdate.cs`, Z. 3849–3876, Else-Zweig Z. 3924–3927)
- [x] `UpdatePlaybackProgress`: `IsWatched`/`DeleteWatchedDownloadEffect` an `EndWindowStreak >= EndWindowRequiredTicks` gekoppelt — vorhanden (`AppUpdate.cs`, Z. 3882–3896)
- [x] `UpdatePlaybackProgress`: Playlist-Advance um Streak-Bedingung ergänzt; Serien-Advance um Streak-Bedingung und `!updated.IsResolving` ergänzt — vorhanden (`AppUpdate.cs`, Z. 3898–3923)
- [x] `AdvanceToNextEpisode`: `EndWindowStreak = 0` beim Medienwechsel — vorhanden (`AppUpdate.cs`, Z. 3782)
- [x] `UpdatePlaylistNavigationCompleted`: `EndWindowStreak = 0` beim Eintragswechsel — vorhanden (`AppUpdate.cs`, Z. 2604)
- [x] Feld `appliedMediaId` (`long`) in `PlayerPage` — vorhanden (`VideoPlayer.Maui/Pages/PlayerPage.xaml.cs`, Z. 38–42)
- [x] `PlayerPage.UpdateUi`: `appliedMediaId` beim Quellen-Setzen (`player.MediaId`) bzw. Leeren (`0`) gepflegt — vorhanden (`PlayerPage.xaml.cs`, Z. 256–260)
- [x] `PlayerPage.ReportPosition`: `PlaybackProgressMessage` wird mit `appliedMediaId` dispatcht — vorhanden (`PlayerPage.xaml.cs`, Z. 197–203)
- [x] `PlayerPage.ReportPosition`: `LogDebug`-Diagnoselog im Fenster (Position, Dauer, IsPlaying/IsBuffering, `appliedMediaId`, `EndWindowStreak` aus `mvuHost.Model`, Fenster = `max(WatchedEndWindowSeconds, AutoResumeRemainingSeconds)`, keine Quell-URLs) — vorhanden (`PlayerPage.xaml.cs`, Z. 177–196)
- [x] `PlayingState`-Hilfsmethode (`AppUpdateTests_Player`) um `endWindowStreak`-Parameter erweitert (`mediaId` bestand bereits) — vorhanden (`AppUpdateTests_Player.cs`, Z. 66–82)
- [x] `PlayingInPlaylist`-Hilfsmethode: mediaId-konsistente Takte — die Tests melden `MediaId: 10` passend zur Fixture-MediaId — vorhanden (`AppUpdateTests_PlaylistPlayback.cs`)
- [x] Alle `PlaybackProgressMessage`-Aufrufstellen auf die 5-Argument-Signatur umgestellt — repo-weit geprüft, keine 4-Argument-Aufrufe mehr
- [x] Bestehende Tests auf Zwei-Takt-Sequenzen/Streak umgestellt: `PlaybackProgress_MarksWatchedWithinWindow`, `PlaybackProgress_AutoResumeAdvancesWhenEnabled`, `PlaybackProgress_WatchedWindow_MarksEpisodeWatchedInDetails` (`AppUpdateTests_Player`); `PlaybackProgress_WatchedEpisode_EmitsAutoDelete`, `PlaybackProgress_WatchedMovie_NoAutoDelete`, `PlaybackProgress_AutoDeleteDisabled_NoEffect` (`AppUpdateTests_Downloads`); `PlaybackProgress_NearEnd_InPlaylist_AdvancesPlaylist`, `PlaybackProgress_NearEnd_WhileResolving_NoSecondAdvance`, `PlaybackProgress_InPlaylist_ReportsPlaylistId` (`AppUpdateTests_PlaylistPlayback`); `PlayerPage_DispatchesPlaybackMessages` (`PlayerContractTests`, Assert auf `appliedMediaId`)
- [x] Neuer Test `PlaybackProgress_SingleWindowTick_DoesNotAdvance` — vorhanden (`AppUpdateTests_Player.cs`)
- [x] Neuer Test `PlaybackProgress_SecondWindowTick_AdvancesAndMarksWatched` — vorhanden (assertiert u. a. Streak-Reset nach dem Wechsel)
- [x] Neuer Test `PlaybackProgress_FrozenPosition_ResetsStreak` — vorhanden
- [x] Neuer Test `PlaybackProgress_ShrunkDuration_ResetsStreak` — vorhanden
- [x] Neuer Test `PlaybackProgress_PositionJump_SingleTick_NoAdvance` — vorhanden
- [x] Neuer Test `PlaybackProgress_PausedTick_ResetsStreak` — vorhanden
- [x] Neuer Test `PlaybackProgress_WrongMediaId_IsDiscarded` — vorhanden
- [x] Neuer Test `PlaybackProgress_WhileResolving_NoWindowEffects` — vorhanden
- [x] Neuer Test `PlaybackProgress_SeriesAdvance_RequiresNotResolving` — vorhanden
- [x] Neuer Test `PlaybackProgress_TwoWindowTicks_EmitsAutoDelete` — vorhanden (`AppUpdateTests_Downloads.cs`)
- [x] Neuer Test `PlaybackProgress_NearEnd_TwoTicks_AdvancesPlaylist` — vorhanden (`AppUpdateTests_PlaylistPlayback.cs`)
- [x] Neuer Test `PlaybackProgress_TwoTicks_MarksEpisodeWatchedInDetails` — vorhanden (`AppUpdateTests_Player.cs`)
- [x] E2E-Timing-Prüfung: `PlaybackEnd_AdvancesToNextPlaylistEntry`, `TapContinueWatchingEpisode_OpensDetailsAndStartsPlaybackAndAdvancesToNextEpisode` und `ContinueWatchingPlaylistEntry_AdvancesByPlaylistOrder_NotSeries` warten jeweils mit 60-s-Timeout auf den Folgezustand — die verlängerte Wechselzeit (~10 s statt ~5 s) ist abgedeckt, keine Anpassung erforderlich

## Offene Aufgaben

- [ ] `Testlauf dotnet test VideoPlayer.Maui.Tests ausführen und Ergebnis dokumentieren` — fehlt vollständig: kein `test-results.md` im Feature-Verzeichnis, kein dokumentierter Testlauf

## Hinweise

- Der fehlende Testlauf entspricht Plan-Schritt 7 und ist im Lifecycle als eigener Schritt 8b („Tests ausführen") vorgesehen — er kann dort nachgeholt werden, ohne dass eine Nachimplementierung nötig ist.
- Der Reset in `UpdatePlaylistNavigationCompleted` (Task 10) hat keinen direkten Test: kein `NavigationCompleted_*`-Test startet mit `EndWindowStreak > 0`. Ein gezielter Test (Streak > 0 → Eintragswechsel → Streak 0) würde die Absicherung vervollständigen, ist aber nicht Teil des Plans.
- `PlaybackProgress_NearEnd_WhileResolving_NoSecondAdvance` assertiert zusätzlich `EndWindowStreak == 0` im Resolving-Zustand — deckt die IsResolving-Sperre im Playlist-Pfad mit ab.
