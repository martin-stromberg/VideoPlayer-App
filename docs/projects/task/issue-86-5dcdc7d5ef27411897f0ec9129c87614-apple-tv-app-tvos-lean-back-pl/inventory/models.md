# Datenmodell — Bestandsaufnahme

Alle Angaben ohne Gewähr auf Vollständigkeit der reinen Anzeige-Hilfsproperties; dokumentiert sind die fachlich relevanten Eigenschaften. Dateipfade relativ zum Repository-Root.

## `AppModel`
`VideoPlayer.Mvu/AppModel.cs` — unveränderliches Gesamtmodell des MVU-Kerns (`sealed record`).

| Eigenschaft | Typ | Beschreibung |
|---|---|---|
| `CurrentPage` | `AppPage` | Aktuell angezeigte Seite (Start: `Loading`) |
| `IsStartupRunning` | `bool` | Start-Workflow läuft |
| `ServerHealth` | `ServerHealthStatus` | Ergebnis der Erreichbarkeitsprüfung |
| `ConnectionState` | `ConnectionState` | Verbindungszustand (steuert Navigation) |
| `IsLoginRunning` / `LoginError` / `LoginUsername` | `bool` / `string?` / `string?` | Anmeldezustand und -fehler |
| `HasOfflineData` | `bool` | Lokale Offline-Daten vorhanden |
| `IsOffline` | `bool` | Abgeleitet: `ConnectionState == Offline` |
| `IsOnline` | `bool` | Letzter Netzwerkstatus (via `NetworkAvailabilityChangedMessage`) |
| `IsSyncRunning` | `bool` | Voll-Synchronisation läuft |
| `Settings` | `AppSettings` | Geladene App-Einstellungen |
| `KnownServers` | `IReadOnlyList<KnownServer>` | Bekannte Server |
| `ActiveServerAddress` | `string?` | Kanonische Adresse des aktiven Servers |
| `DiscoveredServers` | `IReadOnlyList<DiscoveredServer>` | Discovery-Treffer |
| `IsDiscoveryRunning` / `DiscoveryAttempted` / `IsServerCheckRunning` / `ServerSetupError` | `bool`/`bool`/`bool`/`string?` | Server-Einrichtungszustand |
| `IsPairingRequired` / `IsPairingRunning` / `PairingError` | `bool`/`bool`/`string?` | Pairing-Zustand (`api/pairing/exchange`) |
| `IsBootstrapRunning` / `BootstrapError` | `bool`/`string?` | Ticket-Bootstrap-Zustand (`api/pairing/bootstrap`) |
| `ContinueWatching` / `Favorites` / `RecentEntries` | `IReadOnlyList<HomeMediaItem>` | Startseiten-Listen (Cache-Stand) |
| `SeasonalGenres` | `IReadOnlyList<HomeSeasonalSection>` | Saisonale Genre-Sektionen |
| `HomeHero` | `HomeHeroState` | Hero-Kopfbereich (Ladezustand + Bildbytes) |
| `ShowHomeHero` | `bool` | Abgeleitet aus `Settings.HomeHeroEnabled` |
| `SourcesOverview` | `SourcesOverviewState` | Quellenliste des Menüs |
| `SourceBrowser` | `SourceBrowserState` | Quellen-Browsing-Seite |
| `MediaDetails` | `MediaDetailsState` | Medien-Detailseite |
| `FavoritesPage` | `FavoritesPageState` | Favoriten-Seite |
| `PlaylistsPage` | `PlaylistsPageState` | Playlists-Übersicht |
| `PlaylistDetails` | `PlaylistDetailsState` | Playlist-Detailseite |
| `PlaylistForm` | `PlaylistFormState` | Playlist-Formular (Anlage/Bearbeitung) |
| `PlaylistGenreEditor` | `PlaylistGenreEditorState` | Genre-Editor |
| `Downloads` | `DownloadsPageState` | Downloads-Seite |
| `Profile` | `ProfilePageState` | Profil-Seite |
| `Player` | `PlayerState` | Wiedergabezustand |
| `StorageNotice` / `LogSendError` | `string?`/`string?` | Hinweise (Ablage-Wiederherstellung, Protokollversand) |

## `PlayerState`
`VideoPlayer.Mvu/PlayerState.cs` — `record`, Ausgangszustand `Empty`.

| Eigenschaft | Typ | Beschreibung |
|---|---|---|
| `MediaType` / `MediaId` / `Title` | `string?`/`long`/`string?` | Abzuspielendes Medium |
| `Source` | `PlaybackSource?` | Aufgelöste Quelle (Stream-URL oder lokale Datei) |
| `IsResolving` / `Attempted` | `bool` | Quellauflösung / Wiedergabeversuch |
| `Position` / `Duration` | `TimeSpan` | Wiedergabeposition/-dauer |
| `IsPlaying` / `IsBuffering` | `bool` | Player-Status |
| `IsWatched` | `bool` | „Angesehen"-Schwelle erreicht |
| `IsPictureInPicture` | `bool` | PiP-/Mini-Player-Modus |
| `UpcomingEpisodes` | `IReadOnlyList<PlayerEpisodeRef>` | Fortsetzungsliste (Auto-Resume, Fehler-Rückfall) |
| `PlaylistId` / `PlaylistName` / `PlaylistEntryId` / `PlaylistPosition` / `PlaylistTotalCount` / `PlaylistNextExhausted` | `long?`/`string?`/`long`/`int`/`int`/`bool` | Playlist-Wiedergabekontext |
| `ProgressReportPlaylistId` | `long?` | Playlist-Bezug für Fortschrittsmeldung |
| `Error` | `string?` | Fehlertext |
| `LastReportedPositionSeconds` | `double` | Throttling der Fortschrittsmeldungen (−1 = noch keine) |
| `EndWindowStreak` | `int` | Bestätigungs-Streak des „angesehen"-Endfensters |
| `DownloadExpiryProtected` | `bool` | Wiedergabe-Schutz des Ablaufdatums erfolgt (Einmal-Semantik) |
| `IsActive` / `HasNextEpisode` / `IsPlaylistPlayback` / `CanNavigateNext` / `CanNavigatePrevious` / `PlaylistLabel` / `HasError` | abgeleitet | Anzeige-/Logik-Hilfsproperties |

## `PlaybackSource` / `PlaybackSourceKind`
`VideoPlayer.Mvu/PlaybackSource.cs`, `PlaybackSourceKind.cs`

| Eigenschaft | Typ | Beschreibung |
|---|---|---|
| `Kind` | `PlaybackSourceKind` | `ServerStream` (Endpunkt `api/items/{typ}/{id}/stream`) oder `LocalFile` |
| `Location` | `string` | Stream-URL inkl. Token bzw. lokaler Dateipfad |
| `ResumePosition` | `TimeSpan?` | Fortsetzungsposition (`null` = ab Anfang) |

## `DownloadsPageState`
`VideoPlayer.Mvu/DownloadsPageState.cs` — `record`, Ausgangszustand `Initial`.

| Eigenschaft | Typ | Beschreibung |
|---|---|---|
| `IsLoading` / `Attempted` / `Error` | `bool`/`bool`/`string?` | Ladezustand |
| `Items` | `IReadOnlyList<DownloadListItem>` | Manifest-Einträge in Auftragsreihenfolge |
| `UsedBytes` | `long` | Belegter Speicher im Video-Verzeichnis |
| `QuotaGb` | `int` | Konfiguriertes Kontingent (Default `AppSettings.DefaultDownloadQuotaGb` = 5) |
| `OrphanedBytes` | `long` | Restdaten ohne Manifest-Eintrag (aus `ReconcileAsync`) |
| `HasFailedItems` / `QuotaText` / `OrphanedText` / `ShowEmpty` | abgeleitet | Anzeige-Hilfsproperties |

## `DownloadListItem`
`VideoPlayer.Mvu/DownloadListItem.cs` — `record`.

| Eigenschaft | Typ | Beschreibung |
|---|---|---|
| `EntryId` | `int` | Manifest-Id |
| `MediaType` / `MediaId` / `Title` | `string`/`long`/`string` | Medium (CLR-Konvention) |
| `PictureId` | `long?` | Vorschaubild (`api/pictures/{id}`) |
| `State` | `DownloadState` | Zustand |
| `DownloadedBytes` / `TotalBytes` / `SizeBytes` | `long` | Fortschritt/Größe |
| `Error` | `string?` | Fehlercode (z. B. `quota_exceeded`, `missing_file`, `http_*`) |
| `Origin` | `DownloadOrigin` | Herkunft (`Manual`/`Auto`; Anzeige „Cache" bei `Auto`) |
| `ExpiresAtUtc` | `DateTime?` | Ablaufdatum (nur `Completed`) |
| `ErrorText` / `StateText` / `ExpiresText` / `OriginText` / `SizeText` / `MediaTypeText` / `Progress` / `CanCancel` / `CanRetry` / `CanDelete` | abgeleitet | Anzeige-Properties; `ErrorText` übersetzt `DownloadService.Error*`-Codes inkl. `ErrorMissingFile` |

## `DownloadEnqueueRequest` / `DownloadSeriesContext` / `DownloadPlaylistContext` / `DownloadEnqueueResult`
`VideoPlayer.Mvu/DownloadEnqueueRequest.cs`, `DownloadSeriesContext.cs`, `DownloadPlaylistContext.cs`, `DownloadService.cs`

- `DownloadEnqueueRequest(MediaType, MediaId, Title, SeriesContext?, PlaylistContext?, PictureId?, Origin = Manual)` — Einreihe-Datensatz.
- `DownloadSeriesContext(ShowId?, ShowTitle?, SeasonNumber?, EpisodeNumber?)` — Serienkontext für Offline-„Weiterschauen".
- `DownloadPlaylistContext(PlaylistId?, PlaylistEntryId?, PlaylistSortOrder?)` — Playlist-Bezug.
- `DownloadEnqueueResult` — `Item` (`DownloadListItem?`), `Notice` (`string?`), `IsError` (`bool`).

## `DownloadReconcileResult`
`VideoPlayer.Mvu/DownloadReconcileResult.cs` — `record`.

| Eigenschaft | Typ | Beschreibung |
|---|---|---|
| `RemovedPartialBytes` / `RemovedPartialCount` | `long`/`int` | Entfernte überholte Teildateien |
| `OrphanedBytes` / `OrphanedFileCount` | `long`/`int` | Erkannte Restdaten ohne Manifest-Bezug (nicht entfernt) |
| `CorrectedCompletedCount` | `int` | „fertig" ohne Datei → `failed`/`missing_file` korrigiert |
| `RemovedExpiredCount` | `int` | Abgelaufene Downloads entfernt (Datei + Zeile) |
| `ExtendedExpiryCount` | `int` | Als Auto-Download-Kandidat verlängerte Einträge |

## `DownloadManifestPaths`
`VideoPlayer.Mvu/DownloadManifestPaths.cs` — `internal static`, `Resolve(layout, entry)`: heilt veraltete Verzeichnisanteile nach Container-Umzug (iOS); Dateiname `{type}_{mediaId}.mp4` ist beständig.

## `HomeLists` / `HomeMediaItem` / `HomeSeasonalSection` / `HomeHeroState`
`VideoPlayer.Mvu/HomeLists.cs`, `HomeMediaItem.cs`, `HomeSeasonalSection.cs`, `HomeHeroState.cs`

- `HomeLists`: `ContinueWatching`, `Favorites`, `Recent`, `SeasonalGenres` (je `IReadOnlyList`), `DiscardedCaches`; `Empty`-Singleton.
- `HomeMediaItem`: `Id`, `MediaType` (Server- oder CLR-Konvention), `Title`, `PosterPictureId`, `Progress` (0–1), `ShowId`/`SeasonId`/`EpisodeId`, `PlaylistId`/`PlaylistName`/`PlaylistEntryId`, `IsPlaceholder` (Offline-Platzhalter), abgeleitete Anzeige-Properties.
- `HomeSeasonalSection`: `GenreId`, `Name`, `ExpiresAt`, `Items`, `AutomationId`.
- `HomeHeroState`: `IsLoading`, `ImageBytes` (flüchtig), `Empty`.

## `SourcesOverviewState` / `SourceBrowserState` / `SourceItem` / `SourceGenreItem` / `SourceMediaItem`
`VideoPlayer.Mvu/SourcesOverviewState.cs`, `SourceBrowserState.cs`, `SourceItem.cs`, `SourceGenreItem.cs`, `SourceMediaItem.cs`

- `SourcesOverviewState`: `IsLoading`, `Attempted`, `Error`, `Sources`; `Initial`.
- `SourceBrowserState`: `SourceId`, `SourceName`, `Genres`, `SelectedGenreId`, `SearchText`, `Items`, `NextPage`, `HasMore`, `IsLoading`, `IsLoadingMore`, `Attempted`, `Error`; `PageSize = 20`, `Empty`.
- `SourceItem`: `Id`, `Name`, `IconPictureId` (`api/sourceicons/{id}`).
- `SourceGenreItem`: `Id` (0 = „Alle"), `Name`, `IsSelected`.
- `SourceMediaItem`: `Id`, `Title`, `MediaType` (normalisiert), `PosterPictureId`.

## `MediaDetailsState` / `MediaDetailsView` / `SeasonItem` / `EpisodeItem` / `CollectionMovieItem`
`VideoPlayer.Mvu/MediaDetailsState.cs`, `MediaDetailsView.cs`

- `MediaDetailsState`: `MediaType`/`MediaId`, `IsLoading`/`Attempted`/`Error`, `IsFavoriteUpdating`/`FavoriteError`, `DownloadNotice`/`DownloadNoticeIsError`, `PlaylistTargets`/`IsLoadingPlaylistTargets`/`PlaylistPickerPending`/`IsAddingToPlaylist`/`AddToPlaylistNotice(…)`, `Details` (`MediaDetailsView?`), `HeaderImageBytes`/`IsHeaderImageLoading`; `Empty`.
- `MediaDetailsView`: `MediaType`, `MediaId`, `Title`, `Plot`, `GenreNames`, `ReleaseYear`, `BannerPictureId`, `PosterPictureId`, `IsFavorite`, `HasLocalFile`, `IsOfflineSnapshot`, `ShowId`, `Seasons`, `SelectedSeasonId`, `HighlightedEpisodeId`/`HighlightedEpisodeTitle`, `SelectedEpisodeId`, `Movies`; Anzeige-Ableitungen (`HeaderTitle`, `HeaderMeta`, `HeaderPlot`, `SelectedSeason`, `SelectedSeasonEpisodes`, `IsPlayable`, `MediaTypeText` u. a.).
- `SeasonItem`: `Id`, `Number`, `Name`, `BannerPictureId`, `Year`, `Episodes`.
- `EpisodeItem`: `Id`, `Number`, `Title`, `Plot`, `ReleaseYear`, `PosterPictureId`, `IsHighlighted`, `WatchedAt`, `HasLocalFile`, `IsPlaceholder`.
- `CollectionMovieItem`: `Id`, `Title`, `Plot`, `GenreNames`, `ReleaseYear`, `PosterPictureId`, `IsFavorite`.

## `PlaylistsPageState` / `PlaylistListItem` / `PlaylistOverviewItem`
`VideoPlayer.Mvu/PlaylistsPageState.cs`

- `PlaylistsPageState`: `Items` (flache Liste), `IsLoading`/`Attempted`/`Error`, Konstanten `OwnSectionTitle`/`PublicSectionTitle`, `BuildItems(own, public)`; `Initial`.
- `PlaylistListItem`: `PlaylistId`, `Name`, `Description`, `SortMode`, `CoverPictureId`, `GenreText`, `IsPublic`, `IsOwner`, `AllGenreIds`, `GenresManuallyOverridden`.
- `PlaylistOverviewItem`: `IsHeader`, `Title`, `Playlist`; Fabriken `Header`/`Entry`.

## `PlaylistDetailsState` / `PlaylistEntryItem`
`VideoPlayer.Mvu/PlaylistDetailsState.cs`

- `PlaylistDetailsState`: `PlaylistId`, `Name`, `Description`, `SortMode`, `Entries`, `NextPage` (1-basiert), `HasMore`, `IsLoading`/`IsLoadingMore`/`Attempted`/`Error`, Verwaltungsfelder (`CanManage`, `IsOwner`, `IsAdmin`, `IsPublic`, `GenreText`, `GenresManuallyOverridden`, `ManageNotice`, `PendingRemoveConflict`, `PendingSortModeConflict`, Genre-Optionen); `PageSize = 20`, `Empty`.
- `PlaylistEntryItem`: `EntryId`, `MediaId`, `MediaType`, `Title`, `ParentMediaType`/`ParentMediaId`/`ParentMediaTitle`, `ResolvedPictureId`, `EpisodeNumber`, `IsAccessible`, `ReleaseDate`, `SortOrder`.

## `PlaylistFormState` / `PlaylistGenreEditorState` / Ergebnisrecords
`VideoPlayer.Mvu/PlaylistManagementModels.cs`

- `PlaylistFormState`: `IsOpen`, `EditingPlaylistId`, `Name`, `Description`, `SortMode`, `IsSaving`, `Error`; `Closed`.
- `PlaylistGenreEditorState`: `IsOpen`, `PlaylistId`, `Options`, `IsLoading`, `IsSaving`, `Error`, `SelectedIds`; `Closed`.
- `PlaylistGenreOptionItem`: `Id`, `Name`, `IsSelected`.
- `PlaylistMutationOutcome(Succeeded, RequiresConfirmation)`, `PlaylistSortModeChangeResult(Playlist?, RequiresConfirmation)` — 409-Konflikt-Transport.
- (Für tvOS relevant: diese Typen decken den Schreibbereich ab, der laut Anforderung entfällt.)

## `FavoritesPageState` / `FavoriteListItem`
`VideoPlayer.Mvu/FavoritesPageState.cs` — `FavoriteListItem` (inkl. `FavoriteId`, `IsRemoving`, `ToMediaItem()`), `FavoritesPageState` (`Items`, Lade-/Fehlerzustand; `Initial`).

## `ProfilePageState` / `ProfileInfo`
`VideoPlayer.Mvu/ProfilePageState.cs` — `ProfileInfo(Username?, Email?, ProfileName?)`, `ProfilePageState` (`IsLoading`, `Attempted`, `Username`, `Email`, `ProfileName`; `Initial`).

## `PlayerEpisodeRef`
`VideoPlayer.Mvu/PlayerEpisodeRef.cs` — `record`: `MediaId`, `Title`.

## `AuthAttempt`
`VideoPlayer.Mvu/AuthAttempt.cs` — `AuthAttempt(AuthAttemptStatus, Username?)`, `Succeeded`-Ableitung (Enum siehe enums.md).

## `CachedShowEntry` / `CachedShowSeason` / `CachedShowEpisode` / `ShowEpisodeRef`
`VideoPlayer.Mvu/ShowDetailsCache.cs` — Cache-Records der Offline-Serienstruktur (`cached_lists`, Schlüssel `show:{id}`): `ShowId`/`Title`/`Seasons`; `Id`/`Number`/`Episodes`; `Id`/`Number`/`Title`/`WatchedAt`; `ShowEpisodeRef(SeasonNumber, Episode)`.

## `CachedPlaylistEntries` / `CachedPlaylistEntry`
`VideoPlayer.Mvu/PlaylistEntriesCache.cs` — Cache-Records (`cached_lists`, Schlüssel `playlist:{id}`): `PlaylistId`/`Entries`; `EntryId`/`MediaType`/`MediaId`/`SortOrder`/`IsAccessible`.

## `DownloadItemViewModel`
`VideoPlayer.Mvu/DownloadItemViewModel.cs` — `INotifyPropertyChanged`-Wrapper um `DownloadListItem` für In-Place-Updates (`Update(DownloadListItem)`); MAUI-Bindungs-typisch, aber ohne MAUI-Referenz.

## `AppTransition`
`VideoPlayer.Mvu/AppTransition.cs` — `AppTransition(Model, Effects)`, `Unchanged(model)`.

## `DevApiModelSnapshot` (+ `DevApiGenreOption`, `DevApiKnownServer`, `DevApiDiscoveredServer`)
`VideoPlayer.Mvu/DevApi/DevApiModelSnapshot.cs` — `#if DEBUG || DEV_API`; serialisierbarer Auszug von `AppModel` für `GET /model` (`FromModel(AppModel)`).

# Storage (`VideoPlayer.Storage`)

## `AppSettings`
`VideoPlayer.Storage/AppSettings.cs` — `record`, `Normalize()` klammert Werte.

| Eigenschaft | Typ | Beschreibung |
|---|---|---|
| `AutoDownloadEnabled` | `bool` | Auto-Download an/aus (Default aus) |
| `PreloadEpisodeCount` | `int` | Vorlauf-Episoden (1–10, Default 3) |
| `AutoResumeEnabled` | `bool` | Auto-Fortsetzung (Default an) |
| `WifiOnlyDownloads` | `bool` | Nur WLAN (Default an) |
| `WatchedEndWindowSeconds` | `int` | „Angesehen"-Endfenster (30–3600, Default 300) |
| `DownloadQuotaGb` | `int` | Quota (1–500, Default 5 = `DefaultDownloadQuotaGb`) |
| `DownloadRetentionDays` | `int` | Aufbewahrungszeit (1–365, Default 7 = `DefaultDownloadRetentionDays`) |
| `AutoDeletePlayedDownloads` | `bool` | Nach Abspielen löschen (Default aus) |
| `DeleteAfterPlaybackDelayMinutes` | `int` | Löschverzögerung (0–1440, Default 15) |
| `HomeHeroEnabled` | `bool` | Hero-Bereich (Default an) |
| `LoggingEnabled` | `bool` | Datei-Protokoll (Default aus) |

## `AppProfile`
`VideoPlayer.Storage/AppProfile.cs` — `EnvironmentVariable` = `VIDEOPLAYER_PROFILE`, `DefaultName` = `default`, `Name`, `PreferencesPrefix`, `DirectoryName`, `PrefixKey(string)`, `FromEnvironment(...)`, `Create(string?)`.

## `KnownServer`
`VideoPlayer.Storage/KnownServer.cs` — `record`: `Address`, `DisplayName`, `LastUsedAtUtc`, `NameOrAddress`.

## `ServerAddress`
`VideoPlayer.Storage/ServerAddress.cs` — `static`: `TryNormalize`, `Normalize` (kanonische Form `scheme://host[:port][/pfad]/`).

## `AppStorageLayout`
`VideoPlayer.Storage/AppStorageLayout.cs` — `class`: Backup-/NoBackup-/Cache-Wurzeln + Profil-Scoping; `BackupDirectory`, `DataDatabasePath` (`videoplayer.db3`), `NoBackupDirectory`, `CacheDatabasePath` (`videoplayer-cache.db3`), `VideosDirectory`, `PreviewsDirectory`, `LogsDirectory`, `TempDirectory`, `EnsureDirectoriesExist()`.

## Entities (SQLite, `VideoPlayer.Storage/Entities/`)

### `DownloadManifestEntry` — Tabelle `download_manifest` (nicht backup-pflichtig, in `CacheDatabase`)
`Id` (PK), `VideoId` (indiziert), `VideoType`, `FilePath`, `SizeBytes`, `Title`, `DownloadedBytes`, `TotalBytes`, `Error`, `State`, `ShowId`, `ShowTitle`, `SeasonNumber`, `EpisodeNumber`, `PlaylistId`, `PlaylistEntryId`, `PlaylistSortOrder`, `PictureId`, `UpdatedAtUtc`, `ExpiresAtUtc`, `Origin`.

### `KnownServerEntry` — Tabelle `known_servers` (backup-pflichtig, `BackupDatabase`)
`Id` (PK), `Address` (eindeutig indiziert), `DisplayName`, `LastUsedAtUtc`.

### `CachedListEntry` — Tabelle `cached_lists` (nicht backup-pflichtig, `CacheDatabase`)
`Key` (PK), `Json`, `UpdatedAtUtc`. Schlüssel u. a. `continue_watching`, `recent`, `sources`, `seasonal_genres`, `show:{id}`, `playlist:{id}`.

### `CachedFavoritesEntry` — Tabelle `cached_favorites` (`CacheDatabase`)
`Key` (PK), `VideoId`, `VideoType`, `Json`, `UpdatedAtUtc`.

### `PendingProgressSyncEntry` — Tabelle `pending_progress` (backup-pflichtig, `BackupDatabase`)
`Id` (PK), `VideoId` (indiziert), `VideoType`, `PositionTicks`, `DurationTicks`, `UpdatedAtUtc`, `PlaylistId` (nullable, migriert).

### `BackupStateEntry` — Tabelle `backup_state` (`BackupDatabase`)
`Key` (PK), `Value`; Konstante `LastConnectedServerKey`.

# API-Client-Modelle (`VideoPlayer.ApiClient/Models/`)

## `MediaEntry` (+ Vererbung `Movie`, `MovieCollection`, `TvShow`, `TvShowSeason`, `TvShowEpisode`, `Picture`)
`MediaEntry.cs` — polymorphe JSON-Basis (`$type`-Diskriminator): `Id`, `Name`, `MediaSourceId`, `ReleaseDate`, `PremieredAt`, `EndedAt`, `PosterPictureId`, `BannerPictureId`, `FanartPictureId`, `PosterPicture`, `BannerPicture`, `FanartPicture`, `IsFavorite`, `IsManuallyEdited`, `WatchedAt`. Subtypen: `Movie` (`GenreNames`, `Plot`, `Collection`), `MovieCollection` (`Movies`), `TvShow` (`Seasons`, `GenreNames`, `Plot`), `TvShowSeason` (`Number`, `Show`, `Episodes`), `TvShowEpisode` (`Season`, `Number`, `Plot`, `GeneratedBackgroundPictureId`), `Picture` (`Id`, `MediaItemId`, `Type`, `Width`, `Height`, `Description`, `Data`, `ContentType`).

## `Playlist` / `PlaylistEntriesPagedResult` / `PlaylistEntry`
`Playlist.cs`, `PlaylistEntry.cs`

- `Playlist`: `Id`, `Name`, `Description`, `SortMode`, `CreatedAt`, `UpdatedAt`, `Genres`, `AllGenreIds`, `GenresManuallyOverridden`, `CoverPictureId`, `CoverPictureIsUserUploaded`, `IsPublic`, `IsOwner`.
- `PlaylistEntriesPagedResult`: `Entries`, `TotalCount`, `HasNextPage`, `PageNumber`, `PageSize`.
- `PlaylistEntry`: `Id`, `PlaylistId`, `MediaType`, `MediaId`, `MediaTitle`, `ParentMediaType`/`ParentMediaId`/`ParentMediaTitle`, `AddedAt`, `ResolvedPictureId`, `IsAccessible`, `SortOrder`, `ReleaseDate`, `Plot`, `EpisodeNumber`.

## `ContinueWatchingEntry` / `PlaybackProgressReport` / `ContinueWatchingMutationResult`
`ContinueWatching.cs` — `MediaType`, `Entry` (`MediaEntry?`), `PositionSeconds`, `DurationSeconds`, `Title`, `PosterPictureId`, `PlaylistId`, `PlaylistName`, `PlaylistEntryId`; Report: `MediaType`, `MediaId`, `PositionSeconds`, `DurationSeconds`, `PlaylistId`; `ContinueWatchingMutationResult(Status, Message)`.

## `RecentEntry` / `FavoriteEntry` / `SeasonalGenre` / `MediaItemSummary` / `MediaSource` / `SourceGenres` / `Genre` / `GenreOption`
`RecentEntry.cs`, `FavoriteEntry.cs`, `SeasonalGenre.cs`, `MediaItemSummary.cs`, `MediaSource.cs`

- `RecentEntry`: `Id`, `MediaSourceId`, `PublishedAt`, `CreatedAt`, `Type` (`RecentEntryKind`), `Entry`.
- `FavoriteEntry`: `Id`, `Entry`, `CreatedAt`.
- `SeasonalGenre`: `GenreId`, `Name`, `ExpiresAt`, `Entries`.
- `MediaItemSummary`: `Type`, `Id`, `Title`, `Description`, `Url`, `CreatedAt`, `PictureId`, `ItemCount`.
- `MediaSource` (erbt `MediaEntry`): `CreatedAt`, `ClassifiedAt`, `Changed`, `LastScannedAt`, `IconPictureId`.
- `SourceGenres`: `SourceId`, `SourceName`, `Genres`; `Genre`: `Id`, `Name`, `IconUrl`; `GenreOption`: `Id`, `Name`.

## Pairing/Auth-Modelle
`AuthToken.cs` (`Token`, `Expires`), `LoginRequest.cs` (`Email`, `Password`), `RefreshTokenRequest.cs` (`RefreshToken`), `RefreshSessionResult.cs` (`Token`, `Expires`, `RefreshToken`), `BootstrapResult.cs` (`DeviceToken`, `Token`, `Expires`, `RefreshToken`), `PairingExchangeRequest.cs` (`code`, `clientPublicKey`, `deviceName`), `PairingExchangeResponse.cs` (`serverPublicKey`, `encryptedToken`), `PairingBootstrapRequest.cs` (`ticket`, `clientPublicKey`, `deviceName`), `PairingBootstrapResponse.cs` (`serverPublicKey`, `encryptedPayload`), `PairingBootstrapPayload.cs` (`deviceToken`, `token`, `expires`, `refreshToken`).

## Playlist-Wiedergabe/-Navigation
`PlaylistPlaybackStart.cs` (`PlaylistId`, `PlaylistName`, `TotalCount`, `CurrentPosition`, `CurrentEntryId`, `CurrentEntry`, `StreamUrl`, `MediaType`, `MediaId`, `StartPositionSeconds`), `PlaylistNavigationResult.cs` (`Entry`, `Position`, `StartPositionSeconds`), `PlaylistSortModeValues.cs` (`ByReleaseDate`, `Manual`, `GetDisplayLabel`).

## Playlist-Verwaltung (Schreibbereich — entfällt für tvOS)
`PlaylistAdminModels.cs`: `CreatePlaylistRequest`, `UpdatePlaylistRequest`, `AddPlaylistEntryRequest`, `PlaylistAddResult`, `PlaylistConflictResponse`, `ReorderPlaylistEntryRequest`, `ChangeSortModeRequest`, `SetPlaylistGenresRequest`, `SetPlaylistPublicRequest`, `PlaylistMaxSortOrderResult`.

## Sonstige
`HealthCheckResult.cs`, `ApiException.cs` (`Endpoint`, `ResponseBody`, `StatusCode`), `VideoPlayerApiClientOptions.cs` (`BaseAddress`, `ApiKey`, `TokenRefresher`, `DelayAsync`, `Logger`), `DiscoveredServer.cs` (`Address`, `Source`, `Name`, `NameOrAddress`).
