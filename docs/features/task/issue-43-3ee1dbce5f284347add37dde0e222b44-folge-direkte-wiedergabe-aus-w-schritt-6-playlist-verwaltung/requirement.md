# Anforderung — Issue #43, Entwicklungsschritt 6: Playlist-Verwaltung in der App

## Quelle

Issue #43 „Folge: Direkte Wiedergabe aus Weiterschauen, Protokollversand und
Playlist-Support", Entwicklungsschritt 6 (Playlist-Verwaltung). Schritte 4
(Anzeige) und 5 (Wiedergabe) sind bereits umgesetzt; Schritt 6 ergänzt die
Verwaltung, soweit sie auf dem Gerät sinnvoll ist. Vergleichs-Referenz ist das
Server-Web-UI (`VideoWebPlayer/Components/Playlists/`); Request-/Response-
Formate stammen aus `IPlaylistApiClient.cs` und den `Dto*` des Server-Repositorys
(`martin-stromberg/VideoPlayer`).

## Zielbild

Die App soll neben der bestehenden Anzeige und Wiedergabe die serverseitige
Playlist-Verwaltung anbieten:

- **Playlists verwalten:** Erstellen, Bearbeiten (Name/Beschreibung) und
  Löschen eigener Playlists — auf der Übersichtsseite und der Detailseite.
- **Einträge verwalten:** Medien von der Medien-Detailseite zu einer eigenen
  Playlist hinzufügen (Auswahl der Playlist per Klartextname, niemals über
  interne Kennungen) und Einträge aus einer Playlist entfernen.
- **Weiterschauen-Konflikt (HTTP 409):** Entfernt der Benutzer einen Eintrag,
  der in der Weiterschauen-Liste referenziert ist, quittiert die App die
  Warnung mit einem Bestätigungsdialog und sendet bei Zustimmung die
  serverseitige Bestätigungsflagge erneut.
- **Reihenfolge:** Einträge manuell sortierbarer Playlists verschieben —
  mindestens „an den Anfang" und zwischen Positionen (analog Server-UI:
  an Anfang/Ende sowie positionelle Verschiebung).
- **Sortiermodus:** Manuell ↔ Nach Erscheinungsdatum umschalten, inklusive
  der serverseitigen Verlust-Bestätigung (HTTP 409 bei Wechsel weg von
  „Manuell").
- **Genres:** Anzeige-Genres einer eigenen Playlist pflegen (setzen und
  auf die berechneten Genres zurücksetzen); Auswahl über Genre-Namen aus
  `api/items/genres`.
- **Öffentlich-Flag:** Eigene Playlist öffentlich/privat schalten —
  wie in der Server-UI nur für Administrator:innen angeboten
  (JWT-Claim `IsAdmin`, Server prüft zusätzlich); fremde öffentliche
  Playlists bleiben strikt schreibgeschützt.

## Abgrenzung (explizit ausgeschlossen)

- **Playlist-Cover-Verwaltung** wird NICHT implementiert (kein Upload,
  keine Neuerzeugung, keine Vorschau/Löschung) — die Cover-Anzeige aus
  Schritt 4 bleibt wie sie ist.
- Kein Merge des Vergleichs-Branches
  `task/issue-43-3ee1dbce5f284347add37dde0e222b44-folge-direkte-wiedergabe-aus-w`.

## Server-Endpunkte (aus `IPlaylistApiClient.cs`/`PlaylistsController.cs`)

| Operation | Route | Request-Body | Besonderheiten |
|---|---|---|---|
| Erstellen | `POST api/playlists` | `DtoCreatePlaylistRequest{Name, Description?, SortMode}` | `SortMode` = `"Manual"`/`"ByReleaseDate"` |
| Bearbeiten | `PUT api/playlists/{id}` | `DtoUpdatePlaylistRequest{Name, Description}` | Nur Name/Beschreibung; nur eigene |
| Löschen | `DELETE api/playlists/{id}` | — | Bestätigungsdialog wie Server-UI |
| Eintrag hinzufügen | `POST api/playlists/{id}/entries` | `DtoAddMediaToPlaylistRequest{MediaType, MediaId}` | `MediaType` als CLR-Enum-Name (`Movie`, `TVShow`, `TVShowSeason`, `TVShowEpisode`, `MovieCollection`; Server parst `Enum.TryParse` case-insensitiv); Serie/Staffel/Kollektion kaskadiert serverseitig; Antwort `DtoPlaylistAddResult{Message?}` |
| Eintrag entfernen | `DELETE api/playlists/{id}/entries/{mediaType}/{mediaId}` | — | Query `confirmContinueWatchingRemoval=true` nach 409-`DtoRemovePlaylistEntryConflictResponse{IsContinueWatchingConfirmationRequired}` |
| An Anfang | `POST api/playlists/{id}/entries/{entryId}/move-to-beginning` | — | Nur `SortMode=Manual` |
| Ans Ende | `GET api/playlists/{id}/entries/max-sort-order` + `PUT api/playlists/{id}/entries/{entryId}/order` | `DtoReorderPlaylistEntryRequest{NewSortOrder}` | Server-UI-Pattern (kein `move-to-end`-Endpunkt) |
| Zwischen Positionen | `POST api/playlists/{id}/entries/{entryId}/move-between` | `DtoReorderPlaylistEntryRequest{NewSortOrder}` | `NewSortOrder` = `SortOrder` des Ziel-Nachbarn |
| Sortiermodus | `PATCH api/playlists/{id}/sort-mode` | `DtoChangeSortModeRequest{NewSortMode, ConfirmLossOfManualOrder}` | 409 → `DtoChangeSortModeConflictResponse` → Bestätigung + Retry |
| Genres setzen | `PUT api/playlists/{id}/genres` | `DtoSetPlaylistGenresRequest{GenreIds}` | Vorauswahl = `AllGenreIds` der Playlist |
| Genres zurücksetzen | `POST api/playlists/{id}/genres/reset` | — | Nur bei `GenresManuallyOverridden` |
| Öffentlich-Flag | `PUT api/playlists/{id}/public` | `DtoSetPlaylistPublicRequest{IsPublic}` | Nur Administrator:innen (JWT-Claim `IsAdmin`); 403 sonst |
| Genre-Optionen | `GET api/items/genres` | — | `GenreOption{Id, Name}` (vorhanden: `GetGenresAsync`) |
| Eigene Playlists | `GET api/playlists` | — | `DtoPlaylist{Id, Name, Description, SortMode, Genres, AllGenreIds, GenresManuallyOverridden, CoverPictureId, IsPublic, IsOwner}` (vorhanden) |

## Übersetzung in Projekt-Begriffe

- **MVU-Fluss:** neue deutsche `AppMessage`-Records (Anfragen, Ergebnisse,
  Konflikte, Dialog-Bestätigungen), `AppEffect`-Records und Zweige in
  `AppUpdate.Update`; Serverzugriffe in `ShellAppEffectHandler`.
- **Service-Schicht:** `PlaylistService` (VideoPlayer.Mvu) erhält
  Verwaltungsmethoden; `VideoPlayerApiClient` (VideoPlayer.ApiClient) erhält
  die REST-Endpunkte + Request-DTOs unter `Models/`.
- **UI:** `PlaylistDetailsPage` erhält Verwaltungsaktionen (Kopf-Menü,
  Eintrags-Kontextaktionen, Bestätigungsdialoge); `PlaylistsPage` erhält
  die Neuanlage; `MediaDetailsPage` erhält „Zu Playlist hinzufügen".
  Formulare (Playlist-Formular, Genre-Auswahl) als In-Page-Overlay-Panels
  statt nativer Dialoge — testbar über die Developer-API
  (`/invoke`, `/set-text`, `/tree`).
- **Owner-Schutz:** Schreibaktionen nur bei `IsOwner` der Playlist
  (serverseitig zusätzlich erzwungen); fremde öffentliche Playlists
  zeigen keine Verwaltungsaktionen.
- **E2E:** `MockVideoServer` erhält alle Verwaltungsrouten inkl. 409-
  Konfliktszenarien (Weiterschauen-Bezug, Sortiermodus-Verlust);
  neue E2E-Testklasse(n) fahren die realen UI-Flüsse.

## Akzeptanzkriterien (aus der Anforderung)

1. Playlists erstellen, umbenennen/bearbeiten und löschen lassen sich
   über die App bedienen.
2. Ein Medium lässt sich von seiner Detailseite zu einer Playlist
   hinzufügen; Playlist-Einträge lassen sich entfernen — der
   Weiterschauen-Referenzkonflikt (HTTP 409) wird mit
   Bestätigungsdialog und Wiederholung der Anfrage mit
   Bestätigungsflagge behandelt.
3. Einträge lassen sich an den Anfang und zwischen Positionen
   verschieben.
4. Sortiermodus lässt sich umschalten (inkl. serverseitiger
   Verlust-Bestätigung).
5. Genres lassen sich setzen und zurücksetzen.
6. Das Öffentlich-Flag lässt sich pflegen.
7. Fremde öffentliche Playlists sind in der App nicht editierbar.
8. Playlist-Cover-Verwaltung wird NICHT implementiert.

## Annahmen

- Eintrittsstelle „Medium zu Playlist hinzufügen" ist die
  Medien-Detailseite (Fixierung aus der Anforderung).
- „Zwischen Positionen verschieben" wird als positionelle Verschiebung
  (vor/zurück um einen Eintrag) plus An-Anfang/Ans-Ende umgesetzt —
  deckt die Server-Endpunkte `move-to-beginning` und `move-between` ab.
- Das Öffentlich-Flag wird nur bei eigenen Playlists und
  Administrator:innen angeboten (JWT-Claim `IsAdmin` wie die
  Claim-Prüfung der Server-UI); ein fehlender/abweichender Serverstand
  wird über die 403-Fehleranzeige abgedeckt.
