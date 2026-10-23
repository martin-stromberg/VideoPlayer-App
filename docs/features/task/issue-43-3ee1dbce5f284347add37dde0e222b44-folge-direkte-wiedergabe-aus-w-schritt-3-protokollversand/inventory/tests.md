# Bestandsaufnahme: Tests

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-10-11, lokale Zeit (UTC+2)
- Branch und Commit-ID: `task/issue-43-3ee1dbce5f284347add37dde0e222b44-folge-direkte-wiedergabe-aus-w-schritt-3-protokollversand`, Commit `a841a75ed6baaa94b4e45704c57bfcbb6130864b`
- Uncommittete Änderungen im getesteten Stand: nur neue Lifecycle-Artefakte unter `docs/features/…schritt-3-protokollversand/` (kein Produktivcode)
- Testumgebung und Runtime-/SDK-Versionen: `dotnet` SDK 10.0.401; Workloads maui-windows/android/ios/maccatalyst installiert; xUnit v3; E2E = FlaUI UIA3 auf `net10.0-windows`, Debug-App-Build `net10.0-windows10.0.19041.0` (unpackaged)
- Ermittelte Testsuiten: `VideoWebPlayer.Maui.Tests` (Unit, `net10.0`), `VideoPlayer.E2E.Tests` (`net10.0-windows`, FlaUI + `MockVideoServer` + Developer-API)

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| Baseline Unit | `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj --nologo -v q` | Repo-Stamm | 0 | 1104 | 0 | 0 | [baseline-unittests.txt](test-results/baseline-unittests.txt) |

### Nachgewiesene bestehende Testfehler

Keine — der Baseline-Lauf der Unit-Testsuite war vollständig grün. Die E2E-Suite wurde in der Bestandsaufnahme nicht ausgeführt (siehe Testlücken); daraus folgt keine Aussage über ihren Ausgangszustand.

### Testlücken und Ausführungsprobleme

- **E2E-Baseline nicht ausgeführt:** Der komplette E2E-Lauf (Debug-Build der MAUI-App + 25+ FlaUI-Szenarien) dauert erfahrungsgemäß mehrere Minuten bis zu einer Stunde (im Vorgänger-Schritt lief ein Baseline-Lauf >45 min ohne Ergebnis). Die E2E-Suite wird nach der Implementierung vollständig ausgeführt; deren Ergebnis gilt dann als Nachweis des Ist-Standes.

## Testklassen (relevant für diese Anforderung)

### `AppSettingsTests`
- `Defaults_AreExpected` — Standardwerte aller Einstellungen (hier um `LoggingEnabled` zu ergänzen)
- `Normalize_*` — Clamp-Tests (nicht betroffen; `LoggingEnabled` ist boolesch)
- `Normalize_LeavesBooleansUntouched` — boolesche Werte bleiben unverändert

### `AppSettingsServiceTests`
- `Save_ThenLoad_RoundTrips` — Roundtrip aller Werte
- `Load_WhenStoreEmpty_ReturnsDefaults`, `Load_ClampsOutOfRangeStoredValues`, `Profiles_DoNotInterfere`, `UsesProfilePrefixedKeys`

### `AppStorageLayoutTests`
- `EnsureDirectoriesExist_CreatesAll` — prüft das Anlegen der Layout-Verzeichnisse (um `LogsDirectory` zu ergänzen)
- `SeparatesBackupAndNoBackupRoots`, `Profiles_GetIsolatedDirectories`

### `AppUpdateTests_Settings`
- `Update_Startup_EmitsStorageAndSettingsEffects`, `Update_LoadSettingsRequested_EmitsLoadEffect`, `Update_SettingsLoaded_SetsModelSettings`, `Update_SaveSettings_NormalizesAndPersists`

### `EnumCoverageTests`
- Erzwingt Testabdeckung je Enum-Wert — kein neuer Enum geplant, keine Anpassung nötig.

### Contract-Tests (Quelltext-Pins der nicht referenzierbaren MAUI-Projekte)
- Konvention: `ContinueWatchingContextMenuContractTests`, `MauiSettingsContractTests`, `VideoPlayerMauiContractTests` lesen Quelldateien via `RepositoryRoot.Find()` und pinnen Verdrahtung/Texte.

### Hilfsmethoden
- `Stubs/InMemorySettingsStore` — `ISettingsStore`-Fake für `AppSettingsService`-Tests
- `Stubs/RepositoryRoot` — Repo-Stamm-Ermittlung der Contract-Tests
- E2E: `E2ETestBase.StartAppAsync` (isoliertes Profil), `AppInstance.Profile`, `UiDriver`, `DevApiClient.WaitForModelAsync`
