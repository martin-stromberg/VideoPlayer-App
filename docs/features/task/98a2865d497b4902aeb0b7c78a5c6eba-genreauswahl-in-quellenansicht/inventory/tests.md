# Tests

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-10-25, ca. 13:22–13:30 (lokale Zeit, Europe/Berlin)
- Branch und Commit-ID: `task/98a2865d497b4902aeb0b7c78a5c6eba-genreauswahl-in-quellenansicht`, `ffea5e92fc25ab31087a2df62c9d96c0544cd46e`
- Uncommittete Änderungen im getesteten Stand: nur Lifecycle-Artefakte (`docs/features/…`), `issue.md` (untracked), `testflight_feedback (1).zip` (untracked) — kein Produktivcode geändert
- Testumgebung und Runtime-/SDK-Versionen: Windows, .NET SDK 10.0.402, Workloads maui-windows 10.0.20 / ios 26.5.10318, xunit.v3 3.2.2, Microsoft.NET.Test.Sdk 18.8.1
- Ermittelte Testsuiten und Quellen der Testbefehle:
  - `VideoPlayer.Maui.Tests` (net10.0, xunit.v3) — Befehl aus `.github/workflows/pr-checks.yml`: `dotnet test VideoPlayer.Maui.Tests/VideoPlayer.Maui.Tests.csproj --configuration Release`
  - `VideoPlayer.E2E.Tests` (net10.0-windows, FlaUI/UIA3) — Befehl aus `docs/help/e2e-tests.md`: `dotnet test VideoPlayer.E2E.Tests/VideoPlayer.E2E.Tests.csproj` (baut Debug-App selbst, Desktop-Session erforderlich, ~20–25 min, 25 Tests)

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| 1 | `dotnet test VideoPlayer.Maui.Tests/VideoPlayer.Maui.Tests.csproj --configuration Release` | Repo-Root | 1 | 1249 (laut Abschlusszeile) | 0 | 0 | [maui-tests-baseline.log](test-results/maui-tests-baseline.log) |
| 2 | `dotnet test VideoPlayer.Maui.Tests/VideoPlayer.Maui.Tests.csproj --configuration Release --no-build` | Repo-Root | 0 | 1354 | 0 | 0 | [maui-tests-baseline-rerun.log](test-results/maui-tests-baseline-rerun.log) |

Hinweis Lauf 1: Nach Meldung aller 1249 erfolgreichen Tests stürzte der Testprozess beim Teardown ab (`[FATAL ERROR] Xunit.Sdk.TestPipelineException — Catastrophic failure: Test process crashed with exit code -1073741819`). Lauf 2 (identischer Build) lief vollständig sauber (1354 Tests) → der Absturz ist ein Flake beim Prozessende, kein Testfehler. Die abweichende Gesamtzahl von Lauf 1 ist auf den vorzeitigen Abbruch der Abschlussberichterstattung zurückzuführen.

### Nachgewiesene bestehende Testfehler

Keine — kein Testfall ist fehlgeschlagen (nur der Infrastruktur-Abbruch von Lauf 1 nach erfolgreicher Testausführung).

### Testlücken und Ausführungsprobleme

- **E2E-Suite nicht im Ausgangszustand ausgeführt:** `VideoPlayer.E2E.Tests` benötigt eine Windows-Desktop-Session mit sichtbaren Fenstern und ~20–25 min Laufzeit; Ausführung erfolgt nach der Implementierung (Lifecycle-Schritt „Tests ausführen"), da die UI-Änderung selbst E2E-Nachweise erfordert.
- **Kein Layout-/Höhennachweis der Genre-Leiste:** Kein bestehender Test prüft die belegte Höhe von `GenresCollection`; das Test-DTO `DevApiTreeNode` bildet die serverseitig vorhandenen `bounds`-Daten noch nicht ab.
- **Absturz-Flake des Testprozesses** (Lauf 1) ist dokumentiert, ohne dass Produktiv- oder Testcode verändert wurde.

## Testklassen

### `AppUpdateTests_Sources` (VideoPlayer.Maui.Tests)
- `SourceBrowserOpened_Restore_LoadsGenresAndFirstPage` — Wiederherstellungspfad lädt Genres + erste Seite
- `SourceGenresLoaded_WrongSource_IsDiscarded` — Antwort verlassener Quelle wird verworfen
- `SourceGenresLoaded_OnlyAllEntry_HidesGenreFilter` — „Alle"-nur-Liste → `ShowGenreFilter` = false
- `SourceGenresLoaded_ServerGenres_ShowsGenreFilter` — reale Genres → `ShowGenreFilter` = true
- (weitere Browser-Fälle: Auswahl, Suche, Paging, Fehlerzustände)

### `SourcesContractTests` (VideoPlayer.Maui.Tests)
- `SourceOverviewPage_HasGenreBar_Search_AndIncrementalGrid` — **pinnt `{Binding Model.SourceBrowser.Genres}` im XAML** sowie Suche/Raster/stabile ItemsSource
- `SourceOverviewPage_DispatchesBrowserMessages` — pinnt Dispatch von `SourceGenreSelectedMessage` u. a.

### `DesignTokensContractTests` (VideoPlayer.Maui.Tests)
- `Styles_PinsPillShapes` — pinnt `Chip`-Style (`RoundRectangle 999`) und `{StaticResource Chip}` in `SourceOverviewPage.xaml`

### `PageLifecycleContractTests` (VideoPlayer.Maui.Tests)
- `SourceOverviewPage_UnsubscribesModelChangedOnDisappearing`, `SourceOverviewPage_IgnoresModelChangesAfterDisappearing` — Abo-Lebenszyklus der Seite

### `SourceOverviewSearchContractTests` (VideoPlayer.Maui.Tests)
- Suchfeld-Verhalten der Seite (Mirroring des abgeschickten Suchtexts)

### `SourceBrowsingTests` (VideoPlayer.E2E.Tests)
- `GenreBar_VisibleAndFilters_ResetsOnAll` — Leiste sichtbar bei Server-Genres; `InvokeAsync("GenresCollection", 1)` → `genreId=4`-Request; „Alle" setzt zurück
- `GenreBar_Hidden_WhenServerHasNoGenres` — leere Serverliste → `IsVisible` = false

### `SourceSearchTests` / `SourcePagingTests` (VideoPlayer.E2E.Tests)
- Sucheingabe mit `GenresCollection`-Invoke (Index 1) bzw. seitenweises Nachladen der `ItemsCollection`

## Hilfsmethoden

### `E2ETestBase` (VideoPlayer.E2E.Tests)
- `StartAppAsync` — App-Start mit isoliertem Profil (`VIDEOPLAYER_PROFILE=e2e-…`)
- `ConnectToPairingRequiredAsync`, `PairAsync`, `LoginAsync` — Setup-Fluss bis zur angemeldeten App

### `SourceBrowsingTests` (eigen)
- `NewServer(withGenres)` — `MockVideoServer` mit Quelle 41 + zwei Einträgen; optional Genres „Komödie" (4)/„Drama" (8)
- `LoginToSourceAsync` — Setup-Fluss + Öffnen der Quelle per Flyout-Eintrag
- `WaitForBarVisibilityAsync` — polltt `GET /tree` auf `GenresCollection.IsVisible`

### `DevApiClient` (VideoPlayer.E2E.Tests)
- `GetTreeAsync` — Elementbaum mit `FindByAutomationId`
- `InvokeAsync(automationId, itemIndex)` — Clicked-/Item-Auswahl per `POST /invoke`
- `WaitForModelAsync` — Modell-Polling mit Timeout und Letztstand im Fehlertext

### `MockVideoServer` (VideoPlayer.E2E.Tests)
- `Sources`, `SourceItems`, `SourceGenres` — konfigurierbare Listen; `SourceItemRequests` protokolliert `api/items`-Aufrufe (Nachweis der `genreId`-Filterung)
