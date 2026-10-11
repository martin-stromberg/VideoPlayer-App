# Abnahmeprüfung – Entwicklungsschritt 4

## Ergebnis

**Status:** Anforderung vollständig erfüllt

## Abweichungen

- [ ] Keine.

## Hinweise

**AK 1 – Menüeintrag „Playlists":** `VideoPlayer.Maui/AppShell.xaml` Z. 56–61 enthält `FlyoutItem Title="Playlists" Route="playlists"` mit Icon zwischen „Favoriten" und „Downloads"; `VideoPlayer.Mvu/AppPage.cs` Z. 19 (`AppPage.Playlists`), `AppRoutes.cs` Z. 15 (Route) und Z. 38 (`MenuPages`-Position). Gepinnt durch `AppRoutesTests.MenuPages_ContainsAllMenuEntriesInOrder` und `VideoPlayerMauiContractTests.AppShell_ContainsAllMenuEntries` (beide konsistent auf 6 Einträge angehoben).

**AK 2 – Eigene + öffentliche Playlists:** `PlaylistService.LoadPlaylistsAsync` (`VideoPlayer.Mvu/PlaylistService.cs` Z. 54–62) ruft beide Endpunkte ab (`api/playlists`, `api/playlists/public`); `PlaylistsPageState.BuildItems` (Z. 136–154) rendert beide Sektionen „Meine Playlists"/„Öffentliche Playlists" in einer flachen Liste. E2E `PlaylistsTests.PlaylistsMenuItem_ShowsOwnAndPublicPlaylists` verifiziert beide Sektionen und beide Server-Aufrufe.

**AK 3 – Paginierte Einträge in Server-Reihenfolge:** `GetPlaylistEntriesPagedAsync` (`VideoPlayerApiClient.cs`, Region Playlists) ruft `api/playlists/{id}/entries/paged?pageNumber=…&pageSize=…` 1-basiert ab; `PlaylistDetailsPage.xaml` Z. 99–100 nutzt `RemainingItemsThreshold="4"` → `PlaylistEntriesNextPageRequestedMessage` → Append in Empfangsreihenfolge (`AppUpdate.UpdatePlaylistEntriesLoaded`, `Entries.Concat` — keine Re-Sortierung; veraltete Anhänge-Antworten werden verworfen). E2E `SelectingPlaylist_ShowsEntriesInServerOrder` prüft die exakte Server-Reihenfolge (C, A, B, Episode).

**AK 4 – Cover-Anzeige:** `GetPlaylistCoverAsync` (`api/playlists/{id}/cover`, 404 → `null`); `CachedPlaylistCoverImage` + `PosterImageService.GetPlaylistCoverSourceAsync` mit Titel-Fallback ohne Cover; Kopfbereich der Detailseite ebenfalls mit Cover. E2E `PlaylistWithCover_RequestsCoverEndpoint` prüft Abruf bei `CoverPictureId` und Nicht-Abruf ohne.

**AK 5 – Fehleranzeige statt Absturz:** Offline-Guard in `UpdatePlaylistsPageOpened` (`PlaylistsOfflineError`), Lade-Fehler → `PlaylistsLoadError` im Fehlerpanel mit „Erneut laden"-Button (`PlaylistsPage.xaml` Z. 22–35); Detailseite mit Fehlerpanel + Footer-Retry bei Folgeseiten (`PlaylistDetailsPage.xaml` Z. 68–81, 151–160). E2E `PlaylistsUnavailable_ShowsError_AndRetryLoads` prüft Fehleranzeige + erfolgreichen Neuversuch.

**DTO-Plausibilisierung:** `Playlist.cs`, `PlaylistEntry.cs`, `PlaylistEntriesPagedResult` und `PlaylistSortModeValues` stimmen feldweise mit den Server-Referenzen `DtoPlaylist`, `DtoPlaylistEntry`, `DtoPlaylistEntriesPagedResult` und `PlaylistSortModeValues` überein (per `gh api` gegen `martin-stromberg/VideoPlayer` geprüft). Endpunkt-Pfade decken sich mit `PlaylistsController.cs` (`[HttpGet]`, `[HttpGet("public")]`, `[HttpGet("{id}")]`, `[HttpGet("{id}/entries/paged")]`, `[HttpGet("{id}/cover")]`).

**Testnachweis:** Lokal ausgeführt — 1170/1170 Unit-Tests grün (`VideoWebPlayer.Maui.Tests`, inkl. 256 gefilterte Playlist-/Routen-/Contract-Tests). Die behauptete Zahl 1139 ist durch die Suite übererfüllt (höher, nicht niedriger). Die 4 E2E-Tests (`PlaylistsTests`) wurden code-seitig geprüft, nicht erneut ausgeführt (erfordern deployte Windows-App); Mock-Server-Endpunkte (`HandlePlaylistRequest`, `MockVideoServer.cs` Z. 616 ff.) bilden Listen, Detail, 1-basiertes Paging mit 400-Validierung, Cover und `PlaylistsUnavailable`-Fehlerflag korrekt ab.

**Regressionen/Scope:** Contract-Test-Änderungen sind konsistente Anpassungen (5→6 Menüeinträge, neue Lifecycle-Tests), keine Abschwächung; `IsTopmostSourceOverview`-Refactor zu `IsTopmostRouteWithId` verhaltensgleich. Keine Verwaltungs-Endpunkte (kein POST/PUT/DELETE im ApiClient) — Schritt-6-Scope sauber ausgespart; Wiedergabe aus Playlist korrekt nicht implementiert (Tap → Medien-Detailseite, zulässig).

**Offline-Verhalten:** Kein Cache der Playlist-Daten (`PlaylistService` ohne Storage-Abhängigkeit); Cover-Bilder werden wie Poster/Icons in `PreviewsDirectory` zwischengespeichert — betrifft nur Bilddaten, die Eintragsliste ist offline ohnehin nicht erreichbar (Fehleranzeige). Kleinigkeit: War die Liste bereits geladen und die Verbindung fällt aus, bleibt der in-memory-Stand sichtbar und die Fehleranzeige wird zusätzlich eingeblendet (kein persistenter Cache).

**Einschränkung:** Die behauptete Verifikation der Endpunkte gegen `localhost:83` (401 ohne Credentials) ist im `changes.log` dokumentiert, aber nicht nachträglich reproduzierbar — die Endpunkt-Pfade sind jedoch gegen den Server-Quellcode verifiziert.
