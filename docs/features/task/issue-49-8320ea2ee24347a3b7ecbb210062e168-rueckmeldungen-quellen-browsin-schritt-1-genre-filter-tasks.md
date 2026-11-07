# Tasks: Genre-Filter im Quellen-Browsing (Schritt 1)

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Logik | `SourceBrowserState.ShowGenreFilter` (abgeleitete Eigenschaft, `Genres.Count > 1` — „Alle" ist der einzige synthetische Eintrag) mit XML-Doc anlegen | Erledigt | `AppUpdateTests_Sources.SourceGenresLoaded_*` (Hide/Show) |
| 2 | UI | `GenresCollection`: `IsVisible`-Binding auf `Model.SourceBrowser.ShowGenreFilter` + `AutomationId="GenresCollection"` setzen | Erledigt | `SourceBrowsingTests` (E2E: sichtbar bei Genres / verborgen bei leerer Liste) |
| 3 | UI | `ItemsCollection`: `AutomationId="ItemsCollection"` setzen | Erledigt | `SourceBrowsingTests` (E2E-Tree-Zugriff) |
| 4 | E2E-Infra | `DevApiModelSnapshot`: `SourceBrowserSourceId`, `SourceBrowserGenreNames`, `SourceBrowserSelectedGenreId`, `SourceBrowserShowGenreFilter`, `SourceBrowserItemTitles` ergänzen (inkl. `FromModel`-Befüllung) | Erledigt | `DevApiModelSnapshotTests.DevApiModelSnapshot_MapsSourceBrowser` |
| 5 | E2E-Infra | `DevApiModel` (E2E-DTO): Spiegelfelder für die neuen Snapshot-Properties ergänzen | Erledigt | `SourceBrowsingTests` (E2E nutzt die Felder) |
| 6 | E2E-Infra | `MockVideoServer`: `Sources`/`SourceGenres`/`SourceItems`-Konfiguration + Routen `api/SourceGenres/{id}` und `api/items?mediaSourceId=…` + `SourceItemRequests`-Aufzeichnung + `api/Sources`-Override | Erledigt | `SourceBrowsingTests` (E2E gegen Mock-Server) |
| 7 | Tests | `AppUpdateTests_Sources.SourceGenresLoaded_OnlyAllEntry_HidesGenreFilter` | Erledigt | Test selbst — bestanden |
| 8 | Tests | `AppUpdateTests_Sources.SourceGenresLoaded_ServerGenres_ShowsGenreFilter` | Erledigt | Test selbst — bestanden |
| 9 | Tests | `SourceBrowserServiceTests.LoadGenres_EmptyServerList_ReturnsOnlyAllEntry` | Erledigt | Test selbst — bestanden |
| 10 | Tests | `DevApiModelSnapshotTests`: Pin für die neuen `SourceBrowser*`-Felder | Erledigt | `DevApiModelSnapshot_MapsSourceBrowser` — bestanden |
| 11 | E2E-Tests | `SourceBrowsingTests.GenreBar_VisibleAndFilters_ResetsOnAll` | Erledigt | Test selbst — bestanden |
| 12 | E2E-Tests | `SourceBrowsingTests.GenreBar_Hidden_WhenServerHasNoGenres` | Erledigt | Test selbst — bestanden |
| 13 | Doku | `docs/help/quellen-browsing-details.md`: Sichtbarkeitsregel + Diagnosehinweis | Erledigt | — |
| 14 | Doku | `README.md` prüfen/aktualisieren (Genre-Filter-Erwähnung) | Erledigt | — |
| 15 | Doku | `docs/RELEASE_NOTES.md`: Eintrag (EN + DE) | Erledigt | — |
