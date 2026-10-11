# Tasks: Kontextmenü in der Weiterschauen-Liste (Ausblenden / Überspringen)

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | MVU | `ContinueWatchingHideRequestedMessage` + `ContinueWatchingSkipRequestedMessage` in `AppMessage.cs` anlegen | Offen | – |
| 2 | MVU | `HideContinueWatchingEntryEffect` + `SkipContinueWatchingEntryEffect` in `AppEffect.cs` anlegen | Offen | – |
| 3 | MVU | `ContinueWatchingMutationService` neu anlegen (Hide/Skip + Offline-Cache-Entfernen) | Offen | – |
| 4 | MVU | `AppUpdate`: Switch-Zweige + Guards für beide Request-Nachrichten | Offen | – |
| 5 | App-Rand | `ShellAppEffectHandler`: `HideContinueWatchingEntryEffect`/`SkipContinueWatchingEntryEffect`-Zweige + `CachedListsChangedMessage` bei Erfolg | Offen | – |
| 6 | App-Rand | `MauiProgram`: `ContinueWatchingMutationService` als Singleton registrieren | Offen | – |
| 7 | UI | `MediaCarousel`: `LongPressEnabled`-Property, `ItemLongPressed`-Event, Pointer-2-s-Timer, `SendItemLongPressed` | Offen | – |
| 8 | UI | `HomePage`: `LongPressEnabled`+`ItemLongPressed` nur am Weiterschauen-Karussell, `DisplayActionSheet` (Ausblenden/Überspringen/Abbrechen), Dispatch der Nachrichten | Offen | – |
| 9 | DevApi | `MauiDevApiService`/`DevApiVisualTree`: optionales `gesture`-Feld und Long-Press-Auslösung via `SendItemLongPressed` | Offen | – |
| 10 | E2E-Infrastruktur | `MockVideoServer`: hide/skip-Routen + Entfernen aus Listenantwort + Diagnose | Offen | – |
| 11 | E2E-Infrastruktur | `DevApiClient.LongPressAsync` + `UiDriver.ClickByName` | Offen | – |
| 12 | Tests | `AppUpdateTests_ContinueWatchingActions` (Hide/Skip-Effekte + Guards) | Offen | – |
| 13 | Tests | `ContinueWatchingMutationServiceTests` (Online hide/skip + Resync, Offline-Entfernen, Fehlfall) | Offen | – |
| 14 | Tests | `ContinueWatchingContextMenuContractTests` (Verdrahtung, Beschränkung, Texte) | Offen | – |
| 15 | E2E-Tests | `ContinueWatchingContextMenuTests` (Hide, Skip, Abbrechen) | Offen | – |
