# Tasks: Kontextmenü in der Weiterschauen-Liste (Ausblenden / Überspringen)

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | MVU | `ContinueWatchingHideRequestedMessage` + `ContinueWatchingSkipRequestedMessage` in `AppMessage.cs` anlegen | Erledigt | `AppUpdateTests_ContinueWatchingActions.*` |
| 2 | MVU | `HideContinueWatchingEntryEffect` + `SkipContinueWatchingEntryEffect` in `AppEffect.cs` anlegen | Erledigt | `AppUpdateTests_ContinueWatchingActions.*` |
| 3 | MVU | `ContinueWatchingMutationService` neu anlegen (Hide/Skip + Offline-Cache-Entfernen) | Erledigt | `ContinueWatchingMutationServiceTests.*` |
| 4 | MVU | `AppUpdate`: Switch-Zweige + Guards für beide Request-Nachrichten | Erledigt | `AppUpdateTests_ContinueWatchingActions.*` |
| 5 | App-Rand | `ShellAppEffectHandler`: `HideContinueWatchingEntryEffect`/`SkipContinueWatchingEntryEffect`-Zweige + `CachedListsChangedMessage` bei Erfolg | Erledigt | `ContinueWatchingContextMenuContractTests.ShellAppEffectHandler_HandlesHideAndSkipEffects` |
| 6 | App-Rand | `MauiProgram`: `ContinueWatchingMutationService` als Singleton registrieren | Erledigt | `ContinueWatchingContextMenuContractTests.ContinueWatchingMutationService_UsesApiEndpoints_AndIsRegistered` |
| 7 | UI | `MediaCarousel`: `LongPressEnabled`-Property, `ItemLongPressed`-Event, Pointer-2-s-Timer, `SendItemLongPressed` | Erledigt | `ContinueWatchingContextMenuContractTests.MediaCarousel_*` |
| 8 | UI | `HomePage`: `LongPressEnabled`+`ItemLongPressed` nur am Weiterschauen-Karussell, `DisplayActionSheet` (Ausblenden/Überspringen/Abbrechen), Dispatch der Nachrichten | Erledigt | `ContinueWatchingContextMenuContractTests.HomePage_*` |
| 9 | DevApi | `MauiDevApiService`/`DevApiVisualTree`: optionales `gesture`-Feld und Long-Press-Auslösung via `SendItemLongPressed` | Erledigt | `ContinueWatchingContextMenuContractTests.DevApi_Invoke_SupportsLongPressGesture` |
| 10 | E2E-Infrastruktur | `MockVideoServer`: hide/skip-Routen + Entfernen aus Listenantwort + Diagnose | Erledigt | `ContinueWatchingContextMenuTests.*` (E2E) |
| 11 | E2E-Infrastruktur | `DevApiClient.LongPressAsync` + `UiDriver.ClickByName` | Erledigt | `ContinueWatchingContextMenuTests.*` (E2E) |
| 12 | Tests | `AppUpdateTests_ContinueWatchingActions` (Hide/Skip-Effekte + Guards) | Erledigt | Testlauf VideoWebPlayer.Maui.Tests (1104 grün) |
| 13 | Tests | `ContinueWatchingMutationServiceTests` (Online hide/skip + Resync, Offline-Entfernen, Fehlfall) | Erledigt | Testlauf VideoWebPlayer.Maui.Tests (1104 grün) |
| 14 | Tests | `ContinueWatchingContextMenuContractTests` (Verdrahtung, Beschränkung, Texte) | Erledigt | Testlauf VideoWebPlayer.Maui.Tests (1104 grün) |
| 15 | E2E-Tests | `ContinueWatchingContextMenuTests` (Hide, Skip, Abbrechen) | Erledigt | E2E-Lauf (steht in Schritt 10 aus) |
