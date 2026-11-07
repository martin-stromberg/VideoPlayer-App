# Inventar — Schritt 12: Offlinebetrieb und Wiederherstellung

Stand: Branch `task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app-schritt-12-offline-wiederherstellung` (Tip `94da5b3`), Basis für Vergleich: `task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app`.

Baseline-Testlauf: `inventory/test-results/baseline-dotnet-test.log` — **580/580 Tests bestanden** (xUnit v3, `VideoWebPlayer.Maui.Tests`).

## Bereits vorhandene Mechanismen (Wiederverwendung)

### MVU-Kern (`VideoPlayer.Mvu/`)

| Artefakt | Rolle für diesen Schritt |
|---|---|
| `AppModel.cs` | `ConnectionState`, `HasOfflineData`, `IsOffline`, `IsOnline`, `IsSyncRunning`, `ContinueWatching`, `Favorites`, `RecentEntries`, `SourcesOverview`, `Profile`, `Downloads`, `StartupStatusText`, `LoginError` |
| `AppMessage.cs` | `StartupRequestedMessage`, `ServersLoadedMessage`, `ServerHealthCheckedMessage`, `AutoLoginCompletedMessage`, `OfflineDataCheckCompletedMessage`, `SynchronizationCompletedMessage`, `HomeListsLoadedMessage`, `CachedListsChangedMessage`, `NetworkStatusChangedMessage`, `MediaDetailsRequestedMessage`, `MediaItemSelectedMessage`, `DownloadRequestedMessage`, `ProfilePageOpenedMessage` u. a. |
| `AppEffect.cs` | `InitializeStorageEffect`, `LoadSettingsEffect`, `LoadServersEffect`, `CheckServerHealthEffect`, `CheckOfflineDataEffect`, `AttemptAutoLoginEffect`, `RunFullSyncEffect`, `LoadHomeListsEffect`, `NavigateToPageEffect`, `EnqueueDownloadEffect`, `LoadMediaDetailsEffect` u. a. |
| `AppUpdate.cs` | `UpdateOfflineDataCheckCompleted` (Offline-Daten → `Offline` + Home; sonst `NeedsServerSetup`), `UpdateAutoLoginCompleted` (Erfolg → `EnterConnected`; `MissingCredentials`/Fehler → Login-Seite), `EnterConnected` (Connected + `ConnectServerEventsEffect` + `RunFullSyncEffect` + `ResumeInterruptedDownloadsEffect` + Home-Navigation), `UpdateSynchronizationCompleted` (→ `LoadHomeListsEffect`), `UpdateNetworkStatusChanged` (Verlust → Offline; Rückkehr → Health-Check → Reconnect) |
| `MvuHost.cs`, `AppTransition.cs`, `IAppEffectHandler.cs`, `AppRoutes.cs` | MVU-Zyklus, unverändert nutzbar |

### Dienste (`VideoPlayer.Mvu/`)

| Dienst | Bestehende Funktion | Lücke für Schritt 12 |
|---|---|---|
| `HomeListService` | Liest `cached_lists` (`continue_watching`, `recent`, `sources`) + `cached_favorites`; robust gegen fehlerhafte JSON (Teilliste leer) | Weiterschauen kommt aus dem Server-Cache, nicht aus lokalen Downloads; beschädigte Einträge werden nicht verworfen/gemeldet |
| `DownloadService` | `download_manifest` (`queued`/`downloading`/`completed`/`failed`), Dateien unter `AppStorageLayout.VideosDirectory`, `LoadItemsAsync`, `ResumeInterruptedAsync`, `EnqueueAsync`, lokale Dateiprüfung | Manifest enthält keinen Serienkontext (`ShowId`, Staffel-/Episodennummer) für Offline-Platzhalter |
| `PlaybackSourceService` | Bevorzugt lokale `completed`-Datei vor Server-Stream | vorhanden, ausreichend |
| `AuthSessionService` | `TryStoredLoginAsync` (gespeicherte Credentials → Login), `Client`, `ClientBaseAddress`, Auto-Login | kein Token-Wiederverwendungspfad („bei noch gültigem Token direkter Einstieg") |
| `ServerSyncService` | `SynchronizeAsync`: `pending_progress` flushen → Metadaten (`sources`, `recent`) → `continue_watching` → Favoriten; Konstanten `SourcesCacheKey`, `RecentCacheKey`, `ContinueWatchingCacheKey` | ausreichend; muss nur orchestriert werden |
| `PendingProgressSyncService`/`ProgressReportService` | Outbox `pending_progress` + Flush beim Sync | ausreichend |
| `MediaDetailsService` | Lädt Details via API; Hints (`ShowId`, `SeasonId`, `EpisodeId`) | kein Offline-Pfad (wirft ohne Client) → heruntergeladene Videos ohne Server nicht über Details erreichbar |
| `AutoDownloadService` | `EnqueueNextEpisodesAsync` über `api/items/tvshow/{id}` | Episoden werden ohne Serienkontext eingereiht; Serienstruktur wird nicht gecacht |
| `FavoritesService` | Toggle/Remove + lokale Kopie erneuern; offline → `null`/`false` | ausreichend (klare Fehlermeldung offline) |
| `ProfileInfoService` | Liest Token-Claims + Benutzername + Profil (lokal) | `UpdateProfilePageOpened` startet Ladeeffekt auch offline; Seite zeigt keinen Offline-Hinweis |
| `INetworkStatusMonitor` | Online/Offline + WLAN; Offline-Banner verdrahtet | ausreichend |

### Speicher (`VideoPlayer.Storage/`)

| Artefakt | Bestehende Funktion | Lücke |
|---|---|---|
| `CacheDatabase` | `cached_lists`, `cached_favorites`, `download_manifest`; Lazy-Init, `CreateTableAsync`; liegt im NoBackup-Bereich | kein Recovery bei nicht initialisierbarer DB (Lazy cached die Ausnahme) |
| `BackupDatabase` | `known_servers`, `backup_state`, `pending_progress`; im Backup-Bereich | kein Recovery |
| `AppStorageLayout` | `BackupDirectory`, `NoBackupDirectory`, `CacheDirectory`, `VideosDirectory` | ausreichend |
| `AppSecretsService` | `SecureStorage`: Token, Credentials — nicht backup-pflichtig (Restore-Szenario: leer) | ausreichend |
| `DownloadManifestEntry` | Manifest-Zeile (MediaType/Id/Title/State/FilePath/…) | fehlende Serienkontext-Spalten |

### UI (`VideoPlayer.Maui/`)

| Artefakt | Bestehende Funktion | Lücke |
|---|---|---|
| `HomePage.xaml(.cs)` | Offline-Banner (`model.IsOffline`), Karussells Weiterschauen/Favoriten/Neu | kein Platzhalter-Rendering; kein Speicher-Hinweis-Banner |
| `MediaCarousel.xaml(.cs)` | Karten mit Titel/Poster; Tap → `MediaItemSelectedMessage` | keine Markierung nicht-verfügbarer Einträge |
| `MediaDetailsPage.xaml(.cs)` | Episodenliste, Play/Download/Favorite, `DownloadRequestedMessage` | keine Offline-Darstellung (keine lokalen Marker); Download-Request ohne Serienkontext |
| `SourcesPage` | Offline-Hinweis bereits vorhanden (live-only beabsichtigt) | — |
| `FavoritesPage` | Offline-Banner + Cache-Stand; Entfernen offline deaktiviert | — |
| `ProfilePage` | Benutzer-/Profilinfo | kein Offline-Hinweis; lädt auch offline |
| `ShellAppEffectHandler` | `InitializeStorageAsync`, `CheckOfflineDataAsync` (`HasOfflineDataAsync`), `LoadHomeListsAsync`, `AttemptAutoLoginAsync`, `EnqueueDownloadAsync` u. a. | kein DB-Recovery/Beiseitelegen; kein Offline-Pfad für Weiterschauen |

### Start-/Wiederherstellungsablauf (Ist)

`StartupRequestedMessage` → `InitializeStorage` → `LoadSettings` → `LoadServers` → `StartNetworkMonitoring` + `LoadHomeLists` → `ServersLoadedMessage` → aktiver Server vorhanden → `CheckServerHealthEffect` → erreichbar → `AttemptAutoLoginEffect` → `AuthSessionService.TryStoredLoginAsync` → `MissingCredentials` (Restore: `SecureStorage` leer) → `AutoLoginCompletedMessage` → `NeedsLogin` + Login-Seite (Benutzername vorbefüllt). Nicht erreichbar → `CheckOfflineDataEffect` → `HasOfflineDataAsync` → `Offline`+Home bzw. `NeedsServerSetup`. → Der Restore-Ablauf ist weitgehend vorhanden; fehlt: Token-Wiederverwendung, Wiederanmelde-Hinweis, DB-Recovery-Meldung.

## Altbestand-Referenz

`VideoWebPlayer.Maui/ViewModels/HomePageViewModel.cs` (`IsOfflineMode`, `LoadDownloadsAsync`, Cache-First-Anzeige) — weniger umfangreich als die neue Anforderung; dient nur als fachliche Referenz.

## Identifizierte Umsetzungslücken

1. Offline-„Weiterschauen" aus `download_manifest` statt Server-Cache, mit Platzhaltern für fehlende Folge-Episoden (Serienkontext).
2. Serienkontext-Speicherung: Manifest-Spalten + gecachte Serienstruktur (`cached_lists`-Schlüssel `show:{id}`).
3. Offline-Detailseite bzw. abspielbarer Pfad zu lokalen Dateien (`MediaDetailsService` ohne Client).
4. Token-Wiederverwendung beim Auto-Login + Wiederanmelde-Hinweis.
5. Cache-Verwerfen + Neu-Synchronisation bei beschädigten Einträgen.
6. Datenbank-Recovery (alte Datei sichern, neu initialisieren, Nutzer informieren).
7. Profil-Seite: Offline-Hinweis + kein Ladeversuch.
8. Tests aller genannten Fälle.
