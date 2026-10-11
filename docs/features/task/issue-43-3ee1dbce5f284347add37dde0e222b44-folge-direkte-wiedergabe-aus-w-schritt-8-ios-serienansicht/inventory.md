# Bestandsaufnahme: iOS-Bugfix — Serienansicht: Episodenliste und Staffelauswahl

Analyse der Serienansicht der Medien-Detailseite (`MediaDetailsPage` / `SeasonSection`), des zugehörigen MVU-Modells und der Testinfrastruktur bezogen auf die iOS-Fehler „fehlende Episodenliste", „falsche Staffelreihenfolge" und „zurückspringende Staffelauswahl".

## Zusammenfassung

- **Episodenliste:** `EpisodesCollection` ist eine `CollectionView` innerhalb des `ScrollView` `DetailsScroll` (`MediaDetailsPage.xaml` Z. 29–351, `SeasonSection` Z. 164–308). CollectionView-in-ScrollView ist ein bekanntes iOS-Renderproblem (die Liste wird u. U. mit Höhe 0 gemessen) — unter Windows funktioniert die Ansicht. `MediaCarousel` zeigt das projekteigene Alternativmuster: `StackLayout` + `BindableLayout.ItemsSource` + `TapGestureRecognizer` (innerhalb eines `ScrollView`).
- **Staffelreihenfolge:** Die Staffeln kommen aus dem Modell bereits aufsteigend sortiert — `MediaDetailsService` sortiert offline (`MapOfflineShow`, Z. 456–457) wie online (`MapSeasons`, Z. 606–610) mit `OrderBy(s.Number)`; Episoden je Staffel ebenfalls `OrderBy(e.Number)`. Die falsche Anzeigereihenfolge entsteht vermutlich im iOS-`Picker`, weil `UpdateSeasonSection` (`MediaDetailsPage.xaml.cs` Z. 229–248) bei **jedem** Model-Tick `ItemsSource` und `SelectedItem` neu setzt — der Picker baut seine Anzeige dabei wiederholt neu auf.
- **Zurückspringen der Auswahl:** `OnSeasonSelected` (Z. 259–271) wird nur über das synchrone Flag `updatingSeasonPicker` geschützt. Ein verzögertes iOS-`SelectedIndexChanged` (z. B. Echo des ItemsSource-/SelectedItem-Resets) läuft als `SeasonSelectedMessage` durch und setzt `SelectedSeasonId` zurück auf die erste Staffel. `AppUpdate.UpdateSeasonSelected` (Z. 1889–1932) prüft nur `SelectedSeasonId == message.SeasonId`, kein Echo-Schutz auf Seitenebene.
- **Keine iOS-Spezialbehandlung im Bestand:** `#if IOS` existiert nur für Backup/QR — der Fix liegt im gemeinsamen UI-Code.
- **Testinfrastruktur:** `MockVideoServer` liefert für `ContinueWatchingShowId` eine Serie **ohne** Staffeln (Z. 1615–1625) und für `PlaybackShowId` eine Serie mit **einer** Staffel/2 Episoden (Z. 1570–1608) — für einen Staffelwechsel-E2E fehlt ein Mehr-Staffeln-Fixture. `DevApiModelSnapshot` enthält keine Serienansicht-Felder (kein `SelectedSeason`, keine Episoden-/Staffelzahlen). Die Developer-API unterstützt `/invoke` mit `itemIndex` auch für `BindableLayout`-Container. `MediaDetailsPageContractTests` pinnen den String `EpisodesCollection` (Z. 68) — betroffen bei Umbenennung/Ersatz.

Test-Ausgangszustand: `dotnet test VideoWebPlayer.Maui.Tests` — 1285/1285 bestanden (siehe [Tests](inventory/tests.md)). Die E2E-Suite (`VideoPlayer.E2E.Tests`, FlaUI/Windows, ~20–25 min) wurde im Ausgangslauf nicht ausgeführt; die Serienansicht besitzt bislang keinen E2E-Test.

## Details

- [Datenmodell](inventory/models.md)
- [Logik](inventory/logic.md)
- [Tests](inventory/tests.md)
