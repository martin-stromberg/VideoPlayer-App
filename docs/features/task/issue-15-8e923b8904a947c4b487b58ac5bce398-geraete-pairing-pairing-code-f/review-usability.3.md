# Usability-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### ServerSetupPage.xaml (Server-Einrichtung / Abschnitt „Gerät koppeln")

- **Erreichbarkeit** — Das Feld `PairingCodeEntry` begrenzt die Eingabe hart auf `MaxLength="32"` und schneidet längere Eingaben ohne jede Rückmeldung ab. Die Codelänge ist serverseitig konfigurierbar (`Pairing:CodeLength`, Mindestwert 4, kein dokumentiertes Maximum). Ist ein Server mit Codes über 32 Zeichen konfiguriert, tippt die Anwenderin den korrekt angezeigten Code vollständig ein, die App sendet aber nur die ersten 32 Zeichen — der Server antwortet mit „Code ungültig, abgelaufen oder bereits verwendet". Die Anwenderin kann die geforderte Aufgabe (Gerät koppeln) dann nicht erledigen und erhält eine irreführende Fehlermeldung, obwohl sie alles richtig gemacht hat.

  Empfehlung: `MaxLength` entfernen oder deutlich oberhalb jeder plausiblen Server-Konfiguration ansetzen; alternativ die lokale Formatprüfung in `PairingCode.TryNormalize` so ausgeben, dass eine zu lange Eingabe einen verständlichen Hinweis statt eines stillen Abschneidens erzeugt.

## Geprüfte Interaktionen

Liste der aus der Anforderung geprüften Benutzerinteraktionen:
- Server auswählen/verbinden (manuelle Adresse, Netzwerk-Suche, bekannte Server) → unauffällig: alle Wege sind sichtbar beschriftet, Klartext-Namen statt Kennungen.
- Pairing-Code eingeben und Gerät koppeln → Befund vorhanden (stilles Abschneiden bei `MaxLength="32"`); übrige Aspekte unauffällig: Herkunftshinweis auf die Server-Verwaltung (Bereich „Geräte") vorhanden, Kleinschreibung wird automatisch korrigiert, eigener verständlicher Formatfehler-Text mit Zeichen-Hinweis, keine harte 8-Stellen-Annahme.
- Pairing-Fehlerfälle verstehen und erneut versuchen → unauffällig: getrennte Inline-Fehlertexte für ungültigen/abgelaufenen Code (401), Versuchssperre (429), Server ohne Pairing-Unterstützung (404 mit Anlaufstelle „Person, die den Server eingerichtet hat") und Nichterreichbarkeit; Eingabefeld und Button bleiben nach Fehler editierbar.
- Re-Pairing nach widerrufenem Geräte-Token → unauffällig: Rückkehr zur Einrichtungsseite mit aktivem Pairing-Abschnitt und verständlichem Hinweis („Der Geräte-Zugang wurde vom Server abgelehnt. Bitte das Gerät erneut koppeln.").
- Erfolgreiches Pairing → unauffällig: Übergang in den bisherigen Anmelde-/Start-Workflow ohne zusätzliche Nutzeraktion.

## Geprüfte Dateien

Liste aller geprüften UI-Dateien:
- `VideoPlayer.Maui/Pages/ServerSetupPage.xaml`
- `VideoPlayer.Maui/Pages/ServerSetupPage.xaml.cs`
- `VideoPlayer.Mvu/AppUpdate.cs` (Pairing-Übergänge und Fehlertexte)
- `VideoPlayer.Mvu/PairingCode.cs` (Eingabe-Normalisierung/Formatprüfung)
- `VideoPlayer.Mvu/PairingStatus.cs` (Ergebnisstatus der Pairing-Eingabe)
- `VideoPlayer.Mvu/AppMessage.cs` (Pairing-Nachrichten)
- `VideoPlayer.Maui/Services/ShellAppEffectHandler.cs` (Statuscode-Mapping 400/401/404/429 → Anzeigetexte)
