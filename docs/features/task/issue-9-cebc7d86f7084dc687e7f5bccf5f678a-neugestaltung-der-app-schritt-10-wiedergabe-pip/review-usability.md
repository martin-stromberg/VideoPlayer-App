# Usability-Review

Betrachtete Änderungen (aus Endanwendersicht, ohne Kenntnis der
Implementierung):

- `VideoPlayer.Maui/Pages/MediaDetailsPage.xaml` + `.xaml.cs`
  („Abspielen"-Schaltflächen im Kopf und je Episode)
- `VideoPlayer.Maui/Pages/PlayerPage.xaml` + `.xaml.cs`
  (Wiedergabeseite mit Titel-/Fehler-/Lade-Overlays und
  Mini-Player-Schalter)
- `VideoPlayer.Maui/Services/PictureInPictureService.cs`
  (Windows-Mini-Player)
- `VideoPlayer.Maui/Platforms/iOS/Info.plist`
  (Hintergrund-Audio für PiP)

## Ergebnis

**Status:** Keine Befunde (mit Hinweisen)

## Prüfschritte / Benutzerflüsse

### Wiedergabe starten

- Auf der Detailseite eines Films oder einer Episode erscheint im
  Kopfbereich neben dem Titel die Schaltfläche **„Abspielen"**; bei
  Serien, Staffeln und Kollektionen fehlt sie bewusst — dort wird
  über die Episoden- bzw. Filmliste gespielt.
- Jede Episode der Episodenliste trägt rechts eine ▶-Schaltfläche —
  direkter Start aus der Liste, wie im Altbestand.
- Die Wiedergabeseite öffnet sich mit dem Titel des Mediums; während
  die Quelle aufgelöst wird, läuft ein Ladeindikator — kein
  leerer/schwarzer Moment ohne Rückmeldung.

### Fortsetzung und Fortschritt

- Ein begonnenes Medium springt nach dem Öffnen auf den gemerkten
  Stand der Weiterschauen-Liste (online vom Server, offline aus der
  gespeicherten Kopie) — die Fortsetzung ist nicht sichtbar
  „nachgeladen", sondern erfolgt vor dem ersten Bild.
- Der Fortschritt wird alle 5 Sekunden gemeldet; ohne
  Serververbindung geht nichts verloren (Warteschlange) und wird bei
  der nächsten Online-Rückkehr nachgesendet — für den Anwender
  unsichtbar.
- Nahe am Ende erscheint dezent der Hinweis **„Angesehen"** im Kopf —
  passend zur Merkliste des Servers.

### Automatische Fortsetzung (Serien)

- Ist die Auto-Fortsetzung eingeschaltet (Einstellung
  „Automatisch nächste Episode"), startet am Ende bzw. in den letzten
  Minuten automatisch die nächste Episode — staffelübergreifend in
  der richtigen Reihenfolge.
- Wichtig: Pausiert der Anwender in der Schlussphase, bleibt das
  Medium stehen — die Fortsetzung greift nur bei laufender
  Wiedergabe.

### Fehlerfall

- Ist eine Quelle nicht verfügbar oder bricht die Wiedergabe ab,
  erscheint unten eine verständliche Meldung („Das Video kann nicht
  abgespielt werden."); bei Serien startet automatisch die nächste
  Episode — die Fehlermeldung bleibt als Hinweis sichtbar, blockiert
  aber nicht.

### Picture-in-Picture / Mini-Player

- **Windows:** der Schalter **„Mini-Player"** im Kopf verkleinert das
  App-Fenster zu einem kompakten, immer vordergrundigen Overlay mit
  nur dem Videobild; erneutes Umschalten stellt das normale Fenster
  wieder her. Beim Verlassen der Seite wird der Normalmodus
  automatisch wiederhergestellt.
- **iOS:** die PiP-Schaltfläche steckt in der nativen
  Transportsteuerung des Players — vertrauter Ort, kein zweiter
  Schalter nötig; die Wiedergabe läuft im PiP-/Hintergrundmodus
  weiter (Hintergrund-Audio ist deklariert).

### Zurück / Schließen

- Der Zurück-Pfeil der Shell verlässt die Wiedergabeseite; der
  erreichte Stand wird abschließend gemeldet — ein späterer
  Wiedereinstieg setzt am selben Punkt an.

## Hinweise (keine Befunde)

- Der Mini-Player-Schalter heißt bewusst „Mini-Player" statt „PiP" —
  für Endanwender der verständlichere Begriff; die Funktion ist die
  geforderte kompakte Overlay-Darstellung.
- Unter iOS gibt es keinen eigenen Umschalter (PiP ist in die
  Plattform-Steuerung integriert) — dokumentierte Designentscheidung,
  keine fehlende Funktion.
- Downloads sind noch nicht umgesetzt (Schritt 11) — die
  lokale-Datei-Wiedergabe ist aber bereits verdrahtet und greift,
  sobald das Download-Manifest gefüllt ist.
