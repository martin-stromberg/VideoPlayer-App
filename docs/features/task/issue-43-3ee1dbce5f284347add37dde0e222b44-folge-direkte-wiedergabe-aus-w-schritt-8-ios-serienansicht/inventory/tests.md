# Tests — Serienansicht

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-10-11 ~19:45 MESZ (UTC+02:00)
- Branch und Commit-ID: `task/issue-43-3ee1dbce5f284347add37dde0e222b44-folge-direkte-wiedergabe-aus-w-schritt-8-ios-serienansicht` @ `3ec07b8a8d11a73f3d5560fbd73e66099a2f326f`
- Uncommittete Änderungen im getesteten Stand: nur untracked Lifecycle-Artefakte unter `docs/features/task/…-schritt-8-ios-serienansicht/` — kein Produktivcode geändert
- Testumgebung und Runtime-/SDK-Versionen: Windows, .NET SDK 10.0.401; xUnit + FluentAssertions
- Ermittelte Testsuiten und Quellen der Testbefehle: `README.md` („Bauen und Testen": `dotnet test VideoWebPlayer.Maui.Tests`), `docs/help/e2e-tests.md` (`dotnet test VideoPlayer.E2E.Tests`, FlaUI/Windows)

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| Baseline Unit | `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj --logger "console;verbosity=normal"` | Repo-Stamm | 0 | 1285 | 0 | 0 | [baseline-unittests.log](test-results/baseline-unittests.log) |

### Nachgewiesene bestehende Testfehler

Keine — der Baseline-Lauf ist fehlerfrei durchgelaufen (1285/1285).

### Testlücken und Ausführungsprobleme

- **E2E-Suite nicht im Ausgangslauf ausgeführt:** `VideoPlayer.E2E.Tests` (FlaUI/Windows, laut `docs/help/e2e-tests.md` ~20–25 min, 19 Tests) wurde aus Zeitgründen nicht als Baseline gefahren. Für die betroffene Serienansicht existiert bislang **kein** E2E-Test; der einzige an der Detailseite angrenzende E2E-Test ist `ContinueWatchingPlaybackTests.TapContinueWatchingShowEntry_OpensDetails_NotPlayback`. Der Nachweis „unverändert korrektes Windows-Verhalten" erfolgt über den geplanten neuen E2E-Test plus Wiederholung angrenzender Tests nach der Umsetzung.
- **iOS-Lauf nicht möglich:** `scripts/iOS-Deployment.ps1` erfordert macOS/Xcode (Pair-to-Mac); der iOS-Fehler ist auf dieser Maschine nicht reproduzier- oder verifizierbar.
- **Kein Unit-Test der Seitenlogik:** `MediaDetailsPage` (MAUI-Code-Behind) ist im Testprojekt nicht instanziierbar; die Verdrahtung wird über `MediaDetailsPageContractTests` (Quelltext-Contract-Tests) abgesichert.

## Testklassen

### `MediaDetailsPageContractTests`
- `MediaDetailsPage_FullWidthHeader_WithGradients` — Header-Struktur inkl. `SeasonSection`-Reihenfolge
- `MediaDetailsPage_HeaderOverlaysContentAndActions` — pinnt u. a. die Strings `SeasonPicker` und `EpisodesCollection` im XAML (Z. 67–68) — **betroffen** beim Ersetzen der `CollectionView`
- `MediaDetailsPage_Reappearing_RequestsOwnIdentityAfterForeignDetails` — Re-Anforderung der eigenen Identität
- `MediaDetailsPage_CodeBehind_HeaderBindingAndResponsiveHeight` — Header-Bindings und Höhen-Clamp
- `MediaDetailsPage_EpisodeWatchedBadge_BindsIsWatched` / `…_FollowsWebDesign` — Watched-Badge im Episoden-Template (`x:DataType="mvu:EpisodeItem"`, `AutomationId="WatchedIndicator"`)
- `EpisodeItem_CarriesWatchedState`, `MvuCore_CarriesHeaderImageStateAndMessages`, `MediaDetailsView_CarriesHeaderIdentityAndTexts`, `ShellAppEffectHandler_HandlesDetailsHeaderLoadEffect`, `ApiClient_EpisodeStillEndpoint_UsesSendPipeline`

### `AppUpdateTests_MediaDetails` (VideoWebPlayer.Maui.Tests)
- Tests des Detail-/Staffel-Update-Pfads, u. a. `SeasonSelectedMessage`-Verhalten (`UpdateSeasonSelected`)

### `MediaDetailsServiceTests` (VideoWebPlayer.Maui.Tests)
- Tests der Detail-Mappings inkl. Serien-/Staffelstruktur

### `ContinueWatchingPlaybackTests` (VideoPlayer.E2E.Tests)
- `TapContinueWatchingShowEntry_OpensDetails_NotPlayback` — Tap auf Weiterschauen-`show`-Eintrag öffnet die Serien-Detailseite (`HeaderSection` sichtbar, `api/items/tvshow/8301` abgerufen)

## Hilfsmethoden / Testinfrastruktur

### `MockVideoServer` (VideoPlayer.E2E.Tests/Support)
- `ContinueWatchingShowId`/`ContinueWatchingShowTitle` — Serien-Weiterschauen-Eintrag (Serien-Detailseite ohne Staffeln)
- `PlaybackShowId`/`PlaybackEpisodeId`/`PlaybackNextEpisodeId` — Serienfluss mit 1 Staffel + 2 Episoden
- `WasPathRequested` — Request-Protokoll für Negativ-/Pfadnachweise

### `DevApiClient` (VideoPlayer.E2E.Tests/Support)
- `WaitForModelAsync`/`GetModelAsync` — Modell-Snapshot (`GET /model`, aktuell ohne Serienansicht-Felder)
- `GetTreeAsync` — UIA-/Elementbaum; `FindByAutomationId` liefert Knoten mit `Items` (auch `BindableLayout`-Container)
- `InvokeAsync(automationId, itemIndex)` — löst Eintragsaktion auf `CollectionView`- und `BindableLayout`-Containern aus

### `UiDriver` (VideoPlayer.E2E.Tests/Support)
- `FindByAutomationId`, `Exists`, `WaitForVisible`, `ClickButton`, `ClickByName`, `EnterText`, `Toggle`, `OpenFlyoutItem`, `DumpTree`
