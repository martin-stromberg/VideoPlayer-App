# Logik — Direkte Wiedergabe aus der Weiterschauen-Liste

## `MediaCarousel`
Datei: `VideoPlayer.Maui/Controls/MediaCarousel.xaml(.cs)`

| Methode / Member | Sichtbarkeit | Kurzbeschreibung |
|------------------|-------------|------------------|
| `Title`, `Items`, `ItemWidth`, `ItemHeight` | public (BindableProperty) | Abschnittsüberschrift, Einträge (`IReadOnlyList<HomeMediaItem>`), Kartenmaße |
| `ItemTapped` (Event) | public | `EventHandler<HomeMediaItem>` — liefert nur das Item, **keine** Herkunftslisten-Kennung |
| `OnItemTapped` | private | Tap-Handler der Karten-`Border` (`TapGestureRecognizer`, `MediaCarousel.xaml` Z. 39–41) → `ItemTapped` |
| `OnItemsChanged` | private static | Blendet Abschnitt bei leerer Liste aus |

Publizierte Events: `ItemTapped` → abonniert in `HomePage.xaml` (3×).

## `HomePage`
Datei: `VideoPlayer.Maui/Pages/HomePage.xaml(.cs)`

| Methode / Member | Sichtbarkeit | Kurzbeschreibung |
|------------------|-------------|------------------|
| `OnMediaItemTapped` | private | Dispatcht `MediaItemSelectedMessage(item)` — für **alle drei** Karussells identisch (Z. 103–108) |
| `OnModelChanged` | private | UI-Aktualisierung (Banner, Hero) auf dem Hauptthread |

XAML Z. 150–161: `MediaCarousel` „Weiterschauen" (256×144, `Model.ContinueWatching`, **keine** `AutomationId`), „Favoriten" (`Model.Favorites`), „Neu im Programm" (`AutomationId="RecentCarousel"`, `Model.RecentEntries`) — alle `ItemTapped="OnMediaItemTapped"`.

## `AppUpdate`
Datei: `VideoPlayer.Mvu/AppUpdate.cs`

Statisches Switch `Update(AppModel, AppMessage)` → `AppTransition` (rein, ohne Seiteneffekte).

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `UpdateMediaItemSelected` | private | Z. 1188–1203: Guard (`Id > 0`, `MediaType` belegt, `IsSelectable`), dann einziger Effekt `NavigateToMediaDetailsEffect(MediaType, Id, Title, ShowId, SeasonId, EpisodeId)` |
| `UpdatePlayRequested` | private | Z. 1923–1945: Normalisierung via `MediaTypeNames.Normalize`, Guard nur `movie`/`tvshowepisode`; setzt `PlayerState` (`IsResolving`, `Attempted`, `UpcomingEpisodes = NextEpisodeQueue(model, mediaId)`) + `NavigateToPlayerEffect` + `ResolvePlaybackSourceEffect` |
| `NextEpisodeQueue` | private | Z. 1956–1978: Folge-Episoden **aus dem Serienkontext der geladenen Detailseite** (`model.MediaDetails.Details.Seasons`), staffelübergreifend sortiert; leer ohne `HasSeasons` |
| `UpdatePlaybackSourceResolved` | private | Z. 1988–2017: Identitäts-Guard (veraltete Antworten verworfen); Erfolg → `Source`; Fehlschlag → `AdvanceToNextEpisode` |
| `AdvanceToNextEpisode` | private | Z. 2030–2072: Wechsel auf `UpcomingEpisodes[0]` + neuer `ResolvePlaybackSourceEffect` |
| `UpdatePlaybackProgress` / `UpdatePlaybackEnded` | private | Auto-Fortsetzung bei Restlaufzeit < `Settings.AutoResumeRemainingSeconds` bzw. am Ende |

Publizierte Effekte: `NavigateToMediaDetailsEffect`, `NavigateToPlayerEffect`, `ResolvePlaybackSourceEffect`, `ReportPlaybackProgressEffect`, `DeleteWatchedDownloadEffect` u. a.

## `ShellAppEffectHandler`
Datei: `VideoPlayer.Maui/Services/ShellAppEffectHandler.cs`

`IAppEffectHandler`-Implementierung — führt Effekte am App-Rand aus; Dienste per `services.GetRequiredService<T>()` bzw. `authSession.Client` (angemeldeter `VideoPlayerApiClient`).

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `HandleAsync` | public | Z. 87–233: großes `switch` über alle `AppEffect`-Typen |
| `ResolvePlaybackSourceAsync` | private | Z. 252–278: `PlaybackSourceService.ResolveAsync` (offline ohne Server-Stream) → `PlaybackSourceResolvedMessage` |
| `LoadMediaDetailsAsync` | private | Z. 594–614: `MediaDetailsService.LoadAsync` → `MediaDetailsLoadedMessage` |
| `NavigateToPlayerAsync` / `NavigateToMediaDetailsAsync` | private | Z. 235–250 / 437–469: `Shell.Current.GoToAsync` (Routes `player` bzw. `mediadetails?…`) |
| `DispatchMessageAsync` | private | Z. 859–870: `MvuHost.DispatchAsync` auf dem MainThread (Fehler isoliert) |

Abonnierte Events: `SessionExpired`, `DeviceTokenRejected`, `AvailabilityChanged`, `serverEvents.*`, `downloads.*` — Meldungen gehen als Messages in den MVU-Zyklus.

## `PlaybackSourceService`
Datei: `VideoPlayer.Mvu/PlaybackSourceService.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `ResolveAsync` | public | Z. 78–130: Fortsetzungsposition via `ResolveResumePositionAsync` (online `GetContinueWatchingAsync`, sonst Cache `cached_lists[continue_watching]`), Quelle = fertige lokale Datei vor Server-Stream `api/items/{typ}/{id}/stream?access_token=…` |
| `FindResumePosition` | private | Z. 191–201: Eintrag mit Typ + `Entry.Id == mediaId` und `PositionSeconds > 0` → `TimeSpan` |

## `ShowDetailsCache`
Datei: `VideoPlayer.Mvu/ShowDetailsCache.cs`

Statischer Lese-/Schreibzugang der Serienstruktur (`cached_lists`, `show:{id}`): `KeyFor`, `WriteAsync` (nach `GetTvShowAsync`-Aufrufen), `ReadAsync` (Fehler → `null`), `OrderedEpisodes`, `NextEpisodeAfter`, `NextEpisodeAfterId`. Geschrieben wird die Struktur aktuell von `MediaDetailsService.FetchShowAsync` (Detailseite) und `AutoDownloadService.EnqueueNextEpisodesAsync` (Vorlauf).

## `MediaDetailsService`
Datei: `VideoPlayer.Mvu/MediaDetailsService.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `LoadAsync` | public | Online: `api/items/{typ}/{id}` je Medientyp (`GetMovieAsync`, `GetMovieCollectionAsync`, `FetchShowAsync`, `GetTvShowEpisodeAsync`); offline: `LoadOfflineAsync` aus Manifest + `ShowDetailsCache` |
| `FetchShowAsync` | private | Z. 328–332: `GetTvShowAsync(showId)` + `ShowDetailsCache.WriteAsync` — **Muster für die Serienkontext-Ladung ohne Detailseite** |
| `LoadEpisodeAsync` | private | Episoden-Details + Serienauflösung über `show`-Hint oder eingebetteten `Season.Show`-Bezug |

## `AutoDownloadService`
Datei: `VideoPlayer.Mvu/AutoDownloadService.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `EnqueueNextEpisodesAsync` | private | Z. 154–192: `GetTvShowAsync(item.ShowId)` + `ShowDetailsCache.WriteAsync`, staffelübergreifende Ordnung `OrderBy(s.Number).SelectMany(s.Episodes.OrderBy(e.Number))`, Index der Weiterschauen-Episode, Folge-Episoden einreihen — **Referenzmuster der Folge-Episoden-Auflösung** |

## `MediaTypeNames`
Datei: `VideoPlayer.Mvu/MediaTypeNames.cs`

`Normalize(string?)` — bildet Server- (`show`, `season`, `episode`, `collection`) und CLR-Konvention auf die CLR-Konvention ab (`tvshow`, `tvshowseason`, `tvshowepisode`, `moviecollection`, `movie`).

## `HomePage`-Verdrahtung / MVU-Host
`MvuHost.DispatchAsync(AppMessage)` — einziger Eingang in den MVU-Zyklus aus der UI; `HomePage` dispatcht aus den Event-Handlern.

## DI-Registrierung (`MauiProgram.cs`)
Datei: `VideoPlayer.Maui/MauiProgram.cs` — Mvu-Services als Singletons (Z. 86–110: `HomeListService`, `OfflineContinueWatchingService`, `MediaDetailsService`, `PlaybackSourceService`, `AutoDownloadService` u. a.), Seiten als `AddTransient`.

## E2E-Infrastruktur

### `MauiDevApiService` / `DevApiVisualTree`
Dateien: `msTools.DeveloperApi.Maui/MauiDevApiService.cs`, `msTools.DeveloperApi.Maui/DevApiVisualTree.cs` (nur `#if DEBUG || DEV_API`)

- `GET /model` → `DevApiModelSnapshot`; `GET /tree` → logischer Baum (inkl. `Items` der `BindableLayout`-/`CollectionView`-Container — UIA-Lücke); `POST /invoke` mit `itemIndex` → `TrySelectItem` löst den `TapGestureRecognizer` des indexierten Eintrags per internem `SendTapped` aus (dieselbe Ereigniskette wie Fingertipp); `POST /set-text`.
- DevApi-Contract-Tests pinnen: genau 4 Endpunkte, kein `dispatch`-Endpunkt, `#if`-Umschließung, Loopback-Bindung.

### `MockVideoServer`
Datei: `VideoPlayer.E2E.Tests/Support/MockVideoServer.cs`

Hermetischer Mini-Server: Pairing/Bootstrap/Login/Refresh/Logout, `api/continue-watching/progress`-Senke, Listen-Endpunkte (`IsListEndpoint`: `api/favorites`, `api/continue-watching`, `api/Sources`, `api/items/recent`), Opt-in-Wiedergabefluss (`PlaybackMovieId`, `PlaybackMovieTitle`, `PlaybackVideoPath`, `PlaybackResumePositionSeconds`, `PlaybackStreamFails`): Movie-Details `api/items/movie/{id}`, Stream `api/items/movie/{id}/stream` (Range-fähig), continue-watching-Eintrag nur `mediaType="movie"`. **Kein** `api/items/tvshow/{id}`, **kein** Episode-Typ im Weiterschauen-Eintrag, **kein** `api/items/tvshowepisode/{id}/stream`.

### `E2ETestBase` / `DevApiClient`
Dateien: `VideoPlayer.E2E.Tests/Tests/E2ETestBase.cs`, `VideoPlayer.E2E.Tests/Support/DevApiClient.cs`

Hilfen: `StartAppAsync`, `ConnectToPairingRequiredAsync`, `PairAsync`, `LoginAsync`, `DevApi.WaitForModelAsync`, `GetTreeAsync`, `InvokeAsync(automationId, itemIndex)`. `DevApiModels` spiegelt den Snapshot.
