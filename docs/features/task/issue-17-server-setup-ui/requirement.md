# Übersetzte Anforderung: Modernisierung der Serververbindungs-UI (`ServerSetupPage`)

Quelle: Projekt `issue-17-testbarkeit-und-onboarding`, Entwicklungsschritt 4 (`project-plan.md` Z. 138–150) samt Auftragskontext.

## Fachliche Zusammenfassung

Die Workflow-Seite `ServerSetupPage` (Erststart-Einrichtung und „Server wechseln" aus den Einstellungen) wird optisch und strukturell modernisiert: klare visuelle Hierarchie mit Kopfbereich (aktiver Server), Server-Einträge als Karten statt reiner Textliste, ein geführtes, hervorgehobenes Pairing-Karten-Element und explizites Zustandsfeedback („Prüfung läuft", „erreichbar", „nicht erreichbar"). Der MVU-Fluss bleibt vollständig unverändert — es ist eine reine UI-Restrukturierung auf Basis des bestehenden `AppModel`, ohne neue Messages oder Effekte. Der Umbau bereitet den Einstiegspunkt für den QR-/Ticket-Bootstrap (Schritt 5b) vor, ohne diesen zu implementieren.

## Akzeptanzkriterien (aus dem Projektplan)

1. **Visuelle Hierarchie:** Kopfbereich mit aktivem Server; bekannte und gefundene Server als Karten (`Border` + `RoundRectangle` + `VpSurface*`/`VpBorder*`) mit Name + Adresse + Status; der Pairing-Abschnitt als hervorgehobene, geführte Karte (Hinweistext „Profilseite öffnen" o. ä. als Vorbereitung für Schritt 5b — der QR-Flow selbst wird NICHT gebaut).
2. **Zustandsfeedback explizit:** „Prüfung läuft" (Indicator), „erreichbar"/„nicht erreichbar" (Badge/Text via `ServerHealth`/`ServerHealthText`/`ErrorLabel`) — visuell sichtbar statt nur implizit.
3. **MVU-Fluss unverändert:** Zustand nur aus `AppModel`, gleiche Dispatch-Messages, Route `serversetup` ohne FlyoutItem, Workflow-Seite bleibt Workflow-Seite.
4. **Pins respektieren:** alle gepinnten AutomationIds bleiben (oder Contract-Tests werden bewusst angepasst — dann im Test erklärt); `PairingCodeEntry` weiterhin ohne `MaxLength`; `Vp*`-Tokens statt harter Farben; Dark-only.

## Betroffene Klassen und Komponenten

- **UI:** `VideoPlayer.Maui/Pages/ServerSetupPage.xaml` (vollständige Restrukturierung), `ServerSetupPage.xaml.cs` (`UpdateUi` erweitern für Status-Anzeigen; keine neuen Dispatch-Messages).
- **Design-System (nur lesend/nutzend):** `Resources/Styles/Colors.xaml` (`Vp*`-Tokens), `Resources/Styles/Styles.xaml` (implizite Styles; ggf. ergänzender Karten-Stil).
- **Vorbilder:** `Pages/HomePage.xaml` (Hero-`Border` + `LinearGradientBrush`, Kicker-Badge `RoundRectangle 4`, Banner `RoundRectangle 8`/`VpBorderStrong`), `Pages/SettingsPage.xaml` (Sektionsstruktur).
- **Modell (unverändert, nur lesend):** `VideoPlayer.Mvu/AppModel.cs` — u. a. `ServerHealth`/`ServerHealthText`, `ActiveServerAddress`, `KnownServers`, `DiscoveredServers`, `IsDiscoveryRunning`, `DiscoveryAttempted`, `IsServerCheckRunning`, `ServerSetupError`, `IsPairingRequired`, `IsPairingRunning`, `PairingError`.
- **Contract-Tests:** `VideoWebPlayer.Maui.Tests/ServerSetupContractTests.cs`, `PairingContractTests.cs` (Pins erhalten oder bewusst anpassen/erweitern).
- **E2E-Tests:** `VideoPlayer.E2E.Tests/Tests/ServerSetupTests.cs` (4 bestehende Szenarien müssen laufen; ggf. Erweiterung um Karten-/Pairing-Sichtbarkeit), `UiDriver`, `DevApiClient`, `MockVideoServer`.
- **Dokumentation:** `docs/help/serververwaltung-erststart.md`, ggf. `README.md`, `docs/RELEASE_NOTES.md`.

## Implementierungsansatz

- Seitenlayout neu strukturieren: Kopfbereich (Titel, Hinweis, Karte „Aktiver Server" mit Status-Badge aus `ServerHealth`/`ServerHealthText`), Sektionen „Bekannte Server" und „Im Netzwerk gefunden" als `CollectionView` mit Karten-`DataTemplate` (`Border`, `RoundRectangle`, `VpSurface*`, `VpBorder*`), Karte „Neuen Server verbinden" mit `AddressEntry`/`ConnectButton`/`CheckIndicator`/`ErrorLabel`, hervorgehobene Karte „Gerät koppeln" (`PairingSection`) mit Führungstext.
- Sichtbarkeiten/Laufzustände ausschließlich aus `AppModel` in `UpdateUi` setzen — dieselben Felder wie bisher plus `ServerHealth`/`ServerHealthText` für das Status-Badge des aktiven Servers.
- Alle gepinnten AutomationIds beibehalten (`ActiveServerLabel`, `KnownServersView`, `DiscoverButton`, `DiscoveredServersView`, `DiscoveryEmptyLabel`, `AddressEntry`, `ConnectButton`, `ErrorLabel`, `PairingSection`/`PairingCodeEntry`/`PairButton`/`PairingIndicator`/`PairingErrorLabel`); neue Elemente erhalten eigene AutomationIds und werden in den Contract-Tests ergänzt.
- `PairingCodeEntry` bleibt ohne `MaxLength` (Pin: `DoesNotContain("MaxLength=")`); Hinweistext ohne feste Stellenzahl (Pin: `DoesNotContain("-stellig")`), enthält weiterhin „Verwaltung".
- Nur `Vp*`-Farbtokens, kein Light-Theme, deutsche UI-Texte in einfacher, sachlicher Sprache.

## Konfiguration

Keine — reine UI-Restrukturierung ohne neue Einstellungen.

## Offene Fragen

Keine — Akzeptanzkriterien, Pins und Design-Vorbilder sind im Projektplan und in der Projektinventur (`inventory/server-setup-ui.md`) vollständig festgelegt.
