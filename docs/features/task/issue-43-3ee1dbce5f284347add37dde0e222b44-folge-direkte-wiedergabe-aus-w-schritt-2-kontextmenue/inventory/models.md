# Bestandsaufnahme: Datenmodell

## `HomeMediaItem`
Datei: `VideoPlayer.Mvu/HomeMediaItem.cs`

Anzeigefertiger Eintrag der Startseiten-Listen (sealed record).

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `long` (required) | Medien-Id (Navigations-/API-Ziel). |
| `MediaType` | `string` (required) | Medientyp — bei Weiterschauen-Einträgen die Server-Konvention (`movie`, `show`, `season`, `episode`, `collection`), sonst CLR-Konvention. |
| `Title` | `string` (required) | Anzeigetitel. |
| `PosterPictureId` | `long?` | Bild-Id für `api/pictures/{id}`. |
| `Progress` | `double?` | Fortschritt 0–1 (nur Weiterschauen). |
| `ShowId` / `SeasonId` / `EpisodeId` | `long?` | Serienkontext-Hints. |
| `IsPlaceholder` | `bool` | Offline-Platzhalter (nicht abspielbar/nicht auswählbar). |
| `IsSelectable` | `bool` (computed) | `!IsPlaceholder`. |
| `CardOpacity` | `double` (computed) | 0.55 bei Platzhalter. |
| `HasPoster` / `ShowProgress` | `bool` (computed) | Anzeige-Hilfen. |

## `ContinueWatchingEntry`
Datei: `VideoPlayer.ApiClient/Models/ContinueWatching.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `MediaType` | `string` | Server-Medientyp (`movie`, `episode`, …). |
| `Entry` | `MediaEntry?` | Eingebetteter Medieneintrag (trägt `Id`, `Name`, ggf. Seriengraph). |
| `PositionSeconds` | `long` | Gemerkte Position. |
| `DurationSeconds` | `long?` | Gesamtdauer. |
| `Title` | `string` | Anzeigetitel. |
| `PosterPictureId` | `long?` | Bild-Id. |

## `ContinueWatchingMutationResult`
Datei: `VideoPlayer.ApiClient/Models/ContinueWatching.cs`

Sealed record `ContinueWatchingMutationResult(string Status, string Message)` — Ergebnis von hide/skip (z. B. `{"status":"hidden","message":"ok"}`).

## `HomeLists`
Datei: `VideoPlayer.Mvu/HomeLists.cs` (Implizit aus Verwendung)

Bündel `ContinueWatching`, `Favorites`, `Recent`, `DiscardedCaches` — Ergebnis von `LoadHomeListsAsync`.

## `CachedListEntry` (Persistenz)
Datei: `VideoPlayer.Storage/Entities/` (Tabelle `cached_lists`)

| Spalte | Typ | Zweck |
|--------|-----|-------|
| `Key` | `string` | Cache-Schlüssel (`continue_watching`, `recent`, `sources`). |
| `Json` | `string` | Serialisierte Liste (`ContinueWatchingEntry[]` u. a.). |
| `UpdatedAtUtc` | `DateTime` | Schreibzeitpunkt. |

## `AppModel` (Auszug)
Datei: `VideoPlayer.Mvu/AppModel.cs`

| Eigenschaft | Typ | Bezug zur Anforderung |
|-------------|-----|----------------------|
| `ContinueWatching` | `IReadOnlyList<HomeMediaItem>` | Die anzuzeigende Weiterschauen-Liste. |
| `ConnectionState` | `ConnectionState` | Online/Offline-Verzweigung der Mutation. |
| `IsOffline` | `bool` (computed) | `ConnectionState == Offline`. |
| `Player` | `PlayerState` | Direktstart bei kurzem Tap — darf vom Kontextmenü unberührt bleiben. |
