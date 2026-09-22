# Tasks: App-Grundgerüst mit MVU-Struktur und Navigation

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Projekt | `VideoPlayer.Mvu`-Projekt anlegen (`net10.0`-Bibliothek) | Offen | — |
| 2 | MVU-Kern | `AppPage`-Enum (Loading, Home, Sources, Favorites, Downloads, Profile, Settings) | Offen | — |
| 3 | MVU-Kern | `ServerHealthStatus`-Enum (Unknown, Checking, Reachable, Unreachable) | Offen | — |
| 4 | MVU-Kern | `AppModel`-Record (CurrentPage, IsStartupRunning, ServerHealth, ServerHealthText) | Offen | — |
| 5 | MVU-Kern | `AppMessage`-Hierarchie (`StartupRequestedMessage`, `ServerHealthCheckCompletedMessage`, `NavigateToMessage`, `NavigatedMessage`) | Offen | — |
| 6 | MVU-Kern | `AppEffect`-Hierarchie (`NavigateToPageEffect`, `CheckServerHealthEffect`) + `AppTransition` | Offen | — |
| 7 | MVU-Kern | `AppUpdate.Update` (reine Update-Funktion) | Offen | — |
| 8 | MVU-Kern | `AppRoutes` (Route-Mapping für die sechs Menüseiten) | Offen | — |
| 9 | MVU-Kern | `IAppEffectHandler` + `MvuHost` (DispatchAsync, ModelChanged, INotifyPropertyChanged) | Offen | — |
| 10 | Projekt | `VideoPlayer.Maui`-Projekt anlegen (TFMs, `UseMaui`, References auf `VideoPlayer.Mvu` + `VideoPlayer.ApiClient`) | Offen | — |
| 11 | Projekt | `Platforms/` (Windows, iOS, MacCatalyst, Android) und `Resources/` für die neue App | Offen | — |
| 12 | MVU-Rand | `IServerAddressProvider`/`ServerAddressProvider` (Preferences → Env → Default `http://localhost/videoplayer`) | Offen | — |
| 13 | MVU-Rand | `IServerHealthProbe`/`ServerHealthProbe` (nutzt `VideoPlayerApiClient.CheckHealthAsync`) | Offen | — |
| 14 | MVU-Rand | `ShellAppEffectHandler` (`IAppEffectHandler`: Navigation + Health-Check-Dispatch) | Offen | — |
| 15 | MVU-Rand | `MauiProgram` (DI-Registrierung aller Services/Pages, Fonts, Logging) | Offen | — |
| 16 | MVU-Rand | `App.xaml(.cs)` (`CreateWindow` → `LoadingPage`, Startup-Dispatch) | Offen | — |
| 17 | UI | `AppShell.xaml(.cs)` mit sechs `FlyoutItem`s + `Navigated`→`NavigatedMessage` | Offen | — |
| 18 | UI | `LoadingPage` (ActivityIndicator + Statusbindung) | Offen | — |
| 19 | UI | `HomePage` (Grundgerüst „Startseite" + Serverstatus-Anzeige) | Offen | — |
| 20 | UI | `SourcesPage` (Grundgerüst „Quellen") | Offen | — |
| 21 | UI | `FavoritesPage` (Grundgerüst „Favoriten") | Offen | — |
| 22 | UI | `DownloadsPage` (Grundgerüst „Downloads") | Offen | — |
| 23 | UI | `ProfilePage` (Grundgerüst „Profil") | Offen | — |
| 24 | UI | `SettingsPage` (Grundgerüst „Einstellungen") | Offen | — |
| 25 | Projekt | `VideoPlayer.App.sln` um beide neue Projekte erweitern | Offen | — |
| 26 | Tests | `VideoWebPlayer.Maui.Tests.csproj`: `ProjectReference` auf `VideoPlayer.Mvu` | Offen | — |
| 27 | Tests | `Stubs/RepositoryRoot.cs`-Hilfsklasse | Offen | — |
| 28 | Tests | `AppUpdateTests_Startup` + `AppUpdateTests_Navigation` | Offen | — |
| 29 | Tests | `MvuHostTests` + `AppRoutesTests` | Offen | — |
| 30 | Tests | `VideoPlayerMauiContractTests` (Flyout-Einträge, Seitenbindung, TFMs/Referenzen, Health-Check-Verdrahtung) | Offen | — |
| 31 | CI | `pr-checks.yml`: Build-Schritte für `VideoPlayer.Maui` (Android + Windows) | Offen | — |
| 32 | CI | `staging-merge.yml` + `main-merge.yml`: iOS-Build-Schritt für `VideoPlayer.Maui` | Offen | — |
| 33 | CI | `Set-Version.ps1`: beide App-Projektdateien versionieren | Offen | — |
| 34 | Verifikation | Windows- + Android-Build, `dotnet test`, Windows-Smoke-Run, `dotnet format`-Check | Offen | — |
