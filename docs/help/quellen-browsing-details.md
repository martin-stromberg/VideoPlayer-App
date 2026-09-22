# Quellen-Browsing und Medien-Detailseiten (Entwicklungsschritt 8)

Die vom Server freigegebenen Medienbereiche erscheinen als eigene
Einträge **direkt im Navigationsmenü** der neuen VideoPlayer-App und
führen in die Detailseiten für Filme, Filmkollektionen, Serien,
Staffeln und Episoden — im Muster der alten App, vollständig über den
MVU-Kreislauf gesteuert.

## Quellen im Menü

- Hinter dem Eintrag **„Startseite"** steht im Seitenmenü die
  Sektionsüberschrift **„Quellen"** (als Abschnittstitel formatiert);
  darunter erhält jede freigegebene Quelle einen eigenen Menüeintrag
  mit Name und — sofern vorhanden — Icon (`api/sourceicons/{id}`).
- Die Quellenliste wird **live vom Server** geladen (`api/Sources`):
  beim Verbindungsaufbau sowie nach (Teil-)Synchronisationen, sodass
  Freigaben und Entzüge ohne App-Neustart sichtbar werden.
- Während des (Erst-)Ladens zeigt die Sektion die Statuszeile
  **„Quellen werden geladen …"**; bei einem Nachladevorgang bleiben
  bereits vorhandene Einträge stattdessen sichtbar.
- **Offline** steht keine Quellenliste zur Verfügung: beim Übergang in
  den Offline-Zustand werden die Quelleneinträge aus dem Menü entfernt.
- Sind keine Quellen freigegeben, steht unter der Überschrift die
  Statuszeile **„Keine Quellen freigegeben"**.
- Schlägt das Laden fehl, erscheint unter der Überschrift der
  klickbare Eintrag **„Quellen konnten nicht geladen werden — erneut
  versuchen"** (rot hervorgehoben), der den Ladevorgang erneut
  anstößt — das Seitenmenü bleibt dabei geöffnet und der Rest des
  Menüs funktionsfähig.

## Browsing einer Quelle

Die Auswahl einer Quelle schließt das Seitenmenü und öffnet die
Übersichtsseite (Shell-Route `sourceoverview`, auf den
Navigationsstack geschoben):

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

### Kopfbereich der Detailseiten

Jede Detailseite öffnet mit einem **vollbreiten Kopfbereich** nach
Web-Vorbild (`tvshow-header`): das Hintergrundbild liegt bildschirmbreit
in Cover-Darstellung unter geschichteten Dunkelgradienten (seitlicher
Verlauf, weicher Übergang zum Seitenhintergrund, feine Trennlinie). Die
Höhe folgt der Fensterbreite (mindestens 360, höchstens 620).

- **Inhalt unten links:** Titel, eine Metazeile (Medientyp, Jahr,
  Genres bzw. Staffel-/Episoden-Angaben) und die Handlung.
- **Schalter:** der Favoriten-Stern liegt oben rechts auf dem Bild;
  „Abspielen" und „Herunterladen" (bei Filmen und Episoden) unten
  rechts.
- **Bildquelle:** bei Serien, Staffeln, Filmen und Kollektionen das
  Banner- bzw. Fanart-/Poster-Motiv; bei Episoden der Episoden-Still.
  Auf der Serien- und Staffelseite wechselt der Kopfbereich mit der
  Staffelwahl auf das Still der ersten Episode — wie in der
  Webanwendung.
- **Ohne Bild** (fehlende Grafik, Fehlschlag oder Offline) bleibt der
  Kopfbereich mit Gradienten und Texten stabil — kein Layoutbruch,
  kein Fehlerzustand.

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
  `SourcesOverview` (Quellen des Navigationsmenüs), `SourceBrowser`
  (geöffnete Quelle mit Filter/Suche/Paging) und `MediaDetails`
  (Detailansicht inkl. Favoriten-Umschaltung). Die `AppShell` abonniert
  `MvuHost.ModelChanged` und baut die Menüeinträge bei geändertem
  Quellenstand neu auf.
- Plattformneutrale Dienste in `VideoPlayer.Mvu`:
  `SourceBrowserService` (Quellen/Genres/Einträge), `MediaDetailsService`
  (Detail-Mapping je Typ inkl. der Kopfbereichs-Ableitungen
  `HeaderTitle`/`HeaderMeta`/`HeaderPlot`/`HeaderPictureId`/
  `HeaderEpisodeId`), `FavoritesService` (Toggle + Cache-Sync).
- Das Kopfbereichs-Bild wird flüchtig über den MVU-Kreislauf geladen
  (`LoadMediaDetailsHeaderEffect` → `MediaDetailsHeaderImageLoadedMessage`,
  `byte[]` im `MediaDetailsState`): Episoden über den Still-Endpunkt
  `api/episodes/{id}/background-image`, sonst `api/pictures/{id}`;
  verspätete Antworten abgelöster Bildquellen (z. B. nach einem
  Staffelwechsel) werden am Identitätsabgleich verworfen.
- Quellen-Icons und Poster nutzen die lokale Vorschaubild-Ablage
  (`previews/`, nicht backup-pflichtig) über `PosterImageService`
  (Menüeinträge) bzw. das Control `CachedPosterImage`.
