# Playlists-Übersicht, -Detailansicht, -Wiedergabe und -Verwaltung (Issue 43, Entwicklungsschritte 4–7)

Die Menüseite **„Playlists"** der neuen VideoPlayer-App bindet die
Playlist-Funktionalität des Servers an: eigene und öffentliche
Playlists in einer Übersicht sowie eine Detailansicht mit den
Playlist-Einträgen in der Server-Reihenfolge — vollständig über den
MVU-Kreislauf. Seit Schritt 5 sind die Einträge direkt abspielbar und
die Wiedergabe folgt der Playlist-Reihenfolge. Seit Schritt 6 lassen
sich eigene Playlists auf dem Gerät verwalten (Anlage, Bearbeitung,
Löschung, Einträge, Reihenfolge, Sortiermodus, Genres,
Öffentlich-Schalter).

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

- Der **Tippen auf einen abspielbaren Eintrag** (Film oder Episode,
  mit Zugriffsrecht) startet die Wiedergabe **ab genau diesem
  Eintrag** (`POST api/playlists/{id}/play?entryId=…`) — siehe
  „Wiedergabe" unten. Einträge, die nicht direkt abspielbar sind
  (Serien, Staffeln, Kollektionen, nicht zugängliche Medien), öffnen
  weiterhin die Medien-Detailseite — bei Episoden mit dem bekannten
  Serienkontext als Hint.
- Die Schaltfläche **„Abspielen"** im Kopfbereich startet die
  Wiedergabe ohne Startvorgabe (`POST api/playlists/{id}/play` ohne
  `entryId`) — der Server beginnt beim ersten abspielbaren und
  zugänglichen Eintrag; sie ist nur sichtbar, wenn die Playlist
  Einträge enthält.
- **Wiederherstellung:** Wird die Seite ohne bekannten Zustand
  geöffnet, lädt ein nicht-fataler Nachschlag die Kopfdaten nach
  (`GET api/playlists/{id}`); schlägt er fehl, bleibt der Name aus
  dem Navigationspfad als Titel stehen.
- **Fehlerzustand des Erstladens** zeigt Fehlermeldung plus „Erneut
  versuchen"; der Leerzustand meldet „Diese Playlist enthält keine
  Einträge."

## Wiedergabe aus einer Playlist

Die Playlist-Wiedergabe nutzt die serverseitigen Wiedergabe-Endpunkte —
die Reihenfolge entscheidet allein der Server (aktiver Sortiermodus),
die App sortiert nichts um:

- **Start:** `POST api/playlists/{id}/play` — optional mit `entryId`
  (gewählter Eintrag) oder ohne (erster abspielbarer und zugänglicher
  Eintrag). Die Antwort liefert den aufgelösten Eintrag samt Position,
  Medientyp/-id und der Fortsetzungsposition des Mediums.
- **Manuelle Navigation:** im Player stehen bei laufender
  Playlist-Wiedergabe die Schaltflächen **„Zurück"** und **„Weiter"**
  bereit (`POST …/play/previous` bzw. `…/play/next`) — der Server
  überspringt dabei nicht abspielbare oder nicht zugängliche Einträge.
  Eine Kontextzeile unter dem Titel zeigt „„Playlist-Name" · Eintrag
  n/m".
- **Automatische Fortsetzung:** am Titelende — wie bei Serien nur bei
  aktivierter Einstellung und laufender Wiedergabe — holt der Player
  den Folge-Eintrag über `POST …/play/advance`; ebenso setzt der
  Fehler-Rückfall (nicht verfügbare Quelle, Abspielfehler) über denselben
  Endpunkt beim nächsten Eintrag fort.
- **Quelle und Fortsetzungsposition:** die Wiedergabequelle wird wie
  üblich aufgelöst (lokale Datei vor Server-Stream); die mit der
  Play-Antwort gelieferte Fortsetzungsposition (`startPositionSeconds`)
  gilt verbindlich — auch wenn das gleiche Medium in anderen Kontexten
  einen eigenen Weiterschauen-Stand hätte.
- Meldet der Server keinen Folgeeintrag (204), endet die Wiedergabe —
  bei Rückwärts-Navigation am Listenanfang passiert nichts.
- Die Playlist-Wiedergabe ist ein Online-Feature (wie die Anzeige);
  offline startet keine Playlist-Wiedergabe.

## Verwaltung (Schritt 6)

Eigene Playlists lassen sich direkt auf dem Gerät pflegen — alle
Operationen sind wie die Anzeige reine Online-Features und laufen über
die Verwaltungs-Endpunkte des Servers:

- **„Neue Playlist"** auf der Übersichtsseite öffnet ein Dialog-Panel
  mit Name (Pflichtfeld mit Inline-Fehltext), Beschreibung und
  Sortiermodus-Auswahl (`POST api/playlists`).
- **„Verwalten"** im Kopf der Detailseite (nur bei eigenen Playlists)
  bündelt die Operationen: **„Bearbeiten"** (Name/Beschreibung,
  `PUT api/playlists/{id}`), **„Playlist löschen"** nach Bestätigung
  (`DELETE api/playlists/{id}`, danach zurück zur Übersicht),
  **Sortiermodus-Wechsel**, **„Genres bearbeiten"** sowie der
  Öffentlich-Punkt.
- **„Zu Playlist"** auf der Medien-Detailseite (nur online) listet die
  eigenen Playlists; die Wahl legt das Medium über
  `POST api/playlists/{id}/entries` an — Duplikate meldet der Server
  mit Hinweistext statt Fehler.
- **Eintrags-Kontextmenü (⋮)** je Eintrag: „Aus Playlist entfernen"
  (`DELETE …/entries/{type}/{id}`) und im manuellen Sortiermodus die
  Verschiebe-Aktionen „An den Anfang" (`…/move-to-beginning`),
  „Ans Ende" (`GET …/entries/max-sort-order` → `PUT …/entries/{entryId}/order`
  mit max+1) sowie „Eins nach vorn/hinten" (`…/move-between` mit der
  Ziel-SortOrder).
- **Weiterschauen-Konflikt:** Antwortet das Entfernen mit HTTP 409
  (`IsContinueWatchingConfirmationRequired`), fragt ein Dialog „Aus
  Weiterschauen entfernen?" nach — „Entfernen" wiederholt den Aufruf
  mit `confirmContinueWatchingRemoval=true`, „Abbrechen" lässt alles
  unverändert.
- **Sortiermodus-Wechsel** („Nach Erscheinungsdatum sortieren" bzw.
  „Manuell sortieren", `PATCH …/sort-mode`): verwirft der Wechsel die
  manuelle Reihenfolge (409 mit `IsLossOfDataConfirmationRequired`),
  bestätigt der Dialog „Sortiermodus ändern?" den Verlust
  (`confirmLossOfManualOrder=true`).
- **Genre-Editor:** „Genres bearbeiten" öffnet die Auswahl aller
  Server-Genres (`GET api/items/genres`) mit Vorauswahl der
  Playlist-Genres; „Speichern" schreibt die Ids (`PUT …/genres`),
  „Zurücksetzen" stellt die berechneten Genres wieder her
  (`POST …/genres/reset`).
- **Öffentlich/Privat:** „Öffentlich schalten" bzw. „Privat schalten"
  (`PUT …/public`) steht nur Administrator:innen offen — der Punkt
  erscheint ausschließlich, wenn das Anmelde-Token den
  `IsAdmin`-Claim trägt; der Server prüft die Berechtigung unabhängig
  davon erneut.
- **Schreibschutz:** Fremde öffentliche Playlists zeigen weder
  „Verwalten" noch die ⋮-Menüs — die Detailansicht bleibt lesend.
- Playlist-Cover bleiben Anzeige-Feature (Schritt 4) — eine
  Cover-Verwaltung ist nicht Teil dieses Schritts.

## Weiterschauen mit Playlist-Bezug

Weiterschauen-Einträge, die aus einer Playlist-Wiedergabe entstanden
sind, tragen den Playlist-Bezug (`playlistId`, `playlistName`,
`playlistEntryId` des Server-DTOs) bis in die Startseiten-Liste. Ein
Tap auf einen solchen Eintrag startet die Wiedergabe über den
Playlist-Kontext (`POST api/playlists/{id}/play?entryId=…`) — das
Folgevideo kommt aus der **Playlist-Reihenfolge**, nicht aus der
Serienreihenfolge des Mediums. Ist das Medium inzwischen nicht mehr
Teil der Playlist (`playlistEntryId` fehlt), gilt wie bisher der
detailseiten-unabhängige Serienkontext.

Der Bezug wirkt auch in der Gegenrichtung: Fortschrittsmeldungen aus
einer laufenden Playlist-Wiedergabe tragen die `PlaylistId` des
aktiven Kontexts mit (`POST api/continue-watching/progress`, alle
Meldepfade — Takt, Ende und Player-Schluss). Der Server schreibt
damit den playlist-gebundenen Weiterschauen-Eintrag fort, statt
daneben einen zweiten, kontextlosen Eintrag für dasselbe Medium
anzulegen. Auch offline eingereihte Meldungen behalten den Bezug —
die Fortschritts-Warteschlange `pending_progress` speichert die
`PlaylistId` mit und übergibt sie beim Nachsenden. Meldungen ohne
Playlist-Kontext bleiben unverändert kontextlos.

## Downloads mit Playlist-Bezug (Schritt 7)

Der automatische Download-Vorlauf folgt bei playlist-gebundenen
Weiterschauen-Einträgen der **Playlist-Reihenfolge**: statt der
Serienreihenfolge reiht er den aktuellen Eintrag und die folgenden
ladbaren und zugänglichen Einträge der Playlist
(`api/playlists/{id}/entries/paged`, Server-Reihenfolge ab
einschließlich des aktuellen Eintrags) ein — insgesamt bis zur Anzahl
„Anzahl vorzuladender Episoden". Playlist-gebundene Filme laden sich
selbst plus die folgenden Playlist-Einträge — der Film zählt dabei ins
Budget. Der Bezug landet als
`PlaylistId`/`PlaylistEntryId`/`PlaylistSortOrder` im Download-Manifest
(`download_manifest`, Spalten werden in Bestandsinstallationen
nachgezogen). Bei verwaistem Eintragsbezug oder nicht erreichbarer
Playlist gilt der Serien-Pfad; Einträge ohne Playlist-Bezug folgen dem
Serien-Pfad. Details siehe `downloads.md`.

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
`StartPlaylistPlayback` (abspielbar) bzw. `NavigateToMediaDetails`.
Der `PlaylistService` nutzt den angemeldeten Client der
`AuthSessionService`; Cover-Bilder werden über
`PosterImageService.GetPlaylistCoverSourceAsync` in die
Vorschaubild-Ablage geschrieben (`{id}.playlistcover`, nicht
backup-pflichtig) und ohne Cover bewusst nicht negativ gecacht.

Der Wiedergabepfad (Schritt 5): `PlaylistPlaybackStartRequested` und
die Eintragsauswahl erzeugen `StartPlaylistPlaybackEffect` → der
Effekt-Handler ruft `PlaylistService.StartPlaybackAsync` →
`PlaylistPlaybackStartedMessage` belegt `PlayerState` mit dem
Playlist-Kontext (`PlaylistId`, `PlaylistName`, `PlaylistEntryId`,
`PlaylistPosition`, `PlaylistTotalCount`) und startet die
Quellauflösung mit der serverseitigen Fortsetzungsposition
(`ResolvePlaybackSourceEffect.StartPositionSeconds` →
`PlaybackSourceService.ResolveAsync`-Override). Manuelle Navigation:
`PlaylistNextRequested`/`PlaylistPreviousRequested` →
`LoadNextPlaylistEntryEffect`/`LoadPreviousPlaylistEntryEffect` →
`PlaylistNavigationCompletedMessage` (Identitätswache, 204-Erschöpfung
über `PlaylistNextExhausted`); Auto-Fortsetzung und Fehler-Rückfall
nutzen `AdvancePlaylistEntryEffect` (`play/advance`) — alle drei
Navigationspfade laufen über `NavigatePlaylistEntryAsync` im
Effekt-Handler. Der Weiterschauen-Bezug wandert vom DTO
(`ContinueWatchingEntry.Playlist*`) über `HomeMediaItem` in den
Auswahl-Guard von `UpdateContinueWatchingItemSelected`.

## Testabdeckung

Unit-/Contract-Tests für Endpunkt-URLs und DTO-Mapping
(`VideoPlayerApiClientTests_Playlists`, `VideoPlayerApiClientTests_PlaylistAdmin`),
Service-Mapping und Fehlerpfade (`PlaylistServiceTests`,
`PlaylistServiceTests_Management`), alle MVU-Übergänge
(`AppUpdateTests_Playlists`, `AppUpdateTests_PlaylistManagement`) sowie
Menü-/Routen-/Lebenszyklus-Verträge.
Vier E2E-Szenarien (`PlaylistsTests`) fahren die echte App gegen den
`MockVideoServer`: Menüeintrag und beide Sektionen, Detailansicht in
Server-Reihenfolge, Cover-Abruf mit Fallback sowie die Fehleranzeige
mit Neuversuch.

Die Verwaltung (Schritt 6) ist durch elf E2E-Szenarien
(`PlaylistManagementTests`) abgesichert: Anlage über das Formular,
Bearbeiten und Löschen über das Verwaltungsmenü, „Zu Playlist hinzufügen"
der Medien-Detailseite, Entfernen ohne und mit Weiterschauen-Konflikt
(Bestätigung und Abbruch), die drei Verschiebe-Aktionen, Sortiermodus-
Wechsel mit Verlust-Bestätigung, Genres setzen und zurücksetzen, der
Öffentlich-Schalter des Administrator-Tokens sowie die
schreibgeschützte Ansicht fremder öffentlicher Playlists.

Der Wiedergabepfad wird durch `AppUpdateTests_PlaylistPlayback`
(Übergänge: Start, Eintragsauswahl, Weiterschauen-Guard, Navigation,
Erschöpfung, Auto-Fortsetzung, Fehler-Rückfall, Serienpfad-Regression),
die Service-Tests der Play-Endpunkte (Mapping, `entryId`-/`currentEntryId`-Query,
204) und sechs E2E-Szenarien (`PlaylistPlaybackTests`: Button-Start mit
Überspringen nicht abspielbarer Einträge, Eintrag-Start, Vor-/Rückwärts-Navigation,
Auto-Fortsetzung, Fehler-Rückfall sowie der playlist-gebundene
Weiterschauen-Eintrag ohne Serienaufruf) abgesichert.
