← [Zurück zur Übersicht](index.md)

# Server-API-Client — API

## Übersicht

`VideoPlayer.ApiClient` ist eine `net10.0`-Klassenbibliothek. Einstiegspunkt ist `VideoPlayerApiClient`, der über einen `HttpClient` und `VideoPlayerApiClientOptions` konfiguriert wird.

```csharp
var client = new VideoPlayerApiClient(
    new HttpClient { BaseAddress = new Uri("http://server/") },
    new VideoPlayerApiClientOptions());
```

## Authentifizierung

Zweistufig: Client-Gate per `X-API-Key`-Header (auf jeder Anfrage) plus Bearer-Token nach `LoginAsync` bzw. `SetAccessToken`. Bei 401 erfolgt eine einmalige Erneuerung über `VideoPlayerApiClientOptions.TokenRefresher`.

## Rate Limiting / Wiederholungen

Der Server kann `429 Too Many Requests` liefern; der Client wiederholt dann wie bei Serverfehlern (5xx) und `408` bis zu dreimal mit Warteintervallen 1 s, 2 s, 4 s. Übrige 4xx-Fehler werden nicht wiederholt.

## Endpunkte / Methoden

### `LoginAsync(email, password)` / `POST api/auth/login`

**Beschreibung:** Anmeldung; setzt das erhaltene Token für Folgeanfragen. Keine 401-Erneuerung auf diesem Aufruf.

**Rückgabe:** `AuthToken` (`Token`, `Expires`).

**Fehler:** `ApiException`, `ArgumentException` bei leeren Zugangsdaten.

### `CheckHealthAsync()` / `GET api/health`

**Rückgabe:** `bool` — `true` bei Erfolgsantwort, sonst `false`.

### `GetSourcesAsync()` / `GET api/Sources` · `GetSourceAsync(id)` / `GET api/Sources/{id}` · `GetSourceGenresAsync(id)` / `GET api/SourceGenres/{id}`

**Rückgabe:** `IReadOnlyList<MediaSource>` bzw. `MediaSource` / `SourceGenres`.

### `GetRecentEntriesAsync()` / `GET api/items/recent`

**Rückgabe:** `IReadOnlyList<RecentEntry>` — zuletzt hinzugefügte Inhalte mit polymorphem `Entry`.

### `GetContinueWatchingAsync()` / `GET api/continue-watching`

**Rückgabe:** `IReadOnlyList<ContinueWatchingEntry>`.

### `ReportPlaybackProgressAsync(mediaType, mediaId, positionSeconds, durationSeconds)` / `POST api/continue-watching/progress`

**Beschreibung:** Meldet den Wiedergabefortschritt (Body: `PlaybackProgressReport`).

### `HideContinueWatchingEntryAsync(mediaType, mediaId)` / `POST api/continue-watching/hide` · `SkipContinueWatchingEntryAsync(mediaType, mediaId)` / `POST api/continue-watching/skip`

**Rückgabe:** `ContinueWatchingMutationResult` (`Status`, `Message`).

### `GetFavoritesAsync()` / `GET api/favorites` · `ToggleFavoriteAsync(entry)` / `POST api/favorites/toggle` · `RemoveFavoriteAsync(favoriteId)` / `POST api/favorites/remove`

**Rückgabe:** `IReadOnlyList<FavoriteEntry>` bzw. `bool` / `Task`.

### `GetSourceItemsAsync(mediaSourceId, page, pageSize, searchText, genreId)` / `GET api/items`

**Parameter:**

| Name | Typ | Pflicht | Beschreibung |
|------|-----|---------|--------------|
| `mediaSourceId` | `long` | Ja | Quellen-Id |
| `page` | `int` | Nein (0) | Seitenindex |
| `pageSize` | `int` | Nein (30) | Seitengröße |
| `searchText` | `string?` | Nein | Textsuche |
| `genreId` | `long?` | Nein | Genre-Filter |

**Rückgabe:** `IReadOnlyList<MediaItemSummary>`.

### `GetMovieAsync(id)` / `GET api/items/movie/{id}` · `GetMovieCollectionAsync(id)` / `GET api/items/moviecollection/{id}` · `GetTvShowAsync(id)` / `GET api/items/tvshow/{id}` · `GetTvShowEpisodeAsync(id)` / `GET api/items/tvshowepisode/{id}` · `GetGenreOptionsAsync()` / `GET api/items/genres`

**Rückgabe:** `Movie`, `MovieCollection`, `TvShow` (inkl. `Seasons`/`Episodes`), `TvShowEpisode`, `IReadOnlyList<GenreOption>`.

### `GetPictureAsync(pictureId)` / `GET api/pictures/{id}` · `GetSourceIconAsync(pictureId)` / `GET api/sourceicons/{id}`

**Rückgabe:** `byte[]`.

## Fehler

| Code / Exception | Ursache |
|------------------|---------|
| `ApiException` (`StatusCode` = 401) | Anmeldung abgelaufen/fehlgeschlagen, Token-Erneuerung ohne Erfolg |
| `ApiException` (`StatusCode` = 4xx/5xx) | Serverfehler nach Ausschöpfung der Wiederholungen bzw. eindeutiger Clientfehler |
| `ApiException` (`StatusCode` = `null`) | Transportfehler nach 4 Sendevorgängen |
| `InvalidOperationException` | Fehlende Basisadresse oder nicht deserialisierbare Antwort |
| `ArgumentOutOfRangeException` / `ArgumentException` | Ungültige Parameter (Ids ≤ 0, leere Strings) |
