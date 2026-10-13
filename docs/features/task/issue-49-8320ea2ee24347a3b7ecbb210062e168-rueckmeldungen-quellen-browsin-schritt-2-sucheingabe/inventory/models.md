# Bestandsaufnahme — Datenmodell

## `SourceBrowserState`

Datei: `VideoPlayer.Mvu/SourceBrowserState.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `PageSize` | `const int` = 20 | Seitengröße des Nachladens |
| `Empty` | `static SourceBrowserState` | Ausgangszustand (keine Quelle geöffnet) |
| `SourceId` | `long` | Id der geöffneten Quelle; 0 = inaktiv |
| `SourceName` | `string` | Anzeigename der Quelle (Seitentitel) |
| `Genres` | `IReadOnlyList<SourceGenreItem>` | Genre-Leiste inkl. „Alle"-Eintrag (Id 0) |
| `SelectedGenreId` | `long` | Gewählter Genre-Filter; 0 = „Alle" |
| `SearchText` | `string` | **Abgeschickter** Suchtext (`search`-Parameter); wird nur bei `SourceSearchSubmittedMessage` geändert; leer = keine Suche |
| `Items` | `IReadOnlyList<SourceMediaItem>` | Geladene Einträge (kumuliert) |
| `NextPage` | `int` | Nächste zu ladende Seite (0-basiert) |
| `HasMore` | `bool` | Weitere Seiten vermutlich vorhanden |
| `IsLoading` | `bool` | Erste Seite lädt (Ersetzen) |
| `IsLoadingMore` | `bool` | Folgeseite lädt (Anhängen) |
| `Attempted` | `bool` | Mindestens ein Ladeversuch abgeschlossen |
| `Error` | `string?` | Fehlertext des letzten Ersetz-Laufs |
| `IsActive` | `bool` (berechnet) | `SourceId > 0` |
| `HasItems` | `bool` (berechnet) | `Items.Count > 0` |
| `ShowGenreFilter` | `bool` (berechnet) | Leiste sichtbar nur bei realen Server-Genres (Schritt 1) |
| `HasError` | `bool` (berechnet) | `Error is not null` |
| `ShowEmpty` | `bool` (berechnet) | Erfolgreich geladen, aber leer |

**Befund:** Kein Feld für die unbestätigte Eingabe; `SearchText` ist
ausschließlich der zuletzt abgeschickte Filter.

## `SourceGenreItem` / `SourceMediaItem`

Dateien: `VideoPlayer.Mvu/SourceBrowserModels.cs`-Umgebung (Genre-Item mit
`Id`, `Name`, `IsSelected`; Medien-Item mit `Id`, `MediaType`, `Title`,
`PosterPictureId`). Für die Suche nicht direkt relevant.

## `DevApiModelSnapshot` (Auszug)

Datei: `VideoPlayer.Mvu/DevApi/DevApiModelSnapshot.cs`

| Eigenschaft | Typ | Beschreibung |
|-------------|-----|--------------|
| `SourceBrowserSourceId` | `long` | Geöffnete Quelle (0 = kein Browser) |
| `SourceBrowserGenreNames` | `IReadOnlyList<string>` | Genre-Namen inkl. „Alle" |
| `SourceBrowserSelectedGenreId` | `long` | Gewählter Genre-Filter |
| `SourceBrowserShowGenreFilter` | `bool` | Leiste sichtbar |
| `SourceBrowserItemTitles` | `IReadOnlyList<string>` | Geladene Eintragstitel |

**Befund:** `SourceBrowser.SearchText` wird nicht exponiert — kein
E2E-Lesekanal für den aktiven Suchfilter vorhanden.
