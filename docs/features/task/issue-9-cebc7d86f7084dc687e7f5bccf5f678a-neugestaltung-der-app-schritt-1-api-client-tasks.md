# Tasks: Clientseitiger Zugriff auf die Server-API (Neuimplementierung)

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Projekt | `VideoPlayer.ApiClient.csproj` anlegen (`net10.0`, `Logging.Abstractions`, `VideoPlayerApiToken`-Property + Assembly-Metadata) | Offen | — |
| 2 | Projekt | `VideoPlayer.ApiClient` in `VideoPlayer.App.sln` eintragen | Offen | — |
| 3 | Fehlermodell | `ApiException` anlegen (`StatusCode`, `Endpoint`, `ResponseBody`) | Offen | — |
| 4 | Konfiguration | `ClientGate` anlegen (`X-API-Key`, Resolve: Env → Metadata → Platzhalter, testbare Überladung) | Offen | — |
| 5 | Konfiguration | `VideoPlayerApiClientOptions` anlegen (`BaseAddress`, `ApiKey`, `TokenRefresher`, `DelayAsync`, `Logger`) | Offen | — |
| 6 | Datenmodell | `LoginRequest`, `AuthToken` anlegen | Offen | — |
| 7 | Datenmodell | `MediaEntry`-Hierarchie anlegen (`Movie`, `MovieCollection`, `TvShow`, `TvShowSeason`, `TvShowEpisode`, `Picture`, `$type`-Polymorphie) | Offen | — |
| 8 | Datenmodell | `MediaSource`, `SourceGenres`, `Genre`, `GenreOption` anlegen | Offen | — |
| 9 | Datenmodell | `RecentEntry`, `RecentEntryKind` anlegen | Offen | — |
| 10 | Datenmodell | `ContinueWatchingEntry`, `ContinueWatchingMutationResult`, `PlaybackProgressReport` anlegen | Offen | — |
| 11 | Datenmodell | `FavoriteEntry`, `MediaItemSummary` anlegen | Offen | — |
| 12 | Logik | `VideoPlayerApiClient`-Kern: `SendWithPolicyAsync` (Request-Factory, Retry 1 s/2 s/4 s, 401-Refresh einmalig via `SemaphoreSlim`, `allowReauth`) | Offen | — |
| 13 | Logik | `LoginAsync`, `SetAccessToken`, `CurrentToken`, `CheckHealthAsync` implementieren | Offen | — |
| 14 | Logik | Quellen-Methoden (`GetSourcesAsync`, `GetSourceAsync`, `GetSourceGenresAsync`) implementieren | Offen | — |
| 15 | Logik | `GetRecentEntriesAsync` implementieren | Offen | — |
| 16 | Logik | Weiterschauen-Methoden (`GetContinueWatchingAsync`, `ReportPlaybackProgressAsync`, `HideContinueWatchingEntryAsync`, `SkipContinueWatchingEntryAsync`) implementieren | Offen | — |
| 17 | Logik | Favoriten-Methoden (`GetFavoritesAsync`, `ToggleFavoriteAsync`, `RemoveFavoriteAsync`) implementieren | Offen | — |
| 18 | Logik | Medien-Methoden (`GetSourceItemsAsync`, `GetMovieAsync`, `GetMovieCollectionAsync`, `GetTvShowAsync`, `GetTvShowEpisodeAsync`, `GetGenreOptionsAsync`) implementieren | Offen | — |
| 19 | Logik | Bildabrufe (`GetPictureAsync`, `GetSourceIconAsync`) implementieren | Offen | — |
| 20 | Tests | `ProjectReference` auf `VideoPlayer.ApiClient` in `VideoWebPlayer.Maui.Tests.csproj` ergänzen | Offen | — |
| 21 | Tests | `StubHttpMessageHandler` + `RecordingDelay`-Testhilfen anlegen | Offen | — |
| 22 | Tests | `VideoPlayerApiClientTests_Authentication` (Login, Bearer auf Folgerequests, `X-API-Key`) | Offen | — |
| 23 | Tests | `VideoPlayerApiClientTests_RetryPolicy` (5xx-Retries mit Delays, 4xx ohne Retry außer 408/429, Transport-/Timeout-Fehler, Caller-Abbruch) | Offen | — |
| 24 | Tests | `VideoPlayerApiClientTests_TokenRefresh` (401 → einmaliger Refresh + Resend; Refresh-Fehler; zweites 401; Login ohne Reauth) | Offen | — |
| 25 | Tests | `VideoPlayerApiClientTests_Endpoints` (Methoden-/Pfad-Mapping, DTO-Deserialisierung, Bildabrufe) | Offen | — |
| 26 | Tests | `ClientGateTests` (Resolve-Reihenfolge) und `MediaEntrySerializationTests` (`$type`-Roundtrip) | Offen | — |
| 27 | Verifikation | Build + `dotnet test` + `dotnet format whitespace --verify-no-changes` | Offen | — |
