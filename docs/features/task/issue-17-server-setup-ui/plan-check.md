# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan vollständig

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| AK1 Visuelle Hierarchie: Kopfbereich mit aktivem Server, Karten für bekannte/gefundene Server, hervorgehobene Pairing-Führung | Kopfbereich + `ActiveServerCard` mit Badge; Karten-`DataTemplate`s für `KnownServersView`/`DiscoveredServersView`; `ConnectCard`; Pairing-Karte mit „NÄCHSTER SCHRITT"-Badge (Umsetzungsreihenfolge 1) | `ServerSetupPage_UsesCardLayout` (Contract), E2E `FreshStart_ShowsServerSetupPage` + `ReachableServer_ShowsHealthBadgeAndGuidedPairing`, Screenshot-Verifikation | Abgedeckt |
| AK2 Zustandsfeedback explizit: „Prüfung läuft" (Indicator), „erreichbar"/„nicht erreichbar" (Badge/Text via `ServerHealth`/`ServerHealthText`/`ErrorLabel`) | `ServerHealthIndicator` + `ServerHealthDot` (Trigger-Farben) + `ServerHealthLabel`; `CheckStatusLabel` („Verbindung wird geprüft …"), `DiscoveryRunningLabel`, `PairingRunningLabel`; `ErrorLabel` bleibt | E2E `ReachableServer_ShowsHealthBadgeAndGuidedPairing` (Badge-Text „erreichbar" via `/tree`), `UnreachableServer_ShowsError` (Fehler-Pfad), Contract-Test `ServerSetupPage_UsesCardLayout` | Abgedeckt |
| AK3 MVU-Fluss unverändert: Zustand nur aus `AppModel`, gleiche Dispatch-Messages, Route `serversetup` ohne FlyoutItem | Keine neuen Messages/Effekte; `UpdateUi` bleibt alleinige Zustandsquelle; Handler unverändert; Shell/DI unverändert | Bestehende Pins laufen mit: `ServerSetupPage_CodeBehind_DispatchesMvuMessages`, `AppShell_RegistersServerSetupRoute_WithoutFlyoutItem`, `EffectHandler_HandlesAllServerEffects`; E2E-Regression (10 Tests) | Abgedeckt |
| AK4 Pins respektieren: AutomationIds erhalten/bewusst angepasst; `PairingCodeEntry` ohne `MaxLength`; `Vp*`-Tokens; Dark-only | Alle gepinnten Ids/Elemente bleiben; neue Ids dokumentiert; `PairingCodeEntry` ohne `MaxLength`; nur `StaticResource Vp*`; keine Code-Behind-Farben | `ServerSetupContractTests` (Ids + neuer Card-Test), `PairingContractTests` (unverändert grün), `DesignTokensContractTests` (unverändert grün) | Abgedeckt |

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Erststart → Einrichtungsseite (Karten sichtbar, Eingabe bedienbar) | `ServerSetupTests.FreshStart_ShowsServerSetupPage` (erweitert um `ConnectCard`-`/tree`-Assert) | Abgedeckt |
| Ungültige Adresse → Inline-Fehler | `ServerSetupTests.InvalidAddress_ShowsValidationError` (bestehend) | Abgedeckt |
| Nicht erreichbarer Server → Fehlertext | `ServerSetupTests.UnreachableServer_ShowsError` (bestehend) | Abgedeckt |
| Erreichbarer Server → Status-Badge + geführte Pairing-Karte sichtbar/bedienbar | `ServerSetupTests.ReachableServer_RequiresPairing` (bestehend) + `ReachableServer_ShowsHealthBadgeAndGuidedPairing` (neu) | Abgedeckt |
| Pairing-Flows (Formatfehler, falscher Code, gültiger Code) | `PairingTests` (3 bestehende) | Abgedeckt |
| „Server wechseln" aus Einstellungen, Karten-Auswahl per `itemIndex` | `ServerSwitchTests.SwitchToKnownServer_ReturnsToHomePage` (bestehend) | Abgedeckt |
| Anmeldeflüsse | `LoginTests` (2 bestehende) | Abgedeckt |
| Visuelle Verifikation | Screenshot(s) in `live-verification.md` | Abgedeckt |

## Hinweise

- Alle gepinnten Elemente/Ids wurden einzeln gegen die geplante XAML-Struktur abgeglichen (siehe `inventory/contract-pins.md`): `AddressEntry`, `ConnectButton`, `DiscoverButton`, `DiscoveredServersView`, `KnownServersView`, `ActiveServerLabel`, `ErrorLabel`, `DiscoveryEmptyLabel`, `PairingSection`, `PairingCodeEntry`, `PairButton`, `PairingIndicator`, `PairingErrorLabel` bleiben erhalten.
- `DoesNotContain("MaxLength=")` gilt dateiweit — im Plan ist an keiner Stelle `MaxLength` vorgesehen; bei der Implementierung explizit beachten.
- Die bewusste Anpassung ist auf die Erweiterung der AutomationId-Menge beschränkt (im Plan und später im Test kommentiert) — kein Pin wird entfernt.
- `BindingContext = mvuHost` für Trigger-Bindings ist das kleinstmögliche Mittel (kein neuer Konverter, keine harten Farben im Code-Behind); Seiteneffekt im Plan benannt.
