# Übersetzte Anforderung: Playlist-Anbindung und -Anzeige (Übersicht + Detailansicht)

## Fachliche Zusammenfassung

Die Server-Playlist-API (`api/playlists`) wird an die MAUI-App angebunden und über das Hauptmenü erreichbar gemacht. Ein neuer Menüeintrag „Playlists" führt zu einer Übersichtsseite, die eigene Playlists (`GET api/playlists`) und öffentliche Playlists (`GET api/playlists/public`) mit Coverbildern auflistet. Aus der Übersicht lässt sich eine Playlist öffnen; die Detailansicht zeigt deren Einträge paginiert in der serverseitigen Reihenfolge (`GET api/playlists/{id}/entries/paged`). Playlist-Cover (`GET api/playlists/{id}/cover`) werden in Übersicht und Detailansicht angezeigt, sofern der Server eines liefert. Die Funktion ist ein reines Online-Feature — bei fehlender Verbindung erscheint eine verständliche Fehleranzeige statt eines Absturzes oder einer Cache-Anzeige.

## Betroffene Klassen und Komponenten

- **API-Client (`VideoPlayer.ApiClient`)**
  - `VideoPlayerApiClient` — neue Methoden `GetPlaylistsAsync`, `GetPublicPlaylistsAsync`, `GetPlaylistAsync`, `GetPlaylistEntriesPagedAsync`, `GetPlaylistCoverAsync`.
  - Neue DTOs unter `VideoPlayer.ApiClient/Models/` (Felder wortgenau aus dem Referenz-Client `VideoWebPlayer.Client/Models/`): `Playlist`, `PlaylistEntry`, `PlaylistEntriesPagedResult`, `PlaylistSortModeValues` (String-Konstanten; `GenreOption` bereits vorhanden in `MediaSource.cs`, wird wiederverwendet).
- **MVU (`VideoPlayer.Mvu`)**
  - `AppPage` — neuer Enum-Wert `Playlists` (Menüseite; `EnumCoverageTests`-Pflicht beachten).
  - `AppRoutes` — Route `playlists` + `MenuPages`-Eintrag (Contract-Tests `AppRoutesTests`/`VideoPlayerMauiContractTests` sind anzupassen).
  - `AppModel` — neue Zustandsfelder `PlaylistsPage` (Übersicht) und `PlaylistDetails` (Detailansicht).
  - `AppMessage` — neue Message-Records (Öffnen, Laden, Auswahl, Paging, Retry; deutsch, XML-Doku).
  - `AppEffect` — neue Effect-Records (`LoadPlaylistsEffect`, `NavigateToPlaylistDetailsEffect`, `LoadPlaylistEntriesEffect`).
  - `AppUpdate` — neue statische Update-Methoden im zentralen Switch.
  - Neue Zustands-Records `PlaylistsPageState`, `PlaylistDetailsState`, `PlaylistListItem`, `PlaylistEntryItem` sowie `PlaylistService` (Serverzugriff über `AuthSessionService.Client`, Muster `SourceBrowserService`/`FavoritesService`).
  - `DevApiModelSnapshot` (nur `DEBUG || DEV_API`) — Playlist-Kurzfelder für die E2E-Verifikation (Muster: `FavoritesCount`).
- **MAUI-UI (`VideoPlayer.Maui`)**
  - `AppShell.xaml` — neues `FlyoutItem` „Playlists" (Position im Menü nach Projekt-Menüordnung; siehe Plan).
  - `AppShell.xaml.cs` — `Routing.RegisterRoute("playlistdetails", typeof(Pages.PlaylistDetailsPage))` für die geschobene Detailroute.
  - Neue Seiten `Pages/PlaylistsPage.xaml(.cs)` (Menüseite) und `Pages/PlaylistDetailsPage.xaml(.cs)` (geschobene Route mit `?id=`-Query-Property, Muster `SourceOverviewPage`).
  - `MauiProgram.cs` — DI-Registrierung der neuen Seiten (transient) und des `PlaylistService` (Singleton).
  - `Services/ShellAppEffectHandler.cs` — Effekt-Handler für die neuen Effekte.
  - `Services/PosterImageService.cs` — Cover-Auflösung `api/playlists/{id}/cover` (Cache-Konvention wie `GetImageSourceAsync`/`GetSourceIconSourceAsync`).
  - `Controls/` — Cover-Anzeige (Muster `CachedPosterImage`; eigenes Bindable-Property für die Playlist-Id).
  - `CachedPosterImage` — unverändert wiederverwendet für Eintrags-Poster (`ResolvedPictureId` → `api/pictures/{id}`).
- **E2E (`VideoPlayer.E2E.Tests`)**
  - `Support/MockVideoServer.cs` — Playlist-Endpunkte (`/api/playlists`, `/api/playlists/public`, `/api/playlists/{id}`, `/api/playlists/{id}/entries`, `/api/playlists/{id}/entries/paged`, `/api/playlists/{id}/cover`) ergänzen; `IsListEndpoint` erweitern.
  - Neue Testklasse `Tests/PlaylistsTests.cs`.
  - `Support/DevApiModels.cs` — ggf. neue Snapshot-Felder.
- **Tests (`VideoWebPlayer.Maui.Tests`)**
  - `AppRoutesTests`, `VideoPlayerMauiContractTests` — Menüeintrag „Playlists" eingeplant.
  - `AppUpdateTests_Playlists` (neu) — Update-Logik.
  - `VideoPlayerApiClientTests_*` — neue Endpunkt-Tests.
  - `PageLifecycleContractTests` — neue Seiten nachziehen.
  - `EnumCoverageTests` — `AppPage.Playlists`-Abdeckung.

## Implementierungsansatz

- MVU-Konvention wie bei `SourceBrowserState`/`FavoritesPageState`: Übersicht lädt beim Seiten-`OnAppearing` via `PlaylistsPageOpenedMessage` → `LoadPlaylistsEffect` → `PlaylistsPageLoadedMessage`; die Detailansicht wird via `PlaylistSelectedMessage` → `NavigateToPlaylistDetailsEffect` (Shell-Route `playlistdetails?id=…`) geöffnet und lädt Einträge seitenweise (`LoadPlaylistEntriesEffect`, `RemainingItemsThreshold`-Nachladen, Muster `SourceOverviewPage`).
- Fehlerzustände folgen dem etablierten Muster: `Error`-Feld im State, `ErrorPanel` mit „Erneut versuchen"-Button; fehlende Serververbindung oder `ApiException` ergeben eine verständliche Fehleranzeige.
- Playlist-Einträge werden in der Server-Reihenfolge dargestellt — der Client sortiert nicht um (`entries/paged` liefert bereits sortiert).
- Auswahl eines Eintrags darf optional zur Medien-Detailseite navigieren (`NavigateToMediaDetailsEffect` mit `MediaType`/`MediaId`, Normalisierung über `MediaTypeNames.Normalize`) — nicht gefordert, aber zulässig; die Wiedergabe aus der Playlist bleibt Folgeschritt 5.
- Verifikation der Server-Endpunkte gegen `http://localhost:83/` (Existenz = HTTP 401 ohne Credentials, `api/health` = 200) sowie gegen das Server-Repo `martin-stromberg/VideoPlayer`; fachliche Verifikation über `MockVideoServer`-Nachbildung.

## Konfiguration

Keine neue Konfiguration. Playlists sind fest als Online-Feature ausgelegt (Kundenfestlegung: kein Offline-Cache der Playlist-Listen).

## Offene Fragen

Keine — Cover-Verwaltung (Upload/Regenerieren/Löschen) ist vom Kunden verbindlich aus dem Scope genommen; Wiedergabe aus der Playlist ist Folgeschritt 5; Genre-Filter für Playlists ist optional in der API (`?genreId`), aber nicht Teil der Anforderung.
