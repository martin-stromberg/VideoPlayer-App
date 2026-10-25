# Mobile Startseite und Datenfluss

## Darstellung

`VideoPlayer.Maui/Pages/HomePage.xaml` enthält einen optionalen Hero-Kopf und drei `MediaCarousel`-Listen: Weiterschauen, Favoriten und Neu im Programm. Diese binden an `Model.ContinueWatching`, `Model.Favorites` und `Model.RecentEntries`. Es existiert keine dynamische Sammlung von Genre-Listen.

## Zustand, Synchronisation und Cache

- `VideoPlayer.Mvu/HomeLists.cs` enthält drei separate Eigenschaften (ContinueWatching/Favorites/Recent).
- `VideoPlayer.Mvu/HomeListService.cs` liest diese aus den lokalen Tabellen: continue_watching/recent in `cached_lists`, Favoriten in `cached_favorites`; es mappt die Einträge auf `HomeMediaItem`.
- `VideoPlayer.Mvu/ServerSyncService.cs` lädt Neuheiten über `GET api/items/recent` und schreibt sie in den Schlüssel `recent`; Favoriten und Weiterschauen werden separat synchronisiert.
- Startseiten-Neuladen erfolgt über die Home-Listen-Messages/Effects nach Cacheänderungen. Der vorhandene Cache-First-Pfad erlaubt Offlineanzeige.

Ein saisonaler Bereich braucht somit eine Erweiterung des API-Modells, des Synchronisations-/Cachepfads, des MVU-Zustands und der dynamischen Startseitendarstellung. Beim Cache muss festgelegt werden, wie Ablauf/Zeitraum beim Offline-Start behandelt wird; bestehende Listen werden offline angezeigt, ohne dokumentierte zeitliche Validierung.

## Relevante Tests

`VideoPlayer.Maui.Tests/HomeListServiceTests.cs` deckt Mapping, leere Listen und Cacheverhalten ab. `HomePageContractTests.cs` prüft statisch die drei fest verdrahteten Home-Listen und Bindings. AppUpdate-Contract-Tests prüfen Home-Listen-Load-/Refresh-Verdrahtung. Diese Tests enthalten aktuell keine saisonalen Bereiche oder Ablaufbedingungen.
