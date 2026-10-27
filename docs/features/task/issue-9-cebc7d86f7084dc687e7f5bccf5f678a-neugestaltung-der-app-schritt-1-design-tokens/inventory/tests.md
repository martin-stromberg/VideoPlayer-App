# Inventur: Tests und Test-Ausgangszustand

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-09-22T21:13 (+02:00)
- Branch und Commit-ID: `task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app-schritt-1-design-tokens` @ `7159a68a3a1c57e59460402c2da8a475d34e44e3`
- Uncommittete Änderungen im getesteten Stand: nur untracked Arbeitsartefakte
  (`docs/features/task/issue-9-customer-feedback.md` — Fremddatei, und das
  Feature-Verzeichnis dieses Laufs); keine Änderungen an Produktivcode/Tests.
- Testumgebung und Runtime-/SDK-Versionen: Windows, .NET SDK 10.0.401,
  xunit.v3 3.2.2, Microsoft.NET.Test.Sdk 18.8.1.
- Ermittelte Testsuiten und Quellen der Testbefehle: einzige Testsuite des
  Repos ist `VideoWebPlayer.Maui.Tests` (net10.0, xunit). Testbefehl aus der
  DoD (`AGENTS.md` bzw. Auftrag): `dotnet test VideoWebPlayer.Maui.Tests`.
  Es existieren keine Rendering-/E2E-/Playwright-Tests im Repo (Konvention:
  MVU-Tests + quelltextbasierte Contract-Tests).

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| Baseline | `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj --logger "console;verbosity=normal"` | Repo-Root | 0 | 647 | 0 | 0 | [baseline-dotnet-test.txt](test-results/baseline-dotnet-test.txt) (`.txt`, da `*.log` in `.gitignore` liegt) |

### Nachgewiesene bestehende Testfehler

Keine — der Baseline-Lauf war vollständig erfolgreich (647/647 bestanden).

### Testlücken und Ausführungsprobleme

- Der Baseline-Lauf umfasste nur `VideoWebPlayer.Maui.Tests`. Der DoD-Build
  (`dotnet build VideoPlayer.App.sln`, Debug+Release) wurde im Rahmen der
  Bestandsaufnahme noch nicht ausgeführt; er ist Teil der Implementierungs-
  absicherung.
- Keine UI-Automatisierung: MAUI-Seiten können im Testprojekt nicht
  instanziiert werden; Contract-Tests prüfen Quelltext-Struktur
  (Muster `SourcesContractTests`, `VideoPlayerMauiContractTests` —
  `RepositoryRoot.Find()` + `XDocument`/String-Assert).

## Testklassen (bestehend, für diesen Schritt relevant)

### `VideoPlayerMauiContractTests` (`VideoPlayerMauiContractTests.cs`)
- `AppShell_IsFlyoutNavigation` / `AppShell_ContainsAllMenuEntries` /
  `AppShell_MenuEntries_BindToExistingPages` — Shell-Struktur (5 FlyoutItems).
- `NewApp_ProjectTargetsRequiredPlatforms` /
  `NewApp_DoesNotReferenceLegacyOrExternalMvuFrameworks` — csproj-Pins.

### `SourcesContractTests` (`SourcesContractTests.cs`)
- `AppShell_DefinesSourceSectionAndErrorTemplates` — pinnt die drei
  Templates `SourceSectionTemplate`/`SourceErrorTemplate`/`SourceInfoTemplate`
  in `AppShell.xaml` (deren Textfarben dieser Schritt umstellt).
- `AppShell_HasNoStaticSourcesFlyoutItem` — 5 FlyoutItems (XML-geparst).
- Weitere Pins zu `AppShell.xaml.cs` (Menüaufbau, FlyoutIsPresented, Dispatch).

### `PlayerContractTests` / `DownloadsContractTests` / `HomePageContractTests`
- Struktur-Pins der betroffenen Seiten (Elementnamen, Message-Dispatch);
  farbunabhängig, laufen nach der Design-Umstellung unverändert weiter —
  beim Umbau der XAML-Attribute müssen die gepinnten Bezeichner erhalten
  bleiben.

### `EnumCoverageTests`
- Spiegelt Enum-Werte (Hook `enum-coverage-check.py` prüft zusätzlich) —
  von diesem Schritt nicht betroffen (keine neuen Enums).

## Hilfsmethoden

### `Stubs.RepositoryRoot` (`Stubs/RepositoryRoot.cs`)
- `Find()` — findet das Repo-Wurzelverzeichnis über `VideoPlayer.App.sln`
  (Basis aller Contract-Tests, die Quelldateien lesen).
