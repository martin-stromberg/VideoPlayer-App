# Bestandsaufnahme: Staging Branch

## Projektstruktur

- `VideoWebPlayer.App.sln` – Lösungsdatei
- `VideoWebPlayer.Client/` – .NET 10 Bibliothek
- `VideoWebPlayer.Maui/` – .NET MAUI App mit Zielen:
  - `net10.0-android`
  - `net10.0-ios`
  - `net10.0-maccatalyst`
  - `net10.0-windows10.0.19041.0`
- `VideoWebPlayer.Maui.Tests/` – xUnit Testprojekt

## Versionsverwaltung

- Git-Repository, aktuell ohne `.github/`-Ordner.
- Keine CI/CD-Workflows vorhanden.
- Keine Branch-Protection-Regeln in Dateien konfigurierbar.

## Build- und Testwerkzeuge

- `dotnet build` und `dotnet test` verfügbar.
- `dotnet format` für Formatierungsprüfungen verfügbar.
- Semantic Release ist ein npm-Toolset; für .NET-Projekte muss die Version in `*.csproj` über ein exec-Plugin angepasst werden.

## Offene Punkte

- Keine existierenden Release-Workflows.
- Keine automatische Versionsverwaltung.
