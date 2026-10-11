# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan vollständig

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| Das Menü der App enthält einen Eintrag „Playlists". | `AppPage.Playlists`, `AppRoutes`/`MenuPages`, `FlyoutItem` in `AppShell.xaml`, Seite `PlaylistsPage` + DI (Umsetzungsschritte 2–3) | `AppRoutesTests`, `VideoPlayerMauiContractTests`, E2E Flyout-Navigation | Abgedeckt |
| Eigene (`GET api/playlists`) und öffentliche Playlists (`GET api/playlists/public`) werden aufgelistet. | `GetPlaylistsAsync`/`GetPublicPlaylistsAsync`, `PlaylistService.LoadPlaylistsAsync`, `PlaylistsPageState` mit Sektionszeilen (Schritte 1–3) | `VideoPlayerApiClientTests_Playlists`, `AppUpdateTests_Playlists`, `PlaylistServiceTests`, E2E Übersicht | Abgedeckt |
| Playlist lässt sich öffnen und zeigt Einträge paginiert in Server-Reihenfolge. | `playlistdetails`-Route, `PlaylistDetailsState` (1-basiertes Paging), `entries/paged`-Client-Methode, `RemainingItemsThreshold`-Nachladen (Schritte 1–3) | `VideoPlayerApiClientTests_Playlists`, `AppUpdateTests_Playlists` (Replace/Append/Stale), E2E Detailansicht | Abgedeckt |
| Playlist-Cover werden angezeigt, sofern der Server welche liefert. | `GetPlaylistCoverAsync` (404-tolerant), `PosterImageService.GetPlaylistCoverSourceAsync`, `CachedPlaylistCoverImage` in Übersicht + Detail (Schritt 3) | E2E Cover-Abruf via `WasPathRequested`; Titel-Fallback ohne Cover | Abgedeckt |
| Verifikation gegen `http://localhost:83/`; bei fehlender Verbindung verständliche Fehleranzeige. | Existenznachweis (401 ohne Credentials, dokumentiert in `inventory.md` + `test-results.md`), `MockVideoServer`-Nachbildung; Offline/`!= Connected` → `Error` ohne Effekt (Schritte 2, 4–5) | `AppUpdateTests_Playlists` (Offline-Fehler), E2E `PlaylistsUnavailable` → Fehlerpanel + Retry | Abgedeckt |

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Flyout „Playlists" → Übersicht mit eigenen + öffentlichen Playlists | `PlaylistsTests` (Happy Path) | Abgedeckt |
| Playlist öffnen → Einträge paginiert in Server-Reihenfolge | `PlaylistsTests` (Detailansicht) | Abgedeckt |
| Cover-Abruf `api/playlists/{id}/cover` + Titel-Fallback | `PlaylistsTests` (Cover-Nachweis über `RequestedPaths`) | Abgedeckt |
| Fehleranzeige bei nicht verfügbaren Playlist-Endpunkten + Retry | `PlaylistsTests` (`PlaylistsUnavailable`-Flag, kein Absturz) | Abgedeckt |

## Hinweise

- `entries` (unpaginiert) wird nur im `MockVideoServer` nachgebildet; die App nutzt ausschließlich `entries/paged` (einziger Endpunkt mit `HasNextPage`/Server-Reihenfolge-Garantie) — Absicht, keine Lücke.
- Paging-Basen unterscheiden sich (`api/items` 0-basiert vs. `entries/paged` 1-basiert) — im Plan als Risiko + Konvention (`NextPage` in Server-Konvention) verankert.
- `DevApiModelSnapshot`/`DevApiModel`-Erweiterung ist additiv; `DevApiContractTests` pinnt keine Feldliste (nur `#if`-Blöcke/Endpunkt-Mapping) — kein betroffener Bestandstest.
