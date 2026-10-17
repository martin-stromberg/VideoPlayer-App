# Logik

Betroffene Logikklassen der Anforderung „Playlist-Bezug in der Fortschrittsmeldung (Weiterschauen)". Pfade relativ zum Repository-Root (kein `src/`-Ordner).

## Meldungsstrecke (Ist-Zustand)

`PlayerPage` (5-s-Takt/`StateChanged`/Schließen) → `PlaybackProgressMessage`/`PlaybackEndedMessage`/`PlayerClosedMessage` → `AppUpdate` (drei Erzeugungsstellen) → `ReportPlaybackProgressEffect` → `ShellAppEffectHandler.ReportPlaybackProgressAsync` → `ProgressReportService.ReportProgressAsync` → online: `VideoPlayerApiClient.ReportPlaybackProgressAsync` → `PlaybackProgressReport` (JSON); offline/Fehlschlag: `PendingProgressSyncService.EnqueueAsync` (`pending_progress`) → später `ServerSyncService.FlushPendingProgressAsync` → `VideoPlayerApiClient.ReportPlaybackProgressAsync`.

Der Playlist-Kontext (`PlayerState.PlaylistId`/`PlaylistEntryId`) wird auf **keiner** Stufe dieser Strecke mitgeführt.

## `AppUpdate`
Datei: `VideoPlayer.Mvu/AppUpdate.cs` (statische Klasse, zentraler MVU-Reducer; `Update` dispatcht per Pattern-Matching, Z. 225–300)

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `Update` | public static | Zentraler Reducer; Dispatch `PlaybackProgressMessage` → `UpdatePlaybackProgress` (Z. 283), `PlaybackEndedMessage` → `UpdatePlaybackEnded` (Z. 284), `PlayerClosedMessage` → `UpdatePlayerClosed` (Z. 288) |
| `UpdatePlaybackProgress` | private static (Z. 3792) | Fortschrittstakt: übernimmt Position/Dauer/Laufzustand; erzeugt `ReportPlaybackProgressEffect` bei Positionsänderung > 0,5 s (Throttling über `LastReportedPositionSeconds`, Z. 3816–3817) — **ohne** Playlist-Felder. Setzt `IsWatched` (`WatchedEndWindowSeconds`), löst Auto-Fortsetzung aus: Playlist → `AdvancePlaylistEntry` (Z. 3852), Serie → `AdvanceToNextEpisode` (Z. 3864). Der vor dem Wechsel erzeugte Meldungseffekt geht dem `AdvancePlaylistEntryEffect` in der Effektliste voraus (`[.. effects, .. transition.Effects]`) — Snapshot-Semantik: die Meldung gilt dem alten Eintrag |
| `UpdatePlaybackEnded` | private static (Z. 3895) | Ende-Ereignis: nur plausibel (Restlaufzeit ≤ `PlaybackEndedToleranceSeconds`); markiert `IsWatched`, erzeugt `ReportPlaybackProgressEffect` mit `finalPosition = Duration` (Z. 3919–3920) — **ohne** Playlist-Felder; danach gleiche Auto-Fortsetzungs-Verzweigung |
| `UpdatePlayerClosed` | private static (Z. 4065) | Seitenende: optional `TogglePictureInPictureEffect`, bei `Position > 0` `ReportPlaybackProgressEffect` mit dem letzten Takt-Stand (Z. 4082–4083) — **ohne** Playlist-Felder; setzt `Player = PlayerState.Empty` |
| `UpdatePlaybackFailed` | private static (Z. 4031) | Abspielfehler → Fehler-Rückfall: Playlist → `AdvancePlaylistEntry` (Z. 4044), sonst `AdvanceToNextEpisode`; **keine** Fortschrittsmeldung |
| `UpdatePlaybackSourceResolved` | private static | Quellauflösung: bei Fehlschlag Rückfall — Playlist → `AdvancePlaylistEntry` (Z. 3716–3718), sonst `AdvanceToNextEpisode` |
| `AdvancePlaylistEntry` | private static (Z. 2612) | Wechsel im Playlist-Kontext: Player-Reset (`Source=null`, `IsResolving=true`, …) + `AdvancePlaylistEntryEffect(player.PlaylistId!.Value, player.PlaylistEntryId)` — erzeugt **keine** Fortschrittsmeldung |
| `AdvanceToNextEpisode` | private static (Z. 3735) | Serien-Gegenstück zu `AdvancePlaylistEntry` |
| `StartPlayback` | private static (Z. 3628) | Kontextloser Wiedergabestart: `PlayerState` ohne Playlist-Felder + `NavigateToPlayerEffect` + `ResolvePlaybackSourceEffect` |
| `UpdatePlaylistPlaybackStartRequested` | private static (Z. 2430) | „Abspielen" → `StartPlaylistPlaybackEffect(PlaylistId, EntryId)` |
| `UpdatePlaylistPlaybackStarted` | private static (Z. 2449) | Ergebnis des Starts: belegt `PlayerState` mit `PlaylistId`, `PlaylistName`, `PlaylistEntryId = start.CurrentEntryId`, `PlaylistPosition`, `PlaylistTotalCount` (Z. 2462–2466) + `NavigateToPlayerEffect` + `ResolvePlaybackSourceEffect` mit `StartPositionSeconds` |
| `UpdatePlaylistNextRequested` / `UpdatePlaylistPreviousRequested` | private static (Z. 2494/2515) | Guards `IsPlaylistPlayback && !IsResolving && !PlaylistNextExhausted`; `LoadNextPlaylistEntryEffect`/`LoadPreviousPlaylistEntryEffect` mit `player.PlaylistEntryId` |
| `UpdatePlaylistNavigationCompleted` | private static (Z. 2540) | Übernimmt Navigationsergebnis: Guard auf Identität (`PlaylistId`/`CurrentEntryId`), 204 → `PlaylistNextExhausted`; Eintrag → `PlaylistEntryId = next.EntryId` + `ResolvePlaybackSourceEffect` (Z. 2584, 2598) |
| `UpdatePlaylistEntrySelected` | private static (Z. 2387) | Eintragsauswahl der Playlist-Detailseite: abspielbar+zugänglich → `StartPlaylistPlaybackEffect(PlaylistId, item.EntryId)` (Z. 2406), sonst Detail-Navigation |
| `UpdateContinueWatchingItemSelected` | private static (Z. 1376) | Weiterschauen-Auswahl: `item.PlaylistId is > 0 && item.PlaylistEntryId is > 0` → `StartPlaylistPlaybackEffect` (Z. 1398–1406); sonst Serienpfad (`StartPlayback` + ggf. `LoadUpcomingEpisodesEffect`) |
| `IsPlaybackEndPlausible` / `MarkEpisodeWatchedInDetails` / `NextEpisodeQueue` | private static | Hilfsfunktionen (Toleranzfenster, „angesehen" in Detailansicht, Serien-Fortsetzungsliste) |

## `VideoPlayerApiClient`
Datei: `VideoPlayer.ApiClient/VideoPlayerApiClient.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `ReportPlaybackProgressAsync(mediaType, mediaId, positionSeconds, durationSeconds, cancellationToken)` | public (Z. 269–289) | `POST api/continue-watching/progress` mit `PlaybackProgressReport`-Body — **keine** Playlist-Parameter/Felder |
| `GetContinueWatchingAsync` | public (Z. 258) | `GET api/continue-watching` → `ContinueWatchingEntry[]` (inkl. Playlist-Felder) |
| `HideContinueWatchingEntryAsync` / `SkipContinueWatchingEntryAsync` | public (Z. 296/315) | Kontextmenü-Mutationen `api/continue-watching/hide`/`skip` |
| `StartPlaylistPlaybackAsync(playlistId, entryId?, ct)` | public (Z. 646–661) | `POST api/playlists/{id}/play` bzw. `…?entryId={entryId}` → `PlaylistPlaybackStart` |
| `GetNextPlaylistEntryAsync` / `GetPreviousPlaylistEntryAsync` | public (Z. 673/696) | `POST api/playlists/{id}/play/next|previous?currentEntryId=…` → `PlaylistNavigationResult?` |
| `AdvancePlaylistAsync` | public (Z. 718) | `POST api/playlists/{id}/play/advance?currentEntryId=…` → `PlaylistNavigationResult?` |

Infrastruktur: `SendWithPolicyAsync` (Retry 1 s/2 s/4 s für 5xx, 408, 429, Transportfehler; einmalige Token-Erneuerung bei 401), `PostJsonAsync` serialisiert mit `JsonOptions` (nur `PropertyNameCaseInsensitive` — PascalCase-Feldnamen im Body). Aufgerufen von `ProgressReportService.ReportProgressAsync` und `ServerSyncService.FlushPendingProgressAsync`.

## `ProgressReportService`
Datei: `VideoPlayer.Mvu/ProgressReportService.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `ReportProgressAsync(videoType, videoId, position, duration, cancellationToken)` | public (Z. 57–136) | Sammelstelle der Fortschrittsmeldung: online + Client → `client.ReportPlaybackProgressAsync(videoType, videoId, (long)position.TotalSeconds, (long)duration.TotalSeconds)` (Z. 73–78); offline/kein Client/überholbarer Fehler → `pendingQueue.EnqueueAsync(videoType, videoId, position, duration)` (Z. 118); permanenter 4xx → verworfen. **Signatur ohne Playlist-Parameter** |
| `IsPermanentClientError` | internal static (Z. 163) | 4xx außer 401/408/429 gilt als dauerhaft |
| `LogOutcomeOnce` | private | Dedupe der Warn-Logs je Medium/Outcome |

`videoId` ist hier `int` (Cast `(int)report.MediaId` im Handler); `videoType` wird unverändert (CLR-Normalform) an den Server durchgereicht.

## `ServerSyncService`
Datei: `VideoPlayer.Mvu/ServerSyncService.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `SynchronizeAsync` | public (Z. 73) | Voll-Sync: erst `FlushPendingProgressAsync`, dann Metadaten/Weiterschauen/Favoriten |
| `FlushPendingProgressAsync` | public (Z. 109–162) | Sendet wartende `PendingProgressSyncEntry`s nach — `client.ReportPlaybackProgressAsync(entry.VideoType, entry.VideoId, PositionTicks→s, DurationTicks→s)` (Z. 122–127); Transport-/Serverfehler → Abbruch (Rest bleibt), permanenter 4xx → Eintrag verwerfen; nach Erfolg `RemoveForMediaAsync`. **Playlist-Felder existieren nicht im Entry und werden nicht mitgegeben** |
| `SyncMetadataAsync` / `SyncContinueWatchingAsync` / `SyncFavoritesAsync` | public | Cache-Aktualisierungen |
| `TrySyncAsync` / `WriteCachedListAsync` / `MediaTypeOf` | private | Hilfsfunktionen |

## `PendingProgressSyncService`
Datei: `VideoPlayer.Storage/PendingProgressSyncService.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `EnqueueAsync(videoType, videoId, position, duration)` | public (Z. 40–76) | Reiht Meldung in `pending_progress` ein: Transaktion `DELETE … WHERE VideoType=? AND VideoId=?` + `Insert` (Deduplikation je Medium — neuster Stand gewinnt). **Signatur ohne Playlist-Parameter; Insert befüllt nur VideoType/VideoId/PositionTicks/DurationTicks/UpdatedAtUtc** |
| `GetPendingAsync` | public (Z. 83) | Alle wartenden Einträge, chronologisch |
| `CountAsync` | public (Z. 96) | Anzahl wartender Einträge |
| `RemoveForMediaAsync(videoType, videoId)` | public (Z. 107) | Löscht Einträge eines Mediums nach erfolgreichem Nachsenden |

## `BackupDatabase`
Datei: `VideoPlayer.Storage/BackupDatabase.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `InitializeAsync` | public (Z. 40–46) | `CreateTableAsync` für `KnownServerEntry`, `BackupStateEntry`, `PendingProgressSyncEntry`; danach `EnsureColumnAsync("pending_progress", "DurationTicks", "bigint NOT NULL DEFAULT 0")` — bestehendes Migrationsmuster für nachträgliche Spalten |
| `InitializeWithRecoveryAsync` | public (Z. 59) | Neu-Anlage bei beschädigter Datei |
| `EnsureColumnAsync(table, column, columnDefinition)` | private (Z. 94–103) | `PRAGMA table_info` + `ALTER TABLE … ADD COLUMN` bei fehlender Spalte — das Muster, über das `PlaylistId`/`PlaylistEntryId` nachzuziehen wären |

## `ShellAppEffectHandler`
Datei: `VideoPlayer.Maui/Services/ShellAppEffectHandler.cs` (App-Rand, `IAppEffectHandler`)

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `HandleAsync` | public (Z. 99–332) | Effekt-Dispatch; `case ReportPlaybackProgressEffect report` → `ReportPlaybackProgressAsync` (Z. 292–294); `StartPlaylistPlaybackEffect` → `StartPlaylistPlaybackAsync` (Z. 223); `LoadNext/PreviousPlaylistEntryEffect`/`AdvancePlaylistEntryEffect` → `NavigatePlaylistEntryAsync` (Z. 226–236) |
| `ReportPlaybackProgressAsync` | private (Z. 485–504) | `ProgressReportService.ReportProgressAsync(report.MediaType, (int)report.MediaId, report.Position, report.Duration, ct)` — **gibt nur die vier Felder weiter**; Fehlschlag nicht fatal (nächster Takt meldet erneut) |
| `StartPlaylistPlaybackAsync` | private (Z. 1372–1388) | `PlaylistService.StartPlaybackAsync` → `PlaylistPlaybackStartedMessage` |
| `NavigatePlaylistEntryAsync` | private (Z. 1403–1429) | `PlaylistService.AdvanceAsync`/`GetNextEntryAsync`/`GetPreviousEntryAsync` → `PlaylistNavigationCompletedMessage` |

Abonnierte Events: `authSession.SessionExpired`, `deviceTokens.DeviceTokenRejected`, `networkMonitor.AvailabilityChanged`, `serverEvents.EventReceived`, `serverEvents.ConnectionClosed`, `downloads.ItemChanged`, `downloads.ItemRemoved`.
Publiziert: dispatcht Ergebnis-Messages über `DispatchMessageAsync` in den MVU-Zyklus (`MvuHost`).

## `PlaylistService`
Datei: `VideoPlayer.Mvu/PlaylistService.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `StartPlaybackAsync(playlistId, entryId?, ct)` | public (Z. 213–236) | `client.StartPlaylistPlaybackAsync` → `PlaylistPlaybackStartInfo` (inkl. `CurrentEntryId`, `StartPositionSeconds`) |
| `GetNextEntryAsync` / `GetPreviousEntryAsync` / `AdvanceAsync` | public (Z. 248/267/285) | Navigations-Endpunkte → `PlaylistNavigationItem?` |
| `NavigateAsync` | private (Z. 306) | Gemeinsamer Kern; `entry.Id` → `PlaylistNavigationItem.EntryId` |
| `LoadPlaylistsAsync` / `LoadPlaylistAsync` / `LoadEntriesAsync` u. a. | public | Übrige Playlist-Endpunkte (nicht betroffen) |

## `PlayerPage` (App-Rand)
Datei: `VideoPlayer.Maui/Pages/PlayerPage.xaml.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `StartProgressTimer` | private (Z. 133) | `Dispatcher.StartTimer(ProgressTickInterval = 5 s)` (Z. 25); meldet nur bei `MediaElementState.Playing` via `ReportPosition` |
| `ReportPosition` | private (Z. 167) | Dispatcht `PlaybackProgressMessage(Position, Duration, IsPlaying, IsBuffering)` — nur Position/Dauer, kein Playlist-Bezug (korrekt: Kontext liegt im `PlayerState`) |
| `OnDisappearing` | protected (Z. 112) | Dispatcht `PlayerClosedMessage` → Abschluss-Fortschritt |
| Vor/Zurück-Buttons | private (Z. 461/466) | `PlaylistPreviousRequestedMessage`/`PlaylistNextRequestedMessage` |
| `UpdateUi` | private (Z. 201) | `PlaylistContextLabel`/`PlaylistNavigationBar` aus `IsPlaylistPlayback`/`PlaylistLabel`/`CanNavigate*` |

## `OfflineContinueWatchingService` (Nebenverbraucher)
Datei: `VideoPlayer.Mvu/OfflineContinueWatchingService.cs`

`LoadPendingProgressAsync` (Z. 218–237) liest `pending_progress` direkt aus der `BackupDatabase` — die Offline-Weiterschauen-Liste nutzt die Outbox als lokale Fortschrittsquelle (Typen: `(VideoType, VideoId)` → `(PositionTicks, DurationTicks)`).

## `MockVideoServer` (E2E-Infrastruktur)
Datei: `VideoPlayer.E2E.Tests/Support/MockVideoServer.cs`

| Element | Beschreibung |
|---------|--------------|
| `POST /api/continue-watching/progress` (Z. 448–462) | Fortschritts-Senke: Gate-/Bearer-Prüfung, dann HTTP 204 — **der Request-Body wird nicht erfasst/ausgewertet** (kein belastbarer E2E-Nachweis der Playlist-Felder möglich) |
| `ContinueWatchingPlaylistId`/`ContinueWatchingPlaylistName`/`ContinueWatchingPlaylistEntryId` (Z. 255–265) | Opt-in Playlist-Bezug der Mock-Weiterschauen-Einträge; in `GET /api/continue-watching`-Antworten als `playlistId`/`playlistName`/`playlistEntryId` serialisiert (Z. 730–732, 767–769) |
| `HandlePlaylistPlay` (Z. 1305) | Playlist-Play-Endpunkte des Mocks |
| `WasPathRequested`, Zähler (`HideContinueWatchingCount` u. a.) | Diagnose-Hilfen |

## Altbestand (außerhalb des Issue-Fokus)
Datei: `VideoWebPlayer.Client/VideoWebPlayerClient.cs`, `VideoWebPlayer.Maui/Services/DownloadManager.cs`

`VideoWebPlayerClient.ReportPlaybackProgressAsync` (Z. 555–619): eigene Coalescing-Implementierung (pro Medium ein laufender Request + ein überschreibender Puffer); `SendPlaybackProgressCoreAsync` (Z. 621) postet anonymes Objekt `MediaType`/`MediaId`/`PositionSeconds`/`DurationSeconds` an `api/continue-watching/progress` — keine Playlist-Felder. Verwendung: `DownloadManager` Z. 667 (Positionsmeldung nach Download-Fortschritt).
