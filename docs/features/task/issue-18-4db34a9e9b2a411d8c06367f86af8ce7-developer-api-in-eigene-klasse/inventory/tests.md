# Detail: Tests

## Testklassen — `VideoWebPlayer.Maui.Tests` (xunit.v3, net10.0, alle Dateien `#if DEBUG || DEV_API`-gewrappt)

### `DevApiContractTests`
Statische Quelltext-Vertragsprüfungen über `RepositoryRoot.Find()`:
- `DevApiFiles_ExistInBothProjects` — DevApi-Dateien in `VideoPlayer.Mvu/DevApi` **und** `VideoPlayer.Maui/DevApi` (pfadbezogen!)
- `DevApiFiles_AreFullyWrappedInDebugOrDevApiIfdef` — Zeile 1 `#if DEBUG || DEV_API`, letzte Zeile `#endif`
- `DevApiWiring_IsInsideIfdefBlocks` — `DevApiService`-Verweise in `MauiProgram.cs`/`App.xaml.cs` nur innerhalb `#if`
- `DevApiService_MapsExactlyTheFourEndpoints` — genau 4 `router.Map(`-Aufrufe in `DevApiService.cs`, kein `dispatch`
- `DevApiHttpServer_BindsLoopbackOnly` — `IPAddress.Loopback` vorhanden, `IPAddress.Any` nicht
- `DevApiService_WritesMetadataAndMarshallsToUiThread` — `AppContext.BaseDirectory`, `"devapi.json"`, ≥3× `MainThread.InvokeOnMainThreadAsync`
- `DevApiVisualTree_UsesPlatformInvokeEquivalents` — `SendClicked()`, `InputView`, `ItemsSource`

**Wichtig:** Mehrere Tests sind an konkrete Dateipfade (`VideoPlayer.Mvu/DevApi`, `VideoPlayer.Maui/DevApi/DevApiService.cs`) gebunden und müssen bei einer Auslagerung angepasst werden.

### `DevApiHttpServerTests` (Integraltests mit echtem Loopback-Server)
- `Start_AssignsPort`, `Get_ReturnsHandlerResponse`, `Post_DeliversJsonBody`, `UnknownPath_Returns404_AndWrongMethod_Returns405`, `MalformedRequest_Returns400`, `SequentialConnections_AreServed`

### `DevApiRouterTests`
- `RouteAsync_CallsMappedHandler`, `RouteAsync_Returns404ForUnknownPath`, `RouteAsync_Returns405ForWrongMethod`, `RouteAsync_ToleratesTrailingSlash`

### `DevApiRequestReaderTests`
- `ReadAsync_ParsesGetRequest`, `ReadAsync_ParsesPostBody`, `ReadAsync_HeadersAreCaseInsensitive`, `ReadAsync_StripsQueryFromPath`, `ReadAsync_ReturnsNullOnCleanEnd`, `ReadAsync_ThrowsOnMalformedRequestLine`, `ReadAsync_ThrowsOnInvalidContentLength`, `ReadAsync_ThrowsOnOversizedBody`, `ReadAsync_ReadsBodyFromSameSegment`

### `DevApiMetadataTests`
- `WriteAsync_WritesExpectedFields` — camelCase `port`/`pid`/`startedUtc`

### `DevApiModelSnapshotTests`
- `FromModel_CopiesStateFields`, `Snapshot_SerializesCamelCase`, `Snapshot_ContainsNoSecretFields` (prüft Abwesenheit von `token`/`password`/`secret` im JSON)

## Testklassen — `VideoPlayer.E2E.Tests` (FlaUI + DevApiClient, 17 Facts)

| Datei | Anzahl | Thema |
|-------|--------|-------|
| `BootstrapTests.cs` | 6 | QR-/Ticket-Onboarding |
| `ServerSetupTests.cs` | 5 | Server-Einrichtung |
| `PairingTests.cs` | 3 | Geräte-Pairing |
| `LoginTests.cs` | 2 | Anmeldung |
| `ServerSwitchTests.cs` | 1 | Serverwechsel |

Die E2E-Tests nutzen die Developer-API ausschließlich über HTTP (`DevApiClient`) — kein Compile-Bezug zum Server-Code. Sie sind der Vertragsnachweis für Akzeptanzkriterium 2.

## Hilfsmethoden / -klassen

### `RepositoryRoot` (`VideoWebPlayer.Maui.Tests/Stubs/RepositoryRoot.cs`)
- `Find()` — sucht aufsteigend das Verzeichnis mit `VideoPlayer.App.sln`.

### `DevApiClient` (`VideoPlayer.E2E.Tests/Support/DevApiClient.cs`)
- `GetModelAsync()`, `GetTreeAsync()`, `InvokeAsync(automationId, itemIndex?)`, `SetTextAsync(...)` — HTTP-Client gegen `http://127.0.0.1:{port}/`.

### `AppInstance` (`VideoPlayer.E2E.Tests/Support/AppInstance.cs`)
- Startet die Debug-App, liest `devapi.json` aus dem Exe-Verzeichnis für den Port.

### `DevApiModels` (`VideoPlayer.E2E.Tests/Support/DevApiModels.cs`)
- Client-seitige DTOs für `/model` und `/tree` (camelCase-Vertrag).
