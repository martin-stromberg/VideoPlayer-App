# Tasks: Clientseitiger Zugriff auf die Server-API (Neuimplementierung)

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Projekt | `VideoPlayer.ApiClient.csproj` anlegen (`net10.0`, `Logging.Abstractions`, `VideoPlayerApiToken`-Property + Assembly-Metadata) | Erledigt | `Endpoint_UsesExpectedMethodAndPath` (Projekt kompiliert, Client nutzbar) |
| 2 | Projekt | `VideoPlayer.ApiClient` in `VideoPlayer.App.sln` eintragen | Erledigt | Build der Solution |
| 3 | Fehlermodell | `ApiException` anlegen (`StatusCode`, `Endpoint`, `ResponseBody`) | Erledigt | `ServerError_Permanent_ThrowsAfterExhaustion`, `ClientError_NotRetried` |
| 4 | Konfiguration | `ClientGate` anlegen (`X-API-Key`, Resolve: Env → Metadata → Platzhalter, testbare Überladung) | Erledigt | `ClientGateTests` (3 Tests) |
| 5 | Konfiguration | `VideoPlayerApiClientOptions` anlegen (`BaseAddress`, `ApiKey`, `TokenRefresher`, `DelayAsync`, `Logger`) | Erledigt | `AllRequests_ContainClientGateHeader`, `Unauthorized_RefreshesTokenOnce_AndResends`, Delay-Tests |
| 6 | Datenmodell | `LoginRequest`, `AuthToken` anlegen | Erledigt | `LoginAsync_PostsCredentialsAndSetsBearerToken` |
| 7 | Datenmodell | `MediaEntry`-Hierarchie anlegen (`Movie`, `MovieCollection`, `TvShow`, `TvShowSeason`, `TvShowEpisode`, `Picture`, `$type`-Polymorphie) | Erledigt | `MediaEntrySerializationTests` (7 Tests) |
| 8 | Datenmodell | `MediaSource`, `SourceGenres`, `Genre`, `GenreOption` anlegen | Erledigt | `GetSourcesAsync_DeserializesSources`, Endpoint-Mapping `SourceGenres`/`GenreOptions` |
| 9 | Datenmodell | `RecentEntry`, `RecentEntryKind` anlegen | Erledigt | `GetRecentEntriesAsync_DeserializesPolymorphicEntries` |
| 10 | Datenmodell | `ContinueWatchingEntry`, `ContinueWatchingMutationResult`, `PlaybackProgressReport` anlegen | Erledigt | `GetContinueWatchingAsync_DeserializesEntries`, Endpoint-Mapping `Hide`/`Skip`/`Progress` |
| 11 | Datenmodell | `FavoriteEntry`, `MediaItemSummary` anlegen | Erledigt | `GetFavoritesAsync_DeserializesEntries`, `GetSourceItemsAsync_DeserializesSummaries` |
| 12 | Logik | `VideoPlayerApiClient`-Kern: `SendWithPolicyAsync` (Request-Factory, Retry 1 s/2 s/4 s, 401-Refresh einmalig via `SemaphoreSlim`, `allowReauth`) | Erledigt | `VideoPlayerApiClientTests_RetryPolicy`, `VideoPlayerApiClientTests_TokenRefresh` |
| 13 | Logik | `LoginAsync`, `SetAccessToken`, `CurrentToken`, `CheckHealthAsync` implementieren | Erledigt | `VideoPlayerApiClientTests_Authentication` |
| 14 | Logik | Quellen-Methoden (`GetSourcesAsync`, `GetSourceAsync`, `GetSourceGenresAsync`) implementieren | Erledigt | Endpoint-Mapping `Sources`/`Source`/`SourceGenres` |
| 15 | Logik | `GetRecentEntriesAsync` implementieren | Erledigt | Endpoint-Mapping `Recent` + Deserialisierungstest |
| 16 | Logik | Weiterschauen-Methoden (`GetContinueWatchingAsync`, `ReportPlaybackProgressAsync`, `HideContinueWatchingEntryAsync`, `SkipContinueWatchingEntryAsync`) implementieren | Erledigt | Endpoint-Mapping `ContinueWatching`/`Progress`/`Hide`/`Skip` |
| 17 | Logik | Favoriten-Methoden (`GetFavoritesAsync`, `ToggleFavoriteAsync`, `RemoveFavoriteAsync`) implementieren | Erledigt | Endpoint-Mapping `Favorites`/`ToggleFavorite`/`RemoveFavorite` |
| 18 | Logik | Medien-Methoden (`GetSourceItemsAsync`, `GetMovieAsync`, `GetMovieCollectionAsync`, `GetTvShowAsync`, `GetTvShowEpisodeAsync`, `GetGenreOptionsAsync`) implementieren | Erledigt | Endpoint-Mapping `SourceItems`/`SourceItemsSearchGenre`/`Movie`/`MovieCollection`/`TvShow`/`TvShowEpisode`/`GenreOptions` |
| 19 | Logik | Bildabrufe (`GetPictureAsync`, `GetSourceIconAsync`) implementieren | Erledigt | Endpoint-Mapping `Picture`/`SourceIcon`, `GetPictureAsync_ReturnsBytes` |
| 20 | Tests | `ProjectReference` auf `VideoPlayer.ApiClient` in `VideoWebPlayer.Maui.Tests.csproj` ergänzen | Erledigt | Testlauf kompiliert und läuft |
| 21 | Tests | `StubHttpMessageHandler` + `RecordingDelay`-Testhilfen anlegen | Erledigt | Von allen neuen Tests verwendet |
| 22 | Tests | `VideoPlayerApiClientTests_Authentication` (Login, Bearer auf Folgerequests, `X-API-Key`) | Erledigt | 5 Tests bestanden |
| 23 | Tests | `VideoPlayerApiClientTests_RetryPolicy` (5xx-Retries mit Delays, 4xx ohne Retry außer 408/429, Transport-/Timeout-Fehler, Caller-Abbruch) | Erledigt | 11 Tests bestanden |
| 24 | Tests | `VideoPlayerApiClientTests_TokenRefresh` (401 → einmaliger Refresh + Resend; Refresh-Fehler; zweites 401; Login ohne Reauth) | Erledigt | 5 Tests bestanden |
| 25 | Tests | `VideoPlayerApiClientTests_Endpoints` (Methoden-/Pfad-Mapping, DTO-Deserialisierung, Bildabrufe) | Erledigt | 27 Tests bestanden |
| 26 | Tests | `ClientGateTests` (Resolve-Reihenfolge) und `MediaEntrySerializationTests` (`$type`-Roundtrip) | Erledigt | 10 Tests bestanden |
| 27 | Verifikation | Build + `dotnet test` + `dotnet format whitespace --verify-no-changes` | Erledigt | 69/69 Tests bestanden, Format-Check sauber |
