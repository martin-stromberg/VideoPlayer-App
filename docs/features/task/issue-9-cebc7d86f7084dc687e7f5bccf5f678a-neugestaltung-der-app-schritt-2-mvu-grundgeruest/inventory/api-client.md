# API-Client (`VideoPlayer.ApiClient`)

Plattformneutrales `net10.0`-Projekt aus Entwicklungsschritt 1. Vollständige
Dokumentation: `docs/help/api-client/`.

## `VideoPlayerApiClient`

Datei: `VideoPlayer.ApiClient/VideoPlayerApiClient.cs`

| Methode | Signatur (Kurzform) | Zweck |
|---------|---------------------|-------|
| Konstruktor | `(HttpClient, VideoPlayerApiClientOptions?)` | Wirft `InvalidOperationException`, wenn keine `BaseAddress` gesetzt ist; setzt `X-API-Key`-Header aus `ApiKey` oder `ClientGate.Resolve()` |
| `CheckHealthAsync` | `(CancellationToken) → Task<bool>` | `GET api/health`; liefert `true` bei 2xx, `false` bei `ApiException` (inkl. 4xx/5xx nach Retry) |
| `LoginAsync` | `(email, password, ct) → Task<AuthToken>` | `POST api/auth/login`, setzt Bearer-Token |
| `SetAccessToken` / `ClearAccessToken` | `(AuthToken)` / `()` | Tokenverwaltung |
| `GetSourcesAsync` | `(ct) → Task<IReadOnlyList<MediaSource>>` | `GET api/Sources` |
| `GetSourceAsync`, `GetSourceGenresAsync` | `(id, ct)` | Quelle / Genres einer Quelle |
| `GetRecentEntriesAsync` | `(ct)` | `GET api/items/recent` („Neu im Programm") |
| `GetContinueWatchingAsync`, `ReportPlaybackProgressAsync`, `HideContinueWatchingEntryAsync`, `SkipContinueWatchingEntryAsync` | diverse | Weiterschauen |
| `GetFavoritesAsync`, `ToggleFavoriteAsync`, `RemoveFavoriteAsync` | diverse | Favoriten |
| `GetSourceItemsAsync`, `GetMovieAsync`, `GetMovieCollectionAsync`, `GetTvShowAsync`, `GetTvShowEpisodeAsync`, `GetGenreOptionsAsync` | diverse | Mediennachschlagen |
| `GetPictureAsync`, `GetSourceIconAsync` | `(pictureId, ct) → Task<byte[]>` | Bildabrufe |

Retry-Pipeline: `SendWithPolicyAsync` — Initialversuch + bis zu 3 Wiederholungen
(1 s/2 s/4 s) für 5xx, Transportfehler, 408, 429; bei 401 einmalige Token-Erneuerung via
`VideoPlayerApiClientOptions.TokenRefresher`.

## `VideoPlayerApiClientOptions`

Datei: `VideoPlayer.ApiClient/VideoPlayerApiClientOptions.cs`

| Eigenschaft | Typ | Zweck |
|-------------|-----|-------|
| `BaseAddress` | `Uri?` | Server-Basisadresse; wird auf den `HttpClient` übernommen, falls dort nicht gesetzt |
| `ApiKey` | `string?` | Client-Gate-Schlüssel; Standard `ClientGate.Resolve()` |
| `TokenRefresher` | `Func<CancellationToken, Task<AuthToken?>>?` | einmalige Token-Erneuerung bei 401 |
| `DelayAsync` | `Func<TimeSpan, CancellationToken, Task>?` | Warteimplementierung (Test-Hook) |
| `Logger` | `ILogger?` | Diagnoseausgaben |

## `ClientGate`

Datei: `VideoPlayer.ApiClient/ClientGate.cs`

- `HeaderName = "X-API-Key"`
- Auflösungsreihenfolge: Umgebungsvariable `VIDEOWEBPLAYER_MAUI_API_TOKEN` →
  Assembly-Metadata `VideoPlayer.ApiClient.ApiToken` (MSBuild-Eigenschaft
  `VideoPlayerApiToken` in `VideoPlayer.ApiClient.csproj`) → Platzhalter
  `<MAUI_CLIENT_API_TOKEN>`.

## `ApiException`

Datei: `VideoPlayer.ApiClient/ApiException.cs` — Endpoint, Statuscode, Response-Body.

## Models

`VideoPlayer.ApiClient/Models/`: `AuthToken`, `LoginRequest`, `MediaSource`,
`SourceGenres`, `RecentEntry`, `ContinueWatchingEntry`, `PlaybackProgressReport`,
`ContinueWatchingMutationResult`, `FavoriteEntry`, `MediaEntry`, `MediaItemSummary`,
`GenreOption` u. a.

## Sonstiges

- `InternalsVisibleTo`: `VideoWebPlayer.Maui.Tests`.
- Paket: `Microsoft.Extensions.Logging.Abstractions` 10.0.10.
