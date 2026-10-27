# Datenmodell

## `DownloadManifestEntry`
Datei: `VideoPlayer.Storage/Entities/DownloadManifestEntry.cs`

SQLite-Entität der Tabelle `download_manifest` (Cache-Datenbank, nicht backup-pflichtig). Einträge werden von `DownloadService` angelegt/aktualisiert und von `PlaybackSourceService`, `OfflineContinueWatchingService` und `AutoDownloadService` (indirekt) gelesen.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `int` | Primärschlüssel, `[PrimaryKey, AutoIncrement]`; Grundlage der Manifest-/Warteschlangen-Reihenfolge |
| `VideoId` | `int` | Id des heruntergeladenen Mediums, `[Indexed]` |
| `VideoType` | `string` | Medientyp (`movie`, `tvshowepisode`) |
| `FilePath` | `string` | Pfad der lokalen Videodatei; Verzeichnisanteil kann veralten (Container-Umzug) — `DownloadManifestPaths.Resolve` heilt |
| `SizeBytes` | `long` | Dateigröße bei abgeschlossenen Downloads |
| `Title` | `string` | Anzeigetitel für die Downloads-Seite ohne Server-Roundtrip |
| `DownloadedBytes` | `long` | Bereits übertragene Bytes des laufenden/letzten Laufs |
| `TotalBytes` | `long` | Erwartete Gesamtgröße (`Content-Length`); 0 = unbekannt |
| `Error` | `string?` | Maschinenlesbarer Fehlercode (`quota_exceeded`, `network`, `http_*`, `io`, `missing_file`, `no_connection`); `null` = kein Fehler |
| `State` | `string` | Persistierter Zustand (`queued`, `downloading`, `completed`, `failed`, `cancelled`) — Abbildung über `DownloadStateNames` |
| `ShowId` | `long?` | Serien-Id (nur Episoden-Downloads); Grundlage des Offline-Serienkontexts |
| `ShowTitle` | `string?` | Anzeigetitel der Serie |
| `SeasonNumber` | `int?` | Staffelnummer der Episode |
| `EpisodeNumber` | `int?` | Episodennummer innerhalb der Staffel |
| `PlaylistId` | `long?` | Id der Playlist, aus deren Kontext der Download eingeplant wurde |
| `PlaylistEntryId` | `long?` | Id des Playlist-Eintrags, dem das Medium entspricht |
| `PlaylistSortOrder` | `long?` | Sortierposition in der Playlist (serverseitige Reihenfolge) |
| `PictureId` | `long?` | Bild-Id des Vorschaubilds (`api/pictures/{id}`) |
| `UpdatedAtUtc` | `DateTime` | Zeitpunkt der letzten Aktualisierung (UTC) — einziges Datumsfeld des Eintrags |

Nicht vorhanden: `ExpiresAtUtc`, eine Herkunfts-Spalte (`manual`/`auto`) oder ein Fertigstellungs-Zeitpunkt (`CompletedAtUtc` o. ä.).

## `AppSettings`
Datei: `VideoPlayer.Storage/AppSettings.cs`

Unveränderliches Record aller App-Einstellungen; persistiert über `AppSettingsService` in `ISettingsStore` (Preferences) mit Profil-Präfix. `Normalize()` klammert numerische Werte.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `DefaultDownloadQuotaGb` | `const int = 5` | Standardwert des Speicherplatzkontingents |
| `AutoDownloadEnabled` | `bool` | Automatische Downloads an/aus (Standard: aus) |
| `PreloadEpisodeCount` | `int` | Anzahl vorzuladender Episoden; Standard 3, Clamp 1–10 |
| `AutoResumeEnabled` | `bool` | Auto-Fortsetzung am Wiedergabeende (Standard: an) |
| `WifiOnlyDownloads` | `bool` | Downloads nur im WLAN (Standard: an) |
| `WatchedEndWindowSeconds` | `int` | „Als angesehen"-Endfenster in Sekunden; Standard 300, Clamp 30–3600 |
| `DownloadQuotaGb` | `int` | Speicherplatzkontingent in GB; Standard 5, Clamp 1–500 |
| `AutoDeleteWatchedEpisodes` | `bool` | „Gesehene Episoden automatisch löschen" (Standard: aus) — wirkt nur auf `tvshowepisode`, löst die Sofort-Löschung über `DeleteWatchedDownloadEffect` aus |
| `HomeHeroEnabled` | `bool` | Hero-Kopfbereich der Startseite (Standard: an) |
| `LoggingEnabled` | `bool` | Programmprotokoll (Standard: aus) |
| `Normalize()` | Methode | Kopie mit geklammerten Werten (`PreloadEpisodeCount`, `WatchedEndWindowSeconds`, `DownloadQuotaGb`) |

Persistenzschlüssel in `AppSettingsService` (`VideoPlayer.Storage/AppSettingsService.cs`): `AutoDownloadEnabled`, `PreloadEpisodeCount`, `AutoResumeEnabled`, `WifiOnlyDownloads`, `WatchedEndWindowSeconds`, `DownloadQuotaGb`, `AutoDeleteWatchedEpisodes`, `HomeHeroEnabled`, `LoggingEnabled` — `Load()` liest mit Defaults und normalisiert, `Save()` normalisiert vor dem Schreiben.

## `DownloadListItem`
Datei: `VideoPlayer.Mvu/DownloadListItem.cs`

Anzeigefertiger Snapshot eines Manifest-Eintrags für Downloads-Seite und MVU-Modell; erzeugt in `DownloadService.ToItem`.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `EntryId` | `int` | Id des Manifesteintrags (stabile Sortierung) |
| `MediaType` | `string` | Normalisierter Medientyp |
| `MediaId` | `long` | Id des Mediums |
| `Title` | `string` | Anzeigetitel (Fallback `Medium {id}`) |
| `PictureId` | `long?` | Vorschaubild-Id; `null` = Platzhalter |
| `State` | `DownloadState` | Aktueller Download-Zustand |
| `DownloadedBytes` | `long` | Übertragene Bytes |
| `TotalBytes` | `long` | Erwartete Gesamtgröße |
| `SizeBytes` | `long` | Dateigröße (abgeschlossen) |
| `Error` | `string?` | Fehlercode |
| `IsActive`/`CanCancel`/`CanRetry`/`CanDelete`/`ShowProgress` | `bool` | Abgeleitete Aktions-/Anzeige-Schalter |
| `HasError`/`HasPicture`/`HasSizeText` | `bool` | Abgeleitete Anzeige-Flags |
| `PreviewWidth` | `double` | Vorschaubild-Breite (Episode 128, sonst 48; Höhe 72) |
| `Progress`/`ProgressPercentText` | `double`/`string` | Fortschritt 0–1 bzw. Prozenttext |
| `StateText` | `string` | Übersetzter Zustandstext (Wartend/Läuft/Fertig/Fehler/Abgebrochen) |
| `ErrorText` | `string?` | Übersetzter Fehlertext je Fehlercode |
| `SizeText` | `string?` | Größenanzeige (fertig: Dateigröße; laufend: „x von y") |
| `MediaTypeText` | `string` | Übersetzte Typbezeichnung (Film/Episode/Medium) |
| `FormatBytes` | statische Methode | Byteformatierung (GB ab 0,1 GB, sonst MB) |

Nicht vorhanden: Herkunft/„Cache"-Kennzeichnung, `ExpiresAtUtc`-Anzeige.

## `DownloadsPageState`
Datei: `VideoPlayer.Mvu/DownloadsPageState.cs`

MVU-State der Downloads-Seite (`AppModel.Downloads`).

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Initial` | `static DownloadsPageState` | Ausgangszustand |
| `IsLoading` | `bool` | Ladevorgang läuft |
| `Attempted` | `bool` | Mindestens ein Ladeversuch abgeschlossen |
| `Error` | `string?` | Fehlertext des letzten Ladens |
| `Items` | `IReadOnlyList<DownloadListItem>` | Einträge in Manifest-Reihenfolge |
| `UsedBytes` | `long` | Belegter Speicher |
| `QuotaGb` | `int` | Konfiguriertes Kontingent |
| `OrphanedBytes` | `long` | Erkannte Restdaten ohne Manifest-Eintrag |
| `HasItems`/`HasFailedItems`/`HasError`/`ShowEmpty` | `bool` | Abgeleitete Anzeige-Flags |
| `QuotaText` | `string` | Belegungsanzeige („x GB von y GB belegt") |
| `OrphanedText` | `string?` | Hinweiszeile zu Restdaten |

## `DownloadReconcileResult`
Datei: `VideoPlayer.Mvu/DownloadReconcileResult.cs`

Ergebnis-Record von `DownloadService.ReconcileAsync`.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `RemovedPartialBytes` | `long` | Freigegebene Bytes entfernter Teildateien |
| `RemovedPartialCount` | `int` | Anzahl entfernter Teildateien |
| `OrphanedBytes` | `long` | Bytes erkannter Restdaten (nicht entfernt) |
| `OrphanedFileCount` | `int` | Anzahl erkannter Restdateien |
| `CorrectedCompletedCount` | `int` | „Fertig"-Einträge ohne Datei → `failed` korrigiert |

## `DownloadEnqueueResult`
Datei: `VideoPlayer.Mvu/DownloadService.cs` (Zeilen 31–41)

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Item` | `DownloadListItem?` | Snapshot des betroffenen Eintrags; `null` bei ungültiger Anforderung |
| `Notice` | `string?` | Anzeigbarer Hinweistext (Dedup-/Erfolgs-/Fehlerhinweis) |
| `IsError` | `bool` | Hinweis beschreibt eine Ablehnung/einen Fehler |

## `DownloadItemEventArgs` / `DownloadRemovedEventArgs`
Datei: `VideoPlayer.Mvu/DownloadService.cs` (Zeilen 11–28)

Ereignisdaten von `DownloadService.ItemChanged` (`Item`: geänderter `DownloadListItem`-Snapshot) bzw. `ItemRemoved` (`MediaType`, `MediaId` des entfernten Downloads).

## `DownloadSeriesContext`
Datei: `VideoPlayer.Mvu/DownloadSeriesContext.cs`

Record `(long? ShowId, string? ShowTitle, int? SeasonNumber, int? EpisodeNumber)` — Serienkontext beim Einreihen; persistiert in den `Show*`/`SeasonNumber`/`EpisodeNumber`-Spalten des Manifests.

## `DownloadPlaylistContext`
Datei: `VideoPlayer.Mvu/DownloadPlaylistContext.cs`

Record `(long? PlaylistId, long? PlaylistEntryId, long? PlaylistSortOrder)` — Playlist-Bezug beim Einreihen; persistiert in den `Playlist*`-Spalten des Manifests.

## `PlaybackSource`
Datei: `VideoPlayer.Mvu/PlaybackSource.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Kind` | `PlaybackSourceKind` | `LocalFile` (Download) oder `ServerStream` |
| `Location` | `string` | Stream-URL bzw. lokaler Dateipfad |
| `ResumePosition` | `TimeSpan?` | Fortsetzungsposition; `null` = Start am Anfang |

## `PlayerState` (downloadrelevante Auswahl)
Datei: `VideoPlayer.Mvu/PlayerState.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `MediaType`/`MediaId`/`Title` | `string?`/`long`/`string?` | Identität des laufenden Mediums |
| `Source` | `PlaybackSource?` | Aufgelöste Quelle — `Kind == LocalFile` kennzeichnet Download-Wiedergabe |
| `IsResolving` | `bool` | Quellauflösung läuft (sperrt Endfenster-Wirkung) |
| `Position`/`Duration` | `TimeSpan` | Letzter gemeldeter Wiedergabestand (Grundlage Restdauer) |
| `IsWatched` | `bool` | „Angesehen"-Markierung |
| `EndWindowStreak` | `int` | Bestätigungs-Streak der „angesehen"-Schwelle |
| `IsActive` | `bool` | Wiedergabe angefordert |

## `HomeMediaItem` (downloadrelevante Auswahl)
Datei: `VideoPlayer.Mvu/HomeMediaItem.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `long` | Medien-Id |
| `MediaType` | `string` | Medientyp (Server- oder CLR-Konvention je Herkunft) |
| `Title` | `string` | Anzeigetitel |
| `PosterPictureId` | `long?` | Bild-Id |
| `Progress` | `double?` | Fortschritt 0–1 (Weiterschauen) |
| `ShowId`/`SeasonId`/`EpisodeId` | `long?` | Serienkontext-Hints |
| `PlaylistId`/`PlaylistName`/`PlaylistEntryId` | `long?`/`string?`/`long?` | Playlist-Bezug des Weiterschauen-Eintrags — Grundlage des Playlist-Pfads im `AutoDownloadService` |
| `IsPlaceholder` | `bool` | Offline-Platzhalter (nicht heruntergeladen) |

## `HomeLists`
Datei: `VideoPlayer.Mvu/HomeLists.cs`

Ergebnisbündel: `ContinueWatching`, `Favorites`, `Recent`, `SeasonalGenres` (je `IReadOnlyList<HomeMediaItem>`/`HomeSeasonalSection`) sowie `DiscardedCaches` (verworfene beschädigte Cache-Schlüssel). `HomeLists.Empty` als leerer Ausgangswert.

## `CachedListEntry`
Datei: `VideoPlayer.Storage/Entities/CachedListEntry.cs`

SQLite-Entität `cached_lists` (`Key` PK, `Json`, `UpdatedAtUtc`). Relevante Schlüssel: `continue_watching` (`ServerSyncService.ContinueWatchingCacheKey`), `recent`, saisonale Genres sowie `show:{showId}` der `ShowDetailsCache`-Serienstruktur.

## Serien-Cache-Records (`ShowDetailsCache.cs`)
Datei: `VideoPlayer.Mvu/ShowDetailsCache.cs`

| Record | Eigenschaften | Zweck |
|--------|---------------|-------|
| `CachedShowEpisode` | `Id`, `Number`, `Title`, `WatchedAt` (nullable, abwärtskompatibel) | Gecachte Episode |
| `CachedShowSeason` | `Id`, `Number`, `Episodes` | Gecachte Staffel |
| `CachedShowEntry` | `ShowId`, `Title`, `Seasons` | Gecachte Serienstruktur (JSON in `cached_lists`) |
| `ShowEpisodeRef` | `SeasonNumber`, `Episode` | Positionsverweis in der geordneten Serienfolge |

## `ContinueWatchingEntry` (API-DTO, auch Cache-Schema)
Datei: `VideoPlayer.ApiClient/Models/ContinueWatching.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `MediaType` | `string` | Server-Medientyp (`movie`, `episode`, …) |
| `Entry` | `MediaEntry?` | Eingebetteter Medieneintrag (Seriengraph bei Episoden) |
| `PositionSeconds`/`DurationSeconds` | `long`/`long?` | Wiedergabestand |
| `Title`/`PosterPictureId` | `string`/`long?` | Anzeigedaten |
| `PlaylistId`/`PlaylistName`/`PlaylistEntryId` | `long?`/`string?`/`long?` | Playlist-Bezug des Weiterschauen-Eintrags |

## `PlaylistEntry` (API-DTO)
Datei: `VideoPlayer.ApiClient/Models/PlaylistEntry.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `long` | Eintrags-Id (wird als `PlaylistEntryId` im Manifest gespeichert) |
| `PlaylistId` | `long` | Zugehörige Playlist |
| `MediaType`/`MediaId`/`MediaTitle` | `string`/`long`/`string` | Referenziertes Medium |
| `ParentMediaType`/`ParentMediaId`/`ParentMediaTitle` | `string?`/`long?`/`string?` | Elternkontext (Serien-Hints bei Episoden) |
| `IsAccessible` | `bool` | Zugriffsrecht (Filter des Auto-Download-Vorlaufs) |
| `SortOrder` | `long?` | Manuelle Sortierposition (→ `PlaylistSortOrder`) |
| `EpisodeNumber` | `int?` | Episodennummer bei Episoden-Einträgen |
| `ResolvedPictureId` | `long?` | Serverseitig aufgelöste Bild-Id |
| `AddedAt`/`ReleaseDate`/`Plot` | `DateTime`/`DateTime?`/`string?` | Metadaten |

## `DownloadBackendResult`
Datei: `VideoPlayer.Mvu/IDownloadBackend.cs`

Record `(long BytesWritten, long TotalBytes, string? ErrorCode)` — Ergebnis eines `IDownloadBackend`-Transfers.
