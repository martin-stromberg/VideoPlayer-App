# Tests — Bestandsaufnahme

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-11-24, lokale Zeit (Windows, Europe/Berlin)
- Branch und Commit-ID: `task/issue-43-3ee1dbce5f284347add37dde0e222b44-folge-direkte-wiedergabe-aus-w-schritt-4-playlist-anzeige` @ `b1092ba4f7070733ae763ddb806c128c9def8997`
- Uncommittete Änderungen im getesteten Stand: nur Feature-Artefakte unter `docs/features/task/…-schritt-4-playlist-anzeige/` (untracked)
- Testumgebung: Windows, .NET SDK 10.0.401; Testsuite `VideoWebPlayer.Maui.Tests` (xUnit, net10.0, Release)
- Ermittelte Testsuiten und Quellen der Testbefehle: `.github/workflows/pr-checks.yml` (build `VideoWebPlayer.Maui.Tests` + `dotnet test`; MAUI-Build `net10.0-windows10.0.19041.0`); E2E-Suite `VideoPlayer.E2E.Tests` (xUnit + FlaUI, baut `VideoPlayer.Maui` Debug/Windows — lokaler Lauf, nicht Teil der CI-Checks)

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| Baseline Unit/Contract | `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj --configuration Release --logger "console;verbosity=normal"` | Repo-Stamm | 0 | 1101 | 0 | 0 | [baseline-unittests.txt](test-results/baseline-unittests.txt) |

### Nachgewiesene bestehende Testfehler

Keine — der Baseline-Lauf ist vollständig erfolgreich (1101/1101).

### Testlücken und Ausführungsprobleme

- `VideoPlayer.E2E.Tests` wurde im Baseline-Lauf nicht ausgeführt (erfordert MAUI-Windows-Debug-Build und FlaUI-UIA-Umgebung; wird für den Feature-Nachweis in Schritt 10 ausgeführt).
- `VideoWebPlayer.Client`-Build erzeugt vorhandene Warnungen (CS0108, CS8618) — unverändert, kein Fehlschlag.

## Testklassen

### `AppRoutesTests`
- `MenuPages_ContainsAllFiveMenuEntriesInOrder` — pinnt `AppRoutes.MenuPages` (5 Einträge, Reihenfolge)
- `ToRoute_RoundTripsAllMenuPages` / `ToRoute_Loading_HasNoRoute` / `TryGetPage_*` — Routen-Roundtrip und Ortsauflösung

### `VideoPlayerMauiContractTests`
- `AppShell_ContainsAllMenuEntries` — pinnt `ExpectedMenuEntries` (5 statische `FlyoutItem`s, Titel + Route gegen `AppRoutes.ToRoute`)
- `AppShell_MenuEntries_BindToExistingPages` — `{DataTemplate pages:X}` verweist auf vorhandene `Pages/X.xaml`

### `PageLifecycleContractTests`
- `{Page}_UnsubscribesModelChangedOnDisappearing` / `{Page}_IgnoresModelChangesAfterDisappearing` — je Seite (Quelltext-Vertrag: `subscribed`-Flag, `ModelChanged -=`/`+=`)

### `AppUpdateTests_*` (je Thema eine Datei)
- `AppUpdateTests_Sources`, `AppUpdateTests_FavoritesProfile`, `AppUpdateTests_Navigation` u. a. — prüfen `AppUpdate.Update`-Transitionen (Model + emittierte Effekte)

### `VideoPlayerApiClientTests_*`
- `…_Endpoints`, `…_Authentication`, `…_RetryPolicy`, `…_SessionRefresh`, `…_TokenRefresh` — Endpunkt-URLs, Retry-/Reauth-Verhalten gegen `HttpMessageHandler`-Stubs (`Stubs/`)

### `EnumCoverageTests`
- Je Enum eine Testmethode — neue Enums erfordern Abdeckung (`AppPage.Playlists` wird über `AppRoutesTests` abgedeckt; neue String-Konstantenklassen sind keine Enums)

### `DevApiContractTests` / `DevApiHttpServerTests`
- Vertrag der Developer-API (`/model`-Snapshot-Felder)

### `VideoPlayer.E2E.Tests/Tests/*`
- `E2ETestBase` mit `ConnectToPairingRequiredAsync`, `PairAsync`, `LoginAsync`, `LoginOnHomeAsync` — gemeinsamer Setup-Fluss (ServerSetup → Pairing → Login → Home)
- `UiDriver.OpenFlyoutItem("…")` — Menü-Navigation über UIA

## Hilfsmethoden

### `MockVideoServer` (`VideoPlayer.E2E.Tests/Support/MockVideoServer.cs`)
- `IsListEndpoint` — Gate/Bearer-geschützte Listen-Endpunkte (leere Liste oder `ListOverride`)
- `WasPathRequested`, `RequestedPaths` — Anfrage-Diagnose
- Opt-in-Eigenschaften (`PlaybackMovieId` u. a.) konfigurieren Antworten

### `Stubs` (`VideoWebPlayer.Maui.Tests/Stubs/`)
- `HttpMessageHandler`-Stubs für `VideoPlayerApiClientTests_*`, `RepositoryRoot.Find()` für Quelltext-Vertragstests
