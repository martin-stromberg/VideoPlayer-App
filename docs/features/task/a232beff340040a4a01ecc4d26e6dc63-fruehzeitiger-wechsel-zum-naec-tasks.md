# Tasks: Frühzeitiger Wechsel zum nächsten Video bei fehlschlagendem Fortsetzungs-Seek

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Logik | `PlayerPage.ApplyResumeSeekAsync`: `PlaybackFailedMessage`-Dispatch (inkl. `subscribed`-Guard) aus dem catch-Zweig entfernen, Warn-Log beibehalten | Erledigt | `PlayerContractTests.PlayerPage_ResumeSeekFailure_IsNonFatal` |
| 2 | Logik | `PlayerPage.ApplyResumeSeekAsync`: XML-Dokumentationskommentar auf nicht-fatale Semantik anpassen (Seek-Fehlschlag degradiert statt Wiedergabefehler zu melden) | Erledigt | Kein direkter Test (Dokumentationskommentar; indirekt über `PlayerPage_ResumeSeekFailure_IsNonFatal` abgesichert) |
| 3 | Tests | `PlayerContractTests.PlayerPage_NativeAccessesAreFailureIsolated`: Assert auf `PlaybackFailedMessage` im `ApplyResumeSeekAsync`-Rumpf invertieren/entfernen | Erledigt | `PlayerContractTests.PlayerPage_NativeAccessesAreFailureIsolated` |
| 4 | Tests | `PlayerContractTests`: Neuen Test `PlayerPage_ResumeSeekFailure_IsNonFatal` anlegen (kein `PlaybackFailedMessage` im Seek-Rumpf; `FailPlaybackStart`/`OnMediaFailed` dispatchen weiterhin) | Erledigt | `PlayerContractTests.PlayerPage_ResumeSeekFailure_IsNonFatal` |
| 5 | Tests | Regressionslauf `VideoPlayer.Maui.Tests` (`PlayerContractTests`, `AppUpdateTests_Player`, `AppUpdateTests_PlaylistPlayback`) | Erledigt | Lauf vom 18.10.2026: 86/86 Tests grün (`dotnet test`, Filter auf die drei Suiten) |
| 6 | E2E-Tests | Regressionslauf `VideoPlayer.E2E.Tests` (`PlaybackTests`, `PlaylistPlaybackTests`, `ContinueWatchingPlaybackTests`) — kein neues E2E-Szenario (Seek-Timeout nicht nachstellbar) | Erledigt | Lauf vom 18.10.2026: 11/11 Tests grün (direkter xUnit-v3-Runner-Aufruf; `dotnet test`-Wrapper hing im Fixture-Build) |
