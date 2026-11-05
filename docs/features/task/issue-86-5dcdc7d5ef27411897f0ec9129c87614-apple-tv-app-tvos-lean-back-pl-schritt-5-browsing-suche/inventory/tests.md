# Tests — Bestandsaufnahme

## Testklassen

### `TvOsHomePageContractTests`
Datei: `VideoPlayer.Maui.Tests/TvOsHomePageContractTests.cs` (193 Z.) — Quell-Contract-Tests gegen `VideoPlayer.TvOS`-Dateien (tvOS-Code kompiliert unter Windows nicht; `RepositoryRoot.Find()` + `File.ReadAllText`, `Assert.Contains`). Prüft u. a. `IModelRenderable`-Verdrahtung, Karussell-Bestandteile, `RegisterClassForCell`, `PresentPreparedTarget`-Wiring, Abwesenheit von Mutations-Effekten.

### `TvOsConnectionContractTests`
Datei: `VideoPlayer.Maui.Tests/TvOsConnectionContractTests.cs` — gleiches Muster für Verbindungsseiten (7 Tests).

### `TvOsCompileContractTests`
Datei: `VideoPlayer.Maui.Tests/TvOsCompileContractTests.cs` — Regex-Prüfungen über alle tvOS-Quelldateien:
- Keine `UIStackView`-Member (`Spacing`, `AddArrangedSubview`) auf `UIView`-deklarierten Variablen.
- Kein `MenuPages.IndexOf` (`IReadOnlyList` hat kein `IndexOf`).
- Named-Arguments müssen case-sensitiv zu positionalen Record-Parametern passen (CS1739-Schutz, Fund `isContinueWatching:`).

### `TvOsAppContractTests` / `TvOsProjectContractTests`
Dateien: `VideoPlayer.Maui.Tests/` — App-Grundgerüst-/Projekt-Contracts.

## Hilfsmethoden

### `RepositoryRoot` (`VideoPlayer.Maui.Tests/Stubs`)
- `Find()` — lokalisiert den Repository-Stamm für Quell-Contract-Tests.

## Bekannte tvOS-Kompilierfallen (aus den Vorschritten)

- `IReadOnlyList<T>.IndexOf` existiert nicht → eigene Schleife.
- `const nfloat` unmöglich (Struct) → `static readonly`.
- `RegisterClassForCell` (nicht `RegisterClassForCells`).
- `ReuseId` als `const string` (implizit `NSString`-konvertierbar).
- Named-Arguments case-sensitiv (`Append:`, `IsContinueWatching:`).
- UICollectionViewSource-Overrides: `GetItemsCount`, `GetCell`, `ItemSelected`, `WillDisplayCell`.
- `PressesBegan(NSSet<UIPress>, UIPressesEvent)` + `presses.ToArray<UIPress>()` + `UIPressType.Menu` für die Menü-Taste.

## Testlauf

- `dotnet test VideoPlayer.Maui.Tests` — lokale Suite (Windows), zuletzt 1472/1472 grün.
- tvOS-Kompiliernachweis: CI-Job `build-tvos` (Workflow `PR Checks`, macos-latest, Push-Trigger `task/issue-86-**`).
