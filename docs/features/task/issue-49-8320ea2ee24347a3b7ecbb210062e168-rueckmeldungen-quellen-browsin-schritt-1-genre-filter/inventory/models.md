# Datenmodell

Datenmodellklassen, die für den Genre-Filter des Quellen-Browsing relevant sind.

## `SourceBrowserState`

Datei: `VideoPlayer.Mvu/SourceBrowserState.cs`

Zustand der Quellen-Browsing-Seite (`AppModel.SourceBrowser`, `AppModel.cs:182`). Unveränderlicher Record; `Empty` ist der Ausgangszustand (keine Quelle geöffnet).

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `PageSize` | `const int` = 20 | Seitengröße des Nachladens |
| `Empty` | `static SourceBrowserState` | Ausgangszustand |
| `SourceId` | `long` | Id der geöffneten Quelle; 0 = kein Browser aktiv |
| `SourceName` | `string` | Seitentitel |
| `Genres` | `IReadOnlyList<SourceGenreItem>` | Genre-Leiste inkl. „Alle"-Eintrag (`Id` = 0). **Relevant:** Initial `[]`; nach `UpdateSourceGenresLoaded` stets mindestens `[Alle]` (Service stellt „Alle" voran; Ladefehler-Fallback `[Alle]`). Keine Sichtbarkeits-Eigenschaft vorhanden. |
| `SelectedGenreId` | `long` | Gewählter Genre-Filter; 0 = „Alle" |
| `SearchText` | `string` | Aktiver Suchtext |
| `Items` | `IReadOnlyList<SourceMediaItem>` | Geladene Einträge (kumuliert) |
| `NextPage` | `int` | Nächste zu ladende Seite (0-basiert) |
| `HasMore` | `bool` | Weitere Seiten erwartet |
| `IsLoading` | `bool` | Ersetz-Lauf läuft |
| `IsLoadingMore` | `bool` | Anhänge-Lauf läuft |
| `Attempted` | `bool` | Mindestens ein Ladeversuch abgeschlossen |
| `Error` | `string?` | Fehler des letzten Ersetz-Laufs |
| `IsActive` | `bool` (abgeleitet) | `SourceId > 0` |
| `HasItems` | `bool` (abgeleitet) | `Items.Count > 0` |
| `HasError` | `bool` (abgeleitet) | `Error is not null` |
| `ShowEmpty` | `bool` (abgeleitet) | `Attempted && !IsLoading && !HasError && !HasItems` |

## `SourceGenreItem`

Datei: `VideoPlayer.Mvu/SourceGenreItem.cs`

Eintrag der Genre-Leiste; „Alle" ist der künstliche Eintrag mit `Id` = 0.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `long` (required) | Genre-Id; 0 = „Alle" (`genreId` wird nicht gesendet) |
| `Name` | `string` (required) | Anzeigename |
| `IsSelected` | `bool` | Aktuell gewählter Filter (Chip-Optik per XAML-`DataTrigger`) |

## `SourceGenres` / `Genre` (API-Client)

Datei: `VideoPlayer.ApiClient/Models/MediaSource.cs` (Zeilen 24–56)

Antwortmodell von `GET api/SourceGenres/{id}` (`GetSourceGenresAsync`).

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `SourceGenres.SourceId` | `long` | Id der Quelle |
| `SourceGenres.SourceName` | `string` | Name der Quelle |
| `SourceGenres.Genres` | `List<Genre>` | Genres der Quelle — **kann leer sein (Diagnosepunkt)** |
| `Genre.Id` | `long` | Genre-Id |
| `Genre.Name` | `string` | Anzeigename |
| `Genre.IconUrl` | `string?` | Optionale Icon-URL (wird nicht verwendet) |

## `MediaItemSummary` (API-Client)

Datei: `VideoPlayer.ApiClient` (Modell der `api/items`-Antwort) — Quelle der `SourceMediaItem`-Einträge; nicht geändert.
