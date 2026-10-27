# Bestandsaufnahme — Logik und Abläufe

## `MediaDetailsService`
Datei: `VideoPlayer.Mvu/MediaDetailsService.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `LoadAsync` | public | Lädt `MediaDetailsView` je Medientyp; ohne Client → `LoadOfflineAsync` |
| `LoadOfflineAsync` | private | Offline-Snapshot aus Manifest + `ShowDetailsCache` + Favoriten-Kopie |
| `LoadCollectionOrMovieAsync` | private | Kollektion laden; 404 → Film-Fallback |
| `LoadSeasonInShowContextAsync` | private | Staffel-Aufruf → Serienansicht mit `SelectedSeasonId` = Staffel |
| `LoadEpisodeAsync` | private | Episode → Serienansicht (show-Hint oder `Season.Show`) mit `HighlightedEpisodeId`; ohne Serie → Basisdetails mit `BannerPictureId = GeneratedBackgroundPictureId ?? Banner ?? Poster` |
| `FetchShowAsync` | private | `GetTvShowAsync` + `ShowDetailsCache.WriteAsync` |
| `LoadManifestEntriesAsync` | private | Download-Manifest lesen (Fehler → leere Liste) |
| `IsCachedFavoriteAsync` | private | Favoriten-Kopie `cached_favorites` prüfen |
| `FindCachedTitleAsync` | private | Titel-Fallback aus Favoriten-Kopie |
| `HasLocalFile` | private | Abgeschlossener Download + existierende Datei |
| `MapOfflineShow` | private | Offline-Serienansicht (Platzhalter-Markierung); setzt **keine** Bild-Ids, kein `Plot`, keine `GenreNames` |
| `FindSeasonId` / `FindEpisodeTitle` | private | Suche in `CachedShowEntry` |
| `MapMovie` / `MapCollection` / `MapShow` / `MapSeasons` | private | Mapping auf `MediaDetailsView`/`SeasonItem`/`EpisodeItem` |

Abonnierte Events: keine. Publizierte Events: keine (Rückgabewerte).

## `AppUpdate`
Datei: `VideoPlayer.Mvu/AppUpdate.cs`

| Methode | Kurzbeschreibung |
|---------|------------------|
| `Update` | Zentrale Verteilung aller `AppMessage`-Typen (switch) |
| `UpdateMediaDetailsRequested` | Neue `MediaDetailsState` + `LoadMediaDetailsEffect`; identische Ziele werden nicht erneut geladen; ungültige Identität → Fehler |
| `UpdateMediaDetailsLoaded` | Übernimmt `Details`/Fehler; **verwirft Antworten veralteter Identität** (`MediaType`/`MediaId`-Abgleich) |
| `UpdateSeasonSelected` | Setzt `Details.SelectedSeasonId`; unverändert, wenn keine Staffeln oder gleiche Staffel |
| `UpdateFavoriteToggleRequested` / `UpdateFavoriteToggleCompleted` | Favoriten-Umschaltung |
| `WithHeroLoad` / `UpdateHeroBackgroundLoaded` | Muster Schritt 5: Ladeeffekt beim Verbindungsaufbau; veraltete Antwort ohne `IsLoading` wird ignoriert; Fehlschlag löscht kein vorhandenes Bild |
| `UpdateLogoutRequested` | Setzt u. a. `MediaDetails = MediaDetailsState.Empty` zurück |

## `AppMessage` / `AppEffect`
Dateien: `VideoPlayer.Mvu/AppMessage.cs`, `VideoPlayer.Mvu/AppEffect.cs`

Relevante bestehende Typen: `MediaDetailsRequestedMessage`,
`MediaDetailsLoadedMessage`, `SeasonSelectedMessage`,
`FavoriteToggleRequestedMessage`, `FavoriteToggleCompletedMessage`,
`HeroBackgroundLoadedMessage` — sowie `LoadMediaDetailsEffect`,
`NavigateToMediaDetailsEffect`, `LoadHeroBackgroundEffect`.

## `ShellAppEffectHandler`
Datei: `VideoPlayer.Maui/Services/ShellAppEffectHandler.cs`

| Methode | Kurzbeschreibung |
|---------|------------------|
| `HandleAsync`-Switch | Verteilt jeden `AppEffect`-Record auf eine Ausführungsmethode |
| `LoadMediaDetailsAsync` | `MediaDetailsService.LoadAsync` → `MediaDetailsLoadedMessage`; Fehler → Fehlertext |
| `LoadHeroBackgroundAsync` | Muster Schritt 5: `authSession.Client?.GetHeroBackgroundAsync`, Fehlschlag → `HeroBackgroundLoadedMessage(null)` + Debug-Log |
| `NavigateToMediaDetailsAsync` | Shell-Route `mediadetails` mit show/season/episode-Hints |

## `VideoPlayerApiClient`
Datei: `VideoPlayer.ApiClient/VideoPlayerApiClient.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `GetPictureAsync` | public | `GET api/pictures/{id}` → `byte[]` |
| `GetSourceIconAsync` | public | `GET api/sourceicons/{id}` → `byte[]` |
| `GetHeroBackgroundAsync` | public | `GET api/pictures/hero-background` → `byte[]` |
| `SendWithPolicyAsync` | private | Retry 1 s/2 s/4 s (5xx, 408, 429, Transport), einmalige Re-Auth bei 401, Bearer-Token über `SetAccessToken` |

→ `GET api/episodes/{id}/background-image` fehlt noch (serverseitig
verifiziert: `EpisodesController.GetBackgroundImage` — generiertes Bild
lazy, Fallback `BannerPictureId ?? FanartPictureId`, sonst Platzhalter;
`CheckLogedIn` akzeptiert Bearer).

## `PosterImageService` / `CachedPosterImage`
Dateien: `VideoPlayer.Maui/Services/PosterImageService.cs`,
`VideoPlayer.Maui/Controls/CachedPosterImage.cs`

| Methode | Kurzbeschreibung |
|---------|------------------|
| `PosterImageService.GetImageSourceAsync` | `api/pictures/{id}` mit Dateicache (`{id}.poster` im `PreviewsDirectory`, nicht backup-pflichtig) + Memory-Cache; Fehler → `null` |
| `PosterImageService.GetSourceIconSourceAsync` | `api/sourceicons/{id}`, gleiche Konvention (`{id}.sourceicon`) |
| `CachedPosterImage.PictureId` | Bindable Property; lädt über DI-Lookup des Service |

→ Nur für `api/pictures`-Poster gedacht; deckt den Episoden-Still-Endpunkt
nicht ab. Für den Header wird stattdessen das Schritt-5-Muster
(`byte[]` im Model) verwendet, damit beide Endpunkte einheitlich laufen.

## Web-Referenz (Server-Repository `fd729906-…`, verifiziert)

- `TVShowDetails.razor`: `GetHeaderBackgroundUrl()` =
  `selectedEpisode != null` → `api/episodes/{id}/background-image`,
  sonst `GetBannerUrl(showSeasonInfo ? selectedSeason : show)`;
  `GetBannerUrl` = `BannerPictureId → FanartPictureId → /images/placeholder.png`.
- `SelectSeasonCoreAsync`: Staffelwechsel wählt die erste Episode der Staffel
  (`selectedEpisode` ≠ null → Header wechselt auf Episoden-Still).
- `MovieCollectionDetails.razor`: `.collection-header`/`.movie-header` —
  `GetBannerUrl(collection)` bzw. `GetBannerUrl(selectedMovie)`, Titel/Jahr/
  Genres/Plot unten links, `favorite-btn` oben rechts, `play-button` unten rechts.
- `app.css`: `.tvshow-header` u. a. — `min-height: clamp(360px,52vw,620px)`,
  `background-size: cover`, `background-color: var(--vp-surface-low)`,
  `border-bottom: 1px solid var(--vp-border)`, Vollbreite via negative
  Margins; Overlay `linear-gradient(90deg, rgba(19,19,19,.98) 0%,
  .72 46%, .1 100%)` + `linear-gradient(0deg, var(--vp-bg) 0%,
  rgba(19,19,19,.64) 36%, transparent 100%)`; Inhalt `max-width: 820px`,
  Meta `var(--vp-primary-soft)` fett, Plot `var(--vp-text-soft)`.
