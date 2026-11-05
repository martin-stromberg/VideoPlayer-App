# Datenmodell — Bestandsaufnahme

## `SourceItem`
Datei: `VideoPlayer.Mvu/SourceItem.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `long` (required) | Quellen-Id (Navigations-/Ladekontext) |
| `Name` | `string` (required) | Anzeigename der Quelle |
| `IconPictureId` | `long?` | Bild-Id für `api/sourceicons/{id}`; `null` = kein Icon |
| `HasIcon` | `bool` (abgeleitet) | `IconPictureId is > 0` |

## `SourceGenreItem`
Datei: `VideoPlayer.Mvu/SourceGenreItem.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `long` (required) | Genre-Id; 0 = „Alle" (kein Filter) |
| `Name` | `string` (required) | Anzeigename |
| `IsSelected` | `bool` | Aktuell gewählter Filter |

## `SourceMediaItem`
Datei: `VideoPlayer.Mvu/SourceMediaItem.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `long` (required) | Medien-Id (Navigationsziel) |
| `Title` | `string` (required) | Anzeigetitel |
| `MediaType` | `string` (required) | Normalisierter Typ (`moviecollection`, `tvshow`, …) |
| `PosterPictureId` | `long?` | Bild-Id für `api/pictures/{id}` |
| `HasPoster` | `bool` (abgeleitet) | `PosterPictureId is > 0` |

## `SourceBrowserState`
Datei: `VideoPlayer.Mvu/SourceBrowserState.cs` — `record`, `Empty`-Singleton, `PageSize = 20`.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `SourceId` | `long` | Geöffnete Quelle; 0 = kein Browser aktiv |
| `SourceName` | `string` | Seitentitel |
| `Genres` | `IReadOnlyList<SourceGenreItem>` | Genre-Leiste inkl. „Alle" (Id 0, Index 0) |
| `SelectedGenreId` | `long` | Aktiver Genre-Filter; 0 = „Alle" |
| `SearchText` | `string` | Aktiver Suchtext; leer = keine Suche |
| `Items` | `IReadOnlyList<SourceMediaItem>` | Kumulierte Einträge aller Seiten |
| `NextPage` | `int` | Nächste zu ladende Seite (0-basiert) |
| `HasMore` | `bool` | Weitere Seiten erwartbar |
| `IsLoading` | `bool` | Erste Seite / gefilterte Liste lädt (Ersetzen) |
| `IsLoadingMore` | `bool` | Folgeseite lädt (Anhängen) |
| `Attempted` | `bool` | Mindestens ein Ladeversuch abgeschlossen |
| `Error` | `string?` | Fehlertext des letzten Ersetz-Laufs |
| `IsActive` | `bool` (abgeleitet) | `SourceId > 0` |
| `HasItems` | `bool` (abgeleitet) | `Items.Count > 0` |
| `ShowGenreFilter` | `bool` (abgeleitet) | `Genres.Count > 1` (mehr als „Alle") |
| `HasError` | `bool` (abgeleitet) | `Error is not null` |
| `ShowEmpty` | `bool` (abgeleitet) | `Attempted && !IsLoading && !HasError && !HasItems` |

## `SourcesOverviewState`
Datei: `VideoPlayer.Mvu/SourcesOverviewState.cs` — `record`, `Initial`-Singleton.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `IsLoading` | `bool` | Quellenliste lädt |
| `Attempted` | `bool` | Mindestens ein Ladeversuch abgeschlossen |
| `Error` | `string?` | Fehlertext |
| `Sources` | `IReadOnlyList<SourceItem>` | Freigegebene Quellen |
| `HasSources` | `bool` (abgeleitet) | `Sources.Count > 0` |
| `HasError` | `bool` (abgeleitet) | `Error is not null` |
| `ShowEmpty` | `bool` (abgeleitet) | `Attempted && !IsLoading && !HasError && !HasSources` |

## `HomeMediaItem`
Datei: `VideoPlayer.Mvu/HomeMediaItem.cs` — `record`; required: `Id`, `MediaType`, `Title`. Optional u. a. `PosterPictureId`, `Progress`, `IsPlaceholder`; abgeleitet `CardOpacity`, `ShowProgress`, `HasPoster`, `IsSelectable`. Dient als Update-Eingabe der `HomeMediaItemCell` — ein `SourceMediaItem` ist darauf abbildbar (`Id`, `MediaType`, `Title`, `PosterPictureId`).

## `AppModel` (relevante Ausschnitte)
Datei: `VideoPlayer.Mvu/AppModel.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `SourcesOverview` | `SourcesOverviewState` | Quellenliste des Menüs (Z. 182) |
| `SourceBrowser` | `SourceBrowserState` | Browser-Zustand (Z. 185) |
| `IsOffline` | `bool` | Offline-Zustand |
| `CurrentPage` | `AppPage` | Angezeigte Seite |
