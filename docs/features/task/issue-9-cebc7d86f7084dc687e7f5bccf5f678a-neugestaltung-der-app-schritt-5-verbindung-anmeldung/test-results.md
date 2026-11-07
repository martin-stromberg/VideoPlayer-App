# Testergebnisse

**Status:** Keine Fehler

## Ausgeführte Verifikationen

| Prüfung | Ergebnis |
|---------|----------|
| Build aller geänderten Projekte (`VideoPlayer.Storage`, `VideoPlayer.ApiClient`, `VideoPlayer.Mvu`, `VideoPlayer.Maui` Windows, `VideoWebPlayer.Maui.Tests`), Release | erfolgreich, 0 Fehler |
| `dotnet test VideoWebPlayer.Maui.Tests` (Release, mit Coverage-Sammlung) | 280 Tests: 280 bestanden, 0 fehlgeschlagen, 0 übersprungen |
| Code-Coverage (`--collect:"XPlat Code Coverage"`) | `VideoPlayer.Storage` 99,3 %, `VideoPlayer.Mvu` 94,6 %, `VideoPlayer.ApiClient` 84,6 % Zeilen (`VideoWebPlayer.Client` 7,3 %, Altbestand) |
| Windows-Build `VideoPlayer.Maui` (`net10.0-windows10.0.19041.0`, Release, XAML-SourceGen) | erfolgreich, 0 Warnungen, 0 Fehler |
| iOS/MacCatalyst-/Android-Build | lokal auf Windows nicht ausgeführt — plattformneutraler Code (`VideoPlayer.Storage`, `VideoPlayer.ApiClient`, `VideoPlayer.Mvu`) ist vollständig net10.0-getestet; MAUI-spezifische Verdrahtung über Contract-Tests (`ConnectionWorkflowContractTests`) |
| Windows-Smoke-Run / UI-E2E | nicht verfügbar — das Repository enthält kein UI-Automatisierungs-Framework und das Testprojekt kann `VideoPlayer.Maui` nicht referenzieren (siehe `inventory/tests.md`); Ersatz gemäß Plan: vollständige Message-Sequenz-Tests auf MVU-Ebene + Contract-Tests der Oberflächen-Verdrahtung |

Befehl (Arbeitsverzeichnis: Repository-Wurzel, Exit-Code 0):

```
dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj -c Release --collect:"XPlat Code Coverage" --logger "console;verbosity=normal"
```

Protokoll: `final-dotnet-test.log` (Coverage-Anlage:
`coverage.cobertura.xml` unter `VideoWebPlayer.Maui.Tests/TestResults/`).

## Baseline vs. Ergebnis

- Baseline (vor Implementierung, `inventory/test-results/baseline-dotnet-test.log`):
  229 Tests, 229 bestanden.
- Nach Implementierung: 280 Tests, 280 bestanden (51 neue Tests).

## Neue Tests (Auswahl)

- `AppUpdateTests_Connection` — alle Übergänge des Verbindungs-Workflows:
  Offline-Daten-Verzweigung (`Offline`/`NeedsServerSetup`), automatische
  Anmeldung (`Connected`/`NeedsLogin` mit Fehlertext-Mapping und
  Benutzernamen-Vorbefüllung), manuelle Anmeldung (Validierung,
  Laufzustand, Erfolg/Fehlschlag), `SessionExpired` (Wechsel zu
  `NeedsLogin`/`Login`, ignoriert bei bereits ausstehender Anmeldung),
  Serverwechsel löst `PersistActiveServerEffect` + `AttemptAutoLoginEffect`
  aus, Navigations-Guards bei ausstehendem Workflow.
- `AppUpdateTests_Startup` (angepasst) — Health-Check-Abschluss emittiert
  `AttemptAutoLoginEffect` bzw. `CheckOfflineDataEffect` statt direkter
  Home-Navigation; defensiver Setup-Pfad ohne aktiven Server.
- `AppUpdateTests_ServerManagement` / `MvuHostTests` (angepasst) — Erfolgs-
  Kandidatenprüfung endet in der automatischen Anmeldung; Effektkette
  `Startup → LoadServers → HealthCheck → AutoLogin → Navigate(Home)`.
- `AuthSessionServiceTests` — echte HTTP-Stub-Tests: manuelle/automatische
  Anmeldung mit Persistenz von Zugangsdaten und Token
  (`AppSecretsService` + In-Memory-Stores), Statusklassifikation
  (401 → `InvalidCredentials`, Transportfehler nach 4 Requests →
  `ServerUnreachable`, 5xx → `Failed`), `MissingCredentials` mit
  Benutzernamen-Vorbefüllung, `TokenRefresher`-Kette (401 → stille
  Neuanmeldung → Neuversand mit neuem Bearer-Token, Token persistiert),
  `SessionExpired` bei fehlenden/abgelehnten Zugangsdaten, kein
  `SessionExpired` bei transienten Fehlern.
- `ConnectionWorkflowContractTests` — Quelltext-Contracts: `LoginPage`
  (Controls, `IsPassword`, AutomationIds, Message-Dispatch), Shell-Route
  `login` ohne FlyoutItem, `LoadingPage`-Bindung `StartupStatusText`,
  `OfflineBanner` auf `HomePage`, DI-Registrierungen, Effekt-Handler-
  Zweige, `TokenRefresher`-Verdrahtung.
- `LocalDatabaseTests` — `CacheDatabase.HasOfflineDataAsync` gegen reale
  SQLite-Ablage (leer → `false`; Eintrag in `cached_lists` bzw.
  `download_manifest` → `true`).
