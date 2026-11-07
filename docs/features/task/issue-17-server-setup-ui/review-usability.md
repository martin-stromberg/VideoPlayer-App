# Usability-Review — Modernisierte `ServerSetupPage` (Issue 17, Schritt 4)

Geprüft an der tatsächlich geänderten UI (`ServerSetupPage.xaml`,
`.xaml.cs`) und den Live-Screenshots `live-verification/*.png`
(2026-09-24). Grundlage ist die ursprüngliche Anforderung
(`requirement.md`), nicht der Plan.

## Leseführung und Orientierung

- Die Seite folgt jetzt einer klaren Hierarchie: Kopf (was ist das
  für eine Seite?) → Listen (welche Server gibt es schon?) →
  Aktionen im Fuß (neu verbinden / Gerät koppeln). Vorher standen
  alle Elemente als flache Text-/Button-Folge untereinander.
- Der Kicker „SERVERVERBINDUNG" + Titel „VideoPlayer-Server" +
  Hinweissatz beantwortet die Orientierungsfrage sofort; der
  Hinweis „Die App verbindet sich mit einem VideoPlayer-Server im
  eigenen Netzwerk." ist sachlich und fachlich korrekt.
- Jede Aktion ist eine Karte — das entspricht der Kartenlogik der
  übrigen App (Startseite, Einstellungen) und senkt die
  Lernkurve beim Wechsel zwischen den Seiten.

## Zustandsverständlichkeit

- **Erreichbarkeit:** Das Status-Badge „Server: erreichbar"/
  „… nicht erreichbar" sitzt direkt neben der Serveradresse in der
  Aktiver-Server-Karte — räumliche Nähe macht die Zuordnung
  eindeutig. Der farbige Punkt (blau/rot/gedimmt) ergänzt den
  Text, trägt die Information aber nicht allein (Farbenblindheit
  berücksichtigt).
- **Laufzustände:** Jede Aktion nennt ihre Phase in Textform:
  „Suche läuft …", „Verbindung wird geprüft …", „Verbindung wird
  hergestellt …" (Unterscheidung Prüfung/Auto-Anmeldung ist
  verständlich formuliert), „Kopplung läuft …". Die drehenden
  Indikatoren allein hätten nicht erklärt, *was* gerade passiert.
- **Fehler:** Adress-/Erreichbarkeitsfehler erscheinen weiterhin im
  `ErrorLabel` innerhalb der Verbinden-Karte; Pairing-Fehler in
  `PairingErrorLabel` innerhalb der Pairing-Karte — Fehler sitzen
  am Ort der zugehörigen Eingabe.

## Führung im Pairing-Schritt

- Die Pairing-Karte ist visuell hervorgehoben (`VpSecondary`-Rand,
  „NÄCHSTER SCHRITT"-Badge) — der Nutzer erkennt sie als den
  geforderten nächsten Schritt, ohne dass sie wie ein Fehler wirkt.
- Der Führungstext nennt konkret den Weg: „Öffne die Verwaltung
  deines Servers (Profilseite „Geräte"), erstelle dort einen
  Einmal-Code und gib ihn hier ein." — Handlungsanleitung in einem
  Satz, ohne interne Begriffe (kein „Token", „Gate", „Exchange").
- Der Placeholder „Pairing-Code" und die deaktivierte Rechtschreib-
  prüfung/Textvorhersage passen zum Eingabetyp.

## Terminologie und interne Details

- Keine internen IDs, GUIDs, Protokoll- oder Endpunktnamen in den
  sichtbaren Texten; die Serveradresse selbst ist eine legitime,
  nutzerrelevante Angabe.
- Deutsch, einfache Sätze, konsistente Begriffe („Server",
  „verbinden", „koppeln") — deckt sich mit dem Sprachgebrauch der
  übrigen Seiten und der Hilfe.

## Zugänglichkeit und Bedienbarkeit

- Alle Interaktionselemente behalten ihre AutomationIds; neue
  Statuselemente haben eigene Ids — Screenreader/UIA können die
  Zustände vorlesen.
- Statusfarben kommen ausschließlich aus `Vp*`-Tokens; der
  Dark-only-Charakter ist gewahrt (Screenshot-Analyse: ~97–99 %
  dunkle Pixel, keine hellen Flächen).
- Fuß- und Listenbereich scrollen getrennt — bei sichtbarer
  Pairing-Karte bleibt die Code-Eingabe auch in kleinen Fenstern
  erreichbar.

## Befunde / Hinweise (kein Defekt)

- Karten ohne eigenen AutomationPeer erscheinen nicht im
  Windows-UIA-Baum; die Karten-Sichtbarkeit wird daher über die
  Developer-API `/tree` geprüft — für Endanwender ohne Belang.
- Der Pairing-Code-Alphabet-Hinweis (kein 0/O/1/I/L) erscheint erst
  als Fehlertext bei falscher Eingabe; ein präventiver Hinweis im
  Führungstext wäre eine mögliche spätere Ergänzung (für Schritt 5b
  ohnehin geplant, da QR/Ticket denselben Code verpackt).

## Fazit

Die modernisierte Seite erfüllt die Anforderung: klare Hierarchie,
verständliche Zustandsrückmeldung in Textform, geführter
Pairing-Schritt, keine internen Begriffe, konsistente Sprache und
Kartenlogik. Keine Usability-Blocker gefunden.
