# Logik — Bestandsaufnahme (Schritt 9)

## MVU-Kern

### `AppUpdate` (`VideoPlayer.Mvu/AppUpdate.cs`)

Reine Update-Funktion; `switch` über alle `AppMessage`-Typen. Relevante
vorhandene Übergänge:

- `SourcesPageOpenedMessage` → bei `IsOffline` kein Lauf (Hinweis über
  Binding), sonst `IsLoading` + `LoadSourcesEffect`
- `MediaItemSelectedMessage` → `NavigateToMediaDetailsEffect`
  (mediaType/id/title + Show/Season/Episode-Hints)
- `FavoriteToggleRequestedMessage`/`FavoriteToggleCompletedMessage` —
  Detailseiten-Stern; Erfolg aktualisiert `Details.IsFavorite`, der
  Effekt-Handler dispatcht danach `CachedListsChangedMessage`
- `CachedListsChangedMessage` → `LoadHomeListsEffect`
- `UpdateSessionExpired` → `NeedsLogin` + `LoginError` +
  `NavigateToPageEffect(Login)` + `DisconnectServerEventsEffect`
  (Muster für den Abmelde-Übergang)
- `NavigateToMessage`/`NavigatedMessage` — zustandsgesteuerte
  Navigation; Menüseiten sind bei `NeedsLogin`/`NeedsServerSetup`
  gesperrt

### `AppMessage`/`AppEffect`/`AppPage`/`AppRoutes`

`AppPage.Favorites` (Route `favorites`) und `AppPage.Profile`
(Route `profile`) existieren und sind Menüseiten. Keine
Messages/Effekte für Favoriten-Liste, Profil oder Abmeldung vorhanden.

### `FavoritesService` (`VideoPlayer.Mvu/FavoritesService.cs`)

`ToggleAsync(mediaType, mediaId, title)` → `client.ToggleFavoriteAsync`
+ `syncService.SyncFavoritesAsync`; `CreateEntry` (internal static)
baut den typkorrekten `MediaEntry`. **Fehlt:** Entfernen-Methode.

### `ServerSyncService` (`VideoPlayer.Mvu/ServerSyncService.cs`)

`SyncFavoritesAsync` ersetzt `cached_favorites` vollständig durch den
Serverstand (`GetFavoritesAsync` → DeleteAll + Insert je Eintrag mit
serialisiertem `FavoriteEntry`-JSON). `TrySyncAsync` fängt Fehler
(`false` bei Fehlschlag/ohne Client).

### `HomeListService` (`VideoPlayer.Mvu/HomeListService.cs`)

`LoadHomeListsAsync` liest `cached_favorites` (`ReadFavoritesAsync` —
deserialisiert `FavoriteEntry`-JSON, sortiert nach `UpdatedAtUtc`
absteigend) und mappt auf `HomeMediaItem` (`MapEntry` —
**Favoriten-Eintrags-Id `FavoriteEntry.Id` geht dabei verloren**).
`MediaHints`/`MediaTypeOf` sind privat.

### `AuthSessionService` (`VideoPlayer.Mvu/AuthSessionService.cs`)

`Client`/`ClientBaseAddress` (`private set`), `TryStoredLoginAsync`,
`LoginAsync`, `SessionExpired`-Event, stille Neuanmeldung.
**Fehlt:** Sitzungsende (Client freigeben + Geheimnisse löschen).

### `MediaTypeNames` (`VideoPlayer.Mvu/MediaTypeNames.cs`)

`Normalize` bildet Server- und CLR-Konventionen auf die CLR-Konvention
ab. Typ-Anzeigetext existiert nur in `MediaDetailsView.MediaTypeText`
(Film/Filmkollektion/Serie/Staffel/Episode/Medium).

## App-Rand (`VideoPlayer.Maui`)

### `ShellAppEffectHandler`

Führt alle Effekte aus; löst Dienste über `IServiceProvider`
(`GetRequiredService`), dispatcht Ergebnis-Messages über `MvuHost` auf
dem MainThread. `ToggleFavoriteAsync` (Handler-Methode) zeigt das
Muster „Mutation + Sync + `CachedListsChangedMessage`".
`NavigateAsync(AppPage.Login)` nutzt die Shell-Route `login`.

### `AppShell`/`App.xaml.cs`

Flyout-Menü mit sechs Einträgen; Routen `serversetup`, `login`,
`mediadetails`, `sourceoverview` registriert. `Navigated` →
`NavigatedMessage`.

### Seiten

- `Pages/FavoritesPage.xaml(.cs)` — Platzhalter (parameterloser
  Konstruktor, kein MVU-Bezug).
- `Pages/ProfilePage.xaml(.cs)` — Platzhalter (ebenso).
- `Pages/SourcesPage.xaml(.cs)` — Muster: `MvuHost` im Konstruktor,
  `BindingContext = mvuHost`, `OnAppearing` dispatcht
  „PageOpened"-Message, `CollectionView` mit Karten-Template,
  Offline-Banner, Fehlerpanel + „Erneut laden".
- `Pages/HomePage.xaml(.cs)` — `MediaCarousel`-Listen,
  `MediaItemSelectedMessage` bei Auswahl.
- `Pages/LoginPage.xaml(.cs)` — Anmeldemaske (Ziel der Abmeldung).
- `Controls/CachedPosterImage` (`PictureId` → `PosterImageService`)
  und `Controls/CachedSourceIconImage` — wiederverwendbar.

### `PosterImageService`

Lädt `api/pictures/{id}` über den angemeldeten Client mit lokaler
Vorschaubild-Ablage (nicht backup-pflichtig).

## Server-API-Vertrag (Referenz Server-Repository)

- `GET api/favorites` → `DtoFavoriteEntry[]` (`Id`, `Entry`, `CreatedAt`)
- `POST api/favorites/remove` — Body `FavoriteEntry`
  (`{ id, userId }`); serverseitig matcht `RemoveFavoriteAsync` auf die
  Favoriten-Id **oder** die medienbezogenen FK-Ids. Der Bestandsclient
  sendet `{ Id = favoriteId, UserId = "anonymous" }`.
- `POST api/favorites/toggle` — Body `DtoMediaEntry` → `bool`.
- `api/auth`: nur `login` (JWT) und `impersonate` (adminintern);
  **kein** Benutzerinfo- oder Logout-Endpunkt. JWT-Claims:
  `sub`/`nameid` = User-Id, `email` = E-Mail, `unique_name` =
  UserName, `IsAdmin`.
- Web-Referenz `FavoritesList.razor`: Liste mit Bild, Titel,
  Kontextaktion „Entfernen" → `RemoveFavoriteAsync(favorite.Id)`;
  Staffel-Titel „{Show.Name} - {Season.Name}"; Links auf Film/
  Kollektion/Serie mit Season-/Episode-Vorwahl.
