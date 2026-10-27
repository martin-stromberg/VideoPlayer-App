# Tasks: Plausibilitätsprüfung des Wiedergabe-Ende-Ereignisses

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Logik | Konstante `PlaybackEndedToleranceSeconds` (300 s) mit XML-Doc in `AppUpdate` anlegen | Erledigt | `AppUpdateTests_Player.PlaybackEnded_WithinTolerance_Completes` / `PlaybackEnded_JustOutsideTolerance_IsIgnored` (Grenzfälle) |
| 2 | Logik | Hilfsmethode `IsPlaybackEndPlausible(PlayerState)` in `AppUpdate` anlegen | Erledigt | `AppUpdateTests_Player.PlaybackEnded_MidPlayback_IsIgnored`, `PlaybackEnded_UnknownDuration_IsIgnored` |
| 3 | Logik | `UpdatePlaybackEnded` um den Plausibilitäts-Guard erweitern (verwerfen statt Abschluss) | Erledigt | `AppUpdateTests_Player.PlaybackEnded_MidPlayback_IsIgnored`, `AppUpdateTests_PlaylistPlayback.PlaybackEnded_InPlaylist_MidPlayback_IsIgnored` |
| 4 | Tests | `PlaybackEnded_WatchedEpisode_EmitsAutoDelete` auf plausiblen Endstand setzen | Erledigt | Test selbst (grün) |
| 5 | Tests | `PlaybackEnded_WithoutPlaylist_KeepsSeriesAdvance` mit `Duration`/`Position` am Ende versehen | Erledigt | Test selbst (grün) |
| 6 | Tests | `PlaybackEnded_Movie_DoesNotTouchEpisodeList` Position ans Ende setzen | Erledigt | Test selbst (grün) |
| 7 | Tests | `PlaybackEnded_UnknownEpisode_LeavesDetailsUnchanged` Position ans Ende setzen | Erledigt | Test selbst (grün) |
| 8 | Tests | `PlaybackEnded_MidPlayback_IsIgnored` (neu) | Erledigt | Test selbst (grün) |
| 9 | Tests | `PlaybackEnded_UnknownDuration_IsIgnored` (neu) | Erledigt | Test selbst (grün) |
| 10 | Tests | `PlaybackEnded_WithinTolerance_Completes` (neu) | Erledigt | Test selbst (grün) |
| 11 | Tests | `PlaybackEnded_JustOutsideTolerance_IsIgnored` (neu) | Erledigt | Test selbst (grün) |
| 12 | Tests | `PlaybackEnded_InPlaylist_MidPlayback_IsIgnored` (neu) | Erledigt | Test selbst (grün) |
| 13 | Tests | `PlaybackEnded_AfterAdvance_IgnoresStaleEvent` (neu) | Erledigt | Test selbst (grün) |
