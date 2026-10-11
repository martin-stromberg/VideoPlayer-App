# Inventar — Server-Schnittstellen

Quelle: `martin-stromberg/VideoPlayer` — `VideoWebPlayerClient.Playlists.cs`,
`PlaylistsController.cs`, `PlaylistService.cs`, `PlaylistEntryReorderService.cs`.

## Verwaltungs-Endpunkte

| Methode | Route | Request | Erfolg | Konflikte/Fehler |
|---|---|---|---|---|
| Erstellen | `POST api/playlists` | `{Name, Description?, SortMode}` | `DtoPlaylist` | — |
| Bearbeiten | `PUT api/playlists/{id}` | `{Name, Description}` | `DtoPlaylist` | 403 fremde, 404 |
| Löschen | `DELETE api/playlists/{id}` | — | 200/204 | 403 fremde |
| Eintrag hinzufügen | `POST api/playlists/{id}/entries` | `{MediaType, MediaId}` | `DtoPlaylistAddResult{AddedCount, SkippedCount, Message?}` | 403 fremde; `MediaType` per `Enum.TryParse` (ignoreCase) |
| Eintrag entfernen | `DELETE api/playlists/{id}/entries/{mediaType}/{mediaId}[?confirmContinueWatchingRemoval=true]` | — | 200/204 | 409 `{IsContinueWatchingConfirmationRequired}` bei Weiterschauen-Bezug |
| An Anfang | `POST api/playlists/{id}/entries/{entryId}/move-to-beginning` | — | `DtoPlaylistEntry` | 400 nicht manuell, 403 fremde |
| Zwischen Positionen | `POST api/playlists/{id}/entries/{entryId}/move-between` | `{NewSortOrder}` | 200/`DtoPlaylistEntry` | 400 nicht manuell/ungültig |
| Direkte SortOrder | `PUT api/playlists/{id}/entries/{entryId}/order` | `{NewSortOrder}` | 200 | Kollisionen NICHT geprüft — „Ans Ende" = max+1 |
| Max-SortOrder | `GET api/playlists/{id}/entries/max-sort-order` | — | `{MaxSortOrder}` (long?) | — |
| Batch-Reorder | `POST api/playlists/{id}/entries/batch-reorder` | `{EntryIds}` | `DtoPlaylistEntry[]` | (nicht benötigt — kein UI-Fluss) |
| Sortiermodus | `PATCH api/playlists/{id}/sort-mode` | `{NewSortMode, ConfirmLossOfManualOrder}` | `DtoPlaylist` | 409 `{IsLossOfDataConfirmationRequired}` beim Verlassen von `Manual` |
| Genres setzen | `PUT api/playlists/{id}/genres` | `{GenreIds: long[]}` | `DtoPlaylist` | 403 fremde |
| Genres zurücksetzen | `POST api/playlists/{id}/genres/reset` | — | `DtoPlaylist` | nur bei `GenresManuallyOverridden` sinnvoll |
| Öffentlich-Flag | `PUT api/playlists/{id}/public` | `{IsPublic}` | `DtoPlaylist` | 403 ohne Admin |
| Genre-Optionen | `GET api/items/genres` | — | `GenreOption[]` | vorhanden: `GetGenreOptionsAsync` |
| Eigene Playlists | `GET api/playlists` | — | `DtoPlaylist[]` | vorhanden |
| Öffentliche | `GET api/playlists/public` | — | `DtoPlaylist[]` | vorhanden |
| Kopfdaten | `GET api/playlists/{id}` | — | `DtoPlaylist` (404 → null) | vorhanden |
| Einträge | `GET api/playlists/{id}/entries/paged?pageNumber&pageSize` | — | `DtoPlaylistEntriesPagedResult` | vorhanden |

## Serialisierung

- Client `JsonOptions`: `PropertyNameCaseInsensitive`; Schreiben mit
  C#-Property-Namen (PascalCase) — kompatibel mit der serverseitigen
  case-insensitiven Deserialisierung.
- `SendWithPolicyAsync`: Wiederholung nur bei 5xx/408/429 + einmalige
  Token-Erneuerung bei 401; 400/403/404/409 → `ApiException` (`StatusCode`,
  `ResponseBody`).

## App-interne Schnittstellen

- `PlaylistService` (Mvu): bestehende Lademethoden + neue Verwaltungsmethoden;
  409-Konflikte als Ergebnis-Records (`RequiresConfirmation`-Flag) statt Wurf.
- `ProfileInfoService.ReadTokenClaims` → Erweiterung um `IsAdmin`-Lesung
  (dasselbe Payload-Dekodier-Muster).
