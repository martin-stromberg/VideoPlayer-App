# Tests — Bestandsaufnahme und Ausgangszustand

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-09-22T04:56:28 +02:00 (W. Europe Standard Time)
- Branch und Commit-ID: `task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app-schritt-1-api-client` @ `c88bbf2d6c179146e7dbf82eb31991e4ffea4707`
- Uncommittete Änderungen im getesteten Stand: `.gitignore` (modifiziert, fremd zum Schritt), `.agents/` (untracked, fremd zum Schritt), `docs/features/` (Lifecycle-Artefakte dieses Laufs). Keine Änderungen an Produktivcode oder Tests.
- Testumgebung und Runtime-/SDK-Versionen: Windows, .NET SDK 10.0.401, Testlauf auf .NET 10.0.12 (xUnit.net VSTest Adapter 3.1.5, xUnit v3 3.2.2)
- Ermittelte Testsuiten und Quellen der Testbefehle: einzige Testsuite `VideoWebPlayer.Maui.Tests` (xUnit v3, `net10.0`); Befehl laut `README.md` und `pr-checks.yml`: `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj`

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| Baseline | `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj --logger "console;verbosity=normal"` | Repo-Stamm | 0 | 8 | 0 | 0 | [01-baseline-dotnet-test.log](test-results/01-baseline-dotnet-test.log) |

### Nachgewiesene bestehende Testfehler

Keine — alle 8 Tests bestanden im Baseline-Lauf.

### Testlücken und Ausführungsprobleme

- Das Testprojekt referenziert nur `VideoWebPlayer.Client`; MAUI-Code ist nur über Quelltext-Contract-Tests prüfbar (kein Build-/Referenzproblem, dokumentierte Einschränkung der Umgebung).
- Keine übersprungenen oder nicht ausführbaren Tests.

## Testklassen

### `MauiSettingsContractTests` (`VideoWebPlayer.Maui.Tests/MauiSettingsContractTests.cs`)

- `MauiAuthAndProfileConsumers_DoNotBypassSettingsService` (Theory, 5 Fälle) — prüft per Quelltextlektüre, dass MAUI-Services `Preferences.Default`/`SecureStorage.Default` nicht direkt umgehen.
- `SettingsService_StoresSensitiveValuesInProfileAwareSecureStorage` — Contract-Test auf Schlüsselaufrufe in `SettingsService.cs`.
- Hilfsmethode `FindRepositoryRoot()` — lokalisiert den Repo-Stamm über `VideoPlayer.App.sln`.

### `ContinueWatchingEventIngressTests` (`VideoWebPlayer.Maui.Tests/ContinueWatchingEventIngressTests.cs`)

- `ContinueWatchingDto_RoundTripsThroughJsonContract` — JSON-Roundtrip des Bestands-DTO `ContinueWatchingDto` aus `VideoWebPlayer.Client`.
- `MutationResult_UsesStableStatusAndMessageFields` — prüft `ContinueWatchingMutationResult`-Felder.

## Hilfsmethoden

- `MauiSettingsContractTests.FindRepositoryRoot()` — siehe oben; Muster für quelltextbasierte Contract-Tests wiederverwendbar.
- Für die neue Anforderung ist eine simulierte HTTP-Infrastruktur (Fake-`HttpMessageHandler`) als neue Testhilfe einzuplanen; im Bestand existiert keine.
