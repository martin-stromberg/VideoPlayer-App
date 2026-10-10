# Umsetzungsplan: Developer-API in eigene Klassenbibliothek msTools.DeveloperApi auslagern

## Übersicht

Die Developer-API wird in zwei neue Bibliotheksprojekte im selben Repository ausgelagert: `msTools.DeveloperApi` enthält den plattformneutralen Kern (Mini-HTTP-Server, Parser, Router, Request/Response-Modelle, `devapi.json`-Metadaten, Server-Fassade, `GET /model`-Snapshot-Provider); `msTools.DeveloperApi.Maui` enthält die MAUI-gebundenen Endpunkt-Handler (`/tree`, `/invoke`, `/set-text`, Elementbaum). `VideoPlayer.Maui` nutzt beide Bibliotheken per Projekt-Referenz bei unverändertem HTTP-Vertrag; app-gebunden bleiben nur der `AppModel`-Snapshot (`DevApiModelSnapshot` in `VideoPlayer.Mvu`) und ein dünner `DevApiService`-Adapter.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Code-Ablageort | Zwei neue Projekte `msTools.DeveloperApi/` und `msTools.DeveloperApi.Maui/` direkt im Repo-Root, Aufnahme in `VideoPlayer.App.sln`, Referenz per `ProjectReference` | **Nutzerentscheid (Offener Punkt 1).** Im Repo existiert kein privater NuGet-Feed und kein zentrales Packaging; eine Projekt-Referenz ist der einzige etablierte Referenzierungsweg und dokumentierbar. NuGet-Pack bleibt als spätere Option möglich (`IsPackable` bleibt Default). |
| MAUI-Teil | Eigenes Projekt `msTools.DeveloperApi.Maui` (MAUI-Klassenbibliothek, `<UseMaui>true</UseMaui>`): `DevApiVisualTree`, `DevApiTreeNode`/`DevApiBounds`/`DevApiTreeItem` und die Endpunkt-Verdrahtung (`MauiDevApiService`) wandern dorthin | **Nutzerentscheid (Offener Punkt 2).** Der Kern bleibt dadurch garantiert frei von MAUI-Abhängigkeiten; zweite MAUI-Apps können beide Pakete direkt nutzen. |
| Ziel-Frameworks der MAUI-Bibliothek | Dieselbe TFM-Matrix wie `VideoPlayer.Maui` (net10.0-android/-ios/-maccatalyst/-windows10.0.19041.0) mit demselben `IncludeXxxTarget`-Schalter-Muster | Konsistenz mit dem CI-Verhalten (Store-Builds schalten Targets einzeln ab); die Bibliothek muss überall dorthin kompilieren, wo die App es tut. |
| `DevApiModelSnapshot` | Bleibt in `VideoPlayer.Mvu/DevApi/` (Namespace `VideoPlayer.Mvu.DevApi`) | Der Snapshot ist an `AppModel`/`KnownServer`/`DiscoveredServerCandidate` gebunden und damit app-spezifisch, nicht portierbar. `VideoPlayer.Mvu` ist net10.0 und damit weiterhin in `VideoWebPlayer.Maui.Tests` testbar. |
| App-seitiger Adapter | `DevApiService` bleibt in `VideoPlayer.Maui/DevApi/` als dünner Adapter: injiziert `MvuHost`, reicht den `DevApiModelSnapshot`-Provider an `MauiDevApiService` weiter | Bewahrt die bestehende DI-Registrierung und den Lebenszyklus in `MauiProgram`/`App.xaml.cs` unverändert; die App-spezifische Modell-Kenntnis bleibt aus den Bibliotheken heraus. |
| Typnamen | `DevApi*`-Namenspräfix wird beibehalten (`DevApiHttpServer`, `DevApiRouter`, `DevApiVisualTree`, …), neue Namespaces `msTools.DeveloperApi` bzw. `msTools.DeveloperApi.Maui` | Minimaler Diff; Test- und Dokumentationsnamen bleiben konsistent. |
| `GET /model` | Erweiterungsmethode `MapModel(this DevApiRouter, Func<object?> snapshotProvider)` in `msTools.DeveloperApi` registriert `GET /model` | Issue-Vorschlag „Snapshot-Provider"; ein Delegat ist die einfachste denkbare Schnittstelle, die Bibliothek kennt kein konkretes Modell. |
| `tree`/`invoke`/`set-text` | Registrierung über `DevApiRouter.Map` mit dem neuen benannten Delegaten `DevApiEndpointHandler`; `MauiDevApiService` mappt die drei Endpunkte gegen die MAUI-Implementierung | Der Delegat ist die Handler-Schnittstelle des Kerns: Jede App kann beliebige Endpunkte anbinden; die MAUI-Handler sind eine davon. |
| Server- + Metadaten-Fassade | Neue Klasse `DevApiServer` in `msTools.DeveloperApi`: kapselt `DevApiHttpServer` + `devapi.json`-Schreiben; Primär- und optionales Fallback-Verzeichnis werden als `string`-Parameter übergeben | `FileSystem.AppDataDirectory` ist eine MAUI-API und gehört in `msTools.DeveloperApi.Maui` (dort übergibt `MauiDevApiService` beide Pfade); die Kernbibliothek stellt das komplette `devapi.json`-Verhalten bereit (Akzeptanzkriterium 1). |
| Compile-time-Ausschluss | Alle Dateien beider Bibliotheken behalten den vollständigen `#if DEBUG || DEV_API`-Wrapper; die Release-Assemblies sind faktisch leer | Jeder Verweis außerhalb eines `#if`-Blocks erzeugt im Release-Build einen Compile-Fehler — der Ausschluss kann nicht vergessen werden. Zusätzlich: kein `InternalsVisibleTo` in den Bibliotheken. Die Contract-Tests prüfen den Wrapper weiterhin statisch. |
| Teststandort | Die DevApi-Unit-/Integraltests bleiben in `VideoWebPlayer.Maui.Tests` und erhalten eine `ProjectReference` auf `msTools.DeveloperApi` | Kein neues Testprojekt nötig; die bestehenden Contract-Test-Muster (`RepositoryRoot`) werden auf die neuen Pfade erweitert. `msTools.DeveloperApi.Maui` wird (wie heute der MAUI-Teil) nur statisch über Contract-Tests geprüft — das Testprojekt kann keine MAUI-TFMs referenzieren. |

## Programmabläufe

### App-Start mit Developer-API (unverändertes Laufzeitverhalten)

1. `MauiProgram.CreateMauiApp` registriert `DevApiService` als Singleton (`#if DEBUG || DEV_API`).
2. `App.CreateWindow` löst den Dienst auf, ruft `devApi.Start()`, bindet `Dispose` an `Window.Destroying`.
3. `DevApiService.Start` delegiert an `MauiDevApiService` mit dem Snapshot-Provider `() => DevApiModelSnapshot.FromModel(mvuHost.Model)`.
4. `MauiDevApiService.Start` baut einen `DevApiRouter`, registriert `GET /model` via `MapModel(provider)` sowie `GET /tree`, `POST /invoke`, `POST /set-text` via `Map` (Handler marshallen auf den UI-Thread), erzeugt `DevApiServer(router, AppContext.BaseDirectory, FileSystem.AppDataDirectory)` und startet ihn.
5. `DevApiServer.Start` startet den `DevApiHttpServer` (Loopback, Port 0) und schreibt asynchron `devapi.json` (Primärverzeichnis, bei Fehlschlag Fallback).
6. Requests laufen wie bisher: `DevApiRequestReader` → `DevApiRouter.RouteAsync` → Handler.

Beteiligte Klassen/Komponenten: `DevApiService`, `MauiDevApiService`, `DevApiRouter`, `DevApiRouterExtensions`, `DevApiServer`, `DevApiHttpServer`, `DevApiModelSnapshot`, `DevApiVisualTree`, `MvuHost`

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `msTools.DeveloperApi` (Projekt) | Klassenbibliothek net10.0 | Plattformneutraler Kern, keine MAUI-Abhängigkeit |
| `msTools.DeveloperApi.Maui` (Projekt) | MAUI-Klassenbibliothek (`UseMaui`, TFM-Matrix der App) | MAUI-Handler-Implementierung der drei UI-Endpunkte |
| `DevApiServer` | Klasse (`sealed`, `IDisposable`) in `msTools.DeveloperApi` | Startet `DevApiHttpServer`, schreibt `devapi.json` in Primär- bzw. Fallback-Verzeichnis, exponiert `Port`/`MetadataPath` |
| `DevApiEndpointHandler` | Delegate in `msTools.DeveloperApi` | Benannte Handler-Signatur `Task<DevApiResponse> (DevApiRequest, CancellationToken)` für `DevApiRouter.Map` |
| `DevApiRouterExtensions` | statische Klasse in `msTools.DeveloperApi` | `MapModel(this DevApiRouter, Func<object?>)` — registriert `GET /model` mit Snapshot-Provider |
| `MauiDevApiService` | Klasse (`sealed`, `IDisposable`) in `msTools.DeveloperApi.Maui` | Verdrahtet die vier Endpunkte (`MapModel` + drei `Map`), `MainThread`-Marshalling, Element-Suche, `devapi.json`-Ablage via `DevApiServer`; Konstruktor nimmt `Func<object?>` Snapshot-Provider |

## Änderungen an bestehenden Klassen

### `DevApiRouter` (Klasse, wird verschoben nach `msTools.DeveloperApi`)

- **Geänderte Methoden:** `Map(string method, string path, DevApiEndpointHandler handler)` — Signatur nutzt den benannten Delegaten statt `Func<DevApiRequest, CancellationToken, Task<DevApiResponse>>`; internes `Route`-Record speichert denselben Delegattyp.

### `DevApiService` (Klasse, bleibt in `VideoPlayer.Maui/DevApi/`)

- **Geänderte Methoden:** `Start()` — delegiert an `MauiDevApiService`; `Port`/`MetadataPath` reichen dessen Werte durch.
- **Entfernte Methoden:** `HandleModelAsync`, `HandleTreeAsync`, `HandleInvokeAsync`, `HandleSetTextAsync`, `InvokeOnPage`, `SetTextOnPage`, `FindElement`, `CurrentPage`, `WriteMetadataAsync` und die Body-Records — wandern nach `MauiDevApiService` (`msTools.DeveloperApi.Maui`).
- **Usings:** `using msTools.DeveloperApi.Maui;` + `using VideoPlayer.Mvu.DevApi;` (Snapshot).

### `VideoPlayer.Maui.csproj`

- **Neu:** `<ProjectReference Include="..\msTools.DeveloperApi\msTools.DeveloperApi.csproj" />` und `<ProjectReference Include="..\msTools.DeveloperApi.Maui\msTools.DeveloperApi.Maui.csproj" />`

### `VideoWebPlayer.Maui.Tests.csproj`

- **Neu:** `<ProjectReference Include="..\msTools.DeveloperApi\msTools.DeveloperApi.csproj" />` (nur Kern — das MAUI-Projekt kann nicht referenziert werden)

### `VideoPlayer.App.sln`

- **Neu:** Projekteinträge für `msTools.DeveloperApi` und `msTools.DeveloperApi.Maui` (je neue GUID).

### Verschobene Dateien

Nach `msTools.DeveloperApi/` (Namespace `msTools.DeveloperApi`, `#if`-Wrapper unverändert):
- `DevApiHttpServer.cs`, `DevApiRequestReader.cs`, `DevApiRequest.cs`, `DevApiResponse.cs`, `DevApiRequestException.cs`, `DevApiMetadata.cs`, `DevApiRouter.cs` (inkl. Signaturänderung)

Nach `msTools.DeveloperApi.Maui/` (Namespace `msTools.DeveloperApi.Maui`, `#if`-Wrapper unverändert):
- `DevApiVisualTree.cs`, `DevApiTreeNode.cs` (enthält `DevApiBounds`, `DevApiTreeItem`)

Verbleibt unverändert:
- `VideoPlayer.Mvu/DevApi/DevApiModelSnapshot.cs` (inkl. `DevApiKnownServer`, `DevApiDiscoveredServer`)
- `VideoPlayer.Maui/DevApi/DevApiService.cs` (wird zum Adapter umgeschrieben)
- `MauiProgram.cs`, `App.xaml.cs` (unverändert — `DevApiService` bleibt der registrierte Dienst)

### `DevApiContractTests` (Testklasse)

- `DevApiFiles()`: durchsucht nun `msTools.DeveloperApi`, `msTools.DeveloperApi.Maui`, `VideoPlayer.Mvu/DevApi` und `VideoPlayer.Maui/DevApi`.
- `DevApiFiles_ExistInBothProjects` → prüft Kern-, MAUI-Bibliotheks- und App-Verzeichnis (wird entsprechend angepasst/umbenannt).
- `DevApiService_MapsExactlyTheFourEndpoints`: prüft über `MauiDevApiService.cs` die genau vier Registrierungen (`MapModel` für `/model`, `Map` für `/tree`, `/invoke`, `/set-text`) sowie Abwesenheit von `dispatch` in allen Bibliotheks- und DevApi-Quellen.
- `DevApiHttpServer_BindsLoopbackOnly`: Pfad → `msTools.DeveloperApi/DevApiHttpServer.cs`.
- `DevApiService_WritesMetadataAndMarshallsToUiThread`: prüft `AppContext.BaseDirectory`, `FileSystem.AppDataDirectory`, `DevApiServer`-Verwendung und ≥3× `MainThread.InvokeOnMainThreadAsync` in `msTools.DeveloperApi.Maui/MauiDevApiService.cs`; ergänzend `"devapi.json"` in `msTools.DeveloperApi/DevApiServer.cs`.
- `DevApiVisualTree_UsesPlatformInvokeEquivalents`: Pfad → `msTools.DeveloperApi.Maui/DevApiVisualTree.cs`.
- `DevApiWiring_IsInsideIfdefBlocks`: unverändert (`DevApiService` in `MauiProgram.cs`/`App.xaml.cs` bleibt `#if`-guardiert).
- **Neue Tests:** `MsToolsDeveloperApi_HasNoMauiDependency` (Kern-csproj ohne `UseMaui`, `Microsoft.Maui`, `InternalsVisibleTo`), `MsToolsDeveloperApi_HasNoDispatchEndpoint` (kein `dispatch` in Bibliotheksquellen), `MsToolsDeveloperApiMaui_ReferencesCore` (MAUI-csproj enthält `ProjectReference` auf `msTools.DeveloperApi`), `MsToolsDeveloperApi_HasNoSecretSurface` (Bibliotheksquellen enthalten keine `token`/`password`/`secret`-Bezeichner — prüfbare Namenskonvention der Geheimnis-Invariante).

## Datenbankmigrationen

Keine.

## Validierungsregeln

Keine neuen Eingaben — die bestehenden Body-Validierungen (`TryGetJsonBody`, Pflichtfelder `automationId`/`text`) wandern unverändert nach `MauiDevApiService`.

## Konfigurationsänderungen

Keine.

## Seiteneffekte und Risiken

- **Release-Build der Bibliotheken ist leer:** Alle Typen stehen unter `#if DEBUG || DEV_API`; im Release existieren die Namespaces nicht — Verweise außerhalb `#if`-Blöcke in `VideoPlayer.Maui` brechen den Release-Build. Das ist das gewünschte Sicherheitsverhalten (Invariante „Debug-only"), muss aber bei künftigen Referenzen bedacht werden.
- **`DEV_API`-Opt-in:** Setzen des Defines muss für App **und** Bibliotheken gelten — beim Build propagiert `-p:DefineConstants=DEV_API` zu allen Projekten; ein Define nur in der App-csproj würde die Bibliothek leer lassen. Zu dokumentieren.
- **MAUI-Workload beim Bibliotheks-Build:** `msTools.DeveloperApi.Maui` benötigt die MAUI-Workload; Build der vollständigen Solution setzt sie damit ebenso voraus wie bisher (bereits durch `VideoPlayer.Maui` gegeben).
- **Namespace-Bruch:** `using VideoPlayer.Mvu.DevApi` in Testdateien muss auf `msTools.DeveloperApi` umgestellt werden; Typen der Bibliotheken sind öffentlich — kein `InternalsVisibleTo`-Bedarf.
- **Contract-Tests an Pfaden:** `DevApiContractTests` prüft physische Pfade — wird angepasst, ohne die Sicherheitsprüfungen zu verwässern.
- **E2E-Lauf:** Die 17 E2E-Tests bauen `VideoPlayer.Maui` (Windows-TFM, MAUI-Workload erforderlich) und starten die App — Laufzeitumgebung muss das leisten; falls nicht, wird das im Testergebnis vermerkt.

## Umsetzungsreihenfolge

1. **Projekt `msTools.DeveloperApi` anlegen**
   - Voraussetzungen: Keine.
   - Beschreibung: `msTools.DeveloperApi/msTools.DeveloperApi.csproj` nach `VideoPlayer.Mvu`-Konvention (net10.0, ImplicitUsings, Nullable, GenerateDocumentationFile + WarningsAsErrors CS1591, License-File, Authors/Copyright); Aufnahme in `VideoPlayer.App.sln`.

2. **Generische Kern-Dateien verschieben**
   - Voraussetzungen: Schritt 1.
   - Beschreibung: `DevApiHttpServer.cs`, `DevApiRequestReader.cs`, `DevApiRequest.cs`, `DevApiResponse.cs`, `DevApiRequestException.cs`, `DevApiMetadata.cs`, `DevApiRouter.cs` von `VideoPlayer.Mvu/DevApi/` nach `msTools.DeveloperApi/`; Namespace → `msTools.DeveloperApi`; `#if`-Wrapper unverändert.

3. **Neue Kern-API: `DevApiEndpointHandler`, `DevApiServer`, `DevApiRouterExtensions`**
   - Voraussetzungen: Schritt 2.
   - Beschreibung: Delegat `DevApiEndpointHandler`; `DevApiRouter.Map`-Signatur umstellen; `DevApiServer` mit Metadaten-Schreiblogik (Primär-/Fallback-Verzeichnis, `Debug.WriteLine`-Protokoll); `DevApiRouterExtensions.MapModel`.

4. **Projekt `msTools.DeveloperApi.Maui` anlegen**
   - Voraussetzungen: Schritt 3 (referenziert die Kernbibliothek).
   - Beschreibung: `msTools.DeveloperApi.Maui/msTools.DeveloperApi.Maui.csproj` mit `<UseMaui>true</UseMaui>` und der `IncludeXxxTarget`-TFM-Matrix aus `VideoPlayer.Maui.csproj`; `ProjectReference` auf `msTools.DeveloperApi`; Aufnahme in `VideoPlayer.App.sln`.

5. **MAUI-Dateien verschieben und `MauiDevApiService` anlegen**
   - Voraussetzungen: Schritt 4.
   - Beschreibung: `DevApiVisualTree.cs`, `DevApiTreeNode.cs` von `VideoPlayer.Maui/DevApi/` nach `msTools.DeveloperApi.Maui/` (Namespace → `msTools.DeveloperApi.Maui`); `MauiDevApiService` mit den Handler-Methoden, Body-Records, `FindElement`/`CurrentPage`, `MainThread`-Marshalling und `DevApiServer`-Verdrahtung (`AppContext.BaseDirectory` + `FileSystem.AppDataDirectory`).

6. **`DevApiService` in `VideoPlayer.Maui` zum Adapter umschreiben**
   - Voraussetzungen: Schritt 5, `ProjectReference`s in `VideoPlayer.Maui.csproj`.
   - Beschreibung: `DevApiService(MvuHost)` erzeugt `MauiDevApiService` mit `() => DevApiModelSnapshot.FromModel(mvuHost.Model)`; `Start`/`Dispose`/`Port`/`MetadataPath` delegieren.

7. **Solution- und Projektverdrahtung**
   - Voraussetzungen: Schritte 1–6.
   - Beschreibung: `ProjectReference`s in `VideoPlayer.Maui.csproj` (beide Bibliotheken) und `VideoWebPlayer.Maui.Tests.csproj` (nur Kern); Sln-Einträge prüfen.

8. **Tests anpassen und ergänzen**
   - Voraussetzungen: Schritte 2–7.
   - Beschreibung: `using`-Anpassungen (`msTools.DeveloperApi`) in `DevApiHttpServerTests`, `DevApiRouterTests`, `DevApiRequestReaderTests`, `DevApiMetadataTests`; `DevApiContractTests` gemäß „Änderungen an bestehenden Klassen" überarbeiten; neue Tests `DevApiServerTests_Metadata` und `DevApiRouterTests_MapModel`.

9. **Build und Testlauf**
   - Voraussetzungen: Schritte 1–8.
   - Beschreibung: `dotnet build` der Solution, `dotnet test VideoWebPlayer.Maui.Tests`; E2E-Suite `VideoPlayer.E2E.Tests` ausführen (17 Tests).

10. **Referenzierungsweg dokumentieren**
    - Voraussetzungen: Schritt 9.
    - Beschreibung: Im Dokumentationsschritt festhalten: Verwendung via Projekt-Referenz `msTools.DeveloperApi` (+ `msTools.DeveloperApi.Maui` für MAUI-Apps), Aktivierung via `DEBUG`/`DEV_API`, dass `DEV_API` beim Build für alle Projekte gesetzt werden muss.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `Start_AssignsPort` | `DevApiServerTests_Metadata` | `DevApiServer` vergibt Port > 0 nach `Start` |
| `Start_WritesMetadataToPrimaryDirectory` | `DevApiServerTests_Metadata` | `devapi.json` mit `port`/`pid`/`startedUtc` erscheint im Primärverzeichnis (Polling wg. async Write) |
| `Start_UsesFallbackDirectory_WhenPrimaryFails` | `DevApiServerTests_Metadata` | Ungültiges Primärverzeichnis (Pfad zeigt auf Datei statt Verzeichnis) → Datei landet im Fallback |
| `MapModel_ServesProviderSnapshot` | `DevApiRouterTests_MapModel` | `GET /model` liefert camelCase-serialisiertes Provider-Ergebnis |
| `MapModel_WrongMethod_Returns405` | `DevApiRouterTests_MapModel` | `POST /model` → 405 |
| `MsToolsDeveloperApi_HasNoMauiDependency` | `DevApiContractTests` | Kern-csproj ohne `UseMaui`, `Microsoft.Maui`, `InternalsVisibleTo` |
| `MsToolsDeveloperApi_HasNoDispatchEndpoint` | `DevApiContractTests` | Kein `dispatch` in Bibliotheksquellen |
| `MsToolsDeveloperApiMaui_ReferencesCore` | `DevApiContractTests` | MAUI-csproj referenziert `msTools.DeveloperApi` |
| `MsToolsDeveloperApi_HasNoSecretSurface` | `DevApiContractTests` | Bibliotheksquellen ohne `token`/`password`/`secret`-Bezeichner |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `DevApiHttpServerTests`, `DevApiRouterTests`, `DevApiRequestReaderTests`, `DevApiMetadataTests` | `using VideoPlayer.Mvu.DevApi` → `using msTools.DeveloperApi` |
| `DevApiContractTests` | Neue Dateipfade (`msTools.DeveloperApi`, `msTools.DeveloperApi.Maui`), geänderte Endpunkt-Registrierung (`MapModel`), `devapi.json`-Schreiben in `DevApiServer`, Metadaten-/Marshalling-Prüfung in `MauiDevApiService.cs` |
| `DevApiModelSnapshotTests` | Keine Anpassung — Snapshot bleibt in `VideoPlayer.Mvu.DevApi` |

### E2E-Tests (primärer Funktionsnachweis)

Die Anforderung führt keinen neuen Benutzerfluss ein; sie ist ein Refactoring mit Vertragsgarantie. Primärer Funktionsnachweis ist die **unverändert grüne** bestehende E2E-Suite:

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| Pflicht | Bootstrap-Fluss (6 Tests) | `BootstrapTests` | AC 2: Vertragskompatibilität | Endpunktverhalten nur gegen laufende App prüfbar |
| Pflicht | Server-Setup-Fluss (5 Tests) | `ServerSetupTests` | AC 2 | `/invoke`, `/set-text`, `/model`, `/tree` im echten Fluss |
| Pflicht | Pairing-Fluss (3 Tests) | `PairingTests` | AC 2 | dto. |
| Pflicht | Login-Fluss (2 Tests) | `LoginTests` | AC 2 | dto. |
| Pflicht | Serverwechsel (1 Test) | `ServerSwitchTests` | AC 2 | CollectionView-`itemIndex`-Pfad |

Bestehende E2E-Tests, die angepasst werden müssen: Keine.

## Offene Punkte

Keine. (Beide offenen Punkte wurden durch Nutzerentscheid geklärt: Projekt-Referenz im selben Repo; separates `msTools.DeveloperApi.Maui`-Projekt.)
