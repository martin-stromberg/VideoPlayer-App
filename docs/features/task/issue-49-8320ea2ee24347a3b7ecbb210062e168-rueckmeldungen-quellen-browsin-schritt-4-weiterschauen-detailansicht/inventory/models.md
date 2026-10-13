# Datenmodell – Weiterschauen-Navigation

## `HomeMediaItem`

Datei: `VideoPlayer.Mvu/HomeMediaItem.cs`

Anzeigefertiger Eintrag der Startseiten-Listen. Für die Anforderung relevante Eigenschaften:

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `long` (required) | Id des Mediums (Navigationsziel der Detailseite) |
| `MediaType` | `string` (required) | Medientyp kleingeschrieben; Weiterschauen-Server-Konvention (`movie`, `show`, `season`, `episode`, `collection`) oder CLR-Konvention |
| `Title` | `string` (required) | Anzeigetitel (Titel-Hint der Detailseite) |
| `Progress` | `double?` | Fortschrittsbalken 0–1 (nur Weiterschauen) |
| `ShowId` | `long?` | Serienkontext-Hint (Episoden-/Staffel-Einträge) |
| `SeasonId` | `long?` | Staffel-Hint (Offline-Liste: nur an Platzhaltern belegt) |
| `EpisodeId` | `long?` | Episoden-Hint (Offline-Liste: `= VideoId` bei Episoden) |
| `PlaylistId` | `long?` | Playlist-Kontext des Eintrags (direkter Playlist-Start) |
| `PlaylistName` | `string?` | Name der Playlist |
| `PlaylistEntryId` | `long?` | Id des Playlist-Eintrags; `null`, wenn Medium nicht mehr in der Playlist |
| `IsPlaceholder` | `bool` | Offline-Platzhalter fehlender Folge-Episoden (nicht abspielbar) |
| `IsSelectable` | `bool` (abgeleitet) | `!IsPlaceholder` — Guard-Grundlage |

## `PlayerState` (relevante Eigenschaften)

Datei: `VideoPlayer.Mvu` (`PlayerState` — Verwendung in `AppUpdate.StartPlayback`)

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `MediaType` / `MediaId` / `Title` | `string?` / `long` / `string?` | Aktive Wiedergabe-Identität |
| `IsResolving` / `Attempted` | `bool` | Quellauflösung läuft / Player-Seite wurde geöffnet |
| `UpcomingEpisodes` | `IReadOnlyList<PlayerEpisodeRef>` | Fortsetzungsliste (Folgevideo-Funktion) |
| `IsActive` / `HasNextEpisode` | `bool` | Abgeleitet: Wiedergabe aktiv / Folge-Episode vorhanden |
| `PlayerState.Empty` | statisch | Inaktiver Zustand nach `PlayerClosedMessage` |

## `MediaDetailsState` / `MediaDetailsView` (relevante Eigenschaften)

Datei: `VideoPlayer.Mvu` (`MediaDetailsState`, `MediaDetailsView` — Verwendung in `AppUpdate`/`MediaDetailsPage`)

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `MediaType` / `MediaId` | `string?` / `long` | Identität des globalen Detail-Slots (Rückkehr-Guard der Seite) |
| `Details` | `MediaDetailsView?` | Geladenes Anzeigemodell; `NextEpisodeQueue` liest `Details.Seasons` |
| `IsLoading` / `Attempted` / `Error` / `HasError` | `bool` / `string?` | Ladezustand und Fehleranzeige |
| `MediaDetailsView.IsPlayable` | `bool` | Blendet „Abspielen" ein |
| `MediaDetailsView.IsOfflineSnapshot` | `bool` | Offline-Variante (Favoriten-/Playlist-/Download-Schalter deaktiviert) |
| `MediaDetailsView.HasLocalFile` | `bool` | Offline: „Abspielen" nur mit lokaler Datei |
| `MediaDetailsView.ShowId` / `SelectedSeason` / `Seasons` | `long?` / `SeasonItem?` / Liste | Serienkontext der Serienansicht |
