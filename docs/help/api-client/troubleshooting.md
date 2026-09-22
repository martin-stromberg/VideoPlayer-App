# Server-API-Client — Fehlerbehebung

## `ApiException` mit `StatusCode` 401 (Unauthorized)

**Symptom:** Aufrufe schlagen mit `ApiException` (401) fehl.

**Ursache:** Kein/abgelaufenes Bearer-Token und keine erfolgreiche Erneuerung — `TokenRefresher` ist nicht gesetzt oder liefert `null`; alternativ ist der Client-Gate-Schlüssel ungültig (Server lehnt App-Clients ohne gültigen `X-API-Key` ab).

**Lösung:**
1. `LoginAsync` aufrufen oder `SetAccessToken` mit gültigem Token.
2. `TokenRefresher` in `VideoPlayerApiClientOptions` hinterlegen (z. B. stille Neuanmeldung mit gespeicherten Zugangsdaten).
3. Client-Gate-Schlüssel prüfen (Umgebungsvariable `VIDEOWEBPLAYER_MAUI_API_TOKEN`, Build-Eigenschaft `VideoPlayerApiToken`, explizites `ApiKey`).

## `ApiException` mit `StatusCode` 5xx oder `null`

**Symptom:** Anfragen scheitern nach vier Sendevorgängen (Initialversuch + 3 Wiederholungen mit 1 s/2 s/4 s).

**Ursache:** Serverfehler bzw. nicht erreichbarer Server (Transportfehler/Timeout).

**Lösung:**
1. Server-Erreichbarkeit prüfen (`CheckHealthAsync`).
2. Basisadresse und Netzwerkverbindung prüfen.
3. Server-Logs auswerten.

## `InvalidOperationException` beim Client-Aufbau

**Symptom:** Konstruktor von `VideoPlayerApiClient` wirft `InvalidOperationException`.

**Ursache:** Weder `HttpClient.BaseAddress` noch `VideoPlayerApiClientOptions.BaseAddress` gesetzt.

**Lösung:** Basisadresse an genau einer der beiden Stellen konfigurieren.

> **Hinweis:** Eindeutige Clientfehler (4xx außer 401/408/429) werden bewusst nicht wiederholt — sie deuten auf einen fachlichen Fehler (z. B. ungültige Id) statt einer Störung hin.
