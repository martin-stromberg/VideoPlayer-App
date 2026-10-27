# Logik

## `DownloadService`
Datei: `VideoPlayer.Mvu/DownloadService.cs`

Download-Verwaltung: führt Aufträge als Zeilen des `download_manifest` aus, legt Dateien unter `AppStorageLayout.VideosDirectory` ab (Dateinamens-Muster `{typ}_{mediaId}.mp4`), bis zu `MaxParallelDownloads = 2` parallele Transfers, FIFO-Warteschlange in Manifest-Reihenfolge. Fehlercodes (`ErrorQuotaExceeded`, `ErrorNoConnection`, `ErrorNetwork`, `ErrorIo`, `ErrorHttpPrefix`, `ErrorMissingFile`) und Hinweistexte (`NoticeAlreadyPresent`, `NoticeAlreadyRunning`, `NoticeAlreadyQueued`, `NoticeEnqueued`, `NoticeQuotaExceeded`, `NoticeInvalidMedia`) als Konstanten. Interne Synchronisation über `manifestGate` (Manifest-Schreibzugriffe) und `pumpGate` (Warteschlangen-Pumpe); laufende Transfers in `runningTransfers` (`{typ}:{mediaId}` → `CancellationTokenSource`).

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `LoadItemsAsync` | public | Alle Manifest-Einträge als `DownloadListItem`-Snapshots in Auftragsreihenfolge |
| `GetUsedBytes` | public | Belegter Download-Speicher (Summe der Dateigrößen im Video-Verzeichnis, inkl. Restdaten) |
| `EnqueueAsync` | public | Download einreihen bzw. fehlgeschlagenen/abgebrochenen Auftrag zurücksetzen; Dedup (laufend → `AlreadyRunning`, wartend → `AlreadyQueued`, fertig mit Datei → `AlreadyPresent`); Vorab-Quota-Prüfung → `failed`/`quota_exceeded`; Re-Enqueue behält gespeicherte Kontextdaten (Serien-/Playlist-Kontext, `PictureId` — `??=`-Semantik, wird nicht zurückgesetzt). Parameter: `mediaType`, `mediaId`, `title`, `settings`, `seriesContext`, `playlistContext`, `pictureId`, `cancellationToken` — **kein Herkunftsparameter** |
| `CancelAsync` | public | Wartenden/laufenden Download abbrechen (`cancelled`, laufender Transfer meldet sich selbst) |
| `DeleteAsync` | public | Download vollständig entfernen: Transfer abbrechen, Datei löschen, Manifest-Zeile löschen, `ItemRemoved` melden |
| `DeleteIfCompletedAsync` | public | Wie `DeleteAsync`, aber nur bei Zustand `completed` — Träger der Sofortlöschung gesehener Episoden (`DeleteWatchedDownloadEffect`) |
| `ResumeInterruptedAsync` | public | Wartende/verwaist laufende Aufträge und Nicht-Quota-Fehlschläge erneut einplanen (Transfer startet ab Byte 0); `cancelled` und `quota_exceeded` bleiben unberührt |
| `CompleteExternalTransferAsync` | public | Abschluss eines systemseitig geführten Transfers (iOS-Hintergrund-Session nach App-Neustart): Temp-Datei an den Zielpfad kopieren, `completed` bzw. `failed` setzen |
| `DeleteAllAsync` | public | Alle Downloads entfernen (Transfers abbrechen, Dateien + Manifest leeren), `ItemRemoved` je Eintrag |
| `DeleteFailedAsync` | public | Alle `failed`/`cancelled`-Einträge entfernen (Dateien + Zeilen), `ItemRemoved` je Eintrag |
| `PurgeOrphansAsync` | public | Dateien ohne Manifest-Bezug löschen (Abgleich über Dateinamen) |
| `ReconcileAsync` | public | Abgleich Ablageort ↔ Manifest: überholte Teildateien nicht-abgeschlossener/nicht laufender Einträge löschen; Dateien ohne Manifest-Eintrag als Restdaten zählen (nicht löschen); veraltete `FilePath`-Verzeichnisanteile heilen; `completed`-Einträge ohne Datei → `failed`/`missing_file`. Liefert `DownloadReconcileResult`. **Keine Ablauf-/Herkunfts-Prüfung** |
| `PumpQueueAsync`/`PumpQueue` | private | Warteschlangen-Pumpe: WLAN-Bedingung (`WifiOnlyDownloads && !IsWifi` → wartet), bis zu 2 parallele Transfers starten, Quota-Vorab-Prüfung |
| `NextQueuedEntryAsync` | private | Ältester `queued`-Eintrag ohne laufenden Transfer |
| `TransferAsync` | private | Transfer-Ausführung: Stream-URL mit Token bauen, `IDownloadBackend` (Fallback `HttpDownloadBackend`), Fortschritt gedrosselt ins Manifest, laufende Quota-Prüfung (Abbruch → `quota_exceeded`), Ergebnis `completed`/`failed`/`cancelled` über `SetStateIfPresentAsync` |
| `ReportProgressThrottledAsync` | private | Fortschritt max. alle 250 ms ins Manifest + `ItemChanged` |
| `SetStateAsync`/`SetStateIfPresentAsync` | private | Zustandswechsel persistieren (`Error`, `UpdatedAtUtc` mitsetzen), protokollieren, `ItemChanged`; `IfPresent` bricht ab, wenn der Eintrag zwischenzeitlich gelöscht wurde |
| `FindEntryAsync` | private | Manifest-Eintrag über `VideoId` + normalisierten `VideoType` suchen |
| `BuildStreamUrlAsync` | private | `api/items/{typ}/{id}/stream?access_token=…` aus `AuthSessionService`/`AppSecretsService` |
| `QuotaBytesFor`/`FilePathFor`/`Key`/`TryDeleteFile` | private | Quota-Umrechnung (GB → Bytes, `quotaBytesOverride` für Tests), Dateinamens-Muster, Transfer-Schlüssel, fehlertolerantes Dateilöschen |
| `ToItem` | private | `DownloadManifestEntry` → `DownloadListItem` (Mapping inkl. Fehler-/Titel-Fallbacks) |
| `RaiseItemChanged`/`RaiseItemRemoved` | private | Ereignis-Versand (ausnahmetolerant) |
| `ApplySeriesContext`/`ApplyPlaylistContext` | private | Kontextdaten in den Eintrag übernehmen — nur fehlende Werte (`??=`); Serienkontext nur bei `tvshowepisode` |

Publizierte Events: `ItemChanged` (`DownloadItemEventArgs`) bei jedem Zustands-/Fortschrittswechsel; `ItemRemoved` (`DownloadRemovedEventArgs`) bei jeder Entfernung eines Manifest-Eintrags (`DeleteAsync`, `DeleteIfCompletedAsync`, `DeleteAllAsync`, `DeleteFailedAsync`).

Abonnierte Events: keine (wird über Methodenaufrufe gesteuert; `INetworkStatusMonitor` wird nur lesend für die WLAN-Bedingung genutzt).

## `AutoDownloadService`
Datei: `VideoPlayer.Mvu/AutoDownloadService.cs`

Automatischer Vorlauf der Weiterschauen-Liste: je Eintrag bis zu `PreloadEpisodeCount` Kandidaten (aktueller Titel eingeschlossen). Playlist-gebundene Einträge (`HomeMediaItem.PlaylistId` + `PlaylistEntryId`) folgen der Server-Playlist-Reihenfolge, übrige Episoden dem Serienkontext; Filme laden sich selbst (bei Playlist-Bezug plus Folge-Einträge im selben Budget). Throttle `MinRunInterval` = 60 s; kein Parallel-Lauf (`runInProgress`-Guard). **Die Kandidaten-Auflösung ist online** (API-Aufrufe `GetTvShowAsync`/`GetPlaylistEntriesPagedAsync`); offline liegt nur die gelesene Weiterschauen-Liste vor.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `RunAsync` | public | Vorlauf-Lauf: Guards `AutoDownloadEnabled`, `WifiOnlyDownloads && !IsWifi`, angemeldeter Client, Throttle (außer `force`); iteriert `homeLists.LoadHomeListsAsync().ContinueWatching`; Einzelfehler werden protokolliert und übersprungen |
| `EnqueueEpisodesFromCurrentAsync` | private | Serien-Pfad: `client.GetTvShowAsync(showId)` → `ShowDetailsCache.WriteAsync` (Cache-Ablage!), alle Episoden staffelübergreifend geordnet, ab der aktuellen bis `PreloadEpisodeCount` einreihen (`EnqueueAsync` mit `DownloadSeriesContext` + `pictureId`) |
| `EnqueuePlaylistEntriesFromCurrentAsync` | private | Playlist-Pfad: `client.GetPlaylistEntriesPagedAsync` seitenweise bis zum aktuellen `PlaylistEntryId`; ab dort ladbare, zugängliche Einträge (`movie`/`tvshowepisode`, `IsAccessible`) bis `PreloadEpisodeCount` einreihen (`EnqueueAsync` mit `DownloadPlaylistContext` + `SeriesContextOf`); `null` = Rückfall auf Serien-Pfad (Playlist nicht ladbar/Eintrag nicht gefunden) |
| `EnqueueSelfAsync` | private | Weiterschauen-Eintrag selbst einreihen (Film; Episode ohne Playlist-Treffer/Serienbezug) |
| `PlaylistEntryTitle`/`SeriesContextOf` | private | Titel-/Serienkontext-Ableitung aus `PlaylistEntry` (`ParentMedia*`, `EpisodeNumber`; Staffelnummer bleibt offen) |

Publizierte/abonnierte Events: keine eigenen — Zustandsmeldungen laufen über `DownloadService.ItemChanged`/`ItemRemoved`; Auslösung über `RunAutoDownloadsEffect` (Sync-Abschluss, Cache-Änderung, Einschalten der Einstellung).

Nicht vorhanden: eine vom `RunAsync`-Lauf getrennte, offline-fähige „Ist Medium X Auto-Download-Kandidat?"-Ermittlung; keine Verlängerung bestehender Downloads (Dedup via `EnqueueAsync` ist ohnehin no-op bei `AlreadyPresent`); keine gecachte Playlist-Struktur.

## `PlaybackSourceService`
Datei: `VideoPlayer.Mvu/PlaybackSourceService.cs`

Löst die Wiedergabequelle auf: fertige lokale Datei aus dem Manifest hat Vorrang, sonst Stream-URL mit `access_token`; Fortsetzungsposition online vom Server (`GetContinueWatchingAsync`), sonst aus der `cached_lists`-Kopie.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `ResolveAsync` | public | `PlaybackSource` auflösen (`Kind.LocalFile` bei fertigem Download mit vorhandener Datei, sonst `Kind.ServerStream`; `allowServerStream: false` = Offline: nur lokale Datei); `resumePositionSecondsOverride` überschreibt die Weiterschauen-Auflösung |
| `ResolveResumePositionAsync`/`ReadCachedResumePositionAsync`/`FindResumePosition` | private | Fortsetzungsposition: Server → Cache-Fallback |
| `ResolveLocalFileAsync` | private | Manifest-Eintrag mit `State == "completed"` (String-Vergleich) und vorhandener Datei (`DownloadManifestPaths.Resolve` + `fileExists`) → Dateipfad |

## `AppUpdate` (download-/wiedergaberelevante Handler)
Datei: `VideoPlayer.Mvu/AppUpdate.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `Update` (Dispatch) | public | Nachrichten→Übergänge; downloadrelevante Cases: `DownloadsPageOpenedMessage`, `DownloadsLoadedMessage`, `DownloadRequestedMessage`, `DownloadEnqueueCompletedMessage`, `DownloadItemChangedMessage`, `DownloadDeletedMessage`, `DownloadCancel/Retry/DeleteRequestedMessage`, `DownloadsDeleteAll/PurgeOrphans/DeleteFailedRequestedMessage` |
| `UpdateDownloadsPageOpened` | private | `IsLoading` setzen + `LoadDownloadsEffect` — Öffnen der Downloads-Seite triggert (im Handler) `ReconcileAsync` |
| `UpdateDownloadRequested` | private | Nur `movie`/`tvshowepisode` und nur im Zustand `Connected`: `EnqueueDownloadEffect` mit Serien-Hints/`PictureId`; offline → Hinweis auf der Detailseite |
| `UpdateDownloadEnqueueCompleted`/`UpdateDownloadItemChanged`/`UpdateDownloadDeleted` | private | Upsert/Entfernen in `model.Downloads.Items`; Einplanungs-Hinweis ggf. auf der Detailseite anzeigen |
| `UpdateDownloadCancelRequested`/`UpdateDownloadRetryRequested`/`UpdateDownloadDeleteRequested` | private | `CancelDownloadEffect` bzw. erneutes `EnqueueDownloadEffect` (nur `PictureId` mitgegeben — **kein Serienkontext**) bzw. `DeleteDownloadEffect` |
| `UpdateDownloadsDeleteAllRequested`/`UpdateDownloadsPurgeOrphansRequested`/`UpdateDownloadsDeleteFailedRequested` | private | `DeleteAllDownloadsEffect`/`PurgeOrphanedDownloadsEffect`/`DeleteFailedDownloadsEffect` mit Guards |
| `UpdatePlaybackProgress` | private | Positions-Takt: State fortschreiben, `ReportPlaybackProgressEffect` (Throttle 0,5 s), dann `EvaluateEndWindow` |
| `EvaluateEndWindow` | private | „Angesehen"-Schwelle: Restlaufzeit ≤ `WatchedEndWindowSeconds` über `EndWindowRequiredTicks = 2` plausible Takte (`EndWindowStreak`) → `IsWatched`; bei `AutoDeleteWatchedEpisodes` und `MediaType == "tvshowepisode"` → `DeleteWatchedDownloadEffect` |
| `UpdatePlaybackEnded` | private | Ende-Plausibilität (`PlaybackEndedToleranceSeconds = 300`, Restlaufzeit nahe 0) → `IsWatched`, End-Fortschrittsmeldung, `DeleteWatchedDownloadEffect` (nur Episoden), Auto-Fortsetzung (Playlist-`advance` bzw. `AdvanceToNextEpisode`) |
| `UpdatePlaybackSourceResolved` | private | Übernimmt `PlaybackSource` in `Player.Source` (inkl. `LocalFile`-Fall); bei `null` → Fehler-Rückfall nächster Eintrag. **Keine Ablauf-/Schutz-Prüfung** |
| `IsPlaybackEndPlausible`/`IsProgressTickPlausible` | private | Plausibilitäts-Gates für Ende-Ereignis bzw. Fortschrittstakte |
| `MarkEpisodeWatchedInDetails` | private | Gesehen-Zeitpunkt in der geladenen Episodenliste der Detailseite setzen |
| `EnterConnected`/`UpdateNetworkAvailabilityChanged` | private | `ResumeDownloadsEffect` beim Verbindungsaufbau bzw. bei Online-/Profilwechsel im verbundenen Zustand |
| `UpdateSynchronizationCompleted`/`UpdateCachedListsChanged` | private | `RunAutoDownloadsEffect` (nicht forciert) bei aktiviertem `AutoDownloadEnabled` und Verbindung |
| `UpdateSaveSettingsRequested` | private | `PersistSettingsEffect`; beim Einschalten von `AutoDownloadEnabled` zusätzlich `RunAutoDownloadsEffect(Force: true)` |
| `UpsertItem` | private | Download-Eintrag in die Liste upserten (Identität: `EntryId` oder `MediaId`+`MediaType`) |

## `ShellAppEffectHandler` (downloadrelevante Teile)
Datei: `VideoPlayer.Maui/Services/ShellAppEffectHandler.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `HandleAsync` (Cases) | public | Effekt-Dispatch: `LoadDownloadsEffect` → `LoadDownloadsAsync`; `EnqueueDownloadEffect` → `EnqueueDownloadAsync`; `CancelDownloadEffect` → `CancelDownloadAsync`; `DeleteDownloadEffect`/`DeleteWatchedDownloadEffect` → `DeleteDownloadAsync` (letzterer mit `completedOnly: true`); `DeleteAllDownloadsEffect`/`PurgeOrphanedDownloadsEffect`/`DeleteFailedDownloadsEffect` → jeweilige Methode + Listen-Neuladen; `ResumeDownloadsEffect` → `ResumeDownloadsAsync`; `RunAutoDownloadsEffect` → `RunAutoDownloadsAsync` |
| `LoadDownloadsAsync` | private | **Triggert `downloads.ReconcileAsync()`** (OrphanedBytes für die Anzeige), dann `LoadItemsAsync` + `GetUsedBytes` → `DownloadsLoadedMessage` |
| `EnqueueDownloadAsync` | private | `DownloadSeriesContext` aus den Effekt-Hints aufbauen → `downloads.EnqueueAsync` → `DownloadEnqueueCompletedMessage` (kein Playlist-Kontext am manuellen Pfad) |
| `DeleteDownloadAsync` | private | `DeleteAsync` bzw. `DeleteIfCompletedAsync` (`completedOnly`) — Entfernung läuft als `ItemRemoved` → `DownloadDeletedMessage` zurück |
| `ResumeDownloadsAsync` | private | **`downloads.ReconcileAsync()`** (Trigger bei App-Start/Verbindungsaufbau) + `ResumeInterruptedAsync` |
| `RunAutoDownloadsAsync` | private | `autoDownloads.RunAsync(settings, force)` |
| `DeleteAllDownloadsAsync`/`PurgeOrphanedDownloadsAsync`/`DeleteFailedDownloadsAsync` | private | Service-Aufruf + `LoadDownloadsAsync` (Liste/Belegung neu) |
| `CancelDownloadAsync` | private | `downloads.CancelAsync` |
| `OnDownloadItemChanged`/`OnDownloadItemRemoved` | private | Event-Brücke: `ItemChanged` → `DownloadItemChangedMessage`, `ItemRemoved` → `DownloadDeletedMessage` |

Abonnierte Events: `DownloadService.ItemChanged`, `DownloadService.ItemRemoved` (sowie Netzwerk-/Server-Events anderer Dienste).

## `HomeListService`
Datei: `VideoPlayer.Mvu/HomeListService.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `LoadHomeListsAsync` | public | Alle Startseiten-Listen aus dem Cache (`cached_lists`: `continue_watching`, `recent`, saisonale Genres; `cached_favorites`) → `HomeLists` mit `HomeMediaItem`s inkl. `PlaylistId`/`PlaylistEntryId`; beschädigte Einträge werden verworfen und über `DiscardedCaches` gemeldet — **offline-fähige Quelle der Weiterschauen-Kandidaten** |
| `LoadFavoritesAsync`/`LoadRecentAsync`/`LoadFavoriteItemsAsync`/`LoadSeasonalGenresAsync` | public | Teillisten aus dem Cache |
| `ReadListAsync`/`ReadFavoritesAsync`/`DiscardListEntryAsync`/`DiscardRowAsync` | private | Cache-Lesen/Verwerfen |
| `MapContinueWatching`/`MapEntry`/`MapFavorite`/`MediaHints`/`MediaTypeOf`/`MapAll` | private | DTO→`HomeMediaItem`-Mapping (Serien-Hints aus `Season.Show`, Playlist-Felder) |

## `OfflineContinueWatchingService`
Datei: `VideoPlayer.Mvu/OfflineContinueWatchingService.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `LoadHomeListsAsync` | public | Offline-Startseiten-Listen: Weiterschauen aus dem Manifest, Favoriten/Neuheiten aus dem Cache |
| `LoadContinueWatchingAsync` | internal | Weiterschauen aus `completed`-Manifest-Einträgen mit vorhandener Datei (nach `UpdatedAtUtc` absteigend); fehlende Folge-Episoden als `IsPlaceholder`-Einträge aus `ShowDetailsCache`; Fortschritt aus `pending_progress` bzw. `continue_watching`-Cache |
| `FindMissingNextEpisodeAsync` | private | Folge-Episode über `NextEpisodeAfter` (Staffel-/Episodennummer) bzw. `NextEpisodeAfterId` (Medien-Id) aus der gecachten Serienstruktur |
| `LoadPendingProgressAsync`/`LoadCachedContinueWatchingAsync`/`ResolveProgress` | private | Fortschritts-Auflösung: lokale Outbox → Server-Stand |

## `ShowDetailsCache`
Datei: `VideoPlayer.Mvu/ShowDetailsCache.cs`

Statischer Lese-/Schreibzugang der gecachten Serienstruktur in `cached_lists` (Schlüssel `show:{id}`).

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `KeyFor` | public | Cache-Schlüssel `show:{showId}` |
| `WriteAsync` | public | Serienstruktur (Staffeln+Episoden geordnet, `WatchedAt`) als JSON ablegen; Fehler → protokolliert/übergangen. Aufgerufen von `AutoDownloadService.EnqueueEpisodesFromCurrentAsync`, `UpcomingEpisodesService.LoadAsync` und dem Detailseiten-Ladepfad |
| `ReadAsync` | public | Serienstruktur lesen; fehlend/beschädigt → `null` |
| `OrderedEpisodes` | public | Alle Episoden staffelübergreifend geordnet (`ShowEpisodeRef`) |
| `NextEpisodeAfter` | public | Folge-Episode nach Staffel-/Episodennummer |
| `NextEpisodeAfterId` | public | Folge-Episode nach Episoden-Id |

## `CacheDatabase`
Datei: `VideoPlayer.Storage/CacheDatabase.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `InitializeAsync` | public | `CreateTableAsync` für `CachedListEntry`, `CachedFavoritesEntry`, `DownloadManifestEntry` + **Spalten-Migrationen** über `EnsureColumnAsync` (`ShowId`, `ShowTitle`, `SeasonNumber`, `EpisodeNumber`, `PlaylistId`, `PlaylistEntryId`, `PlaylistSortOrder`, `PictureId`) — etabliertes Muster für neue `download_manifest`-Spalten |
| `InitializeWithRecoveryAsync` | public | Initialisierung mit Wiederherstellung beschädigter Dateien (`*.corrupt-{Zeitstempel}` weglegen, neu anlegen) |
| `HasOfflineDataAsync` | public | Offline-Daten vorhanden? (Cache-Listen, Favoriten oder Manifest-Einträge) |
| `EnsureColumnAsync` | private | `PRAGMA table_info` + `ALTER TABLE … ADD COLUMN`, falls die Spalte fehlt |
| `Connection` | public Property | Lazy `SQLiteAsyncConnection` |

## `DownloadManifestPaths`
Datei: `VideoPlayer.Mvu/DownloadManifestPaths.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `Resolve` | public (internal class) | `entry.FilePath`-Dateiname gegen aktuelles `VideosDirectory` auflösen (Container-Umzug-Heilung) |

## `DownloadListSync`
Datei: `VideoPlayer.Mvu/DownloadListSync.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `Sync` | public | `ObservableCollection<DownloadItemViewModel>` mit `Downloads.Items` abgleichen: in-place `Update`, Add/Remove/Move nur bei Strukturänderungen (Identität: `EntryId` oder `MediaId`+`MediaType`) |

## `DownloadItemViewModel`
Datei: `VideoPlayer.Mvu/DownloadItemViewModel.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `Update` | public | Geänderten `DownloadListItem` übernehmen; bei Record-Ungleichheit `PropertyChanged` für alle Anzeige-Properties |
| Durchreich-Properties | public | `Title`, `PictureId`, `HasPicture`, `PreviewWidth`, `MediaTypeText`, `StateText`, `Progress`, `ProgressPercentText`, `ShowProgress`, `HasError`, `SizeText`, `HasSizeText`, `ErrorText`, `CanCancel`, `CanRetry`, `CanDelete` — keine Herkunfts-/Ablaufanzeige |

## `AppSettingsService`
Datei: `VideoPlayer.Storage/AppSettingsService.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `Load` | public | Alle Einstellungen aus `ISettingsStore` mit Defaults lesen + `Normalize()` |
| `Save` | public | Normalisiert in `ISettingsStore` schreiben (Schlüssel-Konstanten je Eigenschaft) |

## `HttpDownloadBackend` / `IosBackgroundDownloadBackend`
Dateien: `VideoPlayer.Mvu/HttpDownloadBackend.cs`, `VideoPlayer.Maui/Platforms/iOS/IosBackgroundDownloadBackend.cs`

`HttpDownloadBackend.TransferAsync` (public): `HttpClient`-Stream in die Zieldatei, `http_{status}`-Fehlercodes, keine Range-Fortsetzung. `IosBackgroundDownloadBackend` (iOS): `NSUrlSession`-Hintergrund-Transfer, `CompletionSink` meldet systemseitig abgeschlossene Transfers → `DownloadService.CompleteExternalTransferAsync`; Registrierung in `MauiProgram` unter `#if IOS` als `IDownloadBackend`.

## `UpcomingEpisodesService` (Nebenbezug)
Datei: `VideoPlayer.Mvu/UpcomingEpisodesService.cs`

`LoadAsync` lädt die Fortsetzungsliste für den Wiedergabe-Direktstart: online `GetTvShowAsync` + `ShowDetailsCache.WriteAsync`, offline `ShowDetailsCache.ReadAsync` + `OrderedEpisodes` — zweite Schreibquelle des Serien-Caches.

## `SettingsPage`
Dateien: `VideoPlayer.Maui/Pages/SettingsPage.xaml(.cs)`

Download-Abschnitt (XAML): Schalter `AutoDownloadSwitch` („Automatische Downloads"), `WifiOnlySwitch` („Downloads nur im WLAN"), `AutoDeleteWatchedSwitch` („Gesehene Episoden automatisch löschen"); Zahlenfelder `PreloadEpisodeCountEntry`, `DownloadQuotaGbEntry`. Wiedergabe-Abschnitt: `AutoResumeSwitch`, `WatchedEndWindowEntry`. Code-Behind: `PopulateFromModel` schreibt Modellwerte in die Controls (mit `suppressEvents`); `On*Toggled`/`On*Completed` → `Save(...)` → `SaveSettingsRequestedMessage`. Keine Aufbewahrungs-/Löschverzögerungs-Controls.

## `DownloadsPage`
Dateien: `VideoPlayer.Maui/Pages/DownloadsPage.xaml(.cs)`

`OnAppearing` → `DownloadsPageOpenedMessage` (löst Laden inkl. `ReconcileAsync` aus); `UpdateUi` → `DownloadListSync.Sync` + Banner/Quota/Aktions-Sichtbarkeiten. Eintrags-Aktionen: Abbrechen (`DownloadCancelRequestedMessage`), Erneut versuchen (`DownloadRetryRequestedMessage`), Löschen (`DownloadDeleteRequestedMessage`); Gesamtaktionen mit Rückfrage: „Alle löschen", „Alle Fehler löschen", „Speicher bereinigen". ItemTemplate bindet `DownloadItemViewModel`: Titel, Zustands-Chip (`StateText`), `MediaTypeText`, `SizeText`, Fortschritt, `ErrorText` — keine Herkunfts-/Ablaufanzeige.

## `MediaDetailsPage` (Download-Auslöser)
Datei: `VideoPlayer.Maui/Pages/MediaDetailsPage.xaml.cs`

`OnDownloadClicked` (Kopfbereich) und `OnEpisodeDownloadClicked` (Episodenliste) → `DownloadRequestedMessage` mit Serienkontext (`ShowId`, `ShowTitle`, `SeasonNumber`, `EpisodeNumber`) und `PictureId` — die manuellen Einplanungs-Pfade der UI.

## `MauiProgram` (DI-Verdrahtung)
Datei: `VideoPlayer.Maui/MauiProgram.cs` (Zeilen 105–164)

Singletons: `HomeListService`, `OfflineContinueWatchingService`, `PlaybackSourceService`, `UpcomingEpisodesService`, `DownloadService`, `AutoDownloadService`; iOS: `IosBackgroundDownloadBackend` als `IDownloadBackend` mit `CompletionSink` → `CompleteExternalTransferAsync`.
