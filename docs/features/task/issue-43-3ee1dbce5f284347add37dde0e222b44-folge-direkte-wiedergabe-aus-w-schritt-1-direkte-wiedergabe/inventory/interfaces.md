# Interfaces — Direkte Wiedergabe aus der Weiterschauen-Liste

## `IAppEffectHandler`
Datei: `VideoPlayer.Mvu/IAppEffectHandler.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `HandleAsync` | `AppEffect effect`, `CancellationToken` | `Task` | Führt einen MVU-Effekt am App-Rand aus; neue Effekte werden im `switch` von `ShellAppEffectHandler.HandleAsync` ergänzt |

## `INetworkStatusMonitor`
Datei: `VideoPlayer.Mvu/INetworkStatusMonitor.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `Start` | – | `void` | Überwachung starten (idempotent) |
| `IsOnline` | – | `bool` | Letzter bekannter Netzwerkstatus |
| `IsWifi` | – | `bool` | WLAN-Status (Download-Bedingung) |
| `AvailabilityChanged` | – | `EventHandler<bool>` | Netzwerkwechsel-Ereignis |
| `SetOnline` | `bool` | `void` | (Testbarkeit) Status setzen |

## `VideoPlayerApiClient` (Auszug, keine Schnittstelle — konkrete Klasse)
Datei: `VideoPlayer.ApiClient/VideoPlayerApiClient.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetTvShowAsync` | `long id`, `CancellationToken` | `Task<TvShow>` | `GET api/items/tvshow/{id}` — Serie inkl. Staffeln/Episoden (Serienkontext-Quelle für Folge-Episoden) |
| `GetContinueWatchingAsync` | `CancellationToken` | `Task<IReadOnlyList<ContinueWatchingEntry>>` | `GET api/continue-watching` (Fortsetzungsposition, `PlaybackSourceService`) |
| `GetTvShowEpisodeAsync` | `long id`, `CancellationToken` | `Task<TvShowEpisode>` | Episoden-Details (Detailseite) |
| `GetMovieAsync` | `long id`, `CancellationToken` | `Task<Movie>` | Film-Details (Detailseite) |
