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

## Barrierefreiheit

Sämtliche Elemente sind über die tvOS-Focus-Engine per Siri Remote
erreichbar; Texteingaben nutzen die Systemtastatur mit Diktat- und
Continuity-Tastatur-Unterstützung.
