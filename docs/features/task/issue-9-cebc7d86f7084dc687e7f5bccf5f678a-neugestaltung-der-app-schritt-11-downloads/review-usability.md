# Usability-Review

Betrachtete Änderungen (aus Endanwendersicht, ohne Kenntnis der
Implementierung):

- `VideoPlayer.Maui/Pages/DownloadsPage.xaml` + `.xaml.cs`
  (Menüseite „Downloads": Liste, Fortschritt, Aktionen)
- `VideoPlayer.Maui/Pages/MediaDetailsPage.xaml` + `.xaml.cs`
  („Herunterladen"-Aktionen im Kopf und je Episode,
  Hinweis-Rückmeldung)
- `VideoPlayer.Mvu/DownloadListItem.cs` (Anzeigetexte: Zustand,
  Fortschritt, Fehler)
- `VideoPlayer.Mvu/DownloadService.cs` (Hinweistexte der
  Einreihe-Rückmeldungen)
- `VideoPlayer.Maui/Services/ConnectivityNetworkStatusMonitor.cs`
  (WLAN-only-Verhalten auf dem Gerät)

## Ergebnis

**Status:** Ein Befund behoben (WLAN-only auf Ethernet), sonst keine
Befunde — mit Hinweisen

## Behobener Befund

### „Nur im WLAN" hätte Desktop-Nutzer über Ethernet blockiert

- **Beschreibung:** `WifiOnlyDownloads` ist standardmäßig aktiv. Hätte
  die Prüfung nur `ConnectionProfile.WiFi` berücksichtigt, wären alle
  Downloads auf einem per Netzwerkkabel angebundenen Windows-Gerät
  dauerhaft blockiert gewesen — der Anwender hätte wartende Aufträge
  ohne erkennbaren Grund gesehen.
- **Korrektur:** `IsWifi` akzeptiert neben `WiFi` auch `Ethernet`;
  die Einstellung schont damit wie beabsichtigt getaktete
  Mobilfunk-Verbindungen, nicht das Festnetz.

## Prüfschritte / Benutzerflüsse

### Download anfordern

- Auf der Detailseite eines Films/einer Serie trägt der Kopfbereich
  bei abspielbaren Medien die Schaltfläche **„Herunterladen"**; jede
  Episode der Episodenliste hat zusätzlich eine eigene ⤓-Schaltfläche
  — derselbe Einstieg wie die ▶-Wiedergabe.
- Die Rückmeldung erscheint direkt unterhalb als grüner Hinweis
  („Der Download wurde eingeplant." / „…bereits vorhanden/läuft/
  wartet bereits.") oder rot bei Ablehnung (Quota, keine Verbindung,
  ungültiges Medium) — keine stillschweigende Aktion.
- Ohne Serververbindung erscheint die rote Meldung „Keine
  Serververbindung — der Download ist derzeit nicht möglich." — der
  Auftrag wird nicht heimlich geplant, die Erwartung ist eindeutig.

### Downloads verwalten

- Die Menüseite **„Downloads"** zeigt je Eintrag Titel, Medientyp
  („Film"/„Episode"), Zustand („Wartend", „Läuft", „Fertig",
  „Abgebrochen", „Fehler"), Fortschrittsbalken mit Prozentangabe
  während des Laufs und verständliche Fehlertexte.
- Über der Liste steht die Kontingent-Belegung („x von y GB belegt");
  ein Offline-Banner kündigt an, dass wartende Downloads mit der
  Verbindung fortgesetzt werden.
- Aktionen je Eintrag sind zustandsabhängig sichtbar: **Abbrechen**
  (wartend/läuft), **Erneut versuchen** (fehler/abgebrochen),
  **Löschen** (immer). Löschen eines laufenden Downloads bricht den
  Transfer sauber ab.
- Die Seite funktioniert offline: die Liste liegt im lokalen
  Manifest; Fortschritte laufender Transfers aktualisieren die Liste
  ohne Neu laden.

### Unterbrechung und Wiederaufnahme

- App-Neustart, Verbindungsverlust oder WLAN-Wechsel hinterlassen
  wartende/laufende Aufträge im Manifest; mit der nächsten gültigen
  Verbindung laufen sie automatisch erneut an — sichtbar als
  „Wartend" → „Läuft".
- Abgebrochene Downloads werden bewusst nicht wiederaufgenommen —
  sie bleiben als „Abgebrochen" sichtbar bis zum Löschen oder
  erneuten Versuch.
- Dokumentierte Einschränkung: der Server-Stream unterstützt keine
  Range-Fortsetzung — „wiederaufnehmen" bedeutet Neustart ab Byte 0
  (siehe `plan.md`).

### Kontingent und automatische Downloads

- Ist das GB-Kontingent belegt, wird ein neuer Auftrag sofort mit
  Hinweis „Das Speicherplatzkontingent ist erreicht. Downloads löschen
  oder das Kontingent erhöhen." abgelehnt — kein stilles Scheitern.
- Bei aktivierten automatischen Downloads erscheinen die nächsten
  Weiterschauen-Episoden von selbst in der Liste (Anzahl aus der
  Einstellung); bei „nur im WLAN" bleiben sie bis zur WLAN-Verbindung
  „Wartend".
- Ist „Gesehene Episoden automatisch löschen" aktiv, verschwindet
  der Download einer zu Ende gesehenen Episode aus Liste und
  Speicher.

## Hinweise (keine Befunde)

- **Keine Pull-to-Refresh-Geste** auf der Downloads-Seite — sie lädt
  beim Öffnen und wird laufend durch Transfer-Ereignisse aktualisiert;
  im Fehlerfall gibt es „Erneut versuchen". Konsistent mit den
  übrigen Listen-Seiten.
- **Fortschrittsanzeige ohne bekannte Gesamtgröße** zeigt 0 % mit
  laufendem Balken (`TotalBytes = 0` bei fehlendem
  `Content-Length`) — seltene Serverkonfiguration, akzeptabel.
- **Quota-Anzeige** rundet auf eine Nachkommastelle („0,1 GB") —
  ausreichend für die Orientierung.
- **Bildmaterial** zeigt die Liste nicht (kein Poster je Eintrag) —
  bewusst schlicht gehalten, konsistent mit der Verwaltungsfunktion
  der Seite.
