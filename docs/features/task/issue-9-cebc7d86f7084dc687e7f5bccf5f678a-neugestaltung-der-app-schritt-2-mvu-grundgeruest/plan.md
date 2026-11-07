# Umsetzungsplan: App-Grundgerüst mit MVU-Struktur und Navigation

## Übersicht

Neuaufsetzen der App `VideoPlayer` als eigenes MAUI-Projekt `VideoPlayer.Maui` mit eigenem,
schlanken MVU-Zyklus (Model–View–Update, kein externes Framework). Der MVU-Kern liegt in
einer neuen `net10.0`-Bibliothek `VideoPlayer.Mvu`, damit er vom bestehenden xUnit-Projekt
direkt testbar ist (das Testprojekt kann keine MAUI-Targets referenzieren). Die App erhält
eine `Shell`-Hauptnavigation per Hamburgermenü mit sechs Einträgen (Startseite, Quellen,
Favoriten, Downloads, Profil, Einstellungen), je eine Grundgerüst-Seite sowie eine
`LoadingPage` als Start-Ladeanzeige. Die Server-Erreichbarkeit wird beim Start über
`VideoPlayerApiClient.CheckHealthAsync` geprüft; das Ergebnis steuert den Übergang von der
Ladeanzeige zur Shell-Navigation und wird auf der Startseite angezeigt.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Platzierung der neuen App | Neues Projekt `VideoPlayer.Maui` (Namespace `VideoPlayer.Maui`) neben `VideoPlayer.ApiClient` | Sauberer Neubau ohne Übernahme von Altcode; Namen konsistent zur neuen `VideoPlayer.*`-Familie; Solution `VideoPlayer.App.sln` bleibt der Integrationspunkt. |
| Umgang mit Alt-Projekten | `VideoWebPlayer.Maui` und `VideoWebPlayer.Client` bleiben in diesem Schritt in Solution und Repo (nur fachliche Referenz); die neue App referenziert sie nicht. Entfernung erfolgt in einem späteren Schritt | Explizit offen gelassen; minimale Änderungsfläche, die bestehenden Alt-Tests (`MediaEntrySerializationTests`, `MauiSettingsContractTests`) bleiben funktionsfähig, bis die neue App deren Funktionsbereich ablöst. |
| MVU-Kern | Eigene `net10.0`-Bibliothek `VideoPlayer.Mvu`: immutable `AppModel` (record), `AppMessage`-/`AppEffect`-Typen, reine `AppUpdate.Update`-Funktion (`AppTransition` = Model + Effekte), `MvuHost` (Dispatch + Effekt-Delegation an `IAppEffectHandler` + `INotifyPropertyChanged` für Bindings) | MVU-Logik bleibt plattformneutral und unit-testbar (MAUI-Projekt ist nicht referenzierbar); keine externe MVU-Abhängigkeit. Effekte als Daten (PoEAA-ähnlich: Entscheidung im Kern, Ausführung am Rand). |
| Navigation | `AppShell : Shell` mit `FlyoutBehavior="Flyout"` und sechs `FlyoutItem`s (`Route` + `ContentTemplate` auf die Seiten). Seitenwechsel aus dem Menü läuft nativ über Shell; das Model wird über `Shell.Navigated` → `NavigatedMessage` synchronisiert. Programmatische Navigation über `NavigateToPageEffect` (`GoToAsync("//route")`) | Standard-MAUI-Hamburgermenü ohne eigenen Menübau; MVU bleibt führend: Nutzeraktion → Message → Update → Model. Route-Namen liegen zentral in `AppRoutes` (in `VideoPlayer.Mvu`) und sind per Contract-Test mit dem XAML abgeglichen. |
| Start-Ladeanzeige | `App.CreateWindow` zeigt zunächst eine `LoadingPage` (kein Flyout-Eintrag); `StartupRequestedMessage` → `CheckServerHealthEffect` → `ServerHealthCheckCompletedMessage` → `NavigateToPageEffect(Home)` tauscht die Root-Page gegen `AppShell` | Trennung Start-/Hauptphase; Loading-Seite erscheint nie im Menü; Ablauf vollständig über den MVU-Zyklus getrieben und testbar. |
| Serveradresse | `IServerAddressProvider`/`ServerAddressProvider` (MAUI): `Preferences`(`ServerAddress`) → Umgebungsvariable `VIDEOPLAYER_SERVER_URL` → Default `http://localhost/videoplayer` | Minimale, austauschbare Auflösung für diesen Schritt; die eigentliche Serververwaltung folgt in Schritt 4. Default zeigt auf die lokal eingerichtete IIS-Site. |
| Health-Check | `IServerHealthProbe`/`ServerHealthProbe` (MAUI) erzeugt `VideoPlayerApiClient` auf einem eigenen `HttpClient` mit aufgelöster `BaseAddress` und ruft `CheckHealthAsync` | Dünnste mögliche Anbindung; nutzt ausschließlich den neuen Client (inkl. `ClientGate`-Header). |
| Zielplattformen/TFMs | `net10.0-windows10.0.19041.0` + `net10.0-ios`; zusätzlich `net10.0-android` + `net10.0-maccatalyst` im selben konditionalen TFM-Muster wie die Alt-App | Geforderte Ziele sind iOS+Windows; die bereits vorhandenen Workloads/CI machen Android/MacCatalyst faktisch gratis und belegen die geforderte Android-Erweiterbarkeit direkt statt nur strukturell. |
| iOS-Nachweis | CI-Workflows (`staging-merge.yml`, `main-merge.yml`) erhalten einen iOS-Build-Schritt für `VideoPlayer.Maui` (`net10.0-ios`, `iossimulator-arm64`, `macos-latest`); `pr-checks.yml` baut die neue App für Android+Windows. Lokal: csproj-Contract-Test auf korrekte TFMs | Lokaler iOS-Build ist technisch unmöglich (Windows-Rechner); CI-Konfiguration + TFM-Nachweis ist die vom Projekt vorgegebene Nachweisform. |

## Programmabläufe

### App-Start mit Ladeanzeige und Erreichbarkeitsprüfung

1. `MauiProgram.CreateMauiApp` registriert `MvuHost` (Singleton, mit
   `IAppEffectHandler`-Implementierung `ShellAppEffectHandler`), `IServerAddressProvider`,
   `IServerHealthProbe`, `AppShell` und alle Pages.
2. `App.CreateWindow` erzeugt `Window` mit `LoadingPage` (BindingContext = `MvuHost`) und
   startet `MvuHost.DispatchAsync(new StartupRequestedMessage())`.
3. `AppUpdate.Update` → Model `{ IsStartupRunning = true, ServerHealth = Checking }`,
   Effekt `CheckServerHealthEffect`.
4. `MvuHost` übergibt den Effekt an `ShellAppEffectHandler` →
   `ServerHealthProbe.CheckAsync` → `VideoPlayerApiClient.CheckHealthAsync`.
5. Handler dispatcht `ServerHealthCheckCompletedMessage(isReachable)` auf dem UI-Thread
   (`MainThread.BeginInvokeOnMainThread`).
6. `AppUpdate.Update` → Model `{ IsStartupRunning = false, ServerHealth = Reachable|Unreachable,
   CurrentPage = Home }`, Effekt `NavigateToPageEffect(Home)`.
7. `ShellAppEffectHandler` ersetzt `Application.Current.Windows[0].Page` durch `AppShell`
   (aus DI) und führt `Shell.Current.GoToAsync("//home")` aus.

Beteiligte Klassen/Komponenten: `App`, `MvuHost`, `AppUpdate`, `AppModel`,
`StartupRequestedMessage`, `CheckServerHealthEffect`, `ServerHealthCheckCompletedMessage`,
`NavigateToPageEffect`, `ShellAppEffectHandler`, `IServerHealthProbe`, `AppShell`.

### Hamburgermenü-Navigation (Benutzerfluss)

1. Nutzer öffnet das Flyout (Hamburger-Button) — `Shell` zeigt die sechs `FlyoutItem`s.
2. Tipp auf Eintrag → Shell navigiert nativ zum `ShellContent`/`Route` und rendert die
   zugehörige Seite.
3. `Shell.Navigated` feuert → `AppShell` mappt `e.Current.Location` über
   `AppRoutes.TryGetPage` auf `AppPage` und dispatcht `NavigatedMessage(page)`.
4. `AppUpdate.Update` → Model `{ CurrentPage = page }` (kein Effekt, Navigation lief bereits).

Beteiligte Klassen/Komponenten: `AppShell`, `AppRoutes`, `NavigatedMessage`, `AppUpdate`,
`MvuHost`, die sechs Pages.

### Serverstatus-Anzeige auf der Startseite

`HomePage` bindet `BindingContext` auf `MvuHost` und zeigt `Model.ServerHealthText`
(z. B. „Server: erreichbar" / „Server: nicht erreichbar" / „Server: wird geprüft…") als
Label — sichtbarer Nachweis, dass der MVU-Zustand die UI treibt.

## Neue Klassen

### Projekt `VideoPlayer.Mvu` (net10.0)

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `AppPage` | Enum | `Loading`, `Home`, `Sources`, `Favorites`, `Downloads`, `Profile`, `Settings` |
| `ServerHealthStatus` | Enum | `Unknown`, `Checking`, `Reachable`, `Unreachable` |
| `AppModel` | Datenmodellklasse (record) | `CurrentPage`, `IsStartupRunning`, `ServerHealth`; abgeleitet `ServerHealthText` |
| `AppMessage` | abstrakte record-Basisklasse | Basistyp aller Messages |
| `StartupRequestedMessage` | record (AppMessage) | Start-Workflow anstoßen |
| `ServerHealthCheckCompletedMessage` | record (AppMessage) | `IsReachable: bool` |
| `NavigateToMessage` | record (AppMessage) | `Page: AppPage` (programmatische Navigation) |
| `NavigatedMessage` | record (AppMessage) | `Page: AppPage` (vom Shell-`Navigated`-Event) |
| `AppEffect` | abstrakte record-Basisklasse | Basistyp aller Effekte |
| `NavigateToPageEffect` | record (AppEffect) | `Page: AppPage` |
| `CheckServerHealthEffect` | record (AppEffect) | Server-Erreichbarkeit prüfen |
| `AppTransition` | record | `Model: AppModel`, `Effects: IReadOnlyList<AppEffect>` |
| `AppUpdate` | statische Klasse | `Update(AppModel, AppMessage) → AppTransition` (reine Funktion) |
| `AppRoutes` | statische Klasse | `ToRoute(AppPage)`, `TryGetPage(string, out AppPage)` für die sechs Menüseiten |
| `IAppEffectHandler` | Interface | `HandleAsync(AppEffect, CancellationToken) → Task` |
| `MvuHost` | Klasse, `INotifyPropertyChanged` | `Model`, `DispatchAsync(AppMessage)`, `event ModelChanged`; Update-Funktion injizierbar (Standard `AppUpdate.Update`) |

### Projekt `VideoPlayer.Maui` (MAUI)

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `App` | `Application` | `CreateWindow` → `Window` mit `LoadingPage`, Startup-Dispatch |
| `AppShell` | `Shell` | Flyout mit sechs `FlyoutItem`s; `Navigated` → `NavigatedMessage` |
| `MauiProgram` | statische Klasse | `CreateMauiApp`, DI-Registrierung |
| `IServerAddressProvider` / `ServerAddressProvider` | Interface + Klasse | Server-Basisadresse auflösen (Preferences → Env → Default) |
| `IServerHealthProbe` / `ServerHealthProbe` | Interface + Klasse | `CheckAsync` via `VideoPlayerApiClient.CheckHealthAsync` |
| `ShellAppEffectHandler` | `IAppEffectHandler` | `NavigateToPageEffect` → Shell-Root + `GoToAsync`; `CheckServerHealthEffect` → Probe + Result-Dispatch (UI-Thread) |
| `LoadingPage` | ContentPage | `ActivityIndicator` + Statustext (`Model.ServerHealthText`) |
| `HomePage` | ContentPage | Grundgerüst „Startseite" + Serverstatus-Label |
| `SourcesPage` | ContentPage | Grundgerüst „Quellen" |
| `FavoritesPage` | ContentPage | Grundgerüst „Favoriten" |
| `DownloadsPage` | ContentPage | Grundgerüst „Downloads" |
| `ProfilePage` | ContentPage | Grundgerüst „Profil" |
| `SettingsPage` | ContentPage | Grundgerüst „Einstellungen" |
| `Platforms/*` | Plattform-Einstiegspunkte | Windows (`WinUI.App`, `Package.appxmanifest`, `app.manifest`), iOS (`Program`, `AppDelegate`, `Info.plist`, `PrivacyInfo.xcprivacy`), MacCatalyst (`Program`, `AppDelegate`, `Info.plist`, `Entitlements.plist`), Android (`MainActivity`, `MainApplication`, `AndroidManifest.xml`, `colors.xml`) — aus der Template-Struktur der Alt-App übernommen, Namespace `VideoPlayer.Maui` |

## Änderungen an bestehenden Klassen/Dateien

### `VideoPlayer.App.sln` (Solution)

- Neue Projekte `VideoPlayer.Mvu` und `VideoPlayer.Maui` mit vollständigen
  Konfigurationseinträgen (Debug/Release × Any CPU/x64/x86) aufnehmen.

### `VideoWebPlayer.Maui.Tests.csproj` (Testprojekt)

- **Neue Referenz:** `ProjectReference` auf `VideoPlayer.Mvu`.

### `.github/workflows/pr-checks.yml`

- **Neue Schritte:** `dotnet build VideoPlayer.Maui/VideoPlayer.Maui.csproj` für
  `net10.0-android` und `net10.0-windows10.0.19041.0` (Release). Bestehende Schritte
  bleiben unverändert (Alt-App wird vorerst weiter mitgebaut).

### `.github/workflows/staging-merge.yml` und `.github/workflows/main-merge.yml`

- **Neuer Schritt im `build-ios`-Job:** `dotnet build VideoPlayer.Maui/VideoPlayer.Maui.csproj -c Release -f net10.0-ios -r iossimulator-arm64` (iOS-Nachweis der neuen App).

### `.github/scripts/Set-Version.ps1`

- **Geändert:** Versions-Update auf beide App-Projektdateien anwenden
  (`VideoPlayer.Maui/VideoPlayer.Maui.csproj` neu; `VideoWebPlayer.Maui/...` weiterhin,
  solange das Projekt existiert — über eine Pfadliste mit Existenzprüfung).

## Datenbankmigrationen

Keine.

## Validierungsregeln

Keine (keine neue Benutzereingabe; Menünavigation und Start erfolgen ohne Eingaben).

## Konfigurationsänderungen

| Eintrag | Typ | Standardwert | Zweck |
|---------|-----|--------------|-------|
| `ServerAddress` (Preferences-Key, `VideoPlayer.Maui`) | string | – | Gespeicherte Serveradresse (Vorgriff auf Schritt 4; wird nur gelesen) |
| `VIDEOPLAYER_SERVER_URL` (Umgebungsvariable) | string | – | Override der Serveradresse z. B. für Entwicklung/Tests |
| Default-Serveradresse | Konstante in `ServerAddressProvider` | `http://localhost/videoplayer` | Lokale IIS-Installation des Webprojekts |

## Seiteneffekte und Risiken

- **CI-Laufzeit:** Zusätzliche MAUI-Builds in `pr-checks` und den Merge-Workflows erhöhen
  die Laufzeit; Alt-App bleibt vorerst ebenfalls im Build, damit nichts unbemerkt bricht.
- **Release-Artefakte:** Die Merge-Workflows veröffentlichen weiterhin die Alt-App
  (`VideoWebPlayer.Maui`); die neue App wird in diesem Schritt nur gebaut (iOS-Nachweis),
  nicht veröffentlicht. Der Wechsel der veröffentlichten App ist ein späterer,
  bewusster Schritt.
- **Shell-Root-Tausch:** Ersetzen von `Window.Page` beim Übergang Loading→Shell verwirft
  den Navigationsstack — unkritisch, da er zum Startzeitpunkt leer ist.
- **Health-Check-Dauer:** `CheckHealthAsync` kann bei ausgeschaltetem Server mehrere Sekunden
  blockieren (Retry-Pipeline). Für das Grundgerüst akzeptabel; Timeout-Verhalten wird mit
  dem Verbindungs-Workflow (Schritt 5) präzisiert. Die Probe setzt ein eigenes, kurzes
  `HttpClient.Timeout` (10 s).
- **Lokaler IIS liefert HTTP 500:** Der Endpunkt `http://localhost/videoplayer` ist
  konfiguriert, der Server antwortet aber derzeit fehlerhaft → die App zeigt
  „nicht erreichbar". Das belegt den Aufrufpfad (Request geht raus, Antwort kommt an),
  aber keine positive Health-Antwort; wird im Testprotokoll dokumentiert.

## Umsetzungsreihenfolge

1. **`VideoPlayer.Mvu`-Projekt anlegen** (csproj `net10.0`, `ImplicitUsings`, `Nullable`,
   Lizenz-/Author-Metadaten wie `VideoPlayer.ApiClient.csproj`)
   - Voraussetzungen: keine.
   - Beschreibung: Projektdatei + Ordner.

2. **MVU-Kern implementieren** (`AppPage`, `ServerHealthStatus`, `AppModel`, Messages,
   Effekte, `AppTransition`, `AppUpdate`, `AppRoutes`, `IAppEffectHandler`, `MvuHost`)
   - Voraussetzungen: Schritt 1.
   - Beschreibung: reine `net10.0`-Klassen, keine MAUI-Abhängigkeit.

3. **`VideoPlayer.Maui`-Projekt anlegen** (csproj mit konditionalen TFMs wie Alt-Projekt,
   `UseMaui`, `SingleProject`, `MauiXamlInflator=SourceGen`, `ApplicationId
   de.martinstromberg.videoplayer`, `ApplicationTitle VideoPlayer`, `WindowsPackageType=None`,
   iOS-Codesign-Properties; `ProjectReference` auf `VideoPlayer.Mvu` und
   `VideoPlayer.ApiClient`; Pakete nur `Microsoft.Maui.Controls`, `Microsoft.Maui.Essentials`,
   `Microsoft.Extensions.Logging.Debug`)
   - Voraussetzungen: Schritt 1 (Referenz), `VideoPlayer.ApiClient` (vorhanden).
   - Beschreibung: Projektdatei + `Resources/` (AppIcon, Splash, Fonts, Styles, Images,
     Raw — aus Alt-Projekt übernommen) + `Platforms/`-Ordner (Namespace `VideoPlayer.Maui`).

4. **MVU-Rand implementieren** (`MauiProgram`, `App`, `IServerAddressProvider`/
   `ServerAddressProvider`, `IServerHealthProbe`/`ServerHealthProbe`,
   `ShellAppEffectHandler`)
   - Voraussetzungen: Schritte 2–3.

5. **Shell und Seiten implementieren** (`AppShell.xaml(.cs)`, `LoadingPage`, `HomePage`,
   `SourcesPage`, `FavoritesPage`, `DownloadsPage`, `ProfilePage`, `SettingsPage` unter
   `Pages/`; `App.xaml` mit Styles-MergedDictionaries)
   - Voraussetzungen: Schritte 2–4.

6. **Solution erweitern** (`VideoPlayer.Mvu`, `VideoPlayer.Maui` in `VideoPlayer.App.sln`)
   - Voraussetzungen: Schritte 1, 3.

7. **Tests schreiben** (siehe Abschnitt Tests; Testprojekt bekommt `ProjectReference`
   auf `VideoPlayer.Mvu`)
   - Voraussetzungen: Schritte 2–5.

8. **CI/Build-Skripte anpassen** (`pr-checks.yml`, `staging-merge.yml`, `main-merge.yml`,
   `Set-Version.ps1`)
   - Voraussetzungen: Schritt 3.

9. **Verifizieren** (`dotnet restore` Solution; `dotnet build` der neuen App für
   `net10.0-windows10.0.19041.0` und `net10.0-android`; `dotnet test`; Windows-Start-
   Smoke-Test der gebauten Exe; `dotnet format whitespace --verify-no-changes`)
   - Voraussetzungen: Schritte 6–8.

## Tests

### Neue Tests (in `VideoWebPlayer.Maui.Tests`, xUnit)

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `Update_StartupRequested_SetsCheckingAndEmitsHealthCheck` | `AppUpdateTests_Startup` | `StartupRequestedMessage` → `IsStartupRunning=true`, `ServerHealth=Checking`, Effekt `CheckServerHealthEffect` |
| `Update_StartupRequested_WhenAlreadyRunning_NoEffects` | `AppUpdateTests_Startup` | Doppelter Start dispatcht keine zweite Prüfung |
| `Update_HealthCheckCompleted_NavigatesToHome` (theory: reachable true/false) | `AppUpdateTests_Startup` | `ServerHealthCheckCompletedMessage` → `IsStartupRunning=false`, `ServerHealth` korrekt, `CurrentPage=Home`, Effekt `NavigateToPageEffect(Home)` |
| `Update_NavigateTo_SetsPageAndEmitsEffect` (theory: alle 6 Seiten) | `AppUpdateTests_Navigation` | `NavigateToMessage` → `CurrentPage` + `NavigateToPageEffect` |
| `Update_NavigateTo_SamePage_NoEffect` | `AppUpdateTests_Navigation` | No-Op bei identischer Seite |
| `Update_NavigatedMessage_OnlyUpdatesModel` | `AppUpdateTests_Navigation` | `NavigatedMessage` ändert `CurrentPage` ohne Effekt |
| `DispatchAsync_UpdatesModelAndExecutesEffects` | `MvuHostTests` | Host wendet Update an, führt Effekte über `IAppEffectHandler` aus, feuert `ModelChanged`/`PropertyChanged` |
| `AppRoutes_RoundTripsAllMenuPages` | `AppRoutesTests` | `ToRoute`/`TryGetPage` für alle sechs Menüseiten konsistent |
| `AppShell_ContainsAllMenuEntries` | `VideoPlayerMauiContractTests` | `AppShell.xaml` (XML parse): genau die sechs `FlyoutItem`s mit Titeln „Startseite", „Quellen", „Favoriten", „Downloads", „Profil", „Einstellungen" und den `AppRoutes`-Routen; `FlyoutBehavior="Flyout"` |
| `AppShell_MenuEntries_BindToExistingPages` | `VideoPlayerMauiContractTests` | Jedes `ShellContent.ContentTemplate` referenziert eine vorhandene `*.xaml`-Page-Datei; `LoadingPage` ist nicht im Flyout |
| `NewApp_ProjectTargetsRequiredPlatforms` | `VideoPlayerMauiContractTests` | `VideoPlayer.Maui.csproj` enthält `net10.0-ios` + `net10.0-windows10.0.19041.0`, referenziert `VideoPlayer.ApiClient` + `VideoPlayer.Mvu`, referenziert **nicht** `VideoWebPlayer.*` und kein externes MVU-Framework (`Fabulous`, `Comet`) |
| `NewApp_HealthCheckWired` | `VideoPlayerMauiContractTests` | `MauiProgram.cs`/`ShellAppEffectHandler.cs` verdrahten `VideoPlayerApiClient` + `CheckHealthAsync` (Source-Scan, etabliertes Contract-Muster) |
| `RepositoryRoot` (Hilfsklasse) | `Stubs/RepositoryRoot.cs` | `FindRepositoryRoot`-Muster (wie `MauiSettingsContractTests`) wiederverwendbar |

### Betroffene bestehende Tests

Keine — die Alt-Projekte und ihre Tests bleiben unverändert.

### E2E-Tests (primärer Funktionsnachweis)

Es existiert kein UI-Automatisierungs-Framework im Repo (siehe `inventory/tests.md`);
echte gerenderte E2E-Tests sind nicht möglich. Als primärer Funktionsnachweis für den
Benutzerfluss „Hamburgermenü-Navigation" dienen daher die folgende, im Plan verankerte
Nachweiskette, die den Fluss auf allen verfügbaren Ebenen absichert:

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist / Ersatzform |
|-----------|----------|------------------------|-------------------------------|----------------------------------|
| Pflicht | Menü öffnen → alle sechs Einträge sichtbar → Tipp führt zur jeweiligen Seite | `VideoPlayerMauiContractTests.AppShell_ContainsAllMenuEntries` + `AppShell_MenuEntries_BindToExistingPages` | Hamburgermenü enthält alle Einträge und führt zu den Seiten | Struktureller Nachweis des Flyouts (Einträge, Titel, Routen, Seitenbindung) — das einzige automatisierbare Äquivalent ohne UI-Framework |
| Pflicht | Seitenwechsel aktualisiert den App-Zustand (MVU) | `AppUpdateTests_Navigation.*`, `AppRoutesTests`, `MvuHostTests` | MVU-Struktur; Navigation führt zu den jeweiligen Seiten | Unit-Nachweis der Navigations-Logik (Message→Model→Effekt) |
| Pflicht | App baut und startet unter Windows | Windows-Release-Build + Smoke-Run der erzeugten `VideoPlayer.Maui.exe` (Prozess startet und bleibt ~15 s aktiv, dann beendet); dokumentiert in `test-results.md` | „baut und startet unter Windows" | Tatsächlicher App-Start als E2E-Ersatz auf Prozessebene |
| Pflicht | iOS-Build | CI-Schritt `net10.0-ios`/`iossimulator-arm64` in `staging-merge.yml`/`main-merge.yml` + Contract-Test auf TFMs | „iOS-Build über CI nachweisbar" | Lokaler iOS-Build unmöglich; CI-Konfiguration ist die vorgegebene Nachweisform |
| Pflicht | App ruft die Server-API auf (Erreichbarkeitsprüfung) | `ServerHealthProbe` + `ShellAppEffectHandler` (verdrahtet via Contract-Test `NewApp_HealthCheckWired`); `VideoPlayerApiClient.CheckHealthAsync` selbst ist durch die 69 Bestandstests gegen `StubHttpMessageHandler` abgesichert; Live-Aufruf gegen `http://localhost/videoplayer` wird manuell dokumentiert (derzeit HTTP 500 → „nicht erreichbar" belegt den Aufrufpfad) | „API grundsätzlich aufrufbar" | Kein kontrollierbarer Server in CI verfügbar; Call-Path-Nachweis + clientseitige Unit-Abdeckung |

Welche bestehenden E2E-Tests müssen angepasst werden? Keine — es existieren keine.

## Offene Punkte

Keine.
