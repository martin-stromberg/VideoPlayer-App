# Umsetzungsplan: ModelChanged-Abonnements ohne Abmeldung auf mehreren Pages (Lebenszyklus-Leck)

## Übersicht

Das `MvuHost.ModelChanged`-Abo der sechs Seiten `HomePage`, `MediaDetailsPage`, `DownloadsPage`, `FavoritesPage`, `SourceOverviewPage` und `ProfilePage` wird vom Konstruktor in den Sichtbarkeits-Lebenszyklus verlagert (`OnAppearing`/`OnDisappearing`) und mit dem `subscribed`-Guard der `PlayerPage` abgesichert. Die bestehende `OnAppearing`-Fachlogik der Seiten bleibt unverändert.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Abo-Platzierung | Abo in `OnAppearing` mit `-=` vor `+=`, Abmeldung in `OnDisappearing` | Referenzmuster `SettingsPage`/`PlayerPage`; idempotent bei erneutem Erscheinen; verwaiste Seiten werden abgemeldet. Kein zentraler Lebenszyklus-Mechanismus — die Anforderung erlaubt beides, das Seitenmuster ist bereits vierfach etabliert. |
| `subscribed`-Guard | `private bool subscribed` je Seite; Guard ausschließlich innerhalb des per `MainThread.BeginInvokeOnMainThread` gemarshallten Callbacks | Ein bereits abgesendeter Callback kann nach der Abmeldung noch ausgeführt werden und würde `UpdateUi` auf der losgelösten Seite rendern. `volatile` ist nicht nötig — anders als bei `PlayerPage` (Toolkit-Events auf Fremdthreads) wird das Feld nur auf dem UI-Thread gelesen/geschrieben. |
| Initial-Rendering | Konstruktor behält das initiale `UpdateUi`/`UpdateBanners`+`UpdateHero`; `OnAppearing` aktualisiert die Anzeige zusätzlich | Entspricht dem `PlayerPage`-Muster und stellt sicher, dass ein während der Abwesenheit geändertes Modell bei Rückkehr sofort gerendert wird. |

## Programmabläufe

### Seiten-Lebenszyklus (alle sechs Seiten identisch)

1. `OnAppearing`: `subscribed = true` setzen; `mvuHost.ModelChanged -= OnModelChanged` (Schutz vor Doppel-Abo); `mvuHost.ModelChanged += OnModelChanged`; anschließend die bestehende Fachlogik (z. B. `*PageOpenedMessage`-Dispatch) und die Anzeige-Aktualisierung.
2. `ModelChanged` feuert auf dem Dispatch-Thread → `OnModelChanged` marshallt den Callback auf den UI-Thread; der Callback prüft `subscribed` und ruft nur dann `UpdateUi(model)` auf.
3. `OnDisappearing`: `subscribed = false` setzen, `mvuHost.ModelChanged -= OnModelChanged`, `base.OnDisappearing()`.

Beteiligte Klassen/Komponenten: `HomePage`, `MediaDetailsPage`, `DownloadsPage`, `FavoritesPage`, `SourceOverviewPage`, `ProfilePage`, `MvuHost`.

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `PageLifecycleContractTests` | Testklasse | Contract-Pins des Lebenszyklus-Musters für alle sechs Seiten (quelltextbasiert, Muster `PlayerContractTests`) |

## Änderungen an bestehenden Klassen

### `HomePage` (`VideoPlayer.Maui/Pages/HomePage.xaml.cs`)

- **Neue Felder:** `subscribed` (`bool`) — Sichtbarkeits-Guard für den gemarshallten Modell-Callback.
- **Neue Methoden:** `OnAppearing` (override) — `subscribed = true`, `ModelChanged -=`/`+=`, `UpdateBanners` + `UpdateHero` aufrufen; `OnDisappearing` (override) — `subscribed = false`, `ModelChanged -=`.
- **Geänderte Methoden:** Konstruktor — `mvuHost.ModelChanged += OnModelChanged` entfernen (`SizeChanged += OnSizeChanged` und initiale Anzeige-Aktualisierung bleiben); `OnModelChanged` — `UpdateBanners`/`UpdateHero` im gemarshallten Callback an `subscribed` binden.

### `MediaDetailsPage` (`VideoPlayer.Maui/Pages/MediaDetailsPage.xaml.cs`)

- **Neue Felder:** `subscribed` (`bool`).
- **Neue Methoden:** `OnDisappearing` (override) — `subscribed = false`, `ModelChanged -=`.
- **Geänderte Methoden:** Konstruktor — `ModelChanged +=` entfernen (`SizeChanged +=` und `UpdateUi` bleiben); `OnAppearing` — `subscribed = true`, `ModelChanged -=`/`+=` und `UpdateUi(mvuHost.Model)` vor dem bestehenden `requested`-Guard; `OnModelChanged` — `UpdateUi` an `subscribed` binden.

### `DownloadsPage` (`VideoPlayer.Maui/Pages/DownloadsPage.xaml.cs`)

- **Neue Felder:** `subscribed` (`bool`).
- **Neue Methoden:** `OnDisappearing` (override) — `subscribed = false`, `ModelChanged -=`.
- **Geänderte Methoden:** Konstruktor — `ModelChanged +=` entfernen (`ItemsSource`-Verdrahtung und `UpdateUi` bleiben); `OnAppearing` — `subscribed = true`, `ModelChanged -=`/`+=`, `UpdateUi(mvuHost.Model)` vor dem `DownloadsPageOpenedMessage`-Dispatch; `OnModelChanged` — `UpdateUi` an `subscribed` binden.

### `FavoritesPage` (`VideoPlayer.Maui/Pages/FavoritesPage.xaml.cs`)

- **Neue Felder:** `subscribed` (`bool`).
- **Neue Methoden:** `OnDisappearing` (override) — `subscribed = false`, `ModelChanged -=`.
- **Geänderte Methoden:** Konstruktor — `ModelChanged +=` entfernen (`UpdateUi` bleibt); `OnAppearing` — `subscribed = true`, `ModelChanged -=`/`+=`, `UpdateUi(mvuHost.Model)` vor dem `FavoritesPageOpenedMessage`-Dispatch; `OnModelChanged` — `UpdateUi` an `subscribed` binden.

### `SourceOverviewPage` (`VideoPlayer.Maui/Pages/SourceOverviewPage.xaml.cs`)

- **Neue Felder:** `subscribed` (`bool`).
- **Neue Methoden:** `OnDisappearing` (override) — `subscribed = false`, `ModelChanged -=`.
- **Geänderte Methoden:** Konstruktor — `ModelChanged +=` entfernen (`UpdateUi` bleibt); `OnAppearing` — `subscribed = true`, `ModelChanged -=`/`+=`, `UpdateUi(mvuHost.Model)` vor dem bedingten `SourceBrowserOpenedMessage`-Dispatch; `OnModelChanged` — `UpdateUi` an `subscribed` binden.

### `ProfilePage` (`VideoPlayer.Maui/Pages/ProfilePage.xaml.cs`)

- **Neue Felder:** `subscribed` (`bool`).
- **Neue Methoden:** `OnDisappearing` (override) — `subscribed = false`, `ModelChanged -=`.
- **Geänderte Methoden:** Konstruktor — `ModelChanged +=` entfernen (`UpdateUi` bleibt); `OnAppearing` — `subscribed = true`, `ModelChanged -=`/`+=`, `UpdateUi(mvuHost.Model)` vor dem `ProfilePageOpenedMessage`-Dispatch; `OnModelChanged` — `UpdateUi` an `subscribed` binden.

## Datenbankmigrationen

Keine.

## Validierungsregeln

Keine.

## Konfigurationsänderungen

Keine.

## Seiteneffekte und Risiken

- **Verändertes Update-Verhalten bei unsichtbaren Seiten:** Modelländerungen aktualisieren eine nicht sichtbare Seite nicht mehr — genau das beabsichtigte Verhalten; bei Rückkehr stellt der `UpdateUi`-Aufruf in `OnAppearing` den aktuellen Stand her.
- **`HomePage`:** hat bisher keinen `OnAppearing`-Override — die Seite aktualisierte ihre Banner/Hero bisher dauerhaft; mit dem Muster nur noch bei Sichtbarkeit (Offline-/Storage-Banner werden beim Erscheinen frisch gesetzt).
- **`AppShell`:** abonniert ebenfalls im Konstruktor ohne Abmeldung, ist aber eine lebenslang gültige Instanz — bleibt bewusst unverändert (nicht Teil der Anforderung; `SourcesContractTests`-Pin auf `ModelChanged` in `AppShell.xaml.cs` bleibt gültig).
- **`SizeChanged`-Abos** in `HomePage`/`MediaDetailsPage` bleiben konstruktorgebunden — eigenes Event der Seite, gleiche Lebensdauer, kein Leck.
- **Bestehende Contract-Tests:** Kein vorhandener Test pinnt ein Konstruktor-Abo der sechs Seiten — keine Anpassungen nötig (verifiziert über Volltextsuche).

## Umsetzungsreihenfolge

1. **`PageLifecycleContractTests` anlegen (Tests zuerst, Negativprobe)**
   - Voraussetzungen: Keine (xUnit, `RepositoryRoot`-Stub und `Read`/`MethodBody`-Muster aus `PlayerContractTests` vorhanden).
   - Beschreibung: Neue Testklasse `VideoWebPlayer.Maui.Tests/PageLifecycleContractTests.cs` mit eigenen `Read`/`MethodBody`-Hilfsmethoden; zwei Tests je Seite (`{Page}_UnsubscribesModelChangedOnDisappearing`, `{Page}_IgnoresModelChangesAfterDisappearing`). Tests auf dem Ist-Stand ausführen — müssen fehlschlagen (Negativprobe).

2. **Sechs Seiten auf das Lebenszyklus-Muster umstellen**
   - Voraussetzungen: Keine.
   - Beschreibung: Pro Seite gemäß Abschnitt „Änderungen an bestehenden Klassen": `subscribed`-Feld, Abo-Umzug nach `OnAppearing` (`-=` vor `+=`), `OnDisappearing` mit Guard-Reset + Abmeldung, Guard im gemarshallten `OnModelChanged`-Callback, Konstruktor-Abo entfernen.

3. **Build und vollständige Testsuite**
   - Voraussetzungen: Schritte 1–2.
   - Beschreibung: `dotnet build` der gesamten Solution, danach `dotnet test` — alle neuen Contract-Tests grün, kein bestehender Test rot.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `{Page}_UnsubscribesModelChangedOnDisappearing` (× 6: `HomePage`, `MediaDetailsPage`, `DownloadsPage`, `FavoritesPage`, `SourceOverviewPage`, `ProfilePage`) | `PageLifecycleContractTests` | `ModelChanged -= OnModelChanged` im `OnDisappearing`-Rumpf; `ModelChanged +=` im `OnAppearing`-Rumpf; kein `ModelChanged +=` im Konstruktor |
| `{Page}_IgnoresModelChangesAfterDisappearing` (× 6) | `PageLifecycleContractTests` | `subscribed = false` im `OnDisappearing`-Rumpf; `subscribed`-Guard im `OnModelChanged`-Rumpf |
| `Read`, `MethodBody` | `PageLifecycleContractTests` | Hilfsmethoden (Muster `PlayerContractTests`) |

### Betroffene bestehende Tests

Keine.

### E2E-Tests (primärer Funktionsnachweis)

Keine erforderlich — Begründung: Die Änderung betrifft keinen Benutzerfluss; sie ist rein strukturell und ohne sichtbares Verhalten (das beabsichtigte „Update nur bei sichtbarer Seite" war bereits der fachliche Effekt). Das Nichtvorhandensein eines Abonnements bzw. eines Lecks ist über die UI bzw. die Developer-API (`WaitForModelAsync`) nicht beobachtbar — es existiert kein auslösbarer Nutzerfluss, dessen Happy Path hier getestet werden könnte. Der Funktionsnachweis erfolgt über die strukturellen Contract-Pins (Muster `PlayerContractTests.PlayerPage_UnsubscribesModelChangedOnDisappearing`, dokumentierte Projektkonvention).

## Offene Punkte

Keine.
