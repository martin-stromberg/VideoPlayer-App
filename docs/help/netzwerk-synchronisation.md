# Netzwerkstatus und Synchronisation (Entwicklungsschritt 6)

Die neue VideoPlayer-App erkennt Online↔Offline-Wechsel zur Laufzeit,
stellt nach einer Netzwerk-Wiederkehr die Verbindung automatisch wieder
her und synchronisiert dabei Fortschritte und Metadaten mit dem Server.
Eine Echtzeit-Verbindung zum Server hält die angezeigten Daten ohne
manuelle Aktualisierung aktuell.

## Netzwerkstatus-Erkennung

Die App überwacht die Netzwerkverbindung über die
Plattform-`Connectivity`-Schnittstelle (`ConnectivityChanged`):

- **Netzwerkverlust im verbundenen Zustand:** Die App wechselt in den
  Offline-Zustand — auf der Startseite erscheint der gelbe
  Offline-Hinweis, die Ereignisverbindung wird getrennt. Der Anwender
  bleibt auf seiner aktuellen Seite.
- **Online-Rückkehr aus dem Offline-Zustand:** Die App läuft die normale
  Wiederverbindungs-Kette — Erreichbarkeitsprüfung (`GET api/health`),
  automatische Anmeldung mit den gespeicherten Zugangsdaten, Aufbau der
  Ereignisverbindung und anschließende Synchronisation. Auf Menüseiten
  bleibt der Anwender stehen; nur von den Workflow-Seiten (Ladeanzeige,
  Anmeldung, Servereinrichtung) wird zur Startseite navigiert.
- Netzwerkwechsel während der Anmeldung oder Servereinrichtung ändern
  die Maske nicht — die laufenden Prüfungen entscheiden über den
  Ausgang.

## Echtzeit-Verbindung (Server-Ereignisse)

Im verbundenen Zustand hält die App eine SignalR-Verbindung zum Hub
`hubs/mediaupdate` des Servers (JWT-authentifiziert, automatische
Wiederverbindung des Transports bei kurzen Unterbrechungen). Der Server
meldet:

- `ContinueWatchingUpdated` → Weiterschauen-Liste wird neu geladen.
- `FavoritesChanged` → die lokale Kopie der Favoriten wird ersetzt.
- `NewVideosScanned` → Quellen und „Neu im Programm" werden neu geladen.
- `StatusChanged` → wird nur protokolliert.

Die gezielten Aktualisierungen schreiben in die lokale Cache-Ablage;
die betroffenen Anzeigen sind damit ohne manuelle Aktualisierung frisch.
Wird die Verbindung endgültig getrennt, baut die App sie im verbundenen
Online-Zustand erneut auf.

## Fortschritts-Warteschlange

Wiedergabefortschritte werden sofort an den Server gemeldet, sobald die
App online und angemeldet ist. Ist das nicht möglich (offline, kein
angemeldeter Client oder ein überholbarer Sendefehler), landet die
Meldung in einer **persistenten Warteschlange** in der backup-pflichtigen
Datenbank (`pending_progress`):

- Pro Medium wird nur der jeweils neueste Stand vorgehalten.
- Die Einträge überstehen App-Neustarts und Backup-Wiederherstellungen.
- Bei der Synchronisation werden sie in chronologischer Reihenfolge
  nachgesendet; erst nach erfolgreicher Übertragung wird ein Eintrag
  gelöscht. Ein Transport- oder Serverfehler bricht den Lauf ab — der
  Rest bleibt für den nächsten Sync erhalten (kein Datenverlust).
- Eindeutige Clientfehler (4xx außer den wiederholbaren Codes) werden
  verworfen, weil sie bei jedem Versuch erneut scheitern würden.

## Synchronisation nach (Wieder-)Verbindung

Nach dem Übergang in den verbundenen Zustand läuft die vollständige
Synchronisation im Hintergrund:

1. Wartende Fortschrittsmeldungen nachsenden.
2. Quellen und „Neu im Programm" neu laden und cachen.
3. Weiterschauen-Liste neu laden und cachen (enthält dann die
   nachgesendeten Stände).
4. Favoriten vom Server laden und die lokale Kopie ersetzen.

Der Ablauf ist nicht blockierend; schlägt ein Teilschritt fehl, wird er
protokolliert und beim nächsten Ereignis bzw. der nächsten
Wiederverbindung erneut versucht.

## Verhalten bei Sitzungsablauf

Läuft das Anmelde-Token endgültig ab (gespeicherte Zugangsdaten fehlen
oder werden abgelehnt), wird zusätzlich zur bekannten Anmeldemaske die
Ereignisverbindung getrennt — sie kann ohne gültiges Token nicht
bestehen. Nach der erneuten Anmeldung werden Ereignisverbindung und
Synchronisation automatisch wieder aufgenommen.
