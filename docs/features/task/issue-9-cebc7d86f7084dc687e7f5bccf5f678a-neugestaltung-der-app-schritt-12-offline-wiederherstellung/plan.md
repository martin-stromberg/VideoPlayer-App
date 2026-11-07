# Implementierungsplan — Schritt 12: Offlinebetrieb und Wiederherstellung

Vorgehen: vorhandene Mechanismen orchestrieren/erweitern, kein Parallelbau. Alle neuen öffentlichen Member erhalten deutsche XML-Doku. Keine neuen Enum-Typen (Kennzeichnungen über boolsche Flags/Records) — dadurch keine `EnumCoverageTests`-Erweiterung nötig.

## A. Serienkontext im Download-Manifest

Ziel: Offline aus dem Manifest erkennen, zu welcher Serie/Position eine heruntergeladene Episode gehört — Voraussetzung für Platzhalter der Folge-Episoden.

1. `VideoPlayer.Mvu/DownloadSeriesContext.cs` (neu): `sealed record DownloadSeriesContext(long? ShowId, string? ShowTitle, int? SeasonNumber, int? EpisodeNumber)` — wird beim Einreihen mitgegeben.
2. `VideoPlayer.Storage/Entities/DownloadManifestEntry.cs`: neue Spalten `ShowId` (`long?`), `ShowTitle` (`string?`), `SeasonNumber` (`int?`), `EpisodeNumber` (`int?`) — sqlite-net migriert fehlende Spalten per `ALTER TABLE` bei `CreateTableAsync`.
3. `DownloadService.EnqueueAsync`: optionaler Parameter `DownloadSeriesContext? seriesContext`; beim Insert gesetzt, beim Re-Enqueue vorhandener Einträge ergänzt (fehlende Werte auffüllen).
4. `DownloadRequestedMessage` + `EnqueueDownloadEffect`: neue init-only Properties `ShowId`, `ShowTitle`, `SeasonNumber`, `EpisodeNumber` (bestehende Positionsparameter unverändert).
5. `AppUpdate.UpdateDownloadRequested`: Kontext von Message auf Effect kopieren.
6. `MediaDetailsPage.xaml.cs` (`OnDownloadClicked`/`OnEpisodeDownloadClicked`): Kontext mitgeben — Episode: `details.ShowId`, `details.Title`, `SelectedSeason.Number`, `episode.Number`; Film: kein Kontext.
7. `AutoDownloadService.EnqueueNextEpisodesAsync`: `DownloadSeriesContext(show.Id, show.Name, season.Number, episode.Number)` mitgeben; zusätzlich `ShowDetailsCache.WriteAsync` nach `GetTvShowAsync` (s. B). Konstruktor erhält `CacheDatabase`.

## B. Gecachte Serienstruktur (`cached_lists`)

Ziel: Offline-Episodenliste einer Serie (Grundlage für Platzhalter und Offline-Detailseite).

1. `VideoPlayer.Mvu/ShowDetailsCache.cs` (neu):
   - DTOs `CachedShowEntry { long ShowId; string Title; IReadOnlyList<CachedShowSeason> Seasons }`, `CachedShowSeason { int Number; long SeasonId; IReadOnlyList<CachedShowEpisode> Episodes }`, `CachedShowEpisode { long Id; int Number; string Title }`.
   - `WriteAsync(CacheDatabase, TvShow)` → `cached_lists`-Schlüssel `show:{id}` (JSON, `UpdatedAtUtc`).
   - `ReadAsync(CacheDatabase, long showId)` → `CachedShowEntry?`; lesen tolerant (Fehler → `null`).
   - `NextEpisodeAfter(CachedShowEntry, seasonNumber, episodeNumber)` / `IndexOf(episodeId)`: staffelübergreifend geordnete Episodenfolge, nächste Episode bestimmen.
2. `MediaDetailsService`: Konstruktor + `CacheDatabase`; nach erfolgreichem `GetTvShowAsync` → `ShowDetailsCache.WriteAsync`.

## C. Offline-„Weiterschauen" aus Downloads + Platzhalter

1. `VideoPlayer.Mvu/OfflineContinueWatchingService.cs` (neu):
   - Abhängigkeiten: `CacheDatabase`, `BackupDatabase`, `HomeListService`; interner Konstruktor mit `Func<string,bool>? fileExists` (Tests).
   - `LoadHomeListsAsync()`:
     - Abspielbare Einträge: `download_manifest`-Zeilen mit `State == completed` **und** `fileExists(FilePath)`, sortiert `UpdatedAtUtc` desc → `HomeMediaItem` (`Id`, `MediaType`, `Title`, `ShowId`; `Progress` aus `pending_progress` (Position/Dauer), Fallback `cached_lists[continue_watching]`-Position).
     - Platzhalter: für jede abspielbare Episode mit auflösbarem Serienkontext (Manifest `ShowId`, Fallback eingebetteter Graph der Caches) → `ShowDetailsCache.ReadAsync` → nächste Episode nach (`SeasonNumber`, `EpisodeNumber`); fehlt diese im Completed-Bestand → `HomeMediaItem { IsPlaceholder = true, MediaType = "tvshowepisode", Title = "{Show} – {Episode}" }`; Dedupe nach Episoden-Id.
     - `Favorites`/`Recent` über `HomeListService` (neu öffentlich: `LoadFavoritesAsync`, `LoadRecentAsync` → `IReadOnlyList<HomeMediaItem>`).
     - Jeder Schritt einzeln abgesichert (Fehler → Teilliste leer, kein Absturz).
2. `HomeMediaItem`: + `IsPlaceholder` (bool), `IsSelectable => !IsPlaceholder`, `CardOpacity => IsPlaceholder ? 0.55 : 1.0`.
3. `HomeLists`: + `IReadOnlyList<string> DiscardedCaches` (s. F2).
4. `ShellAppEffectHandler.LoadHomeListsAsync`: `host.Model.IsOffline` → `OfflineContinueWatchingService.LoadHomeListsAsync()`, sonst bisheriger Pfad.
5. `AppUpdate.UpdateMediaItemSelected`: `!item.IsSelectable` → `Unchanged` (Platzhalter nicht navigierbar).
6. `MediaCarousel.xaml`: Overlay-Label „Nicht heruntergeladen" (`IsVisible="{Binding IsPlaceholder}"`), `Opacity="{Binding CardOpacity}"` auf dem Kartenrahmen.
7. `AppUpdate.UpdateHomeListsLoaded`: `message.Lists.DiscardedCaches.Count > 0 && model.ConnectionState == Connected` → zusätzlich `RunFullSyncEffect` (verworfene Caches neu synchronisieren).

## D. Offline-Wiedergabeweg (Detailseite ohne Server)

1. `MediaDetailsService`: Konstruktor + `CacheDatabase` (+ interner `Func<string,bool>? fileExists`). `Client == null` → `LoadOfflineAsync`:
   - `movie`: Titel aus Manifest/Caches, `IsPlayable = true` (Medium ist prinzipiell abspielbar), `HasLocalFile` aus Manifest (`completed` + Datei).
   - `tvshowepisode`: `showId`-Hint → `ShowDetailsCache` → `SeasonItem`s aufbauen; `EpisodeItem.HasLocalFile` aus Manifest; `IsPlaceholder = offline && !HasLocalFile`; Titel/IsFavorite aus Manifest/`cached_favorites`.
   - `tvshow`/`tvshowseason`: gecachte Struktur → View; nicht vorhanden → `InvalidOperationException`.
   - übrige Typen → `InvalidOperationException` (bestehender Fehlertext „Die Details konnten nicht geladen werden.").
   - `MediaDetailsView.IsOfflineSnapshot = true` für alle Offline-Views.
2. `MediaDetailsView`: + `HasLocalFile` (bool), `IsOfflineSnapshot` (bool). `EpisodeItem`: + `HasLocalFile`, `IsPlaceholder` (nur im Offline-Snapshot gesetzt), `ShowUnavailable => IsPlaceholder`.
3. `MediaDetailsPage.xaml(.cs)` (`UpdateUi`):
   - `PlayButton.IsVisible = details.IsPlayable && (!details.IsOfflineSnapshot || details.HasLocalFile)`.
   - `DownloadButton`/`FavoriteButton` bei `IsOfflineSnapshot` ausgeblendet bzw. deaktiviert.
   - Episoden-Template: ▶/⤓-Buttons per `DataTrigger` auf `IsPlaceholder` ausblenden, Zeile abgedunkelt + Hinweis „Nicht heruntergeladen".

## E. Wiederherstellung nach Cloud-Backup

1. `AuthSessionService.TryStoredLoginAsync` — Token-Wiederverwendung vor Credential-Login:
   - `secrets.GetAuthTokenAsync()` vorhanden → Client bauen + `SetAccessToken` → validieren via `client.GetFavoritesAsync()`.
   - Erfolg → `Client`/`ClientBaseAddress` setzen → `AuthAttemptStatus.Succeeded` (direkter Einstieg ohne Login-Maske).
   - `ApiException` mit 401/403 → Token verwerfen (`SaveAuthTokenAsync(null)`) → weiter mit Credentials-Pfad.
   - `ApiException` (sonstige), `HttpRequestException`, `TaskCanceledException` → `AuthAttemptStatus.ServerUnreachable` (bestehender Pfad führt zu Offline/Setup-Fallback).
2. `AppUpdate.UpdateAutoLoginCompleted`: `AuthAttemptStatus.MissingCredentials` → `NeedsLogin` + `LoginError = "Die gespeicherte Anmeldung ist nicht mehr verfügbar. Bitte melde dich erneut an."` (Wiederanmelde-Hinweis nach Restore).
3. Bestehender Ablauf bleibt: `EnterConnected` → `RunFullSyncEffect` (→ `pending_progress` flush, Metadaten, Weiterschauen, Favoriten) → `SynchronizationCompletedMessage` → `LoadHomeListsEffect`. Keine neuen Effekte nötig.

## F. Beschädigte lokale Daten

1. `CacheDatabase`/`BackupDatabase`: `Task<bool> InitializeWithRecoveryAsync()` — `InitializeAsync` versuchen; bei `SQLiteException`/`InvalidOperationException`/`IOException`: Lazy-Verbindung zurücksetzen (Feld nicht mehr `readonly`), Datei nach `{name}.corrupt-{yyyyMMddHHmmss}` verschieben, erneut initialisieren; Rückgabe `true` = Recovery erfolgt; zweiter Fehlschlag → Ausnahme an Aufrufer. Beide Klassen speichern den Dateipfad im Konstruktor.
2. `HomeListService`: bei Fehler beim Lesen eines `cached_lists`-Eintrags → Zeile löschen (verwerfen) + Schlüssel in `HomeLists.DiscardedCaches`; bei unlesbarer `cached_favorites`-Zeile → Zeile löschen + `"cached_favorites"` aufnehmen. Nur tatsächlich gelöschte Einträge melden (kein Resync-Loop bei fehlenden Daten). `ReadFavoritesAsync`/`LoadFavoritesAsync`/`LoadRecentAsync` teilen dies.
3. `AppMessage`: + `StorageRecoveredMessage(string Notice)`; `AppModel`: + `string? StorageNotice`; `AppUpdate.UpdateStorageRecovered`: `StorageNotice` setzen.
4. `ShellAppEffectHandler.InitializeStorageAsync`: beide DBs via `InitializeWithRecoveryAsync`; bei Recovery → `StorageRecoveredMessage("Die lokalen Daten waren beschädigt und wurden neu initialisiert. Zwischengespeicherte Inhalte werden neu synchronisiert.")`; Totalversagen → ebenfalls Meldung, App läuft weiter.
5. `HomePage.xaml`: `StorageNoticeBanner` (sichtbar bei `StorageNotice != null`), analog zum Offline-Banner.

## G. Offline-Einschränkung Profil + Feinschliff

1. `AppUpdate.UpdateProfilePageOpened`: `model.IsOffline` → `Profile = ProfilePageState.Initial with { Attempted = true }`, **kein** `LoadProfileInfoEffect`.
2. `ProfilePage.xaml(.cs)`: `ModelChanged`-Verdrahtung + `UpdateUi` — `ProfileOfflineBanner` („Offline – Profilinformationen stehen ohne Serververbindung nicht zur Verfügung."), `UserInfoBorder` offline ausgeblendet; „Lokale Installation" und Abmelden bleiben verfügbar.
3. Offline-Banner-Text `HomePage` präzisieren („zwischengespeicherte Inhalte; keine neuen Inhalte/Quellen/Profildaten").
4. DI-Registrierung (`MauiProgram.cs`): `OfflineContinueWatchingService` als Singleton; geänderte Konstruktoren (`MediaDetailsService`, `AutoDownloadService`) werden per DI aufgelöst.

## H. Geplante Tests (`VideoWebPlayer.Maui.Tests`, xUnit v3)

| Testklasse / -fall | Abgedeckte Anforderung |
|---|---|
| `OfflineContinueWatchingServiceTests.CompletedDownloadWithFile_Appears` | Offline-Weiterschauen nur aus completed+Datei |
| `…Queued/Failed/MissingFile_Excluded` | keine unvollständigen Einträge |
| `…EpisodeWithCachedShow_MissingNextEpisodePlaceholder` | Platzhalter Folge-Episode |
| `…Placeholder_Deduplicated` | Dedupe |
| `…ProgressFromPendingProgress` | Fortschritt offline |
| `…CorruptShowCache_StillReturnsItems` | Robustheit |
| `HomeListServiceTests.CorruptCachedList_DiscardedAndReported` | Cache verwerfen + `DiscardedCaches` |
| `HomeListServiceTests.CorruptFavoriteRow_DiscardedAndReported` | dto. Favoriten |
| `StorageRecoveryTests.CorruptDatabaseFile_RecreatedAndBackedUp` | DB-Recovery + `*.corrupt-*` + Tabellen nutzbar |
| `StorageRecoveryTests.HealthyDatabase_NotRecovered` | kein Eingriff bei intakter DB |
| `AuthSessionServiceTests.StoredToken_Valid_SucceedsWithoutLogin` | direkter Einstieg |
| `…StoredToken_Unauthorized_FallsBackToCredentials` | Token abgelaufen → Credentials |
| `…StoredToken_Unauthorized_NoCredentials_MissingCredentials` | Restore-Fall |
| `…StoredToken_TransportError_ServerUnreachable` | nicht erreichbar |
| `AppUpdateTests.StorageRecovered_SetsNotice` | Startmeldung |
| `AppUpdateTests.MediaItemSelected_Placeholder_Unchanged` | Platzhalter nicht navigierbar |
| `AppUpdateTests.ProfilePageOpened_Offline_NoLoadEffect` | kein Profilabruf offline |
| `AppUpdateTests.HomeListsLoaded_DiscardedCaches_Connected_RunFullSync` | Neu-Synchronisation |
| `AppUpdateTests.AutoLoginCompleted_MissingCredentials_ShowsReloginNotice` | Wiederanmelde-Hinweis |
| `MediaDetailsServiceOfflineTests.Movie_WithLocalFile_Playable` | Offline-Wiedergabeweg Film |
| `…Episode_WithCachedShow_MarksLocalAndPlaceholderEpisodes` | Offline-Serienkontext |
| `…NoLocalData_Throws` | Fehlerpfad |
| Contract-Tests (Erweiterung `HomePageContractTests`, `FavoritesProfileContractTests`, `DownloadsContractTests`-Muster): `StorageNoticeBanner` in `HomePage.xaml`, Platzhalter-Overlay in `MediaCarousel.xaml`, `ProfileOfflineBanner` + `UserInfoBorder` in `ProfilePage.xaml`, Episode-Platzhalter-Trigger in `MediaDetailsPage.xaml` | UI-Vertrag |

## I. Dokumentation

- `docs/help/`: Offline-/Wiederherstellungsverhalten beschreiben (neue/aktualisierte Hilfeseite).
- `README.md`: Feature-Zeile ergänzen.
- `docs/RELEASE_NOTES.md`: Eintrag „Offlinebetrieb und Wiederherstellung".

## J. Abschluss

1. `dotnet format whitespace VideoPlayer.App.sln --no-restore`
2. `dotnet test VideoWebPlayer.Maui.Tests` → Ergebnis in `test-results.md` + `final-dotnet-test.log`
3. Reviews (`review.md`, `review-usability.md`, `review-code.md`)
4. Doku-Updates, Abschlusscommit `feat: Offlinebetrieb und Wiederherstellung`

## Offene Punkte

Keine.
