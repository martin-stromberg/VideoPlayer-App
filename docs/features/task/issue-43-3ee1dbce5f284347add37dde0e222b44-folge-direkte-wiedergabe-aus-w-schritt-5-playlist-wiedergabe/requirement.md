# Übersetzte Anforderung: Playlist-Wiedergabe und Folgevideo-Auflösung (inkl. Weiterschauen mit Playlist-Bezug)

## Fachliche Zusammenfassung

Die Wiedergabe von Playlist-Einträgen wird in der MAUI-App angebunden — sowohl aus der `PlaylistDetailsPage` (Start ab gewähltem Eintrag oder erstem abspielbarem Eintrag) als auch aus der Weiterschauen-Liste, wenn der Weiterschauen-Eintrag einen Playlist-Bezug trägt. Die Folgevideo-Ermittlung (manuelle Vor-/Zurück-Navigation, Auto-Fortsetzung am Ende, Fehler-Rückfall) delegiert bei Playlist-Kontext an die serverseitigen Play-Endpunkte (`POST api/playlists/{id}/play`, `.../play/next`, `.../play/previous`, `.../play/advance`), die die Playlist-Reihenfolge inkl. Überspringen nicht abspielbarer/nicht zugänglicher Einträge serverseitig korrekt auflösen. Weiterschauen-Einträge ohne Playlist-Bezug behalten die bestehende, detailseiten-unabhängige Serienkontext-Auflösung via `UpcomingEpisodesService`.

## Server-Referenz (verifiziert gegen `martin-stromberg/VideoPlayer`)

| Endpunkt | Signatur | Ergebnis |
|----------|----------|----------|
| `POST api/playlists/{id}/play` | Query `entryId` (`long?`, optional) | `DtoPlaylistPlaybackStart`: `PlaylistId`, `PlaylistName`, `TotalCount`, `CurrentPosition` (1-basiert), `CurrentEntryId`, `CurrentEntry` (`DtoPlaylistEntry`), `StreamUrl` (relativ, ohne Token), `MediaType`, `MediaId`, `StartPositionSeconds` |
| `POST api/playlists/{id}/play/next` | Query `currentEntryId` (`long`, Pflicht) | `DtoPlaylistNavigationResult` (`Entry`, `Position`, `StartPositionSeconds`) oder **204 No Content**, wenn kein Folgeeintrag |
| `POST api/playlists/{id}/play/previous` | Query `currentEntryId` (`long`, Pflicht) | wie `next` |
| `POST api/playlists/{id}/play/advance` | Query `currentEntryId` (`long`, Pflicht) | wie `next` — für Auto-Fortsetzung |

`ContinueWatchingDto` (Server) liefert bereits `PlaylistId` (`long?`), `PlaylistName` (`string?`), `PlaylistEntryId` (`long?`) sowie `Id`, `WatchedAt` — die App-seitige `ContinueWatchingEntry`-DTO besitzt diese Felder noch nicht.

## Betroffene Klassen und Komponenten

- **Datenmodell (ApiClient):** `ContinueWatchingEntry` um `Id`, `WatchedAt`, `PlaylistId`, `PlaylistName`, `PlaylistEntryId` ergänzen (Feldnamen am Server-DTO verifiziert); neue DTOs `PlaylistPlaybackStart`, `PlaylistNavigationResult` (Pendant zu `DtoPlaylistPlaybackStart`/`DtoPlaylistNavigationResult`).
- **API-Client:** `VideoPlayerApiClient` um `StartPlaylistPlaybackAsync`/`GetNextPlaylistEntryAsync`/`GetPreviousPlaylistEntryAsync`/`AdvancePlaylistAsync` erweitern (Muster der bestehenden `GetPlaylistEntriesPagedAsync` u. a. folgen).
- **MVU-Modell:** `HomeMediaItem` und `PlayerState` um Playlist-Kontext (`PlaylistId`, `PlaylistEntryId`, ggf. `PlaylistName`) erweitern; `PlayerState` benötigt Unterscheidung „Playlist-Kontext" vs. „Serienkontext" für Folgevideo-Quelle.
- **MVU-Nachrichten/Effekte:** neue `AppMessage`-/`AppEffect`-Records für Playlist-Wiedergabestart und Playlist-Navigation (deutsch, XML-Doku — CS1591 als Fehler); `AppUpdate.Update`-Switch und `ShellAppEffectHandler`/`IAppEffectHandler` erweitern; `EnumCoverageTests` ggf. betroffen.
- **Services:** `PlaylistService` um Wiedergabe-/Navigationsmethoden erweitern oder dedizierter Dienst; `ContinueWatching`→`HomeMediaItem`-Mapping (`HomeListService` o. ä.) um Playlist-Bezug erweitern.
- **UI:** `PlaylistDetailsPage` — Wiedergabe auslösen (Eintrag antippen = ab diesem Eintrag; „Abspielen" = erster abspielbarer Eintrag); `PlayerPage` — Vor/Zurück-Steuerung im Playlist-Kontext (prüfen, was für Episoden bereits existiert).
- **Tests:** Unit-Tests für Update-Logik und Mapping; `EnumCoverageTests`; E2E via `MockVideoServer` (Playlist-Play-Endpunkte nachrüsten) + DevApi.

## Implementierungsansatz

- Playlist-Wiedergabe nutzt denselben Player-Pfad (`PlayRequestedMessage` → `NavigateToPlayerEffect` + Quellenauflösung → `PlayerPage`), trägt aber den Playlist-Kontext (`PlaylistId`, aktuelle `EntryId`, Position) in den `PlayerState`.
- Die Stream-URL kommt bei Playlist-Start direkt aus `DtoPlaylistPlaybackStart.StreamUrl` (relativ, App hängt eigenes Token an) — statt der bisherigen `ResolvePlaybackSourceEffect`-Auflösung kann der Start die Quelle direkt setzen; für Folgeeinträge liefert `DtoPlaylistNavigationResult.Entry` die Medienreferenz (`MediaType`/`MediaId`), deren Quelle wie bisher aufgelöst wird (oder ebenfalls Server-Stream-URL — an Server-Antwort prüfen: `DtoPlaylistNavigationResult` enthält **keine** StreamUrl → Quellenauflösung via `PlaybackSourceService` mit `MediaType`/`MediaId` + `StartPositionSeconds` aus dem Ergebnis).
- Auto-Fortsetzung/Fehler-Rückfall: bei `PlayerState` mit Playlist-Kontext `POST .../play/advance` bzw. `.../play/next` statt `UpcomingEpisodes`; bei `204` → normales Serien-Ende-Verhalten (kein Folgeeintrag).
- Weiterschauen-Pfad: `ContinueWatchingEntry.PlaylistId`/`PlaylistEntryId` → `HomeMediaItem` → `ContinueWatchingItemSelectedMessage`/`StartPlayback` → bei vorhandenem `PlaylistId` statt Serienkontext direkt `POST .../play` mit `entryId = PlaylistEntryId` (Server liefert `StreamUrl`, `StartPositionSeconds`, `CurrentEntryId`).

## Konfiguration

Keine Konfiguration erforderlich.

## Akzeptanzkriterien

- Aus der Playlist-Detailansicht lässt sich die Wiedergabe starten (ab gewähltem Eintrag bzw. erstem abspielbarem Eintrag).
- Innerhalb einer laufenden Playlist-Wiedergabe stehen Vor- und Rückwärts-Navigation zur Verfügung; Auto-Fortsetzung am Ende bzw. Fehler-Rückfall setzen auf den nächsten Playlist-Eintrag (Playlist-Reihenfolge).
- Wird ein Weiterschauen-Eintrag mit Playlist-Bezug abgespielt, wird das richtige Folgevideo gemäß Playlist-Reihenfolge bereitgestellt (nicht die nächste Serienepisode).
- Weiterschauen-Einträge ohne Playlist-Bezug verhalten sich wie bisher (Folgevideo aus Serienreihenfolge über bestehende Serienkontext-Auflösung).

## Offene Fragen

- Ob `PlayerPage` bereits Vor/Zurück-Steuerung für Episoden besitzt (aus Bestandsaufnahme zu klären) — falls nicht, ist sie im Zuge der Playlist-Navigation mit aufzubauen, da die Anforderung Vor-/Rückwärts-Navigation fordert.
- Ob `ContinueWatchingMutationService`/Offline-Pfad (`OfflineContinueWatchingService`) mit den neuen DTO-Feldern umgehen muss (Mapping-Vollständigkeit).
