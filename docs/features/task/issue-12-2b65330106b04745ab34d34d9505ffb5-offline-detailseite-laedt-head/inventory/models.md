# Datenmodell

## `MediaDetailsState`

Datei: `VideoPlayer.Mvu/MediaDetailsState.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Empty` | `MediaDetailsState` (statisch) | Ausgangszustand (kein Medium angefragt) |
| `MediaType` | `string?` | Angefragter Medientyp (normalisiert) |
| `MediaId` | `long` | Angefragte Medien-Id |
| `IsLoading` | `bool` | Detaildaten werden geladen |
| `Attempted` | `bool` | Mindestens eine Anfrage gestellt |
| `Error` | `string?` | Fehlertext des letzten Ladeversuchs |
| `IsFavoriteUpdating` | `bool` | Favoriten-Umschaltung läuft |
| `FavoriteError` | `string?` | Fehlertext der Favoriten-Umschaltung |
| `DownloadNotice` | `string?` | Hinweistext der Download-Anforderung |
| `DownloadNoticeIsError` | `bool` | Hinweis ist Fehler/Ablehnung |
| `Details` | `MediaDetailsView?` | Geladenes Anzeigemodell; bleibt beim Offline-Übergang erhalten |
| `HeaderImageBytes` | `byte[]?` | Bilddaten des Kopfbereichs (flüchtig); `null` = nicht geladen/fehlgeschlagen |
| `IsHeaderImageLoading` | `bool` | Kopfbereichs-Bild wird geladen |
| `HasHeaderImage` | `bool` (abgeleitet) | `HeaderImageBytes is { Length: > 0 }` |
| `HasDetails` | `bool` (abgeleitet) | `Details is not null` |
| `HasError` | `bool` (abgeleitet) | `Error is not null` |

## `MediaDetailsView` (relevante Eigenschaften)

Datei: `VideoPlayer.Mvu/MediaDetailsView.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `MediaType` / `MediaId` | `string` / `long` | Identität der Detailseite (wird von `MediaDetailsHeaderImageLoadedMessage` gegengeprüft) |
| `BannerPictureId` | `long?` | Banner/Fanart/Poster-Quelle des Mediums; im Offline-Snapshot nicht gesetzt |
| `SelectedEpisodeId` | `long?` | Ausgewählte Kopfbereichs-Episode; im Offline-Snapshot bei `tvshowepisode` gesetzt |
| `SelectedSeasonId` | `long?` | Gewählte Staffel |
| `Seasons` | `IReadOnlyList<SeasonItem>` | Staffelstruktur (je `SeasonItem.BannerPictureId` als Staffel-Bildquelle) |
| `HeaderEpisodeId` | `long?` (abgeleitet) | `SelectedEpisodeId is > 0 ? SelectedEpisodeId : null` — Bildquelle `api/episodes/{id}/background-image` |
| `HeaderPictureId` | `long?` (abgeleitet) | `HeaderEpisodeId is > 0 ? null : (tvshowseason ? SelectedSeason?.BannerPictureId ?? BannerPictureId : BannerPictureId)` — Bildquelle `api/pictures/{id}` |
| `IsOfflineSnapshot` | `bool` | Snapshot ohne Serververbindung gebaut |

## `HomeHeroState` (Referenzmuster Startseite)

Datei: `VideoPlayer.Mvu/HomeHeroState.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `IsLoading` | `bool` | Hero-Bild wird geladen |
| `ImageBytes` | `byte[]?` | Bilddaten des Hero-Hintergrunds |
| `HasImage` | `bool` (abgeleitet) | `ImageBytes is { Length: > 0 }` — Guard in `WithHeroLoad` gegen wiederholtes Laden |

## `AppModel` (relevante Eigenschaften)

Datei: `VideoPlayer.Mvu/AppModel.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `ConnectionState` | `ConnectionState` | Verbindungszustand (`Connecting`-Startwert) |
| `IsOnline` | `bool` | Letzter gemeldeter Netzwerk-Ist-Wert |
| `IsOffline` | `bool` (abgeleitet) | `ConnectionState == Offline` |
| `MediaDetails` | `MediaDetailsState` | Zustand der Detailseite; wird beim Offline-Übergang nicht zurückgesetzt |
| `HomeHero` | `HomeHeroState` | Zustand des Startseiten-Hero |
| `CurrentPage` | `AppPage` | Aktive Seite |

## `LoadMediaDetailsHeaderEffect`

Datei: `VideoPlayer.Mvu/AppEffect.cs`

| Parameter | Typ | Beschreibung / Zweck |
|-----------|-----|----------------------|
| `MediaType` | `string` | Medientyp (CLR-Konvention) |
| `MediaId` | `long` | Medien-Id |
| `HeaderPictureId` | `long?` | Bild-Id für `api/pictures/{id}`; `null` bei Episoden-Still |
| `HeaderEpisodeId` | `long?` | Episoden-Id des Stills; `null` bei Bildquelle |

## `MediaDetailsHeaderImageLoadedMessage`

Datei: `VideoPlayer.Mvu/AppMessage.cs`

| Parameter | Typ | Beschreibung / Zweck |
|-----------|-----|----------------------|
| `MediaType` / `MediaId` | `string` / `long` | Identität der Detailseite |
| `HeaderPictureId` / `HeaderEpisodeId` | `long?` | Angefragte Bildquelle (Identitätsabgleich) |
| `ImageBytes` | `byte[]?` | Bilddaten; `null` bei Fehlschlag/fehlendem Bild |
