# Inventur: Logik

## `MediaDetailsService`

Datei: `VideoPlayer.Mvu/MediaDetailsService.cs`

Lädt Detaildaten über `AuthSessionService.Client` und mappt sie auf
`MediaDetailsView`; ohne Client baut `LoadOfflineAsync` den Snapshot aus
Download-Manifest, `ShowDetailsCache` und Favoriten-Kopie.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `LoadAsync` | public | Einstieg; Medientyp-Routing (`api/items/{type}/{id}` über den Client) |
| `LoadOfflineAsync` | private | Offline-Snapshot je Medientyp |
| `LoadCollectionOrMovieAsync` | private | Kollektion mit Film-Fallback bei 404 |
| `LoadSeasonInShowContextAsync` | private | Staffel im Serienkontext (Serie laden, Staffel vorwählen) |
| `LoadEpisodeAsync` | private | Episode im Serienkontext (Hint bzw. `Season.Show`), sonst Basisdetails |
| `FetchShowAsync` | private | `GetTvShowAsync` + `ShowDetailsCache.WriteAsync` — Schreibpunkt des Offline-Serien-Caches |
| `MapOfflineShow` | private | baut `SeasonItem`/`EpisodeItem` aus `CachedShowEntry` (Z. 441-502) |
| `MapMovie` / `MapCollection` / `MapShow` / `MapSeasons` | private | Online-Mapping; `MapSeasons` (Z. 605-631) erzeugt die `EpisodeItem`s |

Relevante Detailbeobachtungen:

- `MapSeasons` mappt `Id`, `Number`, `Title` (Fallback „Episode {Nr}"),
  `Plot`, `ReleaseYear`, `PosterPictureId`
  (`GeneratedBackgroundPictureId ?? PosterPictureId`), `IsHighlighted` —
  `WatchedAt` der `TvShowEpisode` bleibt ungenutzt (Feld existiert im Modell
  noch nicht).
- `MapOfflineShow` setzt zusätzlich `HasLocalFile`/`IsPlaceholder` aus dem
  Download-Manifest; Quelle ist `CachedShowEpisode` (Id/Number/Title).
- `LoadEpisodeAsync` ohne Serienkontext liefert ein `MediaDetailsView` ohne
  `Seasons` — dort gibt es keine Episodenliste, ein Watched-Mapping wäre
  wirkungslos.

## `ShowDetailsCache`

Datei: `VideoPlayer.Mvu/ShowDetailsCache.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `WriteAsync` | public static | serialisiert `TvShow` → `CachedShowEntry` (JSON in `cached_lists`, `show:{id}`); wird von `FetchShowAsync` und dem Auto-Download-Vorlauf aufgerufen |
| `ReadAsync` | public static | deserialisiert tolerant (`PropertyNameCaseInsensitive`), Fehler → `null` |
| `OrderedEpisodes` / `NextEpisodeAfter` / `NextEpisodeAfterId` | public static | Fortsetzungslogik über die gecachte Folge |

Schreibaufrufer: `MediaDetailsService.FetchShowAsync` (Z. 328-333) und
`AutoDownloadService` (Vorlauf der Fortsetzung). JSON-Schemawechsel ist
abwärtskompatibel: unbekannte Felder werden beim Lesen ignoriert, fehlende
Felder liefern Standardwerte.

## `AppUpdate` — Gesehen-Übergänge

Datei: `VideoPlayer.Mvu/AppUpdate.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `UpdatePlaybackProgress` | private | Takt der Wiedergabeseite; setzt `PlayerState.IsWatched` bei `remaining <= WatchedEndWindowSeconds` (Z. 1696-1706), löst `DeleteWatchedDownloadEffect` bei `AutoDeleteWatchedEpisodes` aus |
| `UpdatePlaybackEnded` | private | Ende: `IsWatched = true` + `ReportPlaybackProgressEffect` mit Endposition (Z. 1727-1772); Auto-Fortsetzung via `AdvanceToNextEpisode` |
| `UpdatePlayerClosed` | private | Schließen melden Restposition; `PlayerState.Empty` (Z. 1802-1824) |
| `UpdateMediaDetailsRequested` | private | lädt Details je Identität; identisches Ziel mit geladenen Details → `Unchanged` (Z. 1074-1082) |
| `UpdateMediaDetailsLoaded` | private | übernimmt `MediaDetailsView`, stößt Kopfbild-Ladung an |
| `UpdateSeasonSelected` | private | Staffelwahl + Vorwahl der ersten Episode (Kopfbereich) |

Konsequenz für die Anforderung: Die Seite fragt Details nur einmal pro
Seiteninstanz an (`MediaDetailsPage.xaml.cs:80-92`, `requested`-Flag) und der
Übergang lädt identische Ziele nicht erneut. Ohne Aktualisierung des
geladenen `MediaDetailsView` bei `IsWatched`-Übergängen bliebe eine
innerhalb der Sitzung gesehene Episode in der Liste ohne Badge.

## `MediaDetailsPage` (Code-Behind)

Datei: `VideoPlayer.Maui/Pages/MediaDetailsPage.xaml.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `OnAppearing` | protected | einmalige `MediaDetailsRequestedMessage` pro Seiteninstanz |
| `UpdateUi` | private | rendert `MediaDetailsState`; `UpdateSeasonSection` setzt `EpisodesCollection.ItemsSource = details.SelectedSeasonEpisodes` (Z. 189-208) |
| `OnEpisodeSelected` / `OnEpisodePlayClicked` / `OnEpisodeDownloadClicked` | private | Benutzeraktionen auf `EpisodeItem` (Platzhalter nicht auswählbar) |

Da `UpdateUi` bei jeder Modelländerung `ItemsSource` neu setzt, greift eine
`with`-Aktualisierung der `EpisodeItem`s ohne weiteren UI-Code.
