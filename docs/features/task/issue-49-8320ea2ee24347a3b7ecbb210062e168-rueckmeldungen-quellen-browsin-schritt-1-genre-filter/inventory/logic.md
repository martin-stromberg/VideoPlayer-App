# Logik

Logikklassen des Genre-Filter-Pfads im Quellen-Browsing.

## `AppUpdate` (`VideoPlayer.Mvu/AppUpdate.cs`)

Zentrale Update-Funktion des MVU-Kreislaufs (`public static AppTransition Update(AppModel, AppMessage)`).

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `NewBrowser(long, string)` | `private static` | Baut einen frischen `SourceBrowserState` (`IsLoading = true`, `HasMore = true`, `Genres = []`) (Zeilen 1525–1531) |
| `UpdateSourceSelected(AppModel, SourceSelectedMessage)` | `private static` | Browser initialisieren + `NavigateToSourceOverviewEffect` + `LoadSourceGenresEffect` + `LoadSourceItemsEffect(Seite 0, Append: false)` (Zeilen 1533–1547) |
| `UpdateSourceBrowserOpened(AppModel, SourceBrowserOpenedMessage)` | `private static` | Rückkehr auf gleiche Quelle → unverändert; sonst Browser initialisieren + `LoadSourceGenresEffect` + `LoadSourceItemsEffect` (Zeilen 1549–1571) |
| `UpdateSourceGenresLoaded(AppModel, SourceGenresLoadedMessage)` | `private static` | Übernimmt `Genres` (setzt `IsSelected` passend zu `SelectedGenreId`); verwirft Antworten verlassener Quellen (`!IsActive` / `SourceId`-Mismatch) (Zeilen 1573–1593) |
| `UpdateSourceGenreSelected(AppModel, SourceGenreSelectedMessage)` | `private static` | Setzt `SelectedGenreId`, markiert `IsSelected` auf den Einträgen, `ReloadBrowserItems`; identische Auswahl → unverändert (Zeilen 1678–1694). `GenreId = 0` („Alle") hebt den Filter auf |
| `ReloadBrowserItems(AppModel, SourceBrowserState)` | `private static` | Liste zurücksetzen (`Items = []`, `NextPage = 0`, `HasMore = true`, `IsLoading = true`, `Error = null`) + `LoadSourceItemsEffect` mit `SearchText`/`GenreId` (Zeilen 1654–1676) |
| `UpdateSourceItemsLoaded` / `UpdateSourceItemsNextPageRequested` / `UpdateSourceItemsRetryRequested` | `private static` | Paging-/Fehlerpfad der Eintragsliste (Zeilen 1595–1739) |

Publizierte Effekte: `NavigateToSourceOverviewEffect`, `LoadSourceGenresEffect` (`AppEffect.cs:221`), `LoadSourceItemsEffect`.

## `ShellAppEffectHandler` (`VideoPlayer.Maui/Services/ShellAppEffectHandler.cs`)

Implementiert `IAppEffectHandler`; Effect-Switch ab Zeile 101 (`case LoadSourceGenresEffect` ~Zeile 190).

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `LoadSourceGenresAsync(LoadSourceGenresEffect, CancellationToken)` | `private` | `SourceBrowserService.LoadGenresAsync` → `SourceGenresLoadedMessage`; **Ladefehler-Fallback: Liste nur „Alle"** (Fehler nur per `LogError` protokolliert, kein Fehlerzustand) (Zeilen 816–833) |
| `LoadSourceItemsAsync(LoadSourceItemsEffect, CancellationToken)` | `private` | `LoadItemsAsync` → `SourceItemsLoadedMessage`; Fehler → leere Seite + Fehlertext (Zeilen 835–853) |
| `LoadSourcesAsync` / `NavigateToSourceOverviewAsync` | `private` | Quellenliste bzw. Navigation zur Route `sourceoverview?id=…&name=…` |

## `SourceBrowserService` (`VideoPlayer.Mvu/SourceBrowserService.cs`)

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `AllGenresName` | `public const string` = „Alle" | Anzeigename des Filter-Eintrags ohne Genre-Einschränkung (Zeile 26) |
| `LoadGenresAsync(long, CancellationToken)` | `public` | `GetSourceGenresAsync(sourceId)` → `SourceGenreItem`-Liste mit vorangestelltem „Alle" (`Id = 0`, `IsSelected = true`); ohne Client nur „Alle"; ungültige Server-Genres (`Id <= 0` / leerer Name) werden übersprungen — **bei leerer Serverliste Ergebnis `[Alle]`** (Zeilen 74–99) |
| `LoadItemsAsync(long, int, string?, long?, CancellationToken)` | `public` | `api/items?mediaSourceId=…&page=…&size=…[&search=…][&genreId=…]` → `SourceItemsPage` (Zeilen 115–148) |
| `MapSummaryType(string)` | `internal static` | Normalisierung der Medientypen (Zeilen 157–167) |

## `SourceOverviewPage` (`VideoPlayer.Maui/Pages/`)

| Mitglied | Sichtbarkeit | Kurzbeschreibung |
|----------|-------------|------------------|
| `GenresCollection` (XAML, `.xaml:24-58`) | — | `CollectionView`, `ItemsSource="{Binding Model.SourceBrowser.Genres}"`, horizontal, Chip-Template mit `IsSelected`-Trigger; `SelectionChanged="OnGenreSelected"`; **kein `IsVisible`-Attribut** |
| `UpdateUi(AppModel)` (`.xaml.cs:88-102`) | `private` | Setzt `IsVisible` von `ErrorPanel`, `FooterErrorPanel`, `EmptyLabel`, `ItemsCollection` aus dem Browser-Zustand; spiegelt `SearchText` zurück. **Steuert `GenresCollection` nicht.** |
| `OnGenreSelected` (`.xaml.cs:104-115`) | `private` | Dispatcht `SourceGenreSelectedMessage(genre.Id)`, setzt `SelectedItem = null` |
| `OnAppearing` (`.xaml.cs:48-67`) | `protected override` | `ModelChanged`-Abo + `SourceBrowserOpenedMessage` bei neuer/unversuchter Quelle |

## `VideoPlayerApiClient` (`VideoPlayer.ApiClient/VideoPlayerApiClient.cs`)

| Methode | Signatur | Zweck |
|---------|----------|-------|
| `GetSourceGenresAsync` | `(long sourceId, CancellationToken) → Task<SourceGenres>` | `GET api/SourceGenres/{id}` — wirft bei `sourceId <= 0` (Zeilen 229–237) |
| `GetSourceItemsAsync` | `(mediaSourceId, page, pageSize, searchText?, genreId?, ct)` | `GET api/items?…` — `genreId` nur bei `> 0` angehängt (Zeilen 377–399) |

## Altbestand `SourceOverviewViewModel` (`VideoWebPlayer.Maui/ViewModels/SourceOverviewViewModel.cs:110-156`)

`LoadGenresAsync` ruft denselben Endpunkt (`RequestSourceGenresAsync` → `api/SourceGenres/{id}`), stellt „Alle" (`Id = 0`) voran und wählt es aus; bei Fehler/leerer Antwort bleibt nur „Alle" — identisches Verhalten, keine Sichtbarkeitsregel auch im Altbestand.
