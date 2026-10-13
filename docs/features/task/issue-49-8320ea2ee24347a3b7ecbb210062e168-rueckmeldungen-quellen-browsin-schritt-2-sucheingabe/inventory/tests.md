# Bestandsaufnahme — Tests

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-10-13, ~23:04–23:05 (UTC+2, lokale Windows-Zeit)
- Branch und Commit-ID: `task/issue-49-8320ea2ee24347a3b7ecbb210062e168-rueckmeldungen-quellen-browsin-schritt-2-sucheingabe`, HEAD `2a2a854e593056ebba407d810c1d686e65595177`
- Uncommittete Änderungen im getesteten Stand: nur die neu angelegten
  Arbeitsartefakte unter `docs/features/…` (kein Produktivcode)
- Testumgebung und Runtime-/SDK-Versionen: Windows, .NET SDK 10.0.401,
  Workloads: maui-windows 10.0.20, android, ios, maccatalyst
- Ermittelte Testsuiten und Quellen der Testbefehle:
  - Unit-/Contract-Tests: `VideoWebPlayer.Maui.Tests` (xunit.v3) —
    `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj`
  - FlaUI-E2E: `VideoPlayer.E2E.Tests` — baut die Debug-Windows-App und
    startet sie real (`AppFixture.EnsureBuiltAsync`); im Ausgangslauf
    **nicht ausgeführt** (Ausführbarkeit wird in Schritt 10 geprüft).

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| Baseline | `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj --logger "console;verbosity=normal"` | Repo-Root | 0 | 1316 | 0 | 0 | [baseline-dotnet-test.log](test-results/baseline-dotnet-test.log) |

### Nachgewiesene bestehende Testfehler

Keine — alle 1316 Tests des Ausgangslaufs bestanden.

### Testlücken und Ausführungsprobleme

- `VideoPlayer.E2E.Tests` im Ausgangslauf nicht ausgeführt: erfordert den
  Debug-Build der MAUI-Windows-App und einen laufenden UI-Automations-
  Zugriff (FlaUI). Die Infrastruktur existiert (`AppFixture`,
  `MockVideoServer`, `SourceBrowsingTests` als Vorlage). Keine Aussage
  über deren Stand kann getroffen werden.
- Die Kernfix-Logik liegt in `SourceOverviewPage.xaml.cs`
  (`VideoPlayer.Maui`) — MAUI-UI-Code ist im Testprojekt nicht
  instanziierbar; Verdrahtung wird projektüblich über Quelltext-
  Contract-Tests gepinnt (`PageLifecycleContractTests`-Muster).

## Testklassen

### `AppUpdateTests_Sources` (`VideoWebPlayer.Maui.Tests/AppUpdateTests_Sources.cs`)

- `SourceSelected_NavigatesAndStartsInitialLoad` — Navigation + initiales Laden (SearchText `null` im Effekt)
- `SourceBrowserOpened_SameSource_KeepsState` — Rückkehr auf gleiche Quelle ohne Zustandsreset
- `SourceBrowserOpened_Restore_LoadsGenresAndFirstPage` — Wiederherstellungspfad
- `SearchSubmitted_ReloadsFromFirstPage_WithSearchText` — Submit trimmt, setzt `SearchText`, `LoadSourceItemsEffect` ab Seite 0 mit Suchtext
- `SearchSubmitted_SameText_NoReload` — unveränderter Submit ohne Effekt
- `NextPageRequested_LoadsAppendedPage` / `…_NoMoreOrBusy_IsIgnored` — Nachladen
- `ItemsLoaded_Append_ConcatenatesAndAdvancesPage` u. a. — Eintragsübernahme
- (Hilfsmethoden `Connected()`, `Source()`, `Item()` lokal in der Klasse)

### `PageLifecycleContractTests` (`VideoWebPlayer.Maui.Tests/PageLifecycleContractTests.cs`)

- `SourceOverviewPage_UnsubscribesModelChangedOnDisappearing` — Abo-Muster per Quelltext gepinnt
- `SourceOverviewPage_IgnoresModelChangesAfterDisappearing` — `subscribed`-Guard gepinnt
- (identische Pins für die übrigen `ModelChanged`-Seiten)

### `DevApiModelSnapshotTests` (`VideoWebPlayer.Maui.Tests/DevApiModelSnapshotTests.cs`)

- Snapshot-Abbildung des `SourceBrowser`-Teils (u. a. `SourceBrowserSourceId`, `SourceBrowserGenreNames`)

### `SourceBrowserServiceTests` (`VideoWebPlayer.Maui.Tests/SourceBrowserServiceTests.cs`)

- `LoadItemsAsync`-URL-Aufbau: `search=mat` bzw. kein `search`-Parameter bei leerem Text

### `VideoPlayerMauiContractTests` (`VideoWebPlayer.Maui.Tests/VideoPlayerMauiContractTests.cs`)

- Quelltext-/XAML-Verdrahtungstests des Maui-Projekts (Shell-Menü, Routen u. a.)

## E2E-Vorlage

### `SourceBrowsingTests` (`VideoPlayer.E2E.Tests/Tests/SourceBrowsingTests.cs`)

- `GenreBar_VisibleAndFilters_ResetsOnAll` — Leiste sichtbar, Genre-Wahl lädt gefiltert ab Seite 1, „Alle" hebt auf
- `GenreBar_Hidden_WhenServerHasNoGenres` — leere Server-Genreliste ohne funktionslose Zeile
- Hilfsmethoden: `NewServer(bool)` (Quelle + 2 Einträge, `search`-fähiger `MockVideoServer`), `LoginToSourceAsync` (Pairing → Login → Quellmenü öffnen), `WaitForBarVisibilityAsync`

### Hilfsklassen (`VideoPlayer.E2E.Tests/Support`)

- `MockVideoServer` — `api/items` filtert per `search` (Titel-Teilstring, OrdinalIgnoreCase) und protokolliert `SourceItemRequests`
- `DevApiClient` — `WaitForModelAsync`, `GetTreeAsync` (`Text`-Knoten lesbar), `InvokeAsync`, `SetTextAsync`
- `UiDriver` — `EnterText`, `ClickButton`, `GoBack`, `FindByAutomationId`; FlaUI-Keyboard bereits genutzt (ALT+LEFT in `GoBack`)

## Hilfsmethoden

- `RepositoryRoot.Find()` (Test-Stubs) — Repo-Wurzel für Quelltext-Contract-Tests
