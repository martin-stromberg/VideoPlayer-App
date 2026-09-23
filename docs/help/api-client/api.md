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

Zweistufig: Client-Gate per `X-API-Key`-Header (auf jeder Anfrage) plus Bearer-Token nach `LoginAsync` bzw. `SetAccessToken`. Bei 401 erfolgt eine einmalige Erneuerung über `VideoPlayerApiClientOptions.TokenRefresher`. Als Gate-Schlüssel dient vorrangig ein gepaartes Geräte-Token (`ClientGate.DeviceTokenProvider`, gesetzt nach dem Pairing), sonst die statischen Quellen (`ClientGate.ResolveStatic()`); `SetApiKey` ersetzt den Schlüssel eines bestehenden Clients zur Laufzeit (z. B. direkt nach einem Pairing).

## Rate Limiting / Wiederholungen

Der Server kann `429 Too Many Requests` liefern; der Client wiederholt dann wie bei Serverfehlern (5xx) und `408` bis zu dreimal mit Warteintervallen 1 s, 2 s, 4 s. Übrige 4xx-Fehler werden nicht wiederholt.

## Endpunkte / Methoden

### `LoginAsync(email, password)` / `POST api/auth/login`

**Beschreibung:** Anmeldung; setzt das erhaltene Token für Folgeanfragen. Keine 401-Erneuerung auf diesem Aufruf.

**Rückgabe:** `AuthToken` (`Token`, `Expires`).

**Fehler:** `ApiException`, `ArgumentException` bei leeren Zugangsdaten.

### `CheckHealthAsync()` / `GET api/health`

**Rückgabe:** `HealthCheckResult` — `Reachable` bei Erfolgsantwort, `GateRejected` bei HTTP 401 (das Client-Gate lehnt den Schlüssel ab, z. B. ein widerrufenes Geräte-Token), `Unreachable` bei sonstigem Fehlschlag oder Nichterreichbarkeit.

### `SetApiKey(apiKey)`

**Beschreibung:** Ersetzt den `X-API-Key`-Header des Clients für Folgeanfragen — etwa nach einem erfolgreichen Geräte-Pairing oder dem Rückfall auf einen statischen Schlüssel.

**Fehler:** `ArgumentException` bei leerem Schlüssel.

### `DevicePairingClient.ExchangeAsync(baseAddress, code, deviceName)` / `POST api/pairing/exchange`

**Beschreibung:** Öffentlicher Pairing-Endpunkt — tauscht einen serverseitig erzeugten Einmal-Code (8 Zeichen, Alphabet `ABCDEFGHJKMNPQRSTUVWXYZ23456789`, TTL serverseitig ca. 5 Minuten) gegen ein gerätegebundenes Token. Der Client erzeugt ein flüchtiges ECDH-P-256-Schlüsselpaar, sendet den öffentlichen Schlüssel (SPKI/DER, Base64) als `clientPublicKey` samt `code` und optionalem `deviceName` und entschlüsselt `encryptedToken` aus der Antwort (`nonce(12) ‖ ciphertext ‖ tag(16)`, AES-256-GCM, Schlüssel per SHA-256-HMAC-ECDH abgeleitet). Der Aufruf sendet bewusst **keinen** `X-API-Key`- und **keinen** Bearer-Header und läuft ohne Wiederholungen.

**Rückgabe:** `string` — das entschlüsselte Geräte-Token (anschließend als `X-API-Key` zu verwenden und sicher zu hinterlegen).

**Fehler:** `ApiException` mit `StatusCode` 400 (ungültige Anfrage), 401 (Code ungültig/abgelaufen/verbraucht), 404 (Server ohne Pairing-Endpunkt), 429 (Ratenlimit/IP-Sperre), `null` (Transportfehler); `CryptographicException` bei ungültiger/nicht entschlüsselbarer Antwort.

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
