# Usability-Review

Betrachtete Änderungen (aus Endanwendersicht, ohne Kenntnis der
Implementierung):

- `VideoPlayer.Maui/Pages/SourcesPage.xaml` + `.xaml.cs` (Quellenliste,
  Icons, Offline-/Fehler-/Leerzustände)
- `VideoPlayer.Maui/Pages/SourceOverviewPage.xaml` + `.xaml.cs`
  (Genre-Leiste, Suche, Medien-Raster, Nachladen)
- `VideoPlayer.Maui/Pages/MediaDetailsPage.xaml` + `.xaml.cs`
  (Film-/Episoden-Basisdetails, Serienansicht mit Staffel-Picker,
  Kollektionsansicht, Favoriten-Stern)
- `VideoPlayer.Maui/Controls/CachedSourceIconImage.cs` (Quellen-Icons)
- `VideoPlayer.Maui/AppShell.xaml.cs` (Route `sourceoverview`,
  erweiterte Route `mediadetails`)

## Ergebnis

**Status:** Keine Befunde (mit Hinweisen)

## Prüfschritte / Benutzerflüsse

### Menüseite „Quellen" öffnen

- Die vom Server freigegebenen Medienbereiche erscheinen als Liste aus
  Karten mit Icon und Name — wie die Quellenübersicht der alten App;
  fehlt ein Icon, bleibt der Name lesbar (keine leeren Karten).
- Während des Ladens läuft ein Ladeindikator; bei Fehlschlag erscheint
  eine verständliche Meldung mit Schaltfläche „Erneut laden".
- Sind keine Quellen freigegeben, steht „Keine Quellen freigegeben." —
  ein klarer Leerzustand, kein Fehler.
- **Offline:** statt einer Liste erscheint das bekannte gelbe Banner
  mit dem Hinweis „Offline – die Quellenliste steht ohne Verbindung zum
  Server nicht zur Verfügung." — keine veraltete oder leere Liste wird
  als aktueller Stand ausgegeben.

### Quelle durchblättern

- Tap auf eine Quelle öffnet die Übersichtsseite (Titel = Quellenname);
  per Shell-Navigation steht ein Zurück-Pfeil zur Verfügung.
- Oben steht ein Suchfeld; Abschicken über die Suchen-Taste filtert die
  Liste serverseitig. Darunter die horizontale Genre-Leiste mit „Alle"
  als erstem Eintrag — das gewählte Genre ist farblich hervorgehoben
  (Akzentfarbe, wie in der alten App).
- Das Medien-Raster (3 Spalten) zeigt Poster und zweizeilige Titel;
  fehlt ein Poster, bleibt der Titel lesbar.
- Beim Scrollen ans Ende werden automatisch weitere Einträge
  nachgeladen; ein Fußzeilen-Indikator signalisiert den Lauf.
- Scheitert das Nachladen, erscheint in der Fußzeile „Fehler beim Laden
  weiterer Einträge" mit „Erneut versuchen" — die bereits geladenen
  Einträge bleiben stehen. Scheitert das erste Laden komplett, erscheint
  die Meldung mittig mit Retry; eine erfolgreiche Suche ohne Treffer
  zeigt „Keine Einträge gefunden.".
- Rückkehr aus der Detailseite stellt den Zustand wieder her —
  Genre-Filter, Suchtext und Listenposition bleiben erhalten (kein
  sichtbarer Neulade-Effekt).

### Detailseiten

- **Serie:** Kopfbereich mit Bild, Titel, Medientyp und Jahr, Genres und
  Favoriten-Stern; darunter die Handlung, ein Staffel-`Picker` und die
  Episodenliste der gewählten Staffel mit Bild, „Nr. Titel" und
  Kurzbeschreibung — Aufbau wie die `TVShowDetailsPage` der alten App.
  Episoden, über die navigiert wurde (z. B. aus „Weiterschauen"), sind
  vorgewählt und markiert.
- **Filmkollektion:** Titel und Metadaten der Sammlung, Raster der
  enthaltenen Filme; Tap auf einen Film öffnet dessen Detailseite.
- **Film/Episode:** Bild, Titel, Typ/Jahr, Genres und Handlung.
- Einträge werden ausschließlich über benannte Karten/Listeneinträge
  ausgewählt — interne Kennungen werden nirgends angezeigt oder als
  Eingabe verlangt.

### Favoriten-Stern

- Der Stern `☆`/`★` schaltet die Markierung des angezeigten Mediums um;
  während der Serveranfrage ist er deaktiviert (kein Doppel-Tap).
- Bei Erfolg übernimmt der Stern den Server-Zustand; die
  Startseiten-Liste „Favoriten" aktualisiert sich ohne manuelle
  Aktualisierung.
- Bei Fehlschlag bleibt der Stern unverändert und ein Fehlerhinweis
  erscheint — kein stilles Umschalten.

## Hinweise

- Die Episodenliste ist innerhalb des Seiten-`ScrollView` eingebettet —
  auf kleinen Bildschirmen mit sehr langen Staffeln kann die
  Scrollverschachtelung die Bedienbarkeit leicht beeinträchtigen
  (vertretbar, da der Altbestand dieselbe Struktur nutzt; ohne
  UI-Automatisierung nicht gerätegeprüft).
- Der Staffel-`Picker` zeigt den systemeigenen Auswahldialog —
  plattformkonform auf iOS und Windows.
- Ohne UI-End-to-End-Automatisierung erfolgte die Prüfung als
  statische Sichtung der Oberflächen-Definitionen und der MVU-
  Zustandsübergänge (Einschränkung dokumentiert in `test-results.md`).
