# Tasks: Downloadbereinigung — Ablaufdatum, Herkunft und automatische Bereinigung von Downloads

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Datenmodell | `DownloadOrigin`-Enum + `DownloadOriginNames` (`ToPersisted`/`FromPersisted`, `null`/unbekannt → `Manual`) in `VideoPlayer.Mvu/DownloadOrigin.cs` anlegen | Offen | — |
| 2 | Datenmodell | `DownloadManifestEntry` um `ExpiresAtUtc` (`DateTime?`) und `Origin` (`string?`) erweitern | Offen | — |
| 3 | Datenmodell | `CacheDatabase.InitializeAsync` um `EnsureColumnAsync("download_manifest", "ExpiresAtUtc", "bigint NULL")` und `EnsureColumnAsync("download_manifest", "Origin", "varchar NULL")` ergänzen | Offen | — |
| 4 | Datenmodell | `DownloadReconcileResult` um `RemovedExpiredCount` und `ExtendedExpiryCount` erweitern | Offen | — |
| 5 | Datenmodell | `PlayerState` um `DownloadExpiryProtected` (`bool`, Standard `false`) erweitern | Offen | — |
| 6 | Datenmodell | `DownloadListItem` um `Origin`, `ExpiresAtUtc` sowie abgeleitete `OriginText`/`HasOriginText` („Cache") und `ExpiresText`/`HasExpiresText` („Läuft ab am …") erweitern | Offen | — |
| 7 | Datenmodell | `CachedPlaylistEntry`/`CachedPlaylistEntries`-Records + statische `PlaylistEntriesCache`-Klasse (`KeyFor` `playlist:{id}`, `WriteAsync`, `ReadAsync`) in `VideoPlayer.Mvu/PlaylistEntriesCache.cs` anlegen | Offen | — |
| 8 | Konfiguration | `AppSettings` um `DownloadRetentionDays` (7), `AutoDeletePlayedDownloads` (aus) und `DeleteAfterPlaybackDelayMinutes` (15) erweitern; `AutoDeleteWatchedEpisodes` entfernen; `Normalize()`-Clamps (1–365 Tage, 0–1440 Minuten) | Offen | — |
| 9 | Konfiguration | `AppSettingsService`: drei neue Schlüssel-Konstanten; `Load()` liest neue Schlüssel inkl. Fallback von `AutoDeletePlayedDownloads` auf `AutoDeleteWatchedEpisodesKey`; `Save()` schreibt nur noch neue Schlüssel | Offen | — |
| 10 | Logik | `DownloadService`: internen Konstruktor um `Func<DateTime>? utcNow` erweitern | Offen | — |
| 11 | Logik | `DownloadService.EnqueueAsync`: Parameter `DownloadOrigin origin = DownloadOrigin.Manual`; `Origin` bei Neuanlage setzen, bei Re-Enqueue behalten und `ExpiresAtUtc = null` zurücksetzen | Offen | — |
| 12 | Logik | `DownloadService.EnqueueAsync`: im `AlreadyPresent`-Dedup-Zweig bei `origin == Auto` `ExpiresAtUtc` verlängern (`utcNow + DownloadRetentionDays`) und `Origin = auto` setzen (Kriterium 12) | Offen | — |
| 13 | Logik | `DownloadService.TransferAsync`: Erfolgspfad setzt `ExpiresAtUtc = utcNow() + Retention` vor dem `Completed`-Übergang | Offen | — |
| 14 | Logik | `DownloadService.CompleteExternalTransferAsync`: Parameter `AppSettings settings` ergänzen und im Erfolgspfad `ExpiresAtUtc` setzen | Offen | — |
| 15 | Logik | `DownloadService.ExpireAfterAsync(mediaType, mediaId, delay, ct)` neu: `completed`-Eintrag → `ExpiresAtUtc = utcNow() + delay`, sonst `false` | Offen | — |
| 16 | Logik | `DownloadService.ExtendExpiryAsync(mediaType, mediaId, untilUtc, ct)` neu: `completed`-Eintrag → `ExpiresAtUtc` hochsetzen, wenn `null` oder früher | Offen | — |
| 17 | Logik | `DownloadService`: `ManifestKey(mediaType, mediaId)` als `internal static` exposen (`{type}:{mediaId}`, Typ normalisiert); `Key` delegiert darauf | Offen | — |
| 18 | Logik | `DownloadService.ReconcileAsync`: optionale Parameter `AppSettings? settings`, `IReadOnlySet<string>? autoDownloadCandidateKeys`; Ablauf-Pass für `completed`-Einträge mit `ExpiresAtUtc == null || <= utcNow()` (Kandidat → verlängern + `auto` + `ItemChanged`; sonst Datei+Zeile entfernen + `ItemRemoved`) | Offen | — |
| 19 | Logik | `DownloadService.ToItem`: `Origin` und `ExpiresAtUtc` in den `DownloadListItem`-Snapshot mappen | Offen | — |
| 20 | Logik | `DownloadService.DeleteIfCompletedAsync` entfernen | Offen | — |
| 21 | Logik | `AutoDownloadService`: `origin: DownloadOrigin.Auto` an allen `EnqueueAsync`-Aufrufen (`EnqueueSelfAsync`, `EnqueueEpisodesFromCurrentAsync`, `EnqueuePlaylistEntriesFromCurrentAsync`) | Offen | — |
| 22 | Logik | `AutoDownloadService.GetAutoDownloadCandidateKeysAsync(settings, ct)` neu: leere Menge bei `!AutoDownloadEnabled`; Weiterschauen-Cache → Selbst-Keys + Serien-Vorlauf über `ShowDetailsCache` + Playlist-Vorlauf über `PlaylistEntriesCache` (je bis `PreloadEpisodeCount` ab dem aktuellen Eintrag) | Offen | — |
| 23 | Logik | `AutoDownloadService.EnqueuePlaylistEntriesFromCurrentAsync`: geladene Playlist-Einträge via `PlaylistEntriesCache.WriteAsync` persistieren | Offen | — |
| 24 | MVU | `AppEffect`: `ExpireWatchedDownloadEffect(MediaType, MediaId, DelayMinutes)` und `ProtectDownloadExpiryEffect(MediaType, MediaId, RemainingDuration)` neu; `DeleteWatchedDownloadEffect` entfernen | Offen | — |
| 25 | MVU | `AppUpdate.EvaluateEndWindow` + `UpdatePlaybackEnded`: `AutoDeleteWatchedEpisodes`/`tvshowepisode`-Check + `DeleteWatchedDownloadEffect` durch `AutoDeletePlayedDownloads` + `ExpireWatchedDownloadEffect` (alle Medientypen) ersetzen | Offen | — |
| 26 | MVU | `AppUpdate.UpdatePlaybackProgress`: Wiedergabe-Schutz — bei `!DownloadExpiryProtected && Source.Kind == LocalFile && Duration > 0` Flag setzen und bei `remaining > 0` `ProtectDownloadExpiryEffect` emittieren | Offen | — |
| 27 | MVU | `AppUpdate`: `DownloadExpiryProtected = false` in allen Medienwechsel-`with`-Blöcken (`StartPlayback`, `AdvanceToNextEpisode`, Playlist-Übergänge) ergänzen | Offen | — |
| 28 | MVU | `ShellAppEffectHandler`: `DeleteWatchedDownloadEffect`-Case und `DeleteDownloadAsync`-`completedOnly`-Zweig entfernen; `ExpireWatchedDownloadEffect` → `ExpireAfterAsync`; `ProtectDownloadExpiryEffect` → `ExtendExpiryAsync(utcNow + 2 × Restdauer)` | Offen | — |
| 29 | MVU | `ShellAppEffectHandler.LoadDownloadsAsync` + `ResumeDownloadsAsync`: `GetAutoDownloadCandidateKeysAsync` ermitteln und `ReconcileAsync(settings, keys)` aufrufen | Offen | — |
| 30 | MVU | `ShellAppEffectHandler.EnqueueDownloadAsync`: `origin: DownloadOrigin.Manual` explizit übergeben | Offen | — |
| 31 | Infrastruktur | `MauiProgram` (iOS): `CompletionSink` ruft `CompleteExternalTransferAsync` mit via `AppSettingsService` geladenen `AppSettings` | Offen | — |
| 32 | UI | `DownloadItemViewModel`: `OriginText`/`HasOriginText`/`ExpiresText`/`HasExpiresText` durchreichen + `PropertyChanged` in `Update` ergänzen | Offen | — |
| 33 | UI | `DownloadsPage.xaml`: „Cache"-Badge und Ablaufdatum-Label im Eintrags-Template ergänzen | Offen | — |
| 34 | UI | `SettingsPage.xaml(.cs)`: `AutoDeleteWatchedSwitch` → `AutoDeletePlayedSwitch` („Downloads nach dem Abspielen automatisch löschen"); `DownloadRetentionDaysEntry` und `DeleteAfterPlaybackDelayEntry` neu verdrahten | Offen | — |
| 35 | Tests | `DownloadServiceTests`: Herkunfts-Persistenz (manual/auto), Re-Enqueue behält Herkunft, `ExpiresAtUtc` bei `TransferAsync`/`CompleteExternalTransferAsync`, `ExpireAfterAsync`/`ExtendExpiryAsync`-Fälle; Fixture `Settings` auf neue Eigenschaften umstellen | Offen | — |
| 36 | Tests | `DownloadServiceReconcileTests`: Ablauf-Pass (Kandidat → verlängert/auto, Nicht-Kandidat → entfernt, `null`-Altbestand → abgelaufen, nicht-`completed` unberührt, ohne `settings` kein Pass); `InsertEntryAsync` um `expiresAtUtc`/`origin` erweitern | Offen | — |
| 37 | Tests | `AutoDownloadServiceTests`: `origin: Auto`-Einreihung, `AlreadyPresent`-Verlängerung, `GetAutoDownloadCandidateKeysAsync` (Selbst/Serie/Playlist/deaktiviert/fehlende Caches), `PlaylistEntriesCache`-Roundtrip; Hilfsmethoden `CacheShowAsync`/`CachePlaylistAsync` | Offen | — |
| 38 | Tests | `AppUpdateTests_Downloads`/`_Player`: `ExpireWatchedDownloadEffect` an Schwelle/Ende (inkl. Film), Schutz-Effekt (erster Takt, Einmal-Semantik, Stream-Quelle/unbekannte Dauer ohne Effekt); `DeleteWatchedDownloadEffect`-Tests migrieren | Offen | — |
| 39 | Tests | `AppSettingsTests`/`AppSettingsServiceTests`/`SettingsStorageContractTests`: Defaults/Clamps der neuen Werte, Migration des Alt-Schlüssels, neue Controls im Seiten-Contract; `LocalDatabaseTests`: Spalten-Migration `ExpiresAtUtc`/`Origin`; `DownloadsContractTests`: `OriginText`/`ExpiresText`, `DownloadOriginNames`, verdrahtete Effekte, `CompleteExternalTransferAsync`-Signatur | Offen | — |
| 40 | Tests | Bestehende Tests anpassen/entfernen: `DeleteIfCompleted_QueuedEntry_StaysUntouched` entfernen; `PlaybackProgress_*_AutoDelete`-Tests auf neuen Effekt umstellen | Offen | — |
| 41 | E2E-Tests | `DownloadsTests`: Szenario „Cache"-Herkunftsanzeige (Weiterschauen-Film → Auto-Download einschalten → Downloads-Seite zeigt „Cache"-Badge) | Offen | — |
| 42 | E2E-Tests | `DownloadsTests`: Happy Path „Nach dem Abspielen löschen" (Download fertig → Option ein + Verzögerung 0 → Testvideo abspielen → Downloads-Seite entfernt Eintrag) | Offen | — |
