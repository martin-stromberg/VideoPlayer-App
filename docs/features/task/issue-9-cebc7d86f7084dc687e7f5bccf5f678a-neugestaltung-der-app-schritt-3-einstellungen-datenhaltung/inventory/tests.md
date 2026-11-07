# Tests und Test-Ausgangszustand

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-09-22 06:03 (Europe/Berlin, UTC+02:00)
- Branch und Commit-ID: `task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app-schritt-3-einstellungen-datenhaltung` @ `8ff4e41`
- Uncommittete Änderungen im getesteten Stand: `modified: .gitignore` (fremde
  Änderung — nicht Teil dieses Schritts), untracked `.agents/` — beide ohne
  Einfluss auf den Testlauf.
- Testumgebung und Runtime-/SDK-Versionen: Windows, .NET SDK 10.0.401, xUnit v3
  3.2.2, Microsoft.NET.Test.Sdk 18.8.1.
- Ermittelte Testsuiten und Quellen der Testbefehle: einzige Testsuite ist
  `VideoWebPlayer.Maui.Tests` (xUnit, `net10.0`); Testbefehl aus `pr-checks.yml`
  und `README.md` (`dotnet test VideoWebPlayer.Maui.Tests/...`). Keine weiteren
  Testprojekte, kein E2E-/UI-Test-Framework (kein FlaUI/Appium/Playwright) im
  Repository.

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| Baseline | `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj -c Release --logger "console;verbosity=normal"` | Repo-Root | 0 | 101 | 0 | 0 | [baseline-dotnet-test.log](test-results/baseline-dotnet-test.log) |

### Nachgewiesene bestehende Testfehler

Keine — alle 101 Tests bestanden im Ausgangslauf.

### Testlücken und Ausführungsprobleme

- Kein E2E-/UI-Automatisierungs-Framework im Repo vorhanden; der Benutzerfluss
  „Einstellungsseite" kann nicht per UI-Automatisierung abgedeckt werden. Als
  Ersatz dienen (a) Unit-Tests der plattformneutralen Einstellungs-/
  Ablagebausteine, (b) Contract-Tests auf `SettingsPage.xaml` (Bedienelemente,
  Verdrahtung) sowie `MauiProgram.cs` und (c) Plattformnachweise über
  Quelltext-Prüfung (iOS-Backup-Ausschluss).
- Das Testprojekt referenziert das MAUI-Projekt nicht — Laufzeit-Tests von
  MAUI-Klassen (`Preferences`, `SecureStorage`, `FileSystem`) sind nicht möglich;
  plattformneutrale Abstraktionen müssen die Logik testbar machen.
- `sqlite-net-pcl`/`SQLitePCLRaw.bundle_green` sind `netstandard`-kompatibel und
  auf `net10.0` lauffähig — reale SQLite-Tests im Testprojekt sind möglich,
  sofern die Datenbankklassen plattformneutral in einem referenzierbaren
  Projekt liegen.

## Testklassen (Bestand)

### `AppUpdateTests_Startup` / `AppUpdateTests_Navigation`
Update-Funktion: Start-Workflow, Navigation (alle sechs Seiten), No-Op-Fälle.

### `MvuHostTests`
`DispatchAsync` wendet Update an, führt Effekte über `IAppEffectHandler` aus,
feuert `ModelChanged`/`PropertyChanged`.

### `AppRoutesTests`
`ToRoute`/`TryGetPage`-Roundtrip für alle Menüseiten.

### `VideoPlayerMauiContractTests`
Contract-Tests (Quelltext-/XML-Prüfung) auf `AppShell.xaml` (Flyout-Einträge,
Routen, Seitenbindung), `VideoPlayer.Maui.csproj` (TFMs, Referenzen, keine
Alt-/MVU-Framework-Referenzen) und Health-Check-Verdrahtung.

### `MauiSettingsContractTests`
Contract-Tests auf Alt-App-Quelltexten (kein `Preferences`/`SecureStorage`-
Direktzugriff in Auth-/Profil-Konsumenten; `SettingsService`-Aufbau).

### `ClientGateTests`, `ContinueWatchingEventIngressTests`, `MediaEntrySerializationTests`, `VideoPlayerApiClientTests_*`
API-Client und Alt-Client-DTOs — nicht von dieser Anforderung betroffen.

## Hilfsmethoden

### `RepositoryRoot.Find` (`Stubs/RepositoryRoot.cs`)
Läuft von `AppContext.BaseDirectory` hoch bis `VideoPlayer.App.sln` gefunden
wird — etabliertes Muster für Contract-Tests auf Repository-Dateien.

### `StubHttpMessageHandler` (`Stubs/StubHttpMessageHandler.cs`)
Simulierter HTTP-Server für Client-Tests — für diese Anforderung nicht relevant.
