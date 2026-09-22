# Usability-Review

Betrachtete Änderungen (aus Endanwendersicht, ohne Kenntnis der
Implementierung):

- `VideoPlayer.Maui/Pages/ServerSetupPage.xaml` + `.xaml.cs`
  (Erststart-Einrichtung und Serververwaltung)
- `VideoPlayer.Maui/Pages/SettingsPage.xaml` + `.xaml.cs`
  (neuer Eintrag „Server wechseln")
- `VideoPlayer.Maui/AppShell.xaml` + `.xaml.cs` (Route ohne Menüeintrag)
- `VideoPlayer.Maui/App.xaml.cs`, `Services/ShellAppEffectHandler.cs`
  (Startverhalten: Einrichtungsdialog statt Ladeschleife)

## Ergebnis

**Status:** Keine Befunde

## Prüfschritte / Benutzerflüsse

### Erststart ohne hinterlegten Server

- Beim ersten Start (kein Server hinterlegt) erscheint statt der endlosen
  Ladeanzeige die Seite „Server" mit Titel „VideoPlayer-Server" und dem
  erklärenden Satz „Die App verbindet sich mit einem VideoPlayer-Server im
  eigenen Netzwerk." — der Nutzer weiß sofort, was gefragt ist.
- Zwei gleichwertige Wege sind ohne Vorwissen erkennbar:
  „Server im Netzwerk suchen" (automatisch) und das Eingabefeld mit
  Platzhalter `http://server/videoplayer/` plus Schaltfläche „Verbinden"
  (manuell).
- Während der Suche läuft ein Fortschrittsindikator und die Schaltfläche
  ist gesperrt; ohne Treffer erscheint der Hinweis „Kein Server gefunden.
  Bitte die Adresse manuell eingeben." — kein toter Zustand.
- Gefundene Server erscheinen als auswählbare Liste (Name bzw. Adresse +
  Adresse); ein Tipp genügt — keine internen IDs, Ports oder Schlüssel
  müssen eingegeben werden.
- Ungültige Eingabe → roter Inline-Hinweis „Bitte eine gültige
  Serveradresse eingeben."; nicht erreichbarer Server → „Der Server ist
  nicht erreichbar. Bitte die Adresse prüfen." Beides ohne Modal-Dialog,
  die Seite bleibt editierbar.
- Erst nach erfolgreicher Prüfung wird gespeichert und die App wechselt
  zur Startseite; beim nächsten Start läuft sie direkt mit dem
  gespeicherten Server (kein erneuter Einrichtungsdialog).

### „Server wechseln" aus den Einstellungen

- Die Einstellungsseite erhält am Ende einen unauffälligen Abschnitt
  „Server" mit der Schaltfläche „Server wechseln" — kein neuer
  Menüeintrag, keine Umstellung der gewohnten Einstellungen.
- Die Verwaltungsseite zeigt den aktiven Server und die Liste bekannter
  Server (Name/Adresse + Adresse); Tipp auf einen Eintrag wechselt nach
  erfolgreicher Prüfung und kehrt zur Startseite zurück. Zurück-Navigation
  der Shell funktioniert nativ (geschobene Route).
- Neue Server lassen sich über denselben Dialog per Suche oder manueller
  Eingabe hinzufügen — kein zweiter Ort, keine andere Bedienung.
- Die übrigen Einstellungen (Downloads, Wiedergabe) werden beim Wechsel
  weder angezeigt noch verändert; sie bleiben erhalten (MVU-Test:
  `Settings` unverändert; Persistenz schreibt nur `known_servers`/
  `backup_state`).

### Verständlichkeit / Konsistenz

- Alle Texte sind deutsche Klartext-Beschriftungen ohne Fachjargon
  (kein „mDNS", „UDP", „Endpoint" o. Ä. im UI); die
  `AutomationId`s sind Testanker und für Nutzer unsichtbar.
- Seitenaufbau (Titel, Abschnitte, Abstände, `ScrollView`) entspricht der
  Einstellungsseite; Fehlertexte erscheinen rot inline wie dort.
- Der Einrichtungsdialog hat absichtlich keinen Menüeintrag — er wird beim
  Erststart als Root-Page bzw. über „Server wechseln" erreicht; nach dem
  erfolgreichen Verbinden landet der Nutzer auf der Startseite.

## Befunde

—
