# Tasks: Videowiedergabe mit Picture-in-Picture und Fortschrittsverfolgung (Entwicklungsschritt 10)

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Infrastruktur | `CommunityToolkit.Maui.MediaElement` 10.0.0 in `VideoPlayer.Maui.csproj` referenzieren | Offen | — |
| 2 | Infrastruktur | `UseMauiCommunityToolkitMediaElement()` in `MauiProgram` aufrufen | Offen | — |
| 3 | Infrastruktur | `UIBackgroundModes` `audio` in `Platforms/iOS/Info.plist` ergänzen (iOS-PiP) | Offen | — |
| 4 | Datentypen | Enum `PlaybackSourceKind` (`ServerStream`, `LocalFile`) anlegen | Offen | — |
| 5 | Datentypen | Record `PlaybackSource` (`Kind`, `Location`, `ResumePosition`) anlegen | Offen | — |
| 6 | Datentypen | Record `PlayerEpisodeRef` (`MediaId`, `Title`) anlegen | Offen | — |
| 7 | Datentypen | Record `PlayerState` mit `Empty` anlegen | Offen | — |
| 8 | Datentypen | `AppModel.Player` ergänzen | Offen | — |
| 9 | MVU | Player-Messages in `AppMessage.cs` (`PlayRequestedMessage`, `PlayerPageOpenedMessage`, `PlaybackSourceResolvedMessage`, `PlaybackProgressMessage`, `PlaybackEndedMessage`, `PlaybackFailedMessage`, `PictureInPictureRequestedMessage`, `PictureInPictureChangedMessage`, `PlayerClosedMessage`) anlegen | Offen | — |
| 10 | MVU | Player-Effekte in `AppEffect.cs` (`NavigateToPlayerEffect`, `ResolvePlaybackSourceEffect`, `ReportPlaybackProgressEffect`, `TogglePictureInPictureEffect`) anlegen | Offen | — |
| 11 | Logik | `PlaybackSourceService` anlegen (Manifest→LocalFile, Stream-URL+Token, Fortsetzungsposition Server→Cache, `fileExists` injizierbar) | Offen | — |
| 12 | Logik | `AppUpdate`: `UpdatePlayRequested` + `NextEpisodeQueue` (Serienkontext) | Offen | — |
| 13 | Logik | `AppUpdate`: `UpdatePlaybackSourceResolved` + `AdvanceToNextEpisode` | Offen | — |
| 14 | Logik | `AppUpdate`: `UpdatePlaybackProgress` (Report-Takt, „Angesehen", Auto-Fortsetzung) | Offen | — |
| 15 | Logik | `AppUpdate`: `UpdatePlaybackEnded`, `UpdatePlaybackFailed` | Offen | — |
| 16 | Logik | `AppUpdate`: `UpdatePlayerPageOpened`, `UpdatePlayerClosed`, PiP-Übergänge | Offen | — |
| 17 | Logik | `AppUpdate`: `UpdateLogoutRequested` um `Player`-Reset erweitern | Offen | — |
| 18 | UI | `PlayerPage.xaml` mit `MediaElement`, Titel-/Fehler-/Lade-Overlays, PiP-Schalter | Offen | — |
| 19 | UI | `PlayerPage.xaml.cs`: Takt-Timer, `SeekTo`-Fortsetzung, State→MediaElement-Anwendung | Offen | — |
| 20 | UI | `IPictureInPictureService` + `PictureInPictureService` (Windows-CompactOverlay) anlegen | Offen | — |
| 21 | UI | Route `player` in `AppShell.xaml.cs` registrieren | Offen | — |
| 22 | UI | `MediaDetailsPage`: „Abspielen"-Button (Kopf) + ▶ je Episode | Offen | — |
| 23 | Infrastruktur | `ShellAppEffectHandler`: vier neue Effekte verarbeiten | Offen | — |
| 24 | Infrastruktur | DI: `PlaybackSourceService`, `IPictureInPictureService`, `PlayerPage` | Offen | — |
| 25 | Tests | `AppUpdateTests_Player` (alle Übergänge inkl. Schwellen/Schalter) | Offen | — |
| 26 | Tests | `PlaybackSourceServiceTests` (Stream-URL, Fortsetzung, LocalFile) | Offen | — |
| 27 | Tests | `EnumCoverageTests` um `PlaybackSourceKind` erweitern | Offen | — |
| 28 | Tests | `PlayerContractTests` (Route, Paket, Info.plist, Play-Button) | Offen | — |
| 29 | Doku | `docs/help/`-Benutzerdoku um Player/PiP ergänzen | Offen | — |
| 30 | Doku | `README.md` um Schritt 10 ergänzen | Offen | — |
| 31 | Doku | `docs/RELEASE_NOTES.md` Eintrag für Schritt 10 | Offen | — |
