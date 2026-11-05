# Anforderung (übersetzt und strukturiert)

Quelle: `docs/projects/task/issue-86-…-apple-tv-app-tvos-lean-back-pl/project-plan.md`, „### Schritt 8: Playlists — lesender Zugriff und Wiedergabe".

## Ausgangstext (wörtlich)

> Die tvOS-App soll den rein lesenden Playlist-Zugriff der Anforderung erhalten: Übersicht der eigenen und öffentlichen Playlists, Detailansicht einer Playlist mit seitenweise nachladbaren Einträgen (Titel, Poster, übergeordneter Kontext) sowie Abspielen der gesamten Playlist bzw. ab einem gewählten Eintrag unter Nutzung des in Schritt 7 geschaffenen Wiedergabeflusses inklusive Playlist-Kontext (Positionsanzeige, next/previous-Navigation, Auto-Fortsetzung über den advance-Endpunkt). Jegliche Playlist-Verwaltung (Anlegen, Umbenennen, Sortieren, Eintragsverwaltung, Genre-Editor, Öffentlich-Schalter) ist explizit ausgeschlossen und wird nicht verdrahtet — die entsprechenden Dienst-Methoden und Modellanteile bleiben ohne Oberfläche; es gibt keinerlei Schreibzugriff auf Inhalte.

## Akzeptanzkriterien

1. Die Playlist-Übersicht zeigt eigene und öffentliche Listen.
2. Die Detailansicht lädt Einträge seitenweise.
3. Das Abspielen startet den Wiedergabefluss mit Playlist-Kontext und zeigt Positionsinformationen.
4. Am Ende eines Eintrags bzw. per Fernbedienung ist der Übergang zum nächsten/vorherigen Eintrag möglich.
5. Verwaltungsaktionen sind in der tvOS-App nicht erreichbar.
6. tvOS-Lauffähigkeit wird über den macOS-CI-Build und Simulator geprüft.

## Strukturierte Zerlegung

### Übersicht (Menüseite „Playlists")

- Bereich `AppPage.Playlists` ist bereits in `AppRoutes.MenuPages` enthalten — bisher zeigt die Tab-Leiste dort einen Platzhalter.
- Anzuzeigen: die eigenen Playlists („Meine Playlists") und die öffentlichen Playlists („Öffentliche Playlists") — je mit Sektionsüberschrift, Cover (`api/playlists/{id}/cover`), Name, Sortiermodus und Anzeige-Genres.
- Reines Online-Feature: ohne Serververbindung eine verständliche Fehleranzeige mit Wiederholmöglichkeit; kein Cache-Stand.
- Laden über `PlaylistsPageOpenedMessage` → `LoadPlaylistsEffect` → `PlaylistsLoadedMessage`.

### Detailansicht einer Playlist

- Modal präsentiert über `NavigateToPlaylistDetailsEffect` (Pendant zur Shell-Route `playlistdetails`).
- Kopfbereich: Name, Zusammenfassung (Eintragszahl, Sortiermodus), Beschreibung, Genres, Cover; Aktion „Abspielen" (gesamte Playlist ab dem ersten abspielbaren Eintrag).
- Einträge in serverseitiger Reihenfolge: Titel, Poster (`api/pictures/{id}` über `ResolvedPictureId`), übergeordneter Kontext (Serien-/Staffelkontext — `Subtitle`).
- Seitenweises Nachladen (`api/playlists/{id}/entries/paged`, PageSize 20) bei Annäherung ans Listenende; Lade-, Leer- und Fehlerzustände mit Wiederholmöglichkeit.
- Auswahl eines Eintrags: abspielbarer und zugänglicher Eintrag (Film/Episode) startet die Wiedergabe ab diesem Eintrag; übrige Einträge öffnen die Medien-Detailseite — entscheidet `AppUpdate`, nicht die Oberfläche.
- Menü-Taste der Siri Remote schließt die Seite.

### Wiedergabe mit Playlist-Kontext

- Nutzt den in Schritt 7 verdrahteten Pfad: `StartPlaylistPlaybackEffect` (play), `LoadNextPlaylistEntryEffect`/`LoadPreviousPlaylistEntryEffect` (next/previous), `AdvancePlaylistEntryEffect` (Auto-Fortsetzung) — im Effekt-Handler bereits abgedeckt.
- Positionsanzeige über `TvosPlayerInfoViewController`/`PlaylistLabel` (bereits vorhanden).

### Ausschlüsse (nicht verdrahten)

- Playlist-Verwaltung: Anlegen, Umbenennen, Sortieren, Eintragsverwaltung, Genre-Editor, Öffentlich-Schalter — weder Oberfläche noch Effekt-Verdrahtung.
- Kein Schreibzugriff auf Inhalte.

## Rahmenbedingungen

- `net10.0-tvos` unter Windows nicht kompilierbar → Kompiliernachweis über macOS-CI-Job `build-tvos`.
- Fachliche Logik ist bereits plattformneutral umgesetzt (`AppUpdate`, `PlaylistService`, `PlaylistsPageState`, `PlaylistDetailsState`) — keine Änderungen am geteilten MVU-Kern erforderlich.
- `dotnet test VideoPlayer.Maui.Tests` muss grün bleiben; Contract-Tests analog `TvOsPlaybackContractTests` ergänzen.
