# Tasks: Modernisierung der Serververbindungs-UI (`ServerSetupPage`)

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | UI | `ServerSetupPage.xaml`: Kopfbereich mit Kicker-Badge „SERVERVERBINDUNG", Titel, Hinweistext | Offen | – |
| 2 | UI | `ServerSetupPage.xaml`: Aktiver-Server-Karte `ActiveServerSection`/`ActiveServerCard` mit `ActiveServerLabel` und Status-Badge (`ServerHealthIndicator`, `ServerHealthDot` mit `DataTrigger`n, `ServerHealthLabel`) | Offen | – |
| 3 | UI | `ServerSetupPage.xaml`: `KnownServersView`-`DataTemplate` als Karte (`RoundRectangle 8`, Name, Adresse, „Zuletzt verwendet", „Auswählen"-Hinweis) | Offen | – |
| 4 | UI | `ServerSetupPage.xaml`: Sektion „Im Netzwerk suchen" mit `DiscoverButton`, `DiscoveryIndicator` + `DiscoveryRunningLabel`, `DiscoveryEmptyLabel` | Offen | – |
| 5 | UI | `ServerSetupPage.xaml`: `DiscoveredServersView`-`DataTemplate` als Karte (Name, Adresse, „Im Netzwerk gefunden", „Auswählen"-Hinweis) | Offen | – |
| 6 | UI | `ServerSetupPage.xaml`: Karte `ConnectCard` mit `AddressEntry`, `ConnectButton`, `CheckIndicator` + `CheckStatusLabel`, `ErrorLabel` | Offen | – |
| 7 | UI | `ServerSetupPage.xaml`: hervorgehobene Pairing-Karte `PairingSection` („NÄCHSTER SCHRITT"-Badge, Führungstext mit „Verwaltung", `PairingCodeEntry` ohne `MaxLength`, `PairButton`, `PairingIndicator` + `PairingRunningLabel`, `PairingErrorLabel`) | Offen | – |
| 8 | UI | `ServerSetupPage.xaml`: Layout mit scrollendem Listenbereich und scrollbarem Fuß (`Grid Auto,*,Auto` + `ScrollView`s) | Offen | – |
| 9 | Code-Behind | `ServerSetupPage.xaml.cs`: `BindingContext = mvuHost` im Konstruktor | Offen | – |
| 10 | Code-Behind | `ServerSetupPage.xaml.cs`: `UpdateUi` um `ServerHealthLabel`, `ServerHealthIndicator`, `ServerHealthDot`, `CheckStatusLabel`, `DiscoveryRunningLabel`, `PairingRunningLabel` erweitern | Offen | – |
| 11 | Tests | `ServerSetupContractTests`: AutomationId-Menge um neue Ids erweitern (`ActiveServerCard`, `ServerHealthLabel`, `DiscoveryRunningLabel`, `ConnectCard`, `CheckStatusLabel`, `PairingSection`, `PairingRunningLabel`) | Offen | – |
| 12 | Tests | `ServerSetupContractTests`: neuer Test `ServerSetupPage_UsesCardLayout` (Kartenstruktur + Vp-Tokens) | Offen | – |
| 13 | E2E-Tests | `ServerSetupTests.FreshStart_ShowsServerSetupPage`: `ConnectCard`-`/tree`-Assert ergänzen | Offen | – |
| 14 | E2E-Tests | `ServerSetupTests.ReachableServer_ShowsHealthBadgeAndGuidedPairing` neu: Status-Badge + geführte Pairing-Karte | Offen | – |
| 15 | Verifikation | Build + `dotnet test` beider Suiten + Screenshot(s) der neuen Seite (`live-verification.md`) | Offen | – |
