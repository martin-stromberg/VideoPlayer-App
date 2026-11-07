# Tasks: Genreauswahl in Quellenansicht verhindert die Einsicht der Titel

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Tests | `DevApiBounds`-DTO + `bounds`-Feld an `DevApiTreeNode` (E2E-Support, `DevApiModels.cs`) | Erledigt | `SourceBrowsingTests.GenreBar_HeightBounded_ItemsGridVisible` (nutzt `Bounds` für den Höhennachweis) |
| 2 | Tests | Contract-Test `SourceOverviewPage_GenreBar_UsesBoundedHorizontalLayout` in `SourcesContractTests` (Red-Nachweis) | Erledigt | Test selbst — vor Umbau fehlgeschlagen, danach bestanden |
| 3 | UI | Genre-Leiste in `SourceOverviewPage.xaml` auf `ScrollView` + `HorizontalStackLayout` + `BindableLayout` mit `TapGestureRecognizer` umbauen | Erledigt | `SourcesContractTests.SourceOverviewPage_GenreBar_UsesBoundedHorizontalLayout`, `SourceBrowsingTests.GenreBar_HeightBounded_ItemsGridVisible` |
| 4 | UI | `OnGenreSelected` in `SourceOverviewPage.xaml.cs` durch `OnGenreTapped` ersetzen | Erledigt | `SourcesContractTests.SourceOverviewPage_DispatchesBrowserMessages`, `SourceBrowsingTests.GenreBar_VisibleAndFilters_ResetsOnAll` |
| 5 | E2E-Tests | `GenreBar_HeightBounded_ItemsGridVisible` in `SourceBrowsingTests` (Bounds-Höhennachweis + sichtbares Raster) | Erledigt | Test selbst — E2E-Lauf 7/7 bestanden |
| 6 | Tests | Build + Unit-Suite + E2E-Teilsuite (SourceBrowsing/SourceSearch/SourcePaging) ausführen | Erledigt | Windows-Debug-Build ok; Contract-Tests grün; E2E 7/7 bestanden |
