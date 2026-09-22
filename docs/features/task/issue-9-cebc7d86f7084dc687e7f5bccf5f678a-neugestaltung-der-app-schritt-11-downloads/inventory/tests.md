# Bestandsaufnahme — Test-Ausgangszustand

## Ausgangslauf (Baseline vor der Umsetzung)

- Datum/Uhrzeit: 2026-08-26 (UTC+2, lokale Zeit), Lauf direkt vor den
  ersten Codeänderungen des Schritts.
- Branch: `task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app-schritt-11-downloads`
- Commit: `129be02` (`chore: Entwicklungsschritt 11 in Arbeit`)
- Uncommittete Änderungen zum Laufzeitpunkt: `.gitignore`
  (fremde Änderung, wird nicht committed) sowie die neuen
  Planungsartefakte unter `docs/features/…-schritt-11-downloads/`
  (keine Produktivcode-Änderung).
- SDK: .NET 10.0.401; Testprojekt `VideoWebPlayer.Maui.Tests`
  (xUnit v3, `net10.0`).

Ausgeführter Befehl (Arbeitsverzeichnis: Repository-Wurzel):

```text
dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj
```

Ergebniszeile:

```text
Bestanden!   : Fehler:     0, erfolgreich:   510, übersprungen:     0, gesamt:   510, Dauer: 3 s - VideoWebPlayer.Maui.Tests.dll (net10.0)
```

- Exit-Code: 0
- Nachweis: `inventory/test-results/baseline-dotnet-test.log`
- Fehlgeschlagene Tests: keine. Übersprungene: keine.
- Nicht ausführbar: UI-E2E der MAUI-Seiten (keine UI-Automatisierung im
  Repository — dokumentierte Einschränkung; MAUI-Schicht wird über
  Contract-Tests der Quelldateien abgedeckt, Muster
  `PlayerContractTests`).

## Bestehende, für diese Anforderung relevante Tests

- `AppUpdateTests_*.cs` — MVU-Übergänge je Bereich (hier folgt
  `AppUpdateTests_Downloads.cs`).
- `PlaybackSourceServiceTests.cs` — Manifest-Lesepfad
  (`completed` + Datei → `LocalFile`); bleibt unverändert gültig.
- `EnumCoverageTests.cs` — Deckt neue Enums ab (Hook-Vorgabe:
  `Enum.GetValues`/`nameof` in Tests).
- `MauiSettingsContractTests.cs` — Einstellungs-Contract (Schlüssel,
  Defaults); Quota/Auto-Delete/Auto-Download/WLAN-only bereits belegt.
- `LocalDatabaseTests.cs`, `SqliteDatabaseCollection` (Stubs) —
  SQLite-Testinfrastruktur für die Cache-Datenbank (Manifest).
- `Stubs/StubHttpMessageHandler.cs` — HTTP-Stub für Download-Tests.
- `Stubs/InMemorySettingsStore.cs`, `InMemorySecretStore.cs` —
  Einstellungs-/Geheimnis-Stubs.
