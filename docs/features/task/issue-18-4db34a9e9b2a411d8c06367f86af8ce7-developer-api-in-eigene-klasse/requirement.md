# Übersetzte Anforderung: Developer-API in eigene Klassenbibliothek msTools.DeveloperApi auslagern

## Fachliche Zusammenfassung

Die Developer-API (lokaler Debug-/E2E-HTTP-Server aus Issue #17, Schritt 2) wird aus dem App-Code in eine eigenständige, wiederverwendbare Klassenbibliothek `msTools.DeveloperApi` ausgelagert. Die Bibliothek enthält den plattformneutralen Kern (Mini-HTTP-Server, Request-Parser, Routing, `devapi.json`-Metadaten, Endpunkt-Registrierung) ohne MAUI-Abhängigkeit; die App-spezifischen Endpunkte (`/model`-Snapshot, `/tree`, `/invoke`, `/set-text`) werden über Provider-/Handler-Schnittstellen angebunden. `VideoPlayer.Maui` nutzt anschließend die Bibliothek bei unverändertem HTTP-Vertrag.

## Betroffene Klassen und Komponenten

- **Neue Klassenbibliothek `msTools.DeveloperApi`** (neues Projekt, plattformneutral, ohne MAUI-Abhängigkeit):
  - Zu verschieben aus `VideoPlayer.Mvu/DevApi/`: `DevApiHttpServer`, `DevApiRouter`, `DevApiRequest`, `DevApiResponse`, `DevApiRequestReader`, `DevApiRequestException`, `DevApiMetadata`
  - Neu: Provider-Schnittstelle für `GET /model` (z. B. `IModelSnapshotProvider` oder `Func<object>`-Registrierung) — die Bibliothek kennt kein konkretes Modell
  - Neu: Handler-Schnittstellen für `tree`/`invoke`/`set-text` (damit der Kern auch für Nicht-MAUI-Projekte schlank bleibt)
  - Neu: Start-API so gestaltet, dass der Compile-time-Ausschluss (`DEBUG || DEV_API`) nicht vergessen werden kann
- **Bleibt app-seitig** (`VideoPlayer.Maui/DevApi/` bzw. `VideoPlayer.Mvu/DevApi/`): `DevApiService` (Verdrahtung der vier Endpunkte, `MainThread`-Marshalling, Metadaten-Ablage), `DevApiVisualTree`, `DevApiTreeNode`/`DevApiBounds`/`DevApiTreeItem`, `DevApiModelSnapshot` (gebunden an `AppModel`), `DevApiKnownServer`, `DevApiDiscoveredServer`
- **Anzupassen**: `VideoPlayer.App.sln` (neues Projekt), `VideoPlayer.Maui/VideoPlayer.Maui.csproj` (Projekt-Referenz), `VideoPlayer.Maui/MauiProgram.cs` + `App.xaml.cs` (Namespaces/Verdrahtung), `VideoWebPlayer.Maui.Tests` (Namespace-Updates, `DevApiContractTests`-Pfade/Regeln, Projekt-Referenz auf die Bibliothek)
- **Dokumentation**: `docs/help/developer-api.md`, `README.md`, `docs/RELEASE_NOTES.md`, `changes.log`; Referenzierungsweg (NuGet-Feed oder Projekt-Referenz) dokumentieren
- **Tests**: `VideoPlayer.E2E.Tests` (17 Tests, unverändert grün als Vertragsnachweis), `VideoWebPlayer.Maui.Tests` (bestehende DevApi-Tests migrieren auf die Bibliothek)

## Implementierungsansatz

- Neues SDK-Style-Projekt `msTools.DeveloperApi` (net10.0, Nullable + ImplicitUsings wie die übrigen Bibliotheken), Aufnahme in `VideoPlayer.App.sln` und als `ProjectReference` in `VideoPlayer.Maui` (und ggf. `VideoPlayer.Mvu`, falls `DevApiModelSnapshot` dort verbleibt — eher nicht nötig, da der Snapshot app-seitig bleibt).
- Verschieben der sieben generischen `DevApi*`-Dateien; Namespace-Umstellung auf `msTools.DeveloperApi` (Klassennamen können beibehalten oder umbenannt werden — Designentscheidung in der Planung).
- `GET /model`: Die Bibliothek registriert den Endpunkt gegen einen Snapshot-Provider; die App injiziert `() => DevApiModelSnapshot.FromModel(mvuHost.Model)`.
- `tree`/`invoke`/`set-text`: Als app-seitig registrierte Handler (über das vorhandene `router.Map`-Muster bzw. eine dünne Handler-Abstraktion); die MAUI-Implementierung (`DevApiVisualTree`) verbleibt in `VideoPlayer.Maui`. Ein separates `msTools.DeveloperApi.Maui`-Paket ist im Issue als Option genannt — in der Planung zu entscheiden (Empfehlung: vorerst app-seitig lassen, da nur ein Konsument existiert).
- Compile-time-Ausschluss: Bibliotheksdateien selbst ohne `#if`-Zwang kompilierbar; der Start-Einstiegspunkt wird so designt, dass nur über einen debug-guarded Aufrufpfad gestartet werden kann (z. B. Erweiterungsmethode innerhalb `#if DEBUG || DEV_API` oder Guard-API, die in Release nicht verfügbar ist) — konkrete Ausgestaltung in der Planung.
- Vertragskompatibilität: Endpunkt-Pfade, Methoden, JSON-Feldnamen (camelCase), Statuscodes und `devapi.json`-Format bleiben byte-genau erhalten; `DevApiContractTests` werden auf die neue Struktur angepasst (Pfad-Regeln), ohne das Sicherheitsniveau zu senken.
- Referenzierungsweg: Projekt-Referenz im selben Repo (Empfehlung) oder NuGet-Feed — in der Planung festzulegen und zu dokumentieren (Akzeptanzkriterium 4).

## Konfiguration

Keine neue Laufzeit-Konfiguration. Der Aktivierungsmechanismus bleibt der bestehende Compile-Schalter `#if DEBUG || DEV_API` in der App; die Bibliothek selbst führt keine eigenen Konfigurationseinträge ein (Port bleibt systemvergeben, Adresse fest `127.0.0.1`).

## Offene Fragen

1. **Code-Ablageort der Bibliothek:** Soll `msTools.DeveloperApi` als Projekt im selben Repository/Solution leben (Projekt-Referenz) oder als separat versioniertes NuGet-Paket über einen Feed? (Empfehlung: Projekt im Repo, Dokumentation des Referenzwegs.)
2. **MAUI-Teil separat paketieren?** Verbleibt die MAUI-Handler-Implementierung (`DevApiVisualTree`, `DevApiService`) vorerst in `VideoPlayer.Maui`, oder wird sofort ein `msTools.DeveloperApi.Maui`-Projekt angelegt? (Empfehlung: vorerst in der App — Zweitverwender existiert noch nicht.)
3. **Namensgebung im neuen Namespace:** Behalten die Typen ihr `DevApi*`-Präfix unter `msTools.DeveloperApi` (minimaler Diff) oder werden sie umbenannt (z. B. `DeveloperApiHttpServer`)? (Empfehlung: `DevApi*`-Namen beibehalten.)
4. **Testprojekt-Zuordnung:** Die DevApi-Unit-/Integraltests (`DevApiHttpServerTests`, `DevApiRouterTests`, `DevApiRequestReaderTests`, `DevApiMetadataTests`) testen danach Bibliothekscode — verbleiben sie in `VideoWebPlayer.Maui.Tests` oder wandern sie in ein eigenes `msTools.DeveloperApi.Tests`-Projekt?
