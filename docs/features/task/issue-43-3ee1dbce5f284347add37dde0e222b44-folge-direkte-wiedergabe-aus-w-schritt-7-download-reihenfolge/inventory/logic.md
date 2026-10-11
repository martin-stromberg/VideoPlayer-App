# Bestandsaufnahme: Logik

## `AutoDownloadService`

Datei: `VideoPlayer.Mvu/AutoDownloadService.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `RunAsync(settings, force, ct)` | public | Vorlauf-Lauf: Weiterschauen-Liste aus dem Cache (`HomeListService.LoadHomeListsAsync`); `movie` → Film selbst einreihen (Z. 119–122); `tvshowepisode` mit `ShowId > 0` → `EnqueueNextEpisodesAsync` (Z. 123–126). `PlaylistId`/`PlaylistEntryId` des `HomeMediaItem` werden **nicht ausgewertet**. Fehler je Eintrag werden protokolliert und übersprungen. |
| `EnqueueNextEpisodesAsync(item, settings, ct)` | private | Lädt Serie via `client.GetTvShowAsync(item.ShowId)`, schreibt `ShowDetailsCache`, ordnet Episoden staffelübergreifend (`Season.Number`/`Episode.Number`), reiht die `PreloadEpisodeCount` Folge-Episoden nach `item.Id` mit `DownloadSeriesContext` ein (Z. 154–192). |

Wird aufgerufen vom `RunAutoDownloadsEffect`-Handler (`ShellAppEffectHandler`), Trigger: Einschalten der Einstellung (`AppUpdate.cs` Z. 334), Sync-/Cache-Ereignisse (Z. 1123 `UpdateSyncCompleted`-Kontext, Z. 1140 `UpdateCachedListsChanged`), gedrosselt auf `MinRunInterval` = 60 s.

**Befund:** Der Vorlauf holt seine Folge-Episoden ausschließlich aus dem Serienkontext — Playlist-Bezug ist weder Eingangskriterium noch Reihenfolgequelle.

## `DownloadService`

Datei: `VideoPlayer.Mvu/DownloadService.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `EnqueueAsync(mediaType, mediaId, title, settings, seriesContext, ct)` | public | Dedup pro Medium, Quota-Vorabprüfung, Manifest-Insert/-Update, `ApplySeriesContext`, `PumpQueue` (Z. 224–316). |
| `ApplySeriesContext(entry, seriesContext)` | private static | Überträgt Serienkontext nur für `tvshowepisode` und nur in leere Felder (`??=`, Z. 325–336). |
| `NextQueuedEntryAsync()` | private | Ältester `queued`-Eintrag: `OrderBy(e => e.Id)` = Einreihe-Reihenfolge = **FIFO** (Z. 545–553). |
| `PumpQueueAsync(settings)` / `PumpQueue` | private | Startet wartende Aufträge bis `MaxParallelDownloads = 2`; WLAN-Bedingung; Quota-Prüfung (Z. 484–543). |
| `ResumeInterruptedAsync(settings, ct)` | public | Requeue wartender/verwaister/nicht-Quota-fehlgeschlagener Aufträge — Reihenfolge bleibt die ursprüngliche Manifest-Id (Z. 437–482). |
| `LoadItemsAsync(ct)` | public | Manifest in `Id`-Reihenfolge als `DownloadListItem`-Snapshots (Z. 177–184). |
| `CancelAsync` / `DeleteAsync` / `DeleteIfCompletedAsync` | public | Abbruch/Löschung; `DeleteIfCompletedAsync` für Auto-Löschen gesehener Episoden. |
| `ItemChanged` / `ItemRemoved` | event | Änderungsbenachrichtigungen → `DownloadItemChangedMessage` im MVU-Zyklus. |

Aufrufer von `EnqueueAsync`: `AutoDownloadService` (Vorlauf) und `ShellAppEffectHandler.EnqueueDownloadAsync` (`EnqueueDownloadEffect` — Detailseite „Herunterladen" und Wiederholen auf der Downloads-Seite).

## `CacheDatabase`

Datei: `VideoPlayer.Storage/CacheDatabase.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `InitializeAsync()` | public | `CreateTableAsync` je Entität + `EnsureColumnAsync` für nachträglich ergänzte `download_manifest`-Spalten (`ShowId`, `ShowTitle`, `SeasonNumber`, `EpisodeNumber` — Z. 40–43). Etabliertes Muster für upgrade-sichere Schema-Erweiterung. |
| `EnsureColumnAsync(table, column, columnDefinition)` | private | `PRAGMA table_info` prüfen, fehlende Spalte per `ALTER TABLE … ADD COLUMN` nachziehen (Z. 115–124). |
| `InitializeWithRecoveryAsync()` | public | Bei beschädigter Datei: beiseitelegen + neu anlegen. |

## `PlaylistService`

Datei: `VideoPlayer.Mvu/PlaylistService.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `LoadEntriesAsync(playlistId, page, ct)` | public | `api/playlists/{id}/entries/paged` (Seitengröße `PlaylistDetailsState.PageSize` = 20), Einträge in Server-Reihenfolge, ungültige Einträge werden übersprungen, Mapping auf `PlaylistEntryItem` (Z. 160–199). |
| `StartPlaybackAsync` / `GetNextEntryAsync` / `GetPreviousEntryAsync` / `AdvanceAsync` | public | Playlist-Wiedergabe-Endpunkte (`play`, `play/next|previous|advance`) — irrelevant für den Download-Pfad. |

## `HomeListService`

Datei: `VideoPlayer.Mvu/HomeListService.cs`

`MapContinueWatching` (Z. 244–269) überträgt `PlaylistId`/`PlaylistName`/`PlaylistEntryId` des `ContinueWatchingEntry` in das `HomeMediaItem` — der Playlist-Bezug steht dem Auto-Download-Pfad bereits zur Verfügung.

## `AppUpdate` (Download-/Vorlauf-relevante Pfade)

Datei: `VideoPlayer.Mvu/AppUpdate.cs`

| Methode | Kurzbeschreibung |
|---------|------------------|
| `UpdateDownloadRequested` (Z. 4056–4089) | `DownloadRequestedMessage` → `EnqueueDownloadEffect` mit Serien-Hints (`ShowId`, `ShowTitle`, `SeasonNumber`, `EpisodeNumber`); kein Playlist-Feld. Nur bei `ConnectionState.Connected`. |
| `UpdateDownloadRetryRequested` (Z. 4151–4158) | Wiederholen → `EnqueueDownloadEffect` ohne Kontext-Hints. |
| `UpdateSettingsSaved` (Z. ~334) | `RunAutoDownloadsEffect(Force: true)` beim Einschalten von `AutoDownloadEnabled`. |
| `UpdateCachedListsChanged` / Sync-Abschluss (Z. 1123, 1140) | `RunAutoDownloadsEffect(Force: false)` nach Listen-/Cache-Änderungen. |

## `ShellAppEffectHandler`

Datei: `VideoPlayer.Maui/Services/ShellAppEffectHandler.cs`

`EnqueueDownloadAsync` (Z. 1442–1451): baut `DownloadSeriesContext` aus den Effekt-Feldern und ruft `downloads.EnqueueAsync`; `RunAutoDownloadsEffect` → `autoDownloads.RunAsync`.

## `OfflineContinueWatchingService`

Datei: `VideoPlayer.Mvu/OfflineContinueWatchingService.cs`

Bildet die Offline-„Weiterschauen" aus dem Manifest: Folge-Episode aus `ShowDetailsCache` anhand `ShowId`/`SeasonNumber`/`EpisodeNumber` des Eintrags (Z. 137–195) — Serienkontext-Pfad, kein Playlist-Bezug (Playlists sind Online-Feature; kein Offline-Kontext gefordert).
