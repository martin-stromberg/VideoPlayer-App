# Umsetzungsplan: tvOS-Serververbindung — Einrichtung, Pairing und Anmeldung

## Übersicht

Die tvOS-App erhält die fernbedienungstaugliche Oberfläche des bereits
vollständig verdrahteten Verbindungs-Workflows: die Workflow-Seiten
`AppPage.ServerSetup` und `AppPage.Login` werden als vollwertige,
modellgetriebene UIKit-Seiten umgesetzt (Serverwahl über Erkennung,
bekannte Server oder manuelle Eingabe; Pairing-/Bootstrap-/Zugangsdaten-
Eingabe; alle Fehler- und Laufzustände). Im Menürahmen entstehen die
Einstiege „Server wechseln" (Einstellungen) und „Abmelden" (Profil).
Die plattformneutrale Logik bleibt unverändert — einzige
Effekt-Erweiterung ist die Verdrahtung von `LogoutEffect` im
`TvosAppEffectHandler`.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Seitenarchitektur | `UIViewController` mit `UIScrollView` + vertikalem `UIStackView`; Abschnitte sind Views, die modellgetrieben ein-/ausgeblendet werden (`IsHidden` nimmt sie aus dem Layout). | tvOS-Focus-Engine scrollt fokussierte Elemente automatisch sichtbar; Stufenlogik der MAUI-Vorlage lässt sich 1:1 abbilden; keine XAML-/Storyboard-Abhängigkeit im Projekt. |
| Texteingabe | `UITextField` je Eingabe (`SecureTextEntry` für das Passwort). | tvOS-Standardmechanismus — blendet die Systemtastatur ein, unterstützt Diktat per Siri Remote und die Continuity-Tastatur vom iPhone. |
| Serverlisten | `UITableView` mit je einer `ServerListTableSource` (Untertitel-Zellen), feste Höhe, internes Scrollen. | Fokussierbar per Focus-Engine; identische Darstellung beider Listen über eine gemeinsame Source (kein duplizierter Code). |
| UI-Bausteine | Statische Fabrik `TvosControls` (Labels, Buttons, Textfelder, Abschnitts-Stacks). | Verhindert duplizierte View-Erzeugung über drei Seiten hinweg; einheitliche Fonts/Farben. |
| Modellanbindung | Die Seiten erhalten `Render(AppModel)`; `TvosNavigator.Render` verteilt das Modell; fähige Menüseiten implementieren `IModelRenderable`, das `MenuTabBarController.Render` durchreicht. | Entspricht dem bestehenden Render-Einstieg des Navigators; hält die Seiten zustandslos („Ereignis-Übersetzer" + Renderer). |
| Rücknavigation | Siri-Remote-Menü-Taste (`PressesBegan`, `UIPressType.Menu`) + sichtbarer „Zurück zum Menü"-Button, jeweils nur wirksam bei `ConnectionState.Connected` → `NavigateToMessage` auf die zuletzt aktive Menüseite (`TvosNavigator.LastMenuPage`). | Im Einrichtungs-/Anmeldezwang sperrt `UpdateNavigateTo` Menüseiten ohnehin; bei `Connected` braucht der geschobene Dialog einen Rückweg — beide Bedienwege sind für TV-Nutzer erkennbar. |
| Seitenumfang | `SettingsPageViewController` („Server wechseln" + Serverstatus) und `ProfilePageViewController` („Abmelden" + Verbindungsanzeige) ersetzen die Platzhalter nur für diese zwei Menüseiten. | „Server wechseln"/„Abmelden" brauchen erreichbare Einstiege; die übrigen Platzhalter bleiben Thema späterer Schritte. |
| `ConnectionStatePageViewController` | Wird gelöscht — ersetzt durch die vollständigen Seiten. | Toten Code vermeiden; der Retry-Einstieg wandert als „Erneut prüfen"-Button in die neue Seite. |

## Programmabläufe

### Erststart: Serverwahl → Pairing → Anmeldung

1. `StartupRequestedMessage` → `LoadServersEffect` → kein aktiver Server
   → `CurrentPage = ServerSetup`; `TvosNavigator.Render` zeigt
   `ServerSetupViewController`.
2. `ViewWillAppear` der Seite dispatcht `LoadServersRequestedMessage`
   (Muster: MAUI `OnAppearing`).
3. Anwender startet die Erkennung („Im Netzwerk suchen") →
   `ServerDiscoveryRequestedMessage`; `IsDiscoveryRunning` blendet den
   Laufhinweis ein; `ServerDiscoveryCompletedMessage` befüllt
   `DiscoveredServers`.
4. Auswahl eines gefundenen/bekannten Servers oder Absenden der manuellen
   Adresse → `DiscoveredServerSelectedMessage` /
   `ServerSwitchRequestedMessage` / `ServerAddressSubmittedMessage` →
   `CheckCandidateServerEffect`; `IsServerCheckRunning` zeigt
   „Verbindung wird geprüft …".
5. Erreichbar + `PairingRequired` → die Seite schaltet in Stufe 2 mit
   Pairing-Eingabe; `PairingCodeSubmittedMessage` →
   `ExchangePairingCodeEffect` → `PairingCompletedMessage` →
   `AttemptAutoLoginEffect`.
6. Erreichbar ohne Pairing → `AttemptAutoLoginEffect`; bei fehlenden
   Zugangsdaten `NeedsLogin` → die Seite zeigt (auf `ServerSetup`
   verbleibend) Kurzcode- und Zugangsdaten-Eingabe;
   `LoginSubmittedMessage` bzw. `BootstrapTicketSubmittedMessage` →
   `LoginCompletedMessage`/`BootstrapCompletedMessage` →
   `EnterConnected` → `AppPage.Home`.
7. Alternativ Ticket-Eingabe in Stufe 1: `BootstrapTicketSubmittedMessage`
   → Ticket-Adresse (`CheckBootstrapServerEffect` → `PersistActiveServer`
   → `ExchangeBootstrapTicketEffect`) oder Kurzcode gegen den aktiven
   Server.

### Laufzeit-Flows

- „Server wechseln" (Einstellungen-Tab) → `NavigateToMessage(ServerSetup)`
  → Seite mit bekannter/gefundener Liste; Auswahl →
  `ServerSwitchRequestedMessage` → Prüfung → automatische Anmeldung am
  neuen Server → `EnterConnected` → Home.
- „Abmelden" (Profil-Tab) → `LogoutRequestedMessage` →
  `DisconnectServerEventsEffect` + `LogoutEffect` (neu verdrahtet:
  `AuthSessionService.LogoutAsync`) → `AppPage.Login`.
- Sitzungsablauf/Gate-Ablehnung → `SessionExpiredMessage` → Login-Seite
  mit `SessionExpiredError`; `DeviceTokenRevokedMessage` →
  ServerSetup-Seite mit aktiver Pairing-Eingabe und
  `DeviceRevokedError` — beides über `Render` sichtbar.
- Menü-Taste auf einer Workflow-Seite bei `Connected` →
  `NavigateToMessage(LastMenuPage)` → Menürahmen.

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `TvosControls` | interne statische Klasse (`Ui/TvosControls.cs`) | Fabriken für `UILabel` (Titel/Abschnitt/Text/Hinweis/Fehler/Status), `UITextField`, `UIButton` mit Aktions-Callback, Abschnitts-`UIStackView` — einheitliches Erscheinungsbild |
| `IModelRenderable` | Interface (`Ui/IModelRenderable.cs`) | `void Render(AppModel model)` für modellgetriebene Seiten (Menüseiten innerhalb des Tab-Controllers) |
| `ServerListTableSource` | `UITableViewSource` (`Ui/ServerListTableSource.cs`) | Datenquelle der bekannten/gefundenen Serverlisten: Untertitel-Zellen (Titel + Detailzeile), Auswahl-Callback `Action<nint>` |
| `ServerSetupViewController` | `UIViewController` (`Ui/ServerSetupViewController.cs`) | Einrichtungs-/Verwaltungsseite `AppPage.ServerSetup` (zweistufig, alle Eingaben, Listen, Fehler, „Erneut prüfen", „Zurück zum Menü") |
| `LoginViewController` | `UIViewController` (`Ui/LoginViewController.cs`) | Anmeldemaske `AppPage.Login` (Zugangsdaten, Kurzcode, „Anderen Server einrichten", Lauf-/Fehlerzustände) |
| `SettingsPageViewController` | `UIViewController`, `IModelRenderable` (`Ui/SettingsPageViewController.cs`) | Menüseite „Einstellungen": Serverstatus + „Server wechseln" |
| `ProfilePageViewController` | `UIViewController`, `IModelRenderable` (`Ui/ProfilePageViewController.cs`) | Menüseite „Profil": Verbindungsanzeige + „Abmelden" |

## Änderungen an bestehenden Klassen

### `TvosNavigator` (`Ui/TvosNavigator.cs`)

- **Geänderte Felder:** `serverSetupPage`/`loginPage` werden
  `ServerSetupViewController`/`LoginViewController` (statt
  `ConnectionStatePageViewController`); neues Feld `lastMenuPage`
  (Startwert `AppPage.Home`).
- **Neue Eigenschaft:** `LastMenuPage` — die zuletzt dargestellte
  Menüseite (Rückziel der Menü-Taste).
- **Geänderte Methode:** `NavigateTo(AppPage)` — `ServerSetup`/`Login`
  zeigen die neuen Seiten; der Menüzweig merkt sich `lastMenuPage`.
- **Geänderte Methode:** `Render(AppModel)` — ruft `Render` der neuen
  Seiten und `tabBar?.Render(model)` auf; `UpdateStatus`/`RetryStartup`
  entfallen (Retry liegt in der neuen Seite).
- **Neue Methode:** `NavigateBackToMenu()` — dispatcht
  `NavigateToMessage(LastMenuPage)`.

### `MenuTabBarController` (`Ui/MenuTabBarController.cs`)

- **Geänderte Methode:** `ViewDidLoad` — Menüseiten werden über eine
  Fabrikmethode erzeugt: `Settings` → `SettingsPageViewController`,
  `Profile` → `ProfilePageViewController`, übrige →
  `PagePlaceholderViewController`.
- **Neue Methode:** `Render(AppModel)` — reicht das Modell an alle
  `IModelRenderable`-Kindcontroller weiter.

### `TvosAppEffectHandler` (`Services/TvosAppEffectHandler.cs`)

- **Neuer Effekt-Zweig:** `case LogoutEffect:` → `LogoutAsync()`.
- **Neue Methode:** `LogoutAsync()` — `authSession.LogoutAsync()` mit
  Fehlerprotokollierung (Muster: `ShellAppEffectHandler.LogoutAsync`).

### `ConnectionStatePageViewController` (`Ui/ConnectionStatePageViewController.cs`)

- Wird **gelöscht** (durch `ServerSetupViewController`/`LoginViewController`
  ersetzt).

### `TvOsAppContractTests` (`VideoPlayer.Maui.Tests/TvOsAppContractTests.cs`)

- **Geänderter Test:** `TvOsUi_UsesTabBarAndModelDrivenNavigation` —
  prüft die neuen Seiten-Dateien statt der gelöschten
  `ConnectionStatePageViewController.cs`.

## Datenbankmigrationen

Keine.

## Validierungsregeln

Keine neuen — die Eingabe-Validierung liegt vollständig in der
plattformneutralen Update-Funktion (`ServerAddress.TryNormalize` →
`InvalidAddressError`; `PairingCode.TryNormalize` → `PairingCodeFormatError`;
`BootstrapTicketParser.TryParse` → `InvalidBootstrapInputError`;
leere Zugangsdaten → `CredentialsRequiredError`). Die Seiten reichen die
Roheingaben unverändert durch.

## Konfigurationsänderungen

Keine — `Info.plist` enthält die mDNS-/LAN-Schlüssel bereits
(`NSBonjourServices`, `NSLocalNetworkUsageDescription`,
`NSAppTransportSecurity`).

## Seiteneffekte und Risiken

- **MAUI-App:** unberührt — keine Änderungen an `VideoPlayer.Mvu`,
  `VideoPlayer.ApiClient`, `VideoPlayer.Storage`, `VideoPlayer.Maui`.
- **CI-Job `build-tvos`:** neuer UIKit-Code muss unter `net10.0-tvos`
  kompilieren; lokale Verifikation nicht möglich — sorgfältige
  API-Verwendung (nur verbreitete UIKit-Bindings) + Contract-Tests.
- **`UITextField` auf tvOS:** Fokussierbarkeit und Tastatur-Overlay sind
  Standardverhalten; `PressesBegan` leitet nicht abgefangene Tasten an
  `base` weiter, damit die Menü-Taste ihre Systemfunktion behält, wenn
  keine Rücknavigation möglich ist.
- **Test-Änderung:** `TvOsUi_UsesTabBarAndModelDrivenNavigation` wird an
  die neue Dateilandschaft angepasst (gelöschte Datei).

## Umsetzungsreihenfolge

1. **`TvosControls` + `IModelRenderable` + `ServerListTableSource` anlegen**
   - Voraussetzungen: keine.
   - Beschreibung: UI-Fabriken, Render-Interface, wiederverwendbare
     Tabellen-Datenquelle.
2. **`ServerSetupViewController` implementieren**
   - Voraussetzungen: Schritt 1.
   - Beschreibung: zweistufige Einrichtungsseite mit allen Eingaben,
     Listen, Lauf-/Fehlerzuständen, `ViewWillAppear` →
     `LoadServersRequestedMessage`, Menü-Tasten-Rücknavigation bei
     `Connected`.
3. **`LoginViewController` implementieren**
   - Voraussetzungen: Schritt 1.
   - Beschreibung: Anmeldemaske mit Zugangsdaten, Kurzcode, Ausstieg zur
     Einrichtung, einmaliger Benutzername-Vorbefüllung.
4. **`SettingsPageViewController` + `ProfilePageViewController` implementieren**
   - Voraussetzungen: Schritt 1.
   - Beschreibung: „Server wechseln" + Serverstatus; „Abmelden" +
     Verbindungsanzeige.
5. **`TvosNavigator`, `MenuTabBarController`, `TvosAppEffectHandler`
   anpassen; `ConnectionStatePageViewController` löschen**
   - Voraussetzungen: Schritte 2–4.
   - Beschreibung: neue Seiten einhängen, `LastMenuPage`/
     `NavigateBackToMenu`, `Render`-Verteilung, `LogoutEffect`-Zweig.
6. **Contract-Tests ergänzen/anpassen**
   - Voraussetzungen: Schritte 1–5.
   - Beschreibung: neue `TvOsConnectionContractTests`, Anpassung
     `TvOsUi_UsesTabBarAndModelDrivenNavigation`.
7. **Lokale Validierung**
   - Voraussetzungen: Schritt 6.
   - Beschreibung: `dotnet test VideoPlayer.Maui.Tests` — gesamte
     net10.0-Suite muss grün bleiben.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `ServerSetupPage_DispatchesAllConnectionMessages` | `TvOsConnectionContractTests` | `ServerSetupViewController.cs` dispatcht Discovery-/Auswahl-/Adress-/Pairing-/Bootstrap-/Login-/Reload-/Retry-Messages und nutzt `UITextField`/`UITableView` |
| `ServerSetupPage_RendersAllModelStates` | `TvOsConnectionContractTests` | Rendering aller Modellfelder (Listen, `ActiveServerAddress`, `ServerHealth`, Laufzustände, `IsPairingRequired`, `NeedsLogin`-Stufe, alle Fehler) |
| `ServerSetupPage_AllowsMenuBackNavigationWhenConnected` | `TvOsConnectionContractTests` | `PressesBegan`/`UIPressType.Menu` + „Zurück"-Pfad, wirksam nur bei `Connected` |
| `LoginPage_ProvidesCredentialAndShortCodeEntry` | `TvOsConnectionContractTests` | `LoginSubmittedMessage`, `BootstrapTicketSubmittedMessage`, `SecureTextEntry`, `NavigateToMessage(AppPage.ServerSetup)`, Fehler-/Laufzustände, `LoginUsername`-Vorbefüllung |
| `MenuPages_ExposeServerSwitchAndLogout` | `TvOsConnectionContractTests` | `SettingsPageViewController` → `NavigateToMessage(AppPage.ServerSetup)`; `ProfilePageViewController` → `LogoutRequestedMessage`; `MenuTabBarController` instanziiert beide |
| `TvOsEffectHandler_HandlesLogoutEffect` | `TvOsConnectionContractTests` | `LogoutEffect`-Zweig + `LogoutAsync` im Handler |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `TvOsAppContractTests.TvOsUi_UsesTabBarAndModelDrivenNavigation` | `ConnectionStatePageViewController.cs` wird gelöscht — Assertions auf die neuen Seiten-Dateien umstellen |

### E2E-Tests (Pflicht)

| Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium |
|----------|------------------------|-------------------------------|
| Erststart-Fluss Serverwahl → Pairing → Anmeldung | `TvOsConnectionContractTests` (Nachrichten-/Rendering-Vertrag) + macOS-CI `build-tvos` (Kompilier-/Startfähigkeit) | Durchgängige Bedienbarkeit des Einrichtungsflusses |
| Serverwechsel und Abmeldung | `TvOsConnectionContractTests` | Erreichbarkeit der Einstiege im Menürahmen |
| Fehlerzustände (Code ungültig/abgelaufen, Sperrung, unerreichbar, abgelehnte Anmeldung, verworfenes Token) | `TvOsConnectionContractTests` (Rendering der Modell-Fehlerfelder); fachliche Übergänge bereits durch `AppUpdateTests_*`/`PairingContractTests`/`BootstrapContractTests` abgedeckt | Verständliche Fehleranzeige mit Wiederholmöglichkeit |

Hinweis: Ein Windows-ausführbarer UI-E2E-Pfad existiert für tvOS nicht;
der etablierte Projektstandard (Schritte 1–2) sind Quell-Contract-Tests
plus macOS-CI-Simulator-Build. Die End-to-end-Fachlichkeit des Flusses
(Messages → Effekte → Übergänge) ist durch die bestehende
plattformneutrale Testsuite abgedeckt — die tvOS-Seiten dispatcht
ausschließlich bereits getestete Messages.

## Offene Punkte

Keine.
