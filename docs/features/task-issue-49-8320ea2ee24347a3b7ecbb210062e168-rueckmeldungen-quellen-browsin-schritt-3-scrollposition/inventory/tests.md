# Tests — Ausgangszustand und Bestand

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-10-13 ~23:45 (Ortszeit, UTC+2 Sommerzeit)
- Branch und Commit-ID: `task/issue-49-8320ea2ee24347a3b7ecbb210062e168-rueckmeldungen-quellen-browsin-schritt-3-scrollposition` @ `c08bb5189cb35a2e7ed55e816c137b6ac0f882e6`
- Uncommittete Änderungen im getesteten Stand: nur die neuen Arbeitsartefakte
  unter `docs/features/{branchname}/` (keine Quellcode-Änderungen); untracked
  Fremddatei `nul` im Repo-Stamm (vorhanden, nicht angefasst)
- Testumgebung und Runtime-/SDK-Versionen: .NET SDK 10.0.401, Windows, xUnit
- Ermittelte Testsuiten und Quellen der Testbefehle:
  - `VideoWebPlayer.Maui.Tests` (Unit-Tests; referenziert u. a. `VideoPlayer.Mvu`)
  - `VideoPlayer.E2E.Tests` (FlaUI-E2E gegen echte Windows-App; baut
    `VideoPlayer.Maui` Debug, startet sie, steuert via DevApi + UIA)

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| Baseline Unit | `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj --logger "console;verbosity=normal"` | Repo-Stamm | 0 | 1320 | 0 | 0 (keine gemeldet) | [baseline-unittests.log](test-results/baseline-unittests.log) |

### Nachgewiesene bestehende Testfehler

Keine — der Baseline-Lauf ist vollständig grün (1320/1320).

### Testlücken und Ausführungsprobleme

- `VideoPlayer.E2E.Tests` wurde im Baseline-Lauf **nicht** ausgeführt (baut und
  startet die echte MAUI-Windows-App; Laufzeit hoch). Ausführbarkeit wird im
  Testschritt des Lifecycle geprüft; kein Fehlverhalten bekannt.
- Build der gesamten Solution (`dotnet build`) wurde im Baseline nicht separat
  protokolliert — `dotnet test` baute die Testprojekt-Kette fehlerfrei.

## Testklassen

### `AppUpdateTests_Sources` (`VideoWebPlayer.Maui.Tests/AppUpdateTests_Sources.cs`)

- `SourceSelected_NavigatesAndStartsInitialLoad` — Navigation + Erstladung (`Append: false`, `Page = 0`)
- `SourceBrowserOpened_SameSource_KeepsState` — Rückkehr ohne Reload
- `SourceBrowserOpened_Restore_LoadsGenresAndFirstPage` — Wiederherstellungspfad
- `SourceGenresLoaded_WrongSource_IsDiscarded`, `SourceGenresLoaded_OnlyAllEntry_HidesGenreFilter`, `SourceGenresLoaded_ServerGenres_ShowsGenreFilter`
- `GenreSelected_ReloadsFromFirstPage_WithGenreFilter` — Reset + `Append: false`
- `SearchSubmitted_ReloadsFromFirstPage_WithSearchText`, `SearchSubmitted_SameText_NoReload`
- `NextPageRequested_LoadsAppendedPage` — `Append: true`, `IsLoadingMore`
- `NextPageRequested_NoMoreOrBusy_IsIgnored`
- `ItemsLoaded_Append_ConcatenatesAndAdvancesPage` — Append-Verkettung im Modell
- `ItemsLoaded_StaleAppend_IsDiscarded`, `ItemsLoaded_AppendError_KeepsExistingItems`
- Retry-Tests (`RetryRequested`) — Neuversuch Folgeseite vs. Neulauf

### `DownloadListSyncTests` (`VideoWebPlayer.Maui.Tests/DownloadListSyncTests.cs`)

Vorlage für den geplanten Sync-Helper: prüft u. a., dass Updates ohne
`CollectionChanged` in-place laufen (`Assert.Empty(collectionEvents)`) —
direkt übertragbarer Nachweis „kein Listenreset beim Anhängen".

### `DevApiModelSnapshotTests`, `SourcesContractTests`

Vertragstests des DevApi-Snapshots bzw. der Quellen-Endpunkte.

## E2E-Infrastruktur (`VideoPlayer.E2E.Tests`)

- `AppFixture` — baut `VideoPlayer.Maui` (Debug, Windows-TFM) einmalig je Lauf
- `AppInstance`, `UiDriver` (FlaUI/UIA3), `DevApiClient` (`/model`, `/tree`,
  `/invoke`, `/set-text`), `MockVideoServer` (paginiert `api/items` über
  `page`/`size`, `search`, `genreId`; protokolliert `SourceItemRequests`)
- `SourceBrowsingTests` — Genre-Leiste/Filter (Schritt 1)
- `SourceSearchTests` — Sucheingabe/Submit (Schritt 2)

## Hilfsmethoden

- `MockVideoServer.NewServer`-Vorlagen in den Source-Testklassen (Quelle +
  Einträge + Genres); `LoginToSourceAsync` (Setup-Fluss bis geöffneter Quelle)
- `UiDriver.WaitForVisible` rollt Elemente per `ScrollItemPattern` sichtbar
