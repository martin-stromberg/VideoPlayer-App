# tvOS-App-Gerüst — Bestandsaufnahme

## Verdrahtungskette

`AppDelegate.FinishedLaunching` → `AppComposition.Create()` → `CreateRuntime(MainViewController)` → `TvosNavigator` (Attach(main)) + `TvosAppEffectHandler` + `MvuHost`; `ModelChanged` → `MainThreadInvoker.InvokeOnMainThread(() => navigator.Render(model))` (AppComposition.cs:207).

## `TvosNavigator` (`Ui/TvosNavigator.cs`)

- `NavigateTo(AppPage)`: `Loading` → `main.ShowLoading`; `ServerSetup`/`Login` → `main.Show(workflow-VC)`; Menüseiten → `tabBar.Select(page)` + `main.Show(tabBar)` (idempotent).
- `Render(model)`: `NavigateTo(model.CurrentPage)` + `UpdateLoadingStatus` + `Render` der Workflow-Seiten + `tabBar.Render(model)`. **Wichtig:** jeder Modellwechsel ruft `main.Show(tabBar)` erneut auf — ein per `Show` eingeschobener Platzhalter würde beim nächsten Render verdrängt. Modale Präsentation (`PresentViewController`) umgeht das.
- `dispatch`: `Func<AppMessage, Task>` → `Host.DispatchAsync` auf Hauptthread.
- `LastMenuPage` merkt die letzte Menüseite als Rückziel der Workflow-Seiten.

## `MenuTabBarController` (`Ui/MenuTabBarController.cs`)

- `UITabBarController`; `ViewDidLoad` baut Tabs aus `AppRoutes.MenuPages`; `ViewControllerSelected` → `NavigatedMessage`.
- `CreatePageController`: `Profile`/`Settings` eigene VCs, Rest `PagePlaceholderViewController` — hier ist der Home-Slot zu ersetzen.
- `Render(model)` verteilt an `IModelRenderable`-Kinder.
- Konstruktor heute: `(Func<AppMessage, Task> dispatch)` — Erweiterung um Poster-Dienst nötig (Erstellung in `TvosNavigator.NavigateTo`, `tabBar ??=`).

## `MainViewController` (`MainViewController.cs`)

- Container: `Show(UIViewController)` ersetzt das eingebettete Kind (Add/Remove-Muster); `ShowLoading`, `UpdateLoadingStatus`. Root des `UIWindow` — `PresentViewController` hierauf präsentiert über allem.

## `TvosAppEffectHandler` (`Services/TvosAppEffectHandler.cs`)

- `switch` über `AppEffect`-Subtypen; verdrahtet u. a. `LoadHomeListsEffect`, Teil-Syncs, `NavigateToPageEffect`, `LogoutEffect`; `default:` → `LogDebug` No-Op.
- Unverdrahtet: `NavigateToMediaDetailsEffect`, `NavigateToPlayerEffect`, `StartPlaylistPlaybackEffect`, `ResolvePlaybackSourceEffect`, `LoadMediaDetailsEffect`, `LoadUpcomingEpisodesEffect`, alle Download-/Mutations-Effekte.
- Rückmeldungen laufen über `DispatchMessageAsync` (MainThreadInvoker) — dient der Seite nicht direkt.

## `AppComposition` (`AppComposition.cs`)

- Manuelle Singleton-Verdrahtung; `HomeLists` (`HomeListService`), `OfflineContinueWatching`, `ServerSync`, `AuthSession` (Client für Bildabrufe), `StorageLayout` (inkl. `PreviewsDirectory`) vorhanden; `CreateRuntime` erzeugt `TvosNavigator(dispatch)` — Erweiterungspunkt für den Poster-Dienst.

## Threading / Lebenszyklus

- `MainThreadInvoker.InvokeOnMainThread(Action)` / `InvokeOnMainThreadAsync(Func<Task>)` — UI-Zugriffe nur auf Hauptthread; `Render` kommt bereits auf dem Hauptthread.
- `AppDelegate.OnActivated` dispatcht `HomeListsReloadRequestedMessage` (Vordergrund-Rückkehr).
- `PressesBegan(NSSet<UIPress>, UIPressesEvent)` + `UIPressType.Menu` = Menü-Taste der Siri Remote (Muster: `ServerSetupViewController`, Zeilen 223–237).

## UI-Konventionen

- `TvosControls`: `Title`/`Heading`/`Body`/`Status`/`Error`/`TextField`/`Button`/`Section` (vertikale `UIStackView`); schwarzer Hintergrund, große Schriften (26–64 pt), 90 pt Insets.
- `PagePlaceholderViewController`: Referenz für schlichte Platzhalter-Seiten.
