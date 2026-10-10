# Übersetzte Anforderung: ModelChanged-Abonnements ohne Abmeldung auf mehreren Pages (Lebenszyklus-Leck)

## Fachliche Zusammenfassung

Sechs `ContentPage`-Klassen der MAUI-App (`VideoPlayer.Maui`) abonnieren das `MvuHost.ModelChanged`-Event im Konstruktor, ohne sich beim Verlassen der Seite wieder abzumelden. Verlassene Seiten-Instanzen bleiben dadurch am `MvuHost` hängen (Speicherleck) und führen weiterhin `UpdateUi` auf Modelländerungen aus. Die Anforderung verlangt, das bereits korrigierte Lebenszyklus-Muster der `PlayerPage` (Abo in `OnAppearing` mit `-=` vor `+=`, Abmeldung in `OnDisappearing`, `subscribed`-Guard) bzw. der `SettingsPage` auf die übrigen Seiten zu übertragen.

## Betroffene Klassen und Komponenten

- `HomePage` (`VideoPlayer.Maui/Pages/HomePage.xaml.cs`) — Abo im Konstruktor, kein `OnDisappearing`
- `MediaDetailsPage` (`VideoPlayer.Maui/Pages/MediaDetailsPage.xaml.cs`) — Abo im Konstruktor, `OnAppearing` vorhanden (Detail-Anforderung), kein `OnDisappearing`
- `DownloadsPage` (`VideoPlayer.Maui/Pages/DownloadsPage.xaml.cs`) — Abo im Konstruktor, `OnAppearing` vorhanden (`DownloadsPageOpenedMessage`), kein `OnDisappearing`
- `FavoritesPage` (`VideoPlayer.Maui/Pages/FavoritesPage.xaml.cs`) — Abo im Konstruktor, `OnAppearing` vorhanden (`FavoritesPageOpenedMessage`), kein `OnDisappearing`
- `SourceOverviewPage` (`VideoPlayer.Maui/Pages/SourceOverviewPage.xaml.cs`) — Abo im Konstruktor, `OnAppearing` vorhanden (`SourceBrowserOpenedMessage`), kein `OnDisappearing`
- `ProfilePage` (`VideoPlayer.Maui/Pages/ProfilePage.xaml.cs`) — Abo im Konstruktor, `OnAppearing` vorhanden (`ProfilePageOpenedMessage`), kein `OnDisappearing`

Referenzimplementierungen (unverändert bleiben):

- `PlayerPage` — korrigiertes Muster inkl. `subscribed`-Guard
- `SettingsPage`, `LoginPage`, `ServerSetupPage` — Abo in `OnAppearing` mit `-=` vor `+=`, Abmeldung in `OnDisappearing`
- `AppShell` — Abo im Konstruktor, aber lebenslang gültige App-Instanz; nicht Teil der Anforderung

Nicht betroffen: `LoadingPage` (kein Abo).

## Implementierungsansatz

- Das `mvuHost.ModelChanged += OnModelChanged` wird aus dem Konstruktor entfernt und in `OnAppearing` verlagert — mit `mvuHost.ModelChanged -= OnModelChanged` unmittelbar vor `+=` (idempotent bei erneutem Erscheinen).
- Neue `OnDisappearing`-Override meldet das Abo mit `mvuHost.ModelChanged -= OnModelChanged` ab.
- Ein `subscribed`-Guard (bool-Feld, in `OnAppearing` auf `true`, in `OnDisappearing` auf `false` gesetzt) schützt die per `MainThread.BeginInvokeOnMainThread` gemarshallten `UpdateUi`-Callbacks: Ein bereits abgesendeter Callback einer verlassenen Seite darf nicht mehr auf deren Elementen rendern.
- Bestehende `OnAppearing`-Logik (Dispatch der `*PageOpenedMessage`/`SourceBrowserOpenedMessage`, `requested`-Guard in `MediaDetailsPage`) bleibt erhalten; die Abo-Verdrahtung ergänzt sie.
- Initiales `UpdateUi(mvuHost.Model)` im Konstruktor bleibt bestehen (Initialrendering wie bei `LoginPage`/`ServerSetupPage` bzw. zusätzlichem `UpdateUi` in `OnAppearing` bei `PlayerPage` — je Seite dem nächstliegenden Referenzmuster folgend).

## Konfiguration

Keine — reine Code-Korrektur ohne Konfigurationsbedarf.

## Offene Fragen

Keine — das Zielmuster ist durch `PlayerPage`/`SettingsPage` eindeutig vorgegeben; die Anforderung benennt die betroffenen Seiten abschließend.
