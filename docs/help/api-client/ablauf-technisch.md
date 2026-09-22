← [Zurück zur Übersicht](index.md)

# Server-API-Client — Technischer Ablauf

## Übersicht

Alle öffentlichen Methoden von `VideoPlayerApiClient` laufen über die zentrale Sende-Pipeline `SendWithPolicyAsync`. Diese kapselt die feste Wiederholungsstrategie sowie die einmalige Token-Erneuerung bei `401 Unauthorized`.

## Ablauf

### 1. Anfrage senden

Die Pipeline erzeugt pro Versuch über eine Request-Factory eine neue `HttpRequestMessage` (Nachrichten sind nicht mehrfach sendbar) und sendet sie über den `HttpClient`. Der `X-API-Key`-Header (Client-Gate) und das Bearer-Token werden über `DefaultRequestHeaders` mitgeführt.

Beteiligte Komponenten:
- `VideoPlayerApiClient.SendWithPolicyAsync` — Sendeschleife
- `ClientGate.HeaderName` — Header-Name `X-API-Key`

### 2. Antwort auswerten

- **2xx:** Antwort wird zurückgegeben und ggf. deserialisiert (`PropertyNameCaseInsensitive`).
- **401 Unauthorized:** Einmalig `VideoPlayerApiClientOptions.TokenRefresher` aufrufen (serialisiert über `SemaphoreSlim` `tokenRefreshGate`). Liefert der Delegat ein `AuthToken`, wird es per `SetAccessToken` angewendet und die Anfrage erneut gesendet — ohne das Retry-Budget zu verbrauchen. Bei `null`-Rückgabe oder erneutem 401 wird `ApiException` geworfen.
- **408, 429, 5xx oder Transportfehler (`HttpRequestException`, Timeout-`TaskCanceledException`):** Wiederholung, solange der Versuchszähler < 3 ist; Wartezeit aus `RetryDelays` (1 s, 2 s, 4 s) über `VideoPlayerApiClientOptions.DelayAsync`.
- **Übrige 4xx:** Sofort `ApiException` — keine Wiederholung.
- **Caller-Abbruch:** `OperationCanceledException` bei gecanceltem `CancellationToken` wird ohne Verzögerung durchgereicht.

### 3. Ergebnis

Erfolgsantworten werden als DTO (`MediaEntry`-Hierarchie, `MediaSource`, `RecentEntry`, `ContinueWatchingEntry`, `FavoriteEntry`, `MediaItemSummary`, `GenreOption`, `SourceGenres`) bzw. `byte[]` (Bildabrufe) zurückgegeben; Fehler als `ApiException` mit `Endpoint`, `StatusCode` und `ResponseBody`.

## Fehlerbehandlung

| Fehler | Verhalten |
|--------|-----------|
| 401 | Einmalige Token-Erneuerung + Neuversand; danach `ApiException` |
| 408 / 429 / 5xx | Bis zu 3 Wiederholungen (1 s/2 s/4 s), danach `ApiException` |
| Transportfehler / Timeout | Bis zu 3 Wiederholungen (1 s/2 s/4 s), danach `ApiException` mit `StatusCode = null` |
| Übrige 4xx | Sofort `ApiException` |
| Caller-Abbruch | `OperationCanceledException` durchgereicht |
