# Tasks: Auto-Fortsetzung nur am tatsächlichen Medienende — konfigurierbarer Vorlauf entfernen

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Datenmodell | `AppSettings.AutoResumeRemainingSeconds` (Property inkl. Doc) entfernen | Offen | — |
| 2 | Datenmodell | `AppSettings.Normalize()`: Clamp-Zeile `AutoResumeRemainingSeconds` entfernen | Offen | — |
| 3 | Datenmodell | `AppSettings.EndWindow` (abgeleitete Max-Fenster-Property) entfernen | Offen | — |
| 4 | Datenmodell | `PlayerState.EndWindowStreak`: Doc-Kommentar anpassen (dient nur noch der „angesehen"-Schwelle) | Offen | — |
| 5 | Persistenz | `AppSettingsService.AutoResumeRemainingSecondsKey` entfernen | Offen | — |
| 6 | Persistenz | `AppSettingsService.Load()`: `AutoResumeRemainingSeconds`-Zeile entfernen (verwaister Schlüssel ohne Migration) | Offen | — |
| 7 | Persistenz | `AppSettingsService.Save()`: `AutoResumeRemainingSeconds`-Zeile entfernen | Offen | — |
| 8 | Logik | `AppUpdate.EvaluateEndWindow`: Playlist-Advance-Block entfernen (Bedingung `remaining <= AutoResumeRemainingSeconds` + `AdvancePlaylistEntry`-Aufruf) | Offen | — |
| 9 | Logik | `AppUpdate.EvaluateEndWindow`: Serien-Advance-Block entfernen (Bedingung `remaining <= AutoResumeRemainingSeconds` + `AdvanceToNextEpisode`-Aufruf) | Offen | — |
| 10 | Logik | `AppUpdate.EvaluateEndWindow`: Streak-Fensterquelle von `model.Settings.EndWindow` auf `WatchedEndWindowSeconds` umstellen | Offen | — |
| 11 | Logik | `AppUpdate`: Doc-Kommentare von `UpdatePlaybackProgress`, `EvaluateEndWindow`, `PlaybackEndedToleranceSeconds` und `EndWindowRequiredTicks` an den entfernten Vorlauf anpassen | Offen | — |
| 12 | App-Rand | `PlayerPage.ReportPosition`: `LogDebug`-Fensterbedingung von `model.Settings.EndWindow` auf `WatchedEndWindowSeconds` umstellen und Kommentar anpassen | Offen | — |
| 13 | UI | `SettingsPage.xaml`: `AutoResumeRemainingEntry`-Block entfernen (Entry samt Label und einer der beiden angrenzenden `BoxView`-Trennlinien; genau eine Trennlinie zwischen `AutoResumeSwitch` und `WatchedEndWindowEntry` belassen) | Offen | — |
| 14 | UI | `SettingsPage.xaml.cs`: `WireNumeric(AutoResumeRemainingEntry, …)` im Konstruktor entfernen | Offen | — |
| 15 | UI | `SettingsPage.xaml.cs`: `SetTextIfNotFocused(AutoResumeRemainingEntry, …)` in `PopulateFromModel` entfernen | Offen | — |
| 16 | UI | `SettingsPage.xaml.cs`: Handler `OnAutoResumeRemainingCompleted` entfernen | Offen | — |
| 17 | Tests | `AppSettingsTests.Defaults_AreExpected`: `AutoResumeRemainingSeconds`-Assert entfernen | Offen | — |
| 18 | Tests | `AppSettingsTests.Normalize_ClampsAutoResumeRemainingSeconds` entfernen | Offen | — |
| 19 | Tests | `AppSettingsTests.EndWindow_IsMaxOfBothWindows` entfernen | Offen | — |
| 20 | Tests | `AppSettingsServiceTests.Save_ThenLoad_RoundTrips`: Fixture-Zeile `AutoResumeRemainingSeconds = 600` entfernen | Offen | — |
| 21 | Tests | `SettingsStorageContractTests`: `"AutoResumeRemainingEntry"` aus `ExpectedAutomationIds` entfernen und Entry-Zählung 4 → 3 anpassen | Offen | — |
| 22 | Tests | `AppUpdateTests_Player`: `AutoResumeRemainingSeconds`-Fixture-Belegungen entfernen (u. a. Z. 340, 393, 415) | Offen | — |
| 23 | Tests | `AppUpdateTests_Player.PlaybackProgress_AutoResumeAdvancesWhenEnabled` zu Negativ-Test umwidmen (zwei Fenstertakte → kein Wechsel, MediaId 31, kein `ResolvePlaybackSourceEffect`) und umbenennen | Offen | — |
| 24 | Tests | `AppUpdateTests_Player.PlaybackProgress_AutoResumeDisabled_DoesNotAdvance` entfernen (Schalter-Wirkung nur noch im Ende-Pfad — Abdeckung durch neuen Test, siehe #34) | Offen | — |
| 25 | Tests | `AppUpdateTests_Player.PlaybackProgress_AboveResumeWindow_DoesNotAdvance` anpassen (Streak-Reset-Asserts bleiben, Fenster-Bezug auf Watched-Fenster, Kommentar) | Offen | — |
| 26 | Tests | `AppUpdateTests_Player.PlaybackProgress_PausedWithinWindow_DoesNotAdvance` anpassen bzw. mit `PlaybackProgress_PausedTick_ResetsStreak` zusammenführen | Offen | — |
| 27 | Tests | `AppUpdateTests_Player.PlaybackProgress_NoNextEpisode_DoesNotAdvance` entfernen oder umwidmen (Advance-Bedingung entfällt — Test wird trivial) | Offen | — |
| 28 | Tests | `AppUpdateTests_Player.PlaybackProgress_SecondWindowTick_AdvancesAndMarksWatched`: Advance-Asserts entfernen (kein Wechsel, MediaId bleibt 31), Watched-/Details-Asserts behalten, umbenennen | Offen | — |
| 29 | Tests | `AppUpdateTests_Player.PlaybackProgress_PositionJump_SingleTick_NoAdvance`: zweiten Abschnitt von „Wechsel" auf „angesehen markiert" umstellen | Offen | — |
| 30 | Tests | `AppUpdateTests_Player`: übrige Takt-Tests (`SingleWindowTick`, `FrozenPosition`, `ShrunkDuration`, `PausedTick`, `WrongMediaId`, `WhileResolving`, `MarksWatchedWithinWindow`, `WatchedWindow_MarksEpisodeWatchedInDetails`) — fachlich gültig lassen, Kommentare an neue Invariante anpassen | Offen | — |
| 31 | Tests | `AppUpdateTests_PlaylistPlayback.PlaybackProgress_NearEnd_TwoTicks_AdvancesPlaylist` zu Watched-/Negativ-Test umwidmen (`IsWatched`, kein `AdvancePlaylistEntryEffect`) und umbenennen | Offen | — |
| 32 | Tests | `AppUpdateTests_PlaylistPlayback.PlaybackProgress_NearEnd_WhileResolving_NoSecondAdvance` und `PlaybackProgress_InPlaylist_ReportsPlaylistId`: Kommentare/Fixture an neue Invariante anpassen | Offen | — |
| 33 | Tests | `AppUpdateTests_Downloads.PlaybackProgress_TwoWindowTicks_EmitsAutoDelete`: fachlich gültig lassen, Doc-Kommentar prüfen | Offen | — |
| 34 | Tests | Neuer Test `AppUpdateTests_Player.PlaybackEnded_AutoResumeDisabled_DoesNotAdvance` (Ende + `AutoResumeEnabled=false` + Folge-Episode → Watched + Abschlussmeldung, kein Wechsel) | Offen | — |
| 35 | Tests | `PlayerContractTests` gegen die geänderte `ReportPosition`-Fensterquelle verifizieren (kein `EndWindow`-Assert vorhanden — im Testlauf bestätigen) | Offen | — |
| 36 | E2E-Tests | `PlaylistPlaybackTests.PlaybackEnd_AdvancesToNextPlaylistEntry`: Kommentar auf `MediaEnded`-Pfad umstellen, Timeout verifizieren | Offen | — |
| 37 | E2E-Tests | `PlaylistPlaybackTests.ContinueWatchingPlaylistEntry_AdvancesByPlaylistOrder_NotSeries`: Kommentar anpassen, Timeout verifizieren | Offen | — |
| 38 | E2E-Tests | `PlaylistPlaybackTests.PlayButton_StartsPlaybackAtFirstPlayableEntry` und `NextAndPreviousButtons_NavigatePlaylist`: Kommentare (Auto-Fortsetzung als Durchlauf-/Ausgangsstand) anpassen, Timeouts verifizieren | Offen | — |
| 39 | E2E-Tests | `ContinueWatchingPlaybackTests.TapContinueWatchingEpisode_OpensDetailsAndStartsPlaybackAndAdvancesToNextEpisode`: Kommentar anpassen, Timeout verifizieren | Offen | — |
| 40 | E2E-Tests | `VideoPlayer.E2E.Tests` ausführen — prüfen, ob die 60-s-Wartezeiten das echte `MediaEnded` des Testvideos abdecken (Timeouts ggf. erhöhen) | Offen | — |
| 41 | Dokumentation | `docs/help/wiedergabe-pip.md`: Abschnitt „Automatische Fortsetzung" auf den reinen Ende-Pfad umstellen (positionsgetriebener Wechsel, eingestelltes Fenster und `AppSettings.EndWindow`-Verweis entfernen; Fensterbestätigung verbleibt bei „angesehen") | Offen | — |
| 42 | Dokumentation | `docs/help/einstellungen-datenhaltung.md`: Eintrag „Zeitfenster Auto-Fortsetzung" entfernen, „Auto-Fortsetzung"-Eintrag präzisieren | Offen | — |
| 43 | Dokumentation | `docs/help/index.md`: Einleitungssatz zu `wiedergabe-pip.md` anpassen („Auto-Fortsetzung erst bei bestätigtem Endfenster" → Ende-Pfad) | Offen | — |
| 44 | Tests | `dotnet test VideoPlayer.Maui.Tests` ausführen und Ergebnis dokumentieren | Offen | — |
