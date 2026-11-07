# Usability-Review — FlaUI-E2E-Tests (Issue 17, Schritt 3)

## Umfang der UI-Berührung

Die Implementierung ändert **keine produktive Oberfläche**:

- Kein XAML, keine Styles, keine neuen/verschobenen Bedienelemente.
- Die `/invoke`-Erweiterung (`itemIndex`) betrifft nur die Debug-
  Developer-API — sie wählt `CollectionView.SelectedItem` programmatisch,
  der Effekt in der UI entspricht exakt der Benutzerauswahl
  (`SelectionChanged` → `ServerSwitchRequestedMessage`).
- `ClientGate` (Token-Datei-Pfad) ist für Endanwender unsichtbar; das
  Standardprofil liest weiterhin denselben Pfad wie zuvor.

## Sichtbares Verhalten der Tests gegenüber Bedienung durch Nutzer

- Texteingaben laufen über das UIA-Value-/Typing-Pattern — dieselbe
  `TextChanged`-Kette wie Tastatureingaben (verifiziert: Adresse,
  Pairing-Code, Benutzername mit `@`, Passwort).
- Buttons werden per Invoke-Pattern ausgelöst (Tastatur-/Maus-Äquivalent,
  kein Koordinatenklick).
- Die Flyout-Navigation nutzt das `SelectionItem`-Pattern des
  NavigationView-Eintrags — äquivalent zum Klick auf den Menüeintrag.

## UX-Befund (kein neuer Defekt, dokumentiert)

Beim Serverwechsel bleibt die geschobene Route `serversetup` im
Navigationsstack des Einstellungen-Tabs; öffnet der Nutzer später
„Einstellungen", landet er auf der Serververwaltung statt auf den
Einstellungen (einmal „Zurück" nötig). Das ist Standard-Shell-
Tab-Stack-Verhalten und wurde vom #15-Fix bewusst nicht verändert —
im Test akzeptiert, zur Bewertung in `review-code.md` vermerkt.

## Fazit

Keine Usability-Änderung an der Produktiv-App. Die E2E-Tests prüfen
die realen Bedienwege (Eingabe, Buttons, Flyout, Listen) so, wie ein
Nutzer sie ausführt; der dokumentierte Navigationsrest ist ein
bekanntes Shell-Verhalten, keine Regression.
