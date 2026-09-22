← [Zurück zur Übersicht](index.md)

# Server-API-Client — Beschreibung

## Zweck

Der Server-API-Client stellt der App einen einheitlichen Zugriff auf den VideoPlayer-Server bereit. Er kapselt sämtliche Serverkommunikation: Anmeldung, Abruf von Medienlisten und -details, Favoritenverwaltung, Fortschrittsmeldungen beim Schauen sowie den Abruf von Bildern.

## Funktionsweise

- Nach der Anmeldung mit Benutzername und Passwort verwendet der Client das vom Server ausgestellte Anmelde-Token automatisch für alle Folgeanfragen.
- Läuft das Token ab (Server meldet „nicht autorisiert"), wird die Anmeldung einmalig im Hintergrund erneuert und die Anfrage wiederholt — der Anwender merkt davon nichts.
- Bei vorübergehenden Serverproblemen (Serverfehler, Zeitüberschreitung, zu viele Anfragen) wiederholt der Client die Anfrage automatisch mit wachsenden Wartezeiten. Eindeutige Fehler (z. B. „nicht gefunden") werden dagegen sofort gemeldet.
- Jede Anfrage weist die App gegenüber dem Server über einen API-Schlüssel aus (Client-Gate).

## Beispiele

- Die Startseite fragt „Neu im Programm" und die Weiterschauen-Liste über den Client ab.
- Beim Weiterschauen meldet die App den Wiedergabefortschritt; Einträge können ausgeblendet oder übersprungen werden.
- Favoriten werden serverseitig markiert und wieder entfernt.

## Einschränkungen

- Der Client benötigt eine konfigurierte Server-Basisadresse und einen gültigen API-Schlüssel (Client-Gate).
- Er deckt nur die öffentliche Server-API ab; administrative Endpunkte (z. B. Quellenverwaltung, Metadaten-Schreiben) sind nicht Teil des Clients.
