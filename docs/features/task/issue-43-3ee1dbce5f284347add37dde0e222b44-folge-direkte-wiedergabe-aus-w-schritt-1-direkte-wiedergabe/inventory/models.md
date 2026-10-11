# Datenmodell — Direkte Wiedergabe aus der Weiterschauen-Liste

## `HomeMediaItem`
Datei: `VideoPlayer.Mvu/HomeMediaItem.cs`

Anzeigefertiger Eintrag aller drei Startseiten-Listen (wird von `HomeListService`/`OfflineContinueWatchingService` gemappt).

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `long` (required) | Id des Mediums (Navigations-/Wiedergabeziel) |
| `MediaType` | `string` (required) | Medientyp kleingeschrieben — Server-Konvention der Weiterschauen-Liste (`movie`, `show`, `season`, `episode`, `collection`) oder CLR-Konvention (`movie`, `moviecollection`, `tvshow`, `tvshowseason`, `tvshowepisode`) |
| `Title` | `string` (required) | Anzeigetitel |
| `PosterPictureId` | `long?` | Bild-Id für `api/pictures/{id}` |
| `Progress` | `double?` | Fortschritt 0–1 (nur Weiterschauen) |
| `ShowId` | `long?` | Serienkontext-Hint (aus `MediaHints`: `episode.Season.Show.Id` bzw. Manifest-`ShowId` bei Offline-Downloads) |
| `SeasonId` | `long?` | Staffel-Hint |
| `EpisodeId` | `long?` | Episoden-Hint |
| `IsPlaceholder` | `bool` | Offline-Platzhalter fehlender Folge-Episoden (nicht abspielbar) |
| `IsSelectable` | `bool` | `!IsPlaceholder` — Auswahlbarkeit |
| `CardOpacity` / `HasPoster` / `ShowProgress` | `double`/`bool`/`bool` | Anzeige-Hilfswerte |

## `PlayerState`
Datei: `VideoPlayer.Mvu/PlayerState.cs`

Zustand der Wiedergabeseite; `Empty` als Ausgangszustand.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `MediaType` | `string?` | Medientyp (normalisiert, CLR-Konvention) |
| `MediaId` | `long` | Id des abzuspielenden Mediums |
| `Title` | `string?` | Anzeigetitel |
| `Source` | `PlaybackSource?` | Aufgelöste Quelle (inkl. `ResumePosition`) |
| `IsResolving` / `Attempted` | `bool` | Auflösung läuft / Wiedergabeversuch gestartet |
| `Position` / `Duration` | `TimeSpan` | Wiedergabewerte |
| `IsPlaying` / `IsBuffering` | `bool` | Player-Zustände |
| `IsWatched` | `bool` | „angesehen"-Schwelle erreicht |
| `IsPictureInPicture` | `bool` | PiP-/Mini-Player |
| `UpcomingEpisodes` | `IReadOnlyList<PlayerEpisodeRef>` | Fortsetzungsliste — Basis der Auto-Fortsetzung und des Fehler-Rückfalls; wird derzeit ausschließlich in `AppUpdate.UpdatePlayRequested` aus `model.MediaDetails` befüllt |
| `Error` | `string?` | Fehlertext |
| `LastReportedPositionSeconds` | `double` | Throttling der Fortschrittsmeldungen |
| `HasNextEpisode` | `bool` | `UpcomingEpisodes.Count > 0` |
| `HasError` | `bool` | `Error is not null` |
| `IsActive` | `bool` | `MediaId > 0 && MediaType is not null` |

## `PlayerEpisodeRef`
Datei: `VideoPlayer.Mvu/PlayerEpisodeRef.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `MediaId` | `long` | Episoden-Id |
| `Title` | `string?` | Anzeigetitel |

## `PlaybackSource`
Datei: `VideoPlayer.Mvu/PlaybackSource.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Kind` | `PlaybackSourceKind` | `ServerStream` / `LocalFile` |
| `Location` | `string` | Stream-URL bzw. Dateipfad |
| `ResumePosition` | `TimeSpan?` | Fortsetzungsposition |

## `ContinueWatchingEntry`
Datei: `VideoPlayer.ApiClient/Models/ContinueWatching.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `MediaType` | `string` | Server-Typ (`movie`, `episode`, `show`, `season`, `collection`) |
| `Entry` | `MediaEntry?` | Eingebetteter Medieneintrag (polymorph über `$type`) |
| `PositionSeconds` | `long` | Fortsetzungsposition |
| `DurationSeconds` | `long?` | Gesamtdauer |
| `Title` / `PosterPictureId` | `string`/`long?` | Anzeige |

**Kein** Playlist-Feld (Playlist-Kontext kommt erst in Schritt 5 des Projektplans).

## `MediaEntry`-Hierarchie (Auswahl)
Datei: `VideoPlayer.ApiClient/Models/MediaEntry.cs`

| Klasse | relevante Eigenschaften | Zweck |
|--------|------------------------|-------|
| `MediaEntry` | `Id`, `Name`, `MediaSourceId`, `PosterPictureId`, `IsFavorite`, `WatchedAt` | Basisklasse (`$type`-Diskriminator: `movie`, `movieCollection`, `show`, `season`, `episode`) |
| `TvShow` | `Seasons: TvShowSeason[]` | `GET api/items/tvshow/{id}` liefert Serie mit eingebetteten Staffeln/Episoden |
| `TvShowSeason` | `Number`, `Show`, `Episodes: TvShowEpisode[]` | Staffel mit Serien-Rückverweis |
| `TvShowEpisode` | `Number`, `Season`, `GeneratedBackgroundPictureId` | Episode mit Staffel-Rückverweis |

## `CachedShowEntry` / `CachedShowSeason` / `CachedShowEpisode` / `ShowEpisodeRef`
Datei: `VideoPlayer.Mvu/ShowDetailsCache.cs`

Gecachte Serienstruktur in `cached_lists` (Schlüssel `show:{id}`) — Offline-Serienkontext: Staffeln/Episoden geordnet, `OrderedEpisodes`, `NextEpisodeAfter`, `NextEpisodeAfterId`.

## `DevApiModelSnapshot`
Datei: `VideoPlayer.Mvu/DevApi/DevApiModelSnapshot.cs`

Kuratierter JSON-Auszug des `AppModel` für `GET /model` (E2E-Messpunkte), u. a.: `ContinueWatchingCount`, `FavoritesCount`, `RecentEntriesCount`, `PlayerIsActive`, `PlayerIsPlaying`, `PlayerPositionSeconds`, `PlayerError`, `CurrentPage` (Menüseite — navigierte Detail-/Player-Seiten ändern ihn nicht). **Kein** Feld für `UpcomingEpisodes`.
