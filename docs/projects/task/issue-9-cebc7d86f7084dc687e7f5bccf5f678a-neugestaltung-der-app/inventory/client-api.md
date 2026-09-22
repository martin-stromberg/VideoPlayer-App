# API-Client-Schicht (`VideoWebPlayer.Client`)

Eigenständiges .NET-10-Klassenbibliotheksprojekt ohne MAUI-Abhängigkeit (`VideoWebPlayer.Client/VideoWebPlayer.Client.csproj`, `net10.0`). Paketreferenzen: `Microsoft.Extensions.Logging.Abstractions`, `Microsoft.Extensions.Http`, `Microsoft.Extensions.Configuration.Abstractions` (je 10.0.10). Laut `README.md` ist das Projekt eine „kopierte Übergabeschnittstelle aus dem Web-Repository"; der verbindliche API-Vertrag liegt im Server-Repository (`docs/API.md`).

## `VideoWebPlayerClient`

Datei: `VideoWebPlayer.Client/VideoWebPlayerClient.cs`

HTTP-Client-Wrapper über `HttpClient` mit generischen Hilfsmethoden `HttpGetAsync<T>`, `HttpPostAsync<T>` und `HttpPostAsync` (void). Alle drei prüfen auf `401 Unauthorized` und rufen dann die virtuelle Methode `HandleUnauthorized()` auf (Basisimplementierung: `return false`); bei Erfolg wird der Request einmal wiederholt, bei erneutem 401 oder fehlgeschlagenem Refresh wird `HttpRequestException` geworfen. `skipReauthorize` verhindert den Refresh beim Login-Aufruf selbst. Keine generelle Retry-Strategie für andere Fehlercodes vorhanden (nur 1× Retry nach Token-Refresh).

| Methode | HTTP | Endpunkt | Zweck |
|---------|------|----------|-------|
| `AuthenticateAsync(email, password)` | POST | `api/auth/login` | Login, setzt Bearer-Token via `SetAuthorizationToken` |
| `SetAuthorizationToken(token)` | – | – | Setzt `Authorization: Bearer` auf dem `HttpClient` (virtual) |
| `AuthorizationToken` (Property) | – | – | Liest aktuellen Bearer-Token |
| `EnsureAuthorizationTokenAsync(user, ct)` | – | – | No-op in der Basis (für abgeleitete Impersonation) |
| `HealthCheckAsync()` | GET | `api/health` | Erreichbarkeitsprüfung, `bool` |
| `RequestSourcesAsync()` | GET | `api/Sources` | Liste `DtoMediaSource`, bei Fehler leeres Array |
| `RequestSourceAsync(sourceId)` | GET | `api/Sources/{id}` | Einzelne Quelle, bei Fehler `null` |
| `RequestSourceGenresAsync(sourceId)` | GET | `api/SourceGenres/{id}` | `SourceGenresDto`, bei Fehler `null` |
| `RequestRecentEntriesAsync()` | GET | `api/items/recent` | „Neu im Programm", `DtoRecentEntry[]` |
| `RequestContinueWatchingAsync()` / `GetContinueWatchingAsync()` | GET | `api/continue-watching` | Weiterschauen-Liste (zwei äquivalente Methoden vorhanden) |
| `HideContinueWatchingAsync(mediaType, mediaId)` | POST | `api/continue-watching/hide` | `ContinueWatchingMutationResult` |
| `SkipContinueWatchingAsync(mediaType, mediaId)` | POST | `api/continue-watching/skip` | `ContinueWatchingMutationResult` |
| `ReportPlaybackProgressAsync(mediaType, mediaId, positionSeconds, durationSeconds)` | POST | `api/continue-watching/progress` | Fortschrittsmeldung mit Coalescing (s. u.) |
| `RequestFavoritesAsync()` | GET | `api/favorites` | `DtoFavoriteEntry[]` |
| `ToggleFavorite(entry)` | POST | `api/favorites/toggle` | `bool` |
| `RemoveFavoriteAsync(favoriteId)` | POST | `api/favorites/remove` | void |
| `RequestSourceItems(mediaSourceId, page, pageSize, searchText, genreId)` | GET | `/api/items?mediaSourceId=…&page=…&size=…[&search=…][&genreId=…]` | Paginierte Quelleneinträge, `List<MediaEntryDto>` |
| `RequestMovieCollectionAsync(id)` | GET | `api/items/moviecollection/{id}` | `DtoMovieCollection` |
| `RequestMovieAsync(id)` | GET | `api/items/movie/{id}` | `DtoMovie`, bei Fehler `null` |
| `RequestTVShowAsync(id)` | GET | `api/items/tvshow/{id}` | `DtoTVShow` |
| `RequestTVShowEpisodeAsync(id)` | GET | `api/items/tvshowepisode/{id}` | `DtoTVShowEpisode`, bei Fehler `null` |
| `RequestGenreOptionsAsync()` | GET | `api/items/genres` | `List<DtoGenreOption>` |
| `SaveMetadataAsync(request)` | POST | `api/items/metadata` | Metadaten schreiben |
| `GetPictureAsync(pictureId)` | GET | `api/pictures/{id}` | Bilddaten als `byte[]` |
| `GetSourcePictureAsync(pictureId)` | GET | `api/sourceicons/{id}` | Quellen-Icon als `byte[]` |
| `DeleteSourceAsync(sourceId)` | DELETE | `api/admin/sources/{id}` | Quelle löschen (Admin-API) |

Besonderheiten:

- `ReportPlaybackProgressAsync` implementiert Coalescing pro Medium (`ConcurrentDictionary<string, ProgressSendState>` mit `lock`): Pro `mediaType:mediaId` läuft nur ein Request; währenddessen wird genau eine letzte Meldung gepuffert und nachgesendet. Es gibt **keine** Persistenz/Outbox — bei Offline oder Fehler geht die Meldung verloren (`SendPlaybackProgressCoreAsync` wirft bei Fehler).
- `Initializing` (public Property) und `Logger` (protected) vorhanden.
- Streaming-URL wird nicht im Client, sondern in `VideoWebPlayer.Maui/Services/DownloadManager.cs` (`BuildStreamUrl`) gebaut: `{server}/api/items/{videoType}/{videoId}/stream?access_token={jwt}` (Token als Query-Parameter).
- Deserialisierung mit `PropertyNameCaseInsensitive = true`.

## `MauiVideoWebPlayerClient`

Datei: `VideoWebPlayer.Maui/Services/MauiVideoWebPlayerClient.cs`

Abgeleiteter Client (liegt im MAUI-Projekt, nicht in `VideoWebPlayer.Client`):

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|--------------|------------------|
| `HandleUnauthorized()` | `protected override` | Stiller Re-Login via `IAuthService.GetCredentials()` + `AuthenticateAsync`; setzt neuen Token |
| `LoadAuthTokenFromSettings()` | `private` | Lädt gespeicherten Token aus `ISettingsService` beim Start |
| `ReloadAuthToken()` | `public` | Token neu aus Settings laden (z. B. nach Login) |
| `SetAuthorizationToken(token)` | `public override` | Persistiert Token zusätzlich via `ISettingsService.SetAuthToken` |
| `ClearAuthorizationToken()` | `public` | Löscht Token aus Settings und Client |

Registrierung in `MauiProgram.cs`: `AddHttpClient<MauiVideoWebPlayerClient>` mit `BaseAddress` aus `ISettingsService.ServerAddress` + `MauiClientConfiguration.ApplyClientGateHeader`; zusätzlich `AddScoped<VideoWebPlayerClient>` als Alias.

## `MauiClientConfiguration`

Datei: `VideoWebPlayer.Maui/Services/MauiClientConfiguration.cs` (statische Klasse)

- Client-Gate-Header `X-API-Key` (`ApiTokenHeaderName`).
- Tokenquellen in Reihenfolge: Umgebungsvariable `VIDEOWEBPLAYER_MAUI_API_TOKEN` → Assembly-Metadata `VideoWebPlayer.Maui.ApiToken` (aus MSBuild-Property `MauiClientApiToken`, s. csproj) → Platzhalter `<MAUI_CLIENT_API_TOKEN>`.
- `ApplyClientGateHeader(HttpClient)` fügt den Header hinzu, falls nicht vorhanden.
- Backend-Gegenstück laut Anforderung/README: `Jwt:ApiToken:Maui`.

## DTOs (`VideoWebPlayer.Client/Models/`)

### `AuthenticationRequest` — `AuthenticationRequest.cs`
| Eigenschaft | Typ |
|-------------|-----|
| `Email` | `string` |
| `Password` | `string` |

### `AuthorizationToken` — `AuthorizationToken.cs`
| Eigenschaft | Typ |
|-------------|-----|
| `token` | `string` |
| `expires` | `DateTime` |

### `ContinueWatchingDto` — `ContinueWatchingDto.cs`
| Eigenschaft | Typ |
|-------------|-----|
| `MediaType` | `string` |
| `Entry` | `DtoMediaEntry` |
| `PositionSeconds` | `long` |
| `DurationSeconds` | `long?` |
| `Title` | `string` |
| `PosterPictureId` | `long?` |

### `ContinueWatchingMutationResult` — `ContinueWatchingMutationResult.cs`
`sealed record` mit `Status` und `Message`.

### `MediaEntryDto` — `MediaEntryDto.cs`
Achtung: Namespace ist `VideoWebPlayer.Controllers.Models` (Server-Namespace im Client-Projekt).
| Eigenschaft | Typ |
|-------------|-----|
| `Type`, `Title` | `string` |
| `Id` | `long` |
| `Description`, `Url` | `string?` |
| `CreatedAt` | `DateTime` |
| `PictureId` | `long?` |
| `ItemCount` | `int` |

### Medien-Hierarchie — `DtoMovie.cs`
`DtoMediaEntry` ist polymorph per `[JsonPolymorphic]`/`[JsonDerivedType]` mit Diskriminator `$type` (Werte: `mediaEntry`, `movie`, `movieCollection`, `show`, `season`, `episode`).

`DtoMediaEntry`: `Id`, `Name`, `MediaSourceId`, `ReleaseDate?`, `PremieredAt?`, `EndedAt?`, `PosterPictureId?`, `BannerPictureId?`, `FanartPictureId?`, `PosterPicture?`, `BannerPicture?`, `FanartPicture?` (`DtoPicture`), `IsFavorite`, `IsManuallyEdited`.

Abgeleitet:
- `DtoMovie`: `GenreNames?`, `Plot?`, `Collection?` (`DtoMovieCollection`)
- `DtoMovieCollection`: `Movies` (`DtoMovie[]`, `[IgnoreAssignProperty]`)
- `DtoTVShow`: `Seasons` (`DtoTVShowSeason[]`), `GenreNames?`, `Plot?`
- `DtoTVShowSeason`: `Number`, `Show?`, `Episodes` (`DtoTVShowEpisode[]`)
- `DtoTVShowEpisode`: `Season?`, `Number`, `Plot?`, `GeneratedBackgroundPictureId?`
- `DtoPicture`: `Id`, `MediaItemId`, `Type`, `Width?`, `Height?`, `Description?`, `Data` (`byte[]`), `ContentType`

Weitere Typen in derselben Datei: `DtoGenreOption` (`Id`, `Name`), `MediaMetadataUpdateRequest` (`ObjectType`, `Id`, `Name`, `ReleaseDate?`, `PremieredAt?`, `Plot?`, `GenreNames`), Marker-Attribut `IgnoreAssignPropertyAttribute`.

### `DtoRecentEntry` + `RecentEntryType` — `DtoRecentEntry.cs`
`DtoRecentEntry`: `Id`, `MediaSourceId`, `PublishedAt`, `CreatedAt?`, `Type` (`RecentEntryType`), `Entry` (`DtoMediaEntry`).
`RecentEntryType`: `Movie`, `MovieCollection`, `TVShow`, `TVShowSeason`, `TVShowEpisode` (Enum in `VideoWebPlayer.Client/Models`).

### Quellen-DTOs — `DtoSource.cs`
- `DtoMediaSource : DtoMediaEntry` (blendet `Id` und `Name` ohne `new` aus — Compiler-Warnung CS0108): `Id`, `Name`, `CreatedAt`, `ClassifiedAt?`, `Changed`, `LastScannedAt?`, `IconPictureId?`
- `SourceGenresDto`: `SourceId`, `SourceName`, `Genres` (`List<GenreDto>`)
- `GenreDto`: `Id`, `Name`, `IconUrl?`

### `DtoFavoriteEntry` — `FavoriteEntry.cs`
Achtung: Namespace ist `VideoWebPlayer.Data` (Server-Namespace im Client-Projekt).
| Eigenschaft | Typ |
|-------------|-----|
| `Id` | `long` |
| `Entry` | `DtoMediaEntry` |
| `CreatedAt` | `DateTime` |

### `ImpersonateRequest` — `ImpersonateRequest.cs`
`UserId` (`string`). Wird im Bestandscode nicht aufgerufen (kein Referenzpunkt gefunden).

## Nicht vorhanden (für die Anforderung relevant)

- Keine definierte Retry-Strategie für fehlerhafte Serverantworten (nur 401-Single-Retry).
- Kein Offline-Outbox/Persistenz für Fortschrittsmeldungen im Client.
- Keine Abhängigkeit auf ein MVU-Framework o. ä. — der Client ist plattformneutral.
