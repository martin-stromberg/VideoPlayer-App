# Bestandsaufnahme — Logik und Dienste (Neu-App)

## `PlaybackSourceService`

Datei: `VideoPlayer.Mvu/PlaybackSourceService.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `ResolveAsync(mediaType, mediaId, ct)` | public | Löst die Wiedergabequelle auf: fertiger Manifest-Download (`State == "completed"`, Datei vorhanden) → `LocalFile`; sonst Server-Stream-URL `api/items/{typ}/{id}/stream?access_token=…` mit Fortsetzungsposition aus Weiterschauen (Server, sonst Cache) |
| `ResolveLocalFileAsync` | private | Liest `download_manifest` aus `CacheDatabase` |
| `ResolveResumePositionAsync` | private | Weiterschauen-Stand online (`GetContinueWatchingAsync`) oder aus `cached_lists[continue_watching]` |

Relevanz: Die lokale Wiedergabe ist fertig verdrahtet — Schritt 11 muss
lediglich das Manifest mit `completed`-Einträgen füllen.

## `AppUpdate` (Update-Funktion)

Datei: `VideoPlayer.Mvu/AppUpdate.cs` — reine Funktion `Update(model, message) → AppTransition`.

Relevante Stellen:

- `UpdateStartupRequested` — Effektkette `InitializeStorageEffect`,
  `LoadSettingsEffect`, `LoadServersEffect`, `StartNetworkMonitoringEffect`,
  `LoadHomeListsEffect`.
- `SynchronizationCompletedMessage` — setzt `IsSyncRunning = false`,
  lädt Home-Listen neu (Ankoppelpunkt für Auto-Download-Lauf nach Sync).
- `UpdateServerEventReceived` — `ContinueWatchingUpdated` →
  `SyncContinueWatchingEffect` (Ankoppelpunkt).
- `UpdatePlaybackProgress`/`UpdatePlaybackEnded` — setzen
  `PlayerState.IsWatched` (Ankoppelpunkt der Auto-Löschung; nur bei
  `MediaType == "tvshowepisode"` relevant — „gesehene Episoden").
- `NextEpisodeQueue(model, mediaId)` — private Hilfsfunktion: alle
  Episoden der `MediaDetailsView.Seasons` nach `mediaId`,
  staffelübergreifend sortiert → Muster für die Auto-Download-Ermittlung.
- `UpdateLogoutRequested` — räumt sitzungsbezogenen State (Downloads-State
  einbezogen, falls sitzungsbezogen).

## `ServerSyncService`

Datei: `VideoPlayer.Mvu/ServerSyncService.cs` — `SynchronizeAsync`,
`SyncContinueWatchingAsync` (schreibt `cached_lists[continue_watching]`),
`SyncFavoritesAsync`, `SyncMetadataAsync`,
`FlushPendingProgressAsync`. `ContinueWatchingCacheKey = "continue_watching"`.

## `HomeListService`

Datei: `VideoPlayer.Mvu/HomeListService.cs` — liest Cache-Listen
(`continue_watching`, `favorites`, `recent`) und mappt auf
`HomeMediaItem`/`FavoriteListItem` inkl. Serienkontext-Hints
(`ShowId`/`SeasonId`/`EpisodeId` aus `TvShowEpisode.Season.Show`).

## `MediaDetailsService`

Datei: `VideoPlayer.Mvu/MediaDetailsService.cs` — `LoadAsync` liefert
`MediaDetailsView` mit `Seasons`/`Episodes` (Serienkontext,
staffelübergreifend geordnet); `LoadEpisodeAsync` löst die Serie über
`show`-Hint oder `episode.Season.Show.Id` + `GetTvShowAsync` auf — Muster
für die Auto-Download-Ermittlung der Folge-Episoden.

## `AuthSessionService`

Datei: `VideoPlayer.Mvu/AuthSessionService.cs` — `Client` (angemeldeter
`VideoPlayerApiClient`, Bearer-Token + stille Neuanmeldung),
`ClientBaseAddress`, `LogoutAsync`, `SessionExpired`-Ereignis.
Grundlage für Stream-URL und Token (`Client.CurrentToken`,
`AppSecretsService.GetAuthTokenAsync` als Fallback).

## `INetworkStatusMonitor` / `ConnectivityNetworkStatusMonitor`

Dateien: `VideoPlayer.Mvu/INetworkStatusMonitor.cs`,
`VideoPlayer.Maui/Services/ConnectivityNetworkStatusMonitor.cs` —
`IsOnline` (`NetworkAccess.Internet`/`ConstrainedInternet`),
`AvailabilityChanged`-Ereignis, `Start()`. **WLAN-Erkennung fehlt** —
`Connectivity.Current.ConnectionProfiles` liefert `ConnectionProfile.WiFi`
am App-Rand; die Schnittstelle ist um eine WLAN-Abfrage zu erweitern
(plattformneutral als Eigenschaft, z. B. `IsWifi`).

## `ProgressReportService` / `PendingProgressSyncService`

Persistente Fortschrittswarteschlange `pending_progress` (Muster für
verlustfreie Offline-Pufferung — Referenz für Manifest-Persistenz).

## `MvuHost` / `IAppEffectHandler` / `ShellAppEffectHandler`

- `MvuHost.DispatchAsync` serialisiert Updates, führt Effekte sequenziell
  aus; `ModelChanged`-Ereignis für die Seiten.
- `ShellAppEffectHandler.HandleAsync` — `switch` über alle Effekte;
  Dienste via `services.GetRequiredService<…>()`; Ergebnisse als Messages
  über `DispatchMessageAsync` (MainThread) zurück in den Zyklus.
- Bestehende Messages-Muster für Seiten: `…PageOpenedMessage` → Effekt →
  `…LoadedMessage` → State.

## `VideoPlayerApiClient`

Datei: `VideoPlayer.ApiClient/VideoPlayerApiClient.cs` — kein
Stream-/Download-Endpunkt als Methode; die Stream-URL wird extern gebaut
(Muster `PlaybackSourceService`/`BuildStreamUrl` des Altbestands).
`GetContinueWatchingAsync`, `GetTvShowAsync` (eingebettete
Staffeln/Episoden), `CurrentToken` sind vorhanden. Der eingebaute
Retry/Token-Refresh greift nur für Client-Methoden — der Download nutzt
einen eigenen `HttpClient`-Aufruf (Muster Altbestand: Bearer-Header +
Token-Query).
