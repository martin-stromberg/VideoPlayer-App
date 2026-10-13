# Bestandsaufnahme — Logik

## `SourceOverviewPage`

Datei: `VideoPlayer.Maui/Pages/SourceOverviewPage.xaml.cs`
XAML: `VideoPlayer.Maui/Pages/SourceOverviewPage.xaml`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `SourceOverviewPage(MvuHost)` | public ctor | `UpdateUi` initial, `BindingContext = mvuHost` |
| `OnAppearing` | protected override | `ModelChanged`-Abo aktivieren, `UpdateUi`, ggf. `SourceBrowserOpenedMessage` dispatchen |
| `OnDisappearing` | protected override | Abo lösen (`subscribed = false`, `-= OnModelChanged`) |
| `OnModelChanged` | private | Marshals auf den MainThread, ruft `UpdateUi` nur bei aktivem `subscribed` |
| `UpdateUi(AppModel)` | private | Fehler-/Leerzustände, Sichtbarkeiten — **und Feldspiegelung**: `if (MediaSearchBar.Text != browser.SearchText) MediaSearchBar.Text = browser.SearchText;` (Zeilen 96–101, der Defekt) |
| `OnGenreSelected` | private | `SourceGenreSelectedMessage` dispatchen, Auswahl zurücksetzen |
| `OnSearchButtonPressed` | private | `SourceSearchSubmittedMessage(MediaSearchBar.Text)` dispatchen |
| `OnMediaItemSelected` | private | `SourceMediaItemSelectedMessage` dispatchen |
| `OnRemainingItemsThresholdReached` | private | `SourceItemsNextPageRequestedMessage` dispatchen |
| `OnRetryClicked` | private | `SourceItemsRetryRequestedMessage` dispatchen |

Abonnierte Events: `MvuHost.ModelChanged` (nur während Sichtbarkeit).
Publizierte Messages: `SourceBrowserOpenedMessage`, `SourceGenreSelectedMessage`,
`SourceSearchSubmittedMessage`, `SourceMediaItemSelectedMessage`,
`SourceItemsNextPageRequestedMessage`, `SourceItemsRetryRequestedMessage`.

XAML-Befund: `MediaSearchBar` (SearchBar, `SearchButtonPressed=
OnSearchButtonPressed`) hat **keine** `AutomationId`; kein
`TextChanged`-Handler verdrahtet.

## `AppUpdate` (Quellen-Browser-Ausschnitt)

Datei: `VideoPlayer.Mvu/AppUpdate.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `Update` | public static | Dispatch-Tabelle (Zeile ~200: Browser-Messages) |
| `NewBrowser(long, string)` | private static | Frischer `SourceBrowserState` (IsLoading, HasMore; `SearchText` leer) — Z. 1525 |
| `UpdateSourceSelected` | private static | Neue Quelle → `NewBrowser` + Navigate/Genres/Items-Effekte — Z. 1533 |
| `UpdateSourceBrowserOpened` | private static | Gleiche Quelle → unverändert; andere/verwaiste → `NewBrowser` + Laden — Z. 1549 |
| `UpdateSourceGenresLoaded` | private static | Genres übernehmen, `IsSelected` markieren — Z. 1573 |
| `UpdateSourceItemsLoaded` | private static | Eintragsseite eintragen/anhängen, Fehlerzustände — Z. 1597 |
| `ReloadBrowserItems` | private static | Filter-/Suchwechsel: Liste zurücksetzen, `LoadSourceItemsEffect` ab Seite 0 — Z. 1654 |
| `UpdateSourceGenreSelected` | private static | Genre-Wechsel → `ReloadBrowserItems` — Z. 1678 |
| `UpdateSourceSearchSubmitted` | private static | Trimmen; unverändert/inaktiv → `Unchanged`; sonst `ReloadBrowserItems` mit neuem `SearchText` — Z. 1696 |
| `UpdateSourceItemsNextPageRequested` | private static | Folgeseite (`Append: true`) mit `SearchText`/`SelectedGenreId` — Z. 1709 |
| `UpdateSourceItemsRetryRequested` | private static | Retry: Neu-Lauf oder Folgeseite — Z. 1727 |

`SourceBrowserState.Empty`-Resets (verwerfen auch `SearchText`):
Serverwechsel (Z. 559), Gerät widerrufen (Z. 896), Sitzung abgelaufen
(Z. 1277), Abmeldung (Z. 3529).

## `MvuHost`

Datei: `VideoPlayer.Mvu/MvuHost.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `DispatchAsync` | public | Serialisiert Updates über `SemaphoreSlim`, feuert `ModelChanged` |
| `ModelChanged`-Event | public | Nach jedem angewendeten Update (beliebiger Thread — Seite marshalt selbst) |

Ticks entstehen u. a. durch Sync-/Fortschritts-/Netzwerkereignisse und jeden
abgeschlossenen Ladeeffekt — genau die in der Anforderung genannten
Überschreib-Auslöser.

## `ShellAppEffectHandler.LoadSourceItemsAsync`

Datei: `VideoPlayer.Maui/Services/ShellAppEffectHandler.cs` (Z. 835)

Ruft `SourceBrowserService.LoadItemsAsync(SourceId, Page, SearchText,
GenreId)` → `api/items` mit `search`-Parameter (`VideoPlayerApiClient.cs:374ff`),
Ergebnis als `SourceItemsLoadedMessage`.

## Vergleichbares Muster: `PlaylistFormPanel`

Datei: `VideoPlayer.Maui/Controls/PlaylistFormPanel.xaml.cs`

`ApplyModel` schreibt Feldinhalte **nur beim Öffnen** bzw. externer
Änderung — ein laufender Tippvorgang wird nicht zurückgespiegelt
(Kommentar Zeilen 55–56). Entwurfsfelder leben als `PlaylistFormState.Name`
/`Description` im Modell und werden per `TextChanged`-Messages
(`PlaylistFormNameChangedMessage` u. a.) gefüttert — der Gegenfluss ins
Feld bleibt jedoch bewusst eingeschränkt.
