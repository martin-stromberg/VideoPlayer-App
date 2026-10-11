# Server-API-Referenz: Playlist-Wiedergabe

Quelle: öffentliches Server-Repo `martin-stromberg/VideoPlayer` (via `gh api`), verifiziert gegen
`VideoWebPlayer/Controllers/PlaylistsController.cs`, `VideoWebPlayer.Client/IPlaylistApiClient.cs`
und `VideoWebPlayer.Client/Models/*.cs`. Laufende Instanz `http://localhost:83/` antwortet ohne
Credentials mit 401 (Existenzprüfung bestanden).

## Endpunkte

| Endpunkt | HTTP | Parameter | Erfolg | Kein Folgeeintrag |
|----------|------|-----------|--------|-------------------|
| `api/playlists/{id}/play` | POST | Query `entryId` (`long?`, optional; einfache Typen bindet der Controller aus der Query — kein `[FromBody]`) | 200 `DtoPlaylistPlaybackStart` | Fehler (Service-Ausnahme → Fehlerresultat des `ExecuteAsync`-Wrappers) |
| `api/playlists/{id}/play/next` | POST | Query `currentEntryId` (`long`, Pflicht) | 200 `DtoPlaylistNavigationResult` | **204 No Content** |
| `api/playlists/{id}/play/previous` | POST | Query `currentEntryId` (`long`, Pflicht) | 200 `DtoPlaylistNavigationResult` | **204 No Content** |
| `api/playlists/{id}/play/advance` | POST | Query `currentEntryId` (`long`, Pflicht) | 200 `DtoPlaylistNavigationResult` | **204 No Content** |

Semantik (aus den XML-Dokumenten von `IPlaylistApiClient`/`PlaylistsController`):

- `play`: Startet die Wiedergabe ab `entryId`, oder ab dem **ersten abspielbaren und zugänglichen** Eintrag bei `entryId = null`.
- `next`/`previous`: manuelle Vor-/Rückwärts-Navigation; überspringt nicht abspielbare/nicht zugängliche Einträge serverseitig.
- `advance`: automatisches Weiterschalten am Titelende (gleiche Auflösung wie `next`, eigener Endpunkt für die Semantik „Auto-Fortsetzung").
- Navigation liefert die tatsächliche 1-basierte Position im aktuellen Sortiermodus mit, damit der Client die korrekte Position anzeigen kann, auch wenn Einträge übersprungen wurden.

## DTOs

### `DtoPlaylistPlaybackStart` (Antwort von `POST .../play`)

| Feld | Typ | Bedeutung |
|------|-----|-----------|
| `playlistId` | `long` | Id der Playlist |
| `playlistName` | `string` | Name der Playlist |
| `totalCount` | `int` | Gesamtzahl (nicht-verwaister) Einträge |
| `currentPosition` | `int` | 1-basierte Position des Starteintrags im aktuellen Sortiermodus |
| `currentEntryId` | `long` | Id des aufgelösten Starteintrags (`PlaylistEntry.Id`) |
| `currentEntry` | `DtoPlaylistEntry` | Der aufgelöste Starteintrag |
| `streamUrl` | `string` | Relative Stream-URL des Mediums **ohne** `access_token` (Client hängt eigenes Token an) |
| `mediaType` | `string` | Medientyp in VideoPlayer-Konvention (z. B. `movie`, `episode`) |
| `mediaId` | `long` | Id des Mediums |
| `startPositionSeconds` | `long` | Fortsetzungsposition aus dem passenden `ContinueWatchingEntry` (0 ohne Stand) |

### `DtoPlaylistNavigationResult` (Antwort von `.../play/next|previous|advance`)

| Feld | Typ | Bedeutung |
|------|-----|-----------|
| `entry` | `DtoPlaylistEntry` | Aufgelöster Zieleintrag (enthält `id`, `mediaType`, `mediaId`, `mediaTitle`, `parentMedia*`, `isAccessible` u. a.) |
| `position` | `int` | 1-basierte Position des Eintrags im aktuellen Sortiermodus |
| `startPositionSeconds` | `long` | Fortsetzungsposition des Zieleintrags (0 ohne Stand) — wird synchron mit der Quelle angewendet |

Wichtig: `DtoPlaylistNavigationResult` enthält **keine** `streamUrl` — die Quelle des Folge-Mediums
muss clientseitig über `MediaType`/`MediaId` aufgelöst werden (`api/items/{typ}/{id}/stream`,
bestehender `PlaybackSourceService`-Pfad).

### `ContinueWatchingDto` (Server) — Playlist-Felder

| Feld | Typ | Bedeutung |
|------|-----|-----------|
| `id` | `long` | Datenbank-Id der Zeile (eindeutig auch bei mehrfachem Medium mit verschiedenen Playlist-Bezügen) |
| `playlistId` | `long?` | Id der Playlist, aus der der Eintrag entstand; `null` ohne Playlist-Bezug |
| `playlistName` | `string?` | Name der Playlist; `null` wenn `playlistId` `null` |
| `playlistEntryId` | `long?` | Id des `PlaylistEntry`, dem das Medium aktuell entspricht; `null` wenn `playlistId` `null` **oder das Medium nicht mehr Teil der Playlist ist** |
| `watchedAt` | `DateTime?` | Zeitpunkt des letzten Schauens |

Die App-seitige `ContinueWatchingEntry` (`VideoPlayer.ApiClient/Models/ContinueWatching.cs`) besitzt
derzeit nur `MediaType`, `Entry`, `PositionSeconds`, `DurationSeconds`, `Title`, `PosterPictureId` —
die Playlist-Felder fehlen und sind zu ergänzen (Deserialisierung ist `PropertyNameCaseInsensitive`).

## Signatur-Details für den Client-Aufruf

- Die Play-Endpunkte erwarten **keinen Request-Body** — `entryId`/`currentEntryId` gehen als
  Query-Parameter mit (`POST api/playlists/12/play?entryId=34` bzw. `POST api/playlists/12/play/next?currentEntryId=34`).
- Auth wie üblich: Client-Gate-Header + Bearer-Token (identisch zu den übrigen `api/`-Aufrufen des
  `VideoPlayerApiClient`); die Play-Endpunkte laufen unter dem Standard-`CheckLogedIn()` des Controllers.
