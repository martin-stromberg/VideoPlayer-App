# Usability-Review

Betrachtete Änderungen (aus Endanwendersicht, ohne Kenntnis der
Implementierung):

- `VideoPlayer.Maui/Pages/FavoritesPage.xaml` + `.xaml.cs`
  (Favoritenliste mit Poster/Titel/Typ, Entfernen-Aktion,
  Offline-/Fehler-/Leerzustände)
- `VideoPlayer.Maui/Pages/ProfilePage.xaml` + `.xaml.cs`
  (Benutzerinformationen, lokales Profil, Abmelden)

## Ergebnis

**Status:** Keine Befunde (mit Hinweisen)

## Prüfschritte / Benutzerflüsse

### Menüseite „Favoriten" öffnen (online)

- Die Favoriten des angemeldeten Server-Benutzers erscheinen als
  Kartenliste mit Poster, Titel und Medientyp-Bezeichnung
  („Film", „Filmkollektion", „Serie", „Staffel", „Episode") — die
  Darstellung folgt der Favoriten-Liste der Webanwendung; fehlt ein
  Poster, bleibt der Titel lesbar (Titel-Fallback in der Kachel).
- Während des Ladens läuft ein Ladeindikator; bei Fehlschlag erscheint
  eine verständliche Meldung mit Schaltfläche **„Erneut laden"**.
- Eine erfolgreiche, aber leere Liste zeigt „Keine Favoriten vorhanden.
  Markiere Medien auf der Detailseite mit dem Stern." — ein klarer
  Leerzustand mit Handlungsanleitung, kein Fehler.

### Favorit entfernen

- Jede Zeile trägt rechts eine gut erreichbare Schaltfläche
  **„Entfernen"** — dieselbe Aktion wie die Kontextaktion der
  Webanwendung.
- Während des Entfernens ist die Schaltfläche des betroffenen Eintrags
  deaktiviert (kein Doppelaufruf); bei Erfolg verschwindet der Eintrag
  aus der Liste — und aus der Startseiten-Favoritenliste.
- Scheitert das Entfernen, bleibt der Eintrag stehen und eine
  Fehlermeldung erscheint oberhalb der Liste — die Liste bleibt
  vollständig bedienbar.

### Favoriten offline

- Das bekannte gelbe Banner zeigt „Offline – es wird die gespeicherte
  Favoritenliste angezeigt; Entfernen ist ohne Serververbindung nicht
  möglich." — die zwischengespeicherte Liste bleibt sichtbar.
- Die „Entfernen"-Schaltflächen sind offline deaktiviert — die Sperre
  ist sichtbar, der Grund steht im Banner; Auswahl/Detail-Navigation
  bleibt möglich.

### Eintrag öffnen

- Tap auf eine Karte öffnet die Detailseite des Mediums — bei
  Episoden/Staffeln mit vorgewähltem Serienkontext wie von der
  Startseite bekannt; der Zurück-Pfeil der Shell führt zurück.

### Menüseite „Profil"

- Die Seite zeigt zwei Karten: „Angemeldeter Benutzer" (Benutzername,
  E-Mail) und „Lokale Installation" (Profilname, Serveradresse) —
  nicht ermittelbare Werte erscheinen als „—" statt leer.
- Die Schaltfläche **„Abmelden"** beendet die Sitzung: die App kehrt
  zur Anmeldemaske zurück, die Eingabefelder sind leer (keine
  gespeicherte Kennung mehr), Menüseiten sind bis zur erneuten
  Anmeldung gesperrt.

## Hinweise (keine Befunde)

- Die Abmeldung erfolgt ohne Rückfrage-Dialog — konsistent mit den
  übrigen direkten Aktionen der App; der Vorgang ist verlustfrei
  umkehrbar (erneute Anmeldung mit denselben Zugangsdaten).
- Während eines Entfernen-Laufs gibt es keinen gesonderten Spinner je
  Zeile — die deaktivierte Schaltfläche signalisiert den Lauf
  ausreichend (einzelnes Element, kurzer Vorgang).
