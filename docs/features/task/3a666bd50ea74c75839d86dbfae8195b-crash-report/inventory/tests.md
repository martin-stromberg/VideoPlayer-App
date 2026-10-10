# Tests — Wiedergabe-Pfad

Testprojekt: `VideoWebPlayer.Maui.Tests` (xUnit; die MAUI-Schicht
`VideoPlayer.Maui` ist im Testprojekt nicht instanziierbar —
MAUI-nahe Konventionen werden quelltextbasiert als Contract-Tests
geprüft, MVU-Logik direkt).

## Testklassen

### `AppUpdateTests_Player`

Datei: `VideoWebPlayer.Maui.Tests/AppUpdateTests_Player.cs`

- `PlayRequested_ValidMovie_SetsPlayerStateAndEffects` — Player-State + `NavigateToPlayerEffect` + `ResolvePlaybackSourceEffect`.
- `PlayRequested_Episode_BuildsUpcomingEpisodes` — Fortsetzungsliste über Staffeln hinweg.
- `PlayRequested_LastEpisode_HasNoUpcoming` — leere Liste am Serienende.
- `PlayRequested_InvalidIdentity_Unchanged` — `Id <= 0`/nicht abspielbare Typen ignoriert.
- `PlaybackSourceResolved_Success_SetsSource` — Quelle + `IsResolving=false`.
- `PlaybackSourceResolved_Error_FallsBackToNextEpisode` — Fehler-Rückfall mit Fehlertext.
- `PlaybackSourceResolved_ErrorWithoutNext_StaysOnError` — Fehlerzustand ohne Folge-Episode.
- `PlaybackSourceResolved_StaleResponse_Discarded` — veraltete Antwort verworfen.
- `PlaybackProgress_ReportsChangedPosition` / `_UnchangedPosition_NoReport` — Fortschritts-Throttling.
- `PlaybackProgress_MarksWatchedWithinWindow` — „Angesehen"-Fenster.
- `PlaybackProgress_AutoResumeAdvancesWhenEnabled` / `_Disabled_DoesNotAdvance` / `_AboveResumeWindow_DoesNotAdvance` / `_PausedWithinWindow_DoesNotAdvance` / `_NoNextEpisode_DoesNotAdvance` — Auto-Fortsetzung.
- `PlaybackEnded_MarksWatchedAndAdvances` / `_WithoutNext_MarksWatchedOnly` — Ende-Übergang.
- `PlaybackEnded_MarksEpisodeWatchedInDetails` / `_AutoResume_StillMarksFinishedEpisode` / `_AlreadyWatchedEpisode_KeepsServerTimestamp` / `_Movie_DoesNotTouchEpisodeList` / `_UnknownEpisode_LeavesDetailsUnchanged` — `WatchedAt`-Markierung.
- `PlaybackFailed_ShowsErrorAndFallsBack` — Fehler-Rückfall bei `MediaFailed`.
- `PlayerClosed_ReportsFinalAndResets` / `_WithoutPosition_ResetsOnly` — Abschlussmeldung + Reset + PiP-Effekt.
- `PictureInPicture_TogglesFlag` / `_InactivePlayer_Unchanged` — PiP-Umschaltung.
- `Logout_ResetsPlayer` — Player-Reset bei Abmeldung.

### `PlayerContractTests`

Datei: `VideoWebPlayer.Maui.Tests/PlayerContractTests.cs` —
quelltextbasierte Contract-Tests der MAUI-Schicht:

- `PlayerPage_RouteRegistered` — Route `player` in `AppShell.xaml.cs`.
- `MauiApp_UsesMediaElement` — Paket `CommunityToolkit.Maui.MediaElement`, `UseMauiCommunityToolkitMediaElement`, `PlaybackSourceService`, `IPictureInPictureService` in `MauiProgram.cs`.
- `IosInfoPlist_DeclaresAudioBackgroundMode` — `UIBackgroundModes` = `audio` in `Platforms/iOS/Info.plist`.
- `PlayerPage_UsesMediaElementWithControls` — `ShouldAutoPlay`, `ShouldShowPlaybackControls`, verdrahtete `MediaOpened/Ended/Failed`-Events.
- `PlayerPage_DispatchesPlaybackMessages` — alle Player-Messages, `SeekTo`, `PlaybackSourceKind.LocalFile`, `MediaSource.FromFile`.
- `PlayerPage_OverlayChromeDoesNotCoverPlaybackSurface` — `VerticalOptions="Start"`, `InputTransparent`, `PipButton` außerhalb des Bandes.
- `PlayerPage_TapShowsTransportControlsOnWindows` — Windows-Tap-Workaround.
- `PictureInPictureService_UsesCompactOverlayOnWindows` — Windows-PiP-Pfad.
- `MediaDetailsPage_HasPlayButton` — `PlayButton`, `OnEpisodePlayClicked`, `PlayRequestedMessage`, `IsPlayable`-Guard.
- `PlayerPage_UnsubscribesModelChangedOnDisappearing` — Abo im Sichtbarkeitszyklus (`+=` in `OnAppearing`, `-=` in `OnDisappearing`, kein Abo im Konstruktor).
- `PlayerPage_MarshalsNativeElementAccessOnMainThread` — `BeginInvokeOnMainThread` in `OnMediaOpened`/`OnMediaStateChanged`; genau ein `SeekTo(`-Aufruf, innerhalb des Marshals (Windows-Fix `RPC_E_WRONG_THREAD`).
- `PlayerPage_IgnoresModelChangesAfterDisappearing` — `subscribed`-Guard in allen Event-Handlern.
- `EffectHandler_HandlesPlayerEffects` — alle Player-Effekte + `ProgressReportService` im `ShellAppEffectHandler`.

### `PlaybackSourceServiceTests`

Datei: `VideoWebPlayer.Maui.Tests/PlaybackSourceServiceTests.cs`
(`[Collection("SqliteDatabase")]`, injizierbare `fileExists`-Prüfung)

- `ResolveAsync_Movie_ReturnsStreamUrlWithToken` — Stream-URL mit `access_token`.
- `ResolveAsync_Episode_UsesServerResumePosition` — Fortsetzungsposition vom Server.
- `ResolveAsync_ServerFails_UsesCachedResumePosition` — Cache-Fallback.
- `ResolveAsync_CompletedDownload_ReturnsLocalFile` — `LocalFile` hat Vorrang.
- `ResolveAsync_MissingFile_FallsBackToStream` / `_IncompleteDownload_FallsBackToStream` — lokale Datei nur bei `completed` + vorhandener Datei.
- `ResolveAsync_OfflineMode_ReturnsLocalFileWithoutServerCall` / `_OfflineModeWithoutLocalFile_ReturnsNull` — Offline-Verhalten (`allowServerStream=false`).
- `ResolveAsync_NoSource_ReturnsNull` / `_NonPlayable_ReturnsNull` — Fehler- und Typ-Guards.

### `PageLifecycleContractTests` (angrenzend)

Datei: `VideoWebPlayer.Maui.Tests/PageLifecycleContractTests.cs` —
Lebenszyklus-Konvention (`ModelChanged`-Abo im Sichtbarkeitsfenster)
für alle Seiten, `PlayerPage` als Referenzmuster genannt.

### `IosDeploymentResponseFileTests` (angrenzend)

Datei: `VideoWebPlayer.Maui.Tests/IosDeploymentResponseFileTests.cs` —
Contract-Tests des iOS-Deployment-Skripts (Response-Datei-Sanitizing);
kein Bezug zu dSYM-Archivierung (diese existiert nicht).

## Hilfsmethoden / Stubs

### `Stubs/`

- `InMemorySettingsStore`, `InMemorySecretStore` — Storage-Ersatz.
- `StubHttpMessageHandler` — HTTP-Stub für API-Client-Dienste.
- `RecordingDelays` — deterministische Retry-Verzögerungen.
- `RepositoryRoot.Find()` — Repo-Root für quelltextbasierte Contract-Tests.

### Test-Bausteine in `AppUpdateTests_Player`

- `Connected()` — `AppModel` im verbundenen Zustand.
- `ShowDetails()` — `MediaDetailsView` mit zwei Staffeln à zwei Episoden (31–34).
- `PlayingState(...)` — vorbefüllter `PlayerState` mit `ServerStream`-Quelle.
- `WithDetails`/`WithPlayer` — Model-Komposition.

## Lücke (festgestellt)

Kein Test bildet den iOS-Toolkit-Pfad (`MediaElement`-Handler/Seek)
ab — die Contract-Tests sichern nur das Vorhandensein der
Marshals/Guards im Quelltext, nicht deren Laufzeitverhalten. Ein
Contract-Test könnte z. B. die Konvention „native Player-Aufrufe
try/catch-umschlossen und Fehler → `PlaybackFailedMessage`"
quelltextbasiert pinnen.
