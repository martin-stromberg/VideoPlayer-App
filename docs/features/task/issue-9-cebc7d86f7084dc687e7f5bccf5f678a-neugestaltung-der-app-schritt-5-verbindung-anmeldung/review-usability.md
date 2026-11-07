# Usability-Review

Betrachtete Änderungen (aus Endanwendersicht, ohne Kenntnis der
Implementierung):

- `VideoPlayer.Maui/Pages/LoginPage.xaml` + `.xaml.cs` (neue Anmeldemaske)
- `VideoPlayer.Maui/Pages/LoadingPage.xaml` (Start-Ladeanzeige mit
  Anmeldephase)
- `VideoPlayer.Maui/Pages/HomePage.xaml` + `.xaml.cs` (Offline-Hinweis)
- `VideoPlayer.Maui/Pages/ServerSetupPage` (Wiederverwendung als
  Serverauswahl bei Nichterreichbarkeit ohne Offline-Daten)
- `VideoPlayer.Mvu/AppUpdate.cs` (zustandsgesteuerte Navigation,
  Fehlertexte)
- `VideoPlayer.Maui/Services/ShellAppEffectHandler.cs` (Navigation zur
  Anmeldemaske, Sitzungsablauf)

## Ergebnis

**Status:** Keine Befunde

## Prüfschritte / Benutzerflüsse

### App-Start (verständliche Ladeanzeige)

- Die App zeigt beim Start einen Fortschrittsindikator mit dem Titel
  „VideoPlayer wird geladen" und einem Statuszeilentext. Die Textzeile
  wechselt von „Serververbindung wird geprüft …" zu „Anmeldung beim Server
  läuft …" — der Nutzer erkennt jederzeit, was gerade passiert, und es gibt
  keinen unerklärten Leerzustand.
- Jeder Fehlerpfad (Server nicht erreichbar, Anmeldung schlägt fehl,
  Offline-Daten-Prüfung schlägt fehl) endet in einem sichtbaren Zustand —
  die App kann nicht dauerhaft auf der Ladeanzeige hängen bleiben
  (defensive Fallbacks im Effekt-Handler).

### Anmeldemaske

- Die Maske trägt den Titel „Anmeldung" mit dem erklärenden Satz „Bitte mit
  den Zugangsdaten des VideoPlayer-Servers anmelden." — erfragt werden nur
  Benutzername und Passwort, also die dem Anwender natürlich bekannten
  Angaben. Keine internen IDs, GUIDs, Datenbank- oder Tokenschlüssel.
- Der bekannte Benutzername ist vorbefüllt (u. a. nach einer
  Backup-Wiederherstellung); er wird nur einmalig gesetzt und
  überschreibt keine laufende Eingabe.
- Das Passwortfeld ist geschützt (`IsPassword`), der aktive Server wird
  klein als Kontextzeile angezeigt („Server: http://…").
- „Anmelden" mit leeren Feldern erzeugt den Inline-Hinweis „Bitte
  Benutzername und Passwort eingeben." — kein Netzwerkaufruf, kein
  Modal-Dialog.
- Während des Anmeldeversuchs laufen der Indikator und alle Eingaben sind
  gesperrt (kein Doppel-Absenden); nach einem Fehlschlag ist die Maske
  sofort wieder editierbar.
- Fehlermeldungen sind unterscheidbar und verständlich: „Anmeldung
  fehlgeschlagen. Bitte Benutzername und Passwort prüfen." (falsche
  Zugangsdaten), „Der Server ist nicht erreichbar. Bitte später erneut
  versuchen." (Netz), „Anmeldung fehlgeschlagen. Bitte erneut versuchen."
  (sonstig) sowie „Die Anmeldung ist abgelaufen. Bitte erneut anmelden."
  (Sitzungsablauf zur Laufzeit).
- „Anderen Server einrichten" führt ohne technische Begriffe zurück zur
  Serverauswahl — dort stehen bekannte Server, Netzwerk-Suche und
  manuelle Eingabe bereit (aus Schritt 4, bereits usability-geprüft).

### Server nicht erreichbar

- Mit lokalen Offline-Daten startet die App in den Offline-Betrieb: Die
  Startseite zeigt einen gut sichtbaren gelben Hinweis „Offline-Modus: Der
  Server ist nicht erreichbar. Es werden lokal gespeicherte Daten
  angezeigt. Über „Einstellungen → Serververwaltung" kann ein anderer
  Server gewählt werden." — der Hinweis benennt Zustand, Folge und den
  Ausweg.
- Ohne Offline-Daten landet der Nutzer auf der bekannten
  Server-Einrichtungsseite (Liste bekannter Server, Netzwerk-Suche,
  manuelle Eingabe) — er kann einen anderen Server wählen oder die
  Adresse korrigieren, ohne die App neu starten zu müssen.

### Zustandsgesteuerte Navigation

- Solange die Anmeldung oder die Servereinrichtung aussteht, führen
  Menüeinträge nicht in einen unbrauchbaren Zustand — die App kehrt auf
  die ausstehende Workflow-Seite zurück (Guard in `AppUpdate`).
- Nach erfolgreicher Anmeldung (manuell oder automatisch) landet der
  Nutzer auf der Startseite; das Menü ist dann vollständig nutzbar.

## Feststellungen

- Keine Eingabe interner Bezeichner erforderlich — Benutzername, Passwort
  und Serverauswahl aus Listen sind die einzigen Nutzerangaben.
- Alle Bedienelemente der neuen Anmeldemaske tragen `AutomationId`s
  (Testbarkeit/Bedienbarkeit), Fehlertexte kommen aus den Konstanten in
  `AppUpdate` (eindeutig, übersetzbar-konsistent).
- Einschränkung (akzeptiert): Echte UI-E2E-Ausführung steht nicht zur
  Verfügung (kein UI-Automatisierungs-Framework im Repository); die
  Prüfung erfolgte über Code-Inspektion der Oberflächen und die
  Message-Sequenztests — dokumentiert in `inventory/tests.md` und
  `test-results.md`.
