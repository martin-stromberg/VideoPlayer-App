← [Zurück zur Übersicht](index.md)

# Server-API-Client — Installation und Konfiguration

## Voraussetzungen

- .NET 10 (`net10.0`); keine MAUI-Abhängigkeit — die Bibliothek ist plattformneutral.
- Projektreferenz auf `VideoPlayer.ApiClient`.
- Erreichbarer VideoPlayer-Server (öffentliche HTTP-API).

## Konfiguration

| Parameter | Typ | Standardwert | Beschreibung |
|-----------|-----|--------------|--------------|
| `VideoPlayerApiClientOptions.BaseAddress` | `Uri?` | `HttpClient.BaseAddress` | Server-Basisadresse; genau eine der beiden Quellen ist Pflicht |
| `VideoPlayerApiClientOptions.ApiKey` | `string?` | `ClientGate.Resolve()` | Client-Gate-Schlüssel (`X-API-Key`) |
| `VideoPlayerApiClientOptions.TokenRefresher` | `Func<CancellationToken, Task<AuthToken?>>?` | `null` | Erneuerungslogik bei 401 (z. B. stille Neuanmeldung); `null` = 401 wird direkt als `ApiException` gemeldet |
| `VideoPlayerApiClientOptions.DelayAsync` | `Func<TimeSpan, CancellationToken, Task>?` | `Task.Delay` | Verzögerungsimplementierung der Wiederholungsstrategie (Tests) |
| `VideoPlayerApiClientOptions.Logger` | `ILogger?` | `null` | Optionale Diagnoseausgaben |

## Client-Gate-Schlüssel — Auflösungsreihenfolge (`ClientGate.Resolve()`)

1. Umgebungsvariable `VIDEOWEBPLAYER_MAUI_API_TOKEN`
2. Assembly-Metadata `VideoPlayer.ApiClient.ApiToken` — gesetzt über die MSBuild-Build-Eigenschaft `VideoPlayerApiToken`:
   ```bash
   dotnet build VideoPlayer.ApiClient/VideoPlayer.ApiClient.csproj -p:VideoPlayerApiToken="<MAUI_CLIENT_API_TOKEN>"
   ```
3. Platzhalter `<MAUI_CLIENT_API_TOKEN>`

Der Wert muss backendseitig zu `Jwt:ApiToken:Maui` passen. Der Schlüssel ist kein Ersatz für die Benutzeranmeldung; produktive Werte gehören nicht ins Repository.

## Umgebungsvariablen

| Variable | Pflicht | Beispielwert | Beschreibung |
|----------|---------|--------------|--------------|
| `VIDEOWEBPLAYER_MAUI_API_TOKEN` | Nein | `<MAUI_CLIENT_API_TOKEN>` | Client-Gate-Schlüssel zur Laufzeit |

## Überprüfung

`CheckHealthAsync()` liefert `true`, sobald Server erreichbar und Client-Gate-Schlüssel akzeptiert werden.
