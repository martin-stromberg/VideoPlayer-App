# Download-Verwaltung und automatische Downloads (Entwicklungsschritt 11)

Die neue VideoPlayer-App kann Medien für die Offline-Nutzung
herunterladen: ein plattformneutraler `DownloadService` führt die
Downloads als persistente Aufträge aus, die Menüseite **„Downloads"**
verwaltet sie, und automatische Vorlauf-Läufe halten die nächsten
Weiterschauen-Episoden vor.

## Download starten

- **Detailseite:** bei Filmen und Episoden erscheint im Kopfbereich
  die Schaltfläche **„Herunterladen"** (nur bei abspielbaren Medien).
- **Episodenliste:** jede Episode trägt eine eigene ⤓-Schaltfläche —
  Serienname und Episodentitel werden als Anzeigetitel übernommen.
- Die Rückmeldung erscheint direkt auf der Detailseite: grün bei
  „eingeplant/bereits vorhanden/läuft bereits", rot bei Ablehnung
  (kein Speicherplatz-Kontingent mehr, keine Serververbindung,
  nicht ladbares Medium).
- Ohne Serververbindung ist kein Download möglich — die Anforderung
  wird mit Hinweis abgelehnt statt heimlich geplant.

## Die Downloads-Seite

Die Menüseite „Downloads" listet alle Aufträge — sie funktioniert
auch offline, weil der Bestand lokal im Download-Manifest liegt.

- Je Eintrag: Titel, Medientyp (Film/Episode), Zustand („Wartend",
  „Läuft", „Fertig", „Abgebrochen", „Fehler"), Fortschrittsbalken
  mit Prozentangabe während des Laufs und verständliche Fehlertexte.
- Der Fortschrittsbalken aktualisiert sich flüssig: bei jedem
  Fortschritts-Tick wird nur der betroffene Eintrag aktualisiert —
  die übrige Liste wird nicht neu aufgebaut (kein Flackern).
- Über der Liste steht die Kontingent-Belegung („x von y GB belegt");
  ein Offline-Banner kündigt an, dass wartende Downloads mit der
  Verbindung fortgesetzt werden.
- Aktionen je Eintrag: **Abbrechen** (wartend/läuft), **Erneut
  versuchen** (fehlgeschlagen/abgebrochen), **Löschen** (immer —
  entfernt Datei und Eintrag, laufende Transfers werden abgebrochen).

## Parallele Downloads, Abbruch und Wiederaufnahme

- Bis zu zwei Downloads laufen gleichzeitig; weitere Aufträge warten
  und rücken automatisch nach.
- **Abbrechen** beendet einen laufenden Transfer sauber und entfernt
  die Teildatei; abgebrochene Aufträge bleiben sichtbar und werden
  nicht automatisch wiederaufgenommen.
- **Automatische Wiederaufnahme:** wartende und unterbrochene
  Downloads starten von selbst neu — beim Verbindungsaufbau, nach
  App-Neustart und bei Netzwerk-/WLAN-Rückkehr. Fehlgeschlagene
  Aufträge werden ebenfalls wiederaufgenommen — außer
  Kontingent-Überschreitungen (die erfordern eine Entscheidung des
  Anwenders).
- Einschränkung: der Server-Stream unterstützt keine
  Bereichs-Fortsetzung (Range-Requests) — „Wiederaufnahme" bedeutet
  einen erneuten Download ab Beginn.

## Speicherplatz-Kontingent

- Das in den Einstellungen konfigurierte Kontingent (Standard **5 GB**)
  wird vor dem Einreihen, beim Transfer-Start und laufend während der
  Übertragung durchgesetzt — auch bei parallelen Downloads.
- Die Belegung misst die Summe der Dateien im Video-Ablageort;
  verwaiste Dateien ohne Manifest-Eintrag werden mitgezählt.
- Bei Erreichen schlägt der Auftrag als „Fehler" mit dem Hinweis
  „Das Speicherplatzkontingent ist erreicht" fehl — Downloads löschen
  oder das Kontingent erhöhen löst die Sperre.

## „Downloads nur im WLAN"

- Aktiviert starten Downloads nur, wenn das Gerät über ein
  ungedrosseltes Festnetz-Profil verbunden ist — WLAN **oder**
  kabelgebundenes Ethernet (eine Desktop-Anbindung soll die
  Einstellung nicht blockieren).
- Wartende Aufträge bleiben als „Wartend" sichtbar und laufen bei
  WLAN-Rückkehr automatisch an.

## Automatische Downloads (Vorlauf)

- Ist „Automatische Downloads" aktiviert, lädt die App die nächsten
  **N** Episoden je Weiterschauen-Eintrag vor — die Anzahl kommt aus
  der Einstellung „Vorzuladende Episoden" (staffelübergreifend, in
  Episodenreihenfolge, Serienkontext vom Server).
- **Playlist-Bezug:** Stammt ein Weiterschauen-Eintrag aus einer
  Playlist, folgt der Vorlauf der **Playlist-Reihenfolge** statt der
  Serienreihenfolge — die Folge-Einträge werden ab dem aktuellen
  Playlist-Eintrag in Server-Reihenfolge eingeplant (nur ladbare,
  zugängliche Einträge — Filme und Episoden; Serien-, Staffel- und
  gesperrte Einträge werden übersprungen). Ein playlist-gebundener
  Film lädt sich selbst plus die folgenden Playlist-Einträge. Ist der
  Eintragsbezug verwaist (Medium nicht mehr in der Playlist) oder die
  Playlist nicht erreichbar, gilt wieder die Serienreihenfolge.
- Film-Einträge der Weiterschauen-Liste ohne Playlist-Bezug laden den
  Film selbst.
- Der Vorlauf läuft nach der Synchronisation, nach Server-Ereignissen
  mit Listen-Änderung und beim Einschalten der Einstellung; er ist
  gedrosselt (mindestens 60 Sekunden Abstand) und respektiert die
  WLAN-Bedingung.
- Bereits vorhandene oder laufende Downloads werden nicht doppelt
  angelegt.
- Die Warteschlange arbeitet in Einplanungs-Reihenfolge (ältester
  wartender Auftrag zuerst) — dadurch werden playlist-gebundene
  Downloads auch in der Playlist-Reihenfolge ausgeführt, wie sie
  eingeplant wurden.

## Gesehene Episoden automatisch löschen

Ist die Einstellung aktiv, wird der fertige Download einer Episode
entfernt, sobald sie als „angesehen" gilt (Endfenster-Schwelle oder
Wiedergabe-Ende). Laufende oder wartende Downloads bleiben unberührt.

## Ablage und Wiedergabe

- Dateien und Manifest liegen außerhalb des Backups: die Dateien im
  no-backup-Video-Verzeichnis, das Manifest `download_manifest` in
  der Cache-Datenbank — ein Backup/Wiederherstellen erzeugt keine
  verwaisten Dateireferenzen.
- Die Wiedergabe (`PlaybackSourceService`) bevorzugt automatisch die
  lokale Datei eines abgeschlossenen Downloads; fehlt sie, fällt die
  Wiedergabe auf den Server-Stream zurück.

## Fehlerbilder

| Zustand | Anzeige |
|---------|---------|
| `quota_exceeded` | „Das Speicherplatzkontingent ist erreicht." |
| `no_connection` | „Keine Serververbindung." |
| `network` | „Der Download wurde unterbrochen (Netzwerkfehler)." |
| `io` | „Die Videodatei konnte nicht gespeichert werden." |
| `http_*` | „Der Server hat den Download abgelehnt." |

Fehlerhafte Einträge bleiben sichtbar und können über **„Erneut
versuchen"** neu eingeplant werden.
