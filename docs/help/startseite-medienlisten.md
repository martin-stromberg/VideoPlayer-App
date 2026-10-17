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

## Auswahl: Detailansicht mit Wiedergabe aus der Weiterschauen-Liste

Das Antippen einer Karte führt je nach Liste unterschiedlich weit:

- **„Weiterschauen":** Abspielbare Einträge (Filme und Episoden)
  öffnen die **Titel-Detailansicht und starten darüber die
  Wiedergabe** an der gespeicherten Fortsetzungsposition — Parität
  zur Serveranwendung
  (`ContinueWatchingItemSelectedMessage` → `NavigateToMediaDetailsEffect`
  vor dem gemeinsamen Wiedergabe-Start). Das Schließen des Players
  kehrt damit zur geöffneten Detailansicht zurück statt zur
  Startliste — Titelinformationen bleiben ohne Umweg über die
  Quellenansicht einsehbar. Einträge, die nicht direkt abspielbar
  sind (Serie, Staffel, Kollektion), öffnen wie bisher nur die
  Detailansicht; Einträge mit Playlist-Bezug starten weiterhin direkt
  im Playlist-Kontext.
- **„Favoriten" und „Neu im Programm":** Ein Antippen öffnet
  weiterhin ausschließlich die Detailseite
  (`MediaItemSelectedMessage` → `NavigateToMediaDetailsEffect` per
  Shell-Route `mediadetails?mediaType=…&id=…&title=…`).

Die Fortsetzungsposition kommt über denselben Auflösungspfad wie
beim Wiedergabestart von der Detailseite — der Eintrag muss dafür
keine weiteren Angaben mitbringen.

**Offline-Sonderfall:** Im Offline-Modus gilt dieselbe
Detailansicht-mit-Wiedergabe für Filme und für Episoden mit
Serien-Kennung — die Offline-Detailansicht baut daraus den
Serienkontext aus der gecachten Serienstruktur auf. Eine Episode
ohne Serien-Kennung besitzt keinen nutzbaren Detail-Kontext und
behält die direkte Wiedergabe ohne Detailansicht; nicht auswählbare
Platzhalter fehlender Episoden bleiben ohne Aktion.

## Kontextmenü der Weiterschauen-Liste

Ein **2-Sekunden-Long-Press** auf einen Eintrag der
Weiterschauen-Liste öffnet ein Kontextmenü mit den Aktionen
**„Ausblenden"** und **„Überspringen"** sowie **„Abbrechen"** — analog
zur Serveranwendung. Der kurze Tap bleibt davon unberührt und öffnet
weiterhin die Detailansicht mit Wiedergabe bzw. nur die Detailansicht.

- **„Ausblenden"** ruft den Server-Endpunkt
  `api/continue-watching/hide` auf, **„Überspringen"**
  `api/continue-watching/skip` — danach wird die Cache-Kopie der
  Liste autoritativ vom Serverstand erneuert
  (`SyncContinueWatchingAsync` + `CachedListsChangedMessage`) und der
  Eintrag verschwindet gemäß Server-Antwort aus der Anzeige.
- **Offline** (ohne angemeldeten Client) wird der Eintrag lokal aus
  dem `cached_lists`-Eintrag `continue_watching` entfernt; die Liste
  aktualisiert sich über dieselbe Cache-Geändert-Kette.
- Technisch trägt das `MediaCarousel` einen
  `PointerGestureRecognizer` mit 2-s-Timer, der nur bei gesetztem
  `LongPressEnabled` aktiv ist — das Kontextmenü bleibt auf die
  Weiterschauen-Liste beschränkt; „Favoriten" und „Neu im Programm"
  bieten kein Menü.

Damit das **Folgevideo** (Auto-Fortsetzung bzw. Fehler-Rückfall auf
die nächste Episode) auch beim Start aus der Weiterschauen-Liste
funktioniert, lädt die App den Serienkontext unabhängig vom
Detail-Ladevorgang nach: online über `api/items/tvshow/{showId}` (die
Serien-Id steckt als Kontext-Hinweis im Weiterschauen-Eintrag),
offline aus der gecachten Serienstruktur (`ShowDetailsCache`). Die Folge-Episoden werden dem
Player nachträglich zugeordnet, solange derselbe Titel läuft — ein
Ladefehlschlag entfällt die Fortsetzungsfunktion still, die Wiedergabe
bleibt davon unberührt.

## Offline-Verhalten

Im Offline-Modus zeigt die Startseite das bekannte gelbe Banner; die
gecachten Listen bleiben vollständig sichtbar und bedienbar. Fehlende
Vorschaubilder werden durch den Titel-Fallback ersetzt.
