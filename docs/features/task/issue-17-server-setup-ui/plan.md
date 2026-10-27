# Umsetzungsplan: Modernisierung der Serververbindungs-UI (`ServerSetupPage`)

## Übersicht

Die `ServerSetupPage` wird optisch und strukturell modernisiert: Kopfbereich mit Kicker-Badge und Aktiver-Server-Karte inklusive Status-Badge (`ServerHealth`/`ServerHealthText`), Karten für bekannte und gefundene Server, eine Karte „Neuen Server verbinden" mit explizitem Prüfstatus und eine hervorgehobene, geführte Karte „Gerät koppeln". Der MVU-Fluss bleibt unverändert — alle Zustände kommen weiterhin aus `AppModel`, alle Dispatch-Messages bleiben gleich, und die Route `serversetup` bleibt ohne FlyoutItem. Der QR-/Ticket-Flow (Schritt 5b) wird nicht gebaut; die Pairing-Karte enthält nur den vorbereitenden Hinweistext.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Kartenbau | `Border` + `RoundRectangle 8` + `VpSurface*`/`VpBorder*` direkt im XAML der Seite | Exaktes Projekt-Muster (`HomePage.xaml` Z. 113–138 Banner-Karte, `Styles_PinsButtonAndCardRadius` pinnt Radius 8); kein neuer Style nötig — implizite `Border`-/`Button`-/`Entry`-Styles gelten weiter |
| Status-Badge des aktiven Servers | Pill-`Border` (`RoundRectangle 999`, `VpSurfaceLow`) mit farbigem `BoxView`-Punkt + `ServerHealthLabel` | Pill-Form ist die Badge-Konvention (`Styles_PinsPillShapes`); `ServerHealthText` liefert „erreichbar"/„nicht erreichbar" — bisher auf der Seite ungenutzt |
| Farbumschaltung des Status-Punkts | `DataTrigger` mit `Binding="{Binding Model.ServerHealth}"` + `BindingContext = mvuHost` im Konstruktor | `MvuHost` implementiert `INotifyPropertyChanged` (Z. 11/36/61) — dieselbe Binding-Quelle wie `HomePage`; keine harten Farben im Code-Behind (Contract-Verbot) und keine neuen Konverter (Projekt verwendet nirgends `IValueConverter`) |
| „Aktiv"-Markierung je Server-Karte | **Keine** — aktiver Server hat die eigene Kopfkarte; Karten zeigen stattdessen `LastUsedAtUtc` („Zuletzt verwendet") | Ein Adressvergleich pro Eintrag bräuchte einen neuen Konverter; das widerspricht der projektweiten Konvention (keine Konverter, UI wird per `UpdateUi` gesetzt) und bringt keinen Mehrwert, weil die Kopfkarte den aktiven Server prominent zeigt |
| Geführte Pairing-Karte | `Border` `RoundRectangle 8`, `Stroke=VpSecondary`, `VpSurfaceHigh`-Grund + „NÄCHSTER SCHRITT"-Badge (`RoundRectangle 4`, Muster `HeroKicker`) | `VpSecondary` hebt den Workflow-Schritt von normalen Karten (`VpBorder`) ab; Badge-Muster kommt aus `HomePage.xaml` Z. 86–96 |
| Laufanzeigen | Neben jedem `ActivityIndicator` ein statustextendes Label (`DiscoveryRunningLabel`, `CheckStatusLabel`, `PairingRunningLabel`) | AK2 fordert explizites „Prüfung läuft" — sichtbarer Text statt nur drehendem Indikator |
| Fußbereich | `ScrollView` um die beiden Karten | Bei sichtbarer Pairing-Karte + Verbinden-Karte kann der Fuß wachsen — Scrollen verhindert abgeschnittene Inhalte auf kleinen Fenstern |
| Listen-Darstellung | `CollectionView` bleibt (ItemsSource unverändert modellgetrieben), nur das `DataTemplate` wird zur Karte | `TrySelectItem`/`itemIndex` der Dev-API und `SelectionChanged`-Handler bleiben funktionsfähig; `ServerSwitchTests` hängt daran |
| Neuen Server verbinden | Eigene Karte `ConnectCard` im Fuß statt losem Stack | Visuelle Hierarchie: jede Aktion ist eine Karte (AK1) |

## Programmabläufe

Alle bestehenden Abläufe bleiben unverändert (MVU-Update, Effekte, Dispatches). Neu ist ausschließlich die Darstellung in `UpdateUi` und zwei Model-Bindings für die Statusfarben.

### Seitenaufbau und Zustandsanzeige

1. `ServerSetupPage`-Konstruktor: `InitializeComponent()`, `BindingContext = mvuHost` (ermöglicht `Model.*`-Trigger-Bindings), `UpdateUi(mvuHost.Model)`.
2. `OnAppearing`: wie bisher `LoadServersRequestedMessage`.
3. `OnModelChanged` → `UpdateUi` setzt — wie bisher — `ActiveServerSection.IsVisible`, `ActiveServerLabel.Text`, `ItemsSource`s, `KnownServersHeader`-/View-Sichtbarkeit, Discovery-Flags, `CheckIndicator`, `ErrorLabel`, Pairing-Sichtbarkeiten; **neu zusätzlich**:
   - `ServerHealthLabel.Text = model.ServerHealthText`
   - `ServerHealthIndicator.IsRunning/.IsVisible = (model.ServerHealth == Checking)`
   - `ServerHealthDot.IsVisible = (model.ServerHealth != Checking)`
   - `CheckStatusLabel.IsVisible = model.IsServerCheckRunning`, Text „Verbindung wird geprüft …" bzw. „Verbindung wird hergestellt …" wenn `ConnectionState == Connecting` (Auto-Anmeldungsphase)
   - `DiscoveryRunningLabel.IsVisible = model.IsDiscoveryRunning`
   - `PairingRunningLabel.IsVisible = model.IsPairingRunning`
4. XAML-`DataTrigger` auf `ServerHealthDot`/`ServerHealthLabel` färben anhand `Model.ServerHealth`: `Reachable` → `VpSecondary`, `Unreachable` → `VpDanger`, `Checking`/`Unknown` → `VpTextMuted`.
5. Alle bestehenden Handler dispatchen unverändert: `OnDiscoverClicked` → `ServerDiscoveryRequestedMessage`; `OnConnectClicked` → `ServerAddressSubmittedMessage`; `OnPairClicked` → `PairingCodeSubmittedMessage`; `OnDiscoveredSelectionChanged` → `DiscoveredServerSelectedMessage`; `OnKnownSelectionChanged` → `ServerSwitchRequestedMessage` (jeweils mit Selection-Reset).

Beteiligte Klassen/Komponenten: `ServerSetupPage`, `AppModel`, `ServerHealthStatus`, `MvuHost`, `KnownServer`, `DiscoveredServer`.

## Neue Klassen

Keine — die Umsetzung ist eine reine Restrukturierung bestehender XAML-/Code-Behind-Dateien.

## Änderungen an bestehenden Klassen

### `ServerSetupPage` (`VideoPlayer.Maui/Pages/ServerSetupPage.xaml`, Komplett-Restrukturierung)

Neues Layout — `Grid RowDefinitions="Auto,*,Auto"` bleibt das Grundgerüst:

- **Kopfbereich (Zeile 0):** Kicker-Badge „SERVERVERBINDUNG" (`Border` `RoundRectangle 4`, `VpBorder`, `VpOverlay`), Titel „VideoPlayer-Server" (32, Bold), Hinweissatz (`VpTextMuted`), Aktiver-Server-Karte `x:Name="ActiveServerSection"` + `AutomationId="ActiveServerCard"` (`Border` `RoundRectangle 8`, `VpBorderStrong`, `VpSurfaceHigh`) mit „Aktiver Server"-Bezeichnung, `ActiveServerLabel` (AutomationId bleibt) und Status-Badge: `ServerHealthIndicator` (`ActivityIndicator`), `ServerHealthDot` (`BoxView` mit `DataTrigger`n), `ServerHealthLabel` (`AutomationId="ServerHealthLabel"`).
- **Listenbereich (Zeile 1, `ScrollView`):** Sektion „Bekannte Server" (`KnownServersHeader`) mit `KnownServersView` — `DataTemplate` wird zur Karte (`Border` `RoundRectangle 8`, `VpBorder`, `VpSurface`): `NameOrAddress` fett, `Address` muted, `LastUsedAtUtc` als „Zuletzt verwendet"-Statuszeile, rechts „Auswählen"-Hinweis (`VpSecondarySoft`). Sektion „Im Netzwerk suchen": `DiscoverButton`, `DiscoveryIndicator` + neu `DiscoveryRunningLabel` („Suche läuft …"), `DiscoveryEmptyLabel`, `DiscoveredServersView` mit Karten-`DataTemplate` (Name, Adresse, Statuszeile „Im Netzwerk gefunden" in `VpSecondarySoft`, „Auswählen"-Hinweis).
- **Fußbereich (Zeile 2, `ScrollView`):** Karte `ConnectCard` (`RoundRectangle 8`, `VpBorder`, `VpSurface`) mit Titel „Neuen Server verbinden", `AddressEntry`, `ConnectButton`, `CheckIndicator` + `CheckStatusLabel` (`AutomationId="CheckStatusLabel"`), `ErrorLabel`. Karte „Gerät koppeln" (`x:Name="PairingSection"` + `AutomationId="PairingSection"`, `RoundRectangle 8`, `Stroke=VpSecondary`, `VpSurfaceHigh`): „NÄCHSTER SCHRITT"-Badge, Titel, Führungstext (enthält „Verwaltung", kein „-stellig"), `PairingCodeEntry` (weiterhin **ohne** `MaxLength`, `IsSpellCheckEnabled`/`IsTextPredictionEnabled=False`), `PairButton`, `PairingIndicator` + `PairingRunningLabel` (`AutomationId="PairingRunningLabel"`), `PairingErrorLabel`.
- **Neue AutomationIds:** `ActiveServerCard`, `ServerHealthLabel`, `DiscoveryRunningLabel`, `ConnectCard`, `CheckStatusLabel`, `PairingSection`, `PairingRunningLabel`.
- **Erhaltene AutomationIds/Elemente:** `ActiveServerLabel`, `KnownServersView`, `DiscoverButton`, `DiscoveredServersView`, `DiscoveryEmptyLabel`, `AddressEntry`, `ConnectButton`, `ErrorLabel`, `PairingCodeEntry`, `PairButton`, `PairingIndicator`, `PairingErrorLabel`, `PairingSection`.
- **Neues Root-Element-Name:** `x:Name="Page"` für `x:Reference`-freie Model-Bindings — entfällt, da Bindings über `BindingContext = mvuHost` laufen (kein `x:Reference` nötig).

### `ServerSetupPage` Code-Behind (`ServerSetupPage.xaml.cs`)

- **Geändert:** Konstruktor — zusätzlich `BindingContext = mvuHost` (für `Model.*`-Trigger).
- **Geändert:** `UpdateUi(AppModel)` — zusätzliche Zeilen für `ServerHealthLabel`, `ServerHealthIndicator`, `ServerHealthDot`, `CheckStatusLabel`, `DiscoveryRunningLabel`, `PairingRunningLabel` (alle aus Model-Feldern abgeleitet).
- **Unverändert:** alle Dispatch-Handler, `OnAppearing`/`OnDisappearing`-Logik, `LoadServersRequestedMessage`.

### `ServerSetupContractTests` (`VideoWebPlayer.Maui.Tests/ServerSetupContractTests.cs`)

- **Erweitert:** `ServerSetupPage_ControlsHaveAutomationIds` — die geprüfte Id-Menge wird um die neuen AutomationIds ergänzt (bewusste Anpassung, im Kommentar begründet).
- **Neu:** `ServerSetupPage_UsesCardLayout` — pinnt die Modernisierung: `RoundRectangle 8` für Karten, `ActiveServerCard`/`ConnectCard`-Karten, `VpSurface`-/`VpBorder`-Tokens, `ServerHealthLabel`.

### `ServerSetupTests` (`VideoPlayer.E2E.Tests/Tests/ServerSetupTests.cs`)

- **Erweitert:** `FreshStart_ShowsServerSetupPage` — zusätzlicher `/tree`-Nachweis der `ConnectCard`.
- **Neu:** `ReachableServer_ShowsHealthBadgeAndGuidedPairing` — nach `ConnectToPairingRequiredAsync`: `ActiveServerCard` sichtbar, `ServerHealthLabel` zeigt „erreichbar", `PairingSection` sichtbar, Führungstext enthält „Verwaltung".

## Datenbankmigrationen

Keine.

## Validierungsregeln

Keine — Eingabevalidierung bleibt unverändert in `AppUpdate` (`ServerAddress.TryNormalize`, `PairingCode.TryNormalize`).

## Konfigurationsänderungen

Keine.

## Seiteneffekte und Risiken

- **`PairingContractTests.ServerSetupPage_ContainsPairingSection`:** `DoesNotContain("MaxLength=")` gilt für die **gesamte** XAML-Datei — kein Element der Seite darf `MaxLength` setzen; Hinweistext muss weiterhin „Verwaltung" enthalten und darf „-stellig" nicht enthalten.
- **`DesignTokensContractTests`:** in `Pages/*.xaml` sind nur `StaticResource`-Referenzen auf definierte `Vp*`-Schlüssel erlaubt — alle neuen Elemente verwenden ausschließlich vorhandene Tokens.
- **CollectionView in `ScrollView`:** die `CollectionView`s liegen im scrollenden Mittelbereich; da beide Listen klein sind (bekannte/gefundene Server), ist das unkritisch — der Fußbereich bleibt ebenfalls scrollbar.
- **`BindingContext = mvuHost`:** setzt den Binding-Kontext der gesamten Seite; die `DataTemplate`s binden gegen ihre Items (`x:DataType` gesetzt) und sind davon unberührt. Risiko: versehentliche Erbe-Bindings — es gibt keine anderen `Binding`-Ausdrücke auf Seitenelemente.
- **E2E-Tests:** `UiDriver.Exists`/`EnterText`/`ClickButton` adressieren Elemente über AutomationId — verschachtelte `Border` ändern daran nichts; `ServerSwitchTests` nutzt `KnownServersView` + `itemIndex` — ItemsSource und `SelectionChanged` bleiben gleich.
- **UIA-Baum:** `Border`-Karten ohne eigenen AutomationPeer erscheinen ggf. nicht in der Windows-UIA — Karten-Assertions laufen daher über die Dev-API `/tree` (liefert alle `Element`s), nicht über FlaUI.

## Umsetzungsreihenfolge

1. **`ServerSetupPage.xaml` neu strukturieren**
   - Voraussetzungen: keine (alle Tokens/Styles vorhanden).
   - Beschreibung: Kopf-, Karten-, Fußstruktur wie oben beschrieben; alle gepinnten Elemente/AutomationIds erhalten; neue AutomationIds ergänzen; `PairingCodeEntry` ohne `MaxLength`; nur `Vp*`-Tokens.

2. **`ServerSetupPage.xaml.cs` anpassen**
   - Voraussetzungen: Schritt 1.
   - Beschreibung: `BindingContext = mvuHost` im Konstruktor; `UpdateUi` um die neuen Status-Elemente erweitern; sonst nichts ändern.

3. **Contract-Tests anpassen/erweitern**
   - Voraussetzungen: Schritt 1+2.
   - Beschreibung: `ServerSetupContractTests` — Id-Menge erweitern, neuen `ServerSetupPage_UsesCardLayout`-Test.

4. **E2E-Tests erweitern**
   - Voraussetzungen: Schritt 1+2.
   - Beschreibung: `ServerSetupTests` — `FreshStart` um `ConnectCard`-`/tree`-Assert, neuer Sichtbarkeits-/Badge-Test.

5. **Build + alle Tests + visuelle Verifikation**
   - Voraussetzungen: Schritt 1–4.
   - Beschreibung: `dotnet build`, `dotnet test VideoWebPlayer.Maui.Tests`, `dotnet test VideoPlayer.E2E.Tests`; App starten, Screenshot(s) der neuen Seite (Erststart + Pairing-Zustand) für `live-verification.md`.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `ServerSetupPage_UsesCardLayout` | `ServerSetupContractTests` | Kartenstruktur (`RoundRectangle 8`), `ActiveServerCard`/`ConnectCard`, `ServerHealthLabel`, ausschließlich `Vp*`-Tokens |
| `ReachableServer_ShowsHealthBadgeAndGuidedPairing` | `ServerSetupTests` (E2E) | Status-Badge „erreichbar" + geführte Pairing-Karte sichtbar (Dev-API `/tree` + UIA) |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `ServerSetupContractTests.ServerSetupPage_ControlsHaveAutomationIds` | Neue AutomationIds werden in die gepinnte Menge aufgenommen (bewusste Erweiterung) |
| `ServerSetupTests.FreshStart_ShowsServerSetupPage` | Zusätzlicher `/tree`-Assert der `ConnectCard` |
| `PairingContractTests` | Keine Anpassung nötig — alle Pins bleiben erfüllt (bewusste Prüfung: `PairingSection`, `PairingCodeEntry`, `PairButton`, `PairingIndicator`, `PairingErrorLabel`, kein `MaxLength`, „Verwaltung", kein „-stellig") |

### E2E-Tests (primärer Funktionsnachweis)

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|---------------------|
| Pflicht | Erststart zeigt modernisierte Einrichtungsseite mit Karten (Verbinden-Karte sichtbar, Eingabe bedienbar) | `ServerSetupTests.FreshStart_ShowsServerSetupPage` (erweitert) | AK1, AK3 | Sichtprüfung der echten Oberfläche + echte Eingabekette |
| Pflicht | Erreichbarer Server → Status-Badge „erreichbar" in der Aktiver-Server-Karte + geführte Pairing-Karte sichtbar und bedienbar | `ServerSetupTests.ReachableServer_ShowsHealthBadgeAndGuidedPairing` (neu) | AK1, AK2 | Zustandsfeedback ist nur in der laufenden App prüfbar |
| Pflicht | Bestehende Flüsse laufen unverändert: Validierungsfehler, „nicht erreichbar", Pairing-Flows, „Server wechseln" mit Karten-Auswahl | `ServerSetupTests` (3 bestehende), `PairingTests` (3), `ServerSwitchTests` (1), `LoginTests` (2) | AK3, AK4 | Regression des unveränderten MVU-Flusses gegen neue UI |
| Pflicht | Visuelle Verifikation per Screenshot (Erststart + Pairing-Zustand) | `live-verification.md` | AK1, AK2, AK4 | UI-Feature verlangt Sichtprüfung |

## Offene Punkte

Keine — Akzeptanzkriterien, Pins und Design-Vorgaben sind durch Projektplan, Inventur und Design-System vollständig bestimmt.
