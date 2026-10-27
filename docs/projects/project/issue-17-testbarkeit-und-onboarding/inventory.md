# Bestandsaufnahme — Projekt: iOS-Deployment, Developer-API, E2E-Tests (FlaUI), Serververbindungs-UI, QR-Bootstrap

Basis: `requirement.md` (Issue #17, Sammel-Issue mit fünf Entwicklungsschritten in fester Reihenfolge).
Untersuchte Repositories:

- **App-Repo** `VideoPlayer-App` — lokaler Checkout `D:\Repositories\softwareschmiede\8e923b89-04a9-47c4-b487-b58ac5bce398`
- **Server-Repo** `VideoPlayer` — lokaler Checkout `D:\Repositories\softwareschmiede\487c3041-d72b-486e-811f-9ed8db04630b` (nur Schnittstellen-/Koordinationssicht für Schritt 5)

Die Bestandsaufnahme trennt belegte Beobachtungen (mit Datei-/Zeilenverweisen) von Annahmen und offenen Punkten.

## Übersicht

| # | Schritt | Zentrale Befunde | Hauptrisiken / offene Punkte |
|---|---------|------------------|------------------------------|
| 1 | iOS-Deploy-Fix (MSB1006) | Alle drei MSBuild-Aufrufe in `scripts/iOS-Deployment.ps1` laufen über Response-Dateien (`Add-PropertyLine` → `-p:Name="Wert"`, `New-ResponseFile`); `-p:VideoPlayerApiToken` wird an den Stellen ~725, ~884, ~976 eingespeist | Ursache ist eine Hypothese (Token-Quoting/`#`-Kommentarzeichen/Zeilenumbruch im Token), noch nicht bewiesen; kein reproduzierender Lauf dokumentiert |
| 2 | Developer-API | `MvuHost` bietet `Model`-Zugriff + `DispatchAsync` (serialisiert); `MauiProgram` ist die DI-Wurzel; `App.CreateWindow()` ist der Lifecycle-Einstieg | Kein HTTP-Server im Projekt vorhanden; Loopback-Port-0-Bindung und Metadaten-Datei sind zu entwerfen; Release-Ausschluss muss compile-time gesichert sein |
| 3 | E2E-Tests (FlaUI) | Windows-App ist unpackaged (`WindowsPackageType=None`), echte `VideoPlayer.Maui.exe` direkt startbar; Testprojekt nutzt heute nur Quelltext-Contract-Tests (kein UI-Run) | CollectionView-Items fehlen im Windows-UIA-Baum (belegt aus #15-Verifikation); CI hat keinen interaktiven Desktop, Billing-Blocker; Publish-Pfad/Exe-Ort der neuen App wird von keinem Workflow erzeugt |
| 4 | Serververbindungs-UI | `ServerSetupPage` ist eine schlanke Workflow-Seite (Grid 113 Zeilen), Zustand komplett aus `AppModel`; `Vp*`-Design-Tokens + implizite Styles existieren; `HomePage` liefert Karten-/Gradient-Muster | AutomationIds und Strukturmerkmale sind per Contract-Tests gepinnt — Umbau muss die Pins respektieren bzw. Tests anpassen |
| 5 | QR-Bootstrap | App-Seite hat fertige Pairing-Maschinerie (ECDH P-256 + AES-256-GCM, `DevicePairingClient`, `PairingCrypto`, Token-Ablage); Server hat `PairingCode.CreatedByUserId`, atomaren Verbrauch, `AuthorizationTokenService` (JWT, `IsAdmin`-Claim), `LoginIpBlockService` | Kein QR-/Kamera-Paket und keine Kamera-Berechtigungen; Ticket-→Usersession-Kanal existiert nicht (Exchange liefert nur Geräte-Token); kein Refresh-Token-Mechanismus; Endpunktwahl (reuse vs. neu) offen |

## Themenbereiche

### 1. iOS-Deployment — `scripts/iOS-Deployment.ps1` reparieren

**Ist-Stand**

- Das Skript (~1125 Zeilen) unterstützt `build`, `simulator`, `device`, `store`, `upload`, `list`, `menu` und läuft lokal auf macOS sowie unter Windows mit Pair-to-Mac (`$isWindows`-Zweige, `Invoke-OnMac` via SSH, Zeilen ~177, ~491).
- **Alle** MSBuild-Aufrufe gehen über Response-Dateien:
  - `Add-PropertyLine` (Z. 685–693) schreibt `-p:Name="Wert"` mit literalem Doppelquote; leere Werte werden übersprungen (`if (-not $Value) { return }`, Z. 691).
  - `Add-MaskedDisplay` (Z. 695–708) maskiert `ServerPassword` und `VideoPlayerApiToken` als `-p:Name=***` in der Konsolenanzeige.
  - `New-ResponseFile` (Z. 710–716) legt `.ios-deploy-<guid>.rsp` im Repo-Root via `[System.IO.File]::WriteAllLines` an; alle drei Aufrufer löschen die Datei im `finally`.
- Drei Aufrufpfade:
  - `Invoke-Build` (Z. 718–793): `dotnet build|publish $projectPath -f $framework -c $config @rsp` (Z. 766/771). Mit Codesigning wird `publish` + `ArchiveOnBuild=true` + `CodesignKey`/`CodesignProvision`/`CodesignEntitlements` gesetzt (Z. 746–755) — das ist der Store-Pfad (`Invoke-Store`, Z. 657–670).
  - `Invoke-Run` (Z. 865–953): `dotnet build -t:Run … @rsp` (Z. 937); unter Windows mit Fehlertext abgebrochen (Z. 867–874).
  - `Invoke-DeviceViaSsh` (Z. 965–996): `dotnet publish … "@$rspPath"` (Z. 988) — Windows-Pair-to-Mac-Pfad für Geräte.
- `-VideoPlayerApiToken` ist Skript-Parameter mit Default `$env:VIDEOWEBPLAYER_MAUI_API_TOKEN` (Z. 75–76) und wird in allen drei Response-Dateien eingespeist (Z. 725, 884, 976); die Anzeige ist maskiert (Z. 737, 896).
- `.ios-deploy.user.json` enthält nur Store-Signierung (`CodesignKey`, `CodesignProvision`, leeres `Device`) — **kein** Token.
- `Update-BuildNumber` (Z. 428–438) inkrementiert `<ApplicationVersion>` in der csproj per Regex-Ersetzung; `Invoke-Store` ruft es bei fehlendem `-NoBumpBuildNumber` (Z. 661). `.github/scripts/Set-Version.ps1` setzt `ApplicationDisplayVersion`/`ApplicationVersion` aus SemVer-Tags für beide App-Projekte.
- `VideoPlayer.Maui.csproj`: iOS-TFM `net10.0-ios` (Z. 4), Codesign-Defaults `Apple Development … 466MVK852A` / `VS: WildCard Development` (Z. 33–36), `ApplicationDisplayVersion=0.1`, `ApplicationVersion=4` (Z. 20–21).
- Der Token gelangt per MSBuild-Eigenschaft `VideoPlayerApiToken` in `VideoPlayer.ApiClient.csproj` (Z. 11: Platzhalter-Default `&lt;MAUI_CLIENT_API_TOKEN&gt;`; Z. 14–17: `AssemblyMetadata`-Attribut `VideoPlayer.ApiClient.ApiToken`), zur Laufzeit aufgelöst durch `ClientGate` (`VideoPlayer.ApiClient/ClientGate.cs`, Z. 22, 25, 65–91).

**MSB1006-Hypothesen (zu verifizieren — nichts davon ist belegt)**

- Response-Datei-Zeilen `-p:Name="Wert"`: MSBuild verarbeitet Quotes in Response-Dateien anders als die Shell; ein `"` im Token würde die Zeile spalten.
- Ein `#` im Token-Wert könnte den Rest der Zeile als Kommentar verwerfen (MSBuild-Response-Dateien erlauben `#`-Kommentare) → unterminierte Quote oder leerer Property-Name-Rest.
- Ein Zeilenumbruch im übergebenen Token (z. B. aus `.env`-Copy-Paste) erzeugt eine zweite, ungültige Argumentzeile.
- 257-Zeichen-Token: Länge allein ist für MSBuild unkritisch; relevant sind nur o. g. Zeichenklassen.
- `WriteAllLines` ohne explizites Encoding (Z. 714): auf Windows PowerShell 5.1 UTF-8 ohne BOM — vermutlich unkritisch, aber als Einflussgröße festzuhalten.

**Konventionen**: Secrets nur maskiert anzeigen; Properties ausschließlich über Response-Datei; Fehler → `exit 1` mit roter Meldung; IPA-Suche unter `VideoPlayer.Maui/bin/$config/$framework/$rid` (Z. 779) bzw. `…/publish` (Z. 1000).

**Einschränkungen**: `store`-Pfad benötigt Transporter-App (iTMSTransporter, Z. 645–651); `device`/`simulator` via `-t:Run` nur auf macOS; der Geräte-Restnachweis aus Issue #15 ist im aktuellen Umfeld nicht ausführbar (Voraussetzung laut `continue.md` von #15: echte iOS-/Android-Geräte).

**Wiederverwendbare Bausteine**: `Add-PropertyLine`/`New-ResponseFile`-Muster (Ziel des Fixes ist vermutlich eine sanitizing/escaping-sichere Variante davon), `Add-MaskedDisplay`, `.ios-deploy.user.json`-Mechanismus für benutzerlokale Signaturdaten.

### 2. Developer-API (Debug/Opt-in, nur `127.0.0.1`, Port 0)

**Ist-Stand**

- **MVU-Boundary**: `MvuHost` (`VideoPlayer.Mvu/MvuHost.cs`) — `Model` (Z. 30) liefert den unveränderlichen `AppModel`-Snapshot; `DispatchAsync` (Z. 44–68) serialisiert Updates über `SemaphoreSlim` (Z. 15) und führt Effekte sequenziell über `IAppEffectHandler` aus; `ModelChanged` (Z. 33) und `PropertyChanged` (Z. 36) feuern nach jedem Update.
- **Modellauszug** (`VideoPlayer.Mvu/AppModel.cs`): `CurrentPage` (Z. 13), `IsStartupRunning` (16), `ServerHealth` (19), `ConnectionState` (26), `IsLoginRunning`/`LoginError`/`LoginUsername` (29–38), `HasOfflineData`/`IsOnline`/`IsSyncRunning` (41–54), `Settings` (61), `KnownServers`/`ActiveServerAddress`/`DiscoveredServers` (64–73), `IsDiscoveryRunning`/`DiscoveryAttempted`/`IsServerCheckRunning`/`ServerSetupError` (76–88), `IsPairingRequired`/`IsPairingRunning`/`PairingError` (95–104), Startseiten-Listen (111–129), `SourcesOverview`/`SourceBrowser`/`MediaDetails`/`FavoritesPage`/`Downloads`/`Profile`/`Player` (167–185), `StorageNotice` (211). Alle Felder sind UI-agnostisch und sicher lesbar; sensible Daten (Passwort, Tokens) stehen nicht im Modell.
- **DI-Registrierung**: `VideoPlayer.Maui/MauiProgram.cs` — `MvuHost` Singleton (Z. 44), `IAppEffectHandler`/`ShellAppEffectHandler` (Z. 43), `AppShell` Singleton (Z. 101), alle Pages transient (Z. 102–115). Einziger `#if DEBUG`-Block: `builder.Logging.AddDebug()` (Z. 123–125).
- **Lifecycle-Einstieg**: `App.xaml.cs` — `CreateWindow` (Z. 32–48) startet mit `LoadingPage` und dispatcht einmalig `StartupRequestedMessage` (Z. 50–60). Ein Dev-API-Server kann hier (oder über einen DI-Singleton in `MauiProgram`) gestartet und beim App-Ende gestoppt werden.
- **Elementbaum**: XAML-Elemente tragen `AutomationId` (u. a. `ServerSetupPage.xaml` Z. 20/36/51/56/62/80/84/88/99/105/109; `LoginPage.xaml` Z. 14/21/26/31/36/41; `HomePage.xaml` Z. 19–138). `GET /tree` kann die logische Elementhierarchie via `IVisualTreeElement` traversieren; **CollectionView-Items** lassen sich aus `ItemsSource` (z. B. `model.KnownServers`, `model.DiscoveredServers`) plus ItemTemplate ableiten — sie fehlen unter Windows im UIA-Baum.
- **Steuerung**: `POST /invoke`/`set-text` benötigt Element-Lookup per `AutomationId` über den Visual Tree + `MainThread.InvokeOnMainThreadAsync`; `POST /dispatch` kann `AppMessage`-Records erzeugen (Basistyp `abstract record AppMessage`, `VideoPlayer.Mvu/AppMessage.cs` Z. 7) — umgeht die echte Event-Kette, daher im Issue mit Vorsicht versehen.

**Konventionen**: MVU — Zustand nur über Messages ändern; `MainThread` für UI-Zugriffe (Muster: `ServerSetupPage.xaml.cs` Z. 45, `ShellAppEffectHandler.cs` Z. 1177); Debug-Verhalten bisher nur via `#if DEBUG`/Conditional-Attribut (`ClientGate.WriteDiagnostic`, `ClientGate.cs` Z. 142).

**Einschränkungen**: kein HTTP-Server-Paket im Projekt (nur `Microsoft.AspNetCore.SignalR.Client`, csproj Z. 54); Port-0-Verfahren muss den vom OS vergebenen Port in eine Metadatendatei im Programmverzeichnis schreiben — Schreibbarkeit von `AppContext.BaseDirectory` ist bei der unpackageden Windows-App gegeben, bei anderen Plattformen zu prüfen (Opt-in wird primär Windows betreffen); API darf im Release-Build nicht existieren → sauberste Absicherung ist Compile-Time (`#if DEBUG || DEV_API` bzw. eigene Konstante) statt Laufzeitflag.

**Wiederverwendbare Bausteine**: `MvuHost.Model`/`DispatchAsync`/`ModelChanged`; `AppModel`-Records (System.Text.Json-serialisierbar); `AppRoutes` (`VideoPlayer.Mvu/AppRoutes.cs`) für `CurrentPage`↔Route-Abbildung; `AutomationId`-Konvention; `MainThread.InvokeOnMainThreadAsync`-Muster.

### 3. E2E-Tests mit FlaUI

**Ist-Stand**

- `VideoWebPlayer.Maui.Tests` ist ein plattformneutrales xUnit-Projekt (~80 Testdateien). Zwei Testarten:
  - **MVU-/Service-Tests**: `AppUpdateTests_*` (23 Dateien), `MvuHostTests`, `AuthSessionServiceTests`, `DevicePairingClientTests`, `PairingCryptoTests`, `PairingCodeTests`, `DeviceTokenServiceTests`, `KnownServersServiceTests`, `ServerDiscoveryServiceTests` u. a.
  - **Quelltext-Contract-Tests**: `ServerSetupContractTests`, `PairingContractTests`, `DesignTokensContractTests`, `VideoPlayerMauiContractTests`, `HomePageContractTests` u. a. — lesen XAML/CS als Text und pinnen Struktur/AutomationIds (Muster: `RepositoryRoot.Find()` in `Stubs/RepositoryRoot.cs`, `ServerSetupContractTests.cs` Z. 13–16).
- Es gibt **keine** UI-instantiierten Tests und kein FlaUI-Paket; das Testprojekt kann keine MAUI-Targets ausführen (Kommentar `ServerSetupContractTests.cs` Z. 7–9).
- **Windows-Build**: `VideoPlayer.Maui.csproj` — `WindowsPackageType=None` (Z. 23), TFM `net10.0-windows10.0.19041.0` (Z. 5), `OutputType=Exe` (Z. 6). Die App startet damit als gewöhnliche `.exe` (kein MSIX, kein AppxManifest-Zwang) — für FlaUI direkt launchbar. Build: `dotnet build VideoPlayer.Maui -f net10.0-windows10.0.19041.0` (Ausgabe typischerweise `bin/<config>/net10.0-windows10.0.19041.0/<rid>/VideoPlayer.Maui.exe` — genauer RID-Unterordner vor Ort verifizieren).
- **CI**:
  - `.github/workflows/pr-checks.yml` (57 Zeilen): `windows-latest`, .NET 10, MAUI-Workload, baut alte + neue App (Android + Windows) und führt `dotnet test` + `dotnet format whitespace --verify` aus (Z. 28–50). Kein interaktives Desktop-Setup, keine FlaUI-Stufe.
  - `staging-merge.yml`/`main-merge.yml`: Windows-Job publisht nur die **alte** `VideoWebPlayer.Maui` (Z. 68–71, MSIX-Suche Z. 85); die neue `VideoPlayer.Maui` wird nur als iOS-Simulator-Build erzeugt (Z. 110–113). Ein Release-Publish der neuen Windows-Exe existiert noch nicht.
- **UIA-Lücke (belegt)**: In der #15-Live-Verifikation (`docs/features/task/issue-15-…/live-verification.md` Z. 60–65) mussten CollectionView-Items per Koordinatenklick adressiert werden — Elemente der Listen erscheinen nicht im Windows-UIA-Baum; Zustandslesen (`CurrentPage`/`ConnectionState`) war nur über Screenshots/Serverlogs möglich.

**Konventionen**: Tests liegen flach im Testprojekt; Contract-Tests pinnen UI-Verdrahtung über Quelltext; MVU-Tests arbeiten gegen `AppUpdate.Update` mit `AppTransition`-Assertions.

**Einschränkungen**: GitHub-Actions-Runner sind nicht interaktiv — FlaUI auf `windows-latest` benötigt entweder einen Self-Hosted-Runner mit Desktop-Session oder eine Session-0-Umgehung; laut Requirement ist CI-Integration optional (Billing-Blocker). FlaUI deckt die CollectionView-Lücke nicht ab — dafür ist die Developer-API (Schritt 2) das komplementäre Instrument. App-Start braucht Netz-/Server-Umgebung: ohne erreichbaren Server landet der Workflow auf `NeedsServerSetup`/`ServerSetupPage` — das ist für die E2E-Szenarien (Server-Setup, Pairing, Serverwechsel, Anmeldung) sogar der gewünschte Ausgangszustand; ein Mock-/Testserver oder ein echter Server-Checkout wird benötigt.

**Wiederverwendbare Bausteine**: `RepositoryRoot`-Stub-Muster; AutomationId-Konvention als FlaUI-Selektor-Basis; MVU-Messages für deterministische Szenarien (über Dev-API-`dispatch`, falls gewählt); vorhandene Fehlertext-Konstanten in `AppUpdate` (Z. 12–54) als Assertions.

### 4. Serververbindungs-UI modernisieren

**Ist-Stand**

- `VideoPlayer.Maui/Pages/ServerSetupPage.xaml` (113 Zeilen): Grid `Auto,*,Auto` — Kopfbereich (Titel, Hinweis, aktiver Server Z. 13–22), Listenbereich (bekannte Server `KnownServersView` Z. 34–47; Discovery `DiscoverButton`/`DiscoveryIndicator`/`DiscoveryEmptyLabel` Z. 50–58; gefundene Server `DiscoveredServersView` Z. 60–73), Fußbereich (manuelle Eingabe `AddressEntry`/`ConnectButton`/`CheckIndicator`/`ErrorLabel` Z. 79–88; Pairing-Abschnitt `PairingSection` mit `PairingCodeEntry`/`PairButton`/`PairingIndicator`/`PairingErrorLabel` Z. 92–110).
- Code-Behind `ServerSetupPage.xaml.cs`: `UpdateUi` (Z. 48–82) setzt Sichtbarkeit/Texte/Indicators rein aus `AppModel`; Event-Handler dispatchen `ServerDiscoveryRequestedMessage`, `ServerAddressSubmittedMessage`, `PairingCodeSubmittedMessage`, `DiscoveredServerSelectedMessage`, `ServerSwitchRequestedMessage` (Z. 84–115); `OnAppearing` lädt Serverliste via `LoadServersRequestedMessage` (Z. 33).
- **Zustandsfeedback heute**: `IsServerCheckRunning` → `CheckIndicator`, Fehler → `ErrorLabel` (`VpDanger`); `ServerHealth`/`ServerHealthText` existiert im Modell (`AppModel.cs` Z. 19, 188–194), wird auf der Seite aber nur indirekt genutzt — die Stati „erreichbar/nicht erreichbar" sind textlich schon vorhanden (`ServerHealthText`-Mapping).
- **Design-System**: `Resources/Styles/Colors.xaml` (Z. 12–58) — `VpBg`, `VpBgDeep`, `VpSurfaceLow/…/Bright`, `VpText`/`Muted`/`Soft`, `VpBorder`/`BorderStrong`, `VpPrimary(#e50914)`/`Hover`/`Soft`, `VpSecondary(#00a8e8)`/`Soft`, `VpDanger`, Overlay/Shadow/Badge/Hero-Gradient-Tokens. `Resources/Styles/Styles.xaml` — implizite Styles für `Border` (Z. 19–25), `Button` (Z. 31 ff., inkl. `IconButton` Z. 64), `Entry` (149), `Switch` (366), `Label` (193, `Headline` 212, `SubHeadline` 219) u. a.
- **Referenzseiten**: `HomePage.xaml` (Hero mit `Border`+`LinearGradientBrush` Z. 35–72, Banner-Karten `StrokeShape="RoundRectangle 8"`/`VpBorderStrong` Z. 113–138) ist das stärkste Gestaltungsvorbild; `SettingsPage.xaml` (sektionsbasiert, Z. 19/29/65/87) für einfache Abschnitte; `LoginPage.xaml` für Formular-Feedback.
- **Route**: `serversetup` als geschobene Route in `AppShell.xaml.cs` Z. 59; bewusst **kein** FlyoutItem (Contract-Test `AppShell_RegistersServerSetupRoute_WithoutFlyoutItem`, `ServerSetupContractTests.cs` Z. 88–98); Einstieg „Server wechseln" aus `SettingsPage.xaml` Z. 89–92.

**Konventionen**: Zustand ausschließlich aus dem Modell; AutomationIds auf allen interaktiven Elementen; `Vp*`–Tokens statt harter Farben (per `DesignTokensContractTests` gepinnt); Dark-only (`UserAppTheme = AppTheme.Dark`, `App.xaml.cs` Z. 28).

**Einschränkungen**: MVU-Fluss bleibt unverändert (Workflow-Seite, `NavigateToPageEffect`-Navigation über `AppRoutes`); gepinnte AutomationIds (`KnownServersView`, `ActiveServerLabel`, `DiscoverButton`, `DiscoveredServersView`, `DiscoveryEmptyLabel`, `AddressEntry`, `ConnectButton`, `ErrorLabel`, `PairingCodeEntry`, `PairButton`, `PairingErrorLabel`) und XAML-Merkmale (kein `MaxLength` am Pairing-Eintrag — `PairingContractTests` Z. 32) müssen erhalten bleiben oder die Contract-Tests werden bewusst angepasst.

**Wiederverwendbare Bausteine**: `Vp*` Token-Satz, implizite `Border`/`Button`/`Entry`-Styles, Kartenmuster aus `HomePage` (`Border` + `RoundRectangle`, Gradient-Layering), Status-Texte aus `AppModel.ServerHealthText`/`StartupStatusText`, `PairingError`-Konstanten in `AppUpdate` (Z. 35–51).

### 5. QR-Bootstrap (App-Anteil + Server-Koordination)

**Ist-Stand — App**

- **Pairing-Client**: `VideoPlayer.ApiClient/DevicePairingClient.cs` — `ExchangeEndpoint = "api/pairing/exchange"` (Z. 18), `ExchangeAsync(baseAddress, code, deviceName)` (Z. 60–122): flüchtiges ECDH-Paar, POST ohne Auth-Header, Timeout-Empfehlung 15 s (Z. 26), Fehler als `ApiException` mit Statuscode.
- **Kryptografie**: `VideoPlayer.ApiClient/PairingCrypto.cs` — ECDH NIST P-256 (Z. 23–24), SPKI-Export (Z. 29–33), AES-256-GCM `nonce(12)‖ct‖tag(16)` (Z. 44–99). Direkt für den Ticket-Austausch wiederverwendbar.
- **Code-Validierung**: `VideoPlayer.Mvu/PairingCode.cs` — Alphabet `ABCDEFGHJKMNPQRSTUVWXYZ23456789` (Z. 20), `MinLength=4` (Z. 14), `TryNormalize` (Z. 30–46). **Achtung**: das ist die Prüfung des *Admin-Codes*, nicht des QR-Tickets — ein `t=<ticket>`-Parameter ist opak (Base64url-artig) und braucht einen eigenen Parser; die Alphabet-Prüfung darf nicht darauf angewendet werden.
- **Adress-Normalisierung**: `VideoPlayer.Storage/ServerAddress.cs` — `TryNormalize` (Z. 19–52) erzeugt kanonische `scheme://host[:port]/pfad/`-Form (Vergleichsschlüssel von `known_servers`). Für die QR-URL `https://<server>/pairing?t=…` muss die *Basisadresse* (ohne `/pairing`-Pfadsegment) extrahiert und normalisiert werden — Achtung bei Servern unter einem Unterpfad (`http://server/videoplayer/`, Placeholder `AddressEntry` Z. 81).
- **Token-/Session-Ablage**: `VideoPlayer.Storage/AppSecretsService.cs` — `SetDeviceTokenAsync(serverAddress, token)` (Z. 125–128, profil+adresspräfixiert über `DeviceTokenSecretKey` Z. 143–149), `SetAuthTokenAsync(token)` (Z. 89–93, `AuthToken`-Key in SecureStorage), `SetCredentialsAsync` (Z. 65–69). `SecureStorageSecretStore` (`VideoPlayer.Maui/Services/SecureStorageSecretStore.cs`) ist der `SecureStorage`-Adapter (iOS-Keychain, Windows-Credential-Ablage).
- **Session-Betrieb**: `VideoPlayer.Mvu/AuthSessionService.cs` — `Client`/`ClientBaseAddress` (Z. 75–78), `TryStoredLoginAsync` validiert gespeicherte Tokens via `api/favorites` (Z. 189–230), `SilentReloginAsync` als `TokenRefresher` (Z. 303–351) fällt bei 401 **auf gespeicherte Zugangsdaten** zurück — ohne Passwort gibt es heute keine Erneuerung (offene Frage Refresh/gerätegebundene Session, requirement Z. 128).
- **MVU-Einordnung**: `PairingCodeSubmittedMessage` → `ExchangePairingCodeEffect` → `PairingCompletedMessage`; Erfolg → `AttemptAutoLoginEffect` → `EnterConnected` (`AppUpdate.cs` Z. 479–543, 681). Effect-Handler `ShellAppEffectHandler.ExchangePairingCodeAsync` (Z. 1213–1288): Stale-Guard, `SetPairedTokenIfCurrentAsync`, `authSession.Client?.SetApiKey`, `PairingStatus`-Mapping (401/429/400/404/Timeout). `GetDeviceName()` (Z. 1296 ff.) liefert `DeviceInfo.Name` ≤200 Zeichen.
- **Kamera/QR**: **nicht vorhanden** — kein ZXing-/Camera-Paket in `VideoPlayer.Maui.csproj` (Z. 50–56: nur `CommunityToolkit.Maui.MediaElement`, SignalR, Logging); `Platforms/iOS/Info.plist` hat **kein** `NSCameraUsageDescription`; `Platforms/Android/AndroidManifest.xml` hat **keine** `CAMERA`-Permission (nur `INTERNET`, `ACCESS_NETWORK_STATE`, Z. 4–5); Windows `Package.appxmanifest` deklariert nur `runFullTrust`.
- **UI-Einstieg**: `ServerSetupPage.xaml` Z. 92–110 (PairingSection) bzw. Kopfbereich — der QR-/Ticket-Einstieg muss dort (oder als eigene Section) angebunden werden; Synergie mit Schritt 4.

**Ist-Stand — Server (nur Koordination, kein Umbau hier)**

- `VideoWebPlayer/Data/PairingCode.cs` (Z. 6–32): `Id`, `CodeHash` (SHA-256), `CreatedAtUtc`, `ExpiresAtUtc`, `ConsumedAtUtc`, **`CreatedByUserId`** — das Modell kann ein benutzergebundenes Ticket tragen; für die TV-Gegenrichtung (Gerät zeigt Code) fehlt eine Richtungs-/Rollenunterscheidung (Datenmodell-Perspektive, requirement Z. 68).
- `VideoWebPlayer/Services/PairingService.cs`: `CreatePairingCodeAsync(createdByUserId)` (Z. 154–180; Alphabet Z. 134, Default-Länge 8, TTL 5 min, konfigurierbar `Pairing:CodeLength`/`Pairing:CodeTtlMinutes` Z. 160–161); `ExchangeAsync` (Z. 182–233): atomarer Verbrauch via `ExecuteUpdateAsync` (Z. 217–219), ECDH-Server-Key + `DeriveKeyFromHmac` (Z. 225–226), `IDeviceTokenService.IssueAsync(deviceName, CreatedByUserId)` (Z. 227), AES-GCM-Layout wie appseitig (Z. 260–276). **Lieferumfang heute: nur Geräte-Token** — ein Bootstrap-Endpunkt müsste zusätzlich eine Usersession (JWT) erstellen.
- `VideoWebPlayer/Controllers/PairingController.cs` (Z. 11–66): `POST api/pairing/exchange`, **öffentlich** (kein `ApiTokenCheck`/`BearerTokenCheck`), Rate-Limit über `ILoginIpBlockService` (Z. 45–47, 63–64); Fehler-Mapping 400/401/429.
- `VideoWebPlayer/Services/LoginIpBlockService.cs`: IP-basiert, `Threshold = 5` (Z. 61), persistiert in `BlockedLoginIps` (Z. 152–164). Ein **Rate-Limit je Benutzer** (requirement Z. 73) existiert nicht — wäre serverseitig neu zu bauen (Muster vorhanden: ConcurrentDictionary+Persistenz).
- **JWT**: `VideoWebPlayer/Services/Authentication/IAuthService.cs` — `AuthorizationTokenService.CreateToken(ApplicationUser)` (Z. 59–87): Claims `Sub`/`NameIdentifier`=UserId, `Email`, `Name`, `IsAdmin` (`"True"/"False"`); Issuer `Jwt:Issuer`; **12 h Gültigkeit, kein Refresh-Token**. `BearerTokenCheckAttribute` validiert Issuer/Audience/Lifetime/Key. `AuthController`: `POST api/auth/login` mit `[ApiTokenCheck(ApiTokenScope.MauiOnly)]` (Z. 41–43), `POST api/auth/impersonate` mit ApiToken+Bearer+Admin (Z. 61–74).
- **Admin-Mechanik**: `ApplicationUser.IsAdmin` (`Data/ApplicationUser.cs` Z. 17) → Claim `IsAdmin`; Blazor-Seiten prüfen `user.HasClaim("IsAdmin","True")` (`Components/Pages/Admin/Devices.razor` Z. 197; `NavMenu.razor` Z. 57/124).
- **Profil-/Geräte-UI**: Pairing-Code-Erzeugung existiert **nur** adminseitig unter `/admin/devices` (`Devices.razor` Z. 237–252: `CreatePairingCodeAsync(currentUserId)`; Anzeige mit `data-testid` Z. 54–58). Eine benutzerbezogene Profilseite mit „Gerät koppeln" existiert nicht — nächstliegender Ort wäre `Components/Account/Pages/Manage/*` (Identity-Gerüst) oder eine neue Seite; `NavMenu` verlinkt `Account/Manage` (Z. 70–72).
- **Device-Tokens**: `VideoWebPlayer/Services/DeviceTokenService.cs` — `IssueAsync` erzeugt Base64url(32B) (Z. 65), speichert SHA-256-Hash in `PairedDevices` mit `CreatedByUserId` (Z. 67–80); `ApiTokenCheckAttribute` (`Controllers/Attributes/ApiTokenCheckAttribute.cs` Z. 85–91) akzeptiert Device-Tokens für Scope `MauiOnly`.
- **QR-Erzeugung**: kein QR-Paket im Server-csproj (`VideoWebPlayer.csproj` Z. 18–38) — serverseitige Erzeugung (z. B. QRCoder oder JS) ist Teil des Server-Lifecycles.
- **DTO-Abgleich**: `VideoWebPlayer.Client/Models/PairingExchangeRequest|Response.cs` (Server-Repo) und `VideoPlayer.ApiClient/Models/*` (App-Repo) sind separate Kopien desselben Vertrags (`code`, `clientPublicKey`, `deviceName` / `serverPublicKey`, `encryptedToken`) — ein neuer Bootstrap-Vertrag muss in beiden Repos gepflegt werden.

**Konventionen**: App — Fehler als `PairingStatus`→deutsche Fehlertext-Konstanten (`AppUpdate.cs` Z. 35–51); Secrets nie in normale Ablage; Tokens adressspezifisch. Server — atomarer Verbrauch via `ExecuteUpdateAsync`; `data-testid`-Attribute in Admin-UI; Deutsche UI-Texte.

**Einschränkungen**: Endpunktwahl (`exchange` erweitern vs. `api/pairing/bootstrap` neu) hängt vom Ticket-Vertrag des Server-Lifecycles ab; ohne Session-Erneuerung fällt die App nach JWT-Ablauf (12 h) auf `SessionExpired`/`NeedsLogin` zurück — ein Bootstrap ohne Passwort-Ablage bricht dann ab (`AuthSessionService.SilentReloginAsync` Z. 303–351); QR-Adresse ist servergerendert (`Scheme+Host` des Requests, offene Frage Z. 129) — die App muss „Adresse nicht erreichbar" verständlich melden (`PairingStatus.ServerUnreachable`-Muster vorhanden).

**Wiederverwendbare Bausteine**: `PairingCrypto` + `DevicePairingClient` (ECDH/AES-GCM-Kanal unverändert nutzbar); `ServerAddress.TryNormalize`; `AppSecretsService.SetDeviceTokenAsync`/`SetAuthTokenAsync`; `DeviceTokenService.SetPairedTokenIfCurrentAsync` + `SetApiKey`-Aktivierung (`ShellAppEffectHandler.cs` Z. 1244–1249); `EnterConnected`-Pfad; `PairingStatus`-Fehlermapping. Serverseitig: `PairingCode`-Tabelle, `ExecuteUpdateAsync`-Verbrauch, `AuthorizationTokenService.CreateToken`, `LoginIpBlockService`-Muster.

## Detaildokumente

| Datei | Inhalt |
|-------|--------|
| `inventory/ios-deployment.md` | Aufrufpfade/Response-Datei-Mechanik, Token-Einspeisung, MSB1006-Hypothesen, Fix-Ansätze |
| `inventory/developer-api.md` | `MvuHost`-/`AppModel`-Referenz, Endpunkte-Entwurf, Aktivierung, Sicherheit |
| `inventory/flaui-e2e.md` | Testlandschaft, Build-/Startpfad der Windows-Exe, UIA-Grenzen, CI-Szenarien |
| `inventory/server-setup-ui.md` | Seitenstruktur, AutomationId-/Contract-Pins, Token-/Style-Referenz, Umbau-Spielräume |
| `inventory/qr-bootstrap-app.md` | Pairing-/Crypto-/Storage-Maschinerie, Session-Ablage, Kamera-Lücke, MVU-Einordnung |
| `inventory/qr-bootstrap-server.md` | `PairingCode`-/`PairingService`-/`PairingController`-Referenz, JWT/Admin/Rate-Limit, Koordinationspunkte |

## Offene Punkte (aus der Inventur, zusätzlich zu requirement.md Z. 125–131)

1. **MSB1006-Reproduktion fehlt**: Es gibt keine dokumentierte Fehlerzeile; der Fix sollte eine sanitizing-sichere `Add-PropertyLine`-Variante (Escaping/Weglassen von Quotes oder Zeichenfilter) plus ggf. einen Contract-Test auf die Response-Datei-Erzeugung umfassen.
2. **Port-0-Mechanik der Dev-API**: `HttpListener` benötigt feste Port-Angaben in Prefixes; Port 0 ist direkt nur mit `TcpListener`/`Kestrel` realisierbar — Design-Entscheidung nötig (Paketabhängigkeit vs. `System.Net.HttpListener`-Workaround).
3. **Exe-Ort der neuen Windows-App**: wird von keinem Workflow publisht; der E2E-Lauf muss `dotnet build`/`publish` selbst fahren — RID-Unterordner (`win-x64` vs. `win10-x64`) lokal verifizieren.
4. **Bootstrap-Session ohne Passwort**: `SilentReloginAsync` bricht ohne gespeicherte Zugangsdaten ab — entweder Refresh-/gerätegebundene Erneuerung (Server-Abhängigkeit) oder die App persistiert zusätzlich eine erneuerbare Credential-Form. Klärung im Server-Lifecycle.
5. **QR-Unterpfad-Edge-Case**: `ServerAddress.TryNormalize` kürzt auf `GetLeftPart(Path)` — ein QR `https://host/pairing?t=…` muss erst auf die Server-Basis reduziert werden; bei Servern unter Unterpfad (Placeholder `http://server/videoplayer/`) darf die `/pairing`-Stelle nicht in die Basisadresse wandern — Parsing-Regel festlegen (Convention: Ticket-URL enthält `t=`; Basis = URL ohne `/pairing`-Tail?).

## Konventionen (querschnittlich)

- MVU: alle Zustandsänderungen nur über `AppMessage` → `AppUpdate.Update` → `AppEffect` → `IAppEffectHandler` (`MvuHost.cs` Z. 44–68).
- Fehlertexte: zentrale deutsche Konstanten in `AppUpdate.cs` Z. 12–54.
- Design: `Vp*`-Tokens, dark-only, implizite Styles; AutomationIds auf interaktiven Elementen; Contract-Tests pinnen XAML-Struktur.
- Secrets: `ISecretStore`/`SecureStorage` nur via `AppSecretsService`, profil-präfixiert; Geräte-Tokens adressspezifisch (`DeviceToken:<normalized-address>`).
- Pairing-Krypto: ECDH P-256 + `DeriveKeyFromHmac`(SHA-256) + AES-256-GCM `nonce‖ct‖tag` — app- und serverseitig identisch (`PairingCrypto.cs` ↔ `PairingService.cs` Z. 225–276).
- HTTP-Fehler→Status-Mapping: `ApiException.StatusCode` → `PairingStatus` (`ShellAppEffectHandler.cs` Z. 1266–1276).
