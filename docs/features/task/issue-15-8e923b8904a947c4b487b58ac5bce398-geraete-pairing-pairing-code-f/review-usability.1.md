# Usability-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### ServerSetupPage.xaml (Server-Einrichtung / Gerät koppeln)

- **Erreichbarkeit** - Der Flow „Server verbinden → Code eingeben → gekoppelt" setzt voraus, dass die Anwenderin einen Einmal-Pairing-Code besitzt. Der einzige Hinweis auf der Oberfläche lautet „Einmal-Code aus der Server-Oberfläche eingeben." (`PairingSection`, Zeile ~94). Für eine nicht-technische Person ist „Server-Oberfläche" nicht auflösbar: Sie weiß weder, dass der Code in der Verwaltung des Servers erzeugt wird, noch wo dort (Admin-Bereich / Geräte-Verwaltung). Ohne dieses Wissen ist die geforderte Aufgabe „Code eingeben" nicht durchführbar — der Code ist die Voraussetzung für den gesamten Flow.

  Empfehlung: Hinweistext konkretisieren und den Ort benennen, z. B. „Den Einmal-Code erstellst du in der Verwaltung deines Servers (Bereich ‚Geräte'). Gib ihn hier ein." Ergänzend die erwartete Form nennen („8-stelliger Code"), damit die Anwenderin Tippfehler selbst erkennt.

### AppUpdate.cs / ServerSetupPage.xaml (Pairing-Eingabe)

- **Erreichbarkeit** - Bei leerem oder falsch formatiertem Pairing-Code (zu kurz, nicht im Code-Alphabet enthaltene Zeichen wie „O" oder „0") wird lokal `InvalidPairingCodeError` angezeigt: „Der Pairing-Code ist ungültig oder abgelaufen. Bitte den Code prüfen." (`UpdatePairingCodeSubmitted`, `AppUpdate.cs` Zeile ~471-475). Die Aussage „abgelaufen" ist bei einer reinen Eingabe-/Formatverletzung irreführend: Die Anwenderin hat den Code gerade erst erzeugt und sucht den Fehler am falschen Ort (neuen Code anfordern statt Tippfehler korrigieren). Das etablierte Muster für leere/unvollständige Eingaben in derselben App ist eine direkte Aufforderung (`CredentialsRequiredError` = „Bitte E-Mail-Adresse und Passwort eingeben.").

  Empfehlung: Für die lokale Formatprüfung (`PairingCode.TryNormalize` schlägt fehl) einen eigenen Text verwenden, z. B. „Bitte den 8-stelligen Pairing-Code eingeben." Der serverseitige 401-Fall kann den vorhandenen Text behalten (ggf. ergänzt um „oder bereits verwendet", da verbrauchte Codes ebenfalls 401 liefern).

## Geprüfte Interaktionen

Liste der aus der Anforderung geprüften Benutzerinteraktionen:
- Serveradresse manuell eingeben bzw. bekannten/gefundenen Server auswählen → unauffällig (bestehende Muster: Liste mit Klartext, Netzwerk-Erkennung, manuelle Eingabe)
- Pairing-Code eingeben und „Koppeln" auslösen → Befund vorhanden (Herkunft des Codes nicht erklärt; Formatfehler erzeugt irreführenden Fehlertext)
- Fehlerfälle erkennen und erneut versuchen (ungültiger/abgelaufener Code, IP-Sperre 429, Server ohne Pairing 404, Transportfehler) → unauffällig bis auf den Formatfehler-Text (siehe Befund 2); Fehlertexte sind deutsch, verständlich und inline am Eingabefeld
- Re-Pairing nach widerrufenem Geräte-Token (401) → unauffällig (automatische Navigation zur Einrichtungsseite mit verständlichem Text „Der Geräte-Zugang wurde vom Server abgelehnt. Bitte das Gerät erneut koppeln.")
- Fortsetzung nach erfolgreichem Pairing → unauffällig (Übergang in den bisherigen Anmelde-Workflow, implizite Erfolgsrückmeldung)
- Interne/technische Kennungen eingeben müssen → unauffällig (der Pairing-Code ist eine benutzerseitige Einmal-Kennung aus der Server-Oberfläche, keine interne Datenbank-Id; Gerätename wird automatisch aus `DeviceInfo` ermittelt)

## Geprüfte Dateien

Liste aller geprüften UI-Dateien:
- `VideoPlayer.Maui/Pages/ServerSetupPage.xaml`
- `VideoPlayer.Maui/Pages/ServerSetupPage.xaml.cs`
- `VideoPlayer.Mvu/AppModel.cs` (Pairing-Zustände `IsPairingRequired`, `IsPairingRunning`, `PairingError`)
- `VideoPlayer.Mvu/AppMessage.cs` (`PairingCodeSubmittedMessage`, `PairingCompletedMessage`, `DeviceTokenRevokedMessage`, `PairingRequired`-Flags)
- `VideoPlayer.Mvu/AppUpdate.cs` (Pairing-Übergänge und Fehlertexte)
- `VideoPlayer.Mvu/PairingStatus.cs` (Ergebnis-Enum des Pairings)
- `VideoPlayer.Mvu/PairingCode.cs` (Normalisierung/Formatprüfung der Eingabe)
- `VideoPlayer.Maui/Services/ShellAppEffectHandler.cs` (Pairing-Austausch, Re-Pairing-Auslösung, Gerätename)
