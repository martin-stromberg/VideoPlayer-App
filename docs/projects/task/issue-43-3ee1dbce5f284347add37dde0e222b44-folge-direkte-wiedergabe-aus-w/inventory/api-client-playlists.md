# Detailbefund — Anforderung 3 + 6 (API-Seite): Playlists und Ausblenden/Überspringen

## ApiClient (`VideoPlayer.ApiClient`)

`VideoPlayer.ApiClient/VideoPlayerApiClient.cs` (704 Zeilen) — plattformneutraler Client auf
`HttpClient` mit fester Retry-Strategie (1s/2s/4s, max. 3 Wiederholungen), Client-Gate-Header
(`ClientGate.HeaderName`), einmaliger Token-Erneuerung bei 401 (`TokenRefresher`-Option).

Vorhandene Endpunkte (nachweisbar im Code):

| Bereich | Methode | Endpunkt |
|---|---|---|
| Auth | `LoginAsync` | `POST api/auth/login` (Z. 117) |
| Auth | `RefreshSessionAsync` | `POST api/auth/refresh` (Z. 144) |
| Auth | `LogoutSessionAsync` | `POST api/auth/logout` (Z. 164) |
| Health | `CheckHealthAsync` | `GET api/health` (Z. 188) |
| Quellen | `GetSourcesAsync`/`GetSourceAsync`/`GetSourceGenresAsync` | `GET api/Sources`, `api/Sources/{id}`, `api/SourceGenres/{id}` (Z. 214–237) |
| Neu | `GetRecentEntriesAsync` | `GET api/items/recent` (Z. 246) |
| Weiterschauen | `GetContinueWatchingAsync` | `GET api/continue-watching` (Z. 258) |
| Weiterschauen | `ReportPlaybackProgressAsync` | `POST api/continue-watching/progress` (Z. 269) |
| Weiterschauen | **`HideContinueWatchingEntryAsync`** | **`POST api/continue-watching/hide`** (Z. 296–308) |
| Weiterschauen | **`SkipContinueWatchingEntryAsync`** | **`POST api/continue-watching/skip`** (Z. 315–327) |
| Favoriten | `GetFavoritesAsync`/`ToggleFavoriteAsync`/`RemoveFavoriteAsync` | `GET api/favorites`, `POST api/favorites/toggle`, `POST api/favorites/remove` (Z. 336–361) |
| Einträge | `GetSourceItemsAsync` | `GET api/items` (Paging/Suche/Genre, Z. 378) |
| Einträge | `GetMovieAsync`/`GetMovieCollectionAsync`/`GetTvShowAsync`/`GetTvShowEpisodeAsync`/`GetGenreOptionsAsync` | `GET api/items/{movie|moviecollection|tvshow|tvshowepisode}/{id}`, `api/items/genres` (Z. 407–449) |
| Bilder | `GetPictureAsync`/`GetSourceIconAsync`/`GetHeroBackgroundAsync`/`GetEpisodeBackgroundImageAsync` | `api/pictures/{id}`, `api/sourceicons/{id}`, `api/pictures/hero-background`, `api/episodes/{id}/background-image` (Z. 459–520) |

Pairing/Bootstrap liegt im separaten `DevicePairingClient` (`VideoPlayer.ApiClient/DevicePairingClient.cs`):
`api/pairing/exchange`, `api/pairing/bootstrap`.

## Ausblenden/Überspringen (Anf. 6 — API-Abdeckung)

**Beide Endpunkte existieren bereits** im neuen Client:

- `HideContinueWatchingEntryAsync(mediaType, mediaId)` → `POST api/continue-watching/hide`
  mit Body `{ MediaType, MediaId }` → `ContinueWatchingMutationResult`
- `SkipContinueWatchingEntryAsync(mediaType, mediaId)` → `POST api/continue-watching/skip`
  gleiche Signatur.

`ContinueWatchingMutationResult` = `record (string Status, string Message)`
(`VideoPlayer.ApiClient/Models/ContinueWatching.cs` Z. 33).

Auch der Altbestand-Client hat beide: `VideoWebPlayer.Client/VideoWebPlayerClient.cs`
`HideContinueWatchingAsync` (Z. 377) / `SkipContinueWatchingAsync` (Z. 391) mit eigenem
`ContinueWatchingMutationResult` (`Models/ContinueWatchingMutationResult.cs`) — **wird im
Altbestand aber nicht aufgerufen** (kein Aufrufer außerhalb der Client-Definition; die
Long-Press-Behandlung im Altbestand entfernt Weiterschauen-Einträge nur lokal, siehe
`kontextmenue.md`).

Test-Pins: `VideoWebPlayer.Maui.Tests/VideoPlayerApiClientTests_Endpoints.cs` Z. 29–32
(Cases `Hide`/`Skip` gegen `StubHttpMessageHandler`).

**In der App (VideoPlayer.Mvu/VideoPlayer.Maui) werden beide Methoden bisher nicht benutzt** —
kein Effekt/keine Message existiert dafür.

## Playlists (Anf. 3)

- **Kein Playlist-Code im Repository**: kein Endpunkt, kein DTO, kein Service, keine Seite,
  keine Message. Einzige Treffer für „playlist"/„wiedergabeliste" sind die
  Anforderungsdokumente dieser Aufgabe.
- Damit existiert auch **kein `ContinueWatchingEntry`-Feld mit Playlist-Bezug**
  (`Models/ContinueWatching.cs`: `MediaType`, `Entry`, `PositionSeconds`, `DurationSeconds`,
  `Title`, `PosterPictureId`) und kein Playlist-Feld im `DownloadManifestEntry`
  (`VideoPlayer.Storage/Entities/DownloadManifestEntry.cs`: `VideoId`, `VideoType`, `Title`,
  `FilePath`, `State`, `Error`, `DownloadedBytes`, `TotalBytes`, `SizeBytes`, `ShowId`,
  `ShowTitle`, `SeasonNumber`, `EpisodeNumber`, `UpdatedAtUtc` — siehe Entitäts-Datei).
- Ob der **Server** Playlist-Endpunkte anbietet, ist aus diesem Repository nicht verifizierbar —
  das Serverprojekt liegt in einem separaten Repo (martin-stromberg/VideoPlayer,
  siehe `README.md`). Die bekannte API-Fläche ist in `docs/help/api-client/api.md` dokumentiert
  (ohne Playlist-Einträge).
- Der E2E-Mock-Server `VideoPlayer.E2E.Tests/Support/MockVideoServer.cs` kennt nur die
  bisherigen Endpunkte (health, pairing, auth, continue-watching/progress, items, favorites,
  Sources, items/recent) — keine Playlists.

## Menü/Navigation (Einbauort für „Playlists")

- Shell-Flyout (`VideoPlayer.Maui/AppShell.xaml`): statische `FlyoutItem`s
  Startseite/Favoriten/Downloads/Profil/Einstellungen (`Route`-Attribute `home`, `favorites`,
  `downloads`, `profile`, `settings`).
- `VideoPlayer.Mvu/AppPage.cs`: `enum AppPage` (Loading, Home, Favorites, Downloads, Profile,
  Settings, ServerSetup, Login — letztere ohne Menüeintrag).
- `VideoPlayer.Mvu/AppRoutes.cs`: `Routes`-Map AppPage→Route, `PushedRoutes` (serversetup,
  login), `MenuPages` (Reihenfolge des Hauptmenüs). Kommentar Z. 4–7: Route-Namen sind per
  Contract-Test an `AppShell.xaml` gepinnt.
- Dynamische Quellengruppe: `AppShell.xaml.cs` fügt per `Items.Insert` hinter „Startseite"
  (Index 1) eine Sektion „Quellen" mit je einem `MenuItem` pro Quelle ein
  (`InsertSourceMenuItems` Z. 213–266) — Klick dispatcht `SourceSelectedMessage`.
- Neue Menüseite „Playlists" müsste konventionsgemäß ergänzt werden in: `AppPage`,
  `AppRoutes` (Routes + MenuPages), `AppShell.xaml` (FlyoutItem), neue Seite unter
  `VideoPlayer.Maui/Pages/`, DI-Registrierung in `MauiProgram.cs`, State-Record +
  Messages/Effects im MVU-Projekt. Contract-Tests pinnen Routen/Menü (`AppRoutesTests`,
  `VideoPlayerMauiContractTests`, `SourcesContractTests` in `VideoWebPlayer.Maui.Tests`).
