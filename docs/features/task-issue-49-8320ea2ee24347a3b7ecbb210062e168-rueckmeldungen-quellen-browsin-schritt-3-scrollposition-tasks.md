# Tasks: Scrollposition beim Nachladen der Quellen-Übersicht

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Logik | `SourceItemListSync` in `VideoPlayer.Mvu` anlegen (Präfix-Abgleich über `Id`, minimaler `CollectionChanged`-Umfang) | Erledigt | `SourceItemListSyncTests.*` (6 Tests) |
| 2 | Tests | `SourceItemListSyncTests` anlegen: Append ohne Reset (nur `Add`), Ersetzen ab Divergenz, Schrumpfen, No-Op, Leeren | Erledigt | `SourceItemListSyncTests.*` |
| 3 | UI | `ItemsSource`-Binding aus `SourceOverviewPage.xaml` entfernen | Erledigt | `SourcesContractTests.SourceOverviewPage_HasGenreBar_Search_AndIncrementalGrid` (`DoesNotContain` auf das Binding) |
| 4 | UI | `browserItems`-`ObservableCollection` + einmalige `ItemsSource`-Zuweisung + `Sync`-Aufruf in `UpdateUi` in `SourceOverviewPage.xaml.cs` | Erledigt | `SourcesContractTests.SourceOverviewPage_HasGenreBar_Search_AndIncrementalGrid` |
| 5 | E2E-Tests | `UiDriver.ScrollToEnd` und `UiDriver.GetVerticalScrollPercent` ergänzen | Erledigt | `SourcePagingTests.NextPageLoad_KeepsScrollPosition` (nutzt beide) |
| 6 | E2E-Tests | `SourcePagingTests.NextPageLoad_KeepsScrollPosition` anlegen (Nachladen hält Scrollposition; Einträge wachsen am Ende) | Erledigt | E2E-Lauf bestanden (11 s) |
| 7 | Tests | `dotnet test VideoWebPlayer.Maui.Tests` vollständig grün | Erledigt | 1326/1326 bestanden |
