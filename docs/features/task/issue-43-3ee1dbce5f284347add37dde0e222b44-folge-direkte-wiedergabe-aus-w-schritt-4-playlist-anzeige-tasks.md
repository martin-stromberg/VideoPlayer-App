# Tasks: Playlist-Anbindung und -Anzeige (Übersicht + Detailansicht)

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | API-Client | `Playlist`-DTO (`VideoPlayer.ApiClient/Models/Playlist.cs`) anlegen | Erledigt | `VideoPlayerApiClientTests_Playlists.GetPlaylistsAsync_DeserializesPlaylists` |
| 2 | API-Client | `PlaylistEntry`-DTO (`PlaylistEntry.cs`) anlegen | Erledigt | `VideoPlayerApiClientTests_Playlists.GetPlaylistEntriesPagedAsync_UsesPagedUrlAndDeserializes` |
| 3 | API-Client | `PlaylistEntriesPagedResult`-DTO anlegen | Erledigt | `VideoPlayerApiClientTests_Playlists.GetPlaylistEntriesPagedAsync_UsesPagedUrlAndDeserializes` |
| 4 | API-Client | `PlaylistSortModeValues`-Konstantenklasse anlegen | Erledigt | `AppUpdateTests_Playlists` (SortModeText-Bindung), `VideoPlayerApiClientTests_Playlists` (SortMode-Mapping) |
| 5 | API-Client | `GetPlaylistsAsync`/`GetPublicPlaylistsAsync`/`GetPlaylistAsync`/`GetPlaylistEntriesPagedAsync`/`GetPlaylistCoverAsync` in `VideoPlayerApiClient` | Erledigt | `VideoPlayerApiClientTests_Playlists` (alle Methoden inkl. 404- und Guard-Pfade) |
| 6 | MVU | `AppPage.Playlists` + `AppRoutes`-Eintrag + `MenuPages`-Position | Erledigt | `AppRoutesTests.MenuPages_ContainsAllMenuEntriesInOrder`, `ToRoute_RoundTripsAllMenuPages` |
| 7 | MVU | `PlaylistListItem`/`PlaylistOverviewItem`/`PlaylistEntryItem`-Records | Erledigt | `AppUpdateTests_Playlists`, `PlaylistServiceTests` (Mapping) |
| 8 | MVU | `PlaylistsPageState`/`PlaylistDetailsState`-Records | Erledigt | `AppUpdateTests_Playlists` |
| 9 | MVU | `AppModel.PlaylistsPage` + `AppModel.PlaylistDetails` | Erledigt | `AppUpdateTests_Playlists` |
| 10 | MVU | `AppMessage`-Records (9 Messages) | Erledigt | `AppUpdateTests_Playlists` (je Message ein Übergangstest) |
| 11 | MVU | `AppEffect`-Records (4 Effekte) | Erledigt | `AppUpdateTests_Playlists` (Effekt-Emission geprüft) |
| 12 | MVU | `PlaylistService` + `PlaylistEntriesPage`-Ergebnisrecord | Erledigt | `PlaylistServiceTests` |
| 13 | MVU | `AppUpdate`-Zweige + Update-Methoden (9) | Erledigt | `AppUpdateTests_Playlists` |
| 14 | UI | `PlaylistsPage.xaml(.cs)` (Menüseite, flache Liste mit Sektionsheadern, Cover, Fehlerpanel, AutomationIds) | Erledigt | `PageLifecycleContractTests.PlaylistsPage_*`, `PlaylistsTests.PlaylistsMenuItem_ShowsOwnAndPublicPlaylists` (E2E) |
| 15 | UI | `PlaylistDetailsPage.xaml(.cs)` (Kopfdaten + Eintragsliste + Paging + Fehler, AutomationIds) | Erledigt | `PageLifecycleContractTests.PlaylistDetailsPage_*`, `PlaylistsTests.SelectingPlaylist_ShowsEntriesInServerOrder` (E2E) |
| 16 | UI | `CachedPlaylistCoverImage`-Control | Erledigt | `PlaylistsTests.PlaylistWithCover_RequestsCoverEndpoint` (E2E) |
| 17 | UI | `PosterImageService.GetPlaylistCoverSourceAsync` | Erledigt | `PlaylistsTests.PlaylistWithCover_RequestsCoverEndpoint` (E2E) |
| 18 | UI | `AppShell.xaml`-`FlyoutItem` + `AppShell.xaml.cs`-Route `playlistdetails` | Erledigt | `VideoPlayerMauiContractTests.AppShell_ContainsAllMenuEntries`, `PlaylistsTests.*` (Navigation E2E) |
| 19 | UI | `MauiProgram`-Registrierungen (`PlaylistService`, beide Seiten) | Erledigt | `PlaylistsTests.*` (E2E — Seiten/DI ohne Fehler erreichbar) |
| 20 | UI | `ShellAppEffectHandler`-Zweige (LoadPlaylists, NavigateToPlaylistDetails, LoadPlaylistDetails, LoadPlaylistEntries) | Erledigt | `PlaylistsTests.*` (E2E — Datenfluss Ende-zu-Ende) |
| 21 | E2E | `MockVideoServer`: Playlist-Endpunkte + Opt-in-Daten + `PlaylistsUnavailable` | Erledigt | `PlaylistsTests.*` |
| 22 | E2E | `DevApiModelSnapshot`/`DevApiModel` Playlist-Felder | Erledigt | `PlaylistsTests.*` (`PlaylistsPageItemCount`, `PlaylistDetails*` im Snapshot) |
| 23 | E2E | `Tests/PlaylistsTests.cs` (Übersicht, Detail, Cover-Abruf, Fehleranzeige) | Erledigt | 4/4 E2E-Tests bestanden |
| 24 | Tests | `VideoPlayerApiClientTests_Playlists` | Erledigt | 8/8 bestanden |
| 25 | Tests | `AppUpdateTests_Playlists` | Erledigt | 15/15 bestanden |
| 26 | Tests | `PlaylistServiceTests` | Erledigt | 4/4 bestanden |
| 27 | Tests | `AppRoutesTests`/`VideoPlayerMauiContractTests`/`PageLifecycleContractTests`/`SourcesContractTests` anpassen | Erledigt | Bestanden (Gesamtsuite 1135/1135) |
| 28 | Verifikation | Existenz-Verifikation `http://localhost:83/` dokumentieren | Erledigt | `inventory.md` + `test-results.md` (401-Nachweis; authentifizierter Abruf ohne Credentials nicht möglich — Mock-Server-Durchlauf als Nachweis) |
