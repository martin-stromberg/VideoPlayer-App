# Testergebnisse

**Status:** Keine Fehler

## Ausgeführte Verifikationen

| Prüfung | Ergebnis |
|---------|----------|
| Build aller geänderten Projekte (`VideoPlayer.Storage`, `VideoPlayer.ApiClient`, `VideoPlayer.Mvu`, `VideoPlayer.Maui` Windows, `VideoWebPlayer.Maui.Tests`) | erfolgreich, 0 Fehler |
| `dotnet test VideoWebPlayer.Maui.Tests` (Release, mit Coverage-Sammlung) | 324 Tests: 324 bestanden, 0 fehlgeschlagen, 0 übersprungen |
| Code-Coverage (`--collect:"XPlat Code Coverage"`) | `VideoPlayer.Storage` 99,3 %, `VideoPlayer.Mvu` 94,6 %, `VideoPlayer.ApiClient` 84,6 % Zeilen (`VideoWebPlayer.Client` 7,3 %, Altbestand) |
| Windows-Build `VideoPlayer.Maui` (`net10.0-windows10.0.19041.0`, XAML-SourceGen) | erfolgreich, 0 Warnungen, 0 Fehler |
| iOS/MacCatalyst-/Android-Build | lokal auf Windows nicht ausgeführt — plattformneutraler Code (`VideoPlayer.Storage`, `VideoPlayer.ApiClient`, `VideoPlayer.Mvu`) ist vollständig net10.0-getestet; MAUI-spezifische Verdrahtung über Contract-Tests (`NetworkSyncContractTests`) |
| Windows-Smoke-Run / UI-E2E | nicht verfügbar — das Repository enthält kein UI-Automatisierungs-Framework und das Testprojekt kann `VideoPlayer.Maui` nicht referenzieren (siehe `inventory/tests.md`); Ersatz gemäß Plan: vollständige Message-Sequenz-Tests auf MVU-Ebene + Contract-Tests der Oberflächen-Verdrahtung |

Befehl (Arbeitsverzeichnis: Repository-Wurzel, Exit-Code 0):

```
dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj -c Release --collect:"XPlat Code Coverage" --logger "console;verbosity=normal"
```

Protokoll: `final-dotnet-test.log` (Coverage-Anlage:
`coverage.cobertura.xml` unter `VideoWebPlayer.Maui.Tests/TestResults/`).

## Baseline vs. Ergebnis

- Baseline (vor Implementierung, `inventory/test-results/baseline-dotnet-test.log`):
  280 Tests, 280 bestanden.
- Nach Implementierung: 324 Tests, 324 bestanden (44 neue Tests).

## Neue Tests (Auswahl)

- `AppUpdateTests_NetworkSync` — alle Übergänge des Netzwerk-/Sync-
  Workflows: Monitoring-Effekt beim Start, Offline-Übergang aus
  `Connected` (mit `DisconnectServerEventsEffect`, Seite bleibt),
  Online-Rückkehr aus `Offline` (`CheckServerHealthEffect`),
  Ereignis-Mapping auf die Teil-Sync-Effekte, Ignorieren außerhalb
  `Connected`, Reconnect nach `Closed`, `IsSyncRunning`-Rücksetzung,
  `SessionExpired` trennt die Ereignisverbindung.
- `PendingProgressSyncServiceTests` — Einreihen mit Coalescing pro
  Medium, chronologische Reihenfolge, `RemoveForMediaAsync`,
  Persistenz über Datenbank-Reopen (reale SQLite-Datei).
- `LocalDatabaseTests.BackupDatabase_MigratesDurationTicksOntoExisting
  Table` — altes Schema ohne `DurationTicks` wird migriert; Daten
  bleiben erhalten; doppeltes `InitializeAsync` ist idempotent.
- `ProgressReportServiceTests` — Direktversand inkl.
  Payload-Prüfung (`MediaType`/`MediaId`/`PositionSeconds`/
  `DurationSeconds`), Einreihung bei Offline/fehlendem
  Client/Transportfehler, Verwurf bei permanentem 4xx.
- `ServerSyncServiceTests` — Voll-Sync: Fortschritte vor den
  Metadaten, alle Cache-Tabellen befüllt, `SyncResult` korrekt;
  No-Op ohne Client; Abbruch bei Transportfehler ohne Löschung;
  permanenter 4xx verwirft nur den betroffenen Eintrag;
  Teilfehler (Favoriten 500) meldet `FavoritesSynced = false` ohne
  die übrigen Syncs zu beeinträchtigen.
- `ServerEventsServiceTests` — Hub-URL `{server}hubs/mediaupdate`,
  Bearer-Token über den Provider, idempotenter Connect, Reconnect
  bei Serverwechsel, Event-Übersetzung mit `SourceId`/`Count`,
  `Closed` → `ConnectionClosed` + Neuaufbau, Disconnect/Dispose.
- `NetworkSyncContractTests` — DI-Registrierungen, SignalR-
  Paketreferenz, Effekt-Zweige und Message-Rückdispatch im
  `ShellAppEffectHandler`, Event-Mapping der
  `SignalRServerEventsConnection`, Connectivity-Nutzung des Monitors.

## Angepasste Bestandstests

- `AppUpdateTests_Startup` — Start-Effektliste um
  `StartNetworkMonitoringEffect` erweitert (4 statt 3 Effekte).
- `AppUpdateTests_Connection` — `AutoLoginCompleted`-/`LoginCompleted`-
  Erfolg prüft `[ConnectServerEventsEffect, RunFullSyncEffect,
  NavigateToPageEffect]`; `SessionExpired` prüft
  `[DisconnectServerEventsEffect, NavigateToPageEffect]`.
- `MvuHostTests` — Effektkette des kompletten Start-Workflows von 6
  auf 9 Effekte angepasst.

## Iterationen

Iteration 1: Erster Testlauf zeigte einen Compile-Fehler in den neuen
Tests (`await using` auf `SQLiteAsyncConnection` — nicht
`IAsyncDisposable`) und einen veralteten Effekt-Count in
`MvuHostTests`; beides behoben. Zweiter Lauf: 324/324 grün.
Abschluss nach Iteration 1 — kein weiterer Schleifendurchlauf nötig.
