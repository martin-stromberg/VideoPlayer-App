# Download-Verwaltung und automatische Downloads (Entwicklungsschritt 11)

Die neue VideoPlayer-App kann Medien für die Offline-Nutzung
herunterladen: ein plattformneutraler `DownloadService` führt die
Downloads als persistente Aufträge aus, die Menüseite **„Downloads"**
verwaltet sie, und automatische Vorlauf-Läufe halten die aktuelle und
die folgenden Weiterschauen-Episoden vor.

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
- Automatisch vorgeladene Downloads tragen die Herkunfts-
  Kennzeichnung **„Cache"** (Pill-Chip neben dem Medientyp);
  manuell eingeplante Downloads zeigen keine Kennzeichnung.
- Fertige Downloads zeigen ihr **Ablaufdatum** („Läuft ab am …") —
  nach Ablauf entfernt der Bereinigungslauf Datei und Eintrag,
  sofern der Download nicht als Auto-Download-Kandidat verlängert
  wird (siehe „Ablaufdatum und Bereinigung").
- Der Fortschrittsbalken aktualisiert sich flüssig: bei jedem
  Fortschritts-Tick wird nur der betroffene Eintrag aktualisiert —
  die übrige Liste wird nicht neu aufgebaut (kein Flackern).
- Über der Liste steht die Kontingent-Belegung („x von y GB belegt");
  ein Offline-Banner kündigt an, dass wartende Downloads mit der
  Verbindung fortgesetzt werden.
- Je Eintrag wird die Dateigröße angezeigt (bei laufenden Downloads
  „x von y GB").
- Aktionen je Eintrag: **Abbrechen** (wartend/läuft), **Erneut
  versuchen** (fehlgeschlagen/abgebrochen), **Löschen** (immer —
  entfernt Datei und Eintrag, laufende Transfers werden abgebrochen).
- **„Alle löschen"** (Kopfbereich, mit Rückfrage) entfernt alle
  Aufträge, Dateien und Restdaten.
- **„Alle Fehler löschen"** (Kopfbereich, mit Rückfrage — nur sichtbar,
  wenn fehlerhafte Einträge vorliegen) entfernt alle fehlgeschlagenen
  und abgebrochenen Downloads samt Dateien und Manifest-Einträgen;
  wartende, laufende und fertige Downloads bleiben unberührt.
- **„Speicher bereinigen"** (unter der Belegungszeile, nur bei erkannten
  Restdaten) entfernt nicht zugeordnete Dateien nach Rückfrage.

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

- Ist „Automatische Downloads" aktiviert, lädt die App die aktuelle
  Episode und die folgenden Episoden je Weiterschauen-Eintrag vor —
  insgesamt bis zu **N** je Weiterschauen-Eintrag; die Anzahl kommt aus
  der Einstellung „Anzahl vorzuladender Episoden" (staffelübergreifend,
  in Episodenreihenfolge, Serienkontext vom Server).
- **Playlist-Bezug:** Stammt ein Weiterschauen-Eintrag aus einer
  Playlist, folgt der Vorlauf der **Playlist-Reihenfolge** statt der
  Serienreihenfolge — die Einträge werden ab einschließlich des
  aktuellen Playlist-Eintrags in Server-Reihenfolge eingeplant (nur
  ladbare, zugängliche Einträge — Filme und Episoden; Serien-, Staffel-
  und gesperrte Einträge werden übersprungen). Ein playlist-gebundener
  Film lädt sich selbst plus die folgenden Playlist-Einträge — der Film
  zählt dabei ins Budget der **N** Einträge. Ist der
  Eintragsbezug verwaist (Medium nicht mehr in der Playlist) oder die
  Playlist nicht erreichbar, gilt wieder die Serienreihenfolge.
- Film-Einträge der Weiterschauen-Liste ohne Playlist-Bezug laden den
  Film selbst.
- Episoden-Einträge, deren Serien- oder Playlist-Bezug nicht auflösbar
  ist, laden die aktuelle Episode selbst — ein Folge-Kontext ist dafür
  nicht erforderlich; vorgeladene Folge-Episoden entfallen dann.
- Der Vorlauf läuft nach der Synchronisation, nach Server-Ereignissen
  mit Listen-Änderung und beim Einschalten der Einstellung; er ist
  gedrosselt (mindestens 60 Sekunden Abstand) und respektiert die
  WLAN-Bedingung.
- Bereits vorhandene oder laufende Downloads werden nicht doppelt
  angelegt — berührt der Vorlauf einen Kandidaten, der bereits
  fertig geladen ist, wird er stattdessen **verlängert**: das
  Ablaufdatum wird auf „jetzt + Aufbewahrungszeit" hochgesetzt und
  die Herkunft auf „Cache" umgestellt (auch ein manuell
  eingeplanter Download wird damit zum automatischen).
- Die Warteschlange arbeitet in Einplanungs-Reihenfolge (ältester
  wartender Auftrag zuerst) — dadurch werden playlist-gebundene
  Downloads auch in der Playlist-Reihenfolge ausgeführt, wie sie
  eingeplant wurden.

## Ablaufdatum und Bereinigung

- Jeder fertige Download erhält ein **Ablaufdatum**: Fertigstellung
  plus der eingestellten „Aufbewahrungszeit für Downloads"
  (1–365 Tage, Standard **7 Tage**). Wartende und laufende Aufträge
  tragen kein Ablaufdatum — die Frist beginnt erst mit „Fertig".
  Ein „Erneut versuchen" setzt das Ablaufdatum zurück (die Herkunft
  bleibt erhalten).
- Der **Bereinigungslauf** ist Teil des Abgleichs des Ablageorts
  (siehe unten) und läuft beim App-Start, bei Online-Rückkehr und
  beim Öffnen der Downloads-Seite — vollständig offline auf Basis
  des Manifests und der gecachten Listen. Für jeden abgelaufenen
  fertigen Eintrag gilt:
  - Ist das Medium weiterhin **Auto-Download-Kandidat** — als
    eigener Eintrag der Weiterschauen-Liste oder im Vorlauf-Fenster
    eines Eintrags (Folge-Episoden in Serienreihenfolge bzw.
    Nachfolger in Playlist-Reihenfolge bis zur eingestellten
    „Anzahl vorzuladender Episoden") —, wird er **nicht gelöscht**:
    das Ablaufdatum wird auf „jetzt + Aufbewahrungszeit" verlängert
    und die Herkunft auf „Cache" umgestellt. Bei jedem weiteren
    Lauf wird erneut geprüft — fällt der Kandidatenbezug weg und
    die neue Frist läuft ab, greift die normale Entfernung.
  - Andernfalls werden **Datei und Manifest-Eintrag entfernt** —
    wie ein manuelles „Löschen". Ist „Automatische Downloads"
    deaktiviert, gibt es keine Kandidaten.
- Laufende und wartende Aufträge sowie fehlerhafte und abgebrochene
  Einträge werden nie angefasst — fehlerhafte Einträge entfernt
  weiterhin nur die bewusste Anwenderaktion „Alle Fehler löschen".
- Änderungen der Aufbewahrungszeit oder Löschverzögerung wirken nur
  auf **neu gesetzte Ablaufdaten** — bestehende Fristen werden
  nicht nachträglich neu berechnet.
- **Altbestand:** Downloads aus der Zeit vor dieser Funktion tragen
  kein Ablaufdatum und gelten beim ersten Bereinigungslauf als
  abgelaufen — verbleibende Auto-Download-Kandidaten werden
  verlängert (Herkunft „Cache"), alle übrigen entfernt. Ihre
  Herkunft gilt als manuell (keine „Cache"-Kennzeichnung).

## Downloads nach dem Abspielen automatisch löschen

Ist die Einstellung **„Downloads nach dem Abspielen automatisch
löschen"** aktiv (Standard: aus), wird das Ablaufdatum des Downloads
eines als „angesehen" geltenden Mediums (Endfenster-Schwelle oder
plausibles Wiedergabe-Ende) auf **„jetzt + Löschverzögerung"**
gesetzt — statt einer Sofort-Löschung. Die eigentliche Entfernung
übernimmt der nächste Bereinigungslauf.

- Die Option gilt für **alle ladbaren Medientypen** (Filme und
  Episoden) und ersetzt die frühere, nur auf Episoden wirkende
  Einstellung „Gesehene Episoden automatisch löschen" — deren
  Schaltzustand wird übernommen.
- Die **„Löschverzögerung nach dem Abspielen"** ist eine eigene
  Einstellung in Minuten (0–1440, Standard **15**); der Wert 0 lässt
  das Ablaufdatum sofort erreichen und entspricht damit der
  früheren Sofort-Löschung (Entfernung beim nächsten Lauf).
- Laufende, wartende und fehlerhafte Downloads sowie Medien ohne
  abgeschlossenen Download (gestreamt) bleiben unberührt.

## Hintergrund-Downloads unter iOS

Auf iOS läuft die Dateiübertragung in einer systemseitigen
Hintergrund-Session: Downloads werden fortgesetzt, wenn die App in den
Hintergrund wechselt oder suspendiert wird. Schließt das System einen
Download, während die App beendet war, wird das Ergebnis beim nächsten
Start ins Download-Manifest übernommen (Eintrag „Fertig" bzw.
„Fehlgeschlagen"). Auf anderen Plattformen bleibt der prozessinterne
Transfer aktiv; unterbrochene Downloads werden dort bei der nächsten
Verbindung fortgesetzt (Neustart ab Position 0).

## Abgleich des Ablageorts

Beim App-Start und beim Öffnen der Downloads-Seite gleicht die App den
Video-Ablageort mit dem Download-Manifest ab:

- **Überholte Teildateien** (Reste abgebrochener, fehlgeschlagener oder
  unterbrochener Aufträge) werden automatisch entfernt.
- **Restdaten ohne Manifest-Eintrag** werden erkannt und mit ihrer Größe
  unter der Belegungszeile ausgewiesen („davon x GB nicht zugeordnet") —
  sie werden nicht automatisch gelöscht.
- Als „Fertig" geführte Einträge ohne Datei werden als fehlgeschlagen
  korrigiert und können über „Erneut versuchen" neu eingeplant werden.
- Zum Abschluss prüft die **Ablauf-Prüfung** alle fertigen Einträge:
  abgelaufene Downloads werden entfernt bzw. als Auto-Download-
  Kandidat verlängert (siehe „Ablaufdatum und Bereinigung").

Laufende Transfers und nicht abgelaufene fertige Downloads bleiben
vom Abgleich unberührt.

## Ablage und Wiedergabe

- Dateien und Manifest liegen außerhalb des Backups: die Dateien im
  no-backup-Video-Verzeichnis, das Manifest `download_manifest` in
  der Cache-Datenbank — ein Backup/Wiederherstellen erzeugt keine
  verwaisten Dateireferenzen.
- Die Wiedergabe (`PlaybackSourceService`) bevorzugt automatisch die
  lokale Datei eines abgeschlossenen Downloads; fehlt sie, fällt die
  Wiedergabe auf den Server-Stream zurück.
- **Wiedergabe-Schutz:** Wird ein heruntergeladenes Video abgespielt
  (lokale Datei als Quelle), setzt die App beim Wiedergabestart das
  Ablaufdatum auf **„jetzt + 2 × Restdauer"** hoch, falls es früher
  läge — der Bereinigungslauf entfernt die Datei nicht während des
  Abspielens. Ein weiter in der Zukunft liegendes Ablaufdatum bleibt
  unverändert; ohne bekannte Restdauer entfällt die Hochsetzung.

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
