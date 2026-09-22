# Release Notes

## Important Notes Before Update

- There are no special notices.

## What's New

- New platform-neutral server API client (`VideoPlayer.ApiClient`): login, health check, sources, recently added items, continue watching (progress/hide/skip), favorites, media lookup and picture retrieval.
- Fixed retry strategy for failed server responses (retries with 1 s/2 s/4 s delays) and one-time token renewal on “unauthorized”.
- Client gate via `X-API-Key` header, configurable through environment variable `VIDEOWEBPLAYER_MAUI_API_TOKEN`, build property `VideoPlayerApiToken` or client options.
- New app foundation (`VideoPlayer.Maui` + `VideoPlayer.Mvu`): startup with a loading screen and server reachability check, side menu navigation (Home, Sources, Favorites, Downloads, Profile, Settings) and placeholder pages. The new app uses its own lightweight MVU cycle and runs alongside the existing app, which remains unchanged.
- New settings management (`VideoPlayer.Storage`): the Settings page now offers automatic downloads, number of episodes to preload, Wi-Fi-only downloads, a download quota in gigabytes (default 5 GB), automatic deletion of watched episodes, auto-resume and the watched time window. All values persist across app restarts and are validated against their allowed ranges.
- Structured local storage with backup separation: backup-relevant data (server list, last connected server, profile data, settings, unsynchronized progress) is kept apart from rebuildable data (offline videos, thumbnails, cached lists, download manifest). On iOS the non-backup locations are excluded via the platform backup attribute, on Windows backup-relevant data goes to the roaming profile directory. Secrets (passwords, tokens) are stored exclusively in platform secure storage and never enter backups; separate profiles via `VIDEOPLAYER_PROFILE` are fully isolated.
- Server management with first-run setup (`VideoPlayer.Storage` + `VideoPlayer.Maui`): on first launch without a configured server a setup dialog offers manual address entry and automatic network discovery (mDNS `_http._tcp.local.` plus UDP broadcast on port 5001). Only servers that pass a live connection check are saved; afterwards the app starts directly with the last connected server. A discreet “Switch server” entry in Settings lists known servers, allows adding new ones and switches the active server without touching any other setting.

## Wichtige Hinweise vor dem Update

- Es gibt keine besonderen Hinweise.

## Neuerungen

- Neuer plattformneutraler Server-API-Client (`VideoPlayer.ApiClient`): Anmeldung, Erreichbarkeitsprüfung, Quellen, „Neu im Programm", Weiterschauen (Fortschritt/Ausblenden/Überspringen), Favoriten, Mediennachschlagen und Bildabrufe.
- Feste Wiederholungsstrategie bei fehlerhaften Serverantworten (Wiederholungen nach 1 s/2 s/4 s) und einmalige Token-Erneuerung bei „nicht autorisiert".
- Client-Gate per `X-API-Key`-Header, konfigurierbar über die Umgebungsvariable `VIDEOWEBPLAYER_MAUI_API_TOKEN`, die Build-Eigenschaft `VideoPlayerApiToken` oder die Client-Optionen.
- Neues App-Grundgerüst (`VideoPlayer.Maui` + `VideoPlayer.Mvu`): Startvorgang mit Ladeanzeige und Erreichbarkeitsprüfung des Servers, Seitenmenü-Navigation (Startseite, Quellen, Favoriten, Downloads, Profil, Einstellungen) und Platzhalterseiten. Die neue App nutzt einen eigenen schlanken MVU-Kreislauf und läuft parallel zur bisherigen App, die unverändert bestehen bleibt.
- Neue Einstellungsverwaltung (`VideoPlayer.Storage`): Die Seite „Einstellungen" bietet nun automatische Downloads, Anzahl vorzuladender Episoden, Downloads nur im WLAN, ein Speicherplatzkontingent in Gigabyte (Standard 5 GB), automatisches Löschen gesehener Episoden, Auto-Fortsetzung und das „Als angesehen"-Zeitfenster. Alle Werte bleiben über App-Neustarts erhalten und werden auf ihre zulässigen Bereiche geprüft.
- Strukturierte lokale Datenhaltung mit Backup-Trennung: Backup-pflichtige Daten (Serverliste, zuletzt verbundener Server, Profildaten, Einstellungen, unsynchronisierte Fortschritte) sind von neu aufbaubaren Daten (Offline-Videos, Vorschaubilder, zwischengespeicherte Listen, Download-Manifest) getrennt. Unter iOS werden die nicht zu sichernden Ablageorte über das Plattform-Backup-Attribut ausgeschlossen, unter Windows landen backup-pflichtige Daten im Roaming-Profilverzeichnis. Geheimnisse (Passwörter, Token) liegen ausschließlich in der sicheren Plattformablage und gelangen nie ins Backup; getrennte Profile über `VIDEOPLAYER_PROFILE` sind vollständig isoliert.
- Serververwaltung mit Erststart-Einrichtung (`VideoPlayer.Storage` + `VideoPlayer.Maui`): Beim ersten Start ohne hinterlegten Server bietet ein Einrichtungsdialog die manuelle Adresseingabe und die automatische Netzwerk-Erkennung (mDNS `_http._tcp.local.` sowie UDP-Broadcast auf Port 5001). Gespeichert wird nur, wer eine echte Verbindungsprüfung besteht; danach startet die App direkt mit dem zuletzt verbundenen Server. Ein dezenter Eintrag „Server wechseln" in den Einstellungen listet bekannte Server, erlaubt das Hinzufügen und wechselt den aktiven Server, ohne die übrigen Einstellungen zu verändern.
