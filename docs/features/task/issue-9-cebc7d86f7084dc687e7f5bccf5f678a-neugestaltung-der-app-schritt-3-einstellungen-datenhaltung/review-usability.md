# Usability-Review

Betrachtete Änderungen (aus Endanwendersicht, ohne Kenntnis der
Implementierung):

- `VideoPlayer.Maui/Pages/SettingsPage.xaml` (ausgebaute Einstellungsseite)
- `VideoPlayer.Maui/Pages/SettingsPage.xaml.cs` (Verhalten: Befüllen,
  Speichern, Fehlerhinweis)

## Ergebnis

**Status:** Keine Befunde

## Prüfschritte

Die Seite „Einstellungen" ist über den gleichnamigen Eintrag im Seitenmenü
erreichbar. Beim Öffnen sind alle zuletzt gespeicherten Werte sichtbar.

### Verständlichkeit (nicht-technische Nutzer)

- Alle Bedienelemente tragen deutsche Klartext-Beschriftungen:
  „Automatische Downloads", „Anzahl vorzuladender Episoden",
  „Downloads nur im WLAN", „Speicherplatzkontingent für Downloads (GB)",
  „Gesehene Episoden automatisch löschen", „Auto-Fortsetzung",
  „Zeitfenster Auto-Fortsetzung (Sekunden Restlaufzeit)",
  „Als angesehen ab … Sekunden vor Ende".
- Es werden keine internen Bezeichner, Schlüssel oder IDs angezeigt oder
  abgefragt. Die `AutomationId`s sind ausschließlich Testanker und für
  Nutzer unsichtbar.
- Die Seite ist in zwei sinnvoll benannte Abschnitte („Downloads",
  „Wiedergabe") gegliedert und per `ScrollView` vollständig erreichbar.

### Bedienbarkeit

- Ein-/Aus-Optionen sind Schalter — keine Eingabe nötig, der Zustand ist
  auf einen Blick erkennbar.
- Zahlenwerte werden über numerische Tastatur eingegeben; die Platzhalter
  nennen die zulässigen Bereiche („1 – 10", „30 – 3600", „Standard: 5"),
  sodass die erwartete Eingabeform ohne Vorwissen erkennbar ist.
- Das Speicherplatzkontingent ist ausdrücklich als „GB" beschriftet —
  keine Umrechnung nötig.
- Änderungen werden ohne zusätzlichen „Speichern"-Schritt übernommen
  (Schalter sofort, Zahlenfelder beim Verlassen/Bestätigen); der aktuelle
  Stand bleibt nach dem Neustart der App erhalten.

### Fehlerverhalten

- Ungültige Zahleneingaben (z. B. Text) werden nicht übernommen; die
  Meldung „Bitte eine gültige Zahl eingeben." erscheint in Rot direkt auf
  der Seite und der letzte gültige Wert wird wieder angezeigt.
- Werte außerhalb der zulässigen Bereiche werden automatisch auf die
  Grenzen gesetzt — die App kann nicht in einen ungültigen Zustand
  gelangen.

### Konsistenz

- Navigation und Seitenaufbau entsprechen den übrigen Seiten der neuen
  App (Titel oben, deutsche Beschriftungen, gleiche Abstände).

## Befunde

—
