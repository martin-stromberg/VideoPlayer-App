# Umsetzungsplan: Altbestand VideoWebPlayer.Maui / VideoWebPlayer.Client entfernen

## Übersicht

Die Projekte `VideoWebPlayer.Maui` und `VideoWebPlayer.Client` werden vollständig aus dem Repository entfernt. Zusätzlich wird das verbleibende Testprojekt `VideoWebPlayer.Maui.Tests` — das trotz des Namens die neue App testet — in `VideoPlayer.Maui.Tests` umbenannt. Betroffen sind die Projektverzeichnisse, die Solution `VideoPlayer.App.sln`, Testdateien, die GitHub-Workflows samt `Set-Version.ps1`, `InternalsVisibleTo`-Einträge sowie Referenzen in Dokumentation und Kommentaren. Die Release-Workflows verlieren die Altbestand-Publizierung; der iOS-`.app`-Upload wird auf `VideoPlayer.Maui` umgebogen. Die neue App hat keinerlei Code-Abhängigkeit zum Altbestand.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Altbestand-Tests im Testprojekt | `ContinueWatchingEventIngressTests.cs` und `MauiSettingsContractTests.cs` komplett löschen; `EnumCoverageTests.cs` nur um Legacy-Testfälle und -Hilfsmethoden erleichtern | Die gelöschten Dateien testen ausschließlich Altbestand-Artefakte; `EnumCoverageTests` enthält gemischte Laufzeit-Checks der neuen App, die für den `enum-coverage-check`-Hook weiter benötigt werden |
| Testprojekt | Umbenennung `VideoWebPlayer.Maui.Tests` → `VideoPlayer.Maui.Tests` (Verzeichnis, `.csproj`, Namespaces, `InternalsVisibleTo`, alle Pfad-/Namensreferenzen) | Nutzerentscheidung: Mit dem Altbestand entfällt der Namenspatron; die Umbenennung erfolgt direkt mit, statt einen irreführenden Projektnamen zu konservieren |
| Release-Workflows | Altbestand-Publish/-Upload entfernen; iOS-`.app.zip`-Upload auf `VideoPlayer.Maui` umbiegen; `build-windows`-Job komplett entfernen und aus `needs` nehmen | Nutzerentscheidung: Windows-/Android-Publish entfällt (Windows erzeugt ohnehin kein MSIX — `WindowsPackageType=None`; Android inoffiziell/unsigniert). Der iOS-Simulator-Build der neuen App existiert bereits und liefert weiterhin ein Release-Artefakt |
| Git-Hook `enum-coverage-check` | Keine Änderung am Hook | Der Hook auditiert repo-weit alle Enums; mit dem Altbestand verschwinden dessen Enums, die verbleibenden Checks bleiben ausreichend |

## Programmabläufe

### Bereinigung des Altbestands

1. Altbestand-Abhängigkeiten im Testprojekt auflösen (Testdateien löschen/bereinigen, `ProjectReference` entfernen).
2. Testprojekt in `VideoPlayer.Maui.Tests` umbenennen (Verzeichnis, `.csproj`, Namespaces, externe Referenzen).
3. Verzeichnisse `VideoWebPlayer.Maui/` und `VideoWebPlayer.Client/` per `git rm -r` löschen.
4. `VideoPlayer.App.sln` bereinigen (Altbestand-Einträge entfernen, Testprojekt-Pfad/-Name aktualisieren).
5. Workflows und `Set-Version.ps1` bereinigen.
6. Verbleibende Dokumentations- und Kommentar-Referenzen bereinigen.
7. Verifikation: `dotnet restore`, `dotnet build`, `dotnet test` und `dotnet format whitespace --verify-no-changes` müssen fehlerfrei sein; Repo-Suche nach `VideoWebPlayer` darf nur noch historische Artefakte (`docs/features/`, `docs/projects/`, `changes.log`) und den Env-Var-Namen `VIDEOWEBPLAYER_MAUI_API_TOKEN` liefern.

Beteiligte Komponenten: `VideoPlayer.App.sln`, `VideoWebPlayer.Maui.Tests` → `VideoPlayer.Maui.Tests`, `.github/workflows/*.yml`, `.github/scripts/Set-Version.ps1`, `README.md`, `docs/help/*.md`.

## Neue Klassen

Keine.

## Änderungen an bestehenden Klassen / Dateien

### Testprojekt-Umbenennung `VideoWebPlayer.Maui.Tests` → `VideoPlayer.Maui.Tests`

- `git mv VideoWebPlayer.Maui.Tests VideoPlayer.Maui.Tests`; `.csproj` entsprechend umbenennen. `RootNamespace`/`AssemblyName` sind nicht explizit gesetzt und folgen dem Projektnamen automatisch.
- In allen ~114 Test-/Stub-Dateien: `namespace VideoWebPlayer.Maui.Tests` → `namespace VideoPlayer.Maui.Tests`, `using VideoWebPlayer.Maui.Tests.Stubs` → `using VideoPlayer.Maui.Tests.Stubs`, `<c>VideoWebPlayer.Maui.Tests</c>`-Referenzen in XML-Docs entsprechend.
- `SettingsStorageContractTests.StorageProject_IsReferencedEverywhere`: Pfad `"VideoWebPlayer.Maui.Tests", "VideoWebPlayer.Maui.Tests.csproj"` → `"VideoPlayer.Maui.Tests", "VideoPlayer.Maui.Tests.csproj"`.
- `InternalsVisibleTo` in `VideoPlayer.ApiClient.csproj` und `VideoPlayer.Mvu.csproj`: `VideoWebPlayer.Maui.Tests` → `VideoPlayer.Maui.Tests`.
- `VideoPlayer.App.sln`: Eintrag `VideoWebPlayer.Maui.Tests` → `VideoPlayer.Maui.Tests` (Name + Pfad, GUID `{6FC81FCF-…}` bleibt).
- `pr-checks.yml`: Build-/Test-Pfade `VideoWebPlayer.Maui.Tests/…` → `VideoPlayer.Maui.Tests/…`.
- `README.md`: Test-Kommando und Projektstruktur-Zeile aktualisieren.
- `docs/help/erscheinungsbild.md`: `DesignTokensContractTests` (`VideoWebPlayer.Maui.Tests`) → neuer Name.
- `scripts/iOS-Deployment.ResponseFile.ps1`: Kommentar-Pfad `VideoWebPlayer.Maui.Tests/IosDeploymentResponseFileTests.cs` → `VideoPlayer.Maui.Tests/…`.

### Zu löschende Testdateien

- `VideoWebPlayer.Maui.Tests/ContinueWatchingEventIngressTests.cs` — testet ausschließlich Altbestand-DTOs (`using VideoWebPlayer.Client.Models`)
- `VideoWebPlayer.Maui.Tests/MauiSettingsContractTests.cs` — liest ausschließlich Altbestand-Quellen

### `EnumCoverageTests.cs` (Testklasse)

- **Entfernte Testmethoden:** `RecentEntryType_AllValuesAreCovered`, `LegacyDownloadRetentionType_AllValuesAreCovered`, `LegacyDownloadStatus_AllValuesAreCovered`, `LegacyVideoSourceType_AllValuesAreCovered`, `LegacyCarouselKind_AllValuesAreCovered`
- **Entfernte Hilfsmitglieder:** `Read`, `EnumValuesFromSource`, Feld `Root`
- **Entfernte Usings:** `System.Text.RegularExpressions`, `...Tests.Stubs` (nach Umbenennung)
- **Geänderter Klassen-Doc-Kommentar:** Absatz zum Legacy-Quelltext-Vertrag entfällt
- **Behalten:** `RecentEntryKind`, `PlaybackSourceKind`, `HealthCheckResult`, `PairingStatus` — `using VideoPlayer.ApiClient.Models` bleibt

### `VideoWebPlayer.Maui.Tests.csproj` → `VideoPlayer.Maui.Tests.csproj`

- **Entfernt:** `<ProjectReference Include="..\VideoWebPlayer.Client\VideoWebPlayer.Client.csproj" />`

### `VideoPlayer.App.sln`

- **Entfernt:** `Project`-Einträge `VideoWebPlayer.Client` (`{3512A538-…}`) und `VideoWebPlayer.Maui` (`{FE005A1B-…}`) samt 24 `ProjectConfigurationPlatforms`-Zeilen
- **Geändert:** Eintrag `VideoWebPlayer.Maui.Tests` → `VideoPlayer.Maui.Tests` (`{6FC81FCF-…}` bleibt)

### `.github/workflows/pr-checks.yml`

- **Entfernt:** Step `Build Client` (`VideoWebPlayer.Client`), Step `Build MAUI Windows` (`VideoWebPlayer.Maui`)
- **Geändert:** `Build Tests`/`Test`-Pfade → `VideoPlayer.Maui.Tests`

### `.github/workflows/main-merge.yml` und `.github/workflows/staging-merge.yml`

- **`build-windows` komplett entfernt** (Job enthielt ausschließlich Altbestand-Publish/-Upload)
- **`build-ios`:** `Build iOS` (Altbestand) entfernen; `Upload iOS artifact`: `find`-Pfad `VideoWebPlayer.Maui/bin/…` → `VideoPlayer.Maui/bin/…`; `Build iOS (VideoPlayer.Maui)` bleibt (ggf. in `Build iOS` umbenennen)
- **`needs` in `back-merge` (main-merge) bzw. `draft-pr` (staging-merge):** `[build-windows, build-ios]` → `[build-ios]`

### `.github/scripts/Set-Version.ps1`

- **Entfernt:** `'VideoWebPlayer.Maui/VideoWebPlayer.Maui.csproj'` aus `$csprojPaths` samt Kommentar-Anpassung („die Alt-App solange sie noch existiert" entfällt); `Where-Object Test-Path`-Guard kann bestehen bleiben

### `README.md`

- **Entfernt:** Projektstruktur-Zeile „`VideoWebPlayer.Maui` | Altbestand …"
- **Geändert:** `dotnet test`-Kommando und Zeile `VideoWebPlayer.Maui.Tests` → `VideoPlayer.Maui.Tests` (Beschreibung ggf. präzisieren: „Testprojekt der neuen App")

### `docs/help/app-grundgeruest.md`

- **Geändert:** „Die bislang vorhandene App (`VideoWebPlayer.Maui`) bleibt unverändert bestehen und wird erst später abgelöst." → Der Altbestand ist entfernt; Satz entsprechend neu fassen.

### `docs/help/einstellungen-datenhaltung.md`

- **Geändert:** „(Ausnahmen: Altbestand `VideoWebPlayer.Maui`, Discovery in `VideoPlayer.ApiClient`)" → „(Ausnahme: Discovery in `VideoPlayer.ApiClient`)"

### `VideoPlayer.ApiClient/Models/PlaylistSortModeValues.cs`

- **Geändert:** XML-Doc „spiegelt `VideoWebPlayer.Data.PlaylistSortMode`" → neutral formulieren (z. B. „spiegelt den serverseitigen PlaylistSortMode"), ohne Altbestand-Bezug

### `VideoPlayer.Maui.Tests/LogProtocolContractTests.cs`

- **Geändert:** XML-Doc von `NoDebugWriteLine_RemainsInAppSources` — Satz „Der Altbestand `VideoWebPlayer.Maui` … bleiben ausgenommen" entfällt (Test logikgleich)

## Datenbankmigrationen

Keine.

## Validierungsregeln

Keine.

## Konfigurationsänderungen

Keine.

## Seiteneffekte und Risiken

- **Release-Artefakte:** GitHub-Releases enthalten künftig `release-ios.ipa` (aus `package-ios`, bei aktivierter Signierung) und das iOS-Simulator-`.app.zip` der **neuen** App — keine APK-/MSIX-Artefakte mehr.
- **Testprojekt-Umbenennung:** `InternalsVisibleTo` muss zum neuen Assembly-Namen passen, sonst schlagen `internal`-Tests fehl; alle `using`- und `namespace`-Deklarationen müssen konsistent sein.
- **enum-coverage-check (pre-push `--strict`):** prüft nur noch verbleibende Enums — nach Umbenennung/Löschung verifizieren.
- **CI-Lint (`dotnet format whitespace --verify-no-changes`):** entfernte Usings/Zeilen dürfen keine Whitespace-Verstöße erzeugen.
- **`git mv`:** Dateiumbenennung sollte in einem eigenen Schritt erfolgen, damit die Git-Historie der Testdateien nachvollziehbar bleibt.

## Umsetzungsreihenfolge

1. **Altbestand-Testdateien entfernen/bereinigen**
   - Voraussetzungen: Keine.
   - Beschreibung: `ContinueWatchingEventIngressTests.cs`, `MauiSettingsContractTests.cs` löschen; `EnumCoverageTests.cs` erleichtern.

2. **`ProjectReference` aus Test-`.csproj` entfernen**
   - Voraussetzungen: Schritt 1.
   - Beschreibung: Referenz auf `VideoWebPlayer.Client` löschen.

3. **Testprojekt umbenennen**
   - Voraussetzungen: Keine (kann parallel zu 1–2, aber vor der Solution-Bereinigung sinnvoll).
   - Beschreibung: `git mv` des Verzeichnisses, `.csproj` umbenennen, Namespaces/Usings/XML-Docs in allen Dateien anpassen, `SettingsStorageContractTests`-Pfad, `InternalsVisibleTo` in beiden Bibliotheken, externe Referenzen (README, `erscheinungsbild.md`, ResponseFile-Kommentar, `pr-checks.yml`).

4. **Projektverzeichnisse löschen**
   - Voraussetzungen: Schritte 1–2.
   - Beschreibung: `git rm -r VideoWebPlayer.Maui VideoWebPlayer.Client`.

5. **`VideoPlayer.App.sln` bereinigen**
   - Voraussetzungen: Schritte 3–4.
   - Beschreibung: Altbestand-Einträge + Konfigurationszeilen entfernen; Testprojekt-Eintrag umbenennen.

6. **Workflows und `Set-Version.ps1` bereinigen**
   - Voraussetzungen: Keine.
   - Beschreibung: `pr-checks.yml`, `main-merge.yml`, `staging-merge.yml`, `Set-Version.ps1` gemäß Änderungsliste.

7. **Dokumentation und Kommentare bereinigen**
   - Voraussetzungen: Schritt 3 (für `erscheinungsbild.md` bereits dort erledigt).
   - Beschreibung: `README.md`, `app-grundgeruest.md`, `einstellungen-datenhaltung.md`, `PlaylistSortModeValues.cs`, `LogProtocolContractTests.cs`; `changes.log` folgt im Dokumentationsschritt.

8. **Verifikation**
   - Voraussetzungen: Schritte 1–7.
   - Beschreibung: `dotnet restore`/`build`/`test` des Testprojekts, `dotnet format whitespace --verify-no-changes`, Repo-Suche nach `VideoWebPlayer` (erlaubt: `docs/features/`, `docs/projects/`, `changes.log`, `VIDEOWEBPLAYER_MAUI_API_TOKEN`).

## Tests

### Neue Tests

Keine — reine Entfernung/Umbenennung, keine neue Funktionalität.

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `ContinueWatchingEventIngressTests` | Datei löschen — testet nur Altbestand-DTOs |
| `MauiSettingsContractTests` | Datei löschen — liest nur Altbestand-Quellen |
| `EnumCoverageTests` | Legacy-Testfälle + ungenutzte Hilfsmitglieder/Usings entfernen |
| `SettingsStorageContractTests.StorageProject_IsReferencedEverywhere` | Pfad der Test-`.csproj` an Umbenennung anpassen |
| Alle Testdateien | `namespace`/`using`-Deklarationen an neue Assembly anpassen |
| `LogProtocolContractTests.NoDebugWriteLine_RemainsInAppSources` | Nur XML-Doc-Kommentar anpassen |

### E2E-Tests (Pflicht)

Keine neuen oder geänderten Benutzerinteraktionen → keine neuen E2E-Tests. `VideoPlayer.E2E.Tests` ist unbetroffen.

## Offene Punkte

Keine.
