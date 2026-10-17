# Usability-Review: Startseite — Hero-Kopfbereich (Schritt 5)

Endanwender-Perspektive (ohne Plan-/Code-Kenntnis). Datum: 2026-09-22 · Iteration 1

## Beobachtungen

- **Auffindbarkeit der Einstellung:** Der Schalter „Kopfbereich anzeigen"
  liegt auf der Einstellungsseite im neuen Abschnitt „Darstellung" —
  thematisch passend, vor „Downloads". Erreichbarkeit wie alle anderen
  Einstellungen über das Seitenmenü. ✅
- **Verständlichkeit / positive Formulierung:** „Kopfbereich anzeigen"
  ist positiv formuliert (Schalter an = Bereich sichtbar). Kein
  technischer Begriff, keine internen IDs, kein Fachjargon. Der Begriff
  „Kopfbereich" ist als Seitenkopf verständlich. ✅
- **Bedienbarkeit ohne Fachwissen:** Ein Tap auf den Schalter blendet den
  Bereich sofort aus bzw. wieder ein; der Wert bleibt nach dem Neustart
  erhalten (über denselben Persistenzpfad wie alle übrigen Schalter). ✅
- **Standardverhalten:** Neue Installationen und bisherige
  Installationen ohne gespeicherten Schlüssel zeigen den Kopfbereich —
  entspricht der Kundenvorgabe „standardmäßig aktiviert". ✅
- **Hero-Darstellung:** Der Bereich steht sichtbar oberhalb der Listen,
  vollbreit, mit dunklem Verlauf, Chip „PRIVATE MEDIA LIBRARY", Titel
  (Servername) und kurzem Beschreibungstext. Für einen Anwender wirkt er
  wie ein gestalteter Kopf der Startseite — wie im Web. ✅
- **Offline-/Fehlerfall:** Ohne Bild zeigt der Bereich nur Verlauf und
  Texte — keine Fehlermeldung, kein leeres Loch, kein Layout-Sprung
  (die Höhe ist bildunabhängig). Offline bleibt die Seite stabil. ✅

## Befunde

Keine. Hinweis (kein Befund): Der Titel zeigt den Anzeigenamen des
aktiven Servers (im Web der serverseitig konfigurierte Anwendungstitel,
der der App-API nicht zur Verfügung steht) — semantisch gleichwertig.
