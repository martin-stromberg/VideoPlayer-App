# Plan-Review

Geprüft: `plan.md` gegen die Implementierung (Selbstprüfung ohne Unteragenten).
Alle Planelemente wurden im Code gesucht und vollständig gelesen.

## Ergebnis

**Status:** Vollständig umgesetzt

## Umgesetzte Planelemente

### Projekt `VideoPlayer.Mvu` (net10.0)

- [x] `VideoPlayer.Mvu.csproj` — angelegt (`net10.0`, `ImplicitUsings`, `Nullable`, Lizenz-Metadaten)
- [x] `AppPage` (Enum) — `Loading`, `Home`, `Sources`, `Favorites`, `Downloads`, `Profile`, `Settings`
- [x] `ServerHealthStatus` (Enum) — `Unknown`, `Checking`, `Reachable`, `Unreachable`
- [x] `AppModel` (record) — `CurrentPage`, `IsStartupRunning`, `ServerHealth`, abgeleitet `ServerHealthText`
- [x] `AppMessage` + `StartupRequestedMessage`, `ServerHealthCheckCompletedMessage`, `NavigateToMessage`, `NavigatedMessage`
- [x] `AppEffect` + `NavigateToPageEffect`, `CheckServerHealthEffect`
- [x] `AppTransition` — `Model` + `Effects`, `Unchanged`
- [x] `AppUpdate.Update` — reine Funktion, alle vier Message-Fälle gemäß Plan
- [x] `AppRoutes` — `ToRoute`, `TryGetPage`, `MenuPages` (sechs Routen)
- [x] `IAppEffectHandler` — `HandleAsync(AppEffect, CancellationToken)`
- [x] `MvuHost` — `Model`, `DispatchAsync` (serialisiert via `SemaphoreSlim`), `ModelChanged`, `INotifyPropertyChanged`, injizierbare Update-Funktion

### Projekt `VideoPlayer.Maui`

- [x] `VideoPlayer.Maui.csproj` — konditionale TFMs (android/ios/maccatalyst/windows), `UseMaui`, `SingleProject`, `MauiXamlInflator=SourceGen`, `ApplicationId de.martinstromberg.videoplayer`, `WindowsPackageType=None`, iOS-Codesign, References auf `VideoPlayer.Mvu` + `VideoPlayer.ApiClient`, kein `VideoWebPlayer.*`, kein MVU-Framework
- [x] `MauiProgram` — DI-Registrierung (Provider, Probe, EffectHandler, MvuHost, AppShell, alle Pages), Fonts, Debug-Logging
- [x] `App.xaml(.cs)` — `CreateWindow` → `Window` mit `LoadingPage` + `StartupRequestedMessage`-Dispatch
- [x] `AppShell.xaml(.cs)` — `FlyoutBehavior="Flyout"`, sechs `FlyoutItem`s mit Titeln/Routen gemäß `AppRoutes`, `Navigated` → `NavigatedMessage`
- [x] `IServerAddressProvider`/`ServerAddressProvider` — Preferences `ServerAddress` → `VIDEOPLAYER_SERVER_URL` → Default `http://localhost/videoplayer/`
- [x] `IServerHealthProbe`/`ServerHealthProbe` — `CheckAsync` via `VideoPlayerApiClient.CheckHealthAsync`, 10-s-Timeout
- [x] `ShellAppEffectHandler` — `CheckServerHealthEffect` → Probe + `ServerHealthCheckCompletedMessage` (UI-Thread); `NavigateToPageEffect` → Shell-Root-Tausch + `GoToAsync("//route")`
- [x] `Pages/LoadingPage`, `HomePage` (Serverstatus-Label), `SourcesPage`, `FavoritesPage`, `DownloadsPage`, `ProfilePage`, `SettingsPage` — Grundgerüste mit deutschen Titeln
- [x] `Platforms/` — Windows (`WinUI.App`, `Package.appxmanifest`, `app.manifest`), iOS (`Program`, `AppDelegate`, `Info.plist`, `PrivacyInfo.xcprivacy`), MacCatalyst (`Program`, `AppDelegate`, `Info.plist`, `Entitlements.plist`), Android (`MainActivity`, `MainApplication`, `AndroidManifest.xml`, `colors.xml`)
- [x] `Resources/` — AppIcon, Splash, Fonts, Styles, Images, Raw

### Solution / Tests / CI

- [x] `VideoPlayer.App.sln` — beide Projekte mit allen Konfigurationseinträgen aufgenommen
- [x] `VideoWebPlayer.Maui.Tests.csproj` — `ProjectReference` auf `VideoPlayer.Mvu`
- [x] `Stubs/RepositoryRoot.cs` — Hilfsklasse
- [x] `AppUpdateTests_Startup` (3 Tests), `AppUpdateTests_Navigation` (6 Tests), `MvuHostTests` (3 Tests), `AppRoutesTests` (4 Tests/Theories), `VideoPlayerMauiContractTests` (6 Tests)
- [x] `pr-checks.yml` — Build-Schritte `VideoPlayer.Maui` (Android + Windows)
- [x] `staging-merge.yml`, `main-merge.yml` — iOS-Build-Schritt `VideoPlayer.Maui`
- [x] `Set-Version.ps1` — versioniert beide App-Projektdateien

## Offene Aufgaben

—

## Hinweise

- `ShellAppEffectHandler.RunHealthCheckAsync` wurde um eine defensive Fehlerbehandlung
  ergänzt (Fehler → „nicht erreichbar"), damit die App nicht auf der Ladeanzeige
  hängen bleibt — innerhalb des geplanten Verhaltens, keine Planabweichung.
