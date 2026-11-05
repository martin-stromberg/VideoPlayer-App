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
oder „Zurück zum Menü" kehrt zurück. „Abmelden" (Profil) beendet die
Sitzung und öffnet die Anmeldemaske. Ist der Server nicht erreichbar
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
öffnet sich das vorbereitete Ziel mit einem Hinweis — die
Detailansicht und die Wiedergabe folgen in späteren Schritten.
Zurück geht es mit der Menü-Taste oder „Zurück". Ohne
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
selbsttätig weitere Einträge nach. Eintrags-Auswahl öffnet das
vorbereitete Ziel (Detailansicht folgt später); die Menü-Taste kehrt
zur Startseite zurück. Ladefehler zeigen „Erneut versuchen" — sowohl
bei der Quellen-Reihe als auch in der Browsing-Seite.

## Barrierefreiheit

Sämtliche Elemente sind über die tvOS-Focus-Engine per Siri Remote
erreichbar; Texteingaben nutzen die Systemtastatur mit Diktat- und
Continuity-Tastatur-Unterstützung.
