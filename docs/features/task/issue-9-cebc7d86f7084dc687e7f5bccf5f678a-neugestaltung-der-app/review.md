# Plan-Review

## Ergebnis

**Status:** Vollständig umgesetzt

Geprüft gegen die uncommittete Implementierung im Arbeitsverzeichnis
(`git status`/`git diff`). Nachweis: `dotnet build VideoPlayer.App.sln`
(Debug): 0 Warnungen/0 Fehler; `dotnet test
VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj`: 635/635
Tests grün.

## Umgesetzte Planelemente

### `AppUpdate` (`VideoPlayer.Mvu/AppUpdate.cs`)

- [x] `EnterConnected` — `SourcesOverview` mit `IsLoading = true`/`Error = null`
  unter Beibehaltung der Einträge; `LoadSourcesEffect` als erster Effekt
  (`new List<AppEffect> { new LoadSourcesEffect() }`, ca. Z. 450–453)
- [x] `UpdateSynchronizationCompleted` — bei `Connected && !SourcesOverview.IsLoading`
  `IsLoading = true` + `LoadSourcesEffect` zusätzlich zu `LoadHomeListsEffect` (ca. Z. 532–556)
- [x] `UpdateCachedListsChanged` — gleiche Bedingung, `IsLoading = true` +
  `LoadSourcesEffect` (ca. Z. 558–580)
- [x] `UpdateNetworkAvailabilityChanged` — im Übergang `Connected → Offline`
  `SourcesOverview = SourcesOverviewState.Initial` (ca. Z. 489–499)
- [x] `UpdateSessionExpired` — `SourcesOverview = Initial` und
  `SourceBrowser = SourceBrowserState.Empty` (ca. Z. 621–630)
- [x] `UpdateServerCheckCompleted` — im Erfolgspfad `SourcesOverview = Initial`
  und `SourceBrowser = Empty` (ca. Z. 297–303)
- [x] `UpdateSourcesLoaded` — Guard `ConnectionState != Connected` →
  `AppTransition.Unchanged` (ca. Z. 694–704)
- [x] `UpdateSourcesPageOpened` entfernt; Dispatch-Case
  `SourcesPageOpenedMessage` aus `Update` entfernt

### `AppMessage` / `AppPage` / `AppRoutes` / `AppModel` / `SourcesOverviewState`

- [x] `SourcesPageOpenedMessage` aus `AppMessage.cs` entfernt
- [x] Enum-Wert `AppPage.Sources` entfernt (`AppPage.cs`)
- [x] `AppRoutes.Routes`: `[AppPage.Sources] = "sources"` entfernt;
  `MenuPages` auf fünf Einträge reduziert (`AppRoutes.cs`)
- [x] XML-Doku `AppModel.SourcesOverview` auf Menü-Rolle angepasst
- [x] XML-Doku `SourcesOverviewState` angepasst (zustandsgesteuertes Laden,
  Offline-Reset); keine strukturelle Änderung

### `AppShell` (`VideoPlayer.Maui/AppShell.xaml`, `AppShell.xaml.cs`)

- [x] XAML: statischer `FlyoutItem` „Quellen" entfernt — fünf statische
  `FlyoutItem`s verbleiben
- [x] Konstruktor `AppShell(MvuHost, PosterImageService)` mit
  `mvuHost.ModelChanged += OnModelChanged` (Z. 25–55)
- [x] Felder `sourceMenuItems` (getrackte `ShellItem`s) und
  `renderedSources` (Änderungsvergleich, Z. 19–20)
- [x] `OnModelChanged` — `MainThread.BeginInvokeOnMainThread` +
  Vergleich via `SameSources` über `Id`/`Name`/`IconPictureId` (Z. 79–108)
- [x] `RemoveSourceMenuItems`/`InsertSourceMenuItems` — Entfernen getrackter
  Einträge, Einfügen ab Index 1: deaktivierte Überschrift
  `MenuItem { Text = "Quellen", IsEnabled = false }` + je Quelle
  `MenuItem { Text = source.Name }` mit `Clicked` →
  `SourceSelectedMessage` (Z. 110–150)
- [x] `LoadSourceIconAsync` — `GetSourceIconSourceAsync(pictureId)` bei
  `HasIcon`, `IconImageSource` auf dem Main-Thread, `null`/Fehler →
  Eintrag ohne Icon (Z. 164–180)

### Entfernte Infrastruktur

- [x] `Pages/SourcesPage.xaml` und `Pages/SourcesPage.xaml.cs` gelöscht
- [x] `Controls/CachedSourceIconImage.cs` gelöscht
- [x] `MauiProgram.cs`: `AddTransient<Pages.SourcesPage>()` entfernt;
  `AddSingleton<AppShell>`/`AddSingleton<PosterImageService>` vorhanden
- [x] `ShellAppEffectHandler.LoadSourcesAsync` unverändert wiederverwendet
  (kein Diff an der Datei)

### Tests

- [x] `AppUpdateTests_Sources.EnterConnected_StartsSourcesLoad` — vorhanden, grün
- [x] `SynchronizationCompleted_WhenConnected_ReloadsSources` / `_WhileSourcesLoading_NoDuplicate` — vorhanden, grün
- [x] `CachedListsChanged_WhenConnected_ReloadsSources` / `_WhileSourcesLoading_NoDuplicate` / `_WhenNotConnected_NoSourcesLoad` — vorhanden, grün
- [x] `AppUpdateTests_Offline.NetworkLost_FromConnected_ResetsSourcesOverview` — vorhanden, grün
- [x] `AppUpdateTests_Connection.SessionExpired_ResetsSourcesOverviewAndBrowser` — vorhanden, grün
- [x] `AppUpdateTests_Connection.ServerCheckCompleted_Success_ResetsSourcesOverviewAndBrowser` — vorhanden, grün
- [x] `AppUpdateTests_Sources.SourcesLoaded_WhenNotConnected_IsIgnored` (Theory: Offline, NeedsLogin) — vorhanden, grün
- [x] `SourcesContractTests.AppShell_BuildsSourceMenuItems_FromModel` — pinnt
  `ModelChanged`, `SourcesOverview.Sources`, `MenuItem`, `IsEnabled = false`,
  `Items.Insert`/`Items.Remove`, `SourceSelectedMessage`, `GetSourceIconSourceAsync`
- [x] `SourcesContractTests.AppShell_HasNoStaticSourcesFlyoutItem` — kein
  `Route="sources"`/`Title="Quellen"`, genau fünf `FlyoutItem`s (XML-geprüft)
- [x] `SourcesContractTests.MauiProgram_RegistersPosterImageServiceForShell` —
  `AddSingleton<PosterImageService>`, `AddSingleton<AppShell>`, kein
  `SourcesPage` in `MauiProgram.cs`, `PosterImageService` im Shell-Konstruktor
- [x] `VideoPlayerMauiContractTests.ExpectedMenuEntries` auf fünf Einträge
  ohne „Quellen" umgestellt
- [x] `AppRoutesTests.MenuPages_ContainsAllFiveMenuEntriesInOrder` (umbenannt,
  fünf Seiten); `//sources/SourcesPage` als `UnknownLocation`-Fall
- [x] `Navigated_MenuPageWhileWorkflowPending_NavigatesBack` nutzt
  `AppPage.Favorites`
- [x] `SourcesPageOpened_*`-Tests und `SourcesPage_*`-Contract-Tests entfernt
- [x] `AppUpdateTests_HomeLists`/`_NetworkSync`: `Assert.Single` →
  `Contains`-Assertions, `Assert.Same` ersetzt, `LoadSourcesEffect`-Assertions
  ergänzt
- [x] `AutoLoginCompleted_…`/`LoginCompleted_…`: Zählung 5, `LoadSourcesEffect`
  an `Effects[0]`, Index-Pins verschoben
- [x] `MvuHostTests.DispatchAsync_UpdatesModelAndExecutesEffects`: Zählung 12,
  `LoadSourcesEffect` an Index 7

### Dokumentation

- [x] `docs/help/quellen-browsing-details.md` — Quellen-Menü statt „Quellen"-Seite
- [x] `docs/help/app-grundgeruest.md` — fünf feste Einträge + dynamische Quellen
- [x] `docs/help/index.md` — Beschreibung angepasst
- [x] `docs/help/offline-wiederherstellung.md` — Offline-Verhalten des Menüs
- [x] `README.md` — `VideoPlayer.Maui`-Abschnitt erwähnt dynamische
  Quellen-Menüeinträge

## Hinweise

- Der planmäßig geforderte Release-Build (`net10.0-windows10.0.19041.0` für
  `VideoPlayer.Maui`) und `dotnet format whitespace --verify-no-changes`
  wurden im Review nicht erneut ausgeführt (Debug-Build und volle Testsuite
  im Review grün; Release-Nachweis stammt aus dem Implementierungslauf).
- Der manuelle Sichtcheck der `MenuShellItem`-Darstellung (deaktivierte
  Überschrift, Icons, Flyout-Verhalten) bleibt wie im Plan als Restrisiko
  geführt — er ist nicht automatisierbar und war so vorgesehen.
- `SameSources` vergleicht `Id`/`Name`/`IconPictureId` — deckt die
  planmäßige Vergleichssequenz ab; die instanzstabile Verfolgung der
  `MenuShellItem`-Konvertierung ist über `ShellItem`-Referenzen in
  `sourceMenuItems` korrekt gelöst.
