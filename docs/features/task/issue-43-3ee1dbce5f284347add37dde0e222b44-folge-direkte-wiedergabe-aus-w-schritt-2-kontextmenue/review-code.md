# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### `ContinueWatchingContextMenuTests` (VideoPlayer.E2E.Tests/Tests)

- **Doppelter Code** — Die privaten Helfer `LoginOnHomeAsync` und `ContinueWatchingIndexAsync` sind nahezu wortgleiche Kopien der gleichnamigen Helfer aus `ContinueWatchingPlaybackTests` (Setup-Fluss bis zur befüllten Startseite bzw. Index-Suche im Karussell-Tree). Jede inhaltliche Änderung am Setup-Fluss müsste an mehreren Stellen gepflegt werden.

  Empfehlung: Beide Helfer als `protected static`-Methoden in `E2ETestBase` heben (parametrisiert über den erwarteten Eintragstitel) und die Kopien in beiden Testklassen entfernen.

### `AppUpdate` (VideoPlayer.Mvu)

- **Doppelter Code (leicht)** — `UpdateContinueWatchingHideRequested` und `UpdateContinueWatchingSkipRequested` wiederholen denselben Guard (`Id <= 0 || MediaType leer || !IsSelectable` → `Unchanged`) plus den identischen Übergangsrahmen; nur der Effekttyp unterscheidet sich.

  Empfehlung: Die Guard-Bedingung in eine gemeinsame kleine Hilfsfunktion auslagern (z. B. `IsMutatableContinueWatchingItem(item)`), sodass die Semantik „was ist ein mutierbarer Eintrag" nur einmal beschrieben ist. (Alternativ mit der Guard-Prüfung der Auswahl-Funktion teilen.)

### `UiDriver.ClickByName` (VideoPlayer.E2E.Tests/Support)

- **Redundante Bedingung (leicht)** — `e.Properties.ProcessId.ValueOrDefault == processId && e.Properties.ProcessId.ValueOrDefault != 0` prüft zweimal denselben Wert; `processId` stammt aus dem Fenster und ist nie 0, der zweite Teil ist damit impliziert.

  Empfehlung: Auf `== processId` reduzieren (lesbarer, selbes Verhalten).

## Nachbearbeitung (Iteration 1)

Alle drei Befunde wurden umgesetzt:

- `LoginOnHomeAsync`/`ContinueWatchingIndexAsync` als `protected static`-Methoden nach `E2ETestBase` gehoben; Kopien in `ContinueWatchingPlaybackTests` und `ContinueWatchingContextMenuTests` entfernt.
- Guard-Bedingung in `AppUpdate.IsSelectableEntry` zentralisiert; `UpdateContinueWatchingItemSelected` nutzt sie ebenfalls (identische Semantik).
- `UiDriver.ClickByName`: redundante `!= 0`-Prüfung entfernt.

Zusätzlich wurde die CS0618-Warnung behoben: `DisplayActionSheet` → `DisplayActionSheetAsync` in `HomePage.xaml.cs`. Verifikation: Unit 1104/1104, E2E 25/25, App-Build fehlerfrei.

## Geprüfte Dateien

Liste aller geprüften Dateien (Basisbranch `task/issue-43-3ee1dbce5f284347add37dde0e222b44-folge-direkte-wiedergabe-aus-w`):

- `VideoPlayer.Mvu/AppMessage.cs`
- `VideoPlayer.Mvu/AppEffect.cs`
- `VideoPlayer.Mvu/AppUpdate.cs`
- `VideoPlayer.Mvu/ContinueWatchingMutationService.cs`
- `VideoPlayer.Maui/Services/ShellAppEffectHandler.cs`
- `VideoPlayer.Maui/MauiProgram.cs`
- `VideoPlayer.Maui/Controls/MediaCarousel.xaml`
- `VideoPlayer.Maui/Controls/MediaCarousel.xaml.cs`
- `VideoPlayer.Maui/Pages/HomePage.xaml`
- `VideoPlayer.Maui/Pages/HomePage.xaml.cs`
- `msTools.DeveloperApi.Maui/MauiDevApiService.cs`
- `msTools.DeveloperApi.Maui/DevApiVisualTree.cs`
- `VideoPlayer.E2E.Tests/Support/MockVideoServer.cs`
- `VideoPlayer.E2E.Tests/Support/DevApiClient.cs`
- `VideoPlayer.E2E.Tests/Support/UiDriver.cs`
- `VideoPlayer.E2E.Tests/Tests/ContinueWatchingContextMenuTests.cs`
- `VideoWebPlayer.Maui.Tests/AppUpdateTests_ContinueWatchingActions.cs`
- `VideoWebPlayer.Maui.Tests/ContinueWatchingMutationServiceTests.cs`
- `VideoWebPlayer.Maui.Tests/ContinueWatchingContextMenuContractTests.cs`
