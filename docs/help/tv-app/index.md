# Apple-TV-App (tvOS)

Eigenständige App für Apple TV als Grundlage des geplanten Lean-back-Players.
Aktueller Stand: Serververbindung und Lean-back-Startseite — der
plattformneutrale MVU-Zustandskern läuft mit tvOS-Effekt-Handler und
Plattform-Adaptern; Serverwahl, Geräte-Kopplung, Anmeldung,
Serverwechsel und Abmeldung sind per Siri Remote bedienbar. Die
Startseite zeigt fokussierbare Karussells („Weiterschauen",
„Favoriten", „Neu im Programm" und saisonale Genres) aus dem
lokalen Cache mit Servernachladung. Das Browsing ist als
Lean-back-Oberfläche umgesetzt: Quellen-Auswahl auf der Startseite,
modale Browsing-Seite mit Genre-Filter („Alle"), Suchfeld
(Diktat/Continuity-Tastatur) und seitenweise nachladendem
Poster-Raster; Einträge öffnen ein vorbereitetes Navigationsziel.
Kompilierbarkeit wird über einen macOS-CI-Job fortlaufend gesichert.

## Inhalt

- [Beschreibung](beschreibung.md)
- [Ablauf für Anwender](ablauf-anwender.md)
- [Installation, CI-Build und Kompatibilität](installation.md)
