# Testergebnisse

## Ergebnis

**Status:** Alle Tests bestanden

## Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| Baseline Unit (vor Umsetzung) | `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj --nologo -v q` | Repo-Stamm | 0 | 1079 | 0 | 0 | Konsolenausgabe (Dauer 11 s, Commit `8d9d7b6`) |
| Unit (nach Umsetzung) | `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj --no-build --nologo -v q` | Repo-Stamm | 0 | 1104 | 0 | 0 | Konsolenausgabe |
| Unit + Coverage (final) | `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj --no-build --nologo --collect:"XPlat Code Coverage" --logger "console;verbosity=normal"` | Repo-Stamm | 0 | 1104 | 0 | 0 | `inventory/test-results/coverage-unittests.cobertura.xml` |
| E2E gefiltert (neu) | `dotnet test VideoPlayer.E2E.Tests --no-build --filter "FullyQualifiedName~ContinueWatchingContextMenuTests"` | Repo-Stamm | 0 | 3 | 0 | 0 | Konsolenausgabe |
| E2E vollständig (final) | `dotnet test VideoPlayer.E2E.Tests --no-build --nologo --logger "console;verbosity=normal"` | Repo-Stamm | 0 | 25 | 0 | 0 | Konsolenausgabe (4,6 min) |

## Testumgebung

- SDK/Runtime: `dotnet` 10.0.401, xUnit (Unit `net10.0`, E2E `net10.0-windows` + FlaUI/UIA3, Debug-App-Build `net10.0-windows10.0.19041.0`)
- Branch/Commit der Umsetzung: `task/issue-43-…-kontextmenue` (Arbeitsstand nach Planungscommit `c218b5c`)
- Builds: `dotnet build VideoWebPlayer.Maui.Tests` (0 Fehler), `dotnet build VideoPlayer.Maui -f net10.0-windows10.0.19041.0 -c Debug` (0 Fehler; 1 vorab vorhandene Obsolete-Warnung `DisplayAlert` in `ServerSetupPage.xaml.cs`), `dotnet build VideoPlayer.E2E.Tests` (0 Fehler)

## Coverage (XPlat Code Coverage, Unit-Tests)

- Gesamt-Zeilenabdeckung: **86,0 %** (line-rate 0.8601)
- `VideoPlayer.Mvu/ContinueWatchingMutationService.cs`: **100 % Zeilen** (Zweige 50–100 % je Methode; unabgedeckte Restzweige sind Fehlerpfade)
- `VideoPlayer.Mvu/AppUpdate.cs`: 97,2 % Zeilen / 90,6 % Zweige
- `VideoPlayer.Mvu/AppMessage.cs` / `AppEffect.cs`: 100 % (Records)

## Neue Tests dieser Anforderung

### Unit-/Contract-Tests (VideoWebPlayer.Maui.Tests)

| Test | Zweck | Ergebnis |
|------|-------|----------|
| `AppUpdateTests_ContinueWatchingActions` (7 Tests) | Hide/Skip-Effekte, Typweitergabe, Guards | Bestanden |
| `ContinueWatchingMutationServiceTests` (6 Tests) | Online hide/skip + Cache-Resync, Offline-Entfernen, Fehlfälle | Bestanden |
| `ContinueWatchingContextMenuContractTests` (7 Tests) | Verdrahtung, Menü-Texte, Beschränkung, DevApi, Handler | Bestanden |

### E2E-Tests (VideoPlayer.E2E.Tests)

| Test | Szenario | Ergebnis |
|------|----------|----------|
| `LongPressMovie_ChooseHide_CallsHideEndpoint_EntryDisappears` | Long-Press → Menü (beide Aktionen sichtbar) → „Ausblenden" → `POST /api/continue-watching/hide` → Eintrag verschwindet | Bestanden |
| `LongPressMovie_ChooseSkip_CallsSkipEndpoint_EntryDisappears` | Long-Press → „Überspringen" → `POST /api/continue-watching/skip` → Eintrag verschwindet | Bestanden |
| `LongPressMovie_Cancel_KeepsEntry_NoMutationRequests` | Long-Press → „Abbrechen" → keine hide/skip-Requests, Eintrag bleibt | Bestanden |

### Regressionsnachweis kurzer Tap (bestehende Tests, blieben grün)

- `ContinueWatchingPlaybackTests.TapContinueWatchingMovie_StartsPlaybackAtResume_WithoutDetails`
- `ContinueWatchingPlaybackTests.TapContinueWatchingEpisode_StartsPlaybackAndAdvancesToNextEpisode`
- `ContinueWatchingPlaybackTests.TapContinueWatchingShowEntry_OpensDetails_NotPlayback`

## Nicht ausgeführte / eingeschränkte Prüfungen

- **E2E-Baseline vor der Umsetzung:** nicht abgeschlossen — der erste Baseline-Lauf wurde nach über 45 Minuten ohne Ergebnis abgebrochen (kein laufender Testhost-Prozess mehr). Die finale Suite (25 Tests) lief anschließend mit dem umgesetzten Stand vollständig grün; preexisting-Fehler werden nicht behauptet.
- **Offline-Mutation im E2E:** bewusst nicht e2e-geführt (kein E2E-Offline-Szenario in der Suite vorhanden); der Offline-Pfad ist über `ContinueWatchingMutationServiceTests.HideAsync_Offline_*`/`SkipAsync_Offline_*` hermetisch abgesichert.
- **Physische 2-s-Halteeingabe:** nicht automatisierbar (UIA-Lücke der BindableLayout-Einträge, projekteinheitliche DevApi-Auslösung per `gesture: "longpress"` auf derselben Ereigniskette; die 2-s-Konstante und die Pointer-Verdrahtung sind contract-gepinnt).

## Iterationen

- **Iteration 1 (Code-Review):** 3 Befunde behoben — gemeinsame E2E-Helfer nach `E2ETestBase` gehoben, Guard-Bedingung in `AppUpdate.IsSelectableEntry` zentralisiert, redundante Bedingung in `UiDriver.ClickByName` entfernt; danach `DisplayActionSheet` → `DisplayActionSheetAsync` (CS0618). Alle Läufe danach wiederholt: Unit 1104/1104, E2E 25/25.
