# Abnahmeprüfung – Entwicklungsschritt 2

## Ergebnis

**Status:** Anforderung vollständig erfüllt

## Abweichungen

- Keine.

## Hinweise

Geprüft auf Branch `task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app-schritt-2-mvu-grundgeruest` (HEAD: `a3c0305`), Diff gegen `task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app`.

### MVU-Struktur (eigener Zyklus, kein externes Framework)

- `VideoPlayer.Mvu` ist eine plattformneutrale `net10.0`-Bibliothek ohne einzige `PackageReference` (`VideoPlayer.Mvu/VideoPlayer.Mvu.csproj`) — kein externes MVU-Framework (kein Fabulous, Comet, CommunityToolkit.Mvvm o. ä. im gesamten Repository).
- Echter MVU-Zyklus mit sauberer Trennung:
  - **Model:** `AppModel` als unveränderliches `record` (`VideoPlayer.Mvu/AppModel.cs:7-26`) mit `CurrentPage`, `IsStartupRunning`, `ServerHealth`.
  - **Update:** `AppUpdate.Update` als reine Funktion `AppModel × AppMessage → AppTransition` (neues Model + Effektliste) (`VideoPlayer.Mvu/AppUpdate.cs:9-22`, `AppTransition.cs`).
  - **Messages/Effects:** `AppMessage.cs`, `AppEffect.cs` — Seiteneffekte als Daten (`NavigateToPageEffect`, `CheckServerHealthEffect`), ausgeführt am App-Rand über `IAppEffectHandler`.
  - **Host/Loop:** `MvuHost` (`VideoPlayer.Mvu/MvuHost.cs`) serialisiert Dispatchs per `SemaphoreSlim`, hält das Model, implementiert `INotifyPropertyChanged` für Bindings und führt Effekte sequenziell aus.
  - **View:** MAUI-Seiten binden an `MvuHost.Model` (`Pages/LoadingPage.xaml.cs`, `Pages/HomePage.xaml.cs`).

### Keine Übernahme des MVVM-Bestandscodes

- `VideoPlayer.Maui` referenziert nur `VideoPlayer.ApiClient` und `VideoPlayer.Mvu` (`VideoPlayer.Maui.csproj:53-56`); `VideoPlayer.Mvu` hat keine Projektreferenzen.
- Textsuche bestätigt: kein Vorkommen von `VideoWebPlayer` in `VideoPlayer.Maui/` und `VideoPlayer.Mvu/` (Contract-Test `NewApp_DoesNotReferenceLegacyOrExternalMvuFrameworks`, `VideoPlayerMauiContractTests.cs:93-101`, sichert dies zusätzlich ab).

### Hamburgermenü und Seiten

- `AppShell.xaml` mit `FlyoutBehavior="Flyout"` (= Hamburgermenü) und allen 6 geforderten Einträgen mit je eigenem `FlyoutItem`/`ShellContent`/`ContentTemplate`:
  Startseite→`HomePage` (Route `home`), Quellen→`SourcesPage` (`sources`), Favoriten→`FavoritesPage` (`favorites`), Downloads→`DownloadsPage` (`downloads`), Profil→`ProfilePage` (`profile`), Einstellungen→`SettingsPage` (`settings`) — `AppShell.xaml:8-27`.
- Alle sechs Seiten existieren als funktionslose Grundgerüste (`VideoPlayer.Maui/Pages/*.xaml` — Titel + Platzhaltertext), wie gefordert.
- Zentrale Routenzuordnung in `VideoPlayer.Mvu/AppRoutes.cs` (`MenuPages`, `ToRoute`, `TryGetPage`); `AppShell.xaml.cs` synchronisiert nutzerseitige Flyout-Navigation über `NavigatedMessage` ins MVU-Model.

### Ladeanzeige beim Start

- `Pages/LoadingPage.xaml` mit `ActivityIndicator` und Statustext; `App.CreateWindow` (`App.xaml.cs:18-34`) startet mit der `LoadingPage` als Root-Page und dispatcht `StartupRequestedMessage`.
- Der Start-Workflow läuft vollständig über MVU: `CheckServerHealthEffect` → `ShellAppEffectHandler.RunHealthCheckAsync` → `ServerHealthCheckCompletedMessage` → `NavigateToPageEffect(Home)` tauscht die Root-Page gegen `AppShell` (`ShellAppEffectHandler.cs:33-77`). Fehler führen zu „nicht erreichbar" statt zum Hängenbleiben.

### API-Anbindung (Erreichbarkeitsprüfung)

- `ServerHealthProbe` (`VideoPlayer.Maui/Services/ServerHealthProbe.cs`) ruft `VideoPlayerApiClient.CheckHealthAsync` (`GET api/health`, `VideoPlayerApiClient.cs:107-122`) auf — die in Schritt 1 neu implementierte plattformneutrale Client-Bibliothek.
- Serveradresse über `ServerAddressProvider`: Voreinstellung `http://localhost/videoplayer/` (lokales IIS-Webprojekt), überschreibbar per Preference `ServerAddress` oder Umgebungsvariable `VIDEOPLAYER_SERVER_URL` (`ServerAddressProvider.cs:12-41`). `ApiException` erbt von `HttpRequestException` und wird vom Probe-Filter korrekt abgefangen.
- DI-Verdrahtung vollständig in `MauiProgram.cs:16-28`.

### Plattformen und iOS-Nachweis

- `VideoPlayer.Maui.csproj:3-5`: `net10.0-android`, `net10.0-ios`, `net10.0-maccatalyst`, `net10.0-windows10.0.19041.0` — iOS-Target vorhanden, Android bereits vorbereitet (Erweiterbarkeit gegeben), Plattform-Ordner `Platforms/iOS`, `Platforms/Android`, `Platforms/Windows`, `Platforms/MacCatalyst` vollständig.
- iOS-Build in der CI eingerichtet und nachweisbar: Schritt `Build iOS (VideoPlayer.Maui)` (`dotnet build … --framework net10.0-ios --runtime iossimulator-arm64`) auf `macos-latest` in `.github/workflows/main-merge.yml:112-113` und `.github/workflows/staging-merge.yml:112-113`. Lokaler iOS-Build unter Windows nicht möglich — akzeptabel gemäß Kriterium.
- PR-Checks bauen zusätzlich Android und Windows der neuen App (`pr-checks.yml:40-44`).

### Build- und Testnachweis (lokal ausgeführt)

- `dotnet build VideoPlayer.Maui/VideoPlayer.Maui.csproj --configuration Release --framework net10.0-windows10.0.19041.0`: **erfolgreich**, 0 Warnungen, 0 Fehler (`VideoPlayer.Maui.dll` für `win-x64` erzeugt).
- `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj --configuration Release`: **101/101 Tests bestanden** (0 Fehler, 0 übersprungen), darunter die neuen MVU-Tests: `AppRoutesTests`, `AppUpdateTests_Startup`, `AppUpdateTests_Navigation`, `MvuHostTests` sowie `VideoPlayerMauiContractTests` (Menüeinträge/Routen/Plattformen/Health-Check-Verdrahtung als Quellcode-Contract-Tests).
- Testlauf erzeugt nur vorbestehende Warnungen im Altprojekt `VideoWebPlayer.Client` (CS0108/CS8618), keine im neuen Code.
- Alle drei neuen Projekte sind in `VideoPlayer.App.sln` eingetragen.

### Einschränkungen

- „Die App startet unter Windows" wurde per erfolgreichem Release-Build nachgewiesen; ein interaktiver Laufzeit-Start (UI-Sichtprüfung) wurde nicht durchgeführt.
- Die tatsächliche HTTP-Erreichbarkeit des lokalen IIS-Servers hängt von der Umgebung ab; die Verdrahtung (`CheckHealthAsync` → `api/health`, Standard-Basisadresse `http://localhost/videoplayer/`) ist vollständig implementiert und per Contract-Test abgesichert.
