# Datenmodell — Quellen-Browsing

## `SourceBrowserState`

Datei: `VideoPlayer.Mvu/SourceBrowserState.cs`
Typ: `public sealed record` (unveränderlicher MVU-Zustand; `static Empty` als Ausgangszustand)

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `PageSize` | `const int = 20` | Seitengröße des Nachladens |
| `Empty` | `static SourceBrowserState` | Ausgangszustand (kein Browser aktiv) |
| `SourceId` | `long` | Id der geöffneten Quelle; 0 = inaktiv |
| `SourceName` | `string` | Seitentitel |
| `Genres` | `IReadOnlyList<SourceGenreItem>` | Genre-Leiste inkl. synthetischem „Alle" (Id 0) |
| `SelectedGenreId` | `long` | Aktiver Genre-Filter; 0 = „Alle" |
| `SearchText` | `string` | Abgeschickter Suchtext; leer = keine Suche |
| `Items` | `IReadOnlyList<SourceMediaItem>` | **Kumulierte Einträge — wird bei jedem Update durch neue Instanz ersetzt (Ursache des Sprungs)** |
| `NextPage` | `int` | Nächste zu ladende Seite (0-basiert) |
| `HasMore` | `bool` | Weitere Seiten vermutlich vorhanden |
| `IsLoading` | `bool` | Ersetz-Lauf läuft (Erstladen/Filterwechsel) |
| `IsLoadingMore` | `bool` | Anhänge-Lauf läuft (Folgeseite) |
| `Attempted` | `bool` | Mindestens ein Ladeversuch abgeschlossen |
| `Error` | `string?` | Fehlertext des letzten Ersetz-Laufs |
| `IsActive` | `bool` (abgeleitet) | `SourceId > 0` |
| `HasItems` | `bool` (abgeleitet) | `Items.Count > 0` |
| `ShowGenreFilter` | `bool` (abgeleitet) | `Genres.Count > 1` (Schritt 1) |
| `HasError` | `bool` (abgeleitet) | `Error is not null` |
| `ShowEmpty` | `bool` (abgeleitet) | Erfolgreich geladen, aber leer |

## `SourceMediaItem`

Datei: `VideoPlayer.Mvu/SourceMediaItem.cs`
Typ: `public sealed record` — anzeigefertiger Eintrag des Medienrasters

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `long` (required) | Medien-Id — stabile Identität des Eintrags |
| `Title` | `string` (required) | Anzeigetitel |
| `MediaType` | `string` (required) | Normalisierter Medientyp (CLR-Konvention) |
| `PosterPictureId` | `long?` | Bild-Id für `api/pictures/{id}` |
| `HasPoster` | `bool` (abgeleitet) | `PosterPictureId is > 0` |

Direkt als ItemsSource-Element bindbar (XAML: `x:DataType="mvu:SourceMediaItem"`)
— kein veränderlicher Anzeigezustand pro Eintrag, daher kein ViewModel nötig.

## `SourceGenreItem`

Datei: `VideoPlayer.Mvu/SourceGenreItem.cs`
Typ: `public sealed record` — `Id`, `Name`, `IsSelected` (Schritt 1, unverändert relevant)

## `LoadSourceItemsEffect`

Datei: `VideoPlayer.Mvu/AppEffect.cs`
Record mit `SourceId`, `Page`, `SearchText`, `GenreId`, `Append` —
`Append: true` = Folgeseite anhängen, `Append: false` = Liste ersetzen.

## `SourceItemsLoadedMessage`

Datei: `VideoPlayer.Mvu/AppMessage.cs`
Record mit `SourceId`, `Page`, `Items` (`IReadOnlyList<SourceMediaItem>`),
`HasMore`, `Append`, `Error` — Antwort des Ladeeffekts.

## `SourceItemsNextPageRequestedMessage`

Datei: `VideoPlayer.Mvu/AppMessage.cs` — parameterlose Nachricht,
ausgelöst durch `OnRemainingItemsThresholdReached` der Seite.
