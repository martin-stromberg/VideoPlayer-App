# Tests — Bestand und Ausgangszustand

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-09-22 09:10–09:15 (UTC+2)
- Branch und Commit-ID: `task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app-schritt-7-startseite` @ `69012de4f4ac4f691313f6878fd5e6ee24fa08e8`
- Uncommittete Änderungen im getesteten Stand: `.gitignore` modifiziert
  (`issue.md` ergänzt — fremde Änderung, wird nicht committet),
  untracked `.agents/`; beide ohne Einfluss auf den Teststand.
- Testumgebung: Windows, .NET SDK 10.0.401, xUnit v3 (`xunit.v3` 3.2.2),
  `Microsoft.NET.Test.Sdk` 18.8.1.
- Ermittelte Testsuiten: `VideoWebPlayer.Maui.Tests` (einzige Suite,
  referenziert `VideoPlayer.ApiClient`, `VideoPlayer.Mvu`,
  `VideoPlayer.Storage`, `VideoWebPlayer.Client`).

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| Baseline | `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj -c Release --logger "console;verbosity=normal"` | Repo-Wurzel | 0 | 324 | 0 | 0 | [baseline-dotnet-test.log](test-results/baseline-dotnet-test.log) |

### Nachgewiesene bestehende Testfehler

Keine — alle 324 Tests bestanden.

### Testlücken und Ausführungsprobleme

- Kein UI-Automatisierungs-/E2E-Framework im Repository; das
  Testprojekt kann `VideoPlayer.Maui` (MAUI-Targets) nicht
  referenzieren. Benutzerfluss-Nachweise erfolgen daher — wie in den
  vorherigen Schritten — über MVU-Transitions-Tests (Message →
  Model + Effekte) und Contract-Tests der XAML-/DI-Verdrahtung.
- `VideoPlayer.Maui` ist nur per Windows-Build kompilierbar
  (`net10.0-windows10.0.19041.0`); iOS/MacCatalyst benötigen macOS.

## Testklassen (relevante Bestände)

### `AppUpdateTests_NetworkSync`
- `StartupRequested_IncludesMonitoringEffect` — Start-Effekte
- `ServerEventReceived_MapsToPartialSync` — Ereignis → Teil-Sync-Effekt
- `SynchronizationCompleted_ClearsSyncFlag` — Sync-Abschluss
- Offline/Online-Übergänge, Reconnect, SessionExpired

### `AppUpdateTests_Navigation`
- `Update_NavigateTo_SetsPageAndEmitsEffect` — Menü-Navigation
- `Update_NavigatedMessage_OnlyUpdatesModel` — Shell-Rückmeldung
- Zustands-Guards (`NeedsLogin`/`NeedsServerSetup`)

### `ServerSyncServiceTests` (`[Collection("SqliteDatabase")]`)
- Voll-Sync befüllt `cached_lists` (`sources`, `recent`,
  `continue_watching`) und `cached_favorites` — erzeugt reale
  Cache-Inhalte zum Wiederverwenden in neuen Lesetests
- Teil-Syncs, Fehlerfälle, `SyncResult`

### `LocalDatabaseTests` (`[Collection("SqliteDatabase")]`)
- `CacheDatabase`-Tabellen, `HasOfflineDataAsync`, Ablageorte

### `VideoPlayerMauiContractTests`
- `AppShell`-Menüstruktur, Projekt-Targets, Effekt-Handler-Verdrahtung
  (Quelltext-Prüfung)

### `MvuHostTests`, `ConnectionWorkflowContractTests`,
`NetworkSyncContractTests`
- Host-Zyklus; Quelltext-Contracts für Seiten/DI/Workflow-Verdrahtung

## Hilfsmethoden

### `Stubs/RepositoryRoot`
- `Find()` — liefert die Repo-Wurzel für Quelltext-Contract-Tests.

### `Stubs/StubHttpMessageHandler`
- `EnqueueJson(HttpStatusCode, object)` — simulierte Server-Antworten.

### `Stubs/InMemorySecretStore`, `Stubs/InMemorySettingsStore`
- In-Memory-Ablagen für `AppSecretsService`/`AppSettingsService`.

### `Stubs/SqliteDatabaseCollection`
- Serialisiert Testklassen mit realen SQLite-Datenbanken
  (`SQLiteAsyncConnection.ResetPool()` ist nicht parallel-sicher).
