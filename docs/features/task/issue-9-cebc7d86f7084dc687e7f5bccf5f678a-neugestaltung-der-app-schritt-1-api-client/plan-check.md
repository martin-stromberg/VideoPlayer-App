# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan vollständig

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| Zugriff deckt die genannten Endpunkte ab | Endpunkt-Tabelle in „Programmabläufe" deckt Login, Health, Sources (+Genres), Recent, Continue-Watching (GET/progress/hide/skip), Favorites (GET/toggle/remove), Items (paged/genres/movie/moviecollection/tvshow/tvshowepisode), Pictures, SourceIcons ab; Umsetzungsreihenfolge Schritte 4–5 | `VideoPlayerApiClientTests_Endpoints` (Methoden-/Pfad-Mapping als Theory über alle Endpunkte, DTO-Deserialisierung) | Abgedeckt |
| Automatisiert testbar (simulierter Server oder lokaler IIS) | `StubHttpMessageHandler`-Testinfrastruktur, Schritt 6–7; IIS-Server antwortet mit 500 und ist als Testziel nicht verlässlich — simulierter Server ist von der Anforderung ausdrücklich zugelassen | Sämtliche geplante Tests laufen gegen den simulierten Server | Abgedeckt |
| Wiederholungsstrategie greift exakt (3 Wiederholungen, 1 s/2 s/4 s; keine Wiederholung bei eindeutigen Clientfehlern außer 401/408/429) | Ablauf „Wiederholungsstrategie", Designentscheidung zur Interpretation, `DelayAsync`-Injection für exakte Intervall-Verifikation | `ServerError_RetriesUpToThreeTimes_WithExponentialDelays`, `..._ThrowsAfterExhaustion`, `ClientError_NotRetried` (400/403/404/422), `RetriableClientError_Retried` (408/429), `TransportError_Retried`, `Timeout_Retried`, `CallerCancellation_NotRetried` | Abgedeckt |
| Anmeldung + tokenbasierte Folgeanfragen + einmalige Erneuerung bei 401 | Ablauf „Anmeldung und tokenbasierte Folgeanfragen", `TokenRefresher`-Delegat mit `SemaphoreSlim`, einmalig pro Anfrage | `VideoPlayerApiClientTests_Authentication`, `VideoPlayerApiClientTests_TokenRefresh` (Refresh erfolgreich/fehlgeschlagen/zweites 401/Login ohne Reauth) | Abgedeckt |
| Client-Gate per API-Schlüssel, konfigurierbar über Build-Eigenschaft/Umgebungsvariable | `ClientGate` + `VideoPlayerApiToken`-MSBuild-Property + `VIDEOWEBPLAYER_MAUI_API_TOKEN` + Options-`ApiKey` | `ClientGateTests`, `AllRequests_ContainClientGateHeader` | Abgedeckt |
| Kein Code des bisherigen Clients wiederverwendet | Neues Projekt `VideoPlayer.ApiClient`, neue DTO-Namen, keine Referenz auf `VideoWebPlayer.Client`; Altprojekt bleibt unverändert | Indirekt: neues Projekt ohne ProjectReference auf den Altclient; Code-Review prüft zusätzlich | Abgedeckt |

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| — (kein Benutzerfluss; reine Clientbibliothek ohne UI) | — | Nicht erforderlich — begründet im Plan (kein über UI/Nutzeraktion erreichbarer Ablauf; Funktionsnachweis via simuliertem Server, von der Anforderung zugelassen) |

## Hinweise

- Die Interpretation der Retry-Anzahl (Initialversuch + 3 Wiederholungen = max. 4 Sendevorgänge) ist im Plan als Designentscheidung mit Begründung dokumentiert; „Offene Punkte" ist bewusst leer.
- Testdaten/Fixtures: `StubHttpMessageHandler` und JSON-Beispielantworten sind als Testhilfen geplant (Schritt 6–7 der Umsetzungsreihenfolge).
- Bestehende Tests sind nicht betroffen (Baseline 8/8 grün, additive Änderung).
