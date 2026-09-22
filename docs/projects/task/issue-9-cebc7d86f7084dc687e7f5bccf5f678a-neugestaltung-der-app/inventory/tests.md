# Tests — Bestand und Ausgangszustand

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-09-22, ca. 04:07–04:15 Uhr MESZ (UTC+02:00, Zeitzone „W. Europe Standard Time")
- Branch und Commit-ID: `task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app` @ `83ac4510ac5f64cf8b293e6744f10cc4c66a5cb8`
- Uncommittete Änderungen im getesteten Stand (`git status --porcelain`):
  - ` M .gitignore` (Ergänzung: `issue.md`)
  - `?? .agents/` (untracked, Agenten-Skill-Dateien)
  - `?? docs/projects/` (untracked, Projektdokumentation inkl. dieser Bestandsaufnahme)
  - Keine Änderungen an Produktivcode oder Tests.
- Testumgebung und Runtime-/SDK-Versionen:
  - Windows, .NET SDK `10.0.401`
  - Installierte Workloads: `android` 36.1.69/10.0.100, `ios` 26.5.10318/10.0.100, `maccatalyst` 26.5.10318/10.0.100, `maui-windows` 10.0.20/10.0.100 (SDK 10.0.400, VS 18.10.12210.168)
- Ermittelte Testsuiten und Quellen der Testbefehle:
  - Einzige Testsuite: `VideoWebPlayer.Maui.Tests` (xUnit v3, `net10.0`).
  - Testbefehl laut CI (`.github/workflows/pr-checks.yml`, Schritte „Restore" → „Build Client" → „Build Tests" → „Test"): `dotnet restore`, `dotnet build VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj --no-restore --configuration Release`, `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj --no-build --configuration Release`.
  - `README.md` nennt dieselben Befehle (`dotnet restore VideoPlayer.App.sln`, `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj`).
  - CI baut zusätzlich die MAUI-Ziele `net10.0-android` und `net10.0-windows10.0.19041.0` (kein Testlauf, nur Compile-Nachweis) sowie `dotnet format whitespace --verify-no-changes` als Lint.

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| 1 (Restore) | `dotnet restore` | Repo-Root | 0 | – | – | – | [01-dotnet-restore.log](test-results/01-dotnet-restore.log) |
| 2 (Build Client) | `dotnet build VideoWebPlayer.Client/VideoWebPlayer.Client.csproj --no-restore --configuration Release` | Repo-Root | 0 | – | – | – | [02-build-client.log](test-results/02-build-client.log) (10 Warnungen, 0 Fehler) |
| 3 (Build Tests) | `dotnet build VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj --no-restore --configuration Release` | Repo-Root | 0 | – | – | – | [03-build-tests.log](test-results/03-build-tests.log) (0 Warnungen, 0 Fehler) |
| 4 (Test) | `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj --no-build --configuration Release` | Repo-Root | 0 | 8 | 0 | 0 | [04-dotnet-test.log](test-results/04-dotnet-test.log) |
| 5 (Compile-Nachweis Windows) | `dotnet build VideoWebPlayer.Maui/VideoWebPlayer.Maui.csproj --configuration Release --framework net10.0-windows10.0.19041.0` | Repo-Root | 0 | – | – | – | [05-build-maui-windows.log](test-results/05-build-maui-windows.log) (92 Warnungen, 0 Fehler) |
| 6 (Compile-Nachweis Android) | `dotnet build VideoWebPlayer.Maui/VideoWebPlayer.Maui.csproj --no-restore --configuration Release --framework net10.0-android` | Repo-Root | 0 | – | – | – | [06-build-maui-android.log](test-results/06-build-maui-android.log) (49 Warnungen, 0 Fehler) |

### Nachgewiesene bestehende Testfehler

Keine. Im Ausgangslauf (Lauf 4) sind alle 8 Tests erfolgreich (0 fehlgeschlagen, 0 übersprungen).

### Testlücken und Ausführungsprobleme

- Keine fehlgeschlagenen, abgebrochenen oder übersprungenen Tests; keine Build-/Infrastrukturfehler bei den ausgeführten Läufen.
- Nicht ausgeführt: iOS-/MacCatalyst-Builds (auf Windows nicht möglich; CI nutzt `macos-latest`) und `dotnet format whitespace --verify-no-changes` (Lint-Gate, kein Test).
- Abdeckungslücke gegenüber der Anforderung: Es existieren keine Tests für Backup-Klassifikation, Reconnect-/Wiederherstellungs-Workflow, Offline-Sync der Fortschrittsdaten, Download-Verwaltung (Wiederaufnahme, Kontingent, Auto-Löschung) oder Serververwaltung/Erststart. Diese Bereiche existieren im Produktivcode teilweise gar nicht (s. [services.md](services.md)).
- Strukturelle Einschränkung: Das Testprojekt referenziert nur `VideoWebPlayer.Client`; Tests gegen `VideoWebPlayer.Maui` prüfen Quelltexte als Strings (Contract-Tests), nicht kompiliertes Verhalten.

## Testklassen

### `MauiSettingsContractTests`

Datei: `VideoWebPlayer.Maui.Tests/MauiSettingsContractTests.cs`

- `MauiAuthAndProfileConsumers_DoNotBypassSettingsService` — `[Theory]` mit 5 `[InlineData]` (Quelldateien `AuthService.cs`, `ConnectionService.cs`, `DownloadManager.cs`, `DownloadQueue.cs`, `LoginPage.xaml.cs`): Contract-Test, der per `File.ReadAllText` sicherstellt, dass diese Dateien `Preferences.Default.*`/`SecureStorage.Default.*` nicht direkt aufrufen.
- `SettingsService_StoresSensitiveValuesInProfileAwareSecureStorage` — `[Fact]`: prüft Quelltext von `SettingsService.cs` auf Keys `AuthToken`/`Passkey`, `GetPrefixedKey`-Nutzung und `SecureStorage`-Zugriff.
- Hilfsmethode `FindRepositoryRoot()` — lokalisiert den Repo-Root anhand von `VideoPlayer.App.sln` (ausgehend von `AppContext.BaseDirectory`).

### `ContinueWatchingEventIngressTests`

Datei: `VideoWebPlayer.Maui.Tests\ContinueWatchingEventIngressTests.cs`

- `ContinueWatchingDto_RoundTripsThroughJsonContract` — `[Fact]`: Serialisierungs-Roundtrip von `ContinueWatchingDto` (inkl. polymorphem `DtoMovie`-`Entry`).
- `MutationResult_UsesStableStatusAndMessageFields` — `[Fact]`: prüft `Status`/`Message` des Records `ContinueWatchingMutationResult`.

## Hilfsmethoden

- `FindRepositoryRoot()` in `MauiSettingsContractTests` — einzige vorhandene Testhilfsmethode; es gibt keine Test-Builder, Mocks oder Fixtures.
