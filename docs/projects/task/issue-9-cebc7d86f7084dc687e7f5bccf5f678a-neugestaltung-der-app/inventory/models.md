# Lokale Datenmodelle und Enums (`VideoWebPlayer.Maui/Models/`)

SQLite-Modelle (Annotationen von `sqlite-net-pcl`) und Hilfsmodelle. Persistiert wird über `ClientDatabase` (`FileSystem.AppDataDirectory`, Profil-abhängiger Dateiname).

## `DownloadedVideo`

Datei: `VideoWebPlayer.Maui/Models/DownloadedVideo.cs` — Tabelle `Downloads`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `int` | `[PrimaryKey, AutoIncrement]` |
| `VideoId` | `long` | `[Indexed]` Server-ID des Videos |
| `VideoType` | `string` | `[Indexed]` `"movie"`/`"tvshowepisode"` (via `MediaTypes`) |
| `LocalFilePath` | `string` | Pfad der Videodatei unter `AppDataDirectory/videos[_profil]` |
| `FileSizeBytes` | `long` | Dateigröße |
| `RetentionType` | `DownloadRetentionType` | Cache / Download / Watchlist |
| `DownloadedAt` | `DateTime` | `[Indexed]` |
| `ExpiresAt` | `DateTime` | `[Indexed]` Ablauf für `CleanupExpiredDownloadsAsync` |
| `Title` | `string` | Anzeigetitel |
| `Plot`, `GenreNames`, `ReleaseYear` | `string?` | Metadaten |
| `EpisodeNumber`, `SeasonNumber` | `int?` | Episodeninfos |
| `TVShowName` | `string?` | Serienname |
| `LocalPosterImagePath`, `LocalBannerImagePath` | `string?` | Lokal gespeicherte Bilder (`AppDataDirectory/images`) |
| `MovieId`, `EpisodeId`, `SeasonId`, `TVShowId` | `long?` | Querverweise |
| `Status` | `DownloadStatus` | Queued/Downloading/Completed/Failed |
| `ProgressPercent` | `double` | Fortschritt |
| `PlaybackPositionSeconds` | `double` | Lokale Abspielposition |
| `DurationSeconds` | `double` | Gesamtdauer |

## `CachedCarouselItem`

Datei: `VideoWebPlayer.Maui/Models/CachedCarouselItem.cs` — Tabelle `CarouselCache`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `int` | `[PrimaryKey, AutoIncrement]` |
| `CarouselName` | `string` | `[Indexed]` zugehöriges Carousel |
| `SortOrder` | `int` | Position in der Liste |
| `EntryId` | `long` | Server-ID des Eintrags |
| `MediaType`, `Title`, `ImageUrl` | `string?` | Anzeigedaten |
| `PosterPictureId`, `SeasonId`, `EpisodeId` | `long?` | Bild-/Kontext-IDs |
| `CachedAt` | `DateTime` | Cache-Zeitpunkt |

## `VideoRequest` / `VideoSourceInfo`

Datei: `VideoWebPlayer.Maui/Models/VideoRequest.cs`

| Eigenschaft/Member | Typ | Zweck |
|--------------------|-----|-------|
| `VideoId`, `VideoType`, `Title` | `long`, `string`, `string` | Angefordertes Medium |
| `SourceInfo` | `VideoSourceInfo?` | Aufgelöste Quelle; `IsReady` wenn gesetzt |
| `SourceAvailable` | `event EventHandler<VideoSourceInfo>?` | Benachrichtigung bei Quellauflösung (von `MediaBannerPlayer` abonniert) |
| `SetSource(sourceInfo)` | `internal void` | Setzt Quelle und feuert Event |

`VideoSourceInfo`: `SourcePath` (`string`), `SourceType` (`VideoSourceType`), `ResumePosition?` (`TimeSpan`), `Duration?` (`TimeSpan`).

## `MediaTypes`

Datei: `VideoWebPlayer.Maui/Models/MediaTypes.cs` — statische Klasse

Konstanten, die den Server-Entity-Namen entsprechen: `Movie = "movie"`, `Episode = "tvshowepisode"`. Wird u. a. von `DownloadManager.NormalizeVideoType` verwendet.

## Enums

### `DownloadRetentionType` (`DownloadedVideo.cs`)

| Wert | Bedeutung |
|------|-----------|
| `Cache = 0` | Playback-Cache, Ablauf nach `PlaybackCacheRetentionDays` (Default 1 Tag) |
| `Download = 1` | Regulärer Download, Ablauf nach `DownloadRetentionDays` (Default 7 Tage) |
| `Watchlist = 2` | Weiterschauen-Download, Ablauf nach `WatchlistCacheRetentionDays` (Default 3 Tage) |

### `DownloadStatus` (`DownloadedVideo.cs`)

| Wert | Bedeutung |
|------|-----------|
| `Queued` | In Warteschlange |
| `Downloading` | Läuft |
| `Completed` | Abgeschlossen |
| `Failed` | Fehlgeschlagen |

### `VideoSourceType` (`VideoRequest.cs`)

| Wert | Bedeutung |
|------|-----------|
| `LocalFile` | Lokale Datei aus Download |
| `StreamUrl` | Server-Stream-URL |

### `ConnectionState` (`Services/IConnectionService.cs`)

| Wert | Bedeutung |
|------|-----------|
| `Connected` | Verbunden und angemeldet |
| `NeedsServerSetup` | Keine Serveradresse hinterlegt |
| `NeedsLogin` | Server erreichbar, aber keine gültigen Credentials |
| `Offline` | Server nicht erreichbar |

### `CarouselKind` (`ViewModels/MediaCarouselViewModel.cs`)

| Wert | Bedeutung |
|------|-----------|
| `ContinueWatching` | „Weiterschauen" |
| `Favorites` | „Favoriten" |
| `RecentEntries` | „Neu im Programm" |
| `Downloads` | „Heruntergeladene Videos" |
| `Other` | Sonstige |

### `RecentEntryType` (`VideoWebPlayer.Client/Models/DtoRecentEntry.cs`)

Werte: `Movie`, `MovieCollection`, `TVShow`, `TVShowSeason`, `TVShowEpisode` — Typ des `DtoRecentEntry`.

## Nicht vorhanden (für die Anforderung relevant)

- Kein Modell/Tabelle für unsynchronisierte Fortschrittsmeldungen (Outbox).
- Kein Download-Manifest-Modell.
- Kein Modell für bekannte Server (`KnownServers`).
