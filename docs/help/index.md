# Dokumentation

Übersicht über alle dokumentierten Funktionsbereiche.

## App

- [Neues App-Grundgerüst](app-grundgeruest.md) — Startvorgang, Seitenmenü und Platzhalterseiten der neu aufgebauten App.
- [Einstellungen und lokale Datenhaltung](einstellungen-datenhaltung.md) — Einstellungsseite, Backup-Trennung der Ablageorte, Geheimnisse und Profile.
- [Serververwaltung und Erststart-Einrichtung](serververwaltung-erststart.md) — Erststart-Dialog, Netzwerk-Erkennung (mDNS/UDP), bekannte Server und „Server wechseln".
- [Verbindung und Anmeldung](verbindung-anmeldung.md) — automatischer Verbindungsaufbau beim Start, Anmeldemaske, Offline-Modus, Token-Erneuerung und Verhalten nach einer Backup-Wiederherstellung.
- [Netzwerkstatus und Synchronisation](netzwerk-synchronisation.md) — Online↔Offline-Erkennung zur Laufzeit, automatische Wiederverbindung, Echtzeit-Ereignisse des Servers (SignalR), persistente Fortschritts-Warteschlange und Hintergrund-Synchronisation.
- [Startseite mit gecachten Medienlisten](startseite-medienlisten.md) — „Weiterschauen", „Favoriten" und „Neu im Programm" mit Cache-First-Laden, Poster-Ablage außerhalb des Backups, ereignisgesteuerter Aktualisierung und Detail-Navigation.
- [Quellen-Browsing und Medien-Detailseiten](quellen-browsing-details.md) — Quellenliste mit Icons und Offline-Hinweis, Genre-Filter, Textsuche und seitenweises Nachladen sowie Detailseiten für Filme, Kollektionen, Serien mit Staffeln/Episoden und die Favoriten-Markierung.
- [Favoriten- und Profil-Seite](favoriten-profil.md) — Favoritenliste des Server-Benutzers mit Poster/Titel/Medientyp, Entfernen mit Server als führendem System, Cache-only-Anzeige offline sowie Profilansicht mit Benutzer, lokalem Profil und Abmeldung.
- [Wiedergabe, Picture-in-Picture und Fortschrittsverfolgung](wiedergabe-pip.md) — Wiedergabeseite mit MediaElement, Server-Stream und lokale Dateien, Fortsetzungsposition, Offline-Fortschrittswarteschlange, „angesehen"-Schwelle, Auto-Fortsetzung, Fehler-Rückfall sowie Mini-Player (Windows) und nativem PiP (iOS).

## Infrastruktur

- [Server-API-Client](api-client/index.md) — Plattformneutraler Zugriff auf die öffentliche VideoPlayer-Server-API mit Wiederholungsstrategie und Token-Erneuerung.
- [Staging-Branch](staging-branch.md) — Branching- und Release-Prozess.
