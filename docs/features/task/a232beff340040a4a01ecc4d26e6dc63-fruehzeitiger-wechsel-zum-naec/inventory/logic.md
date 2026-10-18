# Logik: MVU-Update-Pfade und App-Rand

## `AppUpdate` (VideoPlayer.Mvu)
Datei: `VideoPlayer.Mvu/AppUpdate.cs` (4328 Zeilen). Dispatch der Player-Meldungen: Z. 283–285 (`PlaybackProgressMessage` → `UpdatePlaybackProgress`, `PlaybackEndedMessage` → `UpdatePlaybackEnded`, `PlaybackFailedMessage` → `UpdatePlaybackFailed`).

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `UpdatePlaybackProgress` (Z. 3792–3876) | private | Positions-Takt: übernimmt `Position`/`Duration`/`IsPlaying`/`IsBuffering` ungeprüft aus der Nachricht (Z. 3800–3806). Fortschrittsmeldung an den Server mit 0,5-s-Throttling über `LastReportedPositionSeconds` (Z. 3809–3819). `remaining = Duration - Position` (Z. 3823): `!IsWatched && remaining <= WatchedEndWindowSeconds` → `IsWatched = true` + optional `DeleteWatchedDownloadEffect` bei `AutoDeleteWatchedEpisodes` und `MediaType == "tvshowepisode"` (Z. 3826–3836). Auto-Fortsetzung: `AutoResumeEnabled && message.IsPlaying && remaining <= AutoResumeRemainingSeconds` → Playlist-Zweig `AdvancePlaylistEntry` (Z. 3846–3858, zusätzlich `IsPlaylistPlayback && !PlaylistNextExhausted && !IsResolving`) bzw. Serienzweig `AdvanceToNextEpisode` (Z. 3860–3870, nur `HasNextEpisode` — kein `IsResolving`-/Identitäts-Guard). **Keine Plausibilitätsprüfung — ein einzelner Takt löst beide Schadwirkungen aus. `IsBuffering` wird nicht ausgewertet.** |
| `UpdatePlaybackEnded` (Z. 3896–3960) | private | Ende-Ereignis: Frühreturn bei `!IsActive || !IsPlaybackEndPlausible` (Z. 3899–3902). Setzt `IsWatched`, `Position = Duration`, Abschluss-`ReportPlaybackProgressEffect`, optional `DeleteWatchedDownloadEffect` (Z. 3928–3931), danach Auto-Fortsetzung über `AdvancePlaylistEntry`/`AdvanceToNextEpisode`. |
| `IsPlaybackEndPlausible` (Z. 3973–3979) | private | Referenzmuster: Ende plausibel nur bei `Duration > 0`, `Position >= 0` und `Duration - Position <= PlaybackEndedToleranceSeconds`. Prüft den **gemerkten State** (`player.Duration`/`player.Position`), nicht die Nachricht. |
| `MarkEpisodeWatchedInDetails` (Z. 3994–4031) | private | Überträgt `IsWatched` auf `EpisodeItem.WatchedAt` der geladenen Detailansicht (nur `tvshowepisode`, Serienkontext, noch unmarkiert). Wird am Ende jedes `UpdatePlaybackProgress`-/`UpdatePlaybackEnded`-Pfads aufgerufen. |
| `UpdatePlaybackFailed` (Z. 4033–4050) | private | Abspielfehler → `AdvancePlaylistEntry` (Playlist, nicht erschöpft) bzw. `AdvanceToNextEpisode` mit `PlaybackUnavailableError` (Z. 75). Kein Fenster-/Plausibilitätstest. |
| `AdvanceToNextEpisode` (Z. 3735–3777) | private | Serien-Wechsel: ohne `HasNextEpisode` Fehlerzustand (Z. 3737–3751). Sonst `MediaId`/`Title` der Folge-Episode, `IsResolving = true`, **`IsWatched = false`, `Position`/`Duration = TimeSpan.Zero`, `LastReportedPositionSeconds = -1`**, `UpcomingEpisodes.Skip(1)`, + `ResolvePlaybackSourceEffect` (Z. 3754–3776). |
| `AdvancePlaylistEntry` (Z. 2612–2627) | private | Playlist-Wechsel: setzt nur `Source = null`, `IsResolving = true`, `IsPlaying`/`IsBuffering = false`, `Error` — **`MediaId`, `Position`, `Duration`, `IsWatched` bleiben vorerst unverändert** (der Folgeeintrag wird erst in `UpdatePlaylistNavigationCompleted` übernommen). + `AdvancePlaylistEntryEffect`. |
| `StartPlayback` (Z. 3628–3644) | private | Wiedergabestart (`UpdatePlayRequested`, Z. 3605–3614; auch vom Weiterschauen-Pfad): neuer `PlayerState` (Defaults: `Position`/`Duration` = 0, `IsWatched` = false) + `NavigateToPlayerEffect` + `ResolvePlaybackSourceEffect`. |
| `NextEpisodeQueue` (Z. 3655–3677) | private | Baut `UpcomingEpisodes` aus dem Serienkontext der Detailseite (staffelübergreifend sortiert, alle Episoden nach `mediaId`). |
| `UpdateUpcomingEpisodesLoaded` (Z. 1500–1513) | private | Nachgeladene Fortsetzungsliste übernehmen (Guard: `IsActive` + identische `MediaId` + `tvshowepisode`). Auslöser: `LoadUpcomingEpisodesEffect` (Z. 1429). |
| `UpdatePlaylistPlaybackStarted` (Z. 2449–2483) | private | Playlist-Start: neuer `PlayerState` mit Playlist-Kontext + `ResolvePlaybackSourceEffect`. |
| `UpdatePlaylistNavigationCompleted` (Z. 2540–2599) | private | Übernimmt den Folgeeintrag der Server-Playlist: Identitäts-Guard über `PlaylistId`/`PlaylistEntryId` (Z. 2543–2549); bei Treffer `MediaId`/`Title`/Positionsfelder des Eintrags, **`IsWatched = false`, `Position`/`Duration = 0`** (Z. 2575–2598) + `ResolvePlaybackSourceEffect`. 204 → `PlaylistNextExhausted` (Z. 2560–2572). |
| `UpdatePlaybackSourceResolved` (Z. 3687–3722) | private | Quellauflösung: Identitäts-Guard über `MediaId`/`MediaType` (Z. 3690–3695); Quelle anwenden (Z. 3698–3710); Fehlschlag → Rückfall `AdvancePlaylistEntry`/`AdvanceToNextEpisode` (Z. 3716–3721). |
| `UpdatePlayerClosed` (Z. 4067–4090) | private | Seitenende: Abschluss-Fortschrittsmeldung (bei `Position > 0`) + `PlayerState.Empty`. |
| `UpdateDownloadDeleted` (Z. 4203–4217) | private | Entfernt den Manifest-Eintrag aus `Downloads.Items` (Reaktion auf `DownloadDeletedMessage` via `DownloadService.ItemRemoved`). |

## `PlayerPage` (VideoPlayer.Maui) — Quelle der Positionsmeldungen
Datei: `VideoPlayer.Maui/Pages/PlayerPage.xaml.cs` (484 Zeilen).

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `ProgressTickInterval` | private const (Z. 25) | 5 Sekunden. |
| `ResumeSeekTimeout` | private const (Z. 26) | 10 Sekunden (Fortsetzungs-Seek). |
| `StartProgressTimer` (Z. 133–165) | private | Dispatcher-Timer: meldet nur bei `CurrentState == MediaElementState.Playing` via `ReportPosition`. |
| `ReportPosition` (Z. 167–185) | private | Dispatcht `PlaybackProgressMessage(VideoElement.Position, VideoElement.Duration, state == Playing, state is Buffering or Opening)` — direkte, ungefilterte Weitergabe der MediaElement-Werte. Während `Buffering` meldet die Nachricht `IsPlaying = false` (die Auto-Fortsetzung greift dann nicht); ein nativer Stall ohne `CurrentState`-Wechsel bleibt unsichtbar. |
| `OnMediaStateChanged` (Z. 433–450) | private | Zusätzlicher `ReportPosition`-Aufruf bei jedem Zustandswechsel (gemarshalt auf den UI-Thread). |
| `OnMediaEnded` (Z. 415–422) | private | Loggt „Wiedergabe beendet" + `PlaybackEndedMessage`. |
| `OnMediaFailed` (Z. 424–431) | private | Loggt „Wiedergabe fehlgeschlagen" + `PlaybackFailedMessage`. |
| `FailPlaybackStart` (Z. 294–302) | private | Quell-Setz-/Validierungsfehler → Log + `PlaybackFailedMessage` + `sourceApplyFailed`. |
| `ApplyResumeSeekAsync` (Z. 401–413) | private | Fortsetzungs-Seek mit Timeout; Fehlschlag nur Warn-Log, **kein** `PlaybackFailedMessage` (Fix `e465907`). |
| `UpdateUi` (Z. 201–261) | private | Setzt `VideoElement.Source` bei Quellwechsel; `sourceApplyFailed`-Invalidierung; `IsValidSourceLocation`-Frühvalidierung. |

## Download-Löschpfad

- `DeleteWatchedDownloadEffect` (`VideoPlayer.Mvu/AppEffect.cs` Z. 771) → `ShellAppEffectHandler` Fall (Z. 325–327) → `DeleteDownloadAsync(..., completedOnly: true)` (Z. 1520–1547) → `DownloadService.DeleteIfCompletedAsync` (`VideoPlayer.Mvu/DownloadService.cs` Z. 509–519) → `DeleteAsync` (Z. 465–498): löscht nur `Completed`-Einträge, Log „Download {type}/{id} ({title}) entfernt." (Z. 492–496), `ItemRemoved`-Ereignis → `DownloadDeletedMessage` (`ShellAppEffectHandler` Z. 1629–1630).
- Weitere (nicht taktbezogene) Löschpfade: `DeleteDownloadEffect` (Benutzer-Löschung), `DeleteAllDownloadsEffect`, `PurgeOrphanedDownloadsEffect` — andere Logmeldungen bzw. benutzerinitiiert.

## Querverweise / Beobachtungen zum Wechselpfad

- `UpdatePlaybackProgress` verwendet für `remaining` ausschließlich die **Nachrichtenwerte** (`message.Duration`, `message.Position`), nicht den State — eine Zuordnung der Werte zur aktuellen `MediaId` wird nicht geprüft.
- Nach `AdvanceToNextEpisode` steht die Folge-`MediaId` bereits im State, während das `MediaElement` noch das alte Medium spielt: ein nachlaufender Takt (5-s-Timer oder `StateChanged`) mit den alten Werten landet im Fenster → `IsWatched`/`DeleteWatchedDownloadEffect` für die **neue** Episode; bei `HasNextEpisode` und `IsPlaying` ist sogar ein kaskadierender weiterer `AdvanceToNextEpisode`-Aufruf möglich (Serienzweig ohne `IsResolving`-Guard).
- Bei `AdvancePlaylistEntry` bleibt `IsWatched`/`MediaId` des alten Eintrags stehen — ein nachlaufender Takt trifft dort auf `IsResolving = true` (Playlist-Zweig gesperrt); `IsWatched` ist ohnehin schon gesetzt.
