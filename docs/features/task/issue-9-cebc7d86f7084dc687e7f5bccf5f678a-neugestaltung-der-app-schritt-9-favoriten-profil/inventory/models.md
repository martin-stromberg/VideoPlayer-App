# Datenmodell — Bestandsaufnahme (Schritt 9)

## `AppModel` (`VideoPlayer.Mvu/AppModel.cs`)

Unveränderliches Gesamtmodell (MVU). Relevante vorhandene Member:

- `CurrentPage` (`AppPage`), `ConnectionState` (`ConnectionState`),
  `IsOffline` (abgeleitet aus `ConnectionState.Offline`), `IsOnline`
- `LoginUsername` (`string?`) — bekannter Benutzername zur
  Vorbefüllung der Anmeldemaske
- `Favorites`/`ContinueWatching`/`RecentEntries`
  (`IReadOnlyList<HomeMediaItem>`) — Startseiten-Listen aus dem Cache;
  `HasFavorites`/`HasHomeContent` abgeleitet
- `SourcesOverview` (`SourcesOverviewState`), `SourceBrowser`
  (`SourceBrowserState`), `MediaDetails` (`MediaDetailsState`)

**Fehlt:** Zustand der Favoriten-Seite (Liste mit Favoriten-Eintrags-Id,
Lade-/Fehlerzustand, Entfernen-Markierung) und der Profil-Seite
(Benutzername, E-Mail, Profilname).

## `HomeMediaItem` (`VideoPlayer.Mvu/HomeMediaItem.cs`)

Anzeigeeintrag der Startseiten-Listen: `Id`, `MediaType`
(normalisierbar über `MediaTypeNames`), `Title`, `PosterPictureId`,
`Progress`, `ShowId`/`SeasonId`/`EpisodeId` (Serienkontext-Hints),
`HasPoster`, `ShowProgress`.

**Fehlt:** `FavoriteId` (Id des serverseitigen Favoriten-Eintrags,
nötig für `api/favorites/remove`) und eine Typ-Anzeigezeile — die
Favoriten-Seite braucht daher einen eigenen Item-Typ.

## `FavoriteEntry` (`VideoPlayer.ApiClient/Models/FavoriteEntry.cs`)

`Id` (Favoriten-Eintrags-Id — Ziel von `api/favorites/remove`),
`Entry` (`MediaEntry?`, polymorph über `$type`), `CreatedAt`.

## `MediaEntry` und Subtypen (`VideoPlayer.ApiClient/Models/MediaEntry.cs`)

`Movie`, `MovieCollection`, `TvShow`, `TvShowSeason` (mit `Show`),
`TvShowEpisode` (mit `Season.Show`). Gemeinsam: `Id`, `Name`,
`PosterPictureId`, `BannerPictureId`, `FanartPictureId`, `IsFavorite`.
Serienkontext-Hints werden in `HomeListService.MediaHints` aus dem
eingebetteten Objektgraph gelesen (Episode → `Season.Show.Id`,
`Season.Id`, `Id`; Season → `Show.Id`, `Id`).

## `CachedFavoritesEntry` (`VideoPlayer.Storage/Entities/CachedFavoritesEntry.cs`)

Tabelle `cached_favorites` (nicht backup-pflichtig): `Key`
(`{typ}:{id}`, CLR-Typname kleingeschrieben), `VideoId`, `VideoType`,
`Json` (serialisierter `FavoriteEntry` inkl. `Id`), `UpdatedAtUtc`.
Geschrieben durch `ServerSyncService.SyncFavoritesAsync`
(DeleteAll + Insert des kompletten Serverstands).

## `AppProfile` (`VideoPlayer.Storage/AppProfile.cs`)

`Name` (kleingeschrieben, `default` ohne Präfix), `PrefixKey`,
`FromEnvironment`/`Create`. In DI als Singleton
(`AppProfile.FromEnvironment()`).

## `AppSecretsService` (`VideoPlayer.Storage/AppSecretsService.cs`)

Profil-präfixierte Ablage: `GetUsername`, `GetPasswordAsync`,
`HasCredentialsAsync`, `SetCredentialsAsync`, `ClearCredentialsAsync`
(entfernt Benutzername + Passwort + Token), `GetAuthTokenAsync`,
`SetAuthTokenAsync`, `ClearAuthTokenAsync`.

## `AuthToken` (`VideoPlayer.ApiClient/Models/AuthToken.cs`)

`Token` (JWT-Rohtext), `Expires`. Die JWT-Payload trägt (serverseitig
belegt): `sub`, `nameid`, `email`, `unique_name`, `IsAdmin`, `exp`,
`iss`, `aud`.

## Bestehende Seiten-Zustandsmuster

- `SourcesOverviewState`: `Initial`-Singleton, `IsLoading`,
  `Attempted`, `Error`, `Sources`, abgeleitete `HasSources`/`HasError`/
  `ShowEmpty`.
- `MediaDetailsState`: `Empty`-Singleton, `MediaType`/`MediaId`,
  `IsLoading`, `Attempted`, `Error`, `IsFavoriteUpdating`,
  `FavoriteError`, `Details`.
