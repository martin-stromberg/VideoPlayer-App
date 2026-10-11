# Bestandsaufnahme: Datenmodell

## `DownloadManifestEntry`

Datei: `VideoPlayer.Storage/Entities/DownloadManifestEntry.cs`

Persistenter Download-Auftrag (Tabelle `download_manifest` in `videoplayer-cache.db3`, sqlite-net). **Kein Playlist-Feld vorhanden.**

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `int` | Primärschlüssel, `AutoIncrement` — definiert die FIFO-Warteschlangenreihenfolge (`OrderBy(e => e.Id)`) |
| `VideoId` | `int` | Id des Mediums (indiziert) |
| `VideoType` | `string` | Medientyp (`movie`, `tvshowepisode`) |
| `FilePath` | `string` | Pfad der lokalen Videodatei |
| `SizeBytes` | `long` | Dateigröße bei abgeschlossenen Downloads |
| `Title` | `string` | Anzeigetitel |
| `DownloadedBytes` / `TotalBytes` | `long` | Fortschritt / erwartete Gesamtgröße |
| `Error` | `string?` | Maschineller Fehlercode |
| `State` | `string` | `queued`/`downloading`/`completed`/`failed`/`cancelled` |
| `ShowId` / `ShowTitle` | `long?` / `string?` | Serienkontext (nullable, nachträglich per `EnsureColumnAsync` ergänzt) |
| `SeasonNumber` / `EpisodeNumber` | `int?` / `int?` | Serienkontext (nullable) |
| `UpdatedAtUtc` | `DateTime` | Letzte Aktualisierung |

## `DownloadSeriesContext`

Datei: `VideoPlayer.Mvu/DownloadSeriesContext.cs`

Record mit dem Serienkontext eines Episoden-Downloads: `ShowId`, `ShowTitle`, `SeasonNumber`, `EpisodeNumber` (alle nullable). Wird von `EnqueueAsync`/`EnqueueDownloadEffect`/`DownloadRequestedMessage` durchgereicht und via `DownloadService.ApplySeriesContext` ins Manifest geschrieben (nur Episoden, nur fehlende Werte — `??=`).

## `ContinueWatchingEntry` (API-DTO)

Datei: `VideoPlayer.ApiClient/Models/ContinueWatching.cs`

Trägt seit Schritt 5: `PlaylistId` (`long?`), `PlaylistName` (`string?`), `PlaylistEntryId` (`long?` — Id des Playlist-Eintrags; `null`, wenn das Medium nicht mehr Teil der Playlist ist). Wird JSON-serialisiert in `cached_lists` abgelegt (Cache-Kopie der Weiterschauen-Liste).

## `HomeMediaItem`

Datei: `VideoPlayer.Mvu/HomeMediaItem.cs`

Übernimmt `PlaylistId`, `PlaylistName`, `PlaylistEntryId` aus dem DTO (`HomeListService.MapContinueWatching`, Z. 265–267) — steht dem `AutoDownloadService` damit bereits zur Verfügung.

## `PlaylistEntry` (API-DTO)

Datei: `VideoPlayer.ApiClient/Models/PlaylistEntry.cs`

| Eigenschaft | Typ | Bedeutung |
|-------------|-----|-----------|
| `Id` | `long` | Id des Playlist-Eintrags (= `PlaylistEntryId` der Weiterschauen) |
| `PlaylistId` | `long` | Zugehörige Playlist |
| `MediaType` / `MediaId` / `MediaTitle` | `string` / `long` / `string` | Referenziertes Medium (Server-Konvention: `Movie`, `TVShowEpisode`, …) |
| `ParentMediaType` / `ParentMediaId` / `ParentMediaTitle` | `string?` / `long?` / `string?` | Elternkontext (bei Episoden die Serie/Staffel — in `AppUpdate.cs` Z. 2380–2381 als `tvshow`→ShowId / `tvshowseason`→SeasonId gelesen) |
| `IsAccessible` | `bool` | Zugriffsrecht des Benutzers |
| `SortOrder` | `long?` | Manuelle Sortierung; `null` bei `ByReleaseDate` |
| `EpisodeNumber` | `int?` | Episodennummer (nur Episoden-Einträge) |
| `ReleaseDate` | `DateTime?` | Erscheinungsdatum |

## `PlaylistEntriesPagedResult` (API-DTO)

Datei: `VideoPlayer.ApiClient/Models/Playlist.cs` (Z. 84)

`Entries` (`PlaylistEntry[]`, Server-Reihenfolge), `TotalCount`, `HasNextPage`, `PageNumber`, `PageSize`.

## `PlaylistEntryItem` (MVU-Anzeige-Record)

Datei: `VideoPlayer.Mvu/PlaylistDetailsState.cs`

`EntryId`, `MediaId`, `MediaType` (normalisiert, CLR-Konvention), `Title`, `ParentMediaType`/`ParentMediaId`/`ParentMediaTitle` (normalisiert), `IsAccessible`, `SortOrder`, `EpisodeNumber`, `ResolvedPictureId`, `ReleaseDate`. `PlaylistDetailsState.PageSize = 20`.
