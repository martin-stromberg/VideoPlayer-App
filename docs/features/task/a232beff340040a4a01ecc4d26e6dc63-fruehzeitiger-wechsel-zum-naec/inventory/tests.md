# Tests

## Testklassen

### `PlayerContractTests` — `VideoPlayer.Maui.Tests/PlayerContractTests.cs`

Vertragstests der `PlayerPage` per Quelltext-Prüfung (MAUI-Schicht ist im Testprojekt nicht instanziierbar). Hilfsmethode `MethodBody(code, signature)` extrahiert Methodenrümpfe für gezielte Asserts.

- `PlayerPage_RouteRegistered` — Route `player` in `AppShell`.
- `MauiApp_UsesMediaElement` — MediaElement-Paket/-Aktivierung, `PlaybackSourceService`, `IPictureInPictureService` in `MauiProgram`.
- `PlayerPage_UsesMediaElementWithControls` — XAML: Autoplay, Transportsteuerung, `MediaOpened`/`MediaEnded`/`MediaFailed` verdrahtet.
- `PlayerPage_DispatchesPlaybackMessages` — Seite dispatcht `PlayerPageOpenedMessage`, `PlaybackProgressMessage`, `PlaybackEndedMessage`, `PlaybackFailedMessage`, PiP-/Closed-Messages; enthält `SeekTo`, `PlaybackSourceKind.LocalFile`, `MediaSource.FromFile`.
- `PlayerPage_MarshalsNativeElementAccessOnMainThread` (Z. 291) — `BeginInvokeOnMainThread` in `OnMediaOpened`/`OnMediaStateChanged`; **genau ein `SeekTo(`-Aufruf im Code-Behind**, in `ApplyResumeSeekAsync`, nur über `TryApplyPendingResumeSeek` erreichbar; `Assert.DoesNotContain("ApplyResumeSeekAsync", OnMediaOpened-Rumpf)`.
- `PlayerPage_ResumeSeek_WaitsForReadyState` (Z. 323) — kein `SeekTo(` in `OnMediaOpened`; Gate prüft `MediaElementState.Playing`/`Paused`, leert `pendingResumePosition` einmalig; `ApplyResumeSeekAsync` enthält `CancellationTokenSource` + `ResumeSeekTimeout`.
- `PlayerPage_NativeAccessesAreFailureIsolated` (Z. 354) — **pinnt das aktuelle Fehlerverhalten:** `try`/`catch` und `PlaybackFailedMessage` im Rumpf von `ApplyResumeSeekAsync` (Z. 370–373); `FailPlaybackStart` dispatcht `PlaybackFailedMessage`; `UpdateUi`/`ReportPosition`/Timer/Gate try-catch-isoliert.
- `PlayerPage_FailedSourceApply_KeepsStopPathReachable` (Z. 405) — `FailPlaybackStart` setzt `sourceApplyFailed = true` statt `appliedLocation = null`; `UpdateUi` prüft `sourceApplyFailed`.
- `PlayerPage_UnsubscribesModelChangedOnDisappearing` / `PlayerPage_IgnoresModelChangesAfterDisappearing` — Abo-Lebenszyklus und `subscribed`-Guard in allen Handlern.
- `PlayerPage_OverlayChromeDoesNotCoverPlaybackSurface`, `PlayerPage_PlaylistNavigationBarClearsNativeControlsOnIos`, `PlayerPage_TapShowsTransportControlsOnWindows`, `PictureInPictureService_UsesCompactOverlayOnWindows`, `MediaDetailsPage_HasPlayButton`, `IosInfoPlist_DeclaresAudioBackgroundMode` — Layout-/Plattform-Verträge.
- `EffectHandler_HandlesPlayerEffects` — `ShellAppEffectHandler` verdrahtet `NavigateToPlayerEffect`, `ResolvePlaybackSourceEffect`, `ReportPlaybackProgressEffect`, `TogglePictureInPictureEffect`, `ProgressReportService`.

### `AppUpdateTests_Player` — `VideoPlayer.Maui.Tests/AppUpdateTests_Player.cs`

MVU-Übergänge des Serien-/Einzel-Wiedergabepfads.

- `PlayRequested_ValidMovie_SetsPlayerStateAndEffects` / `PlayRequested_Episode_BuildsUpcomingEpisodes` / u. a. — Start belegt `PlayerState` + `NavigateToPlayerEffect` + `ResolvePlaybackSourceEffect`; `UpcomingEpisodes` staffelübergreifend.
- `PlaybackSourceResolved_Success_SetsSource` (Z. 153) — Quelle übernommen, `IsResolving` beendet.
- `PlaybackSourceResolved_Error_FallsBackToNextEpisode` (Z. 182) — nicht auflösbare Quelle → Rückfall auf `UpcomingEpisodes[0]` mit neuem `ResolvePlaybackSourceEffect`.
- `PlaybackSourceResolved_ErrorWithoutNext_StaysOnError` (Z. 209) — ohne Folge-Episode bleibt der Fehlerzustand (`PlaybackUnavailableError`).
- `PlaybackSourceResolved_StaleResponse_Discarded` (Z. 226) — veraltete Antwort verworfen.
- `PlaybackFailed_ShowsErrorAndFallsBack` (Z. 696) — **`PlaybackFailedMessage` → `AdvanceToNextEpisode`:** MediaId der Folge-Episode, `Error = PlaybackUnavailableError`, `ResolvePlaybackSourceEffect` für die neue Id.
- `PlaybackEnded_*`-Tests (mehrere) — Plausibilitätsfenster, „angesehen"-Markierung, Auto-Fortsetzung, `MarkEpisodeWatchedInDetails`.
- `PlaybackProgress_*`-Tests (mehrere) — Fortschrittsmeldung mit Throttling, `IsWatched`-Schwelle, Auto-Fortsetzung (`AutoResumeEnabled`).
- `PlayerClosed_ReportsFinalAndResets` / `PlayerClosed_WithoutPosition_ResetsOnly` — Abschluss-Pfad.
- `PictureInPicture_TogglesFlag` / `PictureInPicture_InactivePlayer_Unchanged`, `Logout_ResetsPlayer`.

Hilfsmethoden: `Connected()` (verbundenes Basismodell), `ShowDetails()` (Serie mit 2 Staffeln à 2 Episoden 31–34), `WithDetails(...)`, `PlayingState(mediaId, upcoming, duration, position)` (aktiver Player mit `ServerStream`-Quelle), `WithPlayer(...)`.

### `AppUpdateTests_PlaylistPlayback` — `VideoPlayer.Maui.Tests/AppUpdateTests_PlaylistPlayback.cs`

MVU-Übergänge des Playlist-Wiedergabepfads (dasselbe Fehler-Rückfall-Muster über `play/advance`).

- `PlaybackFailed_InPlaylist_AdvancesPlaylist` (Z. 568) — **`PlaybackFailedMessage` → `AdvancePlaylistEntryEffect`** (`CurrentEntryId = 5`), `IsResolving`, `Error = PlaybackUnavailableError`.
- `PlaybackFailed_InPlaylist_Exhausted_KeepsErrorState` (Z. 598) — mit `PlaylistNextExhausted` kein Advance-Effekt, Fehlerzustand bleibt.
- `SourceResolveFailed_InPlaylist_AdvancesPlaylist` (Z. 582) — `PlaybackSourceResolvedMessage` mit `Source = null` → gleicher Rückfall.
- `PlaybackEnded_WithoutPlaylist_KeepsSeriesAdvance` (Z. 611) — Serienpfad bleibt parallel bestehen.
- `PlaybackProgress_NearEnd_*`, `PlaybackEnded_InPlaylist_ReportsPlaylistId`, `PlayerClosed_InPlaylist_ReportsPlaylistId` — Auto-Fortsetzung/Fortschritts-Playlist-Bezug.
- Start-/Navigations-Tests (`PlaylistPlaybackStarted_*`, `PlaylistNavigationCompleted_*`).

Hilfsmethoden: `Connected()`, `StartInfo(...)`, `NavItem(...)`, `PlayingInPlaylist(...)` (aktiver Playlist-Player: `ServerStream`-Quelle, `PlaylistId = 7`, `PlaylistEntryId = 5`, Position 96/100 s).

### `PlaybackSourceServiceTests` — `VideoPlayer.Maui.Tests/PlaybackSourceServiceTests.cs`

Dienst-Tests der Quellauflösung (reale SQLite-Ablage, injizierbare `fileExists`-Prüfung):

- `ResolveAsync_Movie_ReturnsStreamUrlWithToken`, `ResolveAsync_Episode_UsesServerResumePosition`, `ResolveAsync_ServerFails_UsesCachedResumePosition` — Stream-URL + Fortsetzungsposition (Server/Cache).
- `ResolveAsync_CompletedDownload_ReturnsLocalFile`, `ResolveAsync_MissingFile_FallsBackToStream`, `ResolveAsync_IncompleteDownload_FallsBackToStream` — LocalFile-Vorrang und Fallbacks.
- `ResolveAsync_OfflineMode_*`, `ResolveAsync_ResumeOverride_SkipsContinueWatchingLookup`, `ResolveAsync_LogsKind_NotLocation`, `ResolveAsync_NoSource_ReturnsNull`, `ResolveAsync_NonPlayable_ReturnsNull`.

### `EnumCoverageTests` — `VideoPlayer.Maui.Tests/EnumCoverageTests.cs`

- `PlaybackSourceKind_AllValuesAreCovered` (Z. 25) — pinnt die Wertemenge des Enums (Hook `enum-coverage-check`).

### `PlaybackTests` — `VideoPlayer.E2E.Tests/Tests/PlaybackTests.cs`

E2E-Szenarien des Wiedergabestarts (Windows-App gegen `MockVideoServer`):

- `PlayFromRecent_StartsPlaybackAtResumePosition` (Z. 25) — Start mit `PlaybackResumePositionSeconds = 1`; wartet, bis `PlayerPositionSeconds` ≥ Fortsetzungsposition; kein Fehler, kein Absturz. **Erfolgspfad des Fortsetzungs-Seeks** — ein Seek-Fehlschlag/Timeout-Szenario existiert nicht.
- `PlayWithFailingStream_ShowsErrorInsteadOfCrash` (Z. 49) — Stream-Endpunkt 404 → `playerError` im Modell statt Prozessabbruch.

Hilfsmethoden: `NewServer(streamFails)` (`MockVideoServer` mit `PlaybackResumePositionSeconds`, `PlaybackStreamFails`), `LoginOnHomeAsync`, `OpenDetailsAndPlayAsync` (`/invoke` auf `RecentCarousel`, `PlayButton`).

### `PlaylistPlaybackTests` — `VideoPlayer.E2E.Tests/Tests/PlaylistPlaybackTests.cs`

- `StreamFailure_AdvancesToNextPlaylistEntry` (Z. 262) — E2E-Beleg des Playlist-Fehler-Rückfalls (Stream-Fehlschlag → `play/advance`).
- `ContinueWatchingPlaylistEntry_AdvancesByPlaylistOrder_NotSeries` (Z. 312) — Fortsetzungsposition im Playlist-Kontext.
- `PlaybackEnd_AdvancesToNextPlaylistEntry`, `PlayButton_StartsPlaybackAtFirstPlayableEntry`, `SelectEntry_StartsPlaybackAtThatEntry`, `NextAndPreviousButtons_NavigatePlaylist`.

### `ContinueWatchingPlaybackTests` — `VideoPlayer.E2E.Tests/Tests/ContinueWatchingPlaybackTests.cs`

- `TapContinueWatchingMovie_OpensDetailsAndStartsPlaybackAtResume` / `TapContinueWatchingEpisode_OpensDetailsAndStartsPlaybackAndAdvancesToNextEpisode` — Fortsetzungsstart aus der Weiterschauen-Liste (Erfolgspfad).

## Hilfsmethoden

### `PlayerContractTests` (privat, in der Testklasse)
- `Read(params string[])` / `PlayerPageXaml()` / `PlayerPageCode()` — Quelltext-/XAML-Zugriff auf die Seite über `RepositoryRoot.Find()`.
- `Named(parent, element, name)` — `x:Name`-Suche im XAML.
- `MethodBody(code, signature)` — extrahiert Methodenrümpfe für Isolations-/Inhalts-Asserts.

### `AppUpdateTests_Player` / `AppUpdateTests_PlaylistPlayback` (privat)
- `PlayingState(...)`, `PlayingInPlaylist(...)` — fertige aktive `PlayerState`-Instanzen (Serien- bzw. Playlist-Kontext, `ServerStream`-Quelle).
- `WithPlayer(...)`, `WithDetails(...)`, `ShowDetails()`, `StartInfo(...)`, `NavItem(...)` — Modell-/Eintragsfabriken.

### `E2ETestBase` / `AppInstance` / `MockVideoServer` (`VideoPlayer.E2E.Tests`)
- `StartAppAsync`, `ConnectToPairingRequiredAsync`, `PairAsync`, `LoginAsync` — Standard-Einrichtungsfluss.
- `DevApi.WaitForModelAsync(predicate, timeout, beschreibung)` — Modell-Erwartungen (`PlayerIsActive`, `PlayerIsPlaying`, `PlayerPositionSeconds`, `PlayerError`).
- `MockVideoServer`-Optionen `PlaybackResumePositionSeconds`, `PlaybackEpisodeResumePositionSeconds`, `PlaybackStreamFails` — gesteuerte Fortsetzungspositionen und Stream-Fehlschlag; **kein Schalter für einen hängenden/abbrechenden Seek** (Toolkit-Timeout ist E2E-seitig nicht nachstellbar).
