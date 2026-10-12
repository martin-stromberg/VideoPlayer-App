# Tasks: iPad — Vollbild- und PiP-Schaltflächen der nativen Wiedergabesteuerung frei erreichbar (Issue #45)

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | UI | `InputTransparent="True"` am `OverlayChrome`-`VerticalStackLayout` (Elternlayout) in `PlayerPage.xaml` setzen | Erledigt | `PlayerPage_OverlayChromeDoesNotCoverPlaybackSurface` |
| 2 | UI | Kommentarblock zu `OverlayChrome` in `PlayerPage.xaml` um die iOS-Hit-Test-Begründung ergänzen (Elternlayout hit-testet trotz transparentem Inhalt in eigenen Bounds) | Erledigt | Kein direkter Test |
| 3 | Logik | `#if IOS`-Block im `PlayerPage`-Konstruktor (`PlayerPage.xaml.cs`): `OverlayChrome`- und `PlaylistNavigationBar`-Margin um die benannte Konstante `IosNativeControlsClearance` (= 96) vertiefen inkl. erläuterndem Kommentar | Erledigt | `PlayerPage_PlaylistNavigationBarClearsNativeControlsOnIos` |
| 4 | Tests | `PlayerContractTests.PlayerPage_OverlayChromeDoesNotCoverPlaybackSurface` um Assertion `InputTransparent="True"` am `OverlayChrome`-Elternlayout erweitern | Erledigt | Bestanden |
| 5 | Tests | Neuen Contract-Test `PlayerContractTests.PlayerPage_PlaylistNavigationBarClearsNativeControlsOnIos` anlegen (`#if IOS`-Margin im Konstruktor; Bar bleibt `PlayerRoot`-Kind ohne `InputTransparent`) | Erledigt | Bestanden |
| 6 | Tests | `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj --configuration Release` ausführen (grün) | Erledigt | 1257 erfolgreich, 0 fehlgeschlagen, Exit-Code 0 |
| 7 | Build | `dotnet build VideoPlayer.App.sln` in Debug und Release fehlerfrei | Erledigt | Debug: 0 Fehler; Release: 0 Fehler (nur preexisting Warnungen) |
| 8 | Verifikation | Manuelle iPad-Verifikation per TestFlight: Vollbild-/PiP-Buttons der nativen Leiste bedienbar, Playlist-Navigation funktionsfähig, Windows-Mini-Player unverändert | Offen | — (manueller Geräte-Prüfschritt, nicht automatisierbar) |
| 9 | Dokumentation | `docs/help/wiedergabe-pip.md`: iOS-Eingabedurchlässigkeit des Elternlayouts, tiefer positionierte Playlist-Navigation und iOS-Verifikationsgrenze dokumentieren | Offen | — (Lifecycle-Schritt Dokumentation) |
| 10 | Review Iteration 2 | iOS-Versatz als benannte Konstante `IosNativeControlsClearance` ausgelagert (Magic Number); `OverlayChrome` unter iOS ebenfalls unter die native Leiste geschoben (visueller Freiraum für Scrim/`WatchedBadge`); XAML-Lookup-Hilfsmethoden (`PlayerPageXaml()`, `PlayerPageCode()`, `Named(...)`) in `PlayerContractTests` extrahiert | Erledigt | `PlayerPage_PlaylistNavigationBarClearsNativeControlsOnIos` (erweitert), 1257 Tests bestanden |
