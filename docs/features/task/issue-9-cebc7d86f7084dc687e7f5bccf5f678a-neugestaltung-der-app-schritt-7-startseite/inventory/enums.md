# Enums

## `AppPage`
Datei: `VideoPlayer.Mvu/AppPage.cs`

| Wert | Bedeutung |
|------|-----------|
| `Loading` | Start-Ladeanzeige (kein Menüeintrag) |
| `Home` | Startseite (Route `home`) |
| `Sources` | Quellen (Route `sources`) |
| `Favorites` | Favoriten (Route `favorites`) |
| `Downloads` | Downloads (Route `downloads`) |
| `Profile` | Profil (Route `profile`) |
| `Settings` | Einstellungen (Route `settings`) |
| `ServerSetup` | Servereinrichtung/-verwaltung (Route `serversetup`, kein Menüeintrag) |
| `Login` | Anmeldemaske (Route `login`, kein Menüeintrag) |

## `ConnectionState`
Datei: `VideoPlayer.Mvu/ConnectionState.cs`

| Wert | Bedeutung |
|------|-----------|
| `Unknown` | Vor Beginn des Verbindungs-Workflows |
| `Connecting` | Erreichbarkeitsprüfung/automatische Anmeldung läuft |
| `Connected` | Verbunden und angemeldet |
| `NeedsLogin` | Anmeldemaske erforderlich |
| `NeedsServerSetup` | Servereinrichtung/-auswahl erforderlich |
| `Offline` | Server nicht erreichbar, Offline-Daten vorhanden |

## `ServerEventKind`
Datei: `VideoPlayer.Mvu/ServerEventKind.cs`

| Wert | Bedeutung |
|------|-----------|
| `ContinueWatchingUpdated` | Weiterschauen-Liste aktualisiert → `SyncContinueWatchingEffect` |
| `FavoritesChanged` | Favoriten geändert → `SyncFavoritesEffect` |
| `NewVideosScanned` | Neue Inhalte eingescannt → `SyncMetadataEffect` |

## `RecentEntryKind`
Datei: `VideoPlayer.ApiClient/Models/RecentEntry.cs`

| Wert | Bedeutung |
|------|-----------|
| `Movie`, `MovieCollection`, `TvShow`, `TvShowSeason`, `TvShowEpisode` | Art des zuletzt hinzugefügten Inhalts |

## `AuthAttemptStatus` / `ServerHealthStatus`
Dateien: `VideoPlayer.Mvu/AuthAttempt.cs`, `VideoPlayer.Mvu/ServerHealthStatus.cs`
— unverändert relevant (Start-Workflow); keine neuen Werte nötig.
