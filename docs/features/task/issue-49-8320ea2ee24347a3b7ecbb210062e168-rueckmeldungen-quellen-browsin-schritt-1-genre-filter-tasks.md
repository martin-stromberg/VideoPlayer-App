# Tasks: Genre-Filter im Quellen-Browsing (Schritt 1)

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Logik | `SourceBrowserState.ShowGenreFilter` (abgeleitete Eigenschaft, `Genres.Any(g => g.Id > 0)`) mit XML-Doc anlegen | Offen | — |
| 2 | UI | `GenresCollection`: `IsVisible`-Binding auf `Model.SourceBrowser.ShowGenreFilter` + `AutomationId="GenresCollection"` setzen | Offen | — |
| 3 | UI | `ItemsCollection`: `AutomationId="ItemsCollection"` setzen | Offen | — |
| 4 | E2E-Infra | `DevApiModelSnapshot`: `SourceBrowserSourceId`, `SourceBrowserGenreNames`, `SourceBrowserSelectedGenreId`, `SourceBrowserShowGenreFilter`, `SourceBrowserItemTitles` ergänzen (inkl. `FromModel`-Befüllung) | Offen | — |
| 5 | E2E-Infra | `DevApiModel` (E2E-DTO): Spiegelfelder für die neuen Snapshot-Properties ergänzen | Offen | — |
| 6 | E2E-Infra | `MockVideoServer`: `Sources`/`SourceGenres`/`SourceItems`-Konfiguration + Routen `api/SourceGenres/{id}` und `api/items?mediaSourceId=…` + `SourceItemRequests`-Aufzeichnung + `api/Sources`-Override | Offen | — |
| 7 | Tests | `AppUpdateTests_Sources.SourceGenresLoaded_OnlyAllEntry_HidesGenreFilter` | Offen | — |
| 8 | Tests | `AppUpdateTests_Sources.SourceGenresLoaded_ServerGenres_ShowsGenreFilter` | Offen | — |
| 9 | Tests | `SourceBrowserServiceTests.LoadGenres_EmptyServerList_ReturnsOnlyAllEntry` | Offen | — |
| 10 | Tests | `DevApiModelSnapshotTests`: Pin für die neuen `SourceBrowser*`-Felder | Offen | — |
| 11 | E2E-Tests | `SourceBrowsingTests.GenreBar_VisibleAndFilters_ResetsOnAll` | Offen | — |
| 12 | E2E-Tests | `SourceBrowsingTests.GenreBar_Hidden_WhenServerHasNoGenres` | Offen | — |
| 13 | Doku | `docs/help/quellen-browsing-details.md`: Sichtbarkeitsregel + Diagnosehinweis | Offen | — |
| 14 | Doku | `README.md` prüfen/aktualisieren (Genre-Filter-Erwähnung) | Offen | — |
| 15 | Doku | `docs/RELEASE_NOTES.md`: Eintrag (EN + DE) | Offen | — |
