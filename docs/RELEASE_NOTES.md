# Release Notes

## Important Notes Before Update

- There are no special notices.

## What's New

- New platform-neutral server API client (`VideoPlayer.ApiClient`): login, health check, sources, recently added items, continue watching (progress/hide/skip), favorites, media lookup and picture retrieval.
- Fixed retry strategy for failed server responses (retries with 1 s/2 s/4 s delays) and one-time token renewal on “unauthorized”.
- Client gate via `X-API-Key` header, configurable through environment variable `VIDEOWEBPLAYER_MAUI_API_TOKEN`, build property `VideoPlayerApiToken` or client options.
- New app foundation (`VideoPlayer.Maui` + `VideoPlayer.Mvu`): startup with a loading screen and server reachability check, side menu navigation (Home, Sources, Favorites, Downloads, Profile, Settings) and placeholder pages. The new app uses its own lightweight MVU cycle and runs alongside the existing app, which remains unchanged.

## Wichtige Hinweise vor dem Update

- Es gibt keine besonderen Hinweise.

## Neuerungen

- Neuer plattformneutraler Server-API-Client (`VideoPlayer.ApiClient`): Anmeldung, Erreichbarkeitsprüfung, Quellen, „Neu im Programm", Weiterschauen (Fortschritt/Ausblenden/Überspringen), Favoriten, Mediennachschlagen und Bildabrufe.
- Feste Wiederholungsstrategie bei fehlerhaften Serverantworten (Wiederholungen nach 1 s/2 s/4 s) und einmalige Token-Erneuerung bei „nicht autorisiert".
- Client-Gate per `X-API-Key`-Header, konfigurierbar über die Umgebungsvariable `VIDEOWEBPLAYER_MAUI_API_TOKEN`, die Build-Eigenschaft `VideoPlayerApiToken` oder die Client-Optionen.
- Neues App-Grundgerüst (`VideoPlayer.Maui` + `VideoPlayer.Mvu`): Startvorgang mit Ladeanzeige und Erreichbarkeitsprüfung des Servers, Seitenmenü-Navigation (Startseite, Quellen, Favoriten, Downloads, Profil, Einstellungen) und Platzhalterseiten. Die neue App nutzt einen eigenen schlanken MVU-Kreislauf und läuft parallel zur bisherigen App, die unverändert bestehen bleibt.
