# Tasks: Detailseiten — Vollbreiten-Header mit Hintergrundgrafik

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | API-Client | `GetEpisodeBackgroundImageAsync` in `VideoPlayerApiClient` anlegen (`GET api/episodes/{id}/background-image`) | Offen | — |
| 2 | Datenmodell | `SeasonItem.BannerPictureId` und `SeasonItem.Year` hinzufügen | Offen | — |
| 3 | Datenmodell | `EpisodeItem.ReleaseYear` hinzufügen | Offen | — |
| 4 | Datenmodell | `MediaDetailsView.SelectedEpisodeId` + berechnete Header-Eigenschaften (`SelectedEpisode`, `HeaderEpisodeId`, `HeaderPictureId`, `HeaderTitle`, `HeaderMeta`, `HeaderPlot`) | Offen | — |
| 5 | Datenmodell | `MediaDetailsState.HeaderImageBytes`, `IsHeaderImageLoading`, `HasHeaderImage` | Offen | — |
| 6 | MVU | `LoadMediaDetailsHeaderEffect` in `AppEffect` anlegen | Offen | — |
| 7 | MVU | `MediaDetailsHeaderImageLoadedMessage` in `AppMessage` anlegen | Offen | — |
| 8 | Logik | `MediaDetailsService`: `MapSeasons` (Bannerkette/Jahr), `MapShow`/`LoadEpisodeAsync`/`MapOfflineShow` (`SelectedEpisodeId`) | Offen | — |
| 9 | Logik | `AppUpdate`: `Update`-Switch + `UpdateMediaDetailsLoaded` (Header-Effekt nach Details) | Offen | — |
| 10 | Logik | `AppUpdate`: `UpdateSeasonSelected` (erste Episode wählen, Header-Effekt bei Identitätswechsel) | Offen | — |
| 11 | Logik | `AppUpdate`: `UpdateMediaDetailsHeaderImageLoaded` + `WithDetailsHeaderLoad` | Offen | — |
| 12 | Effekt | `ShellAppEffectHandler`: Switch-Fall + `LoadMediaDetailsHeaderAsync` | Offen | — |
| 13 | UI | `MediaDetailsPage.xaml`: vollbreiter Header-`Grid` (Bild, Gradienten, Trennlinie, Inhalt, Favorit oben rechts, Aktionen unten rechts), Wurzel-Padding verschieben | Offen | — |
| 14 | UI | `MediaDetailsPage.xaml.cs`: `UpdateHeader` (Texte + `ImageSource.FromStream`), `OnSizeChanged` (`Math.Clamp(Width*0.52,360,620)`) | Offen | — |
| 15 | Tests | `VideoPlayerApiClientTests_Endpoints`: Endpunkt-Fall + Bearer-Test für `GetEpisodeBackgroundImageAsync` | Offen | — |
| 16 | Tests | `AppUpdateTests_DetailsHeader` anlegen (Ladeeffekt je Quelle, Offline, Identitäts-Abgleich, Fehlschlag, Staffelwechsel, Header-Texte) | Offen | — |
| 17 | Tests | `MediaDetailsServiceTests`: Mapping-Erweiterungen (Staffel-Bannerkette, Jahre, `SelectedEpisodeId`, Offline ohne Bild) | Offen | — |
| 18 | Tests | `MediaDetailsPageContractTests` anlegen (XAML-/Code-Behind-/Handler-Verträge) | Offen | — |
