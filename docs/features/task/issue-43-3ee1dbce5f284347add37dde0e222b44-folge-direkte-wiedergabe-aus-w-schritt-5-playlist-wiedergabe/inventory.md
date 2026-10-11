# Bestandsaufnahme: Playlist-Wiedergabe und Folgevideo-Auflösung

Erstellt: 2026-10-11 17:19 +02:00
Branch: `task/issue-43-3ee1dbce5f284347add37dde0e222b44-folge-direkte-wiedergabe-aus-w-schritt-5-playlist-wiedergabe`
Commit: `6e160623bed1d66c9e7faac60db076fb1aa63bd4`
Basisbranch: `task/issue-43-3ee1dbce5f284347add37dde0e222b44-folge-direkte-wiedergabe-aus-w`
Uncommittete Änderungen: keine (nur das neue Feature-Artefakt-Verzeichnis, untracked)
SDK: .NET 10.0.401

## Detaildokumente

- [inventory/server-api.md](inventory/server-api.md) — Serverseitige Playlist-Play-Endpunkte und DTOs (Referenz: `martin-stromberg/VideoPlayer`)
- [inventory/wiedergabepfad.md](inventory/wiedergabepfad.md) — Bestehender Wiedergabepfad (MVU): Start, Quellauflösung, Folgevideo, Fehler-Rückfall
- [inventory/playlists-anzeige.md](inventory/playlists-anzeige.md) — Bestehender Playlist-Anzeige-Pfad aus Schritt 4
- [inventory/weiterschauen.md](inventory/weiterschauen.md) — Weiterschauen-Datenpfad und Playlist-Bezug
- [inventory/tests.md](inventory/tests.md) — Testinfrastruktur, Ausgangszustand, E2E-Muster

## Kurzfazit

Der komplette MVU-Wiedergabepfad existiert (`PlayRequestedMessage`/`ContinueWatchingItemSelectedMessage` → `StartPlayback` → `NavigateToPlayerEffect` + `ResolvePlaybackSourceEffect` → `PlaybackSourceResolvedMessage` → `PlayerPage`/MediaElement). Folgevideo-Logik läuft über `PlayerState.UpcomingEpisodes` (Serienkontext aus Detailseite oder `LoadUpcomingEpisodesEffect`/`UpcomingEpisodesService`). Playlist-Anzeige ist vollständig angebunden (`PlaylistService`, `PlaylistsPage`, `PlaylistDetailsPage`, `api/playlists`-Leseendpunkte im `VideoPlayerApiClient` und `MockVideoServer`).

**Fehlend für diese Anforderung:**

1. `ContinueWatchingEntry`-DTO hat keine Playlist-Felder (`PlaylistId`, `PlaylistName`, `PlaylistEntryId` — Server liefert sie seit dem Referenzstand in `ContinueWatchingDto`).
2. `HomeMediaItem` trägt keinen Playlist-Bezug → `UpdateContinueWatchingItemSelected` kann den Playlist-Pfad nicht wählen.
3. `VideoPlayerApiClient` kennt die Play-Endpunkte nicht (`POST api/playlists/{id}/play`, `.../play/next`, `.../play/previous`, `.../play/advance`).
4. `PlayerState` hat keinen Playlist-Kontext → `AdvanceToNextEpisode` (Auto-Fortsetzung + Fehler-Rückfall) kennt nur die Serienliste.
5. Keine Vor-/Zurück-Navigation im Player (weder für Episoden noch Playlists) — muss neu gebaut werden; die Anforderung fordert sie für den Playlist-Kontext.
6. `PlaylistDetailsPage` kann keine Wiedergabe auslösen (Eintragsauswahl öffnet bisher die Medien-Detailseite — explizit als Übergangslösung kommentiert: „Wiedergabe gehört zum Folgeschritt").
7. `MockVideoServer` bedient `api/playlists` nur lesend (GET) — die POST-Play-Endpunkte fehlen; Stream-Endpunkte sind an Opt-in-Ids (`PlaybackMovieId`, `PlaybackEpisodeId`/`PlaybackNextEpisodeId`) gebunden.
8. `DevApiModelSnapshot` exponiert keine Playlist-Wiedergabe-Felder (für E2E-Nachweise ggf. `PlayerPlaylistId` o. ä. ergänzen).

## Test-Ausgangszustand (Kurzfassung)

- `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj --no-build -c Release` → **1139/1139 bestanden** (Details: [inventory/tests.md](inventory/tests.md))
- E2E-Suite `VideoPlayer.E2E.Tests` — Baseline-Lauf gestartet, Ergebnis wird in `inventory/tests.md` nachgetragen.
