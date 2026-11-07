# Tests und Test-Ausgangszustand

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-10-06 (Europe/Berlin, UTC+02:00)
- Branch und Commit-ID:
  `task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app-schritt-5-verbindung-anmeldung`
  @ `f531187`
- Uncommittete Änderungen im getesteten Stand: `modified: .gitignore`
  (fremde Änderung — nicht Teil dieses Schritts), untracked `.agents/`,
  neu angelegte Lifecycle-Artefakte unter `docs/features/...` — alle ohne
  Einfluss auf den Testlauf.
- Testumgebung und Runtime-/SDK-Versionen: Windows, .NET SDK 10.0.401,
  xUnit v3 3.2.2, Microsoft.NET.Test.Sdk 18.8.1.
- Ermittelte Testsuiten und Quellen der Testbefehle: einzige Testsuite ist
  `VideoWebPlayer.Maui.Tests` (xUnit, `net10.0`); Testbefehl aus `README.md`
  (`dotnet test VideoWebPlayer.Maui.Tests/...`). Keine weiteren Testprojekte,
  kein E2E-/UI-Test-Framework (kein FlaUI/Appium/Playwright) im Repository.

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| Baseline | `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj -c Release --logger "console;verbosity=normal"` | Repo-Root | 0 | 229 | 0 | 0 | [baseline-dotnet-test.log](test-results/baseline-dotnet-test.log) |

### Nachgewiesene bestehende Testfehler

Keine — alle 229 Tests bestanden im Ausgangslauf.

### Testlücken und Ausführungsprobleme

- Kein E2E-/UI-Automatisierungs-Framework im Repo vorhanden; der
  Start-Workflow und die Anmeldemaske können nicht per UI-Automatisierung
  abgedeckt werden. Als Funktionsnachweis der Benutzerflüsse dienen (a)
  Message-Sequenz-Tests über `AppUpdate`/`MvuHost` (vollständige
  Start-Workflow-Pfade auf Modellebene), (b) Contract-Tests auf
  `*.xaml`/`AppShell.xaml.cs`/`MauiProgram.cs`/`ShellAppEffectHandler.cs`
  (Bedienelemente, Route, DI-Verdrahtung) und (c) der Windows-Build der App.
- Das Testprojekt referenziert `VideoPlayer.ApiClient`, `VideoPlayer.Mvu` und
  `VideoPlayer.Storage`, aber nicht `VideoPlayer.Maui` — Laufzeit-Tests von
  MAUI-Klassen (`SecureStorage`, `Shell`) sind nicht möglich; die
  Anmeldeorchestrierung muss in einem der referenzierten, plattformneutralen
  Projekte liegen, um testbar zu sein.

## Testklassen (Bestand, relevant für diese Anforderung)

### `AppUpdateTests_Startup`
`StartupRequested` setzt `IsStartupRunning`/`Checking` und emittiert die drei
Start-Effekte; `ServersLoaded` mit/ohne aktive Adresse verzweigt zu
`CheckServerHealthEffect` bzw. `NavigateToPageEffect(ServerSetup)`.
**Betroffen:** `Update_HealthCheckCompleted_NavigatesToHome` — der
Health-Check-Abschluss navigiert künftig nicht mehr direkt nach `Home`,
sondern stößt die automatische Anmeldung bzw. die Offline-Daten-Prüfung an.

### `AppUpdateTests_ServerManagement`
Discovery-/Eingabe-/Wechsel-Pfade.
**Betroffen:** `Update_ServerCheckCompleted_Success_PersistsAndNavigatesHome` —
nach erfolgreicher Kandidatenprüfung folgt künftig die (Auto-)Anmeldung statt
der direkten Navigation nach `Home`.

### `AppUpdateTests_Navigation`
`NavigateToMessage`/`NavigatedMessage`; `Loading` wird abgelehnt.
**Betroffen:** neue Seite `Login` (keine Menü-Route).

### `MvuHostTests`
`DispatchAsync_UpdatesModelAndExecutesEffects` — Start-Sequenz endet aktuell
mit `NavigateToPageEffect(Home)` nach erfolgreichem Health-Check.
**Betroffen:** Effektliste ändert sich (Auto-Login-Effekt).

### `VideoPlayerApiClientTests_RetryPolicy`
Wiederholungsstrategie (5xx/408/429/Transport, 1 s/2 s/4 s, kein Retry bei
übrigen 4xx) — bereits vollständig abgedeckt, unverändert relevant für
Akzeptanzkriterium 2.

### `VideoPlayerApiClientTests_TokenRefresh` / `..._Authentication`
401 → einmalige Erneuerung über `TokenRefresher` + Neuversand; Login
überspringt Re-Auth. Bereits abgedeckt (Akzeptanzkriterium 3 auf
Client-Ebene); neu hinzukommt die produktive Verdrahtung des Refreshers mit
den gespeicherten Zugangsdaten (Service-Test).

### `AppSecretsServiceTests`
Geheimnis-Ablage (Username/Passkey/AuthToken), Legacy-Migration,
Profil-Isolation. Grundlage für Anmelde- und Wiederherstellungsszenarien.

### `LocalDatabaseTests`
Reale SQLite-Tests für `BackupDatabase`/`CacheDatabase` (Temp-Verzeichnis,
Collection `SqliteDatabase`). Grundlage für den „Offline-Daten"-Test.

### `ServerSetupContractTests` / `VideoPlayerMauiContractTests` / `MauiSettingsContractTests` / `SettingsStorageContractTests`
Contract-Tests auf Quelldateien (XAML-Bedienelemente, Route-Registrierung,
DI-Registrierungen, FlyoutItem-Anzahl = 6). **Betroffen:** neue `LoginPage`,
neue Route `login`, neue DI-Registrierungen; FlyoutItem-Anzahl bleibt 6.

## Hilfsmethoden (Bestand)

### `StubHttpMessageHandler` (`Stubs/`)
Queue-basierter simulierter Server (`Enqueue`, `EnqueueJson`,
`EnqueueException`), zeichnet `RecordedRequest`s auf (Methode, Pfad,
Authorization, ApiKey, Body).

### `RecordingDelays` / `ApiClientTestFactory` (`Stubs/`)
`RecordingDelays.Delay` zeichnet Backoff-Intervalle ohne zu warten;
`ApiClientTestFactory.CreateClient`/`CreateClientWithDelays` erstellt
`VideoPlayerApiClient` gegen den Stub-Handler (Basisadresse
`http://testserver/`, ApiKey `test-api-key`).

### `InMemorySecretStore` / `InMemorySettingsStore` (`Stubs/`)
In-Memory-Fakes der `ISecretStore`-/`ISettingsStore`-Ablagen für
`AppSecretsService`-Tests.

### `RepositoryRoot` (`Stubs/`)
Findet den Repo-Stamm für Contract-Tests.

### `SqliteDatabaseCollection` (`Stubs/`)
xUnit-Collection für serielle SQLite-Tests.
