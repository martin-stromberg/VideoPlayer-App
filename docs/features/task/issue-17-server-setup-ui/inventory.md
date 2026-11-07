# Bestandsaufnahme: Modernisierung der Serververbindungs-UI (`ServerSetupPage`)

Analysiert wurde die Server-Einrichtungs-/Verwaltungsseite der MAUI-App samt Design-System, MVU-Modell, gepinnten Contract-Tests und E2E-Infrastruktur — bezogen auf Schritt 4 des Projektplans `issue-17-testbarkeit-und-onboarding`.

## Zusammenfassung

- `ServerSetupPage.xaml` (113 Zeilen) ist eine funktionale, aber schlichte Workflow-Seite: Text-Header, zwei `CollectionView`s mit einfachem `Grid`-`DataTemplate` (Name fett + Adresse muted), Fußbereich mit manueller Eingabe und Pairing-Abschnitt. Keine Karten, kein Status-Badge, keine visuelle Hierarchie.
- Das MVU-Modell liefert bereits alle benötigten Zustandsfelder — einschließlich des bislang auf dieser Seite ungenutzten `ServerHealth`/`ServerHealthText` („Server: erreichbar"/„nicht erreichbar"/„wird geprüft …"). Keine neuen Model-Felder oder Messages nötig.
- Das Design-System ist vollständig vorhanden: `Vp*`-Farbtokens (`VpSurface*`, `VpBorder*`, `VpPrimary`, `VpSecondary`, `VpDanger`), implizite Styles (Border mit `VpBorder`-Stroke, Button Radius 4, Entry, Label) und etablierte Karten-/Hero-Muster in `HomePage.xaml` (Gradient-`Border`, Kicker-Badge `RoundRectangle 4`, Banner `RoundRectangle 8`/`VpBorderStrong`) sowie Sektionsstruktur in `SettingsPage.xaml`.
- Gepinnte Verträge: `ServerSetupContractTests` (Element-Namen, AutomationId-Menge, Dispatch-Messages, Route ohne FlyoutItem, DI, Effekte) und `PairingContractTests` (`PairingCodeEntry`/`PairButton`/`PairingErrorLabel`-Ids, `DoesNotContain("MaxLength=")`, `DoesNotContain("-stellig")`, enthält „Verwaltung"). `DesignTokensContractTests` verbietet harte Farben/Hexwerte in allen `Pages/*.xaml` und fordert auflösbare `StaticResource`-Schlüssel; Kartenradius `RoundRectangle 8` ist die Konvention.
- E2E-Infrastruktur (`VideoPlayer.E2E.Tests`, neu ab Schritt 3): `AppFixture` (Debug-Build), `AppInstance` (isoliertes Profil + `devapi.json`), `UiDriver` (UIA3: `Exists`, `EnterText`, `ClickButton`, `OpenFlyoutItem`, `DumpTree`), `DevApiClient` (`GET /model`/`/tree`, `POST /invoke` inkl. `itemIndex` für CollectionView, `POST /set-text`), `MockVideoServer` (health/pairing/auth/Listen). 4 `ServerSetupTests`-Szenarien, 3 `PairingTests`, 1 `ServerSwitchTests`, Login-Tests vorhanden.
- Test-Ausgangszustand: `VideoWebPlayer.Maui.Tests` **926/926 bestanden**, `VideoPlayer.E2E.Tests` **10/10 bestanden** (Nachweis in `inventory/tests.md`).

## Details

- [UI-Struktur und Elemente](inventory/ui-structure.md)
- [Modell und Dispatch-Messages](inventory/model.md)
- [Design-System und UI-Muster](inventory/design-system.md)
- [Contract-Test-Pins](inventory/contract-pins.md)
- [Tests und Test-Ausgangszustand](inventory/tests.md)

## Verwandte Projektartefakte

Die Projektinventur enthält bereits eine vollständige Analyse unter `docs/projects/project/issue-17-testbarkeit-und-onboarding/inventory/server-setup-ui.md` — ihre Befunde sind hier bestätigt und mit Zeilenreferenzen auf den aktuellen Stand konkretisiert.
