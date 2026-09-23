# Testergebnisse

**Status:** Keine Fehler

## Ausgeführte Verifikationen

| Prüfung | Ergebnis |
|---------|----------|
| Solution-Restore + Build (Basis für `dotnet test`) | erfolgreich |
| `dotnet test VideoWebPlayer.Maui.Tests` (Release) | 160 Tests: 160 bestanden, 0 fehlgeschlagen, 0 übersprungen |
| Code-Coverage (`--collect:"XPlat Code Coverage"`) | gemessen: gesamt 63,4 % Zeilen — `VideoPlayer.Storage` 100 %, `VideoPlayer.Mvu` 93,0 %, `VideoPlayer.ApiClient` 89,5 % (`VideoWebPlayer.Client` 7,3 %, Altbestand) |
| Windows-Build `VideoPlayer.Maui` (`net10.0-windows10.0.19041.0`, Release) | erfolgreich, 0 Warnungen, 0 Fehler |
| Windows-Smoke-Run | App gestartet, lief 15 s stabil (Ablage-Initialisierung eingeschlossen), wurde beendet |
| `dotnet format whitespace --verify-no-changes` (geänderte Projekte/Dateien) | Exit-Code 0 |
| iOS/MacCatalyst-/Android-Build | lokal auf Windows nicht ausgeführt — Plattformpfade über Contract-Tests (`IosBackupExclusion_IsImplemented`, `WindowsRoaming_IsUsedForBackupRoot`) abgesichert |

Abdeckung: Messbar (siehe oben). Protokoll: `final-dotnet-test.log`.

## Baseline vs. Ergebnis

- Baseline (Commit `8ff4e41`): 101 Tests, 101 bestanden
  (`inventory/test-results/baseline-dotnet-test.log`).
- Nach Implementierung: 160 Tests, 160 bestanden (59 neue Tests).

## Neue Tests (Auswahl)

- `AppSettingsTests` — Defaults (u. a. `DownloadQuotaGb == 5`),
  `Normalize`-Clamping aller Schranken.
- `AppSettingsServiceTests` — Defaults bei leerem Store,
  Save/Load-Roundtrip (Persistenz über „Neustart"), Clamping beim Laden,
  Profil-Isolation, Profil-Präfix-Schlüssel.
- `AppSecretsServiceTests` — Geheimnisse ausschließlich im `ISecretStore`,
  Username in normaler Ablage, Legacy-Bereinigung, `ClearCredentials`,
  Profil-Isolation.
- `AppProfileTests` — `VIDEOPLAYER_PROFILE`-Auswertung (injizierter
  Accessor), Default-Fallback, Kleinschreibung, Präfix-/Verzeichnisregeln.
- `AppStorageLayoutTests` — Trennung Backup-/NoBackup-/Temp-Wurzeln,
  Profil-Unterverzeichnisse, reale Verzeichnisanlage.
- `LocalDatabaseTests` — reale SQLite-Dateien im Temp-Pfad: Tabellen und
  Insert/Select-Roundtrips beider Datenbanken; `videoplayer.db3` im
  Backup-, `videoplayer-cache.db3` im NoBackup-Verzeichnis.
- `AppUpdateTests_Settings` — `LoadSettingsRequested` → `LoadSettingsEffect`,
  `SettingsLoaded` → Model, `SaveSettingsRequested` → Model +
  `PersistSettingsEffect` (normalisiert), Startup-Effektliste.
- `SettingsStorageContractTests` — `SettingsPage.xaml` enthält alle acht
  beschrifteten Controls mit `AutomationId`; Code-Behind nutzt `MvuHost` +
  `SaveSettingsRequestedMessage`; `MauiProgram` registriert alle Dienste;
  `ServerAddressProvider` ohne `Preferences.Default`; kein direkter
  Plattform-Zugriff außerhalb der Adapter; iOS-`SetSkipBackupAttribute`;
  Windows-Roaming-Wurzel.

## E2E-/UI-Flow-Nachweis

- Benutzerfluss „Einstellungen pflegen" (Seite öffnen → Werte anzeigen →
  Schalter/Zahlenfeld ändern → persistiert → nach Neustart geladen) wird
  durch die Nachweiskette Contract-Tests (`SettingsPage_*`) +
  MVU-Update-Tests (`AppUpdateTests_Settings`) + Persistenz-Roundtrips
  (`AppSettingsServiceTests.Save_ThenLoad_RoundTrips`) + Windows-Build +
  Smoke-Run abgedeckt.
- Ungültige Eingabe: `InvalidInputLabel` existiert mit deutscher
  Fehlermeldung; `SaveNumeric` verwirft ungültige Werte ohne Dispatch
  (Contract-Test auf Verdrahtung + Code-Prüfung).
- Es existiert kein UI-Automatisierungs-Framework im Repository
  (`inventory/tests.md`); gerenderte E2E-Tests sind nicht möglich —
  gleiche begründete Ersatzform wie in Schritt 2 akzeptiert.
- iOS-Backup-Ausschluss und Windows-Roaming sind über Quelltext-
  Contract-Tests nachgewiesen; ein iOS-Geräte-Lauf ist lokal nicht
  möglich (CI-Nachweis beim nächsten Push auf `staging`).

## Einschränkungen

- Die Datenbanktabellen werden strukturell angelegt und getestet, aber in
  diesem Schritt noch nicht von App-Funktionen befüllt (folgt in den
  Schritten Serverliste, Fortschritt, Downloads).
- Geheimnis-Dienste (`AppSecretsService`) sind implementiert und getestet,
  haben aber noch keinen UI-Aufrufer (Anmeldung folgt in einem späteren
  Schritt).
