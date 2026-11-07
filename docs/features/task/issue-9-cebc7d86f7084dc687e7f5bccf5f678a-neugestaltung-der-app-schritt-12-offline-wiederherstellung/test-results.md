# Testergebnisse

## Zusammenfassung

| Lauf | Ergebnis |
|------|----------|
| Baseline (vor der Umsetzung) | **580 Tests bestanden, 0 Fehler, 0 übersprungen** — Inventar-Lauf (`inventory/test-results/baseline-dotnet-test.log`) |
| Finaler Testlauf | **623 Tests bestanden, 0 Fehler, 0 übersprungen** — `final-dotnet-test.log` |
| Build `VideoPlayer.Maui` (net10.0-windows10.0.19041.0) | **0 Warnungen, 0 Fehler** |

## Ausgeführte Kommandos

```text
dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj
```

Ergebniszeile:

```text
Bestanden!   : Fehler: 0, erfolgreich: 623, übersprungen: 0, gesamt: 623
```

```text
dotnet build VideoPlayer.Maui/VideoPlayer.Maui.csproj -f net10.0-windows10.0.19041.0
```

Ergebnis: `0 Warnung(en), 0 Fehler`.

## Neue Tests (43 gegenüber der Baseline)

### `AppUpdateTests_Offline` (13 Tests, neue Datei)

- `MediaItemSelected_Placeholder_NoNavigation` — Platzhalter-Karte
  nicht navigierbar.
- `MediaItemSelected_OfflinePlayable_Navigates` — abspielbarer
  Offline-Eintrag navigiert mit Show-Hint.
- `SourcesPageOpened_Offline_NoLoadEffect` — Quellen werden offline
  nicht geladen.
- `ProfilePageOpened_Offline_NoLoadEffect` — kein Profilabruf offline,
  Seite gilt als „versucht".
- `DownloadRequested_Offline_ShowsNoticeWithoutEffect` — Download-
  Anforderung wird mit Hinweis abgelehnt.
- `DownloadRequested_WithSeriesContext_ForwardsHints` — Serienkontext
  landet im `EnqueueDownloadEffect`.
- `HomeListsLoaded_DiscardedCaches_WhileConnected_RunsFullSync` —
  verworfene Caches lösen die Voll-Synchronisation aus.
- `HomeListsLoaded_DiscardedCaches_WhileOffline_NoSync` — offline kein
  Sync-Versuch.
- `HomeListsLoaded_DiscardedCaches_WhileSyncRunning_NoDuplicate` —
  kein zweiter Sync-Lauf.
- `StorageRecovered_SetsNotice` — Ablage-Wiederherstellung landet als
  Hinweis im Model.
- `AutoLoginCompleted_MissingCredentials_ShowsReloginNotice` —
  verständlicher Re-Anmelde-Hinweis nach Backup-Wiederherstellung.
- `AutoLoginCompleted_ServerUnreachable_KeepsSpecificError` —
  konkrete Fehlerart bleibt erhalten.
- `Reconnection_FromOffline_RunsFullSyncAndResumesDownloads` —
  Wiederverbindung: Ereignisverbindung, Voll-Sync, Download-
  Wiederaufnahme.

### `OfflineContinueWatchingServiceTests` (7 Tests, neue Datei)

- `ContinueWatching_CompletedMovieWithFile_Appears` — abgeschlossener
  Film-Download mit vorhandener Datei erscheint.
- `ContinueWatching_IncompleteOrMissingFiles_AreFiltered` —
  `downloading`/`queued`/`failed` und fehlende Dateien werden
  herausgefiltert.
- `ContinueWatching_EpisodeWithMissingNext_ShowsPlaceholder` —
  fehlende Folge-Episode als nicht auswählbarer Platzhalter
  (erwischte den `KeyFor`-Übersetzungsfehler in `ShowDetailsCache`).
- `ContinueWatching_NextEpisodeDownloaded_NoPlaceholder` — kein
  Platzhalter bei vorhandener Folge-Episode.
- `ContinueWatching_Progress_FromPendingOutbox` — Fortschritt aus der
  lokalen Outbox.
- `ContinueWatching_Progress_FallsBackToCachedServerState` —
  Fallback auf den Server-Stand des Caches.
- `HomeLists_FavoritesAndRecent_ComeFromCache` — Favoriten aus dem
  Bestandscache.

### `AuthSessionServiceTests` (Erweiterung)

- `TryStoredLogin_ValidToken_ReusedWithoutLogin` — gespeicherter Token
  wird wiederverwendet, kein Login-Request.
- `TryStoredLogin_RejectedToken_FallsBackToCredentials` — abgelehnter
  Token → Credential-Login.
- `TryStoredLogin_TokenWithoutCredentials_MissingCredentials` —
  Wiederherstellung ohne Geheimnisse → `MissingCredentials`.
- `TryStoredLogin_UnreachableValidation_ServerUnreachable` —
  Validierungsfehler ist nicht „Zugangsdaten ungültig".
- `EnsureFreshToken_AfterRestore_SessionExpiredOnlyForCredentials` —
  stille Erneuerung meldet `SessionExpired` nur bei fehlenden/
  abgelehnten Zugangsdaten.
- `EnsureFreshToken_TransientFailure_NoSessionExpired` — Netzfehler
  erzwingt keine Anmeldung.

### `PlaybackSourceServiceTests` (Erweiterung)

- Offline-Auflösung (`allowServerStream: false`): lokale Datei wird
  zurückgegeben, kein HTTP-Request.
- Ohne lokale Datei: `null`, kein HTTP-Request.
- Online: lokale Datei hat Vorrang vor dem Server-Stream.

### `MediaDetailsServiceTests` (Erweiterung)

- Offline-Film-Snapshot aus abgeschlossenem Manifest-Eintrag
  (`IsOfflineSnapshot`, `HasLocalFile`).
- Nicht unterstützter Medientyp ohne Client → Fehlertext.
- Fehlende gecachte Serienstruktur → erwartete Ausnahme.

### `HomeListServiceTests` (Erweiterung)

- `LoadHomeLists_CorruptListEntry_IsDiscardedAndReported` —
  beschädigter `cached_lists`-Eintrag wird gelöscht und in
  `DiscardedCaches` gemeldet.
- `LoadHomeLists_CorruptFavoriteRow_IsDiscardedAndReported` —
  beschädigte Favoriten-Zeile wird verworfen, intakte bleiben.
- `LoadHomeLists_IntactCache_ReportsNoDiscards` — keine verworfenen
  Bereiche bei intaktem Cache.

### `LocalDatabaseTests` (Erweiterung)

- `CacheDatabase_MigratesSeriesContextOntoExistingManifest` —
  `ALTER TABLE`-Migration des Serienkontexts auf ein Schritt-11-
  Schema mit Bestandsdaten.
- `BackupDatabase_RecoversCorruptFile` — beschädigte Backup-Datei
  wird als `*.corrupt-*` beiseitegelegt, neue Ablage nutzbar.
- `CacheDatabase_RecoversCorruptFile` — analog für die Cache-Ablage.
- `Databases_IntactFile_NoRecovery` — intakte Datei löst keine
  Wiederherstellung aus.

### `DownloadServiceTests` (Erweiterung)

- `Enqueue_WithSeriesContext_PersistsContextInManifest` — Serie,
  Staffel- und Episodennummer landen im Manifest.

### `AutoDownloadServiceTests` (Erweiterung)

- `Run_EpisodeCandidate_CrossesIntoNextSeason` — nach der letzten
  Episode einer Staffel wird der staffelübergreifende Nachfolger
  eingereiht, Kontext persistiert.

### `OfflineRecoveryContractTests` (6 Tests, neue Datei)

- `HomePage_WiresOfflineAndStorageNoticeBanners` — Offline-Banner,
  `StorageNoticeBanner`-Bindung, Leerzustand.
- `MediaCarousel_RendersDownloadPlaceholders` — Platzhalter-Badge,
  `CardOpacity`, „Nicht heruntergeladen".
- `ProfilePage_WiresOfflineWarning` — `ProfileOfflineBanner` und
  `UserInfoBorder`-Umschaltung.
- `MediaDetailsPage_MarksPlaceholderEpisodes` — Platzhalter-Trigger,
  Offline-Snapshot-Aktionen unterbunden.
- `MauiProgram_RegistersOfflineServices` — DI-Registrierung.
- `ShellAppEffectHandler_WiresRecoveryAndOfflineLists` — Recovery-
  Initialisierung und Offline-Listenweg.

## Umgebung

- .NET SDK `10.0.401`, Windows.
- iOS-Ziel wird in dieser Umgebung nicht gebaut (Windows-Target
  als Referenz).
