# Plan-Gegenprüfung

Geprüft: `requirement.md`, `inventory.md` (inkl. `inventory/api-client.md`,
`inventory/app-shell-navigation.md`, `inventory/build-ci.md`, `inventory/tests.md`)
gegen `plan.md`. Hinweis: Selbstprüfung ohne Unteragenten.

## Ergebnis

**Status:** Plan vollständig

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| App baut und startet unter Windows | `VideoPlayer.Maui`-Projekt mit `net10.0-windows10.0.19041.0` (Umsetzungsschritte 3, 6); Verifikationsschritt 9 (Release-Build) | Windows-Release-Build + Smoke-Run der Exe (E2E-Tabelle), `dotnet build` in `pr-checks.yml` | Abgedeckt |
| iOS-Build über CI nachweisbar | `net10.0-ios`-TFM; iOS-Build-Schritt in `staging-merge.yml`/`main-merge.yml` auf `macos-latest` (Schritt 8) | `NewApp_ProjectTargetsRequiredPlatforms` (TFM-Contract-Test) + CI-Schritt als Nachweisform | Abgedeckt |
| Hamburgermenü mit sechs Einträgen → Seiten | `AppShell` mit `FlyoutBehavior="Flyout"` + sechs `FlyoutItem`s + Routen über `AppRoutes` (Schritt 5) | `AppShell_ContainsAllMenuEntries`, `AppShell_MenuEntries_BindToExistingPages`, `AppUpdateTests_Navigation`, `AppRoutesTests` | Abgedeckt |
| MVU ohne MVVM-Übernahme, ohne externes MVU-Framework | Neuer eigener Zyklus in `VideoPlayer.Mvu` (Schritt 2); keine `ViewModels`, kein Fabulous/Comet | `NewApp_ProjectTargetsRequiredPlatforms` (verbietet `VideoWebPlayer.*`-Referenz und MVU-Framework-Pakete), Unit-Tests des Zyklus | Abgedeckt |
| API des lokalen IIS-Webprojekts grundsätzlich aufrufbar | `IServerAddressProvider` (Default `http://localhost/videoplayer`), `IServerHealthProbe` → `CheckHealthAsync`, `CheckServerHealthEffect` im Start-Ablauf | `NewApp_HealthCheckWired` (Contract) + Bestandstests des Clients + dokumentierter Live-Aufruf | Abgedeckt |

## Fehlende oder unvollständige Testanforderungen

—

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Hamburgermenü: sechs Einträge sichtbar, Tipp führt zur Seite | `AppShell_ContainsAllMenuEntries` + `AppShell_MenuEntries_BindToExistingPages` (XAML-Contract) + `AppUpdateTests_Navigation`/`MvuHostTests` (Zustandslogik) | Abgedeckt (Ersatzform — kein UI-Automatisierungsframework im Repo, siehe `inventory/tests.md`; Plan verankert die Nachweiskette explizit) |
| App startet unter Windows (Ladeanzeige → Shell) | Windows-Build + Smoke-Run der Exe | Abgedeckt |
| iOS-Build | CI-Build-Schritt `net10.0-ios`/`iossimulator-arm64` + TFM-Contract-Test | Abgedeckt (konfigurierter Nachweis; lokaler iOS-Build unmöglich) |
| API-Aufruf (Erreichbarkeitsprüfung) | `NewApp_HealthCheckWired` + dokumentierter Live-Aufruf | Abgedeckt |

## Fehlende oder unvollständige Planbestandteile

—

## Hinweise

- Der Plan verzichtet bewusst auf ein UI-Automatisierungsframework (keines im Repo;
  Aufbau wäre ein eigenes Infrastrukturprojekt). Die dokumentierte Ersatz-Nachweiskette
  (Contract-Tests auf `AppShell.xaml` + MVU-Unit-Tests + Windows-Smoke-Run + CI-iOS-Build)
  ist in dieser Umgebung die stärkste automatisierte Absicherung des Benutzerflusses.
- `dotnet format whitespace --verify-no-changes` ist als Lint-Gate im
  Verifikationsschritt 9 berücksichtigt.
