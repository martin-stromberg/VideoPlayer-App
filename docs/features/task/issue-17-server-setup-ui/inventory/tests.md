# Detail: Tests und Test-Ausgangszustand

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-09-24, ab 11:27 +02:00
- Branch und Commit-ID: `task/issue-17-server-setup-ui` @ `317fc4204ccc2607cbc1aaf1e2ca102b9e92d882`
- Uncommittete Änderungen im getesteten Stand: nur neue Lifecycle-Artefakte unter `docs/features/task/issue-17-server-setup-ui/` (keine Codeänderungen)
- Testumgebung: Windows 11 (Desktop, interaktive Session für UIA), .NET SDK 10.0.401, xUnit.net VSTest-Adapter 3.1.5; E2E läuft gegen `VideoPlayer.Maui.exe` (Debug, `net10.0-windows10.0.19041.0`, unverpackt) mit isoliertem Profil (`VIDEOPLAYER_PROFILE`) und `MockVideoServer` (Loopback).
- Ermittelte Testsuiten: `VideoWebPlayer.Maui.Tests` (Unit-/Contract-Tests, `dotnet test`), `VideoPlayer.E2E.Tests` (FlaUI + Dev-API, `dotnet test`; baut die App einmalig via `AppFixture`).

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| Baseline Unit/Contract | `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj --logger "console;verbosity=normal"` | Repo-Wurzel | 0 | 926 | 0 | 0 | [Log](test-results-baseline-maui-tests.txt) |
| Baseline E2E | `dotnet test VideoPlayer.E2E.Tests/VideoPlayer.E2E.Tests.csproj --logger "console;verbosity=normal"` | Repo-Wurzel | 0 | 10 | 0 | 0 | [Log](test-results-baseline-e2e.txt) |

### Nachgewiesene bestehende Testfehler

Keine — beide Baseline-Läufe waren fehlerfrei (Unit/Contract 926/926, E2E 10/10; E2E-Gesamtdauer ca. 16 min, davon überwiegend der einmalige Debug-Build der App via `AppFixture`).

### Testlücken und Ausführungsprobleme

- Die MAUI-Oberfläche kann im Unit-Testprojekt nicht instanziiert werden — UI-Verdrahtung wird über Contract-Tests (Quelltext-Pins) und E2E-Tests abgesichert.
- CollectionView-Einträge fehlen unter Windows im UIA-Baum; Auswahl läuft per Dev-API `POST /invoke` + `itemIndex`.

## Testklassen

### `ServerSetupContractTests` (`VideoWebPlayer.Maui.Tests/ServerSetupContractTests.cs`)
- `ServerSetupPage_ExistsWithManualEntryAndDiscovery` — Element-Namen der Seite.
- `ServerSetupPage_ControlsHaveAutomationIds` — AutomationId-Menge.
- `ServerSetupPage_CodeBehind_DispatchesMvuMessages` — Dispatch-Verdrahtung.
- `SettingsPage_ContainsDiscreetServerSwitchEntry` — „Server wechseln"-Eintrag.
- `AppShell_RegistersServerSetupRoute_WithoutFlyoutItem` — Route ohne Menüeintrag.
- `MauiProgram_RegistersServerManagementServices` — DI-Registrierungen.
- `EffectHandler_HandlesAllServerEffects` — Effekt-Behandlung.
- `Discovery_UsesDocumentedProtocol`, `KnownServersService_StoresInBackupDatabase` — Protokoll/Storage-Pins.

### `PairingContractTests` (`VideoWebPlayer.Maui.Tests/PairingContractTests.cs`)
- `ServerSetupPage_ContainsPairingSection` — Pairing-Elemente + `MaxLength`/„-stellig"-Verbote.
- `ServerSetupPage_PairingControlsHaveAutomationIds` — `PairingCodeEntry`/`PairButton`/`PairingErrorLabel`.
- `ServerSetupPage_PairButton_DispatchesPairingCodeSubmitted` — Dispatch + Model-Felder.
- Weitere Pins: Effekte, Gate-Probe, SecureStorage, iOS-Entitlements (von der UI-Änderung unberührt).

### `DesignTokensContractTests`
- Token-Werte, keine harten Farben, auflösbare `StaticResource`-Schlüssel, Radius-Konventionen, Dark-only.

### `ServerSetupTests` (`VideoPlayer.E2E.Tests/Tests/ServerSetupTests.cs`)
- `FreshStart_ShowsServerSetupPage` — Erststart landet auf Einrichtungsseite mit `AddressEntry`/`ConnectButton`.
- `InvalidAddress_ShowsValidationError` — Formatfehler inline.
- `UnreachableServer_ShowsError` — „nicht erreichbar"-Fehler, bleibt auf der Seite.
- `ReachableServer_RequiresPairing` — Pairing-Eingabe sichtbar (UIA + `/tree`).

### `PairingTests` — Formatfehler, falscher Code (401), erfolgreiches Pairing → Login.
### `ServerSwitchTests` — „Server wechseln"-Flow inkl. `KnownServersView`-Auswahl per `/invoke`+`itemIndex`.
### `LoginTests` — Anmeldeflows.

## Hilfsmethoden

### `E2ETestBase`
- `StartAppAsync` — Debug-Exe + isoliertes Profil.
- `ConnectToPairingRequiredAsync` — Setup-Fluss bis `IsPairingRequired`.
- `PairAsync` — Pairing mit gültigem Code bis `Login`.
- `LoginAsync` — Anmeldung bis `Home`/`Connected`.

### `UiDriver`
- `FindByAutomationId` (Retry), `Exists`, `EnterText` (Value-Pattern), `ClickButton` (Invoke-Pattern), `OpenFlyoutItem`, `FindByName`, `DumpTree`.

### `DevApiClient`
- `GetModelAsync`, `GetTreeAsync`, `InvokeAsync(automationId, itemIndex?)`, `SetTextAsync`, `WaitForModelAsync` (Poll + Timeout-Fehlerbild).

### `MockVideoServer`
- Loopback-TCP-Server: `api/health`, `api/pairing/exchange` (echtes ECDH/AES-GCM via `MockPairingCrypto`), `api/auth/login`, Listen-Endpunkte; `PairingCode`, `PairingExchangeCount`, `LoginRequestBodies` als Prüfpunkte.
