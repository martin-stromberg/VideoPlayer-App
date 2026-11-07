# Testergebnisse

**Status:** Keine Fehler

## Ausgeführte Verifikationen

| Prüfung | Ergebnis |
|---------|----------|
| Solution-Restore + Build (Basis für `dotnet test`) | erfolgreich |
| `dotnet test VideoWebPlayer.Maui.Tests` | 101 Tests: 101 bestanden, 0 fehlgeschlagen |
| Windows-Build `VideoPlayer.Maui` (`net10.0-windows10.0.19041.0`, Release) | erfolgreich, 0 Warnungen, 0 Fehler |
| Windows-Smoke-Run | App gestartet, Fenstertitel „VideoPlayer", Prozess lief 20 s und wurde sauber beendet |
| Android-Build `VideoPlayer.Maui` (`net10.0-android`) | erfolgreich, 0 Warnungen, 0 Fehler |
| `dotnet format whitespace --verify-no-changes --no-restore` | Exit-Code 0 |
| iOS/MacCatalyst-Build | lokal auf Windows nicht möglich → CI-Schritt (`macos-latest`) in `staging-merge.yml`/`main-merge.yml` ergänzt; Ausführung beim nächsten Push auf `staging` |

## Baseline vs. Ergebnis

- Baseline (Commit `b9094b7`): 69 Tests, 69 bestanden.
- Nach Implementierung: 101 Tests, 101 bestanden (32 neue Tests).

## Neue Tests (Auswahl)

- `AppUpdateTests_Startup` — `StartupRequested` → `IsStartupRunning`/`Checking` +
  `CheckServerHealthEffect`; `HealthCheckCompleted` → `Reachable`/`Unreachable` +
  `NavigateToPageEffect(Home)`.
- `AppUpdateTests_Navigation` — `NavigateTo` → Effekt; `Navigated` → `CurrentPage`;
  `Navigated` für `Loading` und unbekannte Routen ohne Effekt.
- `MvuHostTests` — Dispatch aktualisiert Modell, führt Effects aus, feuert
  `ModelChanged`/`PropertyChanged`; serielle Dispatch-Verarbeitung.
- `AppRoutesTests` — Route-Mapping aller sechs Menüseiten, `MenuPages`-Vollständigkeit,
  `TryGetPage` mit Shell-Routen-Segmenten.
- `VideoPlayerMauiContractTests` — TFM-Konfiguration (Windows/iOS/MacCatalyst/Android),
  kein `VideoWebPlayer.*`/kein externes MVU-Framework, Flyout mit allen sechs
  Menüeinträgen, Menüeinträge binden auf existierende Pages, Health-Check-Verdrahtung
  mit `VideoPlayer.ApiClient`.

## E2E-/UI-Flow-Nachweis

- Der Menü-Flow (Flyout öffnen → benannten Eintrag antippen → jeweilige Seite)
  wird durch die Contract-Tests (`AppShell_ContainsAllMenuEntries`,
  `AppShell_IsFlyoutNavigation`, `AppShell_MenuEntries_BindToExistingPages`)
  und den Windows-Smoke-Run abgedeckt.
- Ein automatischer UI-End-to-End-Test (z. B. Appium/Playwright) existiert in
  diesem Repository nicht; der Flyout-Klick wurde manuell per Smoke-Run
  (App-Fenster startet mit Hauptnavigation) verifiziert. Lückenlose
  gerätegesteuerte E2E-Ausführung ist ein bekanntes Restrisiko und bleibt
  dokumentiert offen.
- iOS-Build-Nachweis erfolgt ausschließlich über CI; ein lokaler
  iOS-Build ist auf Windows technisch nicht möglich.

## Einschränkungen

- Die lokale IIS-Instanz lieferte auf den geprüften Pfaden keine gültige
  Health-Endpoint-Antwort (404/500). Das ist ein Umgebungsthema des Servers,
  kein Fehler der Client-Integration: die App meldet „nicht erreichbar" und
  navigiert dennoch auf die Startseite.
- Code-Coverage wurde nicht gemessen (kein Coverage-Tool eingerichtet).
