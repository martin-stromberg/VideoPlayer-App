# Plan-Review

## Ergebnis

**Status:** Vollständig umgesetzt

## Umgesetzte Planelemente

- [x] `DownloadOrigin` (Enum, `VideoPlayer.Mvu/DownloadOrigin.cs`) — angelegt (`Manual`, `Auto`)
- [x] `DownloadOriginNames` (statische Klasse, `VideoPlayer.Mvu/DownloadOrigin.cs`) — `ToPersisted`/`FromPersisted`; `null`/unbekannt → `Manual`
- [x] `CachedPlaylistEntry` (Datenmodellklasse, `VideoPlayer.Mvu/PlaylistEntriesCache.cs`) — angelegt (`EntryId`, `MediaType`, `MediaId`, `SortOrder`, `IsAccessible`)
- [x] `CachedPlaylistEntries` (Datenmodellklasse, `VideoPlayer.Mvu/PlaylistEntriesCache.cs`) — angelegt (`PlaylistId`, geordnete `Entries`)
- [x] `PlaylistEntriesCache` (statische Klasse, `VideoPlayer.Mvu/PlaylistEntriesCache.cs`) — `KeyFor` (`playlist:{id}`), `WriteAsync`, `ReadAsync` nach `ShowDetailsCache`-Muster
- [x] `ExpireWatchedDownloadEffect` (`AppEffect`-Record, `VideoPlayer.Mvu/AppEffect.cs:786`) — angelegt
- [x] `ProtectDownloadExpiryEffect` (`AppEffect`-Record, `VideoPlayer.Mvu/AppEffect.cs:798`) — angelegt
- [x] `DeleteWatchedDownloadEffect` — entfernt (keine Referenz mehr im Code)
- [x] Feld `ExpiresAtUtc` (`DateTime?`) in `DownloadManifestEntry` — vorhanden (`VideoPlayer.Storage/Entities/DownloadManifestEntry.cs:101`)
- [x] Feld `Origin` (`string?`) in `DownloadManifestEntry` — vorhanden (`DownloadManifestEntry.cs:107`)
- [x] Methode `CacheDatabase.InitializeAsync` — zwei `EnsureColumnAsync`-Aufrufe ergänzt (`CacheDatabase.cs:53-54`: `ExpiresAtUtc` `bigint NULL`, `Origin` `varchar NULL`)
- [x] Feld `DownloadRetentionDays` (`int`, Standard 7, Konstante `DefaultDownloadRetentionDays`) in `AppSettings` — vorhanden (`AppSettings.cs:47`)
- [x] Feld `AutoDeletePlayedDownloads` (`bool`, Standard `false`) in `AppSettings` — vorhanden (`AppSettings.cs:55`)
- [x] Feld `DeleteAfterPlaybackDelayMinutes` (`int`, Standard 15, Konstante `DefaultDeleteAfterPlaybackDelayMinutes`) in `AppSettings` — vorhanden (`AppSettings.cs:62`)
- [x] Feld `AutoDeleteWatchedEpisodes` in `AppSettings` — entfernt
- [x] Methode `AppSettings.Normalize()` — Clamps `DownloadRetentionDays` (1–365) und `DeleteAfterPlaybackDelayMinutes` (0–1440) ergänzt (`AppSettings.cs:86-87`)
- [x] Konstanten `DownloadRetentionDaysKey`, `AutoDeletePlayedDownloadsKey`, `DeleteAfterPlaybackDelayMinutesKey` in `AppSettingsService` — vorhanden (`AppSettingsService.cs:29-35`); `AutoDeleteWatchedEpisodesKey` als Lese-Fallback-Konstante erhalten (`:43`)
- [x] Methode `AppSettingsService.Load()` — neue Schlüssel gelesen; Migration `AutoDeletePlayedDownloads` mit Fallback auf `AutoDeleteWatchedEpisodesKey` (`LoadAutoDeletePlayedDownloads`, `:113-122`)
- [x] Methode `AppSettingsService.Save()` — schreibt nur noch die neuen Schlüssel (`:91-101`)
- [x] Konstruktor `DownloadService` (internal) — optionaler Parameter `Func<DateTime>? utcNow` vorhanden (`DownloadService.cs:171,185`)
- [x] Methode `EnqueueAsync` in `DownloadService` — Parameter `DownloadOrigin origin = DownloadOrigin.Manual` (`:258`); Neuanlage persistiert `Origin` (`:380`); Re-Enqueue behält `Origin` und setzt `ExpiresAtUtc = null` (`:359`); `AlreadyPresent`-Dedup bei `origin == Auto` verlängert `ExpiresAtUtc` und setzt `Origin = auto` + `UpdateAsync` + `ItemChanged` (`:338-345`)
- [x] Methode `TransferAsync` in `DownloadService` — Erfolgspfad setzt `ExpiresAtUtc = utcNow() + Retention` vor dem `Completed`-Übergang (`:1279-1280`)
- [x] Methode `CompleteExternalTransferAsync` in `DownloadService` — neuer Parameter `AppSettings settings` (`:690`); Erfolgspfad setzt `ExpiresAtUtc` identisch (`:711`)
- [x] Methode `ReconcileAsync` in `DownloadService` — optionale Parameter `AppSettings? settings`, `IReadOnlySet<string>? autoDownloadCandidateKeys` (`:908-911`); Ablauf-Pass nur bei `settings != null`, nur `completed`-Einträge ohne laufenden Transfer, `ExpiresAtUtc == null || <= utcNow()`; Kandidat → Verlängerung + `auto` + `ItemChanged` + `ExtendedExpiryCount`, sonst Datei + Zeile entfernt + `ItemRemoved` + `RemovedExpiredCount` (`:1025-1072`); Protokollzeile ergänzt (`:1078-1090`)
- [x] Methode `ExpireAfterAsync` in `DownloadService` — neu (`:541-565`): nur `completed` → `ExpiresAtUtc = utcNow() + delay` + `ItemChanged`, sonst `false`
- [x] Methode `ExtendExpiryAsync` in `DownloadService` — neu (`:580-609`): nur `completed`, nur Hochsetzung (`null` oder früher → `untilUtc`) + `ItemChanged`, sonst `false`
- [x] Methode `DeleteIfCompletedAsync` in `DownloadService` — entfernt (nicht mehr auffindbar)
- [x] Methode `ManifestKey` (`internal static`, `{type}:{mediaId}` normalisiert) in `DownloadService` — vorhanden (`:1446-1447`); `Key` delegiert darauf (`:1449`)
- [x] Methode `ToItem` in `DownloadService` — mappt `Origin` (via `DownloadOriginNames.FromPersisted`) und `ExpiresAtUtc` (`:1481-1482`)
- [x] Felder `RemovedExpiredCount` (`int`), `ExtendedExpiryCount` (`int`) in `DownloadReconcileResult` — vorhanden (`DownloadReconcileResult.cs:34,38`)
- [x] Methode `GetAutoDownloadCandidateKeysAsync` in `AutoDownloadService` — neu (`AutoDownloadService.cs:215-259`): leere Menge bei `!AutoDownloadEnabled`; Selbst-Keys + Serien-Vorlauf (`ShowDetailsCache`, `:334-367`) + Playlist-Vorlauf (`PlaylistEntriesCache`, `:274-321`) je bis `PreloadEpisodeCount`; Einzelfehler protokolliert/übersprungen
- [x] Methoden `EnqueueSelfAsync`/`EnqueueEpisodesFromCurrentAsync`/`EnqueuePlaylistEntriesFromCurrentAsync` in `AutoDownloadService` — `origin: DownloadOrigin.Auto` an allen `EnqueueAsync`-Aufrufen (`:629`, `:417`, `:525`); Playlist-Pfad persistiert geladene Einträge via `PlaylistEntriesCache.WriteAsync` (`:544`)
- [x] Feld `DownloadExpiryProtected` (`bool`, Standard `false`) in `PlayerState` — vorhanden (`PlayerState.cs:140`)
- [x] Methode `EvaluateEndWindow` in `AppUpdate` — `AutoDeletePlayedDownloads` + `ExpireWatchedDownloadEffect` ohne Medientyp-Einschränkung (`AppUpdate.cs:3941-3946`)
- [x] Methode `UpdatePlaybackEnded` in `AppUpdate` — dieselbe Ersetzung (`:4011-4016`)
- [x] Methode `UpdatePlaybackProgress` in `AppUpdate` — Wiedergabe-Schutz beim ersten Takt mit bekannter Dauer bei `LocalFile`-Quelle: Flag setzen + `ProtectDownloadExpiryEffect` bei `remaining > 0` (`:3867-3878`)
- [x] Flag-Resets `DownloadExpiryProtected = false` in `AppUpdate` — `UpdatePlaylistNavigated` (`:2613`), `AdvancePlaylistEntry` (`:2642`), `AdvanceToNextEpisode` Fehler-/Normalpfad (`:3769`, `:3794`); `StartPlayback` erzeugt `new PlayerState` (Default `false`, `:3653`)
- [x] Methode `HandleAsync` in `ShellAppEffectHandler` — `DeleteWatchedDownloadEffect`-Case entfernt; neue Cases `ExpireWatchedDownloadEffect` → `ExpireAfterAsync` (`:328-330`, `:1563-1580`) und `ProtectDownloadExpiryEffect` → `ExtendExpiryAsync(DateTime.UtcNow + 2 × RemainingDuration)` (`:331-333`, `:1591-1605`), jeweils fehlertolerant
- [x] Methode `LoadDownloadsAsync` in `ShellAppEffectHandler` — `GetAutoDownloadCandidateKeysAsync` + `ReconcileAsync(settings, keys, ct)` (`:1485-1486`)
- [x] Methode `ResumeDownloadsAsync` in `ShellAppEffectHandler` — identisch verdrahtet (`:1673-1674`)
- [x] Methode `EnqueueDownloadAsync` in `ShellAppEffectHandler` — `origin: DownloadOrigin.Manual` explizit (`:1510`)
- [x] Methode `DeleteDownloadAsync` in `ShellAppEffectHandler` — `completedOnly`-Zweig entfernt (`:1532-1551`)
- [x] `MauiProgram` (iOS-`CompletionSink`) — `CompleteExternalTransferAsync` mit via `AppSettingsService` geladenen `AppSettings` (`MauiProgram.cs:155-160`)
- [x] Felder `Origin` (`DownloadOrigin`, init-Default `Manual`), `ExpiresAtUtc` (`DateTime?`), `OriginText`, `HasOriginText`, `ExpiresText`, `HasExpiresText` in `DownloadListItem` — vorhanden (`DownloadListItem.cs:46-68`; „Cache" bei `Auto`; „Läuft ab am {dd.MM.yyyy HH:mm}" nur bei `completed` mit Datum)
- [x] Durchreich-Properties `OriginText`/`HasOriginText`/`ExpiresText`/`HasExpiresText` in `DownloadItemViewModel` — vorhanden (`DownloadItemViewModel.cs:68-77`); `Update` meldet `PropertyChanged` für alle vier (`:126-129`)
- [x] `DownloadsPage.xaml` — „Cache"-Badge (`Text="{Binding OriginText}"`, `IsVisible="{Binding HasOriginText}"`, Pill-Chip, `:159-178`) und Ablauf-Label (`ExpiresText`/`HasExpiresText`, Caption-Stil, `:180-183`) ergänzt
- [x] `SettingsPage.xaml` — `AutoDeletePlayedSwitch` („Downloads nach dem Abspielen automatisch löschen", `:110-113`), `DownloadRetentionDaysEntry` („Aufbewahrungszeit für Downloads (Tage)", Numeric, Placeholder „Standard: 7", `:93-101`), `DeleteAfterPlaybackDelayEntry` („Löschverzögerung nach dem Abspielen (Minuten)", Placeholder „Standard: 15", `:120-128`); `AutoDeleteWatchedSwitch` entfernt
- [x] `SettingsPage.xaml.cs` — `OnAutoDeletePlayedToggled` → `AutoDeletePlayedDownloads`; `PopulateFromModel` + `WireNumeric`/`SaveNumeric` für beide neuen Felder verdrahtet (`:32,39-40,76,83-84,121-158`)
- [x] Test `Enqueue_Manual_PersistsOriginManual` (`DownloadServiceTests.cs:618`)
- [x] Test `Enqueue_Auto_PersistsOriginAuto` (`AutoDownloadServiceTests.cs:869`)
- [x] Test `Enqueue_Reenqueue_KeepsOrigin` (`DownloadServiceTests.cs:632`)
- [x] Test `Enqueue_AutoAlreadyPresent_ExtendsExpiryAndSwitchesOrigin` (`DownloadServiceTests.cs:807`) + Gegenprobe `Enqueue_ManualAlreadyPresent_KeepsExpiryAndOrigin` (`:840`)
- [x] Test `Transfer_Completed_SetsExpiresAtUtc` (`DownloadServiceTests.cs:651`)
- [x] Test `CompleteExternal_Completed_SetsExpiresAtUtc` (`DownloadServiceTests.cs:667`)
- [x] Tests `Reconcile_ExpiredCompleted_RemovesFileAndEntry`, `Reconcile_ExpiredCandidate_ExtendsAndSetsAuto`, `Reconcile_LegacyEntryWithoutExpiry_TreatedAsExpired`, `Reconcile_ExpiredNonCandidate_QueuedOrRunning_NotTouched`, `Reconcile_WithoutSettings_SkipsExpiryPass` (`DownloadServiceReconcileTests.cs:373-471`)
- [x] Tests `ExpireAfter_CompletedEntry_SetsExpiry`/`_NonCompleted_NoOp`, `ExtendExpiry_RaisesEarlierOrNull`/`_LaterExpiry_Kept`/`_NonCompleted_NoOp` (`DownloadServiceTests.cs:695-805`)
- [x] Tests `GetCandidateKeys_SelfAndSeriesAndPlaylist`, `_AutoDisabled_Empty`, `_MissingCaches_SelfOnly`, `PlaylistCache_WriteRead_RoundTrip` (`AutoDownloadServiceTests.cs:927-1063`)
- [x] Tests `PlaybackProgress_LocalFileFirstTick_EmitsProtectEffect`, `_LocalFileSecondTick_NoRepeat`, `_StreamSource_NoEffect`, `_UnknownDuration_NoEffect` (`AppUpdateTests_Player.cs:1033-1097`)
- [x] Tests `PlaybackProgress_TwoWindowTicks_EmitsExpireEffect`, `PlaybackProgress_WatchedMovie_EmitsExpireEffect`, `PlaybackProgress_AutoDeleteDisabled_NoEffect`, `PlaybackEnded_WatchedEpisode_EmitsExpireEffect` (`AppUpdateTests_Downloads.cs:530-660`)
- [x] Tests `Defaults_AreExpected`, `Normalize_ClampsDownloadRetentionDays`, `Normalize_ClampsDeleteAfterPlaybackDelayMinutes` (`AppSettingsTests.cs:10-97`)
- [x] Tests `Load_MigratesAutoDeleteWatchedEpisodes`, `Load_MigratesAutoDeleteWatchedEpisodes_NewKeyWins`, `Save_DoesNotWriteLegacyAutoDeleteKey` (`AppSettingsServiceTests.cs:124-159`)
- [x] Test `CacheDatabase_MigratesExpiryAndOriginOntoExistingManifest` (`LocalDatabaseTests.cs:348-387`)
- [x] Tests `DownloadListItem_AutoOrigin_CacheText`, `DownloadListItem_ExpiresText_OnlyCompletedWithExpiry`, `DownloadOriginNames_FromPersisted_NullIsManual` (`DownloadsContractTests.cs:352-411`); Contract-Tests `ShellAppEffectHandler_WiresDownloadEffects` (`:117`), `DownloadService_Reconcile_WiredInHandler` (`:165`), `DownloadService_UsesBackend_AndCompletesExternal` (`:342`), `IosBackgroundDownloadBackend_Wired` (`:326`) angepasst
- [x] Test `SettingsPage_ContainsAllSettingControls` erweitert (`SettingsStorageContractTests.cs:18-63`: `AutoDeletePlayedSwitch`, `DownloadRetentionDaysEntry`, `DeleteAfterPlaybackDelayEntry`; `AutoDeleteWatchedSwitch` nicht mehr erwartet)
- [x] Hilfsmethode `InsertEntryAsync(…, expiresAtUtc, origin)` erweitert (`DownloadServiceReconcileTests.cs:85-102`)
- [x] Hilfsmethoden `CacheShowAsync`/`CachePlaylistAsync` angelegt (`AutoDownloadServiceTests.cs:895-925`)
- [x] Betroffene bestehende Tests migriert: `DeleteIfCompleted_QueuedEntry_StaysUntouched` entfernt; `PlaybackProgress_*_AutoDelete`-Tests auf `ExpireWatchedDownloadEffect` umgestellt; Fixture `Settings(autoDelete)` → `AutoDeletePlayedDownloads` (`DownloadServiceTests.cs:84`)
- [x] E2E-Test „Cache"-Herkunftsanzeige: `DownloadsTests.DownloadsPage_AutoDownload_ShowsCacheBadge` (`VideoPlayer.E2E.Tests/Tests/DownloadsTests.cs:108-143`)
- [x] E2E-Test „Nach dem Abspielen löschen" + Bereinigungslauf: `DownloadsTests.DownloadsPage_WatchedDownload_IsCleanedUpAfterExpiry` (`DownloadsTests.cs:151-224`)

## Hinweise

- Benamungsabweichungen gegenüber dem Plan, ohne fachliche Lücke: `Reconcile_ExpiredNonCandidate_QueuedOrRunning_NotTouched` (Plan: `…_ManualStaysDeleted` / `_QueuedOrRunning_NotTouched` — „oder"-Variante), `PlaybackProgress_AutoDeleteDisabled_NoEffect` (Plan: `_Disabled_NoEffect`), `Load_MigratesAutoDeleteWatchedEpisodes_NewKeyWins` (Plan: `_NewKeyWins`).
- Für zwei Aufgaben gibt es keinen direkten Test (in der Tasks-Datei entsprechend vermerkt): die Entfernung von `DeleteIfCompletedAsync` (Aufgabe 20 — toter Code würde keinen Test scheitern lassen) und die explizite Übergabe von `origin: DownloadOrigin.Manual` in `EnqueueDownloadAsync` (Aufgabe 30 — der Parameter ist Default und verhaltensgleich; die Service-Seite ist über `Enqueue_Manual_PersistsOriginManual` abgesichert).
- Der E2E-Test `DownloadsPage_WatchedDownload_IsCleanedUpAfterExpiry` realisiert den Bereinigungs-Trigger über einen App-Neustart (Start-Abgleich via `ResumeDownloadsEffect`) statt über das erneute Öffnen der Downloads-Seite — beide Trigger sind plan-konform.
- Die Tasks-Datei wurde aktualisiert: alle 42 Aufgaben auf `Erledigt` mit Testnachweisen gesetzt.
