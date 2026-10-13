# Tasks: Plausibilitätsprüfung des Wiedergabe-Ende-Ereignisses

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Logik | Konstante `PlaybackEndedToleranceSeconds` (300 s) mit XML-Doc in `AppUpdate` anlegen | Offen | — |
| 2 | Logik | Hilfsmethode `IsPlaybackEndPlausible(PlayerState)` in `AppUpdate` anlegen | Offen | — |
| 3 | Logik | `UpdatePlaybackEnded` um den Plausibilitäts-Guard erweitern (verwerfen statt Abschluss) | Offen | — |
| 4 | Tests | `PlaybackEnded_WatchedEpisode_EmitsAutoDelete` auf plausiblen Endstand setzen | Offen | — |
| 5 | Tests | `PlaybackEnded_WithoutPlaylist_KeepsSeriesAdvance` mit `Duration`/`Position` am Ende versehen | Offen | — |
| 6 | Tests | `PlaybackEnded_Movie_DoesNotTouchEpisodeList` Position ans Ende setzen | Offen | — |
| 7 | Tests | `PlaybackEnded_UnknownEpisode_LeavesDetailsUnchanged` Position ans Ende setzen | Offen | — |
| 8 | Tests | `PlaybackEnded_MidPlayback_IsIgnored` (neu) | Offen | — |
| 9 | Tests | `PlaybackEnded_UnknownDuration_IsIgnored` (neu) | Offen | — |
| 10 | Tests | `PlaybackEnded_WithinTolerance_Completes` (neu) | Offen | — |
| 11 | Tests | `PlaybackEnded_JustOutsideTolerance_IsIgnored` (neu) | Offen | — |
| 12 | Tests | `PlaybackEnded_InPlaylist_MidPlayback_IsIgnored` (neu) | Offen | — |
| 13 | Tests | `PlaybackEnded_AfterAdvance_IgnoresStaleEvent` (neu) | Offen | — |
