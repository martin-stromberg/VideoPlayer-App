# Übersetzte Anforderung: Clientseitiger Zugriff auf die Server-API (Neuimplementierung)

Quelle: Entwicklungsschritt 1 des Projekts „Neugestaltung der App VideoPlayer" (Issue #9).

## Fachliche Zusammenfassung

Es wird ein neues, plattformneutrales .NET-Klassenbibliotheksprojekt als clientseitiger Zugriff auf die öffentliche HTTP-API der Serveranwendung „VideoPlayer" implementiert. Der bestehende Client (`VideoWebPlayer.Client`) wird nicht übernommen und dient nur als fachliche Referenz für Endpunkte und DTO-Felder. Der neue Zugriff kapselt Anmeldung (tokenbasiert, JWT), die serverseitige Zugangskontrolle für App-Clients (Client-Gate per `X-API-Key`-Header) sowie eine fest definierte Wiederholungsstrategie für fehlerhafte Serverantworten.

## Betroffene Klassen und Komponenten

- **Neues Projekt** in `VideoPlayer.App.sln` (Name und Platzierung sind Teil der Planung; Vorschlag: `VideoPlayer.ApiClient`, `net10.0`, keine MAUI-Abhängigkeit).
- **Datenmodellklassen (DTOs, neu geschrieben):** Auth-Request/Token, Medienquelle, Quellen-Genres, Recent-Entry, Continue-Watching-Eintrag samt Mutationsergebnis und Fortschrittsmeldung, Favoriteneintrag, paginierte Medieneinträge, polymorphe Medien-Hierarchie (Film, Filmkollektion, Serie, Staffel, Episode, Bild), Genre-Optionen.
- **Logikklassen / Services:** API-Client-Klasse mit Methoden je Endpunktgruppe; Wiederholungs-/Ausführungskomponente für die Retry-Strategie; Token-Verwaltung inkl. Erneuerung; Client-Gate-Konfiguration (API-Schlüssel).
- **Interfaces:** Abstraktionen für Token-Anbieter/-Erneuerung und Zeitverzögerung (Testbarkeit der Warteintervalle ohne echte Wartezeiten).
- **Exceptions/Fehlermodell:** Typisierte Ausnahme für API-Fehler inkl. HTTP-Statuscode.
- **UI-Komponenten / Controller:** Keine (reine Clientbibliothek).
- **Tests:** xUnit-Tests im bestehenden Projekt `VideoWebPlayer.Maui.Tests` gegen einen simulierten Server (falscher `HttpMessageHandler`).

## Abzudeckende Endpunkte (aus Anforderung und Referenz-Bestandsaufnahme)

- `POST api/auth/login` — Anmeldung, liefert Token (`token`, `expires`).
- `GET api/health` — Erreichbarkeitsprüfung.
- `GET api/Sources`, `GET api/Sources/{id}`, `GET api/SourceGenres/{id}` — Quellenübersicht und Quellen-Genres.
- `GET api/items/recent` — zuletzt hinzugefügte Inhalte („Neu im Programm").
- `GET api/continue-watching`, `POST api/continue-watching/progress`, `POST api/continue-watching/hide`, `POST api/continue-watching/skip` — Weiterschauen inkl. Fortschrittsmeldung, Ausblenden, Überspringen.
- `GET api/favorites`, `POST api/favorites/toggle`, `POST api/favorites/remove` — Favoriten (Abruf, Markieren/Umschalten, Entfernen).
- `GET api/items` (Parameter `mediaSourceId`, `page`, `size`, optional `search`, `genreId`) — paginierte Quelleneinträge mit Suche/Genre-Filter.
- `GET api/items/movie/{id}`, `api/items/moviecollection/{id}`, `api/items/tvshow/{id}`, `api/items/tvshowepisode/{id}` — Mediennachschlagen.
- `GET api/items/genres` — Genre-Optionen.
- `GET api/pictures/{id}`, `GET api/sourceicons/{id}` — Bildabrufe (Medienbilder, Quellen-Icons).

## Implementierungsansatz

- Neuer `HttpClient`-basierter Wrapper; alle Endpunktaufrufe laufen über eine zentrale Sende-Pipeline, die die Wiederholungsstrategie und die 401-Behandlung kapselt.
- **Wiederholungsstrategie (fest):** Bei wiederholbaren Fehlern bis zu drei Wiederholungen mit exponentiell wachsenden Warteintervallen 1 s, 2 s, 4 s (Interpretation siehe Offene Punkte). Wiederholbar sind: Serverfehler (5xx), Transportfehler/Timeouts sowie die Clientfehler `408 Request Timeout` und `429 Too Many Requests`. Nicht wiederholt werden eindeutige Clientfehler (sonstige 4xx). `401 Unauthorized` löst keinen normalen Retry aus, sondern eine einmalige Token-Erneuerung (stille Neuanmeldung) mit anschließendem einmaligen Neuversand der Anfrage.
- **Tokenbasierte Anmeldung:** `POST api/auth/login` liefert Bearer-Token; Folgeanfragen senden `Authorization: Bearer {token}`. Die Erneuerung bei 401 erfolgt über eine injizierbare Erneuerungslogik (Refresh-Delegate/Credentials), genau einmal pro Anfrage.
- **Client-Gate:** Jeder Request trägt `X-API-Key`; der Schlüssel ist per Build-Eigenschaft (MSBuild-Property → Assembly-Metadata) oder Umgebungsvariable konfigurierbar, mit Platzhalter-Fallback.
- **Deserialisierung:** `System.Text.Json` mit `PropertyNameCaseInsensitive`; polymorphe Medien-DTOs über `$type`-Diskriminator (Feldliste laut Referenz-Bestandsaufnahme `client-api.md`).
- Das alte Projekt `VideoWebPlayer.Client` bleibt unverändert; es wird in diesem Schritt nicht referenziert oder umgebaut.

## Konfiguration

- API-Schlüssel (Client-Gate): MSBuild-Build-Eigenschaft bzw. Umgebungsvariable; Auflösungsreihenfolge und Namen werden in der Planung festgelegt (Referenz: `MauiClientApiToken`/`VIDEOWEBPLAYER_MAUI_API_TOKEN`, Header `X-API-Key`).
- Server-Basisadresse: pro Client-Instanz konfigurierbar (z. B. über `HttpClient.BaseAddress` oder Optionsobjekt) — für Tests auf den simulierten Server zeigbar.

## Akzeptanzkriterien

1. Der neue Zugriff deckt die genannten Endpunkte ab und ist automatisiert testbar (simulierter Server oder lokal im IIS eingerichtetes Webprojekt „VideoPlayer").
2. Die Wiederholungsstrategie greift exakt wie definiert (Anzahl Versuche, Warteintervalle, Ausnahmen für 401/408/429).
3. Anmeldung inkl. tokenbasierter Folgeanfragen und einmaliger Erneuerung bei 401 funktioniert.
4. Kein Code des bisherigen Clients wird wiederverwendet.

## Offene Fragen

- Interpretation von „bis zu drei Versuche mit Warteintervallen 1 s, 2 s, 4 s": drei Warteintervalle implizieren einen Initialversuch + drei Wiederholungen (max. 4 HTTP-Anfragen); alternativ wären insgesamt 3 Versuche mit zwei Warteintervallen gemeint. Empfehlung: Initialversuch + bis zu 3 Wiederholungen (Wartezeiten 1 s/2 s/4 s), da drei Intervalle explizit genannt sind.
