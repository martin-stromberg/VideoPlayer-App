# Code-Review

Geprüft werden alle neuen/geänderten Quelldateien gegenüber dem Basisbranch
`task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app`
(Selbstprüfung ohne Unteragenten).

## Geänderte/neue Quelldateien

### `VideoPlayer.Mvu` (neu)

- `AppPage.cs`, `ServerHealthStatus.cs` — Enums, korrekt und vollständig.
- `AppModel.cs` — Record mit `init`-Properties, abgeleiteter `ServerHealthText`
  deckt alle `ServerHealthStatus`-Werte ab.
- `Messages/AppMessages.cs` — `abstract record AppMessage`, vier konkrete
  Messages; `ServerHealthCheckCompletedMessage.IsReachable` korrekt benannt.
- `Effects/AppEffects.cs` — `abstract record AppEffect`, zwei konkrete Effects.
- `AppTransition.cs` — `Model` + `Effects` (unveränderliche Liste), `Unchanged`.
- `AppUpdate.cs` — reine Update-Funktion, erschöpfende `switch`-Ausdrücke;
  `Loading`→`NavigateToPageEffect(Home)` und `Navigated`→effektloses
  `CurrentPage`-Update wie geplant.
- `AppRoutes.cs` — `MenuPages`, `ToRoute` (wirft für `Loading`, begründet),
  `TryGetPage` berücksichtigt Shell-Route-Segmente.
- `Host/IAppEffectHandler.cs`, `Host/MvuHost.cs` — `DispatchAsync` serialisiert
  über `SemaphoreSlim`; `ModelChanged`/`PropertyChanged` werden außerhalb des
  Locks ausgelöst (kein Deadlock bei re-entranten Dispatchs aus Effects);
  Update-Funktion injizierbar (Testbarkeit); `INotifyPropertyChanged` vollständig.

### `VideoPlayer.Maui` (neu)

- `VideoPlayer.Maui.csproj` — konditionale TFMs (Windows 10.0.19041+, iOS,
  MacCatalyst, Android 21+), `UseMaui`, `SingleProject`, `MauiXamlInflator=SourceGen`,
  eigene `ApplicationId`, iOS-Codesign, `WindowsPackageType=None`;
  References nur auf `VideoPlayer.Mvu` + `VideoPlayer.ApiClient`; kein
  `VideoWebPlayer.*`, kein externes MVU-Framework.
- `MauiProgram.cs` — DI-Registrierung vollständig und konsistent
  (Singletons, App/Page-Transients), Fonts, Debug-Logging.
- `App.xaml(.cs)` — `CreateWindow` erzeugt `Window` mit `LoadingPage` und
  dispatcht `StartupRequestedMessage`; `startupDispatched`-Guard gegen
  doppelten Start; Exceptions beim Startup-Dispatch werden protokolliert.
- `AppShell.xaml(.cs)` — `FlyoutBehavior="Flyout"`, sechs `FlyoutItem`s mit
  Titeln/Routen, die zu `AppRoutes`/`AppPage` passen; `Navigated`-Handler
  dispatcht `NavigatedMessage` (fire-and-forget mit Fehlerprotokollierung).
- `Services/ServerAddressProvider.cs` — Preferences `ServerAddress` →
  `VIDEOPLAYER_SERVER_URL` → Default `http://localhost/videoplayer/`; keine
  Secrets.
- `Services/ServerHealthProbe.cs` — nutzt `VideoPlayerApiClient.CheckHealthAsync`
  mit 10-s-Timeout; `HttpClient` wird pro Prüfung erzeugt und disposed
  (einmalig beim Start, vertretbar).
- `Services/ShellAppEffectHandler.cs` — `CheckServerHealthEffect` ruft die
  Probe, fängt jede Ausnahme ab (Ergebnis „nicht erreichbar", App bleibt nicht
  auf der Ladeanzeige hängen) und dispatcht die Completion-Message auf dem
  UI-Thread; `NavigateToPageEffect` tauscht die Root-Page gegen `AppShell`,
  wartet kurz auf `Shell.Current` und führt `GoToAsync("//<route>")` aus;
  `MvuHost` wird lazy über `IServiceProvider` aufgelöst (vermeidet den
  DI-Zyklus Host↔Handler, dokumentiert).
- `Pages/*` — sieben einfache `ContentPage`-Grundgerüste mit deutschen Titeln;
  `LoadingPage`/`HomePage` binden `Model.ServerHealthText` über den
  `BindingContext` (`MvuHost`, `INotifyPropertyChanged` vorhanden).
- `Platforms/*`, `Resources/*` — Standard-MAUI-Gerüst für Windows, iOS,
  MacCatalyst, Android; `AndroidManifest` mit `INTERNET` und
  `usesCleartextTraffic="true"` (erforderlich für lokale HTTP-Server,
  bewusste Entscheidung für LAN-Einsatz).

### Tests / Build

- `VideoWebPlayer.Maui.Tests.csproj` — `ProjectReference` auf `VideoPlayer.Mvu`
  ergänzt, sonst unverändert.
- `Stubs/RepositoryRoot.cs`, `AppUpdateTests_Startup.cs`,
  `AppUpdateTests_Navigation.cs`, `MvuHostTests.cs`, `AppRoutesTests.cs`,
  `VideoPlayerMauiContractTests.cs` — decken Update-Funktion, Host-Zyklus,
  Routen/Menü und Architekturverträge ab; xUnit v3 mit
  `TestContext.Current.CancellationToken` (keine Analyzer-Warnungen).
- `.github/workflows/*.yml` — zusätzliche Build-Schritte für `VideoPlayer.Maui`
  (Android + Windows in PR-Checks, iOS in staging/main), Windows-Build nutzt
  `-p:TargetFrameworks` um die unabhängige App gezielt zu bauen.
- `.github/scripts/Set-Version.ps1` — verarbeitet beide App-Projektdateien mit
  Existenzprüfung.

## Befunde

**Status:** Keine Befunde

## Hinweise (keine Befunde)

- `ServerHealthProbe` erzeugt pro Prüfung einen `HttpClient`; der Aufruf
  erfolgt nur einmal beim App-Start und ist damit unkritisch.
- `AppRoutes.TryGetPage` scannt linear über sechs Einträge — ausreichend.
- Die Contract-Tests prüfen die XAML-Struktur textuell; die konkrete
  Flyout-Navigation wird zusätzlich im Smoke-Run verifiziert.
