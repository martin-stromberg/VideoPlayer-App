# Bestand: Tests und CI

## Contract-Tests (`VideoPlayer.Maui.Tests`)

- `TvOsBrowsingContractTests.cs` (Vorbild): Quelltext-Assertions gegen
  die tvOS-Dateien (TV-Code kompiliert unter Windows nicht) — Seiten-
  Inhalt, Messages, Focus, Zustände, Verdrahtung (Composition/Navigator/
  Handler), Poster-Dienst.
- Weitere tvOS-Contract-Tests: `TvOsAppContractTests`,
  `TvOsConnectionContractTests`, `TvOsHomePageContractTests`,
  `TvOsCompileContractTests`, `TvOsProjectContractTests`.
- `RepositoryRoot.Find()` liefert den Repo-Pfad; Hilfsmethode
  `TvOsFile(params string[])` liest Quelldateien.

## CI (`pr-checks.yml`)

- `verify` (windows): Restore, Test-Build, MAUI-Windows-Build,
  `dotnet test`, `dotnet format whitespace --verify-no-changes`
  (Formatierungsdisziplin beachten).
- `build-tvos` (macos): siehe `tvos-rahmen.md`; Workflow-Pinning der
  tvOS-Workload bereits eingeführt.

## Sonstiges

- `dotnet test VideoPlayer.Maui.Tests` muss lokal (Windows) grün bleiben —
  die neuen Contract-Tests laufen dort mit.
