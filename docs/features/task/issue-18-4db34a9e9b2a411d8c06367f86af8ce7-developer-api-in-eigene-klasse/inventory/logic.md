# Detail: Logik und Verdrahtung

## Generischer Kern — `VideoPlayer.Mvu/DevApi/` (Namespace `VideoPlayer.Mvu.DevApi`)

### `DevApiHttpServer`
Datei: `VideoPlayer.Mvu/DevApi/DevApiHttpServer.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `DevApiHttpServer(DevApiRouter)` | public ctor | Nimmt die Routen-Tabelle entgegen |
| `Port` | public Property | Systemvergebener Port nach `Start` |
| `Start()` | public | `TcpListener(IPAddress.Loopback, 0)`, Hintergrund-Accept-Loop via `Task.Run` |
| `Dispose()` | public | Stoppt Listener und bricht den Loop ab |
| `AcceptLoopAsync(CancellationToken)` | private | Sequentielle Annahme; jede Verbindung in eigenem Task |
| `HandleClientAsync(TcpClient, CancellationToken)` | private | `DevApiRequestReader.ReadAsync` → `router.RouteAsync` → Antwort schreiben; `DevApiRequestException` → 400, sonstige Handler-Fehler → 500; Client-Abbrüche still ignoriert |
| `WriteResponseAsync` | private static | HTTP/1.1-Header + `Connection: close`, UTF-8-Body |
| `ReasonPhrase(int)` | private static | 200/400/404/405/500-Mapping |

Bindung ausschließlich `IPAddress.Loopback`, Port 0 (OS-vergeben). Kein Keep-Alive.

### `DevApiRequestReader` (static)
Datei: `VideoPlayer.Mvu/DevApi/DevApiRequestReader.cs`

| Member | Sichtbarkeit | Kurzbeschreibung |
|--------|-------------|------------------|
| `MaxHeaderBytes` (64 KiB), `MaxBodyBytes` (1 MiB) | public const | Größenlimits |
| `ReadAsync(Stream, CancellationToken)` | public | Liest Request-Line, Header bis `\r\n\r\n`, Body per `Content-Length`; `null` bei sauberem EOF; wirft `DevApiRequestException` |
| `ReadHeaderBlockAsync`, `ReadBodyBytes`, `Parse`, `ParseHeaders`, `IndexOf` | private | Chunked Lesen, Query-Stripping, Header-Dictionary (OrdinalIgnoreCase) |

### `DevApiRouter`
Datei: `VideoPlayer.Mvu/DevApi/DevApiRouter.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `Map(string method, string path, Func<DevApiRequest, CancellationToken, Task<DevApiResponse>>)` | public | Registriert Route (Handler-Delegat, keine Interface-Abstraktion) |
| `RouteAsync(DevApiRequest, CancellationToken)` | public | Exakter Pfadvergleich (Ordinal), toleriert Trailing Slash; 405 bei falschem Verb, 404 sonst |

Internes `Route`-Record (Method, Path, Handler). Kein generischer `dispatch`-Endpunkt — Routen werden ausschließlich per `Map` registriert.

### `DevApiRequestException`
Datei: `VideoPlayer.Mvu/DevApi/DevApiRequestException.cs` — `Exception` für Parse-Fehler → 400.

### `DevApiMetadata`
Datei: `VideoPlayer.Mvu/DevApi/DevApiMetadata.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `Create(int port)` | public static | Setzt `Port`, `Pid` (`Environment.ProcessId`), `StartedUtc` |
| `WriteAsync(string path, CancellationToken)` | public | camelCase-JSON in Datei |

## App-/MAUI-gebunden — `VideoPlayer.Maui/DevApi/` (Namespace `VideoPlayer.Maui.DevApi`)

### `DevApiService`
Datei: `VideoPlayer.Maui/DevApi/DevApiService.cs` — `sealed`, `IDisposable`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `DevApiService(MvuHost)` | public ctor | Quelle des `AppModel` |
| `Port`, `MetadataPath` | public Property | Nach `Start` gefüllt |
| `Start()` | public | Baut `DevApiRouter`, mappt die 4 Endpunkte, startet `DevApiHttpServer`, feuert `WriteMetadataAsync` |
| `Dispose()` | public | Stoppt den Server |
| `HandleModelAsync` | private | `DevApiModelSnapshot.FromModel(mvuHost.Model)` — kein UI-Thread nötig |
| `HandleTreeAsync` | private | `MainThread.InvokeOnMainThreadAsync` → `DevApiVisualTree.BuildTree(CurrentPage())`; 500 ohne aktive Seite |
| `HandleInvokeAsync` | private | Body `InvokeBody` (`automationId`, optional `itemIndex`) → UI-Thread → `InvokeOnPage` |
| `HandleSetTextAsync` | private | Body `SetTextBody` (`automationId`, `text`) → UI-Thread → `SetTextOnPage` |
| `InvokeOnPage`, `SetTextOnPage`, `FindElement`, `CurrentPage` | private | `FindByAutomationId`; `itemIndex` → `TrySelectItem`; sonst `TryInvoke`/`TrySetText`; 404/400/500-Mapping |
| `WriteMetadataAsync` | private | `devapi.json` in `AppContext.BaseDirectory`, Fallback `FileSystem.AppDataDirectory`; Fehler nur `Debug.WriteLine` |

### `DevApiVisualTree` (static)
Datei: `VideoPlayer.Maui/DevApi/DevApiVisualTree.cs` — alle Methoden UI-Thread-pflichtig

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `BuildTree(Element)` | public | Rekursiver Baum: AutomationId, Typname, IsVisible, Bounds, Text, Placeholder, Children, Items (CollectionView) |
| `FindByAutomationId(Element, string)` | public | Rekursive Suche (Ordinal-Vergleich) |
| `TryInvoke(Element, out string?)` | public | `SendClicked` (Button/ImageButton), Toggle (Switch/CheckBox), `Activate` (MenuItem), `Command` per Reflection, Tap-Gesten |
| `TrySelectItem(Element, int, out string?)` | public | `CollectionView.SelectedItem = ItemsSource[itemIndex]` → echtes `SelectionChanged` |
| `TrySetText(Element, string, out string?)` | public | `InputView.Text = text` → `TextChanged` |
| `VisualChildren`, `BoundsOf`, `TextOf`, `ItemsOf`, `ItemText`, `ItemProperty` | private | `IVisualTreeElement.GetVisualChildren()`, Shell-Sonderfall (`CurrentPage`), Reflection-Property-Leser |

## App-gebunden im Kern-Verzeichnis — `VideoPlayer.Mvu/DevApi/DevApiModelSnapshot.cs`

`DevApiModelSnapshot` (Record) + `FromModel(AppModel)` statischer Mapper + `DevApiKnownServer`, `DevApiDiscoveredServer`. Einziger Typ in `VideoPlayer.Mvu/DevApi/` mit Abhängigkeit zum App-Modell (`AppModel`, `AppPage`, `ConnectionState`, `ServerHealthStatus`, `KnownServer`, `DiscoveredServerCandidate`). Gehört fachlich zur App, nicht zum portierbaren Kern.

## Verdrahtung / Lebenszyklus

- `VideoPlayer.Maui/MauiProgram.cs` (Zeilen 134–139): `builder.Services.AddSingleton<DevApi.DevApiService>()` innerhalb `#if DEBUG || DEV_API`.
- `VideoPlayer.Maui/App.xaml.cs` (Zeilen 41–48): in `CreateWindow` `devApi.Start()`, `window.Destroying` → `devApi.Dispose()` — innerhalb `#if DEBUG || DEV_API`.
- `VideoPlayer.Mvu/MvuHost.cs`: `MvuHost.Model` (`AppModel`, unveränderlich) — Datenquelle des `/model`-Endpunkts.

## Compile-time-Ausschluss

- Alle 11 DevApi-Dateien beginnen mit `#if DEBUG || DEV_API` und enden mit `#endif`.
- Die DI-Registrierung und der Startaufruf stehen in eigenen `#if`-Blöcken in `MauiProgram.cs`/`App.xaml.cs`.
- Alle 6 DevApi-Testdateien sind ebenfalls `#if DEBUG || DEV_API`-gewrappt (Release-Build des Testprojekts hätte sonst kein Compile-Ziel).
