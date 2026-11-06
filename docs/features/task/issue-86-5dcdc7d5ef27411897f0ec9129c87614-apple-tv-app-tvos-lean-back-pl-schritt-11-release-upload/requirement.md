# Anforderung (übersetzt) — Schritt 11: Signiertes Release-Paket und automatisierter Upload nach App Store Connect

Quelle: `docs/projects/task/issue-86-.../project-plan.md`, Schritt 11 (wörtlich übernommen, verdichtet strukturiert).

## Ziel

Für die tvOS-App wird der Release-Pfad analog zum bestehenden iOS-Pfad geschaffen: eine wiederverwendbare Paketierungs-Aktion nach dem Muster von `.github/actions/package-ios`, eingebunden in die Staging- und Main-Release-Workflows hinter einer eigenen Aktivierungs-Gate-Variable, plus ein lokales Hilfsskript nach dem Muster von `scripts/iOS-Deployment.ps1`.

## Umfang

1. **Composite-Action `package-tvos`** (Muster `package-ios`):
   - SDK-Band pinnen (`10.0.1xx` / `global.json` `10.0.100`)
   - tvOS-Workload-Rollback installieren (`microsoft.net.sdk.tvos`, Default `26.5.10318`)
   - tvOS-Build für Geräte-Architektur (`tvos-arm64`) signiert kompilieren und archivieren (Zertifikat und tvOS-Provisioning-Profil aus CI-Secrets/Keychain-Import)
   - App-Paket (`.ipa`) samt Symbolsatz (dSYM-Archiv) erzeugen
   - Upload per App-Store-Connect-API-Schlüssel mit iTMSTransporter als Release-Kandidat nach App Store Connect (TestFlight)
2. **Einbindung in `staging-merge.yml` und `main-merge.yml`**: je ein `package-tvos`-Job hinter eigener Gate-Variable `TVOS_SIGNING_ENABLED` (analog `IOS_SIGNING_ENABLED`) — ohne gesetztes Gate bleibt der Workflow grün; Artefakte (`.ipa` + dSYM-Zip) werden wie beim iOS-Pfad ans jeweilige Release gehängt.
3. **Lokales Hilfsskript** `scripts/tvOS-Deployment.ps1` nach dem Muster `scripts/iOS-Deployment.ps1` — gleiche Aktionen für manuelle Builds (ggf. via SSH vom Windows-Rechner auf einen Mac).
4. **Build-Nummerierung**: bestehende Konvention (Commit-Anzahl `git rev-list --count HEAD` + `run_attempt` − 1).

## Fachliche Voraussetzung außerhalb des Codes

Bundle-ID, tvOS-Provisioning-Profil und Signaturzertifikate müssen im Apple-Developer-Account vorliegen — **noch nicht eingerichtet**, manueller Vorlauf des Kontoinhabers. Der Gate-Ansatz hält den Workflow bis zur Bereitstellung grün; erst danach aktiviert sich der produktive Upload.

## Akzeptanzkriterien

- Der neue Paketierungs-Job baut und signiert die tvOS-App im Release-Workflow
- Bei gesetztem API-Schlüssel landet der Release-Kandidat automatisiert in App Store Connect (TestFlight)
- Ohne aktiviertes Gate bleibt der Workflow grün
- Build-Nummerierung folgt der bestehenden Konvention (Commit-Anzahl plus Laufzähler)
- Das lokale Skript ermöglicht denselben Ablauf manuell
- Die iOS-Pfade der Workflows bleiben unverändert funktionsfähig

## Abhängigkeiten / Rahmen

- Abhängig von Schritt 1 (tvOS-Projektgerüst, `build-tvos`-Job) — umgesetzt
- tvOS lokal (Windows) nicht kompilierbar → Push autorisiert (CI `build-tvos`, Trigger `task/issue-86-**`), Abschluss erst bei grünem Lauf
- `dotnet test VideoPlayer.Maui.Tests` bleibt grün; Contract-Tests für die CI-Struktur erwägen (analog `TvOsProjectContractTests`)
- Keine Secrets im Code; kein Schreibzugriff auf `docs/projects/`
