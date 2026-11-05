# Tasks: tvOS-App — Browsing und Suche (Schritt 5)

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Dienste | `TvosPosterImageService.GetSourceIconAsync` ergänzen (`api/sourceicons`, Suffix `.sourceicon`, eigene Speicher-Map) | Erledigt | `TvOsBrowsingContractTests.TvOsPosterImageService_LoadsSourceIconsWithSeparateCache` |
| 2 | UI | `SourceItemCell` anlegen (fokussierbare Quellen-Karte, Icon/Name, `ReuseId`) | Erledigt | `TvOsBrowsingContractTests.SourceCells_AreFocusableCards` |
| 3 | UI | `SourceCarouselView` anlegen (horizontale Quellen-Reihe, Muster `HomeCarouselView`) | Erledigt | `TvOsBrowsingContractTests.HomePage_ShowsSourceSectionWithStates` |
| 4 | UI | `HomePageViewController`: Quellen-Sektion aus `SourcesOverview` (Karten/Lade/Leer/Fehler + Neuversuch, `SourceSelectedMessage`, `SourcesReloadRequestedMessage`) | Erledigt | `TvOsBrowsingContractTests.HomePage_ShowsSourceSectionWithStates` |
| 5 | UI | `SourceGenreCell` anlegen (fokussierbare Genre-Kachel mit Auswahlzustand) | Erledigt | `TvOsBrowsingContractTests.SourceCells_AreFocusableCards` |
| 6 | UI | `SourceBrowserViewController` anlegen (Titel, `UITextField`-Suche mit `mirroredSearchText`-Guard, Genre-Leiste, Raster, Zustände, Menü-Rückkehr, `SourceBrowserOpenedMessage`) | Erledigt | `TvOsBrowsingContractTests.SourceBrowserPage_RendersGridGenresAndSearchFromModel` / `SourceBrowserPage_SearchTextStaysVisibleWhileEditing` / `SourceBrowserPage_StatesAndRetryAreRemoteFriendly` |
| 7 | UI | Raster-Nachladen über `WillDisplayCell` → `SourceItemsNextPageRequestedMessage` | Erledigt | `TvOsBrowsingContractTests.SourceBrowserPage_LoadsNextPageAtListEnd` |
| 8 | Navigation | `TvosNavigator.PresentSourceBrowser` + Render-Verteilung an `sourceBrowserPage` | Erledigt | `TvOsBrowsingContractTests.TvOsBrowsing_WiringIsComplete` |
| 9 | Navigation | `PresentPreparedTarget`-Anker auf `main.PresentedViewController ?? main` | Erledigt | `TvOsBrowsingContractTests.TvOsBrowsing_WiringIsComplete` |
| 10 | Komposition | `AppComposition.SourceBrowser` (`SourceBrowserService`-Singleton) | Erledigt | `TvOsBrowsingContractTests.TvOsBrowsing_WiringIsComplete` |
| 11 | Effekte | `TvosAppEffectHandler`: `LoadSourcesEffect`-Fall + `LoadSourcesAsync` | Erledigt | `TvOsBrowsingContractTests.TvOsBrowsing_WiringIsComplete` |
| 12 | Effekte | `TvosAppEffectHandler`: `LoadSourceGenresEffect`-Fall + `LoadSourceGenresAsync` | Erledigt | `TvOsBrowsingContractTests.TvOsBrowsing_WiringIsComplete` |
| 13 | Effekte | `TvosAppEffectHandler`: `LoadSourceItemsEffect`-Fall + `LoadSourceItemsAsync` | Erledigt | `TvOsBrowsingContractTests.TvOsBrowsing_WiringIsComplete` |
| 14 | Effekte | `TvosAppEffectHandler`: `NavigateToSourceOverviewEffect`-Fall → `PresentSourceBrowser` | Erledigt | `TvOsBrowsingContractTests.TvOsBrowsing_WiringIsComplete` |
| 15 | Tests | `TvOsBrowsingContractTests` anlegen (Quell-Contracts aller neuen Dateien/Verdrahtung) | Erledigt | Alle 8 Tests grün; Gesamtsuite 1480/1480 |
