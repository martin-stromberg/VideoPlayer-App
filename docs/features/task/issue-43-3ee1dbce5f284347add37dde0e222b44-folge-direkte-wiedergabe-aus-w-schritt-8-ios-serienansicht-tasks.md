# Tasks: iOS-Bugfix — Serienansicht: Episodenliste und Staffelauswahl

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | UI | `EpisodesCollection` (`CollectionView`) durch `EpisodesList` (`VerticalStackLayout` + `BindableLayout`) in `MediaDetailsPage.xaml` ersetzen | Erledigt | `MediaDetailsPageContractTests.MediaDetailsPage_EpisodeList_BindableLayoutInSeasonSection`, `SeriesDetailsTests.ShowDetails_ShowsEpisodesAndAscendingSeasonOrder` |
| 2 | UI | `TapGestureRecognizer` (`OnEpisodeTapped`) im Episoden-Template und `AutomationId="SeasonPicker"`/`"EpisodesList"` ergänzen | Erledigt | `MediaDetailsPageContractTests.MediaDetailsPage_EpisodeList_BindableLayoutInSeasonSection`, `SeriesDetailsTests` (beide) |
| 3 | Logik | `MediaDetailsPage.xaml.cs`: `appliedSeasonItems`-Cache + `ItemsSource`/`SelectedItem`-Änderungsguards in `UpdateSeasonSection` | Erledigt | `MediaDetailsPageContractTests.MediaDetailsPage_SeasonPicker_StableItemsSourceAndEchoGuard`, `SeriesDetailsTests.SelectAnotherSeason_SwitchesEpisodesAndKeepsSelection` |
| 4 | Logik | `MediaDetailsPage.xaml.cs`: Echo-Guard in `OnSeasonSelected` (kein Dispatch bei `season.Id == SelectedSeason.Id`) | Erledigt | `MediaDetailsPageContractTests.MediaDetailsPage_SeasonPicker_StableItemsSourceAndEchoGuard`, `SeriesDetailsTests.SelectAnotherSeason_SwitchesEpisodesAndKeepsSelection` |
| 5 | Logik | `MediaDetailsPage.xaml.cs`: `OnEpisodeTapped` statt `OnEpisodeSelected` (Platzhalter-Guard, `MediaItemSelectedMessage`-Dispatch) | Erledigt | `MediaDetailsPageContractTests.MediaDetailsPage_EpisodeList_BindableLayoutInSeasonSection` |
| 6 | Logik | `DevApiModelSnapshot`: `MediaDetailsSeasonNames`, `MediaDetailsSelectedSeasonNumber`, `MediaDetailsSeasonEpisodeTitles` ergänzen | Erledigt | `SeriesDetailsTests` (beide — Modell-Assertions über diese Felder) |
| 7 | Tests | `DevApiModel` (E2E-Support): Gegenstück-Felder des Snapshots ergänzen | Erledigt | `SeriesDetailsTests` (beide) |
| 8 | Tests | `MockVideoServer`: `MockShowSeason`/`MockShowEpisode`, `ShowSeasons`-Opt-in, Staffeln im `ContinueWatchingShowId`-Branch | Erledigt | `SeriesDetailsTests` (beide), Regression `TapContinueWatchingShowEntry_OpensDetails_NotPlayback` |
| 9 | Tests | `UiDriver`: `SelectComboBoxItem` + `GetComboBoxItemNames` | Erledigt | `SeriesDetailsTests` (beide) |
| 10 | E2E-Tests | `SeriesDetailsTests.ShowDetails_ShowsEpisodesAndAscendingSeasonOrder` | Erledigt | Bestanden (siehe `test-results.md`) |
| 11 | E2E-Tests | `SeriesDetailsTests.SelectAnotherSeason_SwitchesEpisodesAndKeepsSelection` | Erledigt | Bestanden (siehe `test-results.md`) |
| 12 | Tests | `MediaDetailsPageContractTests`/`SourcesContractTests`: `EpisodesCollection`-Assertion auf `EpisodesList` + neue Contract-Tests (Picker-Stabilität, BindableLayout) | Erledigt | Unit-Lauf 1287/1287 grün |
| 13 | Dokumentation | iOS-Verifikationsgrenze in `docs/help/` dokumentieren | Erledigt | `docs/help/quellen-browsing-details.md` (Abschnitt „Serienansicht — Stabilität und iOS-Verifikationsgrenze"), `changes.log`, `docs/RELEASE_NOTES.md` |
