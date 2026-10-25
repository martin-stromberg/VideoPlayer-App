# Datenmodell

## `SourceBrowserState`
Datei: `VideoPlayer.Mvu/SourceBrowserState.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `PageSize` | `const int` = 20 | Seitengröße des Nachladens |
| `Empty` | `static SourceBrowserState` | Ausgangszustand (keine Quelle geöffnet) |
| `SourceId` | `long` | Id der geöffneten Quelle; 0 = kein Browser aktiv |
| `SourceName` | `string` | Anzeigename (Seitentitel) |
| `Genres` | `IReadOnlyList<SourceGenreItem>` | Genre-Leiste inkl. „Alle" (Id = 0) an Index 0 |
| `SelectedGenreId` | `long` | Gewählter Genre-Filter; 0 = „Alle" |
| `SearchText` | `string` | Aktiver `search`-Parameter |
| `Items` | `IReadOnlyList<SourceMediaItem>` | Geladene Einträge (kumuliert) |
| `NextPage` | `int` | Nächste zu ladende Seite (0-basiert) |
| `HasMore` | `bool` | Weitere Seiten erwartet |
| `IsLoading` / `IsLoadingMore` | `bool` | Ersetz-/Anhänge-Lauf aktiv |
| `Attempted` | `bool` | Mindestens ein Ladeversuch abgeschlossen |
| `Error` | `string?` | Fehlertext des letzten Ersetz-Laufs |
| `IsActive` | `bool` | `SourceId > 0` |
| `HasItems` | `bool` | `Items.Count > 0` |
| `ShowGenreFilter` | `bool` | `Genres.Count > 1` — Leiste nur bei realen Server-Genres |
| `HasError` / `ShowEmpty` | `bool` | Fehler- bzw. Leer-Zustand |

## `SourceGenreItem`
Datei: `VideoPlayer.Mvu/SourceGenreItem.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `long` (required) | Genre-Id; 0 = „Alle" (kein Filter) |
| `Name` | `string` (required) | Anzeigename |
| `IsSelected` | `bool` | Aktuell gewählter Filter |

## `SourceMediaItem`
Datei: `VideoPlayer.Mvu/SourceMediaItem.cs`

Eintrag des Medien-Rasters (`Id`, `Title`, `MediaType`, `PosterPictureId`) — vom Layout-Defekt verdeckt, selbst unverändert.

## `DevApiTreeNode` / `DevApiBounds` (msTools.DeveloperApi.Maui)
Datei: `msTools.DeveloperApi.Maui/DevApiTreeNode.cs`

| Eigenschaft | Typ | Beschreibung |
|-------------|-----|--------------|
| `AutomationId`, `Type`, `IsVisible`, `Text`, `Placeholder` | … | Elementmerkmale |
| `Bounds` | `DevApiBounds?` (`X`, `Y`, `Width`, `Height` als `double`) | Layout-Position/-Größe — **wird serverseitig bereits befüllt** (`DevApiVisualTree.BoundsOf`) |
| `Children` | `IReadOnlyList<DevApiTreeNode>` | Kindknoten |
| `Items` | `IReadOnlyList<DevApiTreeItem>?` | Abgeleitete Einträge (CollectionView-`ItemsSource` oder BindableLayout-Container) |

Gegenstück im E2E-Projekt: `VideoPlayer.E2E.Tests/Support/DevApiModels.cs` — `DevApiTreeNode` besitzt dort **kein** `bounds`-Feld (Stand der Bestandsaufnahme).
