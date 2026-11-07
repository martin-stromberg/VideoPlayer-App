← [Zurück zur Übersicht](index.md)

# Server-API-Client — Technischer Ablauf

## Übersicht

Alle öffentlichen Methoden von `VideoPlayerApiClient` laufen über die zentrale Sende-Pipeline `SendWithPolicyAsync`. Diese kapselt die feste Wiederholungsstrategie sowie die einmalige Token-Erneuerung bei `401 Unauthorized`. Ausnahme: `DevicePairingClient` (öffentlicher Pairing-Endpunkt) sendet direkt — ohne `X-API-Key`-/Bearer-Header, ohne Wiederholungen und ohne Token-Erneuerung.

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

### 4. Pairing-Austausch (Sonderpfad außerhalb der Pipeline)

`DevicePairingClient.ExchangeAsync` tauscht einen Einmal-Code gegen ein Geräte-Token — bewusst ohne `X-API-Key`-/Bearer-Header, ohne Wiederholungen und ohne `TokenRefresher` (429 muss sofort sichtbar sein, 401 darf keine Erneuerung auslösen):

1. `PairingCrypto.CreateKeyPair()` erzeugt ein flüchtiges ECDH-Schlüsselpaar (nistP256); `PairingCrypto.ExportPublicKeyBase64` exportiert den öffentlichen Schlüssel als SPKI/DER-Base64.
2. `POST api/pairing/exchange` geht als absolute Adresse gegen den Pairing-Server mit camelCase-Body `code`, `clientPublicKey`, `deviceName` (DTOs `PairingExchangeRequest`/`PairingExchangeResponse`).
3. `PairingCrypto.DecryptToken` leitet aus dem `serverPublicKey` der Antwort per `DeriveKeyFromHmac` (SHA-256) den 32-Byte-Schlüssel ab und entschlüsselt `encryptedToken` per AES-256-GCM (`nonce(12) ‖ ciphertext ‖ tag(16)`).
4. HTTP-Fehler werden als `ApiException` mit `StatusCode` (400/401/404/429) bzw. `null` bei Transportfehlern gemeldet; unentschlüsselbare Antworten werfen `CryptographicException`.

Das entschlüsselte Token wird außerhalb dieser Bibliothek sicher hinterlegt und über `ClientGate.DeviceTokenProvider` bzw. `VideoPlayerApiClient.SetApiKey` als `X-API-Key` aktiviert.

## Fehlerbehandlung

| Fehler | Verhalten |
|--------|-----------|
| 401 | Einmalige Token-Erneuerung + Neuversand; danach `ApiException` |
| 408 / 429 / 5xx | Bis zu 3 Wiederholungen (1 s/2 s/4 s), danach `ApiException` |
| Transportfehler / Timeout | Bis zu 3 Wiederholungen (1 s/2 s/4 s), danach `ApiException` mit `StatusCode = null` |
| Übrige 4xx | Sofort `ApiException` |
| Caller-Abbruch | `OperationCanceledException` durchgereicht |
