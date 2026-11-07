# Server-API-Client — Fehlerbehebung

## `ApiException` mit `StatusCode` 401 (Unauthorized)

**Symptom:** Aufrufe schlagen mit `ApiException` (401) fehl.

**Ursache:** Kein/abgelaufenes Bearer-Token und keine erfolgreiche Erneuerung — `TokenRefresher` ist nicht gesetzt oder liefert `null`; alternativ ist der Client-Gate-Schlüssel ungültig (Server lehnt App-Clients ohne gültigen `X-API-Key` ab).

**Lösung:**
1. `LoginAsync` aufrufen oder `SetAccessToken` mit gültigem Token.
2. `TokenRefresher` in `VideoPlayerApiClientOptions` hinterlegen (z. B. stille Neuanmeldung mit gespeicherten Zugangsdaten).
3. Client-Gate-Schlüssel prüfen (gepaartes Geräte-Token, Umgebungsvariable `VIDEOWEBPLAYER_MAUI_API_TOKEN`, Build-Eigenschaft `VideoPlayerApiToken`, Token-Datei `client-api-token.txt`, explizites `ApiKey`).

## `CheckHealthAsync` liefert `GateRejected`

**Symptom:** Der Server ist erreichbar, aber die Erreichbarkeitsprüfung meldet `HealthCheckResult.GateRejected` (HTTP 401 vom Client-Gate).

**Ursache:** Der verwendete Gate-Schlüssel wird vom Server abgelehnt — typischerweise ein widerrufenes Geräte-Token oder ein ungültiger statischer Schlüssel.

**Lösung:**
1. Bei gepaartem Gerät: Gerät erneut koppeln (Einmal-Code in der Server-Oberfläche erzeugen); die App verwirft das widerrufene Token selbständig und prüft einmalig den statischen Schlüssel nach.
2. Statischen Schlüssel prüfen (`VIDEOWEBPLAYER_MAUI_API_TOKEN`, `VideoPlayerApiToken`-Build-Eigenschaft, Token-Datei).

> **Hinweis:** Auf dem Referenzserver ist `api/health` nicht hinter dem Client-Gate — `GateRejected` aus `CheckHealthAsync` ist dort nicht erreichbar und dient als Defense-in-depth für Server mit geschütztem Health-Endpunkt. Der primäre Widerrufs-Detektor der App ist die Login-Probe `DeviceTokenService.IsGateRejectedAsync` (`POST api/auth/login` mit leerem Body und nur dem `X-API-Key`-Header; 401 mit `Content-Type: application/problem+json` = Gate-Ablehnung, 401 mit `text/plain` oder 400 = Schlüssel akzeptiert). Die Probe erzeugt keine IP-Sperr-Fehlversuche, da die serverseitige Fehlerzählung nur im Pairing-Endpunkt läuft.

## `ApiException` vom Pairing-Endpunkt (`api/pairing/exchange`)

**Symptom:** `DevicePairingClient.ExchangeAsync` schlägt mit `ApiException` fehl.

**Ursache und Lösung je Statuscode:**

| `StatusCode` | Ursache | Lösung |
|--------------|---------|--------|
| 400 | Anfrage fachlich ungültig | Code-Eingabe prüfen (8 Zeichen, Alphabet ohne I/L/O/0/1) |
| 401 | Code ungültig, abgelaufen oder bereits verwendet | Neuen Einmal-Code in der Server-Oberfläche erzeugen |
| 404 | Server ohne Pairing-Endpunkt | Server aktualisieren — es gibt keinen automatischen Rückfall auf einen statischen Token |
| 429 | Ratenlimit/IP-Sperre nach zu vielen Versuchen | Sperrung abwarten, dann erneut versuchen (kein automatisches Wiederholen) |
| `null` | Server nicht erreichbar / Timeout | Adresse und Netzwerkverbindung prüfen |

Zusätzlich kann eine `CryptographicException` auftreten, wenn die Pairing-Antwort beschädigt oder nicht entschlüsselbar ist — dann den Vorgang mit einem neuen Code wiederholen.

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
