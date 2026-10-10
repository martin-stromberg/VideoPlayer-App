# Bestandsaufnahme: Developer-API in eigene Klassenbibliothek msTools.DeveloperApi auslagern

Analysiert wurde die bestehende Developer-API-Implementierung (Issue #17, Schritt 2) in `VideoPlayer.Mvu/DevApi/` und `VideoPlayer.Maui/DevApi/` samt Verdrahtung, Tests, E2E-Client und Projektstruktur — bezogen auf die Auslagerung in eine wiederverwendbare Klassenbibliothek `msTools.DeveloperApi`.

## Zusammenfassung

- **Generischer, portierbarer Kern** liegt in `VideoPlayer.Mvu/DevApi/` (7 Dateien, ~560 Zeilen): `DevApiHttpServer`, `DevApiRequestReader`, `DevApiRouter`, `DevApiRequest`, `DevApiResponse`, `DevApiRequestException`, `DevApiMetadata`. Das Projekt `VideoPlayer.Mvu` targetet `net10.0` ohne MAUI-Abhängigkeit — der Kern ist bereits plattformneutral.
- **MAUI-/App-gebundener Teil** liegt in `VideoPlayer.Maui/DevApi/` (3 Dateien): `DevApiService` (Endpunkt-Verdrahtung, `MainThread`-Marshalling, `devapi.json`-Ablage), `DevApiVisualTree`, `DevApiTreeNode`/`DevApiBounds`/`DevApiTreeItem`.
- **App-gebunden, aber im plattformneutralen Projekt:** `DevApiModelSnapshot` (in `VideoPlayer.Mvu/DevApi/`) bildet das konkrete `AppModel` ab — es ist der einzige nicht-generische Typ im Kern-Verzeichnis.
- **Compile-time-Ausschluss:** Jede DevApi-Quelldatei ist vollständig in `#if DEBUG || DEV_API` gewrappt (Zeile 1 `#if`, letzte Zeile `#endif`); die Verdrahtung in `MauiProgram.cs`/`App.xaml.cs` steht in eigenen `#if`-Blöcken. `DevApiContractTests` überwacht das statisch.
- **Vertrag:** 4 Endpunkte (`GET /model`, `GET /tree`, `POST /invoke`, `POST /set-text`), `devapi.json` mit `port`/`pid`/`startedUtc`, Loopback-only, kein `dispatch`-Endpunkt. Dokumentiert in `docs/help/developer-api.md` und `docs/features/task/issue-17-flaui-e2e/inventory/dev-api-vertraege.md`.
- **Tests:** 6 DevApi-Testdateien in `VideoWebPlayer.Maui.Tests` (Unit/Integral + Contract-Tests, alle unter `#if DEBUG || DEV_API`), plus 17 E2E-Tests in `VideoPlayer.E2E.Tests`, die über `DevApiClient` gegen die laufende App sprechen.
- **Fehlend:** Eine eigenständige Bibliothek existiert noch nicht; es gibt kein NuGet-Packaging-Setup im Repo (NuGet.config enthält nur nuget.org-Quelle), kein Directory.Build.props, kein zentrales Package-Management.

## Details

- [Logik und Verdrahtung](inventory/logic.md) — Server, Parser, Router, `DevApiService`, `DevApiVisualTree`, DI-/Startverdrahtung
- [Datenmodelle](inventory/models.md) — Request/Response/Metadaten/TreeNode/Snapshot-Records
- [Tests](inventory/tests.md) — Unit-, Contract- und E2E-Tests inkl. Hilfsklassen
- [Projektstruktur und Referenzierungswege](inventory/projekt-setup.md) — Solution, Ziel-Frameworks, Konventionen, NuGet-Situation
- [HTTP-Vertrag der Developer-API](inventory/vertraege.md) — Endpunkte, Felder, Statuscodes, `devapi.json`
