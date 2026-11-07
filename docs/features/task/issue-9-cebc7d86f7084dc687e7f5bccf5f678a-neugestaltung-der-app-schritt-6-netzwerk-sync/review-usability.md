# Usability-Review

Betrachtete Änderungen (aus Endanwendersicht, ohne Kenntnis der
Implementierung):

- `VideoPlayer.Maui/Pages/HomePage.xaml` + `.xaml.cs` (Offline-Banner,
  bereits aus Schritt 5 vorhanden, jetzt zur Laufzeit umschaltend)
- `VideoPlayer.Mvu/AppUpdate.cs` (Zustandsübergänge bei Netzwerkwechsel,
  Navigationsverhalten bei Wiederverbindung)
- `VideoPlayer.Maui/Services/ShellAppEffectHandler.cs` (Hintergrund-
  synchronisation, Ereignisverbindung)
- `VideoPlayer.Maui/Services/ConnectivityNetworkStatusMonitor.cs`,
  `SignalRServerEventsConnection.cs` (keine Oberfläche)

## Ergebnis

**Status:** Keine Befunde

## Prüfschritte / Benutzerflüsse

### Netzwerkverlust während der Nutzung

- Geht die Netzwerkverbindung im verbundenen Zustand verloren, wechselt
  die App in den Offline-Zustand: Auf der Startseite erscheint der
  bekannte gelbe Hinweis „Offline-Modus: Der Server ist nicht
  erreichbar. Es werden lokal gespeicherte Daten angezeigt. …" — der
  Hinweis benennt Zustand, Folge und Ausweg (Serververwaltung).
- Der Wechsel geschieht **ohne** Navigation: Der Anwender bleibt auf
  seiner aktuellen Seite (z. B. Favoriten, Downloads) — kein Sprung, kein
  Kontextverlust.
- Befindet sich die App gerade in einem Workflow (Start-Ladeanzeige,
  Anmeldemaske, Servereinrichtung), ändert ein Netzwerkverlust nichts an
  der Maske — die ohnehin laufenden Prüfungen enden in ihren bekannten
  Fehlerzuständen.

### Online-Rückkehr

- Kehrt das Netzwerk zurück, läuft die normale Wiederverbindungs-Kette
  (Erreichbarkeitsprüfung → automatische Anmeldung → Synchronisation)
  im Hintergrund — kein Dialog, keine Nutzeraktion.
- Der Anwender bleibt auf seiner Menüseite; nur von den Workflow-Seiten
  (Ladeanzeige, Anmeldung, Servereinrichtung) wird zur Startseite
  navigiert. Ein wiederholtes Offline/Online-Flapping reißt den
  Anwender also nicht aus seiner Ansicht.
- Bleibt der Server weiterhin nicht erreichbar, kehrt die App ohne
  Nebenwirkungen in den Offline-Zustand zurück.

### Hintergrund-Synchronisation und Server-Ereignisse

- Nach der (Wieder-)Verbindung laufen das Nachsenden der lokal
  gesammelten Fortschritte und die Aktualisierung von Quellen, „Neu im
  Programm", Weiterschauen und Favoriten vollständig im Hintergrund —
  kein blockierender Dialog, keine Pflicht-Wartezeit.
- Server-Ereignisse (Weiterschauen aktualisiert, Favoriten geändert,
  neue Inhalte eingescannt) aktualisieren die betroffenen Caches
  gezielt; die Anzeigen sind beim nächsten Seitenaufruf bzw. über die
  Model-Benachrichtigung aktuell — keine manuelle Aktualisierung nötig.
- Der Synchronisationszustand ist als `IsSyncRunning` im Model
  geführt, wird aber bewusst **nicht** als eigener Hinweis eingeblendet
  (kein neues Bedienelement im Plan) — er erzeugt keine Sperrung oder
  Unklarheit.

### Verständlichkeit

- Es werden keine internen Kennungen (IDs, GUIDs, Tabellen- oder
  Tokenschlüssel) angezeigt oder abgefragt; die einzige neue
  Information im Model (`IsOnline`, `IsSyncRunning`) ist technischer
  Zustand ohne Oberfläche.
- Der Offline-Hinweis ist ein vorhandenes, bereits geprüftes Element
  (Schritt 5) mit `AutomationId="OfflineBanner"`; seine Sichtbarkeit
  folgt ausschließlich dem MVU-Model (`IsOffline`).

## Feststellungen

- Keine Befunde. Einschränkung (akzeptiert): Echte UI-E2E-Ausführung
  steht nicht zur Verfügung (kein UI-Automatisierungs-Framework im
  Repository); die Prüfung erfolgte über Code-Inspektion der
  Oberflächen und die Message-Sequenztests — dokumentiert in
  `inventory/tests.md` und `test-results.md`.
