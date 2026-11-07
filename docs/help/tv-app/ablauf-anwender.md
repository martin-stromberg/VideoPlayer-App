← [Zurück zur Übersicht](index.md)

# Apple-TV-App (tvOS) — Ablauf für Anwender

## Voraussetzungen

- Ein erreichbarer VideoPlayer-Server im eigenen Netzwerk.
- Für die Kopplung: der Einmal-Code, die Kopplungsadresse oder der
  Kurzcode aus der Serververwaltung (Profilseite „Geräte") bzw. die
  Zugangsdaten des Servers.

## Schritt-für-Schritt-Anleitung

### 1. Server auswählen

Beim Erststart öffnet die App die Seite „Serververbindung". Wähle einen
Eintrag aus „Bekannte Server", starte „Server im Netzwerk suchen" und
wähle einen gefundenen Server, oder gib die Adresse manuell ein und
wähle „Verbinden". Alternativ verbindet die „Kopplungsadresse" aus der
Serververwaltung direkt — sie enthält den Server selbst.

> **Hinweis:** Wird kein Server gefunden, hilft die manuelle Eingabe;
  „Erneut prüfen" wiederholt die Erreichbarkeitsprüfung.

### 2. Gerät koppeln (falls verlangt)

Verlangt der Server eine Geräte-Kopplung, erscheint die Eingabe „Gerät
koppeln": Einmal-Code aus der Serververwaltung eingeben und „Koppeln"
wählen. Bei ungültigem oder abgelaufenem Code bzw. einer Versuchssperre
erscheint ein Hinweis — die Eingabe kann erneut versucht werden.

### 3. Anmelden

Nach der Kopplung (oder ohne Kopplungszwang) meldet die App gespeicherte
Zugangsdaten automatisch an. Fehlen sie oder wurden sie abgelehnt,
erscheint die Wahl: „Mit Kurzcode anmelden" (Kopplung und Anmeldung in
einem Schritt, ohne Passwort) oder „Mit Zugangsdaten anmelden"
(Benutzername und Passwort). Der bekannte Benutzername ist bereits
eingetragen. Texteingaben öffnen die tvOS-Tastatur — Diktat per
Siri Remote oder die Tastatur des verbundenen iPhone stehen zur
Verfügung.

### 4. Spätere Läufe

Bei bekanntem Server und gültigen Zugangsdaten startet die App ohne
Eingriff in den verbundenen Zustand. „Server wechseln" (Einstellungen)
öffnet die Serververwaltung erneut — die Menü-Taste der Fernbedienung
oder „Zurück zum Menü" kehrt zurück. „Abmelden" (Profil oder
Einstellungen) beendet die Sitzung und öffnet die Anmeldemaske. Ist der Server nicht erreichbar
und liegen lokale Daten vor, startet die App offline; sonst erscheint
die Serverauswahl.

## Ergebnis

Nach erfolgreicher Anmeldung öffnet der Menürahmen mit den Bereichen
Startseite, Favoriten, Playlists, Downloads, Profil und Einstellungen.
Gespeicherte Server und Zugangsdaten überstehen einen App-Neustart.

### 5. Startseite durchblättern

Die Startseite zeigt horizontale Reihen: „Weiterschauen" (mit
Fortschrittsbalken unter dem Bild), „Favoriten", „Neu im Programm"
und — zeitweise — besondere Genre-Sektionen des Servers. Mit der
Siri Remote bewegst du den Fokus zwischen den Einträgen einer Reihe
und von Reihe zu Reihe; die fokussierte Karte vergrößert sich
sichtbar. Gespeicherte Inhalte erscheinen sofort, Aktualisierungen
des Servers folgen automatisch im Hintergrund und bei jeder Rückkehr
in den Vordergrund. Wähle einen Eintrag mit der Touch-Fläche: es
öffnet sich die Detailansicht des Mediums (siehe Schritt 7).
Zurück geht es mit der Menü-Taste. Ohne
Serververbindung zeigt die Startseite die lokal gespeicherten
Bereiche mit einem Offline-Hinweis; leere Reihen entfallen.

### 6. Quellen durchsuchen

Die Reihe „Quellen" auf der Startseite führt die freigegebenen
Medienbereiche des Servers. Wähle eine Quelle: die Browsing-Seite
öffnet sich im Vollbild mit dem Quellennamen als Titel. Darunter
findest du das Suchfeld — bei Auswahl öffnet sich die tvOS-Tastatur,
Diktat per Siri Remote oder die Tastatur des verbundenen iPhone geben
den Suchtext ein; er bleibt sichtbar und filtert die Liste ab der
ersten Seite. Liefert die Quelle Genres, erscheint eine
Filterleiste — „Alle" zeigt die ungefilterte Liste. Das Poster-Raster
darunter zeigt Titel und Vorschaubilder und lädt am Listenende
selbsttätig weitere Einträge nach. Eintrags-Auswahl öffnet die
Detailansicht; die Menü-Taste kehrt
zur Startseite zurück. Ladefehler zeigen „Erneut versuchen" — sowohl
bei der Quellen-Reihe als auch in der Browsing-Seite.

### 7. Detailansicht nutzen

Die Detailansicht zeigt zu einem Film, einer Filmsammlung, einer
Serie, einer Staffel oder einer Episode Titel, Jahr, Genres,
Beschreibung und Bildmaterial. Markierungen kennzeichnen gesehene
Inhalte („✓ Gesehen"), lokal vorhandene Dateien („Auf diesem Gerät")
und einen Offline-Stand ohne Serververbindung. Bei Serien wählst du
Staffeln über die Reihe der Staffel-Kacheln; darunter liegt die
Episodenreihe — jede Episode zeigt Gesehen-Status und den
lokalen-Datei-Hinweis, nicht geladene Offline-Episoden sind als
„nicht heruntergeladen" abgedunkelt. Eine Episode oder ein Film der
Sammlung öffnet die eigene Detailansicht; die Menü-Taste führt eine
Ebene zurück. Die Aktionsleiste bietet „Abspielen" (bei Filmen und
Episoden), „Herunterladen" sowie „★/☆ Favorit" — die
Favoriten-Markierung gilt sofort auf dem Server und in der App;
„Abspielen" startet die Wiedergabe im Vollbild-Player (Fortsetzung
an der gespeicherten Position), „Herunterladen" plant den Download
des Mediums ein (bei Episoden samt Serienkontext) und meldet das
Ergebnis als Hinweistext — auch die Meldung, wenn das
Speicherplatzkontingent erreicht ist.
Ohne Serververbindung
zeigt die Seite den gespeicherten Stand mit deaktiviertem
Favoriten-Schalter und ohne Download-Aktion; „Abspielen" bleibt bei
vorhandener lokaler Datei verfügbar.

### 8. Playlists nutzen

Der Menübereich „Playlists" führt deine eigenen Listen („Meine
Playlists") und die öffentlichen Playlists des Servers in einer
gruppierten Liste — mit Cover, Name, Sortiermodus und Genres. Der
Bereich braucht eine Serververbindung; bei einem Ladefehler steht
„Erneut versuchen" zur Verfügung. Wähle eine Playlist: die Detailseite
zeigt Kopfzeilen (Name, Eintragszahl, Sortiermodus, Genres,
Beschreibung) und darunter die Einträge als Poster-Raster mit Titel
und übergeordnetem Kontext (z. B. Serie und Episodennummer); am
Listenende lädt die Seite selbsttätig weitere Einträge nach. Die
Aktion „▶ Abspielen" startet die Wiedergabe der gesamten Playlist;
die Auswahl eines abspielbaren Eintrags (Film, Episode) startet die
Wiedergabe ab genau dort — andere Einträge (Serie, Staffel,
Sammlung) öffnen ihre Detailansicht. Während der Wiedergabe zeigt der
Infobereich die Playlist-Position; über die Transport-Leiste wechselst
du zum nächsten bzw. vorherigen Eintrag, am Titelende schaltet die
App gemäß Einstellung automatisch weiter. Die Menü-Taste führt eine
Ebene zurück. Das Bearbeiten von Playlists (Anlegen, Umbenennen,
Sortieren, Einträge, Öffentlich-Schalter) ist auf dem TV nicht
möglich — dafür dient die Hauptanwendung.

### 9. Downloads verwalten

Der Menübereich „Downloads" führt die Einträge des Download-Manifests:
Vorschaubild, Titel, Zustand (Wartend/Läuft/Fertig/Fehler/Abgebrochen),
bei laufenden Downloads ein Fortschrittsbalken samt übertragener
Datenmenge, bei fertigen das Ablaufdatum — jeder Download läuft nach
der eingestellten Aufbewahrungszeit ab und wird dann automatisch
entfernt. Im Kopf steht die Belegung des Speicherplatzkontingents
sowie ein Hinweis auf nicht zugeordnete Restdaten. Downloads werden
über „Herunterladen" in der Detailansicht eingeplant und laufen auch
weiter, wenn die App im Hintergrund liegt oder suspendiert ist.
Wähle einen Eintrag: ein Menü bietet „Abbrechen" (bei laufenden),
„Erneut versuchen" (bei fehlgeschlagenen/abgebrochenen) und
„Löschen". Die Kopf-Aktion „Fehlerhafte löschen" entfernt alle
fehlgeschlagenen und abgebrochenen Einträge gesammelt. Die Seite
funktioniert ohne Serververbindung; vom System geräumte Dateien
erscheinen nach dem automatischen Abgleich als fehlgeschlagene
Einträge und können erneut eingeplant werden. Fertige Downloads
stehen der Wiedergabe auch offline als Quelle zur Verfügung.

### 10. Einstellungen anpassen

Der Menübereich „Einstellungen" bündelt die wenigen, auf dem TV
sinnvollen Werte. Unter „Downloads" stellst du die Aufbewahrungszeit
für Downloads (in Tagen) und das Speicherplatzkontingent (in GB) ein —
wähle den Eintrag und bestätige den gewünschten Wert in der
Auswahlliste; beide wirken auf Ablaufdatum und Obergrenze der
Downloads. Unter „Wiedergabe" schaltest du die „Auto-Fortsetzung" ein
oder aus (automatischer Übergang zur nächsten Episode bzw. zum
nächsten Playlist-Eintrag am Titelende) und legst das
„Angesehen"-Endfenster fest — ab wie vielen Sekunden bzw. Minuten vor
dem Ende ein Titel als gesehen gilt. Alle Werte bleiben über
Neustarts der App erhalten. Der Abschnitt „Konto" zeigt den
angemeldeten Benutzer und bietet „Abmelden"; im Abschnitt „Server"
stehen Adresse und Verbindungszustand sowie „Server wechseln".

## Barrierefreiheit

Sämtliche Elemente sind über die tvOS-Focus-Engine per Siri Remote
erreichbar; Texteingaben nutzen die Systemtastatur mit Diktat- und
Continuity-Tastatur-Unterstützung.
