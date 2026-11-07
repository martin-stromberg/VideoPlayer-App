# Testergebnisse

**Status:** Keine Fehler

## Ausgeführte Verifikationen

| Prüfung | Ergebnis |
|---------|----------|
| Build `VideoPlayer.Mvu` (net10.0) | erfolgreich, 0 Warnungen, 0 Fehler |
| Build `VideoPlayer.Maui` (`net10.0-windows10.0.19041.0`, XAML-SourceGen) | erfolgreich, 0 Warnungen, 0 Fehler |
| Gesamtlösung `VideoPlayer.App.sln` (Release, alle Ziele inkl. iOS/MacCatalyst/Android) | erfolgreich, 0 Fehler, 281 Warnungen — alle im Altbestand `VideoWebPlayer.Maui` (bereits vor diesem Schritt vorhanden: `CS0618`/`CS8602`/XA4301 u. ä.), keine neuen Warnungen im geänderten Code |
| `dotnet test VideoWebPlayer.Maui.Tests` (Release, mit Coverage-Sammlung) | 425 Tests: 425 bestanden, 0 fehlgeschlagen, 0 übersprungen |
| Code-Coverage (`--collect:"XPlat Code Coverage"`) | `VideoPlayer.Storage` 99,0 %, `VideoPlayer.Mvu` 93,1 %, `VideoPlayer.ApiClient` 87,8 % Zeilen (`VideoWebPlayer.Client` 7,3 %, Altbestand) |
| Windows-Smoke-Run / UI-E2E | nicht verfügbar — das Repository enthält kein UI-Automatisierungs-Framework und das Testprojekt kann `VideoPlayer.Maui` nicht referenzieren (siehe `inventory/tests.md`); Ersatz gemäß Plan: vollständige Message-Sequenz-Tests auf MVU-Ebene + Contract-Tests der Oberflächen-Verdrahtung |

Befehl (Arbeitsverzeichnis: Repository-Wurzel, Exit-Code 0):

```
dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj -c Release --collect:"XPlat Code Coverage" --logger "console;verbosity=normal"
```

Protokoll: `final-dotnet-test.log` (Coverage-Anlage:
`coverage.cobertura.xml` unter `VideoWebPlayer.Maui.Tests/TestResults/`).

## Baseline vs. Ergebnis

- Baseline (vor Implementierung, `inventory/test-results/baseline-dotnet-test.log`):
  350 Tests, 350 bestanden.
- Nach Implementierung: 425 Tests, 425 bestanden (75 neue Tests).

## Neue Tests für die Akzeptanzkriterien

| Akzeptanzkriterium | Nachweis |
|--------------------|----------|
| Quellenliste entspricht der Server-API | `SourceBrowserServiceTests.LoadSources_MapsServerList_WithIcons`, `SourcesLoaded_SetsList_AndEmptyIsDistinguishable`, `SourcesLoaded_Error_SetsErrorState` |
| Offline zeigt Hinweis statt leerer/fehlerhafter Liste | `SourcesPageOpened_Offline_NoRequestAndStateReset`, `SourcesPage_ShowsSources_WithIcons_AndOfflineHint` |
| Genre-Filter | `GenreSelected_ReloadsFromFirstPage_WithGenreFilter`, `LoadGenres_PrependsAllEntry_AndMapsServerGenres` |
| Suche | `SearchSubmitted_ReloadsFromFirstPage_WithSearchText`, `SearchSubmitted_SameText_NoReload`, `LoadItems_FirstPage_MapsSummaries_AndRequestsQuery` |
| Seitenweises Nachladen | `NextPageRequested_*`, `ItemsLoaded_Append_ConcatenatesAndAdvancesPage`, `ItemsLoaded_StaleAppend_IsDiscarded`, `ItemsLoaded_AppendError_KeepsExistingItems`, `Retry_*`, `LoadItems_FullPage_ReportsHasMore` |
| Serien-Details mit Staffel-/Episodenauswahl | `Load_TvShow_MapsSeasonsAndEpisodes`, `Load_Episode_WithShowHint_OpensShowContextWithPreselection`, `Load_Episode_WithoutHint_ResolvesShowViaEmbeddedSeason`, `Load_Season_*`, `SeasonSelected_*` |
| Filmkollektion mit enthaltenen Filmen | `Load_MovieCollection_MapsContainedMovies`, `Load_MovieCollection_NotFound_FallsBackToMovie` |
| Film-/Episoden-Basisdetails | `Load_Movie_MapsBasicDetails`, `Load_Episode_WithoutShow_ShowsBasicDetails` |
| Favoriten umschalten/entfernen, Server führend, Cache-Kopie | `Toggle_SendsEntry_AndRefreshesCachedFavorites`, `Toggle_RemovedFavorite_EmptiesCacheCopy`, `Toggle_WithoutClient_ReturnsNull_NoRequests`, `FavoriteToggleRequested/Completed_*` |
| Oberflächen-Verdrahtung (XAML/Route/DI/Effekte) | `SourcesContractTests` (9 Tests) |

## Einschränkungen

- UI-End-to-End-Automatisierung ist im Repository nicht vorhanden; die
  Contract-Tests prüfen die Verdrahtung statisch und ersetzen keine
  Geräteprüfung.
