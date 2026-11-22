# E2E-Infrastruktur (`VideoPlayer.E2E.Tests`)

Alle Dateien unter `VideoPlayer.E2E.Tests/`; 19 Testklassen + abstrakte Basisklasse `E2ETestBase` unter `Tests/`, 7 Support-Dateien unter `Support/`, 1 Collection-Fixture. Die gesamte Suite ist umgebungssensitiv (echte Prozesse, FlaUI/UIA3, TCP-Listener, reales Dateisystem) und läuft sequentiell in der Collection `E2E` (`DisableParallelization = true`, `AppFixture.cs:13–14`).

## `AppFixture`

Datei: `VideoPlayer.E2E.Tests/AppFixture.cs`

| Mitglied | Sichtbarkeit | Kurzbeschreibung |
|----------|-------------|------------------|
| `E2ECollectionDefinition` | public | `[CollectionDefinition("E2E", DisableParallelization = true)]` + `ICollectionFixture<AppFixture>` |
| `AppFixture()` | public | Initialisiert `Lazy<Task<string>>` über `BuildAppAsync` |
| `EnsureBuiltAsync()` | public | Liefert den einmalig gebauten Exe-Pfad |
| `BuildAppAsync()` | private static | Startet `dotnet build VideoPlayer.Maui -f net10.0-windows10.0.19041.0 -c Debug` als Kindprozess; Abbruch-/Timeout-CTS **10 min**; bei Fehler `process.Kill(entireProcessTree: true)`; prüft die Exe unter `bin/Debug/.../win-x64/VideoPlayer.Maui.exe` |

Befund: Debug-Build ist Pflicht (Developer-API ist nur unter `DEBUG || DEV_API` kompiliert). Bereits gehärtet: Prozessbaum-Kill beim Build-Abbruch.

## `E2ETestBase`

Datei: `VideoPlayer.E2E.Tests/Tests/E2ETestBase.cs` — Basisklasse aller 19 Testklassen, `[Collection("E2E")]`, `IAsyncLifetime`.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `Ct` | protected static | `TestContext.Current.CancellationToken` |
| `InitializeAsync()` | public | No-op (`ValueTask.CompletedTask`) |
| `DisposeAsync()` | public | Wirft `AggregateException("Mindestens ein App-Prozess wurde unerwartet beendet.", appShutdownFailures)` — **Maskierungsregel bereits implementiert**: Cleanup-/Prozessfehler landen in der getrennten xUnit-Aufräumphase und überschreiben keinen Testfehler (Kommentar Zeilen 35–37) |
| `StartAppAsync(testName, profile)` | protected | `fixture.EnsureBuiltAsync()` + `AppInstance.StartAsync(..., reportShutdownFailure: appShutdownFailures.Enqueue)` |
| `ConnectToPairingRequiredAsync` | protected static | ServerSetup-Fluss; `WaitForModelAsync` Default 20 s + explizit **30 s** |
| `PairAsync` | protected static | Pairing-Code; `WaitForModelAsync` **30 s** |
| `LoginAsync` | protected static | Anmeldung per DevApi; `WaitForModelAsync` **40 s**; bei `TimeoutException` werden `server.LoginRequestBodies` angehängt (Diagnose) |
| `LoginOnHomeAsync` | protected static | Komplett-Setup; `WaitForModelAsync` **30 s** |
| `ContinueWatchingIndexAsync` | protected static | Indexsuche im Karussell-Tree |

## `AppInstance`

Datei: `VideoPlayer.E2E.Tests/Support/AppInstance.cs` (427 Zeilen) — `sealed`, `IDisposable`. Startet `VideoPlayer.Maui.exe` mit isoliertem Profil (`VIDEOPLAYER_PROFILE=e2e-<test>-<guid>`), entfernt geerbtes `VIDEOWEBPLAYER_MAUI_API_TOKEN`, wartet auf `devapi.json` (PID-abgeglichen), attacht UIA3.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `StartAsync(exePath, testName, startupTimeout, ct, profile, reportShutdownFailure)` | public static | `startupTimeout` Standard **45 s**; Prozessstart mit Redirect std*; bei Fehler `TryKillAndWait` + `WriteProcessDiagnostics` + `process.Dispose()` |
| `Dispose()` / `Dispose(bool deleteProfile)` | public | Shutdown-Kette: `Ui.Dispose()`/`DevApi.Dispose()` best-effort → `CloseMainWindow()` → `WaitForExit(10_000)` → sonst `TryKillAndWait` → `WriteProcessDiagnostics` → `process.Dispose()` → `TryDelete(devapi.json)` → `DeleteProfileDirectory()` → `automation.Dispose()`; aggregiert Fehler in `shutdownFailure` und meldet sie an `reportShutdownFailure` |
| `FindLogFile(fileName)` | public | Sucht `logs/videoplayer.log` über `ProfileNoBackupDirectories()` |
| `TryKillAndWait` | private static | `Kill(entireProcessTree: true)` + `WaitForExit(5_000)`; liefert `(RequestSucceeded, Error)` |
| `WriteProcessDiagnostics`/`…Core` | private static | Schreibt `.work/e2e-process-logs/<ts>-<pid>/{stdout,stderr}.log` + `process.json` (pid, profile, exitCode, killAttempted, killRequestSucceeded, killError, forcedKill, exitedBeforeShutdown, shutdownStartedAtUtc, outputDrained); `forcedKill` nur bei `exitCode == -1`; liefert `InvalidOperationException` bei unerwartetem Exit — **Diagnose-Ausgabe bereits vorhanden** |
| **`DeleteProfileDirectory`** | private | Löscht Profil-Verzeichnisse unter `%APPDATA%/VideoPlayer/<profil>`, `%LOCALAPPDATA%/VideoPlayer/<profil>`, `…\de.martinstromberg.videos\Data\nobackup\<profil>` und `…\Cache\<profil>`; `Directory.Delete(dir, recursive: true)` je Kandidat; **`catch (Exception) {}` ohne Retry/Backoff und ohne Protokollierung** (Zeilen 320–333) |
| `ProfileNoBackupDirectories`/`AppDataProfileDirectories` | private | Verzeichnissuche unter `%LOCALAPPDATA%` (Publisher-Abschnitt nicht hart kodiert) |
| `WaitForDevApiPortAsync` | private static | Pollt `devapi.json` alle **200 ms** bis `timeout` (aus `StartAsync`, Standard 45 s); bricht bei `process.HasExited` ab; `catch` auf halbgeschriebene Datei |
| `WaitForMainWindow` | private static | `Application.Attach` + `GetMainWindow(automation, 2 s)`-Schleife mit **eigenem 30-s-Budget** (Zeile 403) |
| `TryDelete` | private static | Stilles `File.Delete` (best effort, z. B. `devapi.json`) |

## `DevApiClient`

Datei: `VideoPlayer.E2E.Tests/Support/DevApiClient.cs` (165 Zeilen) — `sealed`, `IDisposable`; gemeinsamer `HttpClient` auf `http://127.0.0.1:{port}/` (Port aus `devapi.json`).

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `GetModelAsync` / `GetTreeAsync` | public | `GET /model` / `GET /tree`, JSON-Deserialize |
| `InvokeAsync(automationId, itemIndex)` | public | `POST /invoke` (auch `itemIndex` für Listenauswahl) |
| `LongPressAsync` | public | `POST /invoke` mit `gesture: "longpress"` |
| `SetTextAsync` | public | `POST /set-text` |
| `WaitForModelAsync(condition, timeout, description, ct)` | public | Pollt `GET /model` alle **250 ms**, Standard-Timeout **20 s**; toleriert `HttpRequestException`/`TaskCanceledException`/`JsonException` während Start/Kill; `TimeoutException` enthält Letztzustand (JSON) bzw. letzten Fehler — **Diagnose im Fehlertext vorhanden** |
| `Dispose` | public | `http.Dispose()` |

## `UiDriver`

Datei: `VideoPlayer.E2E.Tests/Support/UiDriver.cs` (677 Zeilen) — FlaUI/UIA3-Hilfen auf dem Hauptfenster; Poll-Intervall überall `Thread.Sleep(200)` (ClickByName-Debug + `WaitForNameAbsent` 100/200 ms, Flyout 300 ms).

| Methode | Zeitbudget (Standard) | Kurzbeschreibung |
|---------|----------------------|------------------|
| `FindByAutomationId` | **15 s** | Element per AutomationId, Retry bis Timeout |
| `Exists` | — | Einmalige Sichtbarkeitsprüfung (kein Warten) |
| `EnterText` | **15 s** | ValuePattern mit Rücklese-Retry; PasswordBox → `textBox.Enter` ohne Rücklesen |
| `ClickButton` | 15 s (über `FindByAutomationId`) | Invoke-Pattern |
| `Toggle` | 15 s | Toggle → Invoke → `element.Click()` Fallback |
| `OpenFlyoutItem` | **15 s** | Menüeintrag (ListItem/MenuItem), öffnet Flyout bei Bedarf |
| `WaitForVisible` | **15 s** | ScrollIntoView + Offscreen-Prüfung; `false` bei Timeout (Negativ-Behauptungstauglich) |
| `SelectComboBoxItem` / `GetComboBoxItemNames` | **15 s** | Picker (ComboBox) aufklappen/auswählen |
| `GoBack` | **15 s** | Back-Button; Fallback **Alt+Links** (echte Tastatureingabe) |
| `PressEnter` | 15 s | Fokus + RETURN-Taste |
| `ClickByName` | **15 s** (`initialWait` optional) | Fenster-Teilbaum, dann Desktop-Kinder des App-Prozesses (begrenzte Fremdbaumsuche, `FindInAppDesktopChildren`) |
| `WaitForNameAbsent` | **15 s** | Warten auf Schließen eines Aktionsmenüs |
| `ScrollToEnd` | Element 15 s, Scroll-Deadline **30 s** | ScrollPattern `LargeIncrement` bis ≥99 % oder 3× Stillstand; Fallback: **echte Mausrad-Scrolls** (40× `-3`, je 120 ms) |
| `GetVerticalScrollPercent` | 15 s | Scroll-Prozent oder `NaN` ohne Provider |
| `DumpTree` | — | UIA-Baum-Diagnose in Datei |
| `Dispose` | — | No-op (Automation/Prozess gehören `AppInstance`) |

## `MockVideoServer`

Datei: `VideoPlayer.E2E.Tests/Support/MockVideoServer.cs` (2569 Zeilen) — `sealed`, `IDisposable`; `TcpListener(IPAddress.Loopback, 0)` mit OS-Port, `Connection: close`, unbekannte Routen → 404 (nie 5xx). `Dispose`: `cancellation.Cancel()` + `listener.Stop()` + `acceptLoop.Wait(5 s)` best-effort. Umfangreiche Opt-in-Testdaten (Playlists, Quellen, Wiedergabe, saisonale Genres, Refresh-Rotation) und Diagnose-Zähler/Request-Protokolle (`RequestedPaths`, `LoginRequestBodies`, `ProgressReportBodies`, …).

## Weitere Support-Dateien

- `RepositoryRoot.cs` (27 Z.) — `Find()`: aufsteigende Suche nach `VideoPlayer.App.sln`.
- `MockPairingCrypto.cs` (64 Z.) — ECDH P-256 + AES-256-GCM, deckungsgleich mit `PairingCrypto` der App.
- `DevApiModels.cs` (374 Z.) — `DevApiModel`/`DevApiTreeNode` (Snapshot-Records, `FindByAutomationId`-Hilfe auf dem Tree).

## Deadline-Übersicht (Ist-Stand)

| Ebene | Budget | Ort |
|-------|--------|-----|
| App-Build (einmalig) | 10 min CTS | `AppFixture.BuildAppAsync` |
| App-Startup gesamt (`StartAsync`) | 45 s Standard | `AppInstance.StartAsync:80` |
| ↳ `devapi.json`-Poll | restliches 45-s-Budget, 200 ms Takt | `WaitForDevApiPortAsync` |
| ↳ Hauptfenster-Attach | **eigenes 30-s-Budget** (unabhängig vom verbrauchten Startup-Budget) | `WaitForMainWindow:403` |
| ↳ erstes Modell lesbar | erneut 45 s (volles `timeout`, kein Restbudget) | `StartAsync:113` |
| DevApi-Modellbedingung | 20 s Standard, 250 ms Takt | `DevApiClient.WaitForModelAsync:110` |
| UIA-Element/Interaktion | 15 s Standard | `UiDriver` (alle Methoden) |
| ScrollToEnd-Phase | 30 s Deadline + 3×-Stillstand | `UiDriver.ScrollToEnd` |
| Testdatei-Übersteuerungen | 5/10/15/20/30/40/45/60/120 s | div. `Tests/*.cs` |
| App-Shutdown | `CloseMainWindow` + `WaitForExit(10_000)` → `Kill` + `WaitForExit(5_000)` | `AppInstance.Dispose`/`TryKillAndWait` |
| stdout/stderr-Drain | `Task.WaitAll` 5 s | `WriteProcessDiagnosticsCore:240` |
| Mock-Server-Stop | `acceptLoop.Wait(5 s)` | `MockVideoServer.Dispose` |

**Befund zur Deadline-Staffelung:** Es existiert keine dokumentierte Staffelungskonvention. Auffälligkeiten des Ist-Standes: `WaitForMainWindow` (30 s) besitzt ein eigenes Budget innerhalb des 45-s-Startup-Budgets, kann also nach dem DevApi-Poll das Gesamtbudget überschreiten; `WaitForModelAsync` nach dem Start erhält erneut das volle `timeout` (kein Restbudget). Umgekehrt können Testdatei-Budgets (z. B. 120 s in `DownloadsTests`) größer als die 20-s-/15-s-Defaults der Helfer sein — Übersteuerung ist flächendeckend vorhanden, aber nicht einheitlich.

**Befund Cleanup:** Vorhandene Härtung: `Kill(entireProcessTree: true)`, Best-Effort-`Dispose`-Kette, Diagnose-Protokoll, `AggregateException`-Entkopplung der Aufräumphase. Fehlend: Retry/Backoff + Protokollierung in `DeleteProfileDirectory` (Restfehler werden kommentarlos geschluckt); `TryDelete` für `devapi.json` ebenfalls still.
