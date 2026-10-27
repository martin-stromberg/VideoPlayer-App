# Übersetzte Anforderung: Quellen-Browsing und Medien-Detailseiten (Entwicklungsschritt 8)

## Fachliche Zusammenfassung

Die MVU-App `VideoPlayer.Maui` erhält einen ausgebauten Menüpunkt
**„Quellen"** (`AppPage.Sources`, Route `sources`): eine Übersicht der vom
Server freigegebenen Medienbereiche (`GET api/Sources`). Pro Quelle können
die enthaltenen Medien durchblättert werden — mit Genre-Filterung
(`GET api/SourceGenres/{id}`), Textsuche und seitenweisem Nachladen
(`GET api/items?mediaSourceId=…&page=…&size=…[&search=…][&genreId=…]`).
Die aus Schritt 7 bekannte Platzhalter-Detailseite `MediaDetailsPage`
(Route `mediadetails`) wird zur echten Detailseite ausgebaut: Serien zeigen
Staffel- und Episodenauswahl (`GET api/items/tvshow/{id}`), Filmkollektionen
die enthaltenen Filme (`GET api/items/moviecollection/{id}`), Filme und
Episoden Basisdetails (`api/items/movie/{id}`, `api/items/tvshowepisode/{id}`).
Medien können über die Detailseite als Favorit markiert und die Markierung
wieder entfernt werden (`POST api/favorites/toggle`); die Favoriten werden
primär serverseitig geführt, die lokale Kopie in `cached_favorites` (nicht
backup-pflichtig) wird bei jeder Änderung mitgepflegt und die
Startseiten-Favoritenliste aktualisiert sich über die bestehende
`CachedListsChangedMessage`-Verdrahtung. Im Offlinebetrieb zeigt die
Quellen-Seite einen Hinweis statt leerer oder fehlerhafter Daten.
Aussehen und Bedienung folgen der bestehenden Anwendung (Altbestand
`SourceOverviewPage`, `TVShowDetailsPage`, `MovieCollectionDetailsPage`).

## Betroffene Klassen und Komponenten

- **MVU-Kern (`VideoPlayer.Mvu`):**
  - `AppModel` — neuer Zustand für die Quellenliste (Einträge, Ladezustand,
    Fehlertext), den Quellen-Browser (Quelle, Genres, gewählter Genre-Filter,
    Suchtext, paginierte Einträge, Nachlade-Zustand) und die Medien-Details
    (geladenes Medium, Staffeln/Episoden bzw. Filme, Favoriten-Markierung)
  - `AppMessage` — neue Messages: Quellen laden/geladen/fehlgeschlagen,
    Quelle ausgewählt, Genres geladen, Genre-Filter gewählt, Suche
    abgeschickt, Eintragsseite geladen/nachladen, Detail-Ladung
    angefordert/geladen/fehlgeschlagen, Staffel gewählt,
    Favoriten-Umschaltung angefordert/abgeschlossen
  - `AppEffect` — neue Effekte: Quellen laden, Genres laden,
    Eintragsseite laden, Detaildaten laden, Favoriten-Umschaltung zum Server,
    Navigation zur Quellen-Browsing-Route
  - `AppUpdate` — Übergänge der neuen Messages inkl. Rücksetzen des
    Browser-/Detail-Zustands beim Kontextwechsel
  - Neue plattformneutrale Anzeige-Records (Quelleneintrag, Genre-Eintrag,
    Medien-Summary-Eintrag, Detail-Ansichtsmodell, Staffel-/Episoden-/
    Filmeintrag)
  - Neue plattformneutrale Dienste auf `AuthSessionService.Client` +
    `CacheDatabase`: Quellen-/Browser-Ladedienst (Sources, Genres, Items)
    und Favoriten-Dienst (Server-Toggle + Pflege der `cached_favorites`-
    Kopie, Detail-Ladung je Medientyp)
- **App-Rand (`VideoPlayer.Maui`):**
  - `Pages/SourcesPage.xaml(.cs)` — Ausbau der Platzhalterseite: Liste der
    freigegebenen Quellen mit Icons, Offline-Hinweis, Lade-/Fehlerzustand
  - `Pages/SourceOverviewPage.xaml(.cs)` — neue Seite (Shell-Route
    `sourceoverview`, auf den Navigationsstack geschoben): Genre-Leiste
    (inkl. „Alle"), Suchfeld, Medien-Raster mit seitenweisem Nachladen
  - `Pages/MediaDetailsPage.xaml(.cs)` — Ausbau der Platzhalterseite:
    Banner/Poster, Titel, Jahr, Genres, Handlung, Favoriten-Stern;
    je Medientyp Staffel-Picker + Episodenliste (Serie) bzw. Filmliste
    (Filmkollektion) bzw. Basisdetails (Film/Episode)
  - `AppShell.xaml.cs` — Registrierung der Route `sourceoverview`
  - `ShellAppEffectHandler` — Ausführung der neuen Effekte (Quellen,
    Genres, Eintragsseiten, Details, Favoriten-Toggle) mit Rückdispatch
    der Ergebnis-Messages; nach erfolgreicher Favoriten-Änderung
    `CachedListsChangedMessage` zum Neu-Laden der Startseiten-Liste
  - `Controls/CachedPosterImage` / `Services/PosterImageService` —
    Wiederverwendung für Poster/Banner; Erweiterung um Quellen-Icons
    (`api/sourceicons/{id}`)
  - `MauiProgram` — DI-Registrierung der neuen Seite und Dienste
- **Ablage (`VideoPlayer.Storage`):**
  - `cached_favorites` wird bei Favoriten-Änderungen durch den neuen
    Favoriten-Dienst gepflegt (Schlüsselkonvention `{typ}:{id}` wie
    `ServerSyncService`); keine Schemaänderung
- **Tests (`VideoWebPlayer.Maui.Tests`):**
  - MVU-Übergangstests: Quellen laden/offline, Browser-Zustand
    (Genre-Filter, Suche, Paging), Detail-Ladung je Medientyp,
    Staffelwahl, Favoriten-Toggle inkl. Cache-Pflege
  - Service-Tests: Mapping Quellen/Genres/Items/Details über
    `StubHttpMessageHandler`; `cached_favorites`-Pflege über reale
    SQLite-Datei (`SqliteDatabase`-Collection)
  - Contract-Tests: Seitenstruktur (Quellenliste, Genre-Leiste, Suche,
    Paging-Trigger, Detailseiten-Abschnitte, Favoriten-Stern),
    Route `sourceoverview`, DI-Verdrahtung

## Implementierungsansatz

- Der Browser- und Detail-Zustand wird als unveränderliche Records im
  `AppModel` geführt (MVU, plattformneutral); die Seiten dispatchen
  Benutzeraktionen als Messages und binden an `Model.*`.
- Die Quellenliste wird beim Öffnen der Quellen-Seite live über
  `GetSourcesAsync` geladen (entspricht der Server-API); im Offline-Zustand
  (`model.IsOffline`) wird kein Lauf angestoßen und ein Hinweis angezeigt.
- Die Auswahl einer Quelle dispatcht eine Auswahl-Message; `AppUpdate`
  initialisiert den Browser-Zustand und erzeugt die Effekte Navigation
  (`sourceoverview?id=…&name=…`), Genre-Ladung und erste Eintragsseite —
  Muster wie `NavigateToMediaDetailsEffect` aus Schritt 7.
- Genre-Filter und Textsuche setzen die Eintragsliste zurück und laden
  ab Seite 0 neu; das seitenweise Nachladen hängt Ergebnisse an, solange
  die Seite voll gefüllt zurückkommt (`HasMore` aus `items.Count == size`).
- Die Detailseite meldet beim Erscheinen ihre Identität
  (`mediaType`/`id` aus den Query-Properties); `AppUpdate` setzt den
  Ladezustand und stößt `LoadMediaDetailsEffect` an. Der Handler wählt
  je nach Medientyp den passenden Endpunkt; für `episode`-Einträge wird
  über `Season.Show` zusätzlich die Serie geladen und die Episode in
  Staffel/Episodenliste vorgewählt (Muster Altbestand
  `TVShowDetailsPage(tvShowId, seasonId, episodeId)`).
- Favoriten-Markierung: Stern-Schaltfläche auf der Detailseite dispatcht
  eine Toggle-Message; der Effekt ruft `ToggleFavoriteAsync` mit einem
  typkorrekten `MediaEntry` auf, pflegt die `cached_favorites`-Kopie
  (Upsert/Delete) und meldet den neuen Zustand zurück; danach wird
  `CachedListsChangedMessage` dispatcht, damit die Startseiten-Favoriten
  ohne manuelle Aktualisierung folgen.
- **Auswahl-/Identifikationsanforderungen:** Quellen, Genres, Medien,
  Staffeln und Episoden werden ausschließlich über benannte, sichtbare
  Einträge ausgewählt (Liste, Genre-Leiste, Suche, Picker, Raster) — keine
  Eingabe interner Kennungen.

## Konfiguration

Keine neuen Konfigurationseinträge. Serveradresse und Anmeldung kommen aus
dem bestehenden Verbindungs-Workflow (`AuthSessionService`).

## Akzeptanzkriterien (aus der Anforderung)

1. Die Quellenliste entspricht den Daten der Server-API (`api/Sources`).
2. Genre-Filter, Suche und seitenweises Nachladen funktionieren beim
   Durchblättern einer Quelle.
3. Detailseiten zeigen für Serien Staffeln/Episoden und für
   Filmkollektionen die enthaltenen Filme korrekt an; Filme und Episoden
   zeigen Basisdetails.
4. Favoriten-Markierungen werden zum Server übertragen und sind nach
   Neustart der App konsistent (serverseitig führend, lokale Kopie in
   `cached_favorites` als nicht backup-pflichtiger Offline-Cache).
5. Offline-Einschränkung: Die Quellen-Seite zeigt im Offlinebetrieb einen
   Hinweis statt leerer oder fehlerhafter Daten.

## Offene Fragen

Keine — die Endpunkte sind im neuen `VideoPlayerApiClient` bereits
implementiert und getestet (Schritt 1), die Cache-Tabellen und die
Favoriten-Event-/Sync-Verdrahtung existieren (Schritt 6/7), die
Detail-Route `mediadetails` ist registriert, und die UI-Muster sind durch
den Altbestand (`SourceOverviewPage`, `TVShowDetailsPage`,
`MovieCollectionDetailsPage`) sowie den `MediaCarousel`/`CachedPosterImage`-
Bestand der neuen App vorgegeben.
