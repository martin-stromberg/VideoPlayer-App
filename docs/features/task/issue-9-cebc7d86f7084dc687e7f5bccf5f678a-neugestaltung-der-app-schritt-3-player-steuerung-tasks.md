# Tasks: Videoplayer — Steuerelemente in der Vollansicht bedienbar (Schritt 3)

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | UI | `OverlayChrome` in `PlayerPage.xaml` auf `VerticalOptions="Start"` schrumpfen | Erledigt | `PlayerContractTests.PlayerPage_OverlayChromeDoesNotCoverPlaybackSurface` |
| 2 | UI | Kopfband-`Border` `InputTransparent="True"`, `PipButton` `InputTransparent="False"` | Erledigt | `PlayerContractTests.PlayerPage_OverlayChromeDoesNotCoverPlaybackSurface` |
| 3 | Tests | Contract-Pin `PlayerPage_OverlayChromeDoesNotCoverPlaybackSurface` in `PlayerContractTests` | Erledigt | Negativprobe auf Ausgangsstand fehlgeschlagen, auf Fix-Stand grün |
| 4 | Tests | DoD: Build Debug+Release fehlerfrei, volle Testsuite grün | Offen | — |
