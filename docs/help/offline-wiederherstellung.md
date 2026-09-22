# Offlinebetrieb und Wiederherstellung (Entwicklungsschritt 12)

Die neue VideoPlayer-App ist vollständig offline-tauglich: Ohne
Serververbindung bleibt die Startseite mit den lokalen Downloads,
den gecachten Favoriten und den zuletzt bekannten Listen nutzbar,
heruntergeladene Videos spielen aus der lokalen Datei — und nach
einer Wiederherstellung aus dem Cloud-Backup findet die App den
Weg zurück zum Server oder in einen verständlichen
Wiederanmelde-/Offline-Zustand.

## Offline-Modus der Startseite

- Erreicht die App den Server nicht und es liegen lokale Daten vor,
  wechselt sie in den Offline-Modus; ein Banner kündigt an:
  „Offline-Modus – zwischengespeicherte Inhalte werden angezeigt.
  Neue Inhalte, Quellen und Profildaten sind nicht verfügbar."
- **„Weiterschauen"** wird ausschließlich aus dem Download-Manifest
  gebildet: nur abgeschlossene Downloads, deren Datei tatsächlich
  noch vorhanden ist. Der Fortschrittsbalken kommt aus der lokalen
  Fortschritts-Warteschlange (`pending_progress`), ersatzweise aus
  dem letzten Server-Stand des Caches.
- **Platzhalter:** zur nächsten, noch nicht geladenen Episode einer
  Serie erscheint eine abgedunkelte Karte mit dem Badge „Nicht
  heruntergeladen" — sichtbar, aber nicht auswählbar. Voraussetzung
  ist eine zuvor online geladene Serienstruktur.
- **„Favoriten"** und **„Neu im Programm"** zeigen den letzten
  gecachten Stand.
- Ohne jeglichen lokalen Datenbestand führt ein nicht erreichbarer
  Server stattdessen in die Server-Einrichtung.

## Wiedergabe und Detailseiten offline

- Heruntergeladene Filme und Episoden spielen aus der lokalen Datei —
  die Quelle wird vorab aufgelöst (`PlaybackSourceService`); im
  Offline-Modus wird grundsätzlich kein Server-Stream angefragt.
- Detailseiten entstehen als **Offline-Snapshot** aus Manifest und
  gecachter Serienstruktur: geladene Episoden bleiben abspielbar,
  fehlende erscheinen abgedunkelt mit „Nicht heruntergeladen" und
  ohne Abspiel-/Download-Schalter.
- Serverseitige Aktionen (Favorit umschalten, Download starten) sind
  im Offline-Snapshot ausgeblendet; die Quellen-Einträge des Menüs
  werden offline entfernt — ohne Verbindung steht keine Quellenliste
  zur Verfügung.

## Profilseite offline

- Eine Warnung weist darauf hin, dass Profilinformationen ohne
  Serververbindung nicht verfügbar sind; der Server-Benutzerbereich
  wird ausgeblendet.
- Die lokale Installation (Profil, Serveradresse) und „Abmelden"
  bleiben verfügbar.

## Wiederherstellung nach Cloud-Backup

- Das Backup enthält bewusst **keine Geheimnisse** (Passwort,
  Anmelde-Token). Bekannte Server und der letzte Benutzername liegen
  dagegen in der Backup-Ablage — die App versucht zuerst den
  zuletzt verbundenen Server.
- Ist der Server erreichbar, nutzt die App zuerst ein gültiges
  gespeichertes Token, danach gespeicherte Zugangsdaten. Fehlen
  diese (typisch nach einer Wiederherstellung), erscheint die
  Anmeldemaske mit vorbefülltem Benutzernamen und dem Hinweis, dass
  die gespeicherte Anmeldung nicht mehr verfügbar ist.
- Ist der Server nicht erreichbar: mit lokalem Datenbestand →
  Offline-Modus, ohne → Server-Einrichtung.

## Wiederverbindung

Nach erfolgreicher Rückkehr ins Netz läuft automatisch:

1. stille Anmeldung (Token oder Zugangsdaten),
2. vollständige Synchronisation inklusive Nachsenden der
   Fortschritts-Warteschlange,
3. Aktualisierung der Startseiten-Listen,
4. Wiederaufnahme unterbrochener Downloads.

## Beschädigte lokale Daten

- **Einzelne Cache-Einträge** (z. B. eine defekte gecachte Liste oder
  Favoriten-Zeile) werden verworfen und online automatisch neu
  synchronisiert — sie brechen die Anzeige nicht.
- **Beschädigte Datenbankdateien** werden beim Start als
  `*.corrupt-<Zeitstempel>` beiseitegelegt und neu aufgebaut, statt
  die App abstürzen zu lassen; ein Hinweisbanner auf der Startseite
  informiert: „Die lokalen Daten waren beschädigt und wurden neu
  initialisiert. Zwischengespeicherte Inhalte werden neu
  synchronisiert."

## Serienkontext der Downloads

Damit die Offline-Liste die Folge-Episode kennt, speichert jeder
Episoden-Download Serie, Staffel- und Episodennummer im Manifest —
sowohl bei manuellen Downloads als auch beim automatischen Vorlauf
(staffelübergreifend in Serienreihenfolge). Die gecachte
Serienstruktur wird beim Online-Laden der Detailseite und des
Vorlaufs geschrieben; sie gehört zum verzichtbaren Cache und wird
nach einer Wiederherstellung beim nächsten Online-Laden erneuert.
