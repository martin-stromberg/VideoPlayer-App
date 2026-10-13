# Enums und Typ-Normalisierung

## `ConnectionState`

Datei: `VideoPlayer.Mvu/ConnectionState.cs`

| Wert | Bedeutung |
|------|-----------|
| `Unknown` | Startzustand vor Verbindungs-Workflow |
| `Connecting` | Verbindungsaufbau läuft |
| `Connected` | Verbunden und angemeldet |
| `NeedsLogin` | Server erreichbar, Anmeldung erforderlich |
| `NeedsServerSetup` | Kein Server hinterlegt |
| `Offline` | Server nicht erreichbar, lokale Offline-Daten vorhanden — `AppModel.IsOffline` (`AppModel.cs:44`) bildet genau diesen Wert ab; Guard für die Offline-Fallunterscheidung der Anforderung |

## `AppPage`

Datei: `VideoPlayer.Mvu/AppPage.cs`

| Wert | Bedeutung |
|------|-----------|
| `Loading`, `Home`, `Favorites`, `Playlists`, `Downloads`, `Profile`, `Settings` | Menüseiten bzw. Start-Ladeanzeige |
| `ServerSetup`, `Login` | Geschobene Routen ohne Menüeintrag |

Hinweis: `mediadetails` und `player` sind keine `AppPage`-Werte — sie ändern `CurrentPage` nicht (die darunterliegende Menüseite bleibt aktiv).

## `MediaTypeNames` (statische Hilfsklasse, kein Enum)

Datei: `VideoPlayer.Mvu/MediaTypeNames.cs`

| Normalisierung (`Normalize`) | Ergebnis |
|------------------------------|----------|
| `movie` | `movie` |
| `collection` / `moviecollection` | `moviecollection` |
| `show` / `tvshow` | `tvshow` |
| `season` / `tvshowseason` | `tvshowseason` |
| `episode` / `tvshowepisode` | `tvshowepisode` |
| übrige / leer / `null` | kleingeschrieben durchgereicht / `string.Empty` |

Relevanz: Die Weiterschauen-Liste liefert `episode`; der Abspielbarkeits-Check in `UpdateContinueWatchingItemSelected` prüft die normalisierte CLR-Form (`movie`/`tvshowepisode`).
