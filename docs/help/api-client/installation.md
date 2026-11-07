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

1. Gepaartes Geräte-Token über `ClientGate.DeviceTokenProvider` (wird vom `DeviceTokenService` der App nach dem Pairing bzw. beim Serverwechsel gesetzt)
2. Umgebungsvariable `VIDEOWEBPLAYER_MAUI_API_TOKEN`
3. Assembly-Metadata `VideoPlayer.ApiClient.ApiToken` — gesetzt über die MSBuild-Build-Eigenschaft `VideoPlayerApiToken`:
   ```bash
   dotnet build VideoPlayer.ApiClient/VideoPlayer.ApiClient.csproj -p:VideoPlayerApiToken="<MAUI_CLIENT_API_TOKEN>"
   ```
   Das Deployment-Skript `scripts/iOS-Deployment.ps1` reicht die Eigenschaft über den Parameter `-VideoPlayerApiToken` (Standard: Umgebungsvariable `VIDEOWEBPLAYER_MAUI_API_TOKEN`) in alle Build-/Publish-Response-Dateien durch und maskiert den Wert in der Konsolenanzeige (`-p:VideoPlayerApiToken=***`). Diese Einbettung ist die transitionale Zwischenlösung für Server ohne Pairing-Endpunkt und Alt-App-Versionen. Werte werden beim Eintragen in die Response-Datei getrimmt; enthält ein Wert ein Anführungszeichen, eine Raute oder einen Zeilenumbruch, bricht das Skript mit einer verständlichen Fehlermeldung und der maskierten Zeile ab — statt des kryptischen MSBuild-Fehlers `MSB1006`. Schlägt der `dotnet`-Aufruf fehl, gibt das Skript die Response-Datei-Inhalte maskiert aus (Secrets als `***`).
4. Token-Datei `client-api-token.txt` im lokalen Anwendungsdatenverzeichnis (`%LocalAppData%/VideoPlayer`)
5. Platzhalter `<MAUI_CLIENT_API_TOKEN>`

`ClientGate.ResolveStatic()` liefert nur die statischen Quellen 2–5 (ohne Geräte-Token) — die App nutzt sie zur Feststellung, ob ein Pairing erforderlich ist, und als Rückfall nach dem Verwerfen eines widerrufenen Geräte-Tokens.

Der statische Wert muss backendseitig zu `Jwt:ApiToken:Maui` passen. Der Schlüssel ist kein Ersatz für die Benutzeranmeldung; produktive Werte gehören nicht ins Repository.

## Geräte-Pairing

Ohne nutzbaren Gate-Schlüssel (weder Geräte-Token noch statische Quelle) koppelt die App das Gerät über den öffentlichen Endpunkt `POST api/pairing/exchange`: Der Server erzeugt einen Einmal-Code (8 Zeichen, serverseitig über `Pairing:CodeLength`/`Pairing:CodeTtlMinutes` konfigurierbar), die App tauscht ihn per `DevicePairingClient` gegen ein verschlüsseltes Geräte-Token (ECDH P-256 + AES-256-GCM), hinterlegt es in der sicheren Ablage (`AppSecretsService`, profil- und serveradress-spezifisch) und nutzt es fortan als `X-API-Key`. Ein serverseitig widerrufenes Token wird verworfen und per Re-Pairing ersetzt.

Ist ein statischer Schlüssel konfiguriert (Quellen 2–4), entfällt das Pairing — die App verbindet sich wie bisher. Schlägt dagegen der Pairing-Austausch an einem Server ohne Endpunkt fehl (HTTP 404), gibt es bewusst **keinen** automatischen Laufzeit-Fallback auf einen statischen Token: Der statische Schlüssel muss dann beim Publish/Start über die Quellen 2–4 bereitgestellt sein.

## Umgebungsvariablen

| Variable | Pflicht | Beispielwert | Beschreibung |
|----------|---------|--------------|--------------|
| `VIDEOWEBPLAYER_MAUI_API_TOKEN` | Nein | `<MAUI_CLIENT_API_TOKEN>` | Client-Gate-Schlüssel zur Laufzeit |

## Überprüfung

`CheckHealthAsync()` liefert `HealthCheckResult.Reachable`, sobald Server erreichbar und Client-Gate-Schlüssel akzeptiert werden. `GateRejected` (HTTP 401) weist auf einen ungültigen bzw. widerrufenen Gate-Schlüssel hin — bei einem Geräte-Token ist eine erneute Kopplung erforderlich.
