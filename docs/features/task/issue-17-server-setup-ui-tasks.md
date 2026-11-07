# Tasks: Modernisierung der Serververbindungs-UI (`ServerSetupPage`)

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | UI | `ServerSetupPage.xaml`: Kopfbereich mit Kicker-Badge „SERVERVERBINDUNG", Titel, Hinweistext | Erledigt | `ServerSetupPage_UsesCardLayout`; Screenshot `serversetup-01` |
| 2 | UI | `ServerSetupPage.xaml`: Aktiver-Server-Karte `ActiveServerSection`/`ActiveServerCard` mit `ActiveServerLabel` und Status-Badge (`ServerHealthIndicator`, `ServerHealthDot` mit `DataTrigger`n, `ServerHealthLabel`) | Erledigt | `ServerSetupPage_UsesCardLayout`; E2E `ReachableServer_ShowsHealthBadgeAndGuidedPairing`; Screenshot `serversetup-02` |
| 3 | UI | `ServerSetupPage.xaml`: `KnownServersView`-`DataTemplate` als Karte (`RoundRectangle 8`, Name, Adresse, „Zuletzt verwendet", „Auswählen"-Hinweis) | Erledigt | `ServerSwitchTests.SwitchToKnownServer_ReturnsToHomePage` (Auswahl per Karte); Screenshot `serversetup-03` |
| 4 | UI | `ServerSetupPage.xaml`: Sektion „Im Netzwerk suchen" mit `DiscoverButton`, `DiscoveryIndicator` + `DiscoveryRunningLabel`, `DiscoveryEmptyLabel` | Erledigt | AutomationId-Pin `DiscoveryRunningLabel`; Screenshot `serversetup-01` |
| 5 | UI | `ServerSetupPage.xaml`: `DiscoveredServersView`-`DataTemplate` als Karte (Name, Adresse, „Im Netzwerk gefunden", „Auswählen"-Hinweis) | Erledigt | `ServerSetupPage_UsesCardLayout` (RoundRectangle 8) |
| 6 | UI | `ServerSetupPage.xaml`: Karte `ConnectCard` mit `AddressEntry`, `ConnectButton`, `CheckIndicator` + `CheckStatusLabel`, `ErrorLabel` | Erledigt | E2E `FreshStart_ShowsServerSetupPage` (`ConnectCard` via `/tree`) |
| 7 | UI | `ServerSetupPage.xaml`: hervorgehobene Pairing-Karte `PairingSection` („NÄCHSTER SCHRITT"-Badge, Führungstext mit „Verwaltung", `PairingCodeEntry` ohne `MaxLength`, `PairButton`, `PairingIndicator` + `PairingRunningLabel`, `PairingErrorLabel`) | Erledigt | `PairingContractTests` (unverändert grün); E2E `ReachableServer_ShowsHealthBadgeAndGuidedPairing` |
| 8 | UI | `ServerSetupPage.xaml`: Layout mit scrollendem Listenbereich und scrollbarem Fuß (`Grid Auto,*,Auto` + `ScrollView`s) | Erledigt | Build + Sichtprüfung `live-verification.md` |
| 9 | Code-Behind | `ServerSetupPage.xaml.cs`: `BindingContext = mvuHost` im Konstruktor | Erledigt | E2E-Suite grün (Trigger wirken); `MvuHost` feuert `PropertyChanged("Model")` |
| 10 | Code-Behind | `ServerSetupPage.xaml.cs`: `UpdateUi` um `ServerHealthLabel`, `ServerHealthIndicator`, `ServerHealthDot`, `CheckStatusLabel`, `DiscoveryRunningLabel`, `PairingRunningLabel` erweitern | Erledigt | E2E `ReachableServer_ShowsHealthBadgeAndGuidedPairing` |
| 11 | Tests | `ServerSetupContractTests`: AutomationId-Menge um neue Ids erweitern (`ActiveServerCard`, `ServerHealthLabel`, `DiscoveryRunningLabel`, `ConnectCard`, `CheckStatusLabel`, `PairingSection`, `PairingRunningLabel`) | Erledigt | `ServerSetupPage_ControlsHaveAutomationIds` grün |
| 12 | Tests | `ServerSetupContractTests`: neuer Test `ServerSetupPage_UsesCardLayout` (Kartenstruktur + Vp-Tokens) | Erledigt | Suite 927/927 |
| 13 | E2E-Tests | `ServerSetupTests.FreshStart_ShowsServerSetupPage`: `ConnectCard`-`/tree`-Assert ergänzen | Erledigt | Suite 11/11 |
| 14 | E2E-Tests | `ServerSetupTests.ReachableServer_ShowsHealthBadgeAndGuidedPairing` neu: Status-Badge + geführte Pairing-Karte | Erledigt | Suite 11/11 |
| 15 | Verifikation | Build + `dotnet test` beider Suiten + Screenshot(s) der neuen Seite (`live-verification.md`) | Erledigt | Build 0/0; 927/927; 11/11; 3 Screenshots |
