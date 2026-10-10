# Datenmodell — Wiedergabe-Pfad

## `PlayerState`

Datei: `VideoPlayer.Mvu/PlayerState.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Empty` | `static PlayerState` | Ausgangszustand (keine Wiedergabe angefordert). |
| `MediaType` | `string?` | Medientyp (normalisiert, CLR-Konvention: `movie`/`tvshowepisode`). |
| `MediaId` | `long` | Id des Mediums. |
| `Title` | `string?` | Anzeigetitel. |
| `Source` | `PlaybackSource?` | Aufgelöste Quelle; `null` während Auflösung/Fehlschlag. |
| `IsResolving` | `bool` | Quellauflösung läuft (Ladeanzeige). |
| `Attempted` | `bool` | Seite geöffnet / Versuch gestartet. |
| `Position` | `TimeSpan` | Letzte gemeldete Position. |
| `Duration` | `TimeSpan` | Bekannte Gesamtdauer. |
| `IsPlaying` | `bool` | Wiedergabe läuft. |
| `IsBuffering` | `bool` | Zwischenpufferung (`Opening`/`Buffering`). |
| `IsWatched` | `bool` | „Angesehen"-Schwelle erreicht. |
| `IsPictureInPicture` | `bool` | Kompakter Overlay-/PiP-Modus aktiv. |
| `UpcomingEpisodes` | `IReadOnlyList<PlayerEpisodeRef>` | Geordnete Fortsetzungsliste (Auto-Fortsetzung/Fehler-Rückfall). |
| `HasNextEpisode` | `bool` (berechnet) | `UpcomingEpisodes.Count > 0`. |
| `Error` | `string?` | Fehlertext (`ErrorPanel` der Seite). |
| `HasError` | `bool` (berechnet) | `Error is not null`. |
| `LastReportedPositionSeconds` | `double` | Throttling der Fortschrittsmeldungen (−1 initial). |
| `IsActive` | `bool` (berechnet) | `MediaId > 0 && MediaType is not null`. |

Verwendet von `AppModel.Player` (`AppModel.cs` :197).

## `PlaybackSource`

Datei: `VideoPlayer.Mvu/PlaybackSource.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Kind` | `PlaybackSourceKind` (required) | `ServerStream` oder `LocalFile`. |
| `Location` | `string` (required) | Stream-URL inkl. `access_token` bzw. lokaler Dateipfad — geht ungeprüft in `MediaSource.FromUri`/`FromFile`. |
| `ResumePosition` | `TimeSpan?` | Fortsetzungsposition (Weiterschauen-Stand); `> 0` löst `SeekTo` in `PlayerPage.OnMediaOpened` aus. |

## `PlayerEpisodeRef`

Datei: `VideoPlayer.Mvu/PlayerEpisodeRef.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `MediaId` | `long` (required) | Id der Folge-Episode. |
| `Title` | `string` (required) | Anzeigetitel. |

## `AppMessage`-Records (Wiedergabe)

Datei: `VideoPlayer.Mvu/AppMessage.cs`

| Record | Signatur | Zweck |
|--------|----------|-------|
| `PlayRequestedMessage` | `(string MediaType, long MediaId, string? Title)` :464–467 | Wiedergabe angefordert. |
| `PlayerPageOpenedMessage` | leer :470 | Seite erschienen. |
| `PlaybackSourceResolvedMessage` | `(string MediaType, long MediaId, PlaybackSource? Source, string? Error)` :482–486 | Ergebnis der Quellauflösung. |
| `PlaybackProgressMessage` | `(TimeSpan Position, TimeSpan Duration, bool IsPlaying, bool IsBuffering)` :499–503 | Positions-Takt der Seite. |
| `PlaybackEndedMessage` | leer :506 | Medium zu Ende. |
| `PlaybackFailedMessage` | leer :509 | `MediaFailed` des MediaElement. |
| `PictureInPictureRequestedMessage` | leer :512 | PiP-Umschaltung angefordert. |
| `PictureInPictureChangedMessage` | `(bool IsActive)` :520 | PiP-Ergebnis. |
| `PlayerClosedMessage` | leer :526 | Seite verlassen → Reset + Abschlussmeldung. |

## `AppEffect`-Records (Wiedergabe)

Datei: `VideoPlayer.Mvu/AppEffect.cs`

| Record | Signatur | Zweck |
|--------|----------|-------|
| `NavigateToPlayerEffect` | leer :308 | Shell-Route `player` pushen. |
| `ResolvePlaybackSourceEffect` | `(string MediaType, long MediaId)` :319–321 | Quellauflösung. |
| `ReportPlaybackProgressEffect` | `(string MediaType, long MediaId, TimeSpan Position, TimeSpan Duration)` :334–338 | Fortschritt an Server/Warteschlange. |
| `TogglePictureInPictureEffect` | leer :345 | PiP umschalten. |
| `DeleteWatchedDownloadEffect` | `(string MediaType, long MediaId)` :435 | Download einer gesehenen Episode entfernen. |

## `DownloadManifestEntry` (Quelle `LocalFile`)

Datei: `VideoPlayer.Storage/Entities/DownloadManifestEntry.cs` — Tabelle `download_manifest`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `int` (PK, AutoIncrement) | Manifest-Id. |
| `VideoId` | `int` (indexed) | Id des Videos — Abgleich in `PlaybackSourceService.ResolveLocalFileAsync`. |
| `VideoType` | `string` | Medientyp (normalisiert verglichen). |
| `FilePath` | `string` | Pfad der Datei — wird `PlaybackSource.Location` bei `LocalFile`. |
| `State` | `string` | `queued`/`downloading`/`completed`/`failed`/`cancelled` — nur `completed` spielt lokal. |
| `Error` | `string?` | Fehlercode des letzten Fehlschlags. |
| `SizeBytes`, `DownloadedBytes`, `TotalBytes` | `long` | Größen/Fortschritt. |
| `Title` | `string` | Anzeigetitel. |
| `ShowId`, `ShowTitle`, `SeasonNumber`, `EpisodeNumber` | `long?`/`string?`/`int?` | Serienkontext (Episoden). |
| `UpdatedAtUtc` | `DateTime` | Letztes Update. |

## `CachedListEntry` (Offline-Fortsetzungsstand)

Datei: `VideoPlayer.Storage/Entities/CachedListEntry.cs` — Tabelle `cached_lists`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Key` | `string` (PK) | `ServerSyncService.ContinueWatchingCacheKey` für die Weiterschauen-Kopie. |
| `Json` | `string` | Serialisierte `List<ContinueWatchingEntry>`. |
| `UpdatedAtUtc` | `DateTime` | Letztes Update. |

## `ContinueWatchingEntry` (Fortsetzungsposition)

Datei: `VideoPlayer.ApiClient/Models/ContinueWatching.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `MediaType` | `string` | Medientyp (wird normalisiert verglichen). |
| `Entry` | `MediaEntry?` | Zugehöriger Medieneintrag — `Entry.Id` = Medien-Id. |
| `PositionSeconds` | `long` | Gemerkte Position — wird `PlaybackSource.ResumePosition` bei `> 0`. |
| `DurationSeconds` | `long?` | Dauer (nicht für Resume genutzt). |
| `Title`, `PosterPictureId` | `string`/`long?` | Anzeigedaten. |

## Serienkontext (Quelle der Fortsetzungsliste)

- `MediaDetailsState.Details` → `MediaDetailsView` (`VideoPlayer.Mvu/MediaDetailsView.cs`):
  `HasSeasons`, `Seasons` (`List<SeasonItem>` mit `Number`,
  `Episodes` — `EpisodeItem` mit `Id`, `Number`, `Title`, `WatchedAt`,
  `IsPlaceholder`, `IsWatched`). `NextEpisodeQueue` in `AppUpdate`
  (:1956–1978) bildet daraus `UpcomingEpisodes`; `OnEpisodePlayClicked`
  überspringt `IsPlaceholder`-Einträge.
