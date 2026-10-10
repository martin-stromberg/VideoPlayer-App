# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan vollständig

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| 1. `msTools.DeveloperApi` stellt HTTP-Server, Routing, `devapi.json` und Provider-/Handler-Schnittstellen bereit | Verschiebung der 7 Kern-Dateien; neu: `DevApiServer` (Server + Metadaten-Fassade), `DevApiEndpointHandler` (Handler-Schnittstelle), `DevApiRouterExtensions.MapModel` (Snapshot-Provider) — Umsetzungsschritte 1–3 | `DevApiHttpServerTests`, `DevApiRouterTests`, `DevApiRequestReaderTests`, `DevApiMetadataTests` (migriert), neu `DevApiServerTests_Metadata`, `DevApiRouterTests_MapModel` | Abgedeckt |
| 2. `VideoPlayer.Maui` nutzt die Bibliothek; Verträge (`/model`, `/tree`, `/invoke`, `/set-text`, `devapi.json`) kompatibel; E2E-Suite (17 Tests) grün | `DevApiService`-Adapter + `ProjectReference`s; Endpunkt-Handler wandern inhaltlich unverändert nach `MauiDevApiService`/`DevApiVisualTree`; JSON-Feldnamen und Statuscodes unverändert — Umsetzungsschritte 4–7 | `DevApiContractTests` (Endpunkt-Zählung, Metadaten, Loopback), 17 E2E-Tests als Pflicht-Nachweis | Abgedeckt |
| 3. Sicherheitsinvarianten (Loopback, Debug-only) im Bibliotheksdesign erzwungen | `TcpListener(IPAddress.Loopback, 0)` unverändert; vollständiger `#if DEBUG || DEV_API`-Wrapper in allen Bibliotheksdateien → Release-Assembly leer, Verweise außerhalb `#if` brechen den Build; kein `InternalsVisibleTo`; kein `dispatch` möglich (nur `Map`-Registrierung) | `DevApiHttpServer_BindsLoopbackOnly`, `DevApiFiles_AreFullyWrappedInDebugOrDevApiIfdef`, `DevApiWiring_IsInsideIfdefBlocks`, `MsToolsDeveloperApi_HasNoMauiDependency`, `MsToolsDeveloperApi_HasNoDispatchEndpoint`, `MsToolsDeveloperApi_HasNoSecretSurface` | Abgedeckt |
| 4. Paketveröffentlichung/Referenzierungsweg dokumentiert | Projekt-Referenz im selben Repo (Nutzerentscheid); Umsetzungsschritt 10 + Dokumentationsschritt | — (Dokumentationsartefakt) | Abgedeckt |

## Fehlende oder unvollständige Testanforderungen

Nicht zutreffend (Status `Plan vollständig`).

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| AC 2 — Vertragskompatibilität über alle vier Endpunkte im realen App-Fluss | Bestehende Suite `VideoPlayer.E2E.Tests`: `BootstrapTests` (6), `ServerSetupTests` (5), `PairingTests` (3), `LoginTests` (2), `ServerSwitchTests` (1) — unverändert, muss grün laufen | Abgedeckt |
| Neuer Benutzerfluss | Kein neuer UI-Fluss in der Anforderung — reines Refactoring mit Vertragsgarantie | Nicht erforderlich mit Begründung |

## Fehlende oder unvollständige Planbestandteile

Nicht zutreffend (Status `Plan vollständig`).

## Hinweise

- Verifiziert: Alle Verweise auf `VideoPlayer.Mvu.DevApi`/`VideoPlayer.Maui.DevApi` außerhalb der DevApi-Dateien selbst liegen ausschließlich in den vier Testdateien (`using`-Umstellung geplant) und `DevApiContractTests` (Pfad-Anpassung geplant). `MauiProgram.cs`/`App.xaml.cs` sprechen weiterhin `DevApi.DevApiService` (bestehender Adapter) — keine Änderung nötig.
- Verifiziert: `dotnet test` läuft lokal per Default in `Debug` → `#if`-geschützte Bibliotheks-Typen und Tests haben ein Compile-Ziel; die CI (`pr-checks.yml`) baut das Testprojekt in `Release`, womit die DevApi-Tests dort wie bisher entfallen — kein Verhaltenwechsel.
- Verifiziert: `-p:DefineConstants=DEV_API` propagiert als globale MSBuild-Eigenschaft in alle Projekte des Builds — der im Plan dokumentierte Hinweis zur `DEV_API`-Propagation ist korrekt.
- `msTools.DeveloperApi.Maui` sollte neben `UseMaui`/`SingleProject` auch die `SupportedOSPlatformVersion`-Werte der App spiegeln (Implementierungsdetail, aus `VideoPlayer.Maui.csproj` ableitbar).
- CI führt `dotnet format whitespace --verify-no-changes` aus — neue Dateien müssen format-sauber sein.
- `MsToolsDeveloperApi_HasNoSecretSurface` wurde als zusätzliche prüfbare Namenskonvention für die Geheimnis-Invariante in den Plan aufgenommen.
