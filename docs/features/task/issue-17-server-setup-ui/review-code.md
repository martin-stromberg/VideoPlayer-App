# Code-Review — Modernisierte `ServerSetupPage` (Issue 17, Schritt 4)

Geprüfte Änderungen (2026-09-24):

- `VideoPlayer.Maui/Pages/ServerSetupPage.xaml` (Restrukturierung,
  +ca. 300 geänderte Zeilen)
- `VideoPlayer.Maui/Pages/ServerSetupPage.xaml.cs` (+18 Zeilen)
- `VideoWebPlayer.Maui.Tests/ServerSetupContractTests.cs` (erweitert)
- `VideoPlayer.E2E.Tests/Tests/ServerSetupTests.cs` (erweitert)

## XAML (`ServerSetupPage.xaml`)

- **Struktur:** `Grid Auto,*,Auto` bleibt; Kopf als
  `VerticalStackLayout`, Listen in `ScrollView` (Z. 119–222), Fuß in
  eigener `ScrollView` (Z. 226–324). Keine verschachtelten
  ScrollView-Konflikte: die `CollectionView`s scrollen intern nicht
  (Höhe folgt Inhalt, Bereich scrollt).
- **Tokens:** ausschließlich `StaticResource`-Referenzen auf
  `Vp*`-Schlüssel; kein `#`, kein `AppThemeBinding` nötig (Dark-only).
  Contract-Test `DoesNotContain("#")` pinnt das.
- **Status-Badge:** `DataTrigger` auf `ServerHealthDot`/`ServerHealthLabel`
  binden an `Model.ServerHealth` (Z. 72–109). Alle vier Enum-Werte
  sind abgedeckt (Reachable→`VpSecondary`, Unreachable→`VpDanger`,
  Checking/Unknown→`VpTextMuted`; Label: Reachable→`VpSecondarySoft`,
  Unreachable→`VpDanger`, sonst unverändert). Korrekt, dass der
  Punkt während `Checking` ausgeblendet und durch den
  `ActivityIndicator` ersetzt wird (Code-Behind Z. 59–61).
- **Karten-Templates:** beide `DataTemplate`s setzen `x:DataType`
  (`storage:KnownServer`, `discovery:DiscoveredServer`) —
  kompilierte Bindings bleiben aktiv; `Margin 0,0,0,8` ersetzt den
  früheren `Spacing` der ItemContainer, unauffällig.
- **Pins:** `PairingCodeEntry` ohne `MaxLength`, Spellcheck/
  TextPrediction aus; Führungstext mit „Verwaltung", ohne
  „-stellig"; alle bisherigen AutomationIds erhalten.
- **Kommentare:** knappe deutsche Begründungen an den
  Design-Entscheidungen (Pill-Form, `MaxLength`-Verzicht,
  QR-Vorbereitung) — konsistent mit dem Projektkommentarstil.

## Code-Behind (`ServerSetupPage.xaml.cs`)

- **`BindingContext = mvuHost` (Z. 25):** `MvuHost` implementiert
  `INotifyPropertyChanged` und feuert `PropertyChanged("Model")` bei
  jeder Model-Ersetzung (`MvuHost.cs` Z. 30/36/61) — die
  XAML-Trigger reagieren korrekt. Risiko ererbter Bindings:
  ausgeschlossen, `DataTemplate`s binden per `x:DataType` gegen ihre
  Items; keine anderen `Binding`-Ausdrücke auf Seitenelementen.
- **`UpdateUi`-Erweiterung (Z. 56–61, 71, 76–84, 95):** rein
  lesend aus `AppModel` — `ServerHealth`, `ServerHealthText`,
  `IsDiscoveryRunning`, `IsServerCheckRunning`, `IsPairingRunning`,
  `ConnectionState`. `CheckStatusLabel` unterscheidet
  Prüf-/Anmeldephase über `ConnectionState == Connecting` —
  sinnvolle, dokumentierte Feinheit.
- **Unverändert:** alle Dispatch-Handler und
  `OnAppearing`/`OnDisappearing` inkl. `LoadServersRequestedMessage`
  — der MVU-Fluss ist bytegleich (AK3).

## Tests

- **Contract:** Id-Menge bewusst erweitert (Kommentar benennt die
  Erweiterung und dass kein Pin entfernt wurde);
  `ServerSetupPage_UsesCardLayout` pinnt Kartenradius, Badge und
  Token-Verbot. `PairingContractTests` unverändert grün.
- **E2E:** `CollectTexts` rekursiv über `DevApiTreeNode` — korrekt,
  Assertions auf sichtbare Texte statt interner Struktur. Der neue
  Test nutzt die echte Bedienkette (`EnterText`/`ClickButton`) und
  prüft über `/model` + `/tree` + UIA — kein reiner Quelltext-Test.

## Befunde

- Kein Defekt gefunden. Hinweis (kein Handlungsbedarf): die
  `DataTrigger`-Farben für `Checking`/`Unknown` sind identisch
  (`VpTextMuted`) — bewusst gedimmt statt zweier Farben;
  unterscheidbar bleibt der Zustand über Indicator vs. Punkt.

## Fazit

Saubere, projektkonforme Umsetzung: MVU unverändert, ausschließlich
`Vp*`-Tokens, nachvollziehbar dokumentierte Designentscheidungen,
belastbare Testabdeckung (Contract + E2E + Screenshots).
