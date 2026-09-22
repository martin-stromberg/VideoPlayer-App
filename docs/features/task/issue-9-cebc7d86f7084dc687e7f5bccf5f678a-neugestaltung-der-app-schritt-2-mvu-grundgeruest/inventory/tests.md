# Tests und Test-Ausgangszustand

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-09-22 05:29 (Europe/Berlin, UTC+02:00)
- Branch und Commit-ID: `task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app-schritt-2-mvu-grundgeruest` @ `b9094b7`
- Uncommittete Änderungen im getesteten Stand: `modified: .gitignore` (fremde Änderung,
  `issue.md` ergänzt — nicht Teil dieses Schritts), untracked `.agents/` — beide ohne
  Einfluss auf den Testlauf.
- Testumgebung und Runtime-/SDK-Versionen: Windows, .NET SDK 10.0.401, xUnit v3 3.2.2,
  Microsoft.NET.Test.Sdk 18.8.1.
- Ermittelte Testsuiten und Quellen der Testbefehle: einzige Testsuite ist
  `VideoWebPlayer.Maui.Tests` (xUnit); Testbefehl aus `pr-checks.yml` und `README.md`
  (`dotnet test VideoWebPlayer.Maui.Tests/...`). Keine weiteren Testprojekte,
  kein E2E-/UI-Test-Framework (kein FlaUI/Appium/Playwright) im Repository.

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| Baseline | `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj -c Release --logger "console;verbosity=normal"` | Repo-Root | 0 | 69 | 0 | 0 | [baseline-dotnet-test.log](test-results/baseline-dotnet-test.log) |

### Nachgewiesene bestehende Testfehler

Keine — alle 69 Tests bestanden im Ausgangslauf.

### Testlücken und Ausführungsprobleme

- Kein E2E-/UI-Automatisierungs-Framework im Repo vorhanden; Benutzerflüsse der neuen App
  können daher nicht per UI-Automatisierung abgedeckt werden. Als Ersatz dienen
  (a) Unit-Tests des MVU-Zyklus (falls plattformneutral gehalten) und (b) Contract-Tests
  auf `AppShell.xaml` (Flyout-Einträge/Routen) sowie (c) ein dokumentierter
  Windows-Start-Smoke-Test der gebauten App.
- Das Testprojekt referenziert das MAUI-Projekt nicht — reine Compile-/Laufzeit-Tests von
  MAUI-Klassen sind nicht möglich.

## Testklassen (Bestand)

### `ClientGateTests` (`ClientGateTests.cs`)
- Auflösungsreihenfolge des API-Schlüssels (Umgebungsvariable/Metadata/Platzhalter).

### `ContinueWatchingEventIngressTests` (`ContinueWatchingEventIngressTests.cs`)
- Deserialisierung von Server-Ereignissen (Weiterschauen).

### `MauiSettingsContractTests` (`MauiSettingsContractTests.cs`)
- Contract-Tests auf Alt-App-Quelltexten (`File.ReadAllText` + `FindRepositoryRoot()`):
  keine direkten `Preferences`/`SecureStorage`-Zugriffe in Auth-/Profil-Konsumenten;
  `SettingsService`-Aufbau. **Muster, das für `AppShell.xaml`-Contract-Tests der neuen
  App wiederverwendet werden kann.**

### `MediaEntrySerializationTests` (`MediaEntrySerializationTests.cs`)
- Serialisierung/Deserialisierung der Alt-Client-DTOs (`VideoWebPlayer.Client`).

### `VideoPlayerApiClientTests_*` (4 Dateien)
- `Authentication`, `Endpoints`, `RetryPolicy`, `TokenRefresh` — Verhalten des neuen
  `VideoPlayerApiClient` gegen `StubHttpMessageHandler`.

## Hilfsmethoden

### `StubHttpMessageHandler` (`Stubs/StubHttpMessageHandler.cs`)
- Simulierter Server: Antwort-Queue/Responder-Delegate, zeichnet `RecordedRequest`s auf
  (Methode, Pfad, Authorization, ApiKey, Body). Wiederverwendbar für neue Client-Tests.

### `FindRepositoryRoot` (in `MauiSettingsContractTests.cs`)
- Läuft von `AppContext.BaseDirectory` hoch bis `VideoPlayer.App.sln` gefunden wird —
  etabliertes Muster für Contract-Tests auf Repository-Dateien.
