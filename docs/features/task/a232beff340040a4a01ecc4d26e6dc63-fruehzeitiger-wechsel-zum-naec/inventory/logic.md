# Logik

## `PlayerPage` (`VideoPlayer.Maui`)
Datei: `VideoPlayer.Maui/Pages/PlayerPage.xaml.cs` (486 Zeilen)

Wiedergabeseite (Shell-Route `player`): setzt die aufgelöste Quelle am `MediaElement`, führt den Fortsetzungs-Seek aus und meldet Fortschritt/Fehler in den MVU-Zyklus. Konstruktor-Abhängigkeiten: `MvuHost`, `IPictureInPictureService`, `ILogger<PlayerPage>`.

Felder/Konstanten:

| Name | Typ | Zweck |
|------|-----|-------|
| `ProgressTickInterval` | `static readonly TimeSpan` = 5 s | Fortschrittstakt |
| `ResumeSeekTimeout` | `static readonly TimeSpan` = **10 s** | Internes Zeitlimit des Fortsetzungs-Seeks (Implementierungsdetail, kein Setting) |
| `IosNativeControlsClearance` | `const double` = 96 | iOS-Versatz unter die native Top-Leiste |
| `appliedLocation` | `string?` | Zuletzt gesetzte Quell-Adresse (Wechsel-Erkennung) |
| `sourceApplyFailed` | `bool` | Invalidierungs-Flag nach fehlgeschlagenem Quellenwechsel |
| `pendingResumePosition` | `TimeSpan?` | Gemerkter Fortsetzungs-Seek bis zur Wiedergabebereitschaft (nur UI-Thread) |
| `timerRunning` | `bool` | Fortschrittstimer läuft |
| `subscribed` | `volatile bool` | Seite sichtbar/abonniert — Guard für alle Dispatchs und Modell-Updates |

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `PlayerPage(...)` | public ctor | Verdrahtet Abhängigkeiten, iOS-/Windows-Plattformblöcke, initial `UpdateUi` |
| `OnAppearing` | protected override | Abonniert `ModelChanged`, dispatcht `PlayerPageOpenedMessage`, startet Fortschrittstimer |
| `OnDisappearing` | protected override | Meldet Abo ab (`subscribed = false`), `pictureInPicture.RestoreAsync()`, dispatcht `PlayerClosedMessage` |
| `StartProgressTimer` | private | 5-s-Dispatcher-Timer; `ReportPosition` nur bei `CurrentState == Playing`; Ausnahmen nur protokolliert |
| `ReportPosition` | private | Dispatcht `PlaybackProgressMessage`; try/catch-isoliert |
| `OnModelChanged` | private | Marshals `UpdateUi` auf den UI-Thread (nur solange `subscribed`) |
| `UpdateUi` | private | Rendert `PlayerState`; Quellenwechsel: Stop/Reset bei leerer Quelle, `IsValidSourceLocation`-Prüfung, `MediaSource.FromFile`/`FromUri` setzen; alle Fehlpfade → `FailPlaybackStart` |
| `IsValidSourceLocation` | private static | Frühvalidierung: `ServerStream` verlangt absolute http/https-URL, `LocalFile` nicht leeren Pfad |
| `FailPlaybackStart` | private | **Echter Fehlerpfad des Wiedergabestarts:** loggt Fehler, setzt `sourceApplyFailed = true`, dispatcht `PlaybackFailedMessage` (wenn `subscribed`) |
| `SanitizeForLog` | private static | Maskiert `access_token` in Protokollmeldungen (`AppLogService.MaskAccessToken`) |
| `OnMediaOpened` | private (Event-Handler) | Loggt „Wiedergabe geöffnet"; merkt `Source.ResumePosition` als `pendingResumePosition` und ruft `TryApplyPendingResumeSeek` — gemarshalt auf UI-Thread, kein direkter Seek (iOS `MediaOpened` vor `ReadyToPlay`, Toolkit-Issue #3249) |
| `TryApplyPendingResumeSeek` | private | **Seek-Gate:** liest `CurrentState` (try/catch); nur bei `Playing`/`Paused` wird `pendingResumePosition` geleert und `ApplyResumeSeekAsync` einmalig angestoßen — sonst bleibt der Wunsch bis zum nächsten `StateChanged` liegen |
| `ApplyResumeSeekAsync` | private async | **Fehlerquelle der Anforderung:** `SeekTo(position, timeout.Token)` unter `CancellationTokenSource(ResumeSeekTimeout)`; Erfolg → Info-Log; **jede** Ausnahme (inkl. `TaskCanceledException` des Timeouts) → Warn-Log + `PlaybackFailedMessage`-Dispatch (wenn `subscribed`) |
| `OnMediaEnded` | private (Event-Handler) | Dispatcht `PlaybackEndedMessage` |
| `OnMediaFailed` | private (Event-Handler) | Echter Abspielfehler: loggt `e.ErrorMessage` (sanitized), dispatcht `PlaybackFailedMessage` |
| `OnMediaStateChanged` | private (Event-Handler) | Marshals `ReportPosition` + `TryApplyPendingResumeSeek` auf den UI-Thread |
| `OnPipClicked` / `OnPreviousClicked` / `OnNextClicked` | private | Dispatchen `PictureInPictureRequestedMessage` / `PlaylistPreviousRequestedMessage` / `PlaylistNextRequestedMessage` |
| `OnPlayerTapped` | private | Windows-Workaround: `TransportControls.Show()` |

Abonnierte Events (XAML, `PlayerPage.xaml` Z. 13–20): `MediaOpened`, `MediaEnded`, `MediaFailed`, `StateChanged` des `MediaElement` (Toolkit `CommunityToolkit.Maui.MediaElement` 10.0.0); `ModelChanged` des `MvuHost`.
Publizierte Messages: `PlayerPageOpenedMessage`, `PlaybackProgressMessage`, `PlaybackEndedMessage`, **`PlaybackFailedMessage` (aus drei Methoden: `FailPlaybackStart`, `OnMediaFailed`, `ApplyResumeSeekAsync`)**, `PictureInPictureRequestedMessage`, `PlaylistPreviousRequestedMessage`, `PlaylistNextRequestedMessage`, `PlayerClosedMessage`.

## `AppUpdate` (`VideoPlayer.Mvu`)
Datei: `VideoPlayer.Mvu/AppUpdate.cs` (4328 Zeilen; statische Update-Funktion)

Konstanten: `PlaybackUnavailableError` (public, Z. 75 — „Das Video kann nicht abgespielt werden."), `PlaybackEndedToleranceSeconds` (private, Z. 88 — 300 s).

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `Update` | public static | Zentrale Dispatch-Tabelle (Z. ~244–300); `PlaybackFailedMessage => UpdatePlaybackFailed(model)` (Z. 285) |
| `UpdatePlayRequested` | private static | Validiert Medium (`movie`/`tvshowepisode`) → `StartPlayback` (Z. 3605) |
| `StartPlayback` | private static | Belegt `PlayerState` (`IsResolving`, `Attempted`, `UpcomingEpisodes` via `NextEpisodeQueue`); Effekte `NavigateToPlayerEffect` + `ResolvePlaybackSourceEffect` (Z. 3628) |
| `NextEpisodeQueue` | private static | Baut `UpcomingEpisodes` aus dem Serienkontext der Detailseite (Z. 3655) |
| `UpdatePlayerPageOpened` | private static | Setzt `Attempted` (Z. 3679) |
| `UpdatePlaybackSourceResolved` | private static | Verwirft veraltete Antworten (Identitätsprüfung); bei Erfolg `Source` übernehmen; bei `Source = null` → **Rückfall:** Playlist → `AdvancePlaylistEntry`, sonst `AdvanceToNextEpisode` mit `message.Error ?? PlaybackUnavailableError` (Z. 3687–3722) |
| `UpdatePlaybackFailed` | private static | **Zielmethode der Anforderung** (Z. 4033–4050): bei `!player.IsActive` unverändert; sonst ohne weitere Differenzierung Rückfall — `IsPlaylistPlayback && !PlaylistNextExhausted` → `AdvancePlaylistEntry`, sonst `AdvanceToNextEpisode`, jeweils mit `PlaybackUnavailableError` |
| `AdvanceToNextEpisode` | private static | Serien-Rückfall/Auto-Fortsetzung (Z. 3735): ohne `HasNextEpisode` → Fehlerzustand (Quelle leeren, `Error` setzen); sonst nächste Episode aus `UpcomingEpisodes[0]` übernehmen, Wiedergabe-Werte zurücksetzen, `ResolvePlaybackSourceEffect("tvshowepisode", next.MediaId)` |
| `AdvancePlaylistEntry` | private static | Playlist-Rückfall/Auto-Fortsetzung (Z. 2612): `Source = null`, `IsResolving = true`, `Error = error`; Effekt `AdvancePlaylistEntryEffect(PlaylistId, PlaylistEntryId)` |
| `UpdatePlaybackProgress` | private static | Positions-Takt: Fortschrittsmeldung (Throttling), `IsWatched`, Auto-Fortsetzung über `AdvancePlaylistEntry`/`AdvanceToNextEpisode` mit `error: null` (Z. 3792) |
| `UpdatePlaybackEnded` | private static | Ende-Ereignis nur bei Plausibilität (`IsPlaybackEndPlausible`); Abschluss + Auto-Fortsetzung wie im Takt (Z. 3896) |
| `IsPlaybackEndPlausible` | private static | Ende nur bei Restlaufzeit ≤ `PlaybackEndedToleranceSeconds` (Z. 3973) |
| `UpdatePlaylistNavigationCompleted` | private static | Übernimmt den per `play/advance`/`next`/`previous` aufgelösten Zieleintrag; leeres Ergebnis (204) setzt `PlaylistNextExhausted` — Wiedergabe läuft weiter (Z. 2540) |
| `UpdatePlayerClosed` | private static | Abschluss-Fortschritt + `PlayerState.Empty` (Z. 4067) |
| `MarkEpisodeWatchedInDetails` | private static | Überträgt `IsWatched` auf die geladene Episodenliste (Z. 3994) |

Publizierte Effekte (Player-Pfad): `NavigateToPlayerEffect`, `ResolvePlaybackSourceEffect`, `AdvancePlaylistEntryEffect`, `ReportPlaybackProgressEffect`, `DeleteWatchedDownloadEffect`, `TogglePictureInPictureEffect`, `LoadUpcomingEpisodesEffect`.

## `PlaybackSourceService` (`VideoPlayer.Mvu`)
Datei: `VideoPlayer.Mvu/PlaybackSourceService.cs` (278 Zeilen)

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `ResolveAsync` | public | Löst Quelle auf: `LocalFile` aus Download-Manifest hat Vorrang, sonst `ServerStream`-URL (`api/items/{typ}/{id}/stream?access_token=…`); `null` bei nicht abspielbarem Typ/keiner Quelle. Fortsetzungsposition via `resumePositionSecondsOverride` (Playlist-Antwort) oder `ResolveResumePositionAsync` |
| `ResolveResumePositionAsync` | private | Weiterschauen-Liste vom Server (`GetContinueWatchingAsync`), Fehlschlag → Cache-Fallback |
| `ReadCachedResumePositionAsync` | private | Cache-Kopie `cached_lists[continue_watching]` |
| `FindResumePosition` | private static | Position > 0 → `TimeSpan`, sonst `null` |
| `ResolveLocalFileAsync` | private | Abgeschlossener Download mit existierender Datei |

## `ShellAppEffectHandler` (`VideoPlayer.Maui`)
Datei: `VideoPlayer.Maui/Services/ShellAppEffectHandler.cs` (2300 Zeilen; `IAppEffectHandler`-Implementierung)

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `HandleAsync` | public | Effekt-Switch; u. a. `NavigateToPlayerEffect` (Z. 283), `ResolvePlaybackSourceEffect` (Z. 286), `AdvancePlaylistEntryEffect` (Z. 234), `ReportPlaybackProgressEffect` (Z. 292), `TogglePictureInPictureEffect` (Z. 295) |
| `ResolvePlaybackSourceAsync` | private | Ruft `PlaybackSourceService.ResolveAsync` (`allowServerStream` aus `Model.IsOffline`, `resumePositionSecondsOverride` aus `resolve.StartPositionSeconds`); loggt „Wiedergabe gestartet … über {Kind}"; dispatcht `PlaybackSourceResolvedMessage` — bei Fehlschlag/`null` mit `PlaybackUnavailableError` (Z. 418–458) |
| `NavigatePlaylistEntryAsync` | private | `advance` → `PlaylistService.AdvanceAsync` (`play/advance`), sonst `GetNextEntryAsync`/`GetPreviousEntryAsync`; dispatcht `PlaylistNavigationCompletedMessage` (Z. 1404–1430) |

## `PlaylistService` (`VideoPlayer.Mvu`)
Datei: `VideoPlayer.Mvu/PlaylistService.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `AdvanceAsync` | public | `POST api/playlists/{id}/play/advance` — serverseitige Folge-Ermittlung für Auto-Fortsetzung und Fehler-Rückfall (Z. ~285) |
| `GetNextEntryAsync` / `GetPreviousEntryAsync` | public | Manuelle Playlist-Navigation (`play/next`/`play/previous`) |
| `StartPlaybackAsync` | public | `POST api/playlists/{id}/play` (Playlist-Start) |

## `MvuHost` (`VideoPlayer.Mvu`)
Datei: `VideoPlayer.Mvu/MvuHost.cs` (69 Zeilen)

| Methode/Member | Sichtbarkeit | Kurzbeschreibung |
|----------------|-------------|------------------|
| `Model` | public | Aktuelles unveränderliches `AppModel` (`Player` an `AppModel.cs` Z. 209) |
| `ModelChanged` | public event | Nach jedem Update — von `PlayerPage.OnModelChanged` abonniert |
| `DispatchAsync` | public | Serialisiert Update via `SemaphoreSlim gate`, feuert `ModelChanged`/`PropertyChanged`, führt Effekte sequenziell über `IAppEffectHandler` aus |

## `AppLogService` (`VideoPlayer.Storage`)
Datei: `VideoPlayer.Storage/AppLogService.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `MaskAccessToken` | public static (Z. 194) | Maskiert `access_token`-Query-Werte in Protokollmeldungen — vom Sinkenpunkt selbst (Z. 116/119) und von `PlayerPage.SanitizeForLog` genutzt |
