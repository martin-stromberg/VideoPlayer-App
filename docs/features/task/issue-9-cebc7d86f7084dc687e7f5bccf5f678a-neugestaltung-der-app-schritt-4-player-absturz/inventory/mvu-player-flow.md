# MVU-Wiedergabefluss — betroffene und unberührte Teile

## `MvuHost`

Datei: `VideoPlayer.Mvu/MvuHost.cs`

| Element | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `DispatchAsync(AppMessage, CancellationToken)` | public | Update serialisiert über `SemaphoreSlim gate`, danach `PropertyChanged` + `ModelChanged` auf dem aufrufenden Thread, dann Effekte sequenziell |
| `ModelChanged` | public event | Auslöser der Seiten-Updates; kein Abmeldeweg auf Host-Seite nötig (reines C#-Event) |
| `Model` | public | unveränderliches `AppModel` |

Wichtig: `ModelChanged` wird auf dem Thread ausgelöst, der `DispatchAsync`
aufgerufen hat. Pages müssen deshalb selbst auf den UI-Thread marshallen
(`OnModelChanged` tut das bereits).

## `AppUpdate` — Player-Nachrichten

Datei: `VideoPlayer.Mvu/AppUpdate.cs`

| Methode | Nachricht | Wirkung |
|---------|-----------|---------|
| `UpdatePlayRequested` (:1334) | `PlayRequestedMessage` | Neuer `PlayerState` + `NavigateToPlayerEffect` + `ResolvePlaybackSourceEffect`; nur `movie`/`tvshowepisode` mit Id > 0 |
| `UpdatePlayerPageOpened` (:1391) | `PlayerPageOpenedMessage` | `Attempted = true` |
| `UpdatePlaybackSourceResolved` (:1399) | `PlaybackSourceResolvedMessage` | `Source` setzen oder `AdvanceToNextEpisode`-Rückfall mit Fehler |
| `UpdatePlaybackProgress` (:1498) | `PlaybackProgressMessage` | Position/Dauer übernehmen, `ReportPlaybackProgressEffect` bei >0,5 s Positionsänderung, `IsWatched` über `WatchedEndWindowSeconds`, Auto-Fortsetzung über `AutoResumeRemainingSeconds` |
| `UpdatePlaybackEnded` (:1562) | `PlaybackEndedMessage` | `IsWatched = true`, Endpositions-Meldung, Auto-Fortsetzung |
| `UpdatePlaybackFailed` (:1609) | `PlaybackFailedMessage` | `AdvanceToNextEpisode` mit `PlaybackUnavailableError` |
| `UpdatePlayerClosed` (:1637) | `PlayerClosedMessage` | PiP verlassen, Abschluss-Fortschritt melden, `Player = PlayerState.Empty` |

## `ShellAppEffectHandler` — Player-Effekte

Datei: `VideoPlayer.Maui/Services/ShellAppEffectHandler.cs`

| Stelle | Zweck |
|--------|-------|
| `NavigateToPlayerAsync` (:201) | `Shell.Current.GoToAsync("player")` — erzeugt je Aufruf eine neue `PlayerPage` |
| `ResolvePlaybackSourceAsync` (:218) | `PlaybackSourceService.ResolveAsync` (lokal vor Stream, offline nur lokal) → `PlaybackSourceResolvedMessage` über `DispatchMessageAsync` |
| `ReportPlaybackProgressAsync` (:246) | `ProgressReportService.ReportProgressAsync` (Outbox-fähig) |
| `TogglePictureInPictureAsync` (:262) | `IPictureInPictureService.ToggleAsync` auf dem MainThread |
| `DispatchMessageAsync` (:762) | dispatcht Messages über `MainThread.InvokeOnMainThreadAsync` |

## Unberührt von dieser Anforderung

- `ReportPlaybackProgressEffect`/`ProgressReportService` —
  Fortschrittsmeldung bleibt wie gehabt (Akzeptanzkriterium).
- `IsWatched`/`WatchedEndWindowSeconds`/`PlaybackEndedMessage` —
  Gesehen-Markierung bleibt wie gehabt.
- `AppMessage.cs`/`AppEffect.cs` — keine neuen Messages/Effekte nötig.
- `PlayerState`, `PlaybackSource`, `PlaybackSourceService` — keine
  Änderung.
