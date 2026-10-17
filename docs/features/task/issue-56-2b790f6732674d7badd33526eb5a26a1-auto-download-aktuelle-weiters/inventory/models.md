# Datenmodell — Bestandsaufnahme

## `HomeMediaItem`
Datei: `VideoPlayer.Mvu/HomeMediaItem.cs` (Z. 9–86)

Anzeigefertiger Eintrag der Startseiten-Listen; wird von `HomeListService` aus den gecachten Serverdaten gemappt und von `AutoDownloadService.RunAsync` je Weiterschauen-Eintrag gelesen.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `long` (required) | Id des Mediums — bei Episoden-Einträgen die Id der aktuell abspielbaren Episode (Dedup-Schlüssel und `mediaId` für `EnqueueAsync`). |
| `MediaType` | `string` (required) | Medientyp kleingeschrieben; Server-Konvention der Weiterschauen-Liste (`movie`, `episode`, …) — wird in `RunAsync` via `MediaTypeNames.Normalize` auf `movie`/`tvshowepisode` geprüft. |
| `Title` | `string` (required) | Anzeigetitel des Eintrags (Fallback-Titel beim Film-Selbst-Download). |
| `PosterPictureId` | `long?` | Bild-Id für `api/pictures/{id}`. |
| `Progress` | `double?` | Wiedergabefortschritt 0–1 (nur Weiterschauen-Liste). |
| `ShowId` | `long?` | Serien-Id aus dem eingebetteten Objektgraph (`TvShowEpisode.Season.Show.Id`); steuert, ob der Serien-Pfad greift (`item.ShowId is > 0`). |
| `SeasonId` | `long?` | Staffel-Id (Episoden-/Staffel-Einträge). |
| `EpisodeId` | `long?` | Episoden-Id bei Episoden-Einträgen. |
| `PlaylistId` | `long?` | Playlist-Id des Wiedergabekontexts; zusammen mit `PlaylistEntryId` die Playlist-Bindung (`is > 0`). |
| `PlaylistName` | `string?` | Name der Playlist. |
| `PlaylistEntryId` | `long?` | Id des aktuellen Playlist-Eintrags — Suchkriterium in `EnqueueNextPlaylistEntriesAsync` (`entry.Id == item.PlaylistEntryId`). |
| `IsPlaceholder` | `bool` | Offline-Platzhalter (nicht abspielbar). |
| `IsSelectable`/`CardOpacity`/`HasPoster`/`ShowProgress` | abgeleitet | Anzeige-Properties (für den Vorlauf nicht relevant). |

## `HomeLists`
Datei: `VideoPlayer.Mvu/HomeLists.cs` (Z. 7–28)

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `ContinueWatching` | `IReadOnlyList<HomeMediaItem>` | Weiterschauen-Liste — Iterationsgrundlage des Vorlaufs (`lists.ContinueWatching`). |
| `Favorites` | `IReadOnlyList<HomeMediaItem>` | Favoriten-Liste (vom Vorlauf nicht genutzt). |
| `Recent` | `IReadOnlyList<HomeMediaItem>` | „Neu im Programm" (vom Vorlauf nicht genutzt). |
| `DiscardedCaches` | `IReadOnlyList<string>` | Als beschädigt verworfene Cache-Schlüssel. |
| `Empty` | `static readonly HomeLists` | Leeres Ergebnis. |

## `ContinueWatchingEntry`
Datei: `VideoPlayer.ApiClient/Models/ContinueWatching.cs` (Z. 6–43)

Persistierte Server-Antwort der Weiterschauen-Liste (Cache-Schlüssel `ServerSyncService.ContinueWatchingCacheKey` = `"continue_watching"`); Quelle der `HomeMediaItem`-Werte.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `MediaType` | `string` | Server-Medientyp (`movie`, `episode`, …). |
| `Entry` | `MediaEntry?` | Eingebettetes Medium (`Movie`, `TvShowEpisode`, …) — liefert `Id`, `Name` und den Serien-Objektgraph. |
| `PositionSeconds` | `long` | Gemerkte Wiedergabeposition. |
| `DurationSeconds` | `long?` | Gesamtdauer. |
| `Title` | `string` | Anzeigetitel. |
| `PosterPictureId` | `long?` | Posterbild-Id. |
| `PlaylistId` | `long?` | Playlist-Herkunft des Eintrags. |
| `PlaylistName` | `string?` | Name der Playlist. |
| `PlaylistEntryId` | `long?` | Id des Playlist-Eintrags, dem das Medium aktuell entspricht; `null` ohne Playlist-Bezug oder wenn nicht mehr Teil der Playlist. |

## `MediaEntry`-Hierarchie (`TvShow`, `TvShowSeason`, `TvShowEpisode`)
Datei: `VideoPlayer.ApiClient/Models/MediaEntry.cs`

JSON-polymorphe Hierarchie (`$type`: `movie`, `movieCollection`, `show`, `season`, `episode`). Für den Vorlauf relevant:

| Klasse | Zeilen | Für den Vorlauf genutzte Eigenschaften |
|--------|--------|----------------------------------------|
| `MediaEntry` (Basis) | 16–65 | `Id`, `Name`, `PosterPictureId`, `WatchedAt` u. a. |
| `Movie` | 70–80 | erbt `Id`/`Name` (Film-Selbst-Download). |
| `TvShow` | 94–104 | `Seasons` (Staffelliste der Serienstruktur), `Name` (`showName` für Titel- und `DownloadSeriesContext`-Aufbau). |
| `TvShowSeason` | 109–119 | `Number` (Staffelnummer, Sortier- und Kontext-Wert), `Episodes`, `Show` (Rückverweis → `ShowId`-Auflösung in `HomeListService.MediaHints`). |
| `TvShowEpisode` | 124–137 | `Season` (Rückverweis), `Number` (Episodennummer, Sortier- und Kontext-Wert), `Name` (Episodentitel). |

## `PlaylistEntry`
Datei: `VideoPlayer.ApiClient/Models/PlaylistEntry.cs` (Z. 8–81)

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `long` | Eintrags-Id — Abgleich mit `HomeMediaItem.PlaylistEntryId`. |
| `PlaylistId` | `long` | Zugehörige Playlist. |
| `MediaType` | `string` | Referenztyp in Server-Konvention (`Movie`, `TVShowEpisode`, …) — Ladbarkeitsfilter über `Normalize`. |
| `MediaId` | `long` | Id des referenzierten Mediums (`mediaId` für `EnqueueAsync`). |
| `MediaTitle` | `string` | Anzeigetitel des Mediums (Basis von `PlaylistEntryTitle`). |
| `ParentMediaType` | `string?` | Typ des Elternelements (`TVShow` bei Episoden). |
| `ParentMediaId` | `long?` | Eltern-Id (Serien-Hint für `SeriesContextOf`). |
| `ParentMediaTitle` | `string?` | Eltern-Titel (Serien-Hint, „Serie – Episode"-Titel). |
| `AddedAt` | `DateTime` | Hinzufüge-Zeitpunkt. |
| `ResolvedPictureId` | `long?` | Serverseitig aufgelöste Bild-Id. |
| `IsAccessible` | `bool` | Zugriffsrecht — Ladbarkeitsfilter des Playlist-Pfads. |
| `SortOrder` | `long?` | Manuelle Sortierposition (→ `DownloadPlaylistContext.PlaylistSortOrder`). |
| `ReleaseDate` | `DateTime?` | Erscheinungsdatum. |
| `Plot` | `string?` | Handlungsbeschreibung. |
| `EpisodeNumber` | `int?` | Episodennummer bei `TVShowEpisode`-Einträgen (→ `DownloadSeriesContext.EpisodeNumber`). |

## `PlaylistEntriesPagedResult`
Datei: `VideoPlayer.ApiClient/Models/Playlist.cs` (Z. 84–100)

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Entries` | `PlaylistEntry[]` | Einträge der Seite in Server-Reihenfolge. |
| `TotalCount` | `int` | Gesamtzahl über alle Seiten. |
| `HasNextPage` | `bool` | Steuert die Seitenschleife des Playlist-Pfads. |
| `PageNumber` | `int` | 1-basierte Seitennummer. |
| `PageSize` | `int` | Einträge pro Seite (Vorlauf nutzt `PlaylistEntriesPageSize = 50`). |

## `AppSettings`
Datei: `VideoPlayer.Storage/AppSettings.cs` (Z. 9–68)

Unveränderlicher Record (`with`-Kopien in den Tests möglich). Für die Anforderung relevante Felder:

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `AutoDownloadEnabled` | `bool` | Schalter des Vorlaufs (Guard in `RunAsync` und der `AppUpdate`-Trigger). |
| `PreloadEpisodeCount` | `int` (Default 3) | „Anzahl vorzuladender Episoden" — Budget des Vorlaufs je Weiterschauen-Eintrag; `Normalize` klammert auf 1–10. Heute nur Folge-Episoden. |
| `WifiOnlyDownloads` | `bool` (Default `true`) | WLAN-Bedingung (Guard in `RunAsync` über `INetworkStatusMonitor.IsWifi`, beim Transfer über `DownloadService`). |
| `DownloadQuotaGb` | `int` (Default `DefaultDownloadQuotaGb` = 5) | Speicherkontingent, durch `DownloadService` durchgesetzt. |
| `AutoDeleteWatchedEpisodes` | `bool` | Schalter des `DeleteWatchedDownloadEffect`-Pfads. |
| `AutoResumeEnabled`, `AutoResumeRemainingSeconds`, `WatchedEndWindowSeconds`, `HomeHeroEnabled`, `LoggingEnabled` | — | übrige Einstellungen (für die Anforderung nicht relevant). |

## `DownloadSeriesContext`
Datei: `VideoPlayer.Mvu/DownloadSeriesContext.cs` (Z. 16–20)

| Eigenschaft (Record-Parameter) | Typ | Beschreibung / Zweck |
|--------------------------------|-----|----------------------|
| `ShowId` | `long?` | Serien-Id des Episoden-Downloads. |
| `ShowTitle` | `string?` | Anzeigetitel der Serie. |
| `SeasonNumber` | `int?` | Staffelnummer (Playlist-Pfad lässt sie offen). |
| `EpisodeNumber` | `int?` | Episodennummer. |

Grundlage des Offline-„Weiterschauen" (`OfflineContinueWatchingService`); wird von `DownloadService.ApplySeriesContext` in `DownloadManifestEntry` übernommen.

## `DownloadPlaylistContext`
Datei: `VideoPlayer.Mvu/DownloadPlaylistContext.cs` (Z. 17–20)

| Eigenschaft (Record-Parameter) | Typ | Beschreibung / Zweck |
|--------------------------------|-----|----------------------|
| `PlaylistId` | `long?` | Playlist-Herkunft des Downloads. |
| `PlaylistEntryId` | `long?` | Playlist-Eintrags-Id des Mediums. |
| `PlaylistSortOrder` | `long?` | Server-Sortierposition; `null` bei `ByReleaseDate`. |

Wird von `DownloadService.ApplyPlaylistContext` in `DownloadManifestEntry` übernommen; die Film-Einreihung nutzt heute `new DownloadPlaylistContext(item.PlaylistId, item.PlaylistEntryId, null)`.

## `DownloadManifestEntry`
Datei: `VideoPlayer.Storage/Entities/DownloadManifestEntry.cs` (Z. 11–88)

SQLite-Tabelle `download_manifest` — fachlich relevante Spalten:

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `int` (PK, AutoIncrement) | Manifest-Reihenfolge = Einplanungs-/FIFO-Reihenfolge. |
| `VideoId` | `int` (indiziert) | Medien-Id (Dedup-Schlüssel zusammen mit `VideoType`). |
| `VideoType` | `string` | Normalisierter Medientyp (`movie`/`tvshowepisode`). |
| `FilePath`, `SizeBytes`, `DownloadedBytes`, `TotalBytes`, `Error`, `State`, `UpdatedAtUtc`, `Title` | — | Transfer-/Anzeige-Zustand; `State` persistiert `DownloadStateNames`-Strings. |
| `ShowId`, `ShowTitle`, `SeasonNumber`, `EpisodeNumber` | `long?`/`string?`/`int?` | Serienkontext des Episoden-Downloads (aus `DownloadSeriesContext`). |
| `PlaylistId`, `PlaylistEntryId`, `PlaylistSortOrder` | `long?` | Playlist-Bezug (aus `DownloadPlaylistContext`). |

## `DownloadEnqueueResult`
Datei: `VideoPlayer.Mvu/DownloadService.cs` (Z. 31–41)

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Item` | `DownloadListItem?` | Anzeige-Snapshot des Manifest-Eintrags; `null` bei ungültiger Anforderung. |
| `Notice` | `string?` | Hinweistext — der Vorlauf zählt nur `DownloadService.NoticeEnqueued` als eingeplant. |
| `IsError` | `bool` | Kennzeichnet Ablehnung/Fehler-Hinweis. |

## Cache-Records der Serienstruktur
Datei: `VideoPlayer.Mvu/ShowDetailsCache.cs`

| Record | Zeilen | Eigenschaften / Parameter | Zweck |
|--------|--------|--------------------------|-------|
| `CachedShowEpisode` | 12–29 | `Id`, `Number`, `Title`, `WatchedAt` | Gecachte Episode (Offline-Serienkontext). |
| `CachedShowSeason` | 34–44 | `Id`, `Number`, `Episodes` | Gecachte Staffel. |
| `ShowEpisodeRef` | 53 | `SeasonNumber`, `Episode` | Positionsverweis in der geordneten Serienfolge. |
| `CachedShowEntry` | 61–71 | `ShowId`, `Title`, `Seasons` | Gecachte Serienstruktur (`cached_lists`-Schlüssel `show:{id}`). |
