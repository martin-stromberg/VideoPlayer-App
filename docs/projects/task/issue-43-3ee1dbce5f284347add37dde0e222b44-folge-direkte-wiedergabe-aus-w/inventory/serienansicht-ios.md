# Detailbefund — Anforderung 5: iOS-Serienansicht (Episodenliste + Staffelauswahl)

## Aufbau der Serienansicht

`VideoPlayer.Maui/Pages/MediaDetailsPage.xaml` — Bereich `SeasonSection` (Z. 151–295):

- **`Picker x:Name="SeasonPicker"`** (Z. 154–159) in einem `Border` (Stil `InputFrame`):
  `Title="Staffel"`, `ItemDisplayBinding="{Binding Name}"`, `SelectedIndexChanged="OnSeasonSelected"`.
- **`CollectionView x:Name="EpisodesCollection"`** (Z. 160–294): `SelectionMode="Single"`,
  `SelectionChanged="OnEpisodeSelected"`; ItemTemplate = `Border`-Karte mit
  128×72-Still (`CachedPosterImage`), Angesehen-Badge, `DisplayTitle`, `Plot`,
  Platzhalter-Hinweis sowie Download-/Play-`Button`s.

**Auffällig:** `EpisodesCollection` (und die gesamte `SeasonSection`) steckt innerhalb des
`ScrollView x:Name="DetailsScroll"` (Z. 29–31 → `VerticalStackLayout` Z. 32 →
`SeasonSection` Z. 151). Eine `CollectionView` **innerhalb eines `ScrollView`** ist ein
bekanntes MAUI-Problem-Muster: Das CollectionView wird mit unbegrenzter Höhe gemessen und
rendert auf iOS (UICollectionView) häufig gar nicht oder leer — plausibler Grund, warum die
Episodenliste auf iOS fehlt, unter Windows aber funktioniert (**Vermutung; die Verschachtelung
ist nachweisbar, das iOS-Verhalten selbst wurde nicht ausgeführt**). Dieselbe Verschachtelung
gilt für `CollectionCollection` (Kollektions-Raster, Z. 303 ff. im selben `DetailsScroll`).
Gegenbeispiel: `ItemsCollection` der Quellen-Seite liegt in einem `Grid`
(`SourceOverviewPage.xaml` Z. 87–88) — dort tritt das Muster nicht auf.

## Daten- und Sortierpfad (Staffeln)

- Online: `MediaDetailsService.MapSeasons` (`VideoPlayer.Mvu/MediaDetailsService.cs`
  Z. 606–633): `show.Seasons.OrderBy(s => s.Number)` → `SeasonItem { Id, Number, Name,
  BannerPictureId, Year, Episodes }`; `Name` = Servername oder `Staffel {Number}` (Z. 614).
  Episoden `OrderBy(e => e.Number)`.
- Offline: `MapOfflineShow` (Z. 456–477) — identische Sortierung `OrderBy(s => s.Number)`.
- Vorwahl: `SelectedSeasonId` = Routen-Hint `season`, sonst Staffel der hervorgehobenen
  Episode (Z. 581–582 online / Z. 479–480 offline); `MediaDetailsView.SelectedSeason`
  fällt auf `Seasons.FirstOrDefault()` zurück (`MediaDetailsView.cs` Z. 234–235).

**Befund Sortierung:** Das Modell liefert die Staffeln **korrekt aufsteigend** sortiert —
die in der Anforderung beschriebene falsche Reihenfolge („Staffel 02", „Staffel 03",
„Staffel 01") entsteht also nicht im Datenpfad. (Vermutung: iOS-`Picker`-Darstellung/Binding,
z. B. bekannte MAUI-iOS-Picker-Probleme bei `ItemsSource`-Neuzuweisung.)

## Binding-Mechanik des Pickers (möglicher Jump-Back)

`MediaDetailsPage.xaml.cs`:

- `UpdateSeasonSection` (Z. 218–237) läuft bei **jedem** `UpdateUi`-Aufruf — also bei jeder
  `ModelChanged`-Meldung, solange `HasSeasons`. Es setzt **jedes Mal**:
  `SeasonPicker.ItemsSource = details.Seasons.ToList(); SeasonPicker.SelectedItem =
  details.SelectedSeason; EpisodesCollection.ItemsSource = details.SelectedSeasonEpisodes;`
  — geschützt durch das Flag `updatingSeasonPicker` (Z. 226–236).
- `OnSeasonSelected` (Z. 248–260): bei `updatingSeasonPicker` früh return; sonst dispatcht
  `SeasonSelectedMessage(season.Id)`.
- `AppUpdate.UpdateSeasonSelected` (`AppUpdate.cs` Z. 1611–1654): setzt
  `SelectedSeasonId` und `SelectedEpisodeId` (erste Episode der Staffel — Web-Parität),
  löst ggf. Kopfbereichs-Bild-Nachladen aus (`WithDetailsHeaderLoad`).

**Möglicher Mechanismus des „Zurückspringens" (Vermutung, nicht verifiziert):** Die
Bedingung `details.SelectedSeasonId == message.SeasonId` (Z. 1616) verhindert zwar redundante
Updates, aber `UpdateSeasonSection` weist `SelectedItem` bei jedem Model-Tick erneut zu.
Wenn der iOS-`Picker` beim Setzen von `ItemsSource`/`SelectedItem` `SelectedIndexChanged`
**verzögert/asynchron** auslöst (nach `updatingSeasonPicker = false`), kann ein `SelectedItem`
von `null`/alter Auswahl als `SeasonSelectedMessage` alter/erster Staffel durchgehen und das
Modell zurücksetzen → die Auswahl „springt zurück". Das Flag schützt nur synchrone Aufrufe.
Ebenso feuert `ModelChanged` bei *jeder* Model-Änderung (z. B. Favoriten-Update,
Download-Hinweis), wodurch `ItemsSource`/`SelectedItem` häufig neu gesetzt werden.

## iOS-spezifische Codepfade

- `#if IOS`/`#if MACCATALYST` in der neuen App nur an zwei Stellen:
  `VideoPlayer.Maui/Services/AppStorageLayoutProvider.cs` (Z. 76: `SetSkipBackupAttribute`)
  und `QrScannerPage.cs` / `MauiProgram.cs` (`#if ANDROID || IOS` Barcode-Scan).
  **Keine** iOS-spezifische Sonderbehandlung der Serienansicht — der Fehler liegt im
  gemeinsamen XAML/Code-Behind.
- iOS-Hinweis im Player: `PlayerPage.xaml.cs` Z. 263–288 (Seek erst ab ReadyToPlay,
  Toolkit-Issue #3249) — zeigt, dass iOS-Timing-Probleme im Projekt bereits dokumentiert sind.
- Altbestand: `VideoWebPlayer.Maui/TVShowDetailsPage.xaml(.cs)` +
  `ViewModels/TVShowDetailsViewModel.cs` (mit `Picker`-Verwendung; dort wurde
  offenbar dasselbe Feature gebaut — Vergleich möglich).

## Relevante Nachbarmodule

- `MediaDetailsView` (`VideoPlayer.Mvu/MediaDetailsView.cs`): `Seasons`, `SelectedSeason`,
  `SelectedSeasonEpisodes`, `SelectedEpisode`, `HeaderTitle`/`HeaderMeta`/`HeaderPlot`
  (Kopfbereich folgt der Staffel-/Episodenwahl).
- `MediaDetailsState` (`VideoPlayer.Mvu/MediaDetailsState.cs`): Identität, Lade-/Fehlerstand,
  `Details`, `HeaderImageBytes`.
- `MediaDetailsService` (`VideoPlayer.Mvu/MediaDetailsService.cs`): `LoadAsync` (Z. 65–95)
  verteilt je Medientyp; Serienkontext-Auflösung für `tvshowseason`/`tvshowepisode` über
  `show.Seasons` bzw. eingebetteten `Season.Show`-Bezug (Z. 233–299).
- Contract-Tests der Seite: `VideoWebPlayer.Maui.Tests/MediaDetailsPageContractTests.cs`,
  Update-Tests `AppUpdateTests_MediaDetails.cs`.
