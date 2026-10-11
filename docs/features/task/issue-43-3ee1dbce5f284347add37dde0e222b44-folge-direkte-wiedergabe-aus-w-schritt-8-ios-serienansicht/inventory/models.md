# Datenmodell — Serienansicht

## `MediaDetailsView`
Datei: `VideoPlayer.Mvu/MediaDetailsView.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Seasons` | `IReadOnlyList<SeasonItem>` | Staffeln der Serie (aus `MediaDetailsService` aufsteigend nach `Number` sortiert); wird als `ItemsSource` des `SeasonPicker` verwendet |
| `SelectedSeasonId` | `long?` | Vom Benutzer gewählte Staffel-Id; `null` = Vorwahl (erste Staffel bzw. Staffel der navigierten Episode) |
| `SelectedSeason` | `SeasonItem?` | Gewählte Staffel (`SelectedSeasonId`-Auflösung, Fallback `Seasons.FirstOrDefault()`) — wird `SeasonPicker.SelectedItem` zugewiesen |
| `SelectedSeasonEpisodes` | `IReadOnlyList<EpisodeItem>` | Episoden der gewählten Staffel — `ItemsSource` der `EpisodesCollection` |
| `SelectedEpisodeId` | `long?` | Kopfbereichs-Episode (Staffelwahl wählt die erste Episode) |
| `HasSeasons` | `bool` | `Seasons.Count > 0` — Sichtbarkeit der `SeasonSection` |
| `ShowId` | `long?` | Serienkontext-Id der Detailseite |

## `SeasonItem`
Datei: `VideoPlayer.Mvu/MediaDetailsView.cs` (Z. 6–28, `record`)

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `long` | Id der Staffel |
| `Number` | `int` | Staffelnummer (Sortierschlüssel) |
| `Name` | `string` | Anzeigename — bei leerem Servernamen „Staffel {Number}" (`ItemDisplayBinding` des Pickers) |
| `BannerPictureId` | `long?` | Staffelbild für den Kopfbereich |
| `Year` | `int?` | Erscheinungsjahr |
| `Episodes` | `IReadOnlyList<EpisodeItem>` | Episoden der Staffel (in der Serienantwort eingebettet) |

## `EpisodeItem`
Datei: `VideoPlayer.Mvu/MediaDetailsView.cs` (Z. 33 ff., `record`)

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `long` | Id der Episode |
| `Number` | `int` | Episodennummer innerhalb der Staffel |
| `Title` | `string` | Anzeigetitel |
| `Plot` | `string?` | Handlungstext |
| `PosterPictureId` | `long?` | Bild-Id des Episoden-Stills (`CachedPosterImage`) |
| `IsHighlighted` | `bool` | Navigierte Episode (visuell hervorgehoben) |
| `IsPlaceholder` | `bool` | Offline-Platzhalter ohne lokale Datei (nicht auswählbar, abgedunkelt) |
| `IsWatched` / `WatchedAt` | `bool` / `DateTime?` | Gesehen-Badge-Quelle (`WatchedIndicator`) |
| `ShowUnavailable` | `bool` | Sichtbarkeit des Hinweises „Nicht heruntergeladen" |
| `DisplayTitle` | `string` | Anzeigetitel inkl. Nummernpräfix |
| `HasLocalFile` | `bool` | Lokale Datei vorhanden (Offline) |

## `SeasonSelectedMessage`
Datei: `VideoPlayer.Mvu/AppMessage.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `SeasonId` | `long` | Vom `SeasonPicker` gemeldete Staffel-Id; wird von `AppUpdate.UpdateSeasonSelected` in `SelectedSeasonId` überführt |
