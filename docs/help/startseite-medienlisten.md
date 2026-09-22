# Startseite mit gecachten Medienlisten (Entwicklungsschritt 7)

Die Startseite der neuen VideoPlayer-App zeigt drei horizontale
Medienlisten — **„Weiterschauen"**, **„Favoriten"** und **„Neu im
Programm"** — mit Poster-Karten im Muster der alten App. Grundlage ist
das Cache-First-Prinzip: beim Öffnen erscheint sofort der zuletzt
bekannte Stand aus der lokalen Ablage; sobald aktuelle Serverdaten
eintreffen, aktualisieren sich die Listen von selbst.

## Hero-Kopfbereich

Oberhalb der Listen zeigt die Startseite einen vollbreiten
Kopfbereich nach Web-Vorbild: das servergenerierte Hintergrundbild
(Endpunkt `api/pictures/hero-background`, Abruf über den angemeldeten
API-Client mit Bearer-Token) liegt unter geschichteten
Dunkelgradienten — seitlicher Verlauf, Bottom-Fade in den
Seitenhintergrund und dezente Primär-/Sekundär-Akzente. Unten links
stehen ein Kicker-Chip („Private Media Library"), der Anzeigename des
aktiven Servers als Titel und ein Beschreibungstext; eine Trennlinie
bildet den Abschluss nach unten. Die Höhe folgt der Fensterbreite
(mindestens 260, höchstens 520 — das App-Gegenstück zum Web-CSS
`clamp(260px, 42vw, 520px)`).

Das Bild wird einmalig je Verbindung geladen und nur flüchtig im
Arbeitsspeicher gehalten — keine Dateiablage. Ist kein Bild verfügbar
(offline, fehlgeschlagener Abruf), zeigt der Bereich nur Gradienten
und Texte; die Seite bleibt stabil. Der Bereich wird bei Abmeldung,
Sitzungsablauf und Serverwechsel zurückgesetzt.

Der Kopfbereich lässt sich über die Einstellung **„Kopfbereich
anzeigen"** (Einstellungsseite, Abschnitt „Darstellung") ein- und
ausblenden. Die Einstellung ist positiv formuliert und standardmäßig
aktiviert; sie wird wie alle App-Einstellungen lokal persistiert und
übersteht einen Neustart.

## Die drei Listen

| Liste | Inhalt | Datenquelle (Cache) |
|-------|--------|---------------------|
| Weiterschauen | Begonnene Filme und Episoden mit Fortschrittsbalken | `cached_lists`-Schlüssel `continue_watching` (Endpunkt `api/continue-watching`) |
| Favoriten | Als Favorit markierte Medien | `cached_favorites` (Endpunkt `api/favorites`) |
| Neu im Programm | Zuletzt hinzugefügte Inhalte | `cached_lists`-Schlüssel `recent` (Endpunkt `api/items/recent`) |

Jede Karte zeigt das Vorschaubild des Mediums; Einträge der
Weiterschauen-Liste tragen zusätzlich einen Fortschrittsbalken am
unteren Rand. Fehlt ein Poster (offline oder kein Bild hinterlegt),
zeigt die Karte den Titel — zusätzlich steht der Titel als zweizeilige
Beschriftung unter jeder Karte. Leere Abschnitte werden ausgeblendet;
sind alle Listen leer, erscheint ein Hinweis.

## Cache-First beim Start

- Beim App-Start werden die drei Listen **unmittelbar nach der
  Initialisierung der lokalen Ablage** aus dem Cache gelesen
  (`LoadHomeListsEffect` im Start-Workflow) — die Startseite ist damit
  auch ohne Serververbindung sofort gefüllt.
- Nach der Hintergrund-Synchronisation (Voll-Sync nach dem
  Verbindungsaufbau) werden die Listen aus dem aktualisierten Cache neu
  geladen und ins MVU-Modell übernommen.
- Die Übersetzung der Server-Rohdaten in anzeigefertige Einträge
  (`HomeMediaItem`: Titel, Medientyp, Poster-Bild-Id, Fortschritt)
  übernimmt der plattformneutrale `HomeListService`
  (`VideoPlayer.Mvu`). Einträge ohne identifizierbares Medium werden
  übersprungen, beschädigte Cache-Daten ergeben eine leere Teilliste —
  die Seite bleibt in jedem Fall anzeigbar.

## Aktualisierung durch Server-Ereignisse

Die in Schritt 6 eingeführte Echtzeit-Verbindung triggert die gezielte
Aktualisierung der betroffenen Caches; danach meldet der
Effekt-Handler die Änderung über `CachedListsChangedMessage` an den
MVU-Kreislauf zurück und die Startseite lädt ihren Cache-Stand neu:

- `ContinueWatchingUpdated` → nur die Weiterschauen-Liste.
- `FavoritesChanged` → nur die Favoriten.
- `NewVideosScanned` → Quellen und „Neu im Programm".

Eine manuelle Aktualisierung ist zu keinem Zeitpunkt nötig.

## Vorschaubilder

Poster werden über `api/pictures/{id}` geladen und vom
`PosterImageService` dreistufig aufgelöst: Arbeitsspeicher → lokale
Datei `{id}.poster` im **nicht backup-pflichtigen** Verzeichnis
`PreviewsDirectory` → Serverabruf. Die Dateiablage ist damit nach einem
Neustart ohne Serveranfrage verfügbar und belastet keine Backups.
Das Control `CachedPosterImage` bindet die Bild-Id und weist
nachgereichte Ergebnisse nur zu, wenn die Id noch stimmt.

## Auswahl und Detailseite

Das Antippen einer Karte dispatcht `MediaItemSelectedMessage`; das
MVU-Update erzeugt daraus — nur bei identifizierbarem Medium — den
`NavigateToMediaDetailsEffect`. Die Navigation führt per Shell-Route
`mediadetails?mediaType=…&id=…&title=…` auf die Platzhalter-Seite
`MediaDetailsPage`, die Titel und übersetzten Medientyp anzeigt. Die
vollständige Detailseite folgt in Entwicklungsschritt 8; die
Medien-Identität steht der Seite bereits als `MediaId`/`MediaType` zur
Verfügung.

## Offline-Verhalten

Im Offline-Modus zeigt die Startseite das bekannte gelbe Banner; die
gecachten Listen bleiben vollständig sichtbar und bedienbar. Fehlende
Vorschaubilder werden durch den Titel-Fallback ersetzt.
