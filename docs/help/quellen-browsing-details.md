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

- **Genre-Leiste:** eine einzeilige, horizontal scrollende Chip-Leiste
  unter dem Suchfeld, mit vorangestelltem Eintrag **„Alle"**
  (kein Filter); das gewählte Genre ist hervorgehoben. Die Leiste
  belegt nur ihre Inhaltshöhe und verdrängt das Medien-Raster nicht —
  technisch ist sie als `ScrollView` + `BindableLayout` umgesetzt
  (Muster `MediaCarousel`), weil eine `CollectionView` in der
  `Auto`-Grid-Zeile sich unter iOS auf die volle Resthöhe ausdehnte
  und sämtliche Einträge verdeckte (TestFlight-Feedback, iPad). Ein
  Wechsel lädt die Liste ab der ersten Seite neu (`genreId`-Parameter
  von `api/items`).
- **Sichtbarkeit der Genre-Leiste:** Die Leiste erscheint nur, wenn
  der Server für die Quelle tatsächlich Genres liefert
  (`api/SourceGenres/{id}`). Liefert der Endpunkt eine leere Liste —
  oder schlägt der Abruf fehl — bleibt die Leiste ausgeblendet, statt
  eine funktionslose Zeile mit nur „Alle" zu zeigen
  (`SourceBrowserState.ShowGenreFilter`: mindestens ein Genre mit
  `Id > 0` erforderlich). Diagnosebefund des betreffenden Schritts:
  Leiste und Ladepfad waren im Auslieferungsstand vollständig
  vorhanden — die fehlende Anzeige entstand, weil der Genre-Abruf
  für die Quelle keine Einträge lieferte (leere Serverantwort bzw.
  Ladefehler-Fallback auf „Alle"); serverseitige Ursachen bleiben
  unverändert dokumentiert.
- **Textsuche:** das Suchfeld filtert serverseitig über den
  `search`-Parameter; Abschicken erfolgt über die Suchen-/Return-Taste
  und lädt die Trefferliste ab der ersten Seite neu. Eine getippte,
  noch nicht abgeschickte Eingabe bleibt über beliebige
  Modell-Aktualisierungen hinweg erhalten (Synchronisation,
  Fortschritt, Netzwerkereignisse, Ladezustände) — die Seite spiegelt
  den Modell-Suchtext nur noch ins Feld, wenn sich der abgeschickte
  Wert tatsächlich geändert hat (Abschicken, Quellen-Wechsel,
  Zustands-Reset). Es gibt bewusst kein Live-Suchen pro Tastenschlag.
- **Medien-Raster:** dreispaltige Poster-Karten mit Titel; fehlt ein
  Poster, bleibt der Titel lesbar.
- **Seitenweises Nachladen:** beim Erreichen des Listenendes wird die
  nächste Seite (`size` = 20, Wert des Altbestands) automatisch
  angehängt; scheitert das Nachladen, bietet die Fußzeile „Erneut
  versuchen" — die geladenen Einträge bleiben stehen.
- **Scrollposition beim Nachladen:** die Trefferliste bleibt beim
  Anhängen einer Folgeseite an der bisherigen Position — die neuen
  Einträge erscheinen am Ende, ohne dass die Ansicht an den Anfang
  springt. Die Seite bindet dazu eine stabile eigene Eintragsliste,
  die `SourceItemListSync` granular mit der unveränderlichen
  Modell-Liste abgleicht (gemeinsames Präfix per `Id`, Anhänge erzeugen
  nur `Add`-Ereignisse) — ein `ItemsSource`-Referenzwechsel pro
  Folgeseite, der den Ansichts-Reset verursachte, entfällt. Erstladen,
  Genre-Wechsel und neue Suche ersetzen die Liste dagegen bewusst
  komplett und zeigen sie wieder von Anfang an; das gilt für die
  ungefilterte Liste wie für Such- und Genre-Treffer (gleicher
  Anzeige- und Nachladepfad).
- **Rückkehr** aus einer Detailseite stellt Genre-Filter, Suchtext und
  Listenstand wieder her — kein erneutes Laden; der zuletzt
  abgeschickte Suchtext steht als erkennbarer aktiver Filter weiterhin
  im Suchfeld.

## Medien-Detailseiten

Die Route `mediadetails` trägt die Identität des Mediums
(`mediaType`, `id`, `title`) sowie optional die Serienkontext-Hints
`show`/`season`/`episode` (aus den eingebetteten Objektgraphen der
Startseiten-Listen).

| Medientyp | Darstellung |
|-----------|-------------|
| Film (`movie`) | Bild, Titel, Jahr, Genres, Handlung |
| Filmkollektion (`moviecollection`) | Sammlungs-Metadaten + Raster der enthaltenen Filme; Tap öffnet die Film-Detailseite. Antwortet der Server 404 (Einzelfilm ohne Hülle), wird automatisch die Filmansicht geladen |
| Serie (`tvshow`) | Serien-Metadaten, Staffel-Auswahl (`Picker`) und Episodenliste der gewählten Staffel mit Bild, „Nr. Titel", Kurzbeschreibung und Angesehen-Badge gesehener Episoden |
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
  kein Fehlerzustand. War die Detailseite im Offline-Modus aus dem
  Snapshot geöffnet, wird das Bild mit der zurückkehrenden
  Verbindung automatisch nachgeladen, sofern der Snapshot eine
  Bildquelle nennt (z. B. der Episoden-Still) — ohne dass die Seite
  erneut geöffnet werden muss.

### Angesehen-Symbol in der Episodenliste

In der Episodenliste der Serien-/Staffelansicht trägt jede als gesehen
markierte Episode ein **rundes Angesehen-Badge** nach Web-Vorbild
(`WatchedIndicator` / `.watched-indicator-episode -image-left`):

- Dunkler transluzenter Kreis mit feinem hellem Rand und einem
  **Augen-Symbol** in der weichen Primärfarbe, oben links über dem
  Episoden-Still; Tooltip/Hinweis „Gesehen".
- Grundlage ist der serverseitige Gesehen-Zeitpunkt
  (`MediaEntry.WatchedAt` → `EpisodeItem.IsWatched`); Episoden ohne
  Zeitpunkt zeigen **kein** Symbol.
- Markiert der Player eine Episode der geöffneten Serie als gesehen
  (Ende der Wiedergabe bzw. Watched-Endfenster), erscheint das Badge
  in der Liste **ohne erneutes Laden** (`MarkEpisodeWatchedInDetails`
  im Update-Zyklus); der exakte Server-Zeitpunkt folgt beim nächsten
  Laden.
- Der Offline-Snapshot übernimmt den Stand aus `CachedShowEpisode`
  (alte Cache-Einträge ohne das Feld gelten als ungesehen).

### Serienansicht — Stabilität und iOS-Verifikationsgrenze

Die Serienansicht (`SeasonSection` der `MediaDetailsPage`) wurde für
die iOS-Plattform gehärtet — drei plausibilisierte Fehlerquellen des
gemeinsamen UI-Codes sind behoben:

- **Episodenliste:** Die Liste wird nicht mehr als virtualisierte
  `CollectionView` innerhalb des `ScrollView` gerendert — unter iOS
  konnte sie dort mit Höhe 0 gemessen werden (keine sichtbaren
  Episoden). Stattdessen nutzt sie ein `VerticalStackLayout` mit
  `BindableLayout`-Template nach dem Muster des `MediaCarousel`;
  die Episodenkarte öffnet per `TapGestureRecognizer` weiterhin die
  Episoden-Detailseite. Aussehen und Bedienung bleiben identisch.
- **Staffel-Reihenfolge:** `SeasonPicker.ItemsSource` wird nur noch
  neu gesetzt, wenn sich die Staffel-Id-Sequenz tatsächlich ändert —
  der Picker baut seine Anzeige nicht mehr bei jeder
  Modell-Aktualisierung neu auf. Die Reihenfolge bleibt die
  aufsteigende Modell-Sortierung (`OrderBy(s.Number)`).
- **Bestehende Auswahl:** `SelectedItem` wird nur bei Abweichung
  gesetzt, und `OnSeasonSelected` ignoriert zusätzlich zum
  `updatingSeasonPicker`-Flag Echo-Events, die die bereits
  angewendete Modell-Auswahl erneut melden würden — verzögerte
  `SelectedIndexChanged`-Aufrufe (iOS) können die Staffelwahl nicht
  mehr auf „Staffel 01" zurücksetzen.

**iOS-Verifikationsgrenze:** Die Fehler waren iOS-spezifisch; ein
iOS-Build/-Testlauf (`scripts/iOS-Deployment.ps1`, erfordert
macOS/Xcode) stand bei der Umsetzung nicht zur Verfügung. Die
Ursachenbehebung ist plausibilisiert und im gemeinsamen Code wirksam;
der auf dieser Maschine geführte Nachweis umfasst die unverändert
korrekte Windows-Serienansicht (E2E-Tests `SeriesDetailsTests`:
Episodenliste sichtbar, Staffeln aufsteigend, Staffelwechsel bleibt
bestehen). Die abschließende iOS-Laufzeitverifikation ist einem
späteren iOS-Testlauf vorbehalten.

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
  Staffelwechsel) werden am Identitätsabgleich verworfen. Der
  Lade-Trigger (`WithDetailsHeaderLoad`) läuft nach der
  Detail-Ladung, bei geänderter Bildquelle durch eine Staffelwahl
  und beim Übergang in `ConnectionState.Connected`
  (`EnterConnected` — Reconnect-Nachladung analog `WithHeroLoad`);
  das Gate überspringt den Abruf offline, ohne Details, ohne
  Bildquelle, bei laufendem Laden und bei bereits vorhandenem Bild.
- Quellen-Icons und Poster nutzen die lokale Vorschaubild-Ablage
  (`previews/`, nicht backup-pflichtig) über `PosterImageService`
  (Menüeinträge) bzw. das Control `CachedPosterImage`.
