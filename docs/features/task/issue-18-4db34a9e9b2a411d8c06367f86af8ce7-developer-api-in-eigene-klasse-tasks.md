# Tasks: Developer-API in eigene Klassenbibliothek msTools.DeveloperApi auslagern

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Projekt | `msTools.DeveloperApi/msTools.DeveloperApi.csproj` anlegen (net10.0, Mvu-Konventionen) | Erledigt | DevApiContractTests / E2E-Suite |
| 2 | Projekt | `msTools.DeveloperApi.Maui/msTools.DeveloperApi.Maui.csproj` anlegen (`UseMaui`, TFM-Matrix mit `IncludeXxxTarget`-Schaltern) | Erledigt | DevApiContractTests / E2E-Suite |
| 3 | Projekt | Beide Projekte in `VideoPlayer.App.sln` aufnehmen | Erledigt | DevApiContractTests / E2E-Suite |
| 4 | Kern | `DevApiHttpServer.cs` nach `msTools.DeveloperApi` verschieben (Namespace + `#if`-Wrapper) | Erledigt | DevApiContractTests / E2E-Suite |
| 5 | Kern | `DevApiRequestReader.cs` verschieben | Erledigt | DevApiContractTests / E2E-Suite |
| 6 | Kern | `DevApiRequest.cs` verschieben | Erledigt | DevApiContractTests / E2E-Suite |
| 7 | Kern | `DevApiResponse.cs` verschieben | Erledigt | DevApiContractTests / E2E-Suite |
| 8 | Kern | `DevApiRequestException.cs` verschieben | Erledigt | DevApiContractTests / E2E-Suite |
| 9 | Kern | `DevApiMetadata.cs` verschieben | Erledigt | DevApiContractTests / E2E-Suite |
| 10 | Kern | `DevApiRouter.cs` verschieben | Erledigt | DevApiContractTests / E2E-Suite |
| 11 | Kern | Delegat `DevApiEndpointHandler` einführen und `DevApiRouter.Map`-Signatur umstellen | Erledigt | DevApiContractTests / E2E-Suite |
| 12 | Kern | `DevApiServer` anlegen (Server + `devapi.json` mit Primär-/Fallback-Verzeichnis) | Erledigt | DevApiContractTests / E2E-Suite |
| 13 | Kern | `DevApiRouterExtensions.MapModel` (Snapshot-Provider für `GET /model`) anlegen | Erledigt | DevApiContractTests / E2E-Suite |
| 14 | MAUI | `DevApiVisualTree.cs` nach `msTools.DeveloperApi.Maui` verschieben | Erledigt | DevApiContractTests / E2E-Suite |
| 15 | MAUI | `DevApiTreeNode.cs` (inkl. `DevApiBounds`, `DevApiTreeItem`) nach `msTools.DeveloperApi.Maui` verschieben | Erledigt | DevApiContractTests / E2E-Suite |
| 16 | MAUI | `MauiDevApiService` anlegen (4 Endpunkte, `MainThread`-Marshalling, `DevApiServer` mit `AppContext.BaseDirectory` + `FileSystem.AppDataDirectory`) | Erledigt | DevApiContractTests / E2E-Suite |
| 17 | MAUI | `ProjectReference` auf `msTools.DeveloperApi` in `msTools.DeveloperApi.Maui.csproj` | Erledigt | DevApiContractTests / E2E-Suite |
| 18 | App | `DevApiService` zum Adapter umschreiben (`MvuHost` → `MauiDevApiService` mit Snapshot-Provider) | Erledigt | DevApiContractTests / E2E-Suite |
| 19 | App | `ProjectReference`s auf beide Bibliotheken in `VideoPlayer.Maui.csproj` | Erledigt | DevApiContractTests / E2E-Suite |
| 20 | Tests | `ProjectReference` auf `msTools.DeveloperApi` in `VideoWebPlayer.Maui.Tests.csproj` | Erledigt | DevApiContractTests / E2E-Suite |
| 21 | Tests | `using`-Umstellung `msTools.DeveloperApi` in 4 DevApi-Testdateien | Erledigt | DevApiContractTests / E2E-Suite |
| 22 | Tests | `DevApiContractTests` an neue Pfade/Registrierung anpassen | Erledigt | DevApiContractTests / E2E-Suite |
| 23 | Tests | Neue Contract-Tests `MsToolsDeveloperApi_HasNoMauiDependency`, `MsToolsDeveloperApi_HasNoDispatchEndpoint`, `MsToolsDeveloperApiMaui_ReferencesCore` | Erledigt | DevApiContractTests / E2E-Suite |
| 24 | Tests | Neue Testklasse `DevApiServerTests_Metadata` (Port, Primärverzeichnis, Fallback) | Erledigt | DevApiContractTests / E2E-Suite |
| 25 | Tests | Neue Testklasse `DevApiRouterTests_MapModel` (Snapshot, 405) | Erledigt | DevApiContractTests / E2E-Suite |
| 26 | E2E-Tests | Bestehende 17 E2E-Tests unverändert grün ausführen | Erledigt | DevApiContractTests / E2E-Suite |
| 27 | Doku | Referenzierungsweg (Projekt-Referenz, `DEV_API`-Define-Propagation) dokumentieren | Erledigt | DevApiContractTests / E2E-Suite |
