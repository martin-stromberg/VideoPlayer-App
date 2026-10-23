# Tasks: App-Grundgerüst mit MVU-Struktur und Navigation

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Projekt | `VideoPlayer.Mvu`-Projekt anlegen (`net10.0`-Bibliothek) | Erledigt | `VideoPlayerMauiContractTests.NewApp_ProjectTargetsRequiredPlatforms` (Referenznachweis) + Build |
| 2 | MVU-Kern | `AppPage`-Enum (Loading, Home, Sources, Favorites, Downloads, Profile, Settings) | Erledigt | `AppRoutesTests.*`, `AppUpdateTests_Navigation.*` |
| 3 | MVU-Kern | `ServerHealthStatus`-Enum (Unknown, Checking, Reachable, Unreachable) | Erledigt | `AppUpdateTests_Startup.*` |
| 4 | MVU-Kern | `AppModel`-Record (CurrentPage, IsStartupRunning, ServerHealth, ServerHealthText) | Erledigt | `AppUpdateTests_*`, `MvuHostTests` |
| 5 | MVU-Kern | `AppMessage`-Hierarchie (4 Messages) | Erledigt | `AppUpdateTests_*`, `MvuHostTests` |
| 6 | MVU-Kern | `AppEffect`-Hierarchie (`NavigateToPageEffect`, `CheckServerHealthEffect`) + `AppTransition` | Erledigt | `AppUpdateTests_Startup.Update_HealthCheckCompleted_NavigatesToHome` |
| 7 | MVU-Kern | `AppUpdate.Update` (reine Update-Funktion) | Erledigt | `AppUpdateTests_Startup.*`, `AppUpdateTests_Navigation.*` |
| 8 | MVU-Kern | `AppRoutes` (Route-Mapping für die sechs Menüseiten) | Erledigt | `AppRoutesTests.*` |
| 9 | MVU-Kern | `IAppEffectHandler` + `MvuHost` (DispatchAsync, ModelChanged, INotifyPropertyChanged) | Erledigt | `MvuHostTests.*` |
| 10 | Projekt | `VideoPlayer.Maui`-Projekt anlegen (TFMs, `UseMaui`, References) | Erledigt | `VideoPlayerMauiContractTests.NewApp_ProjectTargetsRequiredPlatforms` + `NewApp_DoesNotReferenceLegacyOrExternalMvuFrameworks` + Windows-/Android-Build |
| 11 | Projekt | `Platforms/` + `Resources/` für die neue App | Erledigt | Windows-/Android-Build erfolgreich (TFM-Contract-Test deckt iOS-Konfiguration ab) |
| 12 | MVU-Rand | `IServerAddressProvider`/`ServerAddressProvider` | Erledigt | `NewApp_HealthCheckWired` (indirekt) + Windows-Smoke-Run |
| 13 | MVU-Rand | `IServerHealthProbe`/`ServerHealthProbe` | Erledigt | `VideoPlayerMauiContractTests.NewApp_HealthCheckWired` |
| 14 | MVU-Rand | `ShellAppEffectHandler` | Erledigt | `NewApp_HealthCheckWired` + `MvuHostTests.DispatchAsync_UpdatesModelAndExecutesEffects` (Handler-Vertrag) |
| 15 | MVU-Rand | `MauiProgram` (DI, Fonts, Logging) | Erledigt | Windows-Smoke-Run (App startet) + `NewApp_HealthCheckWired` |
| 16 | MVU-Rand | `App.xaml(.cs)` (`CreateWindow` → `LoadingPage`, Startup-Dispatch) | Erledigt | Windows-Smoke-Run |
| 17 | UI | `AppShell.xaml(.cs)` mit sechs `FlyoutItem`s + `Navigated`→`NavigatedMessage` | Erledigt | `AppShell_ContainsAllMenuEntries`, `AppShell_IsFlyoutNavigation`, `AppShell_MenuEntries_BindToExistingPages` |
| 18 | UI | `LoadingPage` | Erledigt | `AppShell_MenuEntries_BindToExistingPages` (nicht im Flyout) + Windows-Smoke-Run |
| 19 | UI | `HomePage` (+ Serverstatus-Anzeige) | Erledigt | `AppShell_MenuEntries_BindToExistingPages` + Windows-Smoke-Run |
| 20 | UI | `SourcesPage` | Erledigt | `AppShell_MenuEntries_BindToExistingPages` |
| 21 | UI | `FavoritesPage` | Erledigt | `AppShell_MenuEntries_BindToExistingPages` |
| 22 | UI | `DownloadsPage` | Erledigt | `AppShell_MenuEntries_BindToExistingPages` |
| 23 | UI | `ProfilePage` | Erledigt | `AppShell_MenuEntries_BindToExistingPages` |
| 24 | UI | `SettingsPage` | Erledigt | `AppShell_MenuEntries_BindToExistingPages` |
| 25 | Projekt | `VideoPlayer.App.sln` um beide neue Projekte erweitern | Erledigt | `dotnet build`/Restore der Solution erfolgreich |
| 26 | Tests | `VideoWebPlayer.Maui.Tests.csproj`: `ProjectReference` auf `VideoPlayer.Mvu` | Erledigt | Testlauf kompiliert + läuft |
| 27 | Tests | `Stubs/RepositoryRoot.cs`-Hilfsklasse | Erledigt | `VideoPlayerMauiContractTests.*` nutzt sie |
| 28 | Tests | `AppUpdateTests_Startup` + `AppUpdateTests_Navigation` | Erledigt | alle bestanden (101 Tests gesamt) |
| 29 | Tests | `MvuHostTests` + `AppRoutesTests` | Erledigt | alle bestanden |
| 30 | Tests | `VideoPlayerMauiContractTests` | Erledigt | alle bestanden |
| 31 | CI | `pr-checks.yml`: Build-Schritte `VideoPlayer.Maui` (Android + Windows) | Erledigt | Workflow-YAML angepasst; Builds lokal verifiziert |
| 32 | CI | `staging-merge.yml` + `main-merge.yml`: iOS-Build-Schritt | Erledigt | Workflow-YAML angepasst (Ausführung bei nächstem staging/main-Push) |
| 33 | CI | `Set-Version.ps1`: beide App-Projektdateien | Erledigt | Skript angepasst (Pfadliste mit Existenzprüfung) |
| 34 | Verifikation | Windows-/Android-Build, `dotnet test`, Smoke-Run, `dotnet format` | Erledigt | Build-Logs + Testlauf + Smoke-Run (Prozess lief 20 s, Fenstertitel „VideoPlayer") + format exit 0 |
