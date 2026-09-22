# Abnahmeprüfung – Entwicklungsschritt 1

## Ergebnis

**Status:** Anforderung vollständig erfüllt

## Abweichungen

- Keine.

## Hinweise

Geprüft auf Branch `task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app-schritt-1-api-client` (HEAD: `6abd844`), Diff gegen `task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app`.

### Endpunkt-Abdeckung (gegen `VideoPlayer.ApiClient/VideoPlayerApiClient.cs`)

| Geforderter Bereich | Implementierung | Fundstelle |
|---|---|---|
| Anmeldung | `LoginAsync` → `POST api/auth/login`, setzt Bearer-Token für Folgeanfragen | `VideoPlayerApiClient.cs:87-100` |
| Erreichbarkeitsprüfung | `CheckHealthAsync` → `GET api/health` | `VideoPlayerApiClient.cs:107-122` |
| Quellenübersicht | `GetSourcesAsync`, `GetSourceAsync`, `GetSourceGenresAsync` → `api/Sources`, `api/Sources/{id}`, `api/SourceGenres/{id}` | `VideoPlayerApiClient.cs:129-146` |
| „Neu im Programm" | `GetRecentEntriesAsync` → `GET api/items/recent` | `VideoPlayerApiClient.cs:153-156` |
| Weiterschauen | `GetContinueWatchingAsync` (`api/continue-watching`), `ReportPlaybackProgressAsync` (`api/continue-watching/progress`), `HideContinueWatchingEntryAsync` (`…/hide`), `SkipContinueWatchingEntryAsync` (`…/skip`) | `VideoPlayerApiClient.cs:163-219` |
| Favoriten | `GetFavoritesAsync` (`api/favorites`), `ToggleFavoriteAsync` (`api/favorites/toggle`), `RemoveFavoriteAsync` (`api/favorites/remove`) | `VideoPlayerApiClient.cs:226-246` |
| Mediennachschlagen | `GetSourceItemsAsync` (`api/items` mit Suche/Genre-Filter/Paging), `GetMovieAsync` (`api/items/movie/{id}`), `GetMovieCollectionAsync` (`api/items/moviecollection/{id}`), `GetTvShowAsync` (`api/items/tvshow/{id}`, inkl. `Seasons`/`Episodes` — Staffeln sind wie im Altbestand in der Serienantwort eingebettet, kein eigener Endpunkt), `GetTvShowEpisodeAsync` (`api/items/tvshowepisode/{id}`), `GetGenreOptionsAsync` (`api/items/genres`) | `VideoPlayerApiClient.cs:256-313` |
| Bildabrufe | `GetPictureAsync` (`api/pictures/{id}`), `GetSourceIconAsync` (`api/sourceicons/{id}`) | `VideoPlayerApiClient.cs:320-341` |

### Wiederholungsstrategie (`SendWithPolicyAsync`, `VideoPlayerApiClient.cs:394-468`)

- Feste Warteintervalle `RetryDelays` = 1 s / 2 s / 4 s (`Zeile 21-26`), `MaxRetries = 3` (`Zeile 28`): ein Initialversuch plus bis zu drei Wiederholungen. Diese Auslegung ist die einzig konsistente Lesart der Anforderung — drei angegebene Warteintervalle erfordern vier Sendevorgänge.
- Wiederholt werden 5xx, 408 (`RequestTimeout`), 429 (`TooManyRequests`) sowie Transportfehler (`HttpRequestException`, `TaskCanceledException`/Timeout); übrige 4xx-Fehler werden sofort als `ApiException` weitergereicht (`IsRetriable`, `Zeile 463-468`).
- Bei 401 erfolgt außerhalb des Wiederholungsbudgets genau eine Token-Erneuerung über `VideoPlayerApiClientOptions.TokenRefresher` mit Neuversand (`Zeile 429-443`, `TryRefreshTokenAsync` `Zeile 470-494`, serialisiert per `SemaphoreSlim`); schlägt die Erneuerung fehl oder antwortet der Server erneut mit 401, wird `ApiException` geworfen. Der Login-Aufruf selbst ist von der Erneuerung ausgenommen (`allowReauth: false`, `Zeile 96`).
- Abbruch durch den Aufrufer (`CancellationToken`) wird nicht wiederholt (`Zeile 411-414`).

### Client-Gate und Token-Authentifizierung

- `X-API-Key`-Header auf jeder Anfrage via `DefaultRequestHeaders` (`VideoPlayerApiClient.cs:55-61`).
- Auflösung des Schlüssels (`ClientGate.cs`): Umgebungsvariable `VIDEOWEBPLAYER_MAUI_API_TOKEN` → Assembly-Metadata `VideoPlayer.ApiClient.ApiToken` (gesetzt über MSBuild-Eigenschaft `VideoPlayerApiToken`, `VideoPlayer.ApiClient.csproj:9-15`) → Platzhalter `<MAUI_CLIENT_API_TOKEN>`; alternativ explizit über `VideoPlayerApiClientOptions.ApiKey`.
- Tokenbasierte Anmeldung: Bearer-Token aus der Login-Antwort (`AuthToken.Token`), verwaltet über `SetAccessToken`/`ClearAccessToken`/`CurrentToken` (`VideoPlayerApiClient.cs:64-79`).

### Testnachweis

- `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj` ausgeführt: **69/69 Tests bestanden** (0 Fehler, 0 übersprungen), darunter 61 neue Tests.
- Tests laufen gegen einen simulierten Server (`Stubs/StubHttpMessageHandler.cs` mit Request-Aufzeichnung und Antwort-Queue; `RecordingDelays` zeichnet Warteintervalle ohne reale Wartezeit auf). Damit ist die automatisierte Testbarkeit erfüllt; ein Test gegen den lokalen IIS-Server ist nicht erforderlich („oder einen simulierten Server").
- Abgedeckt durch Tests: Endpunkt-Mapping (Methode/Pfad/API-Key-Header) für alle 20 Endpunkte (`VideoPlayerApiClientTests_Endpoints.cs`), Retry-Verhalten inkl. exakter Delays 1 s/2 s/4 s, kein Retry bei 400/403/404/422, Retry bei 408/429/Transport/Timeout, kein Retry bei Caller-Abbruch (`VideoPlayerApiClientTests_RetryPolicy.cs`), Login + Bearer auf Folgeanfragen + Gate-Header (`VideoPlayerApiClientTests_Authentication.cs`), einmaliger Token-Refresh inkl. Fehlschlag-/Doppel-401-Fälle und Login ohne Reauth (`VideoPlayerApiClientTests_TokenRefresh.cs`), Client-Gate-Auflösungsreihenfolge (`ClientGateTests.cs`), `$type`-Polymorphie (`MediaEntrySerializationTests.cs`).

### Keine Wiederverwendung des bisherigen Clients

- `VideoPlayer.ApiClient` hat keine `ProjectReference` auf `VideoWebPlayer.Client` (nur `Microsoft.Extensions.Logging.Abstractions`); die Sende-Pipeline (Request-Factory, Retry-Schleife, Options-Pattern, `ClientGate`) ist eine Neuentwicklung ohne Entsprechung im Altbestand (der alte Client kannte nur den 401-Einzel-Retry).
- Die DTO-Felder der neuen Modelle (`VideoPlayer.ApiClient/Models/`) spiegeln zwangsläufig den Wire-Vertrag des Altbestands (gleiche Endpunkte/Felder — genau dafür dient der alte Code als fachliche Referenz); Typen, Namen und Dateistruktur sind neu. `ContinueWatchingMutationResult` ist als einzeiliger Record formal identisch zum Altbestand — vertretbar, da der Datentyp nur den Server-Vertrag abbildet.
- Die bestehende `ProjectReference` des Testprojekts auf `VideoWebPlayer.Client` sowie die des MAUI-Projekts sind Altbestand (vorbestehende Tests nutzen `VideoWebPlayer.Client.Models`); der neue Client ist davon unabhängig.

### Einschränkungen

- Zwei Endpunkte des Altbestands wurden bewusst nicht übernommen, da sie außerhalb der genannten Bereiche liegen: `POST api/items/metadata` (Metadaten schreiben — kein „Nachschlagen") und `DELETE api/admin/sources/{id}` (Admin-API). Die Akzeptanzkriterien verlangen nur „die genannten Endpunkte".
- Das Fortschritts-Coalescing des alten Clients (Pufferung paralleler Progress-Meldungen pro Medium) wurde nicht übernommen — von der Anforderung nicht gefordert.
- Testlauf erzeugt nur bereits vorbestehende Compiler-Warnungen im Altprojekt `VideoWebPlayer.Client` (CS0108/CS8618), keine im neuen Code.
