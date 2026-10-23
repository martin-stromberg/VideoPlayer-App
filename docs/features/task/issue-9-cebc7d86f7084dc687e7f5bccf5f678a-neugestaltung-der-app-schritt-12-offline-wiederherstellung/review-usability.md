# Usability-Review

Geprüft: alle UI-Änderungen des Schritts 12 (Startseite, Medien-
Carousel, Detailseite, Profilseite) gegen die Anforderungen —
Verständlichkeit der Offline-Zustände, keine irreführenden Aktionen,
klare Wiederherstellungs-Kommunikation. Die MAUI-Schicht lässt sich
in dieser Umgebung nicht interaktiv prüfen (Windows-Target baut,
kein Gerät/Simulator) — Prüfung daher anhand der XAML-/Code-Struktur
und der Contract-Tests; dokumentierte Einschränkung.

## Ergebnis

**Status:** Keine offenen Befunde

## Geprüfte Flächen

### Startseite (`HomePage.xaml`, `MediaCarousel.xaml`)

- [x] **Offline-Banner:** dauerhaft sichtbar im Offline-Modus mit
  konkretem Text („zwischengespeicherte Inhalte werden angezeigt.
  Neue Inhalte, Quellen und Profildaten sind nicht verfügbar") — der
  Nutzer erkennt Zustand und Einschränkung auf einen Blick.
- [x] **Ablage-Hinweis (`StorageNoticeBanner`):** erklärt in einem
  Satz, dass lokale Daten beschädigt waren und neu synchronisiert
  werden — keine technischen Begriffe (SQLite/Korruption).
- [x] **Platzhalter-Karten:** fehlende Folge-Episoden bleiben sichtbar
  (Serie „geht weiter"), sind aber abgedunkelt (`CardOpacity` 0,55)
  und tragen das Badge „Nicht heruntergeladen" — keine tote Karte,
  keine spielbar wirkende Fläche.
- [x] **Platzhalter nicht auswählbar:** `IsSelectable = false`
  verhindert Navigation; ein Tap führt nicht in eine leere
  Detailseite.
- [x] **Leerzustand:** ohne zwischengespeicherte Inhalte erklärt ein
  Satz, warum nichts angezeigt wird — kein unbeschrifteter leerer
  Screen.
- [x] **Fortschrittsbalken offline:** erscheint nur, wenn ein
  Fortschritt bekannt ist (`ShowProgress`) — keine Balken bei 0 %
  auf Platzhaltern.

### Detailseite (`MediaDetailsPage.xaml(.cs)`)

- [x] **Episoden-Platzhalter:** Zeile abgedunkelt, Label „Nicht
  heruntergeladen"; ▶- und ⤓-Aktionen werden per Trigger
  ausgeblendet — der Nutzer kann nichts „Kaputtes" auslösen.
- [x] **Serverseitige Aktionen unterbunden:** im Offline-Snapshot
  sind Download- und Favoriten-Schalter ausgeblendet/deaktiviert
  (statt Fehlermeldung beim Tippen).
- [x] **Abspielen nur bei lokaler Datei:** der Play-Schalter erscheint
  nur, wenn die Datei tatsächlich vorliegt — kein „Spinner ins
  Leere" im Offline-Modus.
- [x] **Fehlertexte unverändert:** nicht ablegbare Typen behalten den
  bestehenden, verständlichen Fehlertext.

### Profilseite (`ProfilePage.xaml(.cs)`)

- [x] **Offline-Warnung** erklärt, warum keine Profildaten sichtbar
  sind; der Server-Benutzerbereich wird ausgeblendet statt
  Platzhalter- oder Stale-Daten zu zeigen.
- [x] **Verfügbare Bereiche bleiben:** lokale Installation und
  Abmelden funktionieren offline — keine Sackgasse.

### Wiederherstellung / Re-Anmeldung

- [x] **Re-Anmelde-Hinweis** („Die gespeicherte Anmeldung ist nicht
  mehr verfügbar. Bitte melde dich erneut an.") erscheint auf der
  Login-Seite — der Nutzer versteht, warum die Anmeldemaske kommt.
- [x] **Benutzername vorbefüllt** (`LoginUsername` aus dem
  bekannten Stand) — ein Tipp weniger.
- [x] **Transiente Fehler** führen nicht zur Anmeldemaske — kein
  irritierendes „Ausloggen" bei kurzem Netzaussetzer.

## Einschränkungen

- Keine interaktive Sichtprüfung (kein Emulator/Gerät in dieser
  Umgebung); Kontraste und Abstände folgen den bestehenden
  Banner-/Karten-Stilen der App.
- Offline-Platzhalter zeigen kein Poster (kein lokales Bild) — die
  Titel-Fallback-Darstellung greift.
