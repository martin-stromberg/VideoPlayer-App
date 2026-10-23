# Tests und Test-Ausgangszustand

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-09-22 08:22 (lokale Zeit, Windows)
- Branch und Commit-ID:
  `task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app-schritt-6-netzwerk-sync`
  @ `2e68f6f`
- Uncommittete Änderungen im getesteten Stand: `modified: .gitignore`
  (fremde Änderung — nicht Teil dieses Schritts), untracked `.agents/`,
  neu angelegte Lifecycle-Artefakte unter `docs/features/...` — alle ohne
  Einfluss auf den Testlauf.
- Testumgebung und Runtime-/SDK-Versionen: Windows, .NET SDK 10.0.401,
  xUnit v3 3.2.2, Microsoft.NET.Test.Sdk 18.8.1.
- Ermittelte Testsuiten: einzige Testsuite `VideoWebPlayer.Maui.Tests`
  (xUnit, `net10.0`); Testbefehl aus `README.md`. Keine weiteren
  Testprojekte, kein E2E-/UI-Test-Framework (kein FlaUI/Appium/Playwright)
  im Repository.

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| Baseline | `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj -c Release --logger "console;verbosity=normal"` | Repo-Root | 0 | 280 | 0 | 0 | [baseline-dotnet-test.log](test-results/baseline-dotnet-test.log) |

### Nachgewiesene bestehende Testfehler

Keine — alle 280 Tests bestanden im Ausgangslauf.

### Testlücken und Ausführungsprobleme

- Kein E2E-/UI-Automatisierungs-Framework im Repo; Netzwerkwechsel,
  Wiederverbindung und die Offline-Anzeige können nicht per
  UI-Automatisierung nachgewiesen werden. Als Funktionsnachweis dienen
  (a) Message-Sequenz-/Transitions-Tests über `AppUpdate`/`MvuHost`,
  (b) Service-Tests mit `StubHttpMessageHandler`, InMemory-Stores und
  realer SQLite-Datei im Temp-Pfad sowie (c) der Windows-Build der App.
- Das Testprojekt referenziert `VideoPlayer.ApiClient`, `VideoPlayer.Mvu`
  und `VideoPlayer.Storage`, aber **nicht** `VideoPlayer.Maui` —
  MAUI-abhängige Klassen (`Connectivity`, `HubConnection`,
  `ShellAppEffectHandler`) sind nicht laufzeittestbar; die Orchestrierung
  muss daher plattformneutral hinter Abstraktionen liegen
  (`INetworkStatusMonitor`, `IServerEventsConnection`).

## Testklassen (Bestand, relevant für diese Anforderung)

- `AppUpdateTests_Connection` / `AppUpdateTests_Startup` — Verzweigungen
  des Verbindungs-Workflows. **Betroffen:** `AutoLoginCompleted`/
  `LoginCompleted` (Erfolg) und `OfflineDataCheckCompleted` emittieren
  künftig zusätzliche Effekte (`ConnectServerEventsEffect`,
  `RunFullSyncEffect`) und navigieren nur noch von Workflow-Seiten aus —
  Erwartungen dieser Tests werden angepasst.
- `LocalDatabaseTests` — reale SQLite-Dateien über `AppStorageLayout` im
  Temp-Verzeichnis (`SqliteDatabase`-Collection wegen
  `ResetPool()`); Muster für Warteschlangen- und Migrationstests.
- `AuthSessionServiceTests`, `VideoPlayerApiClientTests_*` — Muster für
  `StubHttpMessageHandler`-basierte Service-Tests.
- `MvuHostTests` — serialisierte Dispatch-Verarbeitung.
- `VideoPlayerMauiContractTests`, `ConnectionWorkflowContractTests` —
  textuelle Contract-Prüfung von XAML/DI-Verdrahtung.
