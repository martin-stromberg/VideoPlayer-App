# Umsetzungsplan: Clientseitiger Zugriff auf die Server-API (Neuimplementierung)

## Übersicht

Es wird ein neues, plattformneutrales .NET-10-Klassenbibliotheksprojekt `VideoPlayer.ApiClient` in der Solution `VideoPlayer.App.sln` angelegt, das den clientseitigen Zugriff auf die öffentliche Server-API von „VideoPlayer" vollständig neu implementiert. Der Bestandsclient `VideoWebPlayer.Client` bleibt unverändert und wird nicht referenziert; er diente nur als fachliche Referenz für Endpunkte und DTO-Felder. Tests entstehen im bestehenden xUnit-Projekt `VideoWebPlayer.Maui.Tests` gegen einen simulierten Server (Fake-`HttpMessageHandler`).

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Projektname/-platzierung | Neues Projekt `VideoPlayer.ApiClient` (`VideoPlayer.ApiClient/VideoPlayer.ApiClient.csproj`, `net10.0`), Namespace `VideoPlayer.ApiClient`, DTOs unter `VideoPlayer.ApiClient/Models` mit Namespace `VideoPlayer.ApiClient.Models` | Eindeutige Trennung vom Altprojekt `VideoWebPlayer.Client`; Name passt zur Solution `VideoPlayer.App.sln` und zum neuen App-Namen „VideoPlayer"; `net10.0` ohne MAUI-Abhängigkeit = plattformneutral. |
| Wiederholungsstrategie — Interpretation | Ein Initialversuch + bis zu **3 Wiederholungen** mit den Warteintervallen 1 s, 2 s, 4 s → maximal 4 HTTP-Sendevorgänge | Die Anforderung nennt drei explizite Warteintervalle; zwischen nur 3 Gesamtversuchen lägen nur zwei Intervalle. Die drei Intervalle sind nur mit 3 Wiederholungen nach dem ersten Versuch konsistent abbildbar. |
| Retry-Ausführung | Eine interne Sende-Pipeline im Client (`SendWithPolicyAsync`), die eine Request-Factory (`Func<HttpRequestMessage>`) pro Versuch neu erzeugt | `HttpRequestMessage`/`HttpContent` sind nicht mehrfach sendbar; eine Factory pro Versuch ist der korrekte Ansatz. |
| Warteintervalle — Testbarkeit | Verzögerung über injizierbaren Delegaten `Func<TimeSpan, CancellationToken, Task>` in `VideoPlayerApiClientOptions` (Standard: `Task.Delay`) | Ermöglicht exakte Verifikation der Intervalle ohne echte Wartezeit; `TimeProvider`/`FakeTimeProvider` würde ein zusätzliches Paket erfordern. |
| Token-Erneuerung bei 401 | Injizierbarer Delegat `Func<CancellationToken, Task<AuthToken?>>` (`TokenRefresher`) in den Options; wird pro Anfrage maximal einmal aufgerufen, über eine `SemaphoreSlim`-Sperre serialisiert; Rückgabe `null` = Erneuerung fehlgeschlagen | Plattformneutral: Die Bibliothek kennt keine Credential-Ablage; die App (oder der Test) entscheidet, wie eine stille Neuanmeldung erfolgt (z. B. erneuter `LoginAsync`-Aufruf mit gespeicherten Zugangsdaten). Ein Refresh-Versand zählt nicht in das Retry-Budget. |
| Fehlermodell | `ApiException : HttpRequestException` mit `StatusCode`, `Endpoint` und `ResponseBody` | Typisierte, auswertbare Fehler statt unformatierter `HttpRequestException`-Strings wie im Altcode. |
| Client-Gate | Statische Klasse `ClientGate` im neuen Projekt: Header `X-API-Key`; Schlüsselauflösung in Reihenfolge Umgebungsvariable `VIDEOWEBPLAYER_MAUI_API_TOKEN` → Assembly-Metadata `VideoPlayer.ApiClient.ApiToken` (MSBuild-Property `VideoPlayerApiToken` im neuen csproj) → Platzhalter `<MAUI_CLIENT_API_TOKEN>`; überschreibbar per `VideoPlayerApiClientOptions.ApiKey` | Gleiche Wire-Konvention wie Bestand (Backend-Gegenstück `Jwt:ApiToken:Maui` bleibt gültig); plattformneutral ohne MAUI-Abhängigkeit. |
| DTO-Benennung | Neue, saubere Namen ohne `Dto`-Prefix und ohne Server-Namespaces (`MediaEntry`, `Movie`, `MovieCollection`, `TvShow`, `TvShowSeason`, `TvShowEpisode`, `Picture`, `MediaSource`, `SourceGenres`, `Genre`, `GenreOption`, `RecentEntry`/`RecentEntryKind`, `FavoriteEntry`, `ContinueWatchingEntry`, `ContinueWatchingMutationResult`, `MediaItemSummary`, `LoginRequest`, `AuthToken`, `PlaybackProgressReport`) | JSON-Feldnamen und `$type`-Diskriminatoren (`mediaEntry`, `movie`, `movieCollection`, `show`, `season`, `episode`) bleiben wire-kompatibel; C#-Typnamen sind Vertragsirrelevant und werden sauber neu gewählt. Bekannte Altcode-Defizite (CS0108-Ausblendung, wirkungsloses `IgnoreAssignPropertyAttribute`, CS8618-Warnungen) werden nicht übernommen. |
| Fortschrittsmeldung | Direkter Sendeaufruf ohne Coalescing/Buffering | Coalescing war Eigenverhalten des Altclients und ist nicht Teil der Anforderung; Offline-Warteschlange ist Entwicklungsschritt 6. |
| Nicht abgedeckte Altendpunkte | `api/items/metadata`, `api/admin/sources/{id}`, Impersonation werden **nicht** implementiert | Nicht Teil der Anforderung (kein Admin-/Metadaten-Schreiben im Funktionsumfang des Schritts). |
| Logging | Optionaler `ILogger` über `VideoPlayerApiClientOptions` (Paket `Microsoft.Extensions.Logging.Abstractions` 10.0.10) | Diagnose von Retries/401-Erneuerungen; konsistent mit Repo-Paketständen. |
| Fehler-Rückgabesemantik | Methoden werfen `ApiException` statt `null`/leere Listen zurückzugeben; Ausnahme: `CheckHealthAsync` liefert `bool` | Die Anforderung verlangt eine funktionierende, überprüfbare Fehlerbehandlung; stillschweigende Leerergebnisse des Altcodes verdecken Fehler und werden nicht übernommen. |

## Programmabläufe

### Anmeldung und tokenbasierte Folgeanfragen

1. Aufruf `LoginAsync(email, password)` serialisiert `LoginRequest` und sendet `POST api/auth/login` über die Sende-Pipeline mit `allowReauth: false`.
2. Die Antwort wird als `AuthToken` deserialisiert; `SetAccessToken` setzt `Authorization: Bearer {token}` auf dem `HttpClient` und merkt den Token in `CurrentToken`.
3. Alle Folgeanfragen laufen über `SendWithPolicyAsync`, das den Bearer-Header mitführt (DefaultRequestHeaders).

Beteiligte Klassen: `VideoPlayerApiClient`, `LoginRequest`, `AuthToken`, `ApiException`.

### Wiederholungsstrategie

1. `SendWithPolicyAsync` erzeugt pro Versuch eine neue `HttpRequestMessage` über die Request-Factory und sendet sie.
2. Erfolgsantwort (2xx): wird zurückgegeben bzw. deserialisiert.
3. Antwort `401 Unauthorized`: falls `allowReauth` und noch kein Refresh für diese Anfrage erfolgte → `TokenRefresher` aufrufen (serialisiert via `SemaphoreSlim`); bei `AuthToken`-Rückgabe Token anwenden und Anfrage einmal neu senden (ohne Retry-Zähler zu erhöhen); bei `null` oder erneutem 401 → `ApiException` (401).
4. Antwort mit wiederholbarem Status (`408`, `429`, 5xx) oder transienter Transportfehler (`HttpRequestException`, `TaskCanceledException` ohne Caller-Abbruch): solange Versuchszähler < 3 → `DelayAsync` mit `RetryDelays[versuch]` (1 s, 2 s, 4 s) und erneuter Versuch; danach `ApiException` bzw. Weitergabe der Ausnahme als `ApiException` mit Statuscode `0`.
5. Eindeutige Clientfehler (übrige 4xx wie `400`, `403`, `404`): sofort `ApiException`, keine Wiederholung.
6. Vom Aufrufer angeforderte Abbrüche (`CancellationToken` gecancelt) werden niemals wiederholt, sondern durchgereicht.

Beteiligte Klassen: `VideoPlayerApiClient`, `ApiException`, `ClientGate`, `VideoPlayerApiClientOptions`, `AuthToken`.

### Client-Gate

1. Beim Aufbau des Clients wird der `X-API-Key`-Header gesetzt: expliziter `ApiKey` aus den Options, sonst `ClientGate.Resolve()` (Umgebungsvariable → Assembly-Metadata → Platzhalter).
2. Jede gesendete Anfrage trägt den Header (über `HttpClient.DefaultRequestHeaders`).

Beteiligte Klassen: `ClientGate`, `VideoPlayerApiClientOptions`.

### Endpunkt-Aufrufe (Datenabrufe/Mutationen)

Jede öffentliche Methode baut Pfad + ggf. JSON-Body, ruft `SendWithPolicyAsync` und deserialisiert mit `PropertyNameCaseInsensitive`-Optionen:

| Methode | HTTP | Pfad | Rückgabe |
|---------|------|------|----------|
| `LoginAsync` | POST | `api/auth/login` | `AuthToken` |
| `CheckHealthAsync` | GET | `api/health` | `bool` (true bei 2xx, false sonst/bei Fehler) |
| `GetSourcesAsync` | GET | `api/Sources` | `IReadOnlyList<MediaSource>` |
| `GetSourceAsync` | GET | `api/Sources/{id}` | `MediaSource` |
| `GetSourceGenresAsync` | GET | `api/SourceGenres/{id}` | `SourceGenres` |
| `GetRecentEntriesAsync` | GET | `api/items/recent` | `IReadOnlyList<RecentEntry>` |
| `GetContinueWatchingAsync` | GET | `api/continue-watching` | `IReadOnlyList<ContinueWatchingEntry>` |
| `ReportPlaybackProgressAsync` | POST | `api/continue-watching/progress` | `Task` (Body: `PlaybackProgressReport`) |
| `HideContinueWatchingEntryAsync` | POST | `api/continue-watching/hide` | `ContinueWatchingMutationResult` |
| `SkipContinueWatchingEntryAsync` | POST | `api/continue-watching/skip` | `ContinueWatchingMutationResult` |
| `GetFavoritesAsync` | GET | `api/favorites` | `IReadOnlyList<FavoriteEntry>` |
| `ToggleFavoriteAsync` | POST | `api/favorites/toggle` | `bool` (Body: `MediaEntry`) |
| `RemoveFavoriteAsync` | POST | `api/favorites/remove` | `Task` (Body: `{ id, userId }` analog Referenz) |
| `GetSourceItemsAsync` | GET | `api/items?mediaSourceId={id}&page={p}&size={s}[&search=…][&genreId=…]` | `IReadOnlyList<MediaItemSummary>` |
| `GetMovieAsync` | GET | `api/items/movie/{id}` | `Movie` |
| `GetMovieCollectionAsync` | GET | `api/items/moviecollection/{id}` | `MovieCollection` |
| `GetTvShowAsync` | GET | `api/items/tvshow/{id}` | `TvShow` |
| `GetTvShowEpisodeAsync` | GET | `api/items/tvshowepisode/{id}` | `TvShowEpisode` |
| `GetGenreOptionsAsync` | GET | `api/items/genres` | `IReadOnlyList<GenreOption>` |
| `GetPictureAsync` | GET | `api/pictures/{id}` | `byte[]` |
| `GetSourceIconAsync` | GET | `api/sourceicons/{id}` | `byte[]` |

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `VideoPlayer.ApiClient/VideoPlayer.ApiClient.csproj` | Projekt | Neue net10.0-Klassenbibliothek; MSBuild-Property `VideoPlayerApiToken` → Assembly-Metadata `VideoPlayer.ApiClient.ApiToken` |
| `VideoPlayerApiClient` | Klasse | Endpunkt-Methoden + Sende-Pipeline mit Retry/401-Handling |
| `VideoPlayerApiClientOptions` | Klasse | `BaseAddress?`, `ApiKey?`, `TokenRefresher?`, `DelayAsync?`, `Logger?` |
| `ClientGate` | statische Klasse | `HeaderName` (`X-API-Key`), `Resolve()` mit testbarer Überladung `Resolve(string? environmentValue, string? metadataValue)` |
| `ApiException` | Exception | `StatusCode` (`HttpStatusCode?`), `Endpoint`, `ResponseBody` |
| `LoginRequest`, `AuthToken` | Datenmodell | Anmeldung (`Email`, `Password`; `Token`, `Expires`) |
| `MediaEntry`, `Movie`, `MovieCollection`, `TvShow`, `TvShowSeason`, `TvShowEpisode`, `Picture` | Datenmodell | Polymorphe Medien-Hierarchie (`$type`-Diskriminator) |
| `MediaSource`, `SourceGenres`, `Genre`, `GenreOption` | Datenmodell | Quellen und Genres |
| `RecentEntry`, `RecentEntryKind` | Datenmodell + Enum | „Neu im Programm" |
| `ContinueWatchingEntry`, `ContinueWatchingMutationResult`, `PlaybackProgressReport` | Datenmodell | Weiterschauen inkl. Mutationen/Fortschritt |
| `FavoriteEntry` | Datenmodell | Favoriten |
| `MediaItemSummary` | Datenmodell | Paginierte Quelleneinträge (`api/items`) |

## Änderungen an bestehenden Klassen

### `VideoPlayer.App.sln` (Solution)

- Neuer Solution-Eintrag für `VideoPlayer.ApiClient` mit Konfigurationen Debug/Release (Any CPU/x64/x86 analog bestehender Einträge).

### `VideoWebPlayer.Maui.Tests.csproj` (Projektdatei)

- Neue `ProjectReference` auf `..\VideoPlayer.ApiClient\VideoPlayer.ApiClient.csproj`.

Keine Änderungen an `VideoWebPlayer.Client` oder `VideoWebPlayer.Maui`.

## Datenbankmigrationen

Keine.

## Validierungsregeln

| Feld / Objekt | Regel | Fehlerfall |
|---------------|-------|------------|
| `LoginAsync(email, password)` | `email`/`password` nicht null/leer | `ArgumentException` vor dem Senden |
| Pfad-Parameter (`id`, `sourceId`, …) | `> 0` bzw. nicht negativ je Semantik | `ArgumentOutOfRangeException` |
| `HttpClient.BaseAddress` / `Options.BaseAddress` | Muss gesetzt sein (eine der beiden Quellen) | `InvalidOperationException` beim Client-Aufbau |

## Konfigurationsänderungen

| Eintrag | Typ | Standardwert | Zweck |
|---------|-----|--------------|-------|
| `VideoPlayerApiToken` | MSBuild-Property im neuen csproj | `<MAUI_CLIENT_API_TOKEN>` (Platzhalter) | Client-Gate-Schlüssel per Build |
| `VIDEOWEBPLAYER_MAUI_API_TOKEN` | Umgebungsvariable | — | Client-Gate-Schlüssel zur Laufzeit (Konvention des Bestands beibehalten) |
| `VideoPlayerApiClientOptions.ApiKey` | Options-Eigenschaft | `ClientGate.Resolve()` | Explizite Überschreibung (Tests) |

## Seiteneffekte und Risiken

- **Testprojekt:** neue `ProjectReference` — kein Einfluss auf bestehende Tests (Baseline 8/8 grün).
- **Solution:** neuer Projekteintrag; CI-Builds (`pr-checks.yml`) bauen die Solution-Projekte — das neue Projekt muss fehlerfrei kompilieren und `dotnet format whitespace` bestehen.
- **Altcode:** bleibt unverändert; kein Risiko für die laufende MAUI-App.
- **Doppelter DTO-Bestand:** bis zum Entfernen von `VideoWebPlayer.Client` (späterer Projektschritt) existieren zwei DTO-Modelle parallel — bewusst gewollt (Nicht-Übernahme).

## Umsetzungsreihenfolge

1. **Projekt `VideoPlayer.ApiClient` anlegen**
   - Voraussetzungen: Keine.
   - Beschreibung: csproj (`net10.0`, `ImplicitUsings`, `Nullable`, `Microsoft.Extensions.Logging.Abstractions` 10.0.10, LICENSE-Pack wie im Repo üblich, `VideoPlayerApiToken`-Property + Assembly-Metadata), Solution-Eintrag in `VideoPlayer.App.sln`.

2. **Fehlermodell und Konfiguration**
   - Voraussetzungen: Schritt 1.
   - Beschreibung: `ApiException`, `ClientGate` (inkl. testbarer Resolve-Überladung), `VideoPlayerApiClientOptions`.

3. **Datenmodelle (DTOs)**
   - Voraussetzungen: Schritt 1.
   - Beschreibung: alle neuen DTO-Klassen/Enums gemäß Tabelle „Neue Klassen", Felder gemäß Referenz `client-api.md`; Polymorphie-Attribute auf `MediaEntry`.

4. **Client-Kern: Sende-Pipeline + Authentifizierung**
   - Voraussetzungen: Schritte 2–3.
   - Beschreibung: `VideoPlayerApiClient` mit `SendWithPolicyAsync` (Request-Factory, Retry-Klassifikation, Delays 1 s/2 s/4 s, einmaliger 401-Refresh über `TokenRefresher` + `SemaphoreSlim`), `LoginAsync`, `SetAccessToken`, `CurrentToken`, `CheckHealthAsync`.

5. **Endpunkt-Methoden**
   - Voraussetzungen: Schritt 4.
   - Beschreibung: alle übrigen Methoden gemäß Endpunkt-Tabelle.

6. **Testinfrastruktur im Testprojekt**
   - Voraussetzungen: Schritt 1; `ProjectReference` in `VideoWebPlayer.Maui.Tests.csproj`.
   - Beschreibung: `StubHttpMessageHandler` (Antwortwarteschlange/Responder-Delegate, Request-Aufzeichnung, Exceptions werfbar), Aufzeichnungs-Delay-Delegate, JSON-Hilfsmethoden für Server-Antworten.

7. **Tests schreiben**
   - Voraussetzungen: Schritte 4–6.
   - Beschreibung: Testklassen gemäß Abschnitt „Tests".

8. **Verifikation**
   - Voraussetzungen: Schritte 1–7.
   - Beschreibung: `dotnet build` der Solution (ohne MAUI-Projekt lokal nicht nötig — Build des neuen Projekts + Testprojekts), `dotnet test`, `dotnet format whitespace --verify-no-changes` auf den geänderten Dateien.

## Tests

### Neue Tests

Alle Tests im Projekt `VideoWebPlayer.Maui.Tests` (xUnit v3), simulierter Server via `StubHttpMessageHandler`; Verzögerungen über aufzeichnenden `DelayAsync`-Delegaten (keine echte Wartezeit).

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `StubHttpMessageHandler` | Test-Hilfsklasse | Queue/Delegate-basierte `HttpResponseMessage`s, aufgezeichnete Requests, konfigurierbare Exceptions |
| `RecordingDelay` | Test-Hilfsklasse/-funktion | Delegat, der angeforderte `TimeSpan`s sammelt statt zu warten |
| `LoginAsync_PostsCredentialsAndSetsBearerToken` | `VideoPlayerApiClientTests_Authentication` | POST `api/auth/login`, Body `Email`/`Password`, Token gespeichert, `Authorization: Bearer` auf Folgerequest |
| `AllRequests_ContainClientGateHeader` | `VideoPlayerApiClientTests_Authentication` | `X-API-Key` auf Login- und Datenrequests; expliziter `ApiKey` greift |
| `ServerError_RetriesUpToThreeTimes_WithExponentialDelays` | `VideoPlayerApiClientTests_RetryPolicy` | 500, 500, 500 → Erfolg: 4 Sendevorgänge, Delays exakt [1 s, 2 s, 4 s] |
| `ServerError_Permanent_ThrowsAfterExhaustion` | `VideoPlayerApiClientTests_RetryPolicy` | 4× 500 → `ApiException` mit `StatusCode=500`, 4 Requests |
| `ClientError_NotRetried` (Theory: 400, 403, 404, 422) | `VideoPlayerApiClientTests_RetryPolicy` | jeweils genau 1 Request, `ApiException` mit Statuscode |
| `RetriableClientError_Retried` (Theory: 408, 429) | `VideoPlayerApiClientTests_RetryPolicy` | jeweils Retry mit Delays bis Erfolg |
| `TransportError_Retried` | `VideoPlayerApiClientTests_RetryPolicy` | `HttpRequestException` aus dem Handler → Retry → Erfolg |
| `Timeout_Retried` | `VideoPlayerApiClientTests_RetryPolicy` | `TaskCanceledException` (kein Caller-Abbruch) → Retry |
| `CallerCancellation_NotRetried` | `VideoPlayerApiClientTests_RetryPolicy` | gecancelter `CancellationToken` → `OperationCanceledException`, genau 0/1 Sendevorgänge, keine Delays |
| `Unauthorized_RefreshesTokenOnce_AndResends` | `VideoPlayerApiClientTests_TokenRefresh` | 401 → `TokenRefresher` liefert neuen `AuthToken` → Resend erfolgreich; Refresh-Zähler = 1 |
| `Unauthorized_RefreshFails_Throws` | `VideoPlayerApiClientTests_TokenRefresh` | `TokenRefresher` → `null` → `ApiException` 401, kein weiterer Versand |
| `Unauthorized_SecondTime_Throws` | `VideoPlayerApiClientTests_TokenRefresh` | 401 → Refresh ok → Resend erneut 401 → `ApiException`; Refresh nur 1× |
| `Login_SkipsReauthentication` | `VideoPlayerApiClientTests_TokenRefresh` | 401 auf `api/auth/login` löst keinen `TokenRefresher` aus |
| Endpunkt-Mapping (Theory, alle Methoden aus Endpunkt-Tabelle) | `VideoPlayerApiClientTests_Endpoints` | Korrekte HTTP-Methode + Pfad je Methode |
| DTO-Deserialisierung (Recent, Sources, Favorites, Movie, MovieCollection, TvShow+Seasons+Episodes, Episode, GenreOptions, MediaItemSummary-Liste, ContinueWatching) | `VideoPlayerApiClientTests_Endpoints` | Felder gemäß Referenz; polymorphe `Entry`-Auflösung über `$type` |
| `GetPictureAsync`/`GetSourceIconAsync` liefern Bytes | `VideoPlayerApiClientTests_Endpoints` | `byte[]`-Rückgabe, korrekte Pfade `api/pictures/{id}` / `api/sourceicons/{id}` |
| `ClientGate_ResolveOrder` | `ClientGateTests` | Umgebungsvariable > Metadata > Platzhalter (über testbare Überladung) |
| `MediaEntry_PolymorphicRoundtrip` | `MediaEntrySerializationTests` | `$type`-Diskriminierung für `movie`, `movieCollection`, `show`, `season`, `episode` |

### Betroffene bestehende Tests

Keine — das neue Projekt ist additiv; die beiden Bestands-Testklassen bleiben unverändert.

### E2E-Tests (primärer Funktionsnachweis)

Keine E2E-Tests erforderlich: Die Anforderung betrifft eine reine Bibliothek ohne UI und ohne über Nutzeraktion erreichbaren Ablauf. Der Funktionsnachweis erfolgt über die automatisierten Tests gegen den simulierten Server (vollständiger HTTP-Verkehr inkl. Header, Statuscodes und Retry-Timing); das Akzeptanzkriterium nennt den simulierten Server ausdrücklich als zulässige Testbasis. Ein Live-Test gegen den lokalen IIS-Server entfällt, weil dieser Stand der Bestandsaufnahme unter `http://localhost/videoplayer/api/health` mit HTTP 500 antwortet und damit nicht als verlässliches Testziel taugt (Dokumentation in `inventory.md`).

## Offene Punkte

Keine.

Hinweis zur Interpretation: „bis zu drei Versuche mit Warteintervallen 1 s, 2 s, 4 s" wurde als Initialversuch + bis zu 3 Wiederholungen (max. 4 Sendevorgänge) festgelegt — siehe Designentscheidungen. Die alternative Lesart (insgesamt 3 Versuche) ließe nur zwei Warteintervalle zu und widerspräche der expliziten Nennung dreier Intervalle.
