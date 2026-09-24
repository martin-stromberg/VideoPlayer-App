# Umsetzungsplan: Developer-API für UI-Automatisierung

Branch: `task/issue-17-developer-api`
Eingaben: `requirement.md`, `inventory.md`, `inventory/dev-api.md`
Gebundene Entscheidungen: OP-3 (`TcpListener` + eigener Mini-HTTP), OP-5
(kein `dispatch`-Endpunkt), `#if DEBUG || DEV_API`, `devapi.json` in
`AppContext.BaseDirectory`, Bindung `127.0.0.1`.

## Zielbild

Ein plattformneutraler Mini-HTTP-Server (neuer Namespace
`VideoPlayer.Mvu.DevApi`, Ordner `VideoPlayer.Mvu/DevApi/`) stellt
Protokoll-Parsing, Routing und Modell-Snapshot bereit – vollständig im
Testprojekt testbar. Die MAUI-gebundene Seite (neuer Ordner
`VideoPlayer.Maui/DevApi/`) verdrahtet die vier Endpunkte mit `MvuHost`,
Visual Tree und `MainThread` und schreibt `devapi.json`. Alle neuen
Dateien und jede Verdrahtungszeile stehen unter `#if DEBUG || DEV_API` –
Release enthält die API nicht (gepinnt per Contract-Test).

## Schritte

### 1. Plattformneutraler Kern (`VideoPlayer.Mvu/DevApi/`)

Alle Dateien beginnen mit `#if DEBUG || DEV_API` vor den `using`-Zeilen
und enden mit `#endif` (Contract-Test pinnt diese Struktur).

1. **`DevApiRequest.cs`** – `record` mit `Method`, `Path` (ohne Query),
   `Headers` (Dictionary, OrdinalIgnoreCase), `Body` (string);
   Hilfsmethode `TryGetJsonBody<T>(out T?, out string? error)` via
   `System.Text.Json` (camelCase-Optionen).
2. **`DevApiResponse.cs`** – `record` mit `StatusCode`, `Body`,
   `ContentType` (Default `application/json; charset=utf-8`);
   Fabriken `Json(object)`, `Error(int status, string message)`
   (`{"error":"…"}`-Payload).
3. **`DevApiRequestReader.cs`** – `static`, `ReadAsync(Stream, ct)`:
   liest Header-Block bis `\r\n\r\n` (Limit 64 KiB), parst Request-Line
   (`METHODE pfad HTTP/x`), Header (`:`-Split), `Content-Length`-Body
   (Limit 1 MiB, sonst 400-Fehlerantwort); liefert `DevApiRequest?`
   (`null` bei sauberem Verbindungsende) bzw. wirft
   `DevApiRequestException` bei Malformation → Aufrufer antwortet 400.
4. **`DevApiRouter.cs`** – `Map(method, path, Func<DevApiRequest,
   CancellationToken, Task<DevApiResponse>>)`;
   `RouteAsync(request, ct)`: Treffer → Handler; Pfad bekannt, Methode
   falsch → 405 mit `Allow`-Header-Info im Body; sonst 404.
5. **`DevApiHttpServer.cs`** – `sealed`, `IDisposable`;
   `Start()`: `TcpListener(IPAddress.Loopback, 0)`, `Port` aus
   `LocalEndpoint`; Accept-Loop als `Task` mit `CancellationToken`;
   je Verbindung: Request lesen → `Router.RouteAsync` → Antwort schreiben
   (`HTTP/1.1 <code> <reason>\r\nContent-Type…Content-Length…Connection:
   close\r\n\r\n` + UTF-8-Body), Socket schließen. `StopAsync()`/Dispose
   beendet Listener + wartet kurz auf offene Handler.
6. **`DevApiModelSnapshot.cs`** – `static DevApiModelSnapshot
   FromModel(AppModel)`; Record-Felder gemäß `inventory/dev-api.md`
   (Enums als `string` via `JsonStringEnumConverter` bzw. `.ToString()`
   in string-Felder); Sub-Records `DevApiKnownServer`/
   `DevApiDiscoveredServer` (`address`, `displayName`/`name`,
   `nameOrAddress`, `lastUsedAtUtc`/`source`).
7. **`DevApiMetadata.cs`** – `record DevApiMetadata(int Port, int Pid,
   DateTime StartedUtc)`; `WriteAsync(path)` serialisiert camelCase
   (`{"port":…,"pid":…,"startedUtc":"…"}`).

### 2. MAUI-Verdrahtung (`VideoPlayer.Maui/DevApi/`)

8. **`DevApiTreeNode.cs`** – `record` (automationId, type, isVisible,
   bounds-Subrecord `DevApiBounds`, text, placeholder, children, items
   `DevApiTreeItem` mit index/type/text/detail) – unter `#if`.
9. **`DevApiVisualTree.cs`** – `static`:
   - `BuildTree(Element root)` → Traversierung via
     `IVisualTreeElement.GetVisualChildren()`; Shell → `CurrentPage`
     zusätzlich einhängen; CollectionView → `items` aus `ItemsSource`
     (Anzeigetext über Property-Kette `NameOrAddress` → `Name` → `Title`
     → `DisplayName` → `Text` → `ToString()`, `detail` aus `Address`).
   - `FindByAutomationId(Element root, string id)` → `Element?` (gleiche
     Traversierung inkl. Shell-`CurrentPage`).
   - `TryInvoke(Element, out string? error)` → Auslöse-Regeln
     (Button/ImageButton `SendClicked()`, Switch `IsToggled`-Flip,
     CheckBox `IsChecked`-Flip, MenuItem `Activate()`/`Command`,
     `ICommandSource`, `TapGestureRecognizer.Command`).
   - `TrySetText(Element, string, out string? error)` → `InputView.Text`.
10. **`DevApiService.cs`** – `sealed`, `IDisposable`; Ctor
    `(MvuHost mvuHost)`; `Start()`:
    - Router mit genau den vier Routen (`inventory/dev-api.md`).
    - `GET /model`: `DevApiModelSnapshot.FromModel(mvuHost.Model)`
      (unveränderlicher Snapshot – kein UI-Thread nötig).
    - `GET /tree`/`POST /invoke`/`POST /set-text`:
      `MainThread.InvokeOnMainThreadAsync`; Root =
      `Application.Current?.Windows.FirstOrDefault()?.Page` (404/500 bei
      fehlendem Fenster).
    - `DevApiHttpServer.Start()` → `DevApiMetadata.WriteAsync` nach
      `AppContext.BaseDirectory` (Fallback `FileSystem.AppDataDirectory`
      bei IOException/nicht-Windows).
    - `Port`/`MetadataPath` als lesbare Eigenschaften (für Live-
      Verifikation/Diagnose).
    - `Dispose()` → `StopAsync` + `devapi.json` stehen lassen (letzter
      bekannter Stand, harmlos).
    - Fehlertexte deutsch (z. B. „Element mit AutomationId '…' nicht
      gefunden.", „Element ist nicht auslösbar.", „Element ist kein
      Texteingabefeld.", „Ungültiger JSON-Body.").

### 3. Verdrahtung am App-Rand

11. **`MauiProgram.cs`** – im `#if DEBUG`-Block erweitern zu
    `#if DEBUG || DEV_API` bzw. eigener Block direkt davor:
    `builder.Services.AddSingleton<DevApi.DevApiService>();`
12. **`App.xaml.cs`** – in `CreateWindow` unter `#if DEBUG || DEV_API`:
    `DevApiService` auflösen, `Start()`, `window.Destroying` → Dispose.

### 4. Tests (`VideoWebPlayer.Maui.Tests/`)

13. **`DevApiRequestReaderTests.cs`** – GET ohne Body, POST mit JSON-Body,
    Header-Groß-/Kleinschreibung, fehlende Content-Length mit Body →
    Fehler, übergroßer Body → Fehler, Malformation → Exception/null.
14. **`DevApiRouterTests.cs`** – Treffer, 404 bei unbekanntem Pfad,
    405 bei falscher Methode.
15. **`DevApiHttpServerTests.cs`** – echter Loopback-Server im Test:
    Start auf Port 0, `HttpClient` GET/POST, JSON-Body-Roundtrip,
    404/405, zwei sequentielle Verbindungen, sauberes Stoppen.
16. **`DevApiModelSnapshotTests.cs`** – Snapshot-Befüllung aus
    `AppModel`, JSON-Serialisierung camelCase, Abwesenheit von
    `token`/`password`-Schlüsseln im JSON.
17. **`DevApiMetadataTests.cs`** – Schreiben/Lesen von `devapi.json`
    (camelCase-Felder).
18. **`DevApiContractTests.cs`** (Quelltext-Verträge via
    `RepositoryRoot.Find()`):
    - Jede Datei unter `VideoPlayer.Mvu/DevApi/` und
      `VideoPlayer.Maui/DevApi/` beginnt mit `#if DEBUG || DEV_API`
      und endet mit `#endif`.
    - `MauiProgram.cs` und `App.xaml.cs` verdrahten `DevApiService` nur
      innerhalb `#if`-Blöcken (zeilenweiser Scan: DevApiService-Vorkommen
      liegen zwischen `#if` und `#endif`).
    - Genau vier `Map(`-Aufrufe in `DevApiService.cs` für `/model`,
      `/tree`, `/invoke`, `/set-text`; die Datei enthält kein `dispatch`.
    - `DevApiService.cs` nutzt `MainThread.InvokeOnMainThreadAsync` für
      UI-Endpunkte und schreibt `devapi.json` unter
      `AppContext.BaseDirectory`.
    - `DevApiHttpServer` bindet an `IPAddress.Loopback` (kein `Any`).
    - `VideoPlayer.Maui.csproj` unverändert bzgl. DevApi (kein neuer
      Define-Eintrag erforderlich).

### 5. Dokumentation (Schritt 12)

19. `docs/help/developer-api.md` (neu) + Eintrag in `docs/help/index.md`.
20. `README.md` – kurzer Hinweis unter Entwicklung/Testbarkeit.
21. `docs/RELEASE_NOTES.md` – Eintrag unter „What's New".

## Programmablauf (Happy Path)

Debug-Start der App → `MauiProgram` registriert `DevApiService` (nur
`#if`) → `App.CreateWindow` startet den Dienst → `TcpListener` bindet
`127.0.0.1:0` → Port steht in `devapi.json` neben der Exe → E2E-Client
liest `devapi.json`, ruft `GET /tree` (ServerSetupPage sichtbar), setzt
per `POST /set-text` die Adresse, klickt per `POST /invoke` „Verbinden",
assertiert per `GET /model` den Zustandsübergang → App-Ende
(`Window.Destroying`) → Server stoppt.

## Betroffene bestehende Tests

Keine funktionalen Änderungen an bestehendem Code → bestehende 896 Tests
müssen unverändert grün bleiben; neue Tests addieren sich.

## Risiken

- **`IMenuItemController.Activate()`-Verfügbarkeit in MAUI 10** – beim
  Kompilieren prüfen; Fallback `Command.Execute` bleibt.
- **`Window.Page` vs. Shell-Visualchildren** – `GetVisualChildren()` auf
  Shell liefert ggf. nicht die Seite → `CurrentPage` explizit einhängen.
- **`devapi.json`-Schreibbarkeit** – bei packaged/privilegiertem Pfad
  IOException → Fallback `FileSystem.AppDataDirectory`.
- **Release-Ausschluss** – nur strukturell (Quelltext-Vertrag) pinnbar,
  da kein Release-Build in der Testpipeline; dokumentiert als ausreichend
  (kein Assembly-Scan nötig, `#if` ist lückenlos).

## Offene Punkte

Keine – alle fachlichen Entscheidungen sind im Projektplan gefallen
(OP-3, OP-5, Aktivierung, Metadaten-Ablage, Bindung).
