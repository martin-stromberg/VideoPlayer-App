# Logik — Serienansicht

## `MediaDetailsPage` (Code-Behind)
Datei: `VideoPlayer.Maui/Pages/MediaDetailsPage.xaml.cs`

| Methode / Member | Sichtbarkeit | Kurzbeschreibung |
|------------------|-------------|------------------|
| `updatingSeasonPicker` | `private bool` | Schutzflag: synchrones `SelectedIndexChanged` während programmatischer Picker-Updates wird ignoriert; deckt **keine** verzögerten (iOS-)Events ab |
| `UpdateUi(AppModel)` | `private` | Zentraler Model-Tick-Handler; ruft u. a. `UpdateSeasonSection` bei jeder Modelländerung auf (auch bei Folgemeldungen wie `MediaDetailsHeaderImageLoadedMessage`) |
| `UpdateSeasonSection(MediaDetailsView)` | `private` | Setzt `SeasonSection.IsVisible` und — bei `HasSeasons` — bei **jedem** Tick `SeasonPicker.ItemsSource = details.Seasons.ToList()`, `SeasonPicker.SelectedItem = details.SelectedSeason` und `EpisodesCollection.ItemsSource = details.SelectedSeasonEpisodes` (Z. 229–248) |
| `OnSeasonSelected(object?, EventArgs)` | `private` | `SelectedIndexChanged`-Handler: bei gesetztem `updatingSeasonPicker` Rückkehr; sonst `SeasonSelectedMessage(season.Id)` an `mvuHost.DispatchAsync` (Z. 259–271) |
| `OnEpisodeSelected(object?, SelectionChangedEventArgs)` | `private` | `SelectionChanged` der `EpisodesCollection`: Auswahl zurücksetzen, Platzhalter ignorieren, sonst `MediaItemSelectedMessage` zur Episoden-Detailseite (Z. 273–299) |
| `OnModelChanged` | `private` | `ModelChanged`-Abo → `MainThread.BeginInvokeOnMainThread` → `UpdateUi` |
| `OnEpisodePlayClicked` / `OnEpisodeDownloadClicked` | `private` | Aktions-Schalter des Episoden-Templates (`Clicked`, `BindingContext` = `EpisodeItem`) |

Abonnierte Events: `mvuHost.ModelChanged`, `SizeChanged`, `SeasonPicker.SelectedIndexChanged`, `EpisodesCollection.SelectionChanged`.
Publizierte Nachrichten: `SeasonSelectedMessage`, `MediaItemSelectedMessage`, `PlayRequestedMessage`, `DownloadRequestedMessage`.

## `MediaDetailsPage` (XAML-Struktur)
Datei: `VideoPlayer.Maui/Pages/MediaDetailsPage.xaml`

| Element | Beschreibung |
|---------|--------------|
| `DetailsScroll` | `ScrollView` (Z. 29–351), enthält die gesamte Detailansicht |
| `SeasonSection` | `VerticalStackLayout` (Z. 164–308) im Scroll-Inhalt: `SeasonPicker` (`Picker` in `Border`, `ItemDisplayBinding={Binding Name}`, `SelectedIndexChanged=OnSeasonSelected`) + `EpisodesCollection` (`CollectionView`, `SelectionMode=Single`, `SelectionChanged=OnEpisodeSelected`, `DataTemplate x:DataType=mvu:EpisodeItem`) |
| `CollectionSection` | `VerticalStackLayout` mit `CollectionCollection` (`CollectionView`, `GridItemsLayout` Span 3) — ebenfalls CollectionView-in-ScrollView, aber außerhalb dieser Anforderung |

## `MediaDetailsService`
Datei: `VideoPlayer.Mvu/MediaDetailsService.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `MapOfflineShow` | `private` | Offline-Snapshot der Serienansicht: `show.Seasons.OrderBy(s.Number)` → `SeasonItem` mit `Episodes.OrderBy(e.Number)` (Z. 456–477); `SelectedSeasonId` = übergebene Auswahl oder Staffel der navigierten Episode |
| `MapShow` | `private static` | Online-Serienansicht: `MapSeasons` + `SelectedSeasonId`-Vorwahl (Z. 570–604) |
| `MapSeasons` | `private static` | `seasons.OrderBy(s.Number)` → `SeasonItem` (Name „Staffel {n}" bei leerem Servernamen), `Episodes.OrderBy(e.Number)` (Z. 606–632) |

**Befund:** Staffel- und Episodenreihenfolge ist modellseitig bereits korrekt aufsteigend — keine Änderung nötig.

## `AppUpdate`
Datei: `VideoPlayer.Mvu/AppUpdate.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `UpdateSeasonSelected` | `private static` | Reagiert auf `SeasonSelectedMessage`: No-Op bei `null`-Details/fehlenden Staffeln/`SelectedSeasonId == message.SeasonId`; sonst `SelectedSeasonId` setzen + erste Episode der Staffel als `SelectedEpisodeId`, bei geänderter Kopfbildquelle `HeaderImageBytes` verwerfen und `LoadMediaDetailsHeaderEffect` auslösen (Z. 1889–1932) |

## `DevApiModelSnapshot`
Datei: `VideoPlayer.Mvu/DevApi/DevApiModelSnapshot.cs` (`#if DEBUG || DEV_API`)

| Methode / Bereich | Kurzbeschreibung |
|-------------------|------------------|
| `FromModel(AppModel)` | Kuratierter JSON-Snapshot für `GET /model`; enthält **keine** Serienansicht-Felder (kein `SelectedSeasonId`, keine Staffel-/Episodenzahlen) — für den E2E-Nachweis der Serienansicht zu erweitern |

## `MockVideoServer`
Datei: `VideoPlayer.E2E.Tests/Support/MockVideoServer.cs`

| Methode / Bereich | Kurzbeschreibung |
|-------------------|------------------|
| `HandleTvShowItem` | `GET api/items/tvshow/{id}`: `PlaybackShowId`-Branch liefert 1 Staffel („Staffel 1", Id 9001) mit 2 Episoden (Z. 1570–1608); `ContinueWatchingShowId`-Branch liefert die Serie **ohne** `Seasons` (Z. 1615–1625) |
| `ListOverride` `/api/continue-watching` | `mediaType="show"`-Eintrag (`ContinueWatchingShowId`) — Tap öffnet die Serien-Detailseite (Bestandstest `TapContinueWatchingShowEntry_OpensDetails_NotPlayback`) |

## `MediaCarousel` (Referenzmuster)
Dateien: `VideoPlayer.Maui/Controls/MediaCarousel.xaml(.cs)`

`HorizontalStackLayout` mit `BindableLayout.ItemsSource`/`BindableLayout.ItemTemplate` innerhalb eines `ScrollView`; Eintrags-Auswahl über `TapGestureRecognizer` (`OnItemTapped`, `BindingContext` des sendenden Elements). Projektetabliertes Muster für Listendarstellung in Scroll-Containern — Grundlage des Episodenlisten-Umbaus.
