# Enums — Bestandsaufnahme

## `AppPage` (`VideoPlayer.Mvu/AppPage.cs`)

| Wert | Bedeutung |
|------|-----------|
| `Loading` | Start-Ladeanzeige (kein Menüeintrag) |
| `Home` | Startseite |
| `Favorites` | Menüseite „Favoriten" |
| `Downloads` | Menüseite „Downloads" |
| `Profile` | Menüseite „Profil" |
| `Settings` | Menüseite „Einstellungen" |
| `ServerSetup` | Server-Einrichtung (geschobene Route, kein Menüeintrag) |
| `Login` | Anmeldemaske (geschobene Route, kein Menüeintrag) |

→ Neuer Wert `Playlists` (Menüseite) erforderlich; `EnumCoverageTests` deckt `AppPage` aktuell **nicht** als Enum-Abdeckungstest ab (die Tests listen andere Enums) — die Abdeckungspflicht greift über die Contract-Tests `AppRoutesTests`/`VideoPlayerMauiContractTests`, die `MenuPages`/FlyoutItems exakt pinnen.

## String-Konstanten statt Enums (Server-Playlist-API)

### `PlaylistSortModeValues` (Referenz `VideoWebPlayer.Client/Models/PlaylistSortModeValues.cs`)

| Konstante | Wert | Bedeutung |
|-----------|------|-----------|
| `ByReleaseDate` | `"ByReleaseDate"` | Sortierung nach Erscheinungsdatum |
| `Manual` | `"Manual"` | Manuelle Sortierung |

### `MediaTypeValues` (Referenz `VideoWebPlayer.Client/Models/MediaTypeValues.cs`)

| Konstante | Wert | Bedeutung |
|-----------|------|-----------|
| `Movie` | `"Movie"` | Film |
| `TVShowEpisode` | `"TVShowEpisode"` | Episode |
| `TVShowSeason` | `"TVShowSeason"` | Staffel |
| `TVShow` | `"TVShow"` | Serie |
| `MovieCollection` | `"MovieCollection"` | Filmkollektion |

→ Die App mappt über `MediaTypeNames.Normalize` (`VideoPlayer.Mvu/MediaTypeNames.cs`) auf die CLR-Konvention (`movie`, `moviecollection`, `tvshow`, `tvshowseason`, `tvshowepisode`); PascalCase-Werte wie `Movie`/`TVShowEpisode` werden bereits korrekt normalisiert (Case-insensitiv via `ToLowerInvariant`).

## Weitere betroffene Enums

- `ConnectionState` (`VideoPlayer.Mvu/ConnectionState.cs`): `Connected`/`Offline` steuern die Lade-Guards (Online-Feature → Laden nur in `Connected`; `Offline` zeigt Fehleranzeige).
- `ServerHealthStatus`: unverändert.
