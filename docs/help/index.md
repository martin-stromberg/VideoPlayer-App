# Dokumentation

Übersicht über alle dokumentierten Funktionsbereiche.

## App

- [Neues App-Grundgerüst](app-grundgeruest.md) — Startvorgang, Seitenmenü und Platzhalterseiten der neu aufgebauten App.
- [Einstellungen und lokale Datenhaltung](einstellungen-datenhaltung.md) — Einstellungsseite (Darstellung, Downloads, Wiedergabe, schaltbares Programmprotokoll mit Ablauf-Einträgen und Versand), Backup-Trennung der Ablageorte, Geheimnisse und Profile.
- [Serververwaltung und Erststart-Einrichtung](serververwaltung-erststart.md) — Erststart-Dialog, Netzwerk-Erkennung (mDNS/UDP), bekannte Server, „Server wechseln" und Geräte-Kopplung per Einmal-Code (Pairing).
- [Verbindung und Anmeldung](verbindung-anmeldung.md) — automatischer Verbindungsaufbau beim Start, Anmeldemaske, Offline-Modus, Token-Erneuerung, Re-Pairing bei widerrufenem Geräte-Token und Verhalten nach einer Backup-Wiederherstellung.
- [Netzwerkstatus und Synchronisation](netzwerk-synchronisation.md) — Online↔Offline-Erkennung zur Laufzeit, automatische Wiederverbindung, Echtzeit-Ereignisse des Servers (SignalR), persistente Fortschritts-Warteschlange und Hintergrund-Synchronisation.
- [Startseite mit gecachten Medienlisten](startseite-medienlisten.md) — „Weiterschauen", „Favoriten" und „Neu im Programm" mit Cache-First-Laden, Poster-Ablage außerhalb des Backups, ereignisgesteuerter Aktualisierung, direkter Wiedergabe ab Fortsetzungsposition für abspielbare Weiterschauen-Einträge und Detail-Navigation, Kontextmenü der Weiterschauen-Liste per Long-Press („Ausblenden"/„Überspringen") sowie der Hero-Kopfbereich mit servergeneriertem Bild (ausblendbar über die Einstellung „Kopfbereich anzeigen").
- [Quellen-Browsing und Medien-Detailseiten](quellen-browsing-details.md) — Quellen als dynamische Menüeinträge mit Icons, Genre-Filter, Textsuche und seitenweises Nachladen sowie Detailseiten für Filme, Kollektionen, Serien mit Staffeln/Episoden, der vollbreite Kopfbereich mit Hintergrundgrafik nach Web-Vorbild, das Angesehen-Badge in der Episodenliste und die Favoriten-Markierung.
- [Favoriten- und Profil-Seite](favoriten-profil.md) — Favoritenliste des Server-Benutzers mit Poster/Titel/Medientyp, Entfernen mit Server als führendem System, Cache-only-Anzeige offline sowie Profilansicht mit Benutzer, lokalem Profil und Abmeldung.
- [Playlists-Übersicht, -Detailansicht, -Wiedergabe und -Verwaltung](playlists.md) — Menüseite „Playlists" mit eigenen und öffentlichen Playlists (Cover, Sortiermodus, Genres; reines Online-Feature mit verständlicher Fehleranzeige), Detailansicht mit Einträgen in Server-Reihenfolge und seitenweisem Nachladen, Wiedergabe aus der Playlist-Reihenfolge inklusive playlist-gebundenem Weiterschauen sowie Verwaltung eigener Playlists (Anlage, Bearbeitung, Löschung, Einträge, Reihenfolge, Sortiermodus, Genres, Öffentlich-Schalter).
- [Wiedergabe, Picture-in-Picture und Fortschrittsverfolgung](wiedergabe-pip.md) — Wiedergabeseite mit MediaElement, Server-Stream und lokale Dateien, Fortsetzungsposition, Offline-Fortschrittswarteschlange, „angesehen"-Schwelle, Auto-Fortsetzung, Fehler-Rückfall sowie Mini-Player (Windows) und nativem PiP (iOS) — inklusive frei erreichbarer nativer Vollbild-/PiP-Schaltflächen auf dem iPad.
- [Download-Verwaltung und automatische Downloads](downloads.md) — Downloads-Seite mit Status/Fortschritt und Aktionen, parallele Transfers mit GB-Kontingent, WLAN-only, automatische Wiederaufnahme nach Neustart/Netzwerkverlust, Weiterschauen-Vorlauf und automatisches Löschen gesehener Episoden.
- [Offlinebetrieb und Wiederherstellung](offline-wiederherstellung.md) — Offline-Startseite aus lokalen Downloads mit Platzhaltern fehlender Episoden, lokale Wiedergabe, Offline-Snapshots der Detailseiten, Re-Anmeldung nach Backup-Wiederherstellung, Neu-Synchronisation verworfener Caches und Wiederherstellung beschädigter Datenbanken.
- [Erscheinungsbild](erscheinungsbild.md) — dunkles Design der Webanwendung: zentrale Design-Tokens (Farben, Abstufungen, Rahmen), Button-Radien (normal 4, Karten 8, Chips/Badges pill-förmig) und die Festlegung auf das dunkle Farbschema.

## Infrastruktur

- [Server-API-Client](api-client/index.md) — Plattformneutraler Zugriff auf die öffentliche VideoPlayer-Server-API mit Wiederholungsstrategie, Token-Erneuerung und Geräte-Pairing-Endpunkt.
- [Developer-API für UI-Automatisierung](developer-api.md) — lokaler Mini-HTTP-Server (nur Debug/Opt-in) mit `GET /model`, `GET /tree`, `POST /invoke` und `POST /set-text` für automatisierte UI-Tests.
- [Windows-End-to-End-Tests](e2e-tests.md) — FlaUI-Testprojekt, das die echte `VideoPlayer.Maui.exe` mit isoliertem Profil startet und Setup, Pairing, Anmeldung, den Serverwechsel sowie den Wiedergabestart gegen einen lokalen Mock-Server durchspielt.
- [Staging-Branch](staging-branch.md) — Branching- und Release-Prozess.
