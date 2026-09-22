# Bestandsaufnahme: Clientseitiger Zugriff auf die Server-API (Neuimplementierung)

Analysiert wurde der bestehende API-Client `VideoWebPlayer.Client` (als fachliche Referenz, nicht als Übernahmequelle), die Solution-/Projektstruktur, das Testprojekt sowie die Projektkonfiguration für das Client-Gate — bezogen auf die Anforderung in `requirement.md`.

## Zusammenfassung

- Die Solution `VideoPlayer.App.sln` enthält drei Projekte: `VideoWebPlayer.Client` (net10.0, plattformneutraler API-Client — wird **nicht** übernommen), `VideoWebPlayer.Maui` (MAUI-App) und `VideoWebPlayer.Maui.Tests` (net10.0, xUnit v3).
- Der Bestandsclient `VideoWebPlayerClient` deckt alle in der Anforderung genannten Endpunkte fachlich ab und dient als verbindliche Referenz für Endpunkt-Pfade, HTTP-Methoden und DTO-Felder (Detaildokument: [Projekt-Bestandsaufnahme client-api.md](../../../projects/task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app/inventory/client-api.md)).
- **Fehlt im Bestand:** eine definierte Wiederholungsstrategie (nur 401-Einzel-Retry), eine eigenständige Client-Gate-Auflösung außerhalb des MAUI-Projekts sowie ein neues, vom Altcode unabhängiges Clientprojekt.
- Das Client-Gate (`X-API-Key`, Umgebungsvariable `VIDEOWEBPLAYER_MAUI_API_TOKEN`, MSBuild-Property `MauiClientApiToken` → Assembly-Metadata) ist heute in `VideoWebPlayer.Maui/Services/MauiClientConfiguration.cs` implementiert — MAUI-gebunden, daher nicht direkt wiederverwendbar für eine plattformneutrale Bibliothek.
- Das Testprojekt `VideoWebPlayer.Maui.Tests` referenziert nur `VideoWebPlayer.Client`; für das neue Projekt ist eine zusätzliche `ProjectReference` erforderlich.
- Das lokal im IIS eingerichtete Webprojekt „VideoPlayer" antwortet unter `http://localhost/videoplayer/api/health` aktuell mit HTTP 500 (leere Antwort) — als Testziel daher nicht zuverlässig; die Anforderung erlaubt einen simulierten Server, der für deterministische Tests genutzt wird.
- **Test-Ausgangszustand:** 8 von 8 Tests bestanden (Nachweis: [tests.md](inventory/tests.md)); keine bekannten Testfehler.

## Details

- [Referenz: Bestehender API-Client und DTO-Felder](../../../projects/task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app/inventory/client-api.md) — vollständige Endpunkt- und DTO-Feldliste des Bestandscodes.
- [Build-, Projekt- und CI-Konfiguration](../../../projects/task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app/inventory/build-environment.md) — Solution-Struktur, Paketstände, CI-Checks (u. a. `dotnet format whitespace --verify-no-changes` als Lint-Gate).
- [Tests und Test-Ausgangszustand](inventory/tests.md) — Baseline-Lauf vor der Umsetzung.

## Relevante Bestandsartefakte im Detail

### `VideoWebPlayerClient` (nur Referenz)

Datei: `VideoWebPlayer.Client/VideoWebPlayerClient.cs`

| Aspekt | Stand |
|--------|-------|
| Endpunktabdeckung | Alle geforderten Endpunkte vorhanden (Login, Health, Sources, SourceGenres, Recent, Continue-Watching inkl. progress/hide/skip, Favorites, Items paged/genres/movie/moviecollection/tvshow/tvshowepisode, Pictures, SourceIcons) |
| Fehlerbehandlung | Nur 401-Single-Retry über virtuelles `HandleUnauthorized()`; keine allgemeine Retry-Strategie; viele Methoden schlucken Fehler und liefern `null`/leere Listen |
| Fortschrittsmeldung | Coalescing pro Medium (`ConcurrentDictionary`), keine Persistenz — Coalescing ist nicht Teil der Anforderung |
| Serialisierung | `System.Text.Json`, `PropertyNameCaseInsensitive = true`; polymorphe DTOs über `$type`-Diskriminator (`mediaEntry`, `movie`, `movieCollection`, `show`, `season`, `episode`) |

### `MauiClientConfiguration` (nur Referenz)

Datei: `VideoWebPlayer.Maui/Services/MauiClientConfiguration.cs`

Header `X-API-Key`; Tokenquellen in Reihenfolge: Umgebungsvariable `VIDEOWEBPLAYER_MAUI_API_TOKEN` → Assembly-Metadata `VideoWebPlayer.Maui.ApiToken` (MSBuild `MauiClientApiToken`) → Platzhalter `<MAUI_CLIENT_API_TOKEN>`. Backend-Gegenstück: `Jwt:ApiToken:Maui`.

### DTO-Felder (Referenz, verbindlich für den Wire-Vertrag)

Siehe [client-api.md](../../../projects/task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app/inventory/client-api.md) Abschnitt „DTOs" — Feldlisten für `AuthenticationRequest`, `AuthorizationToken` (`token`, `expires`), `ContinueWatchingDto`, `ContinueWatchingMutationResult` (`Status`, `Message`), `DtoMediaEntry`-Hierarchie, `DtoRecentEntry`/`RecentEntryType`, `DtoMediaSource`/`SourceGenresDto`/`GenreDto`, `DtoFavoriteEntry`, `MediaEntryDto` (paginierte Einträge: `Type`, `Id`, `Title`, `Description`, `Url`, `CreatedAt`, `PictureId`, `ItemCount`).

Hinweis: Die Referenz-DTOs nutzen teils Server-Namespaces (`VideoWebPlayer.Controllers.Models`, `VideoWebPlayer.Data`) und enthalten bekannte Defizite (CS0108-Ausblendung in `DtoMediaSource`, CS8618-Warnungen, `IgnoreAssignPropertyAttribute` ohne Wirkung). Für den neuen Client werden die Feldnamen übernommen (Wire-Vertrag), die Struktur jedoch sauber neu aufgebaut.
