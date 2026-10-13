# Tasks: Sucheingabe der Quellen-Übersichtsseite übersteht Modell-Aktualisierungen

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | UI | `mirroredSearchText`-Merker in `SourceOverviewPage` ergänzen und `UpdateUi`-Spiegelung auf Modell-Änderung begrenzen | Erledigt | `SourceOverviewSearchContractTests.UpdateUi_WritesSearchText_OnlyWhenModelTextChanged`, E2E `SourceSearchTests.UnsubmittedInput_SurvivesModelTicks` |
| 2 | UI | `AutomationId="MediaSearchBar"` am `SearchBar` in `SourceOverviewPage.xaml` ergänzen | Erledigt | `SourceOverviewSearchContractTests.MediaSearchBar_HasAutomationId` |
| 3 | DevApi | `SourceBrowserSearchText` in `DevApiModelSnapshot` (Mvu) und E2E-`DevApiModels` ergänzen | Erledigt | `DevApiModelSnapshotTests.DevApiModelSnapshot_MapsSourceBrowser` |
| 4 | E2E-Support | `UiDriver.PressEnter(automationId)` (Fokus + Return-Taste) ergänzen | Erledigt | `SourceSearchTests.SubmitViaEnter_FiltersFromFirstPage` (nutzt die Methode) |
| 5 | Tests | `SourceOverviewSearchContractTests` anlegen (Spiegel-Guard, AutomationId, Submit-Dispatch) | Erledigt | 4 Tests grün im Unit-Lauf |
| 6 | Tests | `DevApiModelSnapshotTests` um `SourceBrowserSearchText`-Mapping erweitern | Erledigt | `DevApiModelSnapshot_MapsSourceBrowser` |
| 7 | E2E-Tests | `SourceSearchTests.UnsubmittedInput_SurvivesModelTicks` | Erledigt | E2E-Lauf bestanden |
| 8 | E2E-Tests | `SourceSearchTests.SubmitViaEnter_FiltersFromFirstPage` | Erledigt | E2E-Lauf bestanden |
| 9 | E2E-Tests | `SourceSearchTests.SubmittedText_RemainsVisibleAfterReturn` | Erledigt | E2E-Lauf bestanden |
