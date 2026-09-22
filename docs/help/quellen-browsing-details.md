# Quellen-Browsing und Medien-Detailseiten (Entwicklungsschritt 8)

Die Menüseite **„Quellen"** der neuen VideoPlayer-App zeigt die vom
Server freigegebenen Medienbereiche und führt in die Detailseiten für
Filme, Filmkollektionen, Serien, Staffeln und Episoden — im Muster der
alten App, vollständig über den MVU-Kreislauf gesteuert.

## Quellen-Übersicht

- Beim Öffnen wird die Liste **live vom Server** geladen
  (`api/Sources`); jede Quelle erscheint als Karte mit Icon
  (`api/sourceicons/{id}`) und Name.
- **Offline** steht keine Quellenliste zur Verfügung: statt einer
  leeren oder veralteten Liste zeigt die Seite das bekannte gelbe
  Banner mit einem klaren Hinweis.
- Ladefehler zeigen eine Meldung mit Schaltfläche **„Erneut laden"**;
  eine erfolgreiche, aber leere Antwort zeigt „Keine Quellen
  freigegeben.".

## Browsing einer Quelle

Die Auswahl einer Quelle öffnet die Übersichtsseite (Shell-Route
`sourceoverview`, auf den Navigationsstack geschoben):

- **Genre-Leiste:** horizontal, mit vorangestelltem Eintrag **„Alle"**
  (kein Filter); das gewählte Genre ist hervorgehoben. Ein Wechsel
  lädt die Liste ab der ersten Seite neu (`genreId`-Parameter von
  `api/items`).
- **Textsuche:** das Suchfeld filtert serverseitig über den
  `search`-Parameter; Abschicken erfolgt über die Suchen-Taste.
- **Medien-Raster:** dreispaltige Poster-Karten mit Titel; fehlt ein
  Poster, bleibt der Titel lesbar.
- **Seitenweises Nachladen:** beim Erreichen des Listenendes wird die
  nächste Seite (`size` = 20, Wert des Altbestands) automatisch
  angehängt; scheitert das Nachladen, bietet die Fußzeile „Erneut
  versuchen" — die geladenen Einträge bleiben stehen.
- **Rückkehr** aus einer Detailseite stellt Genre-Filter, Suchtext und
  Listenstand wieder her — kein erneutes Laden.

## Medien-Detailseiten

Die Route `mediadetails` trägt die Identität des Mediums
(`mediaType`, `id`, `title`) sowie optional die Serienkontext-Hints
`show`/`season`/`episode` (aus den eingebetteten Objektgraphen der
Startseiten-Listen).

| Medientyp | Darstellung |
|-----------|-------------|
| Film (`movie`) | Bild, Titel, Jahr, Genres, Handlung |
| Filmkollektion (`moviecollection`) | Sammlungs-Metadaten + Raster der enthaltenen Filme; Tap öffnet die Film-Detailseite. Antwortet der Server 404 (Einzelfilm ohne Hülle), wird automatisch die Filmansicht geladen |
| Serie (`tvshow`) | Serien-Metadaten, Staffel-Auswahl (`Picker`) und Episodenliste der gewählten Staffel mit Bild, „Nr. Titel" und Kurzbeschreibung |
| Staffel (`tvshowseason`) | Serienansicht mit vorgewählter Staffel (Serie über den `show`-Hint aufgelöst — es existiert kein eigener Staffel-Endpunkt) |
| Episode (`tvshowepisode`) | Serienansicht mit vorgewählter Staffel und markierter Episode; ist keine Serie auflösbar, werden die Basisdetails der Episode gezeigt |

## Favoriten

- Der Stern-Schalter `☆`/`★` auf der Detailseite schaltet die
  Markierung des angezeigten Mediums um (`api/favorites/toggle`) —
  der **Server bleibt führend**.
- Nach erfolgreicher Änderung wird die lokale Kopie
  `cached_favorites` (nicht backup-pflichtig) über denselben Pfad wie
  die Ereignis-Synchronisation autoritativ vom Serverstand erneuert;
  die Startseiten-Liste „Favoriten" aktualisiert sich automatisch
  über `CachedListsChangedMessage`.
- Bei Fehlschlag oder ohne Serververbindung bleibt der Stern
  unverändert und ein Fehlerhinweis erscheint — kein stilles
  Umschalten. Nach einem App-Neustart gilt der Serverstand, da die
  lokale Kopie bei jedem Sync aktualisiert wird.

## Architektur-Einordnung

- Zustand liegt in drei unveränderlichen Records im `AppModel`:
  `SourcesOverview` (Quellenliste), `SourceBrowser` (geöffnete Quelle
  mit Filter/Suche/Paging) und `MediaDetails` (Detailansicht inkl.
  Favoriten-Umschaltung).
- Plattformneutrale Dienste in `VideoPlayer.Mvu`:
  `SourceBrowserService` (Quellen/Genres/Einträge), `MediaDetailsService`
  (Detail-Mapping je Typ), `FavoritesService` (Toggle + Cache-Sync).
- Quellen-Icons und Poster nutzen die lokale Vorschaubild-Ablage
  (`previews/`, nicht backup-pflichtig) über `PosterImageService` und
  die Controls `CachedSourceIconImage`/`CachedPosterImage`.
