# Logik — Bestandsaufnahme

## `VideoPlayerApiClient` (`VideoPlayer.ApiClient/VideoPlayerApiClient.cs`, 704 Zeilen)

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `LoginAsync` / `RefreshSessionAsync` / `LogoutSessionAsync` | public | Auth-Endpunkte `api/auth/*` |
| `CheckHealthAsync` | public | `GET api/health`, unterscheidet Gate-Ablehnung |
| `GetSourcesAsync` / `GetSourceAsync` / `GetSourceGenresAsync` | public | `api/Sources*`, `api/SourceGenres/{id}` |
| `GetRecentEntriesAsync` | public | `api/items/recent` |
| `GetContinueWatchingAsync` / `ReportPlaybackProgressAsync` / `HideContinueWatchingEntryAsync` / `SkipContinueWatchingEntryAsync` | public | `api/continue-watching*` |
| `GetFavoritesAsync` / `ToggleFavoriteAsync` / `RemoveFavoriteAsync` | public | `api/favorites*` |
| `GetSourceItemsAsync` | public | `api/items` paginiert (0-basierte `page`, `size`, `search`, `genreId`) |
| `GetMovieAsync` / `GetMovieCollectionAsync` / `GetTvShowAsync` / `GetTvShowEpisodeAsync` / `GetGenreOptionsAsync` | public | `api/items/*` |
| `GetPictureAsync` / `GetSourceIconAsync` / `GetHeroBackgroundAsync` / `GetEpisodeBackgroundImageAsync` | public | Binärabrufe (`api/pictures/{id}` u. a.) via `SendWithPolicyAsync` + `ReadAsByteArrayAsync` |
| `SendWithPolicyAsync` | private | Retry (1 s/2 s/4 s, 5xx+408+429+Transport), 401 → einmalige Token-Erneuerung, sonst `ApiException` |
| `GetJsonAsync` / `PostJsonAsync` / `DeserializeAsync` | private | JSON-Pipeline (`PropertyNameCaseInsensitive`) |

**Keine Playlist-Methoden vorhanden** — neue Region `#region Playlists` folgt dem Muster der Regionen `Favorites`/`Pictures`.

## `AppUpdate` (`VideoPlayer.Mvu/AppUpdate.cs`, 2705 Zeilen)

Statische Switch-Methode `Update(AppModel, AppMessage)` → `AppTransition`. Relevante Muster:

- `UpdateFavoritesPageOpened` (Z. 1860): Guard `IsLoading`, nur in `Connected|Offline`; setzt `IsLoading/Attempted`, emittiert `LoadFavoritesPageEffect`.
- `UpdateSourceBrowserOpened` (Z. 1432): Re-Entry-Guard (`SourceId` identisch → unchanged), sonst Initialisierung + Lade-Effekte.
- `UpdateSourceItemsLoaded` (Z. 1478): Append-/Replace-Paging, veraltete Antworten via `Page != NextPage` verworfen, Fehler bei Append lässt Liste stehen.
- `UpdateSourceItemsNextPageRequested` / `UpdateSourceItemsRetryRequested` (Z. 1591/1609): Guards `HasMore`/`IsLoading*`, Retry unterscheidet Erstlauf vs. Folgeseite.
- `UpdateSourceMediaItemSelected` (Z. 1624): navigiert via `NavigateToMediaDetailsEffect`.
- `UpdateNavigateTo`/`UpdateNavigated` (Z. 2662/2684): `MenuPages`-Guard — Menüseiten sind im `NeedsLogin`/`NeedsServerSetup`-Zustand gesperrt; `AppPage.Playlists` muss in `MenuPages` stehen, um dieselbe Sperre zu erhalten.
- `IsWorkflowPage` (Z. 949): `Loading|Login|ServerSetup` — `Playlists` ist eine reguläre Menüseite, kein Eintrag nötig.

## `AppShell` (`VideoPlayer.Maui/AppShell.xaml`, `.xaml.cs`)

- Statische `FlyoutItem`s: Startseite, Favoriten, Downloads, Profil, Einstellungen — Reihenfolge deckt sich mit `AppRoutes.MenuPages` (Contract-Test).
- Dynamische Quellengruppe hinter „Startseite" via `Items.Insert` (`AppShell.xaml.cs` Z. 213 ff.).
- `Routing.RegisterRoute` für `serversetup`, `login`, `mediadetails`, `sourceoverview`, `player` (Z. 59–77).
- `OnShellNavigated` → `NavigatedMessage` über `AppRoutes.TryGetPage` (Z. 100).

## `ShellAppEffectHandler` (`VideoPlayer.Maui/Services/ShellAppEffectHandler.cs`, 1645 Zeilen)

- `HandleAsync`-Switch je Effekt (Z. 99–256); `DispatchMessageAsync` meldet Ergebnis-Messages in den MVU-Zyklus.
- `NavigateToSourceOverviewAsync` (Z. 618) + `IsTopmostSourceOverview` (Z. 650): Push-Route `sourceoverview?id=…&name=…` mit Duplikat-Guard — Muster für `playlistdetails?id=…`.
- `LoadSourceItemsAsync` (Z. 721): Fehler → `SourceItemsPage` leer + `error`-Text, meldet `SourceItemsLoadedMessage` — Muster für Lade-Handler.
- `NavigateAsync` (Z. 1585): `//{route}`-Navigation der Menüseiten.

## Services

### `SourceBrowserService` (`VideoPlayer.Mvu/SourceBrowserService.cs`)

`LoadSourcesAsync`, `LoadGenresAsync`, `LoadItemsAsync` über `authSession.Client` (`null` → `InvalidOperationException("Kein Server verbunden.")`); Mapping Server-DTO → Anzeige-Record; `SourceItemsPage`-Ergebnisrecord mit `HasMore` aus „Seite voll".

### `PosterImageService` (`VideoPlayer.Maui/Services/PosterImageService.cs`)

`GetImageSourceAsync` (Datei `{id}.poster` in `PreviewsDirectory`, sonst `GetPictureAsync`), `GetSourceIconSourceAsync` (`{id}.sourceicon`, `GetSourceIconAsync`); beide mit Memory-Cache, `null` bei Fehler/Offline — Vorlage für den Cover-Abruf `api/playlists/{id}/cover`.

### `CachedPosterImage` (`VideoPlayer.Maui/Controls/CachedPosterImage.cs`)

`Image` mit Bindable Property `PictureId` (`long?`); löst über `IPlatformApplication.Current.Services.GetService<PosterImageService>().GetImageSourceAsync` auf — Vorlage für ein Cover-Control (Playlist-Id statt Picture-Id).

## `MockVideoServer` (`VideoPlayer.E2E.Tests/Support/MockVideoServer.cs`, 1304 Zeilen)

- TCP-basierter Mini-HTTP-Server; `IsListEndpoint` (Z. 962): `/api/favorites`, `/api/continue-watching`, `/api/Sources`, `/api/items/recent` → Gate/Bearer-Prüfung + Leerliste; `ListOverride` (Z. 482) liefert Opt-in-Antworten.
- Unbekannte Routen → 404 JSON, niemals 5xx.
- Playlist-Endpunkte fehlen komplett; Muster für neue Opt-in-Listen: Eigenschaften `Playlist*`-Konfiguration + `ListOverride`-Zweig bzw. eigener Handler vor dem generischen `IsListEndpoint`-Zweig (`/api/playlists/{id}/…` ist kein Listen-Endpunkt, sondern Objekt-/Binärabruf — eigene `path.StartsWith("/api/playlists/")`-Behandlung nötig).

## Dev-API (E2E-Verifikation)

- `DevApiModelSnapshot.FromModel` (`VideoPlayer.Mvu/DevApi/DevApiModelSnapshot.cs`): kuratierte Modellfelder → `GET /model`; spiegelt sich in `VideoPlayer.E2E.Tests/Support/DevApiModels.cs` (`DevApiModel`).
- `GET /tree` liefert Elementbaum mit `AutomationId`/`Items` (`DevApiTreeNode`/`DevApiTreeItem`) — Listen-Inhalte sind über `FindByAutomationId` + `Items[].Text` prüfbar; die Seiten-Elemente benötigen dafür `AutomationId`-Attribute im XAML (Muster `ContinueWatchingCarousel` auf der Startseite).

## Seiten-Lebenszyklus (Contract-gepinnt, `PageLifecycleContractTests`)

`subscribed`-Flag + `ModelChanged -= OnModelChanged` in `OnDisappearing`, `+=` in `OnAppearing`; `UpdateUi` nur bei `subscribed`. Neue Seiten müssen dasselbe Muster zeigen (siehe `FavoritesPage.xaml.cs`, `SourceOverviewPage.xaml.cs`).
