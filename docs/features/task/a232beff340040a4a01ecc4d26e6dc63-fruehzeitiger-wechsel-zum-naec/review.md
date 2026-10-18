# Plan-Review

## Ergebnis

**Status:** Vollständig umgesetzt

## Umgesetzte Planelemente

- [x] Feld `EndWindowStreak` (`int`, Default 0) in `PlayerState` — vorhanden (`VideoPlayer.Mvu/PlayerState.cs`, Z. 128–135)
- [x] Feld `MediaId` (`long`) in `PlaybackProgressMessage` — vorhanden (`VideoPlayer.Mvu/AppMessage.cs`, Z. 1002–1014)
- [x] Konstante `EndWindowRequiredTicks = 2` (`private const int`) in `AppUpdate` — vorhanden (`VideoPlayer.Mvu/AppUpdate.cs`, Z. 90–98)
- [x] Methode `IsProgressTickPlausible(PlayerState, PlaybackProgressMessage)` → `bool` (`private static`) in `AppUpdate` — vorhanden; prüft `Duration > 0`, `Position >= 0`, realen Fortschritt (`message.Position > player.Position`) und nicht geschrumpfte Dauer (`player.Duration <= 0 || message.Duration >= player.Duration`) (`AppUpdate.cs`, Z. 4077–4085)
- [x] `UpdatePlaybackProgress`: Identitäts-Guard `message.MediaId != player.MediaId` → `AppTransition.Unchanged` — vorhanden (`AppUpdate.cs`, Z. 3819–3827)
- [x] `UpdatePlaybackProgress`/`EvaluateEndWindow`: Streak-Auswertung — plausibler Fenstertakt mit `remaining <= max(WatchedEndWindowSeconds, AutoResumeRemainingSeconds)` → `EndWindowStreak + 1`; alle übrigen Takte (Pausentakt, eingefrorene Position, geschrumpfte Dauer, Rückwärtssprung, Fenster-Außentakt), `IsResolving` und `Duration <= 0` → 0 — vorhanden (`AppUpdate.cs`, Z. 3882–3889, Else-Zweig Z. 3952–3955)
- [x] `UpdatePlaybackProgress`/`EvaluateEndWindow`: `IsWatched`/`DeleteWatchedDownloadEffect` an `EndWindowStreak >= EndWindowRequiredTicks` gekoppelt — vorhanden (`AppUpdate.cs`, Z. 3891–3917)
- [x] `UpdatePlaybackProgress`/`EvaluateEndWindow`: Playlist-Advance um Streak-Bedingung ergänzt; Serien-Advance um Streak-Bedingung und `!updated.IsResolving` ergänzt — vorhanden (`AppUpdate.cs`, Z. 3922–3950)
- [x] `AdvanceToNextEpisode`: `EndWindowStreak = 0` beim Medienwechsel — vorhanden (`AppUpdate.cs`, Z. 3782)
- [x] `UpdatePlaylistNavigationCompleted`: `EndWindowStreak = 0` beim Eintragswechsel — vorhanden (`AppUpdate.cs`, Z. 2604)
- [x] Feld `appliedMediaId` (`long`) in `PlayerPage` — vorhanden (`VideoPlayer.Maui/Pages/PlayerPage.xaml.cs`, Z. 38–42)
- [x] `PlayerPage.UpdateUi`: `appliedMediaId` beim Quellen-Setzen (nach erfolgreichem `VideoElement.Source = …` auf `player.MediaId`) bzw. Leeren (`0`) gepflegt — vorhanden (`PlayerPage.xaml.cs`, Z. 260–263, Z. 285–292)
- [x] `PlayerPage.ReportPosition`: `PlaybackProgressMessage` wird mit `appliedMediaId` dispatcht — vorhanden (`PlayerPage.xaml.cs`, Z. 198–203)
- [x] `PlayerPage.ReportPosition`: `LogDebug`-Diagnoselog im Fenster (Position, Dauer, IsPlaying/IsBuffering, `appliedMediaId`, `EndWindowStreak` aus `mvuHost.Model`, Fenster = `max(WatchedEndWindowSeconds, AutoResumeRemainingSeconds)` via `AppSettings.EndWindow`, keine Quell-URLs) — vorhanden (`PlayerPage.xaml.cs`, Z. 177–196)
- [x] `PlayingState`-Hilfsmethode (`AppUpdateTests_Player`) um `endWindowStreak`-/`mediaId`-Parameter erweitert — vorhanden (`AppUpdateTests_Player.cs`, Z. 68–88)
- [x] `PlayingInPlaylist`-Hilfsmethode (`AppUpdateTests_PlaylistPlayback`) mit `mediaId`-konsistenter Fixture (Default 10, Takte melden `MediaId: 10`) — vorhanden (`AppUpdateTests_PlaylistPlayback.cs`, Z. 51–72)
- [x] Alle `PlaybackProgressMessage`-Aufrufstellen (48) auf die 5-Argument-Signatur mit `mediaId` umgestellt — repo-weit geprüft, keine 4-Argument-Aufrufe mehr
- [x] Bestehende Tests auf Zwei-Takt-Sequenzen/Streak umgestellt: `PlaybackProgress_MarksWatchedWithinWindow`, `PlaybackProgress_AutoResumeAdvancesWhenEnabled`, `PlaybackProgress_WatchedWindow_MarksEpisodeWatchedInDetails` (`AppUpdateTests_Player`); `PlaybackProgress_WatchedEpisode_EmitsAutoDelete`, `PlaybackProgress_WatchedMovie_NoAutoDelete`, `PlaybackProgress_AutoDeleteDisabled_NoEffect` (`AppUpdateTests_Downloads`); `PlaybackProgress_NearEnd_InPlaylist_AdvancesPlaylist`, `PlaybackProgress_NearEnd_WhileResolving_NoSecondAdvance`, `PlaybackProgress_InPlaylist_ReportsPlaylistId` (`AppUpdateTests_PlaylistPlayback`); `PlayerPage_DispatchesPlaybackMessages` (`PlayerContractTests`, Assert auf `appliedMediaId`)
- [x] Neuer Test `PlaybackProgress_SingleWindowTick_DoesNotAdvance` — vorhanden (`AppUpdateTests_Player.cs`, Z. 447)
- [x] Neuer Test `PlaybackProgress_SecondWindowTick_AdvancesAndMarksWatched` — vorhanden (Z. 470; assertiert u. a. Streak-Reset 0 nach dem Wechsel)
- [x] Neuer Test `PlaybackProgress_FrozenPosition_ResetsStreak` — vorhanden (Z. 506)
- [x] Neuer Test `PlaybackProgress_ShrunkDuration_ResetsStreak` — vorhanden (Z. 541)
- [x] Neuer Test `PlaybackProgress_PositionJump_SingleTick_NoAdvance` — vorhanden (Z. 579)
- [x] Neuer Test `PlaybackProgress_PausedTick_ResetsStreak` — vorhanden (Z. 613)
- [x] Neuer Test `PlaybackProgress_WrongMediaId_IsDiscarded` — vorhanden (Z. 648)
- [x] Neuer Test `PlaybackProgress_WhileResolving_NoWindowEffects` — vorhanden (Z. 682)
- [x] Neuer Test `PlaybackProgress_SeriesAdvance_RequiresNotResolving` — vorhanden (Z. 708)
- [x] Neuer Test `PlaybackProgress_TwoWindowTicks_EmitsAutoDelete` — vorhanden (`AppUpdateTests_Downloads.cs`, Z. 495)
- [x] Neuer Test `PlaybackProgress_NearEnd_TwoTicks_AdvancesPlaylist` — vorhanden (`AppUpdateTests_PlaylistPlayback.cs`, Z. 563)
- [x] Neuer Test `PlaybackProgress_TwoTicks_MarksEpisodeWatchedInDetails` — vorhanden (`AppUpdateTests_Player.cs`, Z. 731)
- [x] E2E-Timing-Prüfung: `PlaybackEnd_AdvancesToNextPlaylistEntry`, `ContinueWatchingPlaylistEntry_AdvancesByPlaylistOrder_NotSeries` (`PlaylistPlaybackTests`) und `TapContinueWatchingEpisode_OpensDetailsAndStartsPlaybackAndAdvancesToNextEpisode` (`ContinueWatchingPlaybackTests`) warten mit 60-s-Timeouts — die verlängerte Wechselzeit (≥ 2 Takte, ~10 s) ist abgedeckt, keine Anpassung erforderlich
- [x] Testlauf dokumentiert: `test-results.md` — 1443 Tests gesamt, alle bestanden (1379 `VideoPlayer.Maui.Tests` + 64 `VideoPlayer.E2E.Tests`, 1 nicht feature-bezogener UI-Flake im Erstlauf, im Wiederholungslauf bestanden); Abdeckung 89,7 %

## Hinweise

- Die Endfenster-Auswertung wurde in die private Hilfsmethode `EvaluateEndWindow` ausgelagert (`AppUpdate.cs`, Z. 3875–3959) — Auflösung eines Code-Review-Befunds aus Iteration 1 (`review-code.1.md`, God-Methode); das im Plan beschriebene Verhalten ist unverändert in `UpdatePlaybackProgress` wirksam.
- Die Fenster-Ableitung `max(WatchedEndWindowSeconds, AutoResumeRemainingSeconds)` ist als berechnete Eigenschaft `AppSettings.EndWindow` gekapselt (`VideoPlayer.Storage/AppSettings.cs`, Z. 38–48) und wird von MVU-Kern und Diagnose-Log gemeinsam genutzt — Auflösung des Duplikats-Befunds aus `review-code.1.md`. Es handelt sich um keine neue Benutzereinstellung; abgesichert durch `AppSettingsTests.EndWindow_IsMaxOfBothWindows`.
- `appliedMediaId` wird in `PlayerPage.UpdateUi` erst nach erfolgreichem `VideoElement.Source = …` zugewiesen (nicht optimistisch davor) — Auflösung des Fehlerbehandlungs-Befunds aus `review-code.1.md`; die Zuweisungsreihenfolge ist durch den neuen Contract-Test `PlayerContractTests.PlayerPage_AppliedMediaId_AssignedAfterSourceSet` gepinnt.
- Der in Iteration 1 (`review.1.md`) einzig offene Punkt — fehlender Testlauf — ist mit `test-results.md` geschlossen.
