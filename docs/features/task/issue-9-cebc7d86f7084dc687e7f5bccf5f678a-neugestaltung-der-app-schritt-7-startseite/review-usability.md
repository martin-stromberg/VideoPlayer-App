# Usability-Review

Betrachtete Änderungen (aus Endanwendersicht, ohne Kenntnis der
Implementierung):

- `VideoPlayer.Maui/Pages/HomePage.xaml` + `.xaml.cs` (drei
  Medienlisten, Offline-Banner, Leer-Hinweis, Serverstatus)
- `VideoPlayer.Maui/Controls/MediaCarousel.xaml` + `.xaml.cs`
  (horizontale Kartenliste, Poster, Fortschrittsbalken, Tap-Auswahl)
- `VideoPlayer.Maui/Controls/CachedPosterImage.cs` (Vorschaubilder)
- `VideoPlayer.Maui/Pages/MediaDetailsPage.xaml` + `.xaml.cs`
  (Platzhalter-Detailseite)
- `VideoPlayer.Maui/AppShell.xaml.cs` (Route `mediadetails`)

## Ergebnis

**Status:** Keine Befunde

## Prüfschritte / Benutzerflüsse

### Startseite öffnen (erneuter Start)

- Die drei Abschnitte „Weiterschauen", „Favoriten" und „Neu im
  Programm" zeigen beim Öffnen sofort den zuletzt bekannten Stand aus
  der lokalen Ablage — ohne sichtbare Wartezeit auf eine Serverantwort.
- Sobald aktuelle Serverdaten eintreffen, aktualisieren sich die Listen
  von selbst; es gibt keine Schaltfläche „Aktualisieren" und keinen
  Pull-Geste-Zwang.

### Karten-Darstellung

- Jede Karte zeigt das Vorschaubild (Poster, Seitenverhältnis 140×210,
  abgerundete Ecken mit Schatten) — optisch konsistent zur bekannten
  Darstellung der alten App.
- Fehlt ein Poster (kein Bild hinterlegt, offline ohne lokale Ablage),
  zeigt die Karte den Titel mittig auf dunkler Kachel; zusätzlich steht
  der Titel in einer zweizeiligen Beschriftung unter jeder Karte —
  Einträge bleiben damit immer benennbar und unterscheidbar.
- Einträge der Weiterschauen-Liste zeigen einen schmalen orangefarbenen
  Fortschrittsbalken am unteren Kartenrand; in den anderen Listen
  erscheint kein Balken.
- Leere Abschnitte werden komplett ausgeblendet — keine leeren
  Überschriften ohne Inhalt. Sind alle drei Listen leer, erscheint ein
  verständlicher Hinweis („Noch keine Inhalte vorhanden. Sobald ein
  Server verbunden ist, erscheinen hier deine Medien.").

### Auswahl eines Eintrags

- Ein Karten-Antippen öffnet die Detailseite des Mediums
  (Platzhalter bis Entwicklungsschritt 8) mit Titel und übersetzter
  Medientyp-Bezeichnung („Film", „Serie", „Staffel", „Episode",
  „Filmkollektion"); per Shell-Navigation steht ein Zurück-Pfeil zur
  Verfügung.
- Die Auswahl erfolgt ausschließlich über benannte Einträge mit
  Vorschaubild — nirgends müssen interne Kennungen eingegeben werden;
  die Medien-Id wird weder angezeigt noch als Eingabe verlangt.

### Offline-Verhalten

- Im Offline-Modus zeigt das bekannte gelbe Banner den Hinweis
  „Offline-Modus – zwischengespeicherte Inhalte werden angezeigt.";
  die gecachten Listen bleiben vollständig sichtbar und bedienbar.
- Fehlt ein Vorschaubild lokal, erscheint stattdessen der Titel —
  keine leeren Karten.

### Server-Ereignisse

- Änderungen am Server (Fortschritt, Favoriten, neue Inhalte)
  aktualisieren die betroffenen Listen automatisch — ohne dass der
  Anwender die Seite neu öffnen muss.

## Hinweise

- Horizontale Scrollleisten sind sichtbar (`Always`), damit die
  Bedienbarkeit auf Windows mit Maus gewährleistet ist.
