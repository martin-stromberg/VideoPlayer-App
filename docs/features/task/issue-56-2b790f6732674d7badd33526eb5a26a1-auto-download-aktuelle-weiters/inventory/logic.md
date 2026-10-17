# Logik — Bestandsaufnahme

## `AutoDownloadService`
Datei: `VideoPlayer.Mvu/AutoDownloadService.cs` (Z. 30–403)

`sealed`-Service des automatischen Download-Vorlaufs. Abhängigkeiten: `AuthSessionService` (`Client` = `VideoPlayerApiClient?`), `HomeListService`, `DownloadService`, `INetworkStatusMonitor`, `CacheDatabase`, injizierbare Zeitquelle `Func<DateTime> utcNow` (internal ctor, für Tests), `ILogger<AutoDownloadService>`.

Zustand/Konstanten:
- `public static readonly TimeSpan MinRunInterval = 60 s` (Z. 34) — Throttle zwischen zwei Läufen.
- `private const int PlaylistEntriesPageSize = 50` (Z. 37).
- `private DateTime lastRunUtc` / `private int runInProgress` (Z. 47–48) — Throttle-Stand bzw. Reentrancy-Sperre (`Interlocked.Exchange`).

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `AutoDownloadService(...)` (Z. 57–66) | public ctor | Produktiver Konstruktor (delegiert an den internen). |
| `AutoDownloadService(..., Func<DateTime>? utcNow, ...)` (Z. 76–92) | internal ctor | Konstruktor mit injizierbarer Zeitquelle (für Tests). |
| `RunAsync(AppSettings settings, bool force, CancellationToken)` (Z. 104–190) | public | Vorlauf-Lauf. Guards: `AutoDownloadEnabled` aus, `WifiOnlyDownloads && !IsWifi` oder `Client is null` → Rückkehr (Z. 107–112); parallele Läufe via `runInProgress` abgewehrt (Z. 116); ohne `force` unter `MinRunInterval` → Auslassung (Z. 123–128). Iteriert `homeLists.LoadHomeListsAsync().ContinueWatching` (Z. 131–133) und verzweigt auf den normalisierten `MediaType`. |
| — Film-Zweig (Z. 140–157) | Teil von `RunAsync` | `type == "movie"`: reiht den Film selbst ein (`EnqueueAsync("movie", item.Id, item.Title, …)`, mit `DownloadPlaylistContext(item.PlaylistId, item.PlaylistEntryId, null)` bei Playlist-Bindung); bei Playlist-Bindung zusätzlich `EnqueueNextPlaylistEntriesAsync` für die Folge-Einträge. Die Selbst-Einreihung zählt nicht gegen `PreloadEpisodeCount`. |
| — Episoden-Zweig (Z. 158–171) | Teil von `RunAsync` | `type == "tvshowepisode"`: bei Playlist-Bindung zuerst `EnqueueNextPlaylistEntriesAsync`; liefert diese `null` oder liegt keine Playlist-Bindung vor, greift bei `item.ShowId is > 0` der Serien-Pfad `EnqueueNextEpisodesAsync`. Einträge ohne `ShowId` und ohne Playlist-Treffer werden komplett übersprungen. |
| — Fehlerbehandlung (Z. 173–181) | Teil von `RunAsync` | `OperationCanceledException` wird durchgereicht; `ApiException`, `HttpRequestException`, `TaskCanceledException`, `InvalidOperationException` je Eintrag als Warning protokolliert, Lauf läuft weiter. Abschluss-Log „… {EnqueuedCount} Aufträge eingeplant." (Z. 184). |
| `EnqueueNextEpisodesAsync(HomeMediaItem, AppSettings, CancellationToken)` (Z. 201–254) | private | Serien-Pfad: `client.GetTvShowAsync(item.ShowId.Value)` → `ShowDetailsCache.WriteAsync(cache, show, …)`; baut die flache, staffelübergreifend geordnete Episodenliste (Staffel-, dann Episodennummer, Z. 218–221), sucht die aktuelle Episode per `episodes.FindIndex(e => e.Episode.Id == item.Id)` (Z. 222; `index < 0` → 0). Reiht `episodes.Skip(index + 1).Take(settings.PreloadEpisodeCount)` ein (Z. 230) — Titel `{showName} – {episode.Name}` bzw. Fallback `{showName} – Episode {episode.Number}`, Kontext `new DownloadSeriesContext(show.Id, showName, season.Number, episode.Number)`. Rückgabe: Anzahl der `NoticeEnqueued`-Ergebnisse. |
| `EnqueueNextPlaylistEntriesAsync(HomeMediaItem, AppSettings, CancellationToken)` (Z. 276–359) | private | Playlist-Pfad: Guards `Client is null`, `PlaylistId`/`PlaylistEntryId` nicht `> 0` → `null` (Z. 282–285). Seitenschleife ab Seite 1 solange `remaining = settings.PreloadEpisodeCount > 0`; Ladefehler der Seite (`ApiException`/`HttpRequestException`/`InvalidOperationException`/`JsonException`) → Warning + `null` (Serien-Rückfall, Z. 294–305). Einträge vor dem Fund des aktuellen Eintrags (`entry.Id == currentEntryId`) werden übersprungen (Z. 309–313); danach werden ladbare, zugängliche Einträge (`Normalize(entry.MediaType)` ∈ `movie`/`tvshowepisode` und `entry.IsAccessible`, Z. 320–324) mit `PlaylistEntryTitle(entry)`, `SeriesContextOf(entry)` und `new DownloadPlaylistContext(item.PlaylistId, entry.Id, entry.SortOrder)` eingeplant (Z. 326–336); `remaining--` je Kandidat unabhängig vom `Notice` (Z. 336). `!result.HasNextPage` beendet die Schleife (Z. 339–342). `!found` → `null` (Z. 345–348); sonst Anzahl eingeplanter Einträge. |
| `PlaylistEntryTitle(PlaylistEntry)` (Z. 368–376) | private static | Anzeigetitel: `{ParentMediaTitle} – {MediaTitle}` für Episoden mit Serien-Elternkontext, sonst `entry.MediaTitle`. |
| `SeriesContextOf(PlaylistEntry)` (Z. 386–402) | private static | Serienkontext aus den `ParentMedia*`-Feldern: `null` für Nicht-Episoden bzw. ohne jede Serienangabe; sonst `new DownloadSeriesContext(showId, showTitle, null, entry.EpisodeNumber)` (Staffelnummer offen). |

Abonnierte Events: keine (Dienst ist reaktiv — wird per `RunAutoDownloadsEffect` getriggert).
Publizierte Events: keine.

## `DownloadService`
Datei: `VideoPlayer.Mvu/DownloadService.cs` (Z. 60–1256; hier nur der für den Vorlauf relevante Ausschnitt)

`sealed`-Download-Verwaltung auf dem Manifest `download_manifest`; `MaxParallelDownloads = 2`. Hinweis-Konstanten (Z. 84–99): `NoticeAlreadyPresent`, `NoticeAlreadyRunning`, `NoticeAlreadyQueued`, `NoticeEnqueued`, `NoticeQuotaExceeded`, `NoticeInvalidMedia`; Fehlercodes u. a. `ErrorQuotaExceeded` = `"quota_exceeded"`.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `EnqueueAsync(string mediaType, long mediaId, string? title, AppSettings, DownloadSeriesContext?, DownloadPlaylistContext?, CancellationToken)` (Z. 244–385) | public | Einzige Einreihe-API — wird vom `AutoDownloadService` für Film, Folge-Episoden und Playlist-Einträge genutzt. Normalisiert den Typ; nicht `movie`/`tvshowepisode` oder `mediaId <= 0` → `NoticeInvalidMedia`-Fehler (Z. 297–305). Dedup unter `manifestGate`: bestehender Eintrag `downloading`+laufend → `NoticeAlreadyRunning`, `queued` → `NoticeAlreadyQueued`, `completed`+Datei → `NoticeAlreadyPresent` (Z. 311–326); fehlgeschlagen/abgebrochen/verwaist → Re-Enqueue mit Kontext-Übernahme (Z. 330–342); sonst Insert (Z. 346–357). Vorab-Quota (`GetUsedBytes() >= QuotaBytesFor(settings)`) → `failed`/`quota_exceeded` + `NoticeQuotaExceeded` (Z. 367–380). Danach `ItemChanged`, `PumpQueue`, `NoticeEnqueued` (Z. 382–384). |
| `ApplySeriesContext(DownloadManifestEntry, DownloadSeriesContext?)` (Z. 394–405) | private static | Überträgt `ShowId`/`ShowTitle`/`SeasonNumber`/`EpisodeNumber` nur bei `VideoType == "tvshowepisode"` und nur in leere Felder (`??=`). |
| `ApplyPlaylistContext(DownloadManifestEntry, DownloadPlaylistContext?)` (Z. 415 ff.) | private static | Überträgt `PlaylistId`/`PlaylistEntryId`/`PlaylistSortOrder` in leere Felder (`??=`); gilt für alle ladbaren Typen. |
| `GetUsedBytes()` (Z. 210–225) | public | Belegte Bytes des Video-Verzeichnisses (Quota-Grundlage). |

Publizierte Events: `ItemChanged` (`EventHandler<DownloadItemEventArgs>`), `ItemRemoved` (`EventHandler<DownloadRemovedEventArgs>`) — werden in `ShellAppEffectHandler` als `DownloadItemChangedMessage`/`DownloadDeletedMessage` in den MVU-Zyklus gespeist (Z. 1625–1629).

## `HomeListService`
Datei: `VideoPlayer.Mvu/HomeListService.cs` (Z. 19–344)

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `LoadHomeListsAsync(CancellationToken)` (Z. 48–75) | public | Liest `continue_watching`-/`recent`-Schlüssel aus `cached_lists` und Favoriten aus `cached_favorites`; liefert `HomeLists` inkl. `DiscardedCaches`. Wird von `AutoDownloadService.RunAsync` aufgerufen (Cache-Kopie, kein Serveraufruf). |
| `MapContinueWatching(ContinueWatchingEntry)` (Z. 249–274) | private static | Mappt den Cache-Eintrag auf `HomeMediaItem` inkl. `PlaylistId`/`PlaylistName`/`PlaylistEntryId` und der `MediaHints`-Werte. |
| `MediaHints(MediaEntry)` (Z. 325–333) | private static | Serienkontext-Tupel `(ShowId, SeasonId, EpisodeId)` aus dem eingebetteten Objektgraph (`TvShowEpisode.Season.Show.Id` usw.). |

## `ShowDetailsCache`
Datei: `VideoPlayer.Mvu/ShowDetailsCache.cs` (Z. 81–259)

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `KeyFor(long showId)` (Z. 91) | public static | Cache-Schlüssel `show:{id}`. |
| `WriteAsync(CacheDatabase, TvShow, ILogger?, CancellationToken)` (Z. 104–155) | public static | Schreibt die geordnete Serienstruktur (inkl. `WatchedAt`) in `cached_lists`; Schreibfehler nur Warning. Wird in `EnqueueNextEpisodesAsync` (Z. 216) bei jedem Serien-Abruf aufgerufen. |
| `ReadAsync(CacheDatabase, long showId, …)` (Z. 166–194) | public static | Liest den Cache-Eintrag (`null` bei Fehlen/Lesefehler). |
| `OrderedEpisodes(CachedShowEntry)` (Z. 203–210) | public static | Flache Episodenfolge nach Staffel-/Episodennummer — dieselbe Reihenfolge wie der Serien-Pfad des Vorlaufs. |
| `NextEpisodeAfter` / `NextEpisodeAfterId` (Z. 219–239) | public static | Folge-Episode einer Position bzw. Episoden-Id (Offline-„Weiterschauen"). |

## `MediaTypeNames`
Datei: `VideoPlayer.Mvu/MediaTypeNames.cs` (Z. 12–53)

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `Normalize(string?)` (Z. 17–28) | public static | Bildet Server- und CLR-Konventionen auf `movie`, `moviecollection`, `tvshow`, `tvshowseason`, `tvshowepisode` ab; der Vorlauf verzweigt auf `movie`/`tvshowepisode`. |
| `ToServerName(string?)` (Z. 41–52) | public static | Kanonische Server-Namen (`Movie`, `TVShowEpisode`, …) für Playlist-Endpunkte. |

## Verwendete API-Client-Methoden
Datei: `VideoPlayer.ApiClient/VideoPlayerApiClient.cs` — Zugriff über `AuthSessionService.Client` (`VideoPlayerApiClient?`, `AuthSessionService.cs` Z. 97).

| Methode | Zeilen | Zweck im Vorlauf |
|---------|--------|-------------------|
| `GetTvShowAsync(long id, CancellationToken)` | 427–431 | `GET api/items/tvshow/{id}` — Serienstruktur des Serien-Pfads. |
| `GetPlaylistEntriesPagedAsync(long playlistId, int pageNumber, int pageSize, CancellationToken)` | 598–611 | `GET api/playlists/{id}/entries/paged?pageNumber=…&pageSize=…` — Seiten des Playlist-Pfads. |

## Effekt-Verdrahtung

### `AppEffect`-Records
Datei: `VideoPlayer.Mvu/AppEffect.cs`

| Record | Zeilen | Parameter | Zweck |
|--------|--------|-----------|-------|
| `RunAutoDownloadsEffect` | 759 | `AppSettings Settings`, `bool Force` | Löst `AutoDownloadService.RunAsync` aus; `Force` umgeht den Throttle. |
| `DeleteWatchedDownloadEffect` | 769 | `string MediaType`, `long MediaId` | Entfernt den abgeschlossenen Download eines gesehenen Mediums. |

### Auslöser in `AppUpdate`
Datei: `VideoPlayer.Mvu/AppUpdate.cs`

| Stelle | Zeilen | Bedingung |
|--------|--------|-----------|
| `UpdateSaveSettingsRequested` | 339–357 (Effekt Z. 349) | `AutoDownloadEnabled` neu eingeschaltet und `ConnectionState.Connected` → `RunAutoDownloadsEffect(normalized, Force: true)`. |
| `UpdateSynchronizationCompleted` | ~1125–1142 (Effekt Z. 1138) | Nach Voll-Sync: `Connected` + `AutoDownloadEnabled` → `RunAutoDownloadsEffect(model.Settings, Force: false)`. |
| `UpdateCachedListsChanged` | ~1144–1159 (Effekt Z. 1155) | Nach Teil-Sync/Server-Ereignis: `Connected` + `AutoDownloadEnabled` → `RunAutoDownloadsEffect(model.Settings, Force: false)`. |
| `UpdatePlaybackProgress` | ~3820–3835 (Effekt Z. 3833) | „Angesehen"-Schwelle erreicht, `AutoDeleteWatchedEpisodes` und `MediaType == "tvshowepisode"` → `DeleteWatchedDownloadEffect`. |
| `UpdatePlaybackEnded` | ~3910–3929 (Effekt Z. 3928) | Wiedergabeende (idempotent) unter derselben Bedingung → `DeleteWatchedDownloadEffect`. |

### Effekt-Handler
Datei: `VideoPlayer.Maui/Services/ShellAppEffectHandler.cs`

| Methode | Zeilen | Kurzbeschreibung |
|---------|--------|------------------|
| `case RunAutoDownloadsEffect` (Dispatch-Switch) | 322–324 | Leitet an `RunAutoDownloadsAsync` weiter. |
| `case DeleteWatchedDownloadEffect` (Dispatch-Switch) | 325–327 | `DeleteDownloadAsync(MediaType, MediaId, …, completedOnly: true)` — entfernt nur abgeschlossene Downloads. |
| `RunAutoDownloadsAsync(RunAutoDownloadsEffect, CancellationToken)` | 1610–1623 | Ruft `autoDownloads.RunAsync(autoRun.Settings, autoRun.Force, cancellationToken)`; Fehler werden als Error protokolliert. |
| `OnDownloadItemChanged` / `OnDownloadItemRemoved` | 1625–1629 | Speisen `DownloadService`-Ereignisse als `DownloadItemChangedMessage`/`DownloadDeletedMessage` in den MVU-Zyklus. |
