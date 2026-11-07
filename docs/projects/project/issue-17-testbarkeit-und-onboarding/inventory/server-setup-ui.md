# Detail: Serververbindungs-UI (Schritt 4)

## Bestehende Struktur (`Pages/ServerSetupPage.xaml`, 113 Z.)

- Kopf (Z. 13–22): Titel + Hinweis + `ActiveServerSection`/`ActiveServerLabel` (AutomationId Z. 20).
- Listen (Z. 26–74): `KnownServersView` (Z. 34, `DataTemplate` Name+Adresse Z. 40–45), `DiscoverButton` (50), `DiscoveryIndicator` (54), `DiscoveryEmptyLabel` (55–58), `DiscoveredServersView` (60, Template Z. 66–70).
- Fuß (Z. 77–111): `AddressEntry` (79, Placeholder `http://server/videoplayer/`), `ConnectButton` (83), `CheckIndicator` (87), `ErrorLabel` (88, `VpDanger`), `PairingSection` (92): `PairingCodeEntry` (98 — bewusst **kein** `MaxLength`, Z. 95–97), `PairButton` (104), `PairingIndicator` (108), `PairingErrorLabel` (109).

## Modellgetriebene Darstellung (`ServerSetupPage.xaml.cs` Z. 48–82)

- `UpdateUi(AppModel)` setzt Sichtbarkeiten, `IsRunning`, `IsEnabled`, Fehlertexte; Handler dispatchen Messages (Z. 84–115). `OnAppearing` dispatcht `LoadServersRequestedMessage` (Z. 33).
- Zustandsfeedback-Quellen: `IsServerCheckRunning`, `ServerSetupError`, `IsPairingRequired`/`IsPairingRunning`/`PairingError`, `IsDiscoveryRunning`/`DiscoveryAttempted`. `ServerHealth`/`ServerHealthText` (`AppModel.cs` Z. 19, 188–194) existiert, wird aber auf der Seite nur indirekt angezeigt → freier Spielraum für „erreichbar/nicht erreichbar"-Badge.

## Pins der Contract-Tests (müssen erhalten oder bewusst angepasst werden)

- `ServerSetupContractTests.cs`: Element-Namen Z. 27–36; AutomationId-Menge Z. 51–59; Dispatch-Nachrichten Z. 68–73; `SettingsPage` „Server wechseln"/`ServerSwitchButton` Z. 78–84; Route `serversetup` ohne FlyoutItem Z. 88–98; DI-Registrierungen Z. 102–109; Effekte Z. 113–120.
- `PairingContractTests.cs`: `PairingCodeEntry`/`PairButton`/`PairingErrorLabel`-AutomationIds (Z. 45–55); `Assert.DoesNotContain("MaxLength=")` (Z. 32); SecureStorage-/Entitlements-Pins (Z. 241–332).

## Gestaltungs-Bausteine

- `Colors.xaml` Z. 12–58: `VpSurface*`, `VpText*`, `VpBorder*`, `VpPrimary*`, `VpSecondary*`, `VpDanger`, `VpHero*`-Gradient-Tokens.
- `Styles.xaml`: implizit `Border` (19–25, `VpBorder`-Stroke), `Button` (31 ff., `IconButton` 64), `Entry` (149), `Label`/`Headline`/`SubHeadline` (193/212/219), `Switch` (366), `Page` (416).
- Karten-/Hero-Muster: `HomePage.xaml` Z. 35–72 (Gradient-Layering über `Border`), Z. 86–96 (Kicker-Badge `RoundRectangle 4`/`VpBorder`), Z. 113–138 (Banner `RoundRectangle 8`/`VpBorderStrong`).
- Server-Karten: bestehende `DataTemplate`s (Name fett + Adresse muted) können zu `Border`-Karten mit Status-Icon/Health-Badge erweitert werden — ItemsSource bleibt modellseitig.

## Umbau-Spielräume

- Neue Elemente benötigen eigene AutomationIds + Contract-Test-Erweiterung (Muster vorhanden).
- MVU-Fluss unverändert: keine neuen Messages nötig für reine Restrukturierung; nur falls zusätzliches Feedback gewünscht (z. B. Health-Badge je Karte) sind `AppModel`-Felder (`ServerHealth`) direkt nutzbar.
- „Klarere Pairing-Führung": `PairingSection` kann als hervorgehobene Karte gestaltet werden; Sichtbarkeit bleibt `IsPairingRequired`.
