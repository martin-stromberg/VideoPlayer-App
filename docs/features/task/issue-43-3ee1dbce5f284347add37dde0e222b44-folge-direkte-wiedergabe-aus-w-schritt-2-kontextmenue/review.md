# Plan-Review

## Ergebnis

**Status:** Vollständig umgesetzt

## Umgesetzte Planelemente

- [x] `ContinueWatchingHideRequestedMessage` (AppMessage-Record) — angelegt (`VideoPlayer.Mvu/AppMessage.cs:268`)
- [x] `ContinueWatchingSkipRequestedMessage` (AppMessage-Record) — angelegt (`VideoPlayer.Mvu/AppMessage.cs:278`)
- [x] `HideContinueWatchingEntryEffect` (AppEffect-Record) — angelegt (`VideoPlayer.Mvu/AppEffect.cs:145`)
- [x] `SkipContinueWatchingEntryEffect` (AppEffect-Record) — angelegt (`VideoPlayer.Mvu/AppEffect.cs:159`)
- [x] `ContinueWatchingMutationService` (Klasse, VideoPlayer.Mvu) — angelegt mit `HideAsync`/`SkipAsync`/`MutateAsync`/`RemoveCachedEntryAsync` (`VideoPlayer.Mvu/ContinueWatchingMutationService.cs`)
- [x] `AppUpdate`-Switch-Zweige + `UpdateContinueWatchingHideRequested`/`UpdateContinueWatchingSkipRequested` mit Guards — vorhanden (`VideoPlayer.Mvu/AppUpdate.cs:128-129, 1265-1300`)
- [x] `ShellAppEffectHandler`-Zweige `HideContinueWatchingEntryEffect`/`SkipContinueWatchingEntryEffect` + `MutateContinueWatchingAsync` — vorhanden (`ShellAppEffectHandler.cs:151-156, 430-449`)
- [x] `AddSingleton<ContinueWatchingMutationService>` — vorhanden (`MauiProgram.cs:109`)
- [x] `MediaCarousel`: `LongPressEnabled`-BindableProperty (Default false), `ItemLongPressed`-Event, `OnItemPointerPressed`/`OnItemPointerEnded`, 2-s-`IDispatcherTimer` (`LongPressDurationMs = 2000`), `SendItemLongPressed(int)`, `PointerGestureRecognizer` in der Karten-`Border` — vorhanden (`MediaCarousel.xaml:46-48`, `MediaCarousel.xaml.cs:42-47, 94-104, 124-194`)
- [x] `HomePage`: `LongPressEnabled="True"` + `ItemLongPressed="OnContinueWatchingItemLongPressed"` nur am Weiterschauen-Karussell; `OnContinueWatchingItemLongPressed` mit `DisplayActionSheet` („Abbrechen", „Ausblenden", „Überspringen") + Dispatch der Request-Nachrichten + `IsSelectable`-Guard — vorhanden (`HomePage.xaml:156-157`, `HomePage.xaml.cs:117-140`)
- [x] `MauiDevApiService.InvokeBody.Gesture` + Übergabe an `DevApiVisualTree.TrySelectItem` — vorhanden (`MauiDevApiService.cs:86, 102-126, 172-173`)
- [x] `DevApiVisualTree.TrySelectItem(element, itemIndex, gesture, out error)` + `TryLongPressItem` (SendItemLongPressed-Reflection) + Fehler bei unbekannter Geste — vorhanden (`DevApiVisualTree.cs:159-172, 220-245`)
- [x] `MockVideoServer`: `POST /api/continue-watching/hide`/`skip`-Routen (Gate+Bearer), `HideContinueWatchingCount`/`SkipContinueWatchingCount`, `RemovedContinueWatchingKeys`, `IsRemoved`-Filter in `ListOverride`, `RecordRemoval`/`ReadLongProperty` — vorhanden (`MockVideoServer.cs:173-184, 338-370, 509-561, 591-640`)
- [x] `UiDriver.ClickByName` (Fenster + prozessbezogene Desktop-Popups, Retry) — vorhanden (`UiDriver.cs:222-255`)
- [x] `DevApiClient.LongPressAsync` — vorhanden (`DevApiClient.cs:73-81`)
- [x] `AppUpdateTests_ContinueWatchingActions` — angelegt (7 Tests)
- [x] `ContinueWatchingMutationServiceTests` — angelegt (6 Tests)
- [x] `ContinueWatchingContextMenuContractTests` — angelegt (7 Tests)
- [x] `ContinueWatchingContextMenuTests` (E2E: Hide, Skip, Abbrechen) — angelegt (3 Tests)

## Hinweise

- Der Tap-Regressionstest (`ContinueWatchingPlaybackTests.TapContinueWatchingMovie_StartsPlaybackAtResume_WithoutDetails`) bleibt unverändert bestehen und muss im E2E-Lauf grün bleiben.
- `DisplayActionSheet` benötigt kein `destruction`-Argument (übergeben `null`) — korrekt.
