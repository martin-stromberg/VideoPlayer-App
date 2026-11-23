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
Poster-Raster. Detailansichten für Film, Filmsammlung, Serie, Staffel
und Episode zeigen Bildmaterial, Metadaten, Gesehen-/lokale-Datei-
Kennzeichnung, Staffel- und Episodenauswahl sowie die Aktionen
Abspielen, Herunterladen und Favoriten-Umschaltung;
offline greift der gespeicherte Detail-Snapshot. Die Wiedergabe
läuft im nativen Vollbildplayer mit Fortschritts-Tracking und
Auto-Fortsetzung; Playlists sind lesend angebunden. Manuelle
Downloads laufen suspensionstauglich über eine
Hintergrund-URL-Session — Einplanung von der Detailseite,
Download-Liste mit Zustand und Fortschritt, Abbrechen,
Wiederholen und Löschen, Ablaufdatum und Quota-Obergrenze sowie
Abgleich von Teildateien, fehlenden Dateien und Waisen.
Kompilierbarkeit wird über einen macOS-CI-Job fortlaufend gesichert;
ein signierter Release-Pfad mit TestFlight-Upload ist als
gate-geschaltete Paketierungs-Aktion vorbereitet
(`TVOS_SIGNING_ENABLED`).

## Inhalt

- [Beschreibung](beschreibung.md)
- [Ablauf für Anwender](ablauf-anwender.md)
- [Installation, CI-Build und Kompatibilität](installation.md)
- [Signiertes Release-Paket und App-Store-Connect-Upload](deployment.md)
