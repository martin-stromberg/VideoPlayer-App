# UI / Navigation — Alt-App `VideoWebPlayer.Maui` (nur fachliche Referenz)

**Architektur:** MVVM (`ViewModels/`, `INotifyPropertyChanged`, Commands). Kein MVU-Framework.
Darf nicht als Laufzeitbasis übernommen werden.

## Shell / Hamburgermenü

`VideoWebPlayer.Maui/AppShell.xaml` + `AppShell.xaml.cs`:

- `Shell` mit `FlyoutBehavior="Flyout"` → Hamburgermenü ist vorhanden.
- Ein `FlyoutItem` „Home" (`Route="MainPage"`, `ContentTemplate` → `MainPage`) und ein
  `MenuItem` „Einstellungen" (`Clicked` → `OnShellSettingsClicked` → `PushAsync(SettingsPage)`).
- Konstruktor lädt nach ~200 ms Quellen über den Alt-Client und hängt sie dynamisch als
  `MenuItem`s ans Flyout (Klick → `SourceOverviewPage`).
- `Routing.RegisterRoute("home", typeof(HomePage))`.

## App-Start

- `App.xaml.cs`: `CreateWindow` → `Window` mit `AppShell` (via DI oder `ActivatorUtilities`);
  `Title = string.Empty`, `TitleBar = null` nach `window.Created`.
- `MauiProgram.CreateMauiApp()`: `UseMauiApp<App>()`, `UseMauiCommunityToolkit()`,
  `UseMauiCommunityToolkitMediaElement(true)`; DI-Registrierungen für Services und Pages;
  `builder.Logging.AddDebug()` im DEBUG; `App.ServiceProvider = mauiApp.Services`.

## Vorhandene Seiten (Referenz)

| Page | Zweck |
|------|-------|
| `MainPage` | Einstieg; orchestriert Verbindungs-/Login-Workflow |
| `HomePage` | Startseite mit Carousels |
| `LoadingPage` | `ContentPage` mit `ActivityIndicator` + `Label` (Konstruktor nimmt `message`) |
| `LoginPage` | Anmeldeformular |
| `ServerSetupPage` | Serveradresse + automatische Suche (UI im Code, kein XAML) |
| `SettingsPage` | Aufbewahrungs-Einstellungen |
| `SourceOverviewPage`, `TVShowDetailsPage`, `MovieCollectionDetailsPage` | Browsing/Details |

## Nicht vorhanden (für diese Anforderung relevant)

- MVU-Architektur (Bestand ist MVVM).
- Menüeinträge/Seiten für „Quellen"-Übersicht, „Favoriten", „Downloads", „Profil".
- Eine Start-Ladeanzeige, die in den Navigationsfluss integriert ist (`LoadingPage` existiert,
  wird aber modal/im Workflow genutzt, nicht als fester Startzustand).

## Plattformordner (Vorlage für das neue Projekt)

- `Platforms/Windows/`: `App.xaml(.cs)` (`MauiWinUIApplication`, Namespace `…WinUI`),
  `Package.appxmanifest` (Capability `runFullTrust`), `app.manifest` (DPI/longPath).
- `Platforms/iOS/`: `Program.cs` (`UIApplication.Main` → `AppDelegate`), `AppDelegate.cs`
  (`MauiUIApplicationDelegate`), `Info.plist` (`CFBundleIdentifier`, ATS erlaubt beliebige
  Loads, DisplayName „Videos"), `Resources/PrivacyInfo.xcprivacy`.
- `Platforms/Android/`: `MainActivity.cs`, `MainApplication.cs`, `AndroidManifest.xml`
  (INTERNET/ACCESS_NETWORK_STATE), `Resources/values/colors.xml`.
- `Platforms/MacCatalyst/`: `Program.cs`, `AppDelegate.cs`, `Info.plist`, `Entitlements.plist`.

## Ressourcen (Vorlage)

`Resources/`: `AppIcon/appicon.svg`(+fg), `Splash/splash.svg`, `Fonts/OpenSans-*.ttf`,
`Styles/Colors.xaml` + `Styles.xaml` (MAUI-Template), `Images/*`, `Raw/AboutAssets.txt`.
