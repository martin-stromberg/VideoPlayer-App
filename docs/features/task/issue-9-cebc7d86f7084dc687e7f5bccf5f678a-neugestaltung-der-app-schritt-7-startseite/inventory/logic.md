# Logik und Abläufe

## MVU-Kern (`VideoPlayer.Mvu`)

### `MvuHost`
Datei: `VideoPlayer.Mvu/MvuHost.cs`

| Member | Zweck |
|--------|-------|
| `Model` | Aktuelles `AppModel` (unveränderlich) |
| `DispatchAsync(AppMessage)` | Wendet `AppUpdate.Update` an (serialisiert per `SemaphoreSlim`), setzt `Model`, löst `PropertyChanged`+`ModelChanged` aus und führt die Effekte nacheinander über `IAppEffectHandler` aus |
| `ModelChanged` | Event nach jedem Update — wird von `HomePage` abonniert |

### `AppUpdate` (reine Update-Funktion)
Datei: `VideoPlayer.Mvu/AppUpdate.cs`

Relevante vorhandene Übergänge:

- `StartupRequestedMessage` → `IsStartupRunning/ServerHealth=Checking/
  ConnectionState=Connecting` + Effekte `[InitializeStorageEffect,
  LoadSettingsEffect, LoadServersEffect, StartNetworkMonitoringEffect]`.
  **Erweiterungspunkt für Cache-First-Laden.**
- `SynchronizationCompletedMessage` → `IsSyncRunning=false`, keine Effekte.
  **Erweiterungspunkt für Listen-Nachladen nach Voll-Sync.**
- `ServerEventReceivedMessage` → nur in `Connected`: `SyncContinueWatchingEffect`
  / `SyncFavoritesEffect` / `SyncMetadataEffect`. **Die Teil-Sync-Effekte
  melden keinen Abschluss** — eine Aktualisierung der Anzeige braucht eine
  Rückmeldung (neue Message).
- `NavigateToMessage`/`NavigatedMessage` → Seitenwechsel mit
  Zustands-Guards (Menüseiten gesperrt in `NeedsLogin`/`NeedsServerSetup`);
  `NavigatedMessage` wird vom Shell-`Navigated`-Event dispatcht
  (`AppShell.OnShellNavigated`, nur für bekannte Menü-Routen —
  `AppRoutes.TryGetPage` liefert für unbekannte Routen `false` → eine
  Detail-Route verändert `CurrentPage` nicht).
- `IsWorkflowPage` = `Loading | Login | ServerSetup` — entscheidet, ob
  Übergänge nach `Home` navigieren dürfen.

### `AppModel`
Datei: `VideoPlayer.Mvu/AppModel.cs`

Record mit `init`-Properties; enthält u. a. `CurrentPage`,
`ConnectionState`, `IsOnline`, `IsOffline` (berechnet), `IsSyncRunning`,
`Settings`, `KnownServers`, `ActiveServerAddress`. **Noch keine
Medienlisten-Eigenschaften.**

### `ServerSyncService`
Datei: `VideoPlayer.Mvu/ServerSyncService.cs`

| Methode | Zweck |
|---------|-------|
| `SynchronizeAsync` | Voll-Sync: `FlushPendingProgressAsync` → `SyncMetadataAsync` → `SyncContinueWatchingAsync` → `SyncFavoritesAsync`; Ergebnis `SyncResult` |
| `SyncMetadataAsync` | `GetSourcesAsync` → `cached_lists`/`sources`; `GetRecentEntriesAsync` → `cached_lists`/`recent` |
| `SyncContinueWatchingAsync` | `GetContinueWatchingAsync` → `cached_lists`/`continue_watching` |
| `SyncFavoritesAsync` | `GetFavoritesAsync` → Tabelle `cached_favorites` komplett ersetzen |

Schreibt JSON via `WriteCachedListAsync` (`InsertOrReplaceAsync` auf
`CachedListEntry`) bzw. `CachedFavoritesEntry`-Zeilen. Liest die Caches
**nicht** — dafür fehlt ein Lesedienst. Serialisierung via
`JsonSerializer.Serialize` (Default-Optionen; `MediaEntry`-Polymorphie
über `$type`-Attribut gesichert).

### `AuthSessionService`
Datei: `VideoPlayer.Mvu/AuthSessionService.cs`

`Client` (nullable `VideoPlayerApiClient` mit Bearer-Token und
Token-Refresher) — Zugriffspunkt für `GetPictureAsync` am App-Rand.

## App-Rand (`VideoPlayer.Maui`)

### `ShellAppEffectHandler`
Datei: `VideoPlayer.Maui/Services/ShellAppEffectHandler.cs`

- `HandleAsync` — `switch` über alle `AppEffect`-Subtypen; neue Effekte
  müssen hier einen Case erhalten (Kompilierzeit-Absicherung nicht
  vorhanden — Contract-Test).
- `RunPartialSyncAsync(Func<ServerSyncService, Task>)` — führt
  `SyncContinueWatchingAsync`/`SyncFavoritesAsync`/`SyncMetadataAsync`
  aus, schluckt Fehler mit Debug-Log, **dispatcht keine Abschluss-
  Message**.
- `DispatchMessageAsync(AppMessage)` — Rückdispatch über `MainThread`
  in den `MvuHost`.
- `NavigateAsync(AppPage)` — Menüseiten via `Shell.Current.GoToAsync(
  "//{route}")`; `ServerSetup`/`Login` als geschobene Route bzw.
  Window-Root; sorgt für den Übergang `LoadingPage` → `AppShell`.

### `AppShell` / `AppRoutes`
- `AppShell.xaml`: sechs `FlyoutItem`s (`home`, `sources`, `favorites`,
  `downloads`, `profile`, `settings`).
- `AppShell.xaml.cs`: `Routing.RegisterRoute("serversetup", …)` und
  `("login", …)` im Konstruktor — Muster für die neue Detail-Route.
- `AppRoutes.TryGetPage` ignoriert unbekannte Routen-Segmente →
  Detail-Navigation ändert das Model nicht.

### `HomePage` (Platzhalter)
Dateien: `VideoPlayer.Maui/Pages/HomePage.xaml(.cs)`

- BindingContext = `MvuHost` (injiziert, `AddTransient` in `MauiProgram`).
- `ModelChanged`-Subscription in `OnAppearing`/`OnDisappearing`,
  `UpdateUi` via `MainThread.BeginInvokeOnMainThread`; steuert aktuell
  nur `OfflineBanner.IsVisible = model.IsOffline`.
- XAML: `VerticalStackLayout` mit Titel, `OfflineBanner`
  (AutomationId `OfflineBanner`), Platzhalter-Label,
  `ServerStatusLabel` (`{Binding Model.ServerHealthText}`).

### `MauiProgram`
Registriert Dienste/Serien: `AuthSessionService`, `IAppEffectHandler`
(`ShellAppEffectHandler`), `MvuHost`, Storage-Dienste,
`ServerSyncService` u. a. als Singletons; Pages als Transients. Neue
Dienste/Seiten werden hier ergänzt.

## Altbestand-Referenz (`VideoWebPlayer.Maui`)

- `HomePage.xaml` + `Components/MediaCarousel.xaml(.cs)`: Titelzeile +
  horizontaler `ScrollView` mit `BindableLayout.ItemsSource`; Item =
  `Border` (140×210, `RoundRectangle 8`, `Shadow`, `#23232a`) mit
  `TapGestureRecognizer` `OnItemTapped` und `Image AspectFill`; Sektion
  unsichtbar bei `HasItems=false`. Listen in `HomePageViewModel`:
  „Weiterschauen", „Favoriten", „Neu im Programm",
  „Heruntergeladene Videos" (letztere gehört zu Schritt 11).
- `HomePageViewModel.LoadFromCacheAsync` → Cache zuerst anzeigen, dann
  Server laden — fachliche Referenz für Cache-First.
- `GetEntryDetails` — Navigationsauflösung des Altbestands: Episode/
  Season → Show (mit `seasonId`/`episodeId`-Kontext), Movie in
  Collection → Collection, sonst eigenes `Id`/`MediaType`.
- `LoadImageAsync` — Bild-Bytes über authentifizierten Client laden,
  `StreamImageSource`; Fehler werden geloggt und ergeben fehlende Bilder.
- `ElementCacheService`/`CachedCarouselItem` — Altbestand-Pendant der
  Listen-Caches (nicht wiederverwendbar: andere DB, `CarouselCache`).

## Voraussetzungen/Pakete

- `VideoPlayer.Maui` referenziert `VideoPlayer.ApiClient`, `.Mvu`,
  `.Storage`; Pakete: `Microsoft.Maui.Controls`/`Essentials` 10.0.90,
  `Microsoft.AspNetCore.SignalR.Client` 10.0.10. **Kein
  CommunityToolkit** — für horizontale Listen reichen
  `ScrollView`+`BindableLayout` bzw. `CollectionView`.
- `VideoWebPlayer.Maui.Tests` (net10.0, xUnit v3) referenziert nur die
  plattformneutralen Projekte → MAUI-Verdrahtung über Contract-Tests
  auf Quelltextebene.
