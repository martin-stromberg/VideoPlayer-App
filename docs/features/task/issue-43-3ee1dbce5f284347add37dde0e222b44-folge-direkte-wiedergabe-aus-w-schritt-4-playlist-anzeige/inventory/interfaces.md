# Interfaces — Bestandsaufnahme

## `IAppEffectHandler` (`VideoPlayer.Mvu/IAppEffectHandler.cs`)

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `HandleAsync` | `AppEffect effect`, `CancellationToken` | `Task` | Führt einen MVU-Effekt am App-Rand aus (Implementierung: `ShellAppEffectHandler`) |

## `IPlaylistApiClient` (Referenz: `VideoWebPlayer.Client/IPlaylistApiClient.cs`)

Vollständige Playlist-API des Referenz-Clients — für diesen Schritt relevant nur die Anzeige-Methoden (Verwaltung/Wiedergabe = Folgeschritte):

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `RequestPlaylistsAsync` | `long? genreId = null` | `Task<IEnumerable<DtoPlaylist>>` | Eigene Playlists (`GET api/playlists`) |
| `RequestPublicPlaylistsAsync` | `long? genreId = null` | `Task<IEnumerable<DtoPlaylist>>` | Öffentliche Playlists (`GET api/playlists/public`) |
| `RequestPlaylistAsync` | `long playlistId` | `Task<DtoPlaylist?>` | Einzelne Playlist (`GET api/playlists/{id}`, `null` bei 404) |
| `RequestPlaylistEntriesPagedAsync` | `long playlistId`, `int pageNumber` (1-basiert), `int pageSize`, `CancellationToken` | `Task<DtoPlaylistEntriesPagedResult>` | Paginierte Einträge (`GET api/playlists/{id}/entries/paged`) |
| (Cover) | — | — | Cover-Abruf läuft im Referenz-Client als direkter Bild-GET (`/api/playlists/{id}/cover` → `FileResult`); kein eigenes Interface-Mitglied — siehe `PlaylistsController.cs` Z. 673 |

## `DevApiModelSnapshot` (`VideoPlayer.Mvu/DevApi/DevApiModelSnapshot.cs`, nur `DEBUG || DEV_API`)

`FromModel(AppModel)` → serialisierbarer Auszug für `GET /model`; neue Playlist-Kurzfelder müssen hier **und** im E2E-Spiegel `DevApiModel` (`VideoPlayer.E2E.Tests/Support/DevApiModels.cs`) ergänzt werden. Vertragstest: `DevApiContractTests`.

## `AuthSessionService` (`VideoPlayer.Mvu/AuthSessionService.cs`)

`Client` (Typ `VideoPlayerApiClient?`) — Einstiegspunkt aller Server-Services; `null` = nicht angemeldet/offline.

## `IAppEffectHandler`-Test-Stubs (`VideoWebPlayer.Maui.Tests/Stubs/`)

Vorhandene Stub-Infrastruktur der Update-Tests (z. B. Effekt-Sammler für `AppTransition.Effects`-Assertions; Muster in `AppUpdateTests_*`).
