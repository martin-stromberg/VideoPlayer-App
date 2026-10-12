# Tasks: iPad — Vollbild- und PiP-Schaltflächen der nativen Wiedergabesteuerung frei erreichbar (Issue #45)

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | UI | `InputTransparent="True"` am `OverlayChrome`-`VerticalStackLayout` (Elternlayout) in `PlayerPage.xaml` setzen | Offen | — |
| 2 | UI | Kommentarblock zu `OverlayChrome` in `PlayerPage.xaml` um die iOS-Hit-Test-Begründung ergänzen (Elternlayout hit-testet trotz transparentem Inhalt in eigenen Bounds) | Offen | — |
| 3 | Logik | `#if IOS`-Block im `PlayerPage`-Konstruktor (`PlayerPage.xaml.cs`): `PlaylistNavigationBar.Margin = new Thickness(0, 96, 16, 0)` inkl. erläuterndem Kommentar | Offen | — |
| 4 | Tests | `PlayerContractTests.PlayerPage_OverlayChromeDoesNotCoverPlaybackSurface` um Assertion `InputTransparent="True"` am `OverlayChrome`-Elternlayout erweitern | Offen | — |
| 5 | Tests | Neuen Contract-Test `PlayerContractTests.PlayerPage_PlaylistNavigationBarClearsNativeControlsOnIos` anlegen (`#if IOS`-Margin im Konstruktor; Bar bleibt `PlayerRoot`-Kind ohne `InputTransparent`) | Offen | — |
| 6 | Tests | `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj --configuration Release` ausführen (grün) | Offen | — |
| 7 | Build | `dotnet build VideoPlayer.App.sln` in Debug und Release fehlerfrei | Offen | — |
| 8 | Verifikation | Manuelle iPad-Verifikation per TestFlight: Vollbild-/PiP-Buttons der nativen Leiste bedienbar, Playlist-Navigation funktionsfähig, Windows-Mini-Player unverändert | Offen | — |
| 9 | Dokumentation | `docs/help/wiedergabe-pip.md`: iOS-Eingabedurchlässigkeit des Elternlayouts, tiefer positionierte Playlist-Navigation und iOS-Verifikationsgrenze dokumentieren | Offen | — |
