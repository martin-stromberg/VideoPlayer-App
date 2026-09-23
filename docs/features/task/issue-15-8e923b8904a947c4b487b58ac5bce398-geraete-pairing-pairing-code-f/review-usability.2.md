# Usability-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### ServerSetupPage.xaml (Pairing-Sektion) / PairingCode.cs / AppUpdate.cs

- **Erreichbarkeit** – Die Code-Länge ist hart auf 8 Zeichen verdrahtet: `MaxLength="8"` am Eingabefeld (`PairingCodeEntry`), `PairingCode.RequiredLength = 8` in der lokalen Formatprüfung sowie die Texte "8-stelligen Einmal-Code" (Hinweis) und "Bitte den 8-stelligen Pairing-Code eingeben." (`PairingCodeFormatError`). Die Anforderung sieht die Code-Länge jedoch als serverseitige Option vor (`Pairing:CodeLength`, Default 8, min. 4). Stellt ein Serverbetreiber z. B. 6 oder 10 Zeichen ein, kann die Anwenderin den ausgestellten Code nie erfolgreich eingeben: Bei mehr als 8 Zeichen schneidet das Feld die Eingabe ab, bei weniger als 8 schlägt die lokale Formatprüfung fehl – in beiden Fällen erscheint der (dann falsche) Hinweis "Bitte den 8-stelligen Pairing-Code eingeben.", obwohl der ausgestellte Code korrekt abgetippt wurde. Die geforderte Aufgabe "Code eingeben → gekoppelt" ist in dieser Konfiguration nicht erfüllbar.

  Empfehlung: `MaxLength` entfernen oder großzügig wählen (z. B. 32), die lokale Prüfung auf das Alphabet und eine plausible Längen-Spanne (statt exakt 8) lockern – oder die Formatprüfung ganz dem Server überlassen (HTTP 401 deckt ungültige Codes bereits ab). Hinweis- und Fehlertext so formulieren, dass sie keine feste Stellenzahl behaupten, z. B. "Den Einmal-Code erstellst du in der Verwaltung deines Servers (Bereich „Geräte")." / "Bitte den Pairing-Code vollständig eingeben."

### ServerSetupPage.xaml (Pairing-Sektion) / AppUpdate.cs (PairingCodeFormatError)

- **Erreichbarkeit** – Das Code-Alphabet lässt bewusst keine leicht verwechselbaren Zeichen zu (kein 0, O, 1, I, L). Tippt eine Anwenderin einen 8-stelligen Code ab und verwechselt dabei ein Zeichen (z. B. "0" statt "Q"), meldet die lokale Prüfung `PairingCodeFormatError` = "Bitte den 8-stelligen Pairing-Code eingeben." – obwohl sie bereits 8 Zeichen eingegeben hat. Der Text erklärt nicht, dass ein Zeichen ungültig ist, und gibt keinen Hinweis, worauf zu achten ist; die Anwenderin kann den Fehler nicht selbst einordnen.

  Empfehlung: Fehlertext um den Zeichen-Hinweis ergänzen, z. B. "Der Code ist nicht vollständig oder enthält ein ungültiges Zeichen. Der Code besteht aus 8 Zeichen – ohne 0, O, 1 und I. Bitte die Eingabe prüfen." (an die tatsächlich geprüfte Regel anpassen).

### ServerSetupPage.xaml (PairingCodeEntry)

- **Erreichbarkeit** – Das Code-Feld verwendet `Keyboard="Text"` ohne `IsSpellCheckEnabled="False"` / `IsTextPredictionEnabled="False"`. Auf mobilen Tastaturen sind Rechtschreibprüfung und Wortvorschläge damit aktiv; bei einem zufälligen 8-Zeichen-Code können Autokorrektur oder ein antippbarer Wortvorschlag die Eingabe unbemerkt verändern (z. B. beim Leerzeichen/Get-Bestätigen). Die Anwenderin sendet dann einen anderen Code als abgetippt und erhält einen "ungültigen Code"-Fehler, den sie sich nicht erklären kann.

  Empfehlung: Am `PairingCodeEntry` `IsSpellCheckEnabled="False"` und `IsTextPredictionEnabled="False"` setzen (Muster: Felder für nicht-sprachliche Eingaben wie Codes).

### AppUpdate.cs (PairingNotSupportedError)

- **Erreichbarkeit** – Bei einem Server ohne Pairing-Endpunkt (HTTP 404) lautet der Fehlertext "Der Server unterstützt kein Geräte-Pairing — bitte den Server aktualisieren." Eine nicht-technische Anwenderin weiß in der Regel nicht, wie man "den Server aktualisiert", und bekommt keinen Hinweis, an wen sie sich wenden kann; der Fehler ist ein Endzustand ohne bedienbare Alternative.

  Empfehlung: Text um eine handhabbare Anlaufstelle ergänzen, z. B. "Dieser Server unterstützt das Geräte-Koppeln noch nicht. Bitte den Server aktualisieren oder dich an die Person wenden, die den Server eingerichtet hat."

## Geprüfte Interaktionen

Liste der aus der Anforderung geprüften Benutzerinteraktionen:
- Serveradresse manuell eingeben und "Verbinden" → unauffällig (unverändertes Muster)
- Server im Netzwerk suchen / aus gefundenen oder bekannten Servern wählen → unauffällig (unverändert)
- Pairing-Code eingeben und "Koppeln" nach erfolgreicher Serverprüfung → Befund vorhanden (fixe 8-Stellen-Annahme, unklarer Formatfehler, aktive Autokorrektur)
- Fehlertexte der Pairing-Eingabe verstehen und erneut versuchen (ungültiger/abgelaufener/verbrauchter Code, 429-Sperre, 404 ohne Endpunkt) → Befund vorhanden (Formatfehler ohne Zeichen-Hinweis, 404-Text ohne Anlaufstelle); 401- und 429-Texte unauffällig
- Re-Pairing nach Widerruf des Geräte-Zugangs (`DeviceRevokedError`, Rücksprung auf die Einrichtungsseite) → unauffällig (verständlicher Text, Hinweis zur Code-Herkunft bleibt sichtbar)
- Hinweis "woher kommt der Code?" → unauffällig (Befund der Vorprüfung behoben: Verweis auf Server-Verwaltung, Bereich „Geräte")

## Geprüfte Dateien

Liste aller geprüften UI-Dateien:
- `VideoPlayer.Maui/Pages/ServerSetupPage.xaml`
- `VideoPlayer.Maui/Pages/ServerSetupPage.xaml.cs`
- `VideoPlayer.Mvu/AppUpdate.cs` (UI-wirksame Zustände und Fehlertexte)
- `VideoPlayer.Mvu/PairingCode.cs` (lokale Formatprüfung, wirkt direkt auf die Eingabe)
- `VideoPlayer.Mvu/PairingStatus.cs` / `VideoPlayer.Mvu/AppModel.cs` (Pairing-Zustand, soweit UI-wirksam)
