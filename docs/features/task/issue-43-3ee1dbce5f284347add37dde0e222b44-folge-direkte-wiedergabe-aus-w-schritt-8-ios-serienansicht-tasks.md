# Tasks: iOS-Bugfix — Serienansicht: Episodenliste und Staffelauswahl

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | UI | `EpisodesCollection` (`CollectionView`) durch `EpisodesList` (`VerticalStackLayout` + `BindableLayout`) in `MediaDetailsPage.xaml` ersetzen | Offen | — |
| 2 | UI | `TapGestureRecognizer` (`OnEpisodeTapped`) im Episoden-Template und `AutomationId="SeasonPicker"`/`"EpisodesList"` ergänzen | Offen | — |
| 3 | Logik | `MediaDetailsPage.xaml.cs`: `appliedSeasonItems`-Cache + `ItemsSource`/`SelectedItem`-Änderungsguards in `UpdateSeasonSection` | Offen | — |
| 4 | Logik | `MediaDetailsPage.xaml.cs`: Echo-Guard in `OnSeasonSelected` (kein Dispatch bei `season.Id == SelectedSeason.Id`) | Offen | — |
| 5 | Logik | `MediaDetailsPage.xaml.cs`: `OnEpisodeTapped` statt `OnEpisodeSelected` (Platzhalter-Guard, `MediaItemSelectedMessage`-Dispatch) | Offen | — |
| 6 | Logik | `DevApiModelSnapshot`: `MediaDetailsSeasonNames`, `MediaDetailsSelectedSeasonNumber`, `MediaDetailsSeasonEpisodeTitles` ergänzen | Offen | — |
| 7 | Tests | `DevApiModel` (E2E-Support): Gegenstück-Felder des Snapshots ergänzen | Offen | — |
| 8 | Tests | `MockVideoServer`: `MockShowSeason`/`MockShowEpisode`, `ShowSeasons`-Opt-in, Staffeln im `ContinueWatchingShowId`-Branch | Offen | — |
| 9 | Tests | `UiDriver`: `SelectComboBoxItem` + `GetComboBoxItemNames` | Offen | — |
| 10 | E2E-Tests | `SeriesDetailsTests.ShowDetails_ShowsEpisodesAndAscendingSeasonOrder` | Offen | — |
| 11 | E2E-Tests | `SeriesDetailsTests.SelectAnotherSeason_SwitchesEpisodesAndKeepsSelection` | Offen | — |
| 12 | Tests | `MediaDetailsPageContractTests`: `EpisodesCollection`-Assertion auf `EpisodesList` + neue Contract-Tests (Picker-Stabilität, BindableLayout) | Offen | — |
| 13 | Dokumentation | iOS-Verifikationsgrenze in `docs/help/` dokumentieren | Offen | — |
