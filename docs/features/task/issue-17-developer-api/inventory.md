# Bestandsaufnahme: Developer-API für UI-Automatisierung

Branch: `task/issue-17-developer-api`

Die fachliche Analyse des Projekt-Schritts liegt bereits vollständig vor:
`docs/projects/project/issue-17-testbarkeit-und-onboarding/inventory/developer-api.md`.
Diese Bestandsaufnahme verifiziert die dort genannten Anzapfpunkte gegen den
aktuellen Stand des Schrittbranchs und ergänzt die für die Umsetzung nötigen
konkreten Fundstellen.

## Verifizierte Anzapfpunkte

| Bedarf | Naht | Stelle (verifiziert) |
|--------|------|----------------------|
| Modell lesen | `MvuHost.Model` (unveränderlicher `AppModel`-Record) | `VideoPlayer.Mvu/MvuHost.cs` Z. 30 |
| Änderungen beobachten | `MvuHost.ModelChanged` / `PropertyChanged` | `MvuHost.cs` Z. 33/36 |
| Dispatch (nicht genutzt – OP-5) | `DispatchAsync` via `SemaphoreSlim gate` | `MvuHost.cs` Z. 15, 44–68 |
| DI-Registrierung | `MauiProgram.CreateMauiApp()` | `VideoPlayer.Maui/MauiProgram.cs` Z. 21–128; `MvuHost` Singleton Z. 44, `AppShell` Singleton Z. 101 |
| Lifecycle-Start | `App.CreateWindow()` (LoadingPage als Root, dann `RunStartupAsync`) | `VideoPlayer.Maui/App.xaml.cs` Z. 32–60; `App` erhält `MvuHost` + `IServiceProvider` per DI (Z. 14, 23) |
| UI-Thread | `MainThread.InvokeOnMainThreadAsync` | Muster `ShellAppEffectHandler.cs` Z. 95, 1177; `ServerSetupPage.xaml.cs` Z. 45 (`BeginInvokeOnMainThread`) |
| Debug-only-Vorläufer | `#if DEBUG builder.Logging.AddDebug()` | `MauiProgram.cs` Z. 123–125 |
| Conditional-Muster | `[Conditional("DEBUG")] ClientGate.WriteDiagnostic` | `VideoPlayer.ApiClient/ClientGate.cs` Z. 142–167 |

## AppModel – sicher exponierbarer Auszug (`GET /model`)

`VideoPlayer.Mvu/AppModel.cs` (212 Z.): `CurrentPage` (Z. 13),
`IsStartupRunning` (16), `ServerHealth` + `ServerHealthText` (19/188),
`ConnectionState` (26), `IsLoginRunning`/`LoginError`/`LoginUsername`
(29–38), `HasOfflineData`/`IsOffline`/`IsOnline`/`IsSyncRunning` (41–54),
`KnownServers`/`ActiveServerAddress`/`DiscoveredServers` (64–73),
`IsDiscoveryRunning`/`DiscoveryAttempted`/`IsServerCheckRunning`/
`ServerSetupError` (76–88), `IsPairingRequired`/`IsPairingRunning`/
`PairingError` (95–104), `HeroTitle` (Z. 143ff.), `StorageNotice` (211).
Listen der Startseite (`ContinueWatching`/`Favorites`/`RecentEntries`)
werden als Zählerwerte exponiert. Keine Tokens/Passwörter im Modell –
per Konstruktion safe; `Settings` wird nicht komplett serialisiert, um die
Feldauswahl kuratiert zu halten.

## Visual Tree / Seitenstruktur (`GET /tree`, `POST /invoke`, `POST /set-text`)

- Root: `Application.Current.Windows[0].Page` – Startphase `LoadingPage`,
  danach `AppShell` (Singleton, `AppShell.xaml.cs` Z. 45ff.; FlyoutItems
  `home`/`favorites`/`downloads`/`profile`/`settings`, Routen
  `serversetup`/`login`/`mediadetails`/`sourceoverview`/`player` registriert
  Z. 51–73). Bei Shell zusätzlich `CurrentPage` berücksichtigen.
- Traversierung: `IVisualTreeElement.GetVisualChildren()`; Attribute je
  Knoten: `Element.AutomationId`, `VisualElement.IsVisible`,
  `VisualElement.Bounds`, Text aus `Label.Text`/`Button.Text`/
  `InputView.Text`/`InputView.Placeholder`/`MenuItem.Text`.
- Vorhandene AutomationIds (u. a. `ServerSetupPage.xaml`):
  `KnownServersView`/`DiscoveredServersView` (CollectionView),
  `DiscoverButton`, `AddressEntry`, `ConnectButton`, `ErrorLabel`,
  `PairingCodeEntry`, `PairButton`, `PairingErrorLabel`,
  `DiscoveryEmptyLabel`, `ActiveServerLabel`.
- CollectionView-Lücke: Items aus `ItemsSource` ableiten; Item-Modelle
  `KnownServer` (`VideoPlayer.Storage/KnownServer.cs`: `Address`,
  `DisplayName`, `NameOrAddress`) und `DiscoveredServer`
  (`VideoPlayer.ApiClient/Discovery/DiscoveredServer.cs`: `Address`,
  `Name`, `NameOrAddress`) bieten `NameOrAddress`/`Address` als
  Anzeigetext; generischer Fallback über bekannte Anzeige-Properties
  (`NameOrAddress`, `Name`, `Title`, `DisplayName`, `Text`) → `ToString()`.
- Clicked-Äquivalente: `Button.SendClicked()`/`ImageButton.SendClicked()`
  (public in .NET MAUI), `Switch.IsToggled`/`CheckBox.IsChecked` flip
  (feuert `Toggled`), `MenuItem`-`Command`/`IMenuItemController.Activate()`,
  generischer `ICommandSource`-Fallback, `TapGestureRecognizer`-`Command`.
- `set-text`: `InputView` (Entry/Editor/SearchBar); `Text`-Setter feuert
  `TextChanged` automatisch.

## HTTP-Server-Optionen (gegen Projekt-Entscheid OP-3 verifiziert)

`TcpListener(IPAddress.Loopback, 0)` ist plattformneutral in `net10.0`
verfügbar → der Kern (Parser, Router, Server) kann in `VideoPlayer.Mvu`
liegen (TargetFramework `net10.0`, vom Testprojekt referenziert,
`InternalsVisibleTo VideoWebPlayer.Maui.Tests` bereits vorhanden –
`VideoPlayer.Mvu.csproj` Z. 15). MAUI-Teile in `VideoPlayer.Maui/DevApi/`.

## Testinfrastruktur

- `VideoWebPlayer.Maui.Tests` (xUnit v3, `net10.0`, referenziert
  `VideoPlayer.Mvu`/`.Storage`/`.ApiClient`/`VideoWebPlayer.Client`,
  **nicht** `VideoPlayer.Maui`).
- Contract-Test-Muster: `Stubs/RepositoryRoot.Find()` findet das Repo-Root
  über `VideoPlayer.App.sln`; Quelltext-Assertions (z. B.
  `PairingContractTests.cs`, `ServerSetupContractTests.cs`).
- Testbasis: 896 Tests (Stand Projektplan); `dotnet test
  VideoWebPlayer.Maui.Tests` im Repo-Root.

## Aktivierung / Release-Ausschluss

- `#if DEBUG || DEV_API` um sämtliche Dev-Api-Dateien und die
  Verdrahtung in `MauiProgram.cs`/`App.xaml.cs`; Opt-in-Define `DEV_API`
  dokumentiert (`dotnet build -p:DefineConstants=…DEV_API`).
- `VideoPlayer.Maui.csproj` darf nicht geändert/committed werden
  (lokale `ApplicationVersion`-Änderung) → Opt-in erfolgt ohne
  Projektdatei-Änderung.
- `devapi.json`: `AppContext.BaseDirectory` (unpackaged Windows-App,
  `WindowsPackageType=None`, csproj Z. 23 → schreibbar); Fallback
  `FileSystem.AppDataDirectory`.

## Detaildokumente

- `inventory/dev-api.md` – Endpunkt-Verträge (Request/Response-Formen,
  Statuscodes), Baum-/Invoke-/Textsetz-Semantik, Fehlerfälle.
