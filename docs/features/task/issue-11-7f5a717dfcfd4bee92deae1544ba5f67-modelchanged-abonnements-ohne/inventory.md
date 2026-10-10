# Bestandsaufnahme: ModelChanged-Abonnements ohne Abmeldung auf mehreren Pages (Lebenszyklus-Leck)

Untersucht wurde der Lebenszyklus der `MvuHost.ModelChanged`-Abonnements aller `ContentPage`-Klassen in `VideoPlayer.Maui/Pages/` sowie die vorhandene Testinfrastruktur (`VideoWebPlayer.Maui.Tests`), bezogen auf die Anforderung aus Issue #11.

## Zusammenfassung

- Sechs Seiten abonnieren `mvuHost.ModelChanged` **im Konstruktor** und melden sich nie ab: `HomePage`, `MediaDetailsPage`, `DownloadsPage`, `FavoritesPage`, `SourceOverviewPage`, `ProfilePage`. Keine dieser Seiten besitzt einen `OnDisappearing`-Override.
- Vier Seiten nutzen bereits das korrekte Lebenszyklus-Muster (Abo in `OnAppearing` mit `-=` vor `+=`, Abmeldung in `OnDisappearing`): `SettingsPage`, `LoginPage`, `ServerSetupPage`, `PlayerPage` (letztere zusätzlich mit `subscribed`-Guard).
- `HomePage` besitzt aktuell gar kein `OnAppearing` — der Override muss neu angelegt werden. Die übrigen fünf Seiten haben bereits `OnAppearing` mit fachlicher Logik (Dispatch von `*PageOpenedMessage` bzw. `SourceBrowserOpenedMessage`, `requested`-Guard in `MediaDetailsPage`), die erhalten bleiben muss.
- `AppShell` abonniert `ModelChanged` ebenfalls im Konstruktor ohne Abmeldung — als lebenslang gültige App-Instanz ist das kein Leck und nicht Teil der Anforderung.
- `LoadingPage` abonniert nichts.
- Die `OnModelChanged`-Handler aller betroffenen Seiten marshallen `UpdateUi` bereits über `MainThread.BeginInvokeOnMainThread`; ein bereits abgesendeter Callback kann aber auch nach dem Abmelden noch ausgeführt werden — dafür dient der `subscribed`-Guard des `PlayerPage`-Musters.
- Testinfrastruktur: Die MAUI-Schicht ist im Testprojekt nicht instanziierbar; Contract-Tests prüfen die Quelldateien strukturell (`MethodBody`-Helfer in `PlayerContractTests`). Es existiert bereits der Referenztest `PlayerContractTests.PlayerPage_UnsubscribesModelChangedOnDisappearing`.
- Es gibt ein E2E-Projekt `VideoPlayer.E2E.Tests` (AppFixture-basiert); es enthält keine Seiten-Lebenszyklus-Tests.

## Details

- [Logik (Seiten und MVU-Host)](inventory/logic.md)
- [Tests](inventory/tests.md)
