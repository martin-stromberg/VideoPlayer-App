# Playlists-Übersicht und -Detailansicht (Issue 43, Entwicklungsschritt 4)

Die Menüseite **„Playlists"** der neuen VideoPlayer-App bindet die
Playlist-Funktionalität des Servers an: eigene und öffentliche
Playlists in einer Übersicht sowie eine Detailansicht mit den
Playlist-Einträgen in der Server-Reihenfolge — vollständig über den
MVU-Kreislauf.

## Übersicht „Playlists"

Der Menüeintrag **„Playlists"** (zwischen „Favoriten" und „Downloads",
Playlist-Icon) öffnet eine scrollbare Liste mit zwei Sektionen:

- **„Meine Playlists"** — die Playlists des angemeldeten Benutzers
  (`GET api/playlists`).
- **„Öffentliche Playlists"** — alle als öffentlich markierten
  Playlists (`GET api/playlists/public`).

Jede Karte zeigt das Playlist-Cover (`GET api/playlists/{id}/cover`)
bzw. — wenn kein Cover gesetzt ist — den Playlist-Namen als Fallback,
dazu Name, Sortiermodus („Manuell"/„Nach Erscheinungsdatum") und die
Anzeige-Genres.

- **Reines Online-Feature:** Playlists werden ausschließlich vom
  Server geladen; es gibt keinen Offline-Cache. Ohne Serververbindung
  erscheint eine verständliche Fehleranzeige („Playlists stehen nur
  bei bestehender Serververbindung zur Verfügung.") mit der
  Schaltfläche „Erneut laden".
- **Fehlschlag beim Serverabruf** zeigt „Die Playlists konnten nicht
  geladen werden." mit Neuversuch; ein bereits geladener Stand
  bleibt sichtbar.
- **Leerzustand:** „Keine Playlists vorhanden."

## Detailansicht

Die Auswahl einer Playlist schiebt die Detailseite auf den
Navigationsstack (Route `playlistdetails?id=…&name=…`). Der
Kopfbereich zeigt Cover (oder Titel-Fallback), Name, Beschreibung und
eine Zusammenfassungszeile aus Eintragszahl und Sortiermodus.

Darunter stehen die **Einträge in der serverseitigen Reihenfolge**
(`GET api/playlists/{id}/entries/paged`, 1-basierte Seiten) als
Posterkarten mit Titel, Kontextzeile (bei Episoden „Serie · Episode N")
und Medientyp-Bezeichnung. Beim Erreichen des Listenendes wird die
nächste Seite automatisch nachgeladen; fehlschlagende Folgeseiten
lassen die Liste stehen und können erneut angefragt werden.

- Der **Tippen auf einen Eintrag** öffnet die Medien-Detailseite —
  bei Episoden mit dem bekannten Serienkontext als Hint. Die
  Wiedergabe aus der Playlist heraus ist Folgeschritt 5 und hier
  nicht Bestandteil.
- **Wiederherstellung:** Wird die Seite ohne bekannten Zustand
  geöffnet, lädt ein nicht-fataler Nachschlag die Kopfdaten nach
  (`GET api/playlists/{id}`); schlägt er fehl, bleibt der Name aus
  dem Navigationspfad als Titel stehen.
- **Fehlerzustand des Erstladens** zeigt Fehlermeldung plus „Erneut
  versuchen"; der Leerzustand meldet „Diese Playlist enthält keine
  Einträge."

## MVU-Anbindung

Eigene Zustände `PlaylistsPageState` (flache Zeilenliste aus
Sektionsüberschriften und Playlist-Karten) und `PlaylistDetailsState`
(1-basiertes Paging in Server-Konvention). Messages/Effekte:
`PlaylistsPageOpened`/`PlaylistsLoaded`, `PlaylistSelected` →
`NavigateToPlaylistDetails` + `LoadPlaylistEntries`,
`PlaylistDetailsOpened`/`PlaylistDetailsLoaded` (Rückkehr-/
Wiederherstellungspfad), `PlaylistEntriesLoaded` (Replace/Append mit
Verwerfen veralteter Anhänge-Antworten), `PlaylistEntriesNextPageRequested`,
`PlaylistEntriesRetryRequested` und `PlaylistEntrySelected` →
`NavigateToMediaDetails`. Der `PlaylistService` nutzt den
angemeldeten Client der `AuthSessionService`; Cover-Bilder werden
über `PosterImageService.GetPlaylistCoverSourceAsync` in die
Vorschaubild-Ablage geschrieben (`{id}.playlistcover`, nicht
backup-pflichtig) und ohne Cover bewusst nicht negativ gecacht.

## Testabdeckung

Unit-/Contract-Tests für Endpunkt-URLs und DTO-Mapping
(`VideoPlayerApiClientTests_Playlists`), Service-Mapping und
Fehlerpfade (`PlaylistServiceTests`), alle MVU-Übergänge
(`AppUpdateTests_Playlists`) sowie Menü-/Routen-/Lebenszyklus-Verträge.
Vier E2E-Szenarien (`PlaylistsTests`) fahren die echte App gegen den
`MockVideoServer`: Menüeintrag und beide Sektionen, Detailansicht in
Server-Reihenfolge, Cover-Abruf mit Fallback sowie die Fehleranzeige
mit Neuversuch.
