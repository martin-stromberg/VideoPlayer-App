# Tasks: Developer-API in eigene Klassenbibliothek msTools.DeveloperApi auslagern

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Projekt | `msTools.DeveloperApi/msTools.DeveloperApi.csproj` anlegen (net10.0, Mvu-Konventionen) | Offen | — |
| 2 | Projekt | `msTools.DeveloperApi.Maui/msTools.DeveloperApi.Maui.csproj` anlegen (`UseMaui`, TFM-Matrix mit `IncludeXxxTarget`-Schaltern) | Offen | — |
| 3 | Projekt | Beide Projekte in `VideoPlayer.App.sln` aufnehmen | Offen | — |
| 4 | Kern | `DevApiHttpServer.cs` nach `msTools.DeveloperApi` verschieben (Namespace + `#if`-Wrapper) | Offen | — |
| 5 | Kern | `DevApiRequestReader.cs` verschieben | Offen | — |
| 6 | Kern | `DevApiRequest.cs` verschieben | Offen | — |
| 7 | Kern | `DevApiResponse.cs` verschieben | Offen | — |
| 8 | Kern | `DevApiRequestException.cs` verschieben | Offen | — |
| 9 | Kern | `DevApiMetadata.cs` verschieben | Offen | — |
| 10 | Kern | `DevApiRouter.cs` verschieben | Offen | — |
| 11 | Kern | Delegat `DevApiEndpointHandler` einführen und `DevApiRouter.Map`-Signatur umstellen | Offen | — |
| 12 | Kern | `DevApiServer` anlegen (Server + `devapi.json` mit Primär-/Fallback-Verzeichnis) | Offen | — |
| 13 | Kern | `DevApiRouterExtensions.MapModel` (Snapshot-Provider für `GET /model`) anlegen | Offen | — |
| 14 | MAUI | `DevApiVisualTree.cs` nach `msTools.DeveloperApi.Maui` verschieben | Offen | — |
| 15 | MAUI | `DevApiTreeNode.cs` (inkl. `DevApiBounds`, `DevApiTreeItem`) nach `msTools.DeveloperApi.Maui` verschieben | Offen | — |
| 16 | MAUI | `MauiDevApiService` anlegen (4 Endpunkte, `MainThread`-Marshalling, `DevApiServer` mit `AppContext.BaseDirectory` + `FileSystem.AppDataDirectory`) | Offen | — |
| 17 | MAUI | `ProjectReference` auf `msTools.DeveloperApi` in `msTools.DeveloperApi.Maui.csproj` | Offen | — |
| 18 | App | `DevApiService` zum Adapter umschreiben (`MvuHost` → `MauiDevApiService` mit Snapshot-Provider) | Offen | — |
| 19 | App | `ProjectReference`s auf beide Bibliotheken in `VideoPlayer.Maui.csproj` | Offen | — |
| 20 | Tests | `ProjectReference` auf `msTools.DeveloperApi` in `VideoWebPlayer.Maui.Tests.csproj` | Offen | — |
| 21 | Tests | `using`-Umstellung `msTools.DeveloperApi` in 4 DevApi-Testdateien | Offen | — |
| 22 | Tests | `DevApiContractTests` an neue Pfade/Registrierung anpassen | Offen | — |
| 23 | Tests | Neue Contract-Tests `MsToolsDeveloperApi_HasNoMauiDependency`, `MsToolsDeveloperApi_HasNoDispatchEndpoint`, `MsToolsDeveloperApiMaui_ReferencesCore` | Offen | — |
| 24 | Tests | Neue Testklasse `DevApiServerTests_Metadata` (Port, Primärverzeichnis, Fallback) | Offen | — |
| 25 | Tests | Neue Testklasse `DevApiRouterTests_MapModel` (Snapshot, 405) | Offen | — |
| 26 | E2E-Tests | Bestehende 17 E2E-Tests unverändert grün ausführen | Offen | — |
| 27 | Doku | Referenzierungsweg (Projekt-Referenz, `DEV_API`-Define-Propagation) dokumentieren | Offen | — |
