# Tasks: Scrollposition beim Nachladen der Quellen-Übersicht

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Logik | `SourceItemListSync` in `VideoPlayer.Mvu` anlegen (Präfix-Abgleich über `Id`, minimaler `CollectionChanged`-Umfang) | Offen | — |
| 2 | Tests | `SourceItemListSyncTests` anlegen: Append ohne Reset (nur `Add`), Ersetzen ab Divergenz, Schrumpfen, No-Op, Leeren | Offen | — |
| 3 | UI | `ItemsSource`-Binding aus `SourceOverviewPage.xaml` entfernen | Offen | — |
| 4 | UI | `browserItems`-`ObservableCollection` + einmalige `ItemsSource`-Zuweisung + `Sync`-Aufruf in `UpdateUi` in `SourceOverviewPage.xaml.cs` | Offen | — |
| 5 | E2E-Tests | `UiDriver.ScrollToEnd` und `UiDriver.GetVerticalScrollPercent` ergänzen | Offen | — |
| 6 | E2E-Tests | `SourcePagingTests.NextPageLoad_KeepsScrollPosition` anlegen (Nachladen hält Scrollposition; Einträge wachsen am Ende) | Offen | — |
| 7 | Tests | `dotnet test VideoWebPlayer.Maui.Tests` vollständig grün | Offen | — |
