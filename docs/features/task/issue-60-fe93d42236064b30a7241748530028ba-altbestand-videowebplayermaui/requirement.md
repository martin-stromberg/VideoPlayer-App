# Übersetzte Anforderung: Altbestand VideoWebPlayer.Maui / VideoWebPlayer.Client entfernen

Quelle: Issue #60, `issue.md` im Repository-Stamm.

## Fachliche Zusammenfassung

Die veralteten Projekte `VideoWebPlayer.Maui` (bisherige MAUI-App) und `VideoWebPlayer.Client` (alter API-Client) werden vollständig aus dem Repository entfernt. Sie existieren parallel zur aktuellen Architektur (`VideoPlayer.Maui` / `VideoPlayer.Mvu` / `VideoPlayer.ApiClient` / `VideoPlayer.Storage`) und werden fachlich nicht mehr gepflegt (u. a. fehlt im Legacy-`VideoWebPlayerClient.ReportPlaybackProgressAsync` der Playlist-Kontext, vgl. Issue #59). Entfernt werden die Projektverzeichnisse, alle Solution-Einträge, Projekt-/Quelltext-Referenzen aus verbleibenden Projekten, CI-/Build-Anteile und die zugehörige Dokumentation.

## Betroffene Klassen und Komponenten

### Zu löschende Projekte

- `VideoWebPlayer.Maui/` — gesamtes Projektverzeichnis inkl. `VideoWebPlayer.Maui.csproj` (referenziert `VideoWebPlayer.Client`)
- `VideoWebPlayer.Client/` — gesamtes Projektverzeichnis inkl. `VideoWebPlayer.Client.csproj`, `VideoWebPlayerClient`, `Models/` (u. a. `ContinueWatchingDto`, `ContinueWatchingMutationResult`, `DtoRecentEntry` mit Enum `RecentEntryType`)

### Solution

- `VideoPlayer.App.sln` — Projekteinträge `VideoWebPlayer.Client` (GUID `{3512A538-…}`), `VideoWebPlayer.Maui` (GUID `{FE005A1B-…}`) samt allen zugehörigen `ProjectConfigurationPlatforms`-Zeilen entfernen; `VideoWebPlayer.Maui.Tests` bleibt in der Solution

### Verbleibende Projekte mit Referenzen auf den Altbestand

- `VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj` — `ProjectReference` auf `..\VideoWebPlayer.Client\VideoWebPlayer.Client.csproj` entfernen (Testprojekt selbst bleibt, es testet die neue App)
- `VideoWebPlayer.Maui.Tests/ContinueWatchingEventIngressTests.cs` — testet ausschließlich DTOs des Altbestands (`ContinueWatchingDto`, `DtoMovie`, `ContinueWatchingMutationResult` aus `VideoWebPlayer.Client.Models`) → Datei löschen
- `VideoWebPlayer.Maui.Tests/MauiSettingsContractTests.cs` — Quelltext-Vertragstest, liest nur Dateien aus `VideoWebPlayer.Maui/` → Datei löschen
- `VideoWebPlayer.Maui.Tests/EnumCoverageTests.cs` — gemischter Test: Laufzeit-Checks der neuen App behalten (`RecentEntryKind`, `PlaybackSourceKind`, `HealthCheckResult`, `PairingStatus`); Altbestand-Prüfungen entfernen (`RecentEntryType` aus `VideoWebPlayer.Client`, `DownloadRetentionType`, `DownloadStatus`, `VideoSourceType` aus `VideoWebPlayer.Maui/Models`, `CarouselKind` aus `VideoWebPlayer.Maui/ViewModels`); Hilfsmethoden `Read`/`EnumValuesFromSource` und nicht mehr benötigte Usings mitentfernen

### CI/CD

- `.github/workflows/pr-checks.yml` — Schritte „Build Client" (`VideoWebPlayer.Client`) und „Build MAUI Windows" (`VideoWebPlayer.Maui`) entfernen; Test-/Lint-Schritte bleiben
- `.github/workflows/main-merge.yml` — Altbestand-Publizierung entfernen (`Publish Android`, `Publish Windows`, `Build iOS`, Upload-Schritte für APK/MSIX/.app des Altbestands)
- `.github/workflows/staging-merge.yml` — analog zu `main-merge.yml`

### Dokumentation

- `README.md` — Zeile „`VideoWebPlayer.Maui` | Altbestand …" aus der Projektstruktur-Tabelle entfernen
- `docs/help/app-grundgeruest.md`, `docs/help/einstellungen-datenhaltung.md`, `docs/help/erscheinungsbild.md` — Altbestand-Erwähnungen bereinigen
- `changes.log` — neuer Eintrag (Dokumentationsschritt)

### Bereinigung ohne funktionale Änderung

- `VideoPlayer.ApiClient/Models/PlaylistSortModeValues.cs` — XML-Doc-Kommentar referenziert `VideoWebPlayer.Data.PlaylistSortMode`; Kommentar auf aktuellen Bezug prüfen/anpassen
- `scripts/iOS-Deployment.ResponseFile.ps1` — Kommentar nennt Testpfad `VideoWebPlayer.Maui.Tests/IosDeploymentResponseFileTests.cs` (bleibt korrekt, Testprojekt wird nicht umbenannt — nur prüfen)

### Explizit unverändert

- `VideoWebPlayer.Maui.Tests` (Projektname, Assembly-Name, `InternalsVisibleTo`-Einträge in `VideoPlayer.ApiClient.csproj`/`VideoPlayer.Mvu.csproj`) — bleibt bestehen, testet die neue App
- `VideoPlayer.E2E.Tests`, `msTools.DeveloperApi`, `msTools.DeveloperApi.Maui` — keine Altbestand-Referenzen
- `docs/projects/` und `docs/features/` — historische Artefakte, keine Bereinigung
- `changes.log` — Historie bleibt, nur neuer Eintrag
- Umgebungsvariable `VIDEOWEBPLAYER_MAUI_API_TOKEN` — Name wird vom neuen `ClientGate` weiter gelesen (Deployment-Kompatibilität), kein Rename

## Implementierungsansatz

- Reine Lösch-/Bereinigungsaufgabe, keine neue fachliche Logik.
- Vorgehen: (1) Altbestand-Referenzen im Testprojekt auflösen (Testdateien löschen/bereinigen, `ProjectReference` entfernen), (2) Projektverzeichnisse `VideoWebPlayer.Maui/` und `VideoWebPlayer.Client/` per `git rm -r` löschen, (3) `VideoPlayer.App.sln` bereinigen, (4) Workflows bereinigen, (5) Doku-/Kommentar-Referenzen bereinigen, (6) Build (`dotnet build` der Test- und App-Projekte) und `dotnet test` verifizieren.

## Konfiguration

Keine neuen Konfigurationseinträge. Die Release-Workflows verlieren ggf. Artefakt-Produktion (siehe Offene Fragen).

## Offene Fragen

1. **Release-Artefakte:** `main-merge.yml` und `staging-merge.yml` veröffentlichen aktuell APK/MSIX/iOS-`.app` des **Altbestands** ans GitHub-Release. Sollen die Publish-/Upload-Schritte nur entfernt werden, oder sollen sie auf `VideoPlayer.Maui` umgestellt werden (d. h. künftig die neue App ausliefern)? Empfehlung: nur entfernen — Umstellung der Release-Auslieferung auf die neue App ist eine separate Produktentscheidung/eigenes Issue.
2. **Testprojekt-Name:** `VideoWebPlayer.Maui.Tests` behält seinen Namen trotz Entfernung des Namenspatrons — Umbenennung (inkl. `InternalsVisibleTo`, CI-Pfaden, Verzeichnis) im Issue nicht gefordert. Empfehlung: beibehalten.
