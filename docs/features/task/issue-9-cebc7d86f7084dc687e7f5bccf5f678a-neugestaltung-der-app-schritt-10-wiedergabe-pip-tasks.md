# Tasks: Videowiedergabe mit Picture-in-Picture und Fortschrittsverfolgung (Entwicklungsschritt 10)

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Infrastruktur | `CommunityToolkit.Maui.MediaElement` 10.0.0 in `VideoPlayer.Maui.csproj` referenzieren | Erledigt | `PlayerContractTests.MauiApp_UsesMediaElement` |
| 2 | Infrastruktur | `UseMauiCommunityToolkitMediaElement(true)` in `MauiProgram` aufrufen | Erledigt | `PlayerContractTests.MauiApp_UsesMediaElement` |
| 3 | Infrastruktur | `UIBackgroundModes` `audio` in `Platforms/iOS/Info.plist` ergänzen (iOS-PiP) | Erledigt | `PlayerContractTests.IosInfoPlist_DeclaresAudioBackgroundMode` |
| 4 | Datentypen | Enum `PlaybackSourceKind` (`ServerStream`, `LocalFile`) anlegen | Erledigt | `EnumCoverageTests.PlaybackSourceKind_AllValuesAreCovered` |
| 5 | Datentypen | Record `PlaybackSource` (`Kind`, `Location`, `ResumePosition`) anlegen | Erledigt | `PlaybackSourceServiceTests` |
| 6 | Datentypen | Record `PlayerEpisodeRef` (`MediaId`, `Title`) anlegen | Erledigt | `AppUpdateTests_Player.PlayRequested_Episode_BuildsUpcomingEpisodes` |
| 7 | Datentypen | Record `PlayerState` mit `Empty` anlegen | Erledigt | `AppUpdateTests_Player` (alle) |
| 8 | Datentypen | `AppModel.Player` ergänzen | Erledigt | `AppUpdateTests_Player` (alle) |
| 9 | MVU | Player-Messages in `AppMessage.cs` (`PlayRequestedMessage`, `PlayerPageOpenedMessage`, `PlaybackSourceResolvedMessage`, `PlaybackProgressMessage`, `PlaybackEndedMessage`, `PlaybackFailedMessage`, `PictureInPictureRequestedMessage`, `PictureInPictureChangedMessage`, `PlayerClosedMessage`) anlegen | Erledigt | `AppUpdateTests_Player`, `PlayerContractTests.PlayerPage_DispatchesPlaybackMessages` |
| 10 | MVU | Player-Effekte in `AppEffect.cs` (`NavigateToPlayerEffect`, `ResolvePlaybackSourceEffect`, `ReportPlaybackProgressEffect`, `TogglePictureInPictureEffect`) anlegen | Erledigt | `AppUpdateTests_Player`, `PlayerContractTests.EffectHandler_HandlesPlayerEffects` |
| 11 | Logik | `PlaybackSourceService` anlegen (Manifest→LocalFile, Stream-URL+Token, Fortsetzungsposition Server→Cache, `fileExists` injizierbar) | Erledigt | `PlaybackSourceServiceTests` (8 Tests) |
| 12 | Logik | `AppUpdate`: `UpdatePlayRequested` + `NextEpisodeQueue` (Serienkontext) | Erledigt | `PlayRequested_*` (4 Tests) |
| 13 | Logik | `AppUpdate`: `UpdatePlaybackSourceResolved` + `AdvanceToNextEpisode` | Erledigt | `PlaybackSourceResolved_*` (4 Tests) |
| 14 | Logik | `AppUpdate`: `UpdatePlaybackProgress` (Report-Takt, „Angesehen", Auto-Fortsetzung) | Erledigt | `PlaybackProgress_*` (8 Tests) |
| 15 | Logik | `AppUpdate`: `UpdatePlaybackEnded`, `UpdatePlaybackFailed` | Erledigt | `PlaybackEnded_*` (2 Tests), `PlaybackFailed_ShowsErrorAndFallsBack` |
| 16 | Logik | `AppUpdate`: `UpdatePlayerPageOpened`, `UpdatePlayerClosed`, PiP-Übergänge | Erledigt | `PlayerClosed_*` (2 Tests), `PictureInPicture_*` (2 Tests) |
| 17 | Logik | `AppUpdate`: `UpdateLogoutRequested` um `Player`-Reset erweitern | Erledigt | `Logout_ResetsPlayer` |
| 18 | UI | `PlayerPage.xaml` mit `MediaElement`, Titel-/Fehler-/Lade-Overlays, PiP-Schalter | Erledigt | `PlayerContractTests.PlayerPage_UsesMediaElementWithControls` |
| 19 | UI | `PlayerPage.xaml.cs`: Takt-Timer, `SeekTo`-Fortsetzung, State→MediaElement-Anwendung | Erledigt | `PlayerContractTests.PlayerPage_DispatchesPlaybackMessages` |
| 20 | UI | `IPictureInPictureService` + `PictureInPictureService` (Windows-CompactOverlay) anlegen | Erledigt | `PlayerContractTests.PictureInPictureService_UsesCompactOverlayOnWindows` |
| 21 | UI | Route `player` in `AppShell.xaml.cs` registrieren | Erledigt | `PlayerContractTests.PlayerPage_RouteRegistered` |
| 22 | UI | `MediaDetailsPage`: „Abspielen"-Button (Kopf) + ▶ je Episode | Erledigt | `PlayerContractTests.MediaDetailsPage_HasPlayButton` |
| 23 | Infrastruktur | `ShellAppEffectHandler`: vier neue Effekte verarbeiten | Erledigt | `PlayerContractTests.EffectHandler_HandlesPlayerEffects` |
| 24 | Infrastruktur | DI: `PlaybackSourceService`, `IPictureInPictureService`, `PlayerPage` | Erledigt | `PlayerContractTests.MauiApp_UsesMediaElement` |
| 25 | Tests | `AppUpdateTests_Player` (alle Übergänge inkl. Schwellen/Schalter) | Erledigt | 23 Tests |
| 26 | Tests | `PlaybackSourceServiceTests` (Stream-URL, Fortsetzung, LocalFile) | Erledigt | 9 Tests |
| 27 | Tests | `EnumCoverageTests` um `PlaybackSourceKind` erweitern | Erledigt | 1 Test |
| 28 | Tests | `PlayerContractTests` (Route, Paket, Info.plist, Play-Button) | Erledigt | 8 Tests |
| 29 | Doku | `docs/help/`-Benutzerdoku um Player/PiP ergänzen | Erledigt | `docs/help/wiedergabe-pip.md` + Index-Eintrag |
| 30 | Doku | `README.md` um Schritt 10 ergänzen | Erledigt | README-Projektbeschreibungen |
| 31 | Doku | `docs/RELEASE_NOTES.md` Eintrag für Schritt 10 | Erledigt | je ein Eintrag in „What's New" und „Neuerungen" |

**Gesamt:** 510 Tests bestanden (Baseline 469 → +41), 0 Fehler — siehe `…-schritt-10-wiedergabe-pip/test-results.md`.
