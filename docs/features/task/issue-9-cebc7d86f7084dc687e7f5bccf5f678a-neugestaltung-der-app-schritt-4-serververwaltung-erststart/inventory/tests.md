# Tests und Test-Ausgangszustand

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-09-22 06:38 (Europe/Berlin, UTC+02:00)
- Branch und Commit-ID:
  `task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app-schritt-4-serververwaltung-erststart`
  @ `64cfe8e`
- Uncommittete Änderungen im getesteten Stand: `modified: .gitignore`
  (fremde Änderung — nicht Teil dieses Schritts), untracked `.agents/`,
  neu angelegte Lifecycle-Artefakte unter `docs/features/...` sowie
  `discovery-check.ps1` (Verifikationsskript) — alle ohne Einfluss auf den
  Testlauf.
- Testumgebung und Runtime-/SDK-Versionen: Windows, .NET SDK 10.0.401,
  xUnit v3 3.2.2, Microsoft.NET.Test.Sdk 18.8.1.
- Ermittelte Testsuiten und Quellen der Testbefehle: einzige Testsuite ist
  `VideoWebPlayer.Maui.Tests` (xUnit, `net10.0`); Testbefehl aus
  `README.md` bzw. Schritt-3-Inventory (`dotnet test
  VideoWebPlayer.Maui.Tests/...`). Keine weiteren Testprojekte, kein
  E2E-/UI-Test-Framework (kein FlaUI/Appium/Playwright) im Repository.

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| Baseline | `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj -c Release --logger "console;verbosity=normal"` | Repo-Root | 0 | 160 | 0 | 0 | [baseline-dotnet-test.log](test-results/baseline-dotnet-test.log) |

### Nachgewiesene bestehende Testfehler

Keine — alle 160 Tests bestanden im Ausgangslauf.

### Testlücken und Ausführungsprobleme

- Kein E2E-/UI-Automatisierungs-Framework im Repo vorhanden; die
  Benutzerflüsse „Erststart-Einrichtung" und „Server wechseln" können nicht
  per UI-Automatisierung abgedeckt werden. Als Ersatz dienen (a) Unit-Tests
  der plattformneutralen Bausteine (Serververwaltung, Discovery-Parsing,
  MVU-Übergänge), (b) Contract-Tests auf `*.xaml`/`MauiProgram.cs`/
  `ShellAppEffectHandler.cs` (Bedienelemente, Verdrahtung) und (c) der
  Windows-Build/Smoke-Run der App.
- Das Testprojekt referenziert das MAUI-Projekt nicht — Laufzeit-Tests von
  MAUI-Klassen (`Preferences`, `SecureStorage`, `FileSystem`, UDP/mDNS mit
  echten Sockets auf dem Gerät) sind nicht möglich; plattformneutrale
  Abstraktionen müssen die Logik testbar machen.
- Echte UDP-Sockets sind im Testprozess (`net10.0`, Windows) grundsätzlich
  lauffähig; für deterministische Tests ist dennoch eine Abstraktion der
  Discovery (Transport injizierbar) vorzusehen. Ein geräteübergreifender
  Live-Nachweis der Discovery wurde manuell geführt (siehe
  `discovery-verification.md`), ist aber nicht Teil der automatisierten
  Suite.

## Testklassen (Bestand, relevant für diese Anforderung)

### `AppUpdateTests_Startup`
`StartupRequested` setzt `IsStartupRunning`/`Checking` und emittiert die
drei Start-Effekte; `ServerHealthCheckCompleted` navigiert nach `Home`.
**Betroffen:** Der Start-Workflow erhält eine Verzweigung (kein Server →
Einrichtung) — `Update_HealthCheckCompleted_NavigatesToHome` und die
Effektliste des Starts müssen ggf. angepasst werden.

### `AppUpdateTests_Navigation`
`NavigateTo`/`Navigated` für alle Menüseiten, No-Op-Fälle.

### `MvuHostTests`
`DispatchAsync` wendet Update an, führt Effekte über `IAppEffectHandler`
aus, feuert `ModelChanged`/`PropertyChanged`.

### `AppRoutesTests`
`ToRoute`/`TryGetPage`-Roundtrip für alle Menüseiten.

### `VideoPlayerMauiContractTests`
Contract-Tests (Quelltext-/XML-Prüfung) auf `AppShell.xaml`
(Flyout-Einträge, Routen, Seitenbindung), `VideoPlayer.Maui.csproj`,
Health-Check-Verdrahtung. **Betroffen:** erwartet exakt sechs FlyoutItems —
ein Einrichtungsdialog darf **kein** FlyoutItem sein.

### `SettingsStorageContractTests`
Contract-Tests auf `SettingsPage.xaml` (AutomationIds, 4 Switches/4 numerische
Entries), `MauiProgram`-Registrierungen, `ShellAppEffectHandler`-Effekte,
`ServerAddressProvider` ohne `Preferences.Default`, kein direkter
Plattform-Zugriff außerhalb der Adapter. **Betroffen:** „Server wechseln"
erweitert die SettingsPage — Zählungen der Switches/Entries bleiben gültig,
solange kein weiterer Switch/Entry hinzukommt.

### `LocalDatabaseTests`
Reale SQLite-Tests: `BackupDatabase` legt `known_servers`/`backup_state` an,
Roundtrips beider Entitäten — Basis für Serververwaltungs-Tests.

### `AppSettingsServiceTests`, `AppSecretsServiceTests`, `AppProfileTests`, `AppStorageLayoutTests`, `AppSettingsTests`
Schritt-3-Abdeckung der Ablagebausteine — unverändert relevant
(Einstellungen müssen beim Serverwechsel erhalten bleiben).

## Hilfsmethoden

### `RepositoryRoot.Find` (`Stubs/RepositoryRoot.cs`)
Läuft von `AppContext.BaseDirectory` hoch bis `VideoPlayer.App.sln` gefunden
wird — etabliertes Muster für Contract-Tests auf Repository-Dateien.

### `InMemorySettingsStore` / `InMemorySecretStore` (`Stubs/`)
In-Memory-Fakes der Ablage-Abstraktionen — wiederverwendbar für
Serververwaltungs-Tests.

### `StubHttpMessageHandler` (`Stubs/StubHttpMessageHandler.cs`)
Simulierter HTTP-Server für `VideoPlayerApiClient`-Tests — wiederverwendbar
für Tests der Verbindungsprüfung gegen Kandidatenadressen.
