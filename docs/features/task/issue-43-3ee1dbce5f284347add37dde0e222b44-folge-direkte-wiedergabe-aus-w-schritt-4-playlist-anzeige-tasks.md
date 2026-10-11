# Tasks: Playlist-Anbindung und -Anzeige (Übersicht + Detailansicht)

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | API-Client | `Playlist`-DTO (`VideoPlayer.ApiClient/Models/Playlist.cs`) anlegen | Offen | — |
| 2 | API-Client | `PlaylistEntry`-DTO (`PlaylistEntry.cs`) anlegen | Offen | — |
| 3 | API-Client | `PlaylistEntriesPagedResult`-DTO anlegen | Offen | — |
| 4 | API-Client | `PlaylistSortModeValues`-Konstantenklasse anlegen | Offen | — |
| 5 | API-Client | `GetPlaylistsAsync`/`GetPublicPlaylistsAsync`/`GetPlaylistAsync`/`GetPlaylistEntriesPagedAsync`/`GetPlaylistCoverAsync` in `VideoPlayerApiClient` | Offen | — |
| 6 | MVU | `AppPage.Playlists` + `AppRoutes`-Eintrag + `MenuPages`-Position | Offen | — |
| 7 | MVU | `PlaylistListItem`/`PlaylistOverviewItem`/`PlaylistEntryItem`-Records | Offen | — |
| 8 | MVU | `PlaylistsPageState`/`PlaylistDetailsState`-Records | Offen | — |
| 9 | MVU | `AppModel.PlaylistsPage` + `AppModel.PlaylistDetails` | Offen | — |
| 10 | MVU | `AppMessage`-Records (9 Messages) | Offen | — |
| 11 | MVU | `AppEffect`-Records (4 Effekte) | Offen | — |
| 12 | MVU | `PlaylistService` + `PlaylistEntriesPage`-Ergebnisrecord | Offen | — |
| 13 | MVU | `AppUpdate`-Zweige + Update-Methoden (9) | Offen | — |
| 14 | UI | `PlaylistsPage.xaml(.cs)` (Menüseite, flache Liste mit Sektionsheadern, Cover, Fehlerpanel, AutomationIds) | Offen | — |
| 15 | UI | `PlaylistDetailsPage.xaml(.cs)` (Kopfdaten + Eintragsliste + Paging + Fehler, AutomationIds) | Offen | — |
| 16 | UI | `CachedPlaylistCoverImage`-Control | Offen | — |
| 17 | UI | `PosterImageService.GetPlaylistCoverSourceAsync` | Offen | — |
| 18 | UI | `AppShell.xaml`-`FlyoutItem` + `AppShell.xaml.cs`-Route `playlistdetails` | Offen | — |
| 19 | UI | `MauiProgram`-Registrierungen (`PlaylistService`, beide Seiten) | Offen | — |
| 20 | UI | `ShellAppEffectHandler`-Zweige (LoadPlaylists, NavigateToPlaylistDetails, LoadPlaylistDetails, LoadPlaylistEntries) | Offen | — |
| 21 | E2E | `MockVideoServer`: Playlist-Endpunkte + Opt-in-Daten + `PlaylistsUnavailable` | Offen | — |
| 22 | E2E | `DevApiModelSnapshot`/`DevApiModel` Playlist-Felder | Offen | — |
| 23 | E2E | `Tests/PlaylistsTests.cs` (Übersicht, Detail, Cover-Abruf, Fehleranzeige) | Offen | — |
| 24 | Tests | `VideoPlayerApiClientTests_Playlists` | Offen | — |
| 25 | Tests | `AppUpdateTests_Playlists` | Offen | — |
| 26 | Tests | `PlaylistServiceTests` | Offen | — |
| 27 | Tests | `AppRoutesTests`/`VideoPlayerMauiContractTests`/`PageLifecycleContractTests` anpassen | Offen | — |
| 28 | Verifikation | Existenz-Verifikation `http://localhost:83/` dokumentieren | Offen | — |
