# Tests

Bestehende Tests des Download-Bereichs in `VideoPlayer.Maui.Tests/` (xUnit; SQLite-nahe Tests laufen in der Collection `SqliteDatabase` mit echten Temp-Datenbanken) sowie `VideoPlayer.E2E.Tests/`.

## Testklassen

### `DownloadServiceTests`
Datei: `VideoPlayer.Maui.Tests/DownloadServiceTests.cs` — Einreihen/Dedup, Parallelität, Fortschritt, Abbrechen, Löschen, Quota, WLAN-Bedingung, Wiederaufnahme.

- `Enqueue_Completes_AndWritesFileAndManifest` — kompletter Download: Datei + Manifest `completed`
- `EnqueueAsync_TransferOutcome_Logged` / `EnqueueAsync_DedupNotice_LogsBelowInformation` / `EnqueueAsync_QuotaExceeded_LogsSingleWarning` — Protokoll-Schichtung
- `Enqueue_WithSeriesContext_PersistsContextInManifest` — Serienkontext landet in `ShowId`/`SeasonNumber`/`EpisodeNumber`
- `Enqueue_WithPictureId_PersistsPictureIdInManifest` / `Enqueue_ReenqueueWithoutPictureId_KeepsStoredPictureId` — `PictureId`-Persistenz und `??=`-Semantik beim Re-Enqueue
- `Enqueue_CompletedExisting_ReturnsAlreadyPresent` / `Enqueue_QueuedExisting_ReturnsAlreadyQueued` — Dedup-Hinweise
- `Enqueue_TwoDownloads_RunInParallel` — `MaxParallelDownloads`
- `Enqueue_QuotaExhausted_FailsWithQuotaError` / `Enqueue_QuotaExceededDuringTransfer_FailsAndRemovesPartial` — Quota-Vorab- und Laufzeit-Prüfung
- `Cancel_RunningDownload_MarksCancelled` / `Cancel_QueuedDownload_MarksCancelled` — Abbruch
- `Enqueue_WifiOnlyWithoutWifi_StaysQueued` / `Resume_WifiOnly_WifiAvailable_StartsDownload` — WLAN-Bedingung
- `Resume_OrphanedDownloadingEntry_RestartsDownload` / `Resume_CancelledEntry_StaysUntouched` / `Resume_QuotaFailedEntry_StaysUntouched` — `ResumeInterruptedAsync`-Regeln
- `Enqueue_ServerError_FailsWithHttpError` / `Enqueue_TransportError_FailsWithNetworkError` / `Enqueue_WithoutLogin_FailsNoConnection` / `Enqueue_InvalidMediaType_Rejected` — Fehlerpfade
- `Delete_Completed_RemovesFileAndEntry` / `Delete_RunningDownload_RemovesEntry` — `DeleteAsync`
- `DeleteIfCompleted_QueuedEntry_StaysUntouched` — `DeleteIfCompletedAsync` greift nur bei `completed`
- `DeleteFailed_FailedAndCancelled_RemovesOnlyThose` / `DeleteFailed_NoMatchingEntries_ReturnsZero` — `DeleteFailedAsync`
- `Enqueue_FailedEntry_Retries` — Re-Enqueue fehlgeschlagener Einträge
- `Enqueue_ReportsProgressBytes` — Fortschritts-Meldung
- `LoadItems_ReturnsManifestOrder` — Reihenfolge der Liste

### `DownloadServiceReconcileTests`
Datei: `VideoPlayer.Maui.Tests/DownloadServiceReconcileTests.cs` — `ReconcileAsync`, `DeleteAllAsync`, `PurgeOrphansAsync`.

- `Reconcile_OrphanedFile_IsCountedAndKept` — Restdaten werden gezählt, nicht gelöscht
- `Reconcile_StalePartialOfFailedEntry_IsRemoved` / `…OfCancelledEntry_IsRemoved` / `…OfOrphanedDownloadingEntry_IsRemoved` — überholte Teildateien nicht-abgeschlossener Einträge werden entfernt
- `Reconcile_CompletedFile_IsKept` / `Reconcile_RunningTransferFile_IsKept` — `completed`-Dateien und laufende Transfers bleiben unberührt
- `Reconcile_CompletedWithoutFile_MarkedMissing` — „fertig" ohne Datei → `failed`/`missing_file`
- `Reconcile_QueuedEntryFile_IsRemoved` — Datei eines wartenden Eintrags = Teildatei
- `Reconcile_MissingDirectory_ReturnsEmpty` — fehlendes Verzeichnis → leeres Ergebnis
- `Reconcile_RelocatedFile_HealsManifestPath` — Container-Umzug: `FilePath`-Heilung
- `Enqueue_CompletedEntryWithRelocatedFile_NotRequeued` — Dedup auf geheiltem Pfad
- `DeleteAll_RemovesFilesAndManifest` / `PurgeOrphans_RemovesOnlyOrphaned` / `PurgeOrphans_RelocatedFile_IsKept` — Gesamtlöschung und Restdaten-Bereinigung

### `AutoDownloadServiceTests`
Datei: `VideoPlayer.Maui.Tests/AutoDownloadServiceTests.cs` — Kandidaten-Ermittlung, Serien-/Playlist-Pfad, Dedup, Throttle, Guards.

- `Run_MovieCandidate_EnqueuesDirectly` — Film lädt sich selbst
- `Run_EpisodeCandidate_EnqueuesUpcomingEpisodes` / `…_CrossesIntoNextSeason` — Serien-Vorlauf staffelübergreifend bis `PreloadEpisodeCount`
- `Run_EpisodeCandidate_EnqueuesCurrentFirst` / `…_AlreadyDownloaded_DeduplicatesAndCountsBudget` — aktuelle Episode zuerst; Dedup verbraucht Budget-Platz
- `Run_EpisodeWithoutSeason_EnqueuesSelf` — Orphan-Fallback ohne Serienbezug
- `Run_Disabled_DoesNothing` / `Run_WifiOnlyWithoutWifi_DoesNothing` / `Run_NotLoggedIn_DoesNothing` — Lauf-Guards
- `Run_ExistingManifestEntry_Deduplicates` — Dedup vorhandener Einträge
- `Run_Throttled_UnlessForced` — `MinRunInterval`/`force`
- `Run_PlaylistEpisode_EnqueuesPlaylistOrder` / `…_SkipsNonDownloadableEntries` / `…_PersistsSeriesHints` / `…_EnqueuesCurrentEntry` / `…_CurrentEntryNotAccessible_SkipsSelf` — Playlist-Pfad (Server-Reihenfolge, `IsAccessible`-Filter, Serien-Hints aus `ParentMedia*`)
- `Run_PlaylistMovie_EnqueuesSelfAndNextEntries` — Film + Folge-Einträge im Budget
- `Run_PlaylistEntryNotFound_FallsBackToSeries` / `Run_PlaylistEntryMissing_FallsBackToSeries` / `Run_PlaylistEntryNotFoundNoShow_EnqueuesSelf` — Rückfall-Pfade
- `RunAsync_LogsOutcome` — Protokoll-Eintrag des Laufs

### `AppUpdateTests_Downloads`
Datei: `VideoPlayer.Maui.Tests/AppUpdateTests_Downloads.cs` — MVU-Übergänge des Download-Bereichs.

- `DownloadsPageOpened_LoadsManifestList` / `…_WhileLoading_StaysUnchanged` — `LoadDownloadsEffect` beim Öffnen
- `DownloadsLoaded_AppliesItemsAndQuota` / `…_Error_SetsErrorState` / `…_OrphanedBytes_InState` — Ladeergebnis ins Model
- `DownloadsDeleteAll_WithItems_EmitsEffect` / `…_Empty_NoEffect`, `DownloadsPurgeOrphans_…`, `DownloadsDeleteFailed_…` — Gesamtaktionen mit Guards
- `DownloadRequested_Connected_EnqueuesDownload` / `…_Episode_EnqueuesDownload` / `…_NonPlayableType_Unchanged` / `…_InvalidId_Unchanged` / `…_Offline_ShowsNoticeWithoutEffect` / `…_WithPictureId_ForwardsToEffect` — Einreihe-Nachrichten (Offline-Guard: nur `Connected`)
- `DownloadEnqueueCompleted_UpsertsItem` / `…_MatchingDetails_SetsNotice` / `…_OtherDetails_IgnoresNotice` — Einplanungs-Rückmeldung
- `DownloadItemChanged_UpsertsProgress` / `DownloadDeleted_RemovesItem` — Listen-Pflege
- `DownloadCancelRequested_Active_EmitsCancelEffect` / `…_Completed_Unchanged`, `DownloadRetryRequested_Failed_EnqueuesAgain` / `…_ForwardsPictureId` / `…_Active_Unchanged`, `DownloadDeleteRequested_EmitsDeleteEffect` — Eintrags-Aktionen
- `LoginCompleted_Success_ResumesDownloads` / `NetworkOnline_WhileConnected_ResumesDownloads` — `ResumeDownloadsEffect`-Trigger
- `SyncCompleted_AutoDownloadsEnabled_RunsAutoDownloads` / `…_Disabled_NoAutoRun`, `CachedListsChanged_AutoDownloadsEnabled_RunsAutoDownloads` / `…_NotConnected_NoAutoRun`, `SaveSettings_EnablingAutoDownloads_ForcesAutoRun` / `…_AlreadyEnabled_NoForcedRun` — `RunAutoDownloadsEffect`-Trigger
- `PlaybackProgress_TwoWindowTicks_EmitsAutoDelete` / `…_WatchedMovie_NoAutoDelete` / `…_AutoDeleteDisabled_NoEffect`, `PlaybackEnded_WatchedEpisode_EmitsAutoDelete` — `DeleteWatchedDownloadEffect` an der Angesehen-Schwelle bzw. am Ende (nur Episoden)

### `AppUpdateTests_Player` (Nebenbezug)
Datei: `VideoPlayer.Maui.Tests/AppUpdateTests_Player.cs` — Endfenster-Logik, deren Auslöser die bisherige Sofortlöschung nutzt: `PlaybackProgress_*` (Streak-Aufbau/-Rücksetzung: `…_MarksWatchedWithinWindow`, `…_SecondWindowTick_MarksWatched`, `…_FrozenPosition_ResetsStreak`, `…_WhileResolving_NoWindowEffects` u. a.) und `PlaybackEnded_*` (Plausibilität: `…_MidPlayback_IsIgnored`, `…_WithinTolerance_Completes`, `…_MarksWatchedAndAdvances` u. a.), `PlaybackSourceResolved_*` (Quelle übernehmen, Fehler-Rückfall, Stale-Verwerfung).

### `DownloadsContractTests`
Datei: `VideoPlayer.Maui.Tests/DownloadsContractTests.cs` — XAML-/Verdrahtungs-Contracts (textuelle Prüfung).

- `DownloadsPage_ShowsItems_WithStatusAndProgress` / `…_HasCancelRetryDeleteActions` / `…_DispatchesMessages` / `…_SyncsItems_WithoutItemsSourceReset` / `…_WiresDeleteFailed` / `…_ShowsOrphanedHint` / `…_ShowsPreviewImage_FromPictureCache` — Seiten-Contract
- `MediaDetailsPage_HasDownloadButtons` — Download-Schalter auf der Detailseite
- `MauiProgram_RegistersDownloadServices` / `ShellAppEffectHandler_WiresDownloadEffects` / `DownloadService_Reconcile_WiredInHandler` — DI-/Effekt-Verdrahtung (inkl. `ReconcileAsync`-Aufruf im Handler)
- `ConnectivityMonitor_ReportsWifi` / `StorageLayout_DownloadsUnderNoBackup` — Infrastruktur
- `DownloadsPageState_HasFailedItems_OnlyFailedOrCancelled` / `…_OrphanedText_…` — State-Ableitungen
- `DownloadListItem_MissingFile_ErrorText` / `…_HasPicture_OnlyWithPositivePictureId` / `…_PreviewWidth_EpisodeWiderThanMovie` — Eintrags-Anzeige
- `IosBackgroundDownloadBackend_Wired` / `DownloadService_UsesBackend_AndCompletesExternal` — iOS-Backend

### `DownloadListSyncTests`
Datei: `VideoPlayer.Maui.Tests/DownloadListSyncTests.cs` — `Sync_…` (Insert/Update-in-place ohne `CollectionChanged`/Remove/Move/Reorder, Identität `EntryId` vs. `MediaId`+`MediaType`) und `ViewModel_…` (`Update`-Rückgabe, `PropertyChanged`-Benachrichtigungen, Durchreich-Properties).

### `PlaybackSourceServiceTests`
Datei: `VideoPlayer.Maui.Tests/PlaybackSourceServiceTests.cs` — `ResolveAsync_*`: Stream-URL mit Token, Fortsetzungsposition Server/Cache/Override, `…_CompletedDownload_ReturnsLocalFile`, `…_MissingFile_FallsBackToStream`, `…_IncompleteDownload_FallsBackToStream`, `…_OfflineMode_…`, `…_LogsKind_NotLocation`, `…_NonPlayable_ReturnsNull`.

### `OfflineContinueWatchingServiceTests`
Datei: `VideoPlayer.Maui.Tests/OfflineContinueWatchingServiceTests.cs` — `ContinueWatching_…` (nur `completed` mit Datei, Filter, Platzhalter fehlender Folge-Episoden über `ShowDetailsCache`, Fortschritt aus Outbox/Cache-Fallback) und `HomeLists_…` (Favoriten/Neuheiten/Saisonales aus dem Cache).

### `AppSettingsTests` / `AppSettingsServiceTests` / `SettingsStorageContractTests`
Dateien: `VideoPlayer.Maui.Tests/AppSettingsTests.cs`, `AppSettingsServiceTests.cs`, `SettingsStorageContractTests.cs`

- `Defaults_AreExpected`, `Normalize_ClampsPreloadEpisodeCount`/`…WatchedEndWindowSeconds`/`…DownloadQuotaGb`, `Normalize_LeavesBooleansUntouched` — Defaults und Clamp-Grenzen
- `Load_WhenStoreEmpty_ReturnsDefaults`, `Save_ThenLoad_RoundTrips`, `Load_WithoutLoggingKey_KeepsLoggingDisabled`, `Load_WithoutHomeHeroKey_KeepsHeroEnabled`, `Load_ClampsOutOfRangeStoredValues`, `Save_NormalizesBeforeWriting`, `Profiles_DoNotInterfere`, `UsesProfilePrefixedKeys` — Persistenz (fehlende Schlüssel = Defaults, etabliertes Muster für neue Einstellungs-Schlüssel)
- `SettingsPage_ContainsAllSettingControls` / `…_WiresMvuHostAndSaveMessage` — Contract der Einstellungsseite

### `LocalDatabaseTests` (Schema/Migrationen)
Datei: `VideoPlayer.Maui.Tests/LocalDatabaseTests.cs` — `CacheDatabase_CreatesTablesAndRoundTrips`, `…_HasOfflineData_…`, `CacheDatabase_MigratesSeriesContextOntoExistingManifest` / `…_MigratesPlaylistContextOntoExistingManifest` / `…_MigratesPictureIdOntoExistingManifest` (Migrations-Muster: Tabelle ohne Spalte anlegen → `InitializeAsync` → Spalte vorhanden + Altbestand `null`), `…_RecoversCorruptFile`, `Databases_LandInClassifiedDirectories`.

### `EnumCoverageTests`
Datei: `VideoPlayer.Maui.Tests/EnumCoverageTests.cs` — `PlaybackSourceKind_AllValuesAreCovered` (Anzeige-Abdeckung; `DownloadState` ist nicht enthalten).

### `DownloadsTests` (E2E)
Datei: `VideoPlayer.E2E.Tests/Tests/DownloadsTests.cs` — `DownloadsPage_DeleteFailed_RemovesFailedEntry`: fehlgeschlagener Download über die Detailseite → Downloads-Seite → „Alle Fehler löschen" mit Rückfrage → Eintrag entfernt, Schalter ausgeblendet.

## Hilfsmethoden und Stubs

### `DownloadServiceTests` / `DownloadServiceReconcileTests` / `AutoDownloadServiceTests` (klassenintern)
- `CreateService(quotaBytesOverride)` — `DownloadService` mit `StubHttpMessageHandler`, `File.Exists`, optionalem Quota-Override
- `Settings(wifiOnly, autoDelete)` — `AppSettings`-Fixture (inkl. `AutoDeleteWatchedEpisodes`)
- `LoginAsync` — Anmeldung am `Mock`-Token-Endpunkt
- `EnqueueStream(size)` — Stream-Antwort in den Handler-Queue legen
- `ReadEntryAsync(type, mediaId)` / `EntriesAsync` — Manifest direkt aus SQLite lesen
- `WaitForStateAsync(read, state, timeoutMs)` / `WaitForAsync(done, timeoutMs)` — asynchrones Warten auf Zustände
- `InsertEntryAsync(state, filePath, mediaId)` — Manifest-Zeile direkt einfügen (Reconcile-Tests)
- `WriteFile(name, size)` — Datei im `VideosDirectory` anlegen
- `CacheContinueWatchingAsync(entries)` — `cached_lists[continue_watching]` befüllen
- `ShowWithEpisodes(id, name, eps…)` — `TvShow`-Graph mit Staffeln/Episoden für `GetTvShowAsync`
- `PlaylistPageJson(hasNextPage, entries…)` / `PlaylistEntryJson(id, mediaType, mediaId, title, parentMedia*, isAccessible, sortOrder, episodeNumber)` — JSON-Fixtures der `entries/paged`-Antwort
- `GatedContent` — `HttpContent` mit Start-/Freigabe-Gates (Parallelitäts-Test)
- `FakeNetworkMonitor` — `INetworkStatusMonitor`-Stub (`IsOnline`, `IsWifi`, `SetOnline`)

### `VideoPlayer.Maui.Tests/Stubs`
- `SqliteDatabaseCollection` — xUnit-Collection (`SQLitePCL.Batteries_V2.Init` + Pool-Reset)
- `StubHttpMessageHandler` — Queue-basierter `HttpMessageHandler` (`Enqueue`, `EnqueueJson`, `Requests`)
- `InMemorySettingsStore` / `InMemorySecretStore` — `ISettingsStore`-/`ISecretStore`-Stubs
- `RecordingLoggerProvider` / `RecordedLogEntry` / `RecordingDelays` — Protokoll-/Delay-Stubs
- `RepositoryRoot` — Repo-Pfadauflösung für Contract-Tests
