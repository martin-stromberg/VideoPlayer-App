# Bestandsaufnahme: Playlist-Anbindung und -Anzeige (Übersicht + Detailansicht)

Analyse der bestehenden MVU-App (`VideoPlayer.Maui`/`VideoPlayer.Mvu`/`VideoPlayer.ApiClient`) sowie der E2E-Infrastruktur bezogen auf die Playlist-Anbindung (Entwicklungsschritt 4). Die Server-Playlist-API wurde gegen das Referenz-Repo `martin-stromberg/VideoPlayer` (Client `VideoWebPlayer.Client`, Controller `VideoWebPlayer/Controllers/PlaylistsController.cs`) und die laufende Instanz `http://localhost:83/` verifiziert.

## Zusammenfassung

- `VideoPlayerApiClient` kennt noch keine Playlist-Endpunkte; das Request-/Retry-/Reauth-Grundgerüst (`SendWithPolicyAsync`, `GetJsonAsync`, `PostJsonAsync`) ist vorhanden und wird von allen Listen-Endpunkten gleichermaßen genutzt.
- Die Server-DTOs (`DtoPlaylist`, `DtoPlaylistEntry`, `DtoPlaylistEntriesPagedResult`, `PlaylistSortModeValues`, `MediaTypeValues`) sind aus dem Referenz-Client übernommen worden und müssen als `VideoPlayer.ApiClient.Models`-Klassen nachgebaut werden; `GenreOption` existiert bereits (`MediaSource.cs:57`).
- Das MVU-Muster für Menüseiten (Favorites: `FavoritesPageState`, `FavoritesPageOpenedMessage`, `LoadFavoritesPageEffect`) und für geschobene Browsing-/Paging-Seiten (SourceBrowser: `SourceBrowserState`, `SourceBrowserOpenedMessage`, `LoadSourceItemsEffect` mit `Append`) ist vollständig ausgebaut und direkt wiederverwendbar.
- Der Menü-Einbauort ist klar: `AppPage`-Enum + `AppRoutes.Routes`/`MenuPages` + statisches `FlyoutItem` in `AppShell.xaml` + Seiten-DI in `MauiProgram.cs`; geschobene Detailrouten folgen dem Muster `Routing.RegisterRoute("sourceoverview", …)` + `NavigateToSourceOverviewEffect`.
- Bild-Caching existiert mit `PosterImageService` (`api/pictures/{id}`, `api/sourceicons/{id}`) und dem Control `CachedPosterImage`; für `api/playlists/{id}/cover` fehlt ein Pendant.
- `MockVideoServer` beantwortet bisher nur die Sync-/Auth-/Wiedergabe-Endpunkte; `/api/playlists*` liefert derzeit 404 (`IsListEndpoint` deckt es nicht ab).
- Verifikation gegen `http://localhost:83/`: `GET api/playlists`, `api/playlists/public`, `api/playlists/1`, `api/playlists/1/entries/paged`, `api/playlists/1/cover` → HTTP 401 (Endpunkt existiert, Auth erforderlich); `api/health` → 200. Keine Credentials verfügbar → fachliche Verifikation über `MockVideoServer`-Nachbildung.
- Test-Ausgangszustand: 1101/1101 Tests in `VideoWebPlayer.Maui.Tests` (Release) erfolgreich — keine bekannten Fehlschläge, siehe [Tests](inventory/tests.md).

## Details

- [Datenmodell](inventory/models.md) — DTO-Felder der Server-Playlist-API und bestehende Anzeige-Records
- [Logik](inventory/logic.md) — API-Client, MVU-Update, Effekt-Handler, Services, Mock-Server
- [Enums](inventory/enums.md) — `AppPage`, `PlaylistSortModeValues`/`MediaTypeValues` (Server-Stringkonstanten)
- [Interfaces](inventory/interfaces.md) — `IAppEffectHandler`, `IPlaylistApiClient` (Referenz), `DevApiModelSnapshot`
- [Tests](inventory/tests.md) — Test-Ausgangszustand, bestehende Testklassen und Contract-Pins
