# Detail: E2E-Tests mit FlaUI (Schritt 3)

## Bestehende Testlandschaft

`VideoWebPlayer.Maui.Tests` (xUnit, net10.0): ~80 Dateien, zwei Arten:

- **Domain-/MVU-Tests**: `AppUpdateTests_*` (23 Dateien — Startup, Connection, ServerManagement, Pairing, Offline, Navigation, Sources, Downloads, Player …), `MvuHostTests`, Service-Tests (`AuthSessionService`, `DeviceTokenService`, `KnownServersService`, `ServerDiscoveryService`, `ServerSyncService` …), API-Client-Tests (`VideoPlayerApiClientTests_*`, `DevicePairingClientTests`, `PairingCryptoTests`, `PairingCodeTests`).
- **Quelltext-Contract-Tests**: lesen XAML/CS über `RepositoryRoot.Find()` (`Stubs/RepositoryRoot.cs`) — `ServerSetupContractTests`, `PairingContractTests`, `DesignTokensContractTests`, `HomePageContractTests`, `VideoPlayerMauiContractTests`, `ConnectionWorkflowContractTests`, `EnumCoverageTests` u. a.

**Keine** UI-instantiierten Tests, kein FlaUI; das Projekt kann keine MAUI-Targets hosten (`ServerSetupContractTests.cs` Z. 7–9).

## Windows-App starten

- `VideoPlayer.Maui.csproj`: `OutputType=Exe` (Z. 6), TFM `net10.0-windows10.0.19041.0` (Z. 5), `WindowsPackageType=None` (Z. 23) → `VideoPlayer.Maui.exe` direkt ausführbar (kein MSIX/`runFullTrust`-Kontext nötig; `Package.appxmanifest` ist für den unpackageden Modus irrelevant).
- Build: `dotnet build VideoPlayer.Maui/VideoPlayer.Maui.csproj -f net10.0-windows10.0.19041.0` → Exe unter `bin/<config>/net10.0-windows10.0.19041.0/<rid>/` (RID-Ordner lokal verifizieren; MAUI-Windows-Default `win-x64` ab .NET 9+).
- Der App-Start ohne Server landet deterministisch auf `ServerSetupPage` (`UpdateServersLoaded`, `AppUpdate.cs` Z. 230–241) — idealer Ausgangszustand für Setup-/Pairing-Szenarien. Für Login-/Wechsel-Szenarien wird ein Server benötigt (echter Server-Checkout `487c3041-…` oder Test-Backend).

## UIA-Grenzen (belegt)

- CollectionView-Items erscheinen im Windows-UIA-Baum nicht zuverlässig — die #15-Verifikation musste Koordinatenklicks nutzen (`live-verification.md` Z. 60–65). Zustandslesen (`CurrentPage`/`ConnectionState`) war nur über Screenshots/IIS-Logs möglich.
- Dynamische Flyout-Einträge der Quellen (`AppShell.xaml.cs` Z. 213–266) sind ebenfalls schwer adressierbar.

## Verhältnis zur Developer-API (offener Punkt requirement Z. 51/127)

- FlaUI über UIA: echte Nutzerinteraktion, aber CollectionView-Lücke + kein Zustandslesen.
- Dev-API als Treiber: `GET /model`/`/tree` schließt beide Lücken; `POST /invoke`/`set-text` ersetzt Koordinatenklicks. Empfehlung für die Planung: **Kombination** — FlaUI für Start/Seitenpräsenz/echte Klicks, Dev-API für Zustands-Assertions und Listen-Interaktion.

## CI

- `pr-checks.yml`: `windows-latest`, .NET 10, MAUI-Workload, Build beider Apps + `dotnet test` + Format-Check (Z. 11–50). Kein interaktiver Desktop — FlaUI auf GH-hosted Windows benötigt i. d. R. eine UI-Session (Self-Hosted Runner oder Session-Setup); Billing-Blocker macht CI-Integration ohnehin optional (requirement Z. 49).
- Release-Workflows publishen nur die alte `VideoWebPlayer.Maui` (Windows-MSIX, `staging-merge.yml` Z. 68–85); die neue `VideoPlayer.Maui` hat keinen Windows-Publish-Job — E2E baut lokal selbst.

## Szenario-Mapping (requirement Z. 48)

| Szenario | Treiber |
|----------|---------|
| Server-Setup (Erststart → Adresse → Verbinden) | FlaUI (`AddressEntry`/`ConnectButton`-AutomationIds) + Dev-API `GET /model` (`ConnectionState`) |
| Pairing-Flow | FlaUI (`PairingCodeEntry`/`PairButton`) oder Dev-API `dispatch PairingCodeSubmittedMessage` |
| Serverwechsel | `SettingsPage` `ServerSwitchButton` → `serversetup`-Route; Assertion `CurrentPage` via Dev-API |
| Anmeldung | `LoginPage`-Ids (`LoginUsernameEntry`/`LoginPasswordEntry`/`LoginButton`) |
